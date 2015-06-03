// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class CustomAttributeBuilderCtor1
    {
        [Fact]
        public void PosTest1()
        {
            string str = "PosTest1";
            Type[] ctorParams = new Type[] { typeof(string) };
            object[] paramValues = new object[] { str };
            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                typeof(ObsoleteAttribute).GetConstructor(ctorParams), paramValues);

            Assert.NotNull(cab);
        }

        [Fact]
        public void PosTest2()
        {
            Type[] ctorParams = new Type[] { };
            object[] paramValues = new object[] { };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                typeof(ObsoleteAttribute).GetConstructor(ctorParams), paramValues);

            Assert.NotNull(cab);
        }

        [Fact]
        public void PosTest3()
        {
            string str = "PosTest3";
            bool b = TestLibrary.Generator.GetByte() > byte.MaxValue / 2;

            Type[] ctorParams = new Type[] { typeof(string), typeof(bool) };
            object[] paramValues = new object[] { str, b };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                typeof(ObsoleteAttribute).GetConstructor(ctorParams), paramValues);

            Assert.NotNull(cab);
        }

        [Fact]
        public void NegTest1()
        {
            object[] paramValues = new object[] { };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                .Where(c => c.IsStatic).First(), paramValues);
            });
        }

        [Fact]
        public void NegTest2()
        {
            object[] paramValues = new object[]
            {
                false
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                 typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                 .Where(c => c.IsPrivate).First(), paramValues);
            });
        }

        [Fact]
        public void NegTest3()
        {
            string str = "NegTest3";
            bool b = false;

            Type[] ctorParams = new Type[] { typeof(string) };
            object[] paramValues = new object[] { str, b };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       typeof(ObsoleteAttribute).GetConstructor(ctorParams), paramValues);
            });
        }

        [Fact]
        public void NegTest4()
        {
            string str = "NegTest4";
            bool b = true;

            Type[] ctorParams = new Type[] { typeof(string), typeof(bool) };
            object[] paramValues = new object[] { b, str };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       typeof(ObsoleteAttribute).GetConstructor(ctorParams), paramValues);
            });
        }

        [Fact]
        public void NegTest5()
        {
            string str = "NegTest5";
            bool b = false;

            object[] paramValues = new object[] { str, b };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       null, paramValues);
            });
        }

        [Fact]
        public void NegTest6()
        {
            Type[] ctorParams = new Type[] { typeof(bool), typeof(string) };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       typeof(ObsoleteAttribute).GetConstructor(ctorParams), null);
            });
        }
    }

    public class TestConstructor
    {
        static TestConstructor() { }
        internal TestConstructor(bool b) { }
    }
}
