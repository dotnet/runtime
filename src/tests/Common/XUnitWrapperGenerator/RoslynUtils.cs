// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace XUnitWrapperGenerator
{
    internal class RoslynUtils
    {
        /// <summary>
        ///   Returns the Main method that would serve as the entry point of the assembly, ignoring
        ///   whether the current target is an executable.
        /// </summary>
        /// <remarks>
        ///   Replacement for CSharpCompilation.GetEntryPoint() which only works for executables.
        ///   Replacement for its helpers that are internal.
        ///
        ///   Intended for the analyzer that is trying to find Main methods that won't be called in
        ///   merged test groups. Ignores details such as SynthesizedSimpleProgramEntryPointSymbol.
        ///   Ignores top-level statements as (1) in exes, they will generate an error for conflicting
        ///   with the auto-generated main, and (2) in libs, they will generate an error for existing
        ///   at all.
        /// </remarks>
        internal static IEnumerable<IMethodSymbol> GetPossibleEntryPoints(Compilation comp, CancellationToken cancellationToken)
            => comp
                .GetSymbolsWithName(WellKnownMemberNames.EntryPointMethodName, SymbolFilter.Member)
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic && !m.IsAbstract && !m.IsVirtual);
    }
}
