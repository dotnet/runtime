// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Numerics.Hashing;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Descriptor for diagnostic instances using structural equality comparison.
    /// Provides a work-around for https://github.com/dotnet/roslyn/issues/68291.
    /// </summary>
    public readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
    {
        public required DiagnosticDescriptor Descriptor { get; init; }
        public required object?[] MessageArgs { get; init; }
        public required Location? Location { get; init; }

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
}
