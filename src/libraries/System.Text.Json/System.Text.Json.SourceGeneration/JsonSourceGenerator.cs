// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Base JsonSerializerSourceGenerator. This class will invoke CodeGenerator within Execute
    /// to generate wanted output code for JsonSerializers.
    /// </summary>
    [Generator]
    public class JsonSourceGenerator : ISourceGenerator
    {
        public Dictionary<string, Type> FoundTypes = new Dictionary<string, Type>();

        public void Execute(SourceGeneratorContext context)
        {
            JsonSerializableSyntaxReceiver receiver = (JsonSerializableSyntaxReceiver)context.SyntaxReceiver;
            MetadataLoadContext metadataLoadContext = new MetadataLoadContext(context.Compilation);

            // Filter classes and structs with JsonSerializable attribute semantically.
            INamedTypeSymbol jsonSerializableAttributeSymbol = context.Compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonSerializableAttribute");

            if (jsonSerializableAttributeSymbol == null)
            {
                return;
            }

            // Find classes with JsonSerializable Attributes.
            foreach (TypeDeclarationSyntax typeDeclarationNode in receiver.TypesWithAttributes)
            {
                SemanticModel model = context.Compilation.GetSemanticModel(typeDeclarationNode.SyntaxTree);

                // Check if it contains a JsonSerializableAttribute.
                INamedTypeSymbol typeSymbol = (INamedTypeSymbol)model.GetDeclaredSymbol(typeDeclarationNode);
                if(typeSymbol.GetAttributes().Any(attr => attr.AttributeClass.Equals(jsonSerializableAttributeSymbol, SymbolEqualityComparer.Default)))
                {
                    // JsonSerializableAttribute has AllowMultiple as False, should have a single attribute.
                    AttributeListSyntax attributeList = typeDeclarationNode.AttributeLists.Single();
                    IEnumerable<AttributeSyntax> serializableAttributes = attributeList.Attributes.Where(node => node is AttributeSyntax).Cast<AttributeSyntax>();
                    AttributeSyntax attributeNode = serializableAttributes.First();
                    Debug.Assert(serializableAttributes.Count() == 1);

                    // Check if the attribute is being passed a type.
                    if (attributeNode.DescendantNodes().Where(node => node is TypeOfExpressionSyntax).Any())
                    {
                        // Get JsonSerializable attribute arguments.
                        AttributeArgumentSyntax attributeArgumentNode = (AttributeArgumentSyntax)attributeNode.DescendantNodes().Where(node => node is AttributeArgumentSyntax).SingleOrDefault();
                        // Get external class token from arguments.
                        IdentifierNameSyntax externalTypeNode = (IdentifierNameSyntax)attributeArgumentNode?.DescendantNodes().Where(node => node is IdentifierNameSyntax).SingleOrDefault();

                        // Get non-user owned typeSymbol from IdentifierNameSyntax and add to found types.
                        INamedTypeSymbol externalTypeSymbol = model.GetTypeInfo(externalTypeNode).ConvertedType as INamedTypeSymbol;
                        FoundTypes[typeDeclarationNode.Identifier.Text] = new TypeWrapper(externalTypeSymbol, metadataLoadContext);
                    }
                    else
                    {
                        // Add user owned type into found types.
                        FoundTypes[typeDeclarationNode.Identifier.Text] = new TypeWrapper(typeSymbol, metadataLoadContext);
                    }
                }
            }

            JsonSourceGeneratorHelper codegen = new JsonSourceGeneratorHelper();

            // Add base default instance source.
            context.AddSource("BaseClassInfo.g.cs", SourceText.From(codegen.GenerateHelperContextInfo(), Encoding.UTF8));

            // Run type discovery generation for each root type.
            foreach (KeyValuePair<string, Type> entry in FoundTypes)
            {
                codegen.GenerateClassInfo(entry.Value);
            }

            // Generate sources for each type.
            foreach (KeyValuePair<Type, string> entry in codegen.Types)
            {
                context.AddSource($"{entry.Key.Name}ClassInfo.g.cs", SourceText.From(entry.Value, Encoding.UTF8));
            }

            // For each diagnostic, report to the user.
            foreach (Diagnostic diagnostic in codegen.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic);
            }
        }

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }
    }
}
