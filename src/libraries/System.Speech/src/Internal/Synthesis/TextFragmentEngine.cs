// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Xml;

namespace System.Speech.Internal.Synthesis
{
    internal class TextFragmentEngine : ISsmlParser
    {
        #region Constructors

        internal TextFragmentEngine(SpeakInfo speakInfo, string ssmlText, bool pexml, ResourceLoader resourceLoader, List<LexiconEntry> lexicons)
        {
            _lexicons = lexicons;
            _ssmlText = ssmlText;
            _speakInfo = speakInfo;
            _resourceLoader = resourceLoader;
        }

        #endregion

        #region Internal Methods

        public object ProcessSpeak(string sVersion, string sBaseUri, CultureInfo culture, List<SsmlXmlAttribute> extraNamespace)
        {
            _speakInfo.SetVoice(null, culture, VoiceGender.NotSet, VoiceAge.NotSet, 1);
            return _speakInfo.Voice;
        }

        public void ProcessText(string text, object voice, ref FragmentState fragmentState, int position, bool fIgnore)
        {
            if (!fIgnore)
            {
                TtsEngineAction action = fragmentState.Action;
                if (_paragraphStarted)
                {
                    fragmentState.Action = TtsEngineAction.StartParagraph;
                    _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState));
                    _paragraphStarted = false;

                    // Always add the start sentence.
                    _sentenceStarted = true;
                }
                if (_sentenceStarted)
                {
                    fragmentState.Action = TtsEngineAction.StartSentence;
                    _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState));
                    _sentenceStarted = false;
                }
                fragmentState.Action = ActionTextFragment(action);
                _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState, text, _ssmlText, position, text.Length));
                fragmentState.Action = action;
            }
        }

        public void ProcessAudio(object voice, string sUri, string baseUri, bool fIgnore)
        {
            if (!fIgnore)
            {
                // Prepend the base Uri if necessary
                Uri uri = new(sUri, UriKind.RelativeOrAbsolute);
                if (!uri.IsAbsoluteUri && !string.IsNullOrEmpty(baseUri))
                {
                    if (baseUri[baseUri.Length - 1] != '/' && baseUri[baseUri.Length - 1] != '\\')
                    {
                        int posSlash = baseUri.LastIndexOf('/');
                        if (posSlash < 0)
                        {
                            posSlash = baseUri.LastIndexOf('\\');
                        }
                        if (posSlash >= 0)
                        {
                            baseUri = baseUri.Substring(0, posSlash);
                        }
                        baseUri += '/';
                    }
                    StringBuilder sb = new(baseUri);
                    sb.Append(sUri);
                    uri = new Uri(sb.ToString(), UriKind.RelativeOrAbsolute);
                }

                // This checks if we can read the file
                {
                    _speakInfo.AddAudio(new AudioData(uri, _resourceLoader));
                }
            }
        }

        public void ProcessBreak(object voice, ref FragmentState fragmentState, EmphasisBreak eBreak, int time, bool fIgnore)
        {
            if (!fIgnore)
            {
                TtsEngineAction action = fragmentState.Action;
                fragmentState.Action = ActionTextFragment(fragmentState.Action);
                _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState));
                fragmentState.Action = action;
            }
        }

        public void ProcessDesc(CultureInfo culture)
        {
        }

        public void ProcessEmphasis(bool noLevel, EmphasisWord word)
        {
        }

        public void ProcessMark(object voice, ref FragmentState fragmentState, string name, bool fIgnore)
        {
            if (!fIgnore)
            {
                TtsEngineAction action = fragmentState.Action;
                fragmentState.Action = ActionTextFragment(fragmentState.Action);
                _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState, name));
                fragmentState.Action = action;
            }
        }

        public object ProcessTextBlock(bool isParagraph, object voice, ref FragmentState fragmentState, CultureInfo culture, bool newCulture, VoiceGender gender, VoiceAge age)
        {
            if (culture != null && newCulture)
            {
                _speakInfo.SetVoice(null, culture, gender, age, 1);
            }
            if (isParagraph)
            {
                _paragraphStarted = true;
            }
            else
            {
                _sentenceStarted = true;
            }
            return _speakInfo.Voice;
        }

        public void EndProcessTextBlock(bool isParagraph)
        {
            if (isParagraph)
            {
                _paragraphStarted = true;
            }
            else
            {
                _sentenceStarted = true;
            }
        }

        public void ProcessPhoneme(ref FragmentState fragmentState, AlphabetType alphabet, string ph, char[] phoneIds)
        {
            fragmentState.Action = TtsEngineAction.Pronounce;
            fragmentState.Phoneme = _speakInfo.Voice.TtsEngine.ConvertPhonemes(phoneIds, alphabet);
        }

        public void ProcessProsody(string pitch, string range, string rate, string volume, string duration, string points)
        {
        }

        public void ProcessSayAs(string interpretAs, string format, string detail)
        {
        }

        public void ProcessSub(string alias, object voice, ref FragmentState fragmentState, int position, bool fIgnore)
        {
            ProcessText(alias, voice, ref fragmentState, position, fIgnore);
        }

        public object ProcessVoice(string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool fNewCulture, List<SsmlXmlAttribute> extraNamespace)
        {
            _speakInfo.SetVoice(name, culture, gender, age, variant);
            return _speakInfo.Voice;
        }

        public void ProcessLexicon(Uri uri, string type)
        {
            _lexicons.Add(new LexiconEntry(uri, type));
        }

        public void ProcessUnknownElement(object voice, ref FragmentState fragmentState, XmlReader reader)
        {
            StringWriter sw = new(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new(sw);
            writer.WriteNode(reader, false);
            writer.Close();
            string text = sw.ToString();

            AddParseUnknownFragment(voice, ref fragmentState, text);
        }

        public void StartProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string element, List<SsmlXmlAttribute> extraAttributes)
        {
            StringBuilder sb = new();
            sb.AppendFormat(CultureInfo.InvariantCulture, "<{0}", element);
            foreach (SsmlXmlAttribute attribute in extraAttributes)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, " {0}:{1}=\"{2}\" xmlns:{3}=\"{4}\"", attribute._prefix, attribute._name, attribute._value, attribute._prefix, attribute._ns);
            }
            sb.Append('>');

            AddParseUnknownFragment(voice, ref fragmentState, sb.ToString());
        }

        public void EndProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string element, List<SsmlXmlAttribute> extraAttributes)
        {
            AddParseUnknownFragment(voice, ref fragmentState, string.Format(CultureInfo.InvariantCulture, "</{0}>", element));
        }

        #region Prompt Engine

        public void ContainsPexml(string pexmlPrefix)
        {
        }

        public bool BeginPromptEngineOutput(object voice)
        {
            return false;
        }

        public void EndPromptEngineOutput(object voice)
        {
        }

        public bool ProcessPromptEngineDatabase(object voice, string fname, string delta, string idset)
        {
            return false;
        }

        public bool ProcessPromptEngineDiv(object voice)
        {
            return false;
        }

        public bool ProcessPromptEngineId(object voice, string id)
        {
            return false;
        }

        public bool BeginPromptEngineTts(object voice)
        {
            return false;
        }

        public void EndPromptEngineTts(object voice)
        {
        }

        public bool BeginPromptEngineWithTag(object voice, string tag)
        {
            return false;
        }

        public void EndPromptEngineWithTag(object voice, string tag)
        {
        }

        public bool BeginPromptEngineRule(object voice, string name)
        {
            return false;
        }

        public void EndPromptEngineRule(object voice, string name)
        {
        }
        #endregion

        public void EndElement()
        {
        }

        public void EndSpeakElement()
        {
        }

        #endregion

        #region Internal Properties

        public string Ssml
        {
            get
            {
                return _ssmlText;
            }
        }

        #endregion

        #region Private Methods

        private static TtsEngineAction ActionTextFragment(TtsEngineAction action)
        {
            return action;
        }

        private void AddParseUnknownFragment(object voice, ref FragmentState fragmentState, string text)
        {
            TtsEngineAction action = fragmentState.Action;
            fragmentState.Action = TtsEngineAction.ParseUnknownTag;
            _speakInfo.AddText((TTSVoice)voice, new TextFragment(fragmentState, text));
            fragmentState.Action = action;
        }

        #endregion

        #region Private Fields

        private List<LexiconEntry> _lexicons;
        private SpeakInfo _speakInfo;
        private string _ssmlText;
        private bool _paragraphStarted = true;
        private bool _sentenceStarted = true;
        private ResourceLoader _resourceLoader;

        #endregion
    }
}
