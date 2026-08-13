// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private readonly Stack<RuleContext> _namespaceOwners = new();
    private readonly Stack<RuleContext> _typeOwners = new();

    /// <summary>
    /// Releases the namespace, type and method state that a top-level declaration introduced.
    /// </summary>
    /// <remarks>
    /// This runs from the <c>decl</c> rule's <c>finally</c> block so that a syntax error inside the
    /// declaration body cannot leak a namespace, type or method scope into the following declarations.
    /// </remarks>
    internal void EndDeclaration(CILParser.DeclContext context)
    {
        EndScopesOwnedBy(context);
    }

    /// <summary>
    /// Releases the type and method state that a class member declaration introduced.
    /// </summary>
    internal void EndClassDeclaration(CILParser.ClassDeclContext context)
    {
        EndClassGenericDirective(context);
        EndPropertyAndEventBodies(context);
        EndScopesOwnedBy(context);
    }

    private void EndScopesOwnedBy(RuleContext owner)
    {
        if (ReferenceEquals(_methodOwner, owner))
        {
            EndMethod();
        }

        if (_typeOwners.Count > 0 && ReferenceEquals(_typeOwners.Peek(), owner))
        {
            EndType();
        }

        if (_namespaceOwners.Count > 0 && ReferenceEquals(_namespaceOwners.Peek(), owner))
        {
            EndNamespace();
        }
    }

    private void EndType()
    {
        CompleteClassMethodOverrides(_currentTypeDefinition.Peek());
        _typeOwners.Pop();
        _currentTypeDefinition.Pop();
        ClearPendingCustomAttributeOwners();
    }

    private void EndNamespace()
    {
        _namespaceOwners.Pop();
        _currentNamespace.Pop();
        ClearPendingCustomAttributeOwners();
    }

    private void ResetTypeScopes()
    {
        _typeOwners.Clear();
        _currentTypeDefinition.Clear();
        _namespaceOwners.Clear();
        _currentNamespace.Clear();
    }

    /// <summary>
    /// Drops the owners that a trailing <c>.custom</c> directive would bind to.
    /// </summary>
    /// <remarks>
    /// A trailing custom attribute only binds to the preceding field, generic parameter or generic
    /// constraint within the same type body, so the pending owners must be dropped whenever a
    /// namespace, type or method boundary is crossed.
    /// </remarks>
    private void ClearPendingCustomAttributeOwners()
    {
        _lastFieldDefinition = null;
        _pendingClassCustomAttributeOwner = null;
    }

#pragma warning disable CA1822 // Mark members as static
        public GrammarResult VisitClassDecls(CILParser.ClassDeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitClassName(CILParser.ClassNameContext context) => VisitClassName(context);

        public GrammarResult.Literal<EntityRegistry.TypeEntity> VisitClassName(CILParser.ClassNameContext context)
            => new(ResolveClassName(GetClassNameValue(context.Value)));

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeq(CILParser.ClassSeqContext context) => VisitClassSeq(context);
        public GrammarResult.FormattedBlob VisitClassSeq(CILParser.ClassSeqContext context)
        {
            BlobBuilder objSeqBlob = new(0);
            foreach (var item in context.classSeqElement())
            {
                objSeqBlob.LinkSuffix(VisitClassSeqElement(item).Value);
            }
            return new(objSeqBlob);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitClassSeqElement(CILParser.ClassSeqElementContext context) => VisitClassSeqElement(context);

        public GrammarResult.FormattedBlob VisitClassSeqElement(CILParser.ClassSeqElementContext context)
        {
            BlobBuilder blob = new();
            if (context.className() is CILParser.ClassNameContext className)
            {
                if (VisitClassName(className).Value is EntityRegistry.IHasReflectionNotation notation)
                {
                    blob.WriteSerializedString(notation.ReflectionNotation);
                }
                else
                {
                    blob.WriteSerializedString("");
                }
                return new(blob);
            }

            blob.WriteSerializedString(
                context.SQSTRING() is { } stringNode
                    ? StringHelpers.ParseQuotedString(stringNode.Symbol.Text)
                    : null);
            return new(blob);
        }
#pragma warning restore CA1822 // Mark members as static
}
