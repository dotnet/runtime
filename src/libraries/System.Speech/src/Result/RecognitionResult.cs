// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Speech.AudioFormat;
using System.Speech.Internal;
using System.Speech.Internal.SapiInterop;
using System.Text;

#pragma warning disable 56507 // check for null or empty strings

namespace System.Speech.Recognition
{
    [DebuggerDisplay("{DebuggerDisplayString())}")]
    [Serializable]
    public sealed class RecognitionResult : RecognizedPhrase, ISerializable
    {
        #region Constructors
        internal RecognitionResult(IRecognizerInternal recognizer, ISpRecoResult recoResult, byte[] sapiResultBlob, int maxAlternates)
        {
            Initialize(recognizer, recoResult, sapiResultBlob, maxAlternates);
        }

        internal RecognitionResult()
        {
        }

#pragma warning disable SYSLIB0050 // Legacy formatter infrastructure is obsolete
        private RecognitionResult(SerializationInfo info, StreamingContext context)
        {
            // Get the set of serializable members for our class and base classes
            Type thisType = this.GetType();
            MemberInfo[] mis = FormatterServices.GetSerializableMembers(
               thisType, context);

            // Do not copy all the field for App Domain transition
            bool appDomainTransition = context.State == StreamingContextStates.CrossAppDomain;

            // Deserialize the base class's fields from the info object
            foreach (MemberInfo mi in mis)
            {
                // To ease coding, treat the member as a FieldInfo object
                FieldInfo fi = (FieldInfo)mi;

                // Set the field to the deserialized value
                if (!appDomainTransition || (mi.Name != "_recognizer" && mi.Name != "_grammar" && mi.Name != "_ruleList" && mi.Name != "_audio" && mi.Name != "_audio"))
                {
                    fi.SetValue(this, info.GetValue(fi.Name, fi.FieldType));
                }
            }
        }
#pragma warning restore SYSLIB0050

        #endregion

        #region Public Methods
        public RecognizedAudio GetAudioForWordRange(RecognizedWordUnit firstWord, RecognizedWordUnit lastWord)
        {
            Helpers.ThrowIfNull(firstWord, nameof(firstWord));
            Helpers.ThrowIfNull(lastWord, nameof(lastWord));

            return Audio.GetRange(firstWord._audioPosition, lastWord._audioPosition + lastWord._audioDuration - firstWord._audioPosition);
        }

#pragma warning disable SYSLIB0050 // Legacy formatter infrastructure is obsolete
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            Helpers.ThrowIfNull(info, nameof(info));

            bool appDomainTransition = context.State == StreamingContextStates.CrossAppDomain;

            if (!appDomainTransition)
            {
                // build all the properties
                foreach (RecognizedPhrase phrase in Alternates)
                {
                    try
                    {
                        // Get the sml Content and toy with this variable to fool the compiler in not doing the calucation at all
                        string sml = phrase.SmlContent;
                        RecognizedAudio audio = Audio;
                        if (phrase.Text == null || phrase.Homophones == null || phrase.Semantics == null || (sml == null && sml != null) || (audio == null && audio != null))
                        {
                            throw new SerializationException();
                        }
                    }
#pragma warning disable 56502 // Remove the empty catch statements warnings
                    catch (NotSupportedException)
                    {
                    }
#pragma warning restore 56502
                }
            }

            // Get the set of serializable members for our class and base classes
            Type thisType = this.GetType();
            MemberInfo[] mis = FormatterServices.GetSerializableMembers(thisType, context);

            // Serialize the base class's fields to the info object
            foreach (MemberInfo mi in mis)
            {
                if (!appDomainTransition || (mi.Name != "_recognizer" && mi.Name != "_grammar" && mi.Name != "_ruleList" && mi.Name != "_audio" && mi.Name != "_audio"))
                {
                    info.AddValue(mi.Name, ((FieldInfo)mi).GetValue(this));
                }
            }
        }
#pragma warning restore SYSLIB0050

        internal bool SetTextFeedback(string text, bool isSuccessfulAction)
        {
            if (_sapiRecoResult == null)
            {
                throw new NotSupportedException(SR.Get(SRID.NotSupportedWithThisVersionOfSAPI));
            }
            try
            {
                _sapiRecoResult.SetTextFeedback(text, isSuccessfulAction);
            }
            catch (COMException ex)
            {
                // If we failed to set the text feedback, it is likely an inproc Recognition result.
                if (ex.ErrorCode == (int)SAPIErrorCodes.SPERR_NOT_SUPPORTED_FOR_INPROC_RECOGNIZER)
                {
                    throw new NotSupportedException(SR.Get(SRID.SapiErrorNotSupportedForInprocRecognizer));
                }

                // Otherwise, this could also fail for various reasons, e.g. we have changed the recognizer under
                // the hood. In any case, we don't want this function to fail.
                return false;
            }

            return true;
        }
        #endregion

        #region Public Properties

        // Recognized Audio:
        public RecognizedAudio Audio
        {
            get
            {
                if (_audio == null && _header.ulRetainedOffset > 0)
                {
                    SpeechAudioFormatInfo audioFormat;
                    int audioLength = _sapiAudioBlob.Length;

                    GCHandle gc = GCHandle.Alloc(_sapiAudioBlob, GCHandleType.Pinned);
                    try
                    {
                        IntPtr audioBuffer = gc.AddrOfPinnedObject();

                        SPWAVEFORMATEX audioHeader = Marshal.PtrToStructure<SPWAVEFORMATEX>(audioBuffer);

                        IntPtr rawDataBuffer = new((long)audioBuffer + audioHeader.cbUsed);
                        byte[] rawAudioData = new byte[audioLength - audioHeader.cbUsed];
                        Marshal.Copy(rawDataBuffer, rawAudioData, 0, audioLength - (int)audioHeader.cbUsed);

                        byte[] formatSpecificData = new byte[audioHeader.cbSize];
                        if (audioHeader.cbSize > 0)
                        {
                            IntPtr codecDataBuffer = new((long)audioBuffer + 38); // 38 is sizeof(SPWAVEFORMATEX) without padding.
                            Marshal.Copy(codecDataBuffer, formatSpecificData, 0, audioHeader.cbSize);
                        }
                        audioFormat = new SpeechAudioFormatInfo((EncodingFormat)audioHeader.wFormatTag,
                                                        (int)audioHeader.nSamplesPerSec, (short)audioHeader.wBitsPerSample, (short)audioHeader.nChannels, (int)audioHeader.nAvgBytesPerSec,
                                                        (short)audioHeader.nBlockAlign,
                                                        formatSpecificData);
                        DateTime startTime;
                        if (_header.times.dwTickCount == 0)
                        {
                            startTime = _startTime - AudioDuration;
                        }
                        else
                        {
                            startTime = DateTime.FromFileTime((long)((ulong)_header.times.ftStreamTime.dwHighDateTime << 32) + _header.times.ftStreamTime.dwLowDateTime);
                        }
                        _audio = new RecognizedAudio(rawAudioData, audioFormat, startTime, AudioPosition, AudioDuration);
                    }
                    finally
                    {
                        gc.Free();
                    }
                }

                return _audio; // Will be null if there's no audio.
            }
        }

        // Alternates. This returns a list of Alternate recognitions.
        // We use the same class here for alternates as the main RecognitionResult class. This simplifies the API surface. Calling Alternates on a Result that's already an Alternate will throw a NotSupportedException.
        public ReadOnlyCollection<RecognizedPhrase> Alternates
        {
            get
            {
                return new ReadOnlyCollection<RecognizedPhrase>(GetAlternates());
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// This method converts a given pronunciation from SAPI phonetic alphabet to IPA for a given language
        /// </summary>
        /// <returns>New pronunciation in IPA alphabet</returns>
        internal string ConvertPronunciation(string pronunciation, int langId)
        {
            if (_alphabetConverter == null)
            {
                _alphabetConverter = new AlphabetConverter(langId);
            }
            else
            {
                _alphabetConverter.SetLanguageId(langId);
            }

            char[] ipa = _alphabetConverter.SapiToIpa(pronunciation.ToCharArray());

            if (ipa != null)
            {
                pronunciation = new string(ipa);
            }
            else
            {
                Trace.TraceError("Cannot convert the pronunciation to IPA alphabet.");
            }
            return pronunciation;
        }

        #endregion

        #region Internal Properties

        internal IRecognizerInternal Recognizer
        {
            get
            {
                // If this recognition result comes from a deserialize, then throw
                if (_recognizer == null)
                {
                    throw new NotSupportedException(SR.Get(SRID.CantGetPropertyFromSerializedInfo, "Recognizer"));
                }
                return _recognizer;
            }
        }

        internal TimeSpan AudioPosition
        {
            get
            {
                if (_audioPosition == null)
                {
                    _audioPosition = new TimeSpan((long)_header.times.ullStart);
                }
                return (TimeSpan)_audioPosition;
            }
        }

        internal TimeSpan AudioDuration
        {
            get
            {
                if (_audioDuration == null)
                {
                    _audioDuration = new TimeSpan((long)_header.times.ullLength);
                }
                return (TimeSpan)_audioDuration;
            }
        }

        #endregion

        #region Private Methods

        private void Initialize(IRecognizerInternal recognizer, ISpRecoResult recoResult, byte[] sapiResultBlob, int maxAlternates)
        {
            // record parameters
            _recognizer = recognizer;
            _maxAlternates = maxAlternates;

            try
            {
                _sapiRecoResult = recoResult as ISpRecoResult2;
            }
            catch (COMException)
            {
                _sapiRecoResult = null;
            }
            GCHandle gc = GCHandle.Alloc(sapiResultBlob, GCHandleType.Pinned);
            try
            {
                IntPtr buffer = gc.AddrOfPinnedObject();

                int headerSize = Marshal.ReadInt32(buffer, 4); // Read header size directly from buffer - 4 is the offset of cbHeaderSize.

                if (headerSize == Marshal.SizeOf<SPRESULTHEADER_Sapi51>()) // SAPI 5.1 size
                {
                    SPRESULTHEADER_Sapi51 legacyHeader = Marshal.PtrToStructure<SPRESULTHEADER_Sapi51>(buffer);
                    _header = new SPRESULTHEADER(legacyHeader);
                    _isSapi53Header = false;
                }
                else
                {
                    _header = Marshal.PtrToStructure<SPRESULTHEADER>(buffer);
                    _isSapi53Header = true;
                }

                // Validate the header fields
                _header.Validate();

                // initialize the parent to be this result - this is needed for the homophones
                IntPtr phraseBuffer = new((long)buffer + (int)_header.ulPhraseOffset);

                SPSERIALIZEDPHRASE serializedPhrase = RecognizedPhrase.GetPhraseHeader(phraseBuffer, _header.ulPhraseDataSize, _isSapi53Header);

                // Get the alphabet of the main phrase, which should be the same as the current alphabet selected by us (applications).
                bool hasIPAPronunciation = (_header.fAlphabet & (uint)SPRESULTALPHABET.SPRA_APP_UPS) != 0;

                InitializeFromSerializedBuffer(this, serializedPhrase, phraseBuffer, (int)_header.ulPhraseDataSize, _isSapi53Header, hasIPAPronunciation);

                if (recoResult != null)
                {
                    ExtractDictationAlternates(recoResult, maxAlternates);
                    // Since we took ownership of this unmanaged object we can discard information that don't need.
                    recoResult.Discard(SapiConstants.SPDF_ALL);
                }
            }
            finally
            {
                gc.Free();
            }

            // save the sapi blobs splitting it in the relevant bits

            // audio
            _sapiAudioBlob = new byte[(int)_header.ulRetainedDataSize];
            Array.Copy(sapiResultBlob, (int)_header.ulRetainedOffset, _sapiAudioBlob, 0, (int)_header.ulRetainedDataSize);

            // alternates
            _sapiAlternatesBlob = new byte[(int)_header.ulPhraseAltDataSize];
            Array.Copy(sapiResultBlob, (int)_header.ulPhraseAltOffset, _sapiAlternatesBlob, 0, (int)_header.ulPhraseAltDataSize);
        }

        private Collection<RecognizedPhrase> ExtractAlternates(int numberOfAlternates, bool isSapi53Header)
        {
            Collection<RecognizedPhrase> alternates = new();

            if (numberOfAlternates > 0)
            {
                GCHandle gc = GCHandle.Alloc(_sapiAlternatesBlob, GCHandleType.Pinned);
                try
                {
                    IntPtr buffer = gc.AddrOfPinnedObject();

                    int sizeOfSpSerializedPhraseAlt = Marshal.SizeOf<SPSERIALIZEDPHRASEALT>();
                    int offset = 0;
                    for (int i = 0; i < numberOfAlternates; i++)
                    {
                        IntPtr altBuffer = new((long)buffer + offset);
                        SPSERIALIZEDPHRASEALT alt = Marshal.PtrToStructure<SPSERIALIZEDPHRASEALT>(altBuffer);

                        offset += sizeOfSpSerializedPhraseAlt; // advance over SPSERIALIZEDPHRASEALT
                        if (isSapi53Header)
                        {
                            offset += (int)((alt.cbAltExtra + 7) & ~7); // advance over extra data with alignment padding
                        }
                        else
                        {
                            offset += (int)alt.cbAltExtra; // no alignment padding
                        }

                        // we cannot use a constructor parameter because RecognitionResult also derives from RecognizedPhrase
                        IntPtr phraseBuffer = new((long)buffer + offset);
                        SPSERIALIZEDPHRASE serializedPhrase = RecognizedPhrase.GetPhraseHeader(phraseBuffer, _header.ulPhraseAltDataSize - (uint)offset, _isSapi53Header);
                        int serializedPhraseSize = (int)serializedPhrase.ulSerializedSize;

                        RecognizedPhrase phrase = new();

                        // Get the alphabet of the raw phrase alternate, which should be the same as the engine
                        bool hasIPAPronunciation = (_header.fAlphabet & (uint)SPRESULTALPHABET.SPRA_ENGINE_UPS) != 0;

                        phrase.InitializeFromSerializedBuffer(this, serializedPhrase, phraseBuffer, serializedPhraseSize, isSapi53Header, hasIPAPronunciation);
                        if (isSapi53Header)
                        {
                            offset += ((serializedPhraseSize + 7) & ~7); // advance over phrase with alignment padding
                        }
                        else
                        {
                            offset += serializedPhraseSize; // advance over phrase
                        }

                        alternates.Add(phrase);
                    }
                }
                finally
                {
                    gc.Free();
                }
            }

            return alternates;
        }

        private void ExtractDictationAlternates(ISpRecoResult recoResult, int maxAlternates)
        {
            // Get the alternates for dictation
            // alternates for dictation are not part of the recognition results and must be pulled out
            // from the recognition result bits.

            if (recoResult != null) // recoResult is null if we are in the case of our unit test.
            {
                if (Grammar is DictationGrammar)
                {
                    _alternates = new Collection<RecognizedPhrase>();
                    IntPtr[] sapiAlternates = new IntPtr[maxAlternates];
                    try
                    {
                        recoResult.GetAlternates(0, -1, maxAlternates, sapiAlternates, out maxAlternates);
                    }
                    catch (COMException)
                    {
                        // In some cases such as when the dictation grammar has been unloaded, the engine may not be able
                        // to provide the alternates. We set the alternate list to empty.
                        maxAlternates = 0;
                    }

                    //InnerList.Capacity = (int)numSapiAlternates;
                    for (uint i = 0; i < maxAlternates; i++)
                    {
                        ISpPhraseAlt phraseAlt = (ISpPhraseAlt)Marshal.GetObjectForIUnknown(sapiAlternates[i]);
                        try
                        {
                            IntPtr coMemSerializedPhrase;
                            phraseAlt.GetSerializedPhrase(out coMemSerializedPhrase);
                            try
                            {
                                // Build a recognition phrase result
                                RecognizedPhrase phrase = new();

                                // we cannot use a constructor parameter because RecognitionResult also derives from RecognizedPhrase
                                SPSERIALIZEDPHRASE serializedPhrase = RecognizedPhrase.GetPhraseHeader(coMemSerializedPhrase, uint.MaxValue, _isSapi53Header);

                                //
                                // If we are getting the alternates from SAPI, the alphabet should have already been converted
                                // to the alphabet we (applications) want.
                                //
                                bool hasIPAPronunciation = (_header.fAlphabet & (uint)SPRESULTALPHABET.SPRA_APP_UPS) != 0;

                                phrase.InitializeFromSerializedBuffer(this, serializedPhrase, coMemSerializedPhrase, (int)serializedPhrase.ulSerializedSize, _isSapi53Header, hasIPAPronunciation);
                                _alternates.Add(phrase);
                            }
                            finally
                            {
                                Marshal.FreeCoTaskMem(coMemSerializedPhrase);
                            }
                        }
                        finally
                        {
                            Marshal.Release(sapiAlternates[i]);
                        }
                    }
                }
            }
        }

        private Collection<RecognizedPhrase> GetAlternates()
        {
            if (_alternates == null)
            {
                // extract alternates even if ulNumPhraseAlts is 0 so that the list gets initialized to empty
                _alternates = ExtractAlternates((int)_header.ulNumPhraseAlts, _isSapi53Header);

                // If no alternated then create one from the top result
                if (_alternates.Count == 0 && _maxAlternates > 0)
                {
                    RecognizedPhrase alternate = new();
                    GCHandle gc = GCHandle.Alloc(_phraseBuffer, GCHandleType.Pinned);
                    try
                    {
                        alternate.InitializeFromSerializedBuffer(this, _serializedPhrase, gc.AddrOfPinnedObject(), _phraseBuffer.Length, _isSapi53Header, _hasIPAPronunciation);
                    }
                    finally
                    {
                        gc.Free();
                    }
                    _alternates.Add(alternate);
                }
            }
            return _alternates;
        }

        internal string DebuggerDisplayString()
        {
            StringBuilder sb = new("Recognized text: '");
            sb.Append(Text);
            sb.Append('\'');
            if (Semantics.Value != null)
            {
                sb.Append(" - Semantic Value  = ");
                sb.Append(Semantics.Value.ToString());
            }

            if (Semantics.Count > 0)
            {
                sb.Append(" - Semantic children count = ");
                sb.Append(Semantics.Count.ToString(CultureInfo.InvariantCulture));
            }

            if (Alternates.Count > 1)
            {
                sb.Append(" - Alternate word count = ");
                sb.Append(Alternates.Count.ToString(CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }

        #endregion

        #region Private Fields

        [field: NonSerialized]
        private IRecognizerInternal _recognizer;

        [field: NonSerialized]
        private int _maxAlternates;

        [field: NonSerialized]
        private AlphabetConverter _alphabetConverter;

        // sapi blobs
        private byte[] _sapiAudioBlob;
        private byte[] _sapiAlternatesBlob;

        private Collection<RecognizedPhrase> _alternates;

        private SPRESULTHEADER _header;

        private RecognizedAudio _audio;
        private DateTime _startTime = DateTime.Now;

        [field: NonSerialized]
        private ISpRecoResult2 _sapiRecoResult;
        // Keep as members because MSS uses these fields:
        private TimeSpan? _audioPosition;
        private TimeSpan? _audioDuration;

        #endregion
    }
}
