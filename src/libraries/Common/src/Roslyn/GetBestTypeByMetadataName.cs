// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions
{
    internal static class RoslynExtensions
    {
        // Copied from: https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/CompilationExtensions.cs
        /// <summary>
        /// Gets a type by its metadata name to use for code analysis within a <see cref="Compilation"/>. This method
        /// attempts to find the "best" symbol to use for code analysis, which is the symbol matching the first of the
        /// following rules.
        ///
        /// <list type="number">
        ///   <item><description>
        ///     If only one type with the given name is found within the compilation and its referenced assemblies, that
        ///     type is returned regardless of accessibility.
        ///   </description></item>
        ///   <item><description>
        ///     If the current <paramref name="compilation"/> defines the symbol, that symbol is returned.
        ///   </description></item>
        ///   <item><description>
        ///     If exactly one referenced assembly defines the symbol in a manner that makes it visible to the current
        ///     <paramref name="compilation"/>, that symbol is returned.
        ///   </description></item>
        ///   <item><description>
        ///     Otherwise, this method returns <see langword="null"/>.
        ///   </description></item>
        /// </list>
        /// </summary>
        /// <param name="compilation">The <see cref="Compilation"/> to consider for analysis.</param>
        /// <param name="fullyQualifiedMetadataName">The fully-qualified metadata type name to find.</param>
        /// <returns>The symbol to use for code analysis; otherwise, <see langword="null"/>.</returns>
        public static INamedTypeSymbol? GetBestTypeByMetadataName(this Compilation compilation, string fullyQualifiedMetadataName)
        {
            // Try to get the unique type with this name, ignoring accessibility
            var type = compilation.GetTypeByMetadataName(fullyQualifiedMetadataName);

            // Otherwise, try to get the unique type with this name originally defined in 'compilation'
            type ??= compilation.Assembly.GetTypeByMetadataName(fullyQualifiedMetadataName);

            // Otherwise, try to get the unique accessible type with this name from a reference
            if (type is null)
            {
                foreach (var module in compilation.Assembly.Modules)
                {
                    foreach (var referencedAssembly in module.ReferencedAssemblySymbols)
                    {
                        var currentType = referencedAssembly.GetTypeByMetadataName(fullyQualifiedMetadataName);
                        if (currentType is null)
                            continue;

                        switch (currentType.GetResultantVisibility())
                        {
                            case SymbolVisibility.Public:
                            case SymbolVisibility.Internal when referencedAssembly.GivesAccessTo(compilation.Assembly):
                                break;

                            default:
                                continue;
                        }

                        if (type is object)
                        {
                            // Multiple visible types with the same metadata name are present
                            return null;
                        }

                        type = currentType;
                    }
                }
            }

            return type;
        }

        // copied from https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Extensions/ISymbolExtensions.cs
        private static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            // Start by assuming it's visible.
            SymbolVisibility visibility = SymbolVisibility.Public;

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    // Aliases are uber private.  They're only visible in the same file that they
                    // were declared in.
                    return SymbolVisibility.Private;

                case SymbolKind.Parameter:
                    // Parameters are only as visible as their containing symbol
                    return GetResultantVisibility(symbol.ContainingSymbol);

                case SymbolKind.TypeParameter:
                    // Type Parameters are private.
                    return SymbolVisibility.Private;
            }

            while (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    // If we see anything private, then the symbol is private.
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        return SymbolVisibility.Private;

                    // If we see anything internal, then knock it down from public to
                    // internal.
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        visibility = SymbolVisibility.Internal;
                        break;

                        // For anything else (Public, Protected, ProtectedOrInternal), the
                        // symbol stays at the level we've gotten so far.
                }

                symbol = symbol.ContainingSymbol;
            }

            return visibility;
        }

        // Copied from: https://github.com/dotnet/roslyn/blob/main/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/Utilities/SymbolVisibility.cs
#pragma warning disable CA1027 // Mark enums with FlagsAttribute
        private enum SymbolVisibility
#pragma warning restore CA1027 // Mark enums with FlagsAttribute
        {
            Public = 0,
            Internal = 1,
            Private = 2,
            Friend = Internal,
        }

        internal static bool HasAttributeSuffix(this string name, bool isCaseSensitive)
        {
            const string AttributeSuffix = "Attribute";

            var comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return name.Length > AttributeSuffix.Length && name.EndsWith(AttributeSuffix, comparison);
        }

        public static ImmutableArray<T> ToImmutableArray<T>(this ReadOnlySpan<T> span)
        {
            switch (span.Length)
            {
                case 0: return ImmutableArray<T>.Empty;
                case 1: return ImmutableArray.Create(span[0]);
                case 2: return ImmutableArray.Create(span[0], span[1]);
                case 3: return ImmutableArray.Create(span[0], span[1], span[2]);
                case 4: return ImmutableArray.Create(span[0], span[1], span[2], span[3]);
                default:
                    var builder = ImmutableArray.CreateBuilder<T>(span.Length);
                    foreach (var item in span)
                        builder.Add(item);

                    return builder.MoveToImmutable();
            }
        }

        public static SimpleNameSyntax GetUnqualifiedName(this NameSyntax name)
            => name switch
            {
                AliasQualifiedNameSyntax alias => alias.Name,
                QualifiedNameSyntax qualified => qualified.Right,
                SimpleNameSyntax simple => simple,
                _ => throw new InvalidOperationException("Unreachable"),
            };
    }
}
