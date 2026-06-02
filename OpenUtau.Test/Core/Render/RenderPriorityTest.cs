using System.Linq;
using OpenUtau.Core.Ustx;
using Xunit;

namespace OpenUtau.Core.Render {
    public class RenderPriorityTest {
        [Fact]
        public void PlaybackPriorityPrefersSourceCoveringStart() {
            var ordered = new[] {
                    ("future", 120.0, 160.0),
                    ("past", 0.0, 90.0),
                    ("current", 80.0, 150.0),
                }
                .OrderBy(item => RenderPriority.PlaybackBucket(item.Item2, item.Item3, 100))
                .ThenBy(item => RenderPriority.PlaybackDistance(item.Item2, item.Item3, 100))
                .Select(item => item.Item1)
                .ToArray();

            Assert.Equal(new[] { "current", "future", "past" }, ordered);
        }

        [Fact]
        public void PlaybackPriorityUsesDistanceWithinBucket() {
            var ordered = new[] {
                    ("far", 300.0, 360.0),
                    ("near", 120.0, 180.0),
                }
                .OrderBy(item => RenderPriority.PlaybackBucket(item.Item2, item.Item3, 100))
                .ThenBy(item => RenderPriority.PlaybackDistance(item.Item2, item.Item3, 100))
                .Select(item => item.Item1)
                .ToArray();

            Assert.Equal(new[] { "near", "far" }, ordered);
        }

        [Fact]
        public void PreRenderPriorityBucketsEditedDiffSingerPhraseFirst() {
            Assert.Equal(0, RenderPriority.PreRenderBucket(
                isDiffSinger: true,
                isPriorityPart: true,
                overlapsPriority: true,
                isAfterPriorityStart: true));
            Assert.Equal(1, RenderPriority.PreRenderBucket(
                isDiffSinger: true,
                isPriorityPart: true,
                overlapsPriority: false,
                isAfterPriorityStart: true));
            Assert.Equal(2, RenderPriority.PreRenderBucket(
                isDiffSinger: true,
                isPriorityPart: false,
                overlapsPriority: false,
                isAfterPriorityStart: true));
            Assert.Equal(3, RenderPriority.PreRenderBucket(
                isDiffSinger: false,
                isPriorityPart: true,
                overlapsPriority: true,
                isAfterPriorityStart: true));
        }

        [Fact]
        public void PreRenderDistanceIsZeroWhenPhraseCoversPriorityStart() {
            Assert.Equal(0, RenderPriority.PreRenderDistance(80, 160, 100));
            Assert.Equal(20, RenderPriority.PreRenderDistance(120, 160, 100));
            Assert.Equal(10, RenderPriority.PreRenderDistance(40, 90, 100));
        }

        [Fact]
        public void OverlapsUsesHalfOpenTickRanges() {
            Assert.True(RenderPriority.Overlaps(80, 120, 100, 160));
            Assert.False(RenderPriority.Overlaps(80, 100, 100, 160));
            Assert.False(RenderPriority.Overlaps(160, 200, 100, 160));
        }

        [Fact]
        public void PreRenderNotificationKeepsMultiplePriorityRanges() {
            var part1 = new UVoicePart();
            var part2 = new UVoicePart();
            var notification = new PreRenderNotification(new[] {
                new PreRenderPriority(part1, 100, 200),
                new PreRenderPriority(part2, 300, 400),
                new PreRenderPriority(part2, 500, 500),
            });

            Assert.Equal(2, notification.priorities.Length);
            Assert.Same(part1, notification.priorityPart);
            Assert.Equal(100, notification.priorityStartTick);
            Assert.Equal(200, notification.priorityEndTick);
        }
    }
}
