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

        [Theory]
        [MemberData(nameof(VisibilityAttributes), true)]
        public void DefineEnum_ValueType(TypeAttributes visibility)
        {
            foreach (Type integerType in s_builtInIntegerTypes)
            {
                ModuleBuilder module = Helpers.DynamicModule();
                EnumBuilder enumBuilder = module.DefineEnum("MyEnum", visibility, integerType);
                Assert.True(enumBuilder.IsEnum);
                Assert.Equal("MyEnum", enumBuilder.FullName);

                enumBuilder.CreateTypeInfo().AsType();
            }
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

        private void VerificationHelperNegative(string name, TypeAttributes myTypeAttribute, Type mytype, bool flag)
        {
            ModuleBuilder myModuleBuilder = Helpers.DynamicModule();
            Assert.Throws<ArgumentException>(() =>
            {
                EnumBuilder myEnumBuilder = myModuleBuilder.DefineEnum(name, myTypeAttribute, mytype);
                if (!flag)
                {
                    myEnumBuilder = myModuleBuilder.DefineEnum(name, myTypeAttribute, typeof(int));
                }
            });
        }
    }
}
