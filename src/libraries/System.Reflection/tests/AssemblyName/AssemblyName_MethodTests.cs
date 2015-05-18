// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class AssemblyNameMethodTests
    {
        //Verify that AssemblyName.ToString method returns correct name for executing assembly.
        [Fact]
        public void Case1_ToString()
        {
            Assembly asm = null;
            Type t = typeof(AssemblyNameMethodTests);
            TypeInfo ti = t.GetTypeInfo();
            asm = ti.Assembly;
            AssemblyName n = asm.GetName();

            Assert.Equal(n.ToString(), n.FullName);
        }


        //Verify that AssemblyName.ToString method returns correct name for assembly.
        [Fact]
        public void Case2_ToString()
        {
            AssemblyName n = new AssemblyName("Foo");
            Assert.True(n.ToString().StartsWith("Foo"));
        }


        //Verify that AssemblyName.ToString method returns correct name for assembly for name with spaces.
        [Fact]
        public void Case3_ToString()
        {
            String name = "Hi There";
            AssemblyName n = new AssemblyName(name);
            Assert.True(n.ToString().StartsWith(name));
        }

        // Verify SetPublicKey with Null
        [Fact]
        public void Case4_SetPublicKey()
        {
            AssemblyName n = new AssemblyName();
            n.SetPublicKey(null);
            Assert.Null(n.GetPublicKey());
        }

        // Verify SetPublicKey with Empty byte Array
        [Fact]
        public void Case5_SetPublicKey()
        {
            AssemblyName n = new AssemblyName();
            byte[] barray = new byte[16];
            n.SetPublicKey(barray);

            byte[] barray_returned = n.GetPublicKey();
            Assert.NotNull(barray_returned);
            Assert.True(isEqual(barray, barray_returned));
        }

        // Verify SetPublicKey with byte Array initialized with '\0'
        [Fact]
        public void Case6_SetPublicKey()
        {
            AssemblyName n = new AssemblyName();
            byte[] barray = new byte[16];
            for (int i = 0; i < barray.Length; i++)
                barray[i] = (byte)'\0';
            n.SetPublicKey(barray);
            byte[] barray_returned = n.GetPublicKey();
            Assert.NotNull(barray_returned);
            Assert.True(isEqual(barray, barray_returned));
        }

        // Verify SetPublicKeyToken with Null
        [Fact]
        public void Case8_SetPublicKeyToken()
        {
            AssemblyName n = new AssemblyName();
            n.SetPublicKeyToken(null);
            Assert.Null(n.GetPublicKeyToken());
        }

        // Verify SetPublicKeyToken with Empty byte Array
        [Fact]
        public void Case9_SetPublicKeyToken()
        {
            AssemblyName n = new AssemblyName();
            byte[] barray = new byte[16];
            n.SetPublicKeyToken(barray);

            byte[] barray_returned = n.GetPublicKeyToken();
            Assert.NotNull(barray_returned);
            Assert.True(isEqual(barray, barray_returned));
        }

        // Verify SetPublicKeyToken with byte Array initialized with '\0'
        [Fact]
        public void Case10_SetPublicKeyToken()
        {
            AssemblyName n = new AssemblyName();
            byte[] barray = new byte[16];

            for (int i = 0; i < barray.Length; i++)
                barray[i] = (byte)'\0';

            n.SetPublicKeyToken(barray);
            byte[] barray_returned = n.GetPublicKeyToken();
            Assert.NotNull(barray_returned);
            Assert.True(isEqual(barray, barray_returned));
        }

        //Verify GetPublicKeyToken for an AssemblyName
        [Fact]
        public void Case11_GetPublicKeyToken()
        {
            AssemblyName n = new AssemblyName();
            n.SetPublicKeyToken(null);
            Assert.Null(n.GetPublicKeyToken());
        }

        //Verify GetPublicKeyToken for currently executing Assembly
        [Fact]
        public void Case12_GetPublicKeyToken()
        {
            Type t = typeof(AssemblyNameMethodTests);
            TypeInfo ti = t.GetTypeInfo();

            AssemblyName n = ti.Assembly.GetName();

            byte[] token = n.GetPublicKeyToken();

            Assert.Equal(8, token.Length);
        }

        private static bool isEqual(byte[] array1, byte[] array2)
        {
            if ((array1 == null) && (array2 != null))
                return false;

            else if ((array2 == null) && (array1 != null))
                return false;

            else if ((array1 == null) && (array2 == null))
                return true;

            if (array1.Length != array2.Length) return false;

            bool result = true;

            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
    }
}
