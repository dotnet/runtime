// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace ILCompiler.ReadyToRun.Tests.TestCasesRunner;

/// <summary>
/// Compiles C# source code into assemblies using Roslyn at test time.
/// </summary>
internal sealed class R2RTestCaseCompiler
{
    private readonly List<MetadataReference> _frameworkReferences;

    public R2RTestCaseCompiler(TestPaths paths)
    {
        _frameworkReferences = new List<MetadataReference>();

        // Add reference assemblies from the ref pack (needed for Roslyn compilation)
        string refPackDir = paths.RefPackDir;
        if (Directory.Exists(refPackDir))
        {
            foreach (string refPath in Directory.EnumerateFiles(refPackDir, "*.dll"))
            {
                _frameworkReferences.Add(MetadataReference.CreateFromFile(refPath));
            }
        }
        else
        {
            // Fallback to runtime pack implementation assemblies
            foreach (string refPath in paths.GetFrameworkReferencePaths())
            {
                _frameworkReferences.Add(MetadataReference.CreateFromFile(refPath));
            }
        }
    }

    /// <summary>
    /// Compiles a single assembly from source files.
    /// </summary>
    /// <param name="assemblyName">Name of the output assembly (without .dll extension).</param>
    /// <param name="sources">C# source code strings.</param>
    /// <param name="additionalReferences">Paths to additional assembly references.</param>
    /// <param name="outputKind">Library or ConsoleApplication.</param>
    /// <param name="additionalDefines">Additional preprocessor defines.</param>
    /// <param name="features">Roslyn feature flags (e.g. "runtime-async=on").</param>
    /// <returns>Path to the compiled assembly.</returns>
    public string CompileAssembly(
        string assemblyName,
        IEnumerable<string> sources,
        string outputPath,
        IEnumerable<string>? additionalReferences = null,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        IEnumerable<string>? additionalDefines = null,
        IEnumerable<KeyValuePair<string, string>>? features = null)
    {
        var parseOptions = new CSharpParseOptions(
            LanguageVersion.Latest,
            preprocessorSymbols: additionalDefines);

        if (features is not null)
            parseOptions = parseOptions.WithFeatures(features);

        var syntaxTrees = sources.Select(src =>
            CSharpSyntaxTree.ParseText(src, parseOptions));

        var references = new List<MetadataReference>(_frameworkReferences);
        if (additionalReferences is not null)
        {
            foreach (string refPath in additionalReferences)
            {
                references.Add(MetadataReference.CreateFromFile(refPath));
            }
        }

        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(outputKind)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithAllowUnsafe(true)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        EmitResult result = compilation.Emit(outputPath);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
            throw new InvalidOperationException(
                $"Compilation of '{assemblyName}' failed:\n{string.Join("\n", errors)}");
        }

        return outputPath;
    }

    /// <summary>
    /// Reads an embedded resource from the test assembly.
    /// </summary>
    public static string ReadEmbeddedSource(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            // Try with different path separator
            string altName = resourceName.Replace('/', '\\');
            using Stream? altStream = assembly.GetManifestResourceStream(altName);
            if (altStream is null)
                throw new FileNotFoundException($"Embedded resource not found: '{resourceName}'. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

            using var altReader = new StreamReader(altStream);
            return altReader.ReadToEnd();
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
