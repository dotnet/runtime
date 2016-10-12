// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineEnum
    {
        private static Type[] s_builtInIntegerTypes = new Type[] { typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong) };

        public static IEnumerable<object[]> DefineEnum_TestData()
        {
            foreach (string name in new string[] { "TestEnum", "testenum", "enum", "\uD800\uDC00", "a\0b\0c" })
            {
                foreach (object[] attributesData in VisibilityAttributes(true))
                {
                    foreach (Type underlyingType in s_builtInIntegerTypes)
                    {
                        yield return new object[] { name, attributesData[0], underlyingType };
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(DefineEnum_TestData))]
        public void DefineEnum_ValueType(string name, TypeAttributes visibility, Type underlyingType)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            EnumBuilder enumBuilder = module.DefineEnum(name, visibility, underlyingType);
            Assert.True(enumBuilder.IsEnum);

            Assert.Equal(module.Assembly, enumBuilder.Assembly);
            Assert.Equal(module, enumBuilder.Module);

            Assert.Equal(name, enumBuilder.Name);
            Assert.Equal(Helpers.GetFullName(name), enumBuilder.FullName);
            Assert.Equal(enumBuilder.FullName + ", " + module.Assembly.FullName, enumBuilder.AssemblyQualifiedName);

            Assert.Equal(typeof(Enum), enumBuilder.BaseType);
            Assert.Equal(null, enumBuilder.DeclaringType);

            Assert.True(enumBuilder.Attributes.HasFlag(visibility));
            Assert.Equal(underlyingType, enumBuilder.UnderlyingField.FieldType);
            
            enumBuilder.CreateTypeInfo().AsType();
        }

        [Theory]
        [MemberData(nameof(VisibilityAttributes), false)]
        public void DefineEnum_NonVisibilityAttributes_ThrowsArgumentException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<ArgumentException>("name", () => module.DefineEnum("MyEnum", visibility, typeof(int)));
        }

        [Theory]
        [MemberData(nameof(VisibilityAttributes), true)]
        public void DefineEnum_EnumWithSameNameExists_ThrowsArgumentException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            module.DefineEnum("MyEnum", visibility, typeof(int));
            Assert.Throws<ArgumentException>(null, () => module.DefineEnum("MyEnum", visibility, typeof(int)));
        }

        [Theory]
        [MemberData(nameof(VisibilityAttributes), true)]
        public void DefineEnum_NullName_ThrowsArgumentNullException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<ArgumentNullException>("fullname", () => module.DefineEnum(null, visibility, typeof(object)));
        }

        [Theory]
        [MemberData(nameof(VisibilityAttributes), true)]
        public void DefineEnum_EmptyName_ThrowsArgumentNullException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<ArgumentException>("fullname", () => module.DefineEnum("", visibility, typeof(object)));
        }

        [Theory]
        [MemberData(nameof(NestedVisibilityAttributes), true)]
        public void DefineEnum_IncorrectVisibilityAttributes_ThrowsArgumentException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            Assert.Throws<ArgumentException>(null, () => module.DefineEnum("MyEnum", visibility, typeof(object)));
        }

        [Theory]
        [MemberData(nameof(VisibilityAttributes), true)]
        public void DefineEnum_ReferecnceType_ThrowsTypeLoadException(TypeAttributes visibility)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            EnumBuilder enumBuilder = module.DefineEnum("MyEnum", visibility, typeof(string));
            Assert.Throws<TypeLoadException>(() => enumBuilder.CreateTypeInfo().AsType());
        }

        public static IEnumerable<object[]> NestedVisibilityAttributes(bool flag)
        {
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedAssembly, flag))
                yield return new object[] { TypeAttributes.NestedAssembly };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamANDAssem, flag))
                yield return new object[] { TypeAttributes.NestedFamANDAssem };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamily, flag))
                yield return new object[] { TypeAttributes.NestedFamily };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamANDAssem, flag))
                yield return new object[] { TypeAttributes.NestedFamANDAssem };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamORAssem, flag))
                yield return new object[] { TypeAttributes.NestedFamORAssem };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedPrivate, flag))
                yield return new object[] { TypeAttributes.NestedPrivate };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedPublic, flag))
                yield return new object[] { TypeAttributes.NestedPublic };
        }

        public static IEnumerable<object[]> VisibilityAttributes(bool flag)
        {
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Abstract, flag))
                yield return new object[] { TypeAttributes.Abstract };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AnsiClass, flag))
                yield return new object[] { TypeAttributes.AnsiClass };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AutoClass, flag))
                yield return new object[] { TypeAttributes.AutoClass };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AutoLayout, flag))
                yield return new object[] { TypeAttributes.AutoLayout };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.BeforeFieldInit, flag))
                yield return new object[] { TypeAttributes.BeforeFieldInit };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Class, flag))
                yield return new object[] { TypeAttributes.Class };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.ClassSemanticsMask, flag))
                yield return new object[] { TypeAttributes.ClassSemanticsMask };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.CustomFormatClass, flag))
                yield return new object[] { TypeAttributes.CustomFormatClass };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.CustomFormatMask, flag))
                yield return new object[] { TypeAttributes.CustomFormatMask };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.ExplicitLayout, flag))
                yield return new object[] { TypeAttributes.ExplicitLayout };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.HasSecurity, flag))
                yield return new object[] { TypeAttributes.HasSecurity };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Import, flag))
                yield return new object[] { TypeAttributes.Import };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Interface, flag))
                yield return new object[] { TypeAttributes.Interface };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.LayoutMask, flag))
                yield return new object[] { TypeAttributes.LayoutMask };

            if (JudgeVisibilityMaskAttributes(TypeAttributes.NotPublic, flag))
                yield return new object[] { TypeAttributes.NotPublic };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Public, flag))
                yield return new object[] { TypeAttributes.Public };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.RTSpecialName, flag))
                yield return new object[] { TypeAttributes.RTSpecialName };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Sealed, flag))
                yield return new object[] { TypeAttributes.Sealed };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.SequentialLayout, flag))
                yield return new object[] { TypeAttributes.SequentialLayout };

            if (JudgeVisibilityMaskAttributes(TypeAttributes.Serializable, flag))
                yield return new object[] { TypeAttributes.Serializable };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.SpecialName, flag))
                yield return new object[] { TypeAttributes.SpecialName };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.StringFormatMask, flag))
                yield return new object[] { TypeAttributes.StringFormatMask };
            if (JudgeVisibilityMaskAttributes(TypeAttributes.UnicodeClass, flag))
                yield return new object[] { TypeAttributes.UnicodeClass };
        }

        private static bool JudgeVisibilityMaskAttributes(TypeAttributes visibility, bool flag)
        {
            if (flag)
            {
                return (visibility & ~TypeAttributes.VisibilityMask) == 0;
            }
            else
            {
                return (visibility & ~TypeAttributes.VisibilityMask) != 0;
            }
        }
    }
}
