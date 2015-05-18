// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using System;
using System.Reflection;
using System.Text;

namespace System.Reflection.Tests
{
    public class GetTypeTests
    {
        //Try to Load a type from currently executing Assembly and return True or false depending on whether type is loaded or not
        private static bool LoadType(string ptype)
        {
            Assembly asm = GetExecutingAssembly();
            Type type = null;

            type = asm.GetType(ptype);

            if (type == null)
            {
                return false;
            }
            else if (!type.FullName.Equals(ptype))
            {
                return false;
            }

            return true;
        }

        //Load Type PublicClass from itself
        [Fact]
        public void GetTypeTest1()
        {
            string type = "System.Reflection.GetTypesTests.Data.PublicClass";
            //Try to Load Type
            Assert.True(LoadType(type));
        }

        //Load Type NonPublicClass from itself
        [Fact]
        public void GetTypeTest2()
        {
            string type = "System.Reflection.GetTypesTests.Data.NonPublicClass";
            //Try to Load Type
            Assert.True(LoadType(type));
        }

        //Load Type FriendClass from itself
        [Fact]
        public void GetTypeTest3()
        {
            string type = "System.Reflection.GetTypesTests.Data.FriendClass";
            //Try to Load Type
            Assert.True(LoadType(type));
        }

        //Load Type PublicEnum from itself
        [Fact]
        public void GetTypeTest4()
        {
            string type = "System.Reflection.GetTypesTests.Data.PublicEnum";
            //Try to Load Type
            Assert.True(LoadType(type));
        }

        //Load Type PublicStruct from itself
        [Fact]
        public void GetTypeTest5()
        {
            string type = "System.Reflection.GetTypesTests.Data.PublicStruct";
            //Try to Load Type
            Assert.True(LoadType(type));
        }

        private static Assembly GetExecutingAssembly()
        {
            Assembly currentasm = null;

            Type t = typeof(GetTypeTests);
            TypeInfo ti = t.GetTypeInfo();
            currentasm = ti.Assembly;
            return currentasm;
        }
    }
}

namespace System.Reflection.GetTypesTests.Data
{
    // Metadata for Reflection
    public class PublicClass
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

    //include metadata for non public class
    internal class NonPublicClass
    {
        public class PublicNestedClass { }

        protected class ProtectedNestedClass { }

        internal class FriendNestedClass { }

        private class PrivateNestedClass { }
    }

    //include metadata for non public class
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


