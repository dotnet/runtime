// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public abstract record GeneratorDiagnostic
    {
        private GeneratorDiagnostic(TypePositionInfo typePositionInfo, StubCodeContext stubCodeContext, bool isFatal)
        {
            TypePositionInfo = typePositionInfo;
            StubCodeContext = stubCodeContext;
            IsFatal = isFatal;
        }

        /// <summary>
        /// [Optional] Properties to attach to any diagnostic emitted due to this exception.
        /// </summary>
        public ImmutableDictionary<string, string> DiagnosticProperties { get; init; } = ImmutableDictionary<string, string>.Empty;
        public TypePositionInfo TypePositionInfo { get; }
        public StubCodeContext StubCodeContext { get; }
        public bool IsFatal { get; }

        public abstract DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName);

        public sealed record NotSupported(TypePositionInfo TypePositionInfo, StubCodeContext StubCodeContext) : GeneratorDiagnostic(TypePositionInfo, StubCodeContext, isFatal: true)
        {
            /// <summary>
            /// [Optional] Specific reason marshalling of the supplied type isn't supported.
            /// </summary>
            public string? NotSupportedDetails { get; init; }

            public override DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName)
            {
                if (NotSupportedDetails is not null)
                {
                    return DiagnosticInfo.Create(descriptor, location, DiagnosticProperties, NotSupportedDetails, elementName);
                }
                return DiagnosticInfo.Create(descriptor, location, DiagnosticProperties, TypePositionInfo.ManagedType.DiagnosticFormattedName, elementName);
            }
        }

        public sealed record UnnecessaryData(TypePositionInfo TypePositionInfo, StubCodeContext StubCodeContext, ImmutableArray<Location> UnnecessaryDataLocations) : GeneratorDiagnostic(TypePositionInfo, StubCodeContext, isFatal: false)
        {
            public required string UnnecessaryDataDetails { get; init; }

            public override DiagnosticInfo ToDiagnosticInfo(DiagnosticDescriptor descriptor, Location location, string elementName)
            {
                return DiagnosticInfo.Create(
                    descriptor,
                    location,
                    UnnecessaryDataLocations,
                    // Add "unnecessary locations" property so the IDE fades the right locations.
                    DiagnosticProperties.Add(WellKnownDiagnosticTags.Unnecessary, $"[{string.Join(",", Enumerable.Range(0, UnnecessaryDataLocations.Length))}]"),
                    UnnecessaryDataDetails,
                    elementName);
            }
        }
    }
}
