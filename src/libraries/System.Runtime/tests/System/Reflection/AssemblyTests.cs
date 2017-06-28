// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using Xunit;

[assembly: System.Reflection.CustomAttributesTests.Data.Attr(77, name = "AttrSimple")]
[assembly: System.Reflection.CustomAttributesTests.Data.Int32Attr(77, name = "Int32AttrSimple"),
System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = "Int64AttrSimple"),
System.Reflection.CustomAttributesTests.Data.StringAttr("hello", name = "StringAttrSimple"),
System.Reflection.CustomAttributesTests.Data.EnumAttr(System.Reflection.CustomAttributesTests.Data.MyColorEnum.RED, name = "EnumAttrSimple"),
System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(Object), name = "TypeAttrSimple")]

[assembly: System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]
[assembly: System.Diagnostics.Debuggable((System.Diagnostics.DebuggableAttribute.DebuggingModes)263)]
[assembly: System.CLSCompliant(false)]

namespace System.Reflection.Tests
{
    public class AssemblyTests : FileCleanupTestBase
    {
        private string SourceTestAssemblyPath { get; } = Path.Combine(Environment.CurrentDirectory, "TestAssembly.dll");
        private string DestTestAssemblyPath { get; }
        private string LoadFromTestPath { get; }

        public AssemblyTests() 
        {
            // Assembly.Location not supported (properly) on uapaot.
            DestTestAssemblyPath = Path.Combine(base.TestDirectory, "TestAssembly.dll");
            LoadFromTestPath = Path.Combine(base.TestDirectory, "System.Runtime.Tests.dll");

            // There is no dll to copy in ILC runs
            if (!PlatformDetection.IsNetNative)
            {
                File.Copy(SourceTestAssemblyPath, DestTestAssemblyPath);
                string currAssemblyPath = Path.Combine(Environment.CurrentDirectory, "System.Runtime.Tests.dll");
                File.Copy(currAssemblyPath, LoadFromTestPath, true);
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "GetCallingAssembly() is not supported on UapAot")]
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
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.GetSatelliteAssembly() not supported on UapAot")]
        public void GetSatelliteAssemblyNeg()
        {
            Assert.Throws<ArgumentNullException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(null)));
            Assert.Throws<System.IO.FileNotFoundException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(CultureInfo.InvariantCulture)));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.Load(String) not supported on UapAot")]
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
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.Load(byte[]) not supported on UapAot")]
        public void AssemblyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);

            Assembly loadedAssembly = Assembly.Load(aBytes);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.Load(byte[]) not supported on UapAot")]
        public void AssemblyLoadFromBytesNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((byte[])null));
            Assert.Throws<BadImageFormatException>(() => Assembly.Load(new byte[0]));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.Load(byte[]) not supported on UapAot")]
        public void AssemblyLoadFromBytesWithSymbols()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);
            byte[] symbols = System.IO.File.ReadAllBytes((System.IO.Path.ChangeExtension(assembly.Location, ".pdb")));

            Assembly loadedAssembly = Assembly.Load(aBytes, symbols);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.ReflectionOnlyLoad() not supported on UapAot")]
        public void AssemblyReflectionOnlyLoadFromString()
        {
            AssemblyName an = typeof(AssemblyTests).Assembly.GetName();
            Assert.Throws<NotSupportedException>(() => Assembly.ReflectionOnlyLoad(an.FullName));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.ReflectionOnlyLoad() not supported on UapAot")]
        public void AssemblyReflectionOnlyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);
            Assert.Throws<NotSupportedException>(() => Assembly.ReflectionOnlyLoad(aBytes));
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.ReflectionOnlyLoad() not supported on UapAot")]
        public void AssemblyReflectionOnlyLoadFromNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.ReflectionOnlyLoad((string)null));
            AssertExtensions.Throws<ArgumentException>(null, () => Assembly.ReflectionOnlyLoad(string.Empty));

            Assert.Throws<ArgumentNullException>(() => Assembly.ReflectionOnlyLoad((byte[])null));
        }

        public static IEnumerable<object[]> GetModules_TestData()
        {
            yield return new object[] { LoadSystemCollectionsAssembly() };
            yield return new object[] { LoadSystemReflectionAssembly() };
        }

        [Theory]
        [MemberData(nameof(GetModules_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.GetModules() is not supported on UapAot.")]
        public void GetModules_GetModule(Assembly assembly)
        {
            Assert.NotEmpty(assembly.GetModules());
            foreach (Module module in assembly.GetModules())
            {
                Assert.Equal(module, assembly.GetModule(module.ToString()));
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.GetLoadedModules() is not supported on UapAot.")]
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

        public static IEnumerable<object[]> CreateInstance_TestData()
        {
            yield return new object[] { typeof(AssemblyTests).Assembly, typeof(AssemblyPublicClass).FullName, BindingFlags.CreateInstance, typeof(AssemblyPublicClass) };
            yield return new object[] { typeof(int).Assembly, typeof(int).FullName, BindingFlags.Default, typeof(int) };
            yield return new object[] { typeof(int).Assembly, typeof(Dictionary<int, string>).FullName, BindingFlags.Default, typeof(Dictionary<int, string>) };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_TestData))]
        public void CreateInstance(Assembly assembly, string typeName, BindingFlags bindingFlags, Type expectedType)
        {
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, true, bindingFlags, null, null, null, null));
            Assert.IsType(expectedType, assembly.CreateInstance(typeName, false, bindingFlags, null, null, null, null));
        }

        public static IEnumerable<object[]> CreateInstance_Invalid_TestData()
        {
            yield return new object[] { "", typeof(ArgumentException) };
            yield return new object[] { null, typeof(ArgumentNullException) };
            yield return new object[] { typeof(AssemblyClassWithPrivateCtor).FullName, typeof(MissingMethodException) };
        }

        [Theory]
        [MemberData(nameof(CreateInstance_Invalid_TestData))]
        public void CreateInstance_Invalid(string typeName, Type exceptionType)
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, true, BindingFlags.Public, null, null, null, null));
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, false, BindingFlags.Public, null, null, null, null));
        }

        [Fact]
        public void GetManifestResourceStream()
        {
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "EmbeddedImage.png"));
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "EmbeddedTextFile.txt"));
            Assert.Null(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "IDontExist"));
        }

        [Fact]
        public void Test_GlobalAssemblyCache()
        {
            Assert.False(typeof(AssemblyTests).Assembly.GlobalAssemblyCache);
        }

        [Fact]
        public void Test_HostContext()
        {
            Assert.Equal(0, typeof(AssemblyTests).Assembly.HostContext);
        }

        [Fact]
        public void Test_IsFullyTrusted()
        {
            Assert.True(typeof(AssemblyTests).Assembly.IsFullyTrusted);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The full .NET Framework supports SecurityRuleSet")]
        public void Test_SecurityRuleSet_Netcore()
        {
            Assert.Equal(SecurityRuleSet.None, typeof(AssemblyTests).Assembly.SecurityRuleSet);
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "SecurityRuleSet is ignored in .NET Core")]
        public void Test_SecurityRuleSet_Netfx()
        {
            Assert.Equal(SecurityRuleSet.Level2, typeof(AssemblyTests).Assembly.SecurityRuleSet);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.LoadFile() not supported on UapAot")]
        public void Test_LoadFile()
        {
            Assembly currentAssembly = typeof(AssemblyTests).Assembly;
            const string RuntimeTestsDll = "System.Runtime.Tests.dll";
            string fullRuntimeTestsPath = Path.GetFullPath(RuntimeTestsDll);

            var loadedAssembly1 = Assembly.LoadFile(fullRuntimeTestsPath);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Equal(currentAssembly, loadedAssembly1);
            }
            else
            {
                Assert.NotEqual(currentAssembly, loadedAssembly1);
            }

            string dir = Path.GetDirectoryName(fullRuntimeTestsPath);
            fullRuntimeTestsPath = Path.Combine(dir, ".", RuntimeTestsDll);

            Assembly loadedAssembly2 = Assembly.LoadFile(fullRuntimeTestsPath);
            if (PlatformDetection.IsFullFramework)
            {
                Assert.NotEqual(loadedAssembly1, loadedAssembly2);
            }
            else
            {
                Assert.Equal(loadedAssembly1, loadedAssembly2);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.Uap, "The full .NET Framework has a bug and throws a NullReferenceException")]
        public void LoadFile_NullPath_Netcore_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Assembly.LoadFile(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Uap, "Assembly.LoadFile() not supported on UapAot")]
        public void LoadFile_NoSuchPath_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Assembly.LoadFile("System.Runtime.Tests.dll"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.UapAot, "The full .NET Framework supports Assembly.LoadFrom")]
        public void Test_LoadFromUsingHashValue_Netcore()
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom("abc", null, System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The implementation of Assembly.LoadFrom is stubbed out in .NET Core")]
        public void Test_LoadFromUsingHashValue_Netfx()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.LoadFrom("abc", null, System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.UapAot, "The full .NET Framework supports more than one module per assembly")]
        public void Test_LoadModule_Netcore()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule("abc", null));
            Assert.Throws<NotImplementedException>(() => assembly.LoadModule("abc", null, null));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The coreclr doesn't support more than one module per assembly")]
        public void Test_LoadModule_Netfx()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            AssertExtensions.Throws<ArgumentNullException>(null, () => assembly.LoadModule("abc", null));
            AssertExtensions.Throws<ArgumentNullException>(null, () => assembly.LoadModule("abc", null, null));
        }

#pragma warning disable 618
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFromWithPartialName() not supported on UapAot")]
        public void Test_LoadWithPartialName()
        {
            string simplename = typeof(AssemblyTests).Assembly.GetName().Name;
            var assem = Assembly.LoadWithPartialName(simplename);
            Assert.Equal(typeof(AssemblyTests).Assembly, assem);
        }        
#pragma warning restore 618

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() not supported on UapAot")]
        public void LoadFrom_SamePath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.LoadFrom(DestTestAssemblyPath);
            Assembly assembly2 = Assembly.LoadFrom(DestTestAssemblyPath);
            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() not supported on UapAot")]
        public void LoadFrom_SameIdentityAsAssemblyWithDifferentPath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.LoadFrom(typeof(AssemblyTests).Assembly.Location);
            Assert.Equal(assembly1, typeof(AssemblyTests).Assembly);

            Assembly assembly2 = Assembly.LoadFrom(LoadFromTestPath);

            if (PlatformDetection.IsFullFramework)
            {
                Assert.NotEqual(assembly1, assembly2);
            }
            else
            {
                Assert.Equal(assembly1, assembly2);
            }
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() not supported on UapAot")]
        public void LoadFrom_NullAssemblyFile_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => Assembly.LoadFrom(null));
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => Assembly.UnsafeLoadFrom(null));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() not supported on UapAot")]
        public void LoadFrom_EmptyAssemblyFile_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, (() => Assembly.LoadFrom("")));
            AssertExtensions.Throws<ArgumentException>("path", null, (() => Assembly.UnsafeLoadFrom("")));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.LoadFrom() not supported on UapAot")]
        public void LoadFrom_NoSuchFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Assembly.LoadFrom("NoSuchPath"));
            Assert.Throws<FileNotFoundException>(() => Assembly.UnsafeLoadFrom("NoSuchPath"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.UnsafeLoadFrom() not supported on UapAot")]
        public void UnsafeLoadFrom_SamePath_ReturnsEqualAssemblies()
        {
            Assembly assembly1 = Assembly.UnsafeLoadFrom(DestTestAssemblyPath);
            Assembly assembly2 = Assembly.UnsafeLoadFrom(DestTestAssemblyPath);
            Assert.Equal(assembly1, assembly2);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework | TargetFrameworkMonikers.UapAot, "The implementation of LoadFrom(string, byte[], AssemblyHashAlgorithm is not supported in .NET Core.")]
        public void LoadFrom_WithHashValue_NetCoreCore_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom(DestTestAssemblyPath, new byte[0], Configuration.Assemblies.AssemblyHashAlgorithm.None));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.GetFile() not supported on UapAot")]
        public void GetFile()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(AssemblyTests).Assembly.GetFile(null));
            AssertExtensions.Throws<ArgumentException>(null, () => typeof(AssemblyTests).Assembly.GetFile(""));
            Assert.Null(typeof(AssemblyTests).Assembly.GetFile("NonExistentfile.dll"));
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetFile("System.Runtime.Tests.dll"));
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFile("System.Runtime.Tests.dll").Name, typeof(AssemblyTests).Assembly.Location);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.GetFiles() not supported on UapAot")]
        public void GetFiles()
        {
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetFiles());
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFiles().Length, 1);
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFiles()[0].Name, typeof(AssemblyTests).Assembly.Location);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.UapAot, "Assembly.CodeBase not supported on UapAot")]
        public void Load_AssemblyNameWithCodeBase()
        {
            AssemblyName an = typeof(AssemblyTests).Assembly.GetName();
            Assert.NotNull(an.CodeBase); 
            Assembly a = Assembly.Load(an);
            Assert.Equal(a, typeof(AssemblyTests).Assembly);
        }

        // Helpers
        private static Assembly GetGetCallingAssembly()
        {
            return Assembly.GetCallingAssembly();
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

        public class AssemblyPublicClass
        {
            public class PublicNestedClass { }
        }

        private static class AssemblyPrivateClass { }

        public class AssemblyClassWithPrivateCtor
        {
            private AssemblyClassWithPrivateCtor() { }
        }
    }

    public class AssemblyCustomAttributeTest
    {
        [Fact]
        public void Test_Int32AttrSimple()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int32Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int32Attr((Int32)77, name = \"Int32AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_Int64Attr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Int64Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Int64Attr((Int64)77, name = \"Int64AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_StringAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.StringAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.StringAttr(\"hello\", name = \"StringAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_EnumAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.EnumAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.EnumAttr((System.Reflection.CustomAttributesTests.Data.MyColorEnum)1, name = \"EnumAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_TypeAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.TypeAttr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.TypeAttr(typeof(System.Object), name = \"TypeAttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_CompilationRelaxationsAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Runtime.CompilerServices.CompilationRelaxationsAttribute);
            string attrstr = "[System.Runtime.CompilerServices.CompilationRelaxationsAttribute((Int32)8)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_AssemblyIdentityAttr()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyTitleAttribute);
            string attrstr = "[System.Reflection.AssemblyTitleAttribute(\"System.Reflection.Tests\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_AssemblyDescriptionAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyDescriptionAttribute);
            string attrstr = "[System.Reflection.AssemblyDescriptionAttribute(\"System.Reflection.Tests\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_AssemblyCompanyAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.AssemblyCompanyAttribute);
            string attrstr = "[System.Reflection.AssemblyCompanyAttribute(\"Microsoft Corporation\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_CLSCompliantAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.CLSCompliantAttribute);
            string attrstr = "[System.CLSCompliantAttribute((Boolean)True)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_DebuggableAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Diagnostics.DebuggableAttribute);
            string attrstr = "[System.Diagnostics.DebuggableAttribute((System.Diagnostics.DebuggableAttribute+DebuggingModes)263)]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        [Fact]
        public void Test_SimpleAttribute()
        {
            bool result = false;
            Type attrType = typeof(System.Reflection.CustomAttributesTests.Data.Attr);
            string attrstr = "[System.Reflection.CustomAttributesTests.Data.Attr((Int32)77, name = \"AttrSimple\")]";
            result = VerifyCustomAttribute(attrType, attrstr);

            Assert.True(result, string.Format("Did not find custom attribute of type {0} ", attrType));
        }

        private bool VerifyCustomAttribute(Type type, String attributeStr)
        {
            Assembly asm = typeof(AssemblyCustomAttributeTest).Assembly;

            foreach (CustomAttributeData cad in asm.GetCustomAttributesData())
            {
                if (cad.AttributeType.Equals(type))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class AssemblyTests_GetTYpe
    {
        [Fact]
        public void AssemblyGetTypeNoQualifierAllowed()
        {
            Assembly a = typeof(G<int>).Assembly;
            string s = typeof(G<int>).AssemblyQualifiedName;
            AssertExtensions.Throws<ArgumentException>(null, () => a.GetType(s, throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void AssemblyGetTypeDoesntSearchMscorlib()
        {
            Assembly a = typeof(AssemblyTests_GetTYpe).Assembly;
            Assert.Throws<TypeLoadException>(() => a.GetType("System.Object", throwOnError: true, ignoreCase: false));
            Assert.Throws<TypeLoadException>(() => a.GetType("G`1[[System.Object]]", throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public void AssemblyGetTypeDefaultsToItself()
        {
            Assembly a = typeof(AssemblyTests_GetTYpe).Assembly;
            Type t = a.GetType("G`1[[G`1[[System.Int32, mscorlib]]]]", throwOnError: true, ignoreCase: false);
            Assert.Equal(typeof(G<G<int>>), t);
        }
    }
}

internal class G<T> { }
