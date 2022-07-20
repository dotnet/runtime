// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CSharpCodeGeneratorTests
    {
        private static IEnumerable<string> Identifier_TestData()
        {
            yield return "as";
            yield return "do";
            yield return "if";
            yield return "in";
            yield return "is";
            yield return "for";
            yield return "int";
            yield return "new";
            yield return "out";
            yield return "ref";
            yield return "try";
            yield return "base";
            yield return "byte";
            yield return "bool";
            yield return "case";
            yield return "char";
            yield return "else";
            yield return "enum";
            yield return "goto";
            yield return "lock";
            yield return "long";
            yield return "null";
            yield return "this";
            yield return "true";
            yield return "uint";
            yield return "void";
            yield return "break";
            yield return "catch";
            yield return "class";
            yield return "const";
            yield return "event";
            yield return "false";
            yield return "fixed";
            yield return "float";
            yield return "sbyte";
            yield return "short";
            yield return "throw";
            yield return "ulong";
            yield return "using";
            yield return "where";
            yield return "while";
            yield return "yield";
            yield return "double";
            yield return "extern";
            yield return "object";
            yield return "params";
            yield return "public";
            yield return "return";
            yield return "sealed";
            yield return "sizeof";
            yield return "static";
            yield return "string";
            yield return "struct";
            yield return "switch";
            yield return "typeof";
            yield return "unsafe";
            yield return "ushort";
            yield return "checked";
            yield return "decimal";
            yield return "default";
            yield return "finally";
            yield return "foreach";
            yield return "partial";
            yield return "private";
            yield return "virtual";
            yield return "abstract";
            yield return "continue";
            yield return "delegate";
            yield return "explicit";
            yield return "implicit";
            yield return "internal";
            yield return "operator";
            yield return "override";
            yield return "readonly";
            yield return "volatile";
            yield return "__arglist";
            yield return "__makeref";
            yield return "__reftype";
            yield return "interface";
            yield return "namespace";
            yield return "protected";
            yield return "unchecked";
            yield return "__refvalue";
            yield return "stackalloc";
        }

        public static IEnumerable<object[]> CreateEscapedIdentifier_TestData()
        {
            yield return new object[] { string.Empty, string.Empty };
            yield return new object[] { new string('a', 512), new string('a', 512) };
            yield return new object[] { new string('a', 513), new string('a', 513) };
            yield return new object[] { "@", "@" };
            yield return new object[] { "@value", "@value" };
            yield return new object[] { "@as", "@as" };
            yield return new object[] { "a", "a" };
            yield return new object[] { "_", "_" };
            yield return new object[] { "_a", "_a" };
            yield return new object[] { "__", "__" };
            yield return new object[] { "__a", "@__a" };
            yield return new object[] { "___a", "___a" };

            yield return new object[] { "ab", "ab" };
            yield return new object[] { "abc", "abc" };
            yield return new object[] { "abcd", "abcd" };
            yield return new object[] { "abcde", "abcde" };
            yield return new object[] { "abcdef", "abcdef" };
            yield return new object[] { "abcdefg", "abcdefg" };
            yield return new object[] { "abcdefgh", "abcdefgh" };
            yield return new object[] { "abcdefghi", "abcdefghi" };
            yield return new object[] { "abcdefghij", "abcdefghij" };

            foreach (string identifier in Identifier_TestData())
            {
                yield return new object[] { identifier, $"@{identifier}" };
            }
        }

        [Theory]
        [MemberData(nameof(CreateEscapedIdentifier_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework is missing some newer keywords")]
        public void CreateEscapedIdentifier_Invoke_ReturnsExpected(string value, string expected)
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Equal(expected, generator.CreateEscapedIdentifier(value));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void CreateEscapedIdentifier_NullValue_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Throws<ArgumentNullException>("name", () => generator.CreateEscapedIdentifier(null));
        }

        public static IEnumerable<object[]> CreateValidIdentifier_TestData()
        {
            yield return new object[] { string.Empty, string.Empty };
            yield return new object[] { new string('a', 512), new string('a', 512) };
            yield return new object[] { new string('a', 513), new string('a', 513) };
            yield return new object[] { "@", "@" };
            yield return new object[] { "@value", "@value" };
            yield return new object[] { "value", "value" };
            yield return new object[] { "a", "a" };
            yield return new object[] { "_", "_" };
            yield return new object[] { "_a", "_a" };
            yield return new object[] { "__", "__" };
            yield return new object[] { "__a", "___a" };
            yield return new object[] { "___a", "___a" };

            yield return new object[] { "ab", "ab" };
            yield return new object[] { "abc", "abc" };
            yield return new object[] { "abcd", "abcd" };
            yield return new object[] { "abcde", "abcde" };
            yield return new object[] { "abcdef", "abcdef" };
            yield return new object[] { "abcdefg", "abcdefg" };
            yield return new object[] { "abcdefgh", "abcdefgh" };
            yield return new object[] { "abcdefghi", "abcdefghi" };
            yield return new object[] { "abcdefghij", "abcdefghij" };

            foreach (string identifier in Identifier_TestData())
            {
                yield return new object[] { identifier, $"_{identifier}" };
            }
        }

        [Theory]
        [MemberData(nameof(CreateValidIdentifier_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework is missing some newer keywords")]
        public void CreateValidIdentifier_Invoke_ReturnsExpected(string value, string expected)
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Equal(expected, generator.CreateValidIdentifier(value));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void CreateValidIdentifier_NullValue_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Throws<ArgumentNullException>("name", () => generator.CreateValidIdentifier(null));
        }

        public static IEnumerable<object[]> IsValidIdentifier_TestData()
        {
            yield return new object[] { null, false };
            yield return new object[] { string.Empty, false };
            yield return new object[] { new string('a', 512), true };
            yield return new object[] { new string('a', 513), false };
            yield return new object[] { "  ", false };
            yield return new object[] { "a", true };
            yield return new object[] { "A", true };
            yield return new object[] { "\u01C5", true };
            yield return new object[] { "\u02B0", true };
            yield return new object[] { "\u2163", true };
            yield return new object[] { "\u0620", true };
            yield return new object[] { "_", true };
            yield return new object[] { "_aA\u01C5\u02B0\u2163\u0620_0", true };
            yield return new object[] { "aA\u01C5\u02B0\u2163\u0620_0", true };
            yield return new object[] { " ", false };
            yield return new object[] { "a ", false };
            yield return new object[] { "#", false };
            yield return new object[] { "a#", false };
            yield return new object[] { "\u0300", false };
            yield return new object[] { "a\u0300", true };
            yield return new object[] { "\u0903", false };
            yield return new object[] { "a\u0903", true };
            yield return new object[] { "\u203F", false };
            yield return new object[] { "a\u203F", true };
            yield return new object[] { "0", false };
            yield return new object[] { "1", false };
            yield return new object[] { ":", false };
            yield return new object[] { ".", false };
            yield return new object[] { "$", false };
            yield return new object[] { "+", false };
            yield return new object[] { "<", false };
            yield return new object[] { ">", false };
            yield return new object[] { "-", false };
            yield return new object[] { "[", false };
            yield return new object[] { "]", false };
            yield return new object[] { ",", false };
            yield return new object[] { "&", false };
            yield return new object[] { "*", false };
            yield return new object[] { "`", false };
            yield return new object[] { "a0", true };
            yield return new object[] { "a1", true };
            yield return new object[] { "a:", false };
            yield return new object[] { "a.", false };
            yield return new object[] { "a$", false };
            yield return new object[] { "a+", false };
            yield return new object[] { "a<", false };
            yield return new object[] { "a>", false };
            yield return new object[] { "a-", false };
            yield return new object[] { "a[", false };
            yield return new object[] { "a]", false };
            yield return new object[] { "a,", false };
            yield return new object[] { "a&", false };
            yield return new object[] { "a*", false };
            yield return new object[] { "a*", false };
            yield return new object[] { "\0", false };
            yield return new object[] { "a\0", false };
            yield return new object[] { "\r", false };
            yield return new object[] { "a\r", false };
            yield return new object[] { "\n", false };
            yield return new object[] { "a\n", false };

            yield return new object[] { "@", false };
            yield return new object[] { "@value", true };
            yield return new object[] { "@as", true };
            yield return new object[] { "_a", true };
            yield return new object[] { "__", true };
            yield return new object[] { "__a", true };
            yield return new object[] { "___a", true };

            yield return new object[] { "ab", true };
            yield return new object[] { "abc", true };
            yield return new object[] { "abcd", true };
            yield return new object[] { "abcde", true };
            yield return new object[] { "abcdef", true };
            yield return new object[] { "abcdefg", true };
            yield return new object[] { "abcdefgh", true };
            yield return new object[] { "abcdefghi", true };
            yield return new object[] { "abcdefghij", true };

            foreach (string identifier in Identifier_TestData())
            {
                yield return new object[] { identifier, false };
            }
        }

        [Theory]
        [MemberData(nameof(IsValidIdentifier_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework is missing some newer keywords")]
        public void IsValidIdentifier_Invoke_ReturnsExpected(string value, bool expected)
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Equal(expected, generator.IsValidIdentifier(value));
        }

        public static IEnumerable<object[]> GenerateCodeFromExpression_TestData()
        {
            string nl = Environment.NewLine;
            var customOptions = new CodeGeneratorOptions
            {
                IndentString = "$",
                ElseOnClosing = true,
                BracingStyle = "C"
            };

            // CodeArrayCreateExpression.
            yield return new object[] { new CodeArrayCreateExpression(), null, "new void[0]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type")), null, "new type[0]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type", 2)), null, "new type[0]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference(new CodeTypeReference("type", 1), 1)), null, "new type[0][]" };

            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type"), new CodeExpression[] { new CodePrimitiveExpression(1) }), null, $"new type[] {{{nl}        1}}" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type"), new CodeExpression[] { new CodePrimitiveExpression(1), new CodePrimitiveExpression(2) }), null, $"new type[] {{{nl}        1,{nl}        2}}" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type"), new CodeExpression[] { new CodePrimitiveExpression(1), new CodePrimitiveExpression(2) }) { SizeExpression = new CodeExpression() }, null, $"new type[] {{{nl}        1,{nl}        2}}" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type", 2), new CodeExpression[] { new CodePrimitiveExpression(1) }), null, $"new type[,] {{{nl}        1}}" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference(new CodeTypeReference("type", 1), 1), new CodeExpression[] { new CodePrimitiveExpression(1) }), null, $"new type[][] {{{nl}        1}}" };

            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type")) { SizeExpression = new CodePrimitiveExpression(1) }, null, "new type[1]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type")) { Size = 0 }, null, "new type[0]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type")) { Size = -1 }, null, "new type[-1]" };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("type")) { Size = 1 }, null, "new type[1]" };

            // CodeBaseReferenceExpression.
            yield return new object[] { new CodeBaseReferenceExpression(), null, "base" };

            // CodeBinaryOperatorExpression.
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), null, "(1 + 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Assign, new CodePrimitiveExpression(2)), null, "(1 = 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.BitwiseAnd, new CodePrimitiveExpression(2)), null, "(1 & 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.BitwiseOr, new CodePrimitiveExpression(2)), null, "(1 | 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.BooleanAnd, new CodePrimitiveExpression(2)), null, "(1 && 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.BooleanOr, new CodePrimitiveExpression(2)), null, "(1 || 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Divide, new CodePrimitiveExpression(2)), null, "(1 / 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.GreaterThan, new CodePrimitiveExpression(2)), null, "(1 > 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.GreaterThanOrEqual, new CodePrimitiveExpression(2)), null, "(1 >= 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(2)), null, "(1 == 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.IdentityInequality, new CodePrimitiveExpression(2)), null, "(1 != 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.LessThan, new CodePrimitiveExpression(2)), null, "(1 < 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.LessThanOrEqual, new CodePrimitiveExpression(2)), null, "(1 <= 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Modulus, new CodePrimitiveExpression(2)), null, "(1 % 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodePrimitiveExpression(2)), null, "(1 * 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(2)), null, "(1 - 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.ValueEquality, new CodePrimitiveExpression(2)), null, "(1 == 2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add - 1, new CodePrimitiveExpression(2)), null, "(1  2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.GreaterThanOrEqual + 1, new CodePrimitiveExpression(2)), null, "(1  2)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodePrimitiveExpression(2)), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3)), null, $"((1 * 2) {Environment.NewLine}            + 3)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodeBinaryOperatorExpression(new CodePrimitiveExpression(2), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3))), null, $"(1 {Environment.NewLine}            * (2 + 3))" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodePrimitiveExpression(2)), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3)), customOptions, $"((1 * 2) {Environment.NewLine}$$$+ 3)" };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Multiply, new CodeBinaryOperatorExpression(new CodePrimitiveExpression(2), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(3))), customOptions, $"(1 {Environment.NewLine}$$$* (2 + 3))" };

            // CodeCastExpression.
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("type"), new CodePrimitiveExpression(1)), null, "((type)(1))" };

            // CodeDelegateCreateExpression.
            yield return new object[] { new CodeDelegateCreateExpression((CodeTypeReference)null, new CodePrimitiveExpression(1), string.Empty), null, "new void(1.)" };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("type"), new CodePrimitiveExpression(1), "methodName"), null, "new type(1.methodName)" };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("is"), new CodePrimitiveExpression(1), "as"), null, "new @is(1.@as)" };

            // CodeFieldReferenceExpression.
            yield return new object[] { new CodeFieldReferenceExpression(), null, "" };
            yield return new object[] { new CodeFieldReferenceExpression(null, string.Empty), null, "" };
            yield return new object[] { new CodeFieldReferenceExpression(null, "fieldName"), null, "fieldName" };
            yield return new object[] { new CodeFieldReferenceExpression(null, "as"), null, "@as" };
            yield return new object[] { new CodeFieldReferenceExpression(new CodePrimitiveExpression(1), string.Empty), null, "1." };
            yield return new object[] { new CodeFieldReferenceExpression(new CodePrimitiveExpression(1), "fieldName"), null, "1.fieldName" };
            yield return new object[] { new CodeFieldReferenceExpression(new CodePrimitiveExpression(1), "as"), null, "1.@as" };

            // CodeArgumentReferenceExpression.
            yield return new object[] { new CodeArgumentReferenceExpression(), null, "" };
            yield return new object[] { new CodeArgumentReferenceExpression(string.Empty), null, "" };
            yield return new object[] { new CodeArgumentReferenceExpression("parameterName"), null, "parameterName" };
            yield return new object[] { new CodeArgumentReferenceExpression("as"), null, "@as" };

            // CodeVariableReferenceExpression.
            yield return new object[] { new CodeVariableReferenceExpression(), null, "" };
            yield return new object[] { new CodeVariableReferenceExpression(string.Empty), null, "" };
            yield return new object[] { new CodeVariableReferenceExpression("variableName"), null, "variableName" };
            yield return new object[] { new CodeVariableReferenceExpression("as"), null, "@as" };

            // CodeIndexerExpression.
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1)), null, "1[]" };
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "1[2]" };
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2), new CodePrimitiveExpression(3)), null, "1[2, 3]" };

            // CodeArrayIndexerExpression.
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1)), null, "1[]" };
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "1[2]" };
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2), new CodePrimitiveExpression(3)), null, "1[2, 3]" };

            // GenerateSnippetExpression.
            yield return new object[] { new CodeSnippetExpression(), null, "" };
            yield return new object[] { new CodeSnippetExpression(string.Empty), null, "" };
            yield return new object[] { new CodeSnippetExpression("value"), null, "value" };
            yield return new object[] { new CodeSnippetExpression("as"), null, "as" };

            // CodeMethodInvokeExpression
            yield return new object[] { new CodeMethodInvokeExpression(), null, "()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression()), null, "()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, string.Empty)), null, "()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "methodName")), null, "methodName()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "methodName", new CodeTypeReference("type1"))), null, "methodName<type1>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2"))), null, "methodName<type1, type2>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "methodName"), new CodePrimitiveExpression(1)), null, "methodName(1)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "methodName"), new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "methodName(1, 2)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "as")), null, "@as()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "as", new CodeTypeReference("is"))), null, "@as<@is>()" };

            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), string.Empty)), null, "1.()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName")), null, "1.methodName()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName", new CodeTypeReference("type1"))), null, "1.methodName<type1>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2"))), null, "1.methodName<type1, type2>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName"), new CodePrimitiveExpression(2)), null, "1.methodName(2)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName"), new CodePrimitiveExpression(2), new CodePrimitiveExpression(3)), null, "1.methodName(2, 3)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "as")), null, "1.@as()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "as", new CodeTypeReference("is"))), null, "1.@as<@is>()" };

            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), string.Empty)), null, "((1 + 2)).()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName")), null, "((1 + 2)).methodName()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName", new CodeTypeReference("type1"))), null, "((1 + 2)).methodName<type1>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2"))), null, "((1 + 2)).methodName<type1, type2>()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName"), new CodePrimitiveExpression(3)), null, "((1 + 2)).methodName(3)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName"), new CodePrimitiveExpression(3), new CodePrimitiveExpression(4)), null, "((1 + 2)).methodName(3, 4)" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "as")), null, "((1 + 2)).@as()" };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "as", new CodeTypeReference("is"))), null, "((1 + 2)).@as<@is>()" };

            // GenerateMethodReferenceExpression.
            yield return new object[] { new CodeMethodReferenceExpression(), null, "" };
            yield return new object[] { new CodeMethodReferenceExpression(null, string.Empty), null, "" };
            yield return new object[] { new CodeMethodReferenceExpression(null, "methodName"), null, "methodName" };
            yield return new object[] { new CodeMethodReferenceExpression(null, "methodName", new CodeTypeReference("type1")), null, "methodName<type1>" };
            yield return new object[] { new CodeMethodReferenceExpression(null, "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2")), null, "methodName<type1, type2>" };
            yield return new object[] { new CodeMethodReferenceExpression(null, "as"), null, "@as" };
            yield return new object[] { new CodeMethodReferenceExpression(null, "as", new CodeTypeReference("is")), null, "@as<@is>" };

            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), string.Empty), null, "1." };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName"), null, "1.methodName" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName", new CodeTypeReference("type1")), null, "1.methodName<type1>" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2")), null, "1.methodName<type1, type2>" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "as"), null, "1.@as" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "as", new CodeTypeReference("is")), null, "1.@as<@is>" };

            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), string.Empty), null, "((1 + 2))." };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName"), null, "((1 + 2)).methodName" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName", new CodeTypeReference("type1")), null, "((1 + 2)).methodName<type1>" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "methodName", new CodeTypeReference("type1"), new CodeTypeReference("type2")), null, "((1 + 2)).methodName<type1, type2>" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "as"), null, "((1 + 2)).@as" };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)), "as", new CodeTypeReference("is")), null, "((1 + 2)).@as<@is>" };

            // CodeEventReferenceExpression.
            yield return new object[] { new CodeEventReferenceExpression(), null, "" };
            yield return new object[] { new CodeEventReferenceExpression(null, string.Empty), null, "" };
            yield return new object[] { new CodeEventReferenceExpression(null, "eventName"), null, "eventName" };
            yield return new object[] { new CodeEventReferenceExpression(null, "as"), null, "@as" };
            yield return new object[] { new CodeEventReferenceExpression(new CodePrimitiveExpression(1), string.Empty), null, "1." };
            yield return new object[] { new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "eventName"), null, "1.eventName" };
            yield return new object[] { new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "as"), null, "1.@as" };

            // CodeDelegateInvokeExpression.
            yield return new object[] { new CodeDelegateInvokeExpression(), null, "()" };
            yield return new object[] { new CodeDelegateInvokeExpression(null), null, "()" };
            yield return new object[] { new CodeDelegateInvokeExpression(null, new CodePrimitiveExpression(1)), null, "(1)" };
            yield return new object[] { new CodeDelegateInvokeExpression(null, new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "(1, 2)" };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1)), null, "1()" };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "1(2)" };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2), new CodePrimitiveExpression(3)), null, "1(2, 3)" };

            // CodeObjectCreateExpression.
            yield return new object[] { new CodeObjectCreateExpression(), null, "new void()" };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("type")), null, "new type()" };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("type"), new CodePrimitiveExpression(1)), null, "new type(1)" };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("type"), new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, "new type(1, 2)" };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("as")), null, "new @as()" };

            // CodeParameterDeclarationExpression.
            yield return new object[] { new CodeParameterDeclarationExpression(), null, "void " };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty), null, "type " };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"), null, "type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.In }, null, "type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Out }, null, "out type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Ref }, null, "ref type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.In - 1 }, null, "type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Ref + 1 }, null, "type name" };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("as"), "is"), null, "@as @is" };

            var parameterDeclarationExpression = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            parameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration());
            parameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name1")));
            parameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name2"), new CodeAttributeArgument(new CodePrimitiveExpression(1))));
            parameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name3"), new CodeAttributeArgument("arg1", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));
            parameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("is"), new CodeAttributeArgument("as", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));
            yield return new object[] { parameterDeclarationExpression, null, "[()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)] type name" };

            foreach (string paramsName in new string[] { "System.ParamArrayAttribute", "system.paramsarrayattribute" })
            {
                var paramsParameterDeclarationExpression = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration());
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name1")));
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name2"), new CodeAttributeArgument(new CodePrimitiveExpression(1))));
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name3"), new CodeAttributeArgument("arg1", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("is"), new CodeAttributeArgument("as", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));
                paramsParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("System.ParamArrayAttribute"), new CodeAttributeArgument("arg1", new CodePrimitiveExpression(1))));
                yield return new object[] { paramsParameterDeclarationExpression, null, "[()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)] params type name" };
            }

            // CodeDirectionExpression.
            yield return new object[] { new CodeDirectionExpression(FieldDirection.In, new CodePrimitiveExpression(1)), null, "1" };
            yield return new object[] { new CodeDirectionExpression(FieldDirection.Out, new CodePrimitiveExpression(1)), null, "out 1" };
            yield return new object[] { new CodeDirectionExpression(FieldDirection.Ref, new CodePrimitiveExpression(1)), null, "ref 1" };
            yield return new object[] { new CodeDirectionExpression(FieldDirection.In - 1, new CodePrimitiveExpression(1)), null, "1" };
            yield return new object[] { new CodeDirectionExpression(FieldDirection.Ref + 1, new CodePrimitiveExpression(1)), null, "1" };

            // CodePrimitiveExpression.
            yield return new object[] { new CodePrimitiveExpression(), null, "null" };
            yield return new object[] { new CodePrimitiveExpression('\r'), null, "'\\r'" };
            yield return new object[] { new CodePrimitiveExpression('\t'), null, "'\\t'" };
            yield return new object[] { new CodePrimitiveExpression('\"'), null, "'\\\"'" };
            yield return new object[] { new CodePrimitiveExpression('\''), null, "'\\''" };
            yield return new object[] { new CodePrimitiveExpression('\0'), null, "'\\0'" };
            yield return new object[] { new CodePrimitiveExpression('\\'), null, "'\\\\'" };
            yield return new object[] { new CodePrimitiveExpression('\n'), null, "'\\n'" };
            yield return new object[] { new CodePrimitiveExpression('\u2027'), null, "'\u2027'" };
            yield return new object[] { new CodePrimitiveExpression('\u2028'), null, "'\\u2028'" };
            yield return new object[] { new CodePrimitiveExpression('\u2029'), null, "'\\u2029'" };
            yield return new object[] { new CodePrimitiveExpression('\u2030'), null, "'\u2030'" };
            yield return new object[] { new CodePrimitiveExpression('\u0083'), null, "'\u0083'" };
            yield return new object[] { new CodePrimitiveExpression('\u0084'), null, "'\\u0084'" };
            yield return new object[] { new CodePrimitiveExpression('\u0085'), null, "'\\u0085'" };
            yield return new object[] { new CodePrimitiveExpression('\u0086'), null, "'\u0086'" };
            yield return new object[] { new CodePrimitiveExpression('a'), null, "'a'" };
            yield return new object[] { new CodePrimitiveExpression('\uDC00'), null, "'\\uDC00'" };
            yield return new object[] { new CodePrimitiveExpression('\uD800'), null, "'\\uD800'" };
            yield return new object[] { new CodePrimitiveExpression((sbyte)1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression((ushort)1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression((uint)1), null, "1u" };
            yield return new object[] { new CodePrimitiveExpression((ulong)1), null, "1ul" };
            yield return new object[] { new CodePrimitiveExpression(null), null, "null" };
            yield return new object[] { new CodePrimitiveExpression(""), null, "\"\"" };
            yield return new object[] { new CodePrimitiveExpression("abc"), null, "\"abc\"" };
            yield return new object[] { new CodePrimitiveExpression("\uD800\uDC00"), null, "\"\uD800\uDC00\"" };
            yield return new object[] { new CodePrimitiveExpression("\r\t\"'\0\\\n\u2027\u2028\u2029\u2030\u0083\u0084\u0085"), null, "\"\\r\\t\\\"\\'\\0\\\\\\n\u2027\\u2028\\u2029\u2030\u0083\u0084\\u0085\"" };
            yield return new object[] { new CodePrimitiveExpression("\uDC00"), null, "\"\uDC00\"" };
            yield return new object[] { new CodePrimitiveExpression("\uD800"), null, "\"\uD800\"" };
            yield return new object[] { new CodePrimitiveExpression("01234567890123456789012345678901234567890123456789012345678901234567890123456789"), null, $"\"01234567890123456789012345678901234567890123456789012345678901234567890123456789\"" };
            yield return new object[] { new CodePrimitiveExpression("01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800"), null, $"\"01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800\" +{nl}    \"\"" };
            yield return new object[] { new CodePrimitiveExpression("01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800\uDC00"), null, $"\"01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800\uDC00\" +{nl}    \"\"" };
            yield return new object[] { new CodePrimitiveExpression("01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800a"), null, $"\"01234567890123456789012345678901234567890123456789012345678901234567890123456789\uD800\" +{nl}    \"a\"" };
            yield return new object[] { new CodePrimitiveExpression("012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789"), null, $"\"012345678901234567890123456789012345678901234567890123456789012345678901234567890\" +{nl}    \"123456789\"" };
            yield return new object[] { new CodePrimitiveExpression("012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789"), customOptions, $"\"012345678901234567890123456789012345678901234567890123456789012345678901234567890\" +{nl}$\"123456789\"" };
            yield return new object[] { new CodePrimitiveExpression(new string('a', 256)), null, $"@\"{new string('a', 256)}\"" };
            yield return new object[] { new CodePrimitiveExpression("\"" + new string('a', 254) + "\""), null, $"@\"\"\"{new string('a', 254)}\"\"\"" };
            yield return new object[] { new CodePrimitiveExpression("\"" + new string('a', 1498) + "\""), null, $"@\"\"\"{new string('a', 1498)}\"\"\"" };
            yield return new object[] { new CodePrimitiveExpression((byte)1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression((short)1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression(1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression((long)1), null, "1" };
            yield return new object[] { new CodePrimitiveExpression(float.NaN), null, "float.NaN" };
            yield return new object[] { new CodePrimitiveExpression(float.NegativeInfinity), null, "float.NegativeInfinity" };
            yield return new object[] { new CodePrimitiveExpression(float.PositiveInfinity), null, "float.PositiveInfinity" };
            yield return new object[] { new CodePrimitiveExpression(float.MaxValue), null, "3.4028235E+38F" };
            yield return new object[] { new CodePrimitiveExpression(double.NaN), null, "double.NaN" };
            yield return new object[] { new CodePrimitiveExpression(double.NegativeInfinity), null, "double.NegativeInfinity" };
            yield return new object[] { new CodePrimitiveExpression(double.PositiveInfinity), null, "double.PositiveInfinity" };
            yield return new object[] { new CodePrimitiveExpression(double.MaxValue), null, "1.7976931348623157E+308D" };
            yield return new object[] { new CodePrimitiveExpression(decimal.MaxValue), null, "79228162514264337593543950335m" };
            yield return new object[] { new CodePrimitiveExpression(true), null, "true" };
            yield return new object[] { new CodePrimitiveExpression(false), null, "false" };

            // CodePropertyReferenceExpression.
            yield return new object[] { new CodePropertyReferenceExpression(), null, "" };
            yield return new object[] { new CodePropertyReferenceExpression(null, string.Empty), null, "" };
            yield return new object[] { new CodePropertyReferenceExpression(null, "fieldName"), null, "fieldName" };
            yield return new object[] { new CodePropertyReferenceExpression(null, "as"), null, "@as" };
            yield return new object[] { new CodePropertyReferenceExpression(new CodePrimitiveExpression(1), string.Empty), null, "1." };
            yield return new object[] { new CodePropertyReferenceExpression(new CodePrimitiveExpression(1), "fieldName"), null, "1.fieldName" };
            yield return new object[] { new CodePropertyReferenceExpression(new CodePrimitiveExpression(1), "as"), null, "1.@as" };

            // CodePropertySetValueReferenceExpression.
            yield return new object[] { new CodePropertySetValueReferenceExpression(), null, "value" };

            // CodeThisReferenceExpression.
            yield return new object[] { new CodeThisReferenceExpression(), null, "this" };

            // GenerateTypeReferenceExpression.
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference()), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference((string)null)), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(string.Empty)), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type")), null, "type" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("as")), null, "@as" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  type  ")), null, "  type  " };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`")), null, "type<>" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`-1")), null, "type<>-1" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`0")), null, "type<>" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`1")), null, "type<>" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`1", new CodeTypeReference("type1"))), null, "type<type1>" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`2")), null, "type<, >" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`2", new CodeTypeReference("type1"))), null, "type<type1, >" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`2", new CodeTypeReference("type1"), new CodeTypeReference("type2"))), null, "type<type1, type2>" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`2", new CodeTypeReference("type1"), new CodeTypeReference("type2"), new CodeTypeReference("type3"))), null, "type<type1, type2>" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type+innerType")), null, "type.innerType" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type+as")), null, "type.@as" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("as+innerType")), null, "@as.innerType" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("as+is")), null, "@as.@is" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type.innerType")), null, "type.innerType" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type.as")), null, "type.@as" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("as.innerType")), null, "@as.innerType" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("as.is")), null, "@as.@is" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`1+innerType", new CodeTypeReference("type1"))), null, "type<type1>.innerType" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type`1.innerType", new CodeTypeReference("type1"))), null, "type<type1>.innerType" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference("type"), 0)), null, "type" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference("type"), -1)), null, "type" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference("type"), 1)), null, "type[]" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference("type"), 2)), null, "type[,]" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference("type", 1), 1)), null, "type[][]" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference((CodeTypeReference)null, 1)), null, "void[]" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference(new CodeTypeReference((CodeTypeReference)null, 1), 1)), null, "void[][]" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Byte")), null, "byte" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.byte")), null, "byte" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.byte  ")), null, "byte" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.UInt16")), null, "ushort" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.uint16")), null, "ushort" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.uint16  ")), null, "ushort" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.UInt32")), null, "uint" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.uint32")), null, "uint" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.uint32  ")), null, "uint" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.UInt64")), null, "ulong" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.uint64")), null, "ulong" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.uint64  ")), null, "ulong" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.SByte")), null, "sbyte" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.sbyte")), null, "sbyte" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.sbyte  ")), null, "sbyte" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Int16")), null, "short" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.int16")), null, "short" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.int16  ")), null, "short" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Int32")), null, "int" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.int32")), null, "int" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.int32  ")), null, "int" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Int64")), null, "long" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.int64")), null, "long" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.int64  ")), null, "long" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Boolean")), null, "bool" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.boolean")), null, "bool" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.boolean  ")), null, "bool" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Char")), null, "char" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.char")), null, "char" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.char  ")), null, "char" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Single")), null, "float" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.single")), null, "float" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.single  ")), null, "float" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Double")), null, "double" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.double")), null, "double" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.double  ")), null, "double" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Decimal")), null, "decimal" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.decimal")), null, "decimal" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.decimal  ")), null, "decimal" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.String")), null, "string" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.string")), null, "string" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.string  ")), null, "string" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Object")), null, "object" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.object")), null, "object" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.object  ")), null, "object" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("System.Void")), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.void")), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("  system.void  ")), null, "void" };

            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference() { Options = CodeTypeReferenceOptions.GlobalReference }), null, "void" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("system.int32") { Options = CodeTypeReferenceOptions.GlobalReference }), null, "int" };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("type") { Options = CodeTypeReferenceOptions.GlobalReference }), null, "global::type" };

            // CodeTypeOfExpression.
            yield return new object[] { new CodeTypeOfExpression(), null, "typeof(void)" };
            yield return new object[] { new CodeTypeOfExpression("type"), null, "typeof(type)" };
            yield return new object[] { new CodeTypeOfExpression("as"), null, "typeof(@as)" };

            // CodeDefaultValueExpression.
            yield return new object[] { new CodeDefaultValueExpression(), null, "default(void)" };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference("type")), null, "default(type)" };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference("as")), null, "default(@as)" };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromExpression_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework has different brace semantics.")]
        public void GenerateCodeFromExpression_Invoke_Success(CodeExpression e, CodeGeneratorOptions o, string expected)
        {
            ICodeGenerator generator = GetGenerator();
            var writer = new StringWriter();
            generator.GenerateCodeFromExpression(e, writer, o);
            AssertEqualLong(expected, writer.ToString());
        }

        public static IEnumerable<object[]> GenerateCodeFromExpression_NullE_TestData()
        {
            yield return new object[] { null };

            yield return new object[] { new CodeBinaryOperatorExpression() };
            yield return new object[] { new CodeBinaryOperatorExpression(null, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, null) };

            yield return new object[] { new CodeCastExpression() };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("name"), null) };

            yield return new object[] { new CodeDelegateCreateExpression() };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("type"), null, "methodName") };

            yield return new object[] { new CodeIndexerExpression() };
            yield return new object[] { new CodeIndexerExpression(null) };

            yield return new object[] { new CodeArrayIndexerExpression() };
            yield return new object[] { new CodeArrayIndexerExpression(null) };

            var invalidParameterDeclarationExpression = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name"), new CodeAttributeArgument()));
            yield return new object[] { invalidParameterDeclarationExpression };

            yield return new object[] { new CodeDirectionExpression() };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromExpression_NullE_TestData))]
        public void GenerateCodeFromExpression_NullE_ThrowsArgumentNullException(CodeExpression e)
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCodeFromExpression(e, new StringWriter(), new CodeGeneratorOptions()));
        }

        public static IEnumerable<object[]> GenerateCodeFromExpression_InvalidE_TestData()
        {
            yield return new object[] { new CodeExpression() };

            yield return new object[] { new CodeArrayCreateExpression("type", new CodeExpression[] { new CodeExpression() }) };
            yield return new object[] { new CodeArrayCreateExpression("type", new CodeExpression[] { new CodeExpression() }) { SizeExpression = new CodePrimitiveExpression(1) } };
            yield return new object[] { new CodeArrayCreateExpression("type", new CodeExpression[] { new CodeExpression() }) { Size = 1 } };
            yield return new object[] { new CodeArrayCreateExpression("type") { SizeExpression = new CodeExpression() } };

            yield return new object[] { new CodeBinaryOperatorExpression(new CodeExpression(), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodeExpression()) };

            yield return new object[] { new CodeCastExpression(new CodeTypeReference("name"), new CodeExpression()) };

            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("type"), new CodeExpression(), "methodName") };

            yield return new object[] { new CodeFieldReferenceExpression(new CodeExpression(), "fieldName") };

            yield return new object[] { new CodeIndexerExpression(new CodeExpression()) };
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            yield return new object[] { new CodeArrayIndexerExpression(new CodeExpression()) };
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeExpression(), "methodName")) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "methodName"), new CodeExpression()) };

            yield return new object[] { new CodeMethodReferenceExpression(new CodeExpression(), "methodName") };

            yield return new object[] { new CodeEventReferenceExpression(new CodeExpression(), "fieldName") };

            yield return new object[] { new CodeDelegateInvokeExpression(new CodeExpression()) };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("type"), new CodeExpression()) };

            var invalidParameterDeclarationExpression = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterDeclarationExpression.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name"), new CodeAttributeArgument(new CodeExpression())));
            yield return new object[] { invalidParameterDeclarationExpression };

            yield return new object[] { new CodeDirectionExpression(FieldDirection.In, new CodeExpression()) };

            yield return new object[] { new CodePrimitiveExpression(new DateTime()) };
            yield return new object[] { new CodePrimitiveExpression(new TimeSpan()) };
            yield return new object[] { new CodePrimitiveExpression(new object()) };

            yield return new object[] { new CodePropertyReferenceExpression(new CodeExpression(), "propertyName") };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromExpression_InvalidE_TestData))]
        public void GenerateCodeFromExpression_InvalidE_ThrowsArgumentException(CodeExpression e)
        {
            ICodeGenerator generator = GetGenerator();
            AssertExtensions.Throws<ArgumentException>("e", null, () => generator.GenerateCodeFromExpression(e, new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void GenerateCodeFromExpression_NullWriter_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeBaseReferenceExpression();
            Assert.Throws<ArgumentNullException>("writer", () => generator.GenerateCodeFromExpression(e, null, new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromExpression_InvokeAlreadyGenerating_ThrowsInvalidOperationException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeBaseReferenceExpression();
            var writer = new DelegatingWriter();
            int callCount = 0;
            writer.OnWrite = () =>
            {
                Assert.Throws<InvalidOperationException>(() => generator.GenerateCodeFromExpression(new CodeBaseReferenceExpression(), new StringWriter(), new CodeGeneratorOptions()));
                callCount++;
            };
            generator.GenerateCodeFromExpression(e, writer, new CodeGeneratorOptions());
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> GenerateCodeFromStatement_TestData()
        {
            string nl = Environment.NewLine;
            var customOptions = new CodeGeneratorOptions
            {
                IndentString = "$",
                ElseOnClosing = true,
                BracingStyle = "C"
            };

            // CodeCommentStatement
            yield return new object[] { new CodeCommentStatement(string.Empty), null, $"// {nl}" };
            yield return new object[] { new CodeCommentStatement(string.Empty, docComment: true), null, $"/// {nl}" };

            yield return new object[] { new CodeCommentStatement("text"), null, $"// text{nl}" };
            yield return new object[] { new CodeCommentStatement("text", docComment: true), null, $"/// text{nl}" };
            yield return new object[] { new CodeCommentStatement("text\0more"), null, $"// textmore{nl}" };
            yield return new object[] { new CodeCommentStatement("text\n"), null, $"// text\n//{nl}" };
            yield return new object[] { new CodeCommentStatement("text\nmore"), null, $"// text\n//more{nl}" };
            yield return new object[] { new CodeCommentStatement("text\r"), null, $"// text\r//{nl}" };
            yield return new object[] { new CodeCommentStatement("text\rmore"), null, $"// text\r//more{nl}" };
            yield return new object[] { new CodeCommentStatement("text\r\nmore"), null, $"// text\r\n//more{nl}" };
            yield return new object[] { new CodeCommentStatement("text\u2027\u2028more\u2029\u2030more\u0083\u0084\u0085"), null, $"// text\u2027\u2028//more\u2029//\u2030more\u0083\u0084\u0085//{nl}" };

            // CodeMethodReturnStatement.
            yield return new object[] { new CodeMethodReturnStatement(), null, $"return;{nl}" };
            yield return new object[] { new CodeMethodReturnStatement(new CodePrimitiveExpression(1)), null, $"return 1;{nl}" };

            // GenerateConditionStatement.
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression(1)), null, $"if (1) {{{nl}}}{nl}" };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)) }
                ), null, $"if (1) {{{nl}    2;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)), new CodeExpressionStatement(new CodePrimitiveExpression(3)) }
                ), null, $"if (1) {{{nl}    2;{nl}    3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[0],
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)) }
                ), null, $"if (1) {{{nl}}}{nl}else {{{nl}    2;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)), new CodeExpressionStatement(new CodePrimitiveExpression(3)) },
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(4)), new CodeExpressionStatement(new CodePrimitiveExpression(5)) }
                ), null, $"if (1) {{{nl}    2;{nl}    3;{nl}}}{nl}else {{{nl}    4;{nl}    5;{nl}}}{nl}"
            };

            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression(1)), null, $"if (1) {{{nl}}}{nl}" };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)) }
                ), customOptions, $"if (1){nl}{{{nl}$2;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)), new CodeExpressionStatement(new CodePrimitiveExpression(3)) }
                ), customOptions, $"if (1){nl}{{{nl}$2;{nl}$3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeStatement[0],
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)) }
                ), customOptions, $"if (1){nl}{{{nl}}} else{nl}{{{nl}$2;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(new CodePrimitiveExpression(1),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(2)), new CodeExpressionStatement(new CodePrimitiveExpression(3)) },
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(4)), new CodeExpressionStatement(new CodePrimitiveExpression(5)) }
                ), customOptions, $"if (1){nl}{{{nl}$2;{nl}$3;{nl}}} else{nl}{{{nl}$4;{nl}$5;{nl}}}{nl}"
            };

            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeExpressionStatement(new CodePrimitiveExpression(new string('a', 82)))
                ), null, $"if (1) {{{nl}    \"{new string('a', 81)}\" +{nl}        \"a\";{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeExpressionStatement(new CodePrimitiveExpression(new string('a', 82)))
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        \"{new string('a', 81)}\" +{nl}            \"a\";{nl}    }}{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeConditionStatement(
                            new CodePrimitiveExpression(3),
                            new CodeExpressionStatement(new CodePrimitiveExpression(new string('a', 82)))
                        )
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        if (3) {{{nl}            \"{new string('a', 81)}\" +{nl}                \"a\";{nl}        }}{nl}    }}{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeConditionStatement(
                            new CodePrimitiveExpression(3),
                            new CodeConditionStatement(
                                new CodePrimitiveExpression(4),
                                new CodeExpressionStatement(new CodePrimitiveExpression(new string('a', 82)))
                            )
                        )
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        if (3) {{{nl}            if (4) {{{nl}                \"{new string('a', 81)}\" +{nl}                    \"a\";{nl}            }}{nl}        }}{nl}    }}{nl}}}{nl}"
            };

            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeCommentStatement("text\rmore")
                ), null, $"if (1) {{{nl}    // text\r    //more{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeCommentStatement("text\rmore")
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        // text\r        //more{nl}    }}{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeConditionStatement(
                            new CodePrimitiveExpression(3),
                            new CodeCommentStatement("text\rmore")
                        )
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        if (3) {{{nl}            // text\r            //more{nl}        }}{nl}    }}{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeConditionStatement(
                    new CodePrimitiveExpression(1),
                    new CodeConditionStatement(
                        new CodePrimitiveExpression(2),
                        new CodeConditionStatement(
                            new CodePrimitiveExpression(3),
                            new CodeConditionStatement(
                                new CodePrimitiveExpression(4),
                                new CodeCommentStatement("text\rmore")
                            )
                        )
                    )
                ), null, $"if (1) {{{nl}    if (2) {{{nl}        if (3) {{{nl}            if (4) {{{nl}                // text\r                //more{nl}            }}{nl}        }}{nl}    }}{nl}}}{nl}"
            };

            // CodeTryCatchFinallyStatement.
            yield return new object[] { new CodeTryCatchFinallyStatement(), null, $"try {{{nl}}}{nl}" };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)) },
                    new CodeCatchClause[0],
                    new CodeStatement[0]
                ), null, $"try {{{nl}    1;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[0],
                    new CodeCatchClause[] { new CodeCatchClause(), new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeExpressionStatement(new CodePrimitiveExpression(1))), new CodeCatchClause("as", new CodeTypeReference("is")) },
                    new CodeStatement[0]
                ), null, $"try {{{nl}}}{nl}catch (System.Exception ) {{{nl}}}{nl}catch (typeName name) {{{nl}    1;{nl}}}{nl}catch (@is @as) {{{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[0],
                    new CodeCatchClause[0],
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)) }
                ), null, $"try {{{nl}}}{nl}finally {{{nl}    1;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)), new CodeExpressionStatement(new CodePrimitiveExpression(2)) },
                    new CodeCatchClause[] { new CodeCatchClause(), new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeExpressionStatement(new CodePrimitiveExpression(3)), new CodeExpressionStatement(new CodePrimitiveExpression(4))), new CodeCatchClause("as", new CodeTypeReference("is")) },
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(5)), new CodeExpressionStatement(new CodePrimitiveExpression(6)) }
                ), null, $"try {{{nl}    1;{nl}    2;{nl}}}{nl}catch (System.Exception ) {{{nl}}}{nl}catch (typeName name) {{{nl}    3;{nl}    4;{nl}}}{nl}catch (@is @as) {{{nl}}}{nl}finally {{{nl}    5;{nl}    6;{nl}}}{nl}"
            };

            yield return new object[] { new CodeTryCatchFinallyStatement(), customOptions, $"try{nl}{{{nl}}}{nl}" };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)) },
                    new CodeCatchClause[0],
                    new CodeStatement[0]
                ), customOptions, $"try{nl}{{{nl}$1;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[0],
                    new CodeCatchClause[] { new CodeCatchClause(), new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeExpressionStatement(new CodePrimitiveExpression(1))), new CodeCatchClause("as", new CodeTypeReference("is")) },
                    new CodeStatement[0]
                ), customOptions, $"try{nl}{{{nl}}} catch (System.Exception ){nl}{{{nl}}} catch (typeName name){nl}{{{nl}$1;{nl}}} catch (@is @as){nl}{{{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[0],
                    new CodeCatchClause[0],
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)) }
                ), customOptions, $"try{nl}{{{nl}}} finally{nl}{{{nl}$1;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)), new CodeExpressionStatement(new CodePrimitiveExpression(2)) },
                    new CodeCatchClause[] { new CodeCatchClause(), new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeExpressionStatement(new CodePrimitiveExpression(3)), new CodeExpressionStatement(new CodePrimitiveExpression(4))), new CodeCatchClause("as", new CodeTypeReference("is")) },
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(5)), new CodeExpressionStatement(new CodePrimitiveExpression(6)) }
                ), customOptions, $"try{nl}{{{nl}$1;{nl}$2;{nl}}} catch (System.Exception ){nl}{{{nl}}} catch (typeName name){nl}{{{nl}$3;{nl}$4;{nl}}} catch (@is @as){nl}{{{nl}}} finally{nl}{{{nl}$5;{nl}$6;{nl}}}{nl}"
            };

            // CodeAssignStatement.
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)), null, $"1 = 2;{nl}" };

            // CodeExpressionStatement.
            yield return new object[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)), null, $"1;{nl}" };

            // CodeIterationStatement.
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeGotoStatement("label1"),
                    new CodePrimitiveExpression(1),
                    new CodeGotoStatement("label2"),
                    new CodeStatement[0]
                ), null, $"for (goto label1;{nl}; 1; goto label2;{nl}) {{{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeGotoStatement("label1"),
                    new CodePrimitiveExpression(1),
                    new CodeGotoStatement("label2"),
                    new CodeStatement[] { new CodeGotoStatement("label3"), new CodeGotoStatement("label4") }
                ), null, $"for (goto label1;{nl}; 1; goto label2;{nl}) {{{nl}    goto label3;{nl}    goto label4;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeAssignStatement(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)),
                    new CodePrimitiveExpression(3),
                    new CodeAssignStatement(new CodePrimitiveExpression(4), new CodePrimitiveExpression(5)),
                    new CodeStatement[] { new CodeAssignStatement(new CodePrimitiveExpression(6), new CodePrimitiveExpression(7)) }
                ), null, $"for (1 = 2; 3; 4 = 5) {{{nl}    6 = 7;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeExpressionStatement(new CodePrimitiveExpression(1)),
                    new CodePrimitiveExpression(2),
                    new CodeExpressionStatement(new CodePrimitiveExpression(3)),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(4)) }
                ), null, $"for (1; 2; 3) {{{nl}    4;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeVariableDeclarationStatement("type1", "name1"),
                    new CodePrimitiveExpression(1),
                    new CodeVariableDeclarationStatement("type2", "name2"),
                    new CodeStatement[] { new CodeVariableDeclarationStatement("type3", "name3") }
                ), null, $"for (type1 name1; 1; type2 name2) {{{nl}    type3 name3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeVariableDeclarationStatement("type1", "name1", new CodePrimitiveExpression(1)),
                    new CodePrimitiveExpression(2),
                    new CodeVariableDeclarationStatement("type2", "name2"),
                    new CodeStatement[] { new CodeVariableDeclarationStatement("type3", "name3") }
                ), null, $"for (type1 name1 = 1; 2; type2 name2) {{{nl}    type3 name3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeSnippetStatement("value1"),
                    new CodePrimitiveExpression(1),
                    new CodeSnippetStatement("value2"),
                    new CodeStatement[] { new CodeSnippetStatement("value3") }
                ), null, $"for (value1{nl}; 1; value2{nl}) {{{nl}value3{nl}}}{nl}"
            };

            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeGotoStatement("label1"),
                    new CodePrimitiveExpression(1),
                    new CodeGotoStatement("label2"),
                    new CodeStatement[0]
                ), customOptions, $"for (goto label1;{nl}; 1; goto label2;{nl}){nl}{{{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeGotoStatement("label1"),
                    new CodePrimitiveExpression(1),
                    new CodeGotoStatement("label2"),
                    new CodeStatement[] { new CodeGotoStatement("label3"), new CodeGotoStatement("label4") }
                ), customOptions, $"for (goto label1;{nl}; 1; goto label2;{nl}){nl}{{{nl}$goto label3;{nl}$goto label4;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeAssignStatement(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)),
                    new CodePrimitiveExpression(3),
                    new CodeAssignStatement(new CodePrimitiveExpression(4), new CodePrimitiveExpression(5)),
                    new CodeStatement[] { new CodeAssignStatement(new CodePrimitiveExpression(6), new CodePrimitiveExpression(7)) }
                ), customOptions, $"for (1 = 2; 3; 4 = 5){nl}{{{nl}$6 = 7;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeExpressionStatement(new CodePrimitiveExpression(1)),
                    new CodePrimitiveExpression(2),
                    new CodeExpressionStatement(new CodePrimitiveExpression(3)),
                    new CodeStatement[] { new CodeExpressionStatement(new CodePrimitiveExpression(4)) }
                ), customOptions, $"for (1; 2; 3){nl}{{{nl}$4;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeVariableDeclarationStatement("type1", "name1"),
                    new CodePrimitiveExpression(1),
                    new CodeVariableDeclarationStatement("type2", "name2"),
                    new CodeStatement[] { new CodeVariableDeclarationStatement("type3", "name3") }
                ), customOptions, $"for (type1 name1; 1; type2 name2){nl}{{{nl}$type3 name3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeVariableDeclarationStatement("type1", "name1", new CodePrimitiveExpression(1)),
                    new CodePrimitiveExpression(2),
                    new CodeVariableDeclarationStatement("type2", "name2"),
                    new CodeStatement[] { new CodeVariableDeclarationStatement("type3", "name3") }
                ), customOptions, $"for (type1 name1 = 1; 2; type2 name2){nl}{{{nl}$type3 name3;{nl}}}{nl}"
            };
            yield return new object[]
            {
                new CodeIterationStatement(
                    new CodeSnippetStatement("value1"),
                    new CodePrimitiveExpression(1),
                    new CodeSnippetStatement("value2"),
                    new CodeStatement[] { new CodeSnippetStatement("value3") }
                ), customOptions, $"for (value1{nl}; 1; value2{nl}){nl}{{{nl}value3{nl}}}{nl}"
            };

            // CodeThrowExceptionStatement
            yield return new object[] { new CodeThrowExceptionStatement(), null, $"throw;{nl}" };
            yield return new object[] { new CodeThrowExceptionStatement(new CodePrimitiveExpression(1)), null, $"throw 1;{nl}" };

            // CodeSnippetStatement.
            yield return new object[] { new CodeSnippetStatement(), null, $"{nl}" };
            yield return new object[] { new CodeSnippetStatement(string.Empty), null, $"{nl}" };
            yield return new object[] { new CodeSnippetStatement("value"), null, $"value{nl}" };

            // CodeVariableDeclarationStatement.
            yield return new object[] { new CodeVariableDeclarationStatement(), null, $"void ;{nl}" };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("type"), "name"), null, $"type name;{nl}" };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("type"), "name", new CodePrimitiveExpression(1)), null, $"type name = 1;{nl}" };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("as"), "is"), null, $"@as @is;{nl}" };

            // CodeAttachEventStatement.
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(), new CodePrimitiveExpression(1)), null, $" += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, string.Empty), new CodePrimitiveExpression(1)), null, $" += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "eventName"), new CodePrimitiveExpression(1)), null, $"eventName += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "as"), new CodePrimitiveExpression(1)), null, $"@as += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), string.Empty), new CodePrimitiveExpression(1)), null, $"1. += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "eventName"), new CodePrimitiveExpression(1)), null, $"1.eventName += 1;{nl}" };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "as"), new CodePrimitiveExpression(1)), null, $"1.@as += 1;{nl}" };

            // CodeRemoveEventStatement.
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(), new CodePrimitiveExpression(1)), null, $" -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, string.Empty), new CodePrimitiveExpression(1)), null, $" -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "eventName"), new CodePrimitiveExpression(1)), null, $"eventName -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "as"), new CodePrimitiveExpression(1)), null, $"@as -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), string.Empty), new CodePrimitiveExpression(1)), null, $"1. -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "eventName"), new CodePrimitiveExpression(1)), null, $"1.eventName -= 1;{nl}" };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "as"), new CodePrimitiveExpression(1)), null, $"1.@as -= 1;{nl}" };

            // CodeGotoStatement.
            yield return new object[] { new CodeGotoStatement(), null, $"goto ;{nl}" };
            yield return new object[] { new CodeGotoStatement("label"), null, $"goto label;{nl}" };
            yield return new object[] { new CodeGotoStatement("as"), null, $"goto as;{nl}" };

            // CodeLabeledStatement.
            yield return new object[] { new CodeLabeledStatement(), null, $":{nl}" };
            yield return new object[] { new CodeLabeledStatement("label"), null, $"label:{nl}" };
            yield return new object[] { new CodeLabeledStatement("as"), null, $"as:{nl}" };
            yield return new object[] { new CodeLabeledStatement("label", new CodeExpressionStatement(new CodePrimitiveExpression(1))), null, $"label:{nl}    1;{nl}" };
            yield return new object[] { new CodeLabeledStatement(), customOptions, $":{nl}" };
            yield return new object[] { new CodeLabeledStatement("label"), customOptions, $"label:{nl}" };
            yield return new object[] { new CodeLabeledStatement("as"), customOptions, $"as:{nl}" };
            yield return new object[] { new CodeLabeledStatement("label", new CodeExpressionStatement(new CodePrimitiveExpression(1))), customOptions, $"label:{nl}$1;{nl}" };

            // Custom.
            yield return new object[] { new CodeExpressionStatement(new CodePrimitiveExpression(1)) { LinePragma = new CodeLinePragma() }, null, $"{nl}#line 0 \"\"{nl}1;{nl}{nl}#line default{nl}#line hidden{nl}" };

            var fullStatement = new CodeExpressionStatement(new CodePrimitiveExpression(1));
            fullStatement.StartDirectives.Add(new CodeDirective());
            fullStatement.StartDirectives.Add(new CodeChecksumPragma());
            fullStatement.StartDirectives.Add(new CodeChecksumPragma("startFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[0]));
            fullStatement.StartDirectives.Add(new CodeChecksumPragma("startFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[] { 1, 2, 3 }));
            fullStatement.StartDirectives.Add(new CodeRegionDirective());
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, string.Empty));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, "startText"));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, string.Empty));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "startText"));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "startText"));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, string.Empty));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, "startText"));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, string.Empty));
            fullStatement.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, "startText"));
            fullStatement.LinePragma = new CodeLinePragma("fileName", 1);
            fullStatement.EndDirectives.Add(new CodeDirective());
            fullStatement.EndDirectives.Add(new CodeChecksumPragma());
            fullStatement.EndDirectives.Add(new CodeChecksumPragma("endFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[0]));
            fullStatement.EndDirectives.Add(new CodeChecksumPragma("endFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[] { 1, 2, 3 }));
            fullStatement.EndDirectives.Add(new CodeRegionDirective());
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, string.Empty));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, "endText"));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, string.Empty));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.Start, "endText"));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End, "endText"));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, string.Empty));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, "endText"));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, string.Empty));
            fullStatement.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, "endText"));
            yield return new object[]
            {
                fullStatement,
                null,
                $"#pragma checksum \"\" \"{{00000000-0000-0000-0000-000000000000}}\" \"\"{nl}#pragma checksum \"startFileName\" \"{{00000001-0002-0003-0405-060708090a0b}}\" \"\"{nl}#pragma checksum \"startFileName\" \"{{00000001-0002-0003-0405-060708090a0b}}\" \"010203\"{nl}#region {nl}#region startText{nl}#endregion{nl}#endregion{nl}#region {nl}#region startText{nl}" +
                $"{nl}#line 1 \"fileName\"{nl}" +
                $"1;{nl}" +
                $"{nl}#line default{nl}#line hidden{nl}" +
                $"#pragma checksum \"\" \"{{00000000-0000-0000-0000-000000000000}}\" \"\"{nl}#pragma checksum \"endFileName\" \"{{00000001-0002-0003-0405-060708090a0b}}\" \"\"{nl}#pragma checksum \"endFileName\" \"{{00000001-0002-0003-0405-060708090a0b}}\" \"010203\"{nl}#region {nl}#region endText{nl}#endregion{nl}#endregion{nl}#region {nl}#region endText{nl}"
            };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromStatement_TestData))]
        public void GenerateCodeFromStatement_Invoke_Success(CodeStatement e, CodeGeneratorOptions o, string expected)
        {
            ICodeGenerator generator = GetGenerator();
            var writer = new StringWriter();
            generator.GenerateCodeFromStatement(e, writer, o);
            Assert.Equal(expected, writer.ToString());
        }

        public static IEnumerable<object[]> GenerateCodeFromStatement_NullE_TestData()
        {
            yield return new object[] { null };

            yield return new object[] { new CodeConditionStatement() };
            yield return new object[] { new CodeConditionStatement(null) };

            yield return new object[] { new CodeAssignStatement() };
            yield return new object[] { new CodeAssignStatement(null, new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), null) };

            yield return new object[] { new CodeIterationStatement() };
            yield return new object[] { new CodeIterationStatement(null, new CodePrimitiveExpression(1), new CodeGotoStatement("label")) };
            yield return new object[] { new CodeIterationStatement(new CodeGotoStatement("label"), null, new CodeGotoStatement("label")) };
            yield return new object[] { new CodeIterationStatement(new CodeGotoStatement("label"), new CodePrimitiveExpression(1), null) };

            yield return new object[] { new CodeAttachEventStatement() };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "eventName"), null) };

            yield return new object[] { new CodeRemoveEventStatement() };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "eventName"), null) };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromStatement_NullE_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void GenerateCodeFromStatement_NullE_ThrowsArgumentNullException(CodeStatement e)
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCodeFromStatement(e, new StringWriter(), new CodeGeneratorOptions()));
        }

        public static IEnumerable<object[]> GenerateCodeFromStatement_InvalidE_TestData()
        {
            yield return new object[] { new CodeStatement() };

            yield return new object[] { new CodeCommentStatement() };
            yield return new object[] { new CodeCommentStatement((CodeComment)null) };

            yield return new object[] { new CodeMethodReturnStatement(new CodeExpression()) };

            yield return new object[] { new CodeConditionStatement(new CodeExpression()) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression(1), new CodeStatement[] { new CodeStatement() }, new CodeStatement[] { new CodeGotoStatement("label") }) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression(1), new CodeStatement[] { new CodeGotoStatement("label") }, new CodeStatement[] { new CodeStatement() }) };

            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeGotoStatement("label")) },
                    new CodeStatement[] { new CodeGotoStatement("label") }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeGotoStatement("label") },
                    new CodeCatchClause[] { new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeStatement()) },
                    new CodeStatement[] { new CodeGotoStatement("label") }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeGotoStatement("label") },
                    new CodeCatchClause[] { new CodeCatchClause("name", new CodeTypeReference("typeName"), new CodeGotoStatement("label")) },
                    new CodeStatement[] { new CodeStatement() }
                )
            };

            yield return new object[] { new CodeAssignStatement(new CodeExpression(), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), new CodeExpression()) };

            yield return new object[] { new CodeIterationStatement(new CodeStatement(), new CodePrimitiveExpression(1), new CodeGotoStatement("label"), new CodeGotoStatement("label")) };
            yield return new object[] { new CodeIterationStatement(new CodeGotoStatement("label"), new CodeExpression(), new CodeGotoStatement("label"), new CodeGotoStatement("label")) };
            yield return new object[] { new CodeIterationStatement(new CodeGotoStatement("label"), new CodePrimitiveExpression(1), new CodeStatement(), new CodeGotoStatement("label")) };
            yield return new object[] { new CodeIterationStatement(new CodeGotoStatement("label"), new CodePrimitiveExpression(1), new CodeGotoStatement("label"), new CodeStatement()) };

            yield return new object[] { new CodeThrowExceptionStatement(new CodeExpression()) };

            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("type"), "name", new CodeExpression()) };

            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "eventName"), new CodeExpression()) };

            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "eventName"), new CodeExpression()) };

            yield return new object[] { new CodeLabeledStatement("label", new CodeStatement()) };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromStatement_InvalidE_TestData))]
        public void GenerateCodeFromStatement_InvalidE_ThrowsArgumentException(CodeStatement e)
        {
            ICodeGenerator generator = GetGenerator();
            AssertExtensions.Throws<ArgumentException>("e", () => generator.GenerateCodeFromStatement(e, new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void GenerateCodeFromStatement_NullWriter_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeExpressionStatement(new CodeBaseReferenceExpression());
            Assert.Throws<ArgumentNullException>("writer", () => generator.GenerateCodeFromStatement(e, null, new CodeGeneratorOptions()));
        }

        [Fact]
        public void GenerateCodeFromStatement_InvokeAlreadyGenerating_ThrowsInvalidOperationException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeExpressionStatement(new CodeBaseReferenceExpression());
            var writer = new DelegatingWriter();
            int callCount = 0;
            writer.OnWrite = () =>
            {
                Assert.Throws<InvalidOperationException>(() => generator.GenerateCodeFromStatement(new CodeExpressionStatement(new CodeBaseReferenceExpression()), new StringWriter(), new CodeGeneratorOptions()));
                callCount++;
            };
            generator.GenerateCodeFromStatement(e, writer, new CodeGeneratorOptions());
            Assert.Equal(1, callCount);
        }

        public static IEnumerable<object[]> GenerateCodeFromType_TestData()
        {
            string nl = Environment.NewLine;
            var customOptions = new CodeGeneratorOptions
            {
                IndentString = "$",
                ElseOnClosing = true,
                BracingStyle = "C",
                BlankLinesBetweenMembers = false,
                VerbatimOrder = true
            };

            // Support - CodeAttributeDeclarationCollection.
            var attributes = new CodeAttributeDeclarationCollection();
            attributes.Add(new CodeAttributeDeclaration());
            attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name1")));
            attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name2"), new CodeAttributeArgument(new CodePrimitiveExpression(1))));
            attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("name3"), new CodeAttributeArgument("arg1", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));
            attributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("is"), new CodeAttributeArgument("as", new CodePrimitiveExpression(1)), new CodeAttributeArgument("arg2", new CodePrimitiveExpression(2))));

            // Support - CodeParameterDeclarationExpressionCollection.
            var parameters = new CodeParameterDeclarationExpressionCollection();
            parameters.Add(new CodeParameterDeclarationExpression());
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.In });
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Out });
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Ref });
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.In - 1 });
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") { Direction = FieldDirection.Ref + 1 });
            parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("as"), "is"));

            var singleParameterAttribute = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            singleParameterAttribute.CustomAttributes.Add(new CodeAttributeDeclaration("type"));
            parameters.Add(singleParameterAttribute);

            var fullParameter = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            fullParameter.CustomAttributes.AddRange(attributes);
            parameters.Add(fullParameter);

            foreach (string paramsName in new string[] { "System.ParamArrayAttribute", "system.paramsarrayattribute" })
            {
                var paramsParameter = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
                paramsParameter.CustomAttributes.Add(new CodeAttributeDeclaration(new CodeTypeReference("System.ParamArrayAttribute"), new CodeAttributeArgument("arg1", new CodePrimitiveExpression(1))));
                parameters.Add(paramsParameter);
            }

            var manyParameters1 = new CodeParameterDeclarationExpressionCollection();
            for (int i = 0; i < 15; i++)
            {
                manyParameters1.Add(new CodeParameterDeclarationExpression($"type{i}", $"name{i}"));
            }

            var manyParameters2 = new CodeParameterDeclarationExpressionCollection();
            for (int i = 0; i < 16; i++)
            {
                manyParameters2.Add(new CodeParameterDeclarationExpression($"type{i}", $"name{i}"));
            }

            // Support - CodeTypeParameterCollection.
            var typeParameters = new CodeTypeParameterCollection();
            typeParameters.Add(new CodeTypeParameter());
            typeParameters.Add(new CodeTypeParameter("name"));

            var singleTypeParameterAttribute = new CodeTypeParameter("name");
            singleTypeParameterAttribute.CustomAttributes.Add(new CodeAttributeDeclaration("attribute"));
            typeParameters.Add(singleTypeParameterAttribute);

            typeParameters.Add(new CodeTypeParameter("name") { HasConstructorConstraint = true });

            var singleTypeParameterConstraint = new CodeTypeParameter("name");
            singleTypeParameterConstraint.Constraints.Add(new CodeTypeReference("constraint"));
            typeParameters.Add(singleTypeParameterConstraint);

            var fullTypeParameter = new CodeTypeParameter("name") { HasConstructorConstraint = true };
            fullTypeParameter.CustomAttributes.AddRange(attributes);
            fullTypeParameter.Constraints.Add(new CodeTypeReference());
            fullTypeParameter.Constraints.Add(new CodeTypeReference("constraint"));
            fullTypeParameter.Constraints.Add(new CodeTypeReference("as"));
            typeParameters.Add(fullTypeParameter);

            // Support - CodeDirectiveCollection.
            var directives = new CodeDirectiveCollection();
            directives.Add(new CodeDirective());
            directives.Add(new CodeChecksumPragma());
            directives.Add(new CodeChecksumPragma("startFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[0]));
            directives.Add(new CodeChecksumPragma("startFileName", new Guid(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11), new byte[] { 1, 2, 3 }));
            directives.Add(new CodeRegionDirective());
            directives.Add(new CodeRegionDirective(CodeRegionMode.None, string.Empty));
            directives.Add(new CodeRegionDirective(CodeRegionMode.None, "startText"));
            directives.Add(new CodeRegionDirective(CodeRegionMode.Start, string.Empty));
            directives.Add(new CodeRegionDirective(CodeRegionMode.Start, "startText"));
            directives.Add(new CodeRegionDirective(CodeRegionMode.End, string.Empty));
            directives.Add(new CodeRegionDirective(CodeRegionMode.End, "startText"));
            directives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, string.Empty));
            directives.Add(new CodeRegionDirective(CodeRegionMode.None - 1, "startText"));
            directives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, string.Empty));
            directives.Add(new CodeRegionDirective(CodeRegionMode.End - 1, "startText"));

            // Support - CodeCommentStatementCollection.
            var comments = new CodeCommentStatementCollection();
            comments.Add(new CodeCommentStatement(string.Empty));
            comments.Add(new CodeCommentStatement("comment1"));
            comments.Add(new CodeCommentStatement("comment2", docComment: true));

            // CodeTypeDeclaration.
            yield return new object[] { new CodeTypeDeclaration(), null, $"public class  {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name"), null, $"public class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("as"), null, $"public class @as {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name"), customOptions, $"public class name{nl}{{{nl}}}{nl}" };

            CodeTypeDeclaration CreateType(CodeTypeMember member)
            {
                var t = new CodeTypeDeclaration("type");
                t.Members.Add(member);
                return t;
            };

            // CodeMemberField.
            yield return new object[] { CreateType(new CodeMemberField()), null, $"public class type {{{nl}    {nl}    private void ;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field")), null, $"public class type {{{nl}    {nl}    private fieldType field;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field") { InitExpression = new CodePrimitiveExpression(1) }), null, $"public class type {{{nl}    {nl}    private fieldType field = 1;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field") { LinePragma = new CodeLinePragma() }), null, $"public class type {{{nl}    {nl}    {nl}    #line 0 \"\"{nl}    private fieldType field;{nl}    {nl}    #line default{nl}    #line hidden{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField()), customOptions, $"public class type{nl}{{{nl}$private void ;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field")), customOptions, $"public class type{nl}{{{nl}$private fieldType field;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field") { InitExpression = new CodePrimitiveExpression(1) }), customOptions, $"public class type{nl}{{{nl}$private fieldType field = 1;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField("fieldType", "field") { LinePragma = new CodeLinePragma() }), customOptions, $"public class type{nl}{{{nl}${nl}$#line 0 \"\"{nl}$private fieldType field;{nl}${nl}$#line default{nl}$#line hidden{nl}}}{nl}" };

            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Abstract }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Final }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Static }), null, $"public class type {{{nl}    {nl}    static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Override }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Const }), null, $"public class type {{{nl}    {nl}    const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.ScopeMask }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New }), null, $"public class type {{{nl}    {nl}    new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.VTableMask }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Overloaded }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.AccessMask }), null, $"public class type {{{nl}    {nl}    int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new const int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new static int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new int name;{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberField(typeof(int), "name") { Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new const int name;{nl}}}{nl}" };

            var fullField = new CodeMemberField("fieldType", "fieldName");
            fullField.StartDirectives.AddRange(directives);
            fullField.Comments.AddRange(comments);
            fullField.CustomAttributes.AddRange(attributes);
            fullField.LinePragma = new CodeLinePragma("fileName", 1);
            fullField.InitExpression = new CodePrimitiveExpression(1);
            fullField.EndDirectives.AddRange(directives);
            yield return new object[]
            {
                CreateType(fullField),
                null,
$@"public class type {{
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
    [()]
    [name1()]
    [name2(1)]
    [name3(arg1=1, arg2=2)]
    [@is(@as=1, arg2=2)]
    private fieldType fieldName = 1;
    
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
}}
"
            };
            yield return new object[]
            {
                CreateType(fullField),
                customOptions,
$@"public class type
{{
$#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
$#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
$#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
$#region 
$#region startText
$#endregion
$#endregion
$#region 
$#region startText
$// 
$// comment1
$/// comment2
$
$#line 1 ""fileName""
$[()]
$[name1()]
$[name2(1)]
$[name3(arg1=1, arg2=2)]
$[@is(@as=1, arg2=2)]
$private fieldType fieldName = 1;
$
$#line default
$#line hidden
$#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
$#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
$#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
$#region 
$#region startText
$#endregion
$#endregion
$#region 
$#region startText
}}
"
            };

            // CodeSnippetTypeMember.
            yield return new object[] { CreateType(new CodeSnippetTypeMember()), null, $"public class type {{{nl}    {nl}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeSnippetTypeMember("text")), null, $"public class type {{{nl}    {nl}text{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeSnippetTypeMember("text") { LinePragma = new CodeLinePragma() }), null, $"public class type {{{nl}    {nl}    {nl}    #line 0 \"\"{nl}text{nl}    #line default{nl}    #line hidden{nl}    {nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeSnippetTypeMember()), customOptions, $"public class type{nl}{{{nl}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeSnippetTypeMember("text")), customOptions, $"public class type{nl}{{{nl}text{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeSnippetTypeMember("text") { LinePragma = new CodeLinePragma() }), customOptions, $"public class type{nl}{{{nl}${nl}$#line 0 \"\"{nl}text{nl}${nl}$#line default{nl}$#line hidden{nl}}}{nl}" };

            var fullSnippet = new CodeSnippetTypeMember("text");
            fullSnippet.StartDirectives.AddRange(directives);
            fullSnippet.Comments.AddRange(comments);
            fullSnippet.CustomAttributes.AddRange(attributes);
            fullSnippet.LinePragma = new CodeLinePragma("fileName", 1);
            fullSnippet.EndDirectives.AddRange(directives);
            yield return new object[]
            {
                CreateType(fullSnippet),
                null,
$@"public class type {{
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
text
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    
}}
"
            };

            // CodeMemberMethod.
            yield return new object[] { CreateType(new CodeMemberMethod()), null, $"public class type {{{nl}    {nl}    private void () {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" }), null, $"public class type {{{nl}    {nl}    private returnType name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", PrivateImplementationType = new CodeTypeReference("privateType") }), null, $"public class type {{{nl}    {nl}    returnType privateType.name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", LinePragma = new CodeLinePragma() }), null, $"public class type {{{nl}    {nl}    {nl}    #line 0 \"\"{nl}    private returnType name() {{{nl}    }}{nl}    {nl}    #line default{nl}    #line hidden{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is" }), null, $"public class type {{{nl}    {nl}    private @as @is() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is", PrivateImplementationType = new CodeTypeReference("base") }), null, $"public class type {{{nl}    {nl}    @as @base.@is() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod()), customOptions, $"public class type{nl}{{{nl}$private void (){nl}${{{nl}$}}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" }), customOptions, $"public class type{nl}{{{nl}$private returnType name(){nl}${{{nl}$}}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", PrivateImplementationType = new CodeTypeReference("privateType") }), customOptions, $"public class type{nl}{{{nl}$returnType privateType.name(){nl}${{{nl}$}}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", LinePragma = new CodeLinePragma() }), customOptions, $"public class type{nl}{{{nl}${nl}$#line 0 \"\"{nl}$private returnType name(){nl}${{{nl}$}}{nl}${nl}$#line default{nl}$#line hidden{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is" }), customOptions, $"public class type{nl}{{{nl}$private @as @is(){nl}${{{nl}$}}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is", PrivateImplementationType = new CodeTypeReference("base") }), customOptions, $"public class type{nl}{{{nl}$@as @base.@is(){nl}${{{nl}$}}{nl}}}{nl}" };

            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Abstract }), null, $"public class type {{{nl}    {nl}    abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Final }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Static }), null, $"public class type {{{nl}    {nl}    static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Override }), null, $"public class type {{{nl}    {nl}    override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Const }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.ScopeMask }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New }), null, $"public class type {{{nl}    {nl}    new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.VTableMask }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Overloaded }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.AccessMask }), null, $"public class type {{{nl}    {nl}    void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Assembly }), null, $"public class type {{{nl}    {nl}    internal new virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyAndAssembly }), null, $"public class type {{{nl}    {nl}    internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Family }), null, $"public class type {{{nl}    {nl}    protected new virtual void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.FamilyOrAssembly }), null, $"public class type {{{nl}    {nl}    protected internal new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Private }), null, $"public class type {{{nl}    {nl}    private new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Abstract | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new abstract void name();{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Final | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Static | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new static void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Override | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new override void name() {{{nl}    }}{nl}}}{nl}" };
            yield return new object[] { CreateType(new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.New | MemberAttributes.Const | MemberAttributes.Public }), null, $"public class type {{{nl}    {nl}    public new virtual void name() {{{nl}    }}{nl}}}{nl}" };

            var singleAttributeMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleAttributeMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attributeName"));
            yield return new object[] { CreateType(singleAttributeMethod), null, $"public class type {{{nl}    {nl}    [attributeName()]{nl}    private returnType name() {{{nl}    }}{nl}}}{nl}" };

            var singleReturnTypeAttributeMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleReturnTypeAttributeMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attributeName"));
            yield return new object[] { CreateType(singleReturnTypeAttributeMethod), null, $"public class type {{{nl}    {nl}    [return: attributeName()]{nl}    private returnType name() {{{nl}    }}{nl}}}{nl}" };

            var singleTypeParameterMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleTypeParameterMethod.TypeParameters.Add(new CodeTypeParameter("typeParameter"));
            yield return new object[] { CreateType(singleTypeParameterMethod), null, $"public class type {{{nl}    {nl}    private returnType name<typeParameter>() {{{nl}    }}{nl}}}{nl}" };

            var singleImplementationTypes = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleImplementationTypes.ImplementationTypes.Add(new CodeTypeReference("implementationType"));
            yield return new object[] { CreateType(singleImplementationTypes), null, $"public class type {{{nl}    {nl}    private returnType name() {{{nl}    }}{nl}}}{nl}" };

            var singleParameterMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleParameterMethod.Parameters.Add(new CodeParameterDeclarationExpression("parameterType", "parameterName"));
            yield return new object[] { CreateType(singleParameterMethod), null, $"public class type {{{nl}    {nl}    private returnType name(parameterType parameterName) {{{nl}    }}{nl}}}{nl}" };

            var singleStatementMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            singleStatementMethod.Statements.Add(new CodeExpressionStatement(new CodePrimitiveExpression(1)));
            yield return new object[] { CreateType(singleStatementMethod), null, $"public class type {{{nl}    {nl}    private returnType name() {{{nl}        1;{nl}    }}{nl}}}{nl}" };

            var abstractMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", Attributes = MemberAttributes.Abstract };
            abstractMethod.Statements.Add(new CodeStatement());
            yield return new object[] { CreateType(abstractMethod), null, $"public class type {{{nl}    {nl}    abstract returnType name();{nl}}}{nl}" };

            var fullMethod = new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" };
            fullMethod.StartDirectives.AddRange(directives);
            fullMethod.Comments.AddRange(comments);
            fullMethod.CustomAttributes.AddRange(attributes);
            fullMethod.ReturnTypeCustomAttributes.AddRange(attributes);
            fullMethod.TypeParameters.AddRange(typeParameters);
            fullMethod.ImplementationTypes.Add(new CodeTypeReference());
            fullMethod.ImplementationTypes.Add(new CodeTypeReference("implementationType"));
            fullMethod.ImplementationTypes.Add(new CodeTypeReference("is"));
            fullMethod.Parameters.AddRange(parameters);
            fullMethod.Statements.Add(new CodeExpressionStatement(new CodePrimitiveExpression(1)));
            fullMethod.Statements.Add(new CodeExpressionStatement(new CodePrimitiveExpression(1)));
            fullMethod.EndDirectives.AddRange(directives);

            // Interface.
            var fullInterface = new CodeTypeDeclaration("type") { TypeAttributes = TypeAttributes.Public | TypeAttributes.Interface };
            fullInterface.StartDirectives.AddRange(directives);
            fullInterface.Comments.AddRange(comments);
            fullInterface.CustomAttributes.AddRange(attributes);
            fullInterface.BaseTypes.Add(new CodeTypeReference());
            fullInterface.BaseTypes.Add(new CodeTypeReference("baseType"));
            fullInterface.BaseTypes.Add(new CodeTypeReference("as"));
            fullInterface.LinePragma = new CodeLinePragma("fileName", 1);
            fullInterface.TypeParameters.AddRange(typeParameters);
            fullInterface.EndDirectives.AddRange(directives);
            fullInterface.Members.Add(new CodeSnippetTypeMember());
            fullInterface.Members.Add(new CodeSnippetTypeMember("text"));
            fullInterface.Members.Add(fullSnippet);
            fullInterface.Members.Add(new CodeMemberMethod());
            fullInterface.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" });
            fullInterface.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", PrivateImplementationType = new CodeTypeReference("privateType") });
            fullInterface.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is" });
            fullInterface.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is", PrivateImplementationType = new CodeTypeReference("base") });
            fullInterface.Members.Add(abstractMethod);
            fullInterface.Members.Add(fullMethod);
            fullInterface.Members.Add(new CodeMemberField());
            fullInterface.Members.Add(new CodeMemberField("fieldType", "fieldName"));
            fullInterface.Members.Add(new CodeMemberField("as", "is"));
            fullInterface.Members.Add(fullField);
            fullInterface.Members.Add(new CodeTypeMember());
            yield return new object[]
            {
                fullInterface,
                null,
$@"#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""
[()]
[name1()]
[name2(1)]
[name3(arg1=1, arg2=2)]
[@is(@as=1, arg2=2)]
public interface type<, name, [attribute()]  name, name, name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)]  name> : void, baseType, @as
    where name : new()
    where name : constraint
    where name : void, constraint, @as, new () {{
    
    
    
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
    
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    

text
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
text
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    
    
    void ();
    
    returnType name();
    
    returnType privateType.name();
    
    @as @is();
    
    @as @base.@is();
    
    returnType name();
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    [()]
    [name1()]
    [name2(1)]
    [name3(arg1=1, arg2=2)]
    [@is(@as=1, arg2=2)]
    [return: ()]
    [return: name1()]
    [return: name2(1)]
    [return: name3(arg1=1, arg2=2)]
    [return: @is(@as=1, arg2=2)]
    returnType name<, name, [attribute()]  name, name, name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)]  name>(void , type , type name, type name, out type name, ref type name, type name, type name, @as @is, [type()] type name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)] type name, params type name, params type name)
        where name : new()
        where name : constraint
        where name : void, constraint, @as, new ();
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
}}

#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
"
            };

            // Enum.
            yield return new object[] { new CodeTypeDeclaration { IsEnum = true }, null, $"public enum  {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsEnum = true }, null, $"public enum name {{{nl}}}{nl}" };

            var fullEnum = new CodeTypeDeclaration("name") { IsEnum = true };
            fullEnum.StartDirectives.AddRange(directives);
            fullEnum.Comments.AddRange(comments);
            fullEnum.CustomAttributes.AddRange(attributes);
            fullEnum.BaseTypes.Add(new CodeTypeReference());
            fullEnum.BaseTypes.Add(new CodeTypeReference("baseType"));
            fullEnum.BaseTypes.Add(new CodeTypeReference("as"));
            fullEnum.LinePragma = new CodeLinePragma("fileName", 1);
            fullEnum.TypeParameters.AddRange(typeParameters);
            fullEnum.EndDirectives.AddRange(directives);
            fullEnum.Members.Add(new CodeSnippetTypeMember());
            fullEnum.Members.Add(new CodeSnippetTypeMember("text"));
            fullEnum.Members.Add(fullSnippet);
            fullEnum.Members.Add(new CodeMemberMethod());
            fullEnum.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" });
            fullEnum.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", PrivateImplementationType = new CodeTypeReference("privateType") });
            fullEnum.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is" });
            fullEnum.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is", PrivateImplementationType = new CodeTypeReference("base") });
            fullEnum.Members.Add(abstractMethod);
            fullEnum.Members.Add(fullMethod);
            fullEnum.Members.Add(new CodeMemberField());
            fullEnum.Members.Add(new CodeMemberField("fieldType", "fieldName"));
            fullEnum.Members.Add(new CodeMemberField("as", "is"));
            fullEnum.Members.Add(fullField);
            fullEnum.Members.Add(new CodeTypeMember());
            yield return new object[]
            {
                fullEnum,
                null,
$@"#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""
[()]
[name1()]
[name2(1)]
[name3(arg1=1, arg2=2)]
[@is(@as=1, arg2=2)]
public enum name<, name, [attribute()]  name, name, name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)]  name> : void, baseType, @as
    where name : new()
    where name : constraint
    where name : void, constraint, @as, new () {{
    
    ,
    
    fieldName,
    
    @is,
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
    [()]
    [name1()]
    [name2(1)]
    [name3(arg1=1, arg2=2)]
    [@is(@as=1, arg2=2)]
    fieldName = 1,
    
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    

text
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    
    #line 1 ""fileName""
text
    #line default
    #line hidden
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    
    
    
    
    
    
    
    
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
    // 
    // comment1
    /// comment2
    #pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
    #pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
    #region 
    #region startText
    #endregion
    #endregion
    #region 
    #region startText
}}

#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
"
            };

            // Delegate.
            yield return new object[] { new CodeTypeDelegate(), null, $"public delegate void ();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name"), null, $"public delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference("returnType") }, null, $"public delegate returnType name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("is") { ReturnType = new CodeTypeReference("as") }, null, $"public delegate @as @is();{nl}" };

            var singleAttributeDelegate = new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference("returnType") };
            singleAttributeDelegate.CustomAttributes.Add(new CodeAttributeDeclaration("attributeName"));
            yield return new object[] { singleAttributeDelegate, null, $"[attributeName()]{nl}public delegate returnType name();{nl}" };

            var singleParameterDelegate = new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference("returnType") };
            singleParameterDelegate.Parameters.Add(new CodeParameterDeclarationExpression("parameterType", "parameterName"));
            yield return new object[] { singleParameterDelegate, null, $"public delegate returnType name(parameterType parameterName);{nl}" };

            var fullDelegate = new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference("returnType") };
            fullDelegate.StartDirectives.AddRange(directives);
            fullDelegate.Comments.AddRange(comments);
            fullDelegate.CustomAttributes.AddRange(attributes);
            fullDelegate.BaseTypes.Add(new CodeTypeReference());
            fullDelegate.BaseTypes.Add(new CodeTypeReference("baseType"));
            fullDelegate.BaseTypes.Add(new CodeTypeReference("as"));
            fullDelegate.LinePragma = new CodeLinePragma("fileName", 1);
            fullDelegate.TypeParameters.AddRange(typeParameters);
            fullDelegate.Parameters.AddRange(parameters);
            fullDelegate.EndDirectives.AddRange(directives);
            fullDelegate.Members.Add(new CodeSnippetTypeMember());
            fullDelegate.Members.Add(new CodeSnippetTypeMember("text"));
            fullDelegate.Members.Add(fullSnippet);
            fullDelegate.Members.Add(new CodeMemberMethod());
            fullDelegate.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name" });
            fullDelegate.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("returnType"), Name = "name", PrivateImplementationType = new CodeTypeReference("privateType") });
            fullDelegate.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is" });
            fullDelegate.Members.Add(new CodeMemberMethod { ReturnType = new CodeTypeReference("as"), Name = "is", PrivateImplementationType = new CodeTypeReference("base") });
            fullDelegate.Members.Add(abstractMethod);
            fullDelegate.Members.Add(fullMethod);
            fullDelegate.Members.Add(new CodeMemberField());
            fullDelegate.Members.Add(new CodeMemberField("fieldType", "fieldName"));
            fullDelegate.Members.Add(new CodeMemberField("as", "is"));
            fullDelegate.Members.Add(fullField);
            fullDelegate.Members.Add(new CodeTypeMember());
            yield return new object[]
            {
                fullDelegate,
                null,
$@"#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""
[()]
[name1()]
[name2(1)]
[name3(arg1=1, arg2=2)]
[@is(@as=1, arg2=2)]
public delegate returnType name(void , type , type name, type name, out type name, ref type name, type name, type name, @as @is, [type()] type name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)] type name, params type name, params type name);




#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""

#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText


text
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""
text
#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText








#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText

#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
"
            };

            var largeDelegate1 = new CodeTypeDelegate("name");
            largeDelegate1.Parameters.AddRange(manyParameters1);
            yield return new object[] { largeDelegate1, null, $"public delegate void name(type0 name0, type1 name1, type2 name2, type3 name3, type4 name4, type5 name5, type6 name6, type7 name7, type8 name8, type9 name9, type10 name10, type11 name11, type12 name12, type13 name13, type14 name14);{nl}" };

            var largeDelegate2 = new CodeTypeDelegate("name");
            largeDelegate2.Parameters.AddRange(manyParameters2);
            yield return new object[] { largeDelegate2, null, $"public delegate void name({nl}            type0 name0, {nl}            type1 name1, {nl}            type2 name2, {nl}            type3 name3, {nl}            type4 name4, {nl}            type5 name5, {nl}            type6 name6, {nl}            type7 name7, {nl}            type8 name8, {nl}            type9 name9, {nl}            type10 name10, {nl}            type11 name11, {nl}            type12 name12, {nl}            type13 name13, {nl}            type14 name14, {nl}            type15 name15);{nl}" };
            yield return new object[] { largeDelegate2, customOptions, $"public delegate void name({nl}$$$type0 name0, {nl}$$$type1 name1, {nl}$$$type2 name2, {nl}$$$type3 name3, {nl}$$$type4 name4, {nl}$$$type5 name5, {nl}$$$type6 name6, {nl}$$$type7 name7, {nl}$$$type8 name8, {nl}$$$type9 name9, {nl}$$$type10 name10, {nl}$$$type11 name11, {nl}$$$type12 name12, {nl}$$$type13 name13, {nl}$$$type14 name14, {nl}$$$type15 name15);{nl}" };

            // Custom.
            yield return new object[] { new CodeTypeDeclaration("name") { LinePragma = new CodeLinePragma() }, null, $"{nl}#line 0 \"\"{nl}public class name {{{nl}}}{nl}{nl}#line default{nl}#line hidden{nl}" };

            var singleBaseTypeType = new CodeTypeDeclaration("name");
            singleBaseTypeType.BaseTypes.Add(new CodeTypeReference("baseType"));
            yield return new object[] { singleBaseTypeType, null, $"public class name : baseType {{{nl}}}{nl}" };

            var singleTypeParameterType = new CodeTypeDeclaration("name");
            singleTypeParameterType.TypeParameters.Add(new CodeTypeParameter("name"));
            yield return new object[] { singleTypeParameterType, null, $"public class name<name> {{{nl}}}{nl}" };

            var singleTypeParameterConstraintType = new CodeTypeDeclaration("name");
            singleTypeParameterConstraintType.TypeParameters.Add(singleTypeParameterConstraint);
            yield return new object[] { singleTypeParameterConstraintType, null, $"public class name<name>{nl}    where name : constraint {{{nl}}}{nl}" };

            var singleNewTypeParameterConstraintType = new CodeTypeDeclaration("name");
            singleNewTypeParameterConstraintType.TypeParameters.Add(new CodeTypeParameter("name") { HasConstructorConstraint = true });
            yield return new object[] { singleNewTypeParameterConstraintType, null, $"public class name<name>{nl}    where name : new() {{{nl}}}{nl}" };

            yield return new object[] { CreateType(new CodeTypeMember()), null, $"public class type {{{nl}}}{nl}" };

            var fullType = new CodeTypeDeclaration("name");
            fullType.StartDirectives.AddRange(directives);
            fullType.Comments.AddRange(comments);
            fullType.CustomAttributes.AddRange(attributes);
            fullType.BaseTypes.Add(new CodeTypeReference());
            fullType.BaseTypes.Add(new CodeTypeReference("baseType"));
            fullType.BaseTypes.Add(new CodeTypeReference("as"));
            fullType.LinePragma = new CodeLinePragma("fileName", 1);
            fullType.TypeParameters.AddRange(typeParameters);
            fullType.EndDirectives.AddRange(directives);
            yield return new object[]
            {
                fullType,
                null,
$@"#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
// 
// comment1
/// comment2

#line 1 ""fileName""
[()]
[name1()]
[name2(1)]
[name3(arg1=1, arg2=2)]
[@is(@as=1, arg2=2)]
public class name<, name, [attribute()]  name, name, name, [()] [name1()] [name2(1)] [name3(arg1=1, arg2=2)] [@is(@as=1, arg2=2)]  name> : void, baseType, @as
    where name : new()
    where name : constraint
    where name : void, constraint, @as, new () {{
}}

#line default
#line hidden
#pragma checksum """" ""{{00000000-0000-0000-0000-000000000000}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" """"
#pragma checksum ""startFileName"" ""{{00000001-0002-0003-0405-060708090a0b}}"" ""010203""
#region 
#region startText
#endregion
#endregion
#region 
#region startText
"
            };

            // TypeAttributes.
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NotPublic }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NotPublic, IsStruct = true }, null, $"internal struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NotPublic, IsEnum = true }, null, $"internal enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.AutoLayout }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Class }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.AnsiClass }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Public }, null, $"public class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Public, IsStruct = true }, null, $"public struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Public, IsEnum = true }, null, $"public enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPublic }, null, $"public class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPublic, IsStruct = true }, null, $"public struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPublic, IsEnum = true }, null, $"public enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPrivate }, null, $"private class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPrivate, IsStruct = true }, null, $"private struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedPrivate, IsEnum = true }, null, $"private enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedFamily }, null, $"protected class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedAssembly }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedFamANDAssem }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.VisibilityMask }, null, $"protected internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.NestedFamORAssem }, null, $"protected internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.SequentialLayout }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.ExplicitLayout }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.LayoutMask }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.ClassSemanticsMask }, null, $"internal interface name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Interface }, null, $"internal interface name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Interface, IsStruct = true }, null, $"internal struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Interface, IsEnum = true }, null, $"internal enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Abstract }, null, $"internal abstract class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Abstract, IsStruct = true }, null, $"internal struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Abstract, IsEnum = true }, null, $"internal enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Sealed }, null, $"internal sealed class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Sealed, IsStruct = true }, null, $"internal struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Sealed, IsEnum = true }, null, $"internal enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.SpecialName }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.RTSpecialName }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Import }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.Serializable }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.WindowsRuntime }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.UnicodeClass }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.AutoClass }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.StringFormatMask }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.CustomFormatClass }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.HasSecurity }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.ReservedMask }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.BeforeFieldInit }, null, $"internal class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { TypeAttributes = TypeAttributes.CustomFormatMask }, null, $"internal class name {{{nl}}}{nl}" };

            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NotPublic }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NotPublic, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NotPublic, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.AutoLayout }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Class }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.AnsiClass }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Public }, null, $"public delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Public, IsStruct = true }, null, $"public delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Public, IsEnum = true }, null, $"public delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPublic }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPublic, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPublic, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPrivate }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPrivate, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedPrivate, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedFamily }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedAssembly }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedFamANDAssem }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.VisibilityMask }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.NestedFamORAssem }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.SequentialLayout }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.ExplicitLayout }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.LayoutMask }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.ClassSemanticsMask }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Interface }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Interface, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Interface, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Abstract }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Abstract, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Abstract, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Sealed }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Sealed, IsStruct = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Sealed, IsEnum = true }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.SpecialName }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.RTSpecialName }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Import }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.Serializable }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.WindowsRuntime }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.UnicodeClass }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.AutoClass }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.StringFormatMask }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.CustomFormatClass }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.HasSecurity }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.ReservedMask }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.BeforeFieldInit }, null, $"delegate void name();{nl}" };
            yield return new object[] { new CodeTypeDelegate("name") { TypeAttributes = TypeAttributes.CustomFormatMask }, null, $"delegate void name();{nl}" };

            yield return new object[] { new CodeTypeDeclaration("name") { Attributes = MemberAttributes.New }, null, $"new public class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { Attributes = MemberAttributes.New, IsStruct = true }, null, $"new public struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { Attributes = MemberAttributes.New, IsEnum = true }, null, $"new public enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { Attributes = MemberAttributes.New, TypeAttributes = TypeAttributes.Interface }, null, $"new internal interface name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsPartial = true }, null, $"public partial class name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsPartial = true, IsStruct = true }, null, $"public partial struct name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsPartial = true, IsEnum = true }, null, $"public enum name {{{nl}}}{nl}" };
            yield return new object[] { new CodeTypeDeclaration("name") { IsPartial = true, TypeAttributes = TypeAttributes.Interface }, null, $"internal partial interface name {{{nl}}}{nl}" };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromType_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework has different brace semantics.")]
        public void GenerateCodeFromType_Invoke_Success(CodeTypeDeclaration e, CodeGeneratorOptions o, string expected)
        {
            ICodeGenerator generator = GetGenerator();
            var writer = new StringWriter();
            generator.GenerateCodeFromType(e, writer, o);
            AssertEqualLong(expected, writer.ToString());
        }

        private void AssertEqualLong(string expected, string actual)
        {
            string normalizedExpected = LineEndingsHelper.Normalize(expected);

            try
            {
                Assert.Equal(normalizedExpected, actual);
            }
            catch (Xunit.Sdk.AssertActualExpectedException)
            {
                string Normalize(string s)
                {
                    return s
                        .Replace("\"", "\"\"")
                        .Replace("{", "{{")
                        .Replace("}", "}}");
                }

                var s = new StringBuilder();
                s.AppendLine();
                s.AppendLine($"Expected: {Environment.NewLine}$@\"{Normalize(normalizedExpected)}\"");
                s.AppendLine($"Actual:   {Environment.NewLine}$@\"{Normalize(actual)}\"");

                throw new Exception(s.ToString());
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void GenerateCodeFromType_NullE_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            Assert.Throws<ArgumentNullException>("e", () => generator.GenerateCodeFromStatement(null, new StringWriter(), new CodeGeneratorOptions()));
        }

        public static IEnumerable<object[]> GenerateCodeFromType_InvalidE_TestData()
        {
            var invalidTypeComment = new CodeTypeDeclaration("type");
            invalidTypeComment.Comments.Add(new CodeCommentStatement());
            yield return new object[] { invalidTypeComment };

            CodeTypeDeclaration CreateType(CodeTypeMember member)
            {
                var t = new CodeTypeDeclaration("type");
                t.Members.Add(member);
                return t;
            };
            yield return new object[] { CreateType(new CodeMemberField { InitExpression = new CodeExpression() }) };

            var invalidStatementMethod = new CodeMemberMethod();
            invalidStatementMethod.Statements.Add(new CodeStatement());
            yield return new object[] { CreateType(invalidStatementMethod) };
        }

        [Theory]
        [MemberData(nameof(GenerateCodeFromType_InvalidE_TestData))]
        public void GenerateCodeFromType_InvalidE_ThrowsArgumentException(CodeTypeDeclaration e)
        {
            ICodeGenerator generator = GetGenerator();
            AssertExtensions.Throws<ArgumentException>("e", () => generator.GenerateCodeFromType(e, new StringWriter(), new CodeGeneratorOptions()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws a NullReferenceException")]
        public void GenerateCodeFromType_NullWriter_ThrowsArgumentNullException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeTypeDeclaration("name");
            Assert.Throws<ArgumentNullException>("writer", () => generator.GenerateCodeFromType(e, null, new CodeGeneratorOptions()));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework has different validation semantics.")]
        public void GenerateCodeFromType_InvokeAlreadyGenerating_ThrowsInvalidOperationException()
        {
            ICodeGenerator generator = GetGenerator();
            var e = new CodeTypeDeclaration("name");
            var writer = new DelegatingWriter();
            int callCount = 0;
            writer.OnWrite = () =>
            {
                Assert.Throws<InvalidOperationException>(() => generator.GenerateCodeFromType(new CodeTypeDeclaration("type"), new StringWriter(), new CodeGeneratorOptions()));
                callCount++;
            };
            generator.GenerateCodeFromType(e, writer, new CodeGeneratorOptions());
            Assert.Equal(5, callCount);
        }

        public static IEnumerable<object[]> Validate_Valid_TestData()
        {
            foreach (object[] testData in IsValidIdentifier_TestData())
            {
                if ((bool)testData[1])
                {
                    yield return new object[] { testData[0] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Validate_Valid_TestData))]
        public void ValidateIdentifier_InvokeValid_Nop(string value)
        {
            ICodeGenerator generator = GetGenerator();
            generator.ValidateIdentifier(value);
        }

        public static IEnumerable<object[]> Validate_Invalid_TestData()
        {
            foreach (object[] testData in IsValidIdentifier_TestData())
            {
                if (!(bool)testData[1])
                {
                    yield return new object[] { testData[0] };
                }
            }
        }

        [Theory]
        [MemberData(nameof(Validate_Invalid_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework is missing some newer keywords")]
        public void ValidateIdentifier_InvokeInvalid_ThrowsArgumentException(string value)
        {
            ICodeGenerator generator = GetGenerator();
            AssertExtensions.Throws<ArgumentException>("value", null, () => generator.ValidateIdentifier(value));
        }

        private static ICodeGenerator GetGenerator()
        {
#pragma warning disable 0618
            return CodeDomProvider.CreateProvider("cs").CreateGenerator();
#pragma warning restore 0618
        }

        private class DelegatingWriter : StringWriter
        {
            public Action OnWrite { get; set; }

            public override void Write(string value)
            {
                OnWrite();
                base.Write(value);
            }
        }
    }
}
