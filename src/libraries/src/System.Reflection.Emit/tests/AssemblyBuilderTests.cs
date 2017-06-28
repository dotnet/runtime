// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class BoolAllAttribute : Attribute
    {
        private bool _s;
        public BoolAllAttribute(bool s) { _s = s; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IntClassAttribute : Attribute
    {
        public int i;
        public IntClassAttribute(int i) { this.i = i; }
    }

    public class AssemblyTests
    {
        public static IEnumerable<object[]> DefineDynamicAssembly_TestData()
        {
            foreach (AssemblyBuilderAccess access in new AssemblyBuilderAccess[] { AssemblyBuilderAccess.Run, AssemblyBuilderAccess.RunAndCollect })
            {
                yield return new object[] { new AssemblyName("TestName") { Version = new Version(0, 0, 0, 0) }, access };
                yield return new object[] { new AssemblyName("testname") { Version = new Version(1, 2, 3, 4) }, access };
                yield return new object[] { new AssemblyName("class") { Version = new Version(0, 0, 0, 0) }, access };
                yield return new object[] { new AssemblyName("\uD800\uDC00") { Version = new Version(0, 0, 0, 0) }, access };
            }
        }

        [Theory]
        [MemberData(nameof(DefineDynamicAssembly_TestData))]
        public void DefineDynamicAssembly_AssemblyName_AssemblyBuilderAccess(AssemblyName name, AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, access);
            VerifyAssemblyBuilder(assembly, name, new CustomAttributeBuilder[0]);
        }

        public static IEnumerable<object[]> DefineDynamicAssembly_CustomAttributes_TestData()
        {
            foreach (object[] data in DefineDynamicAssembly_TestData())
            {
                yield return new object[] { data[0], data[1], null };
                yield return new object[] { data[0], data[1], new CustomAttributeBuilder[0] };

                ConstructorInfo constructor = typeof(IntClassAttribute).GetConstructor(new Type[] { typeof(int) });
                yield return new object[] { data[0], data[1], new CustomAttributeBuilder[] { new CustomAttributeBuilder(constructor, new object[] { 10 }) } };
            }
        }

        [Theory]
        [MemberData(nameof(DefineDynamicAssembly_CustomAttributes_TestData))]
        public void DefineDynamicAssembly_AssemblyName_AssemblyBuilderAccess_CustomAttributeBuilder(AssemblyName name, AssemblyBuilderAccess access, IEnumerable<CustomAttributeBuilder> attributes)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, access, attributes);
            VerifyAssemblyBuilder(assembly, name, attributes);
        }

        [Fact]
        public void DefineDynamicAssembly_NullName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => AssemblyBuilder.DefineDynamicAssembly(null, AssemblyBuilderAccess.Run));
            AssertExtensions.Throws<ArgumentNullException>("name", () => AssemblyBuilder.DefineDynamicAssembly(null, AssemblyBuilderAccess.Run, new CustomAttributeBuilder[0]));
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The coreclr doesn't support Save or ReflectionOnly AssemblyBuilders.")]
        [InlineData((AssemblyBuilderAccess)2)] // Save (not supported)
        [InlineData((AssemblyBuilderAccess)2 | AssemblyBuilderAccess.Run)] // RunAndSave (not supported)
        [InlineData((AssemblyBuilderAccess)6)] // ReflectionOnly (not supported)
        public void DefineDynamicAssembly_CoreclrNotSupportedAccess_ThrowsArgumentException(AssemblyBuilderAccess access)
        {
            DefineDynamicAssembly_InvalidAccess_ThrowsArgumentException(access);
        }

        [Theory]
        [InlineData((AssemblyBuilderAccess)(-1))]
        [InlineData((AssemblyBuilderAccess)0)]
        [InlineData((AssemblyBuilderAccess)10)]
        [InlineData((AssemblyBuilderAccess)int.MaxValue)]
        public void DefineDynamicAssembly_InvalidAccess_ThrowsArgumentException(AssemblyBuilderAccess access)
        {
            AssertExtensions.Throws<ArgumentException>("access", () => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), access));
            AssertExtensions.Throws<ArgumentException>("access", () => AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Name"), access, new CustomAttributeBuilder[0]));
        }

        [Fact]
        public void DefineDynamicAssembly_NameIsCopy()
        {
            AssemblyName name = new AssemblyName("Name") { Version = new Version(0, 0, 0, 0) };
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
            Assert.StartsWith(name.ToString(), assembly.FullName);

            name.Name = "NewName";
            Assert.False(assembly.FullName.StartsWith(name.ToString()));
        }

        public static IEnumerable<object[]> DefineDynamicModule_TestData()
        {
            yield return new object[] { "Module" };
            yield return new object[] { "module" };
            yield return new object[] { "class" };
            yield return new object[] { "\uD800\uDC00" };
            yield return new object[] { new string('a', 259) };
        }

        [Theory]
        [MemberData(nameof(DefineDynamicModule_TestData))]
        public void DefineDynamicModule(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule(name);

            Assert.Equal(assembly, module.Assembly);
            Assert.Empty(module.CustomAttributes);

            Assert.Equal("<In Memory Module>", module.Name);

            // The coreclr ignores the name passed to AssemblyBuilder.DefineDynamicModule
            if (PlatformDetection.IsFullFramework)
            {
                Assert.Equal(name, module.FullyQualifiedName);
            }
            else
            {
                Assert.Equal("RefEmit_InMemoryManifestModule", module.FullyQualifiedName);
            }

            Assert.Equal(module, assembly.GetDynamicModule(module.FullyQualifiedName));
        }

        [Fact]
        public void DefineDynamicModule_NullName_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("name", () => assembly.DefineDynamicModule(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0test")]
        public void DefineDynamicModule_InvalidName_ThrowsArgumentException(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentException>("name", () => assembly.DefineDynamicModule(name));
        }

        [Fact]
        [SkipOnTargetFramework(~TargetFrameworkMonikers.NetFramework, "The coreclr only supports AssemblyBuilders with one module.")]
        public void DefineDynamicModule_NetFxModuleAlreadyDefined_ThrowsInvalidOperationException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.DefineDynamicModule("module1");
            assembly.DefineDynamicModule("module2");
            AssertExtensions.Throws<ArgumentException>(null, () => assembly.DefineDynamicModule("module1"));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The coreclr only supports AssemblyBuilders with one module.")]
        public void DefineDynamicModule_CoreFxModuleAlreadyDefined_ThrowsInvalidOperationException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.DefineDynamicModule("module1");
            Assert.Throws<InvalidOperationException>(() => assembly.DefineDynamicModule("module1"));
            Assert.Throws<InvalidOperationException>(() => assembly.DefineDynamicModule("module2"));
        }

        [Fact]
        public void GetManifestResourceNames_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceNames());
        }

        [Fact]
        public void GetManifestResourceStream_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceStream(""));
        }

        [Fact]
        public void GetManifestResourceInfo_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.GetManifestResourceInfo(""));
        }

        [Fact]
        public void ExportedTypes_ThrowsNotSupportedException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws<NotSupportedException>(() => assembly.ExportedTypes);
            Assert.Throws<NotSupportedException>(() => assembly.GetExportedTypes());
        }

        [Theory]
        [InlineData("testmodule")]
        [InlineData("\0test")]
        public void GetDynamicModule_NoSuchModule_ReturnsNull(string name)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.DefineDynamicModule("TestModule");

            Assert.Null(assembly.GetDynamicModule(name));
        }

        [Fact]
        public void GetDynamicModule_InvalidName_ThrowsArgumentException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("name", () => assembly.GetDynamicModule(null));
            AssertExtensions.Throws<ArgumentException>("name", () => assembly.GetDynamicModule(""));
        }

        [Theory]
        [InlineData(AssemblyBuilderAccess.Run)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect)]
        public void SetCustomAttribute_ConstructorBuidler_ByteArray(AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(access: access);
            ConstructorInfo constructor = typeof(BoolAllAttribute).GetConstructor(new Type[] { typeof(bool) });
            assembly.SetCustomAttribute(constructor, new byte[] { 1, 0, 1 });

            IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();
            Assert.IsType<BoolAllAttribute>(attributes.First());
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuidler_ByteArray_NullConstructorBuilder_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("con", () => assembly.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuidler_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => assembly.SetCustomAttribute(constructor, null));
        }

        [Theory]
        [InlineData(AssemblyBuilderAccess.Run)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect)]
        public void SetCustomAttribute_CustomAttributeBuilder(AssemblyBuilderAccess access)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(access: access);
            ConstructorInfo constructor = typeof(IntClassAttribute).GetConstructor(new Type[] { typeof(int) });
            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(constructor, new object[] { 5 });
            assembly.SetCustomAttribute(attributeBuilder);

            IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();
            Assert.IsType<IntClassAttribute>(attributes.First());
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullAttributeBuilder_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => assembly.SetCustomAttribute(null));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly(name: "Name1");
            yield return new object[] { assembly, assembly, true };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name1"), false };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name2"), false };
            yield return new object[] { assembly, Helpers.DynamicAssembly("Name1", access: AssemblyBuilderAccess.RunAndCollect), false };

            yield return new object[] { assembly, new object(), false };
            yield return new object[] { assembly, null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public void Equals(AssemblyBuilder assembly, object obj, bool expected)
        {
            Assert.Equal(expected, assembly.Equals(obj));
            if (obj is AssemblyBuilder)
            {
                Assert.Equal(expected, assembly.GetHashCode().Equals(obj.GetHashCode()));
            }
        }

        public static void VerifyAssemblyBuilder(AssemblyBuilder assembly, AssemblyName name, IEnumerable<CustomAttributeBuilder> attributes)
        {
            Assert.StartsWith(name.ToString(), assembly.FullName);
            Assert.StartsWith(name.ToString(), assembly.GetName().ToString());

            Assert.True(assembly.IsDynamic);

            Assert.Equal(attributes?.Count() ?? 0, assembly.CustomAttributes.Count());

            Assert.Equal(1, assembly.Modules.Count());
            Module module = assembly.Modules.First();
            Assert.NotEmpty(module.Name);
            Assert.Equal(assembly.Modules, assembly.GetModules());

            Assert.Empty(assembly.DefinedTypes);
            Assert.Empty(assembly.GetTypes());
        }
    }
}
