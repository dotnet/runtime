// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using System.CodeDom;

namespace System.CodeDom.Compiler
{
    internal abstract class CodeDomProvider
    {
        /// <devdoc>
        ///    <para>
        ///       Creates an assembly based on options, with the information from the compile units
        ///    </para>
        /// </devdoc>
        public virtual CompilerResults CompileAssemblyFromDom(CompilerParameters options, params CodeCompileUnit[] compilationUnits)
        {
            return CreateCompilerHelper().CompileAssemblyFromDomBatch(options, compilationUnits);
        }

        /// <devdoc>
        ///    <para>
        ///       Creates an assembly based on options, with the information from sources
        ///    </para>
        /// </devdoc>
        public virtual CompilerResults CompileAssemblyFromSource(CompilerParameters options, params string[] sources)
        {
            return CreateCompilerHelper().CompileAssemblyFromSourceBatch(options, sources);
        }

        public virtual bool Supports(GeneratorSupport generatorSupport)
        {
            return CreateGeneratorHelper().Supports(generatorSupport);
        }

#pragma warning disable 618
        private ICodeCompiler CreateCompilerHelper()
        {
            ICodeCompiler compiler = CreateCompiler();
            if (compiler == null)
            {
                throw new NotImplementedException(SR.NotSupported_CodeDomAPI);
            }
            return compiler;
        }

        private ICodeGenerator CreateGeneratorHelper()
        {
            ICodeGenerator generator = CreateGenerator();
            if (generator == null)
            {
                throw new NotImplementedException(SR.NotSupported_CodeDomAPI);
            }
            return generator;
        }
#pragma warning restore 618

        [Obsolete("Callers should not use the ICodeCompiler interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public abstract ICodeCompiler CreateCompiler();

        [Obsolete("Callers should not use the ICodeGenerator interface and should instead use the methods directly on the CodeDomProvider class. Those inheriting from CodeDomProvider must still implement this interface, and should exclude this warning or also obsolete this method.")]
        public abstract ICodeGenerator CreateGenerator();

        public abstract string CreateEscapedIdentifier(string name);

        public static CompilerInfo GetCompilerInfo(String language)
        {
            CompilerInfo compilerInfo = GetCompilerInfoForLanguageNoThrow(language);
            if (compilerInfo == null)
                throw new Exception(SR.CodeDomProvider_NotDefined);
            return compilerInfo;
        }

        // Do argument validation but don't throw if there's no compiler defined for a language.
        private static CompilerInfo GetCompilerInfoForLanguageNoThrow(String language)
        {
            if (language == null)
                throw new ArgumentNullException("language");

            CompilerInfo compilerInfo = (CompilerInfo)Config._compilerLanguages[language.Trim()];
            return compilerInfo;
        }

        // Don't cache the configuration since things are different for asp.net scenarios.
        private static CodeDomCompilationConfiguration Config
        {
            get
            {
                return CodeDomCompilationConfiguration.Default;
            }
        }
    }
}

namespace Microsoft.CSharp
{
    // Ported from \corefx\src\System.Xml.XmlSerializer\src\System\CodeDom\Compiler\CodeDomProvider.cs
    internal class CSharpCodeProvider : CodeDomProvider
    {
        private CSharpCodeGenerator _generator;
        private static Dictionary<string, object> s_fixedStringLookup;

        public CSharpCodeProvider()
        {
            _generator = new CSharpCodeGenerator();
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

    #region class CSharpCodeGenerator

    /// <devdoc>
    ///    <para>
    ///       C# (C Sharp) Code Generator.
    ///    </para>
    /// </devdoc>
    internal class CSharpCodeGenerator //: ICodeCompiler, ICodeGenerator
    {
        internal CSharpCodeGenerator()
        {
        }
    }  // CSharpCodeGenerator

    #endregion class CSharpCodeGenerator
}
