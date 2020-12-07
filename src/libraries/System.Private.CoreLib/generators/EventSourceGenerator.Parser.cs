// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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
        private class Parser
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Compilation _compilation;
            private readonly Action<Diagnostic> _reportDiagnostic;

            public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                _compilation = compilation;
                _cancellationToken = cancellationToken;
                _reportDiagnostic = reportDiagnostic;
            }

            public EventSourceClass[] GetEventSourceClasses(List<ClassDeclarationSyntax> classDeclarations)
            {
                var autogenerateAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSourceAutoGenerateAttribute");
                if (autogenerateAttribute is null)
                {
                    // No EventSourceAutoGenerateAttribute
                    return Array.Empty<EventSourceClass>();
                }

                var eventSourceAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSourceAttribute");
                if (eventSourceAttribute is null)
                {
                    // No EventSourceAttribute
                    return Array.Empty<EventSourceClass>();
                }

                List<EventSourceClass>? results = null;
                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (var group in classDeclarations.GroupBy(x => x.SyntaxTree))
                {
                    SemanticModel? sm = null;
                    EventSourceClass? eventSourceClass = null;
                    foreach (var classDef in group)
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            // be nice and stop if we're asked to
                            return results?.ToArray() ?? Array.Empty<EventSourceClass>();
                        }

                        bool autoGenerate = false;
                        foreach (var cal in classDef.AttributeLists)
                        {
                            foreach (var ca in cal.Attributes)
                            {
                                // need a semantic model for this tree
                                sm ??= _compilation.GetSemanticModel(classDef.SyntaxTree);

                                if (sm.GetSymbolInfo(ca, _cancellationToken).Symbol is not IMethodSymbol caSymbol)
                                {
                                    // badly formed attribute definition, or not the right attribute
                                    continue;
                                }

                                if (autogenerateAttribute.Equals(caSymbol.ContainingType, SymbolEqualityComparer.Default))
                                {
                                    autoGenerate = true;
                                    continue;
                                }
                                if (eventSourceAttribute.Equals(caSymbol.ContainingType, SymbolEqualityComparer.Default))
                                {
                                    string nspace = string.Empty;
                                    var ns = classDef.Parent as NamespaceDeclarationSyntax;
                                    if (ns is null)
                                    {
                                        if (classDef.Parent is not CompilationUnitSyntax)
                                        {
                                            // since this generator doesn't know how to generate a nested type...
                                            //ReportDiagnostic(ErrorNestedType, classDef.Identifier.GetLocation());
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

                                    var className = classDef.Identifier.ToString();
                                    var name = className;
                                    var guid = "";

                                    var args = ca.ArgumentList?.Arguments;
                                    if (args is not null)
                                    {
                                        foreach (var arg in args)
                                        {
                                            var argName = arg.NameEquals!.Name.Identifier.ToString();
                                            var value = sm.GetConstantValue(arg.Expression, _cancellationToken).ToString();

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

                                    eventSourceClass = new EventSourceClass
                                    {
                                        Namespace = nspace,
                                        ClassName = className,
                                        SourceName = name,
                                        Guid = result
                                    };
                                    continue;
                                }
                            }
                        }

                        if (!autoGenerate)
                        {
                            continue;
                        }

                        if (eventSourceClass is null)
                        {
                            continue;
                        }

                        results ??= new List<EventSourceClass>();
                        results.Add(eventSourceClass);
                    }
                }

                return results?.ToArray() ?? Array.Empty<EventSourceClass>();
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

                byte[] combinedbytes = new byte[namespaceBytes.Length + bytes.Length];

                bytes.CopyTo(combinedbytes, namespaceBytes.Length);
                namespaceBytes.CopyTo(combinedbytes);

                using (var sha = SHA1.Create())
                {
                    bytes = sha.ComputeHash(combinedbytes);
                }

                Array.Resize(ref bytes, 16);

                bytes[7] = unchecked((byte)((bytes[7] & 0x0F) | 0x50));    // Set high 4 bits of octet 7 to 5, as per RFC 4122
                return new Guid(bytes);
            }
        }
    }
}
