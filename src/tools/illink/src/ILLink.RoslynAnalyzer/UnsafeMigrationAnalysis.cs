// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: InternalsVisibleTo(
    "ILLink.CodeFixProvider, PublicKey=0024000004800000940000000602000000240000525341310004000001000100b5fc90e7027f67871e773a8fde8938c81dd402ba65b9201d60593e96c492651e889cc13f1415ebb53fac1131ae0bd333c5ee6021672d9718ea31a8aebd0da0072f25d87dba6fc90ffd598ed4da35e44c398c454307e8e33b8426143daec9f596836f97c8f74750e5975c64e2189f45def46b2a2b1247adc3652bf5c308055da9")]

namespace ILLink.RoslynAnalyzer
{
    internal static class UnsafeMigrationAnalysis
    {
        public const string RequiresUnsafeAttribute = "System.Runtime.CompilerServices.RequiresUnsafeAttribute";
        public const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
        private const string MemorySafetyRulesAttribute = "System.Runtime.CompilerServices.MemorySafetyRulesAttribute";
        private const string SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

        private static readonly string[] s_unsafeOperationDiagnosticIds = ["CS9360", "CS9361", "CS9362", "CS9363"];

        public readonly struct ModifierRemoval
        {
            public ModifierRemoval(SyntaxNode declaration, SyntaxToken modifier)
            {
                Declaration = declaration;
                Modifier = modifier;
            }

            public SyntaxNode Declaration { get; }

            public SyntaxToken Modifier { get; }
        }

        public readonly struct DeclarationUpdate
        {
            public DeclarationUpdate(SyntaxNode declaration, bool addUnsafeModifier, bool addSafetyDocumentation)
            {
                Declaration = declaration;
                AddUnsafeModifier = addUnsafeModifier;
                AddSafetyDocumentation = addSafetyDocumentation;
            }

            public SyntaxNode Declaration { get; }

            public bool AddUnsafeModifier { get; }

            public bool AddSafetyDocumentation { get; }
        }

        public static ImmutableArray<ModifierRemoval> GetModifierRemovals(
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<ModifierRemoval>();
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);

            foreach (SyntaxNode declaration in root.DescendantNodesAndSelf())
            {
                SyntaxToken unsafeModifier = GetModifiers(declaration).FirstOrDefault(static modifier => modifier.IsKind(SyntaxKind.UnsafeKeyword));
                if (unsafeModifier.RawKind == 0 || !ShouldRemoveUnsafeModifier(declaration, semanticModel, cancellationToken))
                    continue;

                builder.Add(new(declaration, unsafeModifier));
            }

            return builder.ToImmutable();
        }

        public static ImmutableArray<DeclarationUpdate> GetDeclarationUpdates(
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            var updates = new Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)>();

            AddInteropUpdates(root, semanticModel, updates, cancellationToken);
            AddExplicitLayoutUpdates(root, semanticModel, updates, cancellationToken);
            AddPartialMemberUpdates(root, semanticModel, updates, cancellationToken);
            AddContractUpdates(root, semanticModel, updates, cancellationToken);

            return updates
                .OrderBy(static pair => pair.Key.SpanStart)
                .Select(static pair => new DeclarationUpdate(pair.Key, pair.Value.AddUnsafe, pair.Value.AddSafetyDocumentation))
                .ToImmutableArray();
        }

        public static ImmutableArray<Location> GetUnsafeOperationLocations(
            SemanticModel semanticModel,
            bool skipLocalsInit,
            bool includeCompilerDiagnostics,
            CancellationToken cancellationToken)
        {
            var locations = new Dictionary<TextSpan, Location>();

            if (includeCompilerDiagnostics)
            {
                foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken)
                    .Where(diagnostic => diagnostic.Location.SourceTree == semanticModel.SyntaxTree &&
                        s_unsafeOperationDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal)))
                {
                    AddLocation(locations, diagnostic.Location);
                }
            }

            SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
            foreach (ExpressionSyntax expression in root.DescendantNodes().OfType<ExpressionSyntax>())
            {
                if (!IsInUnsafeContext(expression) &&
                    RequiresUnsafeContext(expression, semanticModel, skipLocalsInit, cancellationToken))
                {
                    AddLocation(locations, expression.GetLocation());
                }
            }

            return locations.Values
                .OrderBy(static location => location.SourceSpan.Start)
                .ToImmutableArray();
        }

        public static bool HasSafetyDocumentation(SyntaxNode declaration)
            => declaration.GetLeadingTrivia()
                .Select(static trivia => trivia.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .SelectMany(static documentation => documentation.DescendantNodes())
                .Any(static node => node switch
                {
                    XmlElementSyntax element => element.StartTag.Name.LocalName.ValueText == "safety",
                    XmlEmptyElementSyntax element => element.Name.LocalName.ValueText == "safety",
                    _ => false
                });

        public static SyntaxTokenList GetModifiers(SyntaxNode declaration)
            => declaration switch
            {
                BaseTypeDeclarationSyntax type => type.Modifiers,
                DelegateDeclarationSyntax @delegate => @delegate.Modifiers,
                BaseMethodDeclarationSyntax method => method.Modifiers,
                LocalFunctionStatementSyntax localFunction => localFunction.Modifiers,
                PropertyDeclarationSyntax property => property.Modifiers,
                IndexerDeclarationSyntax indexer => indexer.Modifiers,
                EventDeclarationSyntax @event => @event.Modifiers,
                EventFieldDeclarationSyntax @event => @event.Modifiers,
                FieldDeclarationSyntax field => field.Modifiers,
                AccessorDeclarationSyntax accessor => accessor.Modifiers,
                _ => default
            };

        public static bool HasUnsafeModifier(SyntaxNode declaration)
            => GetModifiers(declaration).Any(static modifier => modifier.IsKind(SyntaxKind.UnsafeKeyword));

        public static bool HasSafeModifier(SyntaxNode declaration)
            => GetModifiers(declaration).Any(static modifier => modifier.ValueText == "safe");

        public static ImmutableArray<string> GetUnsafeContractEventVariableNames(
            EventFieldDeclarationSyntax declaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            foreach (VariableDeclaratorSyntax variable in declaration.Declaration.Variables)
            {
                if (semanticModel.GetDeclaredSymbol(variable, cancellationToken) is IEventSymbol @event &&
                    RequiresUnsafeContract(@event))
                {
                    builder.Add(variable.Identifier.ValueText);
                }
            }

            return builder.ToImmutable();
        }

        private static bool ShouldRemoveUnsafeModifier(
            SyntaxNode declaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or DestructorDeclarationSyntax)
                return true;

            if (declaration is ConstructorDeclarationSyntax constructor &&
                constructor.Modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
            {
                return true;
            }

            if (declaration is AccessorDeclarationSyntax or FieldDeclarationSyntax)
                return false;

            if (declaration is not (BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or
                PropertyDeclarationSyntax or IndexerDeclarationSyntax or
                EventDeclarationSyntax or EventFieldDeclarationSyntax))
            {
                return false;
            }

            if (IsInteropDeclaration(declaration, semanticModel, cancellationToken) ||
                IsUnsafeModifierRequiredByContract(declaration, semanticModel, cancellationToken))
            {
                return false;
            }

            return !HasSafetyDocumentation(declaration) &&
                !HasPointerInSignature(declaration, semanticModel, cancellationToken);
        }

        private static bool HasPointerInSignature(
            SyntaxNode declaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ISymbol? symbol = declaration switch
            {
                EventFieldDeclarationSyntax eventField when eventField.Declaration.Variables.Count == 1 =>
                    semanticModel.GetDeclaredSymbol(eventField.Declaration.Variables[0], cancellationToken),
                _ => semanticModel.GetDeclaredSymbol(declaration, cancellationToken)
            };

            return symbol switch
            {
                IMethodSymbol method => ContainsPointer(method.ReturnType) ||
                    method.Parameters.Any(static parameter => ContainsPointer(parameter.Type)),
                IPropertySymbol property => ContainsPointer(property.Type) ||
                    property.Parameters.Any(static parameter => ContainsPointer(parameter.Type)),
                IEventSymbol @event => ContainsPointer(@event.Type),
                IFieldSymbol field => ContainsPointer(field.Type),
                _ => GetSignatureTypes(declaration).Any(static type =>
                    type.DescendantNodesAndSelf().Any(static node => node is PointerTypeSyntax or FunctionPointerTypeSyntax))
            };
        }

        private static IEnumerable<TypeSyntax> GetSignatureTypes(SyntaxNode declaration)
            => declaration switch
            {
                MethodDeclarationSyntax method => [method.ReturnType, .. method.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>()],
                ConstructorDeclarationSyntax constructor => constructor.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>(),
                OperatorDeclarationSyntax @operator => [@operator.ReturnType, .. @operator.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>()],
                ConversionOperatorDeclarationSyntax conversion => [conversion.Type, .. conversion.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>()],
                LocalFunctionStatementSyntax localFunction => [localFunction.ReturnType, .. localFunction.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>()],
                PropertyDeclarationSyntax property => [property.Type],
                IndexerDeclarationSyntax indexer => [indexer.Type, .. indexer.ParameterList.Parameters.Select(static parameter => parameter.Type).OfType<TypeSyntax>()],
                EventDeclarationSyntax @event => [@event.Type],
                EventFieldDeclarationSyntax @event => [@event.Declaration.Type],
                _ => []
            };

        private static bool ContainsPointer(ITypeSymbol type)
            => type switch
            {
                IPointerTypeSymbol or IFunctionPointerTypeSymbol => true,
                IArrayTypeSymbol array => ContainsPointer(array.ElementType),
                INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsPointer),
                _ => false
            };

        private static bool RequiresUnsafeContext(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            bool skipLocalsInit,
            CancellationToken cancellationToken)
        {
            switch (expression)
            {
                case PrefixUnaryExpressionSyntax prefix
                    when prefix.IsKind(SyntaxKind.PointerIndirectionExpression):
                case MemberAccessExpressionSyntax memberAccess
                    when memberAccess.IsKind(SyntaxKind.PointerMemberAccessExpression):
                    return true;

                case ElementAccessExpressionSyntax elementAccess
                    when IsPointerElementAccess(elementAccess, semanticModel, cancellationToken):
                    return true;

                case StackAllocArrayCreationExpressionSyntax stackAlloc
                    when IsUnsafeStackAlloc(stackAlloc, semanticModel, skipLocalsInit, cancellationToken):
                    return true;

                case InvocationExpressionSyntax invocation:
                    if (semanticModel.GetTypeInfo(invocation.Expression, cancellationToken).Type is IFunctionPointerTypeSymbol)
                        return true;

                    return GetReferencedSymbol(invocation, semanticModel, cancellationToken) is { } invocationSymbol &&
                        SymbolUseRequiresUnsafe(invocation, invocationSymbol);

                case ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax:
                    return GetReferencedSymbol(expression, semanticModel, cancellationToken) is IMethodSymbol constructor &&
                        IsRequiresUnsafe(constructor);

                case IdentifierNameSyntax identifier
                    when identifier.Parent is InvocationExpressionSyntax identifierInvocation &&
                        identifierInvocation.Expression == identifier:
                case GenericNameSyntax genericName
                    when genericName.Parent is InvocationExpressionSyntax genericInvocation &&
                        genericInvocation.Expression == genericName:
                case SimpleNameSyntax name
                    when name.Parent is MemberAccessExpressionSyntax containingMemberAccess &&
                        containingMemberAccess.Name == name:
                case MemberAccessExpressionSyntax invokedMemberAccess
                    when invokedMemberAccess.Parent is InvocationExpressionSyntax memberInvocation &&
                        memberInvocation.Expression == invokedMemberAccess:
                    return false;
            }

            return IsSymbolBearingExpression(expression) &&
                GetReferencedSymbol(expression, semanticModel, cancellationToken) is { } symbol &&
                SymbolUseRequiresUnsafe(expression, symbol);
        }

        private static bool IsPointerElementAccess(
            ElementAccessExpressionSyntax elementAccess,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetTypeInfo(elementAccess.Expression, cancellationToken).Type is IPointerTypeSymbol)
                return true;

            return GetReferencedSymbol(elementAccess.Expression, semanticModel, cancellationToken) is IFieldSymbol
            {
                IsFixedSizeBuffer: true
            };
        }

        private static bool IsUnsafeStackAlloc(
            StackAllocArrayCreationExpressionSyntax stackAlloc,
            SemanticModel semanticModel,
            bool skipLocalsInit,
            CancellationToken cancellationToken)
        {
            if (stackAlloc.Initializer is not null)
                return false;

            if (!IsSpanStackAlloc(stackAlloc, semanticModel, cancellationToken))
                return false;

            if (skipLocalsInit)
                return true;

            for (ISymbol? symbol = semanticModel.GetEnclosingSymbol(stackAlloc.SpanStart, cancellationToken);
                symbol is not null;
                symbol = symbol.ContainingSymbol)
            {
                if (HasAttribute(symbol, SkipLocalsInitAttribute))
                    return true;
            }

            return semanticModel.Compilation.Assembly.Modules.Any(module =>
                HasAttribute(module, SkipLocalsInitAttribute));
        }

        private static bool IsSpanStackAlloc(
            StackAllocArrayCreationExpressionSyntax stackAlloc,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            for (SyntaxNode? node = stackAlloc; node is not null && node is not StatementSyntax; node = node.Parent)
            {
                TypeInfo typeInfo = node is ExpressionSyntax expression
                    ? semanticModel.GetTypeInfo(expression, cancellationToken)
                    : default;

                if (IsSpanType(typeInfo.ConvertedType) || IsSpanType(typeInfo.Type))
                    return true;

                if (typeInfo.ConvertedType is IPointerTypeSymbol || typeInfo.Type is IPointerTypeSymbol)
                    return false;

                if (node is EqualsValueClauseSyntax
                    {
                        Parent: VariableDeclaratorSyntax
                        {
                            Parent: VariableDeclarationSyntax declaration
                        }
                    } &&
                    IsSpanType(semanticModel.GetTypeInfo(declaration.Type, cancellationToken).Type))
                {
                    return true;
                }

                if (node is AssignmentExpressionSyntax assignment &&
                    IsSpanType(semanticModel.GetTypeInfo(assignment.Left, cancellationToken).Type))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsSpanType(ITypeSymbol? type)
            => type is INamedTypeSymbol
            {
                Name: "Span" or "ReadOnlySpan",
                Arity: 1,
                ContainingNamespace: { } containingNamespace
            } &&
                containingNamespace.ToDisplayString() == "System";

        private static bool SymbolUseRequiresUnsafe(ExpressionSyntax expression, ISymbol symbol)
            => symbol switch
            {
                IMethodSymbol method => IsRequiresUnsafe(method),
                IPropertySymbol property => PropertyUseRequiresUnsafe(expression, property),
                IFieldSymbol field => IsRequiresUnsafe(field),
                IEventSymbol @event => IsRequiresUnsafe(@event),
                _ => false
            };

        private static bool PropertyUseRequiresUnsafe(
            ExpressionSyntax expression,
            IPropertySymbol property)
        {
            if (IsRequiresUnsafe(property))
                return true;

            bool isWrite = expression.Parent switch
            {
                AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                    !assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
                PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                    prefix.IsKind(SyntaxKind.PreDecrementExpression) => true,
                PostfixUnaryExpressionSyntax postfix when postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                    postfix.IsKind(SyntaxKind.PostDecrementExpression) => true,
                ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => true,
                _ => false
            };

            bool isRead = expression.Parent switch
            {
                AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                    !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression),
                ArgumentSyntax argument when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => false,
                _ => true
            };

            return isRead && property.GetMethod is not null && IsRequiresUnsafe(property.GetMethod) ||
                isWrite && property.SetMethod is not null && IsRequiresUnsafe(property.SetMethod);
        }

        private static ISymbol? GetReferencedSymbol(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            return symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault(IsRequiresUnsafe);
        }

        private static bool IsSymbolBearingExpression(ExpressionSyntax expression)
            => expression is IdentifierNameSyntax or GenericNameSyntax or MemberAccessExpressionSyntax or
                ElementAccessExpressionSyntax or BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or
                PostfixUnaryExpressionSyntax or CastExpressionSyntax or AssignmentExpressionSyntax;

        private static bool IsInUnsafeContext(SyntaxNode node)
        {
            foreach (SyntaxNode ancestor in node.AncestorsAndSelf())
            {
                if (ancestor is UnsafeStatementSyntax ||
                    ancestor.GetType().Name == "UnsafeExpressionSyntax")
                {
                    return true;
                }

                if (ancestor is ConstructorInitializerSyntax &&
                    ancestor.Parent is ConstructorDeclarationSyntax constructor &&
                    HasUnsafeModifier(constructor))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddLocation(
            Dictionary<TextSpan, Location> locations,
            Location location)
        {
            if (!locations.ContainsKey(location.SourceSpan))
                locations.Add(location.SourceSpan, location);
        }

        private static void AddInteropUpdates(
            SyntaxNode root,
            SemanticModel semanticModel,
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates,
            CancellationToken cancellationToken)
        {
            foreach (SyntaxNode declaration in root.DescendantNodes().Where(static node =>
                node is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or
                IndexerDeclarationSyntax or EventDeclarationSyntax))
            {
                if (!IsInteropDeclaration(declaration, semanticModel, cancellationToken))
                    continue;

                AddOrMerge(
                    updates,
                    declaration,
                    addUnsafe: !HasUnsafeModifier(declaration) && !HasSafeModifier(declaration),
                    addSafetyDocumentation: !HasSafetyDocumentation(declaration));
            }
        }

        private static bool IsInteropDeclaration(
            SyntaxNode declaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
            => GetModifiers(declaration).Any(static modifier => modifier.IsKind(SyntaxKind.ExternKeyword)) ||
                declaration is MethodDeclarationSyntax method &&
                    IsLibraryImport(method, semanticModel, cancellationToken);

        private static bool IsLibraryImport(
            MethodDeclarationSyntax method,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (semanticModel.GetDeclaredSymbol(method, cancellationToken) is IMethodSymbol methodSymbol &&
                methodSymbol.GetAttributes().Any(static attribute =>
                    attribute.AttributeClass?.ToDisplayString() == LibraryImportAttribute))
            {
                return true;
            }

            return method.AttributeLists
                .SelectMany(static list => list.Attributes)
                .Any(static attribute =>
                {
                    string name = attribute.Name.ToString();
                    return name is "LibraryImport" or "LibraryImportAttribute" ||
                        name.EndsWith(".LibraryImport", StringComparison.Ordinal) ||
                        name.EndsWith(".LibraryImportAttribute", StringComparison.Ordinal);
                });
        }

        private static void AddExplicitLayoutUpdates(
            SyntaxNode root,
            SemanticModel semanticModel,
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates,
            CancellationToken cancellationToken)
        {
            foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken)
                .Where(static diagnostic => diagnostic.Id == "CS9392"))
            {
                SyntaxNode target = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                SyntaxNode? declaration = target.AncestorsAndSelf().FirstOrDefault(static node =>
                    node is FieldDeclarationSyntax or PropertyDeclarationSyntax or EventFieldDeclarationSyntax);

                if (declaration is not null)
                    AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: true);
            }
        }

        private static void AddPartialMemberUpdates(
            SyntaxNode root,
            SemanticModel semanticModel,
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates,
            CancellationToken cancellationToken)
        {
            foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken)
                .Where(static diagnostic => diagnostic.Id == "CS0764"))
            {
                SyntaxNode target = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                SyntaxNode? declaration = target.AncestorsAndSelf().FirstOrDefault(static node =>
                    node is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or
                    IndexerDeclarationSyntax or EventDeclarationSyntax or EventFieldDeclarationSyntax);

                if (declaration is not null && !HasUnsafeModifier(declaration))
                    AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            }
        }

        private static void AddContractUpdates(
            SyntaxNode root,
            SemanticModel semanticModel,
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates,
            CancellationToken cancellationToken)
        {
            foreach (BaseMethodDeclarationSyntax declaration in root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>())
            {
                if (declaration is ConstructorDeclarationSyntax or DestructorDeclarationSyntax ||
                    HasUnsafeModifier(declaration) ||
                    semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not IMethodSymbol method ||
                    !RequiresUnsafeContract(method))
                {
                    continue;
                }

                AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            }

            foreach (PropertyDeclarationSyntax declaration in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IPropertySymbol property)
                    AddPropertyContractUpdates(declaration, property, updates);
            }

            foreach (IndexerDeclarationSyntax declaration in root.DescendantNodes().OfType<IndexerDeclarationSyntax>())
            {
                if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IPropertySymbol property)
                    AddPropertyContractUpdates(declaration, property, updates);
            }

            foreach (EventDeclarationSyntax declaration in root.DescendantNodes().OfType<EventDeclarationSyntax>())
            {
                if (!HasUnsafeModifier(declaration) &&
                    semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IEventSymbol @event &&
                    RequiresUnsafeContract(@event))
                {
                    AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
                }
            }

            foreach (EventFieldDeclarationSyntax declaration in root.DescendantNodes().OfType<EventFieldDeclarationSyntax>())
            {
                if (HasUnsafeModifier(declaration))
                    continue;

                bool requiresUnsafe = declaration.Declaration.Variables
                    .Select(variable => semanticModel.GetDeclaredSymbol(variable, cancellationToken))
                    .OfType<IEventSymbol>()
                    .Any(RequiresUnsafeContract);

                if (requiresUnsafe)
                    AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            }
        }

        private static void AddPropertyContractUpdates(
            BasePropertyDeclarationSyntax declaration,
            IPropertySymbol property,
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates)
        {
            if (HasUnsafeModifier(declaration))
                return;

            (bool getRequiresUnsafe, bool setRequiresUnsafe) = GetPropertyContractRequirements(property);
            if (!getRequiresUnsafe && !setRequiresUnsafe)
                return;

            AccessorListSyntax? accessorList = declaration.AccessorList;
            if (accessorList is null)
            {
                AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
                return;
            }

            AccessorDeclarationSyntax? getter = accessorList.Accessors.FirstOrDefault(static accessor =>
                accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            AccessorDeclarationSyntax? setter = accessorList.Accessors.FirstOrDefault(static accessor =>
                accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration));

            bool getterNeedsModifier = getRequiresUnsafe && getter is not null && !HasUnsafeModifier(getter);
            bool setterNeedsModifier = setRequiresUnsafe && setter is not null && !HasUnsafeModifier(setter);

            if (getterNeedsModifier && setterNeedsModifier &&
                !accessorList.Accessors.Any(HasUnsafeModifier))
            {
                AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
                return;
            }

            if (getterNeedsModifier)
                AddOrMerge(updates, getter!, addUnsafe: true, addSafetyDocumentation: false);
            if (setterNeedsModifier)
                AddOrMerge(updates, setter!, addUnsafe: true, addSafetyDocumentation: false);
        }

        private static (bool GetRequiresUnsafe, bool SetRequiresUnsafe) GetPropertyContractRequirements(
            IPropertySymbol property)
        {
            var contracts = new HashSet<IPropertySymbol>(SymbolEqualityComparer.Default);
            if (property.OverriddenProperty is not null)
                contracts.Add(property.OverriddenProperty);
            contracts.UnionWith(property.ExplicitInterfaceImplementations);

            if (property.ContainingType is { TypeKind: not TypeKind.Interface } containingType)
            {
                foreach (INamedTypeSymbol @interface in containingType.AllInterfaces)
                {
                    foreach (IPropertySymbol interfaceProperty in @interface.GetMembers().OfType<IPropertySymbol>())
                    {
                        if (IsPropertyImplementation(
                            containingType.FindImplementationForInterfaceMember(interfaceProperty),
                            property))
                        {
                            contracts.Add(interfaceProperty);
                        }
                    }
                }
            }

            return (
                RequiresUnsafeAccessorContract(contracts, isGetter: true) ||
                    property.GetMethod is not null && RequiresUnsafeContract(property.GetMethod),
                RequiresUnsafeAccessorContract(contracts, isGetter: false) ||
                    property.SetMethod is not null && RequiresUnsafeContract(property.SetMethod));
        }

        private static bool IsPropertyImplementation(ISymbol? implementation, IPropertySymbol property)
            => SymbolsEqual(implementation, property) ||
                implementation is IMethodSymbol { AssociatedSymbol: IPropertySymbol associatedProperty } &&
                    SymbolsEqual(associatedProperty, property);

        private static bool RequiresUnsafeAccessorContract(
            IEnumerable<IPropertySymbol> properties,
            bool isGetter)
        {
            bool hasUnsafeContract = false;
            foreach (IPropertySymbol property in properties)
            {
                IMethodSymbol? accessor = isGetter ? property.GetMethod : property.SetMethod;
                if (accessor is null)
                    continue;

                if (IsPropertyAccessorRequiresUnsafe(property, accessor, isGetter))
                {
                    hasUnsafeContract = true;
                }
                else
                {
                    return false;
                }
            }

            return hasUnsafeContract;
        }

        private static bool IsPropertyAccessorRequiresUnsafe(
            IPropertySymbol property,
            IMethodSymbol accessor,
            bool isGetter)
        {
            foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is not BasePropertyDeclarationSyntax declaration)
                    continue;

                if (HasUnsafeModifier(declaration))
                    return true;

                if (declaration.AccessorList?.Accessors.Any(candidate =>
                    (isGetter
                        ? candidate.IsKind(SyntaxKind.GetAccessorDeclaration)
                        : candidate.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                            candidate.IsKind(SyntaxKind.InitAccessorDeclaration)) &&
                    HasUnsafeModifier(candidate)) == true)
                {
                    return true;
                }
            }

            return IsRequiresUnsafe(accessor);
        }

        private static bool RequiresUnsafeContract(IMethodSymbol method)
        {
            var contracts = new HashSet<IMethodSymbol>(SymbolEqualityComparer.Default);
            if (method.OverriddenMethod is not null)
                contracts.Add(method.OverriddenMethod);
            contracts.UnionWith(method.ExplicitInterfaceImplementations);

            AddImplicitInterfaceContracts(method, contracts);
            return RequiresUnsafeContract(contracts);
        }

        private static bool RequiresUnsafeContract(IEventSymbol @event)
        {
            var contracts = new HashSet<IEventSymbol>(SymbolEqualityComparer.Default);
            if (@event.OverriddenEvent is not null)
                contracts.Add(@event.OverriddenEvent);
            contracts.UnionWith(@event.ExplicitInterfaceImplementations);

            if (@event.ContainingType is { TypeKind: not TypeKind.Interface } containingType)
            {
                foreach (INamedTypeSymbol @interface in containingType.AllInterfaces)
                {
                    foreach (IEventSymbol interfaceEvent in @interface.GetMembers().OfType<IEventSymbol>())
                    {
                        if (SymbolEqualityComparer.Default.Equals(
                            containingType.FindImplementationForInterfaceMember(interfaceEvent),
                            @event))
                        {
                            contracts.Add(interfaceEvent);
                        }
                    }
                }
            }

            return RequiresUnsafeContract(contracts);
        }

        private static void AddImplicitInterfaceContracts(
            IMethodSymbol method,
            HashSet<IMethodSymbol> contracts)
        {
            if (method.ContainingType is not { TypeKind: not TypeKind.Interface } containingType)
                return;

            foreach (INamedTypeSymbol @interface in containingType.AllInterfaces)
            {
                foreach (IMethodSymbol interfaceMethod in @interface.GetMembers().OfType<IMethodSymbol>())
                {
                    if (SymbolEqualityComparer.Default.Equals(
                        containingType.FindImplementationForInterfaceMember(interfaceMethod),
                        method))
                    {
                        contracts.Add(interfaceMethod);
                    }
                }
            }
        }

        private static bool RequiresUnsafeContract<TSymbol>(IEnumerable<TSymbol> contracts)
            where TSymbol : ISymbol
        {
            bool hasUnsafeContract = false;
            foreach (TSymbol contract in contracts)
            {
                if (IsRequiresUnsafe(contract))
                {
                    hasUnsafeContract = true;
                }
                else
                {
                    return false;
                }
            }

            return hasUnsafeContract;
        }

        private static bool SymbolsEqual(ISymbol? left, ISymbol right)
            => SymbolEqualityComparer.Default.Equals(left, right) ||
                left is not null &&
                    SymbolEqualityComparer.Default.Equals(left.OriginalDefinition, right.OriginalDefinition);

        private static bool IsRequiresUnsafe(ISymbol symbol)
        {
            if (HasAttribute(symbol, RequiresUnsafeAttribute))
            {
                return true;
            }

            if (symbol is IMethodSymbol methodSymbol && IsAssociatedAccessorUnsafe(methodSymbol))
                return true;

            if (symbol.DeclaringSyntaxReferences.Any(static reference =>
                IsUnsafeDeclaration(reference.GetSyntax())))
            {
                return true;
            }

            if (!UsesLegacyMemorySafetyRules(symbol))
                return false;

            return symbol switch
            {
                IMethodSymbol method => ContainsPointer(method.ReturnType) ||
                    method.Parameters.Any(static parameter => ContainsPointer(parameter.Type)),
                IPropertySymbol property => ContainsPointer(property.Type) ||
                    property.Parameters.Any(static parameter => ContainsPointer(parameter.Type)),
                IEventSymbol @event => ContainsPointer(@event.Type),
                IFieldSymbol field => ContainsPointer(field.Type),
                _ => false
            };
        }

        private static bool UsesLegacyMemorySafetyRules(ISymbol symbol)
            => symbol.DeclaringSyntaxReferences.IsEmpty &&
                (symbol.ContainingModule is null ||
                    !HasAttribute(symbol.ContainingModule, MemorySafetyRulesAttribute));

        private static bool HasAttribute(ISymbol symbol, string fullyQualifiedName)
            => symbol.GetAttributes().Any(attribute =>
                attribute.AttributeClass?.ToDisplayString() == fullyQualifiedName);

        private static bool IsAssociatedAccessorUnsafe(IMethodSymbol method)
        {
            if (method.AssociatedSymbol is IPropertySymbol property)
            {
                foreach (SyntaxReference reference in property.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax() is not BasePropertyDeclarationSyntax declaration)
                        continue;

                    if (HasUnsafeModifier(declaration))
                        return true;

                    SyntaxKind accessorKind = method.MethodKind == MethodKind.PropertyGet
                        ? SyntaxKind.GetAccessorDeclaration
                        : SyntaxKind.SetAccessorDeclaration;

                    if (declaration.AccessorList?.Accessors.Any(accessor =>
                        accessor.IsKind(accessorKind) && HasUnsafeModifier(accessor)) == true)
                    {
                        return true;
                    }

                    if (method.MethodKind == MethodKind.PropertySet &&
                        declaration.AccessorList?.Accessors.Any(accessor =>
                            accessor.IsKind(SyntaxKind.InitAccessorDeclaration) && HasUnsafeModifier(accessor)) == true)
                    {
                        return true;
                    }
                }
            }
            else if (method.AssociatedSymbol is IEventSymbol @event)
            {
                return @event.DeclaringSyntaxReferences.Any(static reference =>
                    IsUnsafeDeclaration(reference.GetSyntax()));
            }

            return false;
        }

        private static bool IsUnsafeDeclaration(SyntaxNode declaration)
        {
            SyntaxNode target = declaration switch
            {
                VariableDeclaratorSyntax variable when variable.Parent?.Parent is EventFieldDeclarationSyntax @event => @event,
                VariableDeclaratorSyntax variable when variable.Parent?.Parent is FieldDeclarationSyntax field => field,
                _ => declaration
            };

            if (HasUnsafeModifier(target))
                return true;

            return target is AccessorDeclarationSyntax accessor &&
                accessor.Parent?.Parent is BasePropertyDeclarationSyntax property &&
                HasUnsafeModifier(property);
        }

        private static bool IsUnsafeModifierRequiredByContract(
            SyntaxNode declaration,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            if (declaration is EventFieldDeclarationSyntax eventField)
            {
                return eventField.Declaration.Variables
                    .Select(variable => semanticModel.GetDeclaredSymbol(variable, cancellationToken))
                    .OfType<IEventSymbol>()
                    .Any(RequiresUnsafeContract);
            }

            ISymbol? symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);

            if (symbol is null)
                return false;

            if (symbol.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .Any(otherDeclaration => otherDeclaration != declaration && IsUnsafeDeclaration(otherDeclaration)))
            {
                return true;
            }

            return symbol switch
            {
                IMethodSymbol method => IsMatchingPartialDeclarationUnsafe(method) ||
                    RequiresUnsafeContract(method),
                IPropertySymbol property => GetPropertyContractRequirements(property) is (true, _) or (_, true),
                IEventSymbol @event => RequiresUnsafeContract(@event),
                _ => false
            };
        }

        private static bool IsMatchingPartialDeclarationUnsafe(IMethodSymbol method)
            => method.PartialDefinitionPart is { } definition && IsRequiresUnsafe(definition) ||
                method.PartialImplementationPart is { } implementation && IsRequiresUnsafe(implementation);

        private static void AddOrMerge(
            Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates,
            SyntaxNode declaration,
            bool addUnsafe,
            bool addSafetyDocumentation)
        {
            if (!addUnsafe && !addSafetyDocumentation)
                return;

            if (updates.TryGetValue(declaration, out var existing))
            {
                updates[declaration] = (
                    existing.AddUnsafe || addUnsafe,
                    existing.AddSafetyDocumentation || addSafetyDocumentation);
            }
            else
            {
                updates.Add(declaration, (addUnsafe, addSafetyDocumentation));
            }
        }
    }
}
#endif
