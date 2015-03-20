// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/*=============================================================================
**
**
** Purpose: An implementation of a CodeDomProvider.
**
**
=============================================================================*/

using System;
using System.Collections.Generic;
using System.CodeDom.Compiler;

namespace System.CodeDom.Compiler
{
    internal abstract class CodeDomProvider
    {
        public abstract string CreateEscapedIdentifier(string name);
    }
}

namespace Microsoft.CSharp
{
    // Ported from \ndp\fx\src\CompMod\Microsoft\CSharp\CSharpCodeProvider.cs
    internal class CSharpCodeProvider : CodeDomProvider
    {
        private static Dictionary<string, object> s_fixedStringLookup;

        public override string CreateEscapedIdentifier(string name)
        {
            // Any identifier started with two consecutive underscores are 
            // reserved by CSharp.
            if (IsKeyword(name) || IsPrefixTwoUnderscore(name))
            {
                return "@" + name;
            }
            return name;
        }

        private static readonly string[][] s_keywords = new string[][] {
            null,           // 1 character
            new string[] {  // 2 characters
                "as",
                "do",
                "if",
                "in",
                "is",
            },
            new string[] {  // 3 characters
                "for",
                "int",
                "new",
                "out",
                "ref",
                "try",
            },
            new string[] {  // 4 characters
                "base",
                "bool",
                "byte",
                "case",
                "char",
                "else",
                "enum",
                "goto",
                "lock",
                "long",
                "null",
                "this",
                "true",
                "uint",
                "void",
            },
            new string[] {  // 5 characters
                "break",
                "catch",
                "class",
                "const",
                "event",
                "false",
                "fixed",
                "float",
                "sbyte",
                "short",
                "throw",
                "ulong",
                "using",
                "where",
                "while",
                "yield",
            },
            new string[] {  // 6 characters
                "double",
                "extern",
                "object",
                "params",
                "public",
                "return",
                "sealed",
                "sizeof",
                "static",
                "string",
                "struct",
                "switch",
                "typeof",
                "unsafe",
                "ushort",
            },
            new string[] {  // 7 characters
                "checked",
                "decimal",
                "default",
                "finally",
                "foreach",
                "partial",
                "private",
                "virtual",
            },
            new string[] {  // 8 characters
                "abstract",
                "continue",
                "delegate",
                "explicit",
                "implicit",
                "internal",
                "operator",
                "override",
                "readonly",
                "volatile",
            },
            new string[] {  // 9 characters
                "__arglist",
                "__makeref",
                "__reftype",
                "interface",
                "namespace",
                "protected",
                "unchecked",
            },
            new string[] {  // 10 characters
                "__refvalue",
                "stackalloc",
            },
        };

        static CSharpCodeProvider()
        {
            s_fixedStringLookup = new Dictionary<string, object>();
            for (int i = 0; i < s_keywords.Length; i++)
            {
                string[] values = s_keywords[i];
                if (values != null)
                {
                    for (int j = 0; j < values.Length; j++)
                    {
                        s_fixedStringLookup.Add(values[j], null);
                    }
                }
            }
        }

        private static bool IsKeyword(string value)
        {
            return s_fixedStringLookup.ContainsKey(value);
        }

        private static bool IsPrefixTwoUnderscore(string value)
        {
            if (value.Length < 3)
            {
                return false;
            }
            else
            {
                return ((value[0] == '_') && (value[1] == '_') && (value[2] != '_'));
            }
        }
    }
}
