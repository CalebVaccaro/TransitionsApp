using System.Text.Json;

public class Song
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public int Bpm { get; set; }
    public string Key { get; set; }

    public void Display()
    {
        Console.WriteLine($"{Name}");
    }

    public override string ToString() => $"{Name}";
}

public class Transition
{
    public string FromSongId { get; set; }
    public List<string> ToSongIds { get; set; } = new();
}

class Program
{
    static List<Song> songs = new();
    static List<Transition> transitions = new();

    static readonly string songsFile = Path.Combine(AppContext.BaseDirectory, "songs.json");
    static readonly string transitionsFile = Path.Combine(AppContext.BaseDirectory, "transitions.json");

    static void Main()
    {
        LoadSongs();
        LoadTransitions();

        while (true)
        {
            Console.WriteLine("\n🎵 Song Transition Manager");
            Console.WriteLine("1. Add Song");
            Console.WriteLine("2. View All Songs");
            Console.WriteLine("3. Link Transition");
            Console.WriteLine("4. View Transitions from Song");
            Console.WriteLine("5. Exit");
            Console.WriteLine("6. Import Songs from Folder");
            Console.Write("Choose: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1": AddSong(); break;
                case "2": ViewAllSongs(); break;
                case "3": LinkTransition(); break;
                case "4": ViewTransitions(); break;
                case "5":
                    SaveSongs();
                    SaveTransitions();
                    return;
                case "6": ImportSongsFromFolder(); break;
                default: Console.WriteLine("Invalid choice."); break;
            }
        }
    }

    static void AddSong()
    {
        Console.Write("Enter Song Name: ");
        string name = Console.ReadLine();
        Console.Write("Enter BPM: ");
        if (!int.TryParse(Console.ReadLine(), out int bpm))
        {
            Console.WriteLine("Invalid BPM.");
            return;
        }
        Console.Write("Enter Key (e.g. A Minor, C# Major): ");
        string key = Console.ReadLine();

        songs.Add(new Song { Name = name, Bpm = bpm, Key = key });
        Console.WriteLine("✅ Song added.");
    }

    static void ViewAllSongs()
    {
        if (songs.Count == 0)
        {
            Console.WriteLine("No songs added.");
            return;
        }

        Console.WriteLine("\n🎶 Song List:");
        for (int i = 0; i < songs.Count; i++)
        {
            Console.Write($"{i + 1}. ");
            songs[i].Display();
        }
    }

    static void LinkTransition()
    {
        if (songs.Count < 2)
        {
            Console.WriteLine("Add at least two songs to link.");
            return;
        }

        ViewAllSongs();
        Console.Write("Select base song #: ");
        if (!int.TryParse(Console.ReadLine(), out int fromIndex) || fromIndex < 1 || fromIndex > songs.Count)
        {
            Console.WriteLine("Invalid song number.");
            return;
        }

        Console.Write("Select transition song #: ");
        if (!int.TryParse(Console.ReadLine(), out int toIndex) || toIndex < 1 || toIndex > songs.Count)
        {
            Console.WriteLine("Invalid song number.");
            return;
        }

        if (fromIndex == toIndex)
        {
            Console.WriteLine("Cannot transition a song to itself.");
            return;
        }

        string fromId = songs[fromIndex - 1].Id;
        string toId = songs[toIndex - 1].Id;

        var transition = transitions.FirstOrDefault(t => t.FromSongId == fromId);
        if (transition == null)
        {
            transition = new Transition { FromSongId = fromId };
            transitions.Add(transition);
        }

        if (!transition.ToSongIds.Contains(toId))
        {
            transition.ToSongIds.Add(toId);
            Console.WriteLine($"🔗 Linked '{songs[fromIndex - 1].Name}' to '{songs[toIndex - 1].Name}'");
        }
        else
        {
            Console.WriteLine("These songs are already linked.");
        }
    }

    static void ViewTransitions()
    {
        ViewAllSongs();
        Console.Write("Select a song #: ");
        if (!int.TryParse(Console.ReadLine(), out int index) || index < 1 || index > songs.Count)
        {
            Console.WriteLine("Invalid selection.");
            return;
        }

        var song = songs[index - 1];
        var transition = transitions.FirstOrDefault(t => t.FromSongId == song.Id);

        Console.WriteLine($"\n➡️ Transitions from '{song.Name}':");

        if (transition == null || transition.ToSongIds.Count == 0)
        {
            Console.WriteLine("No transitions linked yet.");
        }
        else
        {
            foreach (var id in transition.ToSongIds)
            {
                var target = songs.FirstOrDefault(s => s.Id == id);
                if (target != null) target.Display();
            }
        }
    }

    static void ImportSongsFromFolder()
    {
        Console.Write("Enter full folder path (e.g., /Volumes/MusicDrive/Tracks): ");
        string folderPath = Console.ReadLine();

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine("❌ Directory does not exist.");
            return;
        }

        var audioFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f =>
                (f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                 f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) &&
                !Path.GetFileName(f).StartsWith("._"))
            .ToList();

        if (audioFiles.Count == 0)
        {
            Console.WriteLine("No .mp3 or .wav files found in folder.");
            return;
        }

        int added = 0;
        foreach (var file in audioFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            if (!songs.Any(s => s.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                songs.Add(new Song
                {
                    Name = fileName,
                    Bpm = 0,
                    Key = "Unknown"
                });
                added++;
            }
        }

        Console.WriteLine($"✅ Imported {added} new songs from folder.");
    }

    static void SaveSongs()
    {
        var json = JsonSerializer.Serialize(songs, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(songsFile, json);
    }

    static void SaveTransitions()
    {
        var json = JsonSerializer.Serialize(transitions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(transitionsFile, json);
    }

    static void LoadSongs()
    {
        if (File.Exists(songsFile))
        {
            var json = File.ReadAllText(songsFile);
            songs = JsonSerializer.Deserialize<List<Song>>(json) ?? new List<Song>();
        }
    }

    static void LoadTransitions()
    {
        if (File.Exists(transitionsFile))
        {
            var json = File.ReadAllText(transitionsFile);
            transitions = JsonSerializer.Deserialize<List<Transition>>(json) ?? new List<Transition>();
        }
    }
}
