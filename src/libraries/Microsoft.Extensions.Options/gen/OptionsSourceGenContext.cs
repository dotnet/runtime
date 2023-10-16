// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Options.Generators
{
    internal sealed class OptionsSourceGenContext
    {
        public OptionsSourceGenContext(Compilation compilation)
        {
            IsLangVersion11AndAbove = ((CSharpCompilation)compilation).LanguageVersion >= Microsoft.CodeAnalysis.CSharp.LanguageVersion.CSharp11;
            ClassModifier = IsLangVersion11AndAbove ? "file" : "internal";
            Suffix = IsLangVersion11AndAbove ? "" : $"_{GetNonRandomizedHashCode(compilation.SourceModule.Name):X8}";
        }

        internal string Suffix { get; }
        internal string ClassModifier { get; }
        internal bool IsLangVersion11AndAbove { get; }
        internal Dictionary<string, HashSet<object>?> AttributesToGenerate { get; set; } = new Dictionary<string, HashSet<object>?>();

        internal void EnsureTrackingAttribute(string attributeName, bool createValue, out HashSet<object>? value)
        {
            bool exist = AttributesToGenerate.TryGetValue(attributeName, out value);
            if (value is null)
            {
                if (createValue)
                {
                    value = new HashSet<object>();
                }

                if (!exist || createValue)
                {
                    AttributesToGenerate[attributeName] = value;
                }
            }
        }

        internal static bool IsConvertibleBasicType(ITypeSymbol typeSymbol)
        {
            return typeSymbol.SpecialType switch
            {
                SpecialType.System_Boolean => true,
                SpecialType.System_Byte => true,
                SpecialType.System_Char => true,
                SpecialType.System_DateTime => true,
                SpecialType.System_Decimal => true,
                SpecialType.System_Double => true,
                SpecialType.System_Int16 => true,
                SpecialType.System_Int32 => true,
                SpecialType.System_Int64 => true,
                SpecialType.System_SByte => true,
                SpecialType.System_Single => true,
                SpecialType.System_UInt16 => true,
                SpecialType.System_UInt32 => true,
                SpecialType.System_UInt64 => true,
                SpecialType.System_String => true,
                _ => false,
            };
        }

        /// <summary>
        /// Returns a non-randomized hash code for the given string.
        /// We always return a positive value.
        /// </summary>
        internal static int GetNonRandomizedHashCode(string s)
        {
            uint result = 2166136261u;
            foreach (char c in s)
            {
                result = (c ^ result) * 16777619;
            }

            return Math.Abs((int)result);
        }
    }
}
