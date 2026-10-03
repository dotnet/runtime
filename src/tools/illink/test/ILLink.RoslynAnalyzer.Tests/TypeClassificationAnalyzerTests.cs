// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
    public sealed class TypeClassificationAnalyzerTests
    {
        [Fact]
        public async Task FrameworkTypesAndNamespaceLookalikesProduceExpectedDiagnostics()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                namespace Outer.System { public class Type { } }
                namespace system { public class Type { } }
                namespace System { public class Type<T> { } }
                namespace System.Reflection { public interface IReflect<T> { } }

                public class Target
                {
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Type FrameworkType;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Reflection.IReflect FrameworkReflect;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public string Text;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public Outer.System.Type WrongNamespace;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public system.Type WrongCase;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Type<int> GenericType;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Reflection.IReflect<int> GenericReflect;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public Missing.Type ErrorType;
                }
                """;

            Assert.Equal(
                ["IL2097", "IL2097", "IL2097", "IL2097", "IL2097"],
                await GetDiagnosticIdsAsync(source));
        }

        [Fact]
        public async Task SourceSystemTypesAreValidAnnotationTargets()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;

                namespace System { public class Type { } }
                namespace System.Reflection { public interface IReflect { } }
                namespace Outer.System { public class Type { } }

                public class Target
                {
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Type SourceType;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Reflection.IReflect SourceReflect;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public Outer.System.Type WrongNamespace;
                }
                """;

            Assert.Equal(["IL2097"], await GetDiagnosticIdsAsync(source));
        }

        [Fact]
        public async Task RepeatedCompilationsKeepClassificationsIndependent()
        {
            const string sourceType = """
                using System.Diagnostics.CodeAnalysis;
                namespace System { public class Type { } }
                public class Target
                {
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Type Value;
                }
                """;
            const string otherType = """
                using System.Diagnostics.CodeAnalysis;
                namespace Probe { public class Type { } }
                public class Target
                {
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public Probe.Type Value;
                }
                """;

            Assert.Empty(await GetDiagnosticIdsAsync(sourceType));
            Assert.Equal(["IL2097"], await GetDiagnosticIdsAsync(otherType));
            Assert.Empty(await GetDiagnosticIdsAsync(sourceType));
        }

        [Fact]
        public async Task ConcurrentAnalysesProduceConsistentDiagnostics()
        {
            const string source = """
                using System.Diagnostics.CodeAnalysis;
                namespace Probe { public class Type { } }
                public class Target
                {
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public System.Type Valid;
                    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
                    public Probe.Type Invalid;
                }
                """;

            string[][] results = await Task.WhenAll(
                Enumerable.Range(0, 8).Select(_ => GetDiagnosticIdsAsync(source)));
            foreach (string[] diagnostics in results)
                Assert.Equal(["IL2097"], diagnostics);
        }

        private static async Task<string[]> GetDiagnosticIdsAsync(string source)
        {
            var (compilation, _, exceptions) = TestCaseCompilation.CreateCompilation(
                source,
                consoleApplication: false,
                TestCaseUtils.UseMSBuildProperties(MSBuildPropertyOptionNames.EnableTrimAnalyzer));
            ImmutableArray<Diagnostic> diagnostics = await compilation.GetAnalyzerDiagnosticsAsync();
            Assert.Empty(exceptions);
            return diagnostics.Select(diagnostic => diagnostic.Id)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
