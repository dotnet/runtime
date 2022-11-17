// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Speech.Internal;
using System.Speech.Internal.Synthesis;
using System.Xml;

namespace System.Speech.Synthesis
{
    [Serializable]
    public class PromptBuilder
    {
        #region Constructors
        public PromptBuilder()
            : this(CultureInfo.CurrentUICulture)
        {
        }
        public PromptBuilder(CultureInfo culture)
        {
            Helpers.ThrowIfNull(culture, nameof(culture));

            if (culture.Equals(CultureInfo.InvariantCulture))
            {
                throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
            }
            _culture = culture;

            // Reset all value to default
            ClearContent();
        }

        #endregion

        #region Public Methods

        // Use Append* naming convention.

        /// <summary>
        /// Clear the content of the prompt builder
        /// </summary>
        public void ClearContent()
        {
            _elements.Clear();
            _elementStack.Push(new StackElement(SsmlElement.Lexicon | SsmlElement.Meta | SsmlElement.MetaData | SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle, SsmlState.Header, _culture));
        }

        /// <summary>
        /// Append Text to the SSML stream
        /// </summary>
        public void AppendText(string textToSpeak)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            // Validate that text can be added in this context
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            _elements.Add(new Element(ElementType.Text, textToSpeak));
        }
        public void AppendText(string textToSpeak, PromptRate rate)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            if (rate < PromptRate.NotSet || rate > PromptRate.ExtraSlow)
            {
                throw new ArgumentOutOfRangeException(nameof(rate));
            }

            // Validate that text can be added in this context
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            Element prosodyElement = new(ElementType.Prosody, textToSpeak);
            _elements.Add(prosodyElement);
            string sPromptRate = null;
            switch (rate)
            {
                case PromptRate.NotSet:
                    break;

                case PromptRate.ExtraFast:
                    sPromptRate = "x-fast";
                    break;

                case PromptRate.ExtraSlow:
                    sPromptRate = "x-slow";
                    break;

                default:
                    sPromptRate = rate.ToString().ToLowerInvariant();
                    break;
            }
            if (!string.IsNullOrEmpty(sPromptRate))
            {
                prosodyElement._attributes = new Collection<AttributeItem>();
                prosodyElement._attributes.Add(new AttributeItem("rate", sPromptRate));
            }
        }
        public void AppendText(string textToSpeak, PromptVolume volume)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            if (volume < PromptVolume.NotSet || volume > PromptVolume.Default)
            {
                throw new ArgumentOutOfRangeException(nameof(volume));
            }

            // Validate that text can be added in this context
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            Element prosodyElement = new(ElementType.Prosody, textToSpeak);
            _elements.Add(prosodyElement);

            string sVolumeLevel = null;
            switch (volume)
            {
                // No volume do not set the attribute
                case PromptVolume.NotSet:
                    break;

                case PromptVolume.ExtraSoft:
                    sVolumeLevel = "x-soft";
                    break;

                case PromptVolume.ExtraLoud:
                    sVolumeLevel = "x-loud";
                    break;

                default:
                    sVolumeLevel = volume.ToString().ToLowerInvariant();
                    break;
            }
            if (!string.IsNullOrEmpty(sVolumeLevel))
            {
                prosodyElement._attributes = new Collection<AttributeItem>();
                prosodyElement._attributes.Add(new AttributeItem("volume", sVolumeLevel));
            }
        }
        public void AppendText(string textToSpeak, PromptEmphasis emphasis)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            if (emphasis < PromptEmphasis.NotSet || emphasis > PromptEmphasis.Reduced)
            {
                throw new ArgumentOutOfRangeException(nameof(emphasis));
            }

            // Validate that text can be added in this context
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            Element emphasisElement = new(ElementType.Emphasis, textToSpeak);
            _elements.Add(emphasisElement);

            if (emphasis != PromptEmphasis.NotSet)
            {
                emphasisElement._attributes = new Collection<AttributeItem>();
                emphasisElement._attributes.Add(new AttributeItem("level", emphasis.ToString().ToLowerInvariant()));
            }
        }
        public void StartStyle(PromptStyle style)
        {
            Helpers.ThrowIfNull(style, nameof(style));

            // Validate that text can be added in this context
            StackElement stackElement = _elementStack.Peek();
            ValidateElement(stackElement, SsmlElement.Prosody);

            // For emphasis or Prosody the list of possible elements that can be children is different.
            SsmlState ssmlState = 0;
            SsmlElement possibleChildren = stackElement._possibleChildren;

            _elements.Add(new Element(ElementType.StartStyle));

            if (style.Emphasis != PromptEmphasis.NotSet)
            {
                Element emphasisElement = new(ElementType.Emphasis);
                _elements.Add(emphasisElement);

                emphasisElement._attributes = new Collection<AttributeItem>();
                emphasisElement._attributes.Add(new AttributeItem("level", style.Emphasis.ToString().ToLowerInvariant()));

                // Set the expected children and mark the element used
                possibleChildren = SsmlElement.AudioMarkTextWithStyle;
                ssmlState = SsmlState.StyleEmphasis;
            }

            if (style.Rate != PromptRate.NotSet || style.Volume != PromptVolume.NotSet)
            {
                // two elements add a second start style
                if (ssmlState != 0)
                {
                    _elements.Add(new Element(ElementType.StartStyle));
                }

                Element prosodyElement = new(ElementType.Prosody);
                _elements.Add(prosodyElement);

                if (style.Rate != PromptRate.NotSet)
                {
                    string sPromptRate;
                    switch (style.Rate)
                    {
                        case PromptRate.ExtraFast:
                            sPromptRate = "x-fast";
                            break;

                        case PromptRate.ExtraSlow:
                            sPromptRate = "x-slow";
                            break;

                        default:
                            sPromptRate = style.Rate.ToString().ToLowerInvariant();
                            break;
                    }
                    prosodyElement._attributes = new Collection<AttributeItem>();
                    prosodyElement._attributes.Add(new AttributeItem("rate", sPromptRate));
                }

                if (style.Volume != PromptVolume.NotSet)
                {
                    string sVolumeLevel;
                    switch (style.Volume)
                    {
                        case PromptVolume.ExtraSoft:
                            sVolumeLevel = "x-soft";
                            break;

                        case PromptVolume.ExtraLoud:
                            sVolumeLevel = "x-loud";
                            break;

                        default:
                            sVolumeLevel = style.Volume.ToString().ToLowerInvariant();
                            break;
                    }
                    prosodyElement._attributes ??= new Collection<AttributeItem>();
                    prosodyElement._attributes.Add(new AttributeItem("volume", sVolumeLevel));
                }

                // Set the expected children and mark the element used
                possibleChildren = SsmlElement.ParagraphOrSentence | SsmlElement.AudioMarkTextWithStyle;
                ssmlState |= SsmlState.StyleProsody;
            }

            _elementStack.Push(new StackElement(possibleChildren, ssmlState, stackElement._culture));
        }
        public void EndStyle()
        {
            StackElement stackElement = _elementStack.Pop();
            if (stackElement._state != 0)
            {
                if ((stackElement._state & (SsmlState.StyleEmphasis | SsmlState.StyleProsody)) == 0)
                {
                    throw new InvalidOperationException(SR.Get(SRID.PromptBuilderMismatchStyle));
                }

                _elements.Add(new Element(ElementType.EndStyle));

                // Check if 2 xml elements have been created
                if (stackElement._state == (SsmlState.StyleEmphasis | SsmlState.StyleProsody))
                {
                    _elements.Add(new Element(ElementType.EndStyle));
                }
            }
        }
        public void StartVoice(VoiceInfo voice)
        {
            Helpers.ThrowIfNull(voice, nameof(voice));

            if (!VoiceInfo.ValidateGender(voice.Gender))
            {
                throw new ArgumentException(SR.Get(SRID.EnumInvalid, "VoiceGender"), nameof(voice));
            }

            if (!VoiceInfo.ValidateAge(voice.Age))
            {
                throw new ArgumentException(SR.Get(SRID.EnumInvalid, "VoiceAge"), nameof(voice));
            }

            StackElement stackElement = _elementStack.Peek();
            ValidateElement(stackElement, SsmlElement.Voice);

            CultureInfo culture = voice.Culture ?? stackElement._culture;

            Element startVoice = new(ElementType.StartVoice);
            startVoice._attributes = new Collection<AttributeItem>();
            _elements.Add(startVoice);

            if (!string.IsNullOrEmpty(voice.Name))
            {
                startVoice._attributes.Add(new AttributeItem("name", voice.Name));
            }

            if (voice.Culture != null)
            {
                startVoice._attributes.Add(new AttributeItem("xml", "lang", voice.Culture.Name));
            }

            if (voice.Gender != VoiceGender.NotSet)
            {
                startVoice._attributes.Add(new AttributeItem("gender", voice.Gender.ToString().ToLowerInvariant()));
            }

            if (voice.Age != VoiceAge.NotSet)
            {
                startVoice._attributes.Add(new AttributeItem("age", ((int)voice.Age).ToString(CultureInfo.InvariantCulture)));
            }

            if (voice.Variant >= 0)
            {
                startVoice._attributes.Add(new AttributeItem("variant", voice.Variant.ToString(CultureInfo.InvariantCulture)));
            }

            _elementStack.Push(new StackElement(SsmlElement.Sentence | SsmlElement.AudioMarkTextWithStyle, SsmlState.Voice, culture));
        }
        public void StartVoice(string name)
        {
            Helpers.ThrowIfEmptyOrNull(name, nameof(name));

            StartVoice(new VoiceInfo(name));
        }
        public void StartVoice(VoiceGender gender)
        {
            StartVoice(new VoiceInfo(gender));
        }
        public void StartVoice(VoiceGender gender, VoiceAge age)
        {
            StartVoice(new VoiceInfo(gender, age));
        }
        public void StartVoice(VoiceGender gender, VoiceAge age, int voiceAlternate)
        {
            StartVoice(new VoiceInfo(gender, age, voiceAlternate));
        }
        public void StartVoice(CultureInfo culture)
        {
            StartVoice(new VoiceInfo(culture));
        }
        public void EndVoice()
        {
            if (_elementStack.Pop()._state != SsmlState.Voice)
            {
                throw new InvalidOperationException(SR.Get(SRID.PromptBuilderMismatchVoice));
            }

            _elements.Add(new Element(ElementType.EndVoice));
        }

        // <paragraph>, <sentence>
        public void StartParagraph()
        {
            StartParagraph(null);
        }
        public void StartParagraph(CultureInfo culture)
        {
            // check for well formed document
            StackElement stackElement = _elementStack.Peek();
            ValidateElement(stackElement, SsmlElement.Paragraph);

            Element startParagraph = new(ElementType.StartParagraph);
            _elements.Add(startParagraph);

            if (culture != null)
            {
                if (culture.Equals(CultureInfo.InvariantCulture))
                {
                    throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
                }
                startParagraph._attributes = new Collection<AttributeItem>();
                startParagraph._attributes.Add(new AttributeItem("xml", "lang", culture.Name));
            }
            else
            {
                culture = stackElement._culture;
            }
            _elementStack.Push(new StackElement(SsmlElement.AudioMarkTextWithStyle | SsmlElement.Sentence, SsmlState.Paragraph, culture));
        }
        public void EndParagraph()
        {
            if (_elementStack.Pop()._state != SsmlState.Paragraph)
            {
                throw new InvalidOperationException(SR.Get(SRID.PromptBuilderMismatchParagraph));
            }
            _elements.Add(new Element(ElementType.EndParagraph));
        }
        public void StartSentence()
        {
            StartSentence(null);
        }
        public void StartSentence(CultureInfo culture)
        {
            // check for well formed document
            StackElement stackElement = _elementStack.Peek();
            ValidateElement(stackElement, SsmlElement.Sentence);

            Element startSentence = new(ElementType.StartSentence);
            _elements.Add(startSentence);

            if (culture != null)
            {
                if (culture.Equals(CultureInfo.InvariantCulture))
                {
                    throw new ArgumentException(SR.Get(SRID.InvariantCultureInfo), nameof(culture));
                }

                startSentence._attributes = new Collection<AttributeItem>();
                startSentence._attributes.Add(new AttributeItem("xml", "lang", culture.Name));
            }
            else
            {
                culture = stackElement._culture;
            }
            _elementStack.Push(new StackElement(SsmlElement.AudioMarkTextWithStyle, SsmlState.Sentence, culture));
        }
        public void EndSentence()
        {
            if (_elementStack.Pop()._state != SsmlState.Sentence)
            {
                throw new InvalidOperationException(SR.Get(SRID.PromptBuilderMismatchSentence));
            }
            _elements.Add(new Element(ElementType.EndSentence));
        }
        public void AppendTextWithHint(string textToSpeak, SayAs sayAs)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));

            if (sayAs < SayAs.SpellOut || sayAs > SayAs.Text)
            {
                throw new ArgumentOutOfRangeException(nameof(sayAs));
            }

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            if (sayAs != SayAs.Text)
            {
                Element sayAsElement = new(ElementType.SayAs, textToSpeak);
                _elements.Add(sayAsElement);

                sayAsElement._attributes = new Collection<AttributeItem>();
                string sInterpretAs = null;
                string sFormat = null;

                switch (sayAs)
                {
                    case SayAs.SpellOut:
                        sInterpretAs = "characters";
                        break;

                    case SayAs.NumberOrdinal:
                        sInterpretAs = "ordinal";
                        break;

                    case SayAs.NumberCardinal:
                        sInterpretAs = "cardinal";
                        break;

                    case SayAs.Date:
                        sInterpretAs = "date";
                        break;

                    case SayAs.DayMonthYear:
                        sInterpretAs = "date";
                        sFormat = "dmy";
                        break;

                    case SayAs.MonthDayYear:
                        sInterpretAs = "date";
                        sFormat = "mdy";
                        break;

                    case SayAs.YearMonthDay:
                        sInterpretAs = "date";
                        sFormat = "ymd";
                        break;

                    case SayAs.YearMonth:
                        sInterpretAs = "date";
                        sFormat = "ym";
                        break;

                    case SayAs.MonthYear:
                        sInterpretAs = "date";
                        sFormat = "my";
                        break;

                    case SayAs.MonthDay:
                        sInterpretAs = "date";
                        sFormat = "md";
                        break;

                    case SayAs.DayMonth:
                        sInterpretAs = "date";
                        sFormat = "dm";
                        break;

                    case SayAs.Year:
                        sInterpretAs = "date";
                        sFormat = "y";
                        break;

                    case SayAs.Month:
                        sInterpretAs = "date";
                        sFormat = "m";
                        break;

                    case SayAs.Day:
                        sInterpretAs = "date";
                        sFormat = "d";
                        break;

                    case SayAs.Time:
                        sInterpretAs = "time";
                        break;

                    case SayAs.Time24:
                        sInterpretAs = "time";
                        sFormat = "hms24";
                        break;

                    case SayAs.Time12:
                        sInterpretAs = "time";
                        sFormat = "hms12";
                        break;

                    case SayAs.Telephone:
                        sInterpretAs = "telephone";
                        break;
                }

                sayAsElement._attributes.Add(new AttributeItem("interpret-as", sInterpretAs));
                if (!string.IsNullOrEmpty(sFormat))
                {
                    sayAsElement._attributes.Add(new AttributeItem("format", sFormat));
                }
            }
            else
            {
                AppendText(textToSpeak);
            }
        }
        public void AppendTextWithHint(string textToSpeak, string sayAs)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));
            Helpers.ThrowIfEmptyOrNull(sayAs, nameof(sayAs));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            Element sayAsElement = new(ElementType.SayAs, textToSpeak);
            _elements.Add(sayAsElement);

            sayAsElement._attributes = new Collection<AttributeItem>();
            sayAsElement._attributes.Add(new AttributeItem("interpret-as", sayAs));
        }
        public void AppendTextWithPronunciation(string textToSpeak, string pronunciation)
        {
            Helpers.ThrowIfEmptyOrNull(textToSpeak, nameof(textToSpeak));
            Helpers.ThrowIfEmptyOrNull(pronunciation, nameof(pronunciation));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            // validate the pronunciation
            PhonemeConverter.ValidateUpsIds(pronunciation);

            Element phoneElement = new(ElementType.Phoneme, textToSpeak);
            _elements.Add(phoneElement);

            phoneElement._attributes = new Collection<AttributeItem>();
            phoneElement._attributes.Add(new AttributeItem("ph", pronunciation));
        }
        public void AppendTextWithAlias(string textToSpeak, string substitute)
        {
            Helpers.ThrowIfNull(textToSpeak, nameof(textToSpeak));
            Helpers.ThrowIfNull(substitute, nameof(substitute));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Text);

            Element subElement = new(ElementType.Sub, textToSpeak);
            _elements.Add(subElement);

            subElement._attributes = new Collection<AttributeItem>();
            subElement._attributes.Add(new AttributeItem("alias", substitute));
        }
        public void AppendBreak()
        {
            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Break);

            _elements.Add(new Element(ElementType.Break));
        }
        public void AppendBreak(PromptBreak strength)
        {
            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Break);

            Element breakElement = new(ElementType.Break);
            _elements.Add(breakElement);

            string sBreak = null;

            switch (strength)
            {
                case PromptBreak.None:
                    sBreak = "none";
                    break;

                case PromptBreak.ExtraSmall:
                    sBreak = "x-weak";
                    break;

                case PromptBreak.Small:
                    sBreak = "weak";
                    break;

                case PromptBreak.Medium:
                    sBreak = "medium";
                    break;

                case PromptBreak.Large:
                    sBreak = "strong";
                    break;

                case PromptBreak.ExtraLarge:
                    sBreak = "x-strong";
                    break;

                default:
                    throw new ArgumentNullException(nameof(strength));
            }

            breakElement._attributes = new Collection<AttributeItem>();
            breakElement._attributes.Add(new AttributeItem("strength", sBreak));
        }
        public void AppendBreak(TimeSpan duration)
        {
            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Break);

            if (duration.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }

            Element breakElement = new(ElementType.Break);
            _elements.Add(breakElement);

            breakElement._attributes = new Collection<AttributeItem>();
            breakElement._attributes.Add(new AttributeItem("time", duration.TotalMilliseconds + "ms"));
        }

        // <audio>
        public void AppendAudio(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));
            Uri uri;

            try
            {
                uri = new Uri(path, UriKind.RelativeOrAbsolute);
            }
            catch (UriFormatException e)
            {
                throw new ArgumentException(e.Message, path, e);
            }

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Audio);

            AppendAudio(uri);
        }
        public void AppendAudio(Uri audioFile)
        {
            Helpers.ThrowIfNull(audioFile, nameof(audioFile));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Audio);

            Element audioElement = new(ElementType.Audio);
            _elements.Add(audioElement);

            audioElement._attributes = new Collection<AttributeItem>();
            audioElement._attributes.Add(new AttributeItem("src", audioFile.ToString()));
        }
        public void AppendAudio(Uri audioFile, string alternateText)
        {
            Helpers.ThrowIfNull(audioFile, nameof(audioFile));
            Helpers.ThrowIfNull(alternateText, nameof(alternateText));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Audio);

            Element audioElement = new(ElementType.Audio, alternateText);
            _elements.Add(audioElement);

            audioElement._attributes = new Collection<AttributeItem>();
            audioElement._attributes.Add(new AttributeItem("src", audioFile.ToString()));
        }

        // <mark>
        public void AppendBookmark(string bookmarkName)
        {
            Helpers.ThrowIfEmptyOrNull(bookmarkName, nameof(bookmarkName));

            // check for well formed document
            ValidateElement(_elementStack.Peek(), SsmlElement.Mark);

            Element bookmarkElement = new(ElementType.Bookmark);
            _elements.Add(bookmarkElement);

            bookmarkElement._attributes = new Collection<AttributeItem>();
            bookmarkElement._attributes.Add(new AttributeItem("name", bookmarkName));
        }
        public void AppendPromptBuilder(PromptBuilder promptBuilder)
        {
            Helpers.ThrowIfNull(promptBuilder, nameof(promptBuilder));

            StringReader sr = new(promptBuilder.ToXml());
            XmlTextReader reader = new(sr);
            AppendSsml(reader);
            reader.Close();
            sr.Close();
        }
        public void AppendSsml(string path)
        {
            Helpers.ThrowIfEmptyOrNull(path, nameof(path));

            AppendSsml(new Uri(path, UriKind.Relative));
        }
        public void AppendSsml(Uri ssmlFile)
        {
            Helpers.ThrowIfNull(ssmlFile, nameof(ssmlFile));

            string localFile;
            Uri redirectUri;
            using (Stream stream = s_resourceLoader.LoadFile(ssmlFile, out localFile, out redirectUri))
            {
                try
                {
                    AppendSsml(new XmlTextReader(stream));
                }
                finally
                {
                    s_resourceLoader.UnloadFile(localFile);
                }
            }
        }
        public void AppendSsml(XmlReader ssmlFile)
        {
            Helpers.ThrowIfNull(ssmlFile, nameof(ssmlFile));

            AppendSsmlInternal(ssmlFile);
        }

        // Advanced: Extensibility model to write through to the underlying stream writer.
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void AppendSsmlMarkup(string ssmlMarkup)
        {
            Helpers.ThrowIfEmptyOrNull(ssmlMarkup, nameof(ssmlMarkup));

            _elements.Add(new Element(ElementType.SsmlMarkup, ssmlMarkup));
        }

        public string ToXml()
        {
            using (StringWriter sw = new(CultureInfo.InvariantCulture))
            {
                using (XmlTextWriter writer = new(sw))
                {
                    WriteXml(writer);

                    SsmlState state = _elementStack.Peek()._state;
                    if (state != SsmlState.Header)
                    {
                        string sMsg = SR.Get(SRID.PromptBuilderInvalideState);
                        switch (state)
                        {
                            case SsmlState.Ended:
                                sMsg += SR.Get(SRID.PromptBuilderStateEnded);
                                break;

                            case SsmlState.Sentence:
                                sMsg += SR.Get(SRID.PromptBuilderStateSentence);
                                break;

                            case SsmlState.Paragraph:
                                sMsg += SR.Get(SRID.PromptBuilderStateParagraph);
                                break;

                            case SsmlState.StyleEmphasis:
                            case SsmlState.StyleProsody:
                            case (SsmlState.StyleProsody | SsmlState.StyleEmphasis):
                                sMsg += SR.Get(SRID.PromptBuilderStateStyle);
                                break;

                            case SsmlState.Voice:
                                sMsg += SR.Get(SRID.PromptBuilderStateVoice);
                                break;

                            default:
                                System.Diagnostics.Debug.Assert(false);
                                throw new NotSupportedException();
                        }

                        throw new InvalidOperationException(sMsg);
                    }

                    return sw.ToString();
                }
            }
        }

        #endregion

        #region public Properties
        public bool IsEmpty
        {
            get
            {
                return _elements.Count == 0;
            }
        }
        public            CultureInfo Culture
        {
            get
            {
                return _culture;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _culture = value;
            }
        }

        #endregion

        #region Internal Enums

        internal enum SsmlState
        {
            Header = 1,
            Paragraph = 2,
            Sentence = 4,
            StyleEmphasis = 8,
            StyleProsody = 16,
            Voice = 32,
            Ended = 64
        }

        #endregion

        #region Protected Methods

        #endregion

        #region Private Methods
        private void WriteXml(XmlTextWriter writer)
        {
            writer.WriteStartElement("speak");

            // Add the required elements.
            writer.WriteAttributeString("version", "1.0");
            writer.WriteAttributeString("xmlns", _xmlnsDefault);
            writer.WriteAttributeString("xml", "lang", null, _culture.Name);

            bool noEndElement = false;

            foreach (Element element in _elements)
            {
                noEndElement = noEndElement || element._type == ElementType.StartSentence || element._type == ElementType.StartParagraph || element._type == ElementType.StartStyle || element._type == ElementType.StartVoice;
                switch (element._type)
                {
                    case ElementType.Text:
                        writer.WriteString(element._text);
                        break;

                    case ElementType.SsmlMarkup:
                        writer.WriteRaw(element._text);
                        break;

                    case ElementType.StartVoice:
                    case ElementType.StartParagraph:
                    case ElementType.StartSentence:
                    case ElementType.Audio:
                    case ElementType.Break:
                    case ElementType.Bookmark:
                    case ElementType.Emphasis:
                    case ElementType.Phoneme:
                    case ElementType.Prosody:
                    case ElementType.SayAs:
                    case ElementType.Sub:
                        writer.WriteStartElement(s_promptBuilderElementName[(int)element._type]);

                        // Write the attributes if any
                        if (element._attributes != null)
                        {
                            foreach (AttributeItem attribute in element._attributes)
                            {
                                if (attribute._namespace == null)
                                {
                                    writer.WriteAttributeString(attribute._key, attribute._value);
                                }
                                else
                                {
                                    writer.WriteAttributeString(attribute._namespace, attribute._key, null, attribute._value);
                                }
                            }
                        }

                        // Write the text if any
                        if (element._text != null)
                        {
                            writer.WriteString(element._text);
                        }

                        // Close the element unless it should wait
                        if (!noEndElement)
                        {
                            writer.WriteEndElement();
                        }
                        noEndElement = false;
                        break;

                    // Ignore just set the bool to not close the element
                    case ElementType.StartStyle:
                        break;

                    // Close the current element
                    case ElementType.EndStyle:
                    case ElementType.EndVoice:
                    case ElementType.EndParagraph:
                    case ElementType.EndSentence:
                        writer.WriteEndElement();
                        break;

                    default:
                        throw new NotSupportedException();
                }
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Ensure the this element is properly placed in the SSML markup
        /// </summary>
        private static void ValidateElement(StackElement stackElement, SsmlElement currentElement)
        {
            if ((stackElement._possibleChildren & currentElement) == 0)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.Get(SRID.PromptBuilderInvalidElement), currentElement.ToString(), stackElement._state.ToString()));
            }
        }
        private void AppendSsmlInternal(XmlReader ssmlFile)
        {
            // check for well formed document
            StackElement stackElement = _elementStack.Peek();
            ValidateElement(_elementStack.Peek(), SsmlElement.Voice);

            using (StringWriter sw = new(CultureInfo.InvariantCulture))
            {
                using (XmlTextWriter writer = new(sw))
                {
                    TextWriterEngine engine = new(writer, stackElement._culture);
                    SsmlParser.Parse(ssmlFile, engine, null);
                }
                _elements.Add(new Element(ElementType.SsmlMarkup, sw.ToString()));
            }
        }

        #endregion

        #region Private Fields

        // Stack of elements for the SSML document
        private Stack<StackElement> _elementStack = new();

        // <xml:lang>
        private CultureInfo _culture;

        // list of all the elements for this prompt builder
        private List<Element> _elements = new();

        // Resource loader for the prompt builder
        private static ResourceLoader s_resourceLoader = new();

        private const string _xmlnsDefault = @"http://www.w3.org/2001/10/synthesis";

        #endregion

        #region Private Type

        [Serializable]
        private struct StackElement
        {
            internal SsmlElement _possibleChildren;
            internal SsmlState _state;
            internal CultureInfo _culture;

            internal StackElement(SsmlElement possibleChildren, SsmlState state, CultureInfo culture)
            {
                _possibleChildren = possibleChildren;
                _state = state;
                _culture = culture;
            }
        }

        private enum ElementType
        {
            Prosody,
            Emphasis,
            SayAs,
            Phoneme,
            Sub,
            Break,
            Audio,
            Bookmark,
            StartVoice,
            StartParagraph,
            StartSentence,
            EndSentence,
            EndParagraph,
            StartStyle,
            EndStyle,
            EndVoice,
            Text,
            SsmlMarkup
        }

        private static readonly string[] s_promptBuilderElementName = new string[]
        {
            "prosody",
            "emphasis",
            "say-as",
            "phoneme",
            "sub",
            "break",
            "audio",
            "mark",
            "voice",
            "p",
            "s"
        };

        [Serializable]
        private struct AttributeItem
        {
            internal string _key;
            internal string _value;
            internal string _namespace;

            internal AttributeItem(string key, string value)
            {
                _key = key;
                _value = value;
                _namespace = null;
            }

            internal AttributeItem(string ns, string key, string value)
                : this(key, value)
            {
                _namespace = ns;
            }
        }

        [Serializable]
        private sealed class Element
        {
            internal ElementType _type;
            internal string _text;
            internal Collection<AttributeItem> _attributes;

            internal Element(ElementType type)
            {
                _type = type;
            }

            internal Element(ElementType type, string text)
                : this(type)
            {
                _text = text;
            }
        }

        #endregion
    }
}
