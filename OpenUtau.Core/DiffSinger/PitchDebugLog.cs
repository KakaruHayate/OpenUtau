using System;
using System.IO;
using System.Text;

namespace OpenUtau.Core.DiffSinger {
    /// <summary>
    /// Temporary frame-level tracing for the DiffSinger pitch path.
    /// Always on in this diagnostic build; writes to %TEMP%/ou_pitch_debug.txt
    /// and stops after a bounded amount so a long session cannot fill the disk.
    /// Remove once the boundary artifact is fixed.
    /// </summary>
    internal static class PitchDebugLog {
        const int MaxLines = 400000;
        static readonly object lockObj = new object();
        static bool initialized;
        static string path;
        static int lines;

        internal static bool Enabled {
            get {
                if (!initialized) {
                    initialized = true;
                    path = Path.Combine(Path.GetTempPath(), "ou_pitch_debug.txt");
                    try {
                        var existing = File.Exists(path) ? File.ReadAllLines(path).Length : 0;
                        lines = existing;
                        File.AppendAllText(path,
                            $"{Environment.NewLine}# OpenUtau pitch debug (diagnostic build) {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
                    } catch {
                        path = null;
                    }
                }
                return path != null && lines < MaxLines;
            }
        }

        internal static void Line(string text) {
            if (!Enabled) {
                return;
            }
            lock (lockObj) {
                try {
                    File.AppendAllText(path, text + Environment.NewLine, Encoding.UTF8);
                    lines++;
                } catch {
                    // Diagnostics must never break rendering.
                }
            }
        }

        internal static void Section(string title) {
            Line("");
            Line($"===== {title} =====");
        }

        /// <summary>Formats a pitch array in cents, rounded, for stage-by-stage diffs.</summary>
        internal static string Values(float[] values, int from = 0, int count = 80) {
            var sb = new StringBuilder();
            int end = Math.Min(values.Length, from + count);
            for (int i = from; i < end; i++) {
                sb.Append((int)values[i]).Append(' ');
            }
            if (end < values.Length) {
                sb.Append("...");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>Compacts a frame mask into runs, e.g. "F8 T44 F9 T43 F8".</summary>
        internal static string Runs(bool[] flags) {
            var sb = new StringBuilder();
            int i = 0;
            while (i < flags.Length) {
                int j = i;
                while (j < flags.Length && flags[j] == flags[i]) {
                    j++;
                }
                sb.Append(flags[i] ? "T" : "F").Append(j - i).Append(' ');
                i = j;
            }
            return sb.ToString().TrimEnd();
        }
    }
}
