// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

[assembly: System.Resources.NeutralResourcesLanguage("en-us")]

namespace System.Text.RegularExpressions.Generator
{
    /// <summary>Generates C# source code to implement regular expressions.</summary>
    [Generator]
    public partial class RegexGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    static (s, _) => IsSyntaxTargetForGeneration(s),
                    static (ctx, _) => GetSemanticTargetForGeneration(ctx))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses =
                context.CompilationProvider.Combine(classDeclarations.Collect());

            context.RegisterImplementationSourceOutput(compilationAndClasses, static (context, source) =>
            {
                ImmutableArray<ClassDeclarationSyntax> classes = source.Item2;
                if (classes.IsDefaultOrEmpty)
                {
                    return;
                }

                string result = "";
                try
                {
                    Compilation compilation = source.Item1;
                    IReadOnlyList<RegexClass> regexClasses = GetRegexClassesToEmit(compilation, context.ReportDiagnostic, classes.Distinct(), context.CancellationToken);
                    if (regexClasses.Count != 0)
                    {
                        result = Emit(regexClasses, context.CancellationToken);
                    }
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    result = "// ERROR:" + Environment.NewLine + string.Join(Environment.NewLine,
                        e.ToString().Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Select(s => $"// {SymbolDisplay.FormatLiteral(s, quote: true)}"));
                }

                if (result.Length > 0)
                {
                    context.AddSource("RegexGenerator.g.cs", result);
                }
            });
        }
    }
}
