// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CSharp;
using Microsoft.VisualBasic;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CodeDomProviderTests
    {
        [Fact]
        public void GetAllCompilerInfo_ReturnsMinimumOfCSharpAndVB()
        {
            Type[] compilerInfos = CodeDomProvider.GetAllCompilerInfo().Where(provider => provider.IsCodeDomProviderTypeValid).Select(provider => provider.CodeDomProviderType).ToArray();
            Assert.True(compilerInfos.Length >= 2);
            Assert.Contains(typeof(CSharpCodeProvider), compilerInfos);
            Assert.Contains(typeof(VBCodeProvider), compilerInfos);
        }

        [Fact]
        public void FileExtension_ReturnsEmpty()
        {
            Assert.Empty(new NullProvider().FileExtension);
        }

        [Fact]
        public void LanguageOptions_ReturnsNone()
        {
            Assert.Equal(LanguageOptions.None, new NullProvider().LanguageOptions);
        }

        [Fact]
        public void CreateGenerator_ReturnsOverridenGenerator()
        {
#pragma warning disable 0618
            CustomProvider provider = new CustomProvider();
            Assert.Same(provider.CreateGenerator(), provider.CreateGenerator("fileName"));
            Assert.Same(provider.CreateGenerator(), provider.CreateGenerator(new StringWriter()));
#pragma warning restore 0618
        }

        [Fact]
        public void CreateParser_ReturnsNull()
        {
#pragma warning disable 0618
            Assert.Null(new NoParserProvider().CreateParser());
#pragma warning restore 0618
        }

        [Fact]
        public void GetConverter_ReturnsNotNull()
        {
            Assert.NotNull(new CustomProvider().GetConverter(typeof(int)));
        }

        [Fact]
        public void GetConverter_NullType_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => new CustomProvider().GetConverter(null));
        }

        public static IEnumerable<object[]> CreateProvider_String_TestData()
        {
            yield return new object[] { "c#", "cs" };
            yield return new object[] { "  c#  ", "cs" };
            yield return new object[] { "cs", "cs" };
            yield return new object[] { "csharp", "cs" };
            yield return new object[] { "CsHaRp", "cs" };

            yield return new object[] { "vb", "vb" };
            yield return new object[] { "vbs", "vb" };
            yield return new object[] { "visualbasic", "vb" };
            yield return new object[] { "vbscript", "vb" };
            yield return new object[] { "VBSCRIPT", "vb" };
        }

        [Theory]
        [MemberData(nameof(CreateProvider_String_TestData))]
        public void CreateProvider_String(string language, string expectedFileExtension)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider(language);
            Assert.Equal(expectedFileExtension, provider.FileExtension);
        }

        public static IEnumerable<object[]> CreateProvider_String_Dictionary_TestData()
        {
            yield return new object[] { "cs", new Dictionary<string, string>(), "cs" };
            yield return new object[] { "cs", new Dictionary<string, string>() { { "option", "value" } }, "cs" };
            yield return new object[] { "cs", new Dictionary<string, string>() { { "option1", "value1" }, { "option2", "value2" } }, "cs" };
            yield return new object[] { "cs", new Dictionary<string, string>() { { "option", null } }, "cs" };
            yield return new object[] { "vb", new Dictionary<string, string>(), "vb" };
            yield return new object[] { "vb", new Dictionary<string, string>() { { "option", "value" } }, "vb" };
            yield return new object[] { "vb", new Dictionary<string, string>() { { "option1", "value1" }, { "option2", "value2" } }, "vb" };
            yield return new object[] { "vb", new Dictionary<string, string>() { { "option", null } }, "vb" };
        }

        [Theory]
        [MemberData(nameof(CreateProvider_String_Dictionary_TestData))]
        public void CreateProvider_String_Dictionary(string language, Dictionary<string, string> providerOptions, string expectedFileExtension)
        {
            CodeDomProvider provider = CodeDomProvider.CreateProvider(language, providerOptions);
            Assert.Equal(expectedFileExtension, provider.FileExtension);
        }

        [Fact]
        public void CreateProvider_NullProviderOptions_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("providerOptions", () => CodeDomProvider.CreateProvider("cs", null));
            AssertExtensions.Throws<ArgumentNullException>("providerOptions", () => CodeDomProvider.CreateProvider("vb", null));
        }

        [Fact]
        public void CreateProvider_NullLanguage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("language", () => CodeDomProvider.CreateProvider(null));
            AssertExtensions.Throws<ArgumentNullException>("language", () => CodeDomProvider.CreateProvider(null, new Dictionary<string, string>()));
        }

        [Theory]
        [InlineData("")]
        [InlineData(".cs")]
        [InlineData("no-such-language")]
        public void CreateProvider_NoSuchLanguage_ThrowsConfigurationErrorsException(string language)
        {
            Exception ex1 = Assert.ThrowsAny<Exception>(() => CodeDomProvider.CreateProvider(language));
            AssertIsConfigurationErrorsException(ex1);

            Exception ex2 = Assert.ThrowsAny<Exception>(() => CodeDomProvider.CreateProvider(language, new Dictionary<string, string>()));
            AssertIsConfigurationErrorsException(ex2);
        }

        [Theory]
        [InlineData("  cs  ", true)]
        [InlineData("cs", true)]
        [InlineData("c#", true)]
        [InlineData("csharp", true)]
        [InlineData("CsHaRp", true)]
        [InlineData("vb", true)]
        [InlineData("vbs", true)]
        [InlineData("visualbasic", true)]
        [InlineData("vbscript", true)]
        [InlineData("VB", true)]
        [InlineData("", false)]
        [InlineData(".cs", false)]
        [InlineData("no-such-language", false)]
        public void IsDefinedLanguage_ReturnsExpected(string language, bool expected)
        {
            Assert.Equal(expected, CodeDomProvider.IsDefinedLanguage(language));
        }

        [Fact]
        public void IsDefinedLanguage_NullLanguage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("language", () => CodeDomProvider.IsDefinedLanguage(null));
        }

        [Theory]
        [InlineData("cs", "c#")]
        [InlineData("  cs  ", "c#")]
        [InlineData(".cs", "c#")]
        [InlineData("Cs", "c#")]
        [InlineData("cs", "c#")]
        [InlineData("vb", "vb")]
        [InlineData(".vb", "vb")]
        [InlineData("VB", "vb")]
        public void GetLanguageFromExtension_ReturnsExpected(string extension, string expected)
        {
            Assert.Equal(expected, CodeDomProvider.GetLanguageFromExtension(extension));
        }

        [Fact]
        public void GetLanguageFromExtension_NullExtension_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("extension", () => CodeDomProvider.GetLanguageFromExtension(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("c#")]
        [InlineData("no-such-extension")]
        public void GetLanguageFromExtension_NoSuchExtension_ThrowsConfigurationErrorsException(string extension)
        {
            Exception ex = Assert.ThrowsAny<Exception>(() => CodeDomProvider.GetLanguageFromExtension(extension));
            AssertIsConfigurationErrorsException(ex);
        }

        [Theory]
        [InlineData("cs", true)]
        [InlineData(".cs", true)]
        [InlineData("Cs", true)]
        [InlineData("cs", true)]
        [InlineData("vb", true)]
        [InlineData(".vb", true)]
        [InlineData("VB", true)]
        [InlineData("", false)]
        [InlineData("c#", false)]
        [InlineData("no-such-extension", false)]
        public void IsDefinedExtension_ReturnsExpected(string extension, bool expected)
        {
            Assert.Equal(expected, CodeDomProvider.IsDefinedExtension(extension));
        }

        [Fact]
        public void IsDefinedExtension_NullExtension_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("extension", () => CodeDomProvider.IsDefinedExtension(null));
        }

        [Theory]
        [InlineData("  cs  ", typeof(CSharpCodeProvider))]
        [InlineData("cs", typeof(CSharpCodeProvider))]
        [InlineData("c#", typeof(CSharpCodeProvider))]
        [InlineData("csharp", typeof(CSharpCodeProvider))]
        [InlineData("CsHaRp", typeof(CSharpCodeProvider))]
        [InlineData("vb", typeof(VBCodeProvider))]
        [InlineData("vbs", typeof(VBCodeProvider))]
        [InlineData("visualbasic", typeof(VBCodeProvider))]
        [InlineData("vbscript", typeof(VBCodeProvider))]
        [InlineData("VB", typeof(VBCodeProvider))]
        public void GetCompilerInfo_ReturnsExpected(string language, Type expectedProviderType)
        {
            Assert.Equal(expectedProviderType, CodeDomProvider.GetCompilerInfo(language).CodeDomProviderType);
        }

        [Fact]
        public void GetCompilerInfo_NullLanguage_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("language", () => CodeDomProvider.GetCompilerInfo(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(".cs")]
        [InlineData("no-such-extension")]
        public void GetCompilerInfo_NoSuchExtension_ThrowsKeyNotFoundException(string language)
        {
            Exception ex = Assert.ThrowsAny<Exception>(() => CodeDomProvider.GetCompilerInfo(language));
            AssertIsConfigurationErrorsException(ex);
        }

        [Fact]
        public void CompileAssemblyFromDom_CallsCompilerMethod()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomProvider().CompileAssemblyFromDom(new CompilerParameters()));
        }

        [Fact]
        public void CompileAssemblyFromDom_NullCompiler_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().CompileAssemblyFromDom(new CompilerParameters()));
        }

        [Fact]
        public void CompileAssemblyFromFile_CallsCompilerMethod()
        {
            AssertExtensions.Throws<ArgumentNullException>("2", () => new CustomProvider().CompileAssemblyFromFile(new CompilerParameters()));
        }

        [Fact]
        public void CompileAssemblyFromFile_NullCompiler_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().CompileAssemblyFromFile(new CompilerParameters()));
        }

        [Fact]
        public void CompileAssemblyFromSource_CallsCompilerMethod()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("3", () => new CustomProvider().CompileAssemblyFromSource(new CompilerParameters()));
        }

        [Fact]
        public void CompileAssemblyFromSource_NullCompiler_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().CompileAssemblyFromSource(new CompilerParameters()));
        }

        [Fact]
        public void CreateEscapedIdentifier_CallsGeneratorMethod()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomProvider().CreateEscapedIdentifier("value"));
        }

        [Fact]
        public void CreateEscapedIdentifier_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().CreateEscapedIdentifier("value"));
        }

        [Fact]
        public void CreateValidIdentifier_CallsGeneratorMethod()
        {
            AssertExtensions.Throws<ArgumentNullException>("2", () => new CustomProvider().CreateValidIdentifier("value"));
        }

        [Fact]
        public void CreateValidIdentifier_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().CreateValidIdentifier("value"));
        }

        [Fact]
        public void GenerateCodeFromCompileUnit_CallsGeneratorMethod()
        {
            AssertExtensions.Throws<ArgumentOutOfRangeException>("3", () => new CustomProvider().GenerateCodeFromCompileUnit(new CodeCompileUnit(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromCompileUnit_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromCompileUnit(new CodeCompileUnit(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromExpression_CallsGeneratorMethod()
        {
            Assert.Throws<ArithmeticException>(() => new CustomProvider().GenerateCodeFromExpression(new CodeExpression(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromExpression_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromExpression(new CodeExpression(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromNamespace_CallsGeneratorMethod()
        {
            Assert.Throws<ArrayTypeMismatchException>(() => new CustomProvider().GenerateCodeFromNamespace(new CodeNamespace(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromNamespace_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromNamespace(new CodeNamespace(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromStatement_CallsGeneratorMethod()
        {
            Assert.Throws<BadImageFormatException>(() => new CustomProvider().GenerateCodeFromStatement(new CodeStatement(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromStatement_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromStatement(new CodeStatement(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromType_CallsGeneratorMethod()
        {
            Assert.Throws<CannotUnloadAppDomainException>(() => new CustomProvider().GenerateCodeFromType(new CodeTypeDeclaration(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromType_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromType(new CodeTypeDeclaration(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void GetTypeOutput_CallsGeneratorMethod()
        {
            Assert.Throws<DataMisalignedException>(() => new CustomProvider().GetTypeOutput(new CodeTypeReference()));
        }

        [Fact]
        public void GetTypeOutput_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GetTypeOutput(new CodeTypeReference()));
        }

        [Fact]
        public void IsValidIdentifier_CallsGeneratorMethod()
        {
            Assert.Throws<DirectoryNotFoundException>(() => new CustomProvider().IsValidIdentifier("value"));
        }

        [Fact]
        public void IsValidIdentifier_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().IsValidIdentifier("value"));
        }

        [Fact]
        public void Supports_CallsGeneratorMethod()
        {
            Assert.Throws<DivideByZeroException>(() => new CustomProvider().Supports(GeneratorSupport.ArraysOfArrays));
        }

        [Fact]
        public void Supports_NullGenerator_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().Supports(GeneratorSupport.ArraysOfArrays));
        }

        [Fact]
        public void GenerateCodeFromMember_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().GenerateCodeFromMember(new CodeTypeMember(), new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        public void Parse_CallsParserMethod()
        {
            Assert.Same(CustomParser.CompileUnit, new CustomProvider().Parse(new StringReader("abc")));
        }

        [Fact]
        public void Parse_NullParser_ThrowsNotImplementedException()
        {
            Assert.Throws<NotImplementedException>(() => new NullProvider().Parse(new StringReader("abc")));
        }

        private static void AssertIsConfigurationErrorsException(Exception ex)
        {
            if (!PlatformDetection.IsNetNative) // Can't do internal Reflection
            {
                Assert.Equal("ConfigurationErrorsException", ex.GetType().Name);
            }
        }

        protected class NullProvider : CodeDomProvider
        {
#pragma warning disable 0672
            public override ICodeCompiler CreateCompiler() => null;
            public override ICodeParser CreateParser() => null;
            public override ICodeGenerator CreateGenerator() => null;
#pragma warning restore 067
        }

        protected class NoParserProvider : CodeDomProvider
        {
            public override ICodeCompiler CreateCompiler() => null;
            public override ICodeGenerator CreateGenerator() => null;
        }

        protected class CustomProvider : CodeDomProvider
        {
            private ICodeCompiler _compiler = new CustomCompiler();
            public override ICodeCompiler CreateCompiler() => _compiler;

            private ICodeGenerator _generator = new CustomGenerator();
            public override ICodeGenerator CreateGenerator() => _generator;

            private ICodeParser _parser = new CustomParser();
            public override ICodeParser CreateParser() => _parser;
        }

        protected class CustomCompiler : ICodeCompiler
        {
            public CompilerResults CompileAssemblyFromDom(CompilerParameters options, CodeCompileUnit compilationUnit) => null;

            public CompilerResults CompileAssemblyFromDomBatch(CompilerParameters options, CodeCompileUnit[] compilationUnits)
            {
                throw new ArgumentException("1");
            }

            public CompilerResults CompileAssemblyFromFile(CompilerParameters options, string fileName) => null;

            public CompilerResults CompileAssemblyFromFileBatch(CompilerParameters options, string[] fileNames)
            {
                throw new ArgumentNullException("2");
            }

            public CompilerResults CompileAssemblyFromSource(CompilerParameters options, string source) => null;

            public CompilerResults CompileAssemblyFromSourceBatch(CompilerParameters options, string[] sources)
            {
                throw new ArgumentOutOfRangeException("3");
            }
        }

        protected class CustomGenerator : ICodeGenerator
        {
            public string CreateEscapedIdentifier(string value)
            {
                throw new ArgumentException("1");
            }

            public string CreateValidIdentifier(string value)
            {
                throw new ArgumentNullException("2");
            }

            public void GenerateCodeFromCompileUnit(CodeCompileUnit e, TextWriter w, CodeGeneratorOptions o)
            {
                throw new ArgumentOutOfRangeException("3");
            }

            public void GenerateCodeFromExpression(CodeExpression e, TextWriter w, CodeGeneratorOptions o)
            {
                throw new ArithmeticException("4");
            }

            public void GenerateCodeFromNamespace(CodeNamespace e, TextWriter w, CodeGeneratorOptions o)
            {
                throw new ArrayTypeMismatchException("5");
            }

            public void GenerateCodeFromStatement(CodeStatement e, TextWriter w, CodeGeneratorOptions o)
            {
                throw new BadImageFormatException("6");
            }

            public void GenerateCodeFromType(CodeTypeDeclaration e, TextWriter w, CodeGeneratorOptions o)
            {
                throw new CannotUnloadAppDomainException("7");
            }

            public string GetTypeOutput(CodeTypeReference type)
            {
                throw new DataMisalignedException("8");
            }

            public bool IsValidIdentifier(string value)
            {
                throw new DirectoryNotFoundException("9");
            }

            public bool Supports(GeneratorSupport supports)
            {
                throw new DivideByZeroException("10");
            }

            public void ValidateIdentifier(string value)
            {
                throw new DllNotFoundException("11");
            }
        }

        protected class CustomParser : ICodeParser
        {
            public static CodeCompileUnit CompileUnit { get; } = new CodeCompileUnit();
            public CodeCompileUnit Parse(TextReader codeStream) => CompileUnit;
        }
    }
}
