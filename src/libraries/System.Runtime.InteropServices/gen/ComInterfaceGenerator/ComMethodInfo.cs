// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Represents a method that has been determined to be a COM interface method. Only contains info immediately available from an IMethodSymbol and the user-declared member syntax (a <see cref="MethodDeclarationSyntax"/> for ordinary methods, a <see cref="PropertyDeclarationSyntax"/> for property accessors, or an <see cref="IndexerDeclarationSyntax"/> for indexer accessors).
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

        /// <summary>
        /// Disambiguator for externally-defined accessors/methods where <see cref="Syntax"/> is
        /// <see langword="null"/> and the IL name alone (for example, <c>get_Item</c>) cannot
        /// distinguish overloads coming from a cross-assembly base interface. Set to a stable
        /// fingerprint of the underlying method's parameter types at construction; always
        /// <see cref="string.Empty"/> for user-declared records, whose <see cref="Syntax"/>
        /// field already provides per-declaration uniqueness.
        /// </summary>
        public string ExternalSignatureKey { get; init; } = string.Empty;

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
                        AddMethodInfo(methods, data.ifaceContext, data.ifaceSymbol, method, ct);
                        break;
                }
            }
            return methods.ToImmutable().ToSequenceEqual();
        }

        /// <summary>
        /// Outcome of analyzing the user-declared shape of a method or property on a
        /// <c>[GeneratedComInterface]</c>-attributed interface.
        /// </summary>
        private enum MemberShapeOutcome
        {
            /// <summary>The member is abstract and should be emitted as a COM ABI vtable slot.</summary>
            ComAbi,
            /// <summary>
            /// The member has a default implementation (body) and must NOT be assigned a vtable slot.
            /// The user-supplied body runs purely on the managed side; the member is invisible to the
            /// COM marshalling pipeline.
            /// </summary>
            DefaultImplementation,
            /// <summary>The member is malformed; emit the accompanying diagnostic and skip emission.</summary>
            Error,
        }

        private static MemberShapeOutcome AnalyzeMethodShape(
            MethodDeclarationSyntax comMethodDeclaringSyntax,
            IMethodSymbol method,
            out DiagnosticInfo? diagnostic)
        {
            diagnostic = null;

            // A method with any body (block or expression) is treated as a default implementation
            // (DIM) and is intentionally NOT assigned a vtable slot. Generic and sealed DIMs are
            // accepted as well because the C# language already enforces the valid combinations.
            if (comMethodDeclaringSyntax.Body is not null || comMethodDeclaringSyntax.ExpressionBody is not null)
            {
                return MemberShapeOutcome.DefaultImplementation;
            }

            // For non-DIM methods (the COM ABI path), generic methods and sealed methods are not supported.
            if (comMethodDeclaringSyntax.TypeParameterList is not null
                || comMethodDeclaringSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
            {
                diagnostic = DiagnosticInfo.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, comMethodDeclaringSyntax.Identifier.GetLocation(), method.Name);
                return MemberShapeOutcome.Error;
            }

            // Verify the method does not have a ref return
            if (method.ReturnsByRef || method.ReturnsByRefReadonly)
            {
                diagnostic = DiagnosticInfo.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, comMethodDeclaringSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
                return MemberShapeOutcome.Error;
            }

            return MemberShapeOutcome.ComAbi;
        }

        private static void AddMethodInfo(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            ComInterfaceInfo ifaceContext,
            INamedTypeSymbol ifaceSymbol,
            IMethodSymbol method,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Assert(method is { IsStatic: false, MethodKind: MethodKind.Ordinary });

            // For externally-defined contexts, we only need minimal information about the method
            // to ensure that we have the right offsets for inheriting vtable types. Default-implemented
            // members in another assembly do not occupy vtable slots; rely on IMethodSymbol.IsAbstract
            // (which is false for DIMs in metadata) to distinguish.
            if (ifaceContext.IsExternallyDefined)
            {
                if (!method.IsAbstract)
                {
                    return;
                }

                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((
                    new ComMethodInfo(null, method.Name, CreateAttributeInfoArray(method.GetAttributes()), false)
                    {
                        ExternalSignatureKey = BuildExternalSignatureKey(method),
                    },
                    method)));
                return;
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
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(DiagnosticInfo.Create(GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface, method.Locations.FirstOrDefault(), method.ToDisplayString())));
                return;
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
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(DiagnosticInfo.Create(GeneratorDiagnostics.CannotAnalyzeMethodPattern, method.Locations.FirstOrDefault(), method.ToDisplayString())));
                return;
            }

            switch (AnalyzeMethodShape(comMethodDeclaringSyntax, method, out var diag))
            {
                case MemberShapeOutcome.Error:
                    methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(diag!));
                    return;
                case MemberShapeOutcome.DefaultImplementation:
                    EmitMarshalAttributeWarningsForMethod(methods, method, method.Name, ifaceSymbol);
                    return;
            }

            var attributeInfos = CreateAttributeInfoArray(method.GetAttributes());

            bool shadowsBaseMethod = comMethodDeclaringSyntax.Modifiers.Any(SyntaxKind.NewKeyword);
            var comMethodInfo = new ComMethodInfo(comMethodDeclaringSyntax, method.Name, attributeInfos, shadowsBaseMethod);
            methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((comMethodInfo, method)));
        }

        /// <summary>
        /// Adds one <see cref="ComMethodInfo"/> per accessor (get first, then set) for a property declared on a
        /// <c>[GeneratedComInterface]</c>-attributed interface, or a single diagnostic if the property's
        /// declaration shape is not supported by source-generated COM.
        /// </summary>
        /// <remarks>
        /// <para>
        /// An abstract property (no accessor bodies) is mapped to one or two consecutive vtable slots
        /// (getter first, then setter) using the same rules built-in COM uses for <c>[ComVisible(true)]</c>
        /// interfaces.
        /// </para>
        /// <para>
        /// A property whose accessors all carry bodies is treated as a default implementation (DIM)
        /// and is NOT assigned a vtable slot — the user-supplied body runs purely on the managed side
        /// and is invisible to the COM marshalling pipeline. Mixing the two shapes within a single
        /// property is reported as <see cref="GeneratorDiagnostics.PropertyAccessorsMustBeAllOrNothing"/>.
        /// </para>
        /// </remarks>
        private static void AddPropertyAccessorInfos(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            ComInterfaceInfo ifaceContext,
            INamedTypeSymbol ifaceSymbol,
            IPropertySymbol property,
            CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            // For externally-defined contexts, mirror the ordinary-method fast path: emit ComMethodInfos
            // only for abstract accessors (handled inside AddExternallyDefinedAccessor).
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

            BasePropertyDeclarationSyntax? propertyDeclaringSyntax = null;
            foreach (var declaringSyntaxReference in property.DeclaringSyntaxReferences)
            {
                if (declaringSyntaxReference.GetSyntax(ct) is BasePropertyDeclarationSyntax candidate
                    && candidate.GetLocation().SourceSpan.Contains(propertyLocationInAttributedInterfaceDeclaration.SourceSpan))
                {
                    propertyDeclaringSyntax = candidate;
                    break;
                }
            }
            if (propertyDeclaringSyntax is null)
            {
                // The syntax tree doesn't cover the located position. This is unexpected for any
                // BasePropertyDeclarationSyntax shape (property or indexer); report the same
                // analysis-failure diagnostic the ordinary-method path uses for the parallel case.
                methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(
                    DiagnosticInfo.Create(GeneratorDiagnostics.CannotAnalyzeMethodPattern, property.Locations.FirstOrDefault(), property.ToDisplayString())));
                return;
            }

            switch (AnalyzePropertyShape(propertyDeclaringSyntax, property, ifaceSymbol, out var shapeDiagnostic))
            {
                case MemberShapeOutcome.Error:
                    methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(shapeDiagnostic!));
                    return;
                case MemberShapeOutcome.DefaultImplementation:
                    EmitMarshalAttributeWarningsForProperty(methods, property, ifaceSymbol);
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
            // Default-implemented accessors in another assembly do not occupy vtable slots, so we
            // must not count them when computing inheritance offsets. Rely on IMethodSymbol.IsAbstract
            // (which is false for DIMs) to distinguish — matching AddMethodInfo's
            // externally-defined fast path for ordinary methods.
            if (accessor is null || !accessor.IsAbstract)
            {
                return;
            }

            methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From((
                new ComMethodInfo(
                    null,
                    accessor.Name,
                    CreateAttributeInfoArray(accessor.GetAttributes()),
                    isUserDefinedShadowingMethod: false,
                    GetAssociatedAttributesForPropertyAccessor(accessor))
                {
                    ExternalSignatureKey = BuildExternalSignatureKey(accessor),
                },
                accessor)));
        }

        private static void AddPropertyAccessor(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            BasePropertyDeclarationSyntax propertyDeclaringSyntax,
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

        /// <summary>
        /// Builds a stable, cache-friendly parameter-types fingerprint used as a tie-breaker in
        /// <see cref="ComMethodInfo.ExternalSignatureKey"/> for externally-defined accessors and
        /// methods whose <see cref="Syntax"/> field is <see langword="null"/>. Returns
        /// <see cref="string.Empty"/> for nullary signatures so the field stays normalized.
        /// </summary>
        private static string BuildExternalSignatureKey(IMethodSymbol method)
        {
            ImmutableArray<IParameterSymbol> parameters = method.Parameters;
            if (parameters.IsDefaultOrEmpty)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append('|');
                }
                builder.Append(parameters[i].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
            return builder.ToString();
        }

        private static MemberShapeOutcome AnalyzePropertyShape(
            BasePropertyDeclarationSyntax propertyDeclaringSyntax,
            IPropertySymbol property,
            INamedTypeSymbol ifaceSymbol,
            out DiagnosticInfo? diagnostic)
        {
            diagnostic = null;

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
                    default:
                        // Any other modifier (e.g. `static`, `partial`, `abstract`, `virtual`, …)
                        // is rejected by the same unsupported-modifier diagnostic with the offending
                        // keyword text. C# already disallows most of these on interface instance
                        // properties, but reporting through our own diagnostic gives a clearer signal.
                        diagnostic = property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InvalidPropertyDeclarationOnGeneratedComInterface,
                            property.Name, ifaceSymbol.ToDisplayString(), modifier.ValueText);
                        return MemberShapeOutcome.Error;
                }
            }

            // Auto-property initializers are not legal on interface instance properties at the C#
            // level (CS8053), so we never reach AnalyzePropertyShape with one. Indexers cannot carry
            // initializers at all.
            Debug.Assert(propertyDeclaringSyntax is not PropertyDeclarationSyntax { Initializer: not null },
                "Interface instance properties cannot have auto-property initializers (CS8053).");

            // Disallow `init` accessors before deciding DIM-vs-ABI: an `init` accessor is conceptually a
            // setter, but `init`-vs-`set` has no meaningful representation in a COM vtable, so it isn't
            // supported regardless of whether the accessor has a body.
            if (propertyDeclaringSyntax.AccessorList is { } accessorList)
            {
                foreach (var accessor in accessorList.Accessors)
                {
                    if (accessor.Keyword.IsKind(SyntaxKind.InitKeyword))
                    {
                        diagnostic = property.CreateDiagnosticInfo(
                            GeneratorDiagnostics.InvalidPropertyDeclarationOnGeneratedComInterface,
                            property.Name, ifaceSymbol.ToDisplayString(), "init");
                        return MemberShapeOutcome.Error;
                    }
                }
            }

            // An expression-bodied property (`T Foo => …;`) is a single-getter default implementation
            // and is treated as a DIM. Indexers can also be expression-bodied (`T this[int i] => …;`)
            // with the same semantics. ExpressionBody lives on the concrete subclasses, not on
            // BasePropertyDeclarationSyntax, so we pattern-match each shape.
            ArrowExpressionClauseSyntax? expressionBody = propertyDeclaringSyntax switch
            {
                PropertyDeclarationSyntax propertyDecl => propertyDecl.ExpressionBody,
                IndexerDeclarationSyntax indexerDecl => indexerDecl.ExpressionBody,
                _ => null,
            };
            if (expressionBody is not null)
            {
                return MemberShapeOutcome.DefaultImplementation;
            }

            // Look at the per-accessor bodies. All-with-bodies → DIM, none-with-bodies → COM ABI,
            // mixed → error. We require the user to commit to one shape per property to avoid the
            // confusion of a property whose getter is in the vtable but whose setter isn't (or
            // vice versa) — that would silently change marshalling semantics on a per-accessor basis.
            if (propertyDeclaringSyntax.AccessorList is { } al)
            {
                int accessorCount = 0;
                int accessorsWithBody = 0;
                foreach (var accessor in al.Accessors)
                {
                    accessorCount++;
                    if (accessor.Body is not null || accessor.ExpressionBody is not null)
                    {
                        accessorsWithBody++;
                    }
                }

                if (accessorCount > 0 && accessorsWithBody > 0 && accessorsWithBody < accessorCount)
                {
                    diagnostic = property.CreateDiagnosticInfo(
                        GeneratorDiagnostics.PropertyAccessorsMustBeAllOrNothing,
                        property.Name, ifaceSymbol.ToDisplayString());
                    return MemberShapeOutcome.Error;
                }

                if (accessorCount > 0 && accessorsWithBody == accessorCount)
                {
                    return MemberShapeOutcome.DefaultImplementation;
                }
            }

            // For the COM ABI path (no bodies), reject ref / ref readonly returns the same way
            // ordinary methods do — there is no representation for a managed ref-return in a COM
            // vtable. DIM-shaped properties returning by ref are unaffected because the DIM
            // branches above have already returned MemberShapeOutcome.DefaultImplementation.
            if (property.ReturnsByRef || property.ReturnsByRefReadonly)
            {
                diagnostic = property.CreateDiagnosticInfo(
                    GeneratorDiagnostics.ReturnConfigurationNotSupported,
                    "ref return", property.ToDisplayString());
                return MemberShapeOutcome.Error;
            }

            // [MarshalUsing] on an accessor's value surface (the getter's return or the setter's
            // value parameter) must specify a marshaller type. A count-only or depth-only attribute
            // on the accessor could partially conflict with a property-level [MarshalUsing] and
            // silently shadow it in the property-to-accessor merge in SignatureContext. We require
            // the user to combine the marshaller type and count on a single accessor attribute or
            // attach the count-only attribute to the property declaration instead.
            if (!HasCompleteAccessorMarshalUsing(property))
            {
                diagnostic = property.CreateDiagnosticInfo(
                    GeneratorDiagnostics.MarshalUsingOnPropertyAccessorMustSpecifyType,
                    property.Name, ifaceSymbol.ToDisplayString());
                return MemberShapeOutcome.Error;
            }

            return MemberShapeOutcome.ComAbi;
        }

        private static bool HasCompleteAccessorMarshalUsing(IPropertySymbol property)
        {
            // Inspect the only two value surfaces that participate in the property-to-accessor
            // attribute merge in SignatureContext.MergeAccessorAndPropertyAttributes:
            //   - The getter's return type attributes (e.g., `[return: MarshalUsing(...)] get`).
            //   - The setter's value parameter attributes (e.g., `[param: MarshalUsing(...)] set`).
            // Index parameters on indexer accessors are deliberately excluded -- they don't merge
            // with property-level attributes, so [MarshalUsing] there cannot create the ambiguity
            // this diagnostic guards against. An accessor surface is "complete" when every
            // [MarshalUsing] on it specifies a marshaller type; a surface with no [MarshalUsing]
            // at all is trivially complete.
            if (property.GetMethod is { } getter
                && !IsMarshalUsingComplete(getter.GetReturnTypeAttributes()))
            {
                return false;
            }

            if (property.SetMethod is { } setter && setter.Parameters.Length > 0
                && !IsMarshalUsingComplete(setter.Parameters[setter.Parameters.Length - 1].GetAttributes()))
            {
                return false;
            }

            return true;

            static bool IsMarshalUsingComplete(ImmutableArray<AttributeData> attributes)
            {
                foreach (AttributeData attr in attributes)
                {
                    if (attr.AttributeClass?.ToDisplayString() == TypeNames.MarshalUsingAttribute
                        && attr.ConstructorArguments.Length == 0)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        private static void EmitMarshalAttributeWarningsForMethod(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            IMethodSymbol method,
            string memberName,
            INamedTypeSymbol ifaceSymbol)
        {
            foreach (var attribute in method.GetAttributes())
            {
                TryAddMarshalAttributeWarning(methods, attribute, memberName, ifaceSymbol);
            }

            foreach (var attribute in method.GetReturnTypeAttributes())
            {
                TryAddMarshalAttributeWarning(methods, attribute, memberName, ifaceSymbol);
            }

            foreach (var parameter in method.Parameters)
            {
                foreach (var attribute in parameter.GetAttributes())
                {
                    TryAddMarshalAttributeWarning(methods, attribute, memberName, ifaceSymbol);
                }
            }
        }

        private static void EmitMarshalAttributeWarningsForProperty(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            IPropertySymbol property,
            INamedTypeSymbol ifaceSymbol)
        {
            foreach (var attribute in property.GetAttributes())
            {
                TryAddMarshalAttributeWarning(methods, attribute, property.Name, ifaceSymbol);
            }

            if (property.GetMethod is { } getter)
            {
                EmitMarshalAttributeWarningsForMethod(methods, getter, property.Name, ifaceSymbol);
            }

            if (property.SetMethod is { } setter)
            {
                EmitMarshalAttributeWarningsForMethod(methods, setter, property.Name, ifaceSymbol);
            }
        }

        private static void TryAddMarshalAttributeWarning(
            ImmutableArray<DiagnosticOr<(ComMethodInfo, IMethodSymbol)>>.Builder methods,
            AttributeData attribute,
            string memberName,
            INamedTypeSymbol ifaceSymbol)
        {
            string? attrName = attribute.AttributeClass?.ToDisplayString();
            if (attrName != TypeNames.MarshalUsingAttribute
                && attrName != TypeNames.System_Runtime_InteropServices_MarshalAsAttribute)
            {
                return;
            }

            methods.Add(DiagnosticOr<(ComMethodInfo, IMethodSymbol)>.From(
                attribute.CreateDiagnosticInfo(
                    GeneratorDiagnostics.MarshalAttributeOnDefaultImplementedComInterfaceMember,
                    memberName, ifaceSymbol.ToDisplayString())));
        }
    }
}
