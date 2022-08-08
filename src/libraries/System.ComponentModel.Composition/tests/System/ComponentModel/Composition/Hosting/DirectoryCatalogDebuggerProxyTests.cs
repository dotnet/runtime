// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.UnitTesting;
using System.Reflection;
using Xunit;

namespace System.ComponentModel.Composition.Primitives
{
    public class DirectoryCatalogDebuggerProxyTests
    {
        [Fact]
        public void Constructor_NullAsCatalogArgument_ShouldThrowArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("catalog", () =>
            {
                new DirectoryCatalog.DirectoryCatalogDebuggerProxy((DirectoryCatalog)null);
            });
        }

        [Fact]
        public void Constructor_ValueAsCatalogArgument_ShouldSetPartsProperty()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                string directoryPath = GetTemporaryDirectory(e.Location);
                var catalog = CreateDirectoryCatalog(directoryPath);

                var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(catalog);

                EqualityExtensions.CheckEquals(catalog.Parts, proxy.Parts);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor_ValueAsCatalogArgument_ShouldSetAssemblyProperty()
        {
            string directoryPath = GetTemporaryDirectory();
            var expectations = Expectations.GetAssemblies();

            foreach (string fileName in expectations.Select(assembly => assembly.Location).ToArray())
            {
                File.Copy(fileName, Path.Combine(directoryPath, Path.GetFileName(fileName)));
            }
            var catalog = CreateDirectoryCatalog(directoryPath);
            var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(catalog);

            Assert.Equal(expectations, proxy.Assemblies);

        }

        [Fact]
        public void Constructor_ValueAsCatalogArgument_ShouldSetPathProperty()
        {
            string path = GetTemporaryDirectory();

            var catalog = CreateDirectoryCatalog(path);
            var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(catalog);

            Assert.Equal(path, proxy.Path);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Constructor_ValueAsCatalogArgument_ShouldSetSearchPatternProperty()
        {
            string directoryPath = GetTemporaryDirectory();
            var expectations = new ExpectationCollection<string, string>();

            expectations.Add("*.*", "*.*");
            expectations.Add("*.doc", "*.doc");
            expectations.Add("*.exe", "*.exe");
            expectations.Add("*.dll", "*.dll");

            foreach (var e in expectations)
            {
                var catalog = CreateDirectoryCatalog(directoryPath, e.Input);
                var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(catalog);

                Assert.Equal(e.Output, proxy.SearchPattern);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void FullPath_ValidPath_ShouldBeFine()
        {
            string directoryPath = GetTemporaryDirectory();
            var expectations = new ExpectationCollection<string, string>();

            // Ensure the path is always normalized properly.
            string rootTempPath = Path.GetFullPath(TemporaryFileCopier.GetRootTemporaryDirectory()).ToUpperInvariant();

            // Note: These relative paths work properly because the unit test temporary directories are always
            // created as a subfolder off the AppDomain.CurrentDomain.BaseDirectory.
            expectations.Add(".", Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".")).ToUpperInvariant());
            expectations.Add(TemporaryFileCopier.RootTemporaryDirectoryName, rootTempPath);
            expectations.Add(TemporaryFileCopier.GetRootTemporaryDirectory(), rootTempPath);
            expectations.Add(directoryPath, Path.GetFullPath(directoryPath).ToUpperInvariant());

            foreach (var e in expectations)
            {
                var cat = CreateDirectoryCatalog(e.Input, DirectoryCatalogTests.NonExistentSearchPattern);
                var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(cat);

                Assert.Equal(e.Output, proxy.FullPath);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void LoadedFiles_EmptyDirectory_ShouldBeFine()
        {
            string directoryPath = GetTemporaryDirectory();
            var cat = CreateDirectoryCatalog(directoryPath);
            var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(cat);

            Assert.Equal(0, proxy.LoadedFiles.Count);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser.")]
        public void LoadedFiles_ContainsMultipleDllsAndSomeNonDll_ShouldOnlyContainDlls()
        {
            string directoryPath = GetTemporaryDirectory();
            // Add one text file
            using (File.CreateText(Path.Combine(directoryPath, "Test.txt")))
            { }

            // Add two dll's
            string dll1 = Path.Combine(directoryPath, "Test1.dll");
            string dll2 = Path.Combine(directoryPath, "Test2.dll");
            File.Copy(Assembly.GetExecutingAssembly().Location, dll1);
            File.Copy(Assembly.GetExecutingAssembly().Location, dll2);

            var cat = CreateDirectoryCatalog(directoryPath);
            var proxy = new DirectoryCatalog.DirectoryCatalogDebuggerProxy(cat);

            EqualityExtensions.CheckEquals(new string[] { dll1.ToUpperInvariant(), dll2.ToUpperInvariant() },
                proxy.LoadedFiles);
        }

        private DirectoryCatalog.DirectoryCatalogDebuggerProxy CreateAssemblyDebuggerProxy(DirectoryCatalog catalog)
        {
            return new DirectoryCatalog.DirectoryCatalogDebuggerProxy(catalog);
        }

        private DirectoryCatalog CreateDirectoryCatalog(string path)
        {
            return new DirectoryCatalog(path);
        }

        private DirectoryCatalog CreateDirectoryCatalog(string path, string filter)
        {
            return new DirectoryCatalog(path, filter);
        }

        private string GetTemporaryDirectory(string location = null)
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }

    public class TemporaryFileCopier
    {
        public const string RootTemporaryDirectoryName = "RootTempDirectory";
        private static string _temporaryDirectory;
        public static string GetRootTemporaryDirectory()
        {
            if (_temporaryDirectory == null)
            {
                string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), RootTemporaryDirectoryName);

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                _temporaryDirectory = path;
            }

            return _temporaryDirectory;
        }

        public static string GetNewTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
