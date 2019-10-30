// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class CodeValidatorTests
    {
        public static IEnumerable<object[]> ValidateIdentifiers_Valid_TestData()
        {
            // CodeComment.
            yield return new object[] { new CodeComment() };
            yield return new object[] { new CodeCommentStatement() };
            yield return new object[] { new CodeCommentStatement((string)null) };
            yield return new object[] { new CodeCommentStatement(string.Empty) };
            yield return new object[] { new CodeCommentStatement("text") };

            // CodeChecksumPragma
            yield return new object[] { new CodeChecksumPragma() };
            yield return new object[] { new CodeChecksumPragma(null, Guid.NewGuid(), new byte[0]) };
            yield return new object[] { new CodeChecksumPragma(string.Empty, Guid.NewGuid(), new byte[0]) };
            yield return new object[] { new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]) };

            // CodeRegionDirective.
            yield return new object[] { new CodeRegionDirective() };
            yield return new object[] { new CodeRegionDirective(CodeRegionMode.None, null) };
            yield return new object[] { new CodeRegionDirective(CodeRegionMode.None, string.Empty) };
            yield return new object[] { new CodeRegionDirective(CodeRegionMode.None, "name") };

            // CodeNamespaceImport.
            yield return new object[] { new CodeNamespaceImport("nameSpace") };

            var fullNamespaceImport = new CodeNamespaceImport("nameSpace") { LinePragma = new CodeLinePragma() };
            yield return new object[] { fullNamespaceImport };

            // CodeMemberEvent.
            yield return new object[] { new CodeMemberEvent() };
            yield return new object[] { new CodeMemberEvent { Name = "0" } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };

            var fullEvent = new CodeMemberEvent
            {
                Name = "name",
                LinePragma = new CodeLinePragma(),
                PrivateImplementationType = new CodeTypeReference("type")
            };
            fullEvent.Comments.Add(new CodeCommentStatement());
            fullEvent.Comments.Add(new CodeCommentStatement("0"));
            fullEvent.Comments.Add(new CodeCommentStatement("text"));
            fullEvent.StartDirectives.Add(new CodeChecksumPragma());
            fullEvent.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullEvent.EndDirectives.Add(new CodeChecksumPragma());
            fullEvent.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullEvent.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullEvent.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEvent.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEvent.ImplementationTypes.Add(new CodeTypeReference((string)null));
            fullEvent.ImplementationTypes.Add(new CodeTypeReference(string.Empty));
            fullEvent.ImplementationTypes.Add(new CodeTypeReference("constraint1"));
            fullEvent.ImplementationTypes.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            yield return new object[] { fullEvent };

            // CodeMemberField.
            yield return new object[] { new CodeMemberField(new CodeTypeReference("type"), "name") };

            var fullField = new CodeMemberField(new CodeTypeReference("type"), "name")
            {
                LinePragma = new CodeLinePragma(),
                InitExpression = new CodePrimitiveExpression(1)
            };
            fullField.Comments.Add(new CodeCommentStatement());
            fullField.Comments.Add(new CodeCommentStatement("0"));
            fullField.Comments.Add(new CodeCommentStatement("text"));
            fullField.StartDirectives.Add(new CodeChecksumPragma());
            fullField.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullField.EndDirectives.Add(new CodeChecksumPragma());
            fullField.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullField.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullField.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullField.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            yield return new object[] { fullField };

            // CodeParameterDeclarationExpression.
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name") };

            var fullParameter = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            fullParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            yield return new object[] { fullParameter };

            var fullTypeParameter = new CodeTypeParameter("parameter");
            fullTypeParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullTypeParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeParameter.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeParameter.Constraints.Add(new CodeTypeReference((string)null));
            fullTypeParameter.Constraints.Add(new CodeTypeReference(string.Empty));
            fullTypeParameter.Constraints.Add(new CodeTypeReference("constraint1"));
            fullTypeParameter.Constraints.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            
            var invalidParameterAttribute1 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());

            var invalidParameterAttribute2 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));

            var invalidParameterAttribute3 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));

            var invalidParameterAttribute4 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));

            var invalidParameterAttribute5 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));

            var invalidParameterAttribute6 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));

            // CodeMemberMethod.
            yield return new object[] { new CodeMemberMethod { Name = "name" } };

            var abstractMethod = new CodeMemberMethod { Name = "name", Attributes = MemberAttributes.Abstract };
            abstractMethod.Statements.Add(new CodeStatement());
            yield return new object[] { abstractMethod };

            var fullMethod = new CodeMemberMethod
            {
                Name = "name",
                LinePragma = new CodeLinePragma(),
                ReturnType = new CodeTypeReference("returnType"),
                PrivateImplementationType = new CodeTypeReference("privateImplementationType")
            };
            fullMethod.Comments.Add(new CodeCommentStatement());
            fullMethod.Comments.Add(new CodeCommentStatement("0"));
            fullMethod.Comments.Add(new CodeCommentStatement("text"));
            fullMethod.StartDirectives.Add(new CodeChecksumPragma());
            fullMethod.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullMethod.EndDirectives.Add(new CodeChecksumPragma());
            fullMethod.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullMethod.ImplementationTypes.Add(new CodeTypeReference((string)null));
            fullMethod.ImplementationTypes.Add(new CodeTypeReference(string.Empty));
            fullMethod.ImplementationTypes.Add(new CodeTypeReference("constraint1"));
            fullMethod.ImplementationTypes.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullMethod.Statements.Add(new CodeMethodReturnStatement());
            fullMethod.Statements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullMethod.Parameters.Add(fullParameter);
            fullMethod.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullMethod.TypeParameters.Add(fullTypeParameter);
            yield return new object[] { fullMethod };

            // CodeEntryPointMethod.
            yield return new object[] { new CodeEntryPointMethod() };
            yield return new object[] { new CodeEntryPointMethod { Name = null } };
            yield return new object[] { new CodeEntryPointMethod { Name = string.Empty } };
            yield return new object[] { new CodeEntryPointMethod { Name = "name" } };
            yield return new object[] { new CodeEntryPointMethod { Name = "0" } };
            yield return new object[] { new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference() } };
            yield return new object[] { new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };

            var abstractEntryPointMethod = new CodeEntryPointMethod { Name = "name", Attributes = MemberAttributes.Abstract };
            abstractEntryPointMethod.Statements.Add(new CodeMethodReturnStatement());
            yield return new object[] { abstractEntryPointMethod };

            var fullEntryPointMethod = new CodeEntryPointMethod
            {
                Name = "name",
                LinePragma = new CodeLinePragma(),
                ReturnType = new CodeTypeReference("returnType"),
                PrivateImplementationType = new CodeTypeReference("privateImplementationType")
            };
            fullEntryPointMethod.Comments.Add(new CodeCommentStatement());
            fullEntryPointMethod.Comments.Add(new CodeCommentStatement("0"));
            fullEntryPointMethod.Comments.Add(new CodeCommentStatement("text"));
            fullEntryPointMethod.StartDirectives.Add(new CodeChecksumPragma());
            fullEntryPointMethod.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullEntryPointMethod.EndDirectives.Add(new CodeChecksumPragma());
            fullEntryPointMethod.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration());
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullEntryPointMethod.ImplementationTypes.Add(new CodeTypeReference((string)null));
            fullEntryPointMethod.ImplementationTypes.Add(new CodeTypeReference(string.Empty));
            fullEntryPointMethod.ImplementationTypes.Add(new CodeTypeReference("constraint1"));
            fullEntryPointMethod.ImplementationTypes.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration());
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullEntryPointMethod.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullEntryPointMethod.Statements.Add(new CodeMethodReturnStatement());
            fullEntryPointMethod.Statements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullEntryPointMethod.Parameters.Add(fullParameter);
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression());
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            fullEntryPointMethod.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute1);
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute2);
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute3);
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute4);
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute5);
            fullEntryPointMethod.Parameters.Add(invalidParameterAttribute6);
            fullEntryPointMethod.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullEntryPointMethod.TypeParameters.Add(fullTypeParameter);
            yield return new object[] { fullEntryPointMethod };

            // CodeConstructor.
            yield return new object[] { new CodeConstructor() };
            yield return new object[] { new CodeConstructor { Name = null } };
            yield return new object[] { new CodeConstructor { Name = string.Empty } };
            yield return new object[] { new CodeConstructor { Name = "0" } };
            yield return new object[] { new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference() } };
            yield return new object[] { new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };

            var fullConstructor = new CodeConstructor
            {
                Name = "name",
                LinePragma = new CodeLinePragma(),
                ReturnType = new CodeTypeReference("returnType"),
                PrivateImplementationType = new CodeTypeReference("privateImplementationType")
            };
            fullConstructor.Comments.Add(new CodeCommentStatement());
            fullConstructor.Comments.Add(new CodeCommentStatement("0"));
            fullConstructor.Comments.Add(new CodeCommentStatement("text"));
            fullConstructor.StartDirectives.Add(new CodeChecksumPragma());
            fullConstructor.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullConstructor.EndDirectives.Add(new CodeChecksumPragma());
            fullConstructor.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullConstructor.ImplementationTypes.Add(new CodeTypeReference((string)null));
            fullConstructor.ImplementationTypes.Add(new CodeTypeReference(string.Empty));
            fullConstructor.ImplementationTypes.Add(new CodeTypeReference("constraint1"));
            fullConstructor.ImplementationTypes.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration());
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullConstructor.Statements.Add(new CodeMethodReturnStatement());
            fullConstructor.Statements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullConstructor.Parameters.Add(fullParameter);
            fullConstructor.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullConstructor.TypeParameters.Add(fullTypeParameter);
            fullConstructor.BaseConstructorArgs.Add(new CodePrimitiveExpression(1));
            fullConstructor.ChainedConstructorArgs.Add(new CodePrimitiveExpression(1));
            yield return new object[] { fullConstructor };

            // CodeTypeConstructor.
            yield return new object[] { new CodeTypeConstructor() };
            yield return new object[] { new CodeTypeConstructor { Name = null } };
            yield return new object[] { new CodeTypeConstructor { Name = string.Empty } };
            yield return new object[] { new CodeTypeConstructor { Name = "name" } };
            yield return new object[] { new CodeTypeConstructor { Name = "0" } };
            yield return new object[] { new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference() } };
            yield return new object[] { new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };

            var abstractTypeConstructor = new CodeTypeConstructor { Name = "name", Attributes = MemberAttributes.Abstract };
            abstractTypeConstructor.Statements.Add(new CodeMethodReturnStatement());
            yield return new object[] { abstractTypeConstructor };

            var fullTypeConstructor = new CodeTypeConstructor
            {
                Name = "name",
                LinePragma = new CodeLinePragma(),
                ReturnType = new CodeTypeReference("returnType"),
                PrivateImplementationType = new CodeTypeReference("privateImplementationType")
            };
            fullTypeConstructor.Comments.Add(new CodeCommentStatement());
            fullTypeConstructor.Comments.Add(new CodeCommentStatement("0"));
            fullTypeConstructor.Comments.Add(new CodeCommentStatement("text"));
            fullTypeConstructor.StartDirectives.Add(new CodeChecksumPragma());
            fullTypeConstructor.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullTypeConstructor.EndDirectives.Add(new CodeChecksumPragma());
            fullTypeConstructor.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration());
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullTypeConstructor.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullTypeConstructor.ImplementationTypes.Add(new CodeTypeReference((string)null));
            fullTypeConstructor.ImplementationTypes.Add(new CodeTypeReference(string.Empty));
            fullTypeConstructor.ImplementationTypes.Add(new CodeTypeReference("constraint1"));
            fullTypeConstructor.ImplementationTypes.Add(new CodeTypeReference("constraint2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration());
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullTypeConstructor.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullTypeConstructor.Statements.Add(new CodeMethodReturnStatement());
            fullTypeConstructor.Statements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullTypeConstructor.Parameters.Add(fullParameter);
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression());
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            fullTypeConstructor.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute1);
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute2);
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute3);
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute4);
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute5);
            fullTypeConstructor.Parameters.Add(invalidParameterAttribute6);
            fullTypeConstructor.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullTypeConstructor.TypeParameters.Add(fullTypeParameter);
            yield return new object[] { fullTypeConstructor };

            // CodeMemberProperty.
            yield return new object[] { new CodeMemberProperty { Name = "name" } };
            yield return new object[] { new CodeMemberProperty { Name = "item" } };
            yield return new object[] { new CodeMemberProperty { Name = "Item" } };

            var abstractProperty = new CodeMemberProperty { Name = "name", Attributes = MemberAttributes.Abstract };
            abstractProperty.GetStatements.Add(new CodeStatement());
            abstractProperty.SetStatements.Add(new CodeStatement());
            yield return new object[] { abstractProperty };

            var fullItemPropertyUpper = new CodeMemberProperty
            {
                Name = "Item",
                PrivateImplementationType = new CodeTypeReference("implementationType")
            };
            fullItemPropertyUpper.Comments.Add(new CodeCommentStatement());
            fullItemPropertyUpper.Comments.Add(new CodeCommentStatement("0"));
            fullItemPropertyUpper.Comments.Add(new CodeCommentStatement("text"));
            fullItemPropertyUpper.StartDirectives.Add(new CodeChecksumPragma());
            fullItemPropertyUpper.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullItemPropertyUpper.EndDirectives.Add(new CodeChecksumPragma());
            fullItemPropertyUpper.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullItemPropertyUpper.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullItemPropertyUpper.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullItemPropertyUpper.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullItemPropertyUpper.GetStatements.Add(new CodeMethodReturnStatement());
            fullItemPropertyUpper.GetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullItemPropertyUpper.SetStatements.Add(new CodeMethodReturnStatement());
            fullItemPropertyUpper.SetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullItemPropertyUpper.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullItemPropertyUpper.Parameters.Add(fullParameter);
            yield return new object[] { fullItemPropertyUpper };

            var fullItemPropertyLower = new CodeMemberProperty
            {
                Name = "Item",
                PrivateImplementationType = new CodeTypeReference("implementationType")
            };
            fullItemPropertyLower.Comments.Add(new CodeCommentStatement());
            fullItemPropertyLower.Comments.Add(new CodeCommentStatement("0"));
            fullItemPropertyLower.Comments.Add(new CodeCommentStatement("text"));
            fullItemPropertyLower.StartDirectives.Add(new CodeChecksumPragma());
            fullItemPropertyLower.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullItemPropertyLower.EndDirectives.Add(new CodeChecksumPragma());
            fullItemPropertyLower.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullItemPropertyLower.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullItemPropertyLower.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullItemPropertyLower.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullItemPropertyLower.GetStatements.Add(new CodeMethodReturnStatement());
            fullItemPropertyLower.GetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullItemPropertyLower.SetStatements.Add(new CodeMethodReturnStatement());
            fullItemPropertyLower.SetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullItemPropertyLower.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullItemPropertyLower.Parameters.Add(fullParameter);
            yield return new object[] { fullItemPropertyLower };

            var fullProperty = new CodeMemberProperty
            {
                Name = "name",
                PrivateImplementationType = new CodeTypeReference("implementationType")
            };
            fullProperty.Comments.Add(new CodeCommentStatement());
            fullProperty.Comments.Add(new CodeCommentStatement("0"));
            fullProperty.Comments.Add(new CodeCommentStatement("text"));
            fullProperty.StartDirectives.Add(new CodeChecksumPragma());
            fullProperty.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullProperty.EndDirectives.Add(new CodeChecksumPragma());
            fullProperty.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullProperty.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullProperty.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullProperty.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullProperty.GetStatements.Add(new CodeMethodReturnStatement());
            fullProperty.GetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullProperty.SetStatements.Add(new CodeMethodReturnStatement());
            fullProperty.SetStatements.Add(new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() });
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullProperty.Parameters.Add(fullParameter);
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression());
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            fullProperty.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            fullProperty.Parameters.Add(invalidParameterAttribute1);
            fullProperty.Parameters.Add(invalidParameterAttribute2);
            fullProperty.Parameters.Add(invalidParameterAttribute3);
            fullProperty.Parameters.Add(invalidParameterAttribute4);
            fullProperty.Parameters.Add(invalidParameterAttribute5);
            fullProperty.Parameters.Add(invalidParameterAttribute6);
            yield return new object[] { fullProperty };

            // CodeSnippetTypeMember.
            yield return new object[] { new CodeSnippetTypeMember() };
            yield return new object[] { new CodeSnippetTypeMember(null) };
            yield return new object[] { new CodeSnippetTypeMember(string.Empty) };
            yield return new object[] { new CodeSnippetTypeMember("0") };
            yield return new object[] { new CodeSnippetTypeMember("text") };

            var fullSnippetTypeMember = new CodeSnippetTypeMember("text");
            fullSnippetTypeMember.Comments.Add(new CodeCommentStatement());
            fullSnippetTypeMember.Comments.Add(new CodeCommentStatement("0"));
            fullSnippetTypeMember.Comments.Add(new CodeCommentStatement("text"));
            fullSnippetTypeMember.StartDirectives.Add(new CodeChecksumPragma());
            fullSnippetTypeMember.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullSnippetTypeMember.EndDirectives.Add(new CodeChecksumPragma());
            fullSnippetTypeMember.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullSnippetTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullSnippetTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullSnippetTypeMember.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            yield return new object[] { fullSnippetTypeMember };

            // CodeTypeDeclaration.
            yield return new object[] { new CodeTypeDeclaration("name") };

            var interfaceTypeDeclaration = new CodeTypeDeclaration("name") { IsInterface = true };
            var interfaceMethod = new CodeMemberMethod { Name = "name" };
            interfaceMethod.Statements.Add(new CodeStatement());
            var interfaceProperty = new CodeMemberProperty { Name = "name", PrivateImplementationType = new CodeTypeReference("0") };
            interfaceProperty.GetStatements.Add(new CodeStatement());
            interfaceProperty.SetStatements.Add(new CodeStatement());
            interfaceTypeDeclaration.Members.Add(interfaceMethod);
            interfaceTypeDeclaration.Members.Add(interfaceProperty);
            yield return new object[] { interfaceTypeDeclaration };

            var fullTypeDeclaration = new CodeTypeDeclaration("name");
            fullTypeDeclaration.Comments.Add(new CodeCommentStatement());
            fullTypeDeclaration.Comments.Add(new CodeCommentStatement("0"));
            fullTypeDeclaration.Comments.Add(new CodeCommentStatement("text"));
            fullTypeDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullTypeDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeDeclaration.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullTypeDeclaration.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullTypeDeclaration.TypeParameters.Add(fullTypeParameter);
            fullTypeDeclaration.BaseTypes.Add(new CodeTypeReference((string)null));
            fullTypeDeclaration.BaseTypes.Add(new CodeTypeReference(string.Empty));
            fullTypeDeclaration.BaseTypes.Add(new CodeTypeReference("baseType1"));
            fullTypeDeclaration.BaseTypes.Add(new CodeTypeReference("baseType2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullTypeDeclaration.Members.Add(new CodeMemberEvent());
            fullTypeDeclaration.Members.Add(new CodeMemberEvent { Name = "0" });
            fullTypeDeclaration.Members.Add(new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(fullEvent);
            fullTypeDeclaration.Members.Add(new CodeMemberField(new CodeTypeReference("type"), "name"));
            fullTypeDeclaration.Members.Add(fullField);
            fullTypeDeclaration.Members.Add(new CodeMemberMethod { Name = "name" });
            fullTypeDeclaration.Members.Add(abstractMethod);
            fullTypeDeclaration.Members.Add(fullMethod);
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod());
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = null });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = string.Empty });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "name" });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "0" });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(abstractEntryPointMethod);
            fullTypeDeclaration.Members.Add(fullEntryPointMethod);
            fullTypeDeclaration.Members.Add(new CodeConstructor());
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = null });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = string.Empty });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = "0" });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(fullConstructor);
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor());
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = null });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = string.Empty });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "name" });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "0" });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullTypeDeclaration.Members.Add(new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullTypeDeclaration.Members.Add(abstractTypeConstructor);
            fullTypeDeclaration.Members.Add(fullTypeConstructor);
            fullTypeDeclaration.Members.Add(new CodeMemberProperty { Name = "name" } );
            fullTypeDeclaration.Members.Add(new CodeMemberProperty { Name = "item" } );
            fullTypeDeclaration.Members.Add(abstractProperty);
            fullTypeDeclaration.Members.Add(fullItemPropertyLower);
            fullTypeDeclaration.Members.Add(fullItemPropertyUpper);
            fullTypeDeclaration.Members.Add(fullProperty);
            fullTypeDeclaration.Members.Add(new CodeSnippetTypeMember());
            fullTypeDeclaration.Members.Add(new CodeSnippetTypeMember(null));
            fullTypeDeclaration.Members.Add(new CodeSnippetTypeMember(string.Empty));
            fullTypeDeclaration.Members.Add(new CodeSnippetTypeMember("text"));
            fullTypeDeclaration.Members.Add(fullSnippetTypeMember);
            yield return new object[] { fullTypeDeclaration };

            // CodeTypeDelegate.
            yield return new object[] { new CodeTypeDelegate("name") };
            
            var fullDelegate = new CodeTypeDelegate("name")
            {
                ReturnType = new CodeTypeReference("returnType")
            };
            fullDelegate.Comments.Add(new CodeCommentStatement());
            fullDelegate.Comments.Add(new CodeCommentStatement("0"));
            fullDelegate.Comments.Add(new CodeCommentStatement("text"));
            fullDelegate.CustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullDelegate.CustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullDelegate.CustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullDelegate.TypeParameters.Add(new CodeTypeParameter("parameter1"));
            fullDelegate.TypeParameters.Add(fullTypeParameter);
            fullDelegate.BaseTypes.Add(new CodeTypeReference((string)null));
            fullDelegate.BaseTypes.Add(new CodeTypeReference(string.Empty));
            fullDelegate.BaseTypes.Add(new CodeTypeReference("baseType1"));
            fullDelegate.BaseTypes.Add(new CodeTypeReference("baseType2`2", new CodeTypeReference("parameter1"), new CodeTypeReference("parameter2")));
            fullDelegate.Members.Add(new CodeMemberEvent());
            fullDelegate.Members.Add(new CodeMemberEvent { Name = "0" });
            fullDelegate.Members.Add(new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(fullEvent);
            fullDelegate.Members.Add(new CodeMemberField(new CodeTypeReference("type"), "name"));
            fullDelegate.Members.Add(fullField);
            fullDelegate.Members.Add(new CodeMemberMethod { Name = "name" });
            fullDelegate.Members.Add(abstractMethod);
            fullDelegate.Members.Add(fullMethod);
            fullDelegate.Members.Add(new CodeEntryPointMethod());
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = null });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = string.Empty });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "name" });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "0" });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeEntryPointMethod { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(abstractEntryPointMethod);
            fullDelegate.Members.Add(fullEntryPointMethod);
            fullDelegate.Members.Add(new CodeConstructor());
            fullDelegate.Members.Add(new CodeConstructor { Name = null });
            fullDelegate.Members.Add(new CodeConstructor { Name = string.Empty });
            fullDelegate.Members.Add(new CodeConstructor { Name = "0" });
            fullDelegate.Members.Add(new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(fullConstructor);
            fullDelegate.Members.Add(new CodeTypeConstructor());
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = null });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = string.Empty });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "name" });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "0" });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "name", ReturnType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference() });
            fullDelegate.Members.Add(new CodeTypeConstructor { Name = "name", PrivateImplementationType = new CodeTypeReference("0") });
            fullDelegate.Members.Add(abstractTypeConstructor);
            fullDelegate.Members.Add(fullTypeConstructor);
            fullDelegate.Members.Add(new CodeMemberProperty { Name = "name" } );
            fullDelegate.Members.Add(new CodeMemberProperty { Name = "item" } );
            fullDelegate.Members.Add(abstractProperty);
            fullDelegate.Members.Add(fullItemPropertyLower);
            fullDelegate.Members.Add(fullItemPropertyUpper);
            fullDelegate.Members.Add(fullProperty);
            fullDelegate.Members.Add(new CodeSnippetTypeMember());
            fullDelegate.Members.Add(new CodeSnippetTypeMember(null));
            fullDelegate.Members.Add(new CodeSnippetTypeMember(string.Empty));
            fullDelegate.Members.Add(new CodeSnippetTypeMember("text"));
            fullDelegate.Members.Add(fullSnippetTypeMember);
            fullDelegate.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name"));
            fullDelegate.Parameters.Add(fullParameter);
            yield return new object[] { fullDelegate };

            // CodeNamespace.
            yield return new object[] { new CodeNamespace() };
            yield return new object[] { new CodeNamespace(null) };
            yield return new object[] { new CodeNamespace(string.Empty) };
            yield return new object[] { new CodeNamespace("name") };

            var fullNamespace = new CodeNamespace("name");
            fullNamespace.Comments.Add(new CodeCommentStatement());
            fullNamespace.Comments.Add(new CodeCommentStatement("0"));
            fullNamespace.Comments.Add(new CodeCommentStatement("text"));
            fullNamespace.Imports.Add(new CodeNamespaceImport("nameSpace1"));
            fullNamespace.Imports.Add(fullNamespaceImport);
            fullNamespace.Types.Add(new CodeTypeDeclaration("name"));
            fullNamespace.Types.Add(interfaceTypeDeclaration);
            fullNamespace.Types.Add(fullTypeDeclaration);
            yield return new object[] { fullNamespace };

            // CodeCompileUnit.
            yield return new object[] { new CodeCompileUnit() };

            var fullCompileUnit = new CodeCompileUnit();
            fullCompileUnit.StartDirectives.Add(new CodeChecksumPragma());
            fullCompileUnit.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullCompileUnit.EndDirectives.Add(new CodeChecksumPragma());
            fullCompileUnit.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullCompileUnit.Namespaces.Add(new CodeNamespace());
            fullCompileUnit.Namespaces.Add(new CodeNamespace(null));
            fullCompileUnit.Namespaces.Add(new CodeNamespace(string.Empty));
            fullCompileUnit.Namespaces.Add(fullNamespace);
            fullCompileUnit.ReferencedAssemblies.Add("");
            fullCompileUnit.ReferencedAssemblies.Add("0");
            fullCompileUnit.ReferencedAssemblies.Add("assembly");
            yield return new object[] { fullCompileUnit };

            // CodeSnippetCompileUnit.
            yield return new object[] { new CodeSnippetCompileUnit() };
            yield return new object[] { new CodeSnippetCompileUnit(null) };
            yield return new object[] { new CodeSnippetCompileUnit("") };
            yield return new object[] { new CodeSnippetCompileUnit("0") };
            yield return new object[] { new CodeSnippetCompileUnit("value") };

            var fullSnippetCompileUnit = new CodeSnippetCompileUnit("value") { LinePragma = new CodeLinePragma() };
            fullSnippetCompileUnit.StartDirectives.Add(new CodeChecksumPragma());
            fullSnippetCompileUnit.StartDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullSnippetCompileUnit.EndDirectives.Add(new CodeChecksumPragma());
            fullSnippetCompileUnit.EndDirectives.Add(new CodeChecksumPragma("fileName", Guid.NewGuid(), new byte[0]));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute1"));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute2", new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("attribute3", new CodeAttributeArgument(null, new CodePrimitiveExpression(1)), new CodeAttributeArgument(string.Empty, new CodePrimitiveExpression(1)), new CodeAttributeArgument("name", new CodePrimitiveExpression(1))));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration());
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            fullSnippetCompileUnit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            fullSnippetCompileUnit.Namespaces.Add(new CodeNamespace());
            fullSnippetCompileUnit.Namespaces.Add(new CodeNamespace(null));
            fullSnippetCompileUnit.Namespaces.Add(new CodeNamespace(string.Empty));
            fullSnippetCompileUnit.Namespaces.Add(new CodeNamespace("0"));
            fullSnippetCompileUnit.Namespaces.Add(fullNamespace);
            fullSnippetCompileUnit.ReferencedAssemblies.Add("");
            fullSnippetCompileUnit.ReferencedAssemblies.Add("0");
            fullSnippetCompileUnit.ReferencedAssemblies.Add("assembly");
            yield return new object[] { fullSnippetCompileUnit };

            // CodeTypeReference.
            yield return new object[] { new CodeTypeReference((string)null) };
            yield return new object[] { new CodeTypeReference(string.Empty) };
            yield return new object[] { new CodeTypeReference("name") };
            yield return new object[] { new CodeTypeReference("name`") };
            yield return new object[] { new CodeTypeReference("name`1") };
            yield return new object[] { new CodeTypeReference("name`2[]") };

            var fullTypeReference = new CodeTypeReference("name`2");
            fullTypeReference.TypeArguments.Add("type1");
            fullTypeReference.TypeArguments.Add("type2");
            yield return new object[] { fullTypeReference };

            // CodeArrayCreateExpression.
            yield return new object[] { new CodeArrayCreateExpression() };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference((string)null)) };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference(string.Empty)) };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name")) };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name")) { SizeExpression = new CodePrimitiveExpression(1) } }  ;
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name"), new CodeExpression[] { new CodePrimitiveExpression() }) { SizeExpression = new CodePrimitiveExpression(1) } };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name"), new CodeExpression[] { new CodePrimitiveExpression(1) }) };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name"), new CodeExpression[] { new CodePrimitiveExpression(1) }) { SizeExpression = new CodeExpression() } };

            // CodeBaseReferenceExpression.
            yield return new object[] { new CodeBaseReferenceExpression() };

            // CodeBinaryOperatorExpression.
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)) };

            // CodeCastExpression.
            yield return new object[] { new CodeCastExpression(new CodeTypeReference((string)null), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference(string.Empty), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1)) };

            // CodeDefaultValueExpression.
            yield return new object[] { new CodeDefaultValueExpression() };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference((string)null)) };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference(string.Empty)) };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference("name")) };

            // CodeDelegateCreateExpression.
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1), "methodName") };

            // CodeFieldReferenceExpression.
            yield return new object[] { new CodeFieldReferenceExpression(null, "name") };
            yield return new object[] { new CodeFieldReferenceExpression(new CodePrimitiveExpression(1), "name") };

            // CodeArgumentReferenceExpression.
            yield return new object[] { new CodeArgumentReferenceExpression("name") };

            // CodeVariableReferenceExpression.
            yield return new object[] { new CodeVariableReferenceExpression("name") };

            // CodeIndexerExpression.
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)) };

            // CodeArrayIndexerExpression.
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)) };

            // CodeSnippetExpression.
            yield return new object[] { new CodeSnippetExpression() };
            yield return new object[] { new CodeSnippetExpression(null) };
            yield return new object[] { new CodeSnippetExpression(string.Empty) };
            yield return new object[] { new CodeSnippetExpression("0") };
            yield return new object[] { new CodeSnippetExpression("name") };

            // CodeMethodInvokeExpression.
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "name")) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name")) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference((string)null), new CodeTypeReference(string.Empty), new CodeTypeReference("name") }), new CodePrimitiveExpression(1)) };

            // CodeMethodReferenceExpression.
            yield return new object[] { new CodeMethodReferenceExpression(null, "name") };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference((string)null), new CodeTypeReference(string.Empty), new CodeTypeReference("name") }) };

            // CodeEventReferenceExpression.
            yield return new object[] { new CodeEventReferenceExpression(null, "name") };
            yield return new object[] { new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "name") };

            // CodeDelegateInvokeExpression.
            yield return new object[] { new CodeDelegateInvokeExpression() };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1), new CodePrimitiveExpression(2)) };

            // CodeObjectCreateExpression.
            yield return new object[] { new CodeObjectCreateExpression() };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("name")) };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1)) };

            // CodeDirectionExpression.
            yield return new object[] { new CodeDirectionExpression(FieldDirection.In, new CodePrimitiveExpression(1)) };

            // CodePrimitiveExpression.
            yield return new object[] { new CodePrimitiveExpression() };
            yield return new object[] { new CodePrimitiveExpression(1) };
            yield return new object[] { new CodePrimitiveExpression(null) };
            yield return new object[] { new CodePrimitiveExpression(string.Empty) };
            yield return new object[] { new CodePrimitiveExpression("0") };
            yield return new object[] { new CodePrimitiveExpression("name") };

            // CodePropertyReferenceExpression.
            yield return new object[] { new CodePropertyReferenceExpression(null, "name") };
            yield return new object[] { new CodePropertyReferenceExpression(new CodePrimitiveExpression(1), "name") };

            // CodePropertySetValueReferenceExpression.
            yield return new object[] { new CodePropertySetValueReferenceExpression() };

            // CodeThisReferenceExpression.
            yield return new object[] { new CodeThisReferenceExpression() };

            // CodeTypeReferenceExpression.
            yield return new object[] { new CodeTypeReferenceExpression() };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("name")) };

            // CodeTypeOfExpression.
            yield return new object[] { new CodeTypeOfExpression() };
            yield return new object[] { new CodeTypeOfExpression(new CodeTypeReference("name")) };

            // CodeMethodReturnStatement.
            yield return new object[] { new CodeMethodReturnStatement() };
            yield return new object[] { new CodeMethodReturnStatement(null) };
            yield return new object[] { new CodeMethodReturnStatement(new CodePrimitiveExpression("1")) };

            // CodeConditionStatement.
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1")) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1"), new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }, new CodeStatement[0]) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1"), new CodeStatement[0], new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1"), new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }, new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }) };

            // CodeTryCatchFinallyStatement.
            yield return new object[] { new CodeTryCatchFinallyStatement() };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } },
                    new CodeCatchClause[] { new CodeCatchClause("localName"), new CodeCatchClause("localName", new CodeTypeReference("exceptionType")), new CodeCatchClause("localName", new CodeTypeReference("exceptionType"), new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() }) },
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[0],
                    new CodeCatchClause[] { new CodeCatchClause("localName"), new CodeCatchClause("localName", new CodeTypeReference("exceptionType")), new CodeCatchClause("localName", new CodeTypeReference("exceptionType"), new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() }) },
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } },
                    new CodeCatchClause[0],
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } },
                    new CodeCatchClause[] { new CodeCatchClause("localName"), new CodeCatchClause("localName", new CodeTypeReference("exceptionType")), new CodeCatchClause("localName", new CodeTypeReference("exceptionType"), new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() }) },
                    new CodeStatement[0]
                )
            };

            // CodeAssignStatement.
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), new CodePrimitiveExpression(1)) };

            // CodeExpressionStatement.
            yield return new object[] { new CodeExpressionStatement(new CodePrimitiveExpression("1")) };

            // CodeIterationStatement.
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodePrimitiveExpression(1), new CodeMethodReturnStatement(), new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() }) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodePrimitiveExpression(1), new CodeMethodReturnStatement()) };

            // CodeThrowExceptionStatement.
            yield return new object[] { new CodeThrowExceptionStatement() };
            yield return new object[] { new CodeThrowExceptionStatement(null) };
            yield return new object[] { new CodeThrowExceptionStatement(new CodePrimitiveExpression(1)) };

            // CodeSnippetStatement.
            yield return new object[] { new CodeSnippetStatement() };
            yield return new object[] { new CodeSnippetStatement(null) };
            yield return new object[] { new CodeSnippetStatement(string.Empty) };
            yield return new object[] { new CodeSnippetStatement("value") };

            // CodeVariableDeclarationStatement.
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), "name", new CodePrimitiveExpression(1)) };

            // CodeAttachEventStatement.
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "name"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "name"), new CodePrimitiveExpression(1)) };

            // CodeRemoveEventStatement.
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "name"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "name"), new CodePrimitiveExpression(1)) };
            
            // CodeGotoStatement.
            yield return new object[] { new CodeGotoStatement("name") };

            // CodeLabeledStatement.
            yield return new object[] { new CodeLabeledStatement("name") };
            yield return new object[] { new CodeLabeledStatement("name", null) };
            yield return new object[] { new CodeLabeledStatement("name", new CodeMethodReturnStatement()) };
        
            // Misc.
            yield return new object[] { new CodeTypeReference(":") };
            yield return new object[] { new CodeTypeReference(".") };
            yield return new object[] { new CodeTypeReference("$") };
            yield return new object[] { new CodeTypeReference("+") };
            yield return new object[] { new CodeTypeReference("<") };
            yield return new object[] { new CodeTypeReference(">") };
            yield return new object[] { new CodeTypeReference("-") };
            yield return new object[] { new CodeTypeReference("[") };
            yield return new object[] { new CodeTypeReference("]") };
            yield return new object[] { new CodeTypeReference(",") };
            yield return new object[] { new CodeTypeReference("&") };
            yield return new object[] { new CodeTypeReference("*") };
            yield return new object[] { new CodeTypeReference("_abc") };
        }

        [Theory]
        [MemberData(nameof(ValidateIdentifiers_Valid_TestData))]
        public void ValidateIdentifiers_InvokeValid_Nop(CodeObject e)
        {
            CodeGenerator.ValidateIdentifiers(e);
        }

        public static IEnumerable<object[]> ValidateIdentifiers_Invalid_TestData()
        {
            // CodeTypeReference.
            yield return new object[] { new CodeTypeReference() };
            yield return new object[] { new CodeTypeReference("0") };

            var invalidTypeReference1 = new CodeTypeReference("name`2");
            invalidTypeReference1.TypeArguments.Add("type1");
            yield return new object[] { invalidTypeReference1 };

            var invalidTypeReference2 = new CodeTypeReference("name`2");
            invalidTypeReference2.TypeArguments.Add(new CodeTypeReference());
            invalidTypeReference2.TypeArguments.Add("name");
            yield return new object[] { invalidTypeReference2 };

            var invalidTypeReference3 = new CodeTypeReference("name`2");
            invalidTypeReference3.TypeArguments.Add(new CodeTypeReference("0"));
            invalidTypeReference3.TypeArguments.Add("name");
            yield return new object[] { invalidTypeReference3 };

            // CodeChecksumPragma.
            yield return new object[] { new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]) };

            // CodeRegionDirective.
            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                yield return new object[] { new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt") };
            }

            // CodeNamespaceImport.
            yield return new object[] { new CodeNamespaceImport() };
            yield return new object[] { new CodeNamespaceImport(null) };
            yield return new object[] { new CodeNamespaceImport(string.Empty) };
            yield return new object[] { new CodeNamespaceImport("0") };

            var invalidNamespaceImport1 = new CodeNamespace();
            invalidNamespaceImport1.Imports.Add(new CodeNamespaceImport());
            yield return new object[] { invalidNamespaceImport1 };

            var invalidNamespaceImport2 = new CodeNamespace();
            invalidNamespaceImport2.Imports.Add(new CodeNamespaceImport(string.Empty));
            yield return new object[] { invalidNamespaceImport2 };

            var invalidNamespaceImport3 = new CodeNamespace();
            invalidNamespaceImport3.Imports.Add(new CodeNamespaceImport(string.Empty));
            yield return new object[] { invalidNamespaceImport3 };

            // CodeMemberEvent.
            yield return new object[] { new CodeMemberEvent { PrivateImplementationType = new CodeTypeReference("name") } };
            yield return new object[] { new CodeMemberEvent { Name = null, PrivateImplementationType = new CodeTypeReference("name") } };
            yield return new object[] { new CodeMemberEvent { Name = string.Empty, PrivateImplementationType = new CodeTypeReference("name") } };
            yield return new object[] { new CodeMemberEvent { Name = "0", PrivateImplementationType = new CodeTypeReference("name") } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("name"), Type = new CodeTypeReference() } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("name"), Type = new CodeTypeReference("0") } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("name"), Type = invalidTypeReference1 } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("name"), Type = invalidTypeReference2 } };
            yield return new object[] { new CodeMemberEvent { Name = "name", PrivateImplementationType = new CodeTypeReference("name"), Type = invalidTypeReference3 } };

            var invalidEventStartDirective1 = new CodeMemberEvent();
            invalidEventStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidEventStartDirective1 };

            var invalidEventStartDirective2 = new CodeMemberEvent();
            invalidEventStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidEventStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidEventStartDirective3 = new CodeMemberEvent();
                invalidEventStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidEventStartDirective3 };
            }

            var invalidEventEndDirective1 = new CodeMemberEvent();
            invalidEventEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidEventEndDirective1 };

            var invalidEventEndDirective2 = new CodeMemberEvent();
            invalidEventEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidEventEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidEventEndDirective3 = new CodeMemberEvent();
                invalidEventEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidEventEndDirective3 };
            }

            var invalidEventImplementationType1 = new CodeMemberEvent();
            invalidEventImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidEventImplementationType1 };

            var invalidEventImplementationType2 = new CodeMemberEvent();
            invalidEventImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidEventImplementationType2 };

            var invalidEventImplementationType3 = new CodeMemberEvent();
            invalidEventImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidEventImplementationType3 };

            var invalidEventImplementationType4 = new CodeMemberEvent();
            invalidEventImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidEventImplementationType4 };

            var invalidEventImplementationType5 = new CodeMemberEvent();
            invalidEventImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidEventImplementationType5 };

            // CodeMemberField.
            yield return new object[] { new CodeMemberField() };
            yield return new object[] { new CodeMemberField(new CodeTypeReference(), "name") };
            yield return new object[] { new CodeMemberField(new CodeTypeReference("0"), "name") };
            yield return new object[] { new CodeMemberField(invalidTypeReference1, "name") };
            yield return new object[] { new CodeMemberField(invalidTypeReference2, "name") };
            yield return new object[] { new CodeMemberField(invalidTypeReference3, "name") };
            yield return new object[] { new CodeMemberField(new CodeTypeReference("type"), null) };
            yield return new object[] { new CodeMemberField(new CodeTypeReference("type"), string.Empty) };
            yield return new object[] { new CodeMemberField(new CodeTypeReference("type"), "0") };

            var invalidFieldAttribute1 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidFieldAttribute1 };

            var invalidFieldAttribute2 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidFieldAttribute2 };

            var invalidFieldAttribute3 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidFieldAttribute3 };

            var invalidFieldAttribute4 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidFieldAttribute4 };

            var invalidFieldAttribute5 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidFieldAttribute5 };

            var invalidFieldAttribute6 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidFieldAttribute6 };

            var invalidFieldStartDirective1 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidFieldStartDirective1 };

            var invalidFieldStartDirective2 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidFieldStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidFieldStartDirective3 = new CodeMemberField(new CodeTypeReference("type"), "name");
                invalidFieldStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidFieldStartDirective3 };
            }

            var invalidFieldEndDirective1 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidFieldEndDirective1 };

            var invalidFieldEndDirective2 = new CodeMemberField(new CodeTypeReference("type"), "name");
            invalidFieldEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidFieldEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidFieldEndDirective3 = new CodeMemberField(new CodeTypeReference("type"), "name");
                invalidFieldEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidFieldEndDirective3 };
            }

            yield return new object[] { new CodeMemberField(new CodeTypeReference("type"), "name") { InitExpression = new CodeExpression() } };

            // CodeParameterDeclarationExpression.
            yield return new object[] { new CodeParameterDeclarationExpression() };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference(), "name") };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name") };
            yield return new object[] { new CodeParameterDeclarationExpression(invalidTypeReference1, "name") };
            yield return new object[] { new CodeParameterDeclarationExpression(invalidTypeReference2, "name") };
            yield return new object[] { new CodeParameterDeclarationExpression(invalidTypeReference3, "name") };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null) };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty) };
            yield return new object[] { new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0") };

            var invalidParameterAttribute1 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidParameterAttribute1 };

            var invalidParameterAttribute2 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidParameterAttribute2 };

            var invalidParameterAttribute3 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidParameterAttribute3 };

            var invalidParameterAttribute4 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidParameterAttribute4 };

            var invalidParameterAttribute5 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidParameterAttribute5 };

            var invalidParameterAttribute6 = new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "name");
            invalidParameterAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidParameterAttribute6 };

            // CodeMemberMethod.
            yield return new object[] { new CodeMemberMethod() };
            yield return new object[] { new CodeMemberMethod { Name = null } };
            yield return new object[] { new CodeMemberMethod { Name = string.Empty } };
            yield return new object[] { new CodeMemberMethod { Name = "0" } };
            yield return new object[] { new CodeMemberMethod { Name = "name", ReturnType = new CodeTypeReference() } };
            yield return new object[] { new CodeMemberMethod { Name = "name", ReturnType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeMemberMethod { Name = "name", ReturnType = invalidTypeReference1 } };
            yield return new object[] { new CodeMemberMethod { Name = "name", ReturnType = invalidTypeReference2 } };
            yield return new object[] { new CodeMemberMethod { Name = "name", ReturnType = invalidTypeReference3 } };
            yield return new object[] { new CodeMemberMethod { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeMemberMethod { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeMemberMethod { Name = "name", PrivateImplementationType = invalidTypeReference1 } };
            yield return new object[] { new CodeMemberMethod { Name = "name", PrivateImplementationType = invalidTypeReference2 } };
            yield return new object[] { new CodeMemberMethod { Name = "name", PrivateImplementationType = invalidTypeReference3 } };

            var invalidMethodAttribute1 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidMethodAttribute1 };

            var invalidMethodAttribute2 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidMethodAttribute2 };

            var invalidMethodAttribute3 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidMethodAttribute3 };

            var invalidMethodAttribute4 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidMethodAttribute4 };

            var invalidMethodAttribute5 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidMethodAttribute5 };

            var invalidMethodAttribute6 = new CodeMemberMethod { Name = "name" };
            invalidMethodAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidMethodAttribute6 };

            var invalidMethodStartDirective1 = new CodeMemberMethod { Name = "name" };
            invalidMethodStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidMethodStartDirective1 };

            var invalidMethodStartDirective2 = new CodeMemberMethod { Name = "name" };
            invalidMethodStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidMethodStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidMethodStartDirective3 = new CodeMemberMethod { Name = "name" };
                invalidMethodStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidMethodStartDirective3 };
            }

            var invalidMethodEndDirective1 = new CodeMemberMethod { Name = "name" };
            invalidMethodEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidMethodEndDirective1 };

            var invalidMethodEndDirective2 = new CodeMemberMethod { Name = "name" };
            invalidMethodEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidMethodEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidMethodEndDirective3 = new CodeMemberMethod { Name = "name" };
                invalidMethodEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidMethodEndDirective3 };
            }

            var invalidMethodImplementationType1 = new CodeMemberMethod { Name = "name" };
            invalidMethodImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidMethodImplementationType1 };

            var invalidMethodImplementationType2 = new CodeMemberMethod { Name = "name" };
            invalidMethodImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidMethodImplementationType2 };

            var invalidMethodImplementationType3 = new CodeMemberMethod { Name = "name" };
            invalidMethodImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidMethodImplementationType3 };

            var invalidMethodImplementationType4 = new CodeMemberMethod { Name = "name" };
            invalidMethodImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidMethodImplementationType4 };

            var invalidMethodImplementationType5 = new CodeMemberMethod { Name = "name" };
            invalidMethodImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidMethodImplementationType5 };

            var invalidMethodReturnTypeAttribute1 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute1.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidMethodReturnTypeAttribute1 };

            var invalidMethodReturnTypeAttribute2 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute2.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidMethodReturnTypeAttribute2 };

            var invalidMethodReturnTypeAttribute3 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute3.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidMethodReturnTypeAttribute3 };

            var invalidMethodReturnTypeAttribute4 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute4.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidMethodReturnTypeAttribute4 };

            var invalidMethodReturnTypeAttribute5 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute5.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidMethodReturnTypeAttribute5 };

            var invalidMethodReturnTypeAttribute6 = new CodeMemberMethod { Name = "name" };
            invalidMethodReturnTypeAttribute6.ReturnTypeCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidMethodReturnTypeAttribute6 };

            var invalidMethodStatement = new CodeMemberMethod { Name = "name" };
            invalidMethodStatement.Statements.Add(new CodeStatement());
            yield return new object[] { invalidMethodStatement };

            var invalidMethodParameter1 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter1.Parameters.Add(new CodeParameterDeclarationExpression());
            yield return new object[] { invalidMethodParameter1 };

            var invalidMethodParameter2 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            yield return new object[] { invalidMethodParameter2 };

            var invalidMethodParameter3 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter3.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            yield return new object[] { invalidMethodParameter3 };

            var invalidMethodParameter4 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter4.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference1, "name"));
            yield return new object[] { invalidMethodParameter4 };

            var invalidMethodParameter5 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter5.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference2, "name"));
            yield return new object[] { invalidMethodParameter5 };

            var invalidMethodParameter6 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter6.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference3, "name"));
            yield return new object[] { invalidMethodParameter6 };

            var invalidMethodParameter7 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter7.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            yield return new object[] { invalidMethodParameter7 };

            var invalidMethodParameter8 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter8.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            yield return new object[] { invalidMethodParameter8 };

            var invalidMethodParameter9 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameter9.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            yield return new object[] { invalidMethodParameter9 };

            var invalidMethodParameterAttribute1 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute1.Parameters.Add(invalidParameterAttribute1);
            yield return new object[] { invalidMethodParameterAttribute1 };

            var invalidMethodParameterAttribute2 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute2.Parameters.Add(invalidParameterAttribute2);
            yield return new object[] { invalidMethodParameterAttribute2 };

            var invalidMethodParameterAttribute3 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute3.Parameters.Add(invalidParameterAttribute3);
            yield return new object[] { invalidMethodParameterAttribute3 };

            var invalidMethodParameterAttribute4 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute4.Parameters.Add(invalidParameterAttribute4);
            yield return new object[] { invalidMethodParameterAttribute4 };

            var invalidMethodParameterAttribute5 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute5.Parameters.Add(invalidParameterAttribute5);
            yield return new object[] { invalidMethodParameterAttribute5 };

            var invalidMethodParameterAttribute6 = new CodeMemberMethod { Name = "name" };
            invalidMethodParameterAttribute6.Parameters.Add(invalidParameterAttribute6);
            yield return new object[] { invalidMethodParameterAttribute6 };

            var invalidMethodTypeParameter1 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidMethodTypeParameter1 };

            var invalidMethodTypeParameter2 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidMethodTypeParameter2 };

            var invalidMethodTypeParameter3 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidMethodTypeParameter3 };

            var invalidMethodTypeParameter4 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidMethodTypeParameter4 };

            var invalidTypeParameterAttribute1 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            var invalidMethodTypeParameterAttribute1 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidMethodTypeParameterAttribute1 };

            var invalidTypeParameterAttribute2 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            var invalidMethodTypeParameterAttribute2 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidMethodTypeParameterAttribute2 };

            var invalidTypeParameterAttribute3 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            var invalidMethodTypeParameterAttribute3 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidMethodTypeParameterAttribute3 };

            var invalidTypeParameterAttribute4 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            var invalidMethodTypeParameterAttribute4 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidMethodTypeParameterAttribute4 };

            var invalidTypeParameterAttribute5 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("attribute", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            var invalidMethodTypeParameterAttribute5 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidMethodTypeParameterAttribute5 };

            var invalidTypeParameterAttribute6 = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("attribute", new CodeAttributeArgument("ARG", new CodeExpression())));
            var invalidMethodTypeParameterAttribute6 = new CodeMemberMethod { Name = "name" };
            invalidMethodTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidMethodTypeParameterAttribute6 };

            // CodeEntryPointMethod.
            var invalidEntryPointMethodStartDirective1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidEntryPointMethodStartDirective1 };

            var invalidEntryPointMethodStartDirective2 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidEntryPointMethodStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidEntryPointMethodStartDirective3 = new CodeEntryPointMethod { Name = "name" };
                invalidEntryPointMethodStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidEntryPointMethodStartDirective3 };
            }

            var invalidEntryPointMethodEndDirective1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidEntryPointMethodEndDirective1 };

            var invalidEntryPointMethodEndDirective2 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidEntryPointMethodEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidEntryPointMethodEndDirective3 = new CodeEntryPointMethod { Name = "name" };
                invalidEntryPointMethodEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidEntryPointMethodEndDirective3 };
            }

            var invalidEntryPointMethodImplementationType1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidEntryPointMethodImplementationType1 };

            var invalidEntryPointMethodImplementationType2 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidEntryPointMethodImplementationType2 };

            var invalidEntryPointMethodImplementationType3 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidEntryPointMethodImplementationType3 };

            var invalidEntryPointMethodImplementationType4 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidEntryPointMethodImplementationType4 };

            var invalidEntryPointMethodImplementationType5 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidEntryPointMethodImplementationType5 };

            var invalidEntryPointMethodStatement1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodStatement1.Statements.Add(new CodeStatement());
            yield return new object[] { invalidEntryPointMethodStatement1 };

            var invalidEntryPointMethodStatement2 = new CodeEntryPointMethod { Name = "name", Attributes = MemberAttributes.Abstract };
            invalidEntryPointMethodStatement2.Statements.Add(new CodeStatement());
            yield return new object[] { invalidEntryPointMethodStatement2 };

            var invalidEntryPointMethodTypeParameter1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidEntryPointMethodTypeParameter1 };

            var invalidEntryPointMethodTypeParameter2 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidEntryPointMethodTypeParameter2 };

            var invalidEntryPointMethodTypeParameter3 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidEntryPointMethodTypeParameter3 };

            var invalidEntryPointMethodTypeParameter4 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidEntryPointMethodTypeParameter4 };

            var invalidEntryPointMethodTypeParameterAttribute1 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute1 };

            var invalidEntryPointMethodTypeParameterAttribute2 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute2 };

            var invalidEntryPointMethodTypeParameterAttribute3 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute3 };

            var invalidEntryPointMethodTypeParameterAttribute4 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute4 };

            var invalidEntryPointMethodTypeParameterAttribute5 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute5 };

            var invalidEntryPointMethodTypeParameterAttribute6 = new CodeEntryPointMethod { Name = "name" };
            invalidEntryPointMethodTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidEntryPointMethodTypeParameterAttribute6 };
    
            // CodeConstructor.
            var invalidConstructorAttribute1 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidConstructorAttribute1 };

            var invalidConstructorAttribute2 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidConstructorAttribute2 };

            var invalidConstructorAttribute3 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidConstructorAttribute3 };

            var invalidConstructorAttribute4 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidConstructorAttribute4 };

            var invalidConstructorAttribute5 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidConstructorAttribute5 };

            var invalidConstructorAttribute6 = new CodeConstructor { Name = "name" };
            invalidConstructorAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidConstructorAttribute6 };

            var invalidConstructorStartDirective1 = new CodeConstructor { Name = "name" };
            invalidConstructorStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidConstructorStartDirective1 };

            var invalidConstructorStartDirective2 = new CodeConstructor { Name = "name" };
            invalidConstructorStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidConstructorStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidConstructorStartDirective3 = new CodeConstructor { Name = "name" };
                invalidConstructorStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidConstructorStartDirective3 };
            }

            var invalidConstructorEndDirective1 = new CodeConstructor { Name = "name" };
            invalidConstructorEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidConstructorEndDirective1 };

            var invalidConstructorEndDirective2 = new CodeConstructor { Name = "name" };
            invalidConstructorEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidConstructorEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidConstructorEndDirective3 = new CodeConstructor { Name = "name" };
                invalidConstructorEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidConstructorEndDirective3 };
            }

            var invalidConstructorImplementationType1 = new CodeConstructor { Name = "name" };
            invalidConstructorImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidConstructorImplementationType1 };

            var invalidConstructorImplementationType2 = new CodeConstructor { Name = "name" };
            invalidConstructorImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidConstructorImplementationType2 };

            var invalidConstructorImplementationType3 = new CodeConstructor { Name = "name" };
            invalidConstructorImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidConstructorImplementationType3 };

            var invalidConstructorImplementationType4 = new CodeConstructor { Name = "name" };
            invalidConstructorImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidConstructorImplementationType4 };

            var invalidConstructorImplementationType5 = new CodeConstructor { Name = "name" };
            invalidConstructorImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidConstructorImplementationType5 };

            var invalidConstructorStatement1 = new CodeConstructor { Name = "name" };
            invalidConstructorStatement1.Statements.Add(new CodeStatement());
            yield return new object[] { invalidConstructorStatement1 };

            var invalidConstructorStatement2 = new CodeConstructor { Name = "name", Attributes = MemberAttributes.Abstract };
            invalidConstructorStatement2.Statements.Add(new CodeStatement());
            yield return new object[] { invalidConstructorStatement2 };

            var invalidConstructorParameter1 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter1.Parameters.Add(new CodeParameterDeclarationExpression());
            yield return new object[] { invalidConstructorParameter1 };

            var invalidConstructorParameter2 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            yield return new object[] { invalidConstructorParameter2 };

            var invalidConstructorParameter3 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter3.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            yield return new object[] { invalidConstructorParameter3 };

            var invalidConstructorParameter4 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter4.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            yield return new object[] { invalidConstructorParameter4 };

            var invalidConstructorParameter5 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter5.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            yield return new object[] { invalidConstructorParameter5 };

            var invalidConstructorParameter6 = new CodeConstructor { Name = "name" };
            invalidConstructorParameter6.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            yield return new object[] { invalidConstructorParameter6 };

            var invalidConstructorParameterAttribute1 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute1.Parameters.Add(invalidParameterAttribute1);
            yield return new object[] { invalidConstructorParameterAttribute1 };

            var invalidConstructorParameterAttribute2 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute2.Parameters.Add(invalidParameterAttribute2);
            yield return new object[] { invalidConstructorParameterAttribute2 };

            var invalidConstructorParameterAttribute3 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute3.Parameters.Add(invalidParameterAttribute3);
            yield return new object[] { invalidConstructorParameterAttribute3 };

            var invalidConstructorParameterAttribute4 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute4.Parameters.Add(invalidParameterAttribute4);
            yield return new object[] { invalidConstructorParameterAttribute4 };

            var invalidConstructorParameterAttribute5 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute5.Parameters.Add(invalidParameterAttribute5);
            yield return new object[] { invalidConstructorParameterAttribute5 };

            var invalidConstructorParameterAttribute6 = new CodeConstructor { Name = "name" };
            invalidConstructorParameterAttribute6.Parameters.Add(invalidParameterAttribute6);
            yield return new object[] { invalidConstructorParameterAttribute6 };

            var invalidConstructorTypeParameter1 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidConstructorTypeParameter1 };

            var invalidConstructorTypeParameter2 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidConstructorTypeParameter2 };

            var invalidConstructorTypeParameter3 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidConstructorTypeParameter3 };

            var invalidConstructorTypeParameter4 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidConstructorTypeParameter4 };

            var invalidConstructorTypeParameterAttribute1 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidConstructorTypeParameterAttribute1 };

            var invalidConstructorTypeParameterAttribute2 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidConstructorTypeParameterAttribute2 };

            var invalidConstructorTypeParameterAttribute3 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidConstructorTypeParameterAttribute3 };

            var invalidConstructorTypeParameterAttribute4 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidConstructorTypeParameterAttribute4 };

            var invalidConstructorTypeParameterAttribute5 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidConstructorTypeParameterAttribute5 };

            var invalidConstructorTypeParameterAttribute6 = new CodeConstructor { Name = "name" };
            invalidConstructorTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidConstructorTypeParameterAttribute6 };

            var invalidConstructorBaseConstructorArg = new CodeConstructor { Name = "name" };
            invalidConstructorBaseConstructorArg.BaseConstructorArgs.Add(new CodeExpression());
            yield return new object[] { invalidConstructorBaseConstructorArg };

            var invalidConstructorChainedConstructorArg = new CodeConstructor { Name = "name" };
            invalidConstructorChainedConstructorArg.ChainedConstructorArgs.Add(new CodeExpression());
            yield return new object[] { invalidConstructorChainedConstructorArg };

            // CodeTypeConstructor.
            var invalidTypeConstructorStartDirective1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidTypeConstructorStartDirective1 };

            var invalidTypeConstructorStartDirective2 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidTypeConstructorStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidTypeConstructorStartDirective3 = new CodeTypeConstructor { Name = "name" };
                invalidTypeConstructorStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidTypeConstructorStartDirective3 };
            }

            var invalidTypeConstructorEndDirective1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidTypeConstructorEndDirective1 };

            var invalidTypeConstructorEndDirective2 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidTypeConstructorEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidTypeConstructorEndDirective3 = new CodeTypeConstructor { Name = "name" };
                invalidTypeConstructorEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidTypeConstructorEndDirective3 };
            }

            var invalidTypeConstructorImplementationType1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidTypeConstructorImplementationType1 };

            var invalidTypeConstructorImplementationType2 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidTypeConstructorImplementationType2 };

            var invalidTypeConstructorImplementationType3 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidTypeConstructorImplementationType3 };

            var invalidTypeConstructorImplementationType4 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidTypeConstructorImplementationType4 };

            var invalidTypeConstructorImplementationType5 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidTypeConstructorImplementationType5 };

            var invalidTypeConstructorStatement1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorStatement1.Statements.Add(new CodeStatement());
            yield return new object[] { invalidTypeConstructorStatement1 };

            var invalidTypeConstructorStatement2 = new CodeTypeConstructor { Name = "name", Attributes = MemberAttributes.Abstract };
            invalidTypeConstructorStatement2.Statements.Add(new CodeStatement());
            yield return new object[] { invalidTypeConstructorStatement2 };

            var invalidTypeConstructorTypeParameter1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidTypeConstructorTypeParameter1 };

            var invalidTypeConstructorTypeParameter2 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidTypeConstructorTypeParameter2 };

            var invalidTypeConstructorTypeParameter3 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidTypeConstructorTypeParameter3 };

            var invalidTypeConstructorTypeParameter4 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidTypeConstructorTypeParameter4 };

            var invalidTypeConstructorTypeParameterAttribute1 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute1 };

            var invalidTypeConstructorTypeParameterAttribute2 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute2 };

            var invalidTypeConstructorTypeParameterAttribute3 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute3 };

            var invalidTypeConstructorTypeParameterAttribute4 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute4 };

            var invalidTypeConstructorTypeParameterAttribute5 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute5 };

            var invalidTypeConstructorTypeParameterAttribute6 = new CodeTypeConstructor { Name = "name" };
            invalidTypeConstructorTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidTypeConstructorTypeParameterAttribute6 };

            // CodeMemberProperty.
            yield return new object[] { new CodeMemberProperty() };
            yield return new object[] { new CodeMemberProperty { Name = null } };
            yield return new object[] { new CodeMemberProperty { Name = string.Empty } };
            yield return new object[] { new CodeMemberProperty { Name = "0" } };
            yield return new object[] { new CodeMemberProperty { Name = "name", PrivateImplementationType = new CodeTypeReference() } };
            yield return new object[] { new CodeMemberProperty { Name = "name", PrivateImplementationType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeMemberProperty { Name = "name", PrivateImplementationType = invalidTypeReference1 } };
            yield return new object[] { new CodeMemberProperty { Name = "name", PrivateImplementationType = invalidTypeReference2 } };
            yield return new object[] { new CodeMemberProperty { Name = "name", PrivateImplementationType = invalidTypeReference3 } };

            var invalidPropertyStartDirective1 = new CodeMemberProperty { Name = "name" };
            invalidPropertyStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidPropertyStartDirective1 };

            var invalidPropertyStartDirective2 = new CodeMemberProperty { Name = "name" };
            invalidPropertyStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidPropertyStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidPropertyStartDirective3 = new CodeMemberProperty { Name = "name" };
                invalidPropertyStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidPropertyStartDirective3 };
            }

            var invalidPropertyEndDirective1 = new CodeMemberProperty { Name = "name" };
            invalidPropertyEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidPropertyEndDirective1 };

            var invalidPropertyEndDirective2 = new CodeMemberProperty { Name = "name" };
            invalidPropertyEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidPropertyEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidPropertyEndDirective3 = new CodeMemberProperty { Name = "name" };
                invalidPropertyEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidPropertyEndDirective3 };
            }

            foreach (string name in new string[] { "item", "Item" })
            {
                var invalidPropertyParameter1 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter1.Parameters.Add(new CodeParameterDeclarationExpression());
                yield return new object[] { invalidPropertyParameter1 };

                var invalidPropertyParameter2 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
                yield return new object[] { invalidPropertyParameter2 };

                var invalidPropertyParameter3 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter3.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
                yield return new object[] { invalidPropertyParameter3 };

                var invalidPropertyParameter4 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter4.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference1, "name"));
                yield return new object[] { invalidPropertyParameter4 };

                var invalidPropertyParameter5 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter5.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference2, "name"));
                yield return new object[] { invalidPropertyParameter5 };

                var invalidPropertyParameter6 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter6.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference3, "name"));
                yield return new object[] { invalidPropertyParameter6 };

                var invalidPropertyParameter7 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter7.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
                yield return new object[] { invalidPropertyParameter7 };

                var invalidPropertyParameter8 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter8.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
                yield return new object[] { invalidPropertyParameter8 };

                var invalidPropertyParameter9 = new CodeMemberProperty { Name = name };
                invalidPropertyParameter9.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
                yield return new object[] { invalidPropertyParameter9 };

                var invalidPropertyParameterAttribute1 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute1.Parameters.Add(invalidParameterAttribute1);
                yield return new object[] { invalidPropertyParameterAttribute1 };

                var invalidPropertyParameterAttribute2 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute2.Parameters.Add(invalidParameterAttribute2);
                yield return new object[] { invalidPropertyParameterAttribute2 };

                var invalidPropertyParameterAttribute3 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute3.Parameters.Add(invalidParameterAttribute3);
                yield return new object[] { invalidPropertyParameterAttribute3 };

                var invalidPropertyParameterAttribute4 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute4.Parameters.Add(invalidParameterAttribute4);
                yield return new object[] { invalidPropertyParameterAttribute4 };

                var invalidPropertyParameterAttribute5 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute5.Parameters.Add(invalidParameterAttribute5);
                yield return new object[] { invalidPropertyParameterAttribute5 };

                var invalidPropertyParameterAttribute6 = new CodeMemberProperty { Name = name };
                invalidPropertyParameterAttribute6.Parameters.Add(invalidParameterAttribute6);
                yield return new object[] { invalidPropertyParameterAttribute6 };
            }

            var invalidPropertyImplementationType1 = new CodeMemberProperty { Name = "name" };
            invalidPropertyImplementationType1.ImplementationTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidPropertyImplementationType1 };

            var invalidPropertyImplementationType2 = new CodeMemberProperty { Name = "name" };
            invalidPropertyImplementationType2.ImplementationTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidPropertyImplementationType2 };

            var invalidPropertyImplementationType3 = new CodeMemberProperty { Name = "name" };
            invalidPropertyImplementationType3.ImplementationTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidPropertyImplementationType3 };

            var invalidPropertyImplementationType4 = new CodeMemberProperty { Name = "name" };
            invalidPropertyImplementationType4.ImplementationTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidPropertyImplementationType4 };

            var invalidPropertyImplementationType5 = new CodeMemberProperty { Name = "name" };
            invalidPropertyImplementationType5.ImplementationTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidPropertyImplementationType5 };

            var invalidPropertyGetStatement = new CodeMemberProperty { Name = "name" };
            invalidPropertyGetStatement.GetStatements.Add(new CodeStatement());
            yield return new object[] { invalidPropertyGetStatement };

            var invalidPropertySetStatement = new CodeMemberProperty { Name = "name" };
            invalidPropertySetStatement.SetStatements.Add(new CodeStatement());
            yield return new object[] { invalidPropertySetStatement };

            // CodeTypeDeclaration.
            yield return new object[] { new CodeTypeDeclaration() };
            yield return new object[] { new CodeTypeDeclaration(null) };
            yield return new object[] { new CodeTypeDeclaration(string.Empty) };
            yield return new object[] { new CodeTypeDeclaration("0") };

            var invalidTypeAttribute1 = new CodeTypeDeclaration("name");
            invalidTypeAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidTypeAttribute1 };

            var invalidTypeAttribute2 = new CodeTypeDeclaration("name");
            invalidTypeAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidTypeAttribute2 };

            var invalidTypeAttribute3 = new CodeTypeDeclaration("name");
            invalidTypeAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidTypeAttribute3 };

            var invalidTypeAttribute4 = new CodeTypeDeclaration("name");
            invalidTypeAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidTypeAttribute4 };

            var invalidTypeAttribute5 = new CodeTypeDeclaration("name");
            invalidTypeAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidTypeAttribute5 };

            var invalidTypeAttribute6 = new CodeTypeDeclaration("name");
            invalidTypeAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidTypeAttribute6 };

            var invalidTypeParameter1 = new CodeTypeDeclaration("name");
            invalidTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidTypeParameter1 };

            var invalidTypeParameter2 = new CodeTypeDeclaration("name");
            invalidTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidTypeParameter2 };

            var invalidTypeParameter3 = new CodeTypeDeclaration("name");
            invalidTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidTypeParameter3 };

            var invalidTypeParameter4 = new CodeTypeDeclaration("name");
            invalidTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidTypeParameter4 };

            var invalidTypeTypeParameterAttribute1 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidTypeTypeParameterAttribute1 };

            var invalidTypeTypeParameterAttribute2 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidTypeTypeParameterAttribute2 };

            var invalidTypeTypeParameterAttribute3 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidTypeTypeParameterAttribute3 };

            var invalidTypeTypeParameterAttribute4 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidTypeTypeParameterAttribute4 };

            var invalidTypeTypeParameterAttribute5 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidTypeTypeParameterAttribute5 };

            var invalidTypeTypeParameterAttribute6 = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidTypeTypeParameterAttribute6 };

            var invalidParameterConstraint1 = new CodeTypeParameter("parameter");
            invalidParameterConstraint1.Constraints.Add(new CodeTypeReference());
            var invalidTypeParameterConstraint1 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint1.TypeParameters.Add(invalidParameterConstraint1);
            yield return new object[] { invalidTypeParameterConstraint1 };

            var invalidParameterConstraint2 = new CodeTypeParameter("parameter");
            invalidParameterConstraint2.Constraints.Add(new CodeTypeReference("0"));
            var invalidTypeParameterConstraint2 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint2.TypeParameters.Add(invalidParameterConstraint2);
            yield return new object[] { invalidTypeParameterConstraint2 };

            var invalidParameterConstraint3 = new CodeTypeParameter("parameter");
            invalidParameterConstraint3.Constraints.Add(invalidTypeReference1);
            var invalidTypeParameterConstraint3 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint3.TypeParameters.Add(invalidParameterConstraint3);
            yield return new object[] { invalidTypeParameterConstraint3 };

            var invalidParameterConstraint4 = new CodeTypeParameter("parameter");
            invalidParameterConstraint4.Constraints.Add(invalidTypeReference2);
            var invalidTypeParameterConstraint4 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint4.TypeParameters.Add(invalidParameterConstraint4);
            yield return new object[] { invalidTypeParameterConstraint4 };

            var invalidParameterConstraint5 = new CodeTypeParameter("parameter");
            invalidParameterConstraint5.Constraints.Add(invalidTypeReference3);
            var invalidTypeParameterConstraint5 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint5.TypeParameters.Add(invalidParameterConstraint5);
            yield return new object[] { invalidTypeParameterConstraint5 };

            var invalidParameterConstraint6 = new CodeTypeParameter("parameter");
            invalidParameterConstraint6.Constraints.Add(new CodeTypeReference("constraint`2", new CodeTypeReference("name")));
            var invalidTypeParameterConstraint6 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint6.TypeParameters.Add(invalidParameterConstraint6);
            yield return new object[] { invalidTypeParameterConstraint6 };

            var invalidParameterConstraint7 = new CodeTypeParameter("parameter");
            invalidParameterConstraint7.Constraints.Add(new CodeTypeReference("constraint", new CodeTypeReference(), new CodeTypeReference("name")));
            var invalidTypeParameterConstraint7 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint7.TypeParameters.Add(invalidParameterConstraint7);
            yield return new object[] { invalidTypeParameterConstraint7 };

            var invalidParameterConstraint8 = new CodeTypeParameter("parameter");
            invalidParameterConstraint8.Constraints.Add(new CodeTypeReference("constraint", new CodeTypeReference("0"), new CodeTypeReference("name")));
            var invalidTypeParameterConstraint8 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint8.TypeParameters.Add(invalidParameterConstraint8);
            yield return new object[] { invalidTypeParameterConstraint8 };

            var invalidParameterConstraint9 = new CodeTypeParameter("parameter");
            invalidParameterConstraint9.Constraints.Add(new CodeTypeReference("constraint", invalidTypeReference1, new CodeTypeReference("name")));
            var invalidTypeParameterConstraint9 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint9.TypeParameters.Add(invalidParameterConstraint9);
            yield return new object[] { invalidTypeParameterConstraint9 };

            var invalidParameterConstraint10 = new CodeTypeParameter("parameter");
            invalidParameterConstraint10.Constraints.Add(new CodeTypeReference("constraint", invalidTypeReference2, new CodeTypeReference("name")));
            var invalidTypeParameterConstraint10 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint10.TypeParameters.Add(invalidParameterConstraint10);
            yield return new object[] { invalidTypeParameterConstraint10 };

            var invalidParameterConstraint11 = new CodeTypeParameter("parameter");
            invalidParameterConstraint11.Constraints.Add(new CodeTypeReference("constraint", invalidTypeReference3, new CodeTypeReference("name")));
            var invalidTypeParameterConstraint11 = new CodeTypeDeclaration("name");
            invalidTypeParameterConstraint11.TypeParameters.Add(invalidParameterConstraint11);
            yield return new object[] { invalidTypeParameterConstraint11 };

            var invalidTypeBaseType1 = new CodeTypeDeclaration("name");
            invalidTypeBaseType1.BaseTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidTypeBaseType1 };

            var invalidTypeBaseType2 = new CodeTypeDeclaration("name");
            invalidTypeBaseType2.BaseTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidTypeBaseType2 };

            var invalidTypeBaseType3 = new CodeTypeDeclaration("name");
            invalidTypeBaseType3.BaseTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidTypeBaseType3 };

            var invalidTypeBaseType4 = new CodeTypeDeclaration("name");
            invalidTypeBaseType4.BaseTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidTypeBaseType4 };

            var invalidTypeBaseType5 = new CodeTypeDeclaration("name");
            invalidTypeBaseType5.BaseTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidTypeBaseType5 };

            // CodeTypeDelegate.
            yield return new object[] { new CodeTypeDelegate() };
            yield return new object[] { new CodeTypeDelegate(null) };
            yield return new object[] { new CodeTypeDelegate(string.Empty) };
            yield return new object[] { new CodeTypeDelegate("0") };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference() } };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = new CodeTypeReference("0") } };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = invalidTypeReference1 } };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = invalidTypeReference2 } };
            yield return new object[] { new CodeTypeDelegate("name") { ReturnType = invalidTypeReference3 } };

            var invalidDelegateAttribute1 = new CodeTypeDelegate("name");
            invalidDelegateAttribute1.CustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidDelegateAttribute1 };

            var invalidDelegateAttribute2 = new CodeTypeDelegate("name");
            invalidDelegateAttribute2.CustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidDelegateAttribute2 };

            var invalidDelegateAttribute3 = new CodeTypeDelegate("name");
            invalidDelegateAttribute3.CustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidDelegateAttribute3 };

            var invalidDelegateAttribute4 = new CodeTypeDelegate("name");
            invalidDelegateAttribute4.CustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidDelegateAttribute4 };

            var invalidDelegateAttribute5 = new CodeTypeDelegate("name");
            invalidDelegateAttribute5.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidDelegateAttribute5 };

            var invalidDelegateAttribute6 = new CodeTypeDelegate("name");
            invalidDelegateAttribute6.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidDelegateAttribute6 };

            var invalidDelegateTypeParameter1 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameter1.TypeParameters.Add(new CodeTypeParameter());
            yield return new object[] { invalidDelegateTypeParameter1 };

            var invalidDelegateTypeParameter2 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameter2.TypeParameters.Add(new CodeTypeParameter(null));
            yield return new object[] { invalidDelegateTypeParameter2 };

            var invalidDelegateTypeParameter3 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameter3.TypeParameters.Add(new CodeTypeParameter(string.Empty));
            yield return new object[] { invalidDelegateTypeParameter3 };

            var invalidDelegateTypeParameter4 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameter4.TypeParameters.Add(new CodeTypeParameter("0"));
            yield return new object[] { invalidDelegateTypeParameter4 };

            var invalidDelegateTypeParameterAttribute1 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute1.TypeParameters.Add(invalidTypeParameterAttribute1);
            yield return new object[] { invalidDelegateTypeParameterAttribute1 };

            var invalidDelegateTypeParameterAttribute2 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute2.TypeParameters.Add(invalidTypeParameterAttribute2);
            yield return new object[] { invalidDelegateTypeParameterAttribute2 };

            var invalidDelegateTypeParameterAttribute3 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute3.TypeParameters.Add(invalidTypeParameterAttribute3);
            yield return new object[] { invalidDelegateTypeParameterAttribute3 };

            var invalidDelegateTypeParameterAttribute4 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute4.TypeParameters.Add(invalidTypeParameterAttribute4);
            yield return new object[] { invalidDelegateTypeParameterAttribute4 };

            var invalidDelegateTypeParameterAttribute5 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute5.TypeParameters.Add(invalidTypeParameterAttribute5);
            yield return new object[] { invalidDelegateTypeParameterAttribute5 };

            var invalidDelegateTypeParameterAttribute6 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterAttribute6.TypeParameters.Add(invalidTypeParameterAttribute6);
            yield return new object[] { invalidDelegateTypeParameterAttribute6 };

            var invalidDelegateTypeParameterConstraint1 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterConstraint1.TypeParameters.Add(invalidParameterConstraint1);
            yield return new object[] { invalidDelegateTypeParameterConstraint1 };

            var invalidDelegateTypeParameterConstraint2 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterConstraint2.TypeParameters.Add(invalidParameterConstraint2);
            yield return new object[] { invalidDelegateTypeParameterConstraint2 };

            var invalidDelegateTypeParameterConstraint3 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterConstraint3.TypeParameters.Add(invalidParameterConstraint6);
            yield return new object[] { invalidDelegateTypeParameterConstraint3 };

            var invalidDelegateTypeParameterConstraint4 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterConstraint4.TypeParameters.Add(invalidParameterConstraint7);
            yield return new object[] { invalidDelegateTypeParameterConstraint4 };

            var invalidDelegateTypeParameterConstraint5 = new CodeTypeDelegate("name");
            invalidDelegateTypeParameterConstraint5.TypeParameters.Add(invalidParameterConstraint8);
            yield return new object[] { invalidDelegateTypeParameterConstraint5 };

            var invalidDelegateBaseType1 = new CodeTypeDelegate("name");
            invalidDelegateBaseType1.BaseTypes.Add(new CodeTypeReference());
            yield return new object[] { invalidDelegateBaseType1 };

            var invalidDelegateBaseType2 = new CodeTypeDelegate("name");
            invalidDelegateBaseType2.BaseTypes.Add(new CodeTypeReference("0"));
            yield return new object[] { invalidDelegateBaseType2 };

            var invalidDelegateBaseType3 = new CodeTypeDelegate("name");
            invalidDelegateBaseType3.BaseTypes.Add(invalidTypeReference1);
            yield return new object[] { invalidDelegateBaseType3 };

            var invalidDelegateBaseType4 = new CodeTypeDelegate("name");
            invalidDelegateBaseType4.BaseTypes.Add(invalidTypeReference2);
            yield return new object[] { invalidDelegateBaseType4 };

            var invalidDelegateBaseType5 = new CodeTypeDelegate("name");
            invalidDelegateBaseType5.BaseTypes.Add(invalidTypeReference3);
            yield return new object[] { invalidDelegateBaseType5 };

            var invalidDelegateParameter1 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter1.Parameters.Add(new CodeParameterDeclarationExpression());
            yield return new object[] { invalidDelegateParameter1 };

            var invalidDelegateParameter2 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter2.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference(), "name"));
            yield return new object[] { invalidDelegateParameter2 };

            var invalidDelegateParameter3 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter3.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("0"), "name"));
            yield return new object[] { invalidDelegateParameter3 };

            var invalidDelegateParameter4 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter4.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference1, "name"));
            yield return new object[] { invalidDelegateParameter4 };

            var invalidDelegateParameter5 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter5.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference2, "name"));
            yield return new object[] { invalidDelegateParameter5 };

            var invalidDelegateParameter6 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter6.Parameters.Add(new CodeParameterDeclarationExpression(invalidTypeReference3, "name"));
            yield return new object[] { invalidDelegateParameter6 };

            var invalidDelegateParameter7 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter7.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), null));
            yield return new object[] { invalidDelegateParameter7 };

            var invalidDelegateParameter8 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter8.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), string.Empty));
            yield return new object[] { invalidDelegateParameter8 };

            var invalidDelegateParameter9 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameter9.Parameters.Add(new CodeParameterDeclarationExpression(new CodeTypeReference("type"), "0"));
            yield return new object[] { invalidDelegateParameter9 };
                        
            var invalidDelegateParameterAttribute1 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute1.Parameters.Add(invalidParameterAttribute1);
            yield return new object[] { invalidDelegateParameterAttribute1 };

            var invalidDelegateParameterAttribute2 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute2.Parameters.Add(invalidParameterAttribute2);
            yield return new object[] { invalidDelegateParameterAttribute2 };

            var invalidDelegateParameterAttribute3 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute3.Parameters.Add(invalidParameterAttribute3);
            yield return new object[] { invalidDelegateParameterAttribute3 };

            var invalidDelegateParameterAttribute4 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute4.Parameters.Add(invalidParameterAttribute4);
            yield return new object[] { invalidDelegateParameterAttribute4 };

            var invalidDelegateParameterAttribute5 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute5.Parameters.Add(invalidParameterAttribute5);
            yield return new object[] { invalidDelegateParameterAttribute5 };

            var invalidDelegateParameterAttribute6 = new CodeTypeDelegate { Name = "name" };
            invalidDelegateParameterAttribute6.Parameters.Add(invalidParameterAttribute6);
            yield return new object[] { invalidDelegateParameterAttribute6 };

            // CodeNamespace.
            yield return new object[] { new CodeNamespace("0") };

            var invalidNamespaceType1 = new CodeNamespace("name");
            invalidNamespaceType1.Types.Add(new CodeTypeDeclaration());
            yield return new object[] { invalidNamespaceType1 };

            var invalidNamespaceType2 = new CodeNamespace("name");
            invalidNamespaceType2.Types.Add(new CodeTypeDeclaration(null));
            yield return new object[] { invalidNamespaceType2 };

            var invalidNamespaceType3 = new CodeNamespace("name");
            invalidNamespaceType3.Types.Add(new CodeTypeDeclaration(string.Empty));
            yield return new object[] { invalidNamespaceType3 };

            var invalidNamespaceType4 = new CodeNamespace("name");
            invalidNamespaceType4.Types.Add(new CodeTypeDeclaration("0"));
            yield return new object[] { invalidNamespaceType4 };

            // CodeCompileUnit.
            var invalidCompileUnitAttribute1 = new CodeCompileUnit();
            invalidCompileUnitAttribute1.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration());
            yield return new object[] { invalidCompileUnitAttribute1 };

            var invalidCompileUnitAttribute2 = new CodeCompileUnit();
            invalidCompileUnitAttribute2.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration((string)null));
            yield return new object[] { invalidCompileUnitAttribute2 };

            var invalidCompileUnitAttribute3 = new CodeCompileUnit();
            invalidCompileUnitAttribute3.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration(string.Empty));
            yield return new object[] { invalidCompileUnitAttribute3 };

            var invalidCompileUnitAttribute4 = new CodeCompileUnit();
            invalidCompileUnitAttribute4.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("0"));
            yield return new object[] { invalidCompileUnitAttribute4 };

            var invalidCompileUnitAttribute5 = new CodeCompileUnit();
            invalidCompileUnitAttribute5.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("0", new CodePrimitiveExpression(1))));
            yield return new object[] { invalidCompileUnitAttribute5 };

            var invalidCompileUnitAttribute6 = new CodeCompileUnit();
            invalidCompileUnitAttribute6.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument("name", new CodeExpression())));
            yield return new object[] { invalidCompileUnitAttribute6 };
            
            var invalidCompileUnitStartDirective1 = new CodeCompileUnit();
            invalidCompileUnitStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidCompileUnitStartDirective1 };

            var invalidCompileUnitStartDirective2 = new CodeCompileUnit();
            invalidCompileUnitStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidCompileUnitStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidCompileUnitStartDirective3 = new CodeCompileUnit();
                invalidCompileUnitStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidCompileUnitStartDirective3 };
            }

            var invalidCompileUnitEndDirective1 = new CodeCompileUnit();
            invalidCompileUnitEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidCompileUnitEndDirective1 };

            var invalidCompileUnitEndDirective2 = new CodeCompileUnit();
            invalidCompileUnitEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidCompileUnitEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidCompileUnitEndDirective3 = new CodeCompileUnit();
                invalidCompileUnitEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidCompileUnitEndDirective3 };
            }

            // CodeSnippetCompileUnit.
            var invalidSnippetCompileUnitStartDirective1 = new CodeSnippetCompileUnit();
            invalidSnippetCompileUnitStartDirective1.StartDirectives.Add(new CodeDirective());
            yield return new object[] { invalidSnippetCompileUnitStartDirective1 };

            var invalidSnippetCompileUnitStartDirective2 = new CodeSnippetCompileUnit();
            invalidSnippetCompileUnitStartDirective2.StartDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidSnippetCompileUnitStartDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidSnippetCompileUnitStartDirective3 = new CodeSnippetCompileUnit();
                invalidSnippetCompileUnitStartDirective3.StartDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidSnippetCompileUnitStartDirective3 };
            }

            var invalidSnippetCompileUnitEndDirective1 = new CodeSnippetCompileUnit();
            invalidSnippetCompileUnitEndDirective1.EndDirectives.Add(new CodeDirective());
            yield return new object[] { invalidSnippetCompileUnitEndDirective1 };

            var invalidSnippetCompileUnitEndDirective2 = new CodeSnippetCompileUnit();
            invalidSnippetCompileUnitEndDirective2.EndDirectives.Add(new CodeChecksumPragma("\0", Guid.NewGuid(), new byte[0]));
            yield return new object[] { invalidSnippetCompileUnitEndDirective2 };

            foreach (char newLineChar in new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' })
            {
                var invalidSnippetCompileUnitEndDirective3 = new CodeSnippetCompileUnit();
                invalidSnippetCompileUnitEndDirective3.EndDirectives.Add(new CodeRegionDirective(CodeRegionMode.None, $"te{newLineChar}xt"));
                yield return new object[] { invalidSnippetCompileUnitEndDirective3 };
            }

            // CodeArrayCreateExpression.
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference()) };
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("0")) };
            yield return new object[] { new CodeArrayCreateExpression(invalidTypeReference1) };
            yield return new object[] { new CodeArrayCreateExpression(invalidTypeReference2) };
            yield return new object[] { new CodeArrayCreateExpression(invalidTypeReference3) { SizeExpression = new CodeExpression() } }  ;
            yield return new object[] { new CodeArrayCreateExpression(new CodeTypeReference("name"), new CodeExpression[] { new CodeExpression() }) };

            // CodeBinaryOperatorExpression.
            yield return new object[] { new CodeBinaryOperatorExpression(new CodeExpression(), CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, new CodeExpression()) };

            // CodeCastExpression.
            yield return new object[] { new CodeCastExpression(new CodeTypeReference(), new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("0"), new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeCastExpression(invalidTypeReference1, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeCastExpression(invalidTypeReference2, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeCastExpression(invalidTypeReference3, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("name"), new CodeExpression()) };

            // CodeDefaultValueExpression.
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference()) };
            yield return new object[] { new CodeDefaultValueExpression(new CodeTypeReference("0")) };
            yield return new object[] { new CodeDefaultValueExpression(invalidTypeReference1) };
            yield return new object[] { new CodeDefaultValueExpression(invalidTypeReference2) };
            yield return new object[] { new CodeDefaultValueExpression(invalidTypeReference3) };

            // CodeDelegateCreateExpression.
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference(), new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("0"), new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(invalidTypeReference1, new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(invalidTypeReference2, new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(invalidTypeReference3, new CodePrimitiveExpression(1), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), new CodeExpression(), "name") };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1), null) };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1), string.Empty) };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), new CodePrimitiveExpression(1), "0") };

            // CodeFieldReferenceExpression.
            yield return new object[] { new CodeFieldReferenceExpression() };
            yield return new object[] { new CodeFieldReferenceExpression(null, null) };
            yield return new object[] { new CodeFieldReferenceExpression(null, string.Empty) };
            yield return new object[] { new CodeFieldReferenceExpression(null, "0") };
            yield return new object[] { new CodeFieldReferenceExpression(new CodeExpression(), "name") };

            // CodeArgumentReferenceExpression.
            yield return new object[] { new CodeArgumentReferenceExpression() };
            yield return new object[] { new CodeArgumentReferenceExpression(null) };
            yield return new object[] { new CodeArgumentReferenceExpression(string.Empty) };
            yield return new object[] { new CodeArgumentReferenceExpression("0") };

            // CodeVariableReferenceExpression.
            yield return new object[] { new CodeVariableReferenceExpression() };
            yield return new object[] { new CodeVariableReferenceExpression(null) };
            yield return new object[] { new CodeVariableReferenceExpression(string.Empty) };
            yield return new object[] { new CodeVariableReferenceExpression("0") };

            // CodeIndexerExpression.
            yield return new object[] { new CodeIndexerExpression(new CodeExpression()) };
            yield return new object[] { new CodeIndexerExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            // CodeArrayIndexerExpression.
            yield return new object[] { new CodeArrayIndexerExpression(new CodeExpression()) };
            yield return new object[] { new CodeArrayIndexerExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            // CodeMethodInvokeExpression.
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression()) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, null)) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, string.Empty)) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "0")) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeExpression(), "name")) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference() })) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference("0") })) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference1 })) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference2 })) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference3 })) };
            yield return new object[] { new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(null, "name"), new CodeExpression()) };

            // CodeMethodReferenceExpression.
            yield return new object[] { new CodeMethodReferenceExpression() };
            yield return new object[] { new CodeMethodReferenceExpression(null, null) };
            yield return new object[] { new CodeMethodReferenceExpression(null, string.Empty) };
            yield return new object[] { new CodeMethodReferenceExpression(null, "0") };
            yield return new object[] { new CodeMethodReferenceExpression(new CodeExpression(), "name") };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference() }) };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { new CodeTypeReference("0") }) };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference1 }) };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference2 }) };
            yield return new object[] { new CodeMethodReferenceExpression(new CodePrimitiveExpression(1), "name", new CodeTypeReference[] { invalidTypeReference3 }) };

            // CodeEventReferenceExpression.
            yield return new object[] { new CodeEventReferenceExpression() };
            yield return new object[] { new CodeEventReferenceExpression(null, null) };
            yield return new object[] { new CodeEventReferenceExpression(null, string.Empty) };
            yield return new object[] { new CodeEventReferenceExpression(null, "0") };
            yield return new object[] { new CodeEventReferenceExpression(new CodeExpression(), "name") };

            // CodeDelegateInvokeExpression.
            yield return new object[] { new CodeDelegateInvokeExpression(new CodeExpression()) };
            yield return new object[] { new CodeDelegateInvokeExpression(new CodePrimitiveExpression(1), new CodeExpression()) };

            // CodeObjectCreateExpression.
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference()) };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("0")) };
            yield return new object[] { new CodeObjectCreateExpression(invalidTypeReference1) };
            yield return new object[] { new CodeObjectCreateExpression(invalidTypeReference2) };
            yield return new object[] { new CodeObjectCreateExpression(invalidTypeReference3) };
            yield return new object[] { new CodeObjectCreateExpression(new CodeTypeReference("name"), new CodeExpression()) };

            // CodeDirectionExpression.
            yield return new object[] { new CodeDirectionExpression(FieldDirection.In, new CodeExpression()) };

            // CodePropertyReferenceExpression.
            yield return new object[] { new CodePropertyReferenceExpression() };
            yield return new object[] { new CodePropertyReferenceExpression(null, null) };
            yield return new object[] { new CodePropertyReferenceExpression(null, string.Empty) };
            yield return new object[] { new CodePropertyReferenceExpression(null, "0") };
            yield return new object[] { new CodePropertyReferenceExpression(new CodeExpression(), "name") };

            // CodeTypeReferenceExpression.
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference()) };
            yield return new object[] { new CodeTypeReferenceExpression(new CodeTypeReference("0")) };
            yield return new object[] { new CodeTypeReferenceExpression(invalidTypeReference1) };
            yield return new object[] { new CodeTypeReferenceExpression(invalidTypeReference2) };
            yield return new object[] { new CodeTypeReferenceExpression(invalidTypeReference3) };

            // CodeTypeOfExpression.
            yield return new object[] { new CodeTypeOfExpression(new CodeTypeReference()) };
            yield return new object[] { new CodeTypeOfExpression(new CodeTypeReference("0")) };
            yield return new object[] { new CodeTypeOfExpression(invalidTypeReference1) };
            yield return new object[] { new CodeTypeOfExpression(invalidTypeReference2) };
            yield return new object[] { new CodeTypeOfExpression(invalidTypeReference3) };

            // CodeMethodReturnStatement.
            yield return new object[] { new CodeMethodReturnStatement(new CodeExpression()) };

            // CodeConditionStatement.
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1"), new CodeStatement[] { new CodeStatement() }, new CodeStatement[] { new CodeMethodReturnStatement(), new CodeMethodReturnStatement { LinePragma = new CodeLinePragma() } }) };
            yield return new object[] { new CodeConditionStatement(new CodePrimitiveExpression("1"), new CodeStatement[] { new CodeMethodReturnStatement() }, new CodeStatement[] { new CodeStatement() }) };

            // CodeTryCatchFinallyStatement.
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("localName") },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause() },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause(null) },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause(string.Empty) },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("0") },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("localName", new CodeTypeReference()) },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("localName", new CodeTypeReference("0")) },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("localName", new CodeTypeReference("exceptionType"), new CodeStatement()) },
                    new CodeStatement[] { new CodeMethodReturnStatement() }
                )
            };
            yield return new object[]
            {
                new CodeTryCatchFinallyStatement(
                    new CodeStatement[] { new CodeMethodReturnStatement() },
                    new CodeCatchClause[] { new CodeCatchClause("localName") },
                    new CodeStatement[] { new CodeStatement() }
                )
            };

            // CodeAssignStatement.
            yield return new object[] { new CodeAssignStatement(new CodeExpression(), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), new CodeExpression()) };

            // CodeExpressionStatement.
            yield return new object[] { new CodeExpressionStatement(new CodeExpression()) };

            // CodeIterationStatement.
            yield return new object[] { new CodeIterationStatement(new CodeStatement(), new CodePrimitiveExpression(1), new CodeMethodReturnStatement()) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodeExpression(), new CodeMethodReturnStatement()) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodePrimitiveExpression(1), new CodeStatement()) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodePrimitiveExpression(1), new CodeMethodReturnStatement(), new CodeStatement()) };

            // CodeThrowExceptionStatement.
            yield return new object[] { new CodeThrowExceptionStatement(new CodeExpression()) };

            // CodeVariableDeclarationStatement.
            yield return new object[] { new CodeVariableDeclarationStatement() };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference(), "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("0"), "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(invalidTypeReference1, "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(invalidTypeReference2, "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(invalidTypeReference3, "name") };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), null) };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), string.Empty) };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), "0") };
            yield return new object[] { new CodeVariableDeclarationStatement(new CodeTypeReference("name"), "name", new CodeExpression()) };

            // CodeAttachEventStatement.
            yield return new object[] { new CodeAttachEventStatement() };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, null), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, string.Empty), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(null, "0"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodeExpression(), "name"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAttachEventStatement(null, new CodeExpression()) };

            // CodeRemoveEventStatement.
            yield return new object[] { new CodeRemoveEventStatement() };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, null), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, string.Empty), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(null, "0"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodeExpression(), "name"), new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeRemoveEventStatement(null, new CodeExpression()) };

            // CodeGotoStatement.
            yield return new object[] { new CodeGotoStatement() };
            yield return new object[] { new CodeGotoStatement("0") };

            // CodeLabeledStatement.
            yield return new object[] { new CodeLabeledStatement() };
            yield return new object[] { new CodeLabeledStatement(null) };
            yield return new object[] { new CodeLabeledStatement(string.Empty) };
            yield return new object[] { new CodeLabeledStatement("0") };
            yield return new object[] { new CodeLabeledStatement("name", new CodeStatement()) };

            // Misc.
            yield return new object[] { new CodeStatement() };
            yield return new object[] { new CustomCodeStatement() };
            yield return new object[] { new CodeExpression() };
            yield return new object[] { new CustomCodeExpression() };
            yield return new object[] { new CodeDirective() };
            yield return new object[] { new CustomCodeDirective() };
            yield return new object[] { new CodeTypeParameter() };
            yield return new object[] { new CodeTypeParameter("name") };
            yield return new object[] { new CodeObject() };
            yield return new object[] { new CustomCodeObject() };
            yield return new object[] { new CodeTypeMember() };
            yield return new object[] { new CustomCodeTypeMember() };

            yield return new object[] { new CodeTypeReference(";") };
            yield return new object[] { new CodeTypeReference("/") };
            yield return new object[] { new CodeTypeReference("#") };
            yield return new object[] { new CodeTypeReference("%") };
            yield return new object[] { new CodeTypeReference("=") };
            yield return new object[] { new CodeTypeReference("?") };
            yield return new object[] { new CodeTypeReference("\\") };
            yield return new object[] { new CodeTypeReference("^") };
            yield return new object[] { new CodeTypeReference("'") };
            yield return new object[] { new CodeTypeReference(")") };
            yield return new object[] { new CodeTypeReference("(") };
        }

        public static IEnumerable<object[]> ValidIdentifier_InvalidMemberInType_TestData()
        {
            foreach (object[] testData in ValidateIdentifiers_Invalid_TestData())
            {
                if (testData[0] is CodeTypeMember member)
                {
                    var t = new CodeTypeDeclaration("name");
                    t.Members.Add(member);
                    yield return new object[] { t };

                    var n = new CodeNamespace("namespace");
                    n.Types.Add(t);
                    yield return new object[] { n };
                }
                else if (testData[0] is CodeTypeDeclaration type)
                {
                    var n = new CodeNamespace();
                    n.Types.Add(type);
                    yield return new object[] { n };
                }
            }
        }

        [Theory]
        [MemberData(nameof(ValidateIdentifiers_Invalid_TestData))]
        [MemberData(nameof(ValidIdentifier_InvalidMemberInType_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fixed incorrect param name in some situations")]
        public void ValidateIdentifiers_InvalidE_ThrowsArgumentException(CodeObject e)
        {
            AssertExtensions.Throws<ArgumentException>("e", () => CodeGenerator.ValidateIdentifiers(e));
        }

        public static IEnumerable<object[]> ValidateIdentifiers_NullE_TestData()
        {
            yield return new object[] { null };

            var invalidTypeAttribute = new CodeTypeDeclaration("name");
            invalidTypeAttribute.CustomAttributes.Add(new CodeAttributeDeclaration("name", new CodeAttributeArgument()));
            yield return new object[] { invalidTypeAttribute };

            var invalidTypeParameterAttribute = new CodeTypeParameter("parameter");
            invalidTypeParameterAttribute.CustomAttributes.Add(new CodeAttributeDeclaration("attribute", new CodeAttributeArgument()));
            var invalidTypeTypeParameterAttribute = new CodeTypeDeclaration("name");
            invalidTypeTypeParameterAttribute.TypeParameters.Add(invalidTypeParameterAttribute);
            yield return new object[] { invalidTypeTypeParameterAttribute };

            yield return new object[] { new CodeBinaryOperatorExpression() };
            yield return new object[] { new CodeBinaryOperatorExpression(null, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(2)) };
            yield return new object[] { new CodeBinaryOperatorExpression(new CodePrimitiveExpression(1), CodeBinaryOperatorType.Add, null) };

            yield return new object[] { new CodeCastExpression() };
            yield return new object[] { new CodeCastExpression(new CodeTypeReference("name"), null) };

            yield return new object[] { new CodeDelegateCreateExpression() };
            yield return new object[] { new CodeDelegateCreateExpression(new CodeTypeReference("name"), null, "methodName") };

            yield return new object[] { new CodeIndexerExpression() };
            yield return new object[] { new CodeIndexerExpression(null) };

            yield return new object[] { new CodeArrayIndexerExpression() };
            yield return new object[] { new CodeArrayIndexerExpression(null) };

            yield return new object[] { new CodeDirectionExpression() };
            yield return new object[] { new CodeDirectionExpression(FieldDirection.In, null) };

            yield return new object[] { new CodeExpressionStatement() };
            yield return new object[] { new CodeExpressionStatement(null) };

            yield return new object[] { new CodeConditionStatement() };
            yield return new object[] { new CodeConditionStatement(null) };

            yield return new object[] { new CodeAssignStatement() };
            yield return new object[] { new CodeAssignStatement(null, new CodePrimitiveExpression(1)) };
            yield return new object[] { new CodeAssignStatement(new CodePrimitiveExpression(1), null) };

            yield return new object[] { new CodeIterationStatement() };
            yield return new object[] { new CodeIterationStatement(null, new CodePrimitiveExpression(1), new CodeMethodReturnStatement()) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), null, new CodeMethodReturnStatement()) };
            yield return new object[] { new CodeIterationStatement(new CodeMethodReturnStatement(), new CodePrimitiveExpression(1), null) };

            yield return new object[] { new CodeAttachEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "name"), null) };

            yield return new object[] { new CodeRemoveEventStatement(new CodeEventReferenceExpression(new CodePrimitiveExpression(1), "name"), null) };
        }

        [Theory]
        [MemberData(nameof(ValidateIdentifiers_NullE_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Fixed NullReferenceException")]
        public void ValidateIdentifiers_NullE_ThrowsArgumentNullException(CodeObject e)
        {
            AssertExtensions.Throws<ArgumentNullException>("e", () => CodeGenerator.ValidateIdentifiers(e));
        }

        private class CustomCodeExpression : CodeExpression
        {
        }

        private class CustomCodeStatement : CodeStatement
        {
        }

        private class CustomCodeTypeMember : CodeTypeMember
        {
        }

        private class CustomCodeDirective : CodeDirective
        {
        }

        private class CustomCodeObject : CodeObject
        {
        }
    }
}
