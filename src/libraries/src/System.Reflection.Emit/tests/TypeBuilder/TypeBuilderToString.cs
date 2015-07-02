// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderToString
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";

        private TypeBuilder GetTypeBuilder(string module_name, string type_name)
        {
            AssemblyName assemblyname = new AssemblyName(module_name);

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, "Module1");
            return modulebuilder.DefineType(type_name);
        }

        [Fact]
        public void TestForRandomTypeName()
        {
            string typename = TestLibrary.Generator.GetString(false, 8, 256);

            // strip nulls and reserved characters out from the type-name
            // there is still a small chance that it might not be a valid identifier, but
            //  this reduces that considerably.
            typename = typename.Replace('\0', 'x');
            typename = typename.Replace('\\', '_');
            typename = typename.Replace(',', '_');
            typename = typename.Replace('[', '_');
            typename = typename.Replace(']', '_');
            typename = typename.Replace('&', '_');
            typename = typename.Replace('*', '_');
            typename = typename.Replace('+', '_');

            TypeBuilder typebuilder = GetTypeBuilder(ModuleName, typename);
            string stringvalue = typebuilder.ToString();
            Assert.Equal(typename, stringvalue);
        }

        [Fact]
        public void TestWithNumberStringName()
        {
            string typename = TestLibrary.Generator.GetInt32().ToString();
            TypeBuilder typebuilder = GetTypeBuilder(ModuleName, typename);
            string stringvalue = typebuilder.ToString();
            Assert.Equal(typename, stringvalue);
        }

        [Fact]
        public void TestWithWhitespaceInName()
        {
            string typename = TestLibrary.Generator.GetString(false, 8, 256);
            int len = typename.Length;
            int pos = GetInt32(0, len - 1);
            typename = typename.Replace(typename[pos], ' ');
            TypeBuilder typebuilder = GetTypeBuilder(ModuleName, typename);
            string stringvalue = typebuilder.ToString();
            Assert.Equal(typename, stringvalue);
        }

        [Fact]
        public void TestWithWhiteSpaceAsName()
        {
            string typename = "  ";
            TypeBuilder typebuilder = GetTypeBuilder(ModuleName, typename);
            string stringvalue = typebuilder.ToString();
            Assert.Equal(typename, stringvalue);
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
