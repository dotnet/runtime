// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineInitializedData
    {
        [Fact]
        public void TestWithStaticAndPublic()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            FieldBuilder myFieldBuilder =
                myModuleBuilder.DefineInitializedData("MyField", new byte[] { 01, 00, 01 },
                           FieldAttributes.Static | FieldAttributes.Public);
            Assert.True(myFieldBuilder.IsStatic);
            Assert.True(myFieldBuilder.IsPublic);
            Assert.Equal(myFieldBuilder.Name, "MyField");
        }

        [Fact]
        public void TestWithStaticAndPrivate()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            FieldBuilder myFieldBuilder =
                myModuleBuilder.DefineInitializedData("MyField", new byte[] { 01, 00, 01 },
                           FieldAttributes.Static | FieldAttributes.Private);
            Assert.True(myFieldBuilder.IsStatic);
            Assert.True(myFieldBuilder.IsPrivate);
            Assert.Equal(myFieldBuilder.Name, "MyField");
        }

        [Fact]
        public void TestIncludeStaticWithDefault()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            FieldBuilder myFieldBuilder =
                myModuleBuilder.DefineInitializedData("MyField", new byte[] { 01, 00, 01 },
                            FieldAttributes.Private);
            Assert.True(myFieldBuilder.IsStatic);
            Assert.True(myFieldBuilder.IsPrivate);
            Assert.Equal(myFieldBuilder.Name, "MyField");
        }

        [Fact]
        public void TestThrowsExceptionOnEmptyName()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            Assert.Throws<ArgumentException>(() =>
            {
                FieldBuilder myFieldBuilder = myModuleBuilder.DefineInitializedData("", new byte[] { 01, 00, 01 }, FieldAttributes.Private);
            });
        }

        [Fact]
        public void TestThrowsExceptionWithSizeOfDataLessThanZero()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            string fieldname = "myField";
            Assert.Throws<ArgumentException>(() =>
            {
                FieldBuilder myFieldBuilder = myModuleBuilder.DefineInitializedData(fieldname, new byte[] { }, FieldAttributes.Private);
            });
        }

        [Fact]
        public void TestThrowsExceptionWithSizeOfDateGreaterThan0x3f0000()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();
            // Define the initialized data field in the .sdata section of the PE file.
            string fieldname = "myField";
            byte[] myByte = new byte[0x3f0000];
            Assert.Throws<ArgumentException>(() =>
            {
                FieldBuilder myFieldBuilder = myModuleBuilder.DefineInitializedData(fieldname, myByte, FieldAttributes.Public);
            });
        }

        [Fact]
        public void TestThrowsExceptionWithNullName()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();

            string fieldname = null;
            byte[] myByte = new byte[] { 01, 00, 01 };
            Assert.Throws<ArgumentNullException>(() =>
            {
                FieldBuilder myFieldBuilder = myModuleBuilder.DefineInitializedData(fieldname, myByte, FieldAttributes.Public);
            });
        }

        [Fact]
        public void TestThrowsExceptionWithNullData()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();

            string fieldname = "MyField";
            byte[] myByte = null;
            Assert.Throws<ArgumentNullException>(() =>
            {
                FieldBuilder myFieldBuilder = myModuleBuilder.DefineInitializedData(fieldname, myByte, FieldAttributes.Public);
            });
        }

        [Fact]
        public void TestThrowsExceptionWhenCreateGlobalFunctionsPreviouslyCalled()
        {
            ModuleBuilder myModuleBuilder = GetModuleBuilder();

            string fieldname = "MyField";
            byte[] myByte = new byte[] { 01, 00, 01 };
            FieldBuilder myFieldBuilder =
                myModuleBuilder.DefineInitializedData(fieldname, myByte, FieldAttributes.Public);
            myModuleBuilder.CreateGlobalFunctions();
            Assert.Throws<InvalidOperationException>(() =>
            {
                myFieldBuilder = myModuleBuilder.DefineInitializedData(fieldname, myByte, FieldAttributes.Public);
            });
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
    }

    public class Container2
    {
        public class Nested
        {
            private Container2 _parent;

            public Nested()
            {
            }
            public Nested(Container2 parent)
            {
                _parent = parent;
            }
        }
    }
}
