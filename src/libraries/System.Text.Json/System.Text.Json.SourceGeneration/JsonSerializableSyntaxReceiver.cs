// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace System.Text.Json.SourceGeneration
{
        public class JsonSerializableSyntaxReceiver : ISyntaxReceiver
        {
            public List<KeyValuePair<string, IdentifierNameSyntax>> ExternalClassTypes = new List<KeyValuePair<string, IdentifierNameSyntax>>();
            public List<KeyValuePair<string, TypeDeclarationSyntax>> InternalClassTypes = new List<KeyValuePair<string, TypeDeclarationSyntax>>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Look for classes or structs for JsonSerializable Attribute.
                if (syntaxNode is ClassDeclarationSyntax || syntaxNode is StructDeclarationSyntax)
                {
                    // Find JsonSerializable Attributes.
                    IEnumerable<AttributeSyntax>? serializableAttributes = null;
                    AttributeListSyntax attributeList = ((TypeDeclarationSyntax)syntaxNode).AttributeLists.SingleOrDefault();
                    if (attributeList != null)
                    {
                        serializableAttributes = attributeList.Attributes.Where(node => (node is AttributeSyntax attr && attr.Name.ToString() == "JsonSerializable")).Cast<AttributeSyntax>();
                    }

                    if (serializableAttributes?.Any() == true)
                    {
                        // JsonSerializableAttribute has AllowMultiple as False, should only have 1 attribute.
                        Debug.Assert(serializableAttributes.Count() == 1);
                        AttributeSyntax attributeNode = serializableAttributes.First();

                        // Check if the attribute is being passed a type.
                        if (attributeNode.DescendantNodes().Where(node => node is TypeOfExpressionSyntax).Any())
                        {
                            // Get JsonSerializable attribute arguments.
                            AttributeArgumentSyntax attributeArgumentNode = (AttributeArgumentSyntax)attributeNode.DescendantNodes().Where(node => node is AttributeArgumentSyntax).SingleOrDefault();
                            // Get external class token from arguments.
                            IdentifierNameSyntax externalTypeNode = (IdentifierNameSyntax)attributeArgumentNode?.DescendantNodes().Where(node => node is IdentifierNameSyntax).SingleOrDefault();
                            ExternalClassTypes.Add(new KeyValuePair<string, IdentifierNameSyntax>(((TypeDeclarationSyntax)syntaxNode).Identifier.Text, externalTypeNode));
                        }
                        else
                        {
                            InternalClassTypes.Add(new KeyValuePair<string, TypeDeclarationSyntax>(((TypeDeclarationSyntax)syntaxNode).Identifier.Text, (TypeDeclarationSyntax)syntaxNode));
                        }
                    }
                }
            }
        }
}
