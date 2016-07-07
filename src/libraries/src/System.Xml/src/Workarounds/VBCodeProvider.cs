// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.CodeDom;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.CodeDom.Compiler;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Microsoft.VisualBasic
{
    #region class VBCodeProvider

    /// <devdoc>
    /// <para>[To be supplied.]</para>
    /// </devdoc>
    internal class VBCodeProvider : CodeDomProvider
    {
        private VBCodeGenerator _generator;

        public VBCodeProvider()
        {
            _generator = new VBCodeGenerator();
        }

        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeGenerator CreateGenerator()
        {
            return (ICodeGenerator)_generator;
        }

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class.")]
        public override ICodeCompiler CreateCompiler()
        {
            return (ICodeCompiler)_generator;
        }

        public override string CreateEscapedIdentifier(string name)
        {
            if (VBCodeGenerator.IsKeyword(name))
            {
                return "[" + name + "]";
            }
            return name;
        }
    }  // VBCodeProvider

    #endregion class VBCodeProvider


    #region class VBCodeGenerator

    /// <devdoc>
    ///    <para>
    ///       Visual Basic 7 Code Generator.
    ///    </para>
    /// </devdoc>
    internal class VBCodeGenerator
    {
        private const int MaxLineLength = 80;

        private const GeneratorSupport LanguageSupport = GeneratorSupport.EntryPointMethod |
                                                         GeneratorSupport.GotoStatements |
                                                         GeneratorSupport.ArraysOfArrays |
                                                         GeneratorSupport.MultidimensionalArrays |
                                                         GeneratorSupport.StaticConstructors |
                                                         GeneratorSupport.ReturnTypeAttributes |
                                                         GeneratorSupport.AssemblyAttributes |
                                                         GeneratorSupport.TryCatchStatements |
                                                         GeneratorSupport.DeclareValueTypes |
                                                         GeneratorSupport.DeclareEnums |
                                                         GeneratorSupport.DeclareEvents |
                                                         GeneratorSupport.DeclareDelegates |
                                                         GeneratorSupport.DeclareInterfaces |
                                                         GeneratorSupport.ParameterAttributes |
                                                         GeneratorSupport.ReferenceParameters |
                                                         GeneratorSupport.ChainedConstructorArguments |
                                                         GeneratorSupport.NestedTypes |
                                                         GeneratorSupport.MultipleInterfaceMembers |
                                                         GeneratorSupport.PublicStaticMembers |
                                                         GeneratorSupport.ComplexExpressions |
                                                         GeneratorSupport.Win32Resources |
                                                         GeneratorSupport.Resources |
                                                         GeneratorSupport.PartialTypes |
                                                         GeneratorSupport.GenericTypeReference |
                                                         GeneratorSupport.GenericTypeDeclaration |
                                                         GeneratorSupport.DeclareIndexerProperties;

        // Implementation of FixedStringLookup ported from \corefx\src\System.Xml.XmlSerializer\src\System\CodeDom\Compiler\CodeDomProvider.cs
        private static Dictionary<string, object> s_fixedStringLookup;

        // This is the keyword list. To minimize search time and startup time, this is stored by length
        // and then alphabetically for use by FixedStringLookup.Contains.
        private static readonly string[][] s_keywords = new string[][] {
            null,           // 1 character
            new string[] {  // 2 characters            
                "as",
                "do",
                "if",
                "in",
                "is",
                "me",
                "of",
                "on",
                "or",
                "to",
            },
            new string[] {  // 3 characters
                "and",
                "dim",
                "end",
                "for",
                "get",
                "let",
                "lib",
                "mod",
                "new",
                "not",
                "rem",
                "set",
                "sub",
                "try",
                "xor",
            },
            new string[] {  // 4 characters
                "ansi",
                "auto",
                "byte",
                "call",
                "case",
                "cdbl",
                "cdec",
                "char",
                "cint",
                "clng",
                "cobj",
                "csng",
                "cstr",
                "date",
                "each",
                "else",
                "enum",
                "exit",
                "goto",
                "like",
                "long",
                "loop",
                "next",
                "step",
                "stop",
                "then",
                "true",
                "wend",
                "when",
                "with",
            },
            new string[] {  // 5 characters  
                "alias",
                "byref",
                "byval",
                "catch",
                "cbool",
                "cbyte",
                "cchar",
                "cdate",
                "class",
                "const",
                "ctype",
                "cuint",
                "culng",
                "endif",
                "erase",
                "error",
                "event",
                "false",
                "gosub",
                "isnot",
                "redim",
                "sbyte",
                "short",
                "throw",
                "ulong",
                "until",
                "using",
                "while",
             },
            new string[] {  // 6 characters
                "csbyte",
                "cshort",
                "double",
                "elseif",
                "friend",
                "global",
                "module",
                "mybase",
                "object",
                "option",
                "orelse",
                "public",
                "resume",
                "return",
                "select",
                "shared",
                "single",
                "static",
                "string",
                "typeof",
                "ushort",
            },
            new string[] { // 7 characters
                "andalso",
                "boolean",
                "cushort",
                "decimal",
                "declare",
                "default",
                "finally",
                "gettype",
                "handles",
                "imports",
                "integer",
                "myclass",
                "nothing",
                "partial",
                "private",
                "shadows",
                "trycast",
                "unicode",
                "variant",
            },
            new string[] {  // 8 characters
                "assembly",
                "continue",
                "delegate",
                "function",
                "inherits",
                "operator",
                "optional",
                "preserve",
                "property",
                "readonly",
                "synclock",
                "uinteger",
                "widening"
            },
            new string [] { // 9 characters
                "addressof",
                "interface",
                "namespace",
                "narrowing",
                "overloads",
                "overrides",
                "protected",
                "structure",
                "writeonly",
            },
            new string [] { // 10 characters
                "addhandler",
                "directcast",
                "implements",
                "paramarray",
                "raiseevent",
                "withevents",
            },
            new string[] {  // 11 characters
                "mustinherit",
                "overridable",
            },
            new string[] { // 12 characters
                "mustoverride",
            },
            new string [] { // 13 characters
                "removehandler",
            },
            // class_finalize and class_initialize are not keywords anymore,
            // but it will be nice to escape them to avoid warning
            new string [] { // 14 characters
                "class_finalize",
                "notinheritable",
                "notoverridable",
            },
            null,           // 15 characters
            new string [] {
                "class_initialize",
            }
        };

        internal VBCodeGenerator()
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

        public static bool IsKeyword(string value)
        {
            return s_fixedStringLookup.ContainsKey(value);
        }
    }  // VBModifierAttributeConverter

    #endregion class VBModifierAttributeConverter
}  // namespace

