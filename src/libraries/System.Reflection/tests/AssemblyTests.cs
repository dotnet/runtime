// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Tests;
using System.Runtime.CompilerServices;
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

namespace System.Reflection.Tests
{
    public class AssemblyTests
    {
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
        [InlineData("NoSuchFile", false)]
        public void EmbeddedFiles(string resource, bool exists)
        {
            string[] resources = Helpers.ExecutingAssembly.GetManifestResourceNames();
            Stream resourceStream = Helpers.ExecutingAssembly.GetManifestResourceStream(resource);

            Assert.Equal(exists, resources.Contains(resource));
            Assert.Equal(exists, resourceStream != null);
        }

        [Fact]
        public void EntryPoint_ExecutingAssembly_IsNull()
        {
            Assert.Null(Helpers.ExecutingAssembly.EntryPoint);
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), true };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), Helpers.ExecutingAssembly, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(Assembly assembly1, Assembly assembly2, bool expected)
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
        public void GetEntryAssembly()
        {
            Assert.NotNull(Assembly.GetEntryAssembly());
            Assert.StartsWith("xunit.console.netcore", Assembly.GetEntryAssembly().ToString(), StringComparison.OrdinalIgnoreCase);
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
        public void GetHashCode(Assembly assembly)
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
        public void GetType(string name, bool exists)
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

        [Theory]
        [MemberData(nameof(Load_TestData))]
        public void Load(AssemblyName assemblyRef)
        {
            Assert.NotNull(Assembly.Load(assemblyRef));
        }

        [Fact]
        public void Load_Invalid()
        {
            Assert.Throws<ArgumentNullException>("assemblyRef", () => Assembly.Load(null)); // AssemblyRef is null
            Assert.Throws<FileNotFoundException>(() => Assembly.Load(new AssemblyName("no such assembly"))); // No such assembly
        }

        [Fact]
        public void Location_ExecutingAssembly_IsNotNull()
        {
            // This test applies on all platforms including .NET Native. Location must at least be non-null (it can be empty).
            // System.Reflection.CoreCLR.Tests adds tests that expect more than that.
            Assert.NotNull(Helpers.ExecutingAssembly.Location);
        }

        [Fact]
        public void CodeBase()
        {
            Assert.NotEmpty(Helpers.ExecutingAssembly.CodeBase);
        }

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

        public IEnumerable<object[]> ToString_TestData()
        {
            yield return new object[] { Helpers.ExecutingAssembly, "System.Reflection.Tests" };
            yield return new object[] { Assembly.Load(new AssemblyName(typeof(List<int>).GetTypeInfo().Assembly.FullName)), "PublicKeyToken=" };
        }

        [Theory]
        public void ToString(Assembly assembly, string expected)
        {
            Assert.Contains(expected, assembly.ToString());
            Assert.Equal(assembly.ToString(), assembly.FullName);
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
            return Assembly.Load(new AssemblyName(typeof(int).GetTypeInfo().Assembly.FullName)); ;
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
