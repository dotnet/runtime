// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public partial class LoggingGenerator
    {
        private const string DiagnosticCategory = "LoggingGenerator";

#pragma warning disable RS2008 // Enable analyzer release tracking

        private static readonly DiagnosticDescriptor ErrorInvalidMethodName = new(
            id: "LG0",
            title: "Logging method names cannot start with __",
            messageFormat: "Logging method names cannot start with __",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorInvalidMessage = new(
            id: "LG1",
            title: "Missing message for logging method",
            messageFormat: "Missing message for logging method {0}",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorInvalidParameterName = new(
            id: "LG2",
            title: "Logging method parameter names cannot start with __",
            messageFormat: "Logging method parameter names cannot start with __",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorInvalidTypeName = new(
            id: "LG3",
            title: "Unable to automatically derive generated type name",
            messageFormat: "Unable to automatically derive a generated type name based on the interface name of {0}, please specify an explicit generated type name instead",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorNestedType = new(
            id: "LG4",
            title: "Logging interfaces cannot be in nested types",
            messageFormat: "Logging interfaces cannot be in nested types",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorMissingRequiredType = new(
            id: "LG5",
            title: "Could not find a required type definition",
            messageFormat: "Could not find definition for type {0}",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorEventIdReuse = new(
            id: "LG6",
            title: "Multiple logging messages cannot use the same event id",
            messageFormat: "Multiple logging messages are using event id {0}",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorInvalidMethodReturnType = new(
            id: "LG7",
            title: "Logging methods must return void",
            messageFormat: "Logging methods must return void",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorInterfaceGeneric = new(
            id: "LG8",
            title: "Logging interfaces cannot be generic",
            messageFormat: "Logging interfaces cannot be generic",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private static readonly DiagnosticDescriptor ErrorMethodGeneric = new(
            id: "LG9",
            title: "Logging methods cannot be generic",
            messageFormat: "Logging methods cannot be generic",
            category: DiagnosticCategory,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        /// <summary>
        /// Gets the set of logging classes that should be generated based on the discovered annotated interfaces.
        /// </summary>
        private static IEnumerable<LoggerClass> GetLogClasses(GeneratorExecutionContext context, Compilation compilation)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
                // nothing to do yet
                yield break;
            }

            var logExtensionsAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerExtensionsAttribute");
            if (logExtensionsAttribute is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorMissingRequiredType, null, "Microsoft.Extensions.Logging.LoggerExtensionsAttribute"));
                yield break;
            }

            var loggerMessageAttribute = compilation.GetTypeByMetadataName("Microsoft.Extensions.Logging.LoggerMessageAttribute");
            if (loggerMessageAttribute is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorMissingRequiredType, null, "Microsoft.Extensions.Logging.LoggerMessageAttribute"));
                yield break;
            }

            var exSymbol = compilation.GetTypeByMetadataName("System.Exception");
            if (exSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorMissingRequiredType, null, "System.Exception"));
                yield break;
            }

            var voidSymbol = compilation.GetTypeByMetadataName("System.Void");
            if (voidSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(ErrorMissingRequiredType, null, "System.Void"));
                yield break;
            }

            var semanticModelMap = new Dictionary<SyntaxTree, SemanticModel>();

            foreach (var iface in receiver.InterfaceDeclarations)
            {
                foreach (var al in iface.AttributeLists)
                {
                    foreach (var a in al.Attributes)
                    {
                        // does this interface have the [LoggerExtensions] atribute?
                        var semanticModel = GetSemanticModel(compilation, semanticModelMap, a.SyntaxTree);
                        var aSymbol = semanticModel.GetSymbolInfo(a, context.CancellationToken);
                        if (aSymbol.Symbol is IMethodSymbol methodSymbol && logExtensionsAttribute.Equals(methodSymbol.ContainingType, SymbolEqualityComparer.Default))
                        {
                            // determine the namespace the interface is declared in, if any
                            NamespaceDeclarationSyntax? ns = null;
                            if (iface.Parent != null)
                            {
                                ns = iface.Parent as NamespaceDeclarationSyntax;
                                if (ns == null && iface.Parent is not CompilationUnitSyntax)
                                {
                                    // since this generator doesn't know how to generate a nested type...
                                    context.ReportDiagnostic(Diagnostic.Create(ErrorNestedType, iface.Identifier.GetLocation()));
                                }
                            }

                            // determine the name of the generated type

                            string? name = null;
                            if (a.ArgumentList?.Arguments.Count > 0)
                            {
                                var arg = a.ArgumentList!.Arguments[0];
                                name = semanticModel.GetConstantValue(arg.Expression).ToString();
                            }

                            // if no name was specified, try to derive it from the interface name
                            var ifaceName = iface.Identifier.ToString();
                            if (name == null)
                            {
                                if (ifaceName[0] == 'I')
                                {
                                    // strip the leading I
                                    name = ifaceName.Substring(1);
                                }
                                else
                                {
                                    // fail
                                    name = string.Empty;
                                }
                            }

                            var lc = new LoggerClass
                            {
                                Namespace = ns?.Name.ToString(),
                                Name = name,
                                OriginalInterfaceName = iface.Identifier.ToString(),
                                AccessModifiers = iface.Modifiers.ToString(),
                                Documentation = GetDocs(iface),
                            };

                            if (string.IsNullOrWhiteSpace(lc.Name))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidTypeName, a.GetLocation(), ifaceName));
                            }

                            if (iface.Arity > 0)
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ErrorInterfaceGeneric, iface.GetLocation()));
                            }

                            var ids = new HashSet<string>();
                            foreach (var method in iface.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>())
                            {
                                foreach (var mal in method.AttributeLists)
                                {
                                    foreach (var ma in mal.Attributes)
                                    {
                                        semanticModel = GetSemanticModel(compilation, semanticModelMap, ma.SyntaxTree);
                                        var maSymbol = semanticModel.GetSymbolInfo(ma, context.CancellationToken);
                                        if (maSymbol.Symbol is IMethodSymbol ms && loggerMessageAttribute.Equals(ms.ContainingType, SymbolEqualityComparer.Default))
                                        {
                                            var arg = ma.ArgumentList!.Arguments[0];
                                            var eventId = semanticModel.GetConstantValue(arg.Expression).ToString();

                                            arg = ma.ArgumentList!.Arguments[1];
                                            var level = semanticModel.GetConstantValue(arg.Expression).ToString();

                                            arg = ma.ArgumentList!.Arguments[2];
                                            var message = semanticModel.GetConstantValue(arg.Expression).ToString();

                                            var lm = new LoggerMethod
                                            {
                                                Name = method.Identifier.ToString(),
                                                EventId = eventId,
                                                Level = level,
                                                Message = message,
                                                MessageHasTemplates = HasTemplates(message),
                                                Documentation = GetDocs(method),
                                            };
                                            lc.Methods.Add(lm);

                                            if (lm.Name.StartsWith("__", StringComparison.Ordinal))
                                            {
                                                // can't have logging method names that start with __ since that can lead to conflicting symbol names
                                                // because the generated symbols start with __
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMethodName, method.Identifier.GetLocation()));
                                            }

                                            if (!GetSemanticModel(compilation, semanticModelMap, method.ReturnType.SyntaxTree).GetTypeInfo(method.ReturnType!).Type!.Equals(voidSymbol, SymbolEqualityComparer.Default))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMethodReturnType, method.ReturnType.GetLocation()));
                                            }

                                            if (method.Arity > 0)
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorMethodGeneric, method.GetLocation()));
                                            }

                                            // ensure there are no duplicate ids.
                                            if (ids.Contains(lm.EventId))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorEventIdReuse, ma.ArgumentList!.Arguments[0].GetLocation(), lm.EventId));
                                            }
                                            else
                                            {
                                                ids.Add(lm.EventId);
                                            }

                                            if (string.IsNullOrWhiteSpace(lm.Message))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMessage, ma.GetLocation(), method.Identifier.ToString()));
                                            }

                                            foreach (var p in method.ParameterList.Parameters)
                                            {
                                                var pSymbol = GetSemanticModel(compilation, semanticModelMap, p.SyntaxTree).GetTypeInfo(p.Type!).Type!;

                                                var lp = new LoggerParameter
                                                {
                                                    Name = p.Identifier.ToString(),
                                                    Type = p.Type!.ToString(),
                                                    IsExceptionType = IsBaseOrIdentity(compilation, pSymbol, exSymbol),
                                                };
                                                lm.Parameters.Add(lp);

                                                if (lp.Name.StartsWith("__", StringComparison.Ordinal))
                                                {
                                                    // can't have logging method parameter names that start with  __ since that can lead to conflicting symbol names
                                                    // because all generated symbols start with __
                                                    context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidParameterName, p.Identifier.GetLocation()));
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            yield return lc;
                        }
                    }
                }
            }
        }

        static string GetDocs(SyntaxNode _)
        {
#if false
This is not ready yet
            foreach (var trivia in node.GetLeadingTrivia().Where(x => x.HasStructure))
            {
                if (trivia.GetStructure() is DocumentationCommentTriviaSyntax sTrivia)
                {
                    return sTrivia.ToString();
                }
            }
#endif
            return string.Empty;
        }

        // Workaround for https://github.com/dotnet/roslyn/pull/49330
        static SemanticModel GetSemanticModel(Compilation compilation, Dictionary<SyntaxTree, SemanticModel> semanticModelMap, SyntaxTree syntaxTree)
        {
            if (!semanticModelMap.TryGetValue(syntaxTree, out var semanticModel))
            {
                semanticModel = compilation.GetSemanticModel(syntaxTree);
                semanticModelMap[syntaxTree] = semanticModel;
            }

            return semanticModel;
        }

        static bool IsBaseOrIdentity(Compilation compilation, ITypeSymbol source, ITypeSymbol dest)
        {
            var conversion = compilation.ClassifyConversion(source, dest);
            return conversion.IsIdentity || (conversion.IsReference && conversion.IsImplicit);
        }
        
        private static bool HasTemplates(string message)
        {
            for (int i = 0; i < message.Length; i++)
            {
                var ch = message[i];
                if (ch == '{')
                {
                    if (i < message.Length - 1 && message[i + 1] != '{')
                    {
                        // look for a non-escaped }
                        i++;
                        for (; i < message.Length; i++)
                        {
                            ch = message[i];
                            if (ch == '}')
                            {
                                if (i == message.Length - 1 || message[i + 1] != '}')
                                {
                                    return true;
                                }
                            }
                        }

                        return false;
                    }
                }
            }

            return false;
        }

#pragma warning disable SA1401 // Fields should be private

        // An logging class holding a bunch of log methods
        private class LoggerClass
        {
            public string? Namespace;
            public string Name = string.Empty;
            public string OriginalInterfaceName = string.Empty;
            public string AccessModifiers = string.Empty;
            public List<LoggerMethod> Methods = new();
            public string Documentation = string.Empty;
        }

        // A log method in a logging class
        private class LoggerMethod
        {
            public string Name = string.Empty;
            public string Message = string.Empty;
            public string Level = string.Empty;
            public string EventId = string.Empty;
            public List<LoggerParameter> Parameters = new();
            public bool MessageHasTemplates;
            public string Documentation = string.Empty;
        }

        // A single parameter to a log method
        private class LoggerParameter
        {
            public string Name = string.Empty;
            public string Type = string.Empty;
            public bool IsExceptionType;
        }
    }
}
