using System;
using System.IO;
using System.Text;

namespace Reflux
{

    /// <summary>
    /// Layout of a single song entry in the song list.
    ///
    /// The game has reshuffled this more than once (buffers widened, encoding changed), and each
    /// time it did, parsing broke in a way no offset value could fix - the addresses were right,
    /// the strides were not. Keeping the layout in offsets.txt means such a patch can be handled
    /// by editing a text file instead of rebuilding.
    ///
    /// Defaults describe the current game (P2D:J:B:A:2026080500 and later). Every field is
    /// optional in offsets.txt; omit it to use the default. To read an older build, override the
    /// ones that differ - for example everything up to 2026-06-03 used:
    ///   songBufferSize = 630 / songTitleSize = 80 / songArtistSize = 80 / songIdOffset = 4B0
    ///   songStringEncoding = shift-jis
    /// (sizes are hex, matching the rest of the file)
    /// </summary>
    public class SongLayout
    {
        /// <summary>Stride between song entries</summary>
        public int BufferSize { get; set; } = 0x730;
        /// <summary>Primary title buffer, at entry start</summary>
        public int TitleSize { get; set; } = 0x100;
        /// <summary>LED ticker text. Always Shift-JIS regardless of StringEncoding</summary>
        public int TickerSize { get; set; } = 0x40;
        public int GenreSize { get; set; } = 0x80;
        public int ArtistSize { get; set; } = 0x100;
        /// <summary>Offset of the song id within the entry</summary>
        public int IdOffset { get; set; } = 0x5B0;
        /// <summary>"utf-16le" or "shift-jis" - applies to title, genre and artist</summary>
        public string StringEncoding { get; set; } = "utf-16le";

        public bool IsWide
        {
            get { return StringEncoding.Equals("utf-16le", StringComparison.OrdinalIgnoreCase); }
        }
    }

    public class OffsetsCollection
    {
        public long SongList { get; set; }
        public long DataMap { get; set; }
        public long JudgeData { get; set; }
        public long PlayData { get; set; }
        public long PlaySettings { get; set; }
        public long UnlockData { get; set; }
        public long CurrentSong { get; set; }
        public SongLayout Song { get; set; } = new SongLayout();
    }
    static class Offsets
    {
        static OffsetsCollection offsets;
        /// <summary>
        /// Version these offsets are valid for
        /// </summary>
        public static string Version { get; private set; }
        /// <summary>
        /// Location of songlist
        /// </summary>
        public static long SongList { get { return offsets.SongList; } }
        /// <summary>
        /// Location of the pointers to the hashmap for player top score data
        /// </summary>
        public static long DataMap { get { return offsets.DataMap; } }
        /// <summary>
        /// Location of judge data
        /// </summary>
        public static long JudgeData { get { return offsets.JudgeData; } }
        /// <summary>
        /// Location of play data (song, difficulty, ex score, lamp)
        /// </summary>
        public static long PlayData { get { return offsets.PlayData; } }
        /// <summary>
        /// Location of play settings
        /// </summary>
        public static long PlaySettings { get { return offsets.PlaySettings; } }
        /// <summary>
        /// Location of the unlock data array
        /// </summary>
        public static long UnlockData { get { return offsets.UnlockData; } }
        /// <summary>
        /// Location where information of currently playing song and difficulty is found
        /// </summary>
        public static long CurrentSong { get { return offsets.CurrentSong; } }
        /// <summary>
        /// Layout of a song entry. Optional overrides in offsets.txt, defaults for the current game
        /// </summary>
        public static SongLayout Song { get { return offsets.Song; } }

        public static void LoadOffsets()
        {
            var lines = File.ReadAllLines("offsets.txt");
            var newoffsets = new OffsetsCollection();
            for (int i = 0; i < lines.Length; i++)
            {
                if (i == 0)
                {
                    Version = lines[i];
                    continue;
                }

                var line = lines[i];
                var sections = line.Split('=');
                if (sections.Length < 2) { continue; }
                var key = sections[0].Trim().ToLower();
                var value = sections[1].Trim();
                /* Not every key is an address - songStringEncoding is a name - so the hex
                 * conversion happens per case rather than up front. */
                switch (key)
                {
                    case "judgedata": newoffsets.JudgeData = Convert.ToInt64(value, 16); break;
                    case "playdata": newoffsets.PlayData = Convert.ToInt64(value, 16); break;
                    case "songlist": newoffsets.SongList = Convert.ToInt64(value, 16); break;
                    case "unlockdata": newoffsets.UnlockData = Convert.ToInt64(value, 16); break;
                    case "datamap": newoffsets.DataMap = Convert.ToInt64(value, 16); break;
                    case "playsettings": newoffsets.PlaySettings = Convert.ToInt64(value, 16); break;
                    case "currentsong": newoffsets.CurrentSong = Convert.ToInt64(value, 16); break;
                    /* Song entry layout - all optional, see SongLayout for defaults */
                    case "songbuffersize": newoffsets.Song.BufferSize = Convert.ToInt32(value, 16); break;
                    case "songtitlesize": newoffsets.Song.TitleSize = Convert.ToInt32(value, 16); break;
                    case "songtickersize": newoffsets.Song.TickerSize = Convert.ToInt32(value, 16); break;
                    case "songgenresize": newoffsets.Song.GenreSize = Convert.ToInt32(value, 16); break;
                    case "songartistsize": newoffsets.Song.ArtistSize = Convert.ToInt32(value, 16); break;
                    case "songidoffset": newoffsets.Song.IdOffset = Convert.ToInt32(value, 16); break;
                    case "songstringencoding": newoffsets.Song.StringEncoding = value; break;
                }
            }
            Apply(Version, newoffsets);
            Utils.Debug("Offsets loaded:");
            Utils.Debug($"SongList: {SongList:X}");
            Utils.Debug($"UnlockData: {UnlockData:X}");
            Utils.Debug($"Playcard: {PlayData:X}");
            Utils.Debug($"Judgeinfo: {JudgeData:X}");
            Utils.Debug($"Playsettings: {PlaySettings:X}");
            Utils.Debug($"CurrentSong: {CurrentSong:X}");
        }
        private static void Apply(string version, OffsetsCollection newOffsets)
        {
            Version = version;
            offsets = newOffsets;
        }
        public static void SaveOffsets(string version, OffsetsCollection newOffsets)
        {
            if (!Directory.Exists("archive"))
            {
                Directory.CreateDirectory("archive");
            }
            File.Move("offsets.txt", $"archive/{Version.Replace(':', '_')}.txt", true);

            /* Offset search only rediscovers addresses, not the entry layout. Carry the loaded
             * layout over, otherwise any override in offsets.txt is silently dropped here and the
             * parser reverts to defaults on the next run. */
            if (offsets != null && offsets.Song != null) { newOffsets.Song = offsets.Song; }

            Apply(version, newOffsets);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(version);
            sb.AppendLine($"songList = 0x{newOffsets.SongList:X}");
            sb.AppendLine($"unlockdata = 0x{newOffsets.UnlockData:X}");
            sb.AppendLine($"playSettings = 0x{newOffsets.PlaySettings:X}");
            sb.AppendLine($"playData = 0x{newOffsets.PlayData:X}");
            sb.AppendLine($"currentsong = 0x{newOffsets.CurrentSong:X}");
            sb.AppendLine($"judgeData = 0x{newOffsets.JudgeData:X}");
            sb.AppendLine($"datamap = 0x{newOffsets.DataMap:X}");

            /* Only write layout keys that differ from the defaults, to keep offsets.txt readable */
            var def = new SongLayout();
            var song = newOffsets.Song;
            if (song.BufferSize != def.BufferSize) { sb.AppendLine($"songBufferSize = 0x{song.BufferSize:X}"); }
            if (song.TitleSize != def.TitleSize) { sb.AppendLine($"songTitleSize = 0x{song.TitleSize:X}"); }
            if (song.TickerSize != def.TickerSize) { sb.AppendLine($"songTickerSize = 0x{song.TickerSize:X}"); }
            if (song.GenreSize != def.GenreSize) { sb.AppendLine($"songGenreSize = 0x{song.GenreSize:X}"); }
            if (song.ArtistSize != def.ArtistSize) { sb.AppendLine($"songArtistSize = 0x{song.ArtistSize:X}"); }
            if (song.IdOffset != def.IdOffset) { sb.AppendLine($"songIdOffset = 0x{song.IdOffset:X}"); }
            if (!song.StringEncoding.Equals(def.StringEncoding, StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine($"songStringEncoding = {song.StringEncoding}");
            }

            File.WriteAllText("offsets.txt", sb.ToString());

        }
    }
}
