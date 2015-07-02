// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using TestLibrary;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineProperty
    {
        [Fact]
        public void TestDefinePropertyWithAttribute1() { GeneralPositiveTest(PropertyAttributes.None); }

        [Fact]
        public void TestDefinePropertyWithAttribute2() { GeneralPositiveTest(PropertyAttributes.HasDefault); }

        [Fact]
        public void TestDefinePropertyWithAttribute3() { GeneralPositiveTest(PropertyAttributes.RTSpecialName); }

        [Fact]
        public void TestDefinePropertyWithAttribute4() { GeneralPositiveTest(PropertyAttributes.HasDefault); }

        [Fact]
        public void TestDefinePropertyWithAttribute5() { GeneralPositiveTest((PropertyAttributes)(-1)); }

        [Fact]
        public void TestDefinePropertyWithAttribute6() { GeneralPositiveTest((PropertyAttributes)0x0800); }

        [Fact]
        public void TestThrowsExceptionForNullName() { GeneralNegativeTest(null, typeof(ArgumentNullException)); }

        [Fact]
        public void TestThrowsExceptionForEmptyName() { GeneralNegativeTest(string.Empty, typeof(ArgumentException)); }

        [Fact]
        public void TestThrowsExceptionForNullTerminatedString() { GeneralNegativeTest("\0", typeof(ArgumentException)); }

        [Fact]
        public void TestThrowsExceptionForNullCharacterInString() { GeneralNegativeTest("\0Testing", typeof(ArgumentException)); }

        public void GeneralPositiveTest(PropertyAttributes attr)
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderDefineProperty");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            myType.DefineProperty("TestProperty", attr, typeof(int), null, null, new Type[] { typeof(int) }, null, null);

            Type t = myType.CreateTypeInfo().AsType();

            PropertyInfo pi = t.GetProperty("TestProperty", BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.NotNull(pi);
            Assert.Equal(typeof(int), pi.PropertyType);
        }

        public void GeneralNegativeTest(string name, Type expected)
        {
            AssemblyName myAsmName =
                new AssemblyName("TypeBuilderDefineProperty");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(
                 myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myType = myModule.DefineType("Sample",
                TypeAttributes.Class | TypeAttributes.Public);

            Action test = () =>
            {
                myType.DefineProperty(name, PropertyAttributes.HasDefault, typeof(int), null, null, new Type[] { typeof(int) }, null, null);
                Type t = myType.CreateTypeInfo().AsType();
            };

            Assert.Throws(expected, test);
        }
    }
}
