// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.CodeDom.Compiler
{
    // This is an internal helper class which walks the tree for the ValidateIdentifiers API in the CodeGenerator. For the most part the generator code has been copied and
    // turned into validation code. This code will only validate identifiers and types to check that they are ok in a language
    // independent manner. By default, this will not be turned on. This gives clients of codedom a mechanism to
    // protect themselves against certain types of code injection attacks (using identifier and type names).
    // You can pass in any node in the tree that is a subclass of CodeObject.
    internal sealed class CodeValidator
    {
        private static readonly char[] s_newLineChars = new char[] { '\r', '\n', '\u2028', '\u2029', '\u0085' };
        private CodeTypeDeclaration _currentClass;

        internal void ValidateIdentifiers(CodeObject e)
        {
            if (e is CodeCompileUnit)
            {
                ValidateCodeCompileUnit((CodeCompileUnit)e);
            }
            else if (e is CodeComment)
            {
                // do nothing
            }
            else if (e is CodeExpression)
            {
                ValidateExpression((CodeExpression)e);
            }
            else if (e is CodeNamespace)
            {
                ValidateNamespace((CodeNamespace)e);
            }
            else if (e is CodeNamespaceImport)
            {
                ValidateNamespaceImport((CodeNamespaceImport)e);
            }
            else if (e is CodeStatement)
            {
                ValidateStatement((CodeStatement)e);
            }
            else if (e is CodeTypeMember)
            {
                ValidateTypeMember((CodeTypeMember)e);
            }
            else if (e is CodeTypeReference)
            {
                ValidateTypeReference((CodeTypeReference)e);
            }
            else if (e is CodeDirective)
            {
                ValidateCodeDirective((CodeDirective)e);
            }
            else if (e == null)
            {
                throw new ArgumentNullException(nameof(e));
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }
        }

        private void ValidateTypeMember(CodeTypeMember e)
        {
            ValidateCommentStatements(e.Comments);
            ValidateCodeDirectives(e.StartDirectives);
            ValidateCodeDirectives(e.EndDirectives);

            if (e is CodeMemberEvent)
            {
                ValidateEvent((CodeMemberEvent)e);
            }
            else if (e is CodeMemberField)
            {
                ValidateField((CodeMemberField)e);
            }
            else if (e is CodeMemberMethod)
            {
                ValidateMemberMethod((CodeMemberMethod)e);
            }
            else if (e is CodeMemberProperty)
            {
                ValidateProperty((CodeMemberProperty)e);
            }
            else if (e is CodeSnippetTypeMember)
            {
                // do nothing
            }
            else if (e is CodeTypeDeclaration)
            {
                ValidateTypeDeclaration((CodeTypeDeclaration)e);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }
        }

        private void ValidateCodeCompileUnit(CodeCompileUnit e)
        {
            ValidateCodeDirectives(e.StartDirectives);
            ValidateCodeDirectives(e.EndDirectives);
            if (e is CodeSnippetCompileUnit)
            {
                // do nothing
            }
            else
            {
                ValidateCompileUnitStart(e);
                ValidateNamespaces(e);
            }
        }

        private void ValidateCompileUnitStart(CodeCompileUnit e)
        {
            if (e.AssemblyCustomAttributes.Count > 0)
            {
                ValidateAttributes(e.AssemblyCustomAttributes);
            }
        }

        private void ValidateNamespaces(CodeCompileUnit e)
        {
            foreach (CodeNamespace n in e.Namespaces)
            {
                ValidateNamespace(n);
            }
        }

        private void ValidateNamespace(CodeNamespace e)
        {
            ValidateCommentStatements(e.Comments);
            ValidateNamespaceStart(e);
            ValidateNamespaceImports(e);
            ValidateTypes(e);
        }


        private static void ValidateNamespaceStart(CodeNamespace e)
        {
            if (!string.IsNullOrEmpty(e.Name))
            {
                ValidateTypeName(e, nameof(e.Name), e.Name);
            }
        }

        private static void ValidateNamespaceImports(CodeNamespace e)
        {
            foreach (CodeNamespaceImport imp in e.Imports)
            {
                ValidateNamespaceImport(imp);
            }
        }

        private static void ValidateNamespaceImport(CodeNamespaceImport e)
        {
            ValidateTypeName(e, nameof(e.Namespace), e.Namespace);
        }

        private void ValidateAttributes(CodeAttributeDeclarationCollection attributes)
        {
            if (attributes.Count == 0) return;
            foreach (CodeAttributeDeclaration current in attributes)
            {
                ValidateTypeName(current, nameof(current.Name), current.Name);
                ValidateTypeReference(current.AttributeType);

                foreach (CodeAttributeArgument arg in current.Arguments)
                {
                    ValidateAttributeArgument(arg);
                }
            }
        }

        private void ValidateAttributeArgument(CodeAttributeArgument arg)
        {
            if (!string.IsNullOrEmpty(arg.Name))
            {
                ValidateIdentifier(arg, nameof(arg.Name), arg.Name);
            }
            ValidateExpression(arg.Value);
        }

        private void ValidateTypes(CodeNamespace e)
        {
            foreach (CodeTypeDeclaration type in e.Types)
            {
                ValidateTypeDeclaration(type);
            }
        }

        private void ValidateTypeDeclaration(CodeTypeDeclaration e)
        {
            // This function can be called recursively and will modify the global variable currentClass
            // We will save currentClass to a local, modify it to do whatever we want and restore it back when we exit so that it is re-entrant.
            CodeTypeDeclaration savedClass = _currentClass;
            _currentClass = e;

            ValidateTypeStart(e);
            ValidateTypeParameters(e.TypeParameters);
            ValidateTypeMembers(e); // Recursive call can come from here.
            ValidateTypeReferences(e.BaseTypes);

            _currentClass = savedClass;
        }

        private void ValidateTypeMembers(CodeTypeDeclaration e)
        {
            foreach (CodeTypeMember currentMember in e.Members)
            {
                ValidateTypeMember(currentMember);
            }
        }

        private void ValidateTypeParameters(CodeTypeParameterCollection parameters)
        {
            for (int i = 0; i < parameters.Count; i++)
            {
                ValidateTypeParameter(parameters[i]);
            }
        }

        private void ValidateTypeParameter(CodeTypeParameter e)
        {
            ValidateIdentifier(e, nameof(e.Name), e.Name);
            ValidateTypeReferences(e.Constraints);
            ValidateAttributes(e.CustomAttributes);
        }

        private void ValidateField(CodeMemberField e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }

            ValidateIdentifier(e, nameof(e.Name), e.Name);
            if (!IsCurrentEnum)
            {
                ValidateTypeReference(e.Type);
            }

            if (e.InitExpression != null)
            {
                ValidateExpression(e.InitExpression);
            }
        }

        private void ValidateConstructor(CodeConstructor e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }

            ValidateParameters(e.Parameters);

            CodeExpressionCollection baseArgs = e.BaseConstructorArgs;
            CodeExpressionCollection thisArgs = e.ChainedConstructorArgs;

            if (baseArgs.Count > 0)
            {
                ValidateExpressionList(baseArgs);
            }

            if (thisArgs.Count > 0)
            {
                ValidateExpressionList(thisArgs);
            }

            ValidateStatements(e.Statements);
        }

        private void ValidateProperty(CodeMemberProperty e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }

            ValidateTypeReference(e.Type);
            ValidateTypeReferences(e.ImplementationTypes);

            if (e.PrivateImplementationType != null && !IsCurrentInterface)
            {
                ValidateTypeReference(e.PrivateImplementationType);
            }

            if (e.Parameters.Count > 0 && string.Equals(e.Name, "Item", StringComparison.OrdinalIgnoreCase))
            {
                ValidateParameters(e.Parameters);
            }
            else
            {
                ValidateIdentifier(e, nameof(e.Name), e.Name);
            }

            if (e.HasGet)
            {
                if (!(IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract))
                {
                    ValidateStatements(e.GetStatements);
                }
            }

            if (e.HasSet)
            {
                if (!(IsCurrentInterface || (e.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Abstract))
                {
                    ValidateStatements(e.SetStatements);
                }
            }
        }

        private void ValidateMemberMethod(CodeMemberMethod e)
        {
            ValidateCommentStatements(e.Comments);

            ValidateTypeParameters(e.TypeParameters);
            ValidateTypeReferences(e.ImplementationTypes);

            if (e is CodeEntryPointMethod)
            {
                ValidateStatements(((CodeEntryPointMethod)e).Statements);
            }
            else if (e is CodeConstructor)
            {
                ValidateConstructor((CodeConstructor)e);
            }
            else if (e is CodeTypeConstructor)
            {
                ValidateTypeConstructor((CodeTypeConstructor)e);
            }
            else
            {
                ValidateMethod(e);
            }
        }

        private void ValidateTypeConstructor(CodeTypeConstructor e)
        {
            ValidateStatements(e.Statements);
        }

        private void ValidateMethod(CodeMemberMethod e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }
            if (e.ReturnTypeCustomAttributes.Count > 0)
            {
                ValidateAttributes(e.ReturnTypeCustomAttributes);
            }

            ValidateTypeReference(e.ReturnType);
            if (e.PrivateImplementationType != null)
            {
                ValidateTypeReference(e.PrivateImplementationType);
            }

            ValidateIdentifier(e, nameof(e.Name), e.Name);
            ValidateParameters(e.Parameters);

            if (!IsCurrentInterface
                && (e.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Abstract)
            {
                ValidateStatements(e.Statements);
            }
        }

        private void ValidateTypeStart(CodeTypeDeclaration e)
        {
            ValidateCommentStatements(e.Comments);
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }

            ValidateIdentifier(e, nameof(e.Name), e.Name);
            if (e is CodeTypeDelegate del)
            {
                ValidateTypeReference(del.ReturnType);
                ValidateParameters(del.Parameters);
            }
            else
            {
                foreach (CodeTypeReference typeRef in e.BaseTypes)
                {
                    ValidateTypeReference(typeRef);
                }
            }
        }

        private static void ValidateCommentStatements(CodeCommentStatementCollection e)
        {
            foreach (CodeCommentStatement comment in e)
            {
                ValidateCommentStatement(comment);
            }
        }

        private static void ValidateCommentStatement(CodeCommentStatement e)
        {
        }

        private void ValidateStatement(CodeStatement e!!)
        {
            ValidateCodeDirectives(e.StartDirectives);
            ValidateCodeDirectives(e.EndDirectives);

            if (e is CodeCommentStatement)
            {
                ValidateCommentStatement((CodeCommentStatement)e);
            }
            else if (e is CodeMethodReturnStatement)
            {
                ValidateMethodReturnStatement((CodeMethodReturnStatement)e);
            }
            else if (e is CodeConditionStatement)
            {
                ValidateConditionStatement((CodeConditionStatement)e);
            }
            else if (e is CodeTryCatchFinallyStatement)
            {
                ValidateTryCatchFinallyStatement((CodeTryCatchFinallyStatement)e);
            }
            else if (e is CodeAssignStatement)
            {
                ValidateAssignStatement((CodeAssignStatement)e);
            }
            else if (e is CodeExpressionStatement)
            {
                ValidateExpressionStatement((CodeExpressionStatement)e);
            }
            else if (e is CodeIterationStatement)
            {
                ValidateIterationStatement((CodeIterationStatement)e);
            }
            else if (e is CodeThrowExceptionStatement)
            {
                ValidateThrowExceptionStatement((CodeThrowExceptionStatement)e);
            }
            else if (e is CodeSnippetStatement)
            {
                // do nothing
            }
            else if (e is CodeVariableDeclarationStatement)
            {
                ValidateVariableDeclarationStatement((CodeVariableDeclarationStatement)e);
            }
            else if (e is CodeAttachEventStatement)
            {
                ValidateAttachEventStatement((CodeAttachEventStatement)e);
            }
            else if (e is CodeRemoveEventStatement)
            {
                ValidateRemoveEventStatement((CodeRemoveEventStatement)e);
            }
            else if (e is CodeGotoStatement)
            {
                ValidateGotoStatement((CodeGotoStatement)e);
            }
            else if (e is CodeLabeledStatement)
            {
                ValidateLabeledStatement((CodeLabeledStatement)e);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }
        }

        private void ValidateStatements(CodeStatementCollection stmts)
        {
            foreach (CodeStatement stmt in stmts)
            {
                ValidateStatement(stmt);
            }
        }

        private void ValidateExpressionStatement(CodeExpressionStatement e)
        {
            ValidateExpression(e.Expression);
        }

        private void ValidateIterationStatement(CodeIterationStatement e)
        {
            ValidateStatement(e.InitStatement);
            ValidateExpression(e.TestExpression);
            ValidateStatement(e.IncrementStatement);
            ValidateStatements(e.Statements);
        }

        private void ValidateThrowExceptionStatement(CodeThrowExceptionStatement e)
        {
            if (e.ToThrow != null)
            {
                ValidateExpression(e.ToThrow);
            }
        }

        private void ValidateMethodReturnStatement(CodeMethodReturnStatement e)
        {
            if (e.Expression != null)
            {
                ValidateExpression(e.Expression);
            }
        }

        private void ValidateConditionStatement(CodeConditionStatement e)
        {
            ValidateExpression(e.Condition);
            ValidateStatements(e.TrueStatements);

            CodeStatementCollection falseStatements = e.FalseStatements;
            if (falseStatements.Count > 0)
            {
                ValidateStatements(e.FalseStatements);
            }
        }

        private void ValidateTryCatchFinallyStatement(CodeTryCatchFinallyStatement e)
        {
            ValidateStatements(e.TryStatements);
            CodeCatchClauseCollection catches = e.CatchClauses;
            if (catches.Count > 0)
            {
                foreach (CodeCatchClause current in catches)
                {
                    ValidateTypeReference(current.CatchExceptionType);
                    ValidateIdentifier(current, nameof(current.LocalName), current.LocalName);
                    ValidateStatements(current.Statements);
                }
            }

            CodeStatementCollection finallyStatements = e.FinallyStatements;
            if (finallyStatements.Count > 0)
            {
                ValidateStatements(finallyStatements);
            }
        }

        private void ValidateAssignStatement(CodeAssignStatement e)
        {
            ValidateExpression(e.Left);
            ValidateExpression(e.Right);
        }

        private void ValidateAttachEventStatement(CodeAttachEventStatement e)
        {
            ValidateEventReferenceExpression(e.Event);
            ValidateExpression(e.Listener);
        }

        private void ValidateRemoveEventStatement(CodeRemoveEventStatement e)
        {
            ValidateEventReferenceExpression(e.Event);
            ValidateExpression(e.Listener);
        }

        private static void ValidateGotoStatement(CodeGotoStatement e)
        {
            ValidateIdentifier(e, nameof(e.Label), e.Label);
        }

        private void ValidateLabeledStatement(CodeLabeledStatement e)
        {
            ValidateIdentifier(e, nameof(e.Label), e.Label);
            if (e.Statement != null)
            {
                ValidateStatement(e.Statement);
            }
        }

        private void ValidateVariableDeclarationStatement(CodeVariableDeclarationStatement e)
        {
            ValidateTypeReference(e.Type);
            ValidateIdentifier(e, nameof(e.Name), e.Name);
            if (e.InitExpression != null)
            {
                ValidateExpression(e.InitExpression);
            }
        }

        private void ValidateEvent(CodeMemberEvent e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }
            if (e.PrivateImplementationType != null)
            {
                ValidateTypeReference(e.Type);
                ValidateIdentifier(e, nameof(e.Name), e.Name);
            }

            ValidateTypeReferences(e.ImplementationTypes);
        }

        private void ValidateParameters(CodeParameterDeclarationExpressionCollection parameters)
        {
            foreach (CodeParameterDeclarationExpression current in parameters)
            {
                ValidateParameterDeclarationExpression(current);
            }
        }

        private void ValidateExpressionList(CodeExpressionCollection expressions)
        {
            foreach (CodeExpression current in expressions)
            {
                ValidateExpression(current);
            }
        }

        private static void ValidateTypeReference(CodeTypeReference e)
        {
            ValidateTypeName(e, nameof(e.BaseType), e.BaseType);
            ValidateArity(e);
            ValidateTypeReferences(e.TypeArguments);
        }

        private static void ValidateTypeReferences(CodeTypeReferenceCollection refs)
        {
            for (int i = 0; i < refs.Count; i++)
            {
                ValidateTypeReference(refs[i]);
            }
        }

        private static void ValidateArity(CodeTypeReference e)
        {
            // Verify that the number of TypeArguments agrees with the arity on the type.
            string baseType = e.BaseType;
            int totalTypeArgs = 0;
            for (int i = 0; i < baseType.Length; i++)
            {
                if (baseType[i] == '`')
                {
                    i++;    // skip the '
                    int numTypeArgs = 0;
                    while (i < baseType.Length && baseType[i] >= '0' && baseType[i] <= '9')
                    {
                        numTypeArgs = numTypeArgs * 10 + (baseType[i] - '0');
                        i++;
                    }

                    totalTypeArgs += numTypeArgs;
                }
            }

            // Check if we have zero type args for open types.
            if ((totalTypeArgs != e.TypeArguments.Count) && (e.TypeArguments.Count != 0))
            {
                throw new ArgumentException(SR.Format(SR.ArityDoesntMatch, baseType, e.TypeArguments.Count), nameof(e));
            }
        }

        private static void ValidateTypeName(object e, string propertyName, string typeName)
        {
            if (!CodeGenerator.IsValidLanguageIndependentTypeName(typeName))
            {
                string message = SR.Format(SR.InvalidTypeName, typeName, propertyName, e.GetType().FullName);
                throw new ArgumentException(message, nameof(e));
            }
        }

        private static void ValidateIdentifier(object e, string propertyName, string identifier)
        {
            if (!CodeGenerator.IsValidLanguageIndependentIdentifier(identifier))
            {
                string message = SR.Format(SR.InvalidLanguageIdentifier, identifier, propertyName, e.GetType().FullName);
                throw new ArgumentException(message, nameof(e));
            }
        }

        private void ValidateExpression(CodeExpression e)
        {
            if (e is CodeArrayCreateExpression)
            {
                ValidateArrayCreateExpression((CodeArrayCreateExpression)e);
            }
            else if (e is CodeBaseReferenceExpression)
            {
                // Nothing to validate
            }
            else if (e is CodeBinaryOperatorExpression)
            {
                ValidateBinaryOperatorExpression((CodeBinaryOperatorExpression)e);
            }
            else if (e is CodeCastExpression)
            {
                ValidateCastExpression((CodeCastExpression)e);
            }
            else if (e is CodeDefaultValueExpression)
            {
                ValidateDefaultValueExpression((CodeDefaultValueExpression)e);
            }
            else if (e is CodeDelegateCreateExpression)
            {
                ValidateDelegateCreateExpression((CodeDelegateCreateExpression)e);
            }
            else if (e is CodeFieldReferenceExpression)
            {
                ValidateFieldReferenceExpression((CodeFieldReferenceExpression)e);
            }
            else if (e is CodeArgumentReferenceExpression)
            {
                ValidateArgumentReferenceExpression((CodeArgumentReferenceExpression)e);
            }
            else if (e is CodeVariableReferenceExpression)
            {
                ValidateVariableReferenceExpression((CodeVariableReferenceExpression)e);
            }
            else if (e is CodeIndexerExpression)
            {
                ValidateIndexerExpression((CodeIndexerExpression)e);
            }
            else if (e is CodeArrayIndexerExpression)
            {
                ValidateArrayIndexerExpression((CodeArrayIndexerExpression)e);
            }
            else if (e is CodeSnippetExpression)
            {
                // do nothing
            }
            else if (e is CodeMethodInvokeExpression)
            {
                ValidateMethodInvokeExpression((CodeMethodInvokeExpression)e);
            }
            else if (e is CodeMethodReferenceExpression)
            {
                ValidateMethodReferenceExpression((CodeMethodReferenceExpression)e);
            }
            else if (e is CodeEventReferenceExpression)
            {
                ValidateEventReferenceExpression((CodeEventReferenceExpression)e);
            }
            else if (e is CodeDelegateInvokeExpression)
            {
                ValidateDelegateInvokeExpression((CodeDelegateInvokeExpression)e);
            }
            else if (e is CodeObjectCreateExpression)
            {
                ValidateObjectCreateExpression((CodeObjectCreateExpression)e);
            }
            else if (e is CodeParameterDeclarationExpression)
            {
                ValidateParameterDeclarationExpression((CodeParameterDeclarationExpression)e);
            }
            else if (e is CodeDirectionExpression)
            {
                ValidateDirectionExpression((CodeDirectionExpression)e);
            }
            else if (e is CodePrimitiveExpression)
            {
                // do nothing
            }
            else if (e is CodePropertyReferenceExpression)
            {
                ValidatePropertyReferenceExpression((CodePropertyReferenceExpression)e);
            }
            else if (e is CodePropertySetValueReferenceExpression)
            {
                // Do nothing
            }
            else if (e is CodeThisReferenceExpression)
            {
                // Do nothing
            }
            else if (e is CodeTypeReferenceExpression)
            {
                ValidateTypeReference(((CodeTypeReferenceExpression)e).Type);
            }
            else if (e is CodeTypeOfExpression)
            {
                ValidateTypeOfExpression((CodeTypeOfExpression)e);
            }
            else
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e));
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
                }
            }
        }

        private void ValidateArrayCreateExpression(CodeArrayCreateExpression e)
        {
            ValidateTypeReference(e.CreateType);
            CodeExpressionCollection init = e.Initializers;
            if (init.Count > 0)
            {
                ValidateExpressionList(init);
            }
            else
            {
                if (e.SizeExpression != null)
                {
                    ValidateExpression(e.SizeExpression);
                }
            }
        }

        private void ValidateBinaryOperatorExpression(CodeBinaryOperatorExpression e)
        {
            ValidateExpression(e.Left);
            ValidateExpression(e.Right);
        }

        private void ValidateCastExpression(CodeCastExpression e)
        {
            ValidateTypeReference(e.TargetType);
            ValidateExpression(e.Expression);
        }

        private static void ValidateDefaultValueExpression(CodeDefaultValueExpression e)
        {
            ValidateTypeReference(e.Type);
        }

        private void ValidateDelegateCreateExpression(CodeDelegateCreateExpression e)
        {
            ValidateTypeReference(e.DelegateType);
            ValidateExpression(e.TargetObject);
            ValidateIdentifier(e, nameof(e.MethodName), e.MethodName);
        }

        private void ValidateFieldReferenceExpression(CodeFieldReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                ValidateExpression(e.TargetObject);
            }
            ValidateIdentifier(e, nameof(e.FieldName), e.FieldName);
        }

        private static void ValidateArgumentReferenceExpression(CodeArgumentReferenceExpression e)
        {
            ValidateIdentifier(e, nameof(e.ParameterName), e.ParameterName);
        }

        private static void ValidateVariableReferenceExpression(CodeVariableReferenceExpression e)
        {
            ValidateIdentifier(e, nameof(e.VariableName), e.VariableName);
        }

        private void ValidateIndexerExpression(CodeIndexerExpression e)
        {
            ValidateExpression(e.TargetObject);
            foreach (CodeExpression exp in e.Indices)
            {
                ValidateExpression(exp);
            }
        }

        private void ValidateArrayIndexerExpression(CodeArrayIndexerExpression e)
        {
            ValidateExpression(e.TargetObject);
            foreach (CodeExpression exp in e.Indices)
            {
                ValidateExpression(exp);
            }
        }

        private void ValidateMethodInvokeExpression(CodeMethodInvokeExpression e)
        {
            ValidateMethodReferenceExpression(e.Method);
            ValidateExpressionList(e.Parameters);
        }

        private void ValidateMethodReferenceExpression(CodeMethodReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                ValidateExpression(e.TargetObject);
            }
            ValidateIdentifier(e, nameof(e.MethodName), e.MethodName);
            ValidateTypeReferences(e.TypeArguments);
        }

        private void ValidateEventReferenceExpression(CodeEventReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                ValidateExpression(e.TargetObject);
            }
            ValidateIdentifier(e, nameof(e.EventName), e.EventName);
        }

        private void ValidateDelegateInvokeExpression(CodeDelegateInvokeExpression e)
        {
            if (e.TargetObject != null)
            {
                ValidateExpression(e.TargetObject);
            }
            ValidateExpressionList(e.Parameters);
        }

        private void ValidateObjectCreateExpression(CodeObjectCreateExpression e)
        {
            ValidateTypeReference(e.CreateType);
            ValidateExpressionList(e.Parameters);
        }

        private void ValidateParameterDeclarationExpression(CodeParameterDeclarationExpression e)
        {
            if (e.CustomAttributes.Count > 0)
            {
                ValidateAttributes(e.CustomAttributes);
            }

            ValidateTypeReference(e.Type);
            ValidateIdentifier(e, nameof(e.Name), e.Name);
        }

        private void ValidateDirectionExpression(CodeDirectionExpression e)
        {
            ValidateExpression(e.Expression);
        }

        private void ValidatePropertyReferenceExpression(CodePropertyReferenceExpression e)
        {
            if (e.TargetObject != null)
            {
                ValidateExpression(e.TargetObject);
            }
            ValidateIdentifier(e, nameof(e.PropertyName), e.PropertyName);
        }

        private static void ValidateTypeOfExpression(CodeTypeOfExpression e)
        {
            ValidateTypeReference(e.Type);
        }

        private static void ValidateCodeDirectives(CodeDirectiveCollection e)
        {
            for (int i = 0; i < e.Count; i++)
                ValidateCodeDirective(e[i]);
        }

        private static void ValidateCodeDirective(CodeDirective e)
        {
            if (e is CodeChecksumPragma)
            {
                ValidateChecksumPragma((CodeChecksumPragma)e);
            }
            else if (e is CodeRegionDirective)
            {
                ValidateRegionDirective((CodeRegionDirective)e);
            }
            else
            {
                throw new ArgumentException(SR.Format(SR.InvalidElementType, e.GetType().FullName), nameof(e));
            }
        }

        private static void ValidateChecksumPragma(CodeChecksumPragma e)
        {
            if (e.FileName.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                throw new ArgumentException(SR.Format(SR.InvalidPathCharsInChecksum, e.FileName), nameof(e));
        }

        private static void ValidateRegionDirective(CodeRegionDirective e)
        {
            if (e.RegionText.IndexOfAny(s_newLineChars) != -1)
                throw new ArgumentException(SR.Format(SR.InvalidRegion, e.RegionText), nameof(e));
        }

        private bool IsCurrentInterface => _currentClass != null && !(_currentClass is CodeTypeDelegate) ? _currentClass.IsInterface : false;

        private bool IsCurrentEnum => _currentClass != null && !(_currentClass is CodeTypeDelegate) ? _currentClass.IsEnum : false;
    }
}
