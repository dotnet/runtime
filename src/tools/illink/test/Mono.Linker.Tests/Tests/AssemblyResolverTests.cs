// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ILLink.Shared;
using Mono.Cecil;
using Mono.Linker.Steps;
using Mono.Linker.Tests.TestCasesRunner;
using Xunit;

namespace Mono.Linker.Tests.Tests
{
    public class AssemblyResolverTests : IDisposable
    {
        readonly string _directory = Directory.CreateTempSubdirectory(nameof(AssemblyResolverTests)).FullName;
        readonly TrimmingTestLogger _logger = new();
        readonly LinkContext _context;

        public AssemblyResolverTests()
        {
            _context = new LinkContext(new Pipeline(), _logger, string.Empty);
        }

        public static IEnumerable<object[]> PathShapedNames()
        {
            string[] names = [
                "../Dependency", @"..\Dependency", "nested/Dependency", @"nested\Dependency",
                "./Dependency", @".\Dependency", "/Dependency", @"\Dependency",
                "C:/Dependency", @"C:\Dependency", "C:Dependency",
                "//server/share/Dependency", @"\\server\share\Dependency", @"\\?\C:\Dependency"
            ];
            foreach (string name in names)
                foreach (bool fullIdentity in new[] { false, true })
                    foreach (bool ignoreUnresolved in new[] { false, true })
                        foreach (bool cached in new[] { false, true })
                            yield return new object[] { name, fullIdentity, ignoreUnresolved, cached };
        }

        [Theory]
        [MemberData(nameof(PathShapedNames))]
        public void InvalidNameStringsUseUnresolvedHandling(string name, bool fullIdentity, bool ignoreUnresolved, bool cached)
        {
            _context.IgnoreUnresolved = ignoreUnresolved;
            _context.LogMessages = true;
            if (cached)
            {
                var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, new Version()), "Dependency", ModuleKind.Dll);
                _context.Resolver.CacheAssembly(assembly);
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                if (fullIdentity)
                    Assert.Null(_context.GetType($"<Module>, {name}, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null"));
                else
                    Assert.Null(_context.TryResolve(name));
            }

            if (fullIdentity)
            {
                var message = Assert.Single(_logger.GetLoggedMessages());
                Assert.Equal(ignoreUnresolved ? MessageCategory.Info : MessageCategory.Error, message.Category);
                if (!ignoreUnresolved)
                    Assert.Equal((int)DiagnosticId.CouldNotFindAssemblyReference, message.Code);
                Assert.Contains(name, message.ToMSBuildString());
            }
            else
            {
                Assert.Empty(_logger.GetLoggedMessages());
            }
        }

        [Theory]
        [InlineData("")]
        [InlineData("../Dependency")]
        [InlineData(@"nested\Dependency")]
        [InlineData("C:Dependency")]
        public void XmlAssemblyNamesUseUnresolvedWarnings(string name)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
                $"<linker><assembly fullname=\"{name}, Version=1.2.3.4\" /></linker>"));
            new DescriptorMarker(_context, stream, "descriptor.xml").Mark();
            _context.FlushCachedWarnings();
            Assert.Equal((int)DiagnosticId.XmlCouldNotResolveAssembly, Assert.Single(_logger.GetLoggedMessages()).Code);
            Assert.Empty(_context.Resolver.AssemblyCache);
        }

        [Theory]
        [InlineData("../Dependency")]
        [InlineData(@"nested\Dependency")]
        [InlineData("C:Dependency")]
        public void XmlAttributeAssemblyNamesUseUnresolvedWarnings(string name)
        {
            var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Target", new Version()), "Target", ModuleKind.Dll);
            _context.Resolver.CacheAssembly(assembly);
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
                $"<linker><assembly fullname=\"Target\"><attribute fullname=\"Example.Attribute\" assembly=\"{name}\" /></assembly></linker>"));
            new LinkAttributesParser(_context, stream, "attributes.xml").Parse(new AttributeInfo());
            _context.FlushCachedWarnings();
            Assert.Equal((int)DiagnosticId.XmlCouldNotResolveAssemblyForAttribute, Assert.Single(_logger.GetLoggedMessages()).Code);
        }

        [Fact]
        public void EmptySimpleNameRemainsUnresolved()
        {
            Assert.Null(_context.TryResolve(string.Empty));
            Assert.Empty(_logger.GetLoggedMessages());
        }

        [Theory]
        [InlineData("../Dependency")]
        [InlineData(@"nested\Dependency")]
        [InlineData("Missing.Dependency")]
        public void UnresolvedRootNamesUseExistingError(string name)
        {
            new RootAssemblyInput(name, AssemblyRootMode.AllMembers).Process(_context);
            Assert.Equal((int)DiagnosticId.RootAssemblyCouldNotBeFound, Assert.Single(_logger.GetLoggedMessages()).Code);
        }

        [Theory]
        [InlineData("../Dependency", ".dll")]
        [InlineData("../Dependency", ".exe")]
        [InlineData("../Dependency", ".winmd")]
        [InlineData("nested/Dependency", ".dll")]
        [InlineData("nested/Dependency", ".exe")]
        [InlineData("nested/Dependency", ".winmd")]
        [InlineData(@"nested\Dependency", ".dll")]
        [InlineData(@"nested\Dependency", ".exe")]
        [InlineData(@"nested\Dependency", ".winmd")]
        [InlineData("/Dependency", ".dll")]
        [InlineData("/Dependency", ".exe")]
        [InlineData("/Dependency", ".winmd")]
        public void DoesNotProbeExistingPathCandidates(string name, string extension)
        {
            _context.IgnoreUnresolved = false;
            string searchDirectory = Path.Combine(_directory, "search");
            Directory.CreateDirectory(searchDirectory);
            _context.Resolver.AddSearchDirectory(searchDirectory);
            if (name.StartsWith('/'))
                name = Path.Combine(_directory, "Dependency");
            string candidate = Path.Combine(searchDirectory, name + extension);
            WriteAssembly(candidate, "Dependency");

            Assert.Null(_context.TryResolve(name));
            Assert.Null(_context.TryResolve(name));
            Assert.Empty(_logger.GetLoggedMessages());
            Assert.Empty(_context.Resolver.AssemblyCache);
        }

        [Theory]
        [InlineData("Dependency", ".dll", false)]
        [InlineData("Dependency", ".exe", false)]
        [InlineData("Dependency", ".winmd", false)]
        [InlineData("Company.Dependency", ".dll", false)]
        [InlineData("Company.Dependency", ".exe", false)]
        [InlineData("Company.Dependency", ".winmd", false)]
        [InlineData("Company.Dependency", ".dll", true)]
        public void ResolvesSimpleAndParsedFullNames(string name, string extension, bool fullIdentity)
        {
            string path = Path.Combine(_directory, name + extension);
            WriteAssembly(path, name);
            _context.Resolver.AddSearchDirectory(_directory);
            var reference = fullIdentity
                ? AssemblyNameReference.Parse($"{name}, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null")
                : new AssemblyNameReference(name, new Version());

            var assembly = _context.Resolve(reference);
            Assert.NotNull(assembly);
            Assert.Equal(path, _context.GetAssemblyLocation(assembly));
            Assert.Same(assembly, _context.TryResolve(reference));
            Assert.Same(assembly, _context.TryResolve(name));
            Assert.Same(assembly.MainModule.Types[0],
                _context.GetType($"<Module>, {name}, Version=1.2.3.4, Culture=neutral, PublicKeyToken=null"));
        }

        [Fact]
        public void TypedReferencesAreNotRevalidated()
        {
            const string name = "Literal/Name";
            var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, new Version()), "Dependency", ModuleKind.Dll);
            _context.Resolver.CacheAssembly(assembly);
            var reference = new AssemblyNameReference(name, new Version());

            Assert.Same(assembly, _context.TryResolve(reference));
            Assert.Same(assembly, _context.Resolve(reference));
            Assert.Empty(_logger.GetLoggedMessages());
        }

        [Theory]
        [InlineData("../Dependency", ".dll")]
        [InlineData("../Dependency", ".exe")]
        [InlineData("../Dependency", ".winmd")]
        [InlineData("nested/Dependency", ".dll")]
        [InlineData("nested/Dependency", ".exe")]
        [InlineData("nested/Dependency", ".winmd")]
        public void MalformedReflectionNamesDoNotResolveExistingFiles(string name, string extension)
        {
            string searchDirectory = Path.Combine(_directory, "search");
            Directory.CreateDirectory(searchDirectory);
            _context.Resolver.AddSearchDirectory(searchDirectory);
            WriteAssembly(Path.Combine(searchDirectory, name + extension), "Dependency");
            using var origin = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Origin", new Version()), "Origin", ModuleKind.Dll);

            Assert.False(_context.TypeNameResolver.TryResolveTypeName(
                origin, $"<Module>, {name}", fallbackToCoreLib: true, out var type, out var records));
            Assert.Null(type);
            Assert.Null(records);
            Assert.Empty(_context.Resolver.AssemblyCache);
            Assert.Empty(_logger.GetLoggedMessages());
        }

        [Theory]
        [InlineData("/Dependency")]
        [InlineData("C:/Dependency")]
        public void RootedReflectionNamesRemainUnresolved(string name)
        {
            using var origin = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition("Origin", new Version()), "Origin", ModuleKind.Dll);

            Assert.False(_context.TypeNameResolver.TryResolveTypeName(
                origin, $"<Module>, {name}", fallbackToCoreLib: true, out var type, out var records));
            Assert.Null(type);
            Assert.Null(records);
            Assert.Empty(_context.Resolver.AssemblyCache);
            Assert.Empty(_logger.GetLoggedMessages());
        }

        [Theory]
        [InlineData("../Dependency", false)]
        [InlineData("../Dependency", true)]
        [InlineData(@"nested\Dependency", false)]
        [InlineData(@"nested\Dependency", true)]
        [InlineData("C:Dependency", false)]
        [InlineData("C:Dependency", true)]
        public void MalformedReflectionAssemblyProbesRemainQuiet(string name, bool cached)
        {
            if (cached)
            {
                var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, new Version()), "Dependency", ModuleKind.Dll);
                _context.Resolver.CacheAssembly(assembly);
            }

            Assert.Null(_context.TryResolve(name));
            Assert.Null(((ITryResolveAssemblyName)_context).TryResolve(name));
            Assert.Empty(_logger.GetLoggedMessages());
            Assert.Equal(cached ? 1 : 0, _context.Resolver.AssemblyCache.Count);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ExplicitReferencePathsTakePrecedence(bool relativePath)
        {
            string searchDirectory = Path.Combine(_directory, "search");
            string referencePath = Path.Combine(_directory, "references", "Company.Dependency.dll");
            WriteAssembly(Path.Combine(searchDirectory, "Company.Dependency.dll"), "Company.Dependency");
            WriteAssembly(referencePath, "Company.Dependency");
            _context.Resolver.AddSearchDirectory(searchDirectory);
            _context.Resolver.AddReferenceAssembly(relativePath ? Path.GetRelativePath(Environment.CurrentDirectory, referencePath) : referencePath);

            var reference = new AssemblyNameReference("Company.Dependency", new Version());
            var assembly = _context.TryResolve(reference);
            Assert.NotNull(assembly);
            Assert.Equal(referencePath, _context.GetAssemblyLocation(assembly));
            Assert.Same(assembly, _context.Resolver.GetAssembly(referencePath));
        }

        [Fact]
        public void ExplicitInputPathsRemainSupported()
        {
            string path = Path.Combine(_directory, "nested", "Dependency.dll");
            WriteAssembly(path, "Dependency");
            var assembly = _context.Resolver.GetAssembly(Path.GetRelativePath(Environment.CurrentDirectory, path));
            Assert.Equal(path, _context.GetAssemblyLocation(assembly));
            Assert.Same(assembly, _context.Resolver.GetAssembly(path));
            _context.Resolver.CacheAssembly(assembly);
            Assert.Same(assembly, _context.TryResolve("Dependency"));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ProcessingReferencesPreservesResolutionPrecedence(bool alreadyLoaded)
        {
            string first = Path.Combine(_directory, "first", "Dependency.dll");
            string second = Path.Combine(_directory, "second", "Dependency.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(first));
            File.WriteAllText(first, "Not an assembly");
            WriteAssembly(second, "Dependency");
            _context.DefaultAction = AssemblyAction.Copy;
            _context.TrimAction = AssemblyAction.Copy;
            _context.Resolver.AddReferenceAssembly(first);
            _context.Resolver.AddReferenceAssembly(second);
            string expectedPath = second;
            if (alreadyLoaded)
            {
                expectedPath = Path.Combine(_directory, "loaded", "Dependency.dll");
                WriteAssembly(expectedPath, "Dependency");
                _context.Resolver.CacheAssembly(_context.Resolver.GetAssembly(expectedPath));
            }

            new ProcessReferencesStep().Process(_context);

            var assembly = Assert.Single(_context.GetAssemblies());
            Assert.Equal(expectedPath, _context.GetAssemblyLocation(assembly));
            Assert.True(_context.Annotations.IsMarked(assembly.MainModule));
            Assert.Empty(_logger.GetLoggedMessages());
        }

        [Fact]
        public void SearchDirectoriesRemainOrdered()
        {
            string first = Path.Combine(_directory, "first");
            string second = Path.Combine(_directory, "second");
            WriteAssembly(Path.Combine(first, "Dependency.exe"), "Dependency");
            WriteAssembly(Path.Combine(second, "Dependency.dll"), "Dependency");
            _context.Resolver.AddSearchDirectory(first);
            _context.Resolver.AddSearchDirectory(second);

            var assembly = _context.TryResolve("Dependency");
            Assert.NotNull(assembly);
            Assert.Equal(Path.Combine(first, "Dependency.exe"), _context.GetAssemblyLocation(assembly));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void OrdinaryUnresolvedHandlingRemainsUnchanged(bool ignoreUnresolved)
        {
            _context.IgnoreUnresolved = ignoreUnresolved;
            _context.LogMessages = true;
            var reference = new AssemblyNameReference("Missing.Dependency", new Version());
            Assert.Null(_context.TryResolve(reference));
            Assert.Empty(_logger.GetLoggedMessages());
            Assert.Null(_context.Resolve(reference));
            Assert.Null(_context.Resolve(reference));
            var message = Assert.Single(_logger.GetLoggedMessages());
            Assert.Equal(ignoreUnresolved ? MessageCategory.Info : MessageCategory.Error, message.Category);
            if (!ignoreUnresolved)
                Assert.Equal((int)DiagnosticId.CouldNotFindAssemblyReference, message.Code);
        }

        static void WriteAssembly(string path, string name)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var assembly = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(name, new Version(1, 2, 3, 4)), name, ModuleKind.Dll);
            assembly.Write(path);
        }

        public void Dispose()
        {
            _context.Dispose();
            Directory.Delete(_directory, recursive: true);
        }
    }
}
