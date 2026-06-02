using System;
using System.Collections.Generic;
using System.Linq;
using K4os.Hash.xxHash;
using OpenUtau.Core.Render;

namespace OpenUtau.Core.DiffSinger {
    internal static class DiffSingerVariancePatcher {
        const double InfluenceMarginMs = 250;
        const double FadeMs = 100;

        internal static ulong HashPitch(float[] pitch) {
            var bytes = new byte[pitch.Length * sizeof(float)];
            Buffer.BlockCopy(pitch, 0, bytes, 0, bytes.Length);
            return XXH64.DigestOf(bytes);
        }

        internal static bool CanPatch(VarianceResult oldResult, VarianceResult newResult) {
            return oldResult.totalFrames == newResult.totalFrames &&
                oldResult.headFrames == newResult.headFrames &&
                oldResult.tailFrames == newResult.tailFrames &&
                Math.Abs(oldResult.frameMs - newResult.frameMs) < 0.0001f &&
                SameLength(oldResult.energy, newResult.energy) &&
                SameLength(oldResult.breathiness, newResult.breathiness) &&
                SameLength(oldResult.voicing, newResult.voicing) &&
                SameLength(oldResult.tension, newResult.tension);
        }

        static bool SameLength(float[]? a, float[]? b) {
            return (a == null && b == null) || (a != null && b != null && a.Length == b.Length);
        }

        internal static VarianceResult Patch(
            RenderPhrase phrase,
            VarianceResult oldResult,
            VarianceResult newResult,
            IEnumerable<PreRenderPriority> priorities) {
            if (!CanPatch(oldResult, newResult)) {
                return newResult;
            }
            var weights = BuildWeights(phrase, newResult, priorities);
            if (weights.All(w => w <= 0)) {
                return oldResult;
            }
            return new VarianceResult {
                energy = PatchArray(oldResult.energy, newResult.energy, weights),
                breathiness = PatchArray(oldResult.breathiness, newResult.breathiness, weights),
                voicing = PatchArray(oldResult.voicing, newResult.voicing, weights),
                tension = PatchArray(oldResult.tension, newResult.tension, weights),
                frameMs = newResult.frameMs,
                headFrames = newResult.headFrames,
                tailFrames = newResult.tailFrames,
                totalFrames = newResult.totalFrames,
            };
        }

        internal static float[] BuildWeights(
            RenderPhrase phrase,
            VarianceResult result,
            IEnumerable<PreRenderPriority> priorities) {
            double startMs = phrase.positionMs - result.headFrames * result.frameMs;
            var ranges = priorities
                .Where(priority =>
                    priority.editKind == PreRenderEditKind.Pitch &&
                    RenderPriority.Overlaps(phrase.position, phrase.end, priority.startTick, priority.endTick))
                .Select(priority => (
                    startMs: phrase.timeAxis.TickPosToMsPos(priority.startTick),
                    endMs: phrase.timeAxis.TickPosToMsPos(priority.endTick)));
            return BuildWeightsForMsRanges(result.totalFrames, result.frameMs, startMs, ranges);
        }

        internal static float[] BuildWeightsForMsRanges(
            int totalFrames,
            float frameMs,
            double startMs,
            IEnumerable<(double startMs, double endMs)> ranges) {
            var weights = new float[totalFrames];
            if (totalFrames <= 0) {
                return weights;
            }
            int marginFrames = Math.Max(0, (int)Math.Round(InfluenceMarginMs / frameMs));
            int fadeFrames = Math.Max(1, (int)Math.Round(FadeMs / frameMs));
            foreach (var range in ranges) {
                int coreStart = FrameIndex(range.startMs, startMs, frameMs, totalFrames);
                int coreEnd = FrameIndex(range.endMs, startMs, frameMs, totalFrames);
                if (coreEnd <= coreStart) {
                    coreEnd = Math.Min(totalFrames, coreStart + 1);
                }
                int replaceStart = Math.Max(0, coreStart - marginFrames);
                int replaceEnd = Math.Min(totalFrames, coreEnd + marginFrames);
                int fadeStart = Math.Max(0, replaceStart - fadeFrames);
                int fadeEnd = Math.Min(totalFrames, replaceEnd + fadeFrames);
                for (int i = fadeStart; i < fadeEnd; ++i) {
                    float weight;
                    if (i < replaceStart) {
                        weight = SmoothStep((float)(i - fadeStart + 1) / Math.Max(1, replaceStart - fadeStart + 1));
                    } else if (i < replaceEnd) {
                        weight = 1;
                    } else {
                        weight = 1 - SmoothStep((float)(i - replaceEnd + 1) / Math.Max(1, fadeEnd - replaceEnd + 1));
                    }
                    weights[i] = Math.Max(weights[i], weight);
                }
            }
            return weights;
        }

        static int FrameIndex(double ms, double startMs, float frameMs, int totalFrames) {
            return Math.Clamp((int)Math.Round((ms - startMs) / frameMs), 0, totalFrames);
        }

        static float SmoothStep(float x) {
            x = Math.Clamp(x, 0, 1);
            return x * x * (3 - 2 * x);
        }

        internal static float[]? PatchArray(float[]? oldValues, float[]? newValues, float[] weights) {
            if (oldValues == null || newValues == null || oldValues.Length != newValues.Length) {
                return newValues;
            }
            var result = new float[newValues.Length];
            for (int i = 0; i < result.Length; ++i) {
                float weight = weights[i];
                result[i] = oldValues[i] * (1 - weight) + newValues[i] * weight;
            }
            return result;
        }
    }
}
