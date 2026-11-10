// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop;

/// <summary>Describes a generated parameter independently of a syntax tree.</summary>
/// <param name="Type">The fully qualified type name.</param>
/// <param name="Identifier">The escaped parameter identifier.</param>
/// <param name="Modifiers">The space-separated parameter modifiers.</param>
/// <param name="Attributes">The comma-separated attribute bodies, without brackets.</param>
public readonly record struct GeneratedParameter(string Type, string Identifier, string Modifiers = "", string? Attributes = null)
{
    /// <summary>Gets the parameter declaration.</summary>
    public string Declaration => (string.IsNullOrEmpty(Attributes) ? "" : $"[{Attributes}] ")
        + (Modifiers.Length == 0 ? "" : Modifiers + " ")
        + Type + " " + Identifier;

    /// <summary>Gets the parameter type and ref modifiers for a function pointer signature.</summary>
    public string FunctionPointerType
    {
        get
        {
            string modifiers = string.Join(" ", Modifiers.Split(' ').Where(static modifier => modifier is "ref" or "readonly" or "in" or "out"));
            return modifiers.Length == 0 ? Type : modifiers + " " + Type;
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Declaration;
}

/// <summary>Describes the native signature shared by import and export stubs.</summary>
/// <param name="Parameters">The parameters in native signature order.</param>
/// <param name="ReturnType">The fully qualified return type.</param>
/// <param name="ReturnTypeAttributes">The return attribute bodies, without brackets or a target.</param>
public sealed record GeneratedMethodSignature(
    ImmutableArray<GeneratedParameter> Parameters,
    string ReturnType,
    string? ReturnTypeAttributes = null)
{
    /// <inheritdoc/>
    public bool Equals(GeneratedMethodSignature? other)
    {
        return other is not null
            && Parameters.SequenceEqual(other.Parameters)
            && ReturnType == other.ReturnType
            && ReturnTypeAttributes == other.ReturnTypeAttributes;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        int hash = HashCode.Combine(ReturnType, ReturnTypeAttributes);
        foreach (GeneratedParameter parameter in Parameters)
        {
            hash = HashCode.Combine(hash, parameter);
        }
        return hash;
    }

    /// <summary>Gets the parenthesized parameter declarations.</summary>
    public string ParameterList => "(" + string.Join(", ", Parameters.Select(static parameter => parameter.Declaration)) + ")";

    /// <summary>Formats the corresponding unmanaged function pointer type.</summary>
    /// <param name="callingConventions">The unmanaged calling conventions.</param>
    /// <returns>The function pointer type.</returns>
    public string GetFunctionPointerType(ImmutableArray<string> callingConventions)
    {
        string conventions = callingConventions.IsDefaultOrEmpty ? "" : "[" + string.Join(", ", callingConventions) + "]";
        return "delegate* unmanaged" + conventions + "<"
            + string.Join(", ", Parameters.Select(static parameter => parameter.FunctionPointerType).Append(ReturnType)) + ">";
    }
}
