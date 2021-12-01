// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Xml;

namespace System.Speech.Internal.Synthesis
{
    #region Internal Types

    internal interface ISsmlParser
    {
        object ProcessSpeak(string sVersion, string sBaseUri, CultureInfo culture, List<SsmlXmlAttribute> extraNamespace);
        void ProcessText(string text, object voice, ref FragmentState fragmentState, int position, bool fIgnore);
        void ProcessAudio(object voice, string sUri, string baseUri, bool fIgnore);
        void ProcessBreak(object voice, ref FragmentState fragmentState, EmphasisBreak eBreak, int time, bool fIgnore);
        void ProcessDesc(CultureInfo culture);
        void ProcessEmphasis(bool noLevel, EmphasisWord word);
        void ProcessMark(object voice, ref FragmentState fragmentState, string name, bool fIgnore);
        object ProcessTextBlock(bool isParagraph, object voice, ref FragmentState fragmentState, CultureInfo culture, bool newCulture, VoiceGender gender, VoiceAge age);
        void EndProcessTextBlock(bool isParagraph);
        void ProcessPhoneme(ref FragmentState fragmentState, AlphabetType alphabet, string ph, char[] phoneIds);
        void ProcessProsody(string pitch, string range, string rate, string volume, string duration, string points);
        void ProcessSayAs(string interpretAs, string format, string detail);
        void ProcessSub(string alias, object voice, ref FragmentState fragmentState, int position, bool fIgnore);
        object ProcessVoice(string name, CultureInfo culture, VoiceGender gender, VoiceAge age, int variant, bool fNewCulture, List<SsmlXmlAttribute> extraNamespace);
        void ProcessLexicon(Uri uri, string type);
        void EndElement();
        void EndSpeakElement();

        void ProcessUnknownElement(object voice, ref FragmentState fragmentState, XmlReader reader);
        void StartProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string element, List<SsmlXmlAttribute> extraAttributes);
        void EndProcessUnknownAttributes(object voice, ref FragmentState fragmentState, string element, List<SsmlXmlAttribute> extraAttributes);

        // Prompt data used
        void ContainsPexml(string pexmlPrefix);

        // Prompt Engine tags
        bool BeginPromptEngineOutput(object voice);
        void EndPromptEngineOutput(object voice);

        // global elements
        bool ProcessPromptEngineDatabase(object voice, string fname, string delta, string idset);
        bool ProcessPromptEngineDiv(object voice);
        bool ProcessPromptEngineId(object voice, string id);

        // scoped elements
        bool BeginPromptEngineTts(object voice);
        void EndPromptEngineTts(object voice);
        bool BeginPromptEngineWithTag(object voice, string tag);
        void EndPromptEngineWithTag(object voice, string tag);
        bool BeginPromptEngineRule(object voice, string name);
        void EndPromptEngineRule(object voice, string name);

        // Properties
        string Ssml { get; }
    }

    internal class LexiconEntry
    {
        internal Uri _uri;
        internal string _mediaType;

        internal LexiconEntry(Uri uri, string mediaType)
        {
            _uri = uri;
            _mediaType = mediaType;
        }

        /// <summary>
        /// Tests whether two objects are equivalent
        /// </summary>
        public override bool Equals(object obj)
        {
            LexiconEntry entry = obj as LexiconEntry;
            return entry != null && _uri.Equals(entry._uri);
        }

        /// <summary>
        /// Overrides Object.GetHashCode()
        /// </summary>
        public override int GetHashCode()
        {
            return _uri.GetHashCode();
        }
    }

    internal class SsmlXmlAttribute
    {
        internal SsmlXmlAttribute(string prefix, string name, string value, string ns)
        {
            _prefix = prefix;
            _name = name;
            _value = value;
            _ns = ns;
        }

        internal string _prefix;
        internal string _name;
        internal string _value;
        internal string _ns;
    }

    #endregion
}
