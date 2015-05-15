// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;

namespace System.Reflection.Tests
{
    public class AssemblyNamePropertyTests
    {
        //Verify AssemblyName.Name
        [Fact]
        public void Case1_Name()
        {
            //the assembly name is not associated with the executing assembly.
            AssemblyName n = new AssemblyName("MyAssemblyName");
            String expectedString = "MyAssemblyName";
            Assert.Equal(n.Name, expectedString);
        }

        //Verify AssemblyName.Name for currently executing Assembly
        [Fact]
        public void Case2_Name()
        {
            //the assembly name is associated with the executing assembly.
            Type t = typeof(AssemblyNamePropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();
            Assert.True(n.Name.StartsWith("System.Reflection.Tests"));
        }


        //Verify AssemblyName.Name.set  
        [Fact]
        public void Case2_SetName()
        {
            //the assembly name is not associated with the executing assembly.
            AssemblyName n = new AssemblyName("MyAssemblyName");
            string newNamestring = "MyNewAssemblyName";
            n.Name = newNamestring;

            Assert.Equal(n.Name, newNamestring);
        }

        //Verify AssemblyName.FullName
        [Fact]
        public void Case3_FullName()
        {
            //the assembly name is not associated with an assembly
            AssemblyName n = new AssemblyName("MyAssemblyName");

            var expected = "MyAssemblyName";
            var extended = expected + ", Culture=neutral, PublicKeyToken=null";

            Assert.False(n.FullName != expected && n.FullName != extended);
        }

        //Verify AssemblyName.FullName for currently executing Assembly
        [Fact]
        public void Case4_FullName()
        {
            //the assembly name is associated with the executing assembly.
            Type t = typeof(AssemblyNamePropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();
            Assert.True(n.FullName.StartsWith("System.Reflection.Tests"));
            Assert.Equal(n.FullName.IndexOf(','), n.Name.Length);
        }

        //Verify AssemblyName.Flags
        [Fact]
        public void Case5_Flags()
        {
            //the assembly name is not associated with an assembly
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.True(n.Flags == 0);
            n.Flags = AssemblyNameFlags.PublicKey;

            Assert.True(n.Flags != 0);
        }

        //Verify AssemblyName.Flags for executing assembly.
        [Fact]
        public void Case6_Flags()
        {
            //the assembly name is associated with the executing assembly.
            Type t = typeof(AssemblyNamePropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();
            Assert.NotNull(n.Flags);
        }

        //Verify AssemblyName.Version
        [Fact]
        public void Case7_Version()
        {
            //the assembly name is not associated with an assembly
            AssemblyName n = new AssemblyName("MyAssemblyName_PropertyTests");
            n.Version = new Version(255, 1, 2, 3);

            var expected = "MyAssemblyName_PropertyTests, Version=255.1.2.3";
            var extended = expected + ", Culture=neutral, PublicKeyToken=null";

            Assert.False(n.FullName != expected && n.FullName != extended);
        }

        //Verify AssemblyName.Version for xecuting assembly.
        [Fact]
        public void Case8_Version()
        {
            //the assembly name is associated with the executing assembly.
            Type t = typeof(AssemblyNamePropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();
            n.Version = new Version(255, 1, 2, 3);

            Assert.True(n.FullName.Contains("Version=255.1.2.3"));
        }

        //Verify AssemblyName.CultureName
        [Fact]
        public void Case9_CultureName()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Null(n.CultureName);
        }

        //Verify AssemblyName.ContentType Deffault
        [Fact]
        public void Case10_ContentType()
        {
            AssemblyName n = new AssemblyName("MyAssemblyName");
            Assert.Equal(n.ContentType, AssemblyContentType.Default);
        }

        //Verify AssemblyName.ContentType WindowsRuntime
        [Fact]
        public void Case11_ContentType()
        {
            AssemblyName n = new AssemblyName("MyAssemblyNameRT");
            n.ContentType = AssemblyContentType.WindowsRuntime;

            Assert.Equal(n.ContentType, AssemblyContentType.WindowsRuntime);
        }

        //Verify AssemblyName.ContentType for executing Assembly
        [Fact]
        public void Case12_ContentType()
        {
            Type t = typeof(AssemblyNamePropertyTests);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();

            Assert.Equal(n.ContentType, AssemblyContentType.Default);
        }

        //Verify AssemblyName.ContentType for FW Assembly
        [Fact]
        public void Case13_ContentType()
        {
            //Get System.Runtime AssemblyName object
            Type t = typeof(int);
            TypeInfo ti = t.GetTypeInfo();
            AssemblyName n = ti.Assembly.GetName();

            Assert.Equal(n.ContentType, AssemblyContentType.Default);
        }
    }
}
