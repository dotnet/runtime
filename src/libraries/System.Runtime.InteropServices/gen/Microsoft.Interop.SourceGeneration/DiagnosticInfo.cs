// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed record DiagnosticInfo
    {
        public required DiagnosticDescriptor Descriptor { get; init; }
        public required SequenceEqualImmutableArray<string> MessageArgs { get; init; }
        public required Location? Location { get; init; }
        public required SequenceEqualImmutableArray<Location>? AdditionalLocations { get; init; }
        public required ValueEqualityImmutableDictionary<string, string>? Properties { get; init; }

        public Diagnostic ToDiagnostic() => Diagnostic.Create(
            Descriptor,
            Location,
            additionalLocations: AdditionalLocations,
            properties: Properties?.Map,
            messageArgs: MessageArgs.Array.ToArray());

        public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params object?[] messageArgs)
        {
            return new DiagnosticInfo()
            {
                Descriptor = descriptor,
                Location = location,
                AdditionalLocations = null,
                Properties = null,
                MessageArgs = messageArgs.Select(o => o?.ToString()).ToSequenceEqualImmutableArray()
            };
        }

        public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, ImmutableDictionary<string, string>? properties, params object?[] messageArgs)
        {
            return new DiagnosticInfo()
            {
                Descriptor = descriptor,
                Location = location,
                AdditionalLocations = null,
                Properties = properties.ToValueEquals(),
                MessageArgs = messageArgs.Select(o => o.ToString()).ToSequenceEqualImmutableArray()
            };
        }

        public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, IEnumerable<Location>? additionalLocations, ImmutableDictionary<string, string>? properties, params object?[] messageArgs)
        {
            return new DiagnosticInfo()
            {
                Descriptor = descriptor,
                Location = location,
                AdditionalLocations = (additionalLocations ?? ImmutableArray<Location>.Empty).ToSequenceEqualImmutableArray(),
                Properties = properties.ToValueEquals(),
                MessageArgs = messageArgs.Select(o => o.ToString()).ToSequenceEqualImmutableArray()
            };
        }
    }
}
