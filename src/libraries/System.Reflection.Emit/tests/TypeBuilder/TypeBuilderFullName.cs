// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderFullName
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";

        private TypeBuilder GetTypeBuilder(string typename)
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");
            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, ModuleName);
            return modulebuilder.DefineType(typename);
        }

        [Fact]
        public void TestRandomString()
        {
            string typename = "StrPostest1";
            TypeBuilder typebuilder = GetTypeBuilder(typename);
            string str = typebuilder.FullName;
            Assert.Equal(typename, str);
        }

        [Fact]
        public void TestStringWithWhitespace()
        {
            string typename = "StrPostTest2";
            int number = GetInt32(1, typename.Length);
            for (int i = 0; i < number; i++)
            {
                int random = GetInt32(0, typename.Length - 1);
                typename = typename.Replace(typename[random], ' ');
            }
            TypeBuilder typebuilder = GetTypeBuilder(typename);
            string str = typebuilder.FullName;
            Assert.Equal(typename, str);
        }

        [Fact]
        public void TestOnNestedType()
        {
            string typename1 = "StrPosTest3_1";
            string typename2 = "StrPosTest3_2";
            TypeBuilder typebuilder1 = GetTypeBuilder(typename1);
            TypeBuilder typebuilder2 = typebuilder1.DefineNestedType(typename2);
            string str = typebuilder2.FullName;
            Assert.Equal((typename1 + "+" + typename2), str);
        }

        private int GetInt32(int minValue, int maxValue)
        {
            if (minValue == maxValue)
            {
                return minValue;
            }
            if (minValue < maxValue)
            {
                return minValue + TestLibrary.Generator.GetInt32() % (maxValue - minValue);
            }

            return minValue;
        }
    }
}
