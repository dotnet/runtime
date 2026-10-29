// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Provides polyfills for the memory safety rules API (dotnet/roslyn#84860), which analyzers and source generators
    /// run against but can't compile against while they reference an older Roslyn.
    /// </summary>
    /// <remarks>
    /// Members are spelled like the API they stand in for. A declared member takes precedence over an extension member,
    /// so once the Roslyn reference has the API, call sites bind to it and this file can be deleted.
    /// </remarks>
    internal static class MemorySafetyRulesPolyfills
    {
        // The enum's underlying type is int, so the getter binds to a delegate returning int.
        private static readonly Func<IModuleSymbol, int>? s_getMemorySafetyRulesVersion =
            typeof(IModuleSymbol).GetProperty("MemorySafetyRulesVersion")?.GetMethod is { } getter
                ? (Func<IModuleSymbol, int>)getter.CreateDelegate(typeof(Func<IModuleSymbol, int>))
                : null;

        extension(IModuleSymbol module)
        {
            /// <summary>
            /// Mirrors <c>IModuleSymbol.MemorySafetyRulesVersion</c>: 1 for the legacy rules, 2 for the updated rules.
            /// </summary>
            /// <remarks>
            /// The real property is an enum, so callers compare <c>(int)module.MemorySafetyRulesVersion</c>. Hosts that
            /// predate the API only know the <c>updated-memory-safety-rules</c> feature flag, which is read from the
            /// parse options of a source module's syntax trees. Experimental like the real API, so switching to it
            /// changes no diagnostics.
            /// </remarks>
            [Experimental("RSEXPERIMENTAL006")]
            public int MemorySafetyRulesVersion =>
                s_getMemorySafetyRulesVersion is { } getVersion ? getVersion(module)
                : module.Locations.FirstOrDefault()?.SourceTree?.Options.Features.ContainsKey("updated-memory-safety-rules") == true ? 2
                : 1;
        }
    }
}
