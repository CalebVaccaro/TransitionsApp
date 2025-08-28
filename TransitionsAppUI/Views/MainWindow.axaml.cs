using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace TransitionsAppUI
{
    public partial class MainWindow : Window
    {
        private List<Song> songs = new();
        private List<Transition> transitions = new();
        private List<Song> setList = new();

        private string SongsFile => Path.Combine(AppContext.BaseDirectory, "songs.json");
        private string TransitionsFile => Path.Combine(AppContext.BaseDirectory, "transitions.json");

        private List<string> watchedFolders = new();
        private string WatchedFoldersFile => Path.Combine(AppContext.BaseDirectory, "watchedFolders.json");

        public MainWindow()
        {
            InitializeComponent();

            LoadSongs();
            LoadTransitions();
            LoadWatchedFolders(); 
            RefreshUI();

            //AddSongButton.Click += AddSongButton_Click;
            LinkButton.Click += LinkButton_Click;
            ViewTransitionsButton.Click += ViewTransitionsButton_Click;
            //ImportFolderButton.Click += ImportFolderButton_Click;
            SearchButton.Click += SearchButton_Click;
            FromSongComboBox.DropDownOpened += (_, __) => fromBoxTouched = true;
            ToSongComboBox.DropDownOpened += (_, __) => toBoxTouched = true;
            AddFromSongListButton.Click += (_, __) => AddSongToSetFrom(SongsListBox);
            //AddFromTransitionListButton.Click += (_, __) => AddSongToSetFrom(TransitionsListBox);
            AddFromTransitionListButton.Click += AddFromTransitionListButton_Click;
            ClearSetListButton.Click += (_, __) => { setList.Clear(); RefreshSetList(); };
            FromSearchBox.TextChanged += (_, __) => FilterFromDropdown();
            ToSearchBox.TextChanged += (_, __) => FilterToDropdown();
            ViewSearchBox.TextChanged += (_, __) => FilterViewDropdown();
            SaveSetListButton.Click += SaveSetListButton_Click;
            RemoveSelectedSetListButton.Click += (_, __) => RemoveSelectedSongFromSetList();
            LoadSetListButton.Click += async (_, __) => await LoadSetListFromFileAsync();
            ClearSearchButton.Click += ClearSearchButton_Click;
            SongsListBox.SelectionChanged += SongsListBox_SelectionChanged;
            AddWatchFolderButton.Click += AddWatchFolderButton_Click;
            RemoveWatchFolderButton.Click += RemoveWatchFolderButton_Click;
            ScanWatchedFoldersButton.Click += ScanWatchedFoldersButton_Click;
        }

        // private void AddSongButton_Click(object? sender, RoutedEventArgs e)
        // {
        //     string name = CleanSongName(NameTextBox.Text?.Trim() ?? "");
        //     if (!int.TryParse(BpmTextBox.Text, out int bpm))
        //     {
        //         ShowMessage("Invalid BPM");
        //         return;
        //     }
        //     string key = KeyTextBox.Text?.Trim() ?? "";

        //     if (string.IsNullOrEmpty(name))
        //     {
        //         ShowMessage("Song Name cannot be empty");
        //         return;
        //     }

        //     if (songs.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        //     {
        //         ShowMessage("Song already exists");
        //         return;
        //     }

        //     songs.Add(new Song { Name = name, Bpm = bpm, Key = key });
        //     SaveSongs();
        //     RefreshUI();
        //     ClearAddSongFields();
        // }

        private void LinkButton_Click(object? sender, RoutedEventArgs e)
        {
            if (FromSongComboBox.SelectedItem is Song fromSong && ToSongComboBox.SelectedItem is Song toSong)
            {
                if (fromSong.Id == toSong.Id)
                {
                    ShowMessage("Cannot link a song to itself");
                    return;
                }

                var transition = transitions.FirstOrDefault(t => t.FromSongId == fromSong.Id);
                if (transition == null)
                {
                    transition = new Transition { FromSongId = fromSong.Id };
                    transitions.Add(transition);
                }

                if (!transition.ToSongIds.Contains(toSong.Id))
                {
                    transition.ToSongIds.Add(toSong.Id);
                    SaveTransitions();
                    ShowMessage($"Linked '{fromSong.Name}' → '{toSong.Name}'");
                    ViewTransitions();
                }
                else
                {
                    ShowMessage("Songs already linked");
                }
            }
            else
            {
                ShowMessage("Select both songs to link");
            }
        }

        private void ViewTransitions()
        {
            if (ViewTransitionsComboBox.SelectedItem is Song selectedSong)
            {
                ShowTransitionsFor(selectedSong);
            }
            else
            {
                ShowMessage("Select a song to view transitions");
            }
        }

        private void ViewTransitionsButton_Click(object? sender, RoutedEventArgs e)
        {
            ViewTransitions();
        }

        private void ShowTransitionsFor(Song selectedSong)
        {
            var transition = transitions.FirstOrDefault(t => t.FromSongId == selectedSong.Id);

            if (transition == null || transition.ToSongIds.Count == 0)
            {
                TransitionsListBox.ItemsSource = Array.Empty<string>();
                //ShowMessage("No transitions for this song");
                return;
            }

            var linkedSongs = songs
                .Where(s => transition.ToSongIds.Contains(s.Id))
                .Select(s => $"{s.Name}")
                .ToList();

            TransitionsListBox.ItemsSource = linkedSongs;
        }

        private void SongsListBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (SongsListBox.SelectedItem is string label)
            {
                string name = label.Split('|')[0].Trim();
                var song = songs.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (song != null)
                {
                    // Set FromSong and ViewTransitions
                    FromSongComboBox.SelectedItem = song;
                    ViewTransitionsComboBox.SelectedItem = song;
                    // Immediately enact ViewTransitions logic
                    ViewTransitions();
                }
            }
        }

        private async void ImportFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder with Songs"
            };

            var folderPath = await dialog.ShowAsync(this);

            if (!string.IsNullOrWhiteSpace(folderPath))
            {
                AddSongsFromDirectory(folderPath);
            }
        }

        private bool fromBoxTouched = false;
        private bool toBoxTouched = false;

        private void SearchButton_Click(object? sender, RoutedEventArgs e)
        {
            string searchTerm = SearchTextBox.Text?.Trim() ?? "";

            var filtered = string.IsNullOrWhiteSpace(searchTerm)
                ? songs
                : songs.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

            SongsListBox.ItemsSource = filtered
                .Select(s => $"{s.Name}")
                .ToList();
        }

        private void RefreshUI()
        {
            SongsListBox.ItemsSource = songs.Select(s => $"{s.Name}").ToList();

            FromSongComboBox.ItemsSource = songs;
            FromSongComboBox.SelectedIndex = -1;
            ToSongComboBox.ItemsSource = songs;
            ToSongComboBox.SelectedIndex = -1;
            ViewTransitionsComboBox.ItemsSource = songs;
            ViewTransitionsComboBox.SelectedIndex = -1;

            RefreshSetList();
            RefreshWatchedFoldersUI(); 
        }

        // private void ClearAddSongFields()
        // {
        //     NameTextBox.Text = "";
        //     BpmTextBox.Text = "";
        //     KeyTextBox.Text = "";
        // }

        private void ShowMessage(string msg)
        {
            var dialog = new Window
            {
                Width = 300,
                Height = 100,
                Content = new TextBlock
                {
                    Text = msg,
                    Margin = new Avalonia.Thickness(20),
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap
                }
            };
            dialog.ShowDialog(this);
        }

        private void SaveSongs()
        {
            var json = JsonSerializer.Serialize(songs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SongsFile, json);
        }

        private void SaveTransitions()
        {
            var json = JsonSerializer.Serialize(transitions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(TransitionsFile, json);
        }

        private void LoadSongs()
        {
            if (File.Exists(SongsFile))
            {
                var json = File.ReadAllText(SongsFile);
                songs = JsonSerializer.Deserialize<List<Song>>(json) ?? new();
            }
        }

        private void LoadTransitions()
        {
            if (File.Exists(TransitionsFile))
            {
                var json = File.ReadAllText(TransitionsFile);
                transitions = JsonSerializer.Deserialize<List<Transition>>(json) ?? new();
            }
        }

        private int AddSongsFromDirectory(string folderPath, bool saveAndRefresh = true, bool showMessage = true)
        {
            if (!Directory.Exists(folderPath))
            {
                if (showMessage) ShowMessage("Directory does not exist.");
                return 0;
            }

            var audioFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                .Where(f =>
                    (f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)) &&
                    !Path.GetFileName(f).StartsWith("._"))
                .ToList();

            if (audioFiles.Count == 0)
            {
                if (showMessage) ShowMessage("No .mp3 or .wav files found.");
                return 0;
            }

            int added = 0;
            foreach (var file in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string cleaned = CleanSongName(fileName);

                if (!songs.Any(s => s.Name.Equals(cleaned, StringComparison.OrdinalIgnoreCase)))
                {
                    songs.Add(new Song
                    {
                        Name = cleaned,
                        Bpm = 0,
                        Key = "Unknown"
                    });
                    added++;
                }
            }

            if (saveAndRefresh)
            {
                SaveSongs();
                RefreshUI();
                FilterFromDropdown();
                FilterToDropdown();
                FilterViewDropdown();
            }

            if (showMessage)
                ShowMessage($"Imported {added} new songs.");

            return added;
        }

        private void AddSongToSetFrom(ListBox sourceList)
        {
            if (sourceList.SelectedItem is string label)
            {
                string name = label.Split('|')[0].Trim();
                var song = songs.FirstOrDefault(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (song != null && !setList.Any(s => s.Id == song.Id))
                {
                    setList.Add(song);
                    RefreshSetList();
                }
            }
        }

        private void AddFromTransitionListButton_Click(object? sender, RoutedEventArgs e)
        {
            // Add the song being viewed
            if (ViewTransitionsComboBox.SelectedItem is Song viewedSong && !setList.Any(s => s.Id == viewedSong.Id))
            {
                setList.Add(viewedSong);
            }

            // Add the song selected from the transition list (if any)
            if (TransitionsListBox.SelectedItem is string selectedLabel)
            {
                string selectedName = selectedLabel.Split('|')[0].Trim();
                var selectedSong = songs.FirstOrDefault(s => s.Name.Equals(selectedName, StringComparison.OrdinalIgnoreCase));
                if (selectedSong != null && !setList.Any(s => s.Id == selectedSong.Id))
                {
                    setList.Add(selectedSong);
                }
            }

            RefreshSetList();
        }

        private void RefreshSetList()
        {
            SetListBox.ItemsSource = setList
                //.Select(s => $"{s.Name} | BPM: {s.Bpm} | Key: {s.Key}")
                .Select(s => $"{s.Name}")
                .ToList();
        }

        private void FilterFromDropdown()
        {
            string term = FromSearchBox.Text?.Trim() ?? "";
            FromSongComboBox.ItemsSource = songs
                .Where(s => string.IsNullOrWhiteSpace(term) || s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Where(s => ToSongComboBox.SelectedItem is not Song to || s.Id != to.Id)
                .ToList();
        }

        private void FilterToDropdown()
        {
            string term = ToSearchBox.Text?.Trim() ?? "";
            ToSongComboBox.ItemsSource = songs
                .Where(s => string.IsNullOrWhiteSpace(term) || s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Where(s => FromSongComboBox.SelectedItem is not Song from || s.Id != from.Id)
                .ToList();
        }

        private void FilterViewDropdown()
        {
            string term = ViewSearchBox.Text?.Trim() ?? "";
            ViewTransitionsComboBox.ItemsSource = songs
                .Where(s => string.IsNullOrWhiteSpace(term) || s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private async void SaveSetListButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Set List",
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Text Files", Extensions = { "txt" } }
                },
                InitialFileName = "setlist.txt"
            };

            var filePath = await dialog.ShowAsync(this);
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                var namesOnly = setList.Select(s => s.Name);
                await File.WriteAllLinesAsync(filePath, namesOnly);
                ShowMessage("Set list saved.");
            }
        }

        private void RemoveSelectedSongFromSetList()
        {
            if (SetListBox.SelectedItem is string selectedLabel)
            {
                // Extract song name (assumes format "Name | BPM: ...")
                string songName = selectedLabel.Split('|')[0].Trim();

                // Find the song in the setList by name
                var songToRemove = setList.FirstOrDefault(s => s.Name.Equals(songName, StringComparison.OrdinalIgnoreCase));
                if (songToRemove != null)
                {
                    setList.Remove(songToRemove);

                    // Refresh the displayed set list
                    SetListBox.ItemsSource = setList
                        .Select(s => $"{s.Name}")
                        .ToList();
                }
            }
        }

        private async Task LoadSetListFromFileAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Open Set List",
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Text Files", Extensions = { "txt" } }
                }
            };

            var filePaths = await dialog.ShowAsync(this);
            if (filePaths == null || filePaths.Length == 0)
                await Task.CompletedTask;

            var lines = await File.ReadAllLinesAsync(filePaths[0]);
            var matchedSongs = songs
                .Where(s => lines.Any(line => line.Equals(s.Name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            setList.Clear();
            setList.AddRange(matchedSongs);
            RefreshSetList();
        }

        private void ClearSearchButton_Click(object? sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;

            // Restore full song list
            SongsListBox.ItemsSource = songs
                .Select(s => $"{s.Name}")
                .ToList();
        }

        private void RemoveSongButton_Click(object? sender, RoutedEventArgs e)
        {
            // Copy selected items first
            var selectedItems = SongsListBox.SelectedItems?.Cast<string>().ToList();
            if (selectedItems == null || selectedItems.Count == 0)
                return;

            // Use a hash set for performance when checking if a song is selected
            var selectedNames = selectedItems
                .Select(label => label.Split('|')[0].Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter songs to remove
            var songsToRemove = songs
                .Where(s => selectedNames.Contains(s.Name))
                .ToList();

            foreach (var songToRemove in songsToRemove)
            {
                songs.Remove(songToRemove);
                transitions.RemoveAll(t => t.FromSongId == songToRemove.Id || t.ToSongIds.Contains(songToRemove.Id));

                // Remove from combo boxes
                if (ViewTransitionsComboBox.ItemsSource is IEnumerable<Song> viewList)
                {
                    ViewTransitionsComboBox.ItemsSource = viewList.Where(s => s.Id != songToRemove.Id).ToList();
                }

                if (FromSongComboBox.ItemsSource is IEnumerable<Song> fromList)
                {
                    FromSongComboBox.ItemsSource = fromList.Where(s => s.Id != songToRemove.Id).ToList();
                }

                if (ToSongComboBox.ItemsSource is IEnumerable<Song> toList)
                {
                    ToSongComboBox.ItemsSource = toList.Where(s => s.Id != songToRemove.Id).ToList();
                }

                if (setList.Contains(songToRemove))
                {
                    setList.Remove(songToRemove);
                }
            }

            // Save changes and refresh UI once at the end
            SaveSongs();
            SaveTransitions();
            RefreshSetList();
            RefreshUI();
            ShowMessage($"{songsToRemove.Count} song(s) removed.");
        }

        public static string CleanSongName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return rawName;

            string cleaned = rawName.Trim();

            // 1) Basic normalization
            cleaned = cleaned.Replace('_', ' ');

            // 2) Remove known junk anywhere it appears
            string[] junk = {
                "SoundLoadMate.com", "YouTube", "SoundCloud",
                "Official Video", "Official Audio", "Lyrics", "Audio", "Visualizer",
                "[YouTube]", "[Official Audio]", "[Lyrics]",
                "(Official Video)", "(Lyrics)", "(Audio)"
            };

            foreach (var j in junk)
            {
                while (cleaned.Contains(j, StringComparison.OrdinalIgnoreCase))
                    cleaned = cleaned.Replace(j, "", StringComparison.OrdinalIgnoreCase);
            }

            // 3) Remove now-empty brackets
            cleaned = cleaned.Replace("()", "").Replace("[]", "").Replace("{}", "");

            // 4) Collapse extra spaces
            while (cleaned.Contains("  ")) cleaned = cleaned.Replace("  ", " ");

            cleaned = cleaned.Trim();

            // 5) Clean trailing separators left behind (e.g., " - ", "-", ":", etc.)
            char[] trail = { '-', '–', '—', ':', '|', '.', ' ' };
            while (cleaned.Length > 0 && trail.Contains(cleaned[^1])) cleaned = cleaned[..^1].TrimEnd();

            return cleaned;
        }

        private void LoadWatchedFolders()
        {
            try
            {
                if (File.Exists(WatchedFoldersFile))
                {
                    var json = File.ReadAllText(WatchedFoldersFile);
                    watchedFolders = JsonSerializer.Deserialize<List<string>>(json) ?? new();
                }
                else
                {
                    watchedFolders = new();
                }
            }
            catch
            {
                watchedFolders = new();
            }
        }

        private void SaveWatchedFolders()
        {
            try
            {
                var json = JsonSerializer.Serialize(watchedFolders, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(WatchedFoldersFile, json);
            }
            catch (Exception ex)
            {
                ShowMessage($"Failed to save watched folders: {ex.Message}");
            }
        }

        private void RefreshWatchedFoldersUI()
        {
            WatchedFoldersListBox.ItemsSource = null;
            WatchedFoldersListBox.ItemsSource = watchedFolders.ToList();
        }

        private async void AddWatchFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog { Title = "Add Watched Folder" };
            var folderPath = await dialog.ShowAsync(this);
            if (string.IsNullOrWhiteSpace(folderPath)) return;

            var path = Path.GetFullPath(folderPath);
            if (!Directory.Exists(path))
            {
                ShowMessage("Folder does not exist.");
                return;
            }
            if (watchedFolders.Any(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase)))
            {
                ShowMessage("Folder is already being watched.");
                return;
            }

            watchedFolders.Add(path);
            SaveWatchedFolders();
            RefreshWatchedFoldersUI();
        }

        private void RemoveWatchFolderButton_Click(object? sender, RoutedEventArgs e)
        {
            if (WatchedFoldersListBox.SelectedItems == null || WatchedFoldersListBox.SelectedItems.Count == 0)
                return;

            var selected = WatchedFoldersListBox.SelectedItems.Cast<string>().ToList();
            watchedFolders = watchedFolders
                .Where(p => !selected.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            SaveWatchedFolders();
            RefreshWatchedFoldersUI();
        }

        private void ScanWatchedFoldersButton_Click(object? sender, RoutedEventArgs e)
        {
            if (watchedFolders.Count == 0)
            {
                ShowMessage("No watched folders.");
                return;
            }

            int totalAdded = 0;

            foreach (var folder in watchedFolders)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        // suppress per-folder refresh & messages
                        totalAdded += AddSongsFromDirectory(folder, saveAndRefresh: false, showMessage: false);
                    }
                }
                catch (Exception ex)
                {
                    ShowMessage($"Error scanning '{folder}': {ex.Message}");
                }
            }

            if (totalAdded > 0)
            {
                SaveSongs();
                RefreshUI();
                FilterFromDropdown();
                FilterToDropdown();
                FilterViewDropdown();
            }

            ShowMessage(totalAdded > 0
                ? $"Imported {totalAdded} new songs from watched folders."
                : "No new songs found in watched folders.");
        }
    }
}