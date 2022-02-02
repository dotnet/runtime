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
    /// <summary>
    /// The target framework to compile against.
    /// </summary>
    /// <remarks>
    /// This enumeration is for testing only and is not to be confused with the product's TargetFramework enum.
    /// </remarks>
    public enum TestTargetFramework
    {
        /// <summary>
        /// The latest supported .NET Framework version.
        /// </summary>
        Framework,
        /// <summary>
        /// The latest supported .NET Core version.
        /// </summary>
        Core,
        /// <summary>
        /// The latest supported .NET Standard version.
        /// </summary>
        Standard,
        /// <summary>
        /// The latest supported (live-built) .NET version.
        /// </summary>
        Net,
        /// <summary>
        /// .NET version 5.0.
        /// </summary>
        Net5,
        /// <summary>
        /// .NET version 6.0.
        /// </summary>
        Net6,
    }

    public static class TestUtils
    {
        /// <summary>
        /// Disable binding redirect warnings. They are disabled by default by the .NET SDK, but not by Roslyn.
        /// See https://github.com/dotnet/roslyn/issues/19640.
        /// </summary>
        internal static ImmutableDictionary<string, ReportDiagnostic> BindingRedirectWarnings { get; } = new Dictionary<string, ReportDiagnostic>()
            {
                { "CS1701", ReportDiagnostic.Suppress },
                { "CS1702", ReportDiagnostic.Suppress },
            }.ToImmutableDictionary();

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
        /// <param name="targetFramework">Target framework of the compilation</param>
        /// <param name="outputKind">Output type</param>
        /// <returns>The resulting compilation</returns>
        public static Task<Compilation> CreateCompilation(string source, TestTargetFramework targetFramework = TestTargetFramework.Net, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, IEnumerable<string>? preprocessorSymbols = null)
        {
            return CreateCompilation(new[] { source }, targetFramework, outputKind, preprocessorSymbols);
        }

        /// <summary>
        /// Create a compilation given sources
        /// </summary>
        /// <param name="sources">Sources to compile</param>
        /// <param name="targetFramework">Target framework of the compilation</param>
        /// <param name="outputKind">Output type</param>
        /// <returns>The resulting compilation</returns>
        public static Task<Compilation> CreateCompilation(string[] sources, TestTargetFramework targetFramework = TestTargetFramework.Net, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary, IEnumerable<string>? preprocessorSymbols = null)
        {
            return CreateCompilation(
                sources.Select(source =>
                    CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: preprocessorSymbols))).ToArray(),
                targetFramework,
                outputKind);
        }

        /// <summary>
        /// Create a compilation given sources
        /// </summary>
        /// <param name="sources">Sources to compile</param>
        /// <param name="targetFramework">Target framework of the compilation</param>
        /// <param name="outputKind">Output type</param>
        /// <returns>The resulting compilation</returns>
        public static async Task<Compilation> CreateCompilation(SyntaxTree[] sources, TestTargetFramework targetFramework = TestTargetFramework.Net, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
        {
            var referenceAssemblies = await GetReferenceAssemblies(targetFramework);

            // [TODO] Can remove once ancillary logic is removed.
            if (targetFramework is TestTargetFramework.Net6 or TestTargetFramework.Net)
            {
                referenceAssemblies = referenceAssemblies.Add(GetAncillaryReference());
            }

            return CSharpCompilation.Create("compilation",
                sources,
                referenceAssemblies,
                new CSharpCompilationOptions(outputKind, allowUnsafe: true, specificDiagnosticOptions: BindingRedirectWarnings));
        }

        /// <summary>
        /// Get the reference assembly collection for the <see cref="TestTargetFramework"/>.
        /// </summary>
        /// <param name="targetFramework">The target framework.</param>
        /// <returns>The reference assembly collection and metadata references</returns>
        private static async Task<ImmutableArray<MetadataReference>> GetReferenceAssemblies(TestTargetFramework targetFramework = TestTargetFramework.Net)
        {
            // Compute the reference assemblies for the target framework.
            if (targetFramework == TestTargetFramework.Net)
            {
                return SourceGenerators.Tests.LiveReferencePack.GetMetadataReferences();
            }
            else
            {
                var referenceAssembliesSdk = targetFramework switch
                {
                    TestTargetFramework.Framework => ReferenceAssemblies.NetFramework.Net48.Default,
                    TestTargetFramework.Standard => ReferenceAssemblies.NetStandard.NetStandard21,
                    TestTargetFramework.Core => ReferenceAssemblies.NetCore.NetCoreApp31,
                    TestTargetFramework.Net5 => ReferenceAssemblies.Net.Net50,
                    TestTargetFramework.Net6 => ReferenceAssemblies.Net.Net60,
                    _ => ReferenceAssemblies.Default
                };

                // Update the reference assemblies to include details from the NuGet.config.
                var referenceAssemblies = referenceAssembliesSdk
                    .WithNuGetConfigFilePath(Path.Combine(Path.GetDirectoryName(typeof(TestUtils).Assembly.Location)!, "NuGet.config"));

                return await ResolveReferenceAssemblies(referenceAssemblies);
            }
        }

        /// <summary>
        /// Get the metadata reference for the ancillary interop helper assembly.
        /// </summary>
        /// <returns></returns>
        internal static MetadataReference GetAncillaryReference()
        {
            // Include the assembly containing the new attribute and all of its references.
            // [TODO] Remove once the attribute has been added to the BCL
            var attrAssem = typeof(GeneratedDllImportAttribute).GetTypeInfo().Assembly;
            return MetadataReference.CreateFromFile(attrAssem.Location);
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
