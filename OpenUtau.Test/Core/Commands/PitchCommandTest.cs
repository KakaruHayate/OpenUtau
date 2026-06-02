using System.Linq;
using OpenUtau.Core.Ustx;
using Xunit;

namespace OpenUtau.Core {
    public class PitchCommandTest {
        [Fact]
        public void MovePitchPointCommandReportsAffectedNote() {
            var part = new UVoicePart();
            var note = new UNote { position = 240, duration = 480 };
            var point = new PitchPoint(0, 0, PitchPointShape.io);

            var command = new MovePitchPointCommand(part, note, point, 10, 20);

            Assert.Single(command.AffectedNotes);
            Assert.Same(note, command.AffectedNotes.First());
        }

        [Fact]
        public void ChangePitchPointShapeCommandReportsAffectedNote() {
            var part = new UVoicePart();
            var note = new UNote { position = 240, duration = 480 };
            var point = new PitchPoint(0, 0, PitchPointShape.io);

            var command = new ChangePitchPointShapeCommand(part, note, point, PitchPointShape.l);

            Assert.Single(command.AffectedNotes);
            Assert.Same(note, command.AffectedNotes.First());
        }

        [Fact]
        public void LegacyPitchPointMoveConstructorHasNoAffectedNote() {
            var part = new UVoicePart();
            var point = new PitchPoint(0, 0, PitchPointShape.io);

            var command = new MovePitchPointCommand(part, point, 10, 20);

            Assert.Empty(command.AffectedNotes);
        }
    }
}
