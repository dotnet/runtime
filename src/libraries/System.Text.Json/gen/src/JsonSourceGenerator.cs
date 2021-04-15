// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.Json.SourceGeneration.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    /// <summary>
    /// Generates source code to optimize serialization and deserialization with JsonSerializer.
    /// </summary>
    [Generator]
    public sealed class JsonSourceGenerator : ISourceGenerator
    {
        private JsonSourceGeneratorHelper? _helper;

        /// <summary>
        /// Helper for unit tests.
        /// TODO: refactor this after adopting emitter/parser pattern.
        /// </summary>
        public Dictionary<string, Type>? SerializableTypes => _helper.GetSerializableTypes();

        /// <summary>
        /// Registers a syntax resolver to receive compilation units.
        /// </summary>
        /// <param name="context"></param>
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }

        /// <summary>
        /// Generates source code to optimize serialization and deserialization with JsonSerializer.
        /// </summary>
        /// <param name="executionContext"></param>
        public void Execute(GeneratorExecutionContext executionContext)
        {
            Compilation compilation = executionContext.Compilation;

            const string JsonSerializableAttributeName = "System.Text.Json.Serialization.JsonSerializableAttribute";
            INamedTypeSymbol jsonSerializableAttribute = compilation.GetTypeByMetadataName(JsonSerializableAttributeName);
            if (jsonSerializableAttribute == null)
            {
                return;
            }

            JsonSerializableSyntaxReceiver receiver = (JsonSerializableSyntaxReceiver)executionContext.SyntaxReceiver;
            MetadataLoadContextInternal metadataLoadContext = new(compilation);

            TypeExtensions.NullableOfTType = metadataLoadContext.Resolve(typeof(Nullable<>));

            JsonSourceGeneratorHelper helper = new(executionContext, metadataLoadContext);
            _helper = helper;

            // Discover serializable types indicated by JsonSerializableAttribute.
            foreach (CompilationUnitSyntax compilationUnit in receiver.CompilationUnits)
            {
                SemanticModel compilationSemanticModel = executionContext.Compilation.GetSemanticModel(compilationUnit.SyntaxTree);

                foreach (AttributeListSyntax attributeListSyntax in compilationUnit.AttributeLists)
                {
                    AttributeSyntax attributeSyntax = attributeListSyntax.Attributes.Single();
                    IMethodSymbol attributeSymbol = compilationSemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;

                    if (attributeSymbol == null || !jsonSerializableAttribute.Equals(attributeSymbol.ContainingType, SymbolEqualityComparer.Default))
                    {
                        // Not the right attribute.
                        continue;
                    }

                    // Get JsonSerializableAttribute arguments.
                    IEnumerable<SyntaxNode> attributeArguments = attributeSyntax.DescendantNodes().Where(node => node is AttributeArgumentSyntax);

                    int argumentCount = attributeArguments.Count();

                    // Compiler shouldn't allow invalid signature for the JsonSerializable attribute.
                    Debug.Assert(argumentCount == 1 || argumentCount == 2);

                    // Obtain the one `Type` argument that must be present in the constructor of the attribute.
                    AttributeArgumentSyntax typeArgumentNode = (AttributeArgumentSyntax)attributeArguments.First();
                    TypeOfExpressionSyntax typeNode = (TypeOfExpressionSyntax)typeArgumentNode.ChildNodes().Single();
                    ExpressionSyntax typeNameSyntax = (ExpressionSyntax)typeNode.ChildNodes().Single();
                    ITypeSymbol typeSymbol = compilationSemanticModel.GetTypeInfo(typeNameSyntax).ConvertedType;

                    string? typeInfoPropertyName = null;
                    // Obtain the optional TypeInfoPropertyName boolean property on the attribute, if present.
                    if (argumentCount == 2)
                    {
                        AttributeArgumentSyntax stringArgumentNode = (AttributeArgumentSyntax)attributeArguments.ElementAt(1);
                        IEnumerable<SyntaxNode> childNodes = stringArgumentNode.ChildNodes();
                        SyntaxNode stringValueNode = childNodes.ElementAt(1);
                        typeInfoPropertyName = stringValueNode.GetFirstToken().ValueText;
                    }

                    Type type = new TypeWrapper(typeSymbol, metadataLoadContext);
                    if (type.Namespace == "<global namespace>")
                    {
                        // typeof() reference where the type's name isn't fully qualified.
                        // The compilation is not valid and user needs to fix their code.
                        // The compiler will notify the user so we don't have to.
                        return;
                    }

                    helper.RegisterRootSerializableType(type, typeInfoPropertyName);
                }
            }

            helper.GenerateSerializationMetadata();
        }
    }
}
