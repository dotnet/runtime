// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    public class FieldInfoIsAssembly
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
        public string Field11;
#pragma warning restore 169
#pragma warning restore 649

        [Fact]
        public void PosTest1()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("s_field1", _allFlags), false, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field2", _allFlags), false, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field3", _allFlags), false, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field4", _allFlags), true, "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field5", _allFlags), false, "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("_field6", _allFlags), false, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field7", _allFlags), false, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field8", _allFlags), false, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field9", _allFlags), true, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field10", _allFlags), false, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PosTest(typeof(FieldInfoIsAssembly).GetField("Field11", _allFlags), false, "00K");
        }

        private void PosTest(FieldInfo fi, bool expected, string id)
        {
            bool actual = fi.IsAssembly;
            Assert.Equal(expected, actual);
        }
    }
}