// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
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
            private List<string> _debugStrings;

            public List<string> GetDebugStrings() => _debugStrings;

            public Parser(Compilation compilation, Action<Diagnostic> reportDiagnostic, CancellationToken cancellationToken)
            {
                _compilation = compilation;
                _cancellationToken = cancellationToken;
                _reportDiagnostic = reportDiagnostic;
                _debugStrings = new List<string>();
            }

            public ITypeSymbol? GetStringTypeSymbol()
            {
                INamedTypeSymbol? stringTypeSymbol = _compilation.GetTypeByMetadataName("System.String");
                return stringTypeSymbol;
            }

            public EventSourceClass[] GetEventSourceClasses(List<ClassDeclarationSyntax> classDeclarations, Dictionary<ClassDeclarationSyntax, List<MethodDeclarationSyntax>> methodDeclarations)
            {
                INamedTypeSymbol? autogenerateAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSourceAutoGenerateAttribute");
                if (autogenerateAttribute is null)
                {
                    // No EventSourceAutoGenerateAttribute
                    return Array.Empty<EventSourceClass>();
                }

                INamedTypeSymbol? eventSourceAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventSourceAttribute");
                if (eventSourceAttribute is null)
                {
                    // No EventSourceAttribute
                    return Array.Empty<EventSourceClass>();
                }

                INamedTypeSymbol? eventMethodAttribute = _compilation.GetTypeByMetadataName("System.Diagnostics.Tracing.EventAttribute");

                List<EventSourceClass>? results = null;
                // we enumerate by syntax tree, to minimize the need to instantiate semantic models (since they're expensive)
                foreach (IGrouping<SyntaxTree, ClassDeclarationSyntax>? group in classDeclarations.GroupBy(x => x.SyntaxTree))
                {
                    SemanticModel? sm = null;
                    EventSourceClass? eventSourceClass = null;
                    foreach (ClassDeclarationSyntax? classDef in group)
                    {
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            // be nice and stop if we're asked to
                            return results?.ToArray() ?? Array.Empty<EventSourceClass>();
                        }

                        bool autoGenerate = false;

                        foreach (AttributeListSyntax? cal in classDef.AttributeLists)
                        {
                            foreach (AttributeSyntax? ca in cal.Attributes)
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
                                        foreach (AttributeArgumentSyntax? arg in args)
                                        {
                                            string? argName = arg.NameEquals!.Name.Identifier.ToString();
                                            string? value = sm.GetConstantValue(arg.Expression, _cancellationToken).ToString();

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
                                    List<string> debugStrings = new List<string>();
                                    Dictionary<string, Dictionary<string, int>> maps = new Dictionary<string, Dictionary<string,int>>();
                                    List<EventSourceEvent> events = GetMethodMetadataToken(methodDeclarations[classDef], eventMethodAttribute, sm, debugStrings, maps);

                                    eventSourceClass = new EventSourceClass
                                    {
                                        Namespace = nspace,
                                        ClassName = className,
                                        SourceName = name,
                                        Guid = result,
                                        Events = events,
                                        DebugStrings = debugStrings,
                                        Maps = maps
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

                        // By now we have an eventSourceClass definition.
                        // Parse class definition for any subclass definitions for EventSource (specifically Keyword and Task class)
                        foreach (SyntaxNode child in classDef.ChildNodes())
                        {
                            if (child is ClassDeclarationSyntax classChild)
                            {
                                string classChildIdentifier = classChild.Identifier.ToString();
                                
                                // check if we care about this child class def
                                // we only care about "Tasks" and "Keywords" classes.
                                if (classChildIdentifier == "Tasks")
                                {
                                    eventSourceClass.TaskMap = GetTasksMap(classChild);
                                }
                                else if (classChildIdentifier == "Keywords")
                                {
                                    eventSourceClass.KeywordMap = GetKeywordsMap(classChild);
                                }
                                else
                                {
                                    // don't care about these
                                    continue;
                                }
                            }
                        }

                        results ??= new List<EventSourceClass>();
                        results.Add(eventSourceClass);
                    }
                }

                return results?.ToArray() ?? Array.Empty<EventSourceClass>();
            }

            private List<EventSourceEvent> GetMethodMetadataToken(List<MethodDeclarationSyntax> methods, INamedTypeSymbol? eventMethodAttribute, SemanticModel sm, List<string> debugStrings, Dictionary<string, Dictionary<string, int>> maps)
            {
                List<EventSourceEvent> metadataTokens = new List<EventSourceEvent>();

                if (eventMethodAttribute is null)
                {
                    return metadataTokens;
                }

                foreach (MethodDeclarationSyntax method in methods)
                {
                    AttributeSyntax? eventAttribute = null;
                    string eventName;
                    string eventId = string.Empty;
                    string eventLevel = "4"; // default is Informational
                    string eventKeywords = "";
                    string opcode;
                    string task;
                    string version = "0";
                    List<EventParameter>? parameters = null;

                    foreach (AttributeListSyntax? mal in method.AttributeLists)
                    {
                        foreach (AttributeSyntax? ma in mal.Attributes)
                        {
                            if (sm.GetSymbolInfo(ma, _cancellationToken).Symbol is not IMethodSymbol maSymbol)
                            {
                                // badly formed attribute definition, or not the right attribute
                                continue;
                            }
                            if (eventMethodAttribute.Equals(maSymbol.ContainingType, SymbolEqualityComparer.Default))
                            {
                                eventAttribute = ma;
                                eventName = method.Identifier.ToString();
                                debugStrings.Add(eventName);
                                if (eventAttribute.ArgumentList is not null)
                                {
                                    SeparatedSyntaxList<AttributeArgumentSyntax>? args = eventAttribute.ArgumentList?.Arguments;
                                    if (args is not null)
                                    {
                                        foreach (AttributeArgumentSyntax? attribArg in args)
                                        {
                                            if (attribArg is null)
                                            {
                                                continue;
                                            }

                                            if (attribArg.NameEquals is null)
                                            {
                                                string? value = sm.GetConstantValue(attribArg.Expression, _cancellationToken).ToString();
                                                eventId = value;
                                            }
                                            else
                                            {
                                                string? argName = attribArg.NameEquals!.Name.Identifier.ToString();
                                                string? value = sm.GetConstantValue(attribArg.Expression, _cancellationToken).ToString();
                                                debugStrings.Add($"{argName} - {value}");
                                                switch (argName)
                                                {
                                                    case "Name":
                                                        eventName = value;
                                                        break;
                                                    case "Level":
                                                        eventLevel = value;
                                                        break;
                                                    case "Keywords":
                                                        eventKeywords = value;
                                                        break;
                                                    case "Opcode":
                                                        opcode = value;
                                                        break;
                                                    case "Task":
                                                        task = value;
                                                        break;
                                                    case "Version":
                                                        version = value;
                                                        break;
                                                    default:
                                                        break;
                                                }
                                            }
                                        }
                                    }
                                }
                                foreach (ParameterSyntax param in method.ParameterList.Parameters)
                                {
                                    parameters ??= new List<EventParameter>();
                                    debugStrings.Add(param.Identifier.ToString());
                                    ISymbol? paramSymbol = sm.GetDeclaredSymbol(param);
                                    if (paramSymbol is IParameterSymbol yes)
                                    {
                                        // For enum, we need to create a mapping for the enum class type.
                                        if (yes.Type.TypeKind == TypeKind.Enum)
                                        {
                                            debugStrings.Add("IsEnum");
                                            INamedTypeSymbol? underlyingEnumType = ((INamedTypeSymbol)(yes.Type)).EnumUnderlyingType;
                                            if (underlyingEnumType != null)
                                            {
                                                debugStrings.Add(underlyingEnumType.ToDisplayString());
                                            }
                                            else
                                            {
                                                debugStrings.Add("underlyingEnumType is null");
                                            }

                                            debugStrings.Add($"Name type: {yes.Type.Name}");
                                            int recordedFieldCount = 0;
                                            foreach (ISymbol symbol in yes.Type.GetMembers())
                                            {
                                                if (symbol.Kind != SymbolKind.Field)
                                                {
                                                    continue;
                                                }
                                                
                                                if (!maps.ContainsKey(yes.Type.Name))
                                                {
                                                    maps.Add(yes.Type.Name, new Dictionary<string, int>());
                                                    maps[yes.Type.Name].Add(symbol.Name, recordedFieldCount);
                                                    recordedFieldCount++;
                                                }
                                                else if (!maps[yes.Type.Name].ContainsKey(symbol.Name))
                                                {
                                                    maps[yes.Type.Name].Add(symbol.Name, recordedFieldCount);
                                                    recordedFieldCount++;
                                                }
                                                debugStrings.Add(symbol.ToDisplayString());
                                            }
                                        }
                                        else
                                        {
                                            debugStrings.Add("IsNotEnum");
                                        }


                                        debugStrings.Add(yes.Type.ToDisplayString());

                                            
                                        parameters.Add(new EventParameter {
                                            Name = param.Identifier.ToString(),
                                            TypeString = param.Type!.ToFullString(),
                                            Type = yes.Type
                                        });
                                    }
                                    else
                                    {
                                        debugStrings.Add("paramSymbol null");
                                    }


                                }

                                metadataTokens.Add(new EventSourceEvent {
                                    Name = eventName,
                                    Id = eventId,
                                    Keywords = eventKeywords,
                                    Level = eventLevel,
                                    Parameters = parameters,
                                    Version = version
                                });
                            }
                        }
                    }
                }
                return metadataTokens;
            }

            private Dictionary<ulong, string> GetKeywordsMap(ClassDeclarationSyntax classDef)
            {
                Dictionary<ulong, string> map = new Dictionary<ulong, string>();

                // grab the semantic model for the child class
                SemanticModel smm = _compilation.GetSemanticModel(classDef.SyntaxTree);

                foreach(SyntaxNode node in classDef.ChildNodes())
                {
                    // go over all field decl, and grab the variable names and values
                    if (node is FieldDeclarationSyntax fieldSyntax)
                    {
                        foreach (var variable in fieldSyntax.Declaration.Variables)
                        {
                            ISymbol? fieldSymbol = smm.GetDeclaredSymbol(variable);

                            // Do stuff with the symbol here
                            if (fieldSymbol is null && variable.Initializer is null)
                            {
                                continue;
                            }

                            string? valStr = smm.GetConstantValue(variable.Initializer!.Value).ToString();
                            ulong value = UInt64.Parse(valStr);

                            map.Add(value, fieldSymbol!.Name);
                        }
                    }
                }

                return map;
            }

            private Dictionary<int, string> GetTasksMap(ClassDeclarationSyntax classDef)
            {
                Dictionary<int, string> map = new Dictionary<int, string>();

                // grab the semantic model for the child class
                SemanticModel smm = _compilation.GetSemanticModel(classDef.SyntaxTree);

                foreach(SyntaxNode node in classDef.ChildNodes())
                {
                    // go over all field decl, and grab the variable names and values
                    if (node is FieldDeclarationSyntax fieldSyntax)
                    {
                        foreach (var variable in fieldSyntax.Declaration.Variables)
                        {
                            ISymbol? fieldSymbol = smm.GetDeclaredSymbol(variable);

                            // Do stuff with the symbol here
                            if (fieldSymbol is null && variable.Initializer is null)
                            {
                                continue;
                            }

                            string? valStr = smm.GetConstantValue(variable.Initializer!.Value).ToString();
                            int value = Int32.Parse(valStr);
                            map.Add(value, fieldSymbol!.Name);
                        }
                    }
                }
                return map;
            }

            public string GetFullMetadataName(ISymbol s) 
            {
                if (s == null || IsRootNamespace(s))
                {
                    return string.Empty;
                }

                var sb = new StringBuilder(s.MetadataName);
                var last = s;

                s = s.ContainingSymbol;

                while (!IsRootNamespace(s))
                {
                    if (s is ITypeSymbol && last is ITypeSymbol)
                    {
                        sb.Insert(0, '+');
                    }
                    else
                    {
                        sb.Insert(0, '.');
                    }

                    sb.Insert(0, s.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                    //sb.Insert(0, s.MetadataName);
                    s = s.ContainingSymbol;
                }

                return sb.ToString();
            }

            private bool IsRootNamespace(ISymbol symbol) 
            {
                INamespaceSymbol s = null;
                return ((s = symbol as INamespaceSymbol) != null) && s.IsGlobalNamespace;
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

                using (SHA1 sha = SHA1.Create())
                {
                    bytes = sha.ComputeHash(combinedBytes);
                }

                Array.Resize(ref bytes, 16);

                bytes[7] = unchecked((byte)((bytes[7] & 0x0F) | 0x50));    // Set high 4 bits of octet 7 to 5, as per RFC 4122
                return new Guid(bytes);
            }
        }
    }
}
