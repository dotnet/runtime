// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Microsoft.Interop
{
    public abstract record GeneratorDiagnostic
    {
        private GeneratorDiagnostic(TypePositionInfo typePositionInfo, bool isFatal)
        {
            TypePositionInfo = typePositionInfo;
            IsFatal = isFatal;
        }

        /// <summary>
        /// [Optional] Properties to attach to any diagnostic emitted due to this exception.
        /// </summary>
        public ImmutableDictionary<string, string> DiagnosticProperties { get; init; } = ImmutableDictionary<string, string>.Empty;
        public TypePositionInfo TypePositionInfo { get; }
        public bool IsFatal { get; }

        public sealed record NotSupported(TypePositionInfo TypePositionInfo) : GeneratorDiagnostic(TypePositionInfo, isFatal: true)
        {
            /// <summary>
            /// [Optional] Specific reason marshalling of the supplied type isn't supported.
            /// </summary>
            public string? NotSupportedDetails { get; init; }

        }

        public sealed record NotRecommended(TypePositionInfo TypePositionInfo) : GeneratorDiagnostic(TypePositionInfo, isFatal: false)
        {
            public string? Details { get; init; }

        }
    }
}
