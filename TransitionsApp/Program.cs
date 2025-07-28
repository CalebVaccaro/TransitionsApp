using System;
using System.Collections.Generic;
using System.Linq;

class Song
{
    public string Name { get; set; }
    public int Bpm { get; set; }
    public string Key { get; set; }
    public List<Song> Transitions { get; set; } = new();

    public void Display()
    {
        Console.WriteLine($"- {Name} | BPM: {Bpm} | Key: {Key}");
    }
}

class Program
{
    static List<Song> songs = new();

    static void Main()
    {
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
                case "5": return;
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

        Song from = songs[fromIndex - 1];
        Song to = songs[toIndex - 1];

        if (!from.Transitions.Contains(to))
        {
            from.Transitions.Add(to);
            Console.WriteLine($"🔗 Linked '{from.Name}' to '{to.Name}'");
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

        Song song = songs[index - 1];
        Console.WriteLine($"\n➡️ Transitions from '{song.Name}':");

        if (song.Transitions.Count == 0)
        {
            Console.WriteLine("No transitions linked yet.");
        }
        else
        {
            foreach (var s in song.Transitions)
                s.Display();
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

            // Avoid duplicates
            if (!songs.Any(s => s.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
            {
                songs.Add(new Song
                {
                    Name = fileName,
                    Bpm = 0, // Default until filled manually
                    Key = "Unknown"
                });
                added++;
            }
        }

        Console.WriteLine($"✅ Imported {added} new songs from folder.");
    }
}
