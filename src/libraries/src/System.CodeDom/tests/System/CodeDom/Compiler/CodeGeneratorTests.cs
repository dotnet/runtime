// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CodeGeneratorTests
    {
        public class Generator : CodeGenerator
        {
            [Fact]
            public void Ctor_Default()
            {
                Generator generator = this;
                Assert.Null(generator.CurrentClass);
                Assert.Null(generator.CurrentMember);
                Assert.Equal("<% unknown %>", generator.CurrentMemberName);
                Assert.Equal("<% unknown %>", generator.CurrentTypeName);
                Assert.Throws<NullReferenceException>(() => generator.Indent);
                Assert.Throws<NullReferenceException>(() => generator.Indent = 1);
                Assert.False(generator.IsCurrentClass);
                Assert.False(generator.IsCurrentDelegate);
                Assert.False(generator.IsCurrentEnum);
                Assert.False(generator.IsCurrentInterface);
                Assert.False(generator.IsCurrentStruct);
                Assert.Null(generator.Options);
                Assert.Null(generator.Output);
            }

            [Fact]
            public void ICodeGeneratorCreateEscapedIdentifier_Invoke_ReturnsExpected()
            {
                ICodeGenerator generator = this;
                Assert.Equal("escapedIdentifier", generator.CreateEscapedIdentifier(null));
            }

            [Fact]
            public void ICodeGeneratorCreateValidIdentifier_Invoke_ReturnsExpected()
            {
                ICodeGenerator generator = this;
                Assert.Equal("validIdentifier", generator.CreateValidIdentifier(null));
            }

            protected override string NullToken => throw new NotImplementedException();

            protected override string CreateEscapedIdentifier(string value)
            {
                return "escapedIdentifier";
            }

            protected override string CreateValidIdentifier(string value)
            {
                return "validIdentifier";
            }

            protected override void GenerateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
            {
                Output.Write("argumentReference");
            }

            protected override void GenerateArrayCreateExpression(CodeArrayCreateExpression e)
            {
                Output.Write("arrayCreate");
            }

            protected override void GenerateArrayIndexerExpression(CodeArrayIndexerExpression e)
            {
                Output.Write("arrayIndexer");
            }

            protected override void GenerateAssignStatement(CodeAssignStatement e)
            {
                Output.Write("assign");
            }

            protected override void GenerateAttachEventStatement(CodeAttachEventStatement e)
            {
                Output.Write("attachEvent");
            }

            protected override void GenerateAttributeDeclarationsEnd(CodeAttributeDeclarationCollection attributes)
            {
                Output.Write("attributeDeclarationsEnd");
            }

            protected override void GenerateAttributeDeclarationsStart(CodeAttributeDeclarationCollection attributes)
            {
                Output.Write("attributeDeclarationsStart");
            }

            protected override void GenerateBaseReferenceExpression(CodeBaseReferenceExpression e)
            {
                Output.Write("baseReference");
            }

            protected override void GenerateCastExpression(CodeCastExpression e)
            {
                Output.Write("cast");
            }

            protected override void GenerateComment(CodeComment e)
            {
                Output.Write("comment");
            }

            protected override void GenerateConditionStatement(CodeConditionStatement e)
            {
                Output.Write("condition");
            }

            protected override void GenerateConstructor(CodeConstructor e, CodeTypeDeclaration c)
            {
                Output.Write("constructor");
            }

            protected override void GenerateDelegateCreateExpression(CodeDelegateCreateExpression e)
            {
                Output.Write("delegateCreate");
            }

            protected override void GenerateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
            {
                Output.Write("delegateInvoke");
            }

            protected override void GenerateEntryPointMethod(CodeEntryPointMethod e, CodeTypeDeclaration c)
            {
                Output.Write("entryPointMethod");
            }

            protected override void GenerateEvent(CodeMemberEvent e, CodeTypeDeclaration c)
            {
                Output.Write("event");
            }

            protected override void GenerateEventReferenceExpression(CodeEventReferenceExpression e)
            {
                Output.Write("eventReference");
            }

            protected override void GenerateExpressionStatement(CodeExpressionStatement e)
            {
                Output.Write("expression");
            }

            protected override void GenerateField(CodeMemberField e)
            {
                Output.Write("field");
            }

            protected override void GenerateFieldReferenceExpression(CodeFieldReferenceExpression e)
            {
                Output.Write("fieldReference");
            }

            protected override void GenerateGotoStatement(CodeGotoStatement e)
            {
                Output.Write("goto");
            }

            protected override void GenerateIndexerExpression(CodeIndexerExpression e)
            {
                Output.Write("indexer");
            }

            protected override void GenerateIterationStatement(CodeIterationStatement e)
            {
                Output.Write("iteration");
            }

            protected override void GenerateLabeledStatement(CodeLabeledStatement e)
            {
                Output.Write("labelled");
            }

            protected override void GenerateLinePragmaEnd(CodeLinePragma e)
            {
                Output.Write("linePragmaEnd");
            }

            protected override void GenerateLinePragmaStart(CodeLinePragma e)
            {
                Output.Write("linePragmaStart");
            }

            protected override void GenerateMethod(CodeMemberMethod e, CodeTypeDeclaration c)
            {
                Output.Write("method");
            }

            protected override void GenerateMethodInvokeExpression(CodeMethodInvokeExpression e)
            {
                Output.Write("methodInvoke");
            }

            protected override void GenerateMethodReferenceExpression(CodeMethodReferenceExpression e)
            {
                Output.Write("methodReference");
            }

            protected override void GenerateMethodReturnStatement(CodeMethodReturnStatement e)
            {
                Output.Write("methodReturn");
            }

            protected override void GenerateNamespaceEnd(CodeNamespace e)
            {
                Output.Write("namespaceEnd");
            }

            protected override void GenerateNamespaceImport(CodeNamespaceImport e)
            {
                Output.Write("namespaceImport");
            }

            protected override void GenerateNamespaceStart(CodeNamespace e)
            {
                Output.Write("namespaceStart");
            }

            protected override void GenerateObjectCreateExpression(CodeObjectCreateExpression e)
            {
                Output.Write("objectCreate");
            }

            protected override void GenerateProperty(CodeMemberProperty e, CodeTypeDeclaration c)
            {
                Output.Write("property");
            }

            protected override void GeneratePropertyReferenceExpression(CodePropertyReferenceExpression e)
            {
                Output.Write("propertyReference");
            }

            protected override void GeneratePropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression e)
            {
                Output.Write("propertySetValueReference");
            }

            protected override void GenerateRemoveEventStatement(CodeRemoveEventStatement e)
            {
                Output.Write("removeEvent");
            }

            protected override void GenerateSnippetExpression(CodeSnippetExpression e)
            {
                Output.Write("snippetExpression");
            }

            protected override void GenerateSnippetMember(CodeSnippetTypeMember e)
            {
                Output.Write("snippetMember");
            }

            protected override void GenerateThisReferenceExpression(CodeThisReferenceExpression e)
            {
                Output.Write("thisReference");
            }

            protected override void GenerateThrowExceptionStatement(CodeThrowExceptionStatement e)
            {
                Output.Write("throwException");
            }

            protected override void GenerateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
            {
                Output.Write("tryCatchFinally");
            }

            protected override void GenerateTypeConstructor(CodeTypeConstructor e)
            {
                Output.Write("typeConstructor");
            }

            protected override void GenerateTypeEnd(CodeTypeDeclaration e)
            {
                Output.Write("typeEnd");
            }

            protected override void GenerateTypeStart(CodeTypeDeclaration e)
            {
                Output.Write("typeStart");
            }

            protected override void GenerateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
            {
                Output.Write("variableDeclarationStatement");
            }

            protected override void GenerateVariableReferenceExpression(CodeVariableReferenceExpression e)
            {
                Output.Write("variableReferenceExpression");
            }

            protected override string GetTypeOutput(CodeTypeReference value)
            {
                throw new NotImplementedException();
            }

            protected override bool IsValidIdentifier(string value)
            {
                return value == "invalid";
            }

            protected override void OutputType(CodeTypeReference typeRef)
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
    }
}
