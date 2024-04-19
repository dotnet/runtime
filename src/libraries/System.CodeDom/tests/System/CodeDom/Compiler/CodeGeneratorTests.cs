// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CodeGeneratorTests : CodeGenerator
    {
        [Fact]
        public void Ctor_Default()
        {
            CodeGeneratorTests generator = this;
            Assert.Null(generator.CurrentClass);
            Assert.Null(generator.CurrentMember);
            Assert.Equal("<% unknown %>", generator.CurrentMemberName);
            Assert.Equal("<% unknown %>", generator.CurrentTypeName);
            Assert.Throws<NullReferenceException>(() => generator.Indent);
            Assert.False(generator.IsCurrentClass);
            Assert.False(generator.IsCurrentDelegate);
            Assert.False(generator.IsCurrentEnum);
            Assert.False(generator.IsCurrentInterface);
            Assert.False(generator.IsCurrentStruct);
            Assert.Null(generator.Options);
            Assert.Null(generator.Output);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("st", "st")]
        public void ContinueOnNewLine_InvokeWithOutput_Appends(string st, string expected)
        {
            CodeGeneratorTests generator = this;
            generator.PerformActionWithOutput(writer =>
            {
                generator.ContinueOnNewLine(st);
                Assert.Equal(expected + Environment.NewLine, writer.ToString());
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("st")]
        public void ContinueOnNewLine_InvokeWithoutOutput_ThrowsNullReferenceException(string st)
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<NullReferenceException>(() => generator.ContinueOnNewLine(st));
        }

        [Theory]
        [InlineData(-1, 0)]
        [InlineData(0, 0)]
        [InlineData(3, 3)]
        public void Indent_SetWithOutput_GetReturnsExpected(int value, int expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.Indent = value;
                Assert.Equal(expected, generator.Indent);
            });
        }

        [Fact]
        public void Indent_SetWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<NullReferenceException>(() => generator.Indent = 1);
        }

        public static IEnumerable<object[]> GenerateBinaryOperatorExpression_TestData()
        {
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "(1 + 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodePrimitiveExpression(2)), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3)), $"((1 * 2) {Environment.NewLine}            + 3)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodeBinaryOperatorExpression(new CodePrimitiveExpression(2), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3))), $"(1 {Environment.NewLine}            * (2 + 3))" };
        }

        [Theory]
        [MemberData(nameof(GenerateBinaryOperatorExpression_TestData))]
        public void GenerateBinaryOperatorExpression_Invoke_Success(CodeBinaryOperatorExpression e, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
                generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateBinaryOperatorExpression(e);
                Assert.Equal(expected, writer.ToString());

                // Call again to make sure indent is reset.
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateBinaryOperatorExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateBinaryOperatorExpression(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateBinaryOperatorExpression_NullLeftE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeBinaryOperatorExpression(null, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1));
                generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                Assert.Throws<ArgumentNullException>("e", () => generator.GenerateBinaryOperatorExpression(null));
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateBinaryOperatorExpression_NullRightE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, null);
                generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                Assert.Throws<ArgumentNullException>("e", () => generator.GenerateBinaryOperatorExpression(null));
            });
        }

        [Fact]
        public void GenerateBinaryOperatorExpression_InvokeWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeBinaryOperatorExpression();
            generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.GenerateBinaryOperatorExpression(e));
        }

        public static IEnumerable<object[]> GenerateCodeFromMember_TestData()
        {
            yield return new object[] { new CodeTypeMember(), null, Environment.NewLine };
            yield return new object[] { new CodeTypeMember(), new CodeGeneratorOptions(), Environment.NewLine };
            yield return new object[] { new CodeTypeMember(), new CodeGeneratorOptions { BlankLinesBetweenMembers = false}, string.Empty };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromMember_TestData))]
        public void GenerateCodeFromMember_Invoke_Success(CodeTypeMember member, CodeGeneratorOptions options, string expected)
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            var writer = new StringWriter();
            generator.GenerateCodeFromMember(member, writer, options);
            Assert.Equal(expected, writer.ToString());
            Assert.Null(generator.Output);
            Assert.Null(generator.Options);
            Assert.Null(generator.CurrentClass);
            Assert.Null(generator.CurrentMember);
            Assert.Equal("<% unknown %>", generator.CurrentMemberName);
            Assert.Equal("<% unknown %>", generator.CurrentTypeName);
        }

        [Fact]
        public void GenerateCodeFromMember_InvokeWithCommentsDirectivesAndLinePragma_Success()
        {
            CodeGeneratorTests generator = this;
            var member = new CodeTypeMember { LinePragma = new CodeLinePragma() };
            member.Comments.Add(new CodeCommentStatement("Comment"));
            member.Comments.Add(new CodeCommentStatement("Comment"));
            member.StartDirectives.Add(new CodeDirective());
            member.StartDirectives.Add(new CodeChecksumPragma());
            member.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            member.EndDirectives.Add(new CodeDirective());
            member.EndDirectives.Add(new CodeChecksumPragma());
            member.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));

            var writer = new StringWriter();
            int generateCommentStatementsCallCount = 0;
            int generateCommentCallCount = 0;
            int generateLinePragmaStartCallCount = 0;
            int generateDirectivesCallCount = 0;
            int generateLinePragmaEndCallCount = 0;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) =>
            {
                baseMethod(actualE);
                Assert.Same(member.Comments, actualE);
                writer.Write("Comments ");
                Assert.Equal(0, generateLinePragmaStartCallCount);
                Assert.Equal(1, generateDirectivesCallCount);
                Assert.Equal(0, generateLinePragmaEndCallCount);
                generateCommentStatementsCallCount++;
            };
            generator.GenerateCommentAction = (actualE) =>
            {
                Assert.Same(member.Comments[generateCommentCallCount].Comment, actualE);
                writer.Write("Comment ");
                generateCommentCallCount++;
            };
            generator.GenerateLinePragmaStartAction = (actualE) =>
            {
                Assert.Same(member.LinePragma, actualE);
                Assert.Equal(0, generateLinePragmaEndCallCount);
                Assert.Equal(1, generateDirectivesCallCount);
                writer.Write("LinePragmaStart ");
                generateLinePragmaStartCallCount++;
            };
            generator.GenerateLinePragmaEndAction = (actualE) =>
            {
                Assert.Same(member.LinePragma, actualE);
                Assert.Equal(1, generateDirectivesCallCount);
                writer.Write("LinePragmaEnd ");
                generateLinePragmaEndCallCount++;
            };
            generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
            {
                baseMethod(actualDirectives);
                Assert.Same(generateDirectivesCallCount == 0 ? member.StartDirectives : member.EndDirectives, actualDirectives);
                writer.Write(generateDirectivesCallCount == 0 ? "StartDirectives " : "EndDirectives");
                generateDirectivesCallCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal($"{Environment.NewLine}StartDirectives Comment Comment Comments LinePragmaStart LinePragmaEnd EndDirectives", writer.ToString());
            Assert.Equal(1, generateCommentStatementsCallCount);
            Assert.Equal(2, generateCommentCallCount);
            Assert.Equal(1, generateLinePragmaStartCallCount);
            Assert.Equal(2, generateDirectivesCallCount);
            Assert.Equal(1, generateLinePragmaEndCallCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeConstructor_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeConstructor();
            int callCount = 0;
            generator.GenerateConstructorAction = (actualE, type) =>
            {
                Assert.Same(member, actualE);
                Assert.IsType<CodeTypeDeclaration>(type);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeEntryPointMethod_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeEntryPointMethod();
            int callCount = 0;
            generator.GenerateEntryPointMethodAction = (actualE, type) =>
            {
                Assert.Same(member, actualE);
                Assert.IsType<CodeTypeDeclaration>(type);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeMemberEvent_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeMemberEvent();
            int callCount = 0;
            generator.GenerateEventAction = (actualE, type) =>
            {
                Assert.Same(member, actualE);
                Assert.IsType<CodeTypeDeclaration>(type);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeMemberField_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeMemberField();
            int callCount = 0;
            generator.GenerateFieldAction = (actualE) =>
            {
                Assert.Same(member, actualE);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeMemberMethod_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeMemberMethod();
            int callCount = 0;
            generator.GenerateMethodAction = (actualE, type) =>
            {
                Assert.Same(member, actualE);
                Assert.IsType<CodeTypeDeclaration>(type);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeMemberProperty_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeMemberProperty();
            int callCount = 0;
            generator.GeneratePropertyAction = (actualE, type) =>
            {
                Assert.Same(member, actualE);
                Assert.IsType<CodeTypeDeclaration>(type);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_CodeSnippetTypeMember_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeSnippetTypeMember();
            int callCount = 0;
            generator.GenerateSnippetMemberAction = (actualE) =>
            {
                Assert.Same(member, actualE);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine + Environment.NewLine, writer.ToString());
        }

        [Fact]
        public void GenerateCodeFromMember_CodeTypeConstructor_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var writer = new StringWriter();
            var member = new CodeTypeConstructor();
            int callCount = 0;
            generator.GenerateTypeConstructorAction = (actualE) =>
            {
                Assert.Same(member, actualE);
                callCount++;
            };
            generator.GenerateCodeFromMember(member, writer, null);
            Assert.Equal(Environment.NewLine, writer.ToString());
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateCodeFromMember_InvokeWithOutput_ThrowsInvalidOperationException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                Assert.Throws<InvalidOperationException>(() => generator.GenerateCodeFromMember(new CodeTypeMember(), new StringWriter(), new CodeGeneratorOptions()));
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCodeFromMember_NullMember_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("member", () => generator.GenerateCodeFromMember(null, new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCodeFromMember_NullWriter_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("writer", () => generator.GenerateCodeFromMember(new CodeTypeMember(), null, new CodeGeneratorOptions()));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("text")]
        public void GenerateCommentStatement_Invoke_CallsCorrectMethod(string text)
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCommentStatement(text);
            int callCount = 0;
            generator.GenerateCommentAction = (actualComment) =>
            {
                Assert.Same(e.Comment, actualComment);
                callCount++;
            };
            generator.GenerateCommentStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCommentStatement_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCommentStatement(null));
        }

        [Fact]
        public void GenerateCommentStatement_NullEComment_ThrowsArgumentException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCommentStatement();
            Assert.Throws<ArgumentException>("e", () => generator.GenerateCommentStatement(e));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("text")]
        public void GenerateCommentStatements_InvokeNonEmpty_CallsCorrectMethod(string text)
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            var e = new CodeCommentStatementCollection(new CodeCommentStatement[]
            {
                new CodeCommentStatement(text),
                new CodeCommentStatement("otherText")
            });
            int callCount = 0;
            generator.GenerateCommentAction = (actualComment) =>
            {
                Assert.Same(e[callCount].Comment, actualComment);
                callCount++;
            };
            generator.GenerateCommentStatements(e);
            Assert.Equal(2, callCount);
        }

        [Fact]
        public void GenerateCommentStatements_InvokeEmptyE_Nop()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCommentStatements(new CodeCommentStatementCollection());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCommentStatements_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCommentStatements(null));
        }

        [Fact]
        public void GenerateCommentStatements_NullValueInE_ThrowsArgumentException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            var e = new CodeCommentStatementCollection(new CodeCommentStatement[] { new CodeCommentStatement() });
            Assert.Throws<ArgumentException>("e", () => generator.GenerateCommentStatements(e));
        }

        public static IEnumerable<object[]> GenerateCompileUnit_TestData()
        {
            yield return new object[] { new CodeCompileUnit() };
        }

        [Theory]
        [MemberData(nameof(GenerateCompileUnit_TestData))]
        public void GenerateCompileUnit_InvokeWithOutput_Success(CodeCompileUnit e)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateCompileUnitAction = (actualE, baseMethod) => baseMethod(actualE);
                int generateCompileUnitStartCallCount = 0;
                int generateCompileUnitEndCallCount = 0;
                generator.GenerateCompileUnitStartAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(e, actualE);
                    Assert.Equal(0, generateCompileUnitEndCallCount);
                    generateCompileUnitStartCallCount++;
                };
                generator.GenerateCompileUnitEndAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(e, actualE);
                    generateCompileUnitEndCallCount++;
                };
                generator.GenerateCompileUnit(e);
                Assert.Equal(1, generateCompileUnitStartCallCount);
                Assert.Equal(1, generateCompileUnitEndCallCount);
                Assert.Empty(writer.ToString());
            });
        }

        [Fact]
        public void GenerateCompileUnit_InvokeWithDirectives_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateCompileUnitAction = (actualE, baseMethod) => baseMethod(actualE);

                var e = new CodeSnippetCompileUnit("value") { LinePragma = new CodeLinePragma() };
                e.StartDirectives.Add(new CodeDirective());
                e.StartDirectives.Add(new CodeChecksumPragma());
                e.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                e.EndDirectives.Add(new CodeDirective());
                e.EndDirectives.Add(new CodeChecksumPragma());
                e.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                int generateCompileUnitStartCallCount = 0;
                int generateCompileUnitEndCallCount = 0;
                int generateDirectivesCallCount = 0;
                generator.GenerateCompileUnitStartAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(e, actualE);
                    Assert.Equal(0, generateCompileUnitEndCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    generateCompileUnitStartCallCount++;
                };
                generator.GenerateCompileUnitEndAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(e, actualE);
                    Assert.Equal(2, generateDirectivesCallCount);
                    generateCompileUnitEndCallCount++;
                };
                generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
                {
                    Assert.Same(generateDirectivesCallCount == 0 ? e.StartDirectives : e.EndDirectives, actualDirectives);
                    writer.Write(generateDirectivesCallCount == 0 ? "StartDirectives " : "EndDirectives");
                    generateDirectivesCallCount++;
                };
                generator.GenerateCompileUnit(e);
                Assert.Equal(1, generateCompileUnitStartCallCount);
                Assert.Equal(1, generateCompileUnitEndCallCount);
                Assert.Equal(2, generateDirectivesCallCount);
                Assert.Equal("StartDirectives EndDirectives", writer.ToString());
            });
        }

        [Theory]
        [MemberData(nameof(GenerateCompileUnit_TestData))]
        public void GenerateCompileUnit_InvokeWithoutOutput_Success(CodeCompileUnit e)
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCompileUnitAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateCompileUnitStartCallCount = 0;
            int generateCompileUnitEndCallCount = 0;
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                Assert.Equal(0, generateCompileUnitEndCallCount);
                generateCompileUnitStartCallCount++;
            };
            generator.GenerateCompileUnitEndAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                generateCompileUnitEndCallCount++;
            };
            generator.GenerateCompileUnit(e);
            Assert.Equal(1, generateCompileUnitStartCallCount);
            Assert.Equal(1, generateCompileUnitEndCallCount);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCompileUnit_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCompileUnitAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCompileUnit(null));
        }

        [Fact]
        public void GenerateCompileUnit_NullOutputWithNamespace_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCompileUnit();
            e.Namespaces.Add(new CodeNamespace("name"));
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCompileUnitAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.GenerateCompileUnit(e));
        }

        [Fact]
        public void GenerateCompileUnitEnd_InvokeWithEndDirectives_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCompileUnit();
            e.EndDirectives.Add(new CodeDirective());
            e.EndDirectives.Add(new CodeChecksumPragma());
            e.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            generator.GenerateCompileUnitEndAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateDirectivesCallCount = 0;
            generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
            {
                baseMethod(actualDirectives);
                Assert.Same(e.EndDirectives, actualDirectives);
                generateDirectivesCallCount++;
            };
            generator.GenerateCompileUnitEnd(e);
            Assert.Equal(1, generateDirectivesCallCount);
        }

        [Fact]
        public void GenerateCompileUnitEnd_InvokeWithoutEndDirectives_Nop()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCompileUnit();
            generator.GenerateCompileUnitEndAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCompileUnitEnd(e);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCompileUnitEnd_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCompileUnitEndAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCompileUnitEnd(null));
        }

        [Fact]
        public void GenerateCompileUnitStart_InvokeWithStartDirectives_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCompileUnit();
            e.StartDirectives.Add(new CodeDirective());
            e.StartDirectives.Add(new CodeChecksumPragma());
            e.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateDirectivesCallCount = 0;
            generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
            {
                baseMethod(actualDirectives);
                Assert.Same(e.StartDirectives, actualDirectives);
                generateDirectivesCallCount++;
            };
            generator.GenerateCompileUnitStart(e);
            Assert.Equal(1, generateDirectivesCallCount);
        }

        [Fact]
        public void GenerateCompileUnitStart_InvokeWithoutStartDirectives_Nop()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCompileUnit();
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCompileUnitStart(e);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateCompileUnitStart_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateCompileUnitStartAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCompileUnitStart(null));
        }

        [Fact]
        public void GenerateDecimalValue_Invoke_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateDecimalValueAction = (actualS, baseMethod) => baseMethod(actualS);
                generator.GenerateDecimalValue(decimal.MaxValue);
                Assert.Equal("79228162514264337593543950335", writer.ToString());
            });
        }

        [Fact]
        public void GenerateDecimalValue_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateDecimalValueAction = (actualS, baseMethod) => baseMethod(actualS);
            Assert.Throws<NullReferenceException>(() => generator.GenerateDecimalValue(1));
        }

        public static IEnumerable<object[]> GenerateDefaultValueExpression_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CodeDefaultValueExpression() };
        }

        [Theory]
        [MemberData(nameof(GenerateDefaultValueExpression_TestData))]
        public void GenerateDefaultValueExpression_InvokeWithOutput_Nop(CodeDefaultValueExpression e)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateDefaultValueExpressionAction = (actualE, baseMethod) => baseMethod(e);
                generator.GenerateDefaultValueExpression(e);
            });
        }

        [Theory]
        [MemberData(nameof(GenerateDefaultValueExpression_TestData))]
        public void GenerateDefaultValueExpression_InvokeWithoutOutput_Nop(CodeDefaultValueExpression e)
        {
            CodeGeneratorTests generator = this;
            generator.GenerateDefaultValueExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateDefaultValueExpression(e);
        }

        public static IEnumerable<object[]> GenerateDirectionExpression_TestData()
        {
            yield return new object[] { FieldDirection.In, "1" };
            yield return new object[] { FieldDirection.Out, "out 1" };
            yield return new object[] { FieldDirection.Ref, "ref 1" };
            yield return new object[] { FieldDirection.In - 1, "1" };
            yield return new object[] { FieldDirection.Ref + 1, "1" };
        }

        [Theory]
        [MemberData(nameof(GenerateDirectionExpression_TestData))]
        public void GenerateDirectionExpression_Invoke_Success(FieldDirection direction, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeDirectionExpression(direction, new CodePrimitiveExpression(1));
                generator.GenerateDirectionExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                int outputDirectionCallCount = 0;
                generator.OutputDirectionAction = (actualDirection, baseMethod) =>
                {
                    baseMethod(actualDirection);
                    Assert.Equal(e.Direction, actualDirection);
                    outputDirectionCallCount++;
                };
                generator.GenerateDirectionExpression(e);
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(1, outputDirectionCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateDirectionExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateDirectionExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateDirectionExpression(null));
        }

        [Fact]
        public void GenerateDirectionExpression_NullEExpression_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDirectionExpression();
            generator.GenerateDirectionExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateDirectionExpression(e));
        }

        [Theory]
        [InlineData(FieldDirection.Out)]
        [InlineData(FieldDirection.Ref)]
        public void GenerateDirectionExpression_InvokeNonInWithoutWriter_ThrowsNullReferenceException(FieldDirection direction)
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDirectionExpression(direction, new CodePrimitiveExpression(1));
            generator.GenerateDirectionExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.GenerateDirectionExpression(e));
        }

        public static IEnumerable<object[]> GenerateDirectives_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { new CodeDirectiveCollection() };
            yield return new object[] { new CodeDirectiveCollection(new CodeDirective[] { new CodeDirective() }) };
        }

        [Theory]
        [MemberData(nameof(GenerateDirectives_TestData))]
        public void GenerateDirectives_InvokeWithOutput_Nop(CodeDirectiveCollection directives)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateDirectivesAction = (actualDirectives, baseMethod) => baseMethod(actualDirectives);
                generator.GenerateDirectives(directives);
            });
        }

        [Theory]
        [MemberData(nameof(GenerateDirectives_TestData))]
        public void GenerateDirectives_InvokeWithoutOutput_Nop(CodeDirectiveCollection directives)
        {
            CodeGeneratorTests generator = this;
            generator.GenerateDirectivesAction = (actualDirectives, baseMethod) => baseMethod(actualDirectives);
            generator.GenerateDirectives(directives);
        }

        [Fact]
        public void GenerateDoubleValue_Invoke_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateDoubleValueAction = (actualS, baseMethod) => baseMethod(actualS);
                generator.GenerateDoubleValue(double.MaxValue);
                Assert.Equal("1.7976931348623157E+308", writer.ToString());
            });
        }

        [Fact]
        public void GenerateDoubleValue_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateDoubleValueAction = (actualS, baseMethod) => baseMethod(actualS);
            Assert.Throws<NullReferenceException>(() => generator.GenerateDoubleValue(1));
        }

        [Fact]
        public void GenerateExpression_CodeArgumentReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeArgumentReferenceExpression();
            int callCount = 0;
            generator.GenerateArgumentReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeArrayCreateExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeArrayCreateExpression();
            int callCount = 0;
            generator.GenerateArrayCreateExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeArrayIndexerExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeArrayIndexerExpression();
            int callCount = 0;
            generator.GenerateArrayIndexerExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeBaseReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeBaseReferenceExpression();
            int callCount = 0;
            generator.GenerateBaseReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeCastExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeCastExpression();
            int callCount = 0;
            generator.GenerateCastExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeDefaultValueExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDefaultValueExpression();
            int callCount = 0;
            generator.GenerateDefaultValueExpressionAction = (actualE, baseMethod) =>
            {
                baseMethod(actualE);
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeDelegateCreateExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDelegateCreateExpression();
            int callCount = 0;
            generator.GenerateDelegateCreateExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeDelegateInvokeExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDelegateInvokeExpression();
            int callCount = 0;
            generator.GenerateDelegateInvokeExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeDirectionExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeDirectionExpression();
            int callCount = 0;
            generator.GenerateDirectionExpressionAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeEventReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeEventReferenceExpression();
            int callCount = 0;
            generator.GenerateEventReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeFieldReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeFieldReferenceExpression();
            int callCount = 0;
            generator.GenerateFieldReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeIndexerExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeIndexerExpression();
            int callCount = 0;
            generator.GenerateIndexerExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeMethodInvokeExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeMethodInvokeExpression();
            int callCount = 0;
            generator.GenerateMethodInvokeExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeMethodReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeMethodReferenceExpression();
            int callCount = 0;
            generator.GenerateMethodReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeObjectCreateExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeObjectCreateExpression();
            int callCount = 0;
            generator.GenerateObjectCreateExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeParameterDeclarationExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeParameterDeclarationExpression();
            int callCount = 0;
            generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodePrimitiveExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression();
            int callCount = 0;
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodePropertyReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePropertyReferenceExpression();
            int callCount = 0;
            generator.GeneratePropertyReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodePropertySetValueReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePropertySetValueReferenceExpression();
            int callCount = 0;
            generator.GeneratePropertySetValueReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeSnippetExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeSnippetExpression();
            int callCount = 0;
            generator.GenerateSnippetExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeThisReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeThisReferenceExpression();
            int callCount = 0;
            generator.GenerateThisReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeTypeOfExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeTypeOfExpression();
            int callCount = 0;
            generator.GenerateTypeOfExpressionAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateExpression_CodeTypeReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeTypeReferenceExpression();
            int callCount = 0;
            generator.GenerateTypeReferenceExpressionAction = (actualE, baseMethod) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateVariableReferenceExpression_CodeVariableReferenceExpression_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeVariableReferenceExpression();
            int callCount = 0;
            generator.GenerateVariableReferenceExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateExpression(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateExpression(null));
        }

        public static IEnumerable<object[]> GenerateExpression_InvalidE_TestData()
        {
            yield return new object[] { new CodeExpression() };
            yield return new object[] { new CustomCodeExpression() };
        }

        [Theory]
        [MemberData(nameof(GenerateExpression_InvalidE_TestData))]
        public void GenerateExpression_InvalidE_ThrowsArgumentException(CodeExpression e)
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentException>("e", () => generator.GenerateExpression(e));
        }

        [Fact]
        public void GenerateNamespace_InvokeEmpty_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                int generateNamespaceStartCallCount = 0;
                int generateNamespaceEndCallCount = 0;
                generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateNamespaceStartAction = (actualE) =>
                {
                    Assert.Same(e, actualE);
                    Assert.Equal(0, generateNamespaceEndCallCount);
                    writer.Write("NamespaceStart ");
                    generateNamespaceStartCallCount++;
                };
                generator.GenerateNamespaceEndAction = (actualE) =>
                {
                    Assert.Same(e, actualE);
                    writer.Write("NamespaceEnd");
                    generateNamespaceEndCallCount++;
                };

                generator.GenerateNamespace(e);
                Assert.Equal(1, generateNamespaceStartCallCount);
                Assert.Equal(1, generateNamespaceEndCallCount);
                Assert.Equal($"NamespaceStart {Environment.NewLine}NamespaceEnd", writer.ToString());
            });
        }

        [Fact]
        public void GenerateNamespace_InvokeWithComments_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Comments.Add(new CodeCommentStatement("Comment"));
                e.Comments.Add(new CodeCommentStatement("Comment"));
                int generateCommentStatementsCallCount = 0;
                int generateCommentCallCount = 0;
                int generateNamespaceStartCallCount = 0;
                int generateNamespaceEndCallCount = 0;
                generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateCommentStatementsAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(e.Comments, actualE);
                    Assert.Equal(0, generateNamespaceStartCallCount);
                    Assert.Equal(0, generateNamespaceEndCallCount);
                    writer.Write("Comments ");
                    generateCommentStatementsCallCount++;
                };
                generator.GenerateCommentAction = (actualE) =>
                {
                    Assert.Same(e.Comments[generateCommentCallCount].Comment, actualE);
                    writer.Write("Comment ");
                    generateCommentCallCount++;
                };
                generator.GenerateNamespaceStartAction = (actualE) =>
                {
                    Assert.Same(e, actualE);
                    Assert.Equal(0, generateNamespaceEndCallCount);
                    writer.Write("NamespaceStart ");
                    generateNamespaceStartCallCount++;
                };
                generator.GenerateNamespaceEndAction = (actualE) =>
                {
                    Assert.Same(e, actualE);
                    writer.Write("NamespaceEnd");
                    generateNamespaceEndCallCount++;
                };

                generator.GenerateNamespace(e);
                Assert.Equal(1, generateCommentStatementsCallCount);
                Assert.Equal(2, generateCommentCallCount);
                Assert.Equal(1, generateNamespaceStartCallCount);
                Assert.Equal(1, generateNamespaceEndCallCount);
                Assert.Equal($"Comment Comment Comments NamespaceStart {Environment.NewLine}NamespaceEnd", writer.ToString());
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateNamespace_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateNamespace(null));
        }

        [Fact]
        public void GenerateNamespace_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeNamespace();
            generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateNamespaceStartAction = (actualE) => {};
            generator.GenerateNamespaceEndAction = (actualE) => {};

            Assert.Throws<NullReferenceException>(() => generator.GenerateNamespace(e));
        }

        [Fact]
        public void GenerateNamespace_NullValueInE_ThrowsArgumentException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Comments.Add(new CodeCommentStatement());
                generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateNamespaceStartAction = (actualE) => {};
                generator.GenerateNamespaceEndAction = (actualE) => {};

                Assert.Throws<ArgumentException>("e", () => generator.GenerateNamespace(e));
            });
        }

        [Fact]
        public void GenerateNamespaceImports_InvokeEmptyWithOutput_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                int generateNamespaceCallCount = 0;
                generator.GenerateNamespaceImportAction = (actualE) => generateNamespaceCallCount++;
                generator.GenerateNamespaceImports(e);
                Assert.Equal(0, generateNamespaceCallCount);
                Assert.Empty(writer.ToString());
            });
        }

        [Fact]
        public void GenerateNamespaceImports_InvokeNonEmptyWithOutput_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Imports.Add(new CodeNamespaceImport("Namespace1"));
                e.Imports.Add(new CodeNamespaceImport("Namespace2"));
                int generateNamespaceCallCount = 0;
                generator.GenerateNamespaceImportAction = (actualE) =>
                {
                    Assert.Same(e.Imports[generateNamespaceCallCount], actualE);
                    generateNamespaceCallCount++;
                };
                generator.GenerateNamespaceImports(e);
                Assert.Equal(2, generateNamespaceCallCount);
                Assert.Empty(writer.ToString());
            });
        }

        [Fact]
        public void GenerateNamespaceImports_InvokeWithOutputWithLinePragma_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Imports.Add(new CodeNamespaceImport("Namespace1") { LinePragma = new CodeLinePragma() });
                e.Imports.Add(new CodeNamespaceImport("Namespace2") { LinePragma = new CodeLinePragma() });
                int generateLinePragmaStartCallCount = 0;
                int generateNamespaceCallCount = 0;
                int generateLinePragmaEndCallCount = 0;
                generator.GenerateLinePragmaStartAction = (actualE) =>
                {
                    Assert.Same(e.Imports[generateLinePragmaEndCallCount].LinePragma, actualE);
                    Assert.Equal(generateLinePragmaStartCallCount, generateNamespaceCallCount);
                    Assert.Equal(generateLinePragmaStartCallCount, generateLinePragmaEndCallCount);
                    writer.Write("LinePragmaStart ");
                    generateLinePragmaStartCallCount++;
                };
                generator.GenerateNamespaceImportAction = (actualE) =>
                {
                    Assert.Same(e.Imports[generateLinePragmaEndCallCount], actualE);
                    Assert.Equal(generateNamespaceCallCount, generateLinePragmaEndCallCount);
                    writer.Write("Namespace ");
                    generateNamespaceCallCount++;
                };
                generator.GenerateLinePragmaEndAction = (actualE) =>
                {
                    Assert.Same(e.Imports[generateLinePragmaEndCallCount].LinePragma, actualE);
                    writer.Write("LinePragmaEnd");
                    generateLinePragmaEndCallCount++;
                };
                generator.GenerateNamespaceImports(e);
                Assert.Equal(2, generateLinePragmaStartCallCount);
                Assert.Equal(2, generateNamespaceCallCount);
                Assert.Equal(2, generateLinePragmaEndCallCount);
                Assert.Equal("LinePragmaStart Namespace LinePragmaEndLinePragmaStart Namespace LinePragmaEnd", writer.ToString());
            });
        }

        [Fact]
        public void GenerateNamespaceImports_InvokeEmptyWithoutOutput_Success()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeNamespace();
            int generateNamespaceCallCount = 0;
            generator.GenerateNamespaceImportAction = (actualE) => generateNamespaceCallCount++;
            generator.GenerateNamespaceImports(e);
            Assert.Equal(0, generateNamespaceCallCount);
        }

        [Fact]
        public void GenerateNamespaceImports_InvokeWithoutOutput_Success()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeNamespace();
            e.Imports.Add(new CodeNamespaceImport("Namespace1"));
            e.Imports.Add(new CodeNamespaceImport("Namespace2"));
            int generateNamespaceCallCount = 0;
            generator.GenerateNamespaceImportAction = (actualE) =>
            {
                Assert.Same(e.Imports[generateNamespaceCallCount], actualE);
                generateNamespaceCallCount++;
            };
            generator.GenerateNamespaceImports(e);
            Assert.Equal(2, generateNamespaceCallCount);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateNamespaceImports_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateNamespaceImports(null));
        }

        public static IEnumerable<object[]> GenerateParameterDeclarationExpression_TestData()
        {
            yield return new object[] { null, null, FieldDirection.In, "Type " };
            yield return new object[] { string.Empty, string.Empty, FieldDirection.In, "Type " };
            yield return new object[] { "type", "name", FieldDirection.In, "Type name" };
            yield return new object[] { null, null, FieldDirection.Out, "out Type " };
            yield return new object[] { string.Empty, string.Empty, FieldDirection.Out, "out Type " };
            yield return new object[] { "type", "name", FieldDirection.Out, "out Type name" };
            yield return new object[] { null, null, FieldDirection.Ref, "ref Type " };
            yield return new object[] { string.Empty, string.Empty, FieldDirection.Ref, "ref Type " };
            yield return new object[] { "type", "name", FieldDirection.Ref, "ref Type name" };
            yield return new object[] { null, null, FieldDirection.In - 1, "Type " };
            yield return new object[] { string.Empty, string.Empty, FieldDirection.In - 1, "Type " };
            yield return new object[] { "type", "name", FieldDirection.In - 1, "Type name" };
            yield return new object[] { null, null, FieldDirection.Ref + 1, "Type " };
            yield return new object[] { string.Empty, string.Empty, FieldDirection.Ref + 1, "Type " };
            yield return new object[] { "type", "name", FieldDirection.Ref + 1, "Type name" };
        }

        [Theory]
        [MemberData(nameof(GenerateParameterDeclarationExpression_TestData))]
        public void GenerateParameterDeclarationExpression_Invoke_Success(string type, string name, FieldDirection direction, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeParameterDeclarationExpression(type, name) { Direction = direction };
                generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                int outputDirectionCallCount = 0;
                int outputTypeNamePairCallCount = 0;
                int outputTypeCallCount = 0;
                generator.OutputDirectionAction = (actualDirection, baseMethod) =>
                {
                    baseMethod(actualDirection);
                    Assert.Equal(e.Direction, actualDirection);
                    Assert.Equal(0, outputTypeNamePairCallCount);
                    Assert.Equal(0, outputTypeCallCount);
                    outputDirectionCallCount++;
                };
                generator.OutputTypeNamePairAction = (actualType, actualName, baseMethod) =>
                {
                    baseMethod(actualType, actualName);
                    Assert.Same(e.Type, actualType);
                    Assert.Same(e.Name, actualName);
                    outputTypeNamePairCallCount++;
                };
                generator.OutputTypeAction = (actualType) =>
                {
                    Assert.Same(e.Type, actualType);
                    writer.Write("Type");
                    outputTypeCallCount++;
                };
                generator.OutputIdentifierAction = (actualIdent, baseMethod) => baseMethod(actualIdent);
                generator.GenerateParameterDeclarationExpression(e);
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(1, outputDirectionCallCount);
                Assert.Equal(1, outputTypeNamePairCallCount);
                Assert.Equal(1, outputTypeCallCount);
            });
        }

        [Fact]
        public void GenerateParameterDeclarationExpression_InvokeWithCustomAttributes_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeParameterDeclarationExpression("Type", "Name") { Direction = FieldDirection.Ref };
                e.CustomAttributes.Add(new CodeAttributeDeclaration("name"));
                e.CustomAttributes.Add(new CodeAttributeDeclaration("name"));
                generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                int outputAttributeDeclarationsCallCount = 0;
                int outputDirectionCallCount = 0;
                int outputTypeNamePairCallCount = 0;
                int outputTypeCallCount = 0;
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) =>
                {
                    baseMethod(actualAttributes);
                    Assert.Same(e.CustomAttributes, actualAttributes);
                    Assert.Equal(0, outputDirectionCallCount);
                    Assert.Equal(0, outputTypeNamePairCallCount);
                    Assert.Equal(0, outputTypeCallCount);
                    outputAttributeDeclarationsCallCount++;
                };
                generator.GenerateAttributeDeclarationsStartAction = (actualArg) => writer.Write("StartAttributes ");
                generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
                generator.GenerateAttributeDeclarationsEndAction = (actualArg) => writer.Write(" EndAttributes");
                generator.OutputDirectionAction = (actualDirection, baseMethod) =>
                {
                    baseMethod(actualDirection);
                    Assert.Equal(e.Direction, actualDirection);
                    Assert.Equal(0, outputTypeNamePairCallCount);
                    Assert.Equal(0, outputTypeCallCount);
                    outputDirectionCallCount++;
                };
                generator.OutputTypeNamePairAction = (actualType, actualName, baseMethod) =>
                {
                    baseMethod(actualType, actualName);
                    Assert.Same(e.Type, actualType);
                    Assert.Same(e.Name, actualName);
                    outputTypeNamePairCallCount++;
                };
                generator.OutputTypeAction = (actualType) =>
                {
                    Assert.Same(e.Type, actualType);
                    writer.Write("Type");
                    outputTypeCallCount++;
                };
                generator.OutputIdentifierAction = (actualIdent, baseMethod) => baseMethod(actualIdent);
                generator.GenerateParameterDeclarationExpression(e);
                Assert.Equal($"StartAttributes name(), {Environment.NewLine}name() EndAttributes ref Type Name", writer.ToString());
                Assert.Equal(1, outputAttributeDeclarationsCallCount);
                Assert.Equal(1, outputDirectionCallCount);
                Assert.Equal(1, outputTypeNamePairCallCount);
                Assert.Equal(1, outputTypeCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateParameterDeclarationExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateParameterDeclarationExpression(null));
        }

        [Fact]
        public void GenerateParameterDeclarationExpression_InvokeWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeParameterDeclarationExpression();
            generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.OutputTypeNamePairAction = (actualType, actualName, baseMethod) => baseMethod(actualType, actualName);
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            generator.OutputTypeAction = (actualType) => { };
            Assert.Throws<NullReferenceException>(() => generator.GenerateParameterDeclarationExpression(e));
        }

        public static IEnumerable<object[]> GeneratePrimitiveExpression_TestData()
        {
            yield return new object[] { null, "NullToken" };
            yield return new object[] { 'a', "'a'" };
            yield return new object[] { (short)1, "1" };
            yield return new object[] { 1, "1" };
            yield return new object[] { (long)1, "1" };
            yield return new object[] { (byte)1, "1" };
            yield return new object[] { true, "true" };
            yield return new object[] { false, "false" };
        }

        [Theory]
        [MemberData(nameof(GeneratePrimitiveExpression_TestData))]
        public void GeneratePrimitiveExpression_Invoke_Success(object value, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodePrimitiveExpression(value);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GeneratePrimitiveExpression(e);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Fact]
        public void GeneratePrimitiveExpression_InvokeFloat_Success()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression((float)1);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateSingleFloatValueCallCount = 0;
            generator.GenerateSingleFloatValueAction = (actualValue, baseMethod) =>
            {
                Assert.Equal((float)1, actualValue);
                generateSingleFloatValueCallCount++;
            };
            generator.GeneratePrimitiveExpression(e);
            Assert.Equal(1, generateSingleFloatValueCallCount);
        }

        [Fact]
        public void GeneratePrimitiveExpression_InvokeDouble_Success()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression((double)1);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateDoubleValueCallCount = 0;
            generator.GenerateDoubleValueAction = (actualValue, baseMethod) =>
            {
                Assert.Equal((double)1, actualValue);
                generateDoubleValueCallCount++;
            };
            generator.GeneratePrimitiveExpression(e);
            Assert.Equal(1, generateDoubleValueCallCount);
        }

        [Fact]
        public void GeneratePrimitiveExpression_InvokeDecimal_Success()
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression((decimal)1);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            int generateDecimalValueCallCount = 0;
            generator.GenerateDecimalValueAction = (actualValue, baseMethod) =>
            {
                Assert.Equal((decimal)1, actualValue);
                generateDecimalValueCallCount++;
            };
            generator.GeneratePrimitiveExpression(e);
            Assert.Equal(1, generateDecimalValueCallCount);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("result", "result")]
        public void GeneratePrimitiveExpression_InvokeString_Success(string result, string expected)
        {
            CodeGeneratorTests generator = this;
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            PerformActionWithOutput(writer =>
            {
                var e = new CodePrimitiveExpression("value");
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                int quoteSnippetCallCount = 0;
                generator.QuoteSnippetStringAction = (actualValue) =>
                {
                    Assert.Equal("value", actualValue);
                    quoteSnippetCallCount++;
                    return result;
                };
                generator.GeneratePrimitiveExpression(e);
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(1, quoteSnippetCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GeneratePrimitiveExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GeneratePrimitiveExpression(null));
        }

        public static IEnumerable<object[]> GeneratePrimitiveExpression_InvalidEValue_TestData()
        {
            yield return new object[] { new object() };
            yield return new object[] { DBNull.Value };
            yield return new object[] { new DateTime() };
            yield return new object[] { (sbyte)1 };
            yield return new object[] { (ushort)1 };
            yield return new object[] { (uint)1 };
            yield return new object[] { (ulong)1 };
        }

        [Theory]
        [MemberData(nameof(GeneratePrimitiveExpression_InvalidEValue_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GeneratePrimitiveExpression_InvalidE_ThrowsArgumentException(object value)
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression(value);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentException>("e", () => generator.GeneratePrimitiveExpression(e));
        }

        public static IEnumerable<object[]> GeneratePrimitiveExpression_WithoutOutput_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "" };
            yield return new object[] { "value" };
            yield return new object[] { 'a' };
            yield return new object[] { (short)1 };
            yield return new object[] { 1 };
            yield return new object[] { (long)1 };
            yield return new object[] { (byte)1 };
            yield return new object[] { (float)1 };
            yield return new object[] { (double)1 };
            yield return new object[] { (decimal)1 };
            yield return new object[] { true };
            yield return new object[] { false };
        }

        [Theory]
        [MemberData(nameof(GeneratePrimitiveExpression_WithoutOutput_TestData))]
        public void GeneratePrimitiveExpression_InvokeWithoutOutput_ThrowsNullRefereneException(object value)
        {
            CodeGeneratorTests generator = this;
            var e = new CodePrimitiveExpression(value);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            generator.GenerateSingleFloatValueAction = (actualS, baseMethod) => baseMethod(actualS);
            generator.GenerateDoubleValueAction = (actualD, baseMethod) => baseMethod(actualD);
            generator.GenerateDecimalValueAction = (actualD, baseMethod) => baseMethod(actualD);
            generator.QuoteSnippetStringAction = (actualValue) => actualValue;
            Assert.Throws<NullReferenceException>(() => generator.GeneratePrimitiveExpression(e));
        }

        [Fact]
        public void GenerateSingleFloatValue_Invoke_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateSingleFloatValueAction = (actualS, baseMethod) => baseMethod(actualS);
                generator.GenerateSingleFloatValue(float.MaxValue);
                Assert.Equal(float.MaxValue.ToString("R", CultureInfo.InvariantCulture.NumberFormat), writer.ToString());
            });
        }

        [Fact]
        public void GenerateSingleFloatValue_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateSingleFloatValueAction = (actualS, baseMethod) => baseMethod(actualS);
            Assert.Throws<NullReferenceException>(() => generator.GenerateSingleFloatValue(1));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("value", "value")]
        public void GenerateSnippetCompileUnit_Invoke_Success(string value, string expected)
        {
            CodeGeneratorTests generator = this;
            var e = new CodeSnippetCompileUnit(value);
            PerformActionWithOutput(writer =>
            {
                generator.GenerateSnippetCompileUnit(e);
                Assert.Equal(expected + Environment.NewLine, writer.ToString());
            });
        }

        [Fact]
        public void GenerateSnippetCompileUnit_InvokeWithDirectivesAndLinePragma_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeSnippetCompileUnit("value") { LinePragma = new CodeLinePragma() };
                e.StartDirectives.Add(new CodeDirective());
                e.StartDirectives.Add(new CodeChecksumPragma());
                e.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                e.EndDirectives.Add(new CodeDirective());
                e.EndDirectives.Add(new CodeChecksumPragma());
                e.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));

                int generateLinePragmaStartCallCount = 0;
                int generateDirectivesCallCount = 0;
                int generateLinePragmaEndCallCount = 0;
                generator.GenerateLinePragmaStartAction = (actualE) =>
                {
                    Assert.Same(e.LinePragma, actualE);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    writer.Write("LinePragmaStart ");
                    generateLinePragmaStartCallCount++;
                };
                generator.GenerateLinePragmaEndAction = (actualE) =>
                {
                    Assert.Same(e.LinePragma, actualE);
                    Assert.Equal(1, generateDirectivesCallCount);
                    writer.Write("LinePragmaEnd ");
                    generateLinePragmaEndCallCount++;
                };
                generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
                {
                    baseMethod(actualDirectives);
                    Assert.Same(generateDirectivesCallCount == 0 ? e.StartDirectives : e.EndDirectives, actualDirectives);
                    writer.Write(generateDirectivesCallCount == 0 ? "StartDirectives " : "EndDirectives");
                    generateDirectivesCallCount++;
                };
                generator.GenerateSnippetCompileUnit(e);
                Assert.Equal($"StartDirectives LinePragmaStart value{Environment.NewLine}LinePragmaEnd EndDirectives", writer.ToString());
                Assert.Equal(1, generateLinePragmaStartCallCount);
                Assert.Equal(2, generateDirectivesCallCount);
                Assert.Equal(1, generateLinePragmaEndCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateSnippetCompileUnit_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateSnippetCompileUnit(null));
        }

        [Fact]
        public void GenerateSnippetCompileUnit_InvokeWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeSnippetCompileUnit();
            Assert.Throws<NullReferenceException>(() => generator.GenerateSnippetCompileUnit(e));
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("value", "value")]
        public void GenerateSnippetStatement_Invoke_Success(string value, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.GenerateSnippetStatementAction = (actualE, baseMethod) => baseMethod(actualE);
                var e = new CodeSnippetStatement(value);
                generator.GenerateSnippetStatement(e);
                Assert.Equal(expected + Environment.NewLine, writer.ToString());
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateSnippetStatement_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateSnippetStatementAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateSnippetStatement(null));
        }

        [Fact]
        public void GenerateStatement_InvokeWithDirectivesAndLinePragma_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeGotoStatement { LinePragma = new CodeLinePragma() };
                e.StartDirectives.Add(new CodeDirective());
                e.StartDirectives.Add(new CodeChecksumPragma());
                e.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                e.EndDirectives.Add(new CodeDirective());
                e.EndDirectives.Add(new CodeChecksumPragma());
                e.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));

                int generateLinePragmaStartCallCount = 0;
                int generateDirectivesCallCount = 0;
                int generateStatementCallCount = 0;
                int generateLinePragmaEndCallCount = 0;
                generator.GenerateLinePragmaStartAction = (actualE) =>
                {
                    Assert.Same(e.LinePragma, actualE);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(0, generateStatementCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    writer.Write("LinePragmaStart ");
                    generateLinePragmaStartCallCount++;
                };
                generator.GenerateGotoStatementAction = (actualE) =>
                {
                    Assert.Same(e, actualE);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    writer.Write("Statement ");
                    generateStatementCallCount++;
                };
                generator.GenerateLinePragmaEndAction = (actualE) =>
                {
                    Assert.Same(e.LinePragma, actualE);
                    Assert.Equal(1, generateDirectivesCallCount);
                    writer.Write("LinePragmaEnd ");
                    generateLinePragmaEndCallCount++;
                };
                generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
                {
                    baseMethod(actualDirectives);
                    Assert.Same(generateDirectivesCallCount == 0 ? e.StartDirectives : e.EndDirectives, actualDirectives);
                    writer.Write(generateDirectivesCallCount == 0 ? "StartDirectives " : "EndDirectives");
                    generateDirectivesCallCount++;
                };
                generator.GenerateStatement(e);
                Assert.Equal($"StartDirectives LinePragmaStart Statement LinePragmaEnd EndDirectives", writer.ToString());
                Assert.Equal(1, generateLinePragmaStartCallCount);
                Assert.Equal(1, generateStatementCallCount);
                Assert.Equal(2, generateDirectivesCallCount);
                Assert.Equal(1, generateLinePragmaEndCallCount);
            });
        }

        [Fact]
        public void GenerateStatement_CodeAssignStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeAssignStatement();
            int callCount = 0;
            generator.GenerateAssignStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeAttachEventStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeAttachEventStatement();
            int callCount = 0;
            generator.GenerateAttachEventStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeConditionStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeConditionStatement();
            int callCount = 0;
            generator.GenerateConditionStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeExpressionStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeExpressionStatement();
            int callCount = 0;
            generator.GenerateExpressionStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeGotoStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeGotoStatement();
            int callCount = 0;
            generator.GenerateGotoStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeIterationStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeIterationStatement();
            int callCount = 0;
            generator.GenerateIterationStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeLabeledStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeLabeledStatement();
            int callCount = 0;
            generator.GenerateLabeledStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeMethodReturnStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeMethodReturnStatement();
            int callCount = 0;
            generator.GenerateMethodReturnStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeRemoveEventStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeRemoveEventStatement();
            int callCount = 0;
            generator.GenerateRemoveEventStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeSnippetStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.Indent = 1;

                var e = new CodeSnippetStatement();
                int callCount = 0;
                generator.GenerateSnippetStatementAction = (actualE, baseMethod) =>
                {
                    Assert.Same(e, actualE);
                    callCount++;
                };
                generator.GenerateStatement(e);
                Assert.Equal(1, callCount);

                Assert.Equal(1, generator.Indent);
            });
        }

        [Fact]
        public void GenerateStatement_CodeThrowExpressionStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeThrowExceptionStatement();
            int callCount = 0;
            generator.GenerateThrowExceptionStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeTryCatchFinallyStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeTryCatchFinallyStatement();
            int callCount = 0;
            generator.GenerateTryCatchFinallyStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GenerateStatement_CodeVariableDeclarationStatement_CallsCorrectMethod()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeVariableDeclarationStatement();
            int callCount = 0;
            generator.GenerateVariableDeclarationStatementAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                callCount++;
            };
            generator.GenerateStatement(e);
            Assert.Equal(1, callCount);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateStatement_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateStatement(null));
        }

        public static IEnumerable<object[]> GenerateStatement_InvalidE_TestData()
        {
            yield return new object[] { new CodeStatement() };
            yield return new object[] { new CustomCodeStatement() };
        }

        [Theory]
        [MemberData(nameof(GenerateStatement_InvalidE_TestData))]
        public void GenerateStatement_InvalidE_ThrowsArgumentException(CodeStatement e)
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentException>("e", () => generator.GenerateStatement(e));
        }

        [Fact]
        public void GenerateStatement_CodeSnippetStatementWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeSnippetStatement();
            generator.GenerateSnippetStatementAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.GenerateStatement(e));
        }

        [Fact]
        public void GenerateStatements_InvokeWithWriter_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var stmts = new CodeStatementCollection(new CodeStatement[] { new CodeGotoStatement(), new CodeGotoStatement() });
                int generateStatementCallCount = 0;
                generator.GenerateGotoStatementAction = (actualE) =>
                {
                    Assert.Same(stmts[generateStatementCallCount], actualE);
                    generateStatementCallCount++;
                };
                generator.GenerateStatements(stmts);
                Assert.Equal(2, generateStatementCallCount);
            });
        }

        [Fact]
        public void GenerateStatements_InvokeEmptyStatementsWithWriter_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var stmts = new CodeStatementCollection();
                generator.GenerateStatements(stmts);
                Assert.Empty(writer.ToString());
            });
        }

        [Fact]
        public void GenerateStatements_InvokeEmptyStatementsWithoutWriter_Nop()
        {
            CodeGeneratorTests generator = this;
            var stmts = new CodeStatementCollection();
            generator.GenerateStatements(stmts);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateStatements_NullStmts_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("stmts", () => generator.GenerateStatements(null));
        }

        [Fact]
        public void GenerateStatements_InvalidStatementInStmts_ThrowsArgumentException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var stmts = new CodeStatementCollection(new CodeStatement[] { new CodeStatement() });
                Assert.Throws<ArgumentException>("e", () => generator.GenerateStatements(stmts));
            });
        }

        [Fact]
        public void GenerateStatements_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var stmts = new CodeStatementCollection(new CodeStatement[] { new CodeStatement() });
            Assert.Throws<NullReferenceException>(() => generator.GenerateStatements(stmts));
        }

        [Fact]
        public void GenerateTypeOfExpression_Invoke_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeTypeOfExpression(new CodeTypeReference());
                generator.GenerateTypeOfExpressionAction = (actualE, baseMethod) => baseMethod(actualE);   
                int outputTypeCallCount = 0;
                generator.OutputTypeAction = (actualTypeRef) =>
                {
                    Assert.Same(e.Type, actualTypeRef);
                    writer.Write("Type");
                    outputTypeCallCount++;
                };
                generator.GenerateTypeOfExpression(e);
                Assert.Equal("typeof(Type)", writer.ToString());
                Assert.Equal(1, outputTypeCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateTypeOfExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateTypeOfExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateTypeOfExpression(null));
        }

        [Fact]
        public void GenerateTypeOfExpression_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeTypeOfExpression();
            generator.GenerateTypeOfExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.GenerateTypeOfExpression(e));
        }

        [Fact]
        public void GenerateTypeReferenceExpression_Invoke_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeTypeReferenceExpression(new CodeTypeReference());
                generator.GenerateTypeReferenceExpressionAction = (actualE, baseMethod) => baseMethod(actualE);   
                int outputTypeCallCount = 0;
                generator.OutputTypeAction = (actualTypeRef) =>
                {
                    Assert.Same(e.Type, actualTypeRef);
                    writer.Write("Type");
                    outputTypeCallCount++;
                };
                generator.GenerateTypeReferenceExpression(e);
                Assert.Equal("Type", writer.ToString());
                Assert.Equal(1, outputTypeCallCount);
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateTypeReferenceExpression_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.GenerateTypeReferenceExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateTypeReferenceExpression(null));
        }

        public static IEnumerable<object[]> GenerateTypes_TestData()
        {
            yield return new object[] { new CodeTypeDeclaration(), null, $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
            yield return new object[] { new CodeTypeDeclaration(), new CodeGeneratorOptions(), $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
            yield return new object[] { new CodeTypeDeclaration(), new CodeGeneratorOptions { BlankLinesBetweenMembers = false }, $"TypeStart TypeEndTypeStart TypeEnd" };

            yield return new object[] { new CodeTypeDeclaration("name") { IsClass = true }, null, $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsEnum = true }, null, $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsInterface = true }, null, $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsStruct = true }, null, $"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd" };
        }

        [Theory]
        [MemberData(nameof(GenerateTypes_TestData))]
        public void GenerateTypes_InvokeClassWithWriter_Success(CodeTypeDeclaration type, CodeGeneratorOptions options, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Types.Add(new CodeTypeDeclaration());
                e.Types.Add(type);
                int generateTypeStartCallCount = 0;
                int generateTypeEndCallCount = 0;
                generator.GenerateTypeStartAction = (actualE) =>
                {
                    Assert.Same(e.Types[generateTypeStartCallCount], actualE);
                    Assert.Equal(generateTypeStartCallCount, generateTypeEndCallCount);
                    writer.Write("TypeStart ");
                    generateTypeStartCallCount++;
                };
                generator.GenerateTypeEndAction = (actualE) =>
                {
                    Assert.Same(e.Types[generateTypeEndCallCount], actualE);
                    writer.Write("TypeEnd");
                    generateTypeEndCallCount++;
                };
                generator.GenerateTypes(e);
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(2, generateTypeStartCallCount);
                Assert.Equal(2, generateTypeEndCallCount);

                Assert.Same(e.Types[1], generator.CurrentClass);
                Assert.Null(generator.CurrentMember);
                Assert.Equal("<% unknown %>", generator.CurrentMemberName);
                Assert.Same(e.Types[1].Name, generator.CurrentTypeName);
                Assert.Equal(e.Types[1].IsClass, generator.IsCurrentClass);
                Assert.False(generator.IsCurrentDelegate);
                Assert.Equal(e.Types[1].IsEnum, generator.IsCurrentEnum);
                Assert.Equal(e.Types[1].IsInterface, generator.IsCurrentInterface);
                Assert.Equal(e.Types[1].IsStruct, generator.IsCurrentStruct);
            }, options);
        }

        [Fact]
        public void GenerateTypes_InvokeDelegateWithWriter_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                e.Types.Add(new CodeTypeDeclaration());
                e.Types.Add(new CodeTypeDelegate("name"));
                int generateTypeStartCallCount = 0;
                int generateTypeEndCallCount = 0;
                generator.GenerateTypeStartAction = (actualE) =>
                {
                    Assert.Same(e.Types[generateTypeStartCallCount], actualE);
                    Assert.Equal(generateTypeStartCallCount, generateTypeEndCallCount);
                    writer.Write("TypeStart ");
                    generateTypeStartCallCount++;
                };
                generator.GenerateTypeEndAction = (actualE) =>
                {
                    Assert.Same(e.Types[generateTypeEndCallCount], actualE);
                    writer.Write("TypeEnd");
                    generateTypeEndCallCount++;
                };
                generator.GenerateTypes(e);
                Assert.Equal($"{Environment.NewLine}TypeStart TypeEnd{Environment.NewLine}TypeStart TypeEnd", writer.ToString());
                Assert.Equal(2, generateTypeStartCallCount);
                Assert.Equal(2, generateTypeEndCallCount);

                Assert.Same(e.Types[1], generator.CurrentClass);
                Assert.Null(generator.CurrentMember);
                Assert.Equal("<% unknown %>", generator.CurrentMemberName);
                Assert.Same(e.Types[1].Name, generator.CurrentTypeName);
                Assert.False(generator.IsCurrentClass);
                Assert.True(generator.IsCurrentDelegate);
                Assert.False(generator.IsCurrentEnum);
                Assert.False(generator.IsCurrentInterface);
                Assert.False(generator.IsCurrentStruct);
            });
        }

        [Fact]
        public void GenerateTypes_InvokeWithCommentsDirectivesAndLinePragma_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var type = new CodeTypeDeclaration { LinePragma = new CodeLinePragma() };
                type.Comments.Add(new CodeCommentStatement("Comment"));
                type.Comments.Add(new CodeCommentStatement("Comment"));
                type.StartDirectives.Add(new CodeDirective());
                type.StartDirectives.Add(new CodeChecksumPragma());
                type.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                type.EndDirectives.Add(new CodeDirective());
                type.EndDirectives.Add(new CodeChecksumPragma());
                type.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
                var e = new CodeNamespace();
                e.Types.Add(type);

                int generateCommentStatementsCallCount = 0;
                int generateCommentCallCount = 0;
                int generateLinePragmaStartCallCount = 0;
                int generateTypeStartCallCount = 0;
                int generateTypeEndCallCount = 0;
                int generateDirectivesCallCount = 0;
                int generateLinePragmaEndCallCount = 0;
                generator.GenerateCommentStatementsAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(type.Comments, actualE);
                    writer.Write("Comments ");
                    Assert.Equal(0, generateLinePragmaStartCallCount);
                    Assert.Equal(0, generateTypeStartCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(0, generateTypeEndCallCount);
                    generateCommentStatementsCallCount++;
                };
                generator.GenerateCommentAction = (actualE) =>
                {
                    Assert.Same(type.Comments[generateCommentCallCount].Comment, actualE);
                    writer.Write("Comment ");
                    generateCommentCallCount++;
                };
                generator.GenerateLinePragmaStartAction = (actualE) =>
                {
                    Assert.Same(type.LinePragma, actualE);
                    Assert.Equal(0, generateTypeStartCallCount);
                    Assert.Equal(1, generateDirectivesCallCount);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(0, generateTypeEndCallCount);
                    writer.Write("LinePragmaStart ");
                    generateLinePragmaStartCallCount++;
                };
                generator.GenerateTypeStartAction = (actualE) =>
                {
                    Assert.Same(type, actualE);
                    Assert.Equal(1, generateDirectivesCallCount);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    Assert.Equal(0, generateTypeEndCallCount);
                    writer.Write("TypeStart ");
                    generateTypeStartCallCount++;
                };
                generator.GenerateTypeEndAction = (actualE) =>
                {
                    Assert.Same(type, actualE);
                    Assert.Equal(1, generateDirectivesCallCount);
                    Assert.Equal(0, generateLinePragmaEndCallCount);
                    writer.Write("TypeEnd ");
                    generateTypeEndCallCount++;
                };
                generator.GenerateLinePragmaEndAction = (actualE) =>
                {
                    Assert.Same(type.LinePragma, actualE);
                    Assert.Equal(1, generateDirectivesCallCount);
                    Assert.Equal(1, generateTypeEndCallCount);
                    writer.Write("LinePragmaEnd ");
                    generateLinePragmaEndCallCount++;
                };
                generator.GenerateDirectivesAction = (actualDirectives, baseMethod) =>
                {
                    baseMethod(actualDirectives);
                    Assert.Same(generateDirectivesCallCount == 0 ? type.StartDirectives : type.EndDirectives, actualDirectives);
                    writer.Write(generateDirectivesCallCount == 0 ? "StartDirectives " : "EndDirectives");
                    generateDirectivesCallCount++;
                };
                generator.GenerateTypes(e);
                Assert.Equal($"{Environment.NewLine}StartDirectives Comment Comment Comments LinePragmaStart TypeStart TypeEnd LinePragmaEnd EndDirectives", writer.ToString());
                Assert.Equal(1, generateCommentStatementsCallCount);
                Assert.Equal(2, generateCommentCallCount);
                Assert.Equal(1, generateLinePragmaStartCallCount);
                Assert.Equal(1, generateTypeStartCallCount);
                Assert.Equal(1, generateTypeEndCallCount);
                Assert.Equal(1, generateLinePragmaEndCallCount);
                Assert.Equal(2, generateDirectivesCallCount);

                Assert.Same(type, generator.CurrentClass);
                Assert.Null(generator.CurrentMember);
                Assert.Equal("<% unknown %>", generator.CurrentMemberName);
                Assert.Same(type.Name, generator.CurrentTypeName);
                Assert.Equal(type.IsClass, generator.IsCurrentClass);
                Assert.False(generator.IsCurrentDelegate);
                Assert.Equal(type.IsEnum, generator.IsCurrentEnum);
                Assert.Equal(type.IsInterface, generator.IsCurrentInterface);
                Assert.Equal(type.IsStruct, generator.IsCurrentStruct);
            });
        }

        [Fact]
        public void GenerateTypes_InvokeWithMembers_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var type = new CodeTypeDeclaration();
                type.Members.Add(new CodeTypeMember());
                var e = new CodeNamespace();
                e.Types.Add(type);

                int generateTypeStartCallCount = 0;
                int generateTypeEndCallCount = 0;
                generator.GenerateTypeStartAction = (actualE) =>
                {
                    Assert.Same(type, actualE);
                    Assert.Equal(0, generateTypeEndCallCount);
                    writer.Write("TypeStart ");
                    generateTypeStartCallCount++;
                };
                generator.GenerateTypeEndAction = (actualE) =>
                {
                    Assert.Same(type, actualE);
                    writer.Write("TypeEnd");
                    generateTypeEndCallCount++;
                };
                generator.GenerateTypes(e);
                Assert.Equal($"{Environment.NewLine}TypeStart TypeEnd", writer.ToString());
                Assert.Equal(1, generateTypeStartCallCount);
                Assert.Equal(1, generateTypeEndCallCount);
                Assert.Same(type, generator.CurrentClass);
            });
        }

        [Fact]
        public void GenerateTypes_InvokeEmpty_Nop()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var e = new CodeNamespace();
                generator.GenerateTypes(e);
                Assert.Empty(writer.ToString());
            });
        }

        [Fact]
        public void GenerateTypes_InvokeEmptyEWithoutWriter_Nop()
        {
            CodeGeneratorTests generator = this;
            var e = new CodeNamespace();
            generator.GenerateTypes(e);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void GenerateTypes_NullE_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateTypes(null));
        }

        [Fact]
        public void GenerateTypes_NullValueInE_ThrowsArgumentException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var type = new CodeTypeDeclaration();
                type.Comments.Add(new CodeCommentStatement());
                var e = new CodeNamespace();
                e.Types.Add(type);

                generator.GenerateNamespaceAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateCommentStatementsAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateNamespaceStartAction = (actualE) => {};
                generator.GenerateNamespaceEndAction = (actualE) => {};

                Assert.Throws<ArgumentException>("e", () => generator.GenerateTypes(e));
            });
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("  ", false)]
        [InlineData("a", true)]
        [InlineData("A", true)]
        [InlineData("\u01C5", true)]
        [InlineData("\u02B0", true)]
        [InlineData("\u2163", true)]
        [InlineData("\u0620", true)]
        [InlineData("_", true)]
        [InlineData("_aA\u01C5\u02B0\u2163\u0620_0", true)]
        [InlineData("aA\u01C5\u02B0\u2163\u0620_0", true)]
        [InlineData(" ", false)]
        [InlineData("a ", false)]
        [InlineData("#", false)]
        [InlineData("a#", false)]
        [InlineData("\u0300", false)]
        [InlineData("a\u0300", true)]
        [InlineData("\u0903", false)]
        [InlineData("a\u0903", true)]
        [InlineData("\u203F", false)]
        [InlineData("a\u203F", true)]
        [InlineData("0", false)]
        [InlineData("1", false)]
        [InlineData(":", false)]
        [InlineData(".", false)]
        [InlineData("$", false)]
        [InlineData("+", false)]
        [InlineData("<", false)]
        [InlineData(">", false)]
        [InlineData("-", false)]
        [InlineData("[", false)]
        [InlineData("]", false)]
        [InlineData(",", false)]
        [InlineData("&", false)]
        [InlineData("*", false)]
        [InlineData("`", false)]
        [InlineData("a0", true)]
        [InlineData("a1", true)]
        [InlineData("a:", false)]
        [InlineData("a.", false)]
        [InlineData("a$", false)]
        [InlineData("a+", false)]
        [InlineData("a<", false)]
        [InlineData("a>", false)]
        [InlineData("a-", false)]
        [InlineData("a[", false)]
        [InlineData("a]", false)]
        [InlineData("a,", false)]
        [InlineData("a&", false)]
        [InlineData("a*", false)]
        [InlineData("\0", false)]
        [InlineData("a\0", false)]
        [InlineData("\r", false)]
        [InlineData("a\r", false)]
        [InlineData("\n", false)]
        [InlineData("a\n", false)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void IsValidLanguageIndependentIdentifier_Invoke_ReturnsExpected(string value, bool expected)
        {
            Assert.Equal(expected, CodeGenerator.IsValidLanguageIndependentIdentifier(value));
        }

        [Theory]
        [InlineData(null, "1")]
        [InlineData("", "1")]
        [InlineData("name", "name=1")]
        public void OutputAttributeArgument_Invoke_Success(string name, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var arg = new CodeAttributeArgument(name, new CodePrimitiveExpression(1));
                generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
                generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.OutputAttributeArgument(arg);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void OutputAttributeArgument_NullArg_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
            generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<ArgumentNullException>("arg", () => generator.OutputAttributeArgument(null));
        }

        [Fact]
        public void OutputAttributeArgument_NullArgValue_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var arg = new CodeAttributeArgument();
                generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
                generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                Assert.Throws<ArgumentNullException>("e", () => generator.OutputAttributeArgument(arg));
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("name")]
        public void OutputAttributeArgument_InvokeNonNullNameWithoutOutput_ThrowsNullReferenceException(string name)
        {
            CodeGeneratorTests generator = this;
            var arg = new CodeAttributeArgument(name, new CodePrimitiveExpression(1));
            generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
            generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
            generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
            Assert.Throws<NullReferenceException>(() => generator.OutputAttributeArgument(arg));
        }

        [Fact]
        public void OutputAttributeDeclarations_NonEmptyAttributes_Success()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var attributes = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[]
                {
                    new CodeAttributeDeclaration(),
                    new CodeAttributeDeclaration(string.Empty),
                    new CodeAttributeDeclaration("name"),
                    new CodeAttributeDeclaration("name", new CodeAttributeArgument(new CodePrimitiveExpression(1))),
                    new CodeAttributeDeclaration("name", new CodeAttributeArgument("AttributeName", new CodePrimitiveExpression(1))),
                    new CodeAttributeDeclaration("name", new CodeAttributeArgument("AttributeName1", new CodePrimitiveExpression(1)), new CodeAttributeArgument("AttributeName2", new CodePrimitiveExpression(2)))
                });
                int generateAttributeDeclarationsStartCallCount = 0;
                int generateAttributeDeclarationsEndCallCount = 0;
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
                generator.GenerateAttributeDeclarationsStartAction = (actualAttributes) =>
                {
                    Assert.Same(attributes, actualAttributes);
                    Assert.Equal(0, generateAttributeDeclarationsEndCallCount);
                    generator.Output.Write("StartAttributes ");
                    generateAttributeDeclarationsStartCallCount++;
                };
                generator.OutputIdentifierAction = (actualIdentifier, baseMethod) => baseMethod(actualIdentifier);
                generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
                generator.GeneratePrimitiveExpressionAction = (actualE, baseMethod) => baseMethod(actualE);
                generator.GenerateAttributeDeclarationsEndAction = (actualAttributes) =>
                {
                    Assert.Same(attributes, actualAttributes);
                    generator.Output.Write(" EndAttributes");
                    generateAttributeDeclarationsEndCallCount++;
                };
                generator.OutputAttributeDeclarations(attributes);
                Assert.Equal(1, generateAttributeDeclarationsStartCallCount);
                Assert.Equal(1, generateAttributeDeclarationsStartCallCount);
                Assert.Equal($"StartAttributes (), {Environment.NewLine}(), {Environment.NewLine}name(), {Environment.NewLine}name(1), {Environment.NewLine}name(AttributeName=1), {Environment.NewLine}name(AttributeName1=1, AttributeName2=2) EndAttributes", writer.ToString());
            });
        }

        [Fact]
        public void OutputAttributeDeclarations_InvokeEmptyAttributes_Nop()
        {
            CodeGeneratorTests generator = this;
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            generator.OutputAttributeDeclarations(new CodeAttributeDeclarationCollection());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void OutputAttributeDeclarations_NullAttributes_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            Assert.Throws<ArgumentNullException>("attributes", () => generator.OutputAttributeDeclarations(null));
        }

        [Fact]
        public void OutputAttributeDeclarations_InvokeNonEmptyAttributesNoOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var attributes = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[]
            {
                new CodeAttributeDeclaration(),
                new CodeAttributeDeclaration(string.Empty),
                new CodeAttributeDeclaration("name")
            });
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            generator.GenerateAttributeDeclarationsStartAction = (actualAttributes) => {};
            generator.GenerateAttributeDeclarationsEndAction = (actualAttributes) => {};
            Assert.Throws<NullReferenceException>(() => generator.OutputAttributeDeclarations(attributes));
        }

        [Fact]
        public void OutputAttributeDeclarations_NullArgumentExpressionInAttributes_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var attributes = new CodeAttributeDeclarationCollection(new CodeAttributeDeclaration[]
                {
                    new CodeAttributeDeclaration("name", new CodeAttributeArgument())
                });
                generator.OutputAttributeDeclarationsAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
                generator.GenerateAttributeDeclarationsStartAction = (actualAttributes) => { };
                generator.OutputAttributeArgumentAction = (actualArg, baseMethod) => baseMethod(actualArg);
                generator.GenerateAttributeDeclarationsEndAction = (actualAttributes) => { };
                Assert.Throws<ArgumentNullException>("e", () => generator.OutputAttributeDeclarations(attributes));
            });
        }

        [Theory]
        [InlineData(FieldDirection.In, "")]
        [InlineData(FieldDirection.Out, "out ")]
        [InlineData(FieldDirection.Ref, "ref ")]
        [InlineData(FieldDirection.In - 1, "")]
        [InlineData(FieldDirection.Ref + 1, "")]
        public void OutputDirection_Invoke_Success(FieldDirection direction, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
                generator.OutputDirection(direction);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(FieldDirection.Out)]
        [InlineData(FieldDirection.Ref)]
        public void OutputDirection_InvokeWithoutOutput_ThrowsNullReferenceException(FieldDirection direction)
        {
            CodeGeneratorTests generator = this;
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            Assert.Throws<NullReferenceException>(() => generator.OutputDirection(direction));
        }

        [Theory]
        [InlineData(FieldDirection.In)]
        [InlineData(FieldDirection.In - 1)]
        [InlineData(FieldDirection.Ref + 1)]
        public void OutputDirection_InvokeWithoutOutputInvaliddirection_Ndirection(FieldDirection direction)
        {
            CodeGeneratorTests generator = this;
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            generator.OutputDirection(direction);
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract, "")]
        [InlineData(MemberAttributes.Final, "")]
        [InlineData(MemberAttributes.Static, "static ")]
        [InlineData(MemberAttributes.Override, "")]
        [InlineData(MemberAttributes.Const, "const ")]
        [InlineData(MemberAttributes.ScopeMask, "")]
        [InlineData(MemberAttributes.New, "new ")]
        [InlineData(MemberAttributes.VTableMask, "")]
        [InlineData(MemberAttributes.Overloaded, "")]
        [InlineData(MemberAttributes.Assembly, "")]
        [InlineData(MemberAttributes.FamilyAndAssembly, "")]
        [InlineData(MemberAttributes.Family, "")]
        [InlineData(MemberAttributes.FamilyOrAssembly, "")]
        [InlineData(MemberAttributes.Private, "")]
        [InlineData(MemberAttributes.Public, "")]
        [InlineData(MemberAttributes.AccessMask, "")]
        [InlineData(MemberAttributes.New | MemberAttributes.Private, "new ")]
        [InlineData(MemberAttributes.Static | MemberAttributes.Private, "static ")]
        [InlineData(MemberAttributes.Const | MemberAttributes.Private, "const ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const, "new const ")]
        public void OutputFieldScopeModifier_Invoke_Success(MemberAttributes attributes, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputFieldScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
                generator.OutputFieldScopeModifier(attributes);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(MemberAttributes.Static)]
        [InlineData(MemberAttributes.Const)]
        [InlineData(MemberAttributes.New)]
        [InlineData(MemberAttributes.New | MemberAttributes.Private)]
        [InlineData(MemberAttributes.Static | MemberAttributes.Private)]
        [InlineData(MemberAttributes.Const | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const)]
        public void OutputFieldScopeModifier_InvokeWithoutOutput_ThrowsNullReferenceException(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputFieldScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            Assert.Throws<NullReferenceException>(() => generator.OutputFieldScopeModifier(attributes));
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract)]
        [InlineData(MemberAttributes.Final)]
        [InlineData(MemberAttributes.Override)]
        [InlineData(MemberAttributes.ScopeMask)]
        [InlineData(MemberAttributes.VTableMask)]
        [InlineData(MemberAttributes.Overloaded)]
        [InlineData(MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.Family)]
        [InlineData(MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.Private)]
        [InlineData(MemberAttributes.Public)]
        [InlineData(MemberAttributes.AccessMask)]
        public void OutputFieldScopeModifier_InvokeWithoutOutputInvalid_Nop(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputFieldScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            generator.OutputFieldScopeModifier(attributes);
        }

        [Theory]
        [InlineData(null, "")]
        [InlineData("", "")]
        [InlineData("ident", "ident")]
        public void OutputIdentifier_InvokeWithOutput_Appends(string st, string expected)
        {
            CodeGeneratorTests generator = this;
            generator.PerformActionWithOutput(writer =>
            {
                generator.OutputIdentifierAction = (actualSt, baseMethod) => baseMethod(actualSt);
                generator.OutputIdentifier(st);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("ident")]
        public void OutputIdentifier_InvokeWithoutOutput_ThrowsNullReferenceException(string ident)
        {
            CodeGeneratorTests generator = this;
            generator.OutputIdentifierAction = (actualSt, baseMethod) => baseMethod(actualSt);
            Assert.Throws<NullReferenceException>(() => generator.OutputIdentifier(ident));
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract, "")]
        [InlineData(MemberAttributes.Final, "")]
        [InlineData(MemberAttributes.Static, "")]
        [InlineData(MemberAttributes.Override, "")]
        [InlineData(MemberAttributes.Const, "")]
        [InlineData(MemberAttributes.ScopeMask, "")]
        [InlineData(MemberAttributes.New, "")]
        [InlineData(MemberAttributes.VTableMask, "")]
        [InlineData(MemberAttributes.Overloaded, "")]
        [InlineData(MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.AccessMask, "")]
        [InlineData(MemberAttributes.New | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly, "internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family, "protected ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly, "protected internal ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private, "private ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public, "public ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public, "public ")]
        public void OutputMemberAccessModifier_Invoke_Success(MemberAttributes attributes, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputMemberAccessModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
                generator.OutputMemberAccessModifier(attributes);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.Family)]
        [InlineData(MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.Private)]
        [InlineData(MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public)]
        public void OutputMemberAccessModifier_InvokeWithoutOutput_ThrowsNullReferenceException(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputMemberAccessModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            Assert.Throws<NullReferenceException>(() => generator.OutputMemberAccessModifier(attributes));
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract)]
        [InlineData(MemberAttributes.Final)]
        [InlineData(MemberAttributes.Static)]
        [InlineData(MemberAttributes.Override)]
        [InlineData(MemberAttributes.Const)]
        [InlineData(MemberAttributes.ScopeMask)]
        [InlineData(MemberAttributes.New)]
        [InlineData(MemberAttributes.VTableMask)]
        [InlineData(MemberAttributes.Overloaded)]
        [InlineData(MemberAttributes.AccessMask)]
        public void OutputMemberAccessModifier_InvokeWithoutOutputInvalid_Nop(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputMemberAccessModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            generator.OutputMemberAccessModifier(attributes);
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract, "abstract ")]
        [InlineData(MemberAttributes.Final, "")]
        [InlineData(MemberAttributes.Static, "static ")]
        [InlineData(MemberAttributes.Override, "override ")]
        [InlineData(MemberAttributes.Const, "")]
        [InlineData(MemberAttributes.ScopeMask, "")]
        [InlineData(MemberAttributes.New, "new ")]
        [InlineData(MemberAttributes.VTableMask, "")]
        [InlineData(MemberAttributes.Overloaded, "")]
        [InlineData(MemberAttributes.Assembly, "")]
        [InlineData(MemberAttributes.FamilyAndAssembly, "")]
        [InlineData(MemberAttributes.Family, "virtual ")]
        [InlineData(MemberAttributes.FamilyOrAssembly, "")]
        [InlineData(MemberAttributes.Private, "")]
        [InlineData(MemberAttributes.Public, "virtual ")]
        [InlineData(MemberAttributes.AccessMask, "")]
        [InlineData(MemberAttributes.New | MemberAttributes.Assembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyAndAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Family, "new virtual ")]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyOrAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Private, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Public, "new virtual ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family, "new virtual ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public, "new abstract ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public, "new ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public, "new static ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public, "new override ")]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public, "new virtual ")]
        public void OutputMemberScopeModifier_Invoke_Success(MemberAttributes attributes, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputMemberScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
                generator.OutputMemberScopeModifier(attributes);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(MemberAttributes.Abstract)]
        [InlineData(MemberAttributes.Final)]
        [InlineData(MemberAttributes.Static)]
        [InlineData(MemberAttributes.Override)]
        [InlineData(MemberAttributes.New)]
        [InlineData(MemberAttributes.Family)]
        [InlineData(MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private)]
        [InlineData(MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public)]
        [InlineData(MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public)]
        public void OutputMemberScopeModifier_InvokeWithoutOutput_ThrowsNullReferenceException(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputMemberScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            Assert.Throws<NullReferenceException>(() => generator.OutputMemberScopeModifier(attributes));
        }

        [Theory]
        [InlineData(MemberAttributes.Const)]
        [InlineData(MemberAttributes.ScopeMask)]
        [InlineData(MemberAttributes.VTableMask)]
        [InlineData(MemberAttributes.Overloaded)]
        [InlineData(MemberAttributes.Assembly)]
        [InlineData(MemberAttributes.FamilyAndAssembly)]
        [InlineData(MemberAttributes.FamilyOrAssembly)]
        [InlineData(MemberAttributes.Private)]
        [InlineData(MemberAttributes.AccessMask)]
        public void OutputMemberScopeModifier_InvokeWithoutOutputInvalid_Nop(MemberAttributes attributes)
        {
            CodeGeneratorTests generator = this;
            generator.OutputMemberScopeModifierAction = (actualAttributes, baseMethod) => baseMethod(actualAttributes);
            generator.OutputMemberScopeModifier(attributes);
        }

        [Theory]
        [InlineData(CodeBinaryOperatorType.Add, "+")]
        [InlineData(CodeBinaryOperatorType.Assign, "=")]
        [InlineData(CodeBinaryOperatorType.BitwiseAnd, "&")]
        [InlineData(CodeBinaryOperatorType.BitwiseOr, "|")]
        [InlineData(CodeBinaryOperatorType.BooleanAnd, "&&")]
        [InlineData(CodeBinaryOperatorType.BooleanOr, "||")]
        [InlineData(CodeBinaryOperatorType.Divide, "/")]
        [InlineData(CodeBinaryOperatorType.GreaterThan, ">")]
        [InlineData(CodeBinaryOperatorType.GreaterThanOrEqual, ">=")]
        [InlineData(CodeBinaryOperatorType.IdentityEquality, "==")]
        [InlineData(CodeBinaryOperatorType.IdentityInequality, "!=")]
        [InlineData(CodeBinaryOperatorType.LessThan, "<")]
        [InlineData(CodeBinaryOperatorType.LessThanOrEqual, "<=")]
        [InlineData(CodeBinaryOperatorType.Modulus, "%")]
        [InlineData(CodeBinaryOperatorType.Multiply, "*")]
        [InlineData(CodeBinaryOperatorType.Subtract, "-")]
        [InlineData(CodeBinaryOperatorType.ValueEquality, "==")]
        [InlineData(CodeBinaryOperatorType.Add - 1, "")]
        [InlineData(CodeBinaryOperatorType.GreaterThanOrEqual + 1, "")]
        public void OutputOperator_Invoke_Success(CodeBinaryOperatorType op, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
                generator.OutputOperator(op);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(CodeBinaryOperatorType.Add)]
        [InlineData(CodeBinaryOperatorType.Assign)]
        [InlineData(CodeBinaryOperatorType.BitwiseAnd)]
        [InlineData(CodeBinaryOperatorType.BitwiseOr)]
        [InlineData(CodeBinaryOperatorType.BooleanAnd)]
        [InlineData(CodeBinaryOperatorType.BooleanOr)]
        [InlineData(CodeBinaryOperatorType.Divide)]
        [InlineData(CodeBinaryOperatorType.GreaterThan)]
        [InlineData(CodeBinaryOperatorType.GreaterThanOrEqual)]
        [InlineData(CodeBinaryOperatorType.IdentityEquality)]
        [InlineData(CodeBinaryOperatorType.IdentityInequality)]
        [InlineData(CodeBinaryOperatorType.LessThan)]
        [InlineData(CodeBinaryOperatorType.LessThanOrEqual)]
        [InlineData(CodeBinaryOperatorType.Modulus)]
        [InlineData(CodeBinaryOperatorType.Multiply)]
        [InlineData(CodeBinaryOperatorType.Subtract)]
        [InlineData(CodeBinaryOperatorType.ValueEquality)]
        public void OutputOperator_InvokeWithoutOutput_ThrowsNullReferenceException(CodeBinaryOperatorType op)
        {
            CodeGeneratorTests generator = this;
            generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
            Assert.Throws<NullReferenceException>(() => generator.OutputOperator(op));
        }

        [Theory]
        [InlineData(CodeBinaryOperatorType.Add - 1)]
        [InlineData(CodeBinaryOperatorType.GreaterThanOrEqual + 1)]
        public void OutputOperator_InvokeWithoutOutputInvalidOp_Nop(CodeBinaryOperatorType op)
        {
            CodeGeneratorTests generator = this;
            generator.OutputOperatorAction = (actualOp, baseMethod) => baseMethod(actualOp);
            generator.OutputOperator(op);
        }
        
        public static IEnumerable<object[]> OutputParameter_TestData()
        {
            yield return new object[] { new CodeParameterDeclarationExpression[0], "" };
            yield return new object[] { new CodeParameterDeclarationExpression[] { new CodeParameterDeclarationExpression() }, "Type " };
            yield return new object[] { new CodeParameterDeclarationExpression[] { new CodeParameterDeclarationExpression("type1", "name1"), new CodeParameterDeclarationExpression("type2", "name2") }, "Type name1, Type name2" };
            yield return new object[]
            {
                new CodeParameterDeclarationExpression[]
                {
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression()
                },
                "Type , Type , Type , Type , Type , Type , Type , Type , Type , Type , Type , Type , Type , Type , Type "
            };
            yield return new object[]
            {
                new CodeParameterDeclarationExpression[]
                {
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(),
                    new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression(), new CodeParameterDeclarationExpression()
                },
                string.Concat(PlatformDetection.IsNetFramework ? string.Empty : $"            ", $"{Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             , {Environment.NewLine}Type             ")
            };
        }

        [Theory]
        [MemberData(nameof(OutputParameter_TestData))]
        public void OutputParameter_Invoke_Success(CodeParameterDeclarationExpression[] parametersArray, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                var parameters = new CodeParameterDeclarationExpressionCollection(parametersArray);
                generator.OutputParametersAction = (actualParameters, baseMethod) => baseMethod(actualParameters);
                int generateParameterDeclarationExpressionCallCount = 0;
                int outputTypeActionCallCount = 0;
                generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) =>
                {
                    baseMethod(actualE);
                    Assert.Same(parameters[generateParameterDeclarationExpressionCallCount], actualE);
                    generateParameterDeclarationExpressionCallCount++;
                };
                generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
                generator.OutputTypeNamePairAction = (actualTypeRef, actualName, baseMethod) => baseMethod(actualTypeRef, actualName);
                generator.OutputTypeAction = (actualTypeRef) =>
                {
                    Assert.Same(parameters[generateParameterDeclarationExpressionCallCount].Type, actualTypeRef);
                    writer.Write("Type");
                    outputTypeActionCallCount++;
                };
                generator.OutputIdentifierAction = (actualTypeRef, baseMethod) => baseMethod(actualTypeRef);
                generator.OutputParameters(parameters);
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(parameters.Count, generateParameterDeclarationExpressionCallCount);
                Assert.Equal(parameters.Count, outputTypeActionCallCount);

                // Call again to make sure indent is reset.
                Assert.Equal(expected, writer.ToString());
                Assert.Equal(parameters.Count, generateParameterDeclarationExpressionCallCount);
                Assert.Equal(parameters.Count, outputTypeActionCallCount);
            });
        }

        [Fact]
        public void OutputParameters_EmptyParametersWithoutWriter_Nop()
        {
            CodeGeneratorTests generator = this;
            var parameters = new CodeParameterDeclarationExpressionCollection();
            generator.OutputParametersAction = (actualParameters, baseMethod) => baseMethod(actualParameters);
            generator.OutputParameters(parameters);
        }

        [Fact]
        public void OutputParameters_InvokeWithoutWriter_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var parameters = new CodeParameterDeclarationExpressionCollection(new CodeParameterDeclarationExpression[] { new CodeParameterDeclarationExpression() });
            generator.OutputParametersAction = (actualParameters, baseMethod) => baseMethod(actualParameters);
            int generateParameterDeclarationExpressionCallCount = 0;
            generator.GenerateParameterDeclarationExpressionAction = (actualE, baseMethod) =>
            {
                baseMethod(actualE);
                Assert.Same(parameters[generateParameterDeclarationExpressionCallCount], actualE);
                generateParameterDeclarationExpressionCallCount++;
            };
            generator.OutputDirectionAction = (actualDirection, baseMethod) => baseMethod(actualDirection);
            generator.OutputTypeNamePairAction = (actualTypeRef, actualName, baseMethod) => baseMethod(actualTypeRef, actualName);
            generator.OutputTypeAction = (actualTypeRef) => { };
            Assert.Throws<NullReferenceException>(() => generator.OutputParameters(parameters));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void OutputParameters_NullParameters_ThrowsArgumentNullException()
        {
            CodeGeneratorTests generator = this;
            generator.OutputParametersAction = (actualParameters, baseMethod) => baseMethod(actualParameters);
            Assert.Throws<ArgumentNullException>("parameters", () => generator.OutputParameters(null));
        }

        public static IEnumerable<object[]> OutputTypeNamePair_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { new CodeTypeReference(), "" };
            yield return new object[] { new CodeTypeReference(), "name" };
        }

        [Theory]
        [MemberData(nameof(OutputTypeNamePair_TestData))]
        public void OutputTypeNamePair_Invoke_Success(CodeTypeReference typeRef, string name)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputTypeNamePairAction = (actualTypeRef, actualName, baseMethod) => baseMethod(actualTypeRef, actualName);
                int outputTypeCallCount = 0;
                int outputIdentifierCallCount = 0;
                generator.OutputTypeAction = (actualTypeRef) =>
                {
                    Assert.Same(typeRef, actualTypeRef);
                    Assert.Equal(0, outputIdentifierCallCount);
                    writer.Write("Type");
                    outputTypeCallCount++;
                };
                generator.OutputIdentifierAction = (actualIdent, baseMethod) =>
                {
                    baseMethod(actualIdent);
                    outputIdentifierCallCount++;
                };
                generator.OutputTypeNamePair(typeRef, name);
                Assert.Equal($"Type {name}", writer.ToString());
                Assert.Equal(1, outputTypeCallCount);
                Assert.Equal(1, outputIdentifierCallCount);
            });
        }

        [Fact]
        public void OutputTypeNamePair_InvokeWithoutOutput_ThrowsNullReferenceException()
        {
            CodeGeneratorTests generator = this;
            var typeRef = new CodeTypeReference();
            generator.OutputTypeNamePairAction = (actualTypeRef, actualName, baseMethod) => baseMethod(actualTypeRef, actualName);
            generator.OutputTypeAction = (actualTypeRef) => {};
            Assert.Throws<NullReferenceException>(() => generator.OutputTypeNamePair(typeRef, "name"));
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, false, false, "class ")]
        [InlineData(TypeAttributes.NotPublic, true, false, "struct ")]
        [InlineData(TypeAttributes.NotPublic, true, true, "struct ")]
        [InlineData(TypeAttributes.NotPublic, false, true, "enum ")]
        [InlineData(TypeAttributes.Public, false, false, "public class ")]
        [InlineData(TypeAttributes.Public, true, false, "public struct ")]
        [InlineData(TypeAttributes.Public, true, true, "public struct ")]
        [InlineData(TypeAttributes.Public, false, true, "public enum ")]
        [InlineData(TypeAttributes.NestedPublic, false, false, "public class ")]
        [InlineData(TypeAttributes.NestedPublic, true, false, "public struct ")]
        [InlineData(TypeAttributes.NestedPublic, true, true, "public struct ")]
        [InlineData(TypeAttributes.NestedPublic, false, true, "public enum ")]
        [InlineData(TypeAttributes.NestedPrivate, false, false, "private class ")]
        [InlineData(TypeAttributes.NestedPrivate, true, false, "private struct ")]
        [InlineData(TypeAttributes.NestedPrivate, true, true, "private struct ")]
        [InlineData(TypeAttributes.NestedPrivate, false, true, "private enum ")]
        [InlineData(TypeAttributes.NestedFamily, false, false, "class ")]
        [InlineData(TypeAttributes.NestedAssembly, false, false, "class ")]
        [InlineData(TypeAttributes.NestedFamANDAssem, false, false, "class ")]
        [InlineData(TypeAttributes.NestedFamORAssem, false, false, "class ")]
        [InlineData(TypeAttributes.SequentialLayout, false, false, "class ")]
        [InlineData(TypeAttributes.ExplicitLayout, false, false, "class ")]
        [InlineData(TypeAttributes.LayoutMask, false, false, "class ")]
        [InlineData(TypeAttributes.Interface, false, false, "interface ")]
        [InlineData(TypeAttributes.Abstract, false, false, "abstract class ")]
        [InlineData(TypeAttributes.Abstract, true, false, "struct ")]
        [InlineData(TypeAttributes.Abstract, true, true, "struct ")]
        [InlineData(TypeAttributes.Abstract, false, true, "enum ")]
        [InlineData(TypeAttributes.Sealed, false, false, "sealed class ")]
        [InlineData(TypeAttributes.Sealed, true, false, "struct ")]
        [InlineData(TypeAttributes.Sealed, true, true, "struct ")]
        [InlineData(TypeAttributes.Sealed, false, true, "enum ")]
        [InlineData(TypeAttributes.SpecialName, false, false, "class ")]
        [InlineData(TypeAttributes.RTSpecialName, false, false, "class ")]
        [InlineData(TypeAttributes.Import, false, false, "class ")]
        [InlineData(TypeAttributes.Serializable, false, false, "class ")]
        [InlineData(TypeAttributes.WindowsRuntime, false, false, "class ")]
        [InlineData(TypeAttributes.UnicodeClass, false, false, "class ")]
        [InlineData(TypeAttributes.AutoClass, false, false, "class ")]
        [InlineData(TypeAttributes.CustomFormatClass, false, false, "class ")]
        [InlineData(TypeAttributes.HasSecurity, false, false, "class ")]
        [InlineData(TypeAttributes.ReservedMask, false, false, "class ")]
        [InlineData(TypeAttributes.BeforeFieldInit, false, false, "class ")]
        [InlineData(TypeAttributes.CustomFormatMask, false, false, "class ")]
        public void OutputTypeAttributes_Invoke_Success(TypeAttributes attributes, bool isStruct, bool isEnum, string expected)
        {
            CodeGeneratorTests generator = this;
            PerformActionWithOutput(writer =>
            {
                generator.OutputTypeAttributesAction = (actualAttributes, isStruct, isEnum, baseMethod) => baseMethod(actualAttributes, isStruct, isEnum);
                generator.OutputTypeAttributes(attributes, isStruct, isEnum);
                Assert.Equal(expected, writer.ToString());
            });
        }

        [Theory]
        [InlineData(TypeAttributes.NotPublic, false, false)]
        [InlineData(TypeAttributes.NotPublic, true, false)]
        [InlineData(TypeAttributes.NotPublic, true, true)]
        [InlineData(TypeAttributes.NotPublic, false, true)]
        [InlineData(TypeAttributes.Public, false, false)]
        [InlineData(TypeAttributes.Public, true, false)]
        [InlineData(TypeAttributes.Public, true, true)]
        [InlineData(TypeAttributes.Public, false, true)]
        [InlineData(TypeAttributes.NestedPublic, false, false)]
        [InlineData(TypeAttributes.NestedPublic, true, false)]
        [InlineData(TypeAttributes.NestedPublic, true, true)]
        [InlineData(TypeAttributes.NestedPublic, false, true)]
        [InlineData(TypeAttributes.NestedPrivate, false, false)]
        [InlineData(TypeAttributes.NestedPrivate, true, false)]
        [InlineData(TypeAttributes.NestedPrivate, true, true)]
        [InlineData(TypeAttributes.NestedPrivate, false, true)]
        [InlineData(TypeAttributes.NestedFamily, false, false)]
        [InlineData(TypeAttributes.NestedAssembly, false, false)]
        [InlineData(TypeAttributes.NestedFamANDAssem, false, false)]
        [InlineData(TypeAttributes.NestedFamORAssem, false, false)]
        [InlineData(TypeAttributes.SequentialLayout, false, false)]
        [InlineData(TypeAttributes.ExplicitLayout, false, false)]
        [InlineData(TypeAttributes.LayoutMask, false, false)]
        [InlineData(TypeAttributes.Interface, false, false)]
        [InlineData(TypeAttributes.Abstract, false, false)]
        [InlineData(TypeAttributes.Abstract, true, false)]
        [InlineData(TypeAttributes.Abstract, true, true)]
        [InlineData(TypeAttributes.Abstract, false, true)]
        [InlineData(TypeAttributes.Sealed, false, false)]
        [InlineData(TypeAttributes.Sealed, true, false)]
        [InlineData(TypeAttributes.Sealed, true, true)]
        [InlineData(TypeAttributes.Sealed, false, true)]
        [InlineData(TypeAttributes.SpecialName, false, false)]
        [InlineData(TypeAttributes.RTSpecialName, false, false)]
        [InlineData(TypeAttributes.Import, false, false)]
        [InlineData(TypeAttributes.Serializable, false, false)]
        [InlineData(TypeAttributes.WindowsRuntime, false, false)]
        [InlineData(TypeAttributes.UnicodeClass, false, false)]
        [InlineData(TypeAttributes.AutoClass, false, false)]
        [InlineData(TypeAttributes.CustomFormatClass, false, false)]
        [InlineData(TypeAttributes.HasSecurity, false, false)]
        [InlineData(TypeAttributes.ReservedMask, false, false)]
        [InlineData(TypeAttributes.BeforeFieldInit, false, false)]
        [InlineData(TypeAttributes.CustomFormatMask, false, false)]
        public void OutputTypeAttributes_InvokeWithoutWriter_ThrowsNullReferenceException(TypeAttributes attributes, bool isStruct, bool isEnum)
        {
            CodeGeneratorTests generator = this;
            generator.OutputTypeAttributesAction = (actualAttributes, isStruct, isEnum, baseMethod) => baseMethod(actualAttributes, isStruct, isEnum);
            Assert.Throws<NullReferenceException>(() => generator.OutputTypeAttributes(attributes, isStruct, isEnum));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void ValidateIdentifier_InvokeValid_Nop(string value)
        {
            CodeGeneratorTests generator = this;
            int isValidIdentifierCallCount = 0;
            generator.ValidateIdentifierAction = (actualValue, baseMethod) => baseMethod(actualValue);
            generator.IsValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                isValidIdentifierCallCount++;
                return true;
            };
            generator.ValidateIdentifier(value);
            Assert.Equal(1, isValidIdentifierCallCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void ValidateIdentifier_InvokeInvalid_ThrowsArgumentException(string value)
        {
            CodeGeneratorTests generator = this;
            int isValidIdentifierCallCount = 0;
            generator.ValidateIdentifierAction = (actualValue, baseMethod) => baseMethod(actualValue);
            generator.IsValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                isValidIdentifierCallCount++;
                return false;
            };
            Assert.Throws<ArgumentException>("value", () => generator.ValidateIdentifier(value));
            Assert.Equal(1, isValidIdentifierCallCount);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData(null, "identifier")]
        [InlineData("", null)]
        [InlineData("", "")]
        [InlineData("", "identifier")]
        [InlineData("identifier", null)]
        [InlineData("identifier", "")]
        [InlineData("identifier", "escapedIdentifier")]
        public void ICodeGeneratorCreateEscapedIdentifier_Invoke_ReturnsExpected(string value, string result)
        {
            CodeGeneratorTests generator = this;
            int callCount = 0;
            generator.CreateEscapedIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                callCount++;
                return result;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Equal(result, iCodeGenerator.CreateEscapedIdentifier(value));
            Assert.Equal(1, callCount);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "")]
        [InlineData(null, "identifier")]
        [InlineData("", null)]
        [InlineData("", "")]
        [InlineData("", "identifier")]
        [InlineData("identifier", null)]
        [InlineData("identifier", "")]
        [InlineData("identifier", "validIdentifier")]
        public void ICodeGeneratorCreateValidIdentifier_Invoke_ReturnsExpected(string value, string result)
        {
            CodeGeneratorTests generator = this;
            int callCount = 0;
            generator.CreateValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                callCount++;
                return result;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Equal(result, iCodeGenerator.CreateValidIdentifier(value));
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> GetTypeOutput_TestData()
        {
            yield return new object[] { null, null };
            yield return new object[] { null, string.Empty };
            yield return new object[] { null, "Output" };
            yield return new object[] { new CodeTypeReference(), null };
            yield return new object[] { new CodeTypeReference(), string.Empty };
            yield return new object[] { new CodeTypeReference(), "Output" };
        }

        [Theory]
        [MemberData(nameof(GetTypeOutput_TestData))]
        public void ICodeGeneratorGetTypeOutput_Invoke_ReturnsExpected(CodeTypeReference value, string result)
        {
            CodeGeneratorTests generator = this;
            int callCount = 0;
            generator.GetTypeOutputAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                callCount++;
                return result;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Equal(result, iCodeGenerator.GetTypeOutput(value));
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> IsValidIdentifier_TestData()
        {
            foreach (bool result in new bool[] { true, false })
            {
                yield return new object[] { null, result };
                yield return new object[] { "", result };
                yield return new object[] { "value", result };
            }
        }

        [Theory]
        [MemberData(nameof(IsValidIdentifier_TestData))]
        public void ICodeGeneratorIsValidIdentifier_Invoke_ReturnsExpected(string value, bool result)
        {
            CodeGeneratorTests generator = this;
            int callCount = 0;
            generator.IsValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                callCount++;
                return result;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Equal(result, iCodeGenerator.IsValidIdentifier(value));
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> Supports_TestData()
        {
            foreach (bool result in new bool[] { true, false })
            {
                yield return new object[] { GeneratorSupport.ArraysOfArrays - 1, result };
                yield return new object[] { GeneratorSupport.AssemblyAttributes, result };
            }
        }

        [Theory]
        [MemberData(nameof(Supports_TestData))]
        public void ICodeGeneratorSupports_Invoke_ReturnsExpected(GeneratorSupport support, bool result)
        {
            CodeGeneratorTests generator = this;
            int callCount = 0;
            generator.SupportsAction = (actualSupport) =>
            {
                Assert.Equal(support, actualSupport);
                callCount++;
                return result;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Equal(result, iCodeGenerator.Supports(support));
            Assert.Equal(1, callCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        public void ICodeGeneratorValidateIdentifier_InvokeValid_Nop(string value)
        {
            CodeGeneratorTests generator = this;
            int isValidIdentifierCallCount = 0;
            generator.ValidateIdentifierAction = (actualValue, baseMethod) => baseMethod(actualValue);
            generator.IsValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                isValidIdentifierCallCount++;
                return true;
            };
            ICodeGenerator iCodeGenerator = generator;
            iCodeGenerator.ValidateIdentifier(value);
            Assert.Equal(1, isValidIdentifierCallCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("value")]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Not fixed on NetFX")]
        public void ICodeGeneratorValidateIdentifier_InvokeInvalid_ThrowsArgumentException(string value)
        {
            CodeGeneratorTests generator = this;
            int isValidIdentifierCallCount = 0;
            generator.ValidateIdentifierAction = (actualValue, baseMethod) => baseMethod(actualValue);
            generator.IsValidIdentifierAction = (actualValue) =>
            {
                Assert.Same(value, actualValue);
                isValidIdentifierCallCount++;
                return false;
            };
            ICodeGenerator iCodeGenerator = generator;
            Assert.Throws<ArgumentException>("value", () => iCodeGenerator.ValidateIdentifier(value));
            Assert.Equal(1, isValidIdentifierCallCount);
        }

        private void PerformActionWithOutput(Action<StringWriter> action, CodeGeneratorOptions options = null)
        {
            CodeGeneratorTests generator = this;
            ICodeGenerator iCodeGenerator = generator;
            var e = new CodeArrayCreateExpression(typeof(int));
            var writer = new StringWriter();
            int callCount = 0;
            generator.GenerateArrayCreateExpressionAction = (actualE) =>
            {
                Assert.Same(e, actualE);
                Assert.Equal(0, generator.Indent);
                Assert.NotNull(generator.Output);
                if (options != null)
                {
                    Assert.Same(options, generator.Options);
                }
                else
                {
                    Assert.NotNull(generator.Options);
                }
                action(writer);
                callCount++;
            };

            iCodeGenerator.GenerateCodeFromExpression(e, writer, options);
            Assert.Equal(1, callCount);
        }

        protected override string NullToken => "NullToken";

        public Func<string, string> CreateEscapedIdentifierAction { get; set; }

        protected override string CreateEscapedIdentifier(string value)
        {
            return CreateEscapedIdentifierAction(value);
        }

        public Func<string, string> CreateValidIdentifierAction { get; set; }

        protected override string CreateValidIdentifier(string value)
        {
            return CreateValidIdentifierAction(value);
        }

        public Action<CodeArgumentReferenceExpression> GenerateArgumentReferenceExpressionAction { get; set; }

        protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
        {
            GenerateArgumentReferenceExpressionAction(e);
        }

        public Action<CodeArrayCreateExpression> GenerateArrayCreateExpressionAction { get; set; }

        protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
        {
            GenerateArrayCreateExpressionAction(e);
        }

        public Action<CodeArrayIndexerExpression> GenerateArrayIndexerExpressionAction { get; set; }

        protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
        {
            GenerateArrayIndexerExpressionAction(e);
        }

        public Action<CodeAssignStatement> GenerateAssignStatementAction { get; set; }

        protected override void GenerateAssignStatement(CodeAssignStatement e)
        {
            GenerateAssignStatementAction(e);
        }

        public Action<CodeAttachEventStatement> GenerateAttachEventStatementAction { get; set; }

        protected override void GenerateAttachEventStatement(CodeAttachEventStatement e)
        {
            GenerateAttachEventStatementAction(e);
        }
        
        public Action<CodeAttributeDeclarationCollection> GenerateAttributeDeclarationsEndAction { get; set; }

        protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
        {
            GenerateAttributeDeclarationsEndAction(attributes);
        }

        public Action<CodeAttributeDeclarationCollection> GenerateAttributeDeclarationsStartAction { get; set; }

        protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
        {
            GenerateAttributeDeclarationsStartAction(attributes);
        }

        public Action<CodeBaseReferenceExpression> GenerateBaseReferenceExpressionAction { get; set; }

        protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
        {
            GenerateBaseReferenceExpressionAction(e);
        }

        public Action<CodeCastExpression> GenerateCastExpressionAction { get; set; }

        protected override void GenerateCastExpression(CodeCastExpression e)
        {
            GenerateCastExpressionAction(e);
        }

        public Action<CodeComment> GenerateCommentAction { get; set; }

        protected override void GenerateComment(CodeComment e)
        {
            GenerateCommentAction(e);
        }

        public Action<CodeCommentStatementCollection, Action<CodeCommentStatementCollection>> GenerateCommentStatementsAction { get; set; }

        protected override void GenerateCommentStatements(CodeCommentStatementCollection e)
        {
            if (e != null)
            {
                if (e.GetEnumerator().MoveNext())
                {
                    GenerateCommentStatementsAction(e, base.GenerateCommentStatements);
                }
            }
            else
            {
                GenerateCommentStatementsAction(e, base.GenerateCommentStatements);
            }
        }

        public Action<CodeCompileUnit, Action<CodeCompileUnit>> GenerateCompileUnitAction { get; set; }

        protected override void GenerateCompileUnit(CodeCompileUnit e)
        {
            GenerateCompileUnitAction(e, base.GenerateCompileUnit);
        }

        public Action<CodeCompileUnit, Action<CodeCompileUnit>> GenerateCompileUnitEndAction { get; set; }

        protected override void GenerateCompileUnitEnd(CodeCompileUnit e)
        {
            GenerateCompileUnitEndAction(e, base.GenerateCompileUnitEnd);
        }

        public Action<CodeCompileUnit, Action<CodeCompileUnit>> GenerateCompileUnitStartAction { get; set; }

        protected override void GenerateCompileUnitStart(CodeCompileUnit e)
        {
            GenerateCompileUnitStartAction(e, base.GenerateCompileUnitStart);
        }

        public Action<CodeConditionStatement> GenerateConditionStatementAction { get; set; }

        protected override void GenerateConditionStatement(CodeConditionStatement e)
        {
            GenerateConditionStatementAction(e);
        }

        public Action<CodeConstructor, CodeTypeDeclaration> GenerateConstructorAction { get; set; }

        protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
        {
            GenerateConstructorAction(e, c);
        }

        public Action<decimal, Action<decimal>> GenerateDecimalValueAction { get; set; }

        protected override void GenerateDecimalValue(decimal d)
        {
            GenerateDecimalValueAction(d, base.GenerateDecimalValue);
        }

        public Action<CodeDefaultValueExpression, Action<CodeDefaultValueExpression>> GenerateDefaultValueExpressionAction { get; set; }

        protected override void GenerateDefaultValueExpression(CodeDefaultValueExpression e)
        {
            GenerateDefaultValueExpressionAction(e, base.GenerateDefaultValueExpression);
        }

        public Action<CodeDelegateCreateExpression> GenerateDelegateCreateExpressionAction { get; set; }

        protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
        {
            GenerateDelegateCreateExpressionAction(e);
        }

        public Action<CodeDelegateInvokeExpression> GenerateDelegateInvokeExpressionAction { get; set; }

        protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
        {
            GenerateDelegateInvokeExpressionAction(e);
        }

        public Action<CodeDirectionExpression, Action<CodeDirectionExpression>> GenerateDirectionExpressionAction { get; set; }

        protected override void GenerateDirectionExpression(CodeDirectionExpression e)
        {
            GenerateDirectionExpressionAction(e, base.GenerateDirectionExpression);
        }

        public Action<CodeDirectiveCollection, Action<CodeDirectiveCollection>> GenerateDirectivesAction { get; set; }

        protected override void GenerateDirectives(CodeDirectiveCollection directives)
        {
            if (directives != null && directives.GetEnumerator().MoveNext())
            {
                GenerateDirectivesAction(directives, base.GenerateDirectives);
            }
        }

        public Action<double, Action<double>> GenerateDoubleValueAction { get; set; }

        protected override void GenerateDoubleValue(double d)
        {
            GenerateDoubleValueAction(d, base.GenerateDoubleValue);
        }

        public Action<CodeEntryPointMethod, CodeTypeDeclaration> GenerateEntryPointMethodAction { get; set; }

        protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
        {
            GenerateEntryPointMethodAction(e, c);
        }

        public Action<CodeMemberEvent, CodeTypeDeclaration> GenerateEventAction { get; set; }

        protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
        {
            GenerateEventAction(e, c);
        }

        public Action<CodeEventReferenceExpression> GenerateEventReferenceExpressionAction { get; set; }

        protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
        {
            GenerateEventReferenceExpressionAction(e);
        }

        public Action<CodeExpressionStatement> GenerateExpressionStatementAction { get; set; }

        protected override void GenerateExpressionStatement(CodeExpressionStatement e)
        {
            GenerateExpressionStatementAction(e);
        }

        public Action<CodeMemberField> GenerateFieldAction { get; set; }

        protected override void GenerateField(CodeMemberField e)
        {
            GenerateFieldAction(e);
        }

        public Action<CodeFieldReferenceExpression> GenerateFieldReferenceExpressionAction { get; set; }

        protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
        {
            GenerateFieldReferenceExpressionAction(e);
        }

        public Action<CodeGotoStatement> GenerateGotoStatementAction { get; set; }

        protected override void GenerateGotoStatement(CodeGotoStatement e)
        {
            GenerateGotoStatementAction(e);
        }

        public Action<CodeIndexerExpression> GenerateIndexerExpressionAction { get; set; }

        protected override void GenerateIndexerExpression(CodeIndexerExpression e)
        {
            GenerateIndexerExpressionAction(e);
        }

        public Action<CodeIterationStatement> GenerateIterationStatementAction { get; set; }

        protected override void GenerateIterationStatement(CodeIterationStatement e)
        {
            GenerateIterationStatementAction(e);
        }

        public Action<CodeLabeledStatement> GenerateLabeledStatementAction { get; set; }

        protected override void GenerateLabeledStatement(CodeLabeledStatement e)
        {
            GenerateLabeledStatementAction(e);
        }

        public Action<CodeLinePragma> GenerateLinePragmaEndAction { get; set; }

        protected override void GenerateLinePragmaEnd(CodeLinePragma e)
        {
            GenerateLinePragmaEndAction(e);
        }

        public Action<CodeLinePragma> GenerateLinePragmaStartAction { get; set; }

        protected override void GenerateLinePragmaStart(CodeLinePragma e)
        {
            GenerateLinePragmaStartAction(e);
        }

        public Action<CodeMemberMethod, CodeTypeDeclaration> GenerateMethodAction { get; set; }

        protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
        {
            GenerateMethodAction(e, c);
        }

        public Action<CodeMethodInvokeExpression> GenerateMethodInvokeExpressionAction { get; set; }

        protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
        {
            GenerateMethodInvokeExpressionAction(e);
        }

        public Action<CodeMethodReferenceExpression> GenerateMethodReferenceExpressionAction { get; set; }

        protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
        {
            GenerateMethodReferenceExpressionAction(e);
        }

        public Action<CodeMethodReturnStatement> GenerateMethodReturnStatementAction { get; set; }

        protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
        {
            GenerateMethodReturnStatementAction(e);
        }

        public Action<CodeNamespace, Action<CodeNamespace>> GenerateNamespaceAction { get; set; }

        protected override void GenerateNamespace(CodeNamespace e)
        {
            GenerateNamespaceAction(e, base.GenerateNamespace);
        }

        public Action<CodeNamespace> GenerateNamespaceEndAction { get; set; }

        protected override void GenerateNamespaceEnd(CodeNamespace e)
        {
            GenerateNamespaceEndAction(e);
        }

        public Action<CodeNamespaceImport> GenerateNamespaceImportAction { get; set; }

        protected override void GenerateNamespaceImport(CodeNamespaceImport e)
        {
            GenerateNamespaceImportAction(e);
        }

        public Action<CodeNamespace> GenerateNamespaceStartAction { get; set; }

        protected override void GenerateNamespaceStart(CodeNamespace e)
        {
            GenerateNamespaceStartAction(e);
        }

        public Action<CodeObjectCreateExpression> GenerateObjectCreateExpressionAction { get; set; }

        protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
        {
            GenerateObjectCreateExpressionAction(e);
        }

        public Action<CodeParameterDeclarationExpression, Action<CodeParameterDeclarationExpression>>  GenerateParameterDeclarationExpressionAction { get; set; }

        protected override void GenerateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            GenerateParameterDeclarationExpressionAction(e, base.GenerateParameterDeclarationExpression);
        }

        public Action<CodePrimitiveExpression, Action<CodePrimitiveExpression>> GeneratePrimitiveExpressionAction { get; set; }

        protected override void GeneratePrimitiveExpression(CodePrimitiveExpression e)
        {
            GeneratePrimitiveExpressionAction(e, base.GeneratePrimitiveExpression);
        }

        public Action<CodeMemberProperty, CodeTypeDeclaration> GeneratePropertyAction { get; set; }

        protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
        {
            GeneratePropertyAction(e, c);
        }

        public Action<CodePropertyReferenceExpression> GeneratePropertyReferenceExpressionAction { get; set; }

        protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
        {
            GeneratePropertyReferenceExpressionAction(e);
        }

        public Action<CodePropertySetValueReferenceExpression> GeneratePropertySetValueReferenceExpressionAction { get; set; }

        protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
        {
            GeneratePropertySetValueReferenceExpressionAction(e);
        }

        public Action<CodeRemoveEventStatement> GenerateRemoveEventStatementAction { get; set; }

        protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
        {
            GenerateRemoveEventStatementAction(e);
        }

        public Action<float, Action<float>> GenerateSingleFloatValueAction { get; set; }

        protected override void GenerateSingleFloatValue(float s)
        {
            GenerateSingleFloatValueAction(s, base.GenerateSingleFloatValue);
        }

        public Action<CodeSnippetExpression> GenerateSnippetExpressionAction { get; set; }

        protected override void GenerateSnippetExpression(CodeSnippetExpression e)
        {
            GenerateSnippetExpressionAction(e);
        }

        public Action<CodeSnippetTypeMember> GenerateSnippetMemberAction { get; set; }

        protected override void GenerateSnippetMember(CodeSnippetTypeMember e)
        {
            GenerateSnippetMemberAction(e);
        }

        public Action<CodeSnippetStatement, Action<CodeSnippetStatement>> GenerateSnippetStatementAction { get; set; }

        protected override void GenerateSnippetStatement(CodeSnippetStatement e)
        {
            GenerateSnippetStatementAction(e, base.GenerateSnippetStatement);
        }

        public Action<CodeThisReferenceExpression> GenerateThisReferenceExpressionAction { get; set; }

        protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
        {
            GenerateThisReferenceExpressionAction(e);
        }

        public Action<CodeThrowExceptionStatement> GenerateThrowExceptionStatementAction { get; set; }

        protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
        {
            GenerateThrowExceptionStatementAction(e);
        }

        public Action<CodeTryCatchFinallyStatement> GenerateTryCatchFinallyStatementAction { get; set; }

        protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
        {
            GenerateTryCatchFinallyStatementAction(e);
        }

        public Action<CodeTypeConstructor> GenerateTypeConstructorAction { get; set; }

        protected override void GenerateTypeConstructor(CodeTypeConstructor e)
        {
            GenerateTypeConstructorAction(e);
        }

        public Action<CodeTypeDeclaration> GenerateTypeEndAction { get; set; }

        protected override void GenerateTypeEnd(CodeTypeDeclaration e)
        {
            GenerateTypeEndAction(e);
        }

        public Action<CodeTypeOfExpression, Action<CodeTypeOfExpression>> GenerateTypeOfExpressionAction { get; set; } 

        protected override void GenerateTypeOfExpression(CodeTypeOfExpression e)
        {
            GenerateTypeOfExpressionAction(e, base.GenerateTypeOfExpression);
        }

        public Action<CodeTypeReferenceExpression, Action<CodeTypeReferenceExpression>> GenerateTypeReferenceExpressionAction { get; set; } 

        protected override void GenerateTypeReferenceExpression(CodeTypeReferenceExpression e)
        {
            GenerateTypeReferenceExpressionAction(e, base.GenerateTypeReferenceExpression);
        }

        public Action<CodeTypeDeclaration> GenerateTypeStartAction { get; set; }

        protected override void GenerateTypeStart(CodeTypeDeclaration e)
        {
            GenerateTypeStartAction(e);
        }

        public Action<CodeVariableDeclarationStatement> GenerateVariableDeclarationStatementAction { get; set; }

        protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
        {
            GenerateVariableDeclarationStatementAction(e);
        }

        public Action<CodeVariableReferenceExpression> GenerateVariableReferenceExpressionAction { get; set; }

        protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
        {
            GenerateVariableReferenceExpressionAction(e);
        }

        public Func<CodeTypeReference, string> GetTypeOutputAction { get; set; }

        protected override string GetTypeOutput(CodeTypeReference value)
        {
            return GetTypeOutputAction(value);
        }

        public Func<string, bool> IsValidIdentifierAction { get; set; }

        protected override bool IsValidIdentifier(string value)
        {
            return IsValidIdentifierAction(value);   
        }

        public Action<CodeAttributeArgument, Action<CodeAttributeArgument>> OutputAttributeArgumentAction { get; set; }

        protected override void OutputAttributeArgument(CodeAttributeArgument arg)
        {
            OutputAttributeArgumentAction(arg, base.OutputAttributeArgument);
        }

        public Action<CodeAttributeDeclarationCollection, Action<CodeAttributeDeclarationCollection>> OutputAttributeDeclarationsAction { get; set; }

        protected override void OutputAttributeDeclarations(CodeAttributeDeclarationCollection attributes)
        {
            OutputAttributeDeclarationsAction(attributes, base.OutputAttributeDeclarations);
        }

        public Action<FieldDirection, Action<FieldDirection>> OutputDirectionAction { get; set; }

        protected override void OutputDirection(FieldDirection dir)
        {
            OutputDirectionAction(dir, base.OutputDirection);
        }

        public Action<MemberAttributes, Action<MemberAttributes>> OutputFieldScopeModifierAction { get; set; }

        protected override void OutputFieldScopeModifier(MemberAttributes attributes)
        {
            OutputFieldScopeModifierAction(attributes, base.OutputFieldScopeModifier);
        }

        public Action<string, Action<string>> OutputIdentifierAction { get; set; }

        protected override void OutputIdentifier(string ident)
        {
            OutputIdentifierAction(ident, base.OutputIdentifier);
        }

        public Action<MemberAttributes, Action<MemberAttributes>> OutputMemberAccessModifierAction { get; set; }

        protected override void OutputMemberAccessModifier(MemberAttributes attributes)
        {
            OutputMemberAccessModifierAction(attributes, base.OutputMemberAccessModifier);
        }

        public Action<MemberAttributes, Action<MemberAttributes>> OutputMemberScopeModifierAction { get; set; }

        protected override void OutputMemberScopeModifier(MemberAttributes attributes)
        {
            OutputMemberScopeModifierAction(attributes, base.OutputMemberScopeModifier);
        }

        public Action<CodeBinaryOperatorType, Action<CodeBinaryOperatorType>> OutputOperatorAction { get; set; }

        protected override void OutputOperator(CodeBinaryOperatorType op)
        {
            OutputOperatorAction(op, base.OutputOperator);
        }

        public Action<CodeParameterDeclarationExpressionCollection, Action<CodeParameterDeclarationExpressionCollection>> OutputParametersAction { get; set; }

        protected override void OutputParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            OutputParametersAction(parameters, base.OutputParameters);
        }

        public Action<CodeTypeReference> OutputTypeAction { get; set; }

        protected override void OutputType(CodeTypeReference typeRef)
        {
            OutputTypeAction(typeRef);
        }

        public Action<TypeAttributes, bool, bool, Action<TypeAttributes, bool, bool>> OutputTypeAttributesAction { get; set; }

        protected override void OutputTypeAttributes(TypeAttributes attributes, bool isStruct, bool isEnum)
        {
            OutputTypeAttributesAction(attributes, isStruct, isEnum, base.OutputTypeAttributes);
        }

        public Action<CodeTypeReference, string, Action<CodeTypeReference, string>> OutputTypeNamePairAction { get; set; }

        protected override void OutputTypeNamePair(CodeTypeReference typeRef, string name)
        {
            OutputTypeNamePairAction(typeRef, name, base.OutputTypeNamePair);
        }

        public Func<string, string> QuoteSnippetStringAction { get; set; }

        protected override string QuoteSnippetString(string value)
        {
            return QuoteSnippetStringAction(value);
        }

        public Func<GeneratorSupport, bool> SupportsAction { get; set; }

        protected override bool Supports(GeneratorSupport support)
        {
            return SupportsAction(support);
        }

        public Action<string, Action<string>> ValidateIdentifierAction { get; set; }

        protected override void ValidateIdentifier(string value)
        {
            ValidateIdentifierAction(value, base.ValidateIdentifier);
        }

        private class CustomCodeExpression : CodeExpression
        {
        }

        private class CustomCodeStatement : CodeStatement
        {
        }
    }
}
