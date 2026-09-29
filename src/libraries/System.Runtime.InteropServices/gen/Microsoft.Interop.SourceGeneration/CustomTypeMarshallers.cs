// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop;

public readonly record struct CustomTypeMarshallerData(
    ManagedTypeInfo MarshallerType,
    ManagedTypeInfo NativeType,
    bool HasState,
    MarshallerShape Shape,
    bool IsStrictlyBlittable,
    ManagedTypeInfo? BufferElementType,
    ManagedTypeInfo? CollectionElementType,
    MarshallingInfo? CollectionElementMarshallingInfo)
{
    /// <summary>Gets the marshaller type template, with null characters marking the unmanaged element parameter.</summary>
    public string? MarshallerTypeTemplate { get; init; }
    /// <summary>Gets the native type template, with null characters marking the unmanaged element parameter.</summary>
    public string? NativeTypeTemplate { get; init; }
    /// <summary>Gets the buffer element type template, with null characters marking the unmanaged element parameter.</summary>
    public string? BufferElementTypeTemplate { get; init; }

    /// <summary>Specializes the symbol-identified unmanaged element parameter in collection marshaller types.</summary>
    /// <param name="unmanagedElementType">The generic-compatible unmanaged element type name.</param>
    /// <returns>The specialized marshaller data.</returns>
    public CustomTypeMarshallerData WithUnmanagedElementType(string unmanagedElementType)
    {
        return this with
        {
            MarshallerType = Specialize(MarshallerType, MarshallerTypeTemplate),
            NativeType = Specialize(NativeType, NativeTypeTemplate),
            BufferElementType = BufferElementType is null ? null : Specialize(BufferElementType, BufferElementTypeTemplate)
        };

        ManagedTypeInfo Specialize(ManagedTypeInfo type, string? template)
        {
            string name = template is null ? type.FullTypeName : template.Replace("\0", unmanagedElementType);
            return type with { FullTypeName = name, DiagnosticFormattedName = name };
        }
    }
}

public readonly record struct CustomTypeMarshallers(
    ImmutableDictionary<MarshalMode, CustomTypeMarshallerData> Modes,
    string? UnsupportedReason = null)
{
    public bool Equals(CustomTypeMarshallers other)
    {
        // Check for equal count, then check if any KeyValuePairs exist in one 'Modes'
        // but not the other (i.e. set equality on the set of items in the dictionary)
        return Modes.Count == other.Modes.Count
            && !Modes.Except(other.Modes).Any()
            && UnsupportedReason == other.UnsupportedReason;
    }

    public override int GetHashCode()
    {
        int hash = UnsupportedReason?.GetHashCode() ?? 0;
        foreach (KeyValuePair<MarshalMode, CustomTypeMarshallerData> mode in Modes)
        {
            hash = HashCode.Combine(hash, mode.Key, mode.Value);
        }
        return hash;
    }

    public CustomTypeMarshallerData GetModeOrDefault(MarshalMode mode)
    {
        CustomTypeMarshallerData data;
        if (Modes.TryGetValue(mode, out data))
            return data;

        if (Modes.TryGetValue(MarshalMode.Default, out data))
            return data;

        // TODO: Hard failure based on previous implementation
        throw new InvalidOperationException();
    }

    public bool TryGetModeOrDefault(MarshalMode mode, out CustomTypeMarshallerData data)
    {
        if (Modes.TryGetValue(mode, out data))
            return true;

        return Modes.TryGetValue(MarshalMode.Default, out data);
    }

    public bool IsDefinedOrDefault(MarshalMode mode)
    {
        return Modes.ContainsKey(mode) || Modes.ContainsKey(MarshalMode.Default);
    }
}
