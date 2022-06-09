// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexReplacement
    {
        public RegexReplacement(string rep, RegexNode concat, Hashtable caps) { }

        private const int Specials = 4;
        public const int LeftPortion = -1;
        public const int RightPortion = -2;
        public const int LastGroup = -3;
        public const int WholeString = -4;
    }
}

namespace System.Text.RegularExpressions.Generator
{
    internal static class DiagnosticDescriptors
    {
        public static DiagnosticDescriptor UseRegexSourceGeneration { get; } = new DiagnosticDescriptor(
            id: "SYSLIB1046",
            title: "Use the Regex source generator.",
            messageFormat: "The inputs to your Regex are known at compile-time so you could be using the source generator to boost performance.",
            category: "RegexGenerator",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
