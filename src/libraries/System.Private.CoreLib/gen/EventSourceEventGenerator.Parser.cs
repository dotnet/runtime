// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Private.CoreLib.Generators.Models;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators
{
    public partial class EventSourceEventGenerator
    {
        private sealed partial class Parser
        {
            private const string EventAttribute = "System.Diagnostics.Tracing.EventAttribute";

            //https://learn.microsoft.com/en-us/dotnet/core/diagnostics/eventsource-instrumentation#supported-parameter-types
            private static readonly ImmutableHashSet<SpecialType> s_supportTypes = ImmutableHashSet.Create<SpecialType>(

                 SpecialType.System_Boolean,
                 SpecialType.System_Byte,
                 SpecialType.System_SByte,
                 SpecialType.System_Char,
                 SpecialType.System_Int16,
                 SpecialType.System_UInt16,
                 SpecialType.System_Int32,
                 SpecialType.System_Int64,
                 SpecialType.System_UInt64,
                 SpecialType.System_UInt32,
                 SpecialType.System_Single,
                 SpecialType.System_Double,
                 SpecialType.System_Decimal,
                 SpecialType.System_String,
                 SpecialType.System_DateTime,
                 SpecialType.System_Enum
            );
            private static bool IsSupportType(ITypeSymbol type, SemanticModel model)
            {
                if (s_supportTypes.Contains(type.SpecialType) || type.TypeKind == TypeKind.Enum)
                {
                    return true;
                }

                INamedTypeSymbol? guidType = model.Compilation.GetTypeByMetadataName("System.Guid");
                INamedTypeSymbol? timeSpanType = model.Compilation.GetTypeByMetadataName("System.TimeSpan");
                INamedTypeSymbol? dateTimeOffsetType = model.Compilation.GetTypeByMetadataName("System.DateTimeOffset");
                INamedTypeSymbol? uintPtrType = model.Compilation.GetTypeByMetadataName("System.UIntPtr");

                if (type.IsValueType)
                {
                    if (SymbolEqualityComparer.Default.Equals(type, guidType) ||
                        SymbolEqualityComparer.Default.Equals(type, timeSpanType) ||
                        SymbolEqualityComparer.Default.Equals(type, dateTimeOffsetType) ||
                        SymbolEqualityComparer.Default.Equals(type, uintPtrType))
                    {
                        return true;
                    }

                    if (type.OriginalDefinition != null &&
                        type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
                        type is INamedTypeSymbol symbol &&
                        symbol.TypeArguments.Length == 1)
                    {
                        return IsSupportType(symbol.TypeArguments[0], model);
                    }
                }
                return false;
            }
            /// <summary>
            /// Parse the event method
            /// </summary>
            /// <param name="methodSyntax">The method declaration syntax</param>
            /// <param name="diagnostics">The dianositic list</param>
            /// <returns>If parse fail, return <see langword="null"/>, otherwise return the parsed result</returns>
            private static EventMethod? ParseEventMethod(MethodDeclarationSyntax methodSyntax, SemanticModel semanticModel, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                var methodSymbol = semanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);
                var eventAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(EventAttribute);

                if (eventAttributeSymbol == null)
                {
                    return null;
                }

                Debug.Assert(methodSyntax != null);

                //Check the arguments is support
                if (methodSymbol.Parameters.Length != 0)
                {
                    foreach (IParameterSymbol parameter in methodSymbol.Parameters)
                    {
                        if (!IsSupportType(parameter.Type, semanticModel))
                        {
                            diagnostics.Add(Diagnostic.Create(EventSourceNoSupportType, parameter.Locations[0], parameter.Type.ToString()));
                            return null;
                        }
                    }
                }

                //Generate parsed event method
                var args = new List<EventMethodArgument>(methodSymbol.Parameters.Length);
                for (int i = 0; i < methodSymbol.Parameters.Length; i++)
                {
                    IParameterSymbol parameter = methodSymbol.Parameters[i];
                    string typeName = parameter.Type.ToDisplayString();
                    var eventMethodArgument = new EventMethodArgument { Name = parameter.Name, SpecialType = parameter.Type.SpecialType, Index = i, TypeName = typeName };
                    args.Add(eventMethodArgument);
                }


                var eventAttribute = methodSymbol.GetAttributes().First(x => SymbolEqualityComparer.Default.Equals(x.AttributeClass, eventAttributeSymbol));

                var eventId = (int)eventAttribute.ConstructorArguments[0].Value;//The construct first argument is eventId

                return new EventMethod { Name = methodSymbol.Name, Arguments = args, EventId = eventId };
            }


            /// <summary>
            /// Returns the kind keyword corresponding to the specified declaration syntax node.
            /// </summary>
            private static string GetTypeKindKeyword(TypeDeclarationSyntax typeDeclaration)
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
            //https://github.com/dotnet/runtime/blob/c87cbf63954f179785bb038c23352e60d3c0a933/src/libraries/System.Text.Json/gen/JsonSourceGenerator.Parser.cs#L126
            private static bool TryGetNestedTypeDeclarations(ClassDeclarationSyntax contextClassSyntax, SemanticModel semanticModel, CancellationToken cancellationToken, [NotNullWhen(true)] out List<string>? typeDeclarations)
            {
                typeDeclarations = null;

                for (TypeDeclarationSyntax? currentType = contextClassSyntax; currentType != null; currentType = currentType.Parent as TypeDeclarationSyntax)
                {
                    StringBuilder stringBuilder = new();
                    bool isPartialType = false;

                    foreach (SyntaxToken modifier in currentType.Modifiers)
                    {
                        stringBuilder.Append(modifier.Text);
                        stringBuilder.Append(' ');
                        isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
                    }

                    if (!isPartialType)
                    {
                        typeDeclarations = null;
                        return false;
                    }

                    stringBuilder.Append(GetTypeKindKeyword(currentType));
                    stringBuilder.Append(' ');

                    INamedTypeSymbol? typeSymbol = semanticModel.GetDeclaredSymbol(currentType, cancellationToken);
                    Debug.Assert(typeSymbol != null);

                    string typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
                    stringBuilder.Append(typeName);

                    (typeDeclarations ??= new()).Add(stringBuilder.ToString());
                }

                Debug.Assert(typeDeclarations?.Count > 0);
                return true;
            }

            public EventMethodsParsedResult Parse(ClassDeclarationSyntax contextClassDeclaration, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                SyntaxToken paritialKeyWordToken = SyntaxFactory.Token(SyntaxKind.PartialKeyword);

                if (!TryGetNestedTypeDeclarations(contextClassDeclaration, semanticModel, cancellationToken, out List<string>? typeDeclarations))
                {
                    return new EventMethodsParsedResult(null,
                        contextClassDeclaration.Identifier.ValueText,
                        ImmutableArray<EventMethod>.Empty,
                        ImmutableArray.Create<Diagnostic>(Diagnostic.Create(ContextClassesMustBePartial, contextClassDeclaration.GetLocation(),
                        contextClassDeclaration.Identifier.ValueText)));
                }

                //Get all partial method
                IEnumerable<MethodDeclarationSyntax> allEventMethods = contextClassDeclaration.Members.OfType<MethodDeclarationSyntax>().Where(IsAcceptMethod);

                ImmutableArray<EventMethod> eventMethods = ImmutableArray.Create<EventMethod>();
                ImmutableArray<Diagnostic> diagnostics = ImmutableArray.Create<Diagnostic>();

                foreach (MethodDeclarationSyntax? method in allEventMethods)
                {
                    EventMethod? eventMethod = ParseEventMethod(method, semanticModel, diagnostics, cancellationToken);
                    eventMethods.Add(eventMethod);
                }

                INamespaceOrTypeSymbol contextTypeSymbol = semanticModel.GetDeclaredSymbol(contextClassDeclaration, cancellationToken);
                Debug.Assert(contextTypeSymbol != null);

                string? @namespace = null;

                if (contextTypeSymbol.ContainingNamespace is { IsGlobalNamespace: false })
                {
                    @namespace = contextTypeSymbol.ContainingNamespace.ToDisplayString();
                }

                return new EventMethodsParsedResult(@namespace, contextClassDeclaration.Identifier.ValueText, eventMethods, diagnostics)
                {
                    ContextClassDeclarations = typeDeclarations.ToImmutableArray()
                };

                //Check the method syntax has partial and has [Event] attribute
                bool IsAcceptMethod(MethodDeclarationSyntax methodSyntax)
                {
                    if (!methodSyntax.Modifiers.Contains(paritialKeyWordToken))//Must has partial keyword
                    {
                        return false;
                    }

                    foreach (AttributeListSyntax attributeList in methodSyntax.AttributeLists)
                    {
                        foreach (AttributeSyntax attribute in attributeList.Attributes)
                        {
                            if (attribute.Name.ToString() == EventAttribute)//Any event attribute
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                }
            }
        }
    }
}
