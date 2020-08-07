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
    public class JsonSerializerSourceGenerator : ISourceGenerator
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
                        ITypeSymbol externalTypeSymbol = model.GetTypeInfo(externalTypeNode).ConvertedType;
                        FoundTypes[typeDeclarationNode.Identifier.Text] = new TypeWrapper(externalTypeSymbol, metadataLoadContext);
                    }
                    else
                    {
                        // Add user owned type into found types.
                        FoundTypes[typeDeclarationNode.Identifier.Text] = new TypeWrapper(typeSymbol, metadataLoadContext);
                    }
                }
            }

            // Create sources for all found types.
            StringBuilder member = new StringBuilder();
            string foundMethods, foundFields, foundProperties, foundCtorParams, foundCtors;

            foreach (KeyValuePair<string, Type> entry in FoundTypes)
            {
                foreach (MethodInfo method in entry.Value.GetMethods())
                {
                    member.Append(@$"""{method.Name}"", "); 
                }
                foundMethods = member.ToString();
                member.Clear();

                foreach (FieldInfo field in entry.Value.GetFields())
                {
                    member.Append(@$"{{""{field.Name}"", ""{field.FieldType.Name}""}}, "); 
                }
                foundFields = member.ToString();
                member.Clear();

                foreach (PropertyInfo property in entry.Value.GetProperties())
                {
                    member.Append(@$"{{""{property.Name}"", ""{property.PropertyType.Name}""}}, "); 
                }
                foundProperties = member.ToString();
                member.Clear();

                foreach (ConstructorInfo ctor in entry.Value.GetConstructors())
                {
                    foreach (ParameterInfo param in ctor.GetParameters())
                    {
                        member.Append(@$"{{""{param.Name}"", ""{param.ParameterType.Name}""}}, "); 
                    }
                }
                foundCtorParams = member.ToString();
                member.Clear();

                foreach (ConstructorInfo ctor in entry.Value.GetConstructors())
                {
                    member.Append($@"""{ctor.Name}"", ");
                }
                foundCtors = member.ToString();
                member.Clear();

                context.AddSource($"{entry.Key}ClassInfo", SourceText.From($@"
using System.Collections.Generic;

namespace HelloWorldGenerated
{{
    public class {entry.Key}ClassInfo
    {{
        public {entry.Key}ClassInfo() {{ }}

        private List<string> ClassCtors = new List<string>()
        {{ {foundCtors} }};
        private Dictionary<string, string> ClassCtorParams = new Dictionary<string, string>()
        {{ {foundCtorParams} }};
        private List<string> ClassMethods = new List<string>()
        {{ {foundMethods} }};
        private Dictionary<string, string> ClassFields = new Dictionary<string, string>()
        {{ {foundFields} }};
        private Dictionary<string, string> ClassProperties = new Dictionary<string, string>()
        {{ {foundProperties} }};

        public string GetClassName()
        {{
            return ""{entry.Key}ClassInfo"";
        }}

        public List<string> Ctors
        {{ get {{ return ClassCtors; }} }}

        public Dictionary<string, string> CtorParams
        {{ get {{ return ClassCtorParams; }} }}

        public List<string> Methods 
        {{ get {{ return ClassMethods; }} }}

        public Dictionary<string, string> Fields
        {{ get {{ return ClassFields; }} }}

        public Dictionary<string, string> Properties
        {{ get {{ return ClassProperties; }} }}
    }}
}}
", Encoding.UTF8));
            }
        }

        public void Initialize(InitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new JsonSerializableSyntaxReceiver());
        }
    }
}
