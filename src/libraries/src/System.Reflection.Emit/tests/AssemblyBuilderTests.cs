// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
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
        [Fact]
        public void DefineDynamicModule()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("Module1");
            TypeBuilder type = module.DefineType("HelloWorld", TypeAttributes.Public);
            ConstructorBuilder constructor = type.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            ILGenerator ilGenerator = constructor.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ldarg_1);
        }

        [Fact]
        public void DefineDynamicModule_LargeName()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule(new string('a', 259));
        }

        [Theory]
        [InlineData(null, typeof(ArgumentNullException))]
        [InlineData("", typeof(ArgumentException))]
        [InlineData("\0test", typeof(ArgumentException))]
        public void DefineDyamicModule_InvalidName_ThrowsArgumentException(string name, Type exceptionType)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            Assert.Throws(exceptionType, () => assembly.DefineDynamicModule(name));
        }

        [Fact]
        public void DefineDyamicModule_ModuleAlreadyDefined_ThrowsInvalidOperationException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder mb = assembly.DefineDynamicModule("module1");
            Assert.Throws<InvalidOperationException>(() => { ModuleBuilder mb2 = assembly.DefineDynamicModule("module2"); });
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
        public void SetCustomAttribute_ConstructorBuidler_ByteArray()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("PT1"), AssemblyBuilderAccess.Run);
            ConstructorInfo constructor = typeof(BoolAllAttribute).GetConstructor(new Type[] { typeof(bool) });
            assembly.SetCustomAttribute(constructor, new byte[] { 01, 00, 01 });

            IEnumerable<Attribute> attributes = assembly.GetCustomAttributes();
            Assert.IsType<BoolAllAttribute>(attributes.First());
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuidler_ByteArray_NullConstructorBuilder_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NT1"), AssemblyBuilderAccess.Run);
            Assert.Throws<ArgumentNullException>("con", () => assembly.SetCustomAttribute(null, new byte[0]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorBuidler_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ConstructorInfo constructor = typeof(DateTime).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("con", () => assembly.SetCustomAttribute(constructor, null));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
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
            Assert.Throws<ArgumentNullException>("customBuilder", () => assembly.SetCustomAttribute(null));
        }
    }
}
