// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Factories;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.IO;
using System.Linq;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    // This is a glorious do nothing ReflectionContext
    public class AssemblyCatalogTestsReflectionContext : ReflectionContext
    {
        public override Assembly MapAssembly(Assembly assembly)
        {
            return assembly;
        }

        public override TypeInfo MapType(TypeInfo type)
        {
            return type;
        }
    }

    public class TestAssemblyOne { }

    public class TestAssemblyTwo { }

    public class TestAssemblyThree { }

    public class TestAssemblyFour { }

    public class AssemblyCatalogTestsHelper
    {
#pragma warning disable SYSLIB0012
        protected string GetAttributedAssemblyCodeBase()
        {
            return Assembly.GetExecutingAssembly().CodeBase;
        }
#pragma warning restore SYSLIB0012

        protected Assembly GetAttributedAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        protected AssemblyCatalog CreateAssemblyCatalog()
        {
            return CreateAssemblyCatalog(GetAttributedAssembly());
        }

        protected AssemblyCatalog CreateAssemblyCatalog(Assembly assembly)
        {
            return new AssemblyCatalog(assembly);
        }

        protected class DerivedAssemblyCatalog : AssemblyCatalog
        {
            public DerivedAssemblyCatalog(Assembly assembly)
                : base(assembly)
            {
            }
        }
    }

    public class AssemblyCatalogConstructorTests : AssemblyCatalogTestsHelper
    {
        // Test Codebase variant of the APIs
        internal static void Constructor_ValueAsCodebaseArgument_ShouldSetAssemblyProperty(Func<string, AssemblyCatalog> catalogCreator)
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
#pragma warning disable SYSLIB0012
                var catalog = catalogCreator(e.CodeBase);
#pragma warning restore SYSLIB0012

                Assert.Same(e, catalog.Assembly);
            }
        }

        internal static void Constructor_LockedFileAsCodeBaseArgument_ShouldThrowIOException(Func<string, AssemblyCatalog> catalogCreator)
        {
            string filename = Path.GetTempFileName();
            using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Throws<System.IO.IOException>(() => catalogCreator(filename));
            }
        }

        internal static void Constructor_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentNullException>("codeBase", () => catalogCreator(null));
        }

        internal static void Constructor_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentException>("codeBase", () => catalogCreator(""));
        }

        internal static void Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.Throws<ArgumentException>(() => catalogCreator("??||>"));
        }

        internal static void Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.ThrowsAny<IOException>(() => catalogCreator("??||>"));
        }

        internal static void Constructor_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad(Func<string, AssemblyCatalog> catalogCreator)
        {
            string directory = Environment.SystemDirectory;
            Assert.True(Directory.Exists(directory));

            Assert.Throws<FileLoadException>(() => catalogCreator(directory));
        }

        internal static void Constructor_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.Throws<PathTooLongException>(() =>
                catalogCreator(@"c:\This is a very long path\And Just to make sure\We will continue to make it very long\This is a very long path\And Just to make sure\We will continue to make it very long\This is a very long path\And Just to make sure\We will continue to make it very long\myassembly.dll"));
        }

        internal static void Constructor_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat(Func<string, AssemblyCatalog> catalogCreator)
        {
            string filename = Path.GetTempFileName();
            Assert.Throws<BadImageFormatException>(() => catalogCreator(filename));
        }

        internal static void Constructor_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound(Func<string, AssemblyCatalog> catalogCreator)
        {
            Assert.Throws<FileNotFoundException>(() => catalogCreator(@"FileThat should not ever exist"));
        }

        // Test Assembly variant of the APIs
        internal static void Constructor_ValueAsAssemblyArgument_ShouldSetAssemblyProperty(Func<Assembly, AssemblyCatalog> catalogCreator)
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = catalogCreator(e);

                Assert.Same(e, catalog.Assembly);
            }
        }

        internal static void Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull(Func<ReflectionContext, AssemblyCatalog> catalogCreator)
        {
            AssertExtensions.Throws<ArgumentNullException>("reflectionContext", () => catalogCreator(null));
        }

        internal static void Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull(Func<ICompositionElement, AssemblyCatalog> catalogCreator)
        {
            AssertExtensions.Throws<ArgumentNullException>("definitionOrigin", () => catalogCreator(null));
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase) constructor
        //=========================================================================================================================================
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor1_ValueAsCodebaseArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsCodebaseArgument_ShouldSetAssemblyProperty((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        public void Constructor1_LockedFileAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_LockedFileAsCodeBaseArgument_ShouldThrowIOException((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        public void Constructor1_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        public void Constructor1_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void Constructor1_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument_Desktop()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")] // Also see https://github.com/dotnet/runtime/issues/25099
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Constructor1_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO_Core()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor1_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor1_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong()
        {
            AssemblyCatalogConstructorTests.Constructor_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // fault segmentation - AnyUnix
        public void Constructor1_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat()
        {
            AssemblyCatalogConstructorTests.Constructor_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        public void Constructor1_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound()
        {
            AssemblyCatalogConstructorTests.Constructor_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound((s) =>
            {
                return new AssemblyCatalog(s);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor1_ShouldSetOriginToNull()
        {
            var catalog = (ICompositionElement)new AssemblyCatalog(GetAttributedAssemblyCodeBase());

            Assert.Null(catalog.Origin);
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase, ReflectionContext reflectionContext) constructor
        //=========================================================================================================================================

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor2_ValueAsCodebaseArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsCodebaseArgument_ShouldSetAssemblyProperty((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        public void Constructor2_LockedFileAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_LockedFileAsCodeBaseArgument_ShouldThrowIOException((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        public void Constructor2_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        public void Constructor2_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void Constructor2_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument_Desktop()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")] // Also see https://github.com/dotnet/runtime/issues/25099
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Constructor2_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor2_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor2_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong()
        {
            AssemblyCatalogConstructorTests.Constructor_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // fault segmentation - AnyUnix
        public void Constructor2_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat()
        {
            AssemblyCatalogConstructorTests.Constructor_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        public void Constructor2_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound()
        {
            AssemblyCatalogConstructorTests.Constructor_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        public void Constructor2_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new AssemblyCatalog(GetAttributedAssemblyCodeBase(), rc);
            });
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase, ICompositionElement definitionOrigin) constructor
        //=========================================================================================================================================
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor3_ValueAsCodebaseArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsCodebaseArgument_ShouldSetAssemblyProperty((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor3_LockedFileAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_LockedFileAsCodeBaseArgument_ShouldThrowIOException((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor3_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor3_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void Constructor3_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")] // // Also see https://github.com/dotnet/runtime/issues/25099
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Constructor3_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO_Core()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor3_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor3_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong()
        {
            AssemblyCatalogConstructorTests.Constructor_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // fault segmentation - AnyUnix
        public void Constructor3_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat()
        {
            AssemblyCatalogConstructorTests.Constructor_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor3_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound()
        {
            AssemblyCatalogConstructorTests.Constructor_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound((s) =>
            {
                return new AssemblyCatalog(s, (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor3_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new AssemblyCatalog(GetAttributedAssemblyCodeBase(), dO);
            });
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase, ICompositionElement definitionOrigin, ReflectionContext reflectionContext) constructor
        //=========================================================================================================================================
        [Fact]
        public void Constructor4_ValueAsCodebaseArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsAssemblyArgument_ShouldSetAssemblyProperty((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor4_LockedFileAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_LockedFileAsCodeBaseArgument_ShouldThrowIOException((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor4_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullFileNameAsCodeBaseArgument_ShouldThrowArgumentNull((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor4_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument()
        {
            AssemblyCatalogConstructorTests.Constructor_EmptyFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework)]
        public void Constructor4_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument_Desktop()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowArgument((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")] // Also see https://github.com/dotnet/runtime/issues/25099
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public void Constructor4_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO_Core()
        {
            AssemblyCatalogConstructorTests.Constructor_InvalidFileNameAsCodeBaseArgument_ShouldThrowIO((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor4_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad()
        {
            AssemblyCatalogConstructorTests.Constructor_DirectoryAsCodeBaseArgument_ShouldThrowFileLoad((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor4_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong()
        {
            AssemblyCatalogConstructorTests.Constructor_TooLongFileNameAsCodeBaseArgument_ShouldThrowPathTooLong((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // fault segmentation - AnyUnix
        public void Constructor4_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat()
        {
            AssemblyCatalogConstructorTests.Constructor_NonAssemblyFileNameAsCodeBaseArgument_ShouldThrowBadImageFormat((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor4_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound()
        {
            AssemblyCatalogConstructorTests.Constructor_NonExistentFileNameAsCodeBaseArgument_ShouldThrowFileNotFound((s) =>
            {
                return new AssemblyCatalog(s, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(s));
            });
        }

        [Fact]
        public void Constructor4_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new AssemblyCatalog(GetAttributedAssemblyCodeBase(), rc, (ICompositionElement)new AssemblyCatalog(GetAttributedAssembly()));
            });
        }

        [Fact]
        public void Constructor4_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new AssemblyCatalog(GetAttributedAssemblyCodeBase(), new AssemblyCatalogTestsReflectionContext(), dO);
            });
        }
        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase, ICompositionElement definitionOrigin) constructor
        //=========================================================================================================================================
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor7_ValueAsAssemblyArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsCodebaseArgument_ShouldSetAssemblyProperty((a) =>
            {
                return new AssemblyCatalog(a, (ICompositionElement)new AssemblyCatalog(GetAttributedAssembly()));
            });
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(Assembly assembly) constructor
        //=========================================================================================================================================
        [Fact]
        public void Constructor5_ValueAsAssemblyArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsAssemblyArgument_ShouldSetAssemblyProperty((a) =>
            {
                return new AssemblyCatalog(a);
            });
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(Assembly assembly, ReflectionContext reflectionContext) constructor
        //=========================================================================================================================================
        [Fact]
        public void Constructor6_ValueAsAssemblyArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsAssemblyArgument_ShouldSetAssemblyProperty((a) =>
            {
                return new AssemblyCatalog(a, new AssemblyCatalogTestsReflectionContext());
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor6_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new AssemblyCatalog(GetAttributedAssembly(), rc);
            });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void Constructor7_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new AssemblyCatalog(GetAttributedAssembly(), dO);
            });
        }

        [Fact]
        public void Constructor7_ShouldSetOriginToNull()
        {
            var catalog = (ICompositionElement)new AssemblyCatalog(GetAttributedAssembly());

            Assert.Null(catalog.Origin);
        }

        //=========================================================================================================================================
        //  Test cases for AssemblyCatalog(string codebase, ICompositionElement definitionOrigin, ReflectionContext reflectionContext) constructor
        //=========================================================================================================================================
        [Fact]
        public void Constructor8_ValueAsAssemblyArgument_ShouldSetAssemblyProperty()
        {
            AssemblyCatalogConstructorTests.Constructor_ValueAsAssemblyArgument_ShouldSetAssemblyProperty((a) =>
            {
                return new AssemblyCatalog(a, new AssemblyCatalogTestsReflectionContext(), (ICompositionElement)new AssemblyCatalog(GetAttributedAssembly()));
            });
        }

        [Fact]
        public void Constructor8_NullReflectionContextArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullReflectionContextArgument_ShouldThrowArgumentNull((rc) =>
            {
                return new AssemblyCatalog(GetAttributedAssemblyCodeBase(), rc, (ICompositionElement)new AssemblyCatalog(GetAttributedAssembly()));
            });
        }

#pragma warning disable SYSLIB0012
        [Fact]
        public void Constructor8_NullDefinitionOriginArgument_ShouldThrowArgumentNull()
        {
            AssemblyCatalogConstructorTests.Constructor_NullDefinitionOriginArgument_ShouldThrowArgumentNull((dO) =>
            {
                return new AssemblyCatalog(GetAttributedAssembly().CodeBase, new AssemblyCatalogTestsReflectionContext(), dO);
            });
        }
#pragma warning restore SYSLIB0012

        //=========================================================================================================================================
        //  Test cases for Assemblies decorated with the CatalogDiscoveryAttribute
        //=========================================================================================================================================

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void DiscoverCatalogUsingReflectionContextCatalogDiscoveryAttribute()
        {
            var catalog = new AssemblyCatalog(typeof(TestAssemblyOne).Assembly);
            Assert.True(catalog.Parts.Count() > 0);

            foreach (ICompositionElement definition in catalog.Parts)
            {
                Assert.Same(catalog, definition.Origin);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void DiscoverCatalogUsingDerivedReflectionContextCatalogDiscoveryAttribute()
        {
            var catalog = new AssemblyCatalog(typeof(TestAssemblyTwo).Assembly);
            Assert.True(catalog.Parts.Count() > 0);

            foreach (ICompositionElement definition in catalog.Parts)
            {
                Assert.Same(catalog, definition.Origin);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void DiscoverCatalogUsingNoDefaultConstructorReflectionContextCatalogDiscoveryAttribute_ShouldThrowArgumentException()
        {
            AssertExtensions.Throws<MissingMethodException>(() =>
            {
                var catalog = new AssemblyCatalog(typeof(TestAssemblyThree).Assembly);
                Assert.True(catalog.Parts.Count() > 0);
            }, string.Empty);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void DiscoverCatalogUsingDerivedReflectionContextCatalogDiscoveryAttribute_ShouldThrowArgumentException()
        {

            AssertExtensions.Throws<InvalidOperationException>(() =>
            {
                var catalog = new AssemblyCatalog(typeof(TestAssemblyFour).Assembly);
                Assert.True(catalog.Parts.Count() > 0);
            }, string.Empty);
        }
    }

    public class AssemblyCatalogTests : AssemblyCatalogTestsHelper
    {
        [Fact]
        public void Assembly_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();

            Assert.NotNull(catalog.Assembly);
        }

        [Fact]
        public void ICompositionElementDisplayName_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();

            var displayName = ((ICompositionElement)catalog).DisplayName;
        }

        [Fact]
        public void ICompositionElementOrigin_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();

            var origin = ((ICompositionElement)catalog).Origin;
        }

        [Fact]
        public void ToString_WhenCatalogDisposed_ShouldNotThrow()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();

            catalog.ToString();
        }

        [Fact]
        public void Parts_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();

            ExceptionAssert.ThrowsDisposed(catalog, () =>
            {
                var parts = catalog.Parts;
            });
        }

        [Fact]
        public void GetExports_WhenCatalogDisposed_ShouldThrowObjectDisposed()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();
            var definition = ImportDefinitionFactory.Create();

            ExceptionAssert.ThrowsDisposed(catalog, () => catalog.GetExports(definition));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void GetExports_NullAsConstraintArgument_ShouldThrowArgumentNull()
        {
            var catalog = CreateAssemblyCatalog();

            AssertExtensions.Throws<ArgumentNullException>("definition", () => catalog.GetExports(null));
        }

        [Fact]
        public void Dispose_ShouldNotThrow()
        {
            using (var catalog = CreateAssemblyCatalog())
            {
            }
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var catalog = CreateAssemblyCatalog();
            catalog.Dispose();
            catalog.Dispose();
            catalog.Dispose();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Parts()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            Assert.NotNull(catalog.Parts);
            Assert.True(catalog.Parts.Count() > 0);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void Parts_ShouldSetDefinitionOriginToCatalogItself()
        {
            var catalog = CreateAssemblyCatalog();
            Assert.True(catalog.Parts.Count() > 0);

            foreach (ICompositionElement definition in catalog.Parts)
            {
                Assert.Same(catalog, definition.Origin);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void AddAssemblyUsingFile()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly.Location);
            var container = new CompositionContainer(catalog);

            Assert.NotNull(container.GetExportedValue<MyExport>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TwoTypesWithSameSimpleName()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);

            NotSoUniqueName unique1 = container.GetExportedValue<NotSoUniqueName>();
            Assert.NotNull(unique1);

            Assert.Equal(23, unique1.MyIntProperty);

            NotSoUniqueName2.NotSoUniqueName nestedUnique = container.GetExportedValue<NotSoUniqueName2.NotSoUniqueName>();

            Assert.NotNull(nestedUnique);
            Assert.Equal("MyStringProperty", nestedUnique.MyStringProperty);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void GettingFunctionExports()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);

            ImportDefaultFunctions import = container.GetExportedValue<ImportDefaultFunctions>("ImportDefaultFunctions");
            import.VerifyIsBound();
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240")]
        public void AnExportOfAnInstanceThatFailsToCompose()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);

            // Rejection causes the part in the catalog whose imports cannot be
            // satisfied to be ignored, resulting in a cardinality mismatch instead of a
            // composition exception
            AssertExtensions.Throws<ImportCardinalityMismatchException>(() =>
            {
                container.GetExportedValue<string>("ExportMyString");
            }, string.Empty);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void SharedPartCreation()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new Int32Exporter(41));
            container.Compose(batch);

            var sharedPart1 = container.GetExportedValue<MySharedPartExport>();
            Assert.Equal(41, sharedPart1.Value);
            var sharedPart2 = container.GetExportedValue<MySharedPartExport>();
            Assert.Equal(41, sharedPart2.Value);

            Assert.Equal(sharedPart1, sharedPart2);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void NonSharedPartCreation()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);
            CompositionBatch batch = new CompositionBatch();
            batch.AddPart(new Int32Exporter(41));
            container.Compose(batch);

            var nonSharedPart1 = container.GetExportedValue<MyNonSharedPartExport>();
            Assert.Equal(41, nonSharedPart1.Value);
            var nonSharedPart2 = container.GetExportedValue<MyNonSharedPartExport>();
            Assert.Equal(41, nonSharedPart2.Value);

            Assert.NotSame(nonSharedPart1, nonSharedPart2);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void RecursiveNonSharedPartCreation()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<DirectCycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart1>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart2>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleWithSharedPartAndNonSharedPart>();
            });

            Assert.NotNull(container.GetExportedValue<CycleSharedPart>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart1>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart2>());
            Assert.NotNull(container.GetExportedValue<NoCycleNonSharedPart>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // typeof(System.Reflection.ReflectionTypeLoadException)
        public void RecursiveNonSharedPartCreationDisableSilentRejection()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog, CompositionOptions.DisableSilentRejection);

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<DirectCycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart1>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleNonSharedPart2>();
            });

            CompositionAssert.ThrowsError(ErrorId.ImportEngine_PartCannotGetExportedValue, () =>
            {
                container.GetExportedValue<CycleWithSharedPartAndNonSharedPart>();
            });

            Assert.NotNull(container.GetExportedValue<CycleSharedPart>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart1>());
            Assert.NotNull(container.GetExportedValue<CycleSharedPart2>());
            Assert.NotNull(container.GetExportedValue<NoCycleNonSharedPart>());
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/24240", TestPlatforms.AnyUnix)] // System.Reflection.ReflectionTypeLoadException : Unable to load one or more of the requested types. Retrieve the LoaderExceptions property for more information.
        public void TryToDiscoverExportWithGenericParameter()
        {
            var catalog = new AssemblyCatalog(typeof(AssemblyCatalogTests).Assembly);
            var container = new CompositionContainer(catalog);

            // Should find a type that inherits from an export
            Assert.NotNull(container.GetExportedValueOrDefault<object>(AttributedModelServices.GetContractName(typeof(ExportWhichInheritsFromGeneric))));

            // This should be exported because it is inherited by ExportWhichInheritsFromGeneric
            Assert.NotNull(container.GetExportedValueOrDefault<object>(AttributedModelServices.GetContractName(typeof(ExportWithGenericParameter<string>))));
        }

        [Fact]
        public void ICompositionElementDisplayName_ShouldIncludeCatalogTypeNameAndAssemblyFullName()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)CreateAssemblyCatalog(e);

                string expected = string.Format("AssemblyCatalog (Assembly=\"{0}\")", e.FullName);

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        public void ICompositionElementDisplayName_ShouldIncludeDerivedCatalogTypeNameAndAssemblyFullName()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)new DerivedAssemblyCatalog(e);

                string expected = string.Format("DerivedAssemblyCatalog (Assembly=\"{0}\")", e.FullName);

                Assert.Equal(expected, catalog.DisplayName);
            }
        }

        [Fact]
        public void ToString_ShouldReturnICompositionElementDisplayName()
        {
            var expectations = Expectations.GetAssemblies();

            foreach (var e in expectations)
            {
                var catalog = (ICompositionElement)CreateAssemblyCatalog(e);

                Assert.Equal(catalog.DisplayName, catalog.ToString());
            }
        }

        [Fact]
        public void NonStaticallyReferencedAssembly()
        {
            string testAssembly = "System.ComponentModel.Composition.Noop.Assembly.dll";
            var directory = TemporaryFileCopier.GetNewTemporaryDirectory();
            Directory.CreateDirectory(directory);
            var finalPath = Path.Combine(directory, testAssembly);
            var sourcePath = Path.Combine(Directory.GetCurrentDirectory(), testAssembly);
            File.Copy(sourcePath, finalPath);
            var assemblyCatalog = new AssemblyCatalog(finalPath);
            Assert.NotEmpty(assemblyCatalog);
        }
    }
}
