// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Speech.Recognition;

namespace System.Speech.Internal.SapiInterop
{
    #region Enum

    internal enum SPRECOSTATE
    {
        SPRST_INACTIVE = 0x00000000,
        SPRST_ACTIVE = 0x00000001,
        SPRST_ACTIVE_ALWAYS = 0x00000002,
        SPRST_INACTIVE_WITH_PURGE = 0x00000003,
        SPRST_NUM_STATES = 0x00000004
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

    /// Note:   This structure doesn't exist in SAPI.idl but is related to SPPHRASEALT.
    ///         We use it to map memory contained in the serialized result (instead of reading sequentially)
    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEALT
    {
        internal uint ulStartElementInParent;
        internal uint cElementsInParent;
        internal uint cElementsInAlternate;
        internal uint cbAltExtra;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct FILETIME
    {
        internal uint dwLowDateTime;
        internal uint dwHighDateTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal struct SPRECORESULTTIMES
    {
        internal FILETIME ftStreamTime;
        internal ulong ullLength;
        internal uint dwTickCount;
        internal ulong ullStart;
    }

    internal struct SPTEXTSELECTIONINFO
    {
        internal uint ulStartActiveOffset;
        internal uint cchActiveChars;
        internal uint ulStartSelection;
        internal uint cchSelection;

        internal SPTEXTSELECTIONINFO(uint ulStartActiveOffset, uint cchActiveChars,
            uint ulStartSelection, uint cchSelection)
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
        internal int cbFreeBuffSpace;
        internal uint cbNonBlockingIO;
        internal SPAUDIOSTATE State;
        internal ulong CurSeekPos;
        internal ulong CurDevicePos;
        internal uint dwAudioLevel;
        internal uint dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPRECOGNIZERSTATUS
    {
        internal SPAUDIOSTATUS AudioStatus;
        internal ulong ullRecognitionStreamPos;
        internal uint ulStreamNumber;
        internal uint ulNumActive;
        internal Guid clsidEngine;
        internal uint cLangIDs;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)] // SP_MAX_LANGIDS
        internal short[] aLangID;
        internal ulong ullRecognitionStreamTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPRECOCONTEXTSTATUS
    {
        internal SPINTERFERENCE eInterference;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
        internal short[] szRequestTypeOfUI; // Can't really be marshaled as a string directly
        internal uint dwReserved1;
        internal uint dwReserved2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSEMANTICERRORINFO
    {
        internal uint ulLineNumber;
        internal uint pszScriptLineOffset;
        internal uint pszSourceOffset;
        internal uint pszDescriptionOffset;
        internal int hrResultCode;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SPSERIALIZEDRESULT
    {
        internal uint ulSerializedSize;       // Count in bytes (including this ULONG) of the entire phrase
    }

    // Serialized result header from versions of SAPI prior to 5.3.
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPRESULTHEADER_Sapi51
    {
        internal uint ulSerializedSize;     // This MUST be the first field to line up with SPSERIALIZEDRESULT
        internal uint cbHeaderSize;         // This must be sizeof(SPRESULTHEADER), or sizeof(SPRESULTHEADER_Sapi51) on SAPI 5.1.
        internal Guid clsidEngine;            // CLSID clsidEngine;
        internal Guid clsidAlternates;        // CLSID clsidAlternates;
        internal uint ulStreamNum;
        internal ulong ullStreamPosStart;
        internal ulong ullStreamPosEnd;
        internal uint ulPhraseDataSize;     // byte size of all the phrase structure
        internal uint ulPhraseOffset;       // offset to phrase
        internal uint ulPhraseAltDataSize;  // byte size of all the phrase alt structures combined
        internal uint ulPhraseAltOffset;    // offset to phrase
        internal uint ulNumPhraseAlts;      // Number of alts in array
        internal uint ulRetainedDataSize;   // byte size of audio data
        internal uint ulRetainedOffset;     // offset to audio data in this phrase blob
        internal uint ulDriverDataSize;     // byte size of driver specific data
        internal uint ulDriverDataOffset;   // offset to driver specific data
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
        internal uint ulSerializedSize;
        internal uint cbHeaderSize;
        internal Guid clsidEngine;
        internal Guid clsidAlternates;
        internal uint ulStreamNum;
        internal ulong ullStreamPosStart;
        internal ulong ullStreamPosEnd;
        internal uint ulPhraseDataSize;
        internal uint ulPhraseOffset;
        internal uint ulPhraseAltDataSize;
        internal uint ulPhraseAltOffset;
        internal uint ulNumPhraseAlts;
        internal uint ulRetainedDataSize;
        internal uint ulRetainedOffset;
        internal uint ulDriverDataSize;
        internal uint ulDriverDataOffset;
        internal float fTimePerByte;
        internal float fInputScaleFactor;
        internal SPRECORESULTTIMES times;

        private void ValidateOffsetAndLength(uint offset, uint length)
        {
            if (offset + length > ulSerializedSize)
            {
                throw new FormatException(SR.Get(SRID.ResultInvalidFormat));
            }
        }
        internal uint fAlphabet;
        // Not present in SAPI 5.1 results; on SAPI 5.without IPA this is set to zero, with IPA it will indicate
        // the alphabet of pronunciations the result
    }

    // Serialized phrase header from versions of SAPI prior to 5.2.
    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASE_Sapi51
    {
        internal uint ulSerializedSize;          // This MUST be the first field to line up with SPSERIALIZEDPHRASE
        internal uint cbSize;      // size of just this structure within the serialized block header")
        internal ushort LangID;
        internal ushort wHomophoneGroupId;
        internal ulong ullGrammarID;
        internal ulong ftStartTime;
        internal ulong ullAudioStreamPosition;
        internal uint ulAudioSizeBytes;
        internal uint ulRetainedSizeBytes;
        internal uint ulAudioSizeTime;
        internal SPSERIALIZEDPHRASERULE Rule;
        internal uint PropertiesOffset;
        internal uint ElementsOffset;
        internal uint cReplacements;
        internal uint ReplacementsOffset;
        internal Guid SREngineID;
        internal uint ulSREnginePrivateDataSize;
        internal uint SREnginePrivateDataOffset;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPPHRASE
    {
        internal uint cbSize;     // Size of structure
        internal ushort LangID;
        internal ushort wReserved;
        internal ulong ullGrammarID;
        internal ulong ftStartTime;
        internal ulong ullAudioStreamPosition;
        internal uint ulAudioSizeBytes;
        internal uint ulRetainedSizeBytes;
        internal uint ulAudioSizeTime;  // In 100ns units
        internal SPPHRASERULE Rule;
        internal IntPtr pProperties;
        internal IntPtr pElements;
        internal uint cReplacements;
        internal IntPtr pReplacements;
        internal Guid SREngineID;
        internal uint ulSREnginePrivateDataSize;
        internal IntPtr pSREnginePrivateData;

        /// <summary>
        /// Helper function used to create a new phrase object from a
        /// test string. Each word in the string is converted to a phrase element.
        /// This is useful to create a phrase to pass to the EmulateRecognition method.
        /// </summary>
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
        internal static ISpPhrase CreatePhraseFromWordUnits(RecognizedWordUnit[] words, CultureInfo culture, out GCHandle[] memHandles, out IntPtr coMem)
        {
            SPPHRASEELEMENT[] elements = new SPPHRASEELEMENT[words.Length];

            // build the unmanaged interop layer
            int size = Marshal.SizeOf<SPPHRASEELEMENT>();
            List<GCHandle> handles = new();

            coMem = Marshal.AllocCoTaskMem(size * elements.Length);
            try
            {
                for (int i = 0; i < words.Length; i++)
                {
                    RecognizedWordUnit word = words[i];
                    elements[i] = new SPPHRASEELEMENT
                    {
                        // display + confidence
                        bDisplayAttributes = RecognizedWordUnit.DisplayAttributesToSapiAttributes(word.DisplayAttributes == DisplayAttributes.None ? DisplayAttributes.OneTrailingSpace : word.DisplayAttributes),
                        SREngineConfidence = word.Confidence,

                        // Timing information
                        ulAudioTimeOffset = unchecked((uint)(word._audioPosition.Ticks * 10000 / TimeSpan.TicksPerMillisecond)),
                        ulAudioSizeTime = unchecked((uint)(word._audioDuration.Ticks * 10000 / TimeSpan.TicksPerMillisecond))
                    };

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

                    Marshal.StructureToPtr<SPPHRASEELEMENT>(elements[i], new IntPtr((long)coMem + size * i), false);
                }
            }
            finally
            {
                memHandles = handles.ToArray();
            }

            SPPHRASE spPhrase = new();
            spPhrase.cbSize = (uint)Marshal.SizeOf<SPPHRASE>();
            spPhrase.LangID = (ushort)culture.LCID;
            spPhrase.Rule = new SPPHRASERULE
            {
                ulCountOfElements = (uint)words.Length
            };

            spPhrase.pElements = coMem;

            // Initialized the phrase
            SpPhraseBuilder phraseBuilder = new();
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
        internal uint ulId;
        internal uint ulFirstElement;
        internal uint ulCountOfElements;
        internal IntPtr pNextSibling;
        internal IntPtr pFirstChild;
        internal float SREngineConfidence;
        internal byte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPPHRASEELEMENT
    {
        internal uint ulAudioTimeOffset;
        internal uint ulAudioSizeTime;    // In 100ns units
        internal uint ulAudioStreamOffset;
        internal uint ulAudioSizeBytes;
        internal uint ulRetainedStreamOffset;
        internal uint ulRetainedSizeBytes;
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
        internal uint ulSerializedSize;
        internal uint cbSize;
        internal ushort LangID;
        internal ushort wHomophoneGroupId;
        internal ulong ullGrammarID;
        internal ulong ftStartTime;
        internal ulong ullAudioStreamPosition;
        internal uint ulAudioSizeBytes;
        internal uint ulRetainedSizeBytes;
        internal uint ulAudioSizeTime;
        internal SPSERIALIZEDPHRASERULE Rule;
        internal uint PropertiesOffset;
        internal uint ElementsOffset;
        internal uint cReplacements;
        internal uint ReplacementsOffset;
        internal Guid SREngineID;
        internal uint ulSREnginePrivateDataSize;
        internal uint SREnginePrivateDataOffset;

        internal uint SMLOffset; // Not present in SAPI 5.1 results.
        internal uint SemanticErrorInfoOffset; // Not present in SAPI 5.1 results.
    }

    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    internal class SPSERIALIZEDPHRASERULE
    {
        internal uint pszNameOffset;
        internal uint ulId;
        internal uint ulFirstElement;
        internal uint ulCountOfElements;
        internal uint NextSiblingOffset;
        internal uint FirstChildOffset;
        internal float SREngineConfidence;
        internal sbyte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEELEMENT
    {
        internal uint ulAudioTimeOffset;
        internal uint ulAudioSizeTime;    // In 100ns units
        internal uint ulAudioStreamOffset;
        internal uint ulAudioSizeBytes;
        internal uint ulRetainedStreamOffset;
        internal uint ulRetainedSizeBytes;
        internal uint pszDisplayTextOffset;
        internal uint pszLexicalFormOffset;
        internal uint pszPronunciationOffset;
        internal byte bDisplayAttributes;
        internal char RequiredConfidence;
        internal char ActualConfidence;
        internal byte Reserved;
        internal float SREngineConfidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPSERIALIZEDPHRASEPROPERTY
    {
        internal uint pszNameOffset;
        internal uint ulId;
        internal uint pszValueOffset;
        internal ushort vValue;                    // sizeof unsigned short
        internal ulong SpVariantSubset;            // sizeof DOUBLE
        internal uint ulFirstElement;
        internal uint ulCountOfElements;
        internal uint pNextSiblingOffset;
        internal uint pFirstChildOffset;
        internal float SREngineConfidence;
        internal sbyte Confidence;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPPHRASEREPLACEMENT
    {
        internal byte bDisplayAttributes;
        internal uint pszReplacementText;
        internal uint ulFirstElement;
        internal uint ulCountOfElements;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPWAVEFORMATEX
    {
        public uint cbUsed;
        public Guid Guid;
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

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
        void SetWordSequenceData([MarshalAs(UnmanagedType.LPWStr)] string pText, uint cchText, ref SPTEXTSELECTIONINFO pInfo);
        void SetTextSelection(ref SPTEXTSELECTIONINFO pInfo);
        void Slot23(); // void IsPronounceable(string pszWord, out SPWORDPRONOUNCEABLE pWordPronounceable);
        void SetGrammarState(SPGRAMMARSTATE eGrammarState);
        void Slot25(); // void SaveCmd(IStream pStream, IntPtr ppszCoMemErrorText);
        void Slot26(); // void GetGrammarState(out SPGRAMMARSTATE peGrammarState);
    }

    [ComImport, Guid("4B37BC9E-9ED6-44a3-93D3-18F022B79EC3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoGrammar2
    {
        void GetRules(out IntPtr ppCoMemRules, out uint puNumRules);
        void LoadCmdFromFile2([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, SPLOADOPTIONS Options, [MarshalAs(UnmanagedType.LPWStr)] string pszSharingUri, [MarshalAs(UnmanagedType.LPWStr)] string pszBaseUri);
        void LoadCmdFromMemory2(IntPtr pGrammar, SPLOADOPTIONS Options, [MarshalAs(UnmanagedType.LPWStr)] string pszSharingUri, [MarshalAs(UnmanagedType.LPWStr)] string pszBaseUri);
        void SetRulePriority([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, uint ulRuleId, int nRulePriority);
        void SetRuleWeight([MarshalAs(UnmanagedType.LPWStr)] string pszRuleName, uint ulRuleId, float flWeight);
        void SetDictationWeight(float flWeight);
        void SetGrammarLoader(ISpGrammarResourceLoader pLoader);
        void Slot2(); //HRESULT SetSMLSecurityManager([in] IInternetSecurityManager* pSMLSecurityManager);
    }

    [ComImport, Guid("F740A62F-7C15-489E-8234-940A33D9272D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoContext : ISpEventSource
    {
        // ISpNotifySource Methods
        new void SetNotifySink(ISpNotifySink pNotifySink);
        new void SetNotifyWindowMessage(uint hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        new void Slot3(); // void SetNotifyCallbackFunction(ref IntPtr pfnCallback, IntPtr wParam, IntPtr lParam);
        new void Slot4(); // void SetNotifyCallbackInterface(ref IntPtr pSpCallback, IntPtr wParam, IntPtr lParam);
        new void Slot5(); // void SetNotifyWin32Event();
        [PreserveSig]
        new int WaitForNotifyEvent(uint dwMilliseconds);
        new void Slot7(); // IntPtr GetNotifyEventHandle();

        // ISpEventSource Methods
        new void SetInterest(ulong ullEventInterest, ulong ullQueuedInterest);
        new void GetEvents(uint ulCount, out SPEVENT pEventArray, out uint pulFetched);
        new void Slot10(); // void GetInfo(out SPEVENTSOURCEINFO pInfo);

        // ISpRecoContext Methods
        void GetRecognizer(out ISpRecognizer ppRecognizer);
        void CreateGrammar(ulong ullGrammarID, out ISpRecoGrammar ppGrammar);
        void GetStatus(out SPRECOCONTEXTSTATUS pStatus);
        void GetMaxAlternates(out uint pcAlternates);
        void SetMaxAlternates(uint cAlternates);
        void SetAudioOptions(SPAUDIOOPTIONS Options, IntPtr pAudioFormatId, IntPtr pWaveFormatEx);
        void Slot17(); // void GetAudioOptions(out SPAUDIOOPTIONS pOptions, out Guid pAudioFormatId, out IntPtr ppCoMemWFEX);
        void Slot18(); // void DeserializeResult(ref SPSERIALIZEDRESULT pSerializedResult, out ISpRecoResult ppResult);
        void Bookmark(SPBOOKMARKOPTIONS Options, ulong ullStreamPosition, IntPtr lparamEvent);
        void Slot20(); // void SetAdaptationData([MarshalAs(UnmanagedType.LPWStr)] string pAdaptationData, UInt32 cch);
        void Pause(uint dwReserved);
        void Resume(uint dwReserved);
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
        void SetAdaptationData2([MarshalAs(UnmanagedType.LPWStr)] string pAdaptationData, uint cch, [MarshalAs(UnmanagedType.LPWStr)] string pTopicName, SPADAPTATIONSETTINGS eSettings, SPADAPTATIONRELEVANCE eRelevance);
    }

    [ComImport, Guid("5B4FB971-B115-4DE1-AD97-E482E3BF6EE4"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpProperties
    {
        // ISpProperties Methods
        [PreserveSig]
        int SetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, int lValue);
        [PreserveSig]
        int GetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, out int plValue);
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
        new int SetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, int lValue);
        [PreserveSig]
        new int GetPropertyNum([MarshalAs(UnmanagedType.LPWStr)] string pName, out int plValue);
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
        void IsUISupported([MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, uint cbExtraData, [MarshalAs(UnmanagedType.Bool)] out bool pfSupported);
        [PreserveSig]
        int DisplayUI(IntPtr hWndParent, [MarshalAs(UnmanagedType.LPWStr)] string pszTitle, [MarshalAs(UnmanagedType.LPWStr)] string pszTypeOfUI, IntPtr pvExtraData, uint cbExtraData);
        [PreserveSig]
        int EmulateRecognition(ISpPhrase pPhrase);
    }

    [ComImport, Guid("8FC6D974-C81E-4098-93C5-0147F61ED4D3"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecognizer2
    {
        // ISpRecognizer2 Methods
        [PreserveSig]
        int EmulateRecognitionEx(ISpPhrase pPhrase, uint dwCompareFlags);
        void SetTrainingState(bool fDoingTraining, bool fAdaptFromTrainingData);
        void ResetAcousticModelAdaptation();
    }

    [ComImport, Guid("2D5F1C0C-BD75-4b08-9478-3B11FEA2586C")]
    internal interface ISpeechRecognizer
    {
        // ISpeechRecognizer Methods
        object Slot1 { get; set; } // [DispId(1)] SpObjectToken Recognizer { set; get; }
        object Slot2 { get; set; } // [DispId(2)] bool AllowAudioInputFormatChangesOnNextSet { set; get; }
        object Slot3 { get; set; } // [DispId(3)] SpObjectToken AudioInput { set; get; }
        object Slot4 { get; set; } // [DispId(4)] ISpeechBaseStream AudioInputStream { set; get; }
        object Slot5 { get; } // [DispId(5)] bool IsShared { get; }
        object Slot6 { get; set; } // [DispId(8)] SpObjectToken Profile { set; get; }
        object Slot7 { get; set; } // [DispId(6)] SpeechRecognizerState State { set; get; }
        object Slot8 { get; } // [DispId(7)] ISpeechRecognizerStatus Status { get; }
        [DispId(9)]
        [PreserveSig]
        int EmulateRecognition(object TextElements, ref object ElementDisplayAttributes, int LanguageId);
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
        void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        void Discard(uint dwValueTypes);
    }

    [ComImport, Guid("20B053BE-E235-43cd-9A2A-8D17A48B7842"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoResult : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

        // ISpRecoResult Methods
        void Slot5(); // void GetResultTimes(out SPRECORESULTTIMES pTimes);
        void GetAlternates(int ulStartElement, int cElements, int ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out int pcPhrasesReturned);
        void GetAudio(uint ulStartElement, uint cElements, out ISpStreamFormat ppStream);
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
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

        // ISpPhraseAlt Methods
        void GetAltInfo(out ISpPhrase ppParent, out uint pulStartElementInParent, out uint pcElementsInParent, out uint pcElementsInAlt);
        void Commit();
    }

    [ComImport, Guid("27CAC6C4-88F2-41f2-8817-0C95E59F1E6E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpRecoResult2 : ISpRecoResult
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

        // ISpRecoResult Methods
        new void Slot5(); // new void GetResultTimes(out SPRECORESULTTIMES pTimes);
        new void GetAlternates(int ulStartElement, int cElements, int ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out int pcPhrasesReturned);
        new void GetAudio(uint ulStartElement, uint cElements, out ISpStreamFormat ppStream);
        new void Slot8(); // void SpeakAudio(UInt32 ulStartElement, UInt32 cElements, UInt32 dwFlags, out UInt32 pulStreamNumber);
        new void Serialize(out IntPtr ppCoMemSerializedResult);
        new void Slot10(); // void ScaleAudio(ref Guid pAudioFormatId, IntPtr pWaveFormatEx);
        new void Slot11(); // void GetRecoContext(out ISpRecoContext ppRecoContext);

        // ISpRecoResult2 Methods
        void CommitAlternate(ISpPhraseAlt pPhraseAlt, out ISpRecoResult ppNewResult);
        void CommitText(uint ulStartElement, uint ulCountOfElements, [MarshalAs(UnmanagedType.LPWStr)] string pszCorrectedData, SPCOMMITFLAGS commitFlags);
        void SetTextFeedback([MarshalAs(UnmanagedType.LPWStr)] string pszFeedback, [MarshalAs(UnmanagedType.Bool)] bool fSuccessful);
    }

    [ComImport, Guid("AE39362B-45A8-4074-9B9E-CCF49AA2D0B6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpXMLRecoResult : ISpRecoResult
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

        // ISpRecoResult Methods
        new void Slot5(); // new void GetResultTimes(out SPRECORESULTTIMES pTimes);
        new void GetAlternates(int ulStartElement, int cElements, int ulRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppPhrases, out int pcPhrasesReturned);
        new void GetAudio(uint ulStartElement, uint cElements, out ISpStreamFormat ppStream);
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
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

        // ISpPhraseEx Methods
        void GetXMLResult([MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemXMLResult, SPXMLRESULTOPTIONS Options);
        void GetXMLErrorInfo(out SPSEMANTICERRORINFO pSemanticErrorInfo);
        void Slot7(); // void GetAudio(UInt32 ulStartElement, UInt32 cElements, out ISpStreamFormat ppStream);
    }

    [ComImport, Guid("C8D7C7E2-0DDE-44b7-AFE3-B0C991FBEB5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpDisplayAlternates
    {
        void GetDisplayAlternates(IntPtr pPhrase, uint cRequestCount, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2), Out] IntPtr[] ppCoMemPhrases, out uint pcPhrasesReturned);
    }

    /// <summary>
    /// Resource Loader interface definition
    /// </summary>
    [ComImport, Guid("B9AC5783-FCD0-4b21-B119-B4F8DA8FD2C3")]
    internal interface ISpGrammarResourceLoader
    {
        /// <summary>
        /// Load some data
        /// </summary>
        [PreserveSig]
        int LoadResource(string bstrResourceUri, bool fAlwaysReload, out IStream pStream, ref string pbstrMIMEType, ref short pfModified, ref string pbstrRedirectUrl);

        /// <summary>
        /// Converts the resourcePath to a location in the file cache and returns a reference into the
        /// cache
        /// </summary>
        string GetLocalCopy(Uri resourcePath, out string mimeType, out Uri redirectUrl);

        /// <summary>
        /// Mark an entry in the file cache as unused.
        /// </summary>
        void ReleaseLocalCopy(string path);
    }

    [ComImport, Guid("88A3342A-0BED-4834-922B-88D43173162F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ISpPhraseBuilder : ISpPhrase
    {
        // ISpPhrase Methods
        new void GetPhrase(out IntPtr ppCoMemPhrase);
        new void GetSerializedPhrase(out IntPtr ppCoMemPhrase);
        new void GetText(uint ulStart, uint ulCount, [MarshalAs(UnmanagedType.Bool)] bool fUseTextReplacements, [MarshalAs(UnmanagedType.LPWStr)] out string ppszCoMemText, out byte pbDisplayAttributes);
        new void Discard(uint dwValueTypes);

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
