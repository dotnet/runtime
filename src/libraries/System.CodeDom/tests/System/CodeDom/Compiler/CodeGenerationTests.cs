// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public abstract class CodeGenerationTests
    {
        [Fact]
        public void Roundtrip_Extension()
        {
            CodeDomProvider provider = GetProvider();
            string ext = provider.FileExtension;
            CodeDomProvider provider2 = CodeDomProvider.CreateProvider(CodeDomProvider.GetLanguageFromExtension(ext));
            Assert.Equal(provider.GetType(), provider2.GetType());
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [Fact]
        public void Compilation_NotSupported()
        {
            CodeDomProvider provider = GetProvider();

            var options = new CompilerParameters(new string[] { }, "test.exe");

            var cu = new CodeCompileUnit();
            var ns = new CodeNamespace("ns");
            var cd = new CodeTypeDeclaration("Program");
            var mm = new CodeEntryPointMethod();
            cd.Members.Add(mm);
            ns.Types.Add(cd);
            cu.Namespaces.Add(ns);

            string tempPath = Path.GetTempFileName();
            try
            {
                File.WriteAllText(tempPath, GetEmptyProgramSource());

                Assert.Throws<PlatformNotSupportedException>(() => provider.CompileAssemblyFromFile(options, tempPath));
                Assert.Throws<PlatformNotSupportedException>(() => provider.CompileAssemblyFromDom(options, cu));
                Assert.Throws<PlatformNotSupportedException>(() => provider.CompileAssemblyFromSource(options, GetEmptyProgramSource()));

#pragma warning disable 0618 // obsolete
                ICodeCompiler cc = provider.CreateCompiler();
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromDom(options, cu));
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromDomBatch(options, new[] { cu }));
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromFile(options, tempPath));
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromFileBatch(options, new[] { tempPath }));
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromSource(options, GetEmptyProgramSource()));
                Assert.Throws<PlatformNotSupportedException>(() => cc.CompileAssemblyFromSourceBatch(options, new[] { GetEmptyProgramSource() }));
#pragma warning restore 0618
            }
            finally
            {
                File.Delete(tempPath);
            }
        }

        [Fact]
        public void GetTypeConverter()
        {
            CodeDomProvider provider = GetProvider();
            TypeConverter tc;

            tc = provider.GetConverter(typeof(MemberAttributes));
            Assert.True(tc.CanConvertFrom(typeof(string)));
            Assert.True(tc.CanConvertTo(typeof(string)));

            tc = provider.GetConverter(typeof(TypeAttributes));
            Assert.True(tc.CanConvertFrom(typeof(string)));
            Assert.True(tc.CanConvertTo(typeof(string)));
        }

        [Fact]
        public void CodeSnippets()
        {
            var snippetStmt = new CodeSnippetStatement("blah");
            AssertEqual(snippetStmt, "blah");

            var snippetExpr = new CodeSnippetExpression("    blah   ");
            AssertEqual(snippetExpr, "    blah   ");

            var snippetCu = new CodeSnippetCompileUnit();
            snippetCu.Value = GetEmptyProgramSource();
            AssertEqual(snippetCu, GetEmptyProgramSource());
        }

        protected abstract CodeDomProvider GetProvider();
        protected abstract string GetEmptyProgramSource();

        protected static CodeStatement CreateVariableIncrementExpression(string variableName, object primitive) =>
            new CodeAssignStatement(
                new CodeVariableReferenceExpression(variableName),
                new CodeBinaryOperatorExpression(new CodeVariableReferenceExpression(variableName), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(primitive)));

        protected void AssertEqual(CodeObject c, string expected)
        {
            // Validate all identifiers are valid
            CodeGenerator.ValidateIdentifiers(c);

            // Generate code
            CodeDomProvider provider = GetProvider();
            string code = GenerateCode(c, provider);

            Assert.Equal(
                CoalesceWhitespace(LineEndingsHelper.Normalize(expected)),
                CoalesceWhitespace(code));            
        }

        protected void AssertEqualPreserveLineBreaks(CodeObject c, string expected)
        {
            // Validate all identifiers are valid
            CodeGenerator.ValidateIdentifiers(c);

            // Generate code
            CodeDomProvider provider = GetProvider();
            string code = GenerateCode(c, provider);

            // Make sure the code matches what we expected
            Assert.Equal(
                CoalesceWhitespace(LineEndingsHelper.Normalize(expected), preserveNewLines: true),
                CoalesceWhitespace(code, preserveNewLines: true));
        }

        private static string GenerateCode(CodeObject c, CodeDomProvider provider)
        {
            var sb = new StringBuilder();
            var writer = new StringWriter(sb);
            var options = new CodeGeneratorOptions();

            if (c is CodeStatement)
            {
                provider.GenerateCodeFromStatement((CodeStatement)c, writer, options);
            }
            else if (c is CodeCompileUnit)
            {
                provider.GenerateCodeFromCompileUnit((CodeCompileUnit)c, writer, options);
            }
            else if (c is CodeExpression)
            {
                provider.GenerateCodeFromExpression((CodeExpression)c, writer, options);
            }
            else if (c is CodeTypeMember)
            {
                provider.GenerateCodeFromMember((CodeTypeMember)c, writer, options);
            }
            else if (c is CodeTypeDeclaration)
            {
                provider.GenerateCodeFromType((CodeTypeDeclaration)c, writer, options);
            }
            else if (c is CodeNamespace)
            {
                provider.GenerateCodeFromNamespace((CodeNamespace)c, writer, options);
            }
            else
            {
                throw new ArgumentException($"Tests not set up for unexpected type: {c.GetType()}");
            }

            return sb.ToString();
        }

        private static string CoalesceWhitespace(string str, bool preserveNewLines = false)
        {
            var sb = new StringBuilder();
            bool lastWasWhitespace = false;
            foreach (char c in str)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (preserveNewLines && (c == '\r' || c == '\n'))
                    {
                        lastWasWhitespace = true;
                        sb.Append(c);
                    }
                    else if (lastWasWhitespace || sb.Length == 0) 
                    {
                        continue;
                    }
                    else
                    {
                        lastWasWhitespace = true;
                        sb.Append(' ');
                    }
                }
                else
                {
                    lastWasWhitespace = false;
                    sb.Append(c);
                }
            }
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
            {
                sb.Length--;
            }
            return sb.ToString();
        }
    }
}
