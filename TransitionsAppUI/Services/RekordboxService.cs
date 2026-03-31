using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace TransitionsAppUI.Services
{
    public static class RekordboxService
    {
        /// <summary>
        /// Parses a Rekordbox XML export and returns a list of songs.
        /// The user exports from Rekordbox via File > Export Collection in xml format.
        /// </summary>
        public static List<Song> ImportSongs(string xmlPath)
        {
            var doc = XDocument.Load(xmlPath);
            var collection = doc.Root?.Element("COLLECTION");
            if (collection == null) return new();

            var imported = new List<Song>();

            foreach (var track in collection.Elements("TRACK"))
            {
                string name = (string?)track.Attribute("Name") ?? "";
                string artist = (string?)track.Attribute("Artist") ?? "";
                string location = (string?)track.Attribute("Location") ?? "";
                int trackId = int.TryParse((string?)track.Attribute("TrackID"), out int tid) ? tid : 0;
                double bpm = double.TryParse((string?)track.Attribute("AverageBpm"), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double b) ? b : 0;
                string tonality = (string?)track.Attribute("Tonality") ?? "";

                if (string.IsNullOrWhiteSpace(name)) continue;

                string displayName = !string.IsNullOrWhiteSpace(artist)
                    ? $"{artist} - {name}"
                    : name;

                string filePath = LocationToPath(location);

                imported.Add(new Song
                {
                    Name = displayName,
                    Bpm = (int)Math.Round(bpm),
                    Key = tonality,
                    RekordboxTrackId = trackId > 0 ? trackId : null,
                    FilePath = filePath
                });
            }

            return imported;
        }

        /// <summary>
        /// Exports transition playlists (and optionally a set list) as a Rekordbox-importable XML.
        /// User imports the saved file in Rekordbox via File > Import > rekordbox xml.
        /// </summary>
        public static void ExportPlaylists(
            string outputXmlPath,
            List<Song> songs,
            List<Transition> transitions,
            List<Song>? setList = null)
        {
            // Assign stable integer IDs for each song
            var trackIdMap = AssignTrackIds(songs);

            var collectionElements = songs.Select(song =>
                BuildTrackElement(song, trackIdMap[song.Id]));

            // One playlist per "from song" that has transitions
            var transitionPlaylistNodes = transitions
                .Where(t => t.ToSongIds.Count > 0)
                .Select(t =>
                {
                    var fromSong = songs.FirstOrDefault(s => s.Id == t.FromSongId);
                    if (fromSong == null) return null;

                    var trackRefs = t.ToSongIds
                        .Where(id => trackIdMap.ContainsKey(id))
                        .Select(id => new XElement("TRACK", new XAttribute("Key", trackIdMap[id])));

                    return new XElement("NODE",
                        new XAttribute("Name", fromSong.Name),
                        new XAttribute("Type", "1"),
                        new XAttribute("KeyType", "0"),
                        new XAttribute("Entries", t.ToSongIds.Count),
                        trackRefs);
                })
                .Where(n => n != null)
                .ToList();

            var folderChildren = new List<XElement>(transitionPlaylistNodes!);

            // Optional set list playlist
            if (setList != null && setList.Count > 0)
            {
                var setListRefs = setList
                    .Where(s => trackIdMap.ContainsKey(s.Id))
                    .Select(s => new XElement("TRACK", new XAttribute("Key", trackIdMap[s.Id])));

                folderChildren.Add(new XElement("NODE",
                    new XAttribute("Name", "Set List"),
                    new XAttribute("Type", "1"),
                    new XAttribute("KeyType", "0"),
                    new XAttribute("Entries", setList.Count),
                    setListRefs));
            }

            var transitionsFolder = new XElement("NODE",
                new XAttribute("Name", "TransitionsApp"),
                new XAttribute("Type", "0"),
                new XAttribute("Count", folderChildren.Count),
                folderChildren);

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                new XElement("DJ_PLAYLISTS",
                    new XAttribute("Version", "1.0.0"),
                    new XElement("PRODUCT",
                        new XAttribute("Name", "rekordbox"),
                        new XAttribute("Version", "6.0.0"),
                        new XAttribute("Company", "AlphaTheta")),
                    new XElement("COLLECTION",
                        new XAttribute("Entries", songs.Count),
                        collectionElements),
                    new XElement("PLAYLISTS",
                        new XElement("NODE",
                            new XAttribute("Type", "0"),
                            new XAttribute("Name", "ROOT"),
                            new XAttribute("Count", "1"),
                            transitionsFolder))));

            doc.Save(outputXmlPath);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static Dictionary<string, int> AssignTrackIds(List<Song> songs)
        {
            var map = new Dictionary<string, int>();
            // Honour existing Rekordbox IDs first, then fill gaps
            var usedIds = new HashSet<int>(songs
                .Where(s => s.RekordboxTrackId.HasValue)
                .Select(s => s.RekordboxTrackId!.Value));

            int next = 1;
            foreach (var song in songs)
            {
                if (song.RekordboxTrackId.HasValue)
                {
                    map[song.Id] = song.RekordboxTrackId.Value;
                }
                else
                {
                    while (usedIds.Contains(next)) next++;
                    map[song.Id] = next;
                    usedIds.Add(next);
                    next++;
                }
            }
            return map;
        }

        private static XElement BuildTrackElement(Song song, int trackId)
        {
            string location = !string.IsNullOrEmpty(song.FilePath)
                ? PathToLocation(song.FilePath)
                : "";

            return new XElement("TRACK",
                new XAttribute("TrackID", trackId),
                new XAttribute("Name", song.Name),
                new XAttribute("Artist", ""),
                new XAttribute("Composer", ""),
                new XAttribute("Album", ""),
                new XAttribute("Grouping", ""),
                new XAttribute("Genre", ""),
                new XAttribute("Kind", "MP3 File"),
                new XAttribute("Size", "0"),
                new XAttribute("TotalTime", "0"),
                new XAttribute("DiscNumber", "0"),
                new XAttribute("TrackNumber", "0"),
                new XAttribute("Year", "0"),
                new XAttribute("AverageBpm", song.Bpm.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)),
                new XAttribute("DateAdded", DateTime.Now.ToString("yyyy-MM-dd")),
                new XAttribute("BitRate", "0"),
                new XAttribute("SampleRate", "44100"),
                new XAttribute("Comments", "Exported by TransitionsApp"),
                new XAttribute("PlayCount", "0"),
                new XAttribute("Rating", "0"),
                new XAttribute("Location", location),
                new XAttribute("Remixer", ""),
                new XAttribute("Tonality", song.Key ?? ""),
                new XAttribute("Label", ""),
                new XAttribute("Mix", ""));
        }

        private static string LocationToPath(string location)
        {
            if (string.IsNullOrEmpty(location)) return "";
            try
            {
                // Rekordbox uses file://localhost/path or file:///path
                location = location
                    .Replace("file://localhost/", "")
                    .Replace("file:///", "")
                    .Replace("file://", "");
                return Uri.UnescapeDataString(location);
            }
            catch { return ""; }
        }

        private static string PathToLocation(string path)
        {
            try
            {
                var uri = new Uri(path);
                // Rekordbox expects file://localhost/... style
                return "file://localhost" + uri.AbsolutePath.Replace("%20", " ");
            }
            catch
            {
                return "file://localhost/" + path.Replace("\\", "/");
            }
        }
    }
}
