// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Speech.Synthesis;
using System.Speech.Synthesis.TtsEngine;
using System.Text;
using System.Xml;

namespace System.Speech.Internal.Synthesis
{
    internal static class SsmlParser
    {
        #region Internal Methods

        /// <summary>
        /// Parse an SSML stream and build a set of SSML Text Fragments
        /// </summary>
        internal static void Parse(string ssml, ISsmlParser engine, object voice)
        {
            // Remove the CR and LF
            string ssmlNoCrLf = ssml.Replace('\n', ' ');
            ssmlNoCrLf = ssmlNoCrLf.Replace('\r', ' ');
            XmlTextReader reader = new(new StringReader(ssmlNoCrLf));

            // Parse the stream
            Parse(reader, engine, voice);
        }

        /// <summary>
        /// Parse an SSML stream and build a set of SSML Text Fragments
        /// </summary>
        internal static void Parse(XmlReader reader, ISsmlParser engine, object voice)
        {
            try
            {
                bool isSpeakElementFound = false;

                while (reader.Read())
                {
                    // Ignore XmlDeclaration, ProcessingInstruction, Comment, DocumentType, Entity, Notation.
                    if ((reader.NodeType == XmlNodeType.Element) && (reader.LocalName == "speak"))
                    {
                        // SSML documents must start with the "speak" element
                        if (isSpeakElementFound)
                        {
                            ThrowFormatException(SRID.GrammarDefTwice);
                        }
                        else
                        {
                            // The XML header is read, real work starts here
                            ProcessSpeakElement(reader, engine, voice);
                            isSpeakElementFound = true;
                        }
                    }
                }

                if (!isSpeakElementFound)
                {
                    ThrowFormatException(SRID.SynthesizerNoSpeak);
                }
            }
            catch (XmlException eXml)
            {
                throw new FormatException(SR.Get(SRID.InvalidXml), eXml);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate the Speak element
        /// </summary>
        private static void ProcessSpeakElement(XmlReader reader, ISsmlParser engine, object voice)
        {
            SsmlAttributes ssmlAttributes = new();
            ssmlAttributes._voice = voice;
            ssmlAttributes._age = VoiceAge.NotSet;
            ssmlAttributes._gender = VoiceGender.NotSet;
            ssmlAttributes._unknownNamespaces = new List<SsmlXmlAttribute>();

            string sVersion = null;
            string sCulture = null;
            string sBaseUri = null;
            CultureInfo culture = null;
            List<SsmlXmlAttribute> extraSpeakAttributes = new();
            Exception innerException = null;

            // Process attributes.
            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                // emptyNamespace
                if (reader.NamespaceURI.Length == 0)
                {
                    switch (reader.LocalName)
                    {
                        case "version":
                            CheckForDuplicates(ref sVersion, reader);
                            if (sVersion != "1.0")
                            {
                                ThrowFormatException(SRID.InvalidVersion);
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                else if (reader.NamespaceURI == xmlNamespace)
                {
                    switch (reader.LocalName)
                    {
                        case "lang":
                            CheckForDuplicates(ref sCulture, reader);
                            try
                            {
                                culture = new CultureInfo(sCulture);
                            }
                            catch (ArgumentException e)
                            {
                                innerException = e;
                                // Unknown Culture info, fall back to the base culture.
                                int pos = reader.Value.IndexOf("-", StringComparison.Ordinal);
                                if (pos > 0)
                                {
                                    try
                                    {
                                        culture = new CultureInfo(reader.Value.Substring(0, pos));
                                    }
                                    catch (ArgumentException)
                                    {
                                        isInvalidAttribute = true;
                                    }
                                }
                                else
                                {
                                    isInvalidAttribute = true;
                                }
                            }
                            break;

                        case "base":
                            CheckForDuplicates(ref sBaseUri, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                else if (reader.NamespaceURI == xmlNamespaceXmlns)
                {
                    if (reader.Value != xmlNamespaceSsml && reader.Value != xmlNamespacePrompt)
                    {
                        ssmlAttributes._unknownNamespaces.Add(new SsmlXmlAttribute(reader.Prefix, reader.LocalName, reader.Value, reader.NamespaceURI));
                    }
                    else if (reader.Value == xmlNamespacePrompt)
                    {
                        engine.ContainsPexml(reader.LocalName);
                    }
                }
                else
                {
                    extraSpeakAttributes.Add(new SsmlXmlAttribute(reader.Prefix, reader.LocalName, reader.Value, reader.NamespaceURI));
                }

                if (isInvalidAttribute)
                {
                    ThrowFormatException(innerException, SRID.InvalidElement, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sVersion))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "version", "speak");
            }

            if (string.IsNullOrEmpty(sCulture))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "lang", "speak");
            }

            // append the local attributes to list of unknown attributes
            List<SsmlXmlAttribute> extraAttributes = null;
            foreach (SsmlXmlAttribute attribute in extraSpeakAttributes)
            {
                ssmlAttributes.AddUnknowAttribute(attribute, ref extraAttributes);
            }

            voice = engine.ProcessSpeak(sVersion, sBaseUri, culture, ssmlAttributes._unknownNamespaces);

            ssmlAttributes._fragmentState.LangId = culture.LCID;
            ssmlAttributes._voice = voice;
            ssmlAttributes._baseUri = sBaseUri;

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.Lexicon | SsmlElement.Meta | SsmlElement.MetaData | SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            ProcessElement(reader, engine, "speak", possibleChild, ssmlAttributes, false, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndSpeakElement();
        }

        /// <summary>
        /// Generic method to process an SSML element.
        /// The element name is fetch from the element name array and
        /// the delegate for that element will be called.
        /// </summary>
        private static void ProcessElement(XmlReader reader, ISsmlParser engine, string sElement, SsmlElement possibleElements, SsmlAttributes ssmAttributesParent, bool fIgnore, List<SsmlXmlAttribute> extraAttributes)
        {
            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            // Flush any remaining attributes from the previous element list
            if (extraAttributes != null && extraAttributes.Count > 0)
            {
                engine.StartProcessUnknownAttributes(ssmlAttributes._voice, ref ssmlAttributes._fragmentState, sElement, extraAttributes);
            }

            // Move to containing element of attributes
            reader.MoveToElement();
            if (!reader.IsEmptyElement)
            {
                // Process each child element while not at end element
                reader.Read();
                do
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            int iElement = Array.BinarySearch<string>(s_elementsName, reader.LocalName);
                            if (iElement >= 0)
                            {
                                s_parseElements[iElement](reader, engine, possibleElements, ssmlAttributes, fIgnore);
                            }
                            else
                            {
                                // Could be an element from some undefined namespace
                                if (!ssmlAttributes.IsOtherNamespaceElement(reader))
                                {
                                    ThrowFormatException(SRID.InvalidElement, reader.Name);
                                }
                                else
                                {
                                    engine.ProcessUnknownElement(ssmlAttributes._voice, ref ssmlAttributes._fragmentState, reader);
                                    continue;
                                }
                            }
                            reader.Read();
                            break;

                        case XmlNodeType.Text:
                            if ((possibleElements & SsmlElement.Text) != 0)
                            {
                                engine.ProcessText(reader.Value, ssmlAttributes._voice, ref ssmlAttributes._fragmentState, GetColumnPosition(reader), fIgnore);
                            }
                            else
                            {
                                ThrowFormatException(SRID.InvalidElement, reader.Name);
                            }
                            reader.Read();
                            break;

                        case XmlNodeType.EndElement:
                            break;

                        default:
                            reader.Read();
                            break;
                    }
                }
                while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None);
            }

            // Flush any remaining attributes from the previous element list
            if (extraAttributes != null && extraAttributes.Count > 0)
            {
                engine.EndProcessUnknownAttributes(ssmlAttributes._voice, ref ssmlAttributes._fragmentState, sElement, extraAttributes);
            }
        }

        private static void ParseAudio(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Audio, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sUri = null;
            bool fRenderDesc = false;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "src":
                            CheckForDuplicates(ref sUri, reader);
                            // Audio element
                            try
                            {
                                engine.ProcessAudio(ssmlAttributes._voice, sUri, ssmlAttributes._baseUri, fIgnore);
                            }
                            catch (IOException)
                            {
                                fRenderDesc = true;
                            }
                            catch (WebException)
                            {
                                fRenderDesc = true;
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            ssmlAttributes._fRenderDesc = fRenderDesc;

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.Desc | SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            ProcessElement(reader, engine, sElement, possibleChild, ssmlAttributes, !fRenderDesc, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseBreak(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Break, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;
            ssmlAttributes._fragmentState.Action = TtsEngineAction.Silence;
            ssmlAttributes._fragmentState.Emphasis = (int)EmphasisBreak.Default;

            string sTime = null;
            string sStrength = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "time":
                            {
                                CheckForDuplicates(ref sTime, reader);
                                ssmlAttributes._fragmentState.Emphasis = (int)EmphasisBreak.None;
                                ssmlAttributes._fragmentState.Duration = ParseCSS2Time(sTime);
                                isInvalidAttribute = ssmlAttributes._fragmentState.Duration < 0;
                            }
                            break;

                        case "strength":
                            CheckForDuplicates(ref sStrength, reader);
                            if (sTime == null)
                            {
                                ssmlAttributes._fragmentState.Duration = 0;
                                int pos = Array.BinarySearch<string>(s_breakStrength, sStrength);
                                if (pos < 0)
                                {
                                    isInvalidAttribute = true;
                                }
                                else
                                {
                                    // SSML Spec if both strength and time are supplied, ignore strength
                                    if (ssmlAttributes._fragmentState.Emphasis != (int)EmphasisBreak.None)
                                    {
                                        ssmlAttributes._fragmentState.Emphasis = (int)s_breakEmphasis[pos];
                                    }
                                }
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidSpeakAttribute, reader.Name, "break");
                }
            }

            engine.ProcessBreak(ssmlAttributes._voice, ref ssmlAttributes._fragmentState, (EmphasisBreak)ssmlAttributes._fragmentState.Emphasis, ssmlAttributes._fragmentState.Duration, fIgnore);

            // No Children allowed .
            ProcessElement(reader, engine, sElement, 0, ssmlAttributes, true, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseDesc(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Desc, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sCulture = null;
            CultureInfo culture = null;
            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = reader.NamespaceURI != xmlNamespace;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        // The W3C spec says ignore
                        case "lang":
                            CheckForDuplicates(ref sCulture, reader);
                            try
                            {
                                culture = new CultureInfo(sCulture);
                            }
                            catch (ArgumentException)
                            {
                                isInvalidAttribute = true;
                            }
                            isInvalidAttribute &= culture != null;
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            engine.ProcessDesc(culture);

            // Process child elements.
            ProcessElement(reader, engine, sElement, SsmlElement.Text, ssmlAttributes, true, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseEmphasis(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Emphasis, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            // Set the default value
            ssmlAttributes._fragmentState.Emphasis = (int)EmphasisWord.Moderate;

            string sLevel = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        // The W3C spec says ignore
                        case "level":
                            CheckForDuplicates(ref sLevel, reader);
                            int pos = Array.BinarySearch<string>(s_emphasisNames, sLevel);
                            if (pos < 0)
                            {
                                isInvalidAttribute = true;
                            }
                            else
                            {
                                ssmlAttributes._fragmentState.Emphasis = (int)s_emphasisWord[pos];
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            engine.ProcessEmphasis(!string.IsNullOrEmpty(sLevel), (EmphasisWord)ssmlAttributes._fragmentState.Emphasis);

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            ProcessElement(reader, engine, sElement, possibleChild, ssmlAttributes, fIgnore, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseMark(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Mark, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sName = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        // The W3C spec says ignore
                        case "name":
                            CheckForDuplicates(ref sName, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sName))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "name", "mark");
            }

            ssmlAttributes._fragmentState.Action = TtsEngineAction.Bookmark;
            engine.ProcessMark(ssmlAttributes._voice, ref ssmlAttributes._fragmentState, sName, fIgnore);

            // No Children allowed.
            ProcessElement(reader, engine, sElement, 0, ssmlAttributes, true, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseMetaData(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            ValidateElement(element, SsmlElement.MetaData, reader.Name);

            // No processing for this element, skip
            if (!reader.IsEmptyElement)
            {
                int cEndNode = 1;
                do
                {
                    // Loop until we reach the end of the metadata element
                    reader.Read();

                    // Count the number of elements processed
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        cEndNode++;
                    }
                    if (reader.NodeType == XmlNodeType.EndElement || reader.NodeType == XmlNodeType.None)
                    {
                        cEndNode--;
                    }
                }
                while (cEndNode > 0);

                // Consume the end element
                System.Diagnostics.Debug.Assert(reader.NodeType == XmlNodeType.EndElement);
            }
        }

        private static void ParseParagraph(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Paragraph, reader.Name);

            ParseTextBlock(reader, engine, true, sElement, ssmAttributesParent, fIgnore);
        }

        private static void ParseSentence(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Sentence, reader.Name);

            ParseTextBlock(reader, engine, false, sElement, ssmAttributesParent, fIgnore);
        }

        private static void ParseTextBlock(XmlReader reader, ISsmlParser engine, bool isParagraph, string sElement, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sCulture = null;
            CultureInfo culture = null;
            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = reader.NamespaceURI != xmlNamespace;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        // The W3C spec says ignore
                        case "lang":
                            CheckForDuplicates(ref sCulture, reader);
                            try
                            {
                                culture = new CultureInfo(sCulture);
                            }
                            catch (ArgumentException)
                            {
                                isInvalidAttribute = true;
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            // Try to change the voice
            bool fNewCulture = culture != null && culture.LCID != ssmlAttributes._fragmentState.LangId;
            ssmlAttributes._voice = engine.ProcessTextBlock(isParagraph, ssmlAttributes._voice, ref ssmlAttributes._fragmentState, culture, fNewCulture, ssmlAttributes._gender, ssmlAttributes._age);
            if (fNewCulture)
            {
                ssmlAttributes._fragmentState.LangId = culture.LCID;
            }

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            if (isParagraph)
            {
                possibleChild |= SsmlElement.Sentence;
            }
            ProcessElement(reader, engine, sElement, possibleChild, ssmlAttributes, fIgnore, extraAttributes);

            engine.EndProcessTextBlock(isParagraph);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParsePhoneme(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Phoneme, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sAlphabet = null;
            AlphabetType alphabet = AlphabetType.Ipa;
            string sPh = null;
            char[] aPhoneIds = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "alphabet":
                            CheckForDuplicates(ref sAlphabet, reader);
                            switch (sAlphabet)
                            {
                                case "ipa":
                                    alphabet = AlphabetType.Ipa;
                                    break;

                                case "sapi":
                                case "x-sapi":
                                case "x-microsoft-sapi":
                                    alphabet = AlphabetType.Sapi;
                                    break;

                                case "ups":
                                case "x-ups":
                                case "x-microsoft-ups":
                                    alphabet = AlphabetType.Ups;
                                    break;

                                default:
                                    throw new FormatException(SR.Get(SRID.UnsupportedAlphabet, sAlphabet));
                            }
                            break;

                        case "ph":
                            CheckForDuplicates(ref sPh, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sPh))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "ph", "phoneme");
            }

            // Try to convert the phoneme set
            try
            {
                switch (alphabet)
                {
                    case AlphabetType.Sapi:
                        aPhoneIds = PhonemeConverter.ConvertPronToId(sPh, ssmlAttributes._fragmentState.LangId).ToCharArray();
                        break;

                    case AlphabetType.Ups:
                        aPhoneIds = PhonemeConverter.UpsConverter.ConvertPronToId(sPh).ToCharArray();
                        alphabet = AlphabetType.Ipa;
                        break;

                    case AlphabetType.Ipa:
                    default:
                        aPhoneIds = sPh.ToCharArray();
                        try
                        {
                            PhonemeConverter.ValidateUpsIds(aPhoneIds);
                        }
                        catch (FormatException)
                        {
                            if (sAlphabet != null)
                            {
                                throw;
                            }
                            else
                            {
                                // try with sapi (backward compatibility)
                                // if not a sapi phoneme either throw the IPA exception
                                aPhoneIds = PhonemeConverter.ConvertPronToId(sPh, ssmlAttributes._fragmentState.LangId).ToCharArray();
                                alphabet = AlphabetType.Sapi;
                            }
                        }
                        break;
                }
            }
            catch (FormatException)
            {
                ThrowFormatException(SRID.InvalidItemAttribute, "phoneme");
            }

            engine.ProcessPhoneme(ref ssmlAttributes._fragmentState, alphabet, sPh, aPhoneIds);

            // Process child elements.
            ProcessElement(reader, engine, sElement, SsmlElement.Text, ssmlAttributes, fIgnore, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseProsody(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Prosody, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sPitch = null;
            string sContour = null;
            string sRange = null;
            string sRate = null;
            string sDuration = null;
            string sVolume = null;
            Prosody prosody = ssmlAttributes._fragmentState.Prosody != null ? ssmlAttributes._fragmentState.Prosody.Clone() : new Prosody();
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "pitch":
                            isInvalidAttribute = ParseNumberHz(reader, ref sPitch, s_pitchNames, s_pitchWords, ref prosody._pitch);
                            break;

                        case "range":
                            isInvalidAttribute = ParseNumberHz(reader, ref sRange, s_rangeNames, s_rangeWords, ref prosody._range);
                            break;

                        case "rate":
                            isInvalidAttribute = ParseNumberRelative(reader, ref sRate, s_rateNames, s_rateWords, ref prosody._rate);
                            break;

                        case "volume":
                            isInvalidAttribute = ParseNumberRelative(reader, ref sVolume, s_volumeNames, s_volumeWords, ref prosody._volume);
                            break;

                        case "duration":
                            CheckForDuplicates(ref sDuration, reader);
                            prosody.Duration = ParseCSS2Time(sDuration);
                            break;

                        case "contour":
                            CheckForDuplicates(ref sContour, reader);
                            prosody.SetContourPoints(ParseContour(sContour));
                            if (prosody.GetContourPoints() == null) { isInvalidAttribute = true; }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sPitch) && string.IsNullOrEmpty(sContour) && string.IsNullOrEmpty(sRange) && string.IsNullOrEmpty(sRate) && string.IsNullOrEmpty(sDuration) && string.IsNullOrEmpty(sVolume))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "pitch, contour, range, rate, duration, volume", "prosody");
            }

            ssmlAttributes._fragmentState.Prosody = prosody;

            engine.ProcessProsody(sPitch, sRange, sRate, sVolume, sDuration, sContour);

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            ProcessElement(reader, engine, sElement, possibleChild, ssmlAttributes, fIgnore, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseSayAs(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.SayAs, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sInterpretAs = null;
            string sFormat = null;
            string sDetail = null;
            System.Speech.Synthesis.TtsEngine.SayAs sayAs = new();
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "type":
                        case "interpret-as":
                            CheckForDuplicates(ref sInterpretAs, reader);
                            sayAs.InterpretAs = sInterpretAs;
                            break;

                        case "format":
                            CheckForDuplicates(ref sFormat, reader);
                            sayAs.Format = sFormat;
                            break;

                        case "detail":
                            CheckForDuplicates(ref sDetail, reader);
                            sayAs.Detail = sDetail;
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sInterpretAs))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "interpret-as", "say-as");
            }

            // Create SayAs attribute
            ssmlAttributes._fragmentState.SayAs = sayAs;

            engine.ProcessSayAs(sInterpretAs, sFormat, sDetail);

            // Process child elements.
            ProcessElement(reader, engine, sElement, SsmlElement.Text, ssmlAttributes, fIgnore, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseSub(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Sub, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sAlias = null;
            int textPosition = 0;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        // The W3C spec says ignore
                        case "alias":
                            CheckForDuplicates(ref sAlias, reader);
                            XmlTextReader textReader = reader as XmlTextReader;
                            if (textReader != null && engine.Ssml != null)
                            {
                                textPosition = engine.Ssml.IndexOf(reader.Value, textReader.LinePosition + reader.LocalName.Length, StringComparison.Ordinal);
                            }
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sAlias))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "alias", "sub");
            }

            engine.ProcessSub(sAlias, ssmlAttributes._voice, ref ssmlAttributes._fragmentState, textPosition, fIgnore);

            // The only allowed children element is text. Ignore it
            ProcessElement(reader, engine, sElement, SsmlElement.Text, ssmlAttributes, true, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }
        private static void ParseVoice(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Voice, reader.Name);

            // Cannot have a voice element in a Prompt bout
            if (ssmAttributesParent._cPromptOutput > 0)
            {
                ThrowFormatException(SRID.InvalidVoiceElementInPromptOutput);
            }

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sCulture = null;
            string sGender = null;
            string sVariant = null;
            string sName = null;
            string sAge = null;
            string xmlns = null;
            CultureInfo culture = null;
            int variant = -1;

            List<SsmlXmlAttribute> extraAttributes = null;
            List<SsmlXmlAttribute> extraAttributesVoice = null;
            List<SsmlXmlAttribute> localUnknownNamespaces = null;

            while (reader.MoveToNextAttribute())
            {
                bool isInvalidAttribute = false;

                // empty namespace
                if (reader.NamespaceURI.Length == 0)
                {
                    switch (reader.LocalName)
                    {
                        case "gender":
                            CheckForDuplicates(ref sGender, reader);
                            VoiceGender gender;
                            if (!SsmlParserHelpers.TryConvertGender(sGender, out gender))
                            {
                                isInvalidAttribute = true;
                            }
                            else
                            {
                                ssmlAttributes._gender = gender;
                            }
                            break;

                        case "age":
                            CheckForDuplicates(ref sAge, reader);
                            VoiceAge age;
                            if (!SsmlParserHelpers.TryConvertAge(sAge, out age))
                            {
                                isInvalidAttribute = true;
                            }
                            else
                            {
                                ssmlAttributes._age = age;
                            }
                            break;

                        case "variant":
                            // Ignore this field. We have no way with the current tokens to
                            // use it
                            CheckForDuplicates(ref sVariant, reader);
                            if (!int.TryParse(sVariant, out variant) || variant <= 0)
                            {
                                isInvalidAttribute = true;
                            }
                            break;

                        case "name":
                            CheckForDuplicates(ref sName, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                else
                {
                    if (reader.Prefix == "xmlns" && reader.Value == xmlNamespacePrompt)
                    {
                        CheckForDuplicates(ref xmlns, reader);
                    }
                    else
                    {
                        if (reader.NamespaceURI == xmlNamespace)
                        {
                            switch (reader.LocalName)
                            {
                                // The W3C spec says ignore
                                case "lang":
                                    CheckForDuplicates(ref sCulture, reader);
                                    try
                                    {
                                        culture = new CultureInfo(sCulture);
                                    }
                                    catch (ArgumentException)
                                    {
                                        isInvalidAttribute = true;
                                    }
                                    break;

                                default:
                                    isInvalidAttribute = true;
                                    break;
                            }
                        }
                        else if (reader.NamespaceURI == xmlNamespaceXmlns)
                        {
                            if (reader.Value != xmlNamespaceSsml)
                            {
                                localUnknownNamespaces ??= new List<SsmlXmlAttribute>();

                                SsmlXmlAttribute ns = new(reader.Prefix, reader.LocalName, reader.Value, reader.NamespaceURI);
                                localUnknownNamespaces.Add(ns);
                                ssmlAttributes._unknownNamespaces.Add(ns);
                            }
                        }
                        else
                        {
                            extraAttributesVoice ??= new List<SsmlXmlAttribute>();
                            extraAttributesVoice.Add(new SsmlXmlAttribute(reader.Prefix, reader.LocalName, reader.Value, reader.NamespaceURI));
                        }
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            // append the local attributes to list of unknown attributes
            if (extraAttributesVoice != null)
            {
                foreach (SsmlXmlAttribute attribute in extraAttributesVoice)
                {
                    ssmlAttributes.AddUnknowAttribute(attribute, ref extraAttributes);
                }
            }

            if (string.IsNullOrEmpty(sCulture) && string.IsNullOrEmpty(sGender) && string.IsNullOrEmpty(sAge) && string.IsNullOrEmpty(sVariant) && string.IsNullOrEmpty(sName) && string.IsNullOrEmpty(xmlns))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "'xml:lang' or 'gender' or 'age' or 'variant' or 'name'", "voice");
            }

            // Try to change the voice
            culture ??= new CultureInfo(ssmlAttributes._fragmentState.LangId);
            bool fNewCulture = culture.LCID != ssmlAttributes._fragmentState.LangId;
            ssmlAttributes._voice = engine.ProcessVoice(sName, culture, ssmlAttributes._gender, ssmlAttributes._age, variant, fNewCulture, localUnknownNamespaces);
            ssmlAttributes._fragmentState.LangId = culture.LCID;

            // Process child elements.
            SsmlElement possibleChild = SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes);
            ProcessElement(reader, engine, sElement, possibleChild, ssmlAttributes, fIgnore, extraAttributes);

            // remove the local namespaces
            if (localUnknownNamespaces != null)
            {
                foreach (SsmlXmlAttribute ns in localUnknownNamespaces)
                {
                    ssmlAttributes._unknownNamespaces.Remove(ns);
                }
            }

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseLexicon(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.Lexicon, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();
            List<SsmlXmlAttribute> extraAttributes = null;

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            string sUri = null;
            string sMediaType = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = reader.NamespaceURI.Length != 0;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "uri":
                            CheckForDuplicates(ref sUri, reader);
                            break;

                        case "type":
                            CheckForDuplicates(ref sMediaType, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute && !ssmlAttributes.AddUnknowAttribute(reader, ref extraAttributes))
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            if (string.IsNullOrEmpty(sUri))
            {
                ThrowFormatException(SRID.MissingRequiredAttribute, "uri", "lexicon");
            }

            // Add the base path if it exist
            Uri uri = new(sUri, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri && ssmlAttributes._baseUri != null)
            {
                sUri = ssmlAttributes._baseUri + '/' + sUri;
                uri = new Uri(sUri, UriKind.RelativeOrAbsolute);
            }

            engine.ProcessLexicon(uri, sMediaType);

            // No Children allowed.
            ProcessElement(reader, engine, sElement, 0, ssmlAttributes, true, extraAttributes);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        #region Prompt Engine

        private delegate bool ProcessPromptEngine0(object voice);
        private delegate bool ProcessPromptEngine1(object voice, string value);

        private static void ParsePromptEngine0(XmlReader reader, ISsmlParser engine, SsmlElement elementAllowed, SsmlElement element, ProcessPromptEngine0 process, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(elementAllowed, element, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            // No attributes allowed
            while (reader.MoveToNextAttribute())
            {
                if (reader.NamespaceURI == xmlNamespaceXmlns && reader.Value == xmlNamespacePrompt)
                {
                    engine.ContainsPexml(reader.LocalName);
                }
                else
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            // Notify the engine that the element is processed
            if (!process(ssmlAttributes._voice))
            {
                ThrowFormatException(SRID.InvalidElement, reader.Name);
            }

            // Process Children
            ProcessElement(reader, engine, sElement, SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes), ssmlAttributes, fIgnore, null);
        }

        private static string ParsePromptEngine1(XmlReader reader, ISsmlParser engine, SsmlElement elementAllowed, SsmlElement element, string attribute, ProcessPromptEngine1 process, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(elementAllowed, element, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            // 1 attribute
            string value = null;
            while (reader.MoveToNextAttribute())
            {
                if (reader.LocalName == attribute)
                {
                    CheckForDuplicates(ref value, reader);
                }
                else
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }

            // Notify the engine that the element is processed
            if (!process(ssmlAttributes._voice, value))
            {
                ThrowFormatException(SRID.InvalidElement, reader.Name);
            }

            // No Children allowed
            ProcessElement(reader, engine, sElement, SsmlElement.AudioMarkTextWithStyle | ElementPromptEngine(ssmlAttributes), ssmlAttributes, fIgnore, null);
            return value;
        }

        private static void ParsePromptOutput(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Increase the ref count for the Prompt output
            ssmAttributesParent._cPromptOutput++;

            ParsePromptEngine0(reader, engine, element, SsmlElement.PromptEngineOutput, new ProcessPromptEngine0(engine.BeginPromptEngineOutput), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();

            // Decrease the ref count for the Prompt output
            ssmAttributesParent._cPromptOutput--;
            engine.EndPromptEngineOutput(ssmAttributesParent._voice);
        }

        private static void ParseDiv(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            ParsePromptEngine0(reader, engine, element, SsmlElement.PromptEngineDiv, new ProcessPromptEngine0(engine.ProcessPromptEngineDiv), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseDatabase(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            // Validate the SSML markup
            string sElement = ValidateElement(element, SsmlElement.PromptEngineDatabase, reader.Name);

            // Make a local copy of the ssmlAttribute
            SsmlAttributes ssmlAttributes = new();

            // This is equivalent to a memcpy
            ssmlAttributes = ssmAttributesParent;

            // No attributes allowed
            string fname = null;
            string delta = null;
            string idset = null;
            while (reader.MoveToNextAttribute())
            {
                // Namespace must be empty
                bool isInvalidAttribute = false;

                if (!isInvalidAttribute)
                {
                    switch (reader.LocalName)
                    {
                        case "fname":
                            CheckForDuplicates(ref fname, reader);
                            break;

                        case "idset":
                            CheckForDuplicates(ref idset, reader);
                            break;

                        case "delta":
                            CheckForDuplicates(ref delta, reader);
                            break;

                        default:
                            isInvalidAttribute = true;
                            break;
                    }
                }
                if (isInvalidAttribute)
                {
                    ThrowFormatException(SRID.InvalidItemAttribute, reader.Name);
                }
            }
            // Notify the engine that the element is processed
            if (!engine.ProcessPromptEngineDatabase(ssmlAttributes._voice, fname, delta, idset))
            {
                ThrowFormatException(SRID.InvalidElement, reader.Name);
            }

            // No Children allowed
            ProcessElement(reader, engine, sElement, 0, ssmlAttributes, fIgnore, null);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseId(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            ParsePromptEngine1(reader, engine, element, SsmlElement.PromptEngineId, "id", new ProcessPromptEngine1(engine.ProcessPromptEngineId), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();
        }

        private static void ParseTts(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            ParsePromptEngine0(reader, engine, element, SsmlElement.PromptEngineTTS, new ProcessPromptEngine0(engine.BeginPromptEngineTts), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();
            engine.EndPromptEngineTts(ssmAttributesParent._voice);
        }

        private static void ParseWithTag(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            string tag = ParsePromptEngine1(reader, engine, element, SsmlElement.PromptEngineWithTag, "tag", new ProcessPromptEngine1(engine.BeginPromptEngineWithTag), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();
            engine.EndPromptEngineWithTag(ssmAttributesParent._voice, tag);
        }

        private static void ParseRule(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmAttributesParent, bool fIgnore)
        {
            string name = ParsePromptEngine1(reader, engine, element, SsmlElement.PromptEngineRule, "name", new ProcessPromptEngine1(engine.BeginPromptEngineRule), ssmAttributesParent, fIgnore);

            // Notify the engine that the element is processed
            engine.EndElement();
            engine.EndPromptEngineRule(ssmAttributesParent._voice, name);
        }

        #endregion

        private static void CheckForDuplicates(ref string dest, XmlReader reader)
        {
            if (!string.IsNullOrEmpty(dest))
            {
                StringBuilder attribute = new(reader.LocalName);
                if (reader.NamespaceURI.Length > 0)
                {
                    attribute.Append(reader.NamespaceURI);
                    attribute.Append(':');
                }
                ThrowFormatException(SRID.InvalidAttributeDefinedTwice, reader.Value, attribute);
            }
            dest = reader.Value;
        }

        private static int ParseCSS2Time(string time)
        {
            time = time.Trim(Helpers._achTrimChars);
            int pos = time.IndexOf("ms", StringComparison.Ordinal);
            int duration = -1;
            float fDuration;
            if (pos > 0 && time.Length == pos + 2)
            {
                if (!float.TryParse(time.Substring(0, pos), out fDuration))
                {
                    duration = -1;
                }
                else
                {
                    duration = (int)(fDuration + 0.5);
                }
            }
            else
                if ((pos = time.IndexOf('s')) > 0 && time.Length == pos + 1)
            {
                if (!float.TryParse(time.Substring(0, pos), out fDuration))
                {
                    duration = -1;
                }
                else
                {
                    duration = (int)(fDuration * 1000);
                }
            }
            return duration;
        }

        private static ContourPoint[] ParseContour(string contour)
        {
            char[] achContour = contour.ToCharArray();
            List<ContourPoint> points = new();
            int start = 0;

            try
            {
                while (start < achContour.Length)
                {
                    bool percent, ignored, hz;
                    // Form is (0%, +20Hz)
                    if ((start = NextChar(achContour, start, '(', false, out ignored)) < 0)
                    {
                        // End of the string found exit
                        break;
                    }

                    int comma = NextChar(achContour, start, ',', true, out percent);
                    int parenthesis = NextChar(achContour, comma, ')', true, out ignored);

                    ProsodyNumber timePosition = new();
                    ProsodyNumber target = new();

                    // Parse the 2 numbers
                    if (!percent || !TryParseNumber(contour.Substring(start, comma - (start + 1)), ref timePosition) || timePosition.SsmlAttributeId == ProsodyNumber.AbsoluteNumber)
                    {
                        return null;
                    }
                    if (!TryParseHz(contour.Substring(comma, parenthesis - (comma + 1)), ref target, true, out hz))
                    {
                        return null;
                    }

                    // First point
                    if (points.Count == 0)
                    {
                        // fake a zero entry if none is provided by duplicating the first entry
                        if (timePosition.Number > 0 && timePosition.Number < 100)
                        {
                            points.Add(new ContourPoint(0, target.Number, ContourPointChangeType.Hz));
                        }
                    }
                    else
                    {
                        // Accept only increasing start points
                        // Add a 100% if necessary
                        if (points[points.Count - 1].Start > timePosition.Number)
                        {
                            return null;
                        }
                    }

                    if (timePosition.Number >= 0 && timePosition.Number <= 1)
                    {
                        points.Add(new ContourPoint(timePosition.Number, target.Number, (hz ? ContourPointChangeType.Hz : ContourPointChangeType.Percentage)));
                    }
                    start = parenthesis;
                }
            }
            catch (InvalidOperationException)
            {
                return null;
            }

            if (points.Count < 1)
            {
                return null;
            }

            // Add a 100% if necessary
            if (!points[points.Count - 1].Start.Equals(1.0))
            {
                points.Add(new ContourPoint(1, points[points.Count - 1].Change, points[points.Count - 1].ChangeType));
            }
            return points.ToArray();
        }

        private static int NextChar(char[] ach, int start, char expected, bool skipDigit, out bool percent)
        {
            percent = false;

            // skip the whitespace
            while (start < ach.Length && (ach[start] == ' ' || ach[start] == '\t' || ach[start] == '\n' || ach[start] == '\r'))
            {
                start++;
            }

            // skip the digits
            if (skipDigit)
            {
                while (start < ach.Length && ach[start] != expected && ((percent = ach[start] == '%') || char.IsDigit(ach[start]) || ach[start] == 'H' || ach[start] == 'z' || ach[start] == '.' || ach[start] == '+' || ach[start] == '-'))
                {
                    start++;
                }

                // skip the trailing white spaces
                while (start < ach.Length && (ach[start] == ' ' || ach[start] == '\t' || ach[start] == '\n' || ach[start] == '\r'))
                {
                    start++;
                }
            }

            // Check if we found the character we wanted
            if (!(start < ach.Length && ach[start] == expected))
            {
                // Check for the end of the string
                if (!skipDigit && start == ach.Length)
                {
                    return -1;
                }
                // bail out
                throw new InvalidOperationException();
            }
            return start + 1;
        }

        private static bool ParseNumberHz(XmlReader reader, ref string attribute, string[] attributeValues, int[] attributeConst, ref ProsodyNumber number)
        {
            bool isInvalidAttribute = false;
            bool isHz;

            CheckForDuplicates(ref attribute, reader);
            int pos = Array.BinarySearch<string>(attributeValues, attribute);
            if (pos < 0)
            {
                if (!TryParseHz(attribute, ref number, false, out isHz))
                {
                    isInvalidAttribute = true;
                }
            }
            else
            {
                number = new ProsodyNumber(attributeConst[pos]);
            }
            return isInvalidAttribute;
        }

        private static bool ParseNumberRelative(XmlReader reader, ref string attribute, string[] attributeValues, int[] attributeConst, ref ProsodyNumber number)
        {
            bool isInvalidAttribute = false;

            CheckForDuplicates(ref attribute, reader);
            int pos = Array.BinarySearch<string>(attributeValues, attribute);
            if (pos < 0)
            {
                if (!TryParseNumber(attribute, ref number))
                {
                    isInvalidAttribute = true;
                }
            }
            else
            {
                number = new ProsodyNumber(attributeConst[pos]);
            }
            return isInvalidAttribute;
        }

        private static bool TryParseNumber(string sNumber, ref ProsodyNumber number)
        {
            bool fResult = false;
            decimal value = 0;

            // always reset the unit to Default
            number.Unit = ProsodyUnit.Default;
            sNumber = sNumber.Trim(Helpers._achTrimChars);
            if (!string.IsNullOrEmpty(sNumber))
            {
                if (!decimal.TryParse(sNumber, out value))
                {
                    if (sNumber[sNumber.Length - 1] == '%')
                    {
                        if (decimal.TryParse(sNumber.Substring(0, sNumber.Length - 1), out value))
                        {
                            float percent = (float)value / 100f;
                            if (sNumber[0] != '+' && sNumber[0] != '-')
                            {
                                number.Number *= percent;
                            }
                            else
                            {
                                number.Number += number.Number * (percent);
                            }

                            fResult = true;
                        }
                    }
                }
                else
                {
                    if (sNumber[0] != '+' && sNumber[0] != '-')
                    {
                        number.Number = (float)value;
                        number.SsmlAttributeId = ProsodyNumber.AbsoluteNumber;
                    }
                    else
                    {
                        if (number.IsNumberPercent)
                        {
                            number.Number *= (float)value;
                        }
                        else
                        {
                            number.Number += (float)value;
                        }
                    }
                    number.IsNumberPercent = false;
                    fResult = true;
                }
            }
            return fResult;
        }

        private static bool TryParseHz(string sNumber, ref ProsodyNumber number, bool acceptHzRelative, out bool isHz)
        {
            isHz = false;

            // Find the Hz at the end of the number
            bool fResult = false;
            number.SsmlAttributeId = ProsodyNumber.AbsoluteNumber;
            ProsodyUnit unit = ProsodyUnit.Default;

            sNumber = sNumber.Trim(Helpers._achTrimChars);
            if (sNumber.IndexOf("Hz", StringComparison.Ordinal) == sNumber.Length - 2)
            {
                unit = ProsodyUnit.Hz;
            }
            else if (sNumber.IndexOf("st", StringComparison.Ordinal) == sNumber.Length - 2)
            {
                unit = ProsodyUnit.Semitone;
            }

            if (unit != ProsodyUnit.Default)
            {
                // Try as an Absolute Hz value
                fResult = TryParseNumber(sNumber.Substring(0, sNumber.Length - 2), ref number) && (acceptHzRelative || number.SsmlAttributeId == ProsodyNumber.AbsoluteNumber);
                isHz = true;
            }
            else
            {
                // Must be a relative number
                fResult = TryParseNumber(sNumber, ref number) && number.SsmlAttributeId == ProsodyNumber.AbsoluteNumber;
            }

            return fResult;
        }

        /// <summary>
        /// Ensure the this element is properly placed in the SSML markup
        /// </summary>
        private static string ValidateElement(SsmlElement possibleElements, SsmlElement currentElement, string sElement)
        {
            if ((possibleElements & currentElement) == 0)
            {
                ThrowFormatException(SRID.InvalidElement, sElement);
            }
            return sElement;
        }

        /// <summary>
        /// Throws an Exception with the error specified by the resource ID.
        /// </summary>
        private static void ThrowFormatException(SRID id, params object[] args)
        {
            throw new FormatException(SR.Get(id, args));
        }

        /// <summary>
        /// Throws an Exception with the error specified by the resource ID.
        /// </summary>
        private static void ThrowFormatException(Exception innerException, SRID id, params object[] args)
        {
            throw new FormatException(SR.Get(id, args), innerException);
        }

        /// <summary>
        /// Non speakable element
        /// </summary>
        private static void NoOp(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmlAttributes, bool fIgnore)
        {
            // No Children allowed .
            ProcessElement(reader, engine, null, 0, ssmlAttributes, true, null);
        }

        private static SsmlElement ElementPromptEngine(SsmlAttributes ssmlAttributes)
        {
            return ssmlAttributes._cPromptOutput > 0 ? SsmlElement.PromptEngineChildren : 0;
        }

        private static int GetColumnPosition(XmlReader reader)
        {
            XmlTextReader textReader = reader as XmlTextReader;
            return textReader != null ? textReader.LinePosition - 1 : 0;
        }

        #endregion

        #region Private Types

        private struct SsmlAttributes
        {
            internal object _voice;
            internal FragmentState _fragmentState;
            internal bool _fRenderDesc;
            internal VoiceAge _age;
            internal VoiceGender _gender;
            internal string _baseUri;
            internal short _cPromptOutput;
            internal List<SsmlXmlAttribute> _unknownNamespaces;

            internal bool AddUnknowAttribute(SsmlXmlAttribute attribute, ref List<SsmlXmlAttribute> extraAttributes)
            {
                foreach (SsmlXmlAttribute ns in _unknownNamespaces)
                {
                    if (ns._name == attribute._prefix)
                    {
                        extraAttributes ??= new List<SsmlXmlAttribute>();
                        extraAttributes.Add(attribute);
                        return true;
                    }
                }
                return false;
            }

            internal bool AddUnknowAttribute(XmlReader reader, ref List<SsmlXmlAttribute> extraAttributes)
            {
                foreach (SsmlXmlAttribute ns in _unknownNamespaces)
                {
                    if (ns._name == reader.Prefix)
                    {
                        extraAttributes ??= new List<SsmlXmlAttribute>();
                        extraAttributes.Add(new SsmlXmlAttribute(reader.Prefix, reader.LocalName, reader.Value, reader.NamespaceURI));
                        return true;
                    }
                }
                return false;
            }

            internal bool IsOtherNamespaceElement(XmlReader reader)
            {
                foreach (SsmlXmlAttribute ns in _unknownNamespaces)
                {
                    if (ns._name == reader.Prefix)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private delegate void ParseElementDelegates(XmlReader reader, ISsmlParser engine, SsmlElement element, SsmlAttributes ssmlAttributes, bool fIgnore);

        #endregion

        #region Private Fields

        private static readonly string[] s_elementsName = new string[]
        {
            "audio",
            "break",
            "database",
            "desc",
            "div",
            "emphasis",
            "id",
            "lexicon",
            "mark",
            "meta",
            "metadata",
            "p",
            "paragraph",
            "phoneme",
            "prompt_output",
            "prosody",
            "rule",
            "s",
            "say-as",
            "sentence",
            "speak",
            "sub",
            "tts",
            "voice",
            "withtag",
        };

        private static readonly ParseElementDelegates[] s_parseElements = new ParseElementDelegates[]
            {
                new ParseElementDelegates (ParseAudio),
                new ParseElementDelegates (ParseBreak),
                new ParseElementDelegates (ParseDatabase),
                new ParseElementDelegates (ParseDesc),
                new ParseElementDelegates (ParseDiv),
                new ParseElementDelegates (ParseEmphasis),
                new ParseElementDelegates (ParseId),
                new ParseElementDelegates (ParseLexicon),
                new ParseElementDelegates (ParseMark),
                new ParseElementDelegates (NoOp),
                new ParseElementDelegates (ParseMetaData),
                new ParseElementDelegates (ParseParagraph),
                new ParseElementDelegates (ParseParagraph),
                new ParseElementDelegates (ParsePhoneme),
                new ParseElementDelegates (ParsePromptOutput),
                new ParseElementDelegates (ParseProsody),
                new ParseElementDelegates (ParseRule),
                new ParseElementDelegates (ParseSentence),
                new ParseElementDelegates (ParseSayAs),
                new ParseElementDelegates (ParseSentence),
                new ParseElementDelegates (NoOp),
                new ParseElementDelegates (ParseSub),
                new ParseElementDelegates (ParseTts),
                new ParseElementDelegates (ParseVoice),
                new ParseElementDelegates (ParseWithTag)
            };

        private static readonly string[] s_breakStrength = new string[]
        {
            "medium", "none", "strong", "weak", "x-strong", "x-weak"
        };

        /// <summary>
        /// Must be in the same order as the _breakStrength enumeration
        /// </summary>
        private static readonly EmphasisBreak[] s_breakEmphasis = new EmphasisBreak[]
        {
            EmphasisBreak.Medium, EmphasisBreak.None, EmphasisBreak.Strong, EmphasisBreak.Weak, EmphasisBreak.ExtraStrong, EmphasisBreak.ExtraWeak
        };

        private static readonly string[] s_emphasisNames = new string[]
        {
            "moderate", "none", "reduced", "strong"
        };

        /// <summary>
        /// Must be in the same order as the _emphasisNames enumeration
        /// </summary>
        private static readonly EmphasisWord[] s_emphasisWord = new EmphasisWord[]
        {
            EmphasisWord.Moderate, EmphasisWord.None, EmphasisWord.Reduced, EmphasisWord.Strong
        };

        /// <summary>
        /// Must be in the same order as the _emphasisNames enumeration
        /// </summary>
        private static readonly int[] s_pitchWords = new int[]
        {
            (int) ProsodyPitch.Default, (int) ProsodyPitch.High, (int) ProsodyPitch.Low, (int) ProsodyPitch.Medium, (int) ProsodyPitch.ExtraHigh, (int) ProsodyPitch.ExtraLow
        };

        private static readonly string[] s_pitchNames = new string[]
        {
            "default", "high", "low", "medium", "x-high", "x-low",
        };

        /// <summary>
        /// Must be in the same order as the _emphasisNames enumeration
        /// </summary>
        private static readonly int[] s_rangeWords = new int[]
        {
            (int) ProsodyRange.Default, (int) ProsodyRange.High, (int) ProsodyRange.Low, (int) ProsodyRange.Medium, (int) ProsodyRange.ExtraHigh, (int) ProsodyRange.ExtraLow
        };

        private static readonly string[] s_rangeNames = new string[]
        {
            "default", "high", "low", "medium", "x-high", "x-low",
        };

        /// <summary>
        /// Must be in the same order as the _emphasisNames enumeration
        /// </summary>
        private static readonly int[] s_rateWords = new int[]
        {
            (int) ProsodyRate.Default, (int) ProsodyRate.Fast, (int) ProsodyRate.Medium, (int) ProsodyRate.Slow, (int) ProsodyRate.ExtraFast, (int) ProsodyRate.ExtraSlow
        };

        private static readonly string[] s_rateNames = new string[]
        {
            "default", "fast", "medium", "slow", "x-fast", "x-slow",
        };

        /// <summary>
        /// Must be in the same order as the _emphasisNames enumeration
        /// </summary>
        private static readonly int[] s_volumeWords = new int[]
        {
            (int) ProsodyVolume.Default, (int) ProsodyVolume.Loud, (int) ProsodyVolume.Medium, (int) ProsodyVolume.Silent, (int) ProsodyVolume.Soft, (int) ProsodyVolume.ExtraLoud, (int) ProsodyVolume.ExtraSoft
        };

        private static readonly string[] s_volumeNames = new string[]
        {
            "default", "loud", "medium", "silent", "soft", "x-loud", "x-soft",
        };

        private const string xmlNamespace = "http://www.w3.org/XML/1998/namespace";
        private const string xmlNamespaceSsml = "http://www.w3.org/2001/10/synthesis";
        private const string xmlNamespaceXmlns = "http://www.w3.org/2000/xmlns/";
        private const string xmlNamespacePrompt = "http://schemas.microsoft.com/Speech/2003/03/PromptEngine";

        #endregion
    }

    internal static class SsmlParserHelpers
    {
        internal static bool TryConvertAge(string sAge, out VoiceAge age)
        {
            bool fResult = false;
            int iAge;
            age = VoiceAge.NotSet;

            switch (sAge)
            {
                case "child":
                    age = VoiceAge.Child;
                    break;

                case "teenager":
                case "teen":
                    age = VoiceAge.Teen;
                    break;

                case "adult":
                    age = VoiceAge.Adult;
                    break;

                case "elder":
                case "senior":
                    age = VoiceAge.Senior;
                    break;
            }
            if (age != VoiceAge.NotSet)
            {
                fResult = true;
            }
            else if (int.TryParse(sAge, out iAge))
            {
                if (iAge <= ((int)VoiceAge.Teen + (int)VoiceAge.Child) / 2)
                {
                    age = VoiceAge.Child;
                }
                else if (iAge <= ((int)VoiceAge.Adult + (int)VoiceAge.Teen) / 2)
                {
                    age = VoiceAge.Teen;
                }
                else if (iAge <= ((int)VoiceAge.Senior + (int)VoiceAge.Adult) / 2)
                {
                    age = VoiceAge.Adult;
                }
                else
                {
                    age = VoiceAge.Senior;
                }
                fResult = true;
            }
            return fResult;
        }

        internal static bool TryConvertGender(string sGender, out VoiceGender gender)
        {
            bool fResult = false;
            gender = VoiceGender.NotSet;

            int pos = Array.BinarySearch<string>(s_genderNames, sGender);
            if (pos >= 0)
            {
                gender = s_genders[pos];
                fResult = true;
            }
            return fResult;
        }

        private static readonly string[] s_genderNames = new string[]
        {
            "female", "male", "neutral"
        };

        /// <summary>
        /// Must be in the same order as the _genderNames enumeration
        /// </summary>
        private static readonly VoiceGender[] s_genders = new VoiceGender[]
        {
            VoiceGender.Female, VoiceGender.Male, VoiceGender.Neutral
        };
    }

    #region Internal Types

    [Flags]
    internal enum SsmlElement
    {
        Speak = 0x0001,
        Voice = 0x0002,
        Audio = 0x0004,
        Lexicon = 0x0008,
        Meta = 0x0010,
        MetaData = 0x0020,
        Sentence = 0x0040,
        Paragraph = 0x0080,
        SayAs = 0x0100,
        Phoneme = 0x0200,
        Sub = 0x0400,
        Emphasis = 0x0800,
        Break = 0x1000,
        Prosody = 0x2000,
        Mark = 0x4000,
        Desc = 0x8000,
        Text = 0x10000,
        PromptEngineOutput = 0x20000,
        PromptEngineDatabase = 0x40000,
        PromptEngineDiv = 0x80000,
        PromptEngineId = 0x100000,
        PromptEngineTTS = 0x200000,
        PromptEngineWithTag = 0x400000,
        PromptEngineRule = 0x800000,

        ParagraphOrSentence = Sentence | Paragraph,

        AudioMarkTextWithStyle = Audio | Mark | Break | Emphasis | Phoneme | Prosody | SayAs | Sub | Voice | Text | PromptEngineOutput,
        PromptEngineChildren = PromptEngineDatabase | PromptEngineDiv | PromptEngineId | PromptEngineTTS | PromptEngineWithTag | PromptEngineRule
    }

    #endregion
}
