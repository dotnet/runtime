// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
    public static class MessageSubCategory
    {
        public const string None = "";
        public const string TrimAnalysis = "Trim analysis";
        public const string UnresolvedAssembly = "Unresolved assembly";
        public const string AotAnalysis = "AOT analysis";
    }
}
