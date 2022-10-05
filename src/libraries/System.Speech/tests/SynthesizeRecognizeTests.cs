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
using Xunit.Abstractions;

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

        private ITestOutputHelper _output;

        public SynthesizeRecognizeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechSynthesizerToSpeechRecognitionEngine1()
        {
            // word chosen to be recognized with high confidence
            SpeechSynthesizerToSpeechRecognitionEngine_Core("recognize", "recognize");
        }

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechSynthesizerToSpeechRecognitionEngine2()
        {
            // word chosen to be recognized with high confidence
            SpeechSynthesizerToSpeechRecognitionEngine_Core("apple", "apple");
        }

        [ConditionalFact(nameof(HasInstalledRecognizers))]
        public void SpeechSynthesizerToSpeechRecognitionEngine_SilenceFails()
        {
            SpeechSynthesizerToSpeechRecognitionEngine_Core("    ", null);
        }

        private void SpeechSynthesizerToSpeechRecognitionEngine_Core(string input, string output)
        {
            if (PlatformDetection.IsWindows7 && PlatformDetection.IsX86Process)
                return; // Flaky on this configuration

            RetryHelper.Execute(() => // Flaky in some cases
            {
                if (Thread.CurrentThread.CurrentCulture.ToString() != "en-US")
                    return;

                using var ms = new MemoryStream();

                using (var synth = new SpeechSynthesizer())
                {
                    synth.SetOutputToWaveStream(ms);
                    var prompt = new Prompt(input);
                    synth.Speak(prompt);
                }

                ms.Position = 0;

                using (var rec = new SpeechRecognitionEngine())
                {
                    Stopwatch sw = new();
                    rec.LoadGrammar(new DictationGrammar());
                    rec.SetInputToWaveStream(ms);
                    rec.InitialSilenceTimeout = TimeSpan.FromSeconds(60); // for slow machines
                    rec.BabbleTimeout = TimeSpan.FromSeconds(60); // for slow machines/robustness

                    StringBuilder diagnostics = new();
                    diagnostics.AppendLine($"Passing synthesized input '{input}'");
                    try
                    {
                        rec.SpeechDetected += (o, args) =>
                        {
                            diagnostics.AppendLine($"Speech detected at position {args.AudioPosition}");
                        };

                        rec.SpeechRecognitionRejected += (o, args) =>
                        {
                            if (output != null)
                            {
                                foreach (RecognizedPhrase phrase in args.Result.Alternates)
                                {
                                    diagnostics.AppendLine($"Alternatives included '{phrase.Text}' with confidence {phrase.Confidence}");
                                }
                                diagnostics.Append($"Elapsed {sw.Elapsed}");
                                Assert.Fail($"Recognition of '{input}' was expected to produce a string containing '{output}', but failed");
                            }
                        };

                        RecognitionResult argsResult = null;
                        rec.SpeechRecognized += (o, args) =>
                        {
                            argsResult = args.Result;
                            diagnostics.AppendLine($"Received speech recognized event with result '{args.Result.Text}'");
                        };

                        sw.Start();
                        RecognitionResult result = rec.Recognize();
                        sw.Stop();

                        Assert.Equal(argsResult, result);

                        if (output == null)
                        {
                            Assert.Null(result);
                        }
                        else
                        {
                            Assert.NotNull(result);
                            diagnostics.AppendLine($"Recognized '{result.Text}' with confidence {result.Confidence}");
                            diagnostics.AppendLine($"Elapsed {sw.Elapsed}");

                            foreach (RecognizedPhrase phrase in result.Alternates)
                            {
                                diagnostics.AppendLine($"Alternatives included '{phrase.Text}' with confidence {phrase.Confidence}");
                            }

                            Assert.True(result.Confidence > 0.1); // strings we use are normally > 0.8

                            // Use Contains as sometimes we get garbage on the end, eg., "recognize" can be "recognized" or "a recognize"
                            Assert.Contains(output, result.Text, StringComparison.OrdinalIgnoreCase);
                        }
                    }
                    catch
                    {
                        _output.WriteLine(diagnostics.ToString());
                        throw;
                    }
                }
            });
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
