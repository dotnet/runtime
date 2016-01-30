// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public class FieldInfoIsPublic
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
            PosTest(typeof(FieldInfoIsPublic).GetField("s_field1", _allFlags), false, "00A");
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field2", _allFlags), true, "00B");
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field3", _allFlags), false, "00C");
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field4", _allFlags), false, "00D");
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field5", _allFlags), false, "00E");
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("_field6", _allFlags), false, "00F");
        }

        [Fact]
        public void PosTest7()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field7", _allFlags), true, "00G");
        }

        [Fact]
        public void PosTest8()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field8", _allFlags), false, "00H");
        }

        [Fact]
        public void PosTest9()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field9", _allFlags), false, "00I");
        }

        [Fact]
        public void PosTest10()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field10", _allFlags), false, "00J");
        }

        [Fact]
        public void PosTest11()
        {
            PosTest(typeof(FieldInfoIsPublic).GetField("Field11", _allFlags), true, "00K");
        }

        private void PosTest(FieldInfo fi, bool expected, string id)
        {
            bool actual = fi.IsPublic;
            Assert.Equal(expected, actual);
        }
    }
}
