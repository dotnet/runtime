// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBuiltInComEnabledWithOSAutomationSupport))]
    public class OleAutBinderTests
    {
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        [return: UnsafeAccessorType("System.OleAutBinder")]
        private static extern object CreateOleAutBinder();

        private static Binder OleAutBinder => (Binder)CreateOleAutBinder();

        [Theory]
        [InlineData(-1, TestEnum.Value1)]
        [InlineData(0, TestEnum.Value2)]
        [InlineData(1, TestEnum.Value3)]
        [InlineData(2, (TestEnum)2)]
        public static void OleAutBinder_Enum(int value, TestEnum expected)
        {
            Assert.Equal(expected, OleAutBinder.ChangeType(value, typeof(TestEnum), null));
        }

        [Fact]
        public static void OleAutBinder_DBNull()
        {
            Assert.Null(OleAutBinder.ChangeType(DBNull.Value, typeof(string), null));
            Assert.Equal(DBNull.Value, OleAutBinder.ChangeType(DBNull.Value, typeof(object), null));
            Assert.Equal(DBNull.Value, OleAutBinder.ChangeType(DBNull.Value, typeof(DBNull), null));
            Assert.Throws<COMException>(() => OleAutBinder.ChangeType(DBNull.Value, typeof(Guid), null));
        }

        public static IEnumerable<object[]> OleAutBinder_Color_TestData()
        {
            yield return new object[] { 0, 0, 0, Color.Black };
            yield return new object[] { 255, 255, 255, Color.White };
            yield return new object[] { 128, 128, 128, Color.Gray };
            yield return new object[] { 255, 0, 0, Color.Red };
            yield return new object[] { 0, 128, 0, Color.Green };
            yield return new object[] { 0, 0, 255, Color.Blue };
        }

        [Theory]
        [MemberData(nameof(OleAutBinder_Color_TestData))]
        public static void OleAutBinder_Color(int r, int g, int b, Color expected)
        {
            // Convert to OLE's COLORREF - https://learn.microsoft.com/windows/win32/gdi/colorref
            int bgr = (b << 16) | (g << 8) | r;
            Assert.Equal(expected, OleAutBinder.ChangeType(bgr, typeof(Color), null));
        }

        [Theory]
        [InlineData(true, "True")]
        [InlineData(false, "False")]
        public static void OleAutBinder_Bool(bool value, string expected)
        {
            Assert.Equal(expected, OleAutBinder.ChangeType(value, typeof(string), null));
        }
    }

    public enum TestEnum
    {
        Value1 = -1,
        Value2 = 0,
        Value3 = 1
    }
}