// © Microsoft Corporation. All rights reserved.

namespace Microsoft.Extensions.Logging.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
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
            messageFormat: "Missing message for logging method",
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
            title: "Missing generated type name",
            messageFormat: "Missing generated type name",
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

        /// <summary>
        /// Gets the known set of annotated logger classes
        /// </summary>
        private static IEnumerable<LoggerClass> GetLogClasses(GeneratorExecutionContext context, Compilation compilation)
        {
            if (!(context.SyntaxReceiver is SyntaxReceiver receiver))
            {
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

            // Temp work around for https://github.com/dotnet/roslyn/pull/49330
            var semanticModelMap = new Dictionary<SyntaxTree, SemanticModel>();

            foreach (var iface in receiver.InterfaceDeclarations)
            {
                foreach (var al in iface.AttributeLists)
                {
                    foreach (var a in al.Attributes)
                    {
                        if (!semanticModelMap.TryGetValue(a.SyntaxTree, out var semanticModel))
                        {
                            semanticModel = compilation.GetSemanticModel(a.SyntaxTree);
                            semanticModelMap[a.SyntaxTree] = semanticModel;
                        }

                        // does this interface have the [LoggerExtensions] atribute?
                        var aSymbol = semanticModel.GetSymbolInfo(a, context.CancellationToken);
                        if (aSymbol.Symbol is IMethodSymbol methodSymbol && logExtensionsAttribute.Equals(methodSymbol.ContainingType, SymbolEqualityComparer.Default))
                        {
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

                            string? name = null;
                            if (a.ArgumentList?.Arguments.Count > 0)
                            {
                                var arg = a.ArgumentList!.Arguments[0];
                                name = compilation.GetSemanticModel(a.SyntaxTree).GetConstantValue(arg.Expression).ToString();
                            }

                            if (name == null)
                            {
                                var ifaceName = iface.Identifier.ToString();
                                if (ifaceName[0] == 'I' && ifaceName.Length > 1)
                                {
                                    name = ifaceName.Substring(1);
                                }
                                else
                                {
                                    name = ifaceName + "Extensions";
                                }
                            }

                            var lc = new LoggerClass
                            {
                                Namespace = ns?.Name.ToString(),
                                Name = name,
                                OriginalInterfaceName = iface.Identifier.ToString(),
                                AccessModifiers = iface.Modifiers.ToString(),
                            };

                            if (string.IsNullOrWhiteSpace(lc.Name))
                            {
                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidTypeName, a.GetLocation()));
                            }

                            foreach (var method in iface.Members.Where(m => m.IsKind(SyntaxKind.MethodDeclaration)).OfType<MethodDeclarationSyntax>())
                            {
                                foreach (var mal in method.AttributeLists)
                                {
                                    foreach (var ma in mal.Attributes)
                                    {
                                        if (!semanticModelMap.TryGetValue(ma.SyntaxTree, out semanticModel))
                                        {
                                            semanticModel = compilation.GetSemanticModel(ma.SyntaxTree);
                                            semanticModelMap[ma.SyntaxTree] = semanticModel;
                                        }

                                        var maSymbol = semanticModel.GetSymbolInfo(ma, context.CancellationToken);
                                        if (maSymbol.Symbol is IMethodSymbol ms && loggerMessageAttribute.Equals(ms.ContainingType, SymbolEqualityComparer.Default))
                                        {
                                            var arg = ma.ArgumentList!.Arguments[0];
                                            var eventId = compilation.GetSemanticModel(ma.SyntaxTree).GetConstantValue(arg.Expression).ToString();

                                            arg = ma.ArgumentList!.Arguments[1];
                                            var level = compilation.GetSemanticModel(ma.SyntaxTree).GetConstantValue(arg.Expression).ToString();

                                            arg = ma.ArgumentList!.Arguments[2];
                                            var message = compilation.GetSemanticModel(ma.SyntaxTree).GetConstantValue(arg.Expression).ToString();

                                            var lm = new LoggerMethod
                                            {
                                                Name = method.Identifier.ToString(),
                                                EventId = eventId,
                                                Level = level,
                                                Message = message,
                                                MessageHasTemplates = HasTemplates(message),
                                            };
                                            lc.Methods.Add(lm);

                                            if (lm.Name.StartsWith("__", StringComparison.Ordinal))
                                            {
                                                // can't have logging method names that start with __ since that can lead to conflicting symbol names
                                                // because the generated symbols start with __
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMethodName, method.Identifier.GetLocation()));
                                            }

                                            if (!compilation.GetSemanticModel(method.ReturnType.SyntaxTree).GetTypeInfo(method.ReturnType!).Type!.Equals(voidSymbol, SymbolEqualityComparer.Default))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMethodReturnType, method.ReturnType.GetLocation()));
                                            }

                                            foreach (var m in lc.Methods)
                                            {
                                                if (m != lm && m.EventId == lm.EventId)
                                                {
                                                    context.ReportDiagnostic(Diagnostic.Create(ErrorEventIdReuse, ma.ArgumentList!.Arguments[0].GetLocation(), m.EventId));
                                                }
                                            }

                                            if (string.IsNullOrWhiteSpace(lm.Message))
                                            {
                                                context.ReportDiagnostic(Diagnostic.Create(ErrorInvalidMessage, ma.GetLocation()));
                                            }

                                            foreach (var p in method.ParameterList.Parameters)
                                            {
                                                var pSymbol = compilation.GetSemanticModel(p.SyntaxTree).GetTypeInfo(p.Type!).Type!;

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
