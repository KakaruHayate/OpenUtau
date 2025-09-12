using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using OpenUtau.Core.Render;
using OpenUtau.Core.Ustx;

namespace OpenUtau.Core.DiffSinger {
    public class DiffSingerScript{
        public double offsetMs;
        public string[] text;
        public string[] ph_seq;
        public double[] phDurMs;
        public int[] ph_num;//number of phonemes in each note's time range, excluding slur notes
        public int[] noteSeq;//notenum per note, including slur notes
        public double[] noteDurMs;//duration in ms per note, including slur notes
        public int[] note_slur;//1 if slur, 0 if not, per note, including slur notes
        public double[]? f0_seq = null;
        public double frameMs;
        public double[]? gender = null;
        public double[]? velocity = null;
        
        //if v2 is true, export diffsinger script for diffsinger's refactor-v2 branch
        public DiffSingerScript(RenderPhrase phrase, bool v2 = false, bool exportPitch = true) {
            const float headMs = DiffSingerUtils.headMs;
            const float tailMs = DiffSingerUtils.tailMs;
            
            var notes = phrase.notes;
            var phones = phrase.phones;

            bool shouldPrependSP = phones.Length == 0 || phones.First().phoneme != "SP";
            bool shouldAppendSP = phones.Length == 0 || phones.Last().phoneme != "SP";

            const double headMsDouble = headMs;
            const double tailMsDouble = tailMs;

            double headMsForCurve = shouldPrependSP ? headMsDouble : 0;
            double tailMsForCurve = shouldAppendSP ? tailMsDouble : 0;

            IEnumerable<string> text_items = notes.Select(n => n.lyric).Where(s => !s.StartsWith("+"));
            if (shouldPrependSP)
            {
                text_items = text_items.Prepend("SP");
            }
            if (shouldAppendSP)
            {
                text_items = text_items.Append("SP");
            }
            text = text_items.ToArray();

            IEnumerable<string> phonemes = phones.Select(p => p.phoneme);
            IEnumerable<double> phoneDurationsMs = phones.Select(p => p.durationMs);
            if (shouldPrependSP)
            {
                phonemes = phonemes.Prepend("SP");
                phoneDurationsMs = phoneDurationsMs.Prepend(headMsDouble);
            }
            if (shouldAppendSP)
            {
                phonemes = phonemes.Append("SP");
                phoneDurationsMs = phoneDurationsMs.Append(tailMsDouble);
            }
            ph_seq = phonemes.ToArray();
            phDurMs = phoneDurationsMs.ToArray();

            IEnumerable<int> note_slur_items = notes.Select(n => n.lyric.StartsWith("+") ? 1 : 0);
            if (shouldPrependSP)
            {
                note_slur_items = note_slur_items.Prepend(0);
            }
            if (shouldAppendSP)
            {
                note_slur_items = note_slur_items.Append(0);
            }
            note_slur = note_slur_items.ToArray();
            //ph_num
            var phNumList = new List<int>();
            int ep = 4;//the max error between note position and phoneme position is 4 ticks
            int prevNotePhId = 0;
            int phId = 0;
            int phCount = phones.Length;
            foreach(var note in notes.Where(n=>!n.lyric.StartsWith("+"))) {
                while(phId < phCount && phones[phId].position < note.position-ep){
                    ++phId;
                }
                phNumList.Add(phId - prevNotePhId);
                prevNotePhId = phId;
            }
            phNumList.Add(phCount - prevNotePhId);
            if (shouldPrependSP)
            {
                phNumList[0]++;
            }
            if (shouldAppendSP)
            {
                phNumList.Add(1);
            }
            ph_num = phNumList.ToArray();

            if (v2)
            {
                IEnumerable<int> noteSeq_items = notes.Select(n => (n.lyric == "SP" || n.lyric == "AP") ? 0 : n.tone);
                if (shouldPrependSP)
                {
                    noteSeq_items = noteSeq_items.Prepend(0);
                }
                if (shouldAppendSP)
                {
                    noteSeq_items = noteSeq_items.Append(0);
                }
                noteSeq = noteSeq_items.ToArray();
            }
            else
            {
                noteSeq = ph_seq.Select(p => (p == "SP" || p == "AP") ? 0 : phones.FirstOrDefault(ph => ph.phoneme == p)?.tone ?? 0).ToArray();
                var phoneTones = phones.ToDictionary(p => p.phoneme, p => p.tone);
                noteSeq = ph_seq.Select(p => (p == "SP" || p == "AP") ? 0 : (phoneTones.ContainsKey(p) ? phoneTones[p] : 0)).ToArray();
            }
            noteDurMs = notes
                .Select(n => n.durationMs)
                .Prepend(headMsForCurve + (notes[0].positionMs-phones[0].positionMs))
                .Append(tailMsForCurve)
                .ToArray();
            
            frameMs = 10;

            int headFrames = (int)(headMsForCurve / frameMs);
            int tailFrames = (int)(tailMsForCurve / frameMs);
            var totalFrames = (int)(phDurMs.Sum() / frameMs);

            //f0
            if (exportPitch){
                f0_seq = DiffSingerUtils.SampleCurve(phrase, phrase.pitches, 
                    0, frameMs, totalFrames, headFrames, tailFrames, 
                    x => MusicMath.ToneToFreq(x * 0.01));
            }

            //velc
            var velocityCurve = phrase.curves.FirstOrDefault(curve => curve.Item1 == DiffSingerUtils.VELC);
            if (velocityCurve != null) {
                velocity = DiffSingerUtils.SampleCurve(phrase, velocityCurve.Item2, 
                    0, frameMs, totalFrames, headFrames, tailFrames,
                    x=>Math.Pow(2, (x - 100) / 100));
            }

            //voicebank specific features
            DiffSingerSinger singer = null;
            if (phrase.singer != null) { 
                singer = phrase.singer as DiffSingerSinger; 
            }
            if(singer != null) {
                //gender
                if (singer.dsConfig.useKeyShiftEmbed) {
                    var range = singer.dsConfig.augmentationArgs.randomPitchShifting.range;
                    var positiveScale = (range[1] == 0) ? 0 : (12 / range[1] / 100);
                    var negativeScale = (range[0] == 0) ? 0 : (-12 / range[0] / 100);
                    gender = DiffSingerUtils.SampleCurve(phrase, phrase.gender, 0, frameMs, totalFrames, headFrames, tailFrames,
                        x => (x < 0) ? (-x * positiveScale) : (-x * negativeScale))
                        .ToArray();
                }
            }

            offsetMs = phrase.phones[0].positionMs - headMsForCurve;
        }
        
        public RawDiffSingerScript toRaw() {
            return new RawDiffSingerScript(this);
        }

        static public void SavePart(UProject project, UVoicePart part, string filePath, bool v2 = false, bool exportPitch = true) {
            var ScriptArray = RenderPhrase.FromPart(project, project.tracks[part.trackNo], part)
                .Select(x => new DiffSingerScript(x, v2, exportPitch).toRaw())
                .ToArray();
            File.WriteAllText(filePath,
                JsonConvert.SerializeObject(ScriptArray, Formatting.Indented),
                new UTF8Encoding(false));
        }
    }
    
    public class RawDiffSingerScript {
        public double offset;
        public string text;
        public string ph_seq;
        public string ph_dur;
        public string ph_num;
        public string note_seq;
        public string note_dur;
        public string note_dur_seq;
        public string note_slur;
        public string is_slur_seq;
        public string? f0_seq = null;
        public string f0_timestep;
        public string input_type = "phoneme";
        public string? gender_timestep = null;
        public string? gender = null;
        public string? velocity_timestep = null;
        public string? velocity = null;

        public RawDiffSingerScript(DiffSingerScript script) {
            offset = script.offsetMs / 1000;
            text = String.Join(" ", script.text);
            ph_seq = String.Join(" ", script.ph_seq);
            ph_num = String.Join(" ", script.ph_num);
            note_seq = String.Join(" ", 
                script.noteSeq
                .Select(x => x <= 0 ? "rest" : MusicMath.GetToneName(x)));
            ph_dur = String.Join(" ",script.phDurMs.Select(x => (x/1000).ToString("f4")));
            note_dur = String.Join(" ",script.noteDurMs.Select(x => (x/1000).ToString("f4")));
            note_dur_seq = ph_dur;
            note_slur = String.Join(" ", script.note_slur);
            is_slur_seq = String.Join(" ", script.ph_seq.Select(x => "0"));
            
            if(script.f0_seq!=null){
                f0_seq = String.Join(" ", script.f0_seq.Select(x => x.ToString("f1")));
            }
            f0_timestep = (script.frameMs / 1000).ToString();

            if(script.gender != null) {
                gender_timestep = f0_timestep;
                gender = String.Join(" ", script.gender.Select(x => x.ToString("f3")));
            }

            if (script.velocity != null) {
                velocity_timestep = f0_timestep;
                velocity = String.Join(" ", script.velocity.Select(x => x.ToString("f3")));
            }
        }
    }
}
