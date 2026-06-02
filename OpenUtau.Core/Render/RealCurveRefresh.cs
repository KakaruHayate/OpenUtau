using System;
using System.Collections.Generic;
using System.Linq;
using OpenUtau.Core.DiffSinger;
using OpenUtau.Core.Ustx;
using OpenUtau.Core.Util;

namespace OpenUtau.Core.Render {
    public static class RealCurveRefresh {
        public static bool CanRefresh(RenderPhrase phrase, bool allowSessionInitialization = true) {
            if (phrase.renderer.SingerType != USingerType.DiffSinger ||
                !phrase.renderer.SupportsRealCurve ||
                !Preferences.Default.DiffSingerTensorCache) {
                return false;
            }
            return true;
        }

        public static List<RenderRealCurveResult> LoadRenderedRealCurves(
            RenderPhrase phrase,
            bool allowSessionInitialization = true) {
            if (!CanRefresh(phrase, allowSessionInitialization)) {
                return new List<RenderRealCurveResult>(0);
            }
            return phrase.renderer.LoadRenderedRealCurves(phrase);
        }

        public static bool ApplyRealCurveResults(
            UProject project,
            UVoicePart part,
            RenderPhrase phrase,
            IEnumerable<RenderRealCurveResult> results) {
            bool changed = false;
            foreach (var result in results) {
                changed |= ApplyRealCurveResult(project, part, phrase, result);
            }
            return changed;
        }

        public static bool ApplyRealCurveResult(
            UProject project,
            UVoicePart part,
            RenderPhrase phrase,
            RenderRealCurveResult result) {
            int count = Math.Min(result.ticks.Length, result.values.Length);
            if (count == 0) {
                return false;
            }
            var curve = part.curves.FirstOrDefault(curve => curve.abbr == result.abbr);
            if (curve == null) {
                if (!project.expressions.TryGetValue(result.abbr, out var descriptor)) {
                    return false;
                }
                curve = new UCurve(descriptor);
                part.curves.Add(curve);
            }
            var ticks = result.ticks
                .Take(count)
                .Select(tick => phrase.position - part.position + (int)Math.Round(tick))
                .ToArray();
            var values = result.values
                .Take(count)
                .Select(value => (int)Math.Round(value * 1000.0))
                .ToArray();
            int rangeStart = ticks.First();
            int rangeEnd = ticks.Last();
            int index = curve.realXs.BinarySearch(rangeStart);
            if (index < 0) {
                index = ~index;
            }
            int removeCount = 0;
            while (index + removeCount < curve.realXs.Count && curve.realXs[index + removeCount] <= rangeEnd) {
                removeCount++;
            }
            if (removeCount > 0) {
                curve.realXs.RemoveRange(index, removeCount);
                curve.realYs.RemoveRange(index, removeCount);
            }
            var insertXs = new List<int>(ticks.Length + 1) { rangeStart };
            var insertYs = new List<int>(values.Length + 1) { -1 };
            insertXs.AddRange(ticks);
            insertYs.AddRange(values);
            curve.realXs.InsertRange(index, insertXs);
            curve.realYs.InsertRange(index, insertYs);
            return true;
        }
    }
}
