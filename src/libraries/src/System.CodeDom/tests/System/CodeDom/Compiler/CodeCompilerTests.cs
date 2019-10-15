// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CodeCompilerTests
    {
        public static IEnumerable<object[]> CodeCompileUnit_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CodeCompileUnit() };

            var unit = new CodeCompileUnit();
            unit.ReferencedAssemblies.Add("assembly1");
            unit.ReferencedAssemblies.Add("assembly2");
            yield return new object[] { unit };

            var referencedUnit = new CodeCompileUnit();
            referencedUnit.ReferencedAssemblies.Add("referenced");
            referencedUnit.ReferencedAssemblies.Add("assembly1");
            yield return new object[] { referencedUnit };
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnit_TestData))]
        public void CompileAssemblyFromDom_ValidCodeCompileUnit_ReturnsExpected(CodeCompileUnit compilationUnit)
        {
            ICodeCompiler compiler = new StubCompiler();
            var options = new CompilerParameters();
            options.ReferencedAssemblies.Add("referenced");
            Assert.Null(compiler.CompileAssemblyFromDom(options, compilationUnit));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnit_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromDom_ValidCodeCompileUnit_ThrowsPlatformNotSupportedException(CodeCompileUnit compilationUnit)
        {
            ICodeCompiler compiler = new Compiler();
            var options = new CompilerParameters();
            options.ReferencedAssemblies.Add("referenced");
            Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromDom(options, compilationUnit));
        }

        [Fact]
        public void CompileAssemblyFromDom_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromDom(null, new CodeCompileUnit()));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnit_TestData))]
        public void FromDom_ValidCodeCompileUnit_ReturnsExpected(CodeCompileUnit compilationUnit)
        {
            var compiler = new StubCompiler();
            var options = new CompilerParameters();
            options.ReferencedAssemblies.Add("referenced");
            Assert.Null(compiler.FromDomEntryPoint(options, compilationUnit));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnit_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromDom_ValidCodeCompileUnit_ThrowsPlatformNotSupportedException(CodeCompileUnit compilationUnit)
        {
            var compiler = new Compiler();
            var options = new CompilerParameters();
            options.ReferencedAssemblies.Add("referenced");
            Assert.Throws<PlatformNotSupportedException>(() => compiler.FromDomEntryPoint(options, compilationUnit));
        }

        [Fact]
        public void FromDom_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromDomEntryPoint(null, new CodeCompileUnit()));
        }

        public static IEnumerable<object[]> CodeCompileUnits_TestData()
        {
            yield return new object[] { new CodeCompileUnit[0] };
            yield return new object[] { new CodeCompileUnit[] { null } };

            var unit = new CodeCompileUnit();
            unit.ReferencedAssemblies.Add("assembly1");
            unit.ReferencedAssemblies.Add("assembly2");
            yield return new object[] { new CodeCompileUnit[] { new CodeCompileUnit(), unit } };
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnits_TestData))]
        public void CompileAssemblyFromDomBatch_ValidCodeCompileUnits_ReturnsExpected(CodeCompileUnit[] compilationUnits)
        {
            ICodeCompiler compiler = new StubCompiler();
            Assert.Null(compiler.CompileAssemblyFromDomBatch(new CompilerParameters(), compilationUnits));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnits_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromDomBatch_ValidCodeCompileUnits_ThrowsPlatformNotSupportedException(CodeCompileUnit[] compilationUnits)
        {
            ICodeCompiler compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromDomBatch(new CompilerParameters(), compilationUnits));
        }

        [Fact]
        public void CompileAssemblyFromDomBatch_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromDomBatch(null, new CodeCompileUnit[0]));
        }

        [Fact]
        public void CompileAssemblyFromDomBatch_NullCompileUnits_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("ea", () => compiler.CompileAssemblyFromDomBatch(new CompilerParameters(), null));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnits_TestData))]
        public void FromDomBatch_ValidCodeCompileUnits_ReturnsExpected(CodeCompileUnit[] compilationUnits)
        {
            var compiler = new StubCompiler();
            Assert.Null(compiler.FromDomBatchEntryPoint(new CompilerParameters(), compilationUnits));
        }

        [Theory]
        [MemberData(nameof(CodeCompileUnits_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromDomBatch_ValidCodeCompileUnits_ThrowsPlatformNotSupportedException(CodeCompileUnit[] compilationUnits)
        {
            var compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.FromDomBatchEntryPoint(new CompilerParameters(), compilationUnits));
        }

        [Fact]
        public void FromDomBatch_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromDomBatchEntryPoint(null, new CodeCompileUnit[0]));
        }

        [Fact]
        public void FromDomBatch_NullCompileUnits_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("ea", () => compiler.FromDomBatchEntryPoint(new CompilerParameters(), null));
        }

        [Fact]
        public void CompileAssemblyFromFile_FileExists_ReturnsExpected()
        {
            ICodeCompiler compiler = new StubCompiler();
            using (var file = new TempFile(Path.GetTempFileName(), 0))
            {
                Assert.Null(compiler.CompileAssemblyFromFile(new CompilerParameters(), file.Path));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromFile_FileExists_ThrowsPlatformNotSupportedException()
        {
            ICodeCompiler compiler = new Compiler();
            using (var file = new TempFile(Path.GetTempFileName(), 0))
            {
                Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromFile(new CompilerParameters(), file.Path));
            }
        }

        [Fact]
        public void CompileAssemblyFromFile_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromFile(null, "fileName"));
        }

        [Fact]
        public void CompileAssemblyFromFile_NullFileName_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => compiler.CompileAssemblyFromFile(new CompilerParameters(), null));
        }

        [Fact]
        public void CompileAssemblyFromFile_EmptyFileName_ThrowsArgumentException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentException>("path", null, () => compiler.CompileAssemblyFromFile(new CompilerParameters(), ""));
        }

        [Fact]
        public void CompileAssemblyFromFile_NoSuchFile_ThrowsFileNotFoundException()
        {
            ICodeCompiler compiler = new Compiler();
            Assert.Throws<FileNotFoundException>(() => compiler.CompileAssemblyFromFile(new CompilerParameters(), "noSuchFile"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromFile_FileExists_ThrowsPlatformNotSupportedException()
        {
            var compiler = new Compiler();
            using (var file = new TempFile(Path.GetTempFileName(), 0))
            {
                Assert.Throws<PlatformNotSupportedException>(() => compiler.FromFileEntryPoint(new CompilerParameters(), file.Path));
            }
        }

        [Fact]
        public void FromFile_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromFileEntryPoint(null, "fileName"));
        }

        [Fact]
        public void FromFile_NullFileName_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("fileName", () => compiler.FromFileEntryPoint(new CompilerParameters(), null));
        }

        [Fact]
        public void FromFile_EmptyFileName_ThrowsArgumentException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentException>("path", null, () => compiler.FromFileEntryPoint(new CompilerParameters(), ""));
        }

        [Fact]
        public void FromFile_NoSuchFile_ThrowsFileNotFoundException()
        {
            var compiler = new Compiler();
            Assert.Throws<FileNotFoundException>(() => compiler.FromFileEntryPoint(new CompilerParameters(), "noSuchFile"));
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_ValidFileNames_ReturnsExpected()
        {
            using (var file = new TempFile(Path.GetTempFileName(), 0))
            {
                ICodeCompiler compiler = new StubCompiler();
                Assert.Null(compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), new string[] { file.Path }));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromFileBatch_ValidFileNames_ThrowsPlatformNotSupportedException()
        {
            using (var file = new TempFile(Path.GetTempFileName(), 0))
            {
                ICodeCompiler compiler = new Compiler();
                Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), new string[] { file.Path }));
            }
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromFileBatch(null, new string[0]));
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_NullFileNames_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("fileNames", () => compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), null));
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_NullFileNameInFileNames_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("path", () => compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), new string[] { null }));
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_EmptyFileNameInFileNames_ThrowsArgumentException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentException>("path", null, () => compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), new string[] { "" }));
        }

        [Fact]
        public void CompileAssemblyFromFileBatch_NoSuchFileInFileNames_ThrowsFileNotFoundException()
        {
            ICodeCompiler compiler = new Compiler();
            Assert.Throws<FileNotFoundException>(() => compiler.CompileAssemblyFromFileBatch(new CompilerParameters(), new string[] { "noSuchFile" }));
        }

        public static IEnumerable<object[]> FromFileBatch_TestData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { null } };
            yield return new object[] { new string[] { "" } };
            yield return new object[] { new string[] { "noSuchFile" } };
        }

        [Theory]
        [MemberData(nameof(FromFileBatch_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromFileBatch_ValidFileNames_ThrowsPlatformNotSupportedException(string[] fileNames)
        {
            var compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.FromFileBatchEntryPoint(new CompilerParameters(), fileNames));
        }

        [Fact]
        public void FromFileBatch_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromFileBatchEntryPoint(null, new string[0]));
        }

        [Fact]
        public void FromFileBatch_NullFileNames_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("fileNames", () => compiler.FromFileBatchEntryPoint(new CompilerParameters(), null));
        }

        public static IEnumerable<object[]> Source_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { "source" };
        }

        [Theory]
        [MemberData(nameof(Source_TestData))]
        public void CompileAssemblyFromSource_ValidSource_ReturnsExpected(string source)
        {
            ICodeCompiler compiler = new StubCompiler();
            Assert.Null(compiler.CompileAssemblyFromSource(new CompilerParameters(), source));
        }

        [Theory]
        [MemberData(nameof(Source_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromSource_ValidSource_ThrowsPlatformNotSupportedException(string source)
        {
            ICodeCompiler compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromSource(new CompilerParameters(), source));
        }

        [Fact]
        public void CompileAssemblyFromSource_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromSource(null, "source"));
        }

        [Theory]
        [MemberData(nameof(Source_TestData))]
        public void FromSource_ValidSource_ReturnsExpected(string source)
        {
            var compiler = new StubCompiler();
            Assert.Null(compiler.FromSourceEntryPoint(new CompilerParameters(), source));
        }

        [Theory]
        [MemberData(nameof(Source_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromSource_ValidSource_ThrowsPlatformNotSupportedException(string source)
        {
            var compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.FromSourceEntryPoint(new CompilerParameters(), source));
        }

        [Fact]
        public void FromSource_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromSourceEntryPoint(null, "source"));
        }

        public static IEnumerable<object[]> Sources_TestData()
        {
            yield return new object[] { new string[0] };
            yield return new object[] { new string[] { null } };
            yield return new object[] { new string[] { "" } };
            yield return new object[] { new string[] { "source1", "source2" } };
        }

        [Theory]
        [MemberData(nameof(Sources_TestData))]
        public void CompileAssemblyFromSourceBatch_ValidSources_ReturnsExpected(string[] sources)
        {
            ICodeCompiler compiler = new StubCompiler();
            Assert.Null(compiler.CompileAssemblyFromSourceBatch(new CompilerParameters(), sources));
        }

        [Theory]
        [MemberData(nameof(Sources_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void CompileAssemblyFromSourceBatch_ValidSources_ThrowsPlatformNotSupportedException(string[] sources)
        {
            ICodeCompiler compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.CompileAssemblyFromSourceBatch(new CompilerParameters(), sources));
        }

        [Fact]
        public void CompileAssemblyFromSourceBatch_NullOptions_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.CompileAssemblyFromSourceBatch(null, new string[] { "source" }));
        }

        [Fact]
        public void CompileAssemblyFromSourceBatch_NullSources_ThrowsArgumentNullException()
        {
            ICodeCompiler compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("sources", () => compiler.CompileAssemblyFromSourceBatch(new CompilerParameters(), null));
        }

        [Theory]
        [MemberData(nameof(Sources_TestData))]
        public void FromSourceBatch_ValidSources_ReturnsExpected(string[] sources)
        {
            var compiler = new StubCompiler();
            Assert.Null(compiler.FromSourceBatchEntryPoint(new CompilerParameters(), sources));
        }

        [Theory]
        [MemberData(nameof(Sources_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void FromSourceBatch_ValidSources_ThrowsPlatformNotSupportedException(string[] sources)
        {
            var compiler = new Compiler();
            Assert.Throws<PlatformNotSupportedException>(() => compiler.FromSourceBatchEntryPoint(new CompilerParameters(), sources));
        }

        [Fact]
        public void FromSourceBatch_NullOptions_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("options", () => compiler.FromSourceBatchEntryPoint(null, new string[] { "source" }));
        }

        [Fact]
        public void FromSourceBatch_NullSources_ThrowsArgumentNullException()
        {
            var compiler = new Compiler();
            AssertExtensions.Throws<ArgumentNullException>("sources", () => compiler.FromSourceBatchEntryPoint(new CompilerParameters(), null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("cmdArgs")]
        public void GetResponseFileCmdArgs_ValidCmdArgs_ReturnsExpected(string cmdArgs)
        {
            var compiler = new Compiler();
            string args = compiler.GetResponseFileCmdArgsEntryPoint(new CompilerParameters(), cmdArgs);
            Assert.StartsWith("@\"", args);
            Assert.EndsWith("\"", args);
        }

        [Fact]
        public void GetResponseFileCmdArgs_NullOptions_ThrowsNullReferenceException()
        {
            var compiler = new Compiler();
            Assert.Throws<NullReferenceException>(() => compiler.GetResponseFileCmdArgsEntryPoint(null, "cmdArgs"));
        }

        public static IEnumerable<object[]> JoinStringArray_TestData()
        {
            yield return new object[] { null, ", ", "" };
            yield return new object[] { new string[0], ", ", "" };
            yield return new object[] { new string[] { "a" }, ", ", "\"a\"" };
            yield return new object[] { new string[] { "a", "b" }, ", ", "\"a\", \"b\"" };
            yield return new object[] { new string[] { "a", "b" }, null, "\"a\"\"b\"" };
            yield return new object[] { new string[] { "a", "b" }, "", "\"a\"\"b\"" };
        }

        [Theory]
        [MemberData(nameof(JoinStringArray_TestData))]
        public void JoinStringArray_Invoke_ReturnsExpected(string[] sa, string separator, string expected)
        {
            var compiler = new Compiler();
            Assert.Equal(expected, compiler.JoinStringArrayEntryPoint(sa, separator));
        }

        public class Compiler : CodeCompiler
        {
            public CompilerResults FromDomEntryPoint(CompilerParameters options, CodeCompileUnit e) => FromDom(options, e);

            public CompilerResults FromDomBatchEntryPoint(CompilerParameters options, CodeCompileUnit[] ea) => FromDomBatch(options, ea);

            public CompilerResults FromFileEntryPoint(CompilerParameters options, string fileName) => FromFile(options, fileName);

            public CompilerResults FromFileBatchEntryPoint(CompilerParameters options, string[] fileNames) => FromFileBatch(options, fileNames);

            public CompilerResults FromSourceEntryPoint(CompilerParameters options, string source) => FromSource(options, source);

            public CompilerResults FromSourceBatchEntryPoint(CompilerParameters options, string[] sources) => FromSourceBatch(options, sources);

            public string GetResponseFileCmdArgsEntryPoint(CompilerParameters options, string cmdArgs) => GetResponseFileCmdArgs(options, cmdArgs);

            public string JoinStringArrayEntryPoint(string[] sa, string separator) => JoinStringArray(sa, separator);

            protected override string CompilerName => throw new NotImplementedException();

            protected override string FileExtension => ".cs";

            protected override string NullToken => throw new NotImplementedException();

            protected override string CmdArgsFromParameters(CompilerParameters options)
            {
                throw new NotImplementedException();
            }

            protected override string CreateEscapedIdentifier(string value)
            {
                throw new NotImplementedException();
            }

            protected override string CreateValidIdentifier(string value)
            {
                throw new NotImplementedException();
            }

            protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e) { }

            protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e) { }

            protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e) { }

            protected override void GenerateAssignStatement(CodeAssignStatement e) { }

            protected override void GenerateAttachEventStatement(CodeAttachEventStatement e) { }

            protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes) { }

            protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes) { }

            protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e) { }

            protected override void GenerateCastExpression(CodeCastExpression e) { }

            protected override void GenerateComment(CodeComment e) { }

            protected override void GenerateConditionStatement(CodeConditionStatement e) { }

            protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c) { }

            protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e) { }

            protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e) { }

            protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c) { }

            protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c) { }

            protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e) { }

            protected override void GenerateExpressionStatement(CodeExpressionStatement e) { }

            protected override void GenerateField(CodeMemberField e) { }

            protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e) { }

            protected override void GenerateGotoStatement(CodeGotoStatement e) { }

            protected override void GenerateIndexerExpression(CodeIndexerExpression e) { }

            protected override void GenerateIterationStatement(CodeIterationStatement e) { }

            protected override void GenerateLabeledStatement(CodeLabeledStatement e) { }

            protected override void GenerateLinePragmaEnd(CodeLinePragma e) { }

            protected override void GenerateLinePragmaStart(CodeLinePragma e) { }

            protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c) { }

            protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e) { }

            protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e) { }

            protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e) { }

            protected override void GenerateNamespaceEnd(CodeNamespace e) { }

            protected override void GenerateNamespaceImport(CodeNamespaceImport e) { }

            protected override void GenerateNamespaceStart(CodeNamespace e) { }

            protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e) { }

            protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c) { }

            protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e) { }

            protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e) { }

            protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e) { }

            protected override void GenerateSnippetExpression(CodeSnippetExpression e) { }

            protected override void GenerateSnippetMember(CodeSnippetTypeMember e) { }

            protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e) { }

            protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e) { }

            protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e) { }

            protected override void GenerateTypeConstructor(CodeTypeConstructor e) { }

            protected override void GenerateTypeEnd(CodeTypeDeclaration e) { }

            protected override void GenerateTypeStart(CodeTypeDeclaration e) { }

            protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e) { }

            protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e) { }

            protected override string GetTypeOutput(CodeTypeReference value)
            {
                throw new NotImplementedException();
            }

            protected override bool IsValidIdentifier(string value)
            {
                throw new NotImplementedException();
            }

            protected override void OutputType(CodeTypeReference typeRef)
            {
                throw new NotImplementedException();
            }

            protected override void ProcessCompilerOutputLine(CompilerResults results, string line)
            {
                throw new NotImplementedException();
            }

            protected override string QuoteSnippetString(string value)
            {
                throw new NotImplementedException();
            }

            protected override bool Supports(GeneratorSupport support)
            {
                throw new NotImplementedException();
            }
        }

        public class StubCompiler : Compiler
        {
            protected override CompilerResults FromFileBatch(CompilerParameters options, string[] fileNames)
            {
                return null;
            }
        }
    }
}
