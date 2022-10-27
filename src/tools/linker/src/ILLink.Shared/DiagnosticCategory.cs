// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
    internal static class DiagnosticCategory
    {
        public const string SingleFile = nameof(SingleFile);
        public const string Trimming = nameof(Trimming);
        public const string AOT = nameof(AOT);
    }
}
