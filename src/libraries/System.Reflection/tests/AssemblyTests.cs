// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using Xunit;

[assembly:
Attr(77, name = "AttrSimple"),
Int32Attr(77, name = "Int32AttrSimple"),
Int64Attr(77, name = "Int64AttrSimple"),
StringAttr("hello", name = "StringAttrSimple"),
EnumAttr(PublicEnum.Case1, name = "EnumAttrSimple"),
TypeAttr(typeof(object), name = "TypeAttrSimple")]
[assembly: CompilationRelaxations(8)]
[assembly: Debuggable((DebuggableAttribute.DebuggingModes)263)]
[assembly: CLSCompliant(false)]
[assembly: TypeForwardedTo(typeof(string))]
[assembly: TypeForwardedTo(typeof(TypeInForwardedAssembly))]

namespace System.Reflection.Tests
{
    public class AssemblyTests : FileCleanupTestBase
    {
        private const string s_sourceTestAssemblyName = "TestAssembly.dll";

        private string SourceTestAssemblyPath { get; } = Path.Combine(Environment.CurrentDirectory, s_sourceTestAssemblyName);
        private string DestTestAssemblyPath { get; }
        private string LoadFromTestPath { get; }

        public AssemblyTests()
        {
            // Assembly.Location does not return the file path for single-file deployment targets.
            DestTestAssemblyPath = Path.Combine(base.TestDirectory, s_sourceTestAssemblyName);
            LoadFromTestPath = Path.Combine(base.TestDirectory, "System.Reflection.Tests.dll");
            File.Copy(SourceTestAssemblyPath, DestTestAssemblyPath);
            string currAssemblyPath = Path.Combine(Environment.CurrentDirectory, "System.Reflection.Tests.dll");
            File.Copy(currAssemblyPath, LoadFromTestPath, true);
        }

        [Theory]
        [InlineData(typeof(Int32Attr))]
        [InlineData(typeof(Int64Attr))]
        [InlineData(typeof(StringAttr))]
        [InlineData(typeof(EnumAttr))]
        [InlineData(typeof(TypeAttr))]
        [InlineData(typeof(CompilationRelaxationsAttribute))]
        [InlineData(typeof(AssemblyTitleAttribute))]
        [InlineData(typeof(AssemblyDescriptionAttribute))]
        [InlineData(typeof(AssemblyCompanyAttribute))]
        [InlineData(typeof(CLSCompliantAttribute))]
        [InlineData(typeof(DebuggableAttribute))]
        [InlineData(typeof(Attr))]
        public void CustomAttributes(Type type)
        {
            Assembly assembly = Helpers.ExecutingAssembly;
            IEnumerable<Type> attributesData = assembly.CustomAttributes.Select(customAttribute => customAttribute.AttributeType);
            Assert.Contains(type, attributesData);

            ICustomAttributeProvider attributeProvider = assembly;
            Assert.Single(attributeProvider.GetCustomAttributes(type, false));
            Assert.True(attributeProvider.IsDefined(type, false));

            IEnumerable<Type> customAttributes = attributeProvider.GetCustomAttributes(false).Select(attribute => attribute.GetType());
            Assert.Contains(type, customAttributes);
        }

        [Theory]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(Attr), true)]
        [InlineData(typeof(Int32Attr), true)]
        [InlineData(typeof(Int64Attr), true)]
        [InlineData(typeof(StringAttr), true)]
        [InlineData(typeof(EnumAttr), true)]
        [InlineData(typeof(TypeAttr), true)]
        [InlineData(typeof(ObjectAttr), true)]
        [InlineData(typeof(NullAttr), true)]
        public void DefinedTypes(Type type, bool expected)
        {
            IEnumerable<Type> customAttrs = Helpers.ExecutingAssembly.DefinedTypes.Select(typeInfo => typeInfo.AsType());

            Assert.Equal(expected, customAttrs.Contains(type));
        }

        [Theory]
        [InlineData("EmbeddedImage.png", true)]
        [InlineData("EmbeddedTextFile.txt", true)]
        [InlineData("NoSuchFile", false)]
        public void EmbeddedFiles(string resource, bool exists)
        {
            string[] resources = Helpers.ExecutingAssembly.GetManifestResourceNames();
            Stream resourceStream = Helpers.ExecutingAssembly.GetManifestResourceStream(resource);

            Assert.Equal(exists, resources.Contains(resource));
            Assert.Equal(exists, resourceStream != null);
        }

        [Theory]
        [InlineData("EmbeddedImage1.png", true)]
        [InlineData("EmbeddedTextFile1.txt", true)]
        [InlineData("NoSuchFile", false)]
        public void GetManifestResourceStream(string resource, bool exists)
        {
            Type assemblyType = typeof(AssemblyTests);
            Stream resourceStream = assemblyType.Assembly.GetManifestResourceStream(assemblyType, resource);
            Assert.Equal(exists, resourceStream != null);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Helpers.ExecutingAssembly, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void EqualsTest(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1.Equals(assembly2));
        }

        [Theory]
        [InlineData(typeof(AssemblyPublicClass), true)]
        [InlineData(typeof(AssemblyTests), true)]
        [InlineData(typeof(AssemblyPublicClass.PublicNestedClass), true)]
        [InlineData(typeof(PublicEnum), true)]
        [InlineData(typeof(AssemblyGenericPublicClass<>), true)]
        [InlineData(typeof(AssemblyInternalClass), false)]
        public void ExportedTypes(Type type, bool expected)
        {
            Assembly assembly = Helpers.ExecutingAssembly;
            Assert.Equal(assembly.GetExportedTypes(), assembly.ExportedTypes);

            Assert.Equal(expected, assembly.ExportedTypes.Contains(type));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser, "entry assembly won't be xunit.console on browser")]
        public void GetEntryAssembly()
        {
            Assert.NotNull(Assembly.GetEntryAssembly());
            string assembly = Assembly.GetEntryAssembly().ToString();
            bool correct = assembly.IndexOf("xunit.console", StringComparison.OrdinalIgnoreCase) != -1;
            Assert.True(correct, $"Unexpected assembly name {assembly}");
        }

        [Fact]
        public void GetFile()
        {
            var asm = typeof(AssemblyTests).Assembly;
            if (asm.Location.Length > 0)
            {
                Assert.Throws<ArgumentNullException>(() => asm.GetFile(null));
                AssertExtensions.Throws<ArgumentException>(null, () => asm.GetFile(""));
                Assert.Null(asm.GetFile("NonExistentfile.dll"));
                Assert.NotNull(asm.GetFile("System.Reflection.Tests.dll"));

                string name = AssemblyPathHelper.GetAssemblyLocation(asm);
                Assert.Equal(asm.GetFile("System.Reflection.Tests.dll").Name, name);
            }
            else
            {
                Assert.Throws<FileNotFoundException>(() => asm.GetFile("System.Reflection.Tests.dll"));
            }
        }

        [Fact]
        public void GetFile_InMemory()
        {
            var inMemBlob = File.ReadAllBytes(SourceTestAssemblyPath);
            var asm = Assembly.Load(inMemBlob);
            Assert.ThrowsAny<Exception>(() => asm.GetFile(null));
            Assert.Throws<FileNotFoundException>(() => asm.GetFile(s_sourceTestAssemblyName));
            Assert.Throws<FileNotFoundException>(() => asm.GetFiles());
            Assert.Throws<FileNotFoundException>(() => asm.GetFiles(getResourceModules: true));
            Assert.Throws<FileNotFoundException>(() => asm.GetFiles(getResourceModules: false));
        }

        [Fact]
        public void CodeBaseInMemory()
        {
            var inMemBlob = File.ReadAllBytes(SourceTestAssemblyPath);
            var asm = Assembly.Load(inMemBlob);
            // Should not throw
            #pragma warning disable SYSLIB0012
            _ = asm.CodeBase;
            #pragma warning restore SYSLIB0012
        }

        [Fact]
        public void GetFiles()
        {
            var asm = typeof(AssemblyTests).Assembly;
            if (asm.Location.Length > 0)
            {
                Assert.NotNull(asm.GetFiles());
                Assert.Equal(1, asm.GetFiles().Length);

                string name = AssemblyPathHelper.GetAssemblyLocation(asm);
                Assert.Equal(asm.GetFiles()[0].Name, name);
            }
            else
            {
                Assert.Throws<FileNotFoundException>(() => asm.GetFiles());
            }
        }

        public static IEnumerable<object[]> GetHashCode_TestData()
        {
            yield return new object[] { LoadSystemRuntimeAssembly() };
            yield return new object[] { LoadSystemCollectionsAssembly() };
            yield return new object[] { LoadSystemReflectionAssembly() };
            yield return new object[] { typeof(AssemblyTests).GetTypeInfo().Assembly };
        }

        [Theory]
        [MemberData(nameof(GetHashCode_TestData))]
        public void GetHashCodeTest(Assembly assembly)
        {
            int hashCode = assembly.GetHashCode();
            Assert.NotEqual(-1, hashCode);
            Assert.NotEqual(0, hashCode);
        }

        [Theory]
        [InlineData("System.Reflection.Tests.AssemblyPublicClass", true)]
        [InlineData("System.Reflection.Tests.AssemblyInternalClass", true)]
        [InlineData("System.Reflection.Tests.PublicEnum", true)]
        [InlineData("System.Reflection.Tests.PublicStruct", true)]
        [InlineData("AssemblyPublicClass", false)]
        [InlineData("NoSuchType", false)]
        public void GetTypeTest(string name, bool exists)
        {
            Type type = Helpers.ExecutingAssembly.GetType(name);
            if (exists)
            {
                Assert.Equal(name, type.FullName);
            }
            else
            {
                Assert.Null(type);
            }
        }

        [Fact]
        public void GetType_NoQualifierAllowed()
        {
            Assembly a = typeof(G<int>).Assembly;
            string s = typeof(G<int>).AssemblyQualifiedName;
            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(s, throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void GetType_DoesntSearchMscorlib()
        {
            Assembly a = typeof(AssemblyTests).Assembly;
            Assert.Throws<TypeLoadException>(() => a.GetType("System.Object", throwOnError: true, ignoreCase: false));
            Assert.Throws<TypeLoadException>(() => a.GetType("G`1[[System.Object]]", throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void GetType_DefaultsToItself()
        {
            Assembly a = typeof(AssemblyTests).Assembly;
            Type t = a.GetType("G`1[[G`1[[System.Int32, mscorlib]]]]", throwOnError: true, ignoreCase: false);
            Assert.Equal(typeof(G<G<int>>), t);
        }

#pragma warning disable SYSLIB0005 // Obsolete: GAC
        [Fact]
        public void GlobalAssemblyCache()
        {
            Assert.False(typeof(AssemblyTests).Assembly.GlobalAssemblyCache);
        }
#pragma warning restore SYSLIB0005 // Obsolete: GAC

        [Fact]
        public void HostContext()
        {
            Assert.Equal(0, typeof(AssemblyTests).Assembly.HostContext);
        }

        public static IEnumerable<object[]> IsDynamic_TestData()
        {
            yield return new object[] { Helpers.ExecutingAssembly, false };
            yield return new object[] { LoadSystemCollectionsAssembly(), false };
        }

        [Theory]
        [MemberData(nameof(IsDynamic_TestData))]
        public void IsDynamic(Assembly assembly, bool expected)
        {
            Assert.Equal(expected, assembly.IsDynamic);
        }

        public static IEnumerable<object[]> Load_TestData()
        {
            yield return new object[] { new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName) };
            yield return new object[] { new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName) };
            yield return new object[] { new AssemblyName(typeof(AssemblyName).GetTypeInfo().Assembly.FullName) };
        }

        [Fact]
        public void IsFullyTrusted()
        {
            Assert.True(typeof(AssemblyTests).Assembly.IsFullyTrusted);
        }

        [Fact]
        public void SecurityRuleSetTest()
        {
            Assert.Equal(SecurityRuleSet.None, typeof(AssemblyTests).Assembly.SecurityRuleSet);
        }

        [Theory]
        [MemberData(nameof(Load_TestData))]
        public void Load(AssemblyName assemblyRef)
        {
            Assert.NotNull(Assembly.Load(assemblyRef));
        }

        [Fact]
        public void Load_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((AssemblyName)null)); // AssemblyRef is null
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName("no such assembly"))); // No such assembly
        }

        [Fact]
        public void LoadFile()
        {
            Assembly currentAssembly = typeof(AssemblyTests).Assembly;
            const string RuntimeTestsDll = "System.Reflection.Tests.dll";
            string fullRuntimeTestsPath = Path.GetFullPath(RuntimeTestsDll);

            var loadedAssembly1 = Assembly.LoadFile(fullRuntimeTestsPath);
            Assert.NotEqual(currentAssembly, loadedAssembly1);

            System.Runtime.Loader.AssemblyLoadContext alc = System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(loadedAssembly1);
            string expectedName = string.Format("Assembly.LoadFile({0})", fullRuntimeTestsPath);
            Assert.Equal(expectedName, alc.Name);
            Assert.Contains(fullRuntimeTestsPath, alc.Name);
            Assert.Contains(expectedName, alc.ToString());
            Assert.Contains("System.Runtime.Loader.IndividualAssemblyLoadContext", alc.ToString());

            string dir = Path.GetDirectoryName(fullRuntimeTestsPath);
            fullRuntimeTestsPath = Path.Combine(dir, ".", RuntimeTestsDll);

            Assembly loadedAssembly2 = Assembly.LoadFile(fullRuntimeTestsPath);
            Assert.Equal(loadedAssembly1, loadedAssembly2);
        }

        [Fact]
        public void LoadFile_NullPath_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Assembly.LoadFile(null));
        }

        [Fact]
        public void LoadFile_NoSuchPath_ThrowsFileNotFoundException()
        {
            string rootedPath = Path.GetFullPath(Guid.NewGuid().ToString("N"));
            AssertExtensions.ThrowsContains<FileNotFoundException>(() => Assembly.LoadFile(rootedPath), rootedPath);
        }

        [Fact]
        public void LoadFile_PartiallyQualifiedPath_ThrowsArgumentException()
        {
            string path = "System.Runtime.Tests.dll";
            ArgumentException ex = AssertExtensions.Throws<ArgumentException>("path", () => Assembly.LoadFile(path));
            Assert.Contains(path, ex.Message);
        }

        // This test should apply equally to Unix, but this reliably hits a particular one of the
        // myriad ways that assembly load can fail
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void LoadFile_ValidPEBadIL_ThrowsBadImageFormatExceptionWithPath()
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "kernelbase.dll");
            if (!File.Exists(path))
                return;

            AssertExtensions.ThrowsContains<BadImageFormatException>(() => Assembly.LoadFile(path), path);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(50)]
        [InlineData(100)]
        // Higher numbers hit some codepaths that currently don't include the path in the exception message
        public void LoadFile_ValidPEBadIL_ThrowsBadImageFormatExceptionWithPath_ByInitialSeek(int seek)
        {
            ReadOnlySpan<byte> garbage = Encoding.UTF8.GetBytes(new string('X', 500));
            string path = GetTestFilePath();
            File.Copy(SourceTestAssemblyPath, path);
            using (var fs = new FileStream(path, FileMode.Open))
            {
                fs.Position = seek;
                fs.Write(garbage);
            }

            AssertExtensions.ThrowsContains<BadImageFormatException>(() => Assembly.LoadFile(path), path);
        }

        [Fact]
        public void LoadFromUsingHashValue()
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom("abc", null, System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1));
        }

        [Fact]
        public void LoadFrom_SamePath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.LoadFrom(DestTestAssemblyPath);
            Assembly assembly2 = Assembly.LoadFrom(DestTestAssemblyPath);
            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        public void LoadFrom_SameIdentityAsAssemblyWithDifferentPath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.LoadFrom(AssemblyPathHelper.GetAssemblyLocation(typeof(AssemblyTests).Assembly));
            Assert.Equal(assembly1, typeof(AssemblyTests).Assembly);

            Assembly assembly2 = Assembly.LoadFrom(LoadFromTestPath);

            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        public void LoadFrom_NullAssemblyFile_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => Assembly.LoadFrom(null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => Assembly.UnsafeLoadFrom(null));
        }

        [Fact]
        public void LoadFrom_EmptyAssemblyFile_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, (() => Assembly.LoadFrom("")));
            AssertExtensions.Throws<ArgumentException>("path", null, (() => Assembly.UnsafeLoadFrom("")));
        }

        [Fact]
        public void LoadFrom_NoSuchFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.LoadFrom("NoSuchPath"));
            Assert.Throws<FileNotFoundException>(() => Assembly.UnsafeLoadFrom("NoSuchPath"));
        }

        [Fact]
        public void UnsafeLoadFrom_SamePath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.UnsafeLoadFrom(DestTestAssemblyPath);
            Assembly assembly2 = Assembly.UnsafeLoadFrom(DestTestAssemblyPath);
            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        public void LoadFrom_WithHashValue_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom(DestTestAssemblyPath, new byte[0], Configuration.Assemblies.AssemblyHashAlgorithm.None));
        }

        [Fact]
        public void LoadModule()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule("abc", null));
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule("abc", null, null));
        }

#pragma warning disable 618
        [Fact]
        public void LoadWithPartialName()
        {
            string simpleName = typeof(AssemblyTests).Assembly.GetName().Name;
            var assembly = Assembly.LoadWithPartialName(simpleName);
            Assert.Equal(typeof(AssemblyTests).Assembly, assembly);
        }

        [Fact]
        public void LoadWithPartialName_Neg()
        {
            AssertExtensions.Throws<ArgumentNullException>("partialName", () => Assembly.LoadWithPartialName(null));
            AssertExtensions.Throws<ArgumentException>("partialName", () => Assembly.LoadWithPartialName(""));
            Assert.Null(Assembly.LoadWithPartialName("no such assembly"));
        }
#pragma warning restore 618

        [Fact]
        public void Location_ExecutingAssembly_IsNotNull()
        {
            // This test applies on all platforms including .NET Native. Location must at least be non-null (it can be empty).
            // System.Reflection.CoreCLR.Tests adds tests that expect more than that.
            Assert.NotNull(Helpers.ExecutingAssembly.Location);
        }

#pragma warning disable SYSLIB0012
        [Fact]
        public void CodeBase()
        {
            Assert.NotEmpty(Helpers.ExecutingAssembly.CodeBase);
        }
#pragma warning restore SYSLIB0012

        [Fact]
        public void ImageRuntimeVersion()
        {
            Assert.NotEmpty(Helpers.ExecutingAssembly.ImageRuntimeVersion);
        }

        public static IEnumerable<object[]> CreateInstance_TestData()
        {
            yield return new object[] { Helpers.ExecutingAssembly, typeof(AssemblyPublicClass).FullName, typeof(AssemblyPublicClass) };
            yield return new object[] { typeof(int).GetTypeInfo().Assembly, typeof(int).FullName, typeof(int) };
            yield return new object[] { typeof(int).GetTypeInfo().Assembly, typeof(Dictionary<int, string>).FullName, typeof(Dictionary<int, string>) };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_TestData))]
        public void CreateInstance(Assembly assembly, string typeName, Type expectedType)
        {
            Assert.IsType(expectedType, assembly.CreateInstance(typeName));
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, false));
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, true));

            Assert.IsType(expectedType, assembly.CreateInstance(typeName.ToUpper(), true));
            Assert.IsType(expectedType, assembly.CreateInstance(typeName.ToLower(), true));
        }

        public static IEnumerable<object[]> CreateInstanceWithBindingFlags_TestData()
        {
            yield return new object[] { typeof(AssemblyTests).Assembly, typeof(AssemblyPublicClass).FullName, BindingFlags.CreateInstance, typeof(AssemblyPublicClass) };
            yield return new object[] { typeof(int).Assembly, typeof(int).FullName, BindingFlags.Default, typeof(int) };
            yield return new object[] { typeof(int).Assembly, typeof(Dictionary<int, string>).FullName, BindingFlags.Default, typeof(Dictionary<int, string>) };
        }

        [Theory]
        [MemberData(nameof(CreateInstanceWithBindingFlags_TestData))]
        public void CreateInstanceWithBindingFlags(Assembly assembly, string typeName, BindingFlags bindingFlags, Type expectedType)
        {
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, true, bindingFlags, null, null, null, null));
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, false, bindingFlags, null, null, null, null));
        }

        public static IEnumerable<object[]> CreateInstance_Invalid_TestData()
        {
            yield return new object[] { "", typeof(ArgumentException) };
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(AssemblyClassWithPrivateCtor).FullName, typeof(MissingMethodException) };
            yield return new object[] { typeof(AssemblyClassWithNoDefaultCtor).FullName, typeof(MissingMethodException) };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_Invalid_TestData))]
        public void CreateInstance_Invalid(string typeName, Type exceptionType)
        {
            Assembly assembly = Helpers.ExecutingAssembly;
            Assert.Throws(exceptionType, () => Helpers.ExecutingAssembly.CreateInstance(typeName));
            Assert.Throws(exceptionType, () => Helpers.ExecutingAssembly.CreateInstance(typeName, true));
            Assert.Throws(exceptionType, () => Helpers.ExecutingAssembly.CreateInstance(typeName, false));

            assembly = typeof(AssemblyTests).Assembly;
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, true, BindingFlags.Public, null, null, null, null));
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, false, BindingFlags.Public, null, null, null, null));
        }

        [Fact]
        public void CreateQualifiedName()
        {
            string assemblyName = Helpers.ExecutingAssembly.ToString();
            Assert.Equal(typeof(AssemblyTests).FullName + ", " + assemblyName, Assembly.CreateQualifiedName(assemblyName, typeof(AssemblyTests).FullName));
        }

        [Fact]
        public void GetReferencedAssemblies()
        {
            // It is too brittle to depend on the assembly references so we just call the method and check that it does not throw.
            AssemblyName[] assemblies = Helpers.ExecutingAssembly.GetReferencedAssemblies();
            Assert.NotEmpty(assemblies);
        }

        public static IEnumerable<object[]> Modules_TestData()
        {
            yield return new object[] { LoadSystemCollectionsAssembly() };
            yield return new object[] { LoadSystemReflectionAssembly() };
        }

        [Theory]
        [MemberData(nameof(Modules_TestData))]
        public void Modules(Assembly assembly)
        {
            Assert.NotEmpty(assembly.Modules);
            foreach (Module module in assembly.Modules)
            {
                Assert.NotNull(module);
            }
        }

        public static IEnumerable<object[]> Equality_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), typeof(AssemblyTests).Assembly, false };
        }

        [Theory]
        [MemberData(nameof(Equality_TestData))]
        public void Equality(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1 == assembly2);
            Assert.NotEqual(expected, assembly1 != assembly2);
        }

        [Fact]
        public void GetAssembly_Nullery()
        {
            AssertExtensions.Throws<ArgumentNullException>("type", () => Assembly.GetAssembly(null));
        }

        public static IEnumerable<object[]> GetAssembly_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(HashSet<int>).GetTypeInfo().Assembly.FullName)), Assembly.GetAssembly(typeof(HashSet<int>)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.GetAssembly(typeof(int)), true };
            yield return new object[] { typeof(AssemblyTests).Assembly, Assembly.GetAssembly(typeof(AssemblyTests)), true };
        }

        [Theory]
        [MemberData(nameof(GetAssembly_TestData))]
        public void GetAssembly(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1.Equals(assembly2));
        }

        public static IEnumerable<object[]> GetCallingAssembly_TestData()
        {
            yield return new object[] { typeof(AssemblyTests).Assembly, GetGetCallingAssembly(), true };
            yield return new object[] { Assembly.GetCallingAssembly(), GetGetCallingAssembly(), false };
        }

        [Theory]
        [MemberData(nameof(GetCallingAssembly_TestData))]
        public void GetCallingAssembly(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1.Equals(assembly2));
        }

        [Fact]
        public void GetExecutingAssembly()
        {
            Assert.True(typeof(AssemblyTests).Assembly.Equals(Assembly.GetExecutingAssembly()));
        }

        [Fact]
        public void GetSatelliteAssemblyNeg()
        {
            Assert.Throws<ArgumentNullException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(null)));
            Assert.Throws<System.IO.FileNotFoundException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(CultureInfo.InvariantCulture)));
        }

        [Fact]
        public void AssemblyLoadFromString()
        {
            AssemblyName an = typeof(AssemblyTests).Assembly.GetName();
            string fullName = an.FullName;
            string simpleName = an.Name;

            Assembly a1 = Assembly.Load(fullName);
            Assert.NotNull(a1);
            Assert.Equal(fullName, a1.GetName().FullName);

            Assembly a2 = Assembly.Load(simpleName);
            Assert.NotNull(a2);
            Assert.Equal(fullName, a2.GetName().FullName);
        }

        [Fact]
        public void AssemblyLoadFromStringNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((string)null));
            AssertExtensions.Throws<ArgumentException>(null, () => Assembly.Load(string.Empty));

            string emptyCName = new string('\0', 1);
            AssertExtensions.Throws<ArgumentException>(null, () => Assembly.Load(emptyCName));

            Assert.Throws<FileNotFoundException>(() => Assembly.Load("no such assembly")); // No such assembly
        }

        [Fact]
        public void AssemblyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(AssemblyPathHelper.GetAssemblyLocation(assembly));

            Assembly loadedAssembly = Assembly.Load(aBytes);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [Fact]
        public void AssemblyLoadFromBytesNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((byte[])null));
            Assert.Throws<BadImageFormatException>(() => Assembly.Load(new byte[0]));
        }

        [Fact]
        public void AssemblyLoadFromBytesWithSymbols()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(AssemblyPathHelper.GetAssemblyLocation(assembly));
            byte[] symbols = System.IO.File.ReadAllBytes((System.IO.Path.ChangeExtension(AssemblyPathHelper.GetAssemblyLocation(assembly), ".pdb")));

            Assembly loadedAssembly = Assembly.Load(aBytes, symbols);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [Fact]
        public void AssemblyReflectionOnlyLoadFromString()
        {
            AssemblyName an = typeof(AssemblyTests).Assembly.GetName();
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad(an.FullName));
        }

        [Fact]
        public void AssemblyReflectionOnlyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(AssemblyPathHelper.GetAssemblyLocation(assembly));
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad(aBytes));
        }

        [Fact]
        public void AssemblyReflectionOnlyLoadFromNeg()
        {
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad((string)null));
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad(string.Empty));
            Assert.Throws<PlatformNotSupportedException>(() => Assembly.ReflectionOnlyLoad((byte[])null));
        }

        public static IEnumerable<object[]> GetModules_TestData()
        {
            yield return new object[] { LoadSystemCollectionsAssembly() };
            yield return new object[] { LoadSystemReflectionAssembly() };
        }

        [Theory]
        [MemberData(nameof(GetModules_TestData))]
        public void GetModules_GetModule(Assembly assembly)
        {
            Assert.NotEmpty(assembly.GetModules());
            foreach (Module module in assembly.GetModules())
            {
                Assert.Equal(module, assembly.GetModule(module.ToString()));
            }
        }

        [Fact]
        public void GetLoadedModules()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.NotEmpty(assembly.GetLoadedModules());
            foreach (Module module in assembly.GetLoadedModules())
            {
                Assert.NotNull(module);
                Assert.Equal(module, assembly.GetModule(module.ToString()));
            }
        }

        [Theory]
        [InlineData(typeof(Int32Attr))]
        [InlineData(typeof(Int64Attr))]
        [InlineData(typeof(StringAttr))]
        [InlineData(typeof(EnumAttr))]
        [InlineData(typeof(TypeAttr))]
        [InlineData(typeof(CompilationRelaxationsAttribute))]
        [InlineData(typeof(AssemblyTitleAttribute))]
        [InlineData(typeof(AssemblyDescriptionAttribute))]
        [InlineData(typeof(AssemblyCompanyAttribute))]
        [InlineData(typeof(CLSCompliantAttribute))]
        [InlineData(typeof(DebuggableAttribute))]
        [InlineData(typeof(Attr))]
        public void GetCustomAttributesData(Type attrType)
        {
            IEnumerable<CustomAttributeData> customAttributesData = typeof(AssemblyTests).Assembly.GetCustomAttributesData().Where(cad => cad.AttributeType == attrType);
            Assert.True(customAttributesData.Count() > 0, $"Did not find custom attribute of type {attrType}");
        }

        [Fact]
        public static void AssemblyGetForwardedTypes()
        {
            Assembly a = typeof(AssemblyTests).Assembly;
            Type[] forwardedTypes = a.GetForwardedTypes();

            forwardedTypes = forwardedTypes.OrderBy(t => t.FullName).ToArray();

            Type[] expected = { typeof(string), typeof(TypeInForwardedAssembly), typeof(TypeInForwardedAssembly.PublicInner), typeof(TypeInForwardedAssembly.PublicInner.PublicInnerInner) };
            expected = expected.OrderBy(t => t.FullName).ToArray();

            Assert.Equal<Type>(expected, forwardedTypes);
        }

        [Fact]
        public static void AssemblyGetForwardedTypesLoadFailure()
        {
            Assembly a = typeof(TypeInForwardedAssembly).Assembly;
            ReflectionTypeLoadException rle = Assert.Throws<ReflectionTypeLoadException>(() => a.GetForwardedTypes());
            Assert.Equal(2, rle.Types.Length);
            Assert.Equal(2, rle.LoaderExceptions.Length);

            bool foundSystemObject = false;
            bool foundBifException = false;
            for (int i = 0; i < rle.Types.Length; i++)
            {
                Type type = rle.Types[i];
                Exception exception = rle.LoaderExceptions[i];

                if (type == typeof(object) && exception == null)
                    foundSystemObject = true;

                if (type == null && exception is BadImageFormatException)
                    foundBifException = true;
            }

            Assert.True(foundSystemObject);
            Assert.True(foundBifException);
        }

        private static Assembly LoadSystemCollectionsAssembly()
        {
            // Force System.collections to be linked statically
            List<int> li = new List<int>();
            li.Add(1);
            return Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName));
        }

        private static Assembly LoadSystemReflectionAssembly()
        {
            // Force System.Reflection to be linked statically
            return Assembly.Load(new AssemblyName(typeof(AssemblyName).GetTypeInfo().Assembly.FullName));
        }

        private static Assembly LoadSystemRuntimeAssembly()
        {
            // Load System.Runtime
            return Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName));
        }

        private static Assembly GetGetCallingAssembly()
        {
            return Assembly.GetCallingAssembly();
        }
    }

    public struct PublicStruct { }

    public class AssemblyPublicClass
    {
        public class PublicNestedClass { }
    }

    public class AssemblyGenericPublicClass<T> { }
    internal class AssemblyInternalClass { }

    public class AssemblyClassWithPrivateCtor
    {
        private AssemblyClassWithPrivateCtor() { }
    }

    public class AssemblyClassWithNoDefaultCtor
    {
        public AssemblyClassWithNoDefaultCtor(int x) { }
    }
}

internal class G<T> { }
