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
    public class AssemblyTests : IDisposable
    {

        string sourceTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestAssembly.dll");
        string destTestAssemblyPath = Path.Combine(Environment.CurrentDirectory, "TestAssembly", "TestAssembly.dll");

        public AssemblyTests()
        {
            // Move TestAssembly.dll to subfolder TestAssembly
            if(!File.Exists(destTestAssemblyPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destTestAssemblyPath));
                File.Move(sourceTestAssemblyPath, destTestAssemblyPath);
            }
        }

        public void Dispose()
        {
            // Revert TestAssembly.dll back to its previous location
            if(!File.Exists(sourceTestAssemblyPath))
                File.Move(destTestAssemblyPath, sourceTestAssemblyPath);
        }

        public static IEnumerable<object[]> Equality_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), typeof(AssemblyTests).Assembly, false };
        }

        [Theory]
        [MemberData(nameof(Equality_TestData))]
        public static void Equality(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1 == assembly2);
            Assert.NotEqual(expected, assembly1 != assembly2);
        }

        [Fact]
        public static void GetAssembly_Nullery()
        {
            Assert.Throws<ArgumentNullException>("type", () => Assembly.GetAssembly(null));
        }

        public static IEnumerable<object[]> GetAssembly_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(HashSet<int>).GetTypeInfo().Assembly.FullName)), Assembly.GetAssembly(typeof(HashSet<int>)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.GetAssembly(typeof(int)), true };
            yield return new object[] { typeof(AssemblyTests).Assembly, Assembly.GetAssembly(typeof(AssemblyTests)), true };
        }

        [Theory]
        [MemberData(nameof(GetAssembly_TestData))]
        public static void GetAssembly(Assembly assembly1, Assembly assembly2, bool expected)
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
        public static void GetCallingAssembly(Assembly assembly1, Assembly assembly2, bool expected)
        {
            Assert.Equal(expected, assembly1.Equals(assembly2));
        }

        [Fact]
        public static void GetExecutingAssembly()
        {
            Assert.True(typeof(AssemblyTests).Assembly.Equals(Assembly.GetExecutingAssembly()));
        }

        [Fact]
        public static void GetSatelliteAssemblyNeg()
        {
            Assert.Throws<ArgumentNullException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(null)));
            Assert.Throws<System.IO.FileNotFoundException>(() => (typeof(AssemblyTests).Assembly.GetSatelliteAssembly(CultureInfo.InvariantCulture)));
        }

        [Fact]
        public static void AssemblyLoadFromString()
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
        public static void AssemblyLoadFromStringNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((string)null));
            Assert.Throws<ArgumentException>(() => Assembly.Load(string.Empty));
        
            string emptyCName = new string('\0', 1);
            Assert.Throws<ArgumentException>(() => Assembly.Load(emptyCName));
        }

        [Fact]
        public static void AssemblyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);

            Assembly loadedAssembly = Assembly.Load(aBytes);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [Fact]
        public static void AssemblyLoadFromBytesNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.Load((byte[])null));
            Assert.Throws<BadImageFormatException>(() => Assembly.Load(new byte[0]));
        }

        [Fact]
        public static void AssemblyLoadFromBytesWithSymbols()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);
            byte[] symbols = System.IO.File.ReadAllBytes((System.IO.Path.ChangeExtension(assembly.Location, ".pdb")));

            Assembly loadedAssembly = Assembly.Load(aBytes, symbols);
            Assert.NotNull(loadedAssembly);
            Assert.Equal(assembly.FullName, loadedAssembly.FullName);
        }

        [Fact]
        public static void AssemblyReflectionOnlyLoadFromString()
        {
            AssemblyName an = typeof(AssemblyTests).Assembly.GetName();

            Assembly a1 = Assembly.ReflectionOnlyLoad(an.FullName);
            Assert.NotNull(a1);
            Assert.Equal(an.FullName, a1.GetName().FullName);
        }

        [Fact]
        public static void AssemblyReflectionOnlyLoadFromBytes()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            byte[] aBytes = System.IO.File.ReadAllBytes(assembly.Location);

            Assembly a1 = Assembly.ReflectionOnlyLoad(aBytes);
            Assert.NotNull(a1);
            Assert.Equal(assembly.FullName, a1.GetName().FullName);
        }

        [Fact]
        public static void AssemblyReflectionOnlyLoadFromNeg()
        {
            Assert.Throws<ArgumentNullException>(() => Assembly.ReflectionOnlyLoad((string)null));
            Assert.Throws<ArgumentException>(() => Assembly.ReflectionOnlyLoad(string.Empty));

            string emptyCName = new string('\0', 1);
            Assert.Throws<ArgumentException>(() => Assembly.ReflectionOnlyLoad(emptyCName));

            Assert.Throws<ArgumentNullException>(() => Assembly.ReflectionOnlyLoad((byte[])null));
            Assert.Throws<BadImageFormatException>(() => Assembly.ReflectionOnlyLoad(new byte[0]));
        }

        public static IEnumerable<object[]> GetModules_TestData()
        {
            yield return new object[] { LoadSystemCollectionsAssembly() };
            yield return new object[] { LoadSystemReflectionAssembly() };
        }

        [Theory]
        [MemberData(nameof(GetModules_TestData))]
        public static void GetModules_GetModule(Assembly assembly)
        {
            Assert.NotEmpty(assembly.GetModules());
            foreach (Module module in assembly.GetModules())
            {
                Assert.NotNull(module);
                Assert.Equal(module, assembly.GetModule(module.Name));
            }
        }

        [Fact]
        public static void GetLoadedModules()
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.NotEmpty(assembly.GetLoadedModules());
            foreach (Module module in assembly.GetLoadedModules())
            {
                Assert.NotNull(module);
                Assert.Equal(module, assembly.GetModule(module.Name));
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
        public static void CreateInstance(Assembly assembly, string typeName, BindingFlags bindingFlags, Type expectedType)
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
        public static void CreateInstance_Invalid(string typeName, Type exceptionType)
        {
            Assembly assembly = typeof(AssemblyTests).Assembly;
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, true, BindingFlags.Public, null, null, null, null));
            Assert.Throws(exceptionType, () => assembly.CreateInstance(typeName, false, BindingFlags.Public, null, null, null, null));
        }

        [Fact]
        public static void GetManifestResourceStream()
        {
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "EmbeddedImage.png"));
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "EmbeddedTextFile.txt"));
            Assert.Null(typeof(AssemblyTests).Assembly.GetManifestResourceStream(typeof(AssemblyTests), "IDontExist"));
        }

        [Fact]
        public static void Test_GlobalAssemblyCache()
        {
            Assert.False(typeof(AssemblyTests).Assembly.GlobalAssemblyCache);
        }        

        [Fact]
        public static void Test_HostContext()
        {
            Assert.Equal(0, typeof(AssemblyTests).Assembly.HostContext);
        }        

        [Fact]
        public static void Test_IsFullyTrusted()
        {
            Assert.True(typeof(AssemblyTests).Assembly.IsFullyTrusted);
        }        

        [Fact]
        public static void Test_SecurityRuleSet()
        {
            Assert.Equal(SecurityRuleSet.None, typeof(AssemblyTests).Assembly.SecurityRuleSet);
        }        

        [Fact]
        public static void Test_LoadFile()
        {
            var assem = typeof(AssemblyTests).Assembly;
            string path = "System.Runtime.Tests.dll";
            string fullpath = Path.GetFullPath(path);
            Assert.Throws<ArgumentNullException>("path", () => Assembly.LoadFile(null));
            Assert.Throws<ArgumentException>(() => Assembly.LoadFile(path));
            var loadfile1 = Assembly.LoadFile(fullpath);
            Assert.NotEqual(assem, loadfile1);
            string dir = Path.GetDirectoryName(fullpath);
            fullpath = Path.Combine(dir, ".", path);
            var loadfile2 = Assembly.LoadFile(fullpath);
            Assert.Equal(loadfile1,loadfile2);
        }        

        [Fact]
        public static void Test_LoadFromUsingHashValue()
        {
            Assert.Throws<NotSupportedException>(() => Assembly.LoadFrom("abc", null, System.Configuration.Assemblies.AssemblyHashAlgorithm.SHA1));
        }        

        [Fact]
        public static void Test_LoadModule()
        {
            var assem = typeof(AssemblyTests).Assembly;
            Assert.Throws<NotImplementedException>(() => assem.LoadModule("abc", null));
            Assert.Throws<NotImplementedException>(() => assem.LoadModule("abc", null, null));
        }        

#pragma warning disable 618
        [Fact]
        public static void Test_LoadWithPartialName()
        {
            string simplename = typeof(AssemblyTests).Assembly.GetName().Name;
            var assem = Assembly.LoadWithPartialName(simplename);
            Assert.Equal(typeof(AssemblyTests).Assembly, assem);
        }        
#pragma warning restore 618

        [Fact]
        public void Test_LoadFrom()
        {
            var assem = Assembly.LoadFrom(destTestAssemblyPath);
            Assert.Throws<ArgumentNullException>("assemblyFile", () => Assembly.LoadFrom(null));
            var assem1 = Assembly.LoadFrom(destTestAssemblyPath);
            Assert.Equal(assem, assem1);
        }        

        [Fact]
        public void Test_UnsafeLoadFrom()
        {
            var assem = Assembly.UnsafeLoadFrom(destTestAssemblyPath);
            Assert.Throws<ArgumentNullException>("assemblyFile", () => Assembly.UnsafeLoadFrom(null));
        }        

        [Fact]
        public void GetFile()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(AssemblyTests).Assembly.GetFile(null));
            Assert.Throws<ArgumentException>(() => typeof(AssemblyTests).Assembly.GetFile(""));
            Assert.Null(typeof(AssemblyTests).Assembly.GetFile("NonExistentfile.dll"));
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetFile("System.Runtime.Tests.dll"));
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFile("System.Runtime.Tests.dll").Name, typeof(AssemblyTests).Assembly.Location);
        }

        [Fact]
        public void GetFiles()
        {
            Assert.NotNull(typeof(AssemblyTests).Assembly.GetFiles());
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFiles().Length, 1);
            Assert.Equal(typeof(AssemblyTests).Assembly.GetFiles()[0].Name, typeof(AssemblyTests).Assembly.Location);
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

        private static bool VerifyCustomAttribute(Type type, String attributeStr)
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

    public static class AssemblyTests_GetTYpe
    {
        [Fact]
        public static void AssemblyGetTypeNoQualifierAllowed()
        {
            Assembly a = typeof(G<int>).Assembly;
            string s = typeof(G<int>).AssemblyQualifiedName;
            Assert.Throws<ArgumentException>(() => a.GetType(s, throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public static void AssemblyGetTypeDoesntSearchMscorlib()
        {
            Assembly a = typeof(AssemblyTests_GetTYpe).Assembly;
            Assert.Throws<TypeLoadException>(() => a.GetType("System.Object", throwOnError: true, ignoreCase: false));
            Assert.Throws<TypeLoadException>(() => a.GetType("G`1[[System.Object]]", throwOnError: true, ignoreCase: false));
        }

        [Fact]
        public static void AssemblyGetTypeDefaultsToItself()
        {
            Assembly a = typeof(AssemblyTests_GetTYpe).Assembly;
            Type t = a.GetType("G`1[[G`1[[System.Int32, mscorlib]]]]", throwOnError: true, ignoreCase: false);
            Assert.Equal(typeof(G<G<int>>), t);
        }
    }
}

internal class G<T> { }