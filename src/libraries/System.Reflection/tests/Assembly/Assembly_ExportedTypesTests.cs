// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.ExportedTypesTests.Data;

namespace System.Reflection.Tests
{
    public class ExportedTypesTests
    {
        [Fact]
        //Verify public class is exported in Currently Executing Assembly
        public void ExportedTypesTest1()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(AssemblyExportedTypesPublicClass);
            Assert.True(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify public class containing main Method is exported in Currently Executing Assembly
        public void ExportedTypesTest2()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(ExportedTypesTests);
            Assert.True(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify public Nested class is exported
        public void ExportedTypesTest3()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(AssemblyExportedTypesPublicClass.PublicNestedClass);
            Assert.True(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify public Enum is exported
        public void ExportedTypesTest4()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(PublicEnum);
            Assert.True(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify Public Struct is exported
        public void ExportedTypesTest5()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(PublicStruct);
            Assert.True(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify Non public class is not exported
        public void ExportedTypesTest6()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(NonPublicClass);
            Assert.False(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify Internal class is not exported
        public void ExportedTypesTest7()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(FriendClass);
            Assert.False(IsTypeExported(asm, etype));
        }

        [Fact]
        //Verify Generic Public class is  exported
        public void ExportedTypesTest8()
        {
            Assembly asm = GetExecutingAssembly();
            Type etype = typeof(GenericPublicClass<>);
            Assert.True(IsTypeExported(asm, etype));
        }

        private static Assembly GetExecutingAssembly()
        {
            Assembly asm = null;

            //currently Executing Assembly
            Type t = typeof(ExportedTypesTests);
            TypeInfo ti = t.GetTypeInfo();
            asm = ti.Assembly;
            return asm;
        }

        private static bool IsTypeExported(Assembly asm, Type type)
        {
            bool typeExported = false;
            IEnumerator<Type> allExportedTypes = asm.ExportedTypes.GetEnumerator();
            Type t = null;
            while (allExportedTypes.MoveNext())
            {
                t = allExportedTypes.Current;
                //
                if (t.Equals(type))
                {
                    //found type
                    typeExported = true;
                    break;
                }
            }

            return typeExported;
        }
    }
}

namespace System.Reflection.ExportedTypesTests.Data
{
    // Metadata for Reflection
    public class AssemblyExportedTypesPublicClass
    {
        public int[] intArray;

        public class PublicNestedClass { }

        protected class ProtectedNestedClass { }

        internal class FriendNestedClass { }

        private class PrivateNestedClass { }
    }


    public class GenericPublicClass<T>
    {
        public T[] array;

        public class PublicNestedClass { }

        protected class ProtectedNestedClass { }

        internal class FriendNestedClass { }

        private class PrivateNestedClass { }
    }


    internal class NonPublicClass
    {
        public class PublicNestedClass { }

        protected class ProtectedNestedClass { }

        internal class FriendNestedClass { }

        private class PrivateNestedClass { }
    }

    internal class FriendClass
    {
        public class PublicNestedClass { }

        protected class ProtectedNestedClass { }

        internal class FriendNestedClass { }

        private class PrivateNestedClass { }
    }

    public enum PublicEnum
    {
        RED = 1,
        BLUE = 2,
        GREEN = 3
    }

    public struct PublicStruct { }
}