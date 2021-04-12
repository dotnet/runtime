// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
    [Generator]
    public sealed class JsonSourceGenerator : ISourceGenerator
    {
        public Dictionary<string, Type>? SerializableTypes { get; private set; }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext executionContext)
        {
            JsonSerializableSyntaxReceiver receiver = (JsonSerializableSyntaxReceiver)executionContext.SyntaxReceiver;
            MetadataLoadContext metadataLoadContext = new(executionContext.Compilation);

            TypeExtensions.NullableOfTType = metadataLoadContext.Resolve(typeof(Nullable<>));

            // Discover serializable types indicated by JsonSerializableAttribute.
            foreach (CompilationUnitSyntax compilationUnit in receiver.CompilationUnits)
            {
                SemanticModel compilationSemanticModel = executionContext.Compilation.GetSemanticModel(compilationUnit.SyntaxTree);

                foreach (AttributeListSyntax attributeListSyntax in compilationUnit.AttributeLists)
                {
                    AttributeSyntax attributeSyntax = attributeListSyntax.Attributes.Single();
                    IMethodSymbol attributeSymbol = compilationSemanticModel.GetSymbolInfo(attributeSyntax).Symbol as IMethodSymbol;

                    if (attributeSymbol?.ToString().StartsWith("System.Text.Json.Serialization.JsonSerializableAttribute") == true)
                    {
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

                       // TODO: parse the TypeInfoPropertyName arg if present.

                        Type type = new TypeWrapper(typeSymbol, metadataLoadContext);

                        if (type.Namespace == "<global namespace>")
                        {
                            // typeof() reference where the type's name isn't fully qualified.
                            // The compilation is not valid and user needs to fix their code.
                            // The compiler will notify the user so we don't have to.
                            return;
                        }

                        (SerializableTypes ??= new Dictionary<string, Type>())[type.FullName] = type;
                    }
                }
            }

            if (SerializableTypes == null)
            {
                return;
            }

            Debug.Assert(SerializableTypes.Count >= 1);
            JsonSourceGeneratorHelper helper = new(executionContext, metadataLoadContext, SerializableTypes);
            helper.GenerateSerializationMetadata();
        }
    }
}
