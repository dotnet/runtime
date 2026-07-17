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

namespace ILLink.RoslynAnalyzer;

internal static class UnsafeMigrationAnalysis
{
    public const string RequiresUnsafeAttribute = "System.Runtime.CompilerServices.RequiresUnsafeAttribute";

    private const string LibraryImportAttribute = "System.Runtime.InteropServices.LibraryImportAttribute";
    private const string MemorySafetyRulesAttribute = "System.Runtime.CompilerServices.MemorySafetyRulesAttribute";
    private const string SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

    private static readonly string[] s_unsafeOperationDiagnosticIds =
        ["CS9360", "CS9361", "CS9362", "CS9363"];

    public readonly record struct ModifierRemoval(
        SyntaxNode Declaration,
        SyntaxToken Modifier);

    public readonly record struct DeclarationUpdate(
        SyntaxNode Declaration,
        bool AddUnsafeModifier,
        bool AddSafetyDocumentation);

    public static ImmutableArray<ModifierRemoval> GetModifierRemovals(
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        return
        [
            .. root.DescendantNodesAndSelf()
                .Select(static declaration => new ModifierRemoval(
                    declaration,
                    GetModifiers(declaration).FirstOrDefault(
                        static modifier => modifier.IsKind(SyntaxKind.UnsafeKeyword))))
                .Where(removal => removal.Modifier.RawKind != 0 &&
                    ShouldRemoveUnsafeModifier(
                        removal.Declaration,
                        semanticModel,
                        cancellationToken))
        ];
    }

    public static ImmutableArray<DeclarationUpdate> GetDeclarationUpdates(
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates = [];

        AddInteropUpdates(root, semanticModel, updates, cancellationToken);
        AddExplicitLayoutUpdates(root, semanticModel, updates, cancellationToken);
        AddPartialMemberUpdates(root, semanticModel, updates, cancellationToken);
        AddContractUpdates(root, semanticModel, updates, cancellationToken);

        return
        [
            .. updates
                .OrderBy(static pair => pair.Key.SpanStart)
                .Select(static pair => new DeclarationUpdate(
                    pair.Key,
                    pair.Value.AddUnsafe,
                    pair.Value.AddSafetyDocumentation))
        ];
    }

    public static ImmutableArray<Location> GetUnsafeOperationLocations(
        SemanticModel semanticModel,
        bool skipLocalsInit,
        CancellationToken cancellationToken)
    {
        Dictionary<TextSpan, Location> locations = [];

        foreach (Diagnostic diagnostic in semanticModel.GetDiagnostics(cancellationToken: cancellationToken)
            .Where(diagnostic =>
                diagnostic.Location.SourceTree == semanticModel.SyntaxTree &&
                s_unsafeOperationDiagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal)))
        {
            AddLocation(locations, diagnostic.Location);
        }

        SyntaxNode root = semanticModel.SyntaxTree.GetRoot(cancellationToken);
        foreach (ExpressionSyntax expression in root.DescendantNodes().OfType<ExpressionSyntax>())
        {
            if (!IsInUnsafeContext(expression) &&
                RequiresUnsafeContext(
                    expression,
                    semanticModel,
                    skipLocalsInit,
                    cancellationToken))
            {
                AddLocation(locations, expression.GetLocation());
            }
        }

        return [.. locations.Values.OrderBy(static location => location.SourceSpan.Start)];
    }

    private static void AddLocation(
        Dictionary<TextSpan, Location> locations,
        Location location)
    {
        if (!locations.ContainsKey(location.SourceSpan))
            locations.Add(location.SourceSpan, location);
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
        => GetModifiers(declaration).Any(
            static modifier => modifier.IsKind(SyntaxKind.UnsafeKeyword));

    public static bool HasSafeModifier(SyntaxNode declaration)
        => GetModifiers(declaration).Any(static modifier => modifier.ValueText == "safe");

    private static bool ShouldRemoveUnsafeModifier(
        SyntaxNode declaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        if (declaration is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax or DestructorDeclarationSyntax)
            return true;

        if (declaration is ConstructorDeclarationSyntax
            {
                Modifiers: var modifiers
            } && modifiers.Any(static modifier => modifier.IsKind(SyntaxKind.StaticKeyword)))
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
            EventFieldDeclarationSyntax { Declaration.Variables: [var variable] } =>
                semanticModel.GetDeclaredSymbol(variable, cancellationToken),
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
                type.DescendantNodesAndSelf().Any(
                    static node => node is PointerTypeSyntax or FunctionPointerTypeSyntax))
        };
    }

    private static IEnumerable<TypeSyntax> GetSignatureTypes(SyntaxNode declaration)
        => declaration switch
        {
            MethodDeclarationSyntax method =>
                [method.ReturnType, .. GetParameterTypes(method.ParameterList)],
            ConstructorDeclarationSyntax constructor => GetParameterTypes(constructor.ParameterList),
            OperatorDeclarationSyntax @operator =>
                [@operator.ReturnType, .. GetParameterTypes(@operator.ParameterList)],
            ConversionOperatorDeclarationSyntax conversion =>
                [conversion.Type, .. GetParameterTypes(conversion.ParameterList)],
            LocalFunctionStatementSyntax localFunction =>
                [localFunction.ReturnType, .. GetParameterTypes(localFunction.ParameterList)],
            PropertyDeclarationSyntax property => [property.Type],
            IndexerDeclarationSyntax indexer =>
                [indexer.Type, .. GetParameterTypes(indexer.ParameterList)],
            EventDeclarationSyntax @event => [@event.Type],
            EventFieldDeclarationSyntax @event => [@event.Declaration.Type],
            _ => []
        };

    private static IEnumerable<TypeSyntax> GetParameterTypes(BaseParameterListSyntax parameterList)
        => parameterList.Parameters
            .Select(static parameter => parameter.Type)
            .OfType<TypeSyntax>();

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
        => expression switch
        {
            PrefixUnaryExpressionSyntax prefix
                when prefix.IsKind(SyntaxKind.PointerIndirectionExpression) => true,

            MemberAccessExpressionSyntax memberAccess
                when memberAccess.IsKind(SyntaxKind.PointerMemberAccessExpression) => true,

            ElementAccessExpressionSyntax elementAccess =>
                IsPointerElementAccess(elementAccess, semanticModel, cancellationToken) ||
                GetReferencedSymbol(elementAccess, semanticModel, cancellationToken) is { } indexer &&
                    SymbolUseRequiresUnsafe(elementAccess, indexer),

            StackAllocArrayCreationExpressionSyntax stackAlloc =>
                IsUnsafeStackAlloc(
                    stackAlloc,
                    semanticModel,
                    skipLocalsInit,
                    cancellationToken),

            InvocationExpressionSyntax invocation =>
                semanticModel.GetTypeInfo(invocation.Expression, cancellationToken).Type is IFunctionPointerTypeSymbol ||
                GetReferencedSymbol(invocation, semanticModel, cancellationToken) is { } invocationSymbol &&
                    SymbolUseRequiresUnsafe(invocation, invocationSymbol),

            ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax =>
                GetReferencedSymbol(expression, semanticModel, cancellationToken) is IMethodSymbol constructor &&
                    IsRequiresUnsafe(constructor),

            IdentifierNameSyntax
            {
                Parent: InvocationExpressionSyntax { Expression: var invoked }
            } identifier when invoked == identifier => false,

            GenericNameSyntax
            {
                Parent: InvocationExpressionSyntax { Expression: var invoked }
            } genericName when invoked == genericName => false,

            SimpleNameSyntax
            {
                Parent: MemberAccessExpressionSyntax { Name: var name }
            } simpleName when name == simpleName => false,

            MemberAccessExpressionSyntax
            {
                Parent: InvocationExpressionSyntax { Expression: var invoked }
            } memberAccess when invoked == memberAccess => false,

            _ => IsSymbolBearingExpression(expression) &&
                GetReferencedSymbol(expression, semanticModel, cancellationToken) is { } symbol &&
                SymbolUseRequiresUnsafe(expression, symbol)
        };

    private static bool IsPointerElementAccess(
        ElementAccessExpressionSyntax elementAccess,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
        => semanticModel.GetTypeInfo(elementAccess.Expression, cancellationToken).Type is IPointerTypeSymbol ||
            GetReferencedSymbol(elementAccess.Expression, semanticModel, cancellationToken) is IFieldSymbol
            {
                IsFixedSizeBuffer: true
            };

    private static bool IsUnsafeStackAlloc(
        StackAllocArrayCreationExpressionSyntax stackAlloc,
        SemanticModel semanticModel,
        bool skipLocalsInit,
        CancellationToken cancellationToken)
    {
        if (stackAlloc.Initializer is not null ||
            !IsSpanStackAlloc(stackAlloc, semanticModel, cancellationToken))
        {
            return false;
        }

        if (skipLocalsInit)
            return true;

        for (ISymbol? symbol = semanticModel.GetEnclosingSymbol(
            stackAlloc.SpanStart,
            cancellationToken);
            symbol is not null;
            symbol = symbol.ContainingSymbol)
        {
            if (HasAttribute(symbol, SkipLocalsInitAttribute))
                return true;
        }

        return semanticModel.Compilation.Assembly.Modules.Any(
            static module => HasAttribute(module, SkipLocalsInitAttribute));
    }

    private static bool IsSpanStackAlloc(
        StackAllocArrayCreationExpressionSyntax stackAlloc,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        for (SyntaxNode? node = stackAlloc;
            node is not null and not StatementSyntax;
            node = node.Parent)
        {
            TypeInfo typeInfo = node is ExpressionSyntax expression
                ? semanticModel.GetTypeInfo(expression, cancellationToken)
                : default;

            if (IsSpanType(typeInfo.ConvertedType) || IsSpanType(typeInfo.Type))
                return true;

            if (typeInfo.ConvertedType is IPointerTypeSymbol ||
                typeInfo.Type is IPointerTypeSymbol)
            {
                return false;
            }

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
        } && containingNamespace.ToDisplayString() == "System";

    private static bool SymbolUseRequiresUnsafe(
        ExpressionSyntax expression,
        ISymbol symbol)
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
            PrefixUnaryExpressionSyntax prefix
                when prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                    prefix.IsKind(SyntaxKind.PreDecrementExpression) => true,
            PostfixUnaryExpressionSyntax postfix
                when postfix.IsKind(SyntaxKind.PostIncrementExpression) ||
                    postfix.IsKind(SyntaxKind.PostDecrementExpression) => true,
            ArgumentSyntax argument
                when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => true,
            _ => false
        };

        bool isRead = expression.Parent switch
        {
            AssignmentExpressionSyntax assignment when assignment.Left == expression =>
                !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression),
            ArgumentSyntax argument
                when argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword) => false,
            _ => true
        };

        return isRead &&
                property.GetMethod is not null &&
                IsRequiresUnsafe(property.GetMethod) ||
            isWrite &&
                property.SetMethod is not null &&
                IsRequiresUnsafe(property.SetMethod);
    }

    private static ISymbol? GetReferencedSymbol(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        CancellationToken cancellationToken)
    {
        SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
        return symbolInfo.Symbol ??
            symbolInfo.CandidateSymbols.FirstOrDefault(IsRequiresUnsafe);
    }

    private static bool IsSymbolBearingExpression(ExpressionSyntax expression)
        => expression is IdentifierNameSyntax or GenericNameSyntax or
            MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or
            BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or
            PostfixUnaryExpressionSyntax or CastExpressionSyntax or
            AssignmentExpressionSyntax;

    private static bool IsInUnsafeContext(SyntaxNode node)
        => node.AncestorsAndSelf().Any(static ancestor =>
            ancestor is UnsafeStatementSyntax ||
            ancestor.GetType().Name == "UnsafeExpressionSyntax" ||
            ancestor is ConstructorInitializerSyntax
            {
                Parent: ConstructorDeclarationSyntax constructor
            } && HasUnsafeModifier(constructor));

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
        => GetModifiers(declaration).Any(
            static modifier => modifier.IsKind(SyntaxKind.ExternKeyword)) ||
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
            SyntaxNode target = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
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
            SyntaxNode target = root.FindNode(
                diagnostic.Location.SourceSpan,
                getInnermostNodeForTie: true);
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
        foreach (BaseMethodDeclarationSyntax declaration in root.DescendantNodes()
            .OfType<BaseMethodDeclarationSyntax>())
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

        foreach (PropertyDeclarationSyntax declaration in root.DescendantNodes()
            .OfType<PropertyDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IPropertySymbol property)
                AddPropertyContractUpdates(declaration, property, updates);
        }

        foreach (IndexerDeclarationSyntax declaration in root.DescendantNodes()
            .OfType<IndexerDeclarationSyntax>())
        {
            if (semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IPropertySymbol property)
                AddPropertyContractUpdates(declaration, property, updates);
        }

        foreach (EventDeclarationSyntax declaration in root.DescendantNodes()
            .OfType<EventDeclarationSyntax>())
        {
            if (!HasUnsafeModifier(declaration) &&
                semanticModel.GetDeclaredSymbol(declaration, cancellationToken) is IEventSymbol @event &&
                RequiresUnsafeContract(@event))
            {
                AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            }
        }

        foreach (EventFieldDeclarationSyntax declaration in root.DescendantNodes()
            .OfType<EventFieldDeclarationSyntax>())
        {
            if (HasUnsafeModifier(declaration))
                continue;

            IEventSymbol[] events =
            [
                .. declaration.Declaration.Variables
                    .Select(variable => semanticModel.GetDeclaredSymbol(variable, cancellationToken))
                    .OfType<IEventSymbol>()
            ];

            if (events.Length == declaration.Declaration.Variables.Count &&
                events is [_, ..] &&
                events.All(RequiresUnsafeContract))
            {
                AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            }
        }
    }

    private static void AddPropertyContractUpdates(
        BasePropertyDeclarationSyntax declaration,
        IPropertySymbol property,
        Dictionary<SyntaxNode, (bool AddUnsafe, bool AddSafetyDocumentation)> updates)
    {
        if (HasUnsafeModifier(declaration))
            return;

        (bool getRequiresUnsafe, bool setRequiresUnsafe) =
            GetPropertyContractRequirements(property);
        if (!getRequiresUnsafe && !setRequiresUnsafe)
            return;

        if (declaration.AccessorList is not { } accessorList)
        {
            AddOrMerge(updates, declaration, addUnsafe: true, addSafetyDocumentation: false);
            return;
        }

        AccessorDeclarationSyntax? getter = accessorList.Accessors.FirstOrDefault(
            static accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
        AccessorDeclarationSyntax? setter = accessorList.Accessors.FirstOrDefault(
            static accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                accessor.IsKind(SyntaxKind.InitAccessorDeclaration));

        bool getterNeedsModifier =
            getRequiresUnsafe && getter is not null && !HasUnsafeModifier(getter);
        bool setterNeedsModifier =
            setRequiresUnsafe && setter is not null && !HasUnsafeModifier(setter);

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

    private static (bool GetRequiresUnsafe, bool SetRequiresUnsafe)
        GetPropertyContractRequirements(IPropertySymbol property)
    {
        HashSet<IPropertySymbol> contracts = new(SymbolEqualityComparer.Default);
        if (property.OverriddenProperty is not null)
            contracts.Add(property.OverriddenProperty);

        contracts.UnionWith(property.ExplicitInterfaceImplementations);

        if (property.ContainingType is { TypeKind: not TypeKind.Interface } containingType)
        {
            foreach (IPropertySymbol interfaceProperty in containingType.AllInterfaces
                .SelectMany(static @interface => @interface.GetMembers().OfType<IPropertySymbol>()))
            {
                if (IsPropertyImplementation(
                    containingType.FindImplementationForInterfaceMember(interfaceProperty),
                    property))
                {
                    contracts.Add(interfaceProperty);
                }
            }
        }

        return (
            RequiresUnsafeAccessorContract(contracts, isGetter: true) ||
                property.GetMethod is not null && RequiresUnsafeContract(property.GetMethod),
            RequiresUnsafeAccessorContract(contracts, isGetter: false) ||
                property.SetMethod is not null && RequiresUnsafeContract(property.SetMethod));
    }

    private static bool IsPropertyImplementation(
        ISymbol? implementation,
        IPropertySymbol property)
        => SymbolsEqual(implementation, property) ||
            implementation is IMethodSymbol
            {
                AssociatedSymbol: IPropertySymbol associatedProperty
            } && SymbolsEqual(associatedProperty, property);

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

            if (!IsPropertyAccessorRequiresUnsafe(property, accessor, isGetter))
                return false;

            hasUnsafeContract = true;
        }

        return hasUnsafeContract;
    }

    private static bool IsPropertyAccessorRequiresUnsafe(
        IPropertySymbol property,
        IMethodSymbol accessor,
        bool isGetter)
    {
        foreach (BasePropertyDeclarationSyntax declaration in property.DeclaringSyntaxReferences
            .Select(static reference => reference.GetSyntax())
            .OfType<BasePropertyDeclarationSyntax>())
        {
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
        HashSet<IMethodSymbol> contracts = new(SymbolEqualityComparer.Default);
        if (method.OverriddenMethod is not null)
            contracts.Add(method.OverriddenMethod);

        contracts.UnionWith(method.ExplicitInterfaceImplementations);
        AddImplicitInterfaceContracts(method, contracts);
        return AllContractsRequireUnsafe(contracts);
    }

    private static bool RequiresUnsafeContract(IEventSymbol @event)
    {
        HashSet<IEventSymbol> contracts = new(SymbolEqualityComparer.Default);
        if (@event.OverriddenEvent is not null)
            contracts.Add(@event.OverriddenEvent);

        contracts.UnionWith(@event.ExplicitInterfaceImplementations);

        if (@event.ContainingType is { TypeKind: not TypeKind.Interface } containingType)
        {
            foreach (IEventSymbol interfaceEvent in containingType.AllInterfaces
                .SelectMany(static @interface => @interface.GetMembers().OfType<IEventSymbol>()))
            {
                if (SymbolEqualityComparer.Default.Equals(
                    containingType.FindImplementationForInterfaceMember(interfaceEvent),
                    @event))
                {
                    contracts.Add(interfaceEvent);
                }
            }
        }

        return AllContractsRequireUnsafe(contracts);
    }

    private static void AddImplicitInterfaceContracts(
        IMethodSymbol method,
        HashSet<IMethodSymbol> contracts)
    {
        if (method.ContainingType is not { TypeKind: not TypeKind.Interface } containingType)
            return;

        foreach (IMethodSymbol interfaceMethod in containingType.AllInterfaces
            .SelectMany(static @interface => @interface.GetMembers().OfType<IMethodSymbol>()))
        {
            if (SymbolEqualityComparer.Default.Equals(
                containingType.FindImplementationForInterfaceMember(interfaceMethod),
                method))
            {
                contracts.Add(interfaceMethod);
            }
        }
    }

    private static bool AllContractsRequireUnsafe<TSymbol>(
        IEnumerable<TSymbol> contracts)
        where TSymbol : ISymbol
    {
        TSymbol[] contractArray = [.. contracts];
        return contractArray is [_, ..] &&
            contractArray.All(static contract => IsRequiresUnsafe(contract));
    }

    private static bool SymbolsEqual(ISymbol? left, ISymbol right)
        => SymbolEqualityComparer.Default.Equals(left, right) ||
            left is not null &&
                SymbolEqualityComparer.Default.Equals(
                    left.OriginalDefinition,
                    right.OriginalDefinition);

    private static bool IsRequiresUnsafe(ISymbol symbol)
    {
        if (HasAttribute(symbol, RequiresUnsafeAttribute))
            return true;

        if (symbol is IMethodSymbol associatedMethod &&
            IsAssociatedAccessorUnsafe(associatedMethod))
            return true;

        if (symbol.DeclaringSyntaxReferences.Any(
            static reference => IsUnsafeDeclaration(reference.GetSyntax())))
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
            foreach (BasePropertyDeclarationSyntax declaration in property.DeclaringSyntaxReferences
                .Select(static reference => reference.GetSyntax())
                .OfType<BasePropertyDeclarationSyntax>())
            {
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
                        accessor.IsKind(SyntaxKind.InitAccessorDeclaration) &&
                        HasUnsafeModifier(accessor)) == true)
                {
                    return true;
                }
            }
        }
        else if (method.AssociatedSymbol is IEventSymbol @event)
        {
            return @event.DeclaringSyntaxReferences.Any(
                static reference => IsUnsafeDeclaration(reference.GetSyntax()));
        }

        return false;
    }

    private static bool IsUnsafeDeclaration(SyntaxNode declaration)
    {
        SyntaxNode target = declaration switch
        {
            VariableDeclaratorSyntax
            {
                Parent.Parent: EventFieldDeclarationSyntax @event
            } => @event,
            VariableDeclaratorSyntax
            {
                Parent.Parent: FieldDeclarationSyntax field
            } => field,
            _ => declaration
        };

        return HasUnsafeModifier(target) ||
            target is AccessorDeclarationSyntax
            {
                Parent.Parent: BasePropertyDeclarationSyntax property
            } && HasUnsafeModifier(property);
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
            .Any(otherDeclaration =>
                otherDeclaration != declaration &&
                IsUnsafeDeclaration(otherDeclaration)))
        {
            return true;
        }

        return symbol switch
        {
            IMethodSymbol method => IsMatchingPartialDeclarationUnsafe(method) ||
                RequiresUnsafeContract(method),
            IPropertySymbol property =>
                GetPropertyContractRequirements(property) is (true, _) or (_, true),
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
#endif
