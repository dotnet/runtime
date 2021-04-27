// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Factories;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.UnitTesting;
using System.ComponentModel.Composition.Primitives;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition
{
    // This is a glorious do nothing ReflectionContext
    public class DirectoryCatalogTestsReflectionContext : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly assembly)
        {
            return assembly;
        }

#if FEATURE_INTERNAL_REFLECTIONCONTEXT
        public override Type MapType(Type type)
#else
        public override TypeInfo MapType(TypeInfo type)
#endif
        {
            return type;
        }
    }

    public class DirectoryCatalogTests
    {
        internal const string NonExistentSearchPattern = "*.NonExistentSearchPattern";

        private static void Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull(Func<ReflectionContext, DirectoryCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentNullException>("reflectionContext", () =>
            {
                var catalog = catalogCreator(null);
            });
        }

        private static void Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull(Func<ICompositionElement, DirectoryCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentNullException>("definitionOrigin", () =>
            {
                var catalog = catalogCreator(null);
            });
        }

        [Fact]
        public void Constructor2_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            DirectoryCatalogTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new DirectoryCatalog(TemporaryFileCopier.GetNewTemporaryDirectory(), rc);
            });
        }

        [Fact]
        public void Constructor3_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            DirectoryCatalogTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new DirectoryCatalog(TemporaryFileCopier.GetNewTemporaryDirectory(), dO);
            });
        }

        [Fact]
        public void Constructor4_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            DirectoryCatalogTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new DirectoryCatalog(TemporaryFileCopier.GetNewTemporaryDirectory(), rc, CreateDirectoryCatalog());
            });
        }

        [Fact]
        public void Constructor4_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            DirectoryCatalogTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new DirectoryCatalog(TemporaryFileCopier.GetNewTemporaryDirectory(), new DirectoryCatalogTestsReflectionContext(), dO);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.IO.DirectoryNotFoundException : Could not find a part of the path '/HOME/HELIXBOT/DOTNETBUILD/WORK/E77C2FB6-5244-4437-8E27-6DD709101152/WORK/D9EBA0EA-A511-4F42-AC8B-AC8054AAF606/UNZIP/'.
        public void ICompositionElementDisplayName_ShouldIncludeCatalogTypeNameAndDirectoryPath()
        {
            var paths = GetPathExpectations();

            foreach (var path in paths)
            {
                var catalog = (ICompositionElement)CreateDirectoryCatalog(path, NonExistentSearchPattern);

                string expected = string.Format("DirectoryCatalog (Path=\"{0}\")", path);

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.IO.DirectoryNotFoundException : Could not find a part of the path '/HOME/HELIXBOT/DOTNETBUILD/WORK/E77C2FB6-5244-4437-8E27-6DD709101152/WORK/D9EBA0EA-A511-4F42-AC8B-AC8054AAF606/UNZIP/'.
        public void ICompositionElementDisplayName_ShouldIncludeDerivedCatalogTypeNameAndAssemblyFullName()
        {
            var paths = GetPathExpectations();

            foreach (var path in paths)
            {
                var catalog = (ICompositionElement)new DerivedDirectoryCatalog(path, NonExistentSearchPattern);

                string expected = string.Format("DerivedDirectoryCatalog (Path=\"{0}\")", path);

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.IO.DirectoryNotFoundException : Could not find a part of the path '/HOME/HELIXBOT/DOTNETBUILD/WORK/E77C2FB6-5244-4437-8E27-6DD709101152/WORK/D9EBA0EA-A511-4F42-AC8B-AC8054AAF606/UNZIP/'.
        public void ToString_ShouldReturnICompositionElementDisplayName()
        {
            var paths = GetPathExpectations();

            foreach (var path in paths)
            {
                var catalog = (ICompositionElement)CreateDirectoryCatalog(path, NonExistentSearchPattern);

                Assert.Equal(catalog.DisplayName, catalog.ToString());
            }
        }

        [Fact]
        public void ICompositionElementDisplayName_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();

            var displayName = ((ICompositionElement)catalog).DisplayName;
        }

        [Fact]
        public void ICompositionElementOrigin_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();

            var origin = ((ICompositionElement)catalog).Origin;
        }

        [Fact]
        public void Parts_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var parts = catalog.Parts;
            });
        }

        [Fact]
        public void GetExports_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();
            var definition = ImportDefinitionFactory.Create();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                catalog.GetExports(definition);
            });
        }

        [Fact]
        public void Refresh_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                catalog.Refresh();
            });
        }

        [Fact]
        public void ToString_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();

            catalog.ToString();
        }

        [Fact]
        public void GetExports_NullAsConstraintArgument_ShouldThrowArgumentNull()
        {
            var catalog = CreateDirectoryCatalog();

            Assert.Throws<ArgumentNullException>("definition", () =>
            {
                catalog.GetExports((ImportDefinition)null);
            });
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            using (var catalog = CreateDirectoryCatalog())
            {
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var catalog = CreateDirectoryCatalog();
            catalog.Dispose();
            catalog.Dispose();
            catalog.Dispose();
        }

        [Fact]
        public void AddAssembly1_NullPathArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new DirectoryCatalog((string)null));
        }

        [Fact]
        public void AddAssembly1_EmptyPathArgument_ShouldThrowArgument()
        {
            Assert.Throws<ArgumentException>(() =>
                new DirectoryCatalog(""));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void AddAssembly1_TooLongPathNameArgument_ShouldThrowPathTooLongException()
        {
            Assert.Throws<PathTooLongException>(() =>
            {
                var c1 = new DirectoryCatalog(@"c:\This is a very long path\And Just to make sure\We will continue to make it very long\This is a very long path\And Just to make sure\We will continue to make it very long\This is a very long path\And Just to make sure\We will continue to make it very long\myassembly.dll");
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Parts()
        {
                var catalog = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());
                Assert.NotNull(catalog.Parts);
                Assert.True(catalog.Parts.Count() > 0);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Parts_ShouldSetDefinitionOriginToCatalogItself()
        {
                var catalog = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());
                Assert.True(catalog.Parts.Count() > 0);

                foreach (ICompositionElement definition in catalog.Parts)
                {
                    Assert.Same(catalog, definition.Origin);
                }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Path_ValidPath_ShouldBeFine()
        {
                var expectations = new ExpectationCollection<string, string>();

                expectations.Add(".", ".");
                expectations.Add(TemporaryFileCopier.RootTemporaryDirectoryName, TemporaryFileCopier.RootTemporaryDirectoryName);
                expectations.Add(TemporaryFileCopier.GetRootTemporaryDirectory(), TemporaryFileCopier.GetRootTemporaryDirectory());
                expectations.Add(TemporaryFileCopier.GetTemporaryDirectory(), TemporaryFileCopier.GetTemporaryDirectory());

                foreach (var e in expectations)
                {
                    var cat = CreateDirectoryCatalog(e.Input, NonExistentSearchPattern);

                    Assert.Equal(e.Output, cat.Path);
                }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void FullPath_ValidPath_ShouldBeFine()
        {
                var expectations = new ExpectationCollection<string, string>();

                // Ensure the path is always normalized properly.
                string rootTempPath = Path.GetFullPath(TemporaryFileCopier.GetRootTemporaryDirectory()).ToUpperInvariant();

                // Note: These relative paths work properly because the unit test temporary directories are always
                // created as a subfolder off the AppDomain.CurrentDomain.BaseDirectory.
                expectations.Add(".", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".")).ToUpperInvariant());
                expectations.Add(TemporaryFileCopier.RootTemporaryDirectoryName, rootTempPath);
                expectations.Add(TemporaryFileCopier.GetRootTemporaryDirectory(), rootTempPath);
                expectations.Add(TemporaryFileCopier.GetTemporaryDirectory(), Path.GetFullPath(TemporaryFileCopier.GetTemporaryDirectory()).ToUpperInvariant());

                foreach (var e in expectations)
                {
                    var cat = CreateDirectoryCatalog(e.Input, NonExistentSearchPattern);

                    Assert.Equal(e.Output, cat.FullPath);
                }
        }

        [Fact]
        public void LoadedFiles_EmptyDirectory_ShouldBeFine()
        {
                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

                Assert.Equal(0, cat.LoadedFiles.Count);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void LoadedFiles_ContainsMultipleDllsAndSomeNonDll_ShouldOnlyContainDlls()
        {
                // Add one text file
                using (File.CreateText(Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.txt"))) { }

                // Add two dll's
                string dll1 = Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test1.dll");
                string dll2 = Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test2.dll");
                File.Copy(Assembly.GetExecutingAssembly().Location, dll1);
                File.Copy(Assembly.GetExecutingAssembly().Location, dll2);

                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

                EqualityExtensions.CheckEquals(new string[] { dll1.ToUpperInvariant(), dll2.ToUpperInvariant() },
                    cat.LoadedFiles);
        }

        [Fact]
        public void LoadedFiles_NonStaticallyReferencedAssembly()
        {
            string testAssembly = "System.ComponentModel.Composition.Noop.Assembly.dll";
            var directory = TemporaryFileCopier.GetNewTemporaryDirectory();
            Directory.CreateDirectory(directory);
            var finalPath = Path.Combine(directory, testAssembly);
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), testAssembly);
            File.Copy(sourcePath, finalPath);
            var catalog = new DirectoryCatalog(directory, "*.dll");
            Assert.NotEmpty(catalog);
        }

        [Fact]
        public void Constructor_InvalidAssembly_ShouldBeFine()
        {
                using (File.CreateText(Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.dll"))) { }
                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());
        }

        [Fact]
        public void Constructor_NonExistentDirectory_ShouldThrow()
        {
                Assert.Throws<DirectoryNotFoundException>(() =>
                   new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory() + @"\NonexistentDirectoryWithoutEndingSlash"));

                Assert.Throws<DirectoryNotFoundException>(() =>
                   new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory() + @"\NonexistentDirectoryWithEndingSlash\"));

        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor_PassExistingFileName_ShouldThrow()
        {
                using (File.CreateText(Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.txt"))) { }
                Assert.Throws<IOException>(() =>
                    new DirectoryCatalog(Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.txt")));
        }

        [Fact]
        public void Constructor_PassNonExistingFileName_ShouldThrow()
        {
                Assert.Throws<DirectoryNotFoundException>(() =>
                    new DirectoryCatalog(Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "NonExistingFile.txt")));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Refresh_AssemblyAdded_ShouldFireOnChanged()
        {
                bool changedFired = false;
                bool changingFired = false;
                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

                Assert.Equal(0, cat.Parts.Count());

                cat.Changing += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
                    {
                        Assert.Equal(0, cat.Parts.Count());
                        changingFired = true;
                    });

                cat.Changed += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
                    {
                        Assert.NotEqual(0, cat.Parts.Count());
                        changedFired = true;
                    });

                File.Copy(Assembly.GetExecutingAssembly().Location, Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.dll"));

                cat.Refresh();

                Assert.True(changingFired);
                Assert.True(changedFired);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Refresh_AssemblyRemoved_ShouldFireOnChanged()
        {
                string file = Path.Combine(TemporaryFileCopier.GetTemporaryDirectory(), "Test.dll");
                File.Copy(Assembly.GetExecutingAssembly().Location, file);
                bool changedFired = false;
                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

                cat.Changed += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
                    changedFired = true);

                // This assembly can be deleted because it was already loaded by the CLR in another context
                // in another location so it isn't locked on disk.
                File.Delete(file);

                cat.Refresh();

                Assert.True(changedFired);
        }

        [Fact]
        public void Refresh_NoChanges_ShouldNotFireOnChanged()
        {
                var cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

                cat.Changed += new EventHandler<ComposablePartCatalogChangeEventArgs>((o, e) =>
                    Assert.False(true));

                cat.Refresh();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Refresh_DirectoryRemoved_ShouldThrowDirectoryNotFound()
        {
            DirectoryCatalog cat;
                cat = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());

            ExceptionAssert.Throws<DirectoryNotFoundException>(RetryMode.DoNotRetry, () =>
                cat.Refresh());
        }

        [Fact]
        public void GetExports()
        {
                var catalog = new AggregateCatalog();
                Expression<Func<ExportDefinition, bool>> constraint = (ExportDefinition exportDefinition) => exportDefinition.ContractName == AttributedModelServices.GetContractName(typeof(MyExport));
                IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> matchingExports = null;

                matchingExports = catalog.GetExports(constraint);
                Assert.NotNull(matchingExports);
                Assert.True(matchingExports.Count() == 0);

                var testsDirectoryCatalog = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());
                catalog.Catalogs.Add(testsDirectoryCatalog);
                matchingExports = catalog.GetExports(constraint);

                Assert.NotNull(matchingExports);
                Assert.True(matchingExports.Count() >= 0);

                IEnumerable<Tuple<ComposablePartDefinition, ExportDefinition>> expectedMatchingExports = catalog.Parts
                    .SelectMany(part => part.ExportDefinitions, (part, export) => new Tuple<ComposablePartDefinition, ExportDefinition>(part, export))
                    .Where(partAndExport => partAndExport.Item2.ContractName == AttributedModelServices.GetContractName(typeof(MyExport)));

                Assert.True(matchingExports.SequenceEqual(expectedMatchingExports));

                catalog.Catalogs.Remove(testsDirectoryCatalog);
                matchingExports = catalog.GetExports(constraint);
                Assert.NotNull(matchingExports);
                Assert.True(matchingExports.Count() == 0);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void AddAndRemoveDirectory()
        {
                var cat = new AggregateCatalog();
                var container = new CompositionContainer(cat);

                Assert.False(container.IsPresent<MyExport>());

                var dir1 = new DirectoryCatalog(TemporaryFileCopier.GetTemporaryDirectory());
                cat.Catalogs.Add(dir1);
                Assert.True(container.IsPresent<MyExport>());

                cat.Catalogs.Remove(dir1);

                Assert.False(container.IsPresent<MyExport>());
        }

        [Fact]
        public void AddDirectoryNotFoundException()
        {
            Assert.Throws<DirectoryNotFoundException>(() =>
            {
                var cat = new DirectoryCatalog("Directory That Should Never Exist tadfasdfasdfsdf");
            });
        }

        [Fact]
        public void ExecuteOnCreationThread()
        {
            // Add a proper test for event notification on caller thread
        }

        private DirectoryCatalog CreateDirectoryCatalog()
        {
            return CreateDirectoryCatalog(TemporaryFileCopier.GetNewTemporaryDirectory());
        }

        private DirectoryCatalog CreateDirectoryCatalog(string path)
        {
            return new DirectoryCatalog(path);
        }

        private DirectoryCatalog CreateDirectoryCatalog(string path, string searchPattern)
        {
            return new DirectoryCatalog(path, searchPattern);
        }

        public IEnumerable<string> GetPathExpectations()
        {
            yield return AppDomain.CurrentDomain.BaseDirectory;
            yield return AppDomain.CurrentDomain.BaseDirectory + @"\";
            yield return ".";
        }

        private class DerivedDirectoryCatalog : DirectoryCatalog
        {
            public DerivedDirectoryCatalog(string path, string searchPattern)
                : base(path, searchPattern)
            {
            }
        }
    }
}
