// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    using System.Reflection.Compatibility.UnitTests.FieldInfoTests.HelperObjects;

    public class FieldInfoFieldType
    {
        private BindingFlags _allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

#pragma warning disable 169
#pragma warning disable 649
        static private int s_field1;
        static public string Field2;
        static protected BindingFlags Field3;
        static internal int? Field4;
        static protected internal int[] Field5;
        private UserMadeStruct _field6;
        public UserMadeClass Field7;
        protected UserMadeGenericClass<int> Field8;
        internal UserMadeGenericClass<string>.UserNestedClass Field9;
        protected internal UserMadeInterface Field10;
        public UserMadeEnum Field11;
#pragma warning restore 169
#pragma warning restore 649

        [Fact]
        public void PosTest1()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("s_field1", _allFlags), typeof(int), "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field2", _allFlags), typeof(string), "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field3", _allFlags), typeof(BindingFlags), "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field4", _allFlags), typeof(int?), "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field5", _allFlags), typeof(int[]), "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("_field6", _allFlags), typeof(UserMadeStruct), "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field7", _allFlags), typeof(UserMadeClass), "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field8", _allFlags), typeof(UserMadeGenericClass<int>), "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field9", _allFlags), typeof(UserMadeGenericClass<string>.UserNestedClass), "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field10", _allFlags), typeof(UserMadeInterface), "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PosTest(typeof(FieldInfoFieldType).GetField("Field11", _allFlags), typeof(UserMadeEnum), "00K");
        }

        private void PosTest(FieldInfo fi, Type expected, string id)
        {
            Type actual = fi.FieldType;
            Assert.Equal(expected, actual);
        }
    }
}

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests.HelperObjects
{
    #region Helper Objects
    internal struct UserMadeStruct
    {
    }

    public class UserMadeClass
    {
    }

    public class UserMadeGenericClass<T>
    {
        internal class UserNestedClass { }
    }

    public interface UserMadeInterface
    {
    }

    public enum UserMadeEnum
    {
        val1,
        val2
    }
    #endregion
}