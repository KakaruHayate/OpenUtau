using System.Linq;
using Xunit;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerVariancePatcherTest {
        [Fact]
        public void HashPitchChangesWhenPitchChanges() {
            var a = DiffSingerVariancePatcher.HashPitch(new[] { 1f, 2f, 3f });
            var b = DiffSingerVariancePatcher.HashPitch(new[] { 1f, 2.1f, 3f });

            Assert.NotEqual(a, b);
        }

        [Fact]
        public void BuildWeightsForMsRangesKeepsOutsideRangeAtZero() {
            var weights = DiffSingerVariancePatcher.BuildWeightsForMsRanges(
                totalFrames: 100,
                frameMs: 10,
                startMs: 0,
                ranges: new[] { (startMs: 500.0, endMs: 600.0) });

            Assert.Equal(0, weights[0]);
            Assert.Equal(0, weights[99]);
            Assert.Contains(weights, weight => weight >= 1);
        }

        [Fact]
        public void BuildWeightsForMsRangesCrossfadesEdges() {
            var weights = DiffSingerVariancePatcher.BuildWeightsForMsRanges(
                totalFrames: 100,
                frameMs: 10,
                startMs: 0,
                ranges: new[] { (startMs: 500.0, endMs: 600.0) });

            Assert.Contains(weights, weight => weight > 0 && weight < 1);
        }

        [Fact]
        public void PatchArrayBlendsOldAndNewByWeight() {
            var oldValues = Enumerable.Repeat(0f, 5).ToArray();
            var newValues = Enumerable.Repeat(10f, 5).ToArray();
            var weights = new[] { 0f, 0.25f, 0.5f, 0.75f, 1f };

            var result = DiffSingerVariancePatcher.PatchArray(oldValues, newValues, weights)!;

            Assert.Equal(new[] { 0f, 2.5f, 5f, 7.5f, 10f }, result);
        }
    }
}
