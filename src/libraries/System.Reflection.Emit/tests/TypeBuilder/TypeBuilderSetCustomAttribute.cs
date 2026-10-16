// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetCustomAttribute
    {
        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);

            Type attributeType = typeof(TypeBuilderIntAttribute);
            ConstructorInfo attriubteConstructor = attributeType.GetConstructors()[0];
            FieldInfo attributeField = attributeType.GetField("Field12345");

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(attriubteConstructor, new object[] { 4 }, new FieldInfo[] { attributeField }, new object[] { "hello" });
            type.SetCustomAttribute(attribute);
            type.CreateType();

            object[] attributes = type.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);

            TypeBuilderIntAttribute obj = (TypeBuilderIntAttribute)attributes[0];
            Assert.Equal("hello", obj.Field12345);
            Assert.Equal(4, obj.m_ctorType2);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => type.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorInfo attributeConstructor = typeof(IntAllAttribute).GetConstructor(new Type[] { typeof(int) });
            type.SetCustomAttribute(attributeConstructor, new byte[] { 1, 0, 5, 0, 0, 0 });
            type.CreateType();

            object[] attributes = type.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);
            IntAllAttribute attribute = Assert.IsType<IntAllAttribute>(attributes[0]);
            Assert.Equal(5, attribute._i);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructor_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("con", () => type.SetCustomAttribute(null, new byte[0]));
        }

        // The decorated module (not the calling test assembly, and not ObjectAllAttribute's own assembly)
        // must be used to resolve an unqualified Type-tagged CA argument. A type visible only inside the
        // dynamic module proves that; RunAndCollect additionally exercises a collectible-assembly lookup,
        // with a GC interleaved to confirm the resolved type/module stay reachable through materialization.
        [Theory]
        [InlineData(AssemblyBuilderAccess.Run)]
        [InlineData(AssemblyBuilderAccess.RunAndCollect)]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_TypeArgument_ResolvesAgainstDecoratedModule(AssemblyBuilderAccess access)
        {
            WeakReference assembly = MaterializeTypeArgument(access);
            if (access == AssemblyBuilderAccess.RunAndCollect && PlatformDetection.IsCoreCLR)
            {
                for (int i = 0; i < 10 && assembly.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                Assert.False(assembly.IsAlive);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference MaterializeTypeArgument(AssemblyBuilderAccess access)
        {
            ModuleBuilder module = Helpers.DynamicAssembly(access: access).DefineDynamicModule("DynamicModule");

            const string MarkerTypeName = "Ca_Managed_Test.OnlyInThisDynamicModuleMarkerType";
            module.DefineType(MarkerTypeName, TypeAttributes.Public).CreateType();

            TypeBuilder taggedType = module.DefineType("Ca_Managed_Test.TypeArgumentHolder", TypeAttributes.Public);

            ConstructorInfo attributeConstructor = typeof(ObjectAllAttribute).GetConstructor(new[] { typeof(object) });
            byte[] blob = CustomAttributeBlob.Concat(
                CustomAttributeBlob.Prolog,
                new byte[] { CustomAttributeBlob.TagType },
                CustomAttributeBlob.PackedString(MarkerTypeName),
                CustomAttributeBlob.U2(0));
            taggedType.SetCustomAttribute(attributeConstructor, blob);
            taggedType.CreateType();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            ObjectAllAttribute attribute = (ObjectAllAttribute)taggedType.GetCustomAttributes(false).Single();
            Type resolvedType = (Type)attribute._o;
            Assert.Equal(MarkerTypeName, resolvedType.FullName);
            Assert.Equal(module.Assembly.FullName, resolvedType.Assembly.FullName);
            return new WeakReference(resolvedType.Assembly);
        }

        [Fact]
        public void SetCustomAttribute()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            ConstructorInfo constructor = typeof(TypeBuilderStringAttribute).GetConstructor(new Type[] { typeof(string) });
            CustomAttributeBuilder cuatbu = new CustomAttributeBuilder(constructor, new object[] { "hello" });
            type.SetCustomAttribute(cuatbu);

            type.CreateType();

            object[] attributes = type.GetCustomAttributes(false).ToArray();
            Assert.Equal(1, attributes.Length);
            Assert.True(attributes[0] is TypeBuilderStringAttribute);
            Assert.Equal("hello", ((TypeBuilderStringAttribute)attributes[0]).Creator);
        }

        public class TypeBuilderStringAttribute : Attribute
        {
            private string _creator;
            public string Creator { get { return _creator; } }

            public TypeBuilderStringAttribute(string name)
            {
                _creator = name;
            }
        }

        public class TypeBuilderIntAttribute : Attribute
        {
            public TypeBuilderIntAttribute(int mc)
            {
                m_ctorType2 = mc;
            }

            public string Field12345;
            public int m_ctorType2;
        }

    }
}
