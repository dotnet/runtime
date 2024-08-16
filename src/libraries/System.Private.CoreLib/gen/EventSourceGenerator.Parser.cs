// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Tracing;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
                    result = EventSourceUtility.GenerateGuidFromName(name.ToUpperInvariant());
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
    }
}
