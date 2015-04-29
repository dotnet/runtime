// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    public class FieldInfoAttributes
    {
        private BindingFlags _allFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

#pragma warning disable 169
#pragma warning disable 649
        static private int s_field1;
        static public string Field2;
        static protected int Field3;
        static internal string Field4;
        static protected internal int Field5;
        private int _field6;
        public string Field7;
        protected int Field8;
        internal string Field9;
        protected internal int Field10;
        public const string Field11 = "";
#pragma warning restore 169
#pragma warning restore 649

        [Fact]
        public void PosTest1()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("s_field1", _allFlags), FieldAttributes.Private | FieldAttributes.Static, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field2", _allFlags), FieldAttributes.Public | FieldAttributes.Static, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field3", _allFlags), FieldAttributes.Family | FieldAttributes.Static, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field4", _allFlags), FieldAttributes.Assembly | FieldAttributes.Static, "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field5", _allFlags), FieldAttributes.FamORAssem | FieldAttributes.Static, "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("_field6", _allFlags), FieldAttributes.Private, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field7", _allFlags), FieldAttributes.Public, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field8", _allFlags), FieldAttributes.Family, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field9", _allFlags), FieldAttributes.Assembly, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field10", _allFlags), FieldAttributes.FamORAssem, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PosTest(typeof(FieldInfoAttributes).GetField("Field11", _allFlags), FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, "00K");
        }

        private void PosTest(FieldInfo fi, FieldAttributes expected, string id)
        {
            FieldAttributes actual = fi.Attributes;
            Assert.Equal(expected, actual);
        }
    }
}