// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    [Flags]
    private enum CaTargets
    {
        None = 0,
        TypeDef = 1 << 0,
        TypeRef = 1 << 1,
        MethodDef = 1 << 2,
        FieldDef = 1 << 3,
        ParamDef = 1 << 4,
        Property = 1 << 5,
        Event = 1 << 6,
        Module = 1 << 7,
        Assembly = 1 << 8,
    }

    private enum KnownAttributeKind
    {
        DllImport,
        Guid,
        ComImport,
        InterfaceType,
        ClassInterface,
        Serializable,
        NonSerialized,
        MethodImpl1,
        MethodImpl2,
        MethodImpl3,
        MarshalAs1,
        MarshalAs2,
        PreserveSig,
        In,
        Out,
        Optional,
        StructLayout1,
        StructLayout2,
        FieldOffset,
        TypeLibVersion,
        ComCompatibleVersion,
        SpecialName,
        AllowPartiallyTrustedCallers,
        WindowsRuntimeImport,
    }

    private sealed record NamedArgument(
        string Name,
        SerializationTypeCode Type,
        SerializationTypeCode EnumType = SerializationTypeCode.Invalid,
        string EnumName = "",
        SerializationTypeCode ArrayType = SerializationTypeCode.Invalid);

    private sealed record KnownAttribute(
        KnownAttributeKind Kind,
        string Namespace,
        string Name,
        CaTargets Targets,
        bool KeepAttribute = false,
        SerializationTypeCode[]? FixedArgumentTypes = null,
        NamedArgument[]? NamedArgumentDescriptors = null,
        bool MatchBySignature = false)
    {
        public SerializationTypeCode[] FixedArguments { get; } = FixedArgumentTypes ?? [];
        public NamedArgument[] NamedArguments { get; } = NamedArgumentDescriptors ?? [];
    }

    private const string InteropNamespace = "System.Runtime.InteropServices";
    private const string CompilerServicesNamespace = "System.Runtime.CompilerServices";

    private static NamedArgument Enum4(string name, string enumName) =>
        new(name, SerializationTypeCode.Enum, SerializationTypeCode.Int32, enumName);

    private const string DllImportCallingConvention = "CallingConvention";
    private const string DllImportCharSet = "CharSet";
    private const string DllImportEntryPoint = "EntryPoint";
    private const string DllImportExactSpelling = "ExactSpelling";
    private const string DllImportSetLastError = "SetLastError";
    private const string DllImportPreserveSig = "PreserveSig";
    private const string DllImportBestFitMapping = "BestFitMapping";
    private const string DllImportThrowOnUnmappableChar = "ThrowOnUnmappableChar";

    private const string MethodImplCodeType = "MethodCodeType";

    private const string MarshalArraySubType = "ArraySubType";
    private const string MarshalSafeArraySubType = "SafeArraySubType";
    private const string MarshalSafeArrayUserDefinedSubType = "SafeArrayUserDefinedSubType";
    private const string MarshalSizeParamIndex = "SizeParamIndex";
    private const string MarshalSizeConst = "SizeConst";
    private const string MarshalType = "MarshalType";
    private const string MarshalTypeRef = "MarshalTypeRef";
    private const string MarshalCookie = "MarshalCookie";
    private const string MarshalIidParameterIndex = "IidParameterIndex";

    private const string StructLayoutPack = "Pack";
    private const string StructLayoutSize = "Size";
    private const string StructLayoutCharSet = "CharSet";

    private static readonly NamedArgument[] s_dllImportNamedArguments =
    [
        Enum4(DllImportCallingConvention, InteropNamespace + ".CallingConvention"),
        Enum4(DllImportCharSet, InteropNamespace + ".CharSet"),
        new(DllImportEntryPoint, SerializationTypeCode.String),
        new(DllImportExactSpelling, SerializationTypeCode.Boolean),
        new(DllImportSetLastError, SerializationTypeCode.Boolean),
        new(DllImportPreserveSig, SerializationTypeCode.Boolean),
        new(DllImportBestFitMapping, SerializationTypeCode.Boolean),
        new(DllImportThrowOnUnmappableChar, SerializationTypeCode.Boolean),
    ];

    private static readonly NamedArgument[] s_methodImplNamedArguments =
    [
        Enum4(MethodImplCodeType, CompilerServicesNamespace + ".MethodCodeType"),
    ];

    private static readonly NamedArgument[] s_marshalAsNamedArguments =
    [
        Enum4(MarshalArraySubType, InteropNamespace + ".UnmanagedType"),
        Enum4(MarshalSafeArraySubType, InteropNamespace + ".VarEnum"),
        new(MarshalSafeArrayUserDefinedSubType, SerializationTypeCode.Type),
        new(MarshalSizeParamIndex, SerializationTypeCode.Int16),
        new(MarshalSizeConst, SerializationTypeCode.Int32),
        new(MarshalType, SerializationTypeCode.String),
        new(MarshalTypeRef, SerializationTypeCode.Type),
        new(MarshalCookie, SerializationTypeCode.String),
        new(MarshalIidParameterIndex, SerializationTypeCode.Int32),
    ];

    private static readonly NamedArgument[] s_structLayoutNamedArguments =
    [
        new(StructLayoutPack, SerializationTypeCode.Int32),
        new(StructLayoutSize, SerializationTypeCode.Int32),
        Enum4(StructLayoutCharSet, InteropNamespace + ".CharSet"),
    ];

    private const CaTargets MarshalTargets = CaTargets.FieldDef | CaTargets.ParamDef | CaTargets.Property;
    private const CaTargets InOutTargets = CaTargets.ParamDef;
    private const CaTargets SpecialNameTargets =
        CaTargets.TypeDef | CaTargets.MethodDef | CaTargets.FieldDef | CaTargets.Property | CaTargets.Event;

    /// <summary>
    /// The known attributes, in the same order as the native <c>KnownCaList</c>. The order matters:
    /// overloads that match by signature are tested before the match-by-name fallback overload.
    /// </summary>
    private static readonly KnownAttribute[] s_knownAttributes =
    [
        new(KnownAttributeKind.DllImport, InteropNamespace, "DllImportAttribute", CaTargets.MethodDef,
            FixedArgumentTypes: [SerializationTypeCode.String],
            NamedArgumentDescriptors: s_dllImportNamedArguments),

        new(KnownAttributeKind.Guid, InteropNamespace, "GuidAttribute",
            CaTargets.TypeDef | CaTargets.TypeRef | CaTargets.Module | CaTargets.Assembly,
            KeepAttribute: true,
            FixedArgumentTypes: [SerializationTypeCode.String]),

        new(KnownAttributeKind.ComImport, InteropNamespace, "ComImportAttribute", CaTargets.TypeDef),

        new(KnownAttributeKind.InterfaceType, InteropNamespace, "InterfaceTypeAttribute", CaTargets.TypeDef,
            KeepAttribute: true,
            FixedArgumentTypes: [SerializationTypeCode.UInt16]),

        new(KnownAttributeKind.ClassInterface, InteropNamespace, "ClassInterfaceAttribute",
            CaTargets.TypeDef | CaTargets.Assembly | CaTargets.TypeRef,
            KeepAttribute: true,
            FixedArgumentTypes: [SerializationTypeCode.UInt16]),

        new(KnownAttributeKind.Serializable, "System", "SerializableAttribute", CaTargets.TypeDef),

        new(KnownAttributeKind.NonSerialized, "System", "NonSerializedAttribute", CaTargets.FieldDef),

        new(KnownAttributeKind.MethodImpl1, CompilerServicesNamespace, "MethodImplAttribute", CaTargets.MethodDef,
            NamedArgumentDescriptors: s_methodImplNamedArguments,
            MatchBySignature: true),

        new(KnownAttributeKind.MethodImpl2, CompilerServicesNamespace, "MethodImplAttribute", CaTargets.MethodDef,
            FixedArgumentTypes: [SerializationTypeCode.Int16],
            NamedArgumentDescriptors: s_methodImplNamedArguments,
            MatchBySignature: true),

        new(KnownAttributeKind.MethodImpl3, CompilerServicesNamespace, "MethodImplAttribute", CaTargets.MethodDef,
            FixedArgumentTypes: [SerializationTypeCode.UInt32],
            NamedArgumentDescriptors: s_methodImplNamedArguments),

        new(KnownAttributeKind.MarshalAs1, InteropNamespace, "MarshalAsAttribute", MarshalTargets,
            FixedArgumentTypes: [SerializationTypeCode.Int16],
            NamedArgumentDescriptors: s_marshalAsNamedArguments,
            MatchBySignature: true),

        new(KnownAttributeKind.MarshalAs2, InteropNamespace, "MarshalAsAttribute", MarshalTargets,
            FixedArgumentTypes: [SerializationTypeCode.UInt32],
            NamedArgumentDescriptors: s_marshalAsNamedArguments),

        new(KnownAttributeKind.PreserveSig, InteropNamespace, "PreserveSigAttribute", CaTargets.MethodDef),

        new(KnownAttributeKind.In, InteropNamespace, "InAttribute", InOutTargets),

        new(KnownAttributeKind.Out, InteropNamespace, "OutAttribute", InOutTargets),

        new(KnownAttributeKind.Optional, InteropNamespace, "OptionalAttribute", InOutTargets),

        new(KnownAttributeKind.StructLayout1, InteropNamespace, "StructLayoutAttribute", CaTargets.TypeDef,
            FixedArgumentTypes: [SerializationTypeCode.Int16],
            NamedArgumentDescriptors: s_structLayoutNamedArguments,
            MatchBySignature: true),

        new(KnownAttributeKind.StructLayout2, InteropNamespace, "StructLayoutAttribute", CaTargets.TypeDef,
            FixedArgumentTypes: [SerializationTypeCode.Int32],
            NamedArgumentDescriptors: s_structLayoutNamedArguments),

        new(KnownAttributeKind.FieldOffset, InteropNamespace, "FieldOffsetAttribute", CaTargets.FieldDef,
            FixedArgumentTypes: [SerializationTypeCode.UInt32]),

        new(KnownAttributeKind.TypeLibVersion, InteropNamespace, "TypeLibVersionAttribute",
            CaTargets.Assembly | CaTargets.TypeRef,
            KeepAttribute: true,
            FixedArgumentTypes: [SerializationTypeCode.Int32, SerializationTypeCode.Int32]),

        new(KnownAttributeKind.ComCompatibleVersion, InteropNamespace, "ComCompatibleVersionAttribute",
            CaTargets.Assembly | CaTargets.TypeRef,
            KeepAttribute: true,
            FixedArgumentTypes:
            [
                SerializationTypeCode.Int32,
                SerializationTypeCode.Int32,
                SerializationTypeCode.Int32,
                SerializationTypeCode.Int32,
            ]),

        new(KnownAttributeKind.SpecialName, CompilerServicesNamespace, "SpecialNameAttribute", SpecialNameTargets),

        new(KnownAttributeKind.AllowPartiallyTrustedCallers, "System.Security", "AllowPartiallyTrustedCallersAttribute",
            CaTargets.Assembly | CaTargets.TypeRef,
            KeepAttribute: true),

        new(KnownAttributeKind.WindowsRuntimeImport, InteropNamespace + ".WindowsRuntime",
            "WindowsRuntimeImportAttribute", CaTargets.TypeDef),
    ];

    private static CaTargets GetTarget(EntityRegistry.EntityBase owner) => owner switch
    {
        EntityRegistry.TypeDefinitionEntity => CaTargets.TypeDef,
        EntityRegistry.TypeReferenceEntity => CaTargets.TypeRef,
        EntityRegistry.MethodDefinitionEntity => CaTargets.MethodDef,
        EntityRegistry.FieldDefinitionEntity => CaTargets.FieldDef,
        EntityRegistry.ParameterEntity => CaTargets.ParamDef,
        EntityRegistry.PropertyEntity => CaTargets.Property,
        EntityRegistry.EventEntity => CaTargets.Event,
        EntityRegistry.ModuleEntity => CaTargets.Module,
        EntityRegistry.AssemblyEntity => CaTargets.Assembly,
        _ => CaTargets.None,
    };
}
