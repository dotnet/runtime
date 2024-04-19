// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Numerics.Hashing;
using Microsoft.CodeAnalysis;

namespace SourceGenerators;

/// <summary>
/// Descriptor for diagnostic instances using structural equality comparison.
/// Provides a work-around for https://github.com/dotnet/roslyn/issues/68291.
/// </summary>
internal readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    public DiagnosticDescriptor Descriptor { get; private init; }
    public object?[] MessageArgs { get; private init; }
    public Location? Location { get; private init; }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, object?[]? messageArgs)
    {
        Location? trimmedLocation = location is null ? null : GetTrimmedLocation(location);

        return new DiagnosticInfo
        {
            Descriptor = descriptor,
            Location = trimmedLocation,
            MessageArgs = messageArgs ?? Array.Empty<object?>()
        };

        // Creates a copy of the Location instance that does not capture a reference to Compilation.
        static Location GetTrimmedLocation(Location location)
            => Location.Create(location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span);
    }

    public Diagnostic CreateDiagnostic()
        => Diagnostic.Create(Descriptor, Location, MessageArgs);

    public override readonly bool Equals(object? obj) => obj is DiagnosticInfo info && Equals(info);

    public readonly bool Equals(DiagnosticInfo other)
    {
        return Descriptor.Equals(other.Descriptor) &&
            MessageArgs.SequenceEqual(other.MessageArgs) &&
            Location == other.Location;
    }

    public override readonly int GetHashCode()
    {
        int hashCode = Descriptor.GetHashCode();
        foreach (object? messageArg in MessageArgs)
        {
            hashCode = HashHelpers.Combine(hashCode, messageArg?.GetHashCode() ?? 0);
        }

        hashCode = HashHelpers.Combine(hashCode, Location?.GetHashCode() ?? 0);
        return hashCode;
    }
}
