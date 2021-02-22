// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Xml;

#pragma warning disable 56524 // The _xmlWriter member is not created in this module and should not be disposed

namespace System.Speech.Internal.Synthesis
{
    internal class TextWriterEngine : ISsmlParser
    {
        #region Constructors

        internal TextWriterEngine(XmlTextWriter writer, CultureInfo culture)
        {
            _writer = writer;
            _culture = culture;
        }

        #endregion

        #region Internal Methods

        public object ProcessSpeak(string sVersion, string baseUri, CultureInfo culture, List<SsmlXmlAttribute> extraNamespace)
        {
            if (!string.IsNullOrEmpty(baseUri))
            {
                throw new ArgumentException(SR.Get(SRID.InvalidSpeakAttribute, "baseUri", "speak"), nameof(baseUri));
            }

            bool fNewCulture = culture != null && !culture.Equals(_culture);
            if (fNewCulture || !string.IsNullOrEmpty(_pexmlPrefix) || extraNamespace.Count > 0)
            {
                _writer.WriteStartElement("voice");

                // Always add the culture info as the voice element cannot not be empty (namespaces declaration don't count)
                _writer.WriteAttributeString("xml", "lang", null, culture != null ? culture.Name : _culture.Name);

                // write all the additional namespace
                foreach (SsmlXmlAttribute ns in extraNamespace)
                {
                    _writer.WriteAttributeString("xmlns", ns._name, ns._ns, ns._value);
                }

                // If the prompt builder is used with to add prompt engine data, add the namespace
                if (!string.IsNullOrEmpty(_pexmlPrefix))
                {
                    _writer.WriteAttributeString("xmlns", _pexmlPrefix, null, xmlNamespacePrompt);
                }

                _closeSpeak = true;
            }

            return null;
        }

        public void ProcessText(string text, object voice, ref FragmentState fragmentState, int position, bool fIgnore)
        {
            _writer.WriteString(text);
        }

        public void ProcessAudio(object voice, string uri, string baseUri, bool fIgnore)
        {
            _writer.WriteStartElement("audio");
            _writer.WriteAttributeString("src", uri);
        }

        public void ProcessBreak(object voice, ref FragmentState fragmentState, EmphasisBreak eBreak, int time, bool fIgnore)
        {
            _writer.WriteStartElement("break");
            if (time > 0 && eBreak == EmphasisBreak.None)
            {
                _writer.WriteAttributeString("time", time.ToString(CultureInfo.InvariantCulture) + "ms");
            }
            else
            {
                string value = null;
                switch (eBreak)
                {
                    case EmphasisBreak.None:
                        value = "none";
                        break;

                    case EmphasisBreak.ExtraWeak:
                        value = "x-weak";
                        break;

                    case EmphasisBreak.Weak:
                        value = "weak";
                        break;

                    case EmphasisBreak.Medium:
                        value = "medium";
                        break;

                    case EmphasisBreak.Strong:
                        value = "strong";
                        break;

                    case EmphasisBreak.ExtraStrong:
                        value = "x-strong";
                        break;
                }
                if (!string.IsNullOrEmpty(value))
                {
                    _writer.WriteAttributeString("strength", value);
                }
            }
        }

        public void ProcessDesc(CultureInfo culture)
        {
            _writer.WriteStartElement("desc");
            if (culture != null)
            {
                _writer.WriteAttributeString("xml", "lang", null, culture.Name);
            }
        }

        public void ProcessEmphasis(bool noLevel, EmphasisWord word)
        {
            _writer.WriteStartElement("emphasis");
            if (word != EmphasisWord.Default)
            {
                _writer.WriteAttributeString("level", word.ToString().ToLowerInvariant());
            }
        }

        public void ProcessMark(object voice, ref FragmentState fragmentState, string name, bool fIgnore)
        {
            _writer.WriteStartElement("mark");
            _writer.WriteAttributeString("name", name);
        }

        public object ProcessTextBlock(bool isParagraph, object voice, ref FragmentState fragmentState, CultureInfo culture, bool newCulture, VoiceGender gender, VoiceAge age)
        {
            _writer.WriteStartElement(isParagraph ? "p" : "s");
            if (culture != null)
            {
                _writer.WriteAttributeString("xml", "lang", null, culture.Name);
            }
            return null;
        }

        public void EndProcessTextBlock(bool isParagraph)
        {
        }

        public void ProcessPhoneme(ref FragmentState fragmentState, AlphabetType alphabet, string ph, char[] phoneIds)
        {
            _writer.WriteStartElement("phoneme");
            if (alphabet != AlphabetType.Ipa)
            {
                _writer.WriteAttributeString("alphabet", alphabet == AlphabetType.Sapi ? "x-microsoft-sapi" : "x-microsoft-ups");
                System.Diagnostics.Debug.Assert(alphabet == AlphabetType.Ups || alphabet == AlphabetType.Sapi);
            }
            _writer.WriteAttributeString("ph", ph);
        }

        public void ProcessProsody(string pitch, string range, string rate, string volume, string duration, string points)
        {
            _writer.WriteStartElement("prosody");
            if (!string.IsNullOrEmpty(range))
            {
                _writer.WriteAttributeString("range", range);
            }
            if (!string.IsNullOrEmpty(rate))
            {
                _writer.WriteAttributeString("rate", rate);
            }
            if (!string.IsNullOrEmpty(volume))
            {
                _writer.WriteAttributeString("volume", volume);
            }
            if (!string.IsNullOrEmpty(duration))
            {
                _writer.WriteAttributeString("duration", duration);
            }
            if (!string.IsNullOrEmpty(points))
            {
                _writer.WriteAttributeString("range", points);
            }
        }

        public void ProcessSayAs(string interpretAs, string format, string detail)
        {
            _writer.WriteStartElement("say-as");
            _writer.WriteAttributeString("interpret-as", interpretAs);
            if (!string.IsNullOrEmpty(format))
            {
                _writer.WriteAttributeString("format", format);
            }
            if (!string.IsNullOrEmpty(detail))
            {
                _writer.WriteAttributeString("detail", detail);
            }
        }

        public void ProcessSub(string alias, object voice, ref FragmentState fragmentState, int position, bool fIgnore)
        {
            _writer.WriteStartElement("sub");
            _writer.WriteAttributeString("alias", alias);
        }
        public object ProcessVoice(string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool fNewCulture, List<SsmlXmlAttribute> extraNamespace)
        {
            _writer.WriteStartElement("voice");
            if (!string.IsNullOrEmpty(name))
            {
                _writer.WriteAttributeString("name", name);
            }
            if (fNewCulture && culture != null)
            {
                _writer.WriteAttributeString("xml", "lang", null, culture.Name);
            }
            if (gender != VoiceGender.NotSet)
            {
                _writer.WriteAttributeString("gender", gender.ToString().ToLowerInvariant());
            }
            if (age != VoiceAge.NotSet)
            {
                _writer.WriteAttributeString("age", ((int)age).ToString(CultureInfo.InvariantCulture));
            }
            if (variant > 0)
            {
                _writer.WriteAttributeString("variant", (variant).ToString(CultureInfo.InvariantCulture));
            }

            // write all the additional namespace
            if (extraNamespace != null)
            {
                foreach (SsmlXmlAttribute ns in extraNamespace)
                {
                    _writer.WriteAttributeString("xmlns", ns._name, ns._ns, ns._value);
                }
            }
            return null;
        }

        public void ProcessLexicon(Uri uri, string type)
        {
            _writer.WriteStartElement("lexicon");
            _writer.WriteAttributeString("uri", uri.ToString());
            if (!string.IsNullOrEmpty(type))
            {
                _writer.WriteAttributeString("type", type);
            }
        }

        public void EndElement()
        {
            _writer.WriteEndElement();
        }

        public void EndSpeakElement()
        {
            if (_closeSpeak)
            {
                _writer.WriteEndElement();
            }
        }

        public void ProcessUnknownElement(object voice, ref FragmentState fragmentState, XmlReader reader)
        {
            _writer.WriteNode(reader, false);
        }

        public void StartProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string sElement, List<SsmlXmlAttribute> extraAttributes)
        {
            // write all the additional namespace
            foreach (SsmlXmlAttribute attribute in extraAttributes)
            {
                _writer.WriteAttributeString(attribute._prefix, attribute._name, attribute._ns, attribute._value);
            }
        }

        public void EndProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string sElement, List<SsmlXmlAttribute> extraAttributes)
        {
        }

        #region Prompt Engine

        public void ContainsPexml(string pexmlPrefix)
        {
            _pexmlPrefix = pexmlPrefix;
        }

        private bool ProcessPromptEngine(string element, params KeyValuePair<string, string>[] attributes)
        {
            _writer.WriteStartElement(_pexmlPrefix, element, xmlNamespacePrompt);

            if (attributes != null)
            {
                foreach (KeyValuePair<string, string> kp in attributes)
                {
                    if (kp.Value != null)
                    {
                        _writer.WriteAttributeString(kp.Key, kp.Value);
                    }
                }
            }
            return true;
        }

        public bool BeginPromptEngineOutput(object voice)
        {
            return ProcessPromptEngine("prompt_output");
        }

        public bool ProcessPromptEngineDatabase(object voice, string fname, string delta, string idset)
        {
            return ProcessPromptEngine("database", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("fname", fname), new KeyValuePair<string, string>("delta", delta), new KeyValuePair<string, string>("idset", idset) });
        }

        public bool ProcessPromptEngineDiv(object voice)
        {
            return ProcessPromptEngine("div");
        }

        public bool ProcessPromptEngineId(object voice, string id)
        {
            return ProcessPromptEngine("id", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("id", id) });
        }

        public bool BeginPromptEngineTts(object voice)
        {
            return ProcessPromptEngine("tts");
        }

        public void EndPromptEngineTts(object voice)
        {
        }

        public bool BeginPromptEngineWithTag(object voice, string tag)
        {
            return ProcessPromptEngine("withtag", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("tag", tag) });
        }

        public void EndPromptEngineWithTag(object voice, string tag)
        {
        }

        public bool BeginPromptEngineRule(object voice, string name)
        {
            return ProcessPromptEngine("rule", new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("name", name) });
        }

        public void EndPromptEngineRule(object voice, string name)
        {
        }

        public void EndPromptEngineOutput(object voice)
        {
        }

        #endregion

        #endregion

        #region Internal Properties

        public string Ssml
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Private Fields

        private XmlTextWriter _writer;
        private CultureInfo _culture;
        private bool _closeSpeak;
        private string _pexmlPrefix;
        private const string xmlNamespacePrompt = "http://schemas.microsoft.com/Speech/2003/03/PromptEngine";

        #endregion
    }
}
