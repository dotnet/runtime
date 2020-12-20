// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.Internal.ObjectTokens;
using System.Speech.Recognition;

namespace System.Speech.Internal.SapiInterop
{
    #region Enum

    [Flags]
    internal enum SPEAKFLAGS : int
    {
        SPF_DEFAULT = 0x0000,   // Synchronous, no purge, xml auto detect
        SPF_ASYNC = 0x0001,   // Asynchronous call
        SPF_PURGEBEFORESPEAK = 0x0002,   // Purge current data prior to speaking this
        SPF_IS_FILENAME = 0x0004,   // The string passed to Speak() is a file name
        SPF_IS_XML = 0x0008,   // The input text will be parsed for XML markup
        SPF_IS_NOT_XML = 0x0010,   // The input text will not be parsed for XML markup
        SPF_PERSIST_XML = 0x0020,   // Persists XML global state changes
        SPF_NLP_SPEAK_PUNC = 0x0040,   // The normalization processor should speak the punctuation
        SPF_PARSE_SAPI = 0x0080,   // Force XML parsing as MS SAPI
        SPF_PARSE_SSML = 0x0100    // Force XML parsing as W3C SSML
    }

    internal enum SpeechRunState
    {
        SPRS_DONE,
        SPRS_IS_SPEAKING
    }

    internal enum SPRECOSTATE
    {
        SPRST_INACTIVE = 0x00000000,
        SPRST_ACTIVE = 0x00000001,
        SPRST_ACTIVE_ALWAYS = 0x00000002,
        SPRST_INACTIVE_WITH_PURGE = 0x00000003,
        SPRST_NUM_STATES = 0x00000004
    }

    internal enum SPVPRIORITY
    {
        SPVPRI_NORMAL = 0x00000000,
        SPVPRI_ALERT = 0x00000001,
        SPVPRI_OVER = 0x00000002
    }

    internal enum SPLOADOPTIONS
    {
        SPLO_STATIC = 0x00000000,
        SPLO_DYNAMIC = 0x00000001
    }

    internal enum SPRULESTATE
    {
        SPRS_INACTIVE = 0x00000000,
        SPRS_ACTIVE = 0x00000001,
        SPRS_ACTIVE_WITH_AUTO_PAUSE = 0x00000003,
        SPRS_ACTIVE_USER_DELIMITED = 0x00000004
    }

    internal enum SPGRAMMAROPTIONS
    {
        SPGO_SAPI = 0x00000001,
        SPGO_SRGS = 0x00000002,
        SPGO_UPS = 0x00000004,
        SPGO_SRGS_MSS_SCRIPT = 0x0008,
        SPGO_FILE = 0x00000010,
        SPGO_HTTP = 0x00000020,
        SPGO_RES = 0x00000040,
        SPGO_OBJECT = 0x00000080,
        SPGO_SRGS_W3C_SCRIPT = 0x100,
        SPGO_SRGS_STG_SCRIPT = 0x200,

        SPGO_SRGS_SCRIPT = SPGO_SRGS | SPGO_SRGS_MSS_SCRIPT | SPGO_SRGS_W3C_SCRIPT | SPGO_SRGS_STG_SCRIPT,
        SPGO_DEFAULT = SPGO_SAPI | SPGO_SRGS | SPGO_FILE | SPGO_HTTP | SPGO_RES | SPGO_OBJECT,
        SPGO_ALL = SPGO_SAPI | SPGO_SRGS | SPGO_SRGS_SCRIPT | SPGO_FILE | SPGO_HTTP | SPGO_RES | SPGO_OBJECT
    }

    internal enum SPSTREAMFORMATTYPE
    {
        SPWF_INPUT = 0x00000000,
        SPWF_SRENGINE = 0x00000001
    }

    [Flags]
    internal enum SpeechEmulationCompareFlags
    {
        SECFIgnoreCase = 0x00000001,
        SECFIgnoreKanaType = 0x00010000,
        SECFIgnoreWidth = 0x00020000,
        SECFNoSpecialChars = 0x20000000,
        SECFEmulateResult = 0x40000000,
        SECFDefault = SECFIgnoreCase | SECFIgnoreKanaType | SECFIgnoreWidth
    }

    [Flags]
    internal enum SPADAPTATIONSETTINGS
    {
        SPADS_Default = 0x0000,
        SPADS_CurrentRecognizer = 0x0001,
        SPADS_RecoProfile = 0x0002,
        SPADS_Immediate = 0x0004,
        SPADS_Reset = 0x0008
    }

    internal enum SPADAPTATIONRELEVANCE
    {
        SPAR_Unknown = 0,
        SPAR_Low = 1,
        SPAR_Medium = 2,
        SPAR_High = 3
    }

    [Flags]
    internal enum SPRECOEVENTFLAGS
    {
        SPREF_AutoPause = 0x0001,
        SPREF_Emulated = 0x0002,
        SPREF_SMLTimeout = 0x0004,
        SPREF_ExtendableParse = 0x0008,
        SPREF_ReSent = 0x0010,
        SPREF_Hypothesis = 0x0020,
        SPREF_FalseRecognition = 0x0040
    }

    [Flags]
    internal enum SPBOOKMARKOPTIONS
    {
        SPBO_NONE = 0x0000,
        SPBO_PAUSE = 0x0001,
        SPBO_AHEAD = 0x0002,
        SPBO_TIME_UNITS = 0x0004
    }

    internal enum SPCATEGORYTYPE
    {
        SPCT_COMMAND = 0x00000000,   // Command category
        SPCT_DICTATION = 0x00000001,   // Dictation category
        SPCT_SUB_COMMAND = 0x00000002,   // Command sub-category
        SPCT_SUB_DICTATION = 0x00000003    // Dictation sub-category
    }

    internal enum SPCATEGORYSTATE
    {
        SPCAS_ENABLED = 0x00000000,
        SPCAS_DISABLED = 0x00000001
    }

    internal enum SapiConfidenceLevels
    {
        SP_LOW_CONFIDENCE = -1,
        SP_NORMAL_CONFIDENCE = 0,
        SP_HIGH_CONFIDENCE = 1
    }

    internal enum SPAUDIOOPTIONS
    {
        SPAO_NONE = 0,
        SPAO_RETAIN_AUDIO = 1
    }

    [Flags]
    internal enum SPENDSRSTREAMFLAGS
    {
        SPESF_NONE = 0x00,
        SPESF_STREAM_RELEASED = 0x01,
        SPESF_EMULATED = 0x02
    };

    [Flags]
    internal enum SPCOMMITFLAGS
    {
        SPCF_NONE = 0x00,
        SPCF_ADD_TO_USER_LEXICON = 0x01,
        SPCF_DEFINITE_CORRECTION = 0x02
    };

    [Flags]
    internal enum SPDISPLAYATTRIBUTES
    {
        SPAF_ZERO_TRAILING_SPACE = 0x00,
        SPAF_ONE_TRAILING_SPACE = 0x02,
        SPAF_TWO_TRAILING_SPACES = 0x04,
        SPAF_CONSUME_LEADING_SPACES = 0x08,
        SPAF_USER_SPECIFIED = 0x80,
    }

    internal enum SPAUDIOSTATE
    {
        SPAS_CLOSED = 0,
        SPAS_STOP = 1,
        SPAS_PAUSE = 2,
        SPAS_RUN = 3
    }

    internal enum SPXMLRESULTOPTIONS
    {
        SPXRO_SML = 0x00000000,
        SPXRO_Alternates_SML = 0x00000001
    }

    internal enum SPCONTEXTSTATE
    {
        SPCS_DISABLED = 0,
        SPCS_ENABLED = 1
    }

    internal enum SPINTERFERENCE
    {
        SPINTERFERENCE_NONE = 0,
        SPINTERFERENCE_NOISE = 1,
        SPINTERFERENCE_NOSIGNAL = 2,
        SPINTERFERENCE_TOOLOUD = 3,
        SPINTERFERENCE_TOOQUIET = 4,
        SPINTERFERENCE_TOOFAST = 5,
        SPINTERFERENCE_TOOSLOW = 6
    }

    internal enum SPGRAMMARSTATE
    {
        SPGS_DISABLED = 0,
        SPGS_ENABLED = 1,
        SPGS_EXCLUSIVE = 3
    }

    [Flags]
    internal enum SPRESULTALPHABET
    {
        SPRA_NONE = 0,
        SPRA_APP_UPS = 0x0001,
        SPRA_ENGINE_UPS = 0x0002
    }

    #endregion



    #region Structure

#pragma warning disable 649

    /// Note:   This structure doesn't exist in SAPI.idl but is related to SPPHRASEALT.
    ///         We use it to map memory containted in the serialized result (instead of reading sequentially)
    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEALT
    {
        internal UInt32 ulStartElementInParent;
        internal UInt32 cElementsInParent;
        internal UInt32 cElementsInAlternate;
        internal UInt32 cbAltExtra;
    }

#pragma warning restore 649

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct FILETIME
    {
        internal UInt32 dwLowDateTime;
        internal UInt32 dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct SPRECORESULTTIMES
    {
        internal FILETIME ftStreamTime;
        internal UInt64 ullLength;
        internal UInt32 dwTickCount;
        internal UInt64 ullStart;
    }

    internal struct SPTEXTSELECTIONINFO
    {
        internal UInt32 ulStartActiveOffset;
        internal UInt32 cchActiveChars;
        internal UInt32 ulStartSelection;
        internal UInt32 cchSelection;

        internal SPTEXTSELECTIONINFO(UInt32 ulStartActiveOffset, UInt32 cchActiveChars,
            UInt32 ulStartSelection, UInt32 cchSelection)
        {
            this.ulStartActiveOffset = ulStartActiveOffset;
            this.cchActiveChars = cchActiveChars;
            this.ulStartSelection = ulStartSelection;
            this.cchSelection = cchSelection;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPAUDIOSTATUS
    {
        internal Int32 cbFreeBuffSpace;
        internal UInt32 cbNonBlockingIO;
        internal SPAUDIOSTATE State;
        internal UInt64 CurSeekPos;
        internal UInt64 CurDevicePos;
        internal UInt32 dwAudioLevel;
        internal UInt32 dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPRECOGNIZERSTATUS
    {
        internal SPAUDIOSTATUS AudioStatus;
        internal UInt64 ullRecognitionStreamPos;
        internal UInt32 ulStreamNumber;
        internal UInt32 ulNumActive;
        internal Guid clsidEngine;
        internal UInt32 cLangIDs;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] // SP_MAX_LANGIDS
        internal Int16[] aLangID;
        internal UInt64 ullRecognitionStreamTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPRECOCONTEXTSTATUS
    {
        internal SPINTERFERENCE eInterference;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        internal Int16[] szRequestTypeOfUI; // Can't really be marsalled as a string directly
        internal UInt32 dwReserved1;
        internal UInt32 dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSEMANTICERRORINFO
    {
        internal UInt32 ulLineNumber;
        internal UInt32 pszScriptLineOffset;
        internal UInt32 pszSourceOffset;
        internal UInt32 pszDescriptionOffset;
        internal Int32 hrResultCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPSERIALIZEDRESULT
    {
        internal UInt32 ulSerializedSize;       // Count in bytes (including this ULONG) of the entire phrase
    }

#pragma warning disable 649

    // Serialized result header from versions of SAPI prior to 5.3.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPRESULTHEADER_Sapi51
    {
        internal UInt32 ulSerializedSize;     // This MUST be the first field to line up with SPSERIALIZEDRESULT
        internal UInt32 cbHeaderSize;         // This must be sizeof(SPRESULTHEADER), or sizeof(SPRESULTHEADER_Sapi51) on SAPI 5.1.
        internal Guid clsidEngine;            // CLSID clsidEngine;
        internal Guid clsidAlternates;        // CLSID clsidAlternates;
        internal UInt32 ulStreamNum;
        internal UInt64 ullStreamPosStart;
        internal UInt64 ullStreamPosEnd;
        internal UInt32 ulPhraseDataSize;     // byte size of all the phrase structure
        internal UInt32 ulPhraseOffset;       // offset to phrase
        internal UInt32 ulPhraseAltDataSize;  // byte size of all the phrase alt structures combined
        internal UInt32 ulPhraseAltOffset;    // offset to phrase
        internal UInt32 ulNumPhraseAlts;      // Number of alts in array
        internal UInt32 ulRetainedDataSize;   // byte size of audio data
        internal UInt32 ulRetainedOffset;     // offset to audio data in this phrase blob
        internal UInt32 ulDriverDataSize;     // byte size of driver specific data
        internal UInt32 ulDriverDataOffset;   // offset to driver specific data
        internal float fTimePerByte;          // Conversion factor from engine stream size to time.
        internal float fInputScaleFactor;     // Conversion factor from engine stream size to input stream size.
        internal SPRECORESULTTIMES times;     // time info of result
    }

    // The SAPI 5.3 result header added extra fields.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPRESULTHEADER
    {
        internal SPRESULTHEADER()
        {
        }

        internal SPRESULTHEADER(SPRESULTHEADER_Sapi51 source)
        {
            ulSerializedSize = source.ulSerializedSize;
            cbHeaderSize = source.cbHeaderSize;
            clsidEngine = source.clsidEngine;
            clsidAlternates = source.clsidAlternates;
            ulStreamNum = source.ulStreamNum;
            ullStreamPosStart = source.ullStreamPosStart;
            ullStreamPosEnd = source.ullStreamPosEnd;
            ulPhraseDataSize = source.ulPhraseDataSize;
            ulPhraseOffset = source.ulPhraseOffset;
            ulPhraseAltDataSize = source.ulPhraseAltDataSize;
            ulPhraseAltOffset = source.ulPhraseAltOffset;
            ulNumPhraseAlts = source.ulNumPhraseAlts;
            ulRetainedDataSize = source.ulRetainedDataSize;
            ulRetainedOffset = source.ulRetainedOffset;
            ulDriverDataSize = source.ulDriverDataSize;
            ulDriverDataOffset = source.ulDriverDataOffset;
            fTimePerByte = source.fTimePerByte;
            fInputScaleFactor = source.fInputScaleFactor;
            times = source.times;
        }

        internal void Validate()
        {
            ValidateOffsetAndLength(0, cbHeaderSize);
            ValidateOffsetAndLength(ulPhraseOffset, ulPhraseDataSize);
            ValidateOffsetAndLength(ulPhraseAltOffset, ulPhraseAltDataSize);
            ValidateOffsetAndLength(ulRetainedOffset, ulRetainedDataSize);
            ValidateOffsetAndLength(ulDriverDataOffset, ulDriverDataSize);
        }

        // Duplicate all the fields of SPRESULTHEADER_Sapi51 - Marshal.PtrToStructure seems to need these to be defined again.
        internal UInt32 ulSerializedSize;
        internal UInt32 cbHeaderSize;
        internal Guid clsidEngine;
        internal Guid clsidAlternates;
        internal UInt32 ulStreamNum;
        internal UInt64 ullStreamPosStart;
        internal UInt64 ullStreamPosEnd;
        internal UInt32 ulPhraseDataSize;
        internal UInt32 ulPhraseOffset;
        internal UInt32 ulPhraseAltDataSize;
        internal UInt32 ulPhraseAltOffset;
        internal UInt32 ulNumPhraseAlts;
        internal UInt32 ulRetainedDataSize;
        internal UInt32 ulRetainedOffset;
        internal UInt32 ulDriverDataSize;
        internal UInt32 ulDriverDataOffset;
        internal float fTimePerByte;
        internal float fInputScaleFactor;
        internal SPRECORESULTTIMES times;

        private void ValidateOffsetAndLength(UInt32 offset, UInt32 length)
        {
            if (offset + length > ulSerializedSize)
            {
                throw new FormatException(SR.Get(SRID.ResultInvalidFormat));
            }
        }
        internal UInt32 fAlphabet;
        // Not present in SAPI 5.1 results; on SAPI 5.without IPA this is set to zero, with IPA it will indicate
        // the alphabet of pronunciations the result

        // TODO-hieung: Append SPRESULTHEADER to reflect new data on the managed side.
    }


    // Serialized phrase header from versions of SAPI prior to 5.2.
    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASE_Sapi51
    {
        internal UInt32 ulSerializedSize;          // This MUST be the first field to line up with SPSERIALIZEDPHRASE
        internal UInt32 cbSize;      // size of just this structure within the serialized block header")
        internal UInt16 LangID;
        internal UInt16 wHomophoneGroupId;
        internal UInt64 ullGrammarID;
        internal UInt64 ftStartTime;
        internal UInt64 ullAudioStreamPosition;
        internal UInt32 ulAudioSizeBytes;
        internal UInt32 ulRetainedSizeBytes;
        internal UInt32 ulAudioSizeTime;
        internal SPSERIALIZEDPHRASERULE Rule;
        internal UInt32 PropertiesOffset;
        internal UInt32 ElementsOffset;
        internal UInt32 cReplacements;
        internal UInt32 ReplacementsOffset;
        internal Guid SREngineID;
        internal UInt32 ulSREnginePrivateDataSize;
        internal UInt32 SREnginePrivateDataOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPPHRASE
    {
        internal UInt32 cbSize;     // Size of structure
        internal UInt16 LangID;
        internal UInt16 wReserved;
        internal UInt64 ullGrammarID;
        internal UInt64 ftStartTime;
        internal UInt64 ullAudioStreamPosition;
        internal UInt32 ulAudioSizeBytes;
        internal UInt32 ulRetainedSizeBytes;
        internal UInt32 ulAudioSizeTime;  // In 100ns units
        internal SPPHRASERULE Rule;
        internal IntPtr pProperties;
        internal IntPtr pElements;
        internal UInt32 cReplacements;
        internal IntPtr pReplacements;
        internal Guid SREngineID;
        internal UInt32 ulSREnginePrivateDataSize;
        internal IntPtr pSREnginePrivateData;

        /// <summary>
        /// Helper function used to create a new phrase object from a
        /// test string. Each word in the string is converted to a phrase element.
        /// This is useful to create a phrase to pass to the EmulateRecognition method.
        /// </summary>
        /// <param name="phrase"></param>
        /// <param name="culture"></param>
        /// <param name="memHandles"></param>
        /// <param name="coMem"></param>
        /// <returns></returns>
        internal static ISpPhrase CreatePhraseFromText(string phrase, CultureInfo culture, out GCHandle[] memHandles, out IntPtr coMem)
        {
            string[] words = phrase.Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries);
            RecognizedWordUnit[] wordUnits = new RecognizedWordUnit[words.Length];
            for (int i = 0; i < wordUnits.Length; i++)
            {
                wordUnits[i] = new RecognizedWordUnit(null, 1.0f, null, words[i], DisplayAttributes.OneTrailingSpace, TimeSpan.Zero, TimeSpan.Zero);
            }
            return CreatePhraseFromWordUnits(wordUnits, culture, out memHandles, out coMem);
        }

        /// <summary>
        /// Helper function used to create a new phrase object from a
        /// test string. Each word in the string is converted to a phrase element.
        /// This is useful to create a phrase to pass to the EmulateRecognition method.
        /// </summary>
        /// <param name="words"></param>
        /// <param name="culture"></param>
        /// <param name="memHandles"></param>
        /// <param name="coMem"></param>
        /// <returns></returns>
        internal static ISpPhrase CreatePhraseFromWordUnits(RecognizedWordUnit[] words, CultureInfo culture, out GCHandle[] memHandles, out IntPtr coMem)
        {
            SPPHRASEELEMENT[] elements = new SPPHRASEELEMENT[words.Length];

            // build the unmanaged interop layer
            int size = Marshal.SizeOf(typeof(SPPHRASEELEMENT));
            List<GCHandle> handles = new List<GCHandle>();

            coMem = Marshal.AllocCoTaskMem(size * elements.Length);
            try
            {
                for (int i = 0; i < words.Length; i++)
                {
                    RecognizedWordUnit word = words[i];
                    elements[i] = new SPPHRASEELEMENT();

                    // diplay + confidence
                    elements[i].bDisplayAttributes = RecognizedWordUnit.DisplayAttributesToSapiAttributes(word.DisplayAttributes == DisplayAttributes.None ? DisplayAttributes.OneTrailingSpace : word.DisplayAttributes);
                    elements[i].SREngineConfidence = word.Confidence;

                    // Timing information
                    elements[i].ulAudioTimeOffset = unchecked((uint)(word._audioPosition.Ticks * 10000 / TimeSpan.TicksPerMillisecond));
                    elements[i].ulAudioSizeTime = unchecked((uint)(word._audioDuration.Ticks * 10000 / TimeSpan.TicksPerMillisecond));

                    // DLP information
                    if (word.Text != null)
                    {
                        GCHandle handle = GCHandle.Alloc(word.Text, GCHandleType.Pinned);
                        handles.Add(handle);
                        elements[i].pszDisplayText = handle.AddrOfPinnedObject();
                    }

                    if (word.Text == null || word.LexicalForm != word.Text)
                    {
                        GCHandle handle = GCHandle.Alloc(word.LexicalForm, GCHandleType.Pinned);
                        handles.Add(handle);
                        elements[i].pszLexicalForm = handle.AddrOfPinnedObject();
                    }
                    else
                    {
                        elements[i].pszLexicalForm = elements[i].pszDisplayText;
                    }

                    if (!string.IsNullOrEmpty(word.Pronunciation))
                    {
                        GCHandle handle = GCHandle.Alloc(word.Pronunciation, GCHandleType.Pinned);
                        handles.Add(handle);
                        elements[i].pszPronunciation = handle.AddrOfPinnedObject();
                    }

                    Marshal.StructureToPtr(elements[i], new IntPtr((long)coMem + size * i), false);
                }
            }
            finally
            {
                memHandles = handles.ToArray();
            }

            SPPHRASE spPhrase = new SPPHRASE();
            spPhrase.cbSize = (uint)Marshal.SizeOf(spPhrase.GetType());
            spPhrase.LangID = (ushort)culture.LCID;
            spPhrase.Rule = new SPPHRASERULE();
            spPhrase.Rule.ulCountOfElements = (uint)words.Length;

            spPhrase.pElements = coMem;

            // Initialized the phrase
            SpPhraseBuilder phraseBuilder = new SpPhraseBuilder();
            ((ISpPhraseBuilder)phraseBuilder).InitFromPhrase(spPhrase);

            return (ISpPhrase)phraseBuilder;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPPHRASERULE
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string pszName;
        internal UInt32 ulId;
        internal UInt32 ulFirstElement;
        internal UInt32 ulCountOfElements;
        internal IntPtr pNextSibling;
        internal IntPtr pFirstChild;
        internal float SREngineConfidence;
        internal byte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPPHRASEELEMENT
    {
        internal UInt32 ulAudioTimeOffset;
        internal UInt32 ulAudioSizeTime;    // In 100ns units
        internal UInt32 ulAudioStreamOffset;
        internal UInt32 ulAudioSizeBytes;
        internal UInt32 ulRetainedStreamOffset;
        internal UInt32 ulRetainedSizeBytes;
        internal IntPtr pszDisplayText;
        internal IntPtr pszLexicalForm;
        internal IntPtr pszPronunciation;
        internal byte bDisplayAttributes;
        internal byte RequiredConfidence;
        internal byte ActualConfidence;
        internal byte Reserved;
        internal float SREngineConfidence;
    }

    // The SAPI 5.2 & 5.3 result header added extra fields.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPSERIALIZEDPHRASE
    {
        internal SPSERIALIZEDPHRASE()
        { }

        internal SPSERIALIZEDPHRASE(SPSERIALIZEDPHRASE_Sapi51 source)
        {
            ulSerializedSize = source.ulSerializedSize;
            cbSize = source.cbSize;
            LangID = source.LangID;
            wHomophoneGroupId = source.wHomophoneGroupId;
            ullGrammarID = source.ullGrammarID;
            ftStartTime = source.ftStartTime;
            ullAudioStreamPosition = source.ullAudioStreamPosition;
            ulAudioSizeBytes = source.ulAudioSizeBytes;
            ulRetainedSizeBytes = source.ulRetainedSizeBytes;
            ulAudioSizeTime = source.ulAudioSizeTime;
            Rule = source.Rule;
            PropertiesOffset = source.PropertiesOffset;
            ElementsOffset = source.ElementsOffset;
            cReplacements = source.cReplacements;
            ReplacementsOffset = source.ReplacementsOffset;
            SREngineID = source.SREngineID;
            ulSREnginePrivateDataSize = source.ulSREnginePrivateDataSize;
            SREnginePrivateDataOffset = source.SREnginePrivateDataOffset;
        }

        // Duplicate all the fields of SPSERIALIZEDPHRASE_Sapi51 - Marshal.PtrToStructure seems to need these to be defined again.
        internal UInt32 ulSerializedSize;
        internal UInt32 cbSize;
        internal UInt16 LangID;
        internal UInt16 wHomophoneGroupId;
        internal UInt64 ullGrammarID;
        internal UInt64 ftStartTime;
        internal UInt64 ullAudioStreamPosition;
        internal UInt32 ulAudioSizeBytes;
        internal UInt32 ulRetainedSizeBytes;
        internal UInt32 ulAudioSizeTime;
        internal SPSERIALIZEDPHRASERULE Rule;
        internal UInt32 PropertiesOffset;
        internal UInt32 ElementsOffset;
        internal UInt32 cReplacements;
        internal UInt32 ReplacementsOffset;
        internal Guid SREngineID;
        internal UInt32 ulSREnginePrivateDataSize;
        internal UInt32 SREnginePrivateDataOffset;

        internal UInt32 SMLOffset; // Not present in SAPI 5.1 results.
        internal UInt32 SemanticErrorInfoOffset; // Not present in SAPI 5.1 results.
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPSERIALIZEDPHRASERULE
    {
        internal UInt32 pszNameOffset;
        internal UInt32 ulId;
        internal UInt32 ulFirstElement;
        internal UInt32 ulCountOfElements;
        internal UInt32 NextSiblingOffset;
        internal UInt32 FirstChildOffset;
        internal float SREngineConfidence;
        internal SByte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEELEMENT
    {
        internal UInt32 ulAudioTimeOffset;
        internal UInt32 ulAudioSizeTime;    // In 100ns units
        internal UInt32 ulAudioStreamOffset;
        internal UInt32 ulAudioSizeBytes;
        internal UInt32 ulRetainedStreamOffset;
        internal UInt32 ulRetainedSizeBytes;
        internal UInt32 pszDisplayTextOffset;
        internal UInt32 pszLexicalFormOffset;
        internal UInt32 pszPronunciationOffset;
        internal byte bDisplayAttributes;
        internal char RequiredConfidence;
        internal char ActualConfidence;
        internal byte Reserved;
        internal float SREngineConfidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEPROPERTY
    {
        internal UInt32 pszNameOffset;
        internal UInt32 ulId;
        internal UInt32 pszValueOffset;
        internal UInt16 vValue;					// sizeof unsigned short
        internal UInt64 SpVariantSubset;			// sizeof DOUBLE
        internal UInt32 ulFirstElement;
        internal UInt32 ulCountOfElements;
        internal UInt32 pNextSiblingOffset;
        internal UInt32 pFirstChildOffset;
        internal float SREngineConfidence;
        internal SByte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPPHRASEREPLACEMENT
    {
        internal byte bDisplayAttributes;
        internal UInt32 pszReplacementText;
        internal UInt32 ulFirstElement;
        internal UInt32 ulCountOfElements;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPVOICESTATUS
    {
        internal UInt32 ulCurrentStream;
        internal UInt32 ulLastStreamQueued;
        internal Int32 hrLastResult;
        internal SpeechRunState dwRunningState;
        internal UInt32 ulInputWordPos;
        internal UInt32 ulInputWordLen;
        internal UInt32 ulInputSentPos;
        internal UInt32 ulInputSentLen;
        internal Int32 lBookmarkId;
        internal UInt16 PhonemeId;
        internal Int32 VisemeId;
        internal UInt32 dwReserved1;
        internal UInt32 dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPWAVEFORMATEX
    {
        public UInt32 cbUsed;
        public Guid Guid;
        public UInt16 wFormatTag;
        public UInt16 nChannels;
        public UInt32 nSamplesPerSec;
        public UInt32 nAvgBytesPerSec;
        public UInt16 nBlockAlign;
        public UInt16 wBitsPerSample;
        public UInt16 cbSize;
    }

#pragma warning restore 649

    #endregion



    #region Interface

    [ComImport, Guid("8137828F-591A-4A42-BE58-49EA7EBAAC68"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpGrammarBuilder
    {
        // ISpGrammarBuilder Methods
        void Slot1(); // void ResetGrammar(UInt16 NewLanguage);
        void Slot2(); // void GetRule([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, UInt32 dwRuleId, UInt32 dwAttributes, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist, out IntPtr phInitialState);
        void Slot3(); // void ClearRule(IntPtr hState);
        void Slot4(); // void CreateNewState(IntPtr hState, out IntPtr phState);
        void Slot5(); // void AddWordTransition(IntPtr hFromState, IntPtr hToState, [MarshalAs(UnmanagedType.LPWStr)] string psz, [MarshalAs(UnmanagedType.LPWStr)] string pszSeparators, SPGRAMMARWORDTYPE eWordType, float Weight, ref SPPROPERTYINFO pPropInfo);
        void Slot6(); // void AddRuleTransition(IntPtr hFromState, IntPtr hToState, IntPtr hRule, float Weight, ref SPPROPERTYINFO pPropInfo);
        void Slot7(); // void AddResource(IntPtr hRuleState, [MarshalAs(UnmanagedType.LPWStr)] string pszResourceName, [MarshalAs(UnmanagedType.LPWStr)] string pszResourceValue);
        void Slot8(); // void Commit(UInt32 dwReserved);
    }

    [ComImport, Guid("2177DB29-7F45-47D0-8554-067E91C80502"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoGrammar : ISpGrammarBuilder
    {
        // ISpGrammarBuilder Methods
        new void Slot1(); // void ResetGrammar(UInt16 NewLanguage);
        new void Slot2(); // void GetRule([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, UInt32 dwRuleId, UInt32 dwAttributes, [MarshalAs(UnmanagedType.Bool)] bool fCreateIfNotExist, out IntPtr phInitialState);
        new void Slot3(); // void ClearRule(IntPtr hState);
        new void Slot4(); // void CreateNewState(IntPtr hState, out IntPtr phState);
        new void Slot5(); // void AddWordTransition(IntPtr hFromState, IntPtr hToState, [MarshalAs(UnmanagedType.LPWStr)] string psz, [MarshalAs(UnmanagedType.LPWStr)] string pszSeparators, SPGRAMMARWORDTYPE eWordType, float Weight, ref SPPROPERTYINFO pPropInfo);
        new void Slot6(); // void AddRuleTransition(IntPtr hFromState, IntPtr hToState, IntPtr hRule, float Weight, ref SPPROPERTYINFO pPropInfo);
        new void Slot7(); // void AddResource(IntPtr hRuleState, [MarshalAs(UnmanagedType.LPWStr)] string pszResourceName, [MarshalAs(UnmanagedType.LPWStr)] string pszResourceValue);
        new void Slot8(); // void Commit(UInt32 dwReserved);

        // ISpRecoGrammar Methods
        void Slot9(); // void GetGrammarId(out UInt64 pullGrammarId);
        void Slot10(); // void GetRecoContext(out ISpRecoContext ppRecoCtxt);
        void LoadCmdFromFile([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, SPLOADOPTIONS Options);
        void Slot12(); // void LoadCmdFromObject(ref Guid rcid, string pszGrammarName, SPLOADOPTIONS Options);
        void Slot13(); // void LoadCmdFromResource(IntPtr hModule, string pszResourceName, string pszResourceType, UInt16 wLanguage, SPLOADOPTIONS Options);
        void LoadCmdFromMemory(IntPtr pGrammar, SPLOADOPTIONS Options);
        void Slot15(); // void LoadCmdFromProprietaryGrammar(ref Guid rguidParam, string pszStringParam, IntPtr pvDataPrarm, UInt32 cbDataSize, SPLOADOPTIONS Options);
        [PreserveSig]
        int SetRuleState([MarshalAs(UnmanagedType.LPWStr)] string pszName, IntPtr pReserved, SPRULESTATE NewState);
        void Slot17(); // void SetRuleIdState(UInt32 ulRuleId, SPRULESTATE NewState);
        void LoadDictation([MarshalAs(UnmanagedType.LPWStr)] string pszTopicName, SPLOADOPTIONS Options);
        void Slot19(); // void UnloadDictation();
        [PreserveSig]
        int SetDictationState(SPRULESTATE NewState);
        void SetWordSequenceData([MarshalAs(UnmanagedType.LPWStr)] string pText, UInt32 cchText, ref SPTEXTSELECTIONINFO pInfo);
        void SetTextSelection(ref SPTEXTSELECTIONINFO pInfo);
        void Slot23(); // void IsPronounceable(string pszWord, out SPWORDPRONOUNCEABLE pWordPronounceable);
        void SetGrammarState(SPGRAMMARSTATE eGrammarState);
        void Slot25(); // void SaveCmd(IStream pStream, IntPtr ppszCoMemErrorText);
        void Slot26(); // void GetGrammarState(out SPGRAMMARSTATE peGrammarState);
    }

    [ComImport, Guid("4B37BC9E-9ED6-44a3-93D3-18F022B79EC3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoGrammar2
    {
        void GetRules(out IntPtr ppCoMemRules, out UInt32 puNumRules);
        void LoadCmdFromFile2([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, SPLOADOPTIONS Options, [MarshalAs(UnmanagedType.LPWStr)] string pszSharingUri, [MarshalAs(UnmanagedType.LPWStr)] string pszBaseUri);
        void LoadCmdFromMemory2(IntPtr pGrammar, SPLOADOPTIONS Options, [MarshalAs(UnmanagedType.LPWStr)] string pszSharingUri, [MarshalAs(UnmanagedType.LPWStr)] string pszBaseUri);
        void SetRulePriority([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, UInt32 ulRuleId, Int32 nRulePriority);
        void SetRuleWeight([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, UInt32 ulRuleId, float flWeight);
        void SetDictationWeight(float flWeight);
        void SetGrammarLoader(ISpGrammarResourceLoader pLoader);
        void Slot2(); //HRESULT SetSMLSecurityManager([in] IInternetSecurityManager* pSMLSecurityManager);
    }

    [ComImport, Guid("F740A62F-7C15-489E-8234-940A33D9272D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoContext : ISpEventSource
    {
        // ISpNotifySource Methods
        new void SetNotifySink(ISpNotifySink pNotifySink);
        new void SetNotifyWindowMessage(UInt32 hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        new void Slot3(); // void SetNotifyCallbackFunction(ref IntPtr pfnCallback, IntPtr wParam, IntPtr lParam);
        new void Slot4(); // void SetNotifyCallbackInterface(ref IntPtr pSpCallback, IntPtr wParam, IntPtr lParam);
        new void Slot5(); // void SetNotifyWin32Event();
        [PreserveSig]
        new int WaitForNotifyEvent(UInt32 dwMilliseconds);
        new void Slot7(); // IntPtr GetNotifyEventHandle();

        // ISpEventSource Methods
        new void SetInterest(UInt64 ullEventInterest, UInt64 ullQueuedInterest);
        new void GetEvents(UInt32 ulCount, out SPEVENT pEventArray, out UInt32 pulFetched);
        new void Slot10(); // void GetInfo(out SPEVENTSOURCEINFO pInfo);

        // ISpRecoContext Methods
        void GetRecognizer(out ISpRecognizer ppRecognizer);
        void CreateGrammar(UInt64 ullGrammarID, out ISpRecoGrammar ppGrammar);
        void GetStatus(out SPRECOCONTEXTSTATUS pStatus);
        void GetMaxAlternates(out UInt32 pcAlternates);
        void SetMaxAlternates(UInt32 cAlternates);
        void SetAudioOptions(SPAUDIOOPTIONS Options, IntPtr pAudioFormatId, IntPtr pWaveFormatEx);
        void Slot17(); // void GetAudioOptions(out SPAUDIOOPTIONS pOptions, out Guid pAudioFormatId, out IntPtr ppCoMemWFEX);
        void Slot18(); // void DeserializeResult(ref SPSERIALIZEDRESULT pSerializedResult, out ISpRecoResult ppResult);
        void Bookmark(SPBOOKMARKOPTIONS Options, UInt64 ullStreamPosition, IntPtr lparamEvent);
        void Slot20(); // void SetAdaptationData([MarshalAs(UnmanagedType.LPWStr)] string pAdaptationData, UInt32 cch);
        void Pause(UInt32 dwReserved);
        void Resume(UInt32 dwReserved);
        void Slot23(); // void SetVoice (ISpVoice pVoice, [MarshalAs (UnmanagedType.Bool)] bool fAllowFormatChanges);
        void Slot24(); // void GetVoice(out ISpVoice ppVoice);
        void Slot25(); // void SetVoicePurgeEvent(UInt64 ullEventInterest);
        void Slot26(); // void GetVoicePurgeEvent(out UInt64 pullEventInterest);
        void SetContextState(SPCONTEXTSTATE eContextState);
        void Slot28(); // void GetContextState(out SPCONTEXTSTATE peContextState);
    }

    [ComImport, Guid("BEAD311C-52FF-437f-9464-6B21054CA73D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoContext2
    {
        // ISpRecoContext2 Methods
        void SetGrammarOptions(SPGRAMMAROPTIONS eGrammarOptions);
        void Slot2(); // void GetGrammarOptions(out SPGRAMMAROPTIONS peGrammarOptions);
        void SetAdaptationData2([MarshalAs(UnmanagedType.LPWStr)] string pAdaptationData, UInt32 cch, [MarshalAs(UnmanagedType.LPWStr)] string pTopicName, SPADAPTATIONSETTINGS eSettings, SPADAPTATIONRELEVANCE eRelevance);
    }

    [ComImport, Guid("5B4FB971-B115-4DE1-AD97-E482E3BF6EE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpProperties
    {
        // ISpProperties Methods
        [PreserveSig]
        int SetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, Int32 lValue);
        [PreserveSig]
        int GetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, out Int32 plValue);
        [PreserveSig]
        int SetPropertyString([MarshalAs(UnmanagedType.LPWStr)] string pName, [MarshalAs(UnmanagedType.LPWStr)] string pValue);
        [PreserveSig]
        int GetPropertyString([MarshalAs(UnmanagedType.LPWStr)] string pName, [MarshalAs(UnmanagedType.LPWStr)] out string ppCoMemValue);
    }

    [ComImport, Guid("C2B5F241-DAA0-4507-9E16-5A1EAA2B7A5C"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecognizer : ISpProperties
    {
        // ISpProperties Methods
        [PreserveSig]
        new int SetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, Int32 lValue);
        [PreserveSig]
        new int GetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, out Int32 plValue);
        [PreserveSig]
        new int SetPropertyString([MarshalAs(UnmanagedType.LPWStr)] string pName, [MarshalAs(UnmanagedType.LPWStr)] string pValue);
        [PreserveSig]
        new int GetPropertyString([MarshalAs(UnmanagedType.LPWStr)] string pName, [MarshalAs(UnmanagedType.LPWStr)] out string ppCoMemValue);

        // ISpRecognizer Methods
        void SetRecognizer(ISpObjectToken pRecognizer);
        void GetRecognizer(out ISpObjectToken ppRecognizer);
        void SetInput([MarshalAs(UnmanagedType.IUnknown)] object pUnkInput, [MarshalAs(UnmanagedType.Bool)] bool fAllowFormatChanges);
        void Slot8(); // void GetInputObjectToken(out ISpObjectToken ppToken);
        void Slot9(); // void GetInputStream(out ISpStreamFormat ppStream);
        void CreateRecoContext(out ISpRecoContext ppNewCtxt);
        void Slot11();//void GetRecoProfile(out ISpObjectToken ppToken);
        void Slot12(); // void SetRecoProfile(ISpObjectToken pToken);
        void Slot13(); // void IsSharedInstance();
        void GetRecoState(out SPRECOSTATE pState);
        void SetRecoState(SPRECOSTATE NewState);
        void GetStatus(out SPRECOGNIZERSTATUS pStatus);
        void GetFormat(SPSTREAMFORMATTYPE WaveFormatType, out Guid pFormatId, out IntPtr ppCoMemWFEX);
        void IsUISupported([MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData, [MarshalAs(UnmanagedType.Bool)] out bool pfSupported);
        [PreserveSig]
        int DisplayUI(IntPtr hWndParent, [MarshalAs(UnmanagedType.LPWStr)] string pszTitle, [MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, UInt32 cbExtraData);
        [PreserveSig]
        int EmulateRecognition(ISpPhrase pPhrase);
    }

    [ComImport, Guid("8FC6D974-C81E-4098-93C5-0147F61ED4D3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecognizer2
    {
        // ISpRecognizer2 Methods
        [PreserveSig]
        int EmulateRecognitionEx(ISpPhrase pPhrase, UInt32 dwCompareFlags);
        void SetTrainingState(bool fDoingTraining, bool fAdaptFromTrainingData);
        void ResetAcousticModelAdaptation();
    }

    [ComImport, Guid("2D5F1C0C-BD75-4b08-9478-3B11FEA2586C"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ISpeechRecognizer
    {
        // ISpeechRecognizer Methods
        object Slot1 { set; get; } // [DispId(1)] SpObjectToken Recognizer { set; get; }
        object Slot2 { set; get; } // [DispId(2)] bool AllowAudioInputFormatChangesOnNextSet { set; get; }
        object Slot3 { set; get; } // [DispId(3)] SpObjectToken AudioInput { set; get; }
        object Slot4 { set; get; } // [DispId(4)] ISpeechBaseStream AudioInputStream { set; get; }
        object Slot5 { get; } // [DispId(5)] bool IsShared { get; }
        object Slot6 { set; get; } // [DispId(8)] SpObjectToken Profile { set; get; }
        object Slot7 { set; get; } // [DispId(6)] SpeechRecognizerState State { set; get; }
        object Slot8 { get; } // [DispId(7)] ISpeechRecognizerStatus Status { get; }
        [DispId(9)]
        [PreserveSig]
        int EmulateRecognition(object TextElements, ref Object ElementDisplayAttributes, Int32 LanguageId);
        void Slot10(); // [DispId(10)] ISpeechRecoContext CreateRecoContext();
        void Slot11(); // [DispId(11)] SpAudioFormat GetFormat(SpeechFormatType Type);
        void Slot12(); // [DispId(12)] bool SetPropertyNumber(string Name, Int32 Value);
        void Slot13(); // [DispId(13)] bool GetPropertyNumber(string Name, out Int32 Value);
        void Slot14(); // [DispId(14)] bool SetPropertyString(string Name, string Value);
        void Slot15(); // [DispId(15)] bool GetPropertyString(string Name, out string Value);
        void Slot16(); // [DispId(16)] bool IsUISupported(string TypeOfUI, ref Object ExtraData);
        void Slot17(); // [DispId(17)] void DisplayUI(Int32 hWndParent, string Title, string TypeOfUI, ref Object ExtraData);
        void Slot18(); // [DispId(18)] ISpeechObjectTokens GetRecognizers(string RequiredAttributes, string OptionalAttributes);
        void Slot19(); // [DispId(19)] ISpeechObjectTokens GetAudioInputs(string RequiredAttributes, string OptionalAttributes);
        void Slot20(); // [DispId(20)] ISpeechObjectTokens GetProfiles(string RequiredAttributes, string OptionalAttributes);
    }

    [ComImport, Guid("1A5C0354-B621-4b5a-8791-D306ED379E53"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhrase
    {
        // ISpPhrase Methods
        void GetPhrase(out IntPtr ppCoMemPhrase);
        void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        void Discard(UInt32 dwValueTypes);
    }

    [ComImport, Guid("20B053BE-E235-43cd-9A2A-8D17A48B7842"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoResult : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        // ISpRecoResult Methods
        void Slot5(); // void GetResultTimes(out SPRECORESULTTIMES pTimes);
        void GetAlternates(Int32 ulStartElement, Int32 cElements, Int32 ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out Int32 pcPhrasesReturned);
        void GetAudio(UInt32 ulStartElement, UInt32 cElements, out ISpStreamFormat ppStream);
        void Slot8(); // void SpeakAudio(UInt32 ulStartElement, UInt32 cElements, UInt32 dwFlags, out UInt32 pulStreamNumber);
        void Serialize(out IntPtr ppCoMemSerializedResult);
        void Slot10(); // void ScaleAudio(ref Guid pAudioFormatId, IntPtr pWaveFormatEx);
        void Slot11(); // void GetRecoContext(out ISpRecoContext ppRecoContext);
    }

    [ComImport, Guid("8FCEBC98-4E49-4067-9C6C-D86A0E092E3D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhraseAlt : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        // ISpPhraseAlt Methods
        void GetAltInfo(out ISpPhrase ppParent, out UInt32 pulStartElementInParent, out UInt32 pcElementsInParent, out UInt32 pcElementsInAlt);
        void Commit();
    }

    [ComImport, Guid("27CAC6C4-88F2-41f2-8817-0C95E59F1E6E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoResult2 : ISpRecoResult
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        // ISpRecoResult Methods
        new void Slot5(); // new void GetResultTimes(out SPRECORESULTTIMES pTimes);
        new void GetAlternates(Int32 ulStartElement, Int32 cElements, Int32 ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out Int32 pcPhrasesReturned);
        new void GetAudio(UInt32 ulStartElement, UInt32 cElements, out ISpStreamFormat ppStream);
        new void Slot8(); // void SpeakAudio(UInt32 ulStartElement, UInt32 cElements, UInt32 dwFlags, out UInt32 pulStreamNumber);
        new void Serialize(out IntPtr ppCoMemSerializedResult);
        new void Slot10(); // void ScaleAudio(ref Guid pAudioFormatId, IntPtr pWaveFormatEx);
        new void Slot11(); // void GetRecoContext(out ISpRecoContext ppRecoContext);

        // ISpRecoResult2 Methods
        void CommitAlternate(ISpPhraseAlt pPhraseAlt, out ISpRecoResult ppNewResult);
        void CommitText(UInt32 ulStartElement, UInt32 ulCountOfElements, [MarshalAs(UnmanagedType.LPWStr)] string pszCorrectedData, SPCOMMITFLAGS commitFlags);
        void SetTextFeedback([MarshalAs(UnmanagedType.LPWStr)] string pszFeedback, [MarshalAs(UnmanagedType.Bool)] bool fSuccessful);
    }

    [ComImport, Guid("AE39362B-45A8-4074-9B9E-CCF49AA2D0B6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpXMLRecoResult : ISpRecoResult
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        // ISpRecoResult Methods
        new void Slot5(); // new void GetResultTimes(out SPRECORESULTTIMES pTimes);
        new void GetAlternates(Int32 ulStartElement, Int32 cElements, Int32 ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out Int32 pcPhrasesReturned);
        new void GetAudio(UInt32 ulStartElement, UInt32 cElements, out ISpStreamFormat ppStream);
        new void Slot8(); // void SpeakAudio(UInt32 ulStartElement, UInt32 cElements, UInt32 dwFlags, out UInt32 pulStreamNumber);
        new void Serialize(out IntPtr ppCoMemSerializedResult);
        new void Slot10(); // void ScaleAudio(ref Guid pAudioFormatId, IntPtr pWaveFormatEx);
        new void Slot11(); // void GetRecoContext(out ISpRecoContext ppRecoContext);

        // ISpXMLRecoResult Methods
        void GetXMLResult([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemXMLResult, SPXMLRESULTOPTIONS Options);
        void GetXMLErrorInfo(out SPSEMANTICERRORINFO pSemanticErrorInfo);
    }

    [ComImport, Guid("F264DA52-E457-4696-B856-A737B717AF79"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhraseEx : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        // ISpPhraseEx Methods
        void GetXMLResult([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemXMLResult, SPXMLRESULTOPTIONS Options);
        void GetXMLErrorInfo(out SPSEMANTICERRORINFO pSemanticErrorInfo);
        void Slot7(); // void GetAudio(UInt32 ulStartElement, UInt32 cElements, out ISpStreamFormat ppStream);
    }

    [ComImport, Guid("C8D7C7E2-0DDE-44b7-AFE3-B0C991FBEB5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpDisplayAlternates
    {
        void GetDisplayAlternates(IntPtr pPhrase, UInt32 cRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppCoMemPhrases, out UInt32 pcPhrasesReturned);
    }

    /// <summary>
    /// Resource Loader interface definition
    /// </summary>
    [ComImport, Guid("B9AC5783-FCD0-4b21-B119-B4F8DA8FD2C3"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    internal interface ISpGrammarResourceLoader
    {
        /// <summary>
        /// Load some data
        /// </summary>
        /// <param name="bstrResourceUri"></param>
        /// <param name="fAlwaysReload"></param>
        /// <param name="pStream"></param>
        /// <param name="pbstrMIMEType"></param>
        /// <param name="pfModified"></param>
        /// <param name="pbstrRedirectUrl"></param>
        /// <returns></returns>
        [PreserveSig]
        int LoadResource(string bstrResourceUri, bool fAlwaysReload, out IStream pStream, ref string pbstrMIMEType, ref short pfModified, ref string pbstrRedirectUrl);

        /// <summary>
        /// Converts the resourcePath to a location in the file cache and returns a reference into the
        /// cache
        /// </summary>
        /// <param name="resourcePath"></param>
        /// <param name="mimeType"></param>
        /// <param name="redirectUrl"></param>
        string GetLocalCopy(Uri resourcePath, out string mimeType, out Uri redirectUrl);

        /// <summary>
        /// Mark an entry in the file cache as unused.
        /// </summary>
        /// <param name="path"></param>
        void ReleaseLocalCopy(string path);
    }

    [ComImport, Guid("88A3342A-0BED-4834-922B-88D43173162F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhraseBuilder : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(UInt32 ulStart, UInt32 ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out Byte pbDisplayAttributes);
        new void Discard(UInt32 dwValueTypes);

        void InitFromPhrase(SPPHRASE pPhrase);
        void Slot6(); // InitFromSerializedPhrase(const SPSERIALIZEDPHRASE * pPhrase);
        void Slot7(); // AddElements(ULONG cElements, const SPPHRASEELEMENT *pElement);
        void Slot8(); // AddRules(const SPPHRASERULEHANDLE hParent, const SPPHRASERULE * pRule, SPPHRASERULEHANDLE * phNewRule);
        void Slot9(); // AddProperties(const SPPHRASEPROPERTYHANDLE hParent, const SPPHRASEPROPERTY * pProperty, SPPHRASEPROPERTYHANDLE * phNewProperty);
        void Slot10(); // AddReplacements(ULONG cReplacements, const SPPHRASEREPLACEMENT * pReplacements);
    };

    #endregion


    #region Class


    [ComImport, Guid("3BEE4890-4FE9-4A37-8C1E-5E7E12791C1F")]
    internal class SpSharedRecognizer { }

    [ComImport, Guid("41B89B6B-9399-11D2-9623-00C04F8EE628")]
    internal class SpInprocRecognizer { }

    [ComImport, Guid("777B6BBD-2FF2-11D3-88FE-00C04F8EF9B5")]
    internal class SpPhraseBuilder { }

    #endregion Class
}
