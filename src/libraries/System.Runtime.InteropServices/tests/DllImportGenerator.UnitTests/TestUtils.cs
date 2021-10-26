// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.DotNet.XUnitExtensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DllImportGenerator.UnitTests
{
    internal static class TestUtils
    {
        /// <summary>
        /// Assert the pre-srouce generator compilation has only
        /// the expected failure diagnostics.
        /// </summary>
        /// <param name="comp"></param>
        public static void AssertPreSourceGeneratorCompilation(Compilation comp)
        {
            var allowedDiagnostics = new HashSet<string>()
            {
                "CS8795", // Partial method impl missing
                "CS0234", // Missing type or namespace - GeneratedDllImportAttribute
                "CS0246", // Missing type or namespace - GeneratedDllImportAttribute
                "CS8019", // Unnecessary using
            };
            var compDiags = comp.GetDiagnostics();
            Assert.All(compDiags, diag =>
            {
                Assert.Subset(allowedDiagnostics, new HashSet<string> { diag.Id });
            });
        }

        /// <summary>
        /// Create a compilation given source
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static Task<Compilation> CreateCompilation(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true, IEnumerable<string>? preprocessorSymbols = null)
        {
            return CreateCompilation(new[] { source }, outputKind, allowUnsafe, preprocessorSymbols);
        }

        /// <summary>
        /// Create a compilation given sources
        /// </summary>
        /// <param name="sources">Sources to compile</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static Task<Compilation> CreateCompilation(string[] sources, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true, IEnumerable<string>? preprocessorSymbols = null)
        {
            return CreateCompilation(
                sources.Select(source =>
                    CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: preprocessorSymbols))).ToArray(),
                outputKind,
                allowUnsafe,
                preprocessorSymbols);
        }

        /// <summary>
        /// Create a compilation given sources
        /// </summary>
        /// <param name="sources">Sources to compile</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static async Task<Compilation> CreateCompilation(SyntaxTree[] sources, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true, IEnumerable<string>? preprocessorSymbols = null)
        {
            var (mdRefs, ancillary) = GetReferenceAssemblies();

            return CSharpCompilation.Create("compilation",
                sources,
                (await ResolveReferenceAssemblies(mdRefs)).Add(ancillary),
                new CSharpCompilationOptions(outputKind, allowUnsafe: allowUnsafe));
        }

        /// <summary>
        /// Create a compilation given source and reference assemblies
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="referenceAssemblies">Reference assemblies to include</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static Task<Compilation> CreateCompilationWithReferenceAssemblies(string source, ReferenceAssemblies referenceAssemblies, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true)
        {
            return CreateCompilationWithReferenceAssemblies(new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) }, referenceAssemblies, outputKind, allowUnsafe);
        }

        /// <summary>
        /// Create a compilation given source and reference assemblies
        /// </summary>
        /// <param name="source">Source to compile</param>
        /// <param name="referenceAssemblies">Reference assemblies to include</param>
        /// <param name="outputKind">Output type</param>
        /// <param name="allowUnsafe">Whether or not use of the unsafe keyword should be allowed</param>
        /// <returns>The resulting compilation</returns>
        public static async Task<Compilation> CreateCompilationWithReferenceAssemblies(SyntaxTree[] sources, ReferenceAssemblies referenceAssemblies, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, bool allowUnsafe = true)
        {
            return CSharpCompilation.Create("compilation",
                sources,
                await ResolveReferenceAssemblies(referenceAssemblies),
                new CSharpCompilationOptions(outputKind, allowUnsafe: allowUnsafe));
        }

        public static (ReferenceAssemblies, MetadataReference) GetReferenceAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Net.Net60
                .WithNuGetConfigFilePath(Path.Combine(Path.GetDirectoryName(typeof(TestUtils).Assembly.Location)!, "NuGet.config"));

            // Include the assembly containing the new attribute and all of its references.
            // [TODO] Remove once the attribute has been added to the BCL
            var attrAssem = typeof(GeneratedDllImportAttribute).GetTypeInfo().Assembly;

            return (referenceAssemblies, MetadataReference.CreateFromFile(attrAssem.Location));
        }

        /// <summary>
        /// Run the supplied generators on the compilation.
        /// </summary>
        /// <param name="comp">Compilation target</param>
        /// <param name="diagnostics">Resulting diagnostics</param>
        /// <param name="generators">Source generator instances</param>
        /// <returns>The resulting compilation</returns>
        public static Compilation RunGenerators(Compilation comp, out ImmutableArray<Diagnostic> diagnostics, params IIncrementalGenerator[] generators)
        {
            CreateDriver(comp, null, generators).RunGeneratorsAndUpdateCompilation(comp, out var d, out diagnostics);
            return d;
        }

        /// <summary>
        /// Run the supplied generators on the compilation.
        /// </summary>
        /// <param name="comp">Compilation target</param>
        /// <param name="diagnostics">Resulting diagnostics</param>
        /// <param name="generators">Source generator instances</param>
        /// <returns>The resulting compilation</returns>
        public static Compilation RunGenerators(Compilation comp, AnalyzerConfigOptionsProvider options, out ImmutableArray<Diagnostic> diagnostics, params IIncrementalGenerator[] generators)
        {
            CreateDriver(comp, options, generators).RunGeneratorsAndUpdateCompilation(comp, out var d, out diagnostics);
            return d;
        }

        public static GeneratorDriver CreateDriver(Compilation c, AnalyzerConfigOptionsProvider? options, IIncrementalGenerator[] generators)
            => CSharpGeneratorDriver.Create(
                ImmutableArray.Create(generators.Select(gen => gen.AsSourceGenerator()).ToArray()),
                parseOptions: (CSharpParseOptions)c.SyntaxTrees.First().Options,
                optionsProvider: options);

        // The non-configurable test-packages folder may be incomplete/corrupt.
        // - https://github.com/dotnet/roslyn-sdk/issues/487
        // - https://github.com/dotnet/roslyn-sdk/issues/590
        internal static void ThrowSkipExceptionIfPackagingException(Exception e)
        {
            if (e.GetType().FullName == "NuGet.Packaging.Core.PackagingException")
                throw new SkipTestException($"Skipping test due to issue with test-packages ({e.Message}). See https://github.com/dotnet/roslyn-sdk/issues/590.");
        }

        private static async Task<ImmutableArray<MetadataReference>> ResolveReferenceAssemblies(ReferenceAssemblies referenceAssemblies)
        {
            try
            {
                ResolveRedirect.Instance.Start();
                return await referenceAssemblies.ResolveAsync(LanguageNames.CSharp, CancellationToken.None);
            }
            catch (Exception e)
            {
                ThrowSkipExceptionIfPackagingException(e);
                throw;
            }
            finally
            {
                ResolveRedirect.Instance.Stop();
            }
        }

        private class ResolveRedirect
        {
            private const string EnvVarName = "NUGET_PACKAGES";

            private static readonly ResolveRedirect s_instance = new ResolveRedirect();
            public static ResolveRedirect Instance => s_instance;

            private int _count = 0;

            public void Start()
            {
                // Set the NuGet package cache location to a subdirectory such that we should always have access to it
                Environment.SetEnvironmentVariable(EnvVarName, Path.Combine(Path.GetDirectoryName(typeof(TestUtils).Assembly.Location)!, "packages"));
                Interlocked.Increment(ref _count);
            }

            public void Stop()
            {
                int count = Interlocked.Decrement(ref _count);
                if (count == 0)
                {
                   Environment.SetEnvironmentVariable(EnvVarName, null);
                }
            }
        }
    }
}
