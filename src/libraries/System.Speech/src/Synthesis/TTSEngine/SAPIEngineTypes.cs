// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Speech.Synthesis.TtsEngine
{
    #region Public Enums

    [ComImport, Guid("A74D7C8E-4CC5-4F2F-A6EB-804DEE18500E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface ITtsEngine
    {
        [PreserveSig]
        void Speak(SPEAKFLAGS dwSpeakFlags, ref Guid rguidFormatId, IntPtr pWaveFormatEx, IntPtr pTextFragList, IntPtr pOutputSite);
        [PreserveSig]
        void GetOutputFormat(ref Guid pTargetFmtId, IntPtr pTargetWaveFormatEx, out Guid pOutputFormatId, out IntPtr ppCoMemOutputWaveFormatEx);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class SPVTEXTFRAG
    {
        public IntPtr pNext;
        public SPVSTATE State;
        public IntPtr pTextStart;
        public int ulTextLen;
        public int ulTextSrcOffset;

        // must be the last element, it is passed to the TTS engine that
        // does not see these fields
        public GCHandle gcText;
        public GCHandle gcNext;
        public GCHandle gcPhoneme;
        public GCHandle gcSayAsCategory;
    }

    [ComConversionLossAttribute]
    [TypeLibTypeAttribute(16)]
    internal struct SPVSTATE
    {
        //--- Action
        public SPVACTIONS eAction;

        //--- Running state values
        public short LangID;
        public short wReserved;
        public int EmphAdj;
        public int RateAdj;
        public int Volume;
        public SPVPITCH PitchAdj;
        public int SilenceMSecs;
        public IntPtr pPhoneIds;
        public SPPARTOFSPEECH ePartOfSpeech;
        public SPVCONTEXT Context;
    }

    [System.Runtime.InteropServices.TypeLibTypeAttribute(16)]
    internal struct SPVCONTEXT
    {
        public IntPtr pCategory;
        public IntPtr pBefore;
        public IntPtr pAfter;
    }

    [System.Runtime.InteropServices.TypeLibTypeAttribute(16)]
    internal struct SPVPITCH
    {
        public int MiddleAdj;
        public int RangeAdj;
    }

    internal static class SAPIGuids
    {
        internal static readonly Guid SPDFID_WaveFormatEx = new("C31ADBAE-527F-4ff5-A230-F62BB61FF70C");
    }

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

    [Flags]
    internal enum SPVESACTIONS
    {
        SPVES_CONTINUE = 0,
        SPVES_ABORT = 1,
        SPVES_SKIP = 2,
        SPVES_RATE = 4,
        SPVES_VOLUME = 8
    }

    [System.Runtime.InteropServices.TypeLibTypeAttribute(16)]
    internal enum SPVACTIONS
    {
        SPVA_Speak = 0,
        SPVA_Silence = 1,
        SPVA_Pronounce = 2,
        SPVA_Bookmark = 3,
        SPVA_SpellOut = 4,
        SPVA_Section = 5,
        SPVA_ParseUnknownTag = 6,
    }

    [System.Runtime.InteropServices.TypeLibTypeAttribute(16)]
    internal enum SPPARTOFSPEECH
    {
        //--- SAPI5 public POS category values (bits 28-31)
        SPPS_NotOverridden = -1,
        SPPS_Unknown = 0,
        SPPS_Noun = 0x1000,
        SPPS_Verb = 0x2000,
        SPPS_Modifier = 0x3000,
        SPPS_Function = 0x4000,
        SPPS_Interjection = 0x5000,
        SPPS_SuppressWord = 0xF000,    // Special flag to indicate this word should not be recognized
    }

    #endregion
}
