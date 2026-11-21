// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Microsoft.Interop;

/// <summary>
/// Simple User-application of System.Runtime.InteropServices.MarshalAsAttribute
/// </summary>
public abstract record MarshalAsInfo(
    UnmanagedType UnmanagedType,
    CharEncoding CharEncoding) : MarshallingInfoStringSupport(CharEncoding), IForwardedMarshallingInfo
{
    // UnmanagedType.LPUTF8Str is not in netstandard2.0, so we define a constant for the value here.
    // See https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.unmanagedtype
    /// <summary>Identifies null-terminated UTF-8 string marshalling.</summary>
    public const UnmanagedType UnmanagedType_LPUTF8Str = (UnmanagedType)0x30;

    private protected abstract bool TryCreateAttribute([NotNullWhen(true)] out string? attribute);

    bool IForwardedMarshallingInfo.TryCreateAttribute([NotNullWhen(true)] out string? attribute) => TryCreateAttribute(out attribute);

    private protected static IndentedTextWriter CreateAttributeWriter(UnmanagedType unmanagedType)
    {
        var writer = new IndentedTextWriter();
        writer.Write($"{TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_MarshalAsAttribute}(({TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_UnmanagedType}){(int)unmanagedType}");
        return writer;
    }
}

public sealed record MarshalAsScalarInfo(
    UnmanagedType UnmanagedType,
    CharEncoding CharEncoding) : MarshalAsInfo(UnmanagedType, CharEncoding)
{
    private protected override bool TryCreateAttribute([NotNullWhen(true)] out string? attribute)
    {
        if (UnmanagedType == UnmanagedType.CustomMarshaler)
        {
            attribute = null;
            return false;
        }

        IndentedTextWriter writer = CreateAttributeWriter(UnmanagedType);
        writer.Write(')');
        attribute = writer.ToString();
        return true;
    }
}

public sealed record MarshalAsInterfaceInfo(
    UnmanagedType UnmanagedType,
    CharEncoding CharEncoding,
    TypePositionInfo? IidParameterIndexInfo) : MarshalAsInfo(UnmanagedType, CharEncoding)
{
    public override IEnumerable<TypePositionInfo> ElementDependencies
        => IidParameterIndexInfo is null ? [] : [IidParameterIndexInfo];

    private protected override bool TryCreateAttribute([NotNullWhen(true)] out string? attribute)
    {
        IndentedTextWriter writer = CreateAttributeWriter(UnmanagedType);

        if (IidParameterIndexInfo is { ManagedIndex: int paramIndex } && !TypePositionInfo.IsSpecialIndex(paramIndex))
        {
            writer.Write($", {nameof(MarshalAsAttribute.IidParameterIndex)} = {paramIndex}");
        }

        writer.Write(')');
        attribute = writer.ToString();
        return true;
    }
}

public sealed record MarshalAsArrayInfo(
    UnmanagedType UnmanagedType,
    CharEncoding CharEncoding,
    UnmanagedType ArraySubType,
    CountInfo CountInfo) : MarshalAsInfo(UnmanagedType, CharEncoding)
{
    private protected override bool TryCreateAttribute([NotNullWhen(true)] out string? attribute)
    {
        if (ArraySubType == UnmanagedType.CustomMarshaler)
        {
            attribute = null;
            return false;
        }

        IndentedTextWriter writer = CreateAttributeWriter(UnmanagedType);

        if (ArraySubType != (UnmanagedType)SizeAndParamIndexInfo.UnspecifiedConstSize)
        {
            writer.Write($", {nameof(ArraySubType)} = ({TypeNames.GlobalAlias}{TypeNames.System_Runtime_InteropServices_UnmanagedType}){(int)ArraySubType}");
        }

        if (CountInfo is SizeAndParamIndexInfo sizeParamIndex)
        {
            if (sizeParamIndex.ConstSize != SizeAndParamIndexInfo.UnspecifiedConstSize)
            {
                writer.Write($", {nameof(MarshalAsAttribute.SizeConst)} = {sizeParamIndex.ConstSize}");
            }
            if (sizeParamIndex.ParamAtIndex is { ManagedIndex: int paramIndex })
            {
                writer.Write($", {nameof(MarshalAsAttribute.SizeParamIndex)} = {paramIndex}");
            }
        }
        writer.Write(')');
        attribute = writer.ToString();
        return true;
    }
}
