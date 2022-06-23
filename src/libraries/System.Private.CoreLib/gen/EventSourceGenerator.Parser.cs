// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        private static bool IsSyntaxTargetForGeneration(SyntaxNode node, CancellationToken cancellationToken) =>
            node is ClassDeclarationSyntax { AttributeLists.Count: > 0 };

        private static EventSourceClass? GetSemanticTargetForGeneration(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            const string EventSourceAutoGenerateAttribute = "System.Diagnostics.Tracing.EventSourceAutoGenerateAttribute";
            const string EventSourceAttribute = "System.Diagnostics.Tracing.EventSourceAttribute";

            var classDef = (ClassDeclarationSyntax)context.Node;
            SemanticModel sm = context.SemanticModel;
            EventSourceClass? eventSourceClass = null;

            bool autoGenerate = false;
            foreach (AttributeListSyntax cal in classDef.AttributeLists)
            {
                foreach (AttributeSyntax ca in cal.Attributes)
                {
                    if (sm.GetSymbolInfo(ca, cancellationToken).Symbol is not IMethodSymbol caSymbol)
                    {
                        // badly formed attribute definition, or not the right attribute
                        continue;
                    }

                    string attributeFullName = caSymbol.ContainingType.ToDisplayString();

                    if (attributeFullName.Equals(EventSourceAutoGenerateAttribute, StringComparison.Ordinal))
                    {
                        autoGenerate = true;
                        continue;
                    }

                    if (!attributeFullName.Equals(EventSourceAttribute, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string nspace = string.Empty;
                    NamespaceDeclarationSyntax? ns = classDef.Parent as NamespaceDeclarationSyntax;
                    if (ns is null)
                    {
                        if (classDef.Parent is not CompilationUnitSyntax)
                        {
                            // since this generator doesn't know how to generate a nested type...
                            continue;
                        }
                    }
                    else
                    {
                        nspace = ns.Name.ToString();
                        while (true)
                        {
                            ns = ns.Parent as NamespaceDeclarationSyntax;
                            if (ns == null)
                            {
                                break;
                            }

                            nspace = $"{ns.Name}.{nspace}";
                        }
                    }

                    string className = classDef.Identifier.ToString();
                    string name = className;
                    string guid = "";

                    SeparatedSyntaxList<AttributeArgumentSyntax>? args = ca.ArgumentList?.Arguments;
                    if (args is not null)
                    {
                        foreach (AttributeArgumentSyntax arg in args)
                        {
                            string argName = arg.NameEquals!.Name.Identifier.ToString();
                            string value = sm.GetConstantValue(arg.Expression, cancellationToken).ToString();

                            switch (argName)
                            {
                                case "Guid":
                                    guid = value;
                                    break;
                                case "Name":
                                    name = value;
                                    break;
                            }
                        }
                    }

                    if (!Guid.TryParse(guid, out Guid result))
                    {
                        result = GenerateGuidFromName(name.ToUpperInvariant());
                    }

                    eventSourceClass = new EventSourceClass(nspace, className, name, result);
                    continue;
                }
            }

            if (!autoGenerate)
            {
                return null;
            }

            return eventSourceClass;
        }

        // From System.Private.CoreLib
        private static Guid GenerateGuidFromName(string name)
        {
            ReadOnlySpan<byte> namespaceBytes = new byte[] // rely on C# compiler optimization to remove byte[] allocation
            {
                    0x48, 0x2C, 0x2D, 0xB2, 0xC3, 0x90, 0x47, 0xC8,
                    0x87, 0xF8, 0x1A, 0x15, 0xBF, 0xC1, 0x30, 0xFB,
            };

            byte[] bytes = Encoding.BigEndianUnicode.GetBytes(name);

            byte[] combinedBytes = new byte[namespaceBytes.Length + bytes.Length];

            bytes.CopyTo(combinedBytes, namespaceBytes.Length);
            namespaceBytes.CopyTo(combinedBytes);

#pragma warning disable CA5350 // Do Not Use Weak Cryptographic Algorithms
            using (SHA1 sha = SHA1.Create())
            {
                bytes = sha.ComputeHash(combinedBytes);
            }
#pragma warning restore CA5350 // Do Not Use Weak Cryptographic Algorithms

            Array.Resize(ref bytes, 16);

            bytes[7] = unchecked((byte)((bytes[7] & 0x0F) | 0x50));    // Set high 4 bits of octet 7 to 5, as per RFC 4122
            return new Guid(bytes);
        }
    }
}
