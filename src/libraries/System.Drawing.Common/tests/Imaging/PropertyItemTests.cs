// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class PropertyItemTests
    {
        private const int PropertyTagLuminanceTable = 0x5090;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Id_Set_GetReturnsExpected(int value)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagLuminanceTable);
            item.Id = value;
            Assert.Equal(value, item.Id);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Len_Set_GetReturnsExpected(int value)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagLuminanceTable);
            item.Len = value;
            Assert.Equal(value, item.Len);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1)]
        [InlineData(0)]
        [InlineData(-1)]
        public void Type_Set_GetReturnsExpected(short value)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagLuminanceTable);
            item.Type = value;
            Assert.Equal(value, item.Type);
        }

        public static IEnumerable<object[]> Value_Set_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { Array.Empty<byte>() };
            yield return new object[] { new byte[] { 1, 2, 3 } };
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Value_Set_TestData))]
        public void Value_Set_GetReturnsExpected(byte[] value)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagLuminanceTable);
            item.Value = value;
            Assert.Same(value, item.Value);
        }

        public static IEnumerable<object[]> Properties_TestData()
        {
            yield return new object[] { int.MaxValue, int.MaxValue, short.MaxValue, new byte[1] { 0 } };
            yield return new object[] { int.MinValue, int.MinValue, short.MinValue, new byte[2] { 1, 1} };
            yield return new object[] { 0, 0, 0, new byte[0] };
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Properties_TestData))]
        public void Properties_SetValues_ReturnsExpected(int id, int len, short type, byte[] value)
        {
            using var image = new Bitmap(Helpers.GetTestBitmapPath("16x16_nonindexed_24bit.png"));
            using Image clone = (Image)image.Clone();
            
            PropertyItem[] propItems = clone.PropertyItems;
            PropertyItem propItem = propItems[0];
            Assert.Equal(771, propItem.Id);
            Assert.Equal(1, propItem.Len);
            Assert.Equal(1, propItem.Type);
            Assert.Equal(new byte[1] { 0 }, propItem.Value);

            propItem.Id = id;
            propItem.Len = len;
            propItem.Type = type;
            propItem.Value = value;

            Assert.Equal(id, propItem.Id);
            Assert.Equal(len, propItem.Len);
            Assert.Equal(type, propItem.Type);
            Assert.Equal(value, propItem.Value);
        }
    }
}
