// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CSharp;
using Microsoft.VisualBasic;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace System.CodeDom.Compiler
{
    [ToolboxItem(false)]
    public abstract class CodeDomProvider : Component
    {
        private static readonly Dictionary<string, CompilerInfo> s_compilerLanguages = new Dictionary<string, CompilerInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, CompilerInfo> s_compilerExtensions = new Dictionary<string, CompilerInfo>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<CompilerInfo> s_allCompilerInfo = new List<CompilerInfo>();

        static CodeDomProvider()
        {
            // C#
            AddCompilerInfo(new CompilerInfo(new CompilerParameters() { WarningLevel = 4 }, typeof(CSharpCodeProvider).FullName)
            {
                _compilerLanguages = new string[] { "c#", "cs", "csharp" },
                _compilerExtensions = new string[] { ".cs", "cs" }
            });

            // VB
            AddCompilerInfo(new CompilerInfo(new CompilerParameters() { WarningLevel = 4 }, typeof(VBCodeProvider).FullName)
            {
                _compilerLanguages = new string[] { "vb", "vbs", "visualbasic", "vbscript" },
                _compilerExtensions = new string[] { ".vb", "vb" }
            });
        }

        private static void AddCompilerInfo(CompilerInfo compilerInfo)
        {
            foreach (string language in compilerInfo._compilerLanguages)
            {
                s_compilerLanguages[language] = compilerInfo;
            }

            foreach (string extension in compilerInfo._compilerExtensions)
            {
                s_compilerExtensions[extension] = compilerInfo;
            }

            s_allCompilerInfo.Add(compilerInfo);
        }


        public static CodeDomProvider CreateProvider(string language, System.Collections.Generic.IDictionary<string, string> providerOptions)
        {
            CompilerInfo compilerInfo = GetCompilerInfo(language);
            return compilerInfo.CreateProvider(providerOptions);
        }

        public static CodeDomProvider CreateProvider(string language)
        {
            CompilerInfo compilerInfo = GetCompilerInfo(language);
            return compilerInfo.CreateProvider();
        }

        public static string GetLanguageFromExtension(string extension)
        {
            CompilerInfo compilerInfo = GetCompilerInfoForExtensionNoThrow(extension);
            if (compilerInfo == null)
            {
                throw new ConfigurationErrorsException(SR.CodeDomProvider_NotDefined);
            }
            return compilerInfo._compilerLanguages[0];
        }

        public static bool IsDefinedLanguage(string language) => GetCompilerInfoForLanguageNoThrow(language) != null;

        public static bool IsDefinedExtension(string extension) => GetCompilerInfoForExtensionNoThrow(extension) != null;

        public static CompilerInfo GetCompilerInfo(string language)
        {
            CompilerInfo compilerInfo = GetCompilerInfoForLanguageNoThrow(language);
            if (compilerInfo == null)
            {
                throw new ConfigurationErrorsException(SR.CodeDomProvider_NotDefined);
            }
            return compilerInfo;
        }

        private static CompilerInfo GetCompilerInfoForLanguageNoThrow(string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            CompilerInfo value;
            s_compilerLanguages.TryGetValue(language.Trim(), out value);
            return value;
        }

        private static CompilerInfo GetCompilerInfoForExtensionNoThrow(string extension)
        {
            if (extension == null)
            {
                throw new ArgumentNullException(nameof(extension));
            }

            CompilerInfo value;
            s_compilerExtensions.TryGetValue(extension.Trim(), out value);
            return value;
        }

        public static CompilerInfo[] GetAllCompilerInfo() => s_allCompilerInfo.ToArray();

        public virtual string FileExtension => string.Empty;

        public virtual LanguageOptions LanguageOptions => LanguageOptions.None;

        [Obsolete("ICodeGenerator has been deprecated. Use the methods directly on the CodeDomProvider class instead. Classes inheriting from CodeDomProvider must still implement this interface, and should suppress this warning or also mark this method as obsolete.")]
        public abstract ICodeGenerator CreateGenerator();

#pragma warning disable 618 // obsolete
        public virtual ICodeGenerator CreateGenerator(TextWriter output) => CreateGenerator();

        public virtual ICodeGenerator CreateGenerator(string fileName) => CreateGenerator();
#pragma warning restore 618

        [Obsolete("ICodeCompiler has been deprecated. Use the methods directly on the CodeDomProvider class instead. Classes inheriting from CodeDomProvider must still implement this interface, and should suppress this warning or also mark this method as obsolete.")]
        public abstract ICodeCompiler CreateCompiler();

        [Obsolete("ICodeParser has been deprecated. Use the methods directly on the CodeDomProvider class instead. Classes inheriting from CodeDomProvider must still implement this interface, and should suppress this warning or also mark this method as obsolete.")]
        public virtual ICodeParser CreateParser() => null;

        public virtual TypeConverter GetConverter(Type type) => TypeDescriptor.GetConverter(type);

        public virtual CompilerResults CompileAssemblyFromDom(CompilerParameters options, params CodeCompileUnit[] compilationUnits) =>
            CreateCompilerHelper().CompileAssemblyFromDomBatch(options, compilationUnits);

        public virtual CompilerResults CompileAssemblyFromFile(CompilerParameters options, params string[] fileNames) =>
            CreateCompilerHelper().CompileAssemblyFromFileBatch(options, fileNames);

        public virtual CompilerResults CompileAssemblyFromSource(CompilerParameters options, params string[] sources) =>
            CreateCompilerHelper().CompileAssemblyFromSourceBatch(options, sources);

        public virtual bool IsValidIdentifier(string value) =>
            CreateGeneratorHelper().IsValidIdentifier(value);

        public virtual string CreateEscapedIdentifier(string value) =>
            CreateGeneratorHelper().CreateEscapedIdentifier(value);

        public virtual string CreateValidIdentifier(string value) =>
            CreateGeneratorHelper().CreateValidIdentifier(value);

        public virtual string GetTypeOutput(CodeTypeReference type) =>
            CreateGeneratorHelper().GetTypeOutput(type);

        public virtual bool Supports(GeneratorSupport generatorSupport) =>
            CreateGeneratorHelper().Supports(generatorSupport);

        public virtual void GenerateCodeFromExpression(CodeExpression expression, TextWriter writer, CodeGeneratorOptions options) =>
            CreateGeneratorHelper().GenerateCodeFromExpression(expression, writer, options);

        public virtual void GenerateCodeFromStatement(CodeStatement statement, TextWriter writer, CodeGeneratorOptions options) =>
            CreateGeneratorHelper().GenerateCodeFromStatement(statement, writer, options);

        public virtual void GenerateCodeFromNamespace(CodeNamespace codeNamespace, TextWriter writer, CodeGeneratorOptions options) =>
            CreateGeneratorHelper().GenerateCodeFromNamespace(codeNamespace, writer, options);

        public virtual void GenerateCodeFromCompileUnit(CodeCompileUnit compileUnit, TextWriter writer, CodeGeneratorOptions options) =>
            CreateGeneratorHelper().GenerateCodeFromCompileUnit(compileUnit, writer, options);

        public virtual void GenerateCodeFromType(CodeTypeDeclaration codeType, TextWriter writer, CodeGeneratorOptions options) =>
            CreateGeneratorHelper().GenerateCodeFromType(codeType, writer, options);

        public virtual void GenerateCodeFromMember(CodeTypeMember member, TextWriter writer, CodeGeneratorOptions options)
        {
            throw new NotImplementedException(SR.NotSupported_CodeDomAPI);
        }

        public virtual CodeCompileUnit Parse(TextReader codeStream) =>
            CreateParserHelper().Parse(codeStream);

#pragma warning disable 0618 // obsolete
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

        private ICodeParser CreateParserHelper()
        {
            ICodeParser parser = CreateParser();
            if (parser == null)
            {
                throw new NotImplementedException(SR.NotSupported_CodeDomAPI);
            }
            return parser;
        }
#pragma warning restore 618

        private sealed class ConfigurationErrorsException : SystemException
        {
            public ConfigurationErrorsException(string message) : base(message) { }
            public ConfigurationErrorsException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
