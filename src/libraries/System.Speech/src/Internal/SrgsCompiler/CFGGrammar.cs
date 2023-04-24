// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Speech.Internal.SrgsParser;

namespace System.Speech.Internal.SrgsCompiler
{
    internal sealed class CfgGrammar
    {
        #region Constructors

        internal CfgGrammar()
        {
        }

        #endregion

        #region Internal Types

        // Preprocess CFG header file
        internal struct CfgHeader
        {
            internal Guid FormatId;

            internal Guid GrammarGUID;

            internal ushort langId;

            internal ushort pszGlobalTags;

            internal int cArcsInLargestState;

            internal StringBlob pszWords;

            internal StringBlob pszSymbols;

            internal CfgRule[] rules;

            internal CfgArc[] arcs;

            internal float[] weights;

            internal CfgSemanticTag[] tags;

            internal CfgScriptRef[] scripts;

            internal uint ulRootRuleIndex;

            internal GrammarOptions GrammarOptions;

            internal GrammarType GrammarMode;

            internal string BasePath;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class CfgSerializedHeader
        {
            internal CfgSerializedHeader()
            {
            }

#pragma warning disable 56518 // The Binary reader cannot be disposed or it would close the underlying stream

            // Initializes a CfgSerializedHeader from a Stream.
            // If the data does not represent a cfg then UnsupportedFormatException is thrown.
            // This isn't a conclusive validity check, but is enough to determine if it's a CFG or not.
            // For a complete check CheckValidCfgFormat is used.
            internal CfgSerializedHeader(Stream stream)
            {
                BinaryReader br = new(stream);
                ulTotalSerializedSize = br.ReadUInt32();
                if (ulTotalSerializedSize < SP_SPCFGSERIALIZEDHEADER_500 || ulTotalSerializedSize > int.MaxValue)
                {
                    // Size is either negative or too small.
                    XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
                }

                FormatId = new Guid(br.ReadBytes(16));
                if (FormatId != CfgGrammar._SPGDF_ContextFree)
                {
                    // Not of cfg format
                    XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
                }

                GrammarGUID = new Guid(br.ReadBytes(16));
                LangID = br.ReadUInt16();
                pszSemanticInterpretationGlobals = br.ReadUInt16();
                cArcsInLargestState = br.ReadInt32();
                cchWords = br.ReadInt32();
                cWords = br.ReadInt32();
                pszWords = br.ReadUInt32();
                if (pszWords < SP_SPCFGSERIALIZEDHEADER_500 || pszWords > ulTotalSerializedSize)
                {
                    // First data points before or before valid range.
                    XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
                }

                cchSymbols = br.ReadInt32();
                pszSymbols = br.ReadUInt32();
                cRules = br.ReadInt32();
                pRules = br.ReadUInt32();
                cArcs = br.ReadInt32();
                pArcs = br.ReadUInt32();
                pWeights = br.ReadUInt32();
                cTags = br.ReadInt32();
                tags = br.ReadUInt32();
                ulReserved1 = br.ReadUInt32();
                ulReserved2 = br.ReadUInt32();

                if (pszWords > SP_SPCFGSERIALIZEDHEADER_500)
                {
                    cScripts = br.ReadInt32();
                    pScripts = br.ReadUInt32();
                    cIL = br.ReadInt32();
                    pIL = br.ReadUInt32();
                    cPDB = br.ReadInt32();
                    pPDB = br.ReadUInt32();
                    ulRootRuleIndex = br.ReadUInt32();
                    GrammarOptions = (GrammarOptions)br.ReadUInt32();
                    cBasePath = br.ReadUInt32();
                    GrammarMode = br.ReadUInt32();
                    ulReserved3 = br.ReadUInt32();
                    ulReserved4 = br.ReadUInt32();
                }
                // Else SAPI 5.0 syntax grammar - parameters set to zero
            }
            internal static bool IsCfg(Stream stream, out int cfgLength)
            {
                cfgLength = 0;
                BinaryReader br = new(stream);
                uint ulTotalSerializedSize = br.ReadUInt32();
                if (ulTotalSerializedSize < SP_SPCFGSERIALIZEDHEADER_500 || ulTotalSerializedSize > int.MaxValue)
                {
                    // Size is either negative or too small.
                    return false;
                }

                Guid formatId = new(br.ReadBytes(16));
                if (formatId != CfgGrammar._SPGDF_ContextFree)
                {
                    // Not of cfg format
                    return false;
                }

                cfgLength = (int)ulTotalSerializedSize;
                return true;
            }

#pragma warning restore 56518 // The Binary reader cannot be disposed or it would close the underlying stream

            internal uint ulTotalSerializedSize;

            internal Guid FormatId;

            internal Guid GrammarGUID;

            internal ushort LangID;

            internal ushort pszSemanticInterpretationGlobals;

            internal int cArcsInLargestState;

            internal int cchWords;

            internal int cWords;

            internal uint pszWords;

            internal int cchSymbols;

            internal uint pszSymbols;

            internal int cRules;

            internal uint pRules;

            internal int cArcs;

            internal uint pArcs;

            internal uint pWeights;

            internal int cTags;

            internal uint tags;

            internal uint ulReserved1;

            internal uint ulReserved2;

            internal int cScripts;

            internal uint pScripts;

            internal int cIL;

            internal uint pIL;

            internal int cPDB;

            internal uint pPDB;

            internal uint ulRootRuleIndex;

            internal GrammarOptions GrammarOptions;

            internal uint cBasePath;

            internal uint GrammarMode;

            internal uint ulReserved3;

            internal uint ulReserved4;
        }

        internal class CfgProperty
        {
            internal string _pszName;

            internal uint _ulId;
#pragma warning disable 0618 // VarEnum is obsolete
            internal VarEnum _comType;
#pragma warning restore 0618
            internal object _comValue;
        }

        #endregion

        #region Internal Methods

        //
        //  This helper converts a serialized CFG grammar header into an in-memory header
        //
        internal static CfgHeader ConvertCfgHeader(StreamMarshaler streamHelper)
        {
            CfgSerializedHeader cfgSerializedHeader = null;
            return ConvertCfgHeader(streamHelper, true, true, out cfgSerializedHeader);
        }

        internal static CfgHeader ConvertCfgHeader(StreamMarshaler streamHelper, bool includeAllGrammarData, bool loadSymbols, out CfgSerializedHeader cfgSerializedHeader)
        {
            cfgSerializedHeader = new CfgSerializedHeader(streamHelper.Stream);

            //
            //  Because in 64-bit code, pointers != sizeof(ULONG) we copy each member explicitly.
            //

            CfgHeader header = new();
            header.FormatId = cfgSerializedHeader.FormatId;
            header.GrammarGUID = cfgSerializedHeader.GrammarGUID;
            header.langId = cfgSerializedHeader.LangID;
            header.pszGlobalTags = cfgSerializedHeader.pszSemanticInterpretationGlobals;
            header.cArcsInLargestState = cfgSerializedHeader.cArcsInLargestState;

            // read all the common fields
            header.rules = Load<CfgRule>(streamHelper, cfgSerializedHeader.pRules, cfgSerializedHeader.cRules);

            if (includeAllGrammarData || loadSymbols)
            {
                header.pszSymbols = LoadStringBlob(streamHelper, cfgSerializedHeader.pszSymbols, cfgSerializedHeader.cchSymbols);
            }

            if (includeAllGrammarData)
            {
                header.pszWords = LoadStringBlob(streamHelper, cfgSerializedHeader.pszWords, cfgSerializedHeader.cchWords);
                header.arcs = Load<CfgArc>(streamHelper, cfgSerializedHeader.pArcs, cfgSerializedHeader.cArcs);
                header.tags = Load<CfgSemanticTag>(streamHelper, cfgSerializedHeader.tags, cfgSerializedHeader.cTags);
                header.weights = Load<float>(streamHelper, cfgSerializedHeader.pWeights, cfgSerializedHeader.cArcs);
            }

            //We know that in SAPI 5.0 grammar format pszWords follows header immediately.
            if (cfgSerializedHeader.pszWords < Marshal.SizeOf<CfgSerializedHeader>())
            {
                //This is SAPI 5.0 and SAPI 5.1 grammar format
                header.ulRootRuleIndex = 0xFFFFFFFF;
                header.GrammarOptions = GrammarOptions.KeyValuePairs;
                header.BasePath = null;
                header.GrammarMode = GrammarType.VoiceGrammar;
            }
            else
            {
                //This is SAPI 5.2 and beyond grammar format
                header.ulRootRuleIndex = cfgSerializedHeader.ulRootRuleIndex;
                header.GrammarOptions = cfgSerializedHeader.GrammarOptions;
                header.GrammarMode = (GrammarType)cfgSerializedHeader.GrammarMode;
                if (includeAllGrammarData)
                {
                    header.scripts = Load<CfgScriptRef>(streamHelper, cfgSerializedHeader.pScripts, cfgSerializedHeader.cScripts);
                }
                // The BasePath string is written after the rules - no offset is provided
                // Get the chars and build the string
                if (cfgSerializedHeader.cBasePath > 0)
                {
                    streamHelper.Stream.Position = (int)cfgSerializedHeader.pRules + (header.rules.Length * Marshal.SizeOf<CfgRule>());
                    header.BasePath = streamHelper.ReadNullTerminatedString();
                }
            }

            // Check the content - should be valid for both SAPI 5.0 and SAPI 5.2 grammars
            CheckValidCfgFormat(cfgSerializedHeader, header, includeAllGrammarData);

            return header;
        }

        //
        //  This helper converts a serialized CFG grammar header into an in-memory header
        //
        internal static ScriptRef[] LoadScriptRefs(StreamMarshaler streamHelper, CfgSerializedHeader pFH)
        {
            //
            //  Because in 64-bit code, pointers != sizeof(ULONG) we copy each member explicitly.
            //
            if (pFH.FormatId != CfgGrammar._SPGDF_ContextFree)
            {
                return null;
            }

            //We know that in SAPI 5.0 grammar format pszWords follows header immediately.
            if (pFH.pszWords < Marshal.SizeOf<CfgSerializedHeader>())
            {
                // Must be SAPI 6.0 or above to hold a .NET script
                return null;
            }

            // Get the symbols
            StringBlob symbols = LoadStringBlob(streamHelper, pFH.pszSymbols, pFH.cchSymbols);

            // Get the script refs
            CfgScriptRef[] cfgScripts = Load<CfgScriptRef>(streamHelper, pFH.pScripts, pFH.cScripts);

            // Convert the CFG script reference to ScriptRef
            ScriptRef[] scripts = new ScriptRef[cfgScripts.Length];
            for (int i = 0; i < cfgScripts.Length; i++)
            {
                CfgScriptRef cfgScript = cfgScripts[i];
                scripts[i] = new ScriptRef(symbols[cfgScript._idRule], symbols[cfgScript._idMethod], cfgScript._method);
            }

            return scripts;
        }

        internal static ScriptRef[] LoadIL(Stream stream)
        {
            using (StreamMarshaler streamHelper = new(stream))
            {
                CfgSerializedHeader pFH = new();

                streamHelper.ReadStream(pFH);

                return LoadScriptRefs(streamHelper, pFH);
            }
        }

        internal static bool LoadIL(Stream stream, out byte[] assemblyContent, out byte[] assemblyDebugSymbols, out ScriptRef[] scripts)
        {
            assemblyContent = assemblyDebugSymbols = null;
            scripts = null;

            using (StreamMarshaler streamHelper = new(stream))
            {
                CfgSerializedHeader pFH = new();

                streamHelper.ReadStream(pFH);

                scripts = LoadScriptRefs(streamHelper, pFH);
                if (scripts == null)
                {
                    return false;
                }

                // Return if no script
                if (pFH.cIL == 0)
                {
                    return false;
                }

                // Get the assembly content
                assemblyContent = Load<byte>(streamHelper, pFH.pIL, pFH.cIL);

                assemblyDebugSymbols = pFH.cPDB > 0 ? Load<byte>(streamHelper, pFH.pPDB, pFH.cPDB) : null;
            }

            return true;
        }

        #endregion

        #region Private Methods

        private static void CheckValidCfgFormat(CfgSerializedHeader pFH, CfgHeader header, bool includeAllGrammarData)
        {
            //See backend commit method to understand the layout of cfg format
            if (pFH.pszWords < SP_SPCFGSERIALIZEDHEADER_500)
            {
                XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
            }

            int ullStartOffset = (int)pFH.pszWords;

            //Check the word offset
            //See stringblob implementation. pFH.cchWords * sizeof(WCHAR) isn't exactly the serialized size, but it is close and must be less than the serialized size
            CheckSetOffsets(pFH.pszWords, pFH.cchWords * Helpers._sizeOfChar, ref ullStartOffset, pFH.ulTotalSerializedSize);

            //Check the symbol offset
            //symbol is right after word
            //pFH.pszSymbols is very close to pFH.pszWords + pFH.cchWords * sizeof(WCHAR)
            CheckSetOffsets(pFH.pszSymbols, pFH.cchSymbols * Helpers._sizeOfChar, ref ullStartOffset, pFH.ulTotalSerializedSize);

            //Check the rule offset
            if (pFH.cRules > 0)
            {
                CheckSetOffsets(pFH.pRules, pFH.cRules * Marshal.SizeOf<CfgRule>(), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }

            //Check the arc offset
            if (pFH.cArcs > 0)
            {
                CheckSetOffsets(pFH.pArcs, pFH.cArcs * Marshal.SizeOf<CfgArc>(), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }

            //Check the weight offset
            if (pFH.pWeights > 0)
            {
                CheckSetOffsets(pFH.pWeights, pFH.cArcs * sizeof(float), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }

            //Check the semantic tag offset
            if (pFH.cTags > 0)
            {
                CheckSetOffsets(pFH.tags, pFH.cTags * Marshal.SizeOf<CfgSemanticTag>(), ref ullStartOffset, pFH.ulTotalSerializedSize);

                if (includeAllGrammarData)
                {
                    //Validate the SPCFGSEMANTICTAG array pointed to by tags
                    //We use header for easy array access
                    //The first arc is dummy, so the start and end arcindex for semantic tag won't be zero
                    for (int i = 0; i < header.tags.Length; i++)
                    {
                        int startArc = (int)header.tags[i].StartArcIndex;
                        int endArc = (int)header.tags[i].EndArcIndex;
                        int cArcs = header.arcs.Length;
#pragma warning disable 0618 // VarEnum is obsolete
                        if (startArc == 0 ||
                             startArc >= cArcs ||
                             endArc == 0 ||
                             endArc >= cArcs ||
                             (
                                header.tags[i].PropVariantType != VarEnum.VT_EMPTY &&
                                header.tags[i].PropVariantType != VarEnum.VT_BSTR &&
                                header.tags[i].PropVariantType != VarEnum.VT_BOOL &&
                                header.tags[i].PropVariantType != VarEnum.VT_R8 &&
                                header.tags[i].PropVariantType != VarEnum.VT_I4)
                             )
                        {
                            XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
                        }
#pragma warning restore 0618
                    }
                }
            }

            //Check the offset for the scripts
            if (pFH.cScripts > 0)
            {
                CheckSetOffsets(pFH.pScripts, pFH.cScripts * Marshal.SizeOf<CfgScriptRef>(), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }

            if (pFH.cIL > 0)
            {
                CheckSetOffsets(pFH.pIL, pFH.cIL * sizeof(byte), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }

            if (pFH.cPDB > 0)
            {
                CheckSetOffsets(pFH.pPDB, pFH.cPDB * sizeof(byte), ref ullStartOffset, pFH.ulTotalSerializedSize);
            }
        }

        private static void CheckSetOffsets(uint offset, int size, ref int start, uint max)
        {
            if (offset < (uint)start ||
 (start = (int)offset + size) > (int)max)
            {
                XmlParser.ThrowSrgsException(SRID.UnsupportedFormat);
            }
        }

        private static StringBlob LoadStringBlob(StreamMarshaler streamHelper, uint iPos, int c)
        {
            char[] ach = new char[c];

            streamHelper.Position = iPos;
            streamHelper.ReadArrayChar(ach, c);

            return new StringBlob(ach);
        }

        private static T[] Load<T>(StreamMarshaler streamHelper, uint iPos, int c)
        {

            T[] t = null;

            t = new T[c];

            if (c > 0)
            {
                streamHelper.Position = iPos;
                streamHelper.ReadArray<T>(t, c);
            }

            return t;
        }

        #endregion

        #region Internal Properties

        internal static uint NextHandle
        {
            get
            {
                return ++s_lastHandle;
            }
        }

        #endregion

        #region Internal Fields

        internal static Guid _SPGDF_ContextFree = new(0x4ddc926d, 0x6ce7, 0x4dc0, 0x99, 0xa7, 0xaf, 0x9e, 0x6b, 0x6a, 0x4e, 0x91);

        //
        internal const int INFINITE = unchecked((int)0xffffffff);

        // INFINITE
        //
        internal static readonly Rule SPRULETRANS_TEXTBUFFER = new(-1);

        internal static readonly Rule SPRULETRANS_WILDCARD = new(-2);

        internal static readonly Rule SPRULETRANS_DICTATION = new(-3);

        //
        internal const int SPTEXTBUFFERTRANSITION = 0x3fffff;

        internal const int SPWILDCARDTRANSITION = 0x3ffffe;

        internal const int SPDICTATIONTRANSITION = 0x3ffffd;

        internal const int MAX_TRANSITIONS_COUNT = 256;

        internal const float DEFAULT_WEIGHT = 1f;

        //
        internal const int SP_LOW_CONFIDENCE = -1;

        internal const int SP_NORMAL_CONFIDENCE = 0;

        internal const int SP_HIGH_CONFIDENCE = +1;

        #endregion

        #region Private Fields

        private const int SP_SPCFGSERIALIZEDHEADER_500 = 100;

        private static uint s_lastHandle;

        #endregion
    }
}
