// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineType
    {
        public static IEnumerable<object[]> TestData()
        {
            foreach (string name in new string[] { "TestName", "testname", "class", "\uD800\uDC00", "a\0b\0c" })
            {
                foreach (TypeAttributes attributes in new TypeAttributes[] { TypeAttributes.NotPublic, TypeAttributes.Interface | TypeAttributes.Abstract, TypeAttributes.Class })
                {
                    foreach (Type parent in new Type[] { null, typeof(ModuleBuilderDefineType) })
                    {
                        foreach (PackingSize packingSize in new PackingSize[] { PackingSize.Unspecified, PackingSize.Size1 })
                        {
                            foreach (int size in new int[] { 0, -1, 1 })
                            {
                                yield return new object[] { name, attributes, parent, packingSize, size, new Type[0] };
                            }
                        }

                        yield return new object[] { name, attributes, parent, PackingSize.Unspecified, 0, null };
                        yield return new object[] { name, attributes, parent, PackingSize.Unspecified, 0, new Type[] { typeof(IComparable) } };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineType(string name, TypeAttributes attributes, Type parent, PackingSize packingSize, int typesize, Type[] implementedInterfaces)
        {
            bool isDefaultImplementedInterfaces = implementedInterfaces?.Length == 0;
            bool isDefaultPackingSize = packingSize == PackingSize.Unspecified;
            bool isDefaultSize = typesize == 0;
            bool isDefaultParent = parent == null;
            bool isDefaultAttributes = attributes == TypeAttributes.NotPublic;

            Action<TypeBuilder, Module> verify = (type, module) =>
            {
                Type baseType = attributes.HasFlag(TypeAttributes.Abstract) && parent == null ? null : (parent ?? typeof(object));
                Helpers.VerifyType(type, module, null, name, attributes, baseType, typesize, packingSize, implementedInterfaces);
            };

            if (isDefaultImplementedInterfaces)
            {
                if (isDefaultSize && isDefaultPackingSize)
                {
                    if (isDefaultParent)
                    {
                        if (isDefaultAttributes)
                        {
                            // Use DefineType(string)
                            ModuleBuilder module1 = Helpers.DynamicModule();
                            verify(module1.DefineType(name), module1);
                        }
                        // Use DefineType(string, TypeAttributes)
                        ModuleBuilder module2 = Helpers.DynamicModule();
                        verify(module2.DefineType(name, attributes), module2);
                    }
                    // Use DefineType(string, TypeAttributes, Type)
                    ModuleBuilder module3 = Helpers.DynamicModule();
                    verify(module3.DefineType(name, attributes, parent), module3);
                }
                else if (isDefaultSize)
                {
                    // Use DefineType(string, TypeAttributes, Type, PackingSize)
                    ModuleBuilder module4 = Helpers.DynamicModule();
                    verify(module4.DefineType(name, attributes, parent, packingSize), module4);
                }
                else if (isDefaultPackingSize)
                {
                    // Use DefineType(string, TypeAttributes, Type, int)
                    ModuleBuilder module5 = Helpers.DynamicModule();
                    verify(module5.DefineType(name, attributes, parent, typesize), module5);
                }
                // Use DefineType(string, TypeAttributes, Type, PackingSize, int)
                ModuleBuilder module6 = Helpers.DynamicModule();
                verify(module6.DefineType(name, attributes, parent, packingSize, typesize), module6);
            }
            else
            {
                // Use DefineType(string, TypeAttributes, Type, Type[])
                Assert.True(isDefaultSize && isDefaultPackingSize); // Sanity check
                ModuleBuilder module7 = Helpers.DynamicModule();
                verify(module7.DefineType(name, attributes, parent, implementedInterfaces), module7);
            }
        }

        [Fact]
        public void DefineType_String_TypeAttributes_Type_TypeCreatedInModule()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            TypeBuilder type1 = module.DefineType("TestType1");
            Type parent = type1.CreateTypeInfo().AsType();

            TypeBuilder type2 = module.DefineType("TestType2", TypeAttributes.NotPublic, parent);
            Type createdType = type2.CreateTypeInfo().AsType();
            Assert.Equal("TestType2", createdType.Name);
            Assert.Equal(TypeAttributes.NotPublic, createdType.GetTypeInfo().Attributes);
            Assert.Equal(parent, createdType.GetTypeInfo().BaseType);
        }

        [Fact]
        public void DefineType_NullName_ThrowsArgumentNullException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType)));

            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), PackingSize.Unspecified));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), 0));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), PackingSize.Unspecified, 0));

            AssertExtensions.Throws<ArgumentNullException>("fullname", () => module.DefineType(null, TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), new Type[0]));
        }

        [Fact]
        public void DefineType_TypeAlreadyExists_ThrowsArgumentException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            module.DefineType("TestType");
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType"));
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic));
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType)));

            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), PackingSize.Unspecified));
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), 0));
            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), PackingSize.Unspecified, 0));

            AssertExtensions.Throws<ArgumentException>(null, () => module.DefineType("TestType", TypeAttributes.NotPublic, typeof(ModuleBuilderDefineType), new Type[0]));
        }

        [Fact]
        public void DefineType_NonAbstractInterface_ThrowsInvalidOperationException()
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<InvalidOperationException>(() => module.DefineType("A", TypeAttributes.Interface));
        }
    }
}
