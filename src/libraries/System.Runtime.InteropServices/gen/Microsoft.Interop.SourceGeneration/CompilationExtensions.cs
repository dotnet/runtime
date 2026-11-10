// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

// IModuleSymbol.MemorySafetyRulesVersion is experimental
#pragma warning disable RSEXPERIMENTAL006

namespace Microsoft.Interop
{
    public static class CompilationExtensions
    {
        public static EnvironmentFlags GetEnvironmentFlags(this Compilation compilation)
        {
            EnvironmentFlags flags = EnvironmentFlags.None;
            if (compilation.SourceModule.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_SkipLocalsInitAttribute))
            {
                flags |= EnvironmentFlags.SkipLocalsInit;
            }
            if (compilation.Assembly.GetAttributes().Any(attr => attr.AttributeClass.ToDisplayString() == TypeNames.System_Runtime_CompilerServices_DisableRuntimeMarshallingAttribute))
            {
                flags |= EnvironmentFlags.DisableRuntimeMarshalling;
            }
            if (compilation.UsesUpdatedMemorySafetyRules())
            {
                flags |= EnvironmentFlags.UpdatedMemorySafetyRules;
            }
            return flags;
        }

        /// <summary>
        /// Determines whether the compilation uses the updated memory safety rules.
        /// </summary>
        public static bool UsesUpdatedMemorySafetyRules(this Compilation compilation)
            => compilation.SourceModule.UsesUpdatedMemorySafetyRules();

        /// <summary>
        /// Determines whether a module uses the updated memory safety rules.
        /// </summary>
        public static bool UsesUpdatedMemorySafetyRules(this IModuleSymbol module)
            => (int)module.MemorySafetyRulesVersion >= 2;
    }
}
