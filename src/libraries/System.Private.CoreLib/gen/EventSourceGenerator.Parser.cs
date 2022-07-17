// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Generators
{
    public partial class EventSourceGenerator
    {
        private static EventSourceClass? GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
        {
            const string EventSourceAttribute = "System.Diagnostics.Tracing.EventSourceAttribute";

            var classDef = (ClassDeclarationSyntax)context.TargetNode;
            NamespaceDeclarationSyntax? ns = classDef.Parent as NamespaceDeclarationSyntax;
            if (ns is null)
            {
                if (classDef.Parent is not CompilationUnitSyntax)
                {
                    // since this generator doesn't know how to generate a nested type...
                    return null;
                }
            }

            EventSourceClass? eventSourceClass = null;
            string? nspace = null;

            foreach (AttributeData attribute in context.TargetSymbol.GetAttributes())
            {
                if (attribute.AttributeClass?.Name != "EventSourceAttribute" ||
                    attribute.AttributeClass.ToDisplayString() != EventSourceAttribute)
                {
                    continue;
                }

                nspace ??= ConstructNamespace(ns);

                string className = classDef.Identifier.ValueText;
                string name = className;
                string guid = "";

                ImmutableArray<KeyValuePair<string, TypedConstant>> args = attribute.NamedArguments;
                foreach (KeyValuePair<string, TypedConstant> arg in args)
                {
                    string argName = arg.Key;
                    string value = arg.Value.Value?.ToString();

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

                if (!Guid.TryParse(guid, out Guid result))
                {
                    result = GenerateGuidFromName(name.ToUpperInvariant());
                }

                eventSourceClass = new EventSourceClass(nspace, className, name, result);
                continue;
            }

            return eventSourceClass;
        }

        private static string? ConstructNamespace(NamespaceDeclarationSyntax? ns)
        {
            if (ns is null)
                return string.Empty;

            string nspace = ns.Name.ToString();
            while (true)
            {
                ns = ns.Parent as NamespaceDeclarationSyntax;
                if (ns == null)
                {
                    break;
                }

                nspace = $"{ns.Name}.{nspace}";
            }

            return nspace;
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
