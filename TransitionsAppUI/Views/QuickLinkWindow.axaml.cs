using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace TransitionsAppUI
{
    public partial class QuickLinkWindow : Window
    {
        private List<Song> _songs;
        private List<Transition> _transitions;
        private readonly string _songsFile;
        private readonly string _transitionsFile;
        private readonly Action? _onTransitionSaved;

        public QuickLinkWindow(List<Song> songs, List<Transition> transitions, string songsFile, string transitionsFile, Action? onTransitionSaved = null)
        {
            InitializeComponent();

            _songs = songs;
            _transitions = transitions;
            _songsFile = songsFile;
            _transitionsFile = transitionsFile;
            _onTransitionSaved = onTransitionSaved;

            RefreshDropdowns();

            QLFromSearchBox.TextChanged += (_, __) => FilterFrom();
            QLToSearchBox.TextChanged += (_, __) => FilterTo();
            QLLinkButton.Click += (_, __) => LinkSongs();
            QLClearButton.Click += (_, __) => Clear();
            QLSwapButton.Click += (_, __) => SwapSongs();

            // Enter key submits
            QLLinkButton.KeyDown += (_, e) => { if (e.Key == Key.Return) LinkSongs(); };
            KeyDown += (_, e) => { if (e.Key == Key.Return) LinkSongs(); };
        }

        /// <summary>
        /// Reloads the song list from disk and refreshes dropdowns.
        /// Call this after the main window adds new songs.
        /// </summary>
        public void ReloadSongs(List<Song> songs, List<Transition> transitions)
        {
            _songs = songs;
            _transitions = transitions;
            RefreshDropdowns();
        }

        private void RefreshDropdowns()
        {
            var fromSelected = QLFromComboBox.SelectedItem as Song;
            var toSelected = QLToComboBox.SelectedItem as Song;

            QLFromComboBox.ItemsSource = _songs.ToList();
            QLToComboBox.ItemsSource = _songs.ToList();

            // Restore previous selections if they still exist
            if (fromSelected != null)
                QLFromComboBox.SelectedItem = _songs.FirstOrDefault(s => s.Id == fromSelected.Id);
            if (toSelected != null)
                QLToComboBox.SelectedItem = _songs.FirstOrDefault(s => s.Id == toSelected.Id);
        }

        private void FilterFrom()
        {
            string term = QLFromSearchBox.Text?.Trim() ?? "";
            var toSong = QLToComboBox.SelectedItem as Song;

            QLFromComboBox.ItemsSource = _songs
                .Where(s => string.IsNullOrWhiteSpace(term) || s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Where(s => toSong == null || s.Id != toSong.Id)
                .ToList();
        }

        private void FilterTo()
        {
            string term = QLToSearchBox.Text?.Trim() ?? "";
            var fromSong = QLFromComboBox.SelectedItem as Song;

            QLToComboBox.ItemsSource = _songs
                .Where(s => string.IsNullOrWhiteSpace(term) || s.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                .Where(s => fromSong == null || s.Id != fromSong.Id)
                .ToList();
        }

        private void LinkSongs()
        {
            if (QLFromComboBox.SelectedItem is not Song fromSong ||
                QLToComboBox.SelectedItem is not Song toSong)
            {
                SetStatus("Select both songs first.");
                return;
            }

            if (fromSong.Id == toSong.Id)
            {
                SetStatus("Cannot link a song to itself.");
                return;
            }

            var transition = _transitions.FirstOrDefault(t => t.FromSongId == fromSong.Id);
            if (transition == null)
            {
                transition = new Transition { FromSongId = fromSong.Id };
                _transitions.Add(transition);
            }

            if (transition.ToSongIds.Contains(toSong.Id))
            {
                SetStatus("Already linked.");
                return;
            }

            transition.ToSongIds.Add(toSong.Id);
            SaveTransitions();

            SetStatus($"Linked: {fromSong.Name} → {toSong.Name}");
            _onTransitionSaved?.Invoke();

            // Advance: set "from" to the current "to" so the DJ can chain the next link
            QLFromComboBox.SelectedItem = _songs.FirstOrDefault(s => s.Id == toSong.Id);
            QLFromSearchBox.Text = "";
            QLToComboBox.SelectedIndex = -1;
            QLToSearchBox.Text = "";
            FilterFrom();
            FilterTo();
        }

        private void SwapSongs()
        {
            var from = QLFromComboBox.SelectedItem as Song;
            var to = QLToComboBox.SelectedItem as Song;
            QLFromComboBox.SelectedItem = to;
            QLToComboBox.SelectedItem = from;
        }

        private void Clear()
        {
            QLFromComboBox.SelectedIndex = -1;
            QLToComboBox.SelectedIndex = -1;
            QLFromSearchBox.Text = "";
            QLToSearchBox.Text = "";
            FilterFrom();
            FilterTo();
            SetStatus("");
        }

        private void SetStatus(string msg) =>
            QLStatusLabel.Text = msg;

        private void SaveTransitions()
        {
            var json = JsonSerializer.Serialize(_transitions, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_transitionsFile, json);
        }
    }
}
