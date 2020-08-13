// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class VersionedTests
    {
        [Theory]
        [MemberData(nameof(CallByName_TestData))]
        public void CallByName(object instance, string methodName, CallType useCallType, object[] args, Func<object, object> getResult, object expected)
        {
            Assert.Equal(getResult is null ? expected : null, Versioned.CallByName(instance, methodName, useCallType, args));
            if (getResult != null)
            {
                Assert.Equal(expected, getResult(instance));
            }
        }

        [Theory]
        [MemberData(nameof(CallByName_ArgumentException_TestData))]
        public void CallByName_ArgumentException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<ArgumentException>(() => Versioned.CallByName(instance, methodName, useCallType, args));
        }

        [Theory]
        [MemberData(nameof(CallByName_MissingMemberException_TestData))]
        public void CallByName_MissingMemberException(object instance, string methodName, CallType useCallType, object[] args)
        {
            Assert.Throws<MissingMemberException>(() => Versioned.CallByName(instance, methodName, useCallType, args));
        }

        public static IEnumerable<object[]> CallByName_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[] { 1, 2 }, null, 3 };
            yield return new object[] { new Class(), "Method", CallType.Get, new object[] { 2, 3 }, null, 5 };
            yield return new object[] { new Class(), "P", CallType.Get, new object[0], null, 0 };
            yield return new object[] { new Class(), "Item", CallType.Get, new object[] { 2 }, null, 2 };
            yield return new object[] { new Class(), "P", CallType.Set, new object[] { 3 }, new Func<object, object>(obj => ((Class)obj).Value), 3 };
            yield return new object[] { new Class(), "Item", CallType.Let, new object[] { 4, 5 }, new Func<object, object>(obj => ((Class)obj).Value), 9 };
        }

        public static IEnumerable<object[]> CallByName_ArgumentException_TestData()
        {
            yield return new object[] { null, null, default(CallType), new object[0] };
            yield return new object[] { new Class(), "Method", default(CallType), new object[] { 1, 2 } };
            yield return new object[] { new Class(), "Method", (CallType)int.MaxValue, new object[] { 1, 2 } };
        }

        public static IEnumerable<object[]> CallByName_MissingMemberException_TestData()
        {
            yield return new object[] { new Class(), "Method", CallType.Method, new object[0] };
            yield return new object[] { new Class(), "Q", CallType.Get, new object[0] };
        }

        private sealed class Class
        {
            public int Value;
            public int Method(int x, int y) => x + y;
            public int P
            {
                get { return Value; }
                set { Value = value; }
            }
            public object this[object index]
            {
                get { return Value + (int)index; }
                set { Value = (int)value + (int)index; }
            }
        }

        [Theory]
        [InlineData(null, false)]
        [InlineData('a', false)]
        [InlineData(1, true)]
        [InlineData("12x", false)]
        [InlineData("123", true)]
        [InlineData('1', true)]
        [InlineData("&O123", true)]
        [InlineData("&H123", true)]
        public void IsNumeric(object value, bool expected)
        {
            Assert.Equal(expected, Versioned.IsNumeric(value));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("OBJECT", "System.Object")]
        [InlineData(" OBJECT ", "System.Object")]
        [InlineData("object", "System.Object")]
        [InlineData("custom", null)]
        public void SystemTypeName(string value, string expected)
        {
            Assert.Equal(expected, Versioned.SystemTypeName(value));
        }

        [Theory]
        [MemberData(nameof(TypeName_TestData))]
        public void TypeName(object expression, string expected)
        {
            Assert.Equal(expected, Versioned.TypeName(expression));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(TypeName_ComObject_TestData))]
        [SkipOnMono("COM Interop not supported on Mono")]
        public void TypeName_ComObject(string progId, string expected)
        {
            Type type = Type.GetTypeFromProgID(progId, true);
            object expression = Activator.CreateInstance(type);
            Assert.Equal(expected, Versioned.TypeName(expression));
        }

        public static IEnumerable<object[]> TypeName_TestData()
        {
            yield return new object[] { null, "Nothing" };
            yield return new object[] { new object(), "Object" };
            yield return new object[] { default(bool), "Boolean" };
            yield return new object[] { default(char), "Char" };
            yield return new object[] { default(sbyte), "SByte" };
            yield return new object[] { default(byte), "Byte" };
            yield return new object[] { default(short), "Short" };
            yield return new object[] { default(ushort), "UShort" };
            yield return new object[] { default(int), "Integer" };
            yield return new object[] { default(uint), "UInteger" };
            yield return new object[] { default(long), "Long" };
            yield return new object[] { default(ulong), "ULong" };
            yield return new object[] { default(float), "Single" };
            yield return new object[] { default(double), "Double" };
            yield return new object[] { default(decimal), "Decimal" };
            yield return new object[] { default(DateTime), "Date" };
            yield return new object[] { "", "String" };
            yield return new object[] { default(object[]), "Nothing" };
            yield return new object[] { new object[0], "Object()" };
            yield return new object[] { new char[0, 0], "Char(,)" };
            yield return new object[] { default(int?), "Nothing" };
            yield return new object[] { (int?)0, "Integer" };
        }

        public static IEnumerable<object[]> TypeName_ComObject_TestData()
        {
            yield return new object[] { "ADODB.Stream", "Stream" };
            yield return new object[] { "MSXML2.DOMDocument", "DOMDocument" };
            yield return new object[] { "Scripting.Dictionary", "Dictionary" };
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("System.Object", "Object")]
        [InlineData("Object", "Object")]
        [InlineData(" object ", "Object")]
        [InlineData("custom", null)]
        public void VbTypeName(string value, string expected)
        {
            Assert.Equal(expected, Versioned.VbTypeName(value));
        }
    }
}
