// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Reflection.Compatibility.UnitTests.FieldInfoTests
{
    public class FieldInfoMemberType
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
            PosTest(typeof(FieldInfoMemberType).GetField("s_field1", _allFlags));
        }

        [Fact]
        public void PosTest2()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field2", _allFlags));
        }

        [Fact]
        public void PosTest3()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field3", _allFlags));
        }

        [Fact]
        public void PosTest4()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field4", _allFlags));
        }

        [Fact]
        public void PosTest5()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field5", _allFlags));
        }

        [Fact]
        public void PosTest6()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("_field6", _allFlags));
        }

        [Fact]
        public void PosTest7()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field7", _allFlags));
        }

        [Fact]
        public void PosTest8()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field8", _allFlags));
        }

        [Fact]
        public void PosTest9()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field9", _allFlags));
        }

        [Fact]
        public void PosTest10()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field10", _allFlags));
        }

        [Fact]
        public void PosTest11()
        {
            PosTest(typeof(FieldInfoMemberType).GetField("Field11", _allFlags));
        }


        private void PosTest(FieldInfo fi)
        {
            Assert.NotNull(fi);
        }
    }
}