// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineEnum
    {
        private static Type[] s_builtInIntegerTypes = new Type[] { typeof(byte), typeof(SByte), typeof(Int16), typeof(ushort),
        typeof(int), typeof(uint), typeof(long), typeof(ulong) };

        [Fact]
        public void TestWithValueType()
        {
            List<object> myArray = new List<object>();
            myArray = GetVisibilityAttr(true);
            foreach (TypeAttributes current in myArray)
            {
                foreach (Type integerType in s_builtInIntegerTypes)
                {
                    VerificationHelper(current, integerType);
                }
            }
        }

        [Fact]
        public void TestForNonVisibilityAttributes()
        {
            List<object> myArray = new List<object>();
            myArray = GetVisibilityAttr(false);
            foreach (TypeAttributes current in myArray)
            {
                string name = "MyEnum";
                VerificationHelperNegative(name, current, typeof(int), true);
            }
        }

        [Fact]
        public void TestForAlreadyExistingEnumWithSameName()
        {
            List<object> myArray = new List<object>();

            myArray = GetVisibilityAttr(true);

            foreach (TypeAttributes current in myArray)
            {
                string name = "MyEnum";
                VerificationHelperNegative(name, current, typeof(object), false);
            }
        }

        [Fact]
        public void TestWithNullName()
        {
            List<object> myArray = new List<object>();

            myArray = GetVisibilityAttr(true);

            foreach (TypeAttributes current in myArray)
            {
                string name = null;
                VerificationHelperNegative(name, current, typeof(object), typeof(ArgumentNullException));
            }
        }

        [Fact]
        public void TestWithEmptyName()
        {
            List<object> myArray = new List<object>();
            myArray = GetVisibilityAttr(true);

            foreach (TypeAttributes current in myArray)
            {
                string name = string.Empty;
                VerificationHelperNegative(name, current, typeof(object), typeof(ArgumentException));
            }
        }

        [Fact]
        public void TestWithIncorrectVisibilityAttributes()
        {
            List<object> myArray = new List<object>();
            myArray = GetNestVisibilityAttr(true);
            foreach (TypeAttributes current in myArray)
            {
                string name = "MyEnum";
                VerificationHelperNegative(name, current, typeof(object), true);
            }
        }

        [Fact]
        public void TestWithReferenceType()
        {
            List<object> myArray = new List<object>();
            myArray = GetVisibilityAttr(true);
            foreach (TypeAttributes current in myArray)
            {
                VerificationHelperNegative("MyEnum", current, typeof(string), typeof(TypeLoadException));
            }
        }

        private ModuleBuilder GetModuleBuilder()
        {
            ModuleBuilder myModuleBuilder;
            AssemblyBuilder myAssemblyBuilder;
            // Get the current application domain for the current thread.
            AssemblyName myAssemblyName = new AssemblyName();
            myAssemblyName.Name = "TempAssembly";

            // Define a dynamic assembly in the current domain.
            myAssemblyBuilder =
               AssemblyBuilder.DefineDynamicAssembly
                           (myAssemblyName, AssemblyBuilderAccess.Run);
            // Define a dynamic module in "TempAssembly" assembly.
            myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");

            return myModuleBuilder;
        }

        private List<object> GetNestVisibilityAttr(bool flag)
        {
            List<object> myArray = new List<object>();
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedAssembly, flag))
                myArray.Add(TypeAttributes.NestedAssembly);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamANDAssem, flag))
                myArray.Add(TypeAttributes.NestedFamANDAssem);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamily, flag))
                myArray.Add(TypeAttributes.NestedFamily);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamANDAssem, flag))
                myArray.Add(TypeAttributes.NestedFamANDAssem);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedFamORAssem, flag))
                myArray.Add(TypeAttributes.NestedFamORAssem);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedPrivate, flag))
                myArray.Add(TypeAttributes.NestedPrivate);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.NestedPublic, flag))
                myArray.Add(TypeAttributes.NestedPublic);
            return myArray;
        }

        private List<object> GetVisibilityAttr(bool flag)
        {
            List<object> myArray = new List<object>();
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Abstract, flag))
                myArray.Add(TypeAttributes.Abstract);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AnsiClass, flag))
                myArray.Add(TypeAttributes.AnsiClass);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AutoClass, flag))
                myArray.Add(TypeAttributes.AutoClass);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.AutoLayout, flag))
                myArray.Add(TypeAttributes.AutoLayout);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.BeforeFieldInit, flag))
                myArray.Add(TypeAttributes.BeforeFieldInit);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Class, flag))
                myArray.Add(TypeAttributes.Class);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.ClassSemanticsMask, flag))
                myArray.Add(TypeAttributes.ClassSemanticsMask);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.CustomFormatClass, flag))
                myArray.Add(TypeAttributes.CustomFormatClass);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.CustomFormatMask, flag))
                myArray.Add(TypeAttributes.CustomFormatMask);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.ExplicitLayout, flag))
                myArray.Add(TypeAttributes.ExplicitLayout);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.HasSecurity, flag))
                myArray.Add(TypeAttributes.HasSecurity);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Import, flag))
                myArray.Add(TypeAttributes.Import);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Interface, flag))
                myArray.Add(TypeAttributes.Interface);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.LayoutMask, flag))
                myArray.Add(TypeAttributes.LayoutMask);


            if (JudgeVisibilityMaskAttributes(TypeAttributes.NotPublic, flag))
                myArray.Add(TypeAttributes.NotPublic);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Public, flag))
                myArray.Add(TypeAttributes.Public);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.RTSpecialName, flag))
                myArray.Add(TypeAttributes.RTSpecialName);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.Sealed, flag))
                myArray.Add(TypeAttributes.Sealed);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.SequentialLayout, flag))
                myArray.Add(TypeAttributes.SequentialLayout);

            if (JudgeVisibilityMaskAttributes(TypeAttributes.Serializable, flag))
                myArray.Add(TypeAttributes.Serializable);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.SpecialName, flag))
                myArray.Add(TypeAttributes.SpecialName);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.StringFormatMask, flag))
                myArray.Add(TypeAttributes.StringFormatMask);
            if (JudgeVisibilityMaskAttributes(TypeAttributes.UnicodeClass, flag))
                myArray.Add(TypeAttributes.UnicodeClass);


            return myArray;
        }

        private bool JudgeVisibilityMaskAttributes(TypeAttributes visibility, bool flag)
        {
            if (flag)
            {
                if ((visibility & ~TypeAttributes.VisibilityMask) == 0)
                    return true;
                else
                    return false;
            }
            else
            {
                if ((visibility & ~TypeAttributes.VisibilityMask) != 0)
                    return true;
                else
                    return false;
            }
        }

        private void VerificationHelper(TypeAttributes myTypeAttribute, Type mytype)
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define a enumeration type with name 'MyEnum' in the 'TempModule'.
            EnumBuilder myEnumBuilder = myModuleBuilder.DefineEnum("MyEnum",
                                 myTypeAttribute, mytype);
            Assert.True(myEnumBuilder.IsEnum);
            Assert.Equal(myEnumBuilder.FullName, "MyEnum");

            myEnumBuilder.CreateTypeInfo().AsType();
        }

        private void VerificationHelperNegative(string name, TypeAttributes myTypeAttribute, Type mytype, bool flag)
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define a enumeration type with name 'MyEnum' in the 'TempModule'.

            Assert.Throws<ArgumentException>(() =>
            {
                EnumBuilder myEnumBuilder = myModuleBuilder.DefineEnum(name, myTypeAttribute, mytype);
                if (!flag)
                {
                    myEnumBuilder = myModuleBuilder.DefineEnum(name, myTypeAttribute, typeof(int));
                }
            });
        }

        private void VerificationHelperNegative(string name, TypeAttributes myTypeAttribute, Type mytype, Type expectedException)
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define a enumeration type with name 'MyEnum' in the 'TempModule'.
            Action test = () =>
            {
                EnumBuilder myEnumBuilder = myModuleBuilder.DefineEnum(name, myTypeAttribute, mytype);
                myEnumBuilder.CreateTypeInfo().AsType();
            };

            Assert.Throws(expectedException, test);
        }
    }

    public class Container1
    {
        public class Nested
        {
            private Container1 _parent;

            public Nested()
            {
            }
            public Nested(Container1 parent)
            {
                _parent = parent;
            }
        }
    }
}
