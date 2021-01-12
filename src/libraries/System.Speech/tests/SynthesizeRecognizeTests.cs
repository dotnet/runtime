// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Speech.AudioFormat;
using System.Speech.Recognition;
using System.Speech.Recognition.SrgsGrammar;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Xml;
using Xunit;

namespace SampleSynthesisTests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoNorServerCore))] // No SAPI on Nano or Server Core
    [SkipOnMono("No SAPI on Mono")]
    public class SynthesizeRecognizeTests : FileCleanupTestBase
    {
        // Our Windows 7 and Windows 8.1 queues seem to have no recognizers installed
        public static bool HasInstalledRecognizers => PlatformDetection.IsNotMonoRuntime &&
                                                      PlatformDetection.IsNotWindowsNanoNorServerCore &&
                                                      SpeechRecognitionEngine.InstalledRecognizers().Count > 0;

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechSynthesizerToSpeechRecognitionEngine()
        {
            if (Thread.CurrentThread.CurrentCulture.ToString() != "en-US")
                return;

            using var ms = new MemoryStream();

            using (var synth = new SpeechSynthesizer())
            {
                synth.SetOutputToWaveStream(ms);
                var prompt = new Prompt("synthesizer");
                synth.Speak(prompt);
            }

            ms.Position = 0;

            using (var rec = new SpeechRecognitionEngine())
            {
                rec.LoadGrammar(new DictationGrammar());
                rec.SetInputToWaveStream(ms);
                RecognitionResult result = rec.Recognize();

                Assert.True(result.Confidence > 0.1);
                // handles "synthesizer", "synthesizes", etc.
                Assert.StartsWith("synthe", result.Text, StringComparison.OrdinalIgnoreCase);
            }
        }

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechRecognitionEngineInvalidInput()
        {
            using var ms = new MemoryStream();
            ms.WriteByte(1);

            using (var rec = new SpeechRecognitionEngine())
            {
                Assert.Throws<FormatException>(() => rec.SetInputToWaveStream(ms));
            }
        }

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechRecognitionEngineProperties()
        {
            using (var rec = new SpeechRecognitionEngine())
            {
                rec.SetInputToNull();
                rec.InitialSilenceTimeout = new TimeSpan();
                rec.BabbleTimeout = new TimeSpan();
                rec.EndSilenceTimeout = new TimeSpan();
                rec.EndSilenceTimeoutAmbiguous = new TimeSpan();
                rec.MaxAlternates = 1;

                Assert.Throws<KeyNotFoundException>(() => rec.QueryRecognizerSetting("foo"));
                Assert.Throws<KeyNotFoundException>(() => rec.UpdateRecognizerSetting("foo", "bar"));
                Assert.Throws<KeyNotFoundException>(() => rec.UpdateRecognizerSetting("foo", 1));
            }
        }

        [Fact]
        public void SpeechSynthesizerToWavAndRepeat()
        {
            string wav = GetTestFilePath() + ".wav";

            using (var synth = new SpeechSynthesizer())
            {
                synth.SetOutputToWaveFile(wav);
                synth.Speak("hello");
            }

            Assert.True(new FileInfo(wav).Length > 0);

            using var ms = new MemoryStream();
            using (var synth = new SpeechSynthesizer())
            {
                synth.SetOutputToWaveStream(ms);

                var builder = new PromptBuilder();
                builder.AppendAudio(wav);
                synth.Speak(builder);

                Assert.True(ms.Position > 0);
            }
        }

        [Fact]
        public void SpeechSynthSsmlInvalidPhoneme()
        {
            using (var synth = new SpeechSynthesizer())
            {
                synth.SetOutputToNull();

                string ssml = @"
<speak version='1.0' xml:lang='en-US' xmlns='https://www.w3.org/2001/10/synthesis'>
	<s>His name is Mike <phoneme alphabet='ups' ph='@#$#@$'>Zhou </phoneme></s>
</speak>";
                Assert.Throws<FormatException>(() => synth.SpeakSsml(ssml));
                ssml = @"
<speak version='1.0' xml:lang='en-US' xmlns='https://www.w3.org/2001/10/synthesis'>
	<s>His name is Mike <phoneme alphabet='@#$@#$' ph='JH'>Zhou </phoneme></s>
</speak>";
                Assert.Throws<FormatException>(() => synth.SpeakSsml(ssml));
            }
        }

        [Fact]
        public void SpeechSynthesizerEventsAndProperties()
        {
            using (var synth = new SpeechSynthesizer())
            {
                using var ms = new MemoryStream();

                synth.SetOutputToNull();
                synth.SetOutputToAudioStream(ms, new SpeechAudioFormatInfo(16000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo));
                synth.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Adult);
                Assert.True(synth.Volume > 0);
                Assert.NotNull(synth.Voice);
                Assert.NotEmpty(synth.GetInstalledVoices());
                Assert.Null(synth.GetCurrentlySpokenPrompt());

                var builder = new PromptBuilder();
                builder.AppendText("synthesizer");

                int events = 0;
                synth.BookmarkReached += (object o, BookmarkReachedEventArgs e) => events++;
                synth.PhonemeReached += (object o, PhonemeReachedEventArgs e) => events++;
                synth.SpeakProgress += (object o, SpeakProgressEventArgs e) => events++;
                synth.SpeakStarted += (object o, SpeakStartedEventArgs e) => events++;
                synth.VisemeReached += (object o, VisemeReachedEventArgs e) => events++;
                synth.VoiceChange += (object o, VoiceChangeEventArgs e) => events++;
                synth.StateChanged += (object o, System.Speech.Synthesis.StateChangedEventArgs e) => events++;
                synth.SpeakCompleted += (object o, SpeakCompletedEventArgs e) =>
                {
                    events++;
                    Assert.Equal(34, events++);
                };

                Assert.Equal(SynthesizerState.Ready, synth.State);
                synth.SpeakSsml(builder.ToXml());
                Assert.Equal(SynthesizerState.Ready, synth.State);
                synth.Pause();
                Assert.Equal(SynthesizerState.Paused, synth.State);
                synth.Resume();
                Assert.Equal(SynthesizerState.Ready, synth.State);
            }
        }

        [Fact]
        public void AddLexicon()
        {
            string temp = GetTestFilePath();
            string content = @"
<lexicon alphabet='x-microsoft-ups' version='1.0' xml:lang='en-US' xmlns='http://www.w3.org/2005/01/pronunciation-lexicon'>
	<lexeme>
		<grapheme>blue </grapheme>
		<phoneme>B L I P </phoneme>
	</lexeme>";
            File.WriteAllText(temp, content);

            using (var synth = new SpeechSynthesizer())
            {
                synth.AddLexicon(new Uri(temp), "application/pls+xml");
            }
        }
    }
}
