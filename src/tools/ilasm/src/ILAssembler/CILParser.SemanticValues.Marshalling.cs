// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public sealed record MarshallingDescriptorValue(
        BlobBuilder? RawBytes,
        NativeTypeValue? NativeType)
    {
        public static MarshallingDescriptorValue Empty { get; } =
            new(new BlobBuilder(0), null);
    }

    public sealed record NativeTypeValue(
        IToken? Token,
        NativeTypeElementValue? Element,
        ImmutableArray<NativeTypeArrayPointerInfoValue> ArrayPointerInfo)
    {
        public static NativeTypeValue Empty { get; } = new(null, null, []);
    }

    public abstract record NativeTypeElementValue;

    public sealed record EmptyNativeTypeElementValue : NativeTypeElementValue
    {
        public static EmptyNativeTypeElementValue Instance { get; } = new();
    }

    public sealed record CustomMarshallerNativeTypeElementValue(
        IToken? Token,
        string? Guid,
        string? NativeTypeName,
        string MarshallerType,
        string Cookie) : NativeTypeElementValue;

    public sealed record FixedSysStringNativeTypeElementValue(
        IToken Size) : NativeTypeElementValue;

    public sealed record FixedArrayNativeTypeElementValue(
        IToken Size,
        NativeTypeValue Element) : NativeTypeElementValue;

    public sealed record DeprecatedNativeTypeElementValue(
        IToken Token,
        int TokenType) : NativeTypeElementValue;

    public sealed record SimpleNativeTypeElementValue(
        int TokenType) : NativeTypeElementValue;

    public sealed record IidNativeTypeElementValue(
        int TokenType,
        IidParamIndexValue IidParamIndex) : NativeTypeElementValue;

    public sealed record SafeArrayNativeTypeElementValue(
        VariantTypeValue VariantType,
        string? UserDefinedType) : NativeTypeElementValue;

    public sealed record UnsignedNativeTypeElementValue(
        int TokenType) : NativeTypeElementValue;

    public sealed record NestedStructNativeTypeElementValue(
        IToken Token) : NativeTypeElementValue;

    public sealed record AnsiBstrNativeTypeElementValue : NativeTypeElementValue
    {
        public static AnsiBstrNativeTypeElementValue Instance { get; } = new();
    }

    public sealed record VariantBoolNativeTypeElementValue : NativeTypeElementValue
    {
        public static VariantBoolNativeTypeElementValue Instance { get; } = new();
    }

    public sealed record NativeTypeTypedefValue(
        IToken Token,
        string Alias) : NativeTypeElementValue;

    public enum NativeTypeArrayPointerInfoKind
    {
        Pointer,
        ArrayNoSizeData,
        ArraySize,
        ArraySizeParamIndex,
        ArrayParamIndex
    }

    public sealed record NativeTypeArrayPointerInfoValue(
        NativeTypeArrayPointerInfoKind Kind,
        IToken? Size = null,
        IToken? ParameterIndex = null);

    public sealed record IidParamIndexValue(IToken? Index)
    {
        public static IidParamIndexValue Empty { get; } = new((IToken?)null);
    }

    public sealed record VariantTypeValue(
        VariantTypeElementValue? Element,
        VarEnum Modifiers)
    {
        public static VariantTypeValue Empty { get; } = new(null, 0);
    }

    public sealed record VariantTypeElementValue(int TokenType)
    {
        public static VariantTypeElementValue Error { get; } =
            new(TokenConstants.InvalidType);
    }

    public sealed class MarshalBlobBuilder
    {
        internal NativeTypeValue? NativeType { get; set; }

        internal BlobBuilder? RawBytes { get; set; }
    }

    public sealed class NativeTypeBuilder
    {
        internal NativeTypeElementValue? Element { get; set; }

        internal List<NativeTypeArrayPointerInfoValue>? ArrayPointerInfo { get; set; }
    }

    public sealed class VariantTypeBuilder
    {
        internal VariantTypeElementValue? Element { get; set; }

        internal VarEnum Modifiers { get; set; }
    }
}
