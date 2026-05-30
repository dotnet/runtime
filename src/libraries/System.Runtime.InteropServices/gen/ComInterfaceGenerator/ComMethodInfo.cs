// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Represents a method that has been determined to be a COM interface method. Only contains info immediately available from an IMethodSymbol and the user-declared member syntax (a <see cref="MethodDeclarationSyntax"/> for ordinary methods, or a <see cref="PropertyDeclarationSyntax"/> for property accessors).
    /// </summary>
    internal sealed record ComMethodInfo
    {
        public MemberDeclarationSyntax? Syntax { get; init; }
        public string MethodName { get; init; }
        public SequenceEqualImmutableArray<AttributeInfo> Attributes { get; init; }
        /// <summary>
        /// Attributes associated with this method that come from a related symbol rather than the method
        /// itself (for example, attributes declared on the source <see cref="IPropertySymbol"/> when this
        /// method is a property accessor).
        /// </summary>
        public SequenceEqualImmutableArray<AttributeInfo> AssociatedAttributes { get; init; }
        public bool IsUserDefinedShadowingMethod { get; init; }

        private ComMethodInfo(
            MemberDeclarationSyntax? syntax,
            string methodName,
            SequenceEqualImmutableArray<AttributeInfo> attributes,
            bool isUserDefinedShadowingMethod,
            SequenceEqualImmutableArray<AttributeInfo> associatedAttributes = default)
        {
            Syntax = syntax;
            MethodName = methodName;
            Attributes = attributes;
            IsUserDefinedShadowingMethod = isUserDefinedShadowingMethod;
            AssociatedAttributes = associatedAttributes.Array.IsDefault
                ? ImmutableArray<AttributeInfo>.Empty.ToSequenceEqual()
                : associatedAttributes;
        }

        /// <summary>
        /// Returns a list of tuples of ComMethodInfo, IMethodSymbol, and Diagnostic. If ComMethodInfo is null, Diagnostic will not be null, and vice versa.
        /// </summary>
        public static SequenceEqualImmutableArray<DiagnosticOr<(ComMethodInfo ComMethod, IMethodSymbol Symbol)>> GetMethodsFromInterface((ComInterfaceInfo ifaceContext, INamedTypeSymbol ifaceSymbol) data, CancellationToken ct)
        {
            var methods = ImmutableArray.CreateBuilder<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>();
            foreach (var member in data.ifaceSymbol.GetMembers())
            {
                if (member.IsStatic)
                {
                    continue;
                }

                switch (member)
                {
                    case IPropertySymbol property:
                        AddPropertyAccessorInfos(methods, data.ifaceContext, data.ifaceSymbol, property, ct);
                        break;
                    case { Kind: SymbolKind.Event }:
                        methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(member.CreateDiagnosticInfo(GeneratorDiagnostics.InstanceEventDeclaredInInterface, member.Name, data.ifaceSymbol.ToDisplayString())));
                        break;
                    case IMethodSymbol { MethodKind: MethodKind.Ordinary } method:
                        methods.Add(CalculateMethodInfo(data.ifaceContext, method, ct));
                        break;
                }
            }
            return methods.ToImmutable().ToSequenceEqual();
        }

        private static DiagnosticInfo? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax comMethodDeclaringSyntax, IMethodSymbol method)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (comMethodDeclaringSyntax.TypeParameterList is not null
                || comMethodDeclaringSyntax.Body is not null
                || comMethodDeclaringSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, comMethodDeclaringSyntax.Identifier.GetLocation(), method.Name);
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                return DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, comMethodDeclaringSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
            }

            return null;
        }

        private static DiagnosticOr<(ComMethodInfo, IMethodSymbol)> CalculateMethodInfo(ComInterfaceInfo ifaceContext, IMethodSymbol method, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Assert(method is { IsStatic: false, MethodKind: MethodKind.Ordinary });

            // For externally-defined contexts, we only need minimal information about the method
            // to ensure that we have the right offsets for inheriting vtable types.
            // Skip all validation as that will be done when that type is compiled.
            if (ifaceContext.IsExternallyDefined)
            {
                return DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((
                    new ComMethodInfo(null, method.Name, CreateAttributeInfoArray(method.GetAttributes()), false),
                    method));
            }

            // We only support methods that are defined in the same partial interface definition as the
            // [GeneratedComInterface] attribute.
            // This restriction not only makes finding the syntax for a given method cheaper,
            // but it also enables us to ensure that we can determine vtable method order easily.
            Location interfaceLocation = ifaceContext.Declaration.GetLocation();
            Location? methodLocationInAttributedInterfaceDeclaration = null;
            foreach (var methodLocation in method.Locations)
            {
                if (methodLocation.SourceTree == interfaceLocation.SourceTree
                    && interfaceLocation.SourceSpan.Contains(methodLocation.SourceSpan))
                {
                    methodLocationInAttributedInterfaceDeclaration = methodLocation;
                    break;
                }
            }

            if (methodLocationInAttributedInterfaceDeclaration is null)
            {
                return DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(DiagnosticInfo.Create(GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface, method.Locations.FirstOrDefault(), method.ToDisplayString()));
            }

            // Find the matching declaration syntax
            MethodDeclarationSyntax? comMethodDeclaringSyntax = null;
            foreach (var declaringSyntaxReference in method.DeclaringSyntaxReferences)
            {
                var declaringSyntax = declaringSyntaxReference.GetSyntax(ct);
                if (declaringSyntax.GetLocation().SourceSpan.Contains(methodLocationInAttributedInterfaceDeclaration.SourceSpan))
                {
                    comMethodDeclaringSyntax = (MethodDeclarationSyntax)declaringSyntax;
                    break;
                }
            }
            if (comMethodDeclaringSyntax is null)
            {
                return DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(DiagnosticInfo.Create(GeneratorDiagnostics.CannotAnalyzeMethodPattern, method.Locations.FirstOrDefault(), method.ToDisplayString()));
            }

            var diag = GetDiagnosticIfInvalidMethodForGeneration(comMethodDeclaringSyntax, method);
            if (diag is not null)
            {
                return DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(diag);
            }

            var attributeInfos = CreateAttributeInfoArray(method.GetAttributes());

            bool shadowsBaseMethod = comMethodDeclaringSyntax.Modifiers.Any(SyntaxKind.NewKeyword);
            var comMethodInfo = new ComMethodInfo(comMethodDeclaringSyntax, method.Name, attributeInfos, shadowsBaseMethod);
            return DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((comMethodInfo, method));
        }

        /// <summary>
        /// Adds one <see cref="ComMethodInfo"/> per accessor (get first, then set) for a property declared on a
        /// <c>[GeneratedComInterface]</c>-attributed interface, or a single diagnostic if the property's
        /// declaration shape is not supported by source-generated COM.
        /// </summary>
        /// <remarks>
        /// Phase 1 only accepts bare auto-property accessors: <c>T Name { get; set; }</c>, <c>{ get; }</c>, or
        /// <c>{ set; }</c>, optionally with accessor-level accessibility modifiers (e.g. <c>private set</c>).
        /// All other shapes (indexers, expression-bodied properties, accessor bodies, auto-property initializers,
        /// any modifier on the property declaration itself, and <c>init</c> accessors) currently produce the
        /// <see cref="GeneratorDiagnostics.InstancePropertyDeclaredInInterface"/> diagnostic. Each of these is
        /// tracked as a follow-up.
        /// </remarks>
        private static void AddPropertyAccessorInfos(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            ComInterfaceInfo ifaceContext,
            INamedTypeSymbol ifaceSymbol,
            IPropertySymbol property,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // For externally-defined contexts, mirror the ordinary-method fast path: emit ComMethodInfos with no
            // declaring syntax and rely on the source-side compilation to validate the property shape.
            if (ifaceContext.IsExternallyDefined)
            {
                AddExternallyDefinedAccessor(methods, property.GetMethod);
                AddExternallyDefinedAccessor(methods, property.SetMethod);
                return;
            }

            Location interfaceLocation = ifaceContext.Declaration.GetLocation();
            Location? propertyLocationInAttributedInterfaceDeclaration = null;
            foreach (var propertyLocation in property.Locations)
            {
                if (propertyLocation.SourceTree == interfaceLocation.SourceTree
                    && interfaceLocation.SourceSpan.Contains(propertyLocation.SourceSpan))
                {
                    propertyLocationInAttributedInterfaceDeclaration = propertyLocation;
                    break;
                }
            }

            if (propertyLocationInAttributedInterfaceDeclaration is null)
            {
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(
                    DiagnosticInfo.Create(GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface, property.Locations.FirstOrDefault(), property.ToDisplayString())));
                return;
            }

            PropertyDeclarationSyntax? propertyDeclaringSyntax = null;
            foreach (var declaringSyntaxReference in property.DeclaringSyntaxReferences)
            {
                if (declaringSyntaxReference.GetSyntax(ct) is PropertyDeclarationSyntax candidate
                    && candidate.GetLocation().SourceSpan.Contains(propertyLocationInAttributedInterfaceDeclaration.SourceSpan))
                {
                    propertyDeclaringSyntax = candidate;
                    break;
                }
            }
            if (propertyDeclaringSyntax is null)
            {
                // Either no PropertyDeclarationSyntax was found (e.g. indexer with IndexerDeclarationSyntax)
                // or the syntax tree doesn't cover the located position. Emit the indexer-specific diagnostic
                // when we can confirm it's an indexer; otherwise fall back to the generic unsupported-property
                // signal so the user gets a clear "this isn't supported" message.
                DiagnosticInfo fallback = property.IsIndexer
                    ? property.CreateDiagnosticInfo(GeneratorDiagnostics.IndexerNotSupportedOnGeneratedComInterface, ifaceSymbol.ToDisplayString())
                    : property.CreateDiagnosticInfo(GeneratorDiagnostics.InstancePropertyDeclaredInInterface, property.Name, ifaceSymbol.ToDisplayString());
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(fallback));
                return;
            }

            DiagnosticInfo? shapeDiagnostic = GetDiagnosticIfUnsupportedPropertyShape(propertyDeclaringSyntax, property, ifaceSymbol);
            if (shapeDiagnostic is not null)
            {
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(shapeDiagnostic));
                return;
            }

            bool shadowsBaseProperty = propertyDeclaringSyntax.Modifiers.Any(SyntaxKind.NewKeyword);

            // Emit one ComMethodInfo per accessor, in vtable slot order (get first, then set), matching the
            // CCW vtable layout produced by the built-in CLR for a [ComVisible] interface.
            AddPropertyAccessor(methods, propertyDeclaringSyntax, property.GetMethod, shadowsBaseProperty);
            AddPropertyAccessor(methods, propertyDeclaringSyntax, property.SetMethod, shadowsBaseProperty);
        }

        private static void AddExternallyDefinedAccessor(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            IMethodSymbol? accessor)
        {
            if (accessor is null)
            {
                return;
            }

            methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((
                new ComMethodInfo(
                    null,
                    accessor.Name,
                    CreateAttributeInfoArray(accessor.GetAttributes()),
                    isUserDefinedShadowingMethod: false,
                    GetAssociatedAttributesForPropertyAccessor(accessor)),
                accessor)));
        }

        private static void AddPropertyAccessor(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            PropertyDeclarationSyntax propertyDeclaringSyntax,
            IMethodSymbol? accessor,
            bool isUserDefinedShadowingMethod)
        {
            if (accessor is null)
            {
                return;
            }

            methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((
                new ComMethodInfo(
                    propertyDeclaringSyntax,
                    accessor.Name,
                    CreateAttributeInfoArray(accessor.GetAttributes()),
                    isUserDefinedShadowingMethod,
                    GetAssociatedAttributesForPropertyAccessor(accessor)),
                accessor)));
        }

        private static SequenceEqualImmutableArray<AttributeInfo> GetAssociatedAttributesForPropertyAccessor(IMethodSymbol accessor)
        {
            if (accessor.AssociatedSymbol is not IPropertySymbol property)
            {
                return ImmutableArray<AttributeInfo>.Empty.ToSequenceEqual();
            }

            return CreateAttributeInfoArray(property.GetAttributes());
        }

        private static SequenceEqualImmutableArray<AttributeInfo> CreateAttributeInfoArray(ImmutableArray<AttributeData> attributes)
        {
            if (attributes.IsDefaultOrEmpty)
            {
                return ImmutableArray<AttributeInfo>.Empty.ToSequenceEqual();
            }

            var builder = ImmutableArray.CreateBuilder<AttributeInfo>(attributes.Length);
            foreach (var attr in attributes)
            {
                builder.Add(AttributeInfo.From(attr));
            }
            return builder.MoveToImmutable().ToSequenceEqual();
        }

        private static DiagnosticInfo? GetDiagnosticIfUnsupportedPropertyShape(PropertyDeclarationSyntax propertyDeclaringSyntax, IPropertySymbol property, INamedTypeSymbol ifaceSymbol)
        {
            foreach (var modifier in propertyDeclaringSyntax.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.PrivateKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.UnsafeKeyword:
                    case SyntaxKind.NewKeyword:
                        continue;
                    case SyntaxKind.ExternKeyword:
                    case SyntaxKind.RequiredKeyword:
                        return property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InvalidPropertyDeclarationOnGeneratedComInterface,
                            property.Name, ifaceSymbol.ToDisplayString(), modifier.ValueText);
                    default:
                        return property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InstancePropertyDeclaredInInterface,
                            property.Name, ifaceSymbol.ToDisplayString());
                }
            }

            if (propertyDeclaringSyntax.ExpressionBody is not null
                || propertyDeclaringSyntax.Initializer is not null)
            {
                return property.CreateDiagnosticInfo(
                    GeneratorDiagnostics.InstancePropertyDeclaredInInterface,
                    property.Name, ifaceSymbol.ToDisplayString());
            }

            if (propertyDeclaringSyntax.AccessorList is { } accessorList)
            {
                foreach (var accessor in accessorList.Accessors)
                {
                    if (accessor.Keyword.IsKind(SyntaxKind.InitKeyword))
                    {
                        return property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InvalidPropertyDeclarationOnGeneratedComInterface,
                            property.Name, ifaceSymbol.ToDisplayString(), "init");
                    }

                    if (accessor.Body is not null || accessor.ExpressionBody is not null)
                    {
                        return property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InstancePropertyDeclaredInInterface,
                            property.Name, ifaceSymbol.ToDisplayString());
                    }
                }
            }

            return null;
        }
    }
}
