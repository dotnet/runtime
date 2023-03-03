// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.ReadyToRunConstants;

namespace ILCompiler.IBC
{
    public class IBCException : Exception
    {
        public IBCException(string message) : base(message) { }
    }

    public static class IBCData
    {
        private static readonly SectionTypeInfo[] s_sectionTypeInfo = ComputeSectionTypeInfos();

        // Methods and types for managing the various stream types

        public enum TokenType
        {
            MetaDataToken,
            MethodToken,
            TypeToken,
            TokenTypeOther
        }

        public struct SectionTypeInfo
        {
            public readonly TokenType TokenType;
            public readonly string Description;

            public SectionTypeInfo(TokenType tokenType, SectionFormat section)
            {
                TokenType = tokenType;
                Description = section.ToString();
            }
        }

        //
        // Class constructor for class IBCData
        //
        private static SectionTypeInfo[] ComputeSectionTypeInfos()
        {
            TokenType tokenType;

            SectionTypeInfo[] sectionTypeInfo = new SectionTypeInfo[(int)SectionFormat.SectionFormatCount];

            tokenType = TokenType.TokenTypeOther;
            sectionTypeInfo[(int)SectionFormat.BasicBlockInfo] = new SectionTypeInfo(tokenType, SectionFormat.BasicBlockInfo);
            sectionTypeInfo[(int)SectionFormat.BlobStream] = new SectionTypeInfo(tokenType, SectionFormat.BlobStream);

            for (SectionFormat section = 0; section < SectionFormat.SectionFormatCount; section++)
            {
                //
                // Initialize tokenType, commonMask and description with their typical values
                //
                tokenType = TokenType.MetaDataToken;

                //
                // Override the typical values of tokenType or commonMask
                // using this switch statement whenever necessary
                //
                switch (section)
                {
                    case SectionFormat.ScenarioInfo:
                    case SectionFormat.BasicBlockInfo:
                    case SectionFormat.BlobStream:
                        tokenType = TokenType.TokenTypeOther;
                        break;

                    case SectionFormat.TypeProfilingData:
                    case SectionFormat.GenericTypeProfilingData:
                        tokenType = TokenType.TypeToken;
                        break;

                    case SectionFormat.MethodProfilingData:
                        tokenType = TokenType.MethodToken;
                        break;
                }

                sectionTypeInfo[(int)section] = new SectionTypeInfo(tokenType, section);
            }

            return sectionTypeInfo;
        }

        public static bool IsTokenList(SectionFormat sectionType)
        {
            return (s_sectionTypeInfo[(int)sectionType].TokenType != TokenType.TokenTypeOther);
        }

        public enum SectionIteratorKind : int
        {
            None,
            All,
            BasicBlocks,
            TokenFlags
        }

        public static IEnumerable<SectionFormat> SectionIterator(SectionIteratorKind iteratorKind)
        {
            switch (iteratorKind)
            {
                case SectionIteratorKind.BasicBlocks:
                    yield return SectionFormat.BasicBlockInfo;
                    yield return SectionFormat.MethodProfilingData;
                    break;

                case SectionIteratorKind.TokenFlags:
                case SectionIteratorKind.All:
                    for (SectionFormat section = 0; section < SectionFormat.SectionFormatCount; section++)
                    {
                        if (IsTokenList(section) || iteratorKind == SectionIteratorKind.All)
                        {
                            yield return section;
                        }
                    }
                    break;
                default:
                    throw new IBCException("Unsupported iteratorKind");
            }
        }
    }

    // Minified files store streams of tokens more efficiently by stripping
    // off the top byte unless it has changed. This class is useful to both
    // the reader and the writer for keeping track of the state necessary
    // to write the next token.
    public class LastTokens
    {
        public uint LastMethodToken = 0x06000000;
        public uint LastBlobToken;
        public uint LastAssemblyToken = 0x23000000;
        public uint LastExternalTypeToken = 0x62000000;
        public uint LastExternalNamespaceToken = 0x61000000;
        public uint LastExternalSignatureToken = 0x63000000;
    }

    public static class Constants
    {
        public const uint HeaderMagicNumber = 0xb1d0f11e;
        public const uint FooterMagicNumber = 0xb4356f98;

        // What is the newest version?
        public const int CurrentMajorVersion = 3;
        public const int CurrentMinorVersion = 0;

        // Unless there is a reason to use a newer format, IBCMerge
        // should write data in this version by default.
        public const int DefaultMajorVersion = 2;
        public const int DefaultMinorVersion = 1;

        // What is the oldest version that can be read?
        public const int CompatibleMajorVersion = 1;
        public const int CompatibleMinorVersion = 0;

        public const int LowestMajorVersionSupportingMinify = 3;

        [Flags]
        public enum FileFlags : uint
        {
            Empty = 0,
            Minified = 1,
            PartialNGen = 2
        }
    }

    public static class Utilities
    {
        public static uint InitialTokenForSection(SectionFormat format)
        {
            return ((uint)format - (uint)CONSTANT.FirstTokenFlagSection) << 24;
        }
    }

    public class ScenarioRunData
    {
        public DateTime RunTime;
        public Guid Mvid;
        public string CommandLine;
        public string SystemInformation;
    }

    public class ScenarioData
    {
        public ScenarioData()
        {
            Runs = new List<ScenarioRunData>();
        }

        public uint Id;
        public uint Mask;
        public uint Priority;
        public string Name;

        public List<ScenarioRunData> Runs;
    }

    public class BasicBlockData
    {
        public uint ILOffset;
        public uint ExecutionCount;
    }

    public class MethodData
    {
        public MethodData()
        {
            BasicBlocks = new List<BasicBlockData>();
        }

        public uint Token;
        public uint ILSize;

        public List<BasicBlockData> BasicBlocks;
    }

    public class TokenData
    {
        public uint Token;
        public uint Flags;
        public uint? ScenarioMask; // Scenario masks aren't stored in minified files.
    }

    public abstract class BlobEntry
    {
        public uint Token;
        public BlobType Type;

        public class PoolEntry : BlobEntry
        {
            public byte[] Data;
        }

        public class SignatureEntry : BlobEntry
        {
            public byte[] Signature;
        }

        public class ExternalNamespaceEntry : BlobEntry
        {
            public byte[] Name;
        }

        public class ExternalTypeEntry : BlobEntry
        {
            public uint AssemblyToken;
            public uint NestedClassToken;
            public uint NamespaceToken;
            public byte[] Name;
        }

        public class ExternalSignatureEntry : BlobEntry
        {
            public byte[] Signature;
        }

        public class ExternalMethodEntry : BlobEntry
        {
            public uint ClassToken;
            public uint SignatureToken;
            public byte[] Name;
        }

        public class UnknownEntry : BlobEntry
        {
            public byte[] Payload;
        }

        public class EndOfStreamEntry : BlobEntry { }
    }

    // Fields are null if the corresponding section did not occur in the file.
    public class AssemblyData
    {
        public AssemblyData()
        {
            // Tokens is special in that it represents more than one section of
            // the file. For that reason it's always initialized.
            Tokens = new Dictionary<SectionFormat, List<TokenData>>();
        }

        public uint FormatMajorVersion;
        public uint FormatMinorVersion;

        public Guid Mvid;
        public bool PartialNGen;
        public uint TotalNumberOfRuns;

        public List<ScenarioData> Scenarios;
        public List<MethodData> Methods;
        public Dictionary<SectionFormat, List<TokenData>> Tokens { get; private set; }
        public List<BlobEntry> BlobStream;
    }

    //
    // Token tags.
    //
    public enum CorTokenType : uint
    {
        mdtModule = 0x00000000,       //
        mdtTypeRef = 0x01000000,       //
        mdtTypeDef = 0x02000000,       //
        mdtFieldDef = 0x04000000,       //
        mdtMethodDef = 0x06000000,       //
        mdtParamDef = 0x08000000,       //
        mdtInterfaceImpl = 0x09000000,       //
        mdtMemberRef = 0x0a000000,       //
        mdtConstant = 0x0b000000,       //
        mdtCustomAttribute = 0x0c000000,       //
        mdtFieldMarshal = 0x0d000000,       //
        mdtPermission = 0x0e000000,       //
        mdtClassLayout = 0x0f000000,       //
        mdtFieldLayout = 0x10000000,       //
        mdtSignature = 0x11000000,       //
        mdtEventMap = 0x12000000,       //
        mdtEvent = 0x14000000,       //
        mdtPropertyMap = 0x15000000,       //
        mdtProperty = 0x17000000,       //
        mdtMethodSemantics = 0x18000000,       //
        mdtMethodImpl = 0x19000000,       //
        mdtModuleRef = 0x1a000000,       //
        mdtTypeSpec = 0x1b000000,       //
        mdtImplMap = 0x1c000000,       //
        mdtFieldRVA = 0x1d000000,       //
        mdtAssembly = 0x20000000,       //
        mdtAssemblyRef = 0x23000000,       //
        mdtFile = 0x26000000,       //
        mdtExportedType = 0x27000000,       //
        mdtManifestResource = 0x28000000,       //
        mdtNestedClass = 0x29000000,       //
        mdtGenericParam = 0x2a000000,       //
        mdtMethodSpec = 0x2b000000,       //
        mdtGenericParamConstraint = 0x2c000000,       //
        mdtLastMetadataTable = mdtGenericParamConstraint,

        ibcExternalNamespace = 0x61000000,
        ibcExternalType = 0x62000000,
        ibcExternalSignature = 0x63000000,
        ibcExternalMethod = 0x64000000,

        ibcTypeSpec = 0x68000000,
        ibcMethodSpec = 0x69000000,

        mdtString = 0x70000000,       //
        mdtName = 0x71000000,       //
        mdtBaseType = 0x72000000,       // Leave this on the high end value. This does not  correspond to metadata table
    }

    public static class Cor
    {
        public static class Macros
        {
            //
            // Build / decompose tokens.
            //
            public static uint RidToToken(uint rid, CorTokenType tktype) { return (((uint)rid) | ((uint)tktype)); }
            public static uint TokenFromRid(uint rid, CorTokenType tktype) { return (((uint)rid) | ((uint)tktype)); }
            public static uint RidFromToken(uint tk) { return (uint)(((uint)tk) & 0x00ffffff); }
            public static CorTokenType TypeFromToken(uint tk) { return ((CorTokenType)(((uint)tk) & 0xff000000)); }
            public static bool IsNilToken(uint tk) { return ((RidFromToken(tk)) == 0); }
        }
    }

    public static class Macros
    {
        // Macros for testing MethodSigFlags
        public static bool IsInstantiationNeeded(uint flags)
        { return (flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_MethodInstantiation) != 0; }
        public static bool IsSlotUsedInsteadOfToken(uint flags)
        { return (flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_SlotInsteadOfToken) != 0; }
        public static bool IsUnboxingStub(uint flags)
        { return (flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_UnboxingStub) != 0; }
        public static bool IsInstantiatingStub(uint flags)
        { return (flags & (uint)ReadyToRunMethodSigFlags.READYTORUN_METHOD_SIG_InstantiatingStub) != 0; }
    }



    public enum SectionFormat : int
    {
        ScenarioInfo = 0,
        BasicBlockInfo = 1,
        BlobStream = 2,

        ModuleProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtModule >> 24),
        TypeRefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtTypeRef >> 24),
        TypeProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtTypeDef >> 24),
        FieldDefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtFieldDef >> 24),
        MethodProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtMethodDef >> 24),
        ParamDefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtParamDef >> 24),
        InterfaceImplProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtInterfaceImpl >> 24),
        MemberRefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtMemberRef >> 24),
        ConstantProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtConstant >> 24),
        CustomAttributeProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtCustomAttribute >> 24),
        FieldMarshalProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtFieldMarshal >> 24),
        PermissionProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtPermission >> 24),
        ClassLayoutProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtClassLayout >> 24),
        FieldLayoutProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtFieldLayout >> 24),
        SignatureProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtSignature >> 24),
        EventMapProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtEventMap >> 24),
        EventProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtEvent >> 24),
        PropertyMapProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtPropertyMap >> 24),
        PropertyProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtProperty >> 24),
        MethodSemanticsProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtMethodSemantics >> 24),
        MethodImplProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtMethodImpl >> 24),
        ModuleRefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtModuleRef >> 24),
        TypeSpecProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtTypeSpec >> 24),
        ImplMapProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtImplMap >> 24),
        FieldRVAProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtFieldRVA >> 24),
        AssemblyProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtAssembly >> 24),
        AssemblyRefProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtAssemblyRef >> 24),
        FileProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtFile >> 24),
        ExportedTypeProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtExportedType >> 24),
        ManifestResourceProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtManifestResource >> 24),
        NestedClassProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtNestedClass >> 24),
        GenericParamProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtGenericParam >> 24),
        MethodSpecProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtMethodSpec >> 24),
        GenericParamConstraintProfilingData = CONSTANT.FirstTokenFlagSection + ((int)CorTokenType.mdtGenericParamConstraint >> 24),

        StringPoolProfilingData,
        GuidPoolProfilingData,
        BlobPoolProfilingData,
        UserStringPoolProfilingData,

        GenericTypeProfilingData = 63,    // 0x3f
        SectionFormatCount = 64,    // 0x40

        SectionFormatInvalid = -1,
    }

    public enum BlobType : int
    {
        // IMPORTANT: Keep the first four enums together in the same order and at the
        //  very beginning of this enum.  See MetaModelPub.h for the order

        MetadataStringPool = 0,
        MetadataGuidPool = 1,
        MetadataBlobPool = 2,
        MetadataUserStringPool = 3,

        FirstMetadataPool = MetadataStringPool,
        LastMetadataPool = MetadataUserStringPool,

        ParamTypeSpec = 4,    // Instantiated Type Signature
        ParamMethodSpec = 5,    // Instantiated Method Signature
        ExternalNamespaceDef = 6,    // External Namespace Token Definition
        ExternalTypeDef = 7,    // External Type Token Definition
        ExternalSignatureDef = 8,    // External Signature Definition
        ExternalMethodDef = 9,    // External Method Token Definition

        IllegalBlob = 10,    // Failed to allocate the blob

        EndOfBlobStream = -1
    }

    public enum ModuleId : uint
    {
        CurrentModule = 0,    // Tokens are encoded/decoded using current modules metadata
        ExternalModule = 1,    // Tokens are (or will be) encoded/decoded using ibcExternalType and ibcExternalMethod
    }

    internal static class CONSTANT
    {
        public const SectionFormat FirstTokenFlagSection = SectionFormat.BlobStream + 1;
    }
}
