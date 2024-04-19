// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace System.Text.Json.SourceGeneration
{
    internal static class RoslynExtensions
    {
        public static LanguageVersion? GetLanguageVersion(this Compilation compilation)
            => compilation is CSharpCompilation csc ? csc.LanguageVersion : null;

        public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, Type type)
        {
            Debug.Assert(!type.IsArray, "Resolution logic only capable of handling named types.");
            Debug.Assert(type.FullName != null);
            return compilation.GetBestTypeByMetadataName(type.FullName);
        }

        public static Location? GetLocation(this ISymbol typeSymbol)
            => typeSymbol.Locations.Length > 0 ? typeSymbol.Locations[0] : null;

        public static Location? GetLocation(this AttributeData attributeData)
        {
            SyntaxReference? reference = attributeData.ApplicationSyntaxReference;
            return reference?.SyntaxTree.GetLocation(reference.Span);
        }

        /// <summary>
        /// Returns true if the specified location is contained in one of the syntax trees in the compilation.
        /// </summary>
        public static bool ContainsLocation(this Compilation compilation, Location location)
            => location.SourceTree != null && compilation.ContainsSyntaxTree(location.SourceTree);

        /// <summary>
        /// Removes any type metadata that is erased at compile time, such as NRT annotations and tuple labels.
        /// </summary>
        public static ITypeSymbol EraseCompileTimeMetadata(this Compilation compilation, ITypeSymbol type)
        {
            if (type.NullableAnnotation is NullableAnnotation.Annotated)
            {
                type = type.WithNullableAnnotation(NullableAnnotation.None);
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsTupleType)
                {
                    if (namedType.TupleElements.Length < 2)
                    {
                        return type;
                    }

                    ImmutableArray<ITypeSymbol> erasedElements = namedType.TupleElements
                        .Select(e => compilation.EraseCompileTimeMetadata(e.Type))
                        .ToImmutableArray();

                    type = compilation.CreateTupleTypeSymbol(erasedElements);
                }
                else if (namedType.IsGenericType)
                {
                    if (namedType.IsUnboundGenericType)
                    {
                        return namedType;
                    }

                    ImmutableArray<ITypeSymbol> typeArguments = namedType.TypeArguments;
                    INamedTypeSymbol? containingType = namedType.ContainingType;

                    if (containingType?.IsGenericType == true)
                    {
                        containingType = (INamedTypeSymbol)compilation.EraseCompileTimeMetadata(containingType);
                        type = namedType = containingType.GetTypeMembers().First(t => t.Name == namedType.Name && t.Arity == namedType.Arity);
                    }

                    if (typeArguments.Length > 0)
                    {
                        ITypeSymbol[] erasedTypeArgs = typeArguments
                            .Select(compilation.EraseCompileTimeMetadata)
                            .ToArray();

                        type = namedType.ConstructedFrom.Construct(erasedTypeArgs);
                    }
                }
            }

            return type;
        }

        public static bool CanUseDefaultConstructorForDeserialization(this ITypeSymbol type, out IMethodSymbol? constructorInfo)
        {
            if (type.IsAbstract || type.TypeKind is TypeKind.Interface || type is not INamedTypeSymbol namedType)
            {
                constructorInfo = null;
                return false;
            }

            constructorInfo = namedType.GetExplicitlyDeclaredInstanceConstructors().FirstOrDefault(ctor => ctor.DeclaredAccessibility is Accessibility.Public && ctor.Parameters.Length == 0);
            return constructorInfo != null || type.IsValueType;
        }

        public static IEnumerable<IMethodSymbol> GetExplicitlyDeclaredInstanceConstructors(this INamedTypeSymbol type)
            => type.Constructors.Where(ctor => !ctor.IsStatic && !(ctor.IsImplicitlyDeclared && type.IsValueType && ctor.Parameters.Length == 0));

        public static bool ContainsAttribute(this ISymbol memberInfo, INamedTypeSymbol? attributeType)
            => attributeType != null && memberInfo.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, attributeType));

        public static bool IsVirtual(this ISymbol symbol)
            => symbol.IsVirtual || symbol.IsOverride || symbol.IsAbstract;

        public static bool IsAssignableFrom(this ITypeSymbol? baseType, ITypeSymbol? type)
        {
            if (baseType is null || type is null)
            {
                return false;
            }

            if (baseType.TypeKind is TypeKind.Interface)
            {
                if (type.AllInterfaces.Contains(baseType, SymbolEqualityComparer.Default))
                {
                    return true;
                }
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, current))
                {
                    return true;
                }
            }

            return false;
        }

        public static INamedTypeSymbol? GetCompatibleGenericBaseType(this ITypeSymbol type, INamedTypeSymbol? baseType)
        {
            if (baseType is null)
            {
                return null;
            }

            Debug.Assert(baseType.IsGenericTypeDefinition());

            if (baseType.TypeKind is TypeKind.Interface)
            {
                foreach (INamedTypeSymbol interfaceType in type.AllInterfaces)
                {
                    if (IsMatchingGenericType(interfaceType, baseType))
                    {
                        return interfaceType;
                    }
                }
            }

            for (INamedTypeSymbol? current = type as INamedTypeSymbol; current != null; current = current.BaseType)
            {
                if (IsMatchingGenericType(current, baseType))
                {
                    return current;
                }
            }

            return null;

            static bool IsMatchingGenericType(INamedTypeSymbol candidate, INamedTypeSymbol baseType)
            {
                return candidate.IsGenericType && SymbolEqualityComparer.Default.Equals(candidate.ConstructedFrom, baseType);
            }
        }

        public static bool IsGenericTypeDefinition(this ITypeSymbol type)
            => type is INamedTypeSymbol { IsGenericType: true } namedType && SymbolEqualityComparer.Default.Equals(namedType, namedType.ConstructedFrom);

        public static bool IsNumberType(this ITypeSymbol type)
        {
            return type.SpecialType is
                SpecialType.System_SByte or SpecialType.System_Int16 or SpecialType.System_Int32 or SpecialType.System_Int64 or
                SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64 or
                SpecialType.System_Single or SpecialType.System_Double or SpecialType.System_Decimal;
        }

        public static bool IsNullableValueType(this ITypeSymbol type, [NotNullWhen(true)] out ITypeSymbol? elementType)
        {
            if (type.IsValueType && type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T })
            {
                elementType = ((INamedTypeSymbol)type).TypeArguments[0];
                return true;
            }

            elementType = null;
            return false;
        }

        public static ITypeSymbol GetMemberType(this ISymbol member)
        {
            Debug.Assert(member is IFieldSymbol or IPropertySymbol);
            return member is IFieldSymbol fs ? fs.Type : ((IPropertySymbol)member).Type;
        }

        public static bool IsOverriddenOrShadowedBy(this ISymbol member, ISymbol otherMember)
        {
            Debug.Assert(member is IFieldSymbol or IPropertySymbol);
            Debug.Assert(otherMember is IFieldSymbol or IPropertySymbol);
            return member.Name == otherMember.Name && member.ContainingType.IsAssignableFrom(otherMember.ContainingType);
        }

        public static bool MemberNameNeedsAtSign(this ISymbol symbol)
            => SyntaxFacts.GetKeywordKind(symbol.Name) != SyntaxKind.None || SyntaxFacts.GetContextualKeywordKind(symbol.Name) != SyntaxKind.None;

        public static INamedTypeSymbol[] GetSortedTypeHierarchy(this ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol namedType)
            {
                return Array.Empty<INamedTypeSymbol>();
            }

            if (type.TypeKind != TypeKind.Interface)
            {
                var list = new List<INamedTypeSymbol>();
                for (INamedTypeSymbol? current = namedType; current != null; current = current.BaseType)
                {
                    list.Add(current);
                }

                return list.ToArray();
            }
            else
            {
                // Interface hierarchies support multiple inheritance.
                // For consistency with class hierarchy resolution order,
                // sort topologically from most derived to least derived.
                return JsonHelpers.TraverseGraphWithTopologicalSort<INamedTypeSymbol>(namedType, static t => t.AllInterfaces, SymbolEqualityComparer.Default);
            }
        }

        /// <summary>
        /// Returns the kind keyword corresponding to the specified declaration syntax node.
        /// </summary>
        public static string GetTypeKindKeyword(this TypeDeclarationSyntax typeDeclaration)
        {
            switch (typeDeclaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    return "class";
                case SyntaxKind.InterfaceDeclaration:
                    return "interface";
                case SyntaxKind.StructDeclaration:
                    return "struct";
                case SyntaxKind.RecordDeclaration:
                    return "record";
                case SyntaxKind.RecordStructDeclaration:
                    return "record struct";
                case SyntaxKind.EnumDeclaration:
                    return "enum";
                case SyntaxKind.DelegateDeclaration:
                    return "delegate";
                default:
                    Debug.Fail("unexpected syntax kind");
                    return null;
            }
        }
    }
}
