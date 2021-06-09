// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Encoder = System.Drawing.Imaging.Encoder;

namespace System.Drawing.Tests
{
    public class ImageTests
    {
        private const int PropertyTagLuminanceTable = 0x5090;
        private const int PropertyTagChrominanceTable = 0x5091;
        private const int PropertyTagExifUserComment = 0x9286;
        private const int PropertyTagTypeASCII = 2;
        private const int PropertyTagTypeShort = 3;

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void PropertyIdList_GetBitmapJpg_Success()
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            Assert.Equal(new int[] { PropertyTagExifUserComment, PropertyTagChrominanceTable, PropertyTagLuminanceTable }, bitmap.PropertyIdList);
            Assert.NotSame(bitmap.PropertyIdList, bitmap.PropertyIdList);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Returns new int[0] in .NET Framework.")]
        public void PropertyIdList_GetEmptyMemoryBitmap_ReturnsExpected()
        {
            using var bitmap = new Bitmap(1, 1);
            Assert.Empty(bitmap.PropertyIdList);
            Assert.Same(bitmap.PropertyIdList, bitmap.PropertyIdList);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void PropertyItems_GetBitmapJpg_Success()
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem[] items = bitmap.PropertyItems;
            Assert.Equal(3, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(29, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("LEAD Technologies Inc. V1.01\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(PropertyTagChrominanceTable, items[1].Id);
            Assert.Equal(128, items[1].Len);
            Assert.Equal(PropertyTagTypeShort, items[1].Type);
            Assert.Equal(new byte[]
            {
                0x16, 0x00, 0x17, 0x00, 0x1F, 0x00, 0x3E, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x17,
                0x00, 0x1B, 0x00, 0x22, 0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x1F,
                0x00, 0x22, 0x00, 0x49, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x3E,
                0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82,
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82,
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82,
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82,
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00
            }, items[1].Value);
            Assert.Equal(PropertyTagLuminanceTable, items[2].Id);
            Assert.Equal(128, items[2].Len);
            Assert.Equal(PropertyTagTypeShort, items[2].Type);
            Assert.Equal(new byte[]
            {
                0x15, 0x00, 0x0E, 0x00, 0x0D, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x43, 0x00, 0x50, 0x00, 0x0F,
                0x00, 0x0F, 0x00, 0x12, 0x00, 0x19, 0x00, 0x22, 0x00, 0x4C, 0x00, 0x4F, 0x00, 0x48, 0x00, 0x12,
                0x00, 0x11, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x4B, 0x00, 0x5B, 0x00, 0x49, 0x00, 0x12,
                0x00, 0x16, 0x00, 0x1D, 0x00, 0x26, 0x00, 0x43, 0x00, 0x72, 0x00, 0x69, 0x00, 0x51, 0x00, 0x17,
                0x00, 0x1D, 0x00, 0x30, 0x00, 0x49, 0x00, 0x59, 0x00, 0x8F, 0x00, 0x87, 0x00, 0x65, 0x00, 0x1F,
                0x00, 0x2E, 0x00, 0x48, 0x00, 0x54, 0x00, 0x6A, 0x00, 0x89, 0x00, 0x95, 0x00, 0x79, 0x00, 0x40,
                0x00, 0x54, 0x00, 0x66, 0x00, 0x72, 0x00, 0x87, 0x00, 0x9F, 0x00, 0x9E, 0x00, 0x85, 0x00, 0x5F,
                0x00, 0x79, 0x00, 0x7D, 0x00, 0x81, 0x00, 0x93, 0x00, 0x84, 0x00, 0x87, 0x00, 0x82, 0x00,
            }, items[2].Value);

            Assert.NotSame(items, bitmap.PropertyItems);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Returns new PropertyItem[0] in .NET Framework.")]
        public void PropertyItems_GetEmptyBitmapBmp_Success()
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("almogaver1bit.bmp"));
            Assert.Empty(bitmap.PropertyItems);
            Assert.Same(bitmap.PropertyItems, bitmap.PropertyItems);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Returns new PropertyItem[0] in .NET Framework.")]
        public void PropertyItems_GetEmptyMemoryBitmap_ReturnsExpected()
        {
            using var bitmap = new Bitmap(1, 1);
            Assert.Empty(bitmap.PropertyItems);
            Assert.Same(bitmap.PropertyItems, bitmap.PropertyItems);
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetPropertyItem_InvokeExistsBitmapBmp_Success()
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagExifUserComment);
            Assert.Equal(PropertyTagExifUserComment, item.Id);
            Assert.Equal(29, item.Len);
            Assert.Equal(PropertyTagTypeASCII, item.Type);
            Assert.Equal("LEAD Technologies Inc. V1.01\0", Encoding.ASCII.GetString(item.Value));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void GetPropertyItem_NoSuchPropertyItemEmptyMemoryBitmap_ThrowsArgumentException(int propid)
        {
            using var bitmap = new Bitmap(1, 1);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(propid));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void GetPropertyItem_NoSuchPropertyItemEmptyImageBitmapBmp_ThrowsArgumentException(int propid)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("almogaver1bit.bmp"));
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(propid));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RemovePropertyItem_InvokeMemoryBitmap_Success()
        {
            using var source = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item1 = source.GetPropertyItem(PropertyTagExifUserComment);
            PropertyItem item2 = source.GetPropertyItem(PropertyTagChrominanceTable);
            PropertyItem item3 = source.GetPropertyItem(PropertyTagLuminanceTable);
            using var bitmap = new Bitmap(1, 1);
            bitmap.SetPropertyItem(item1);
            bitmap.SetPropertyItem(item2);
            bitmap.SetPropertyItem(item3);

            bitmap.RemovePropertyItem(PropertyTagExifUserComment);
            Assert.Equal(new int[] { PropertyTagChrominanceTable, PropertyTagLuminanceTable }, bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagExifUserComment));
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(PropertyTagExifUserComment));
            
            bitmap.RemovePropertyItem(PropertyTagLuminanceTable);
            Assert.Equal(new int[] { PropertyTagChrominanceTable }, bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagLuminanceTable));
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(PropertyTagLuminanceTable));
            
            bitmap.RemovePropertyItem(PropertyTagChrominanceTable);
            Assert.Empty(bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagChrominanceTable));
            Assert.Throws<ExternalException>(() => bitmap.RemovePropertyItem(PropertyTagChrominanceTable));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void RemovePropertyItem_InvokeBitmapJpg_Success()
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            bitmap.RemovePropertyItem(PropertyTagExifUserComment);
            Assert.Equal(new int[] { PropertyTagChrominanceTable, PropertyTagLuminanceTable }, bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagExifUserComment));
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(PropertyTagExifUserComment));
            
            bitmap.RemovePropertyItem(PropertyTagLuminanceTable);
            Assert.Equal(new int[] { PropertyTagChrominanceTable }, bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagLuminanceTable));
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(PropertyTagLuminanceTable));
            
            bitmap.RemovePropertyItem(PropertyTagChrominanceTable);
            Assert.Empty(bitmap.PropertyIdList);
            Assert.Throws<ArgumentException>(null, () => bitmap.GetPropertyItem(PropertyTagChrominanceTable));
            Assert.Throws<ExternalException>(() => bitmap.RemovePropertyItem(PropertyTagChrominanceTable));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void RemovePropertyItem_NoSuchPropertyItemEmptyMemoryBitmap_ThrowsExternalException(int propid)
        {
            using var bitmap = new Bitmap(1, 1);
            Assert.Throws<ExternalException>(() => bitmap.RemovePropertyItem(propid));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void RemovePropertyItem_NoSuchPropertyItemEmptyImageBitmapBmp_ThrowsExternalException(int propid)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("almogaver1bit.bmp"));
            Assert.Throws<ExternalException>(() => bitmap.RemovePropertyItem(propid));
        }
  
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void RemovePropertyItem_NoSuchPropertyNotEmptyMemoryBitmap_ThrowsArgumentException(int propid)
        {
            using var source = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            PropertyItem item1 = source.GetPropertyItem(PropertyTagExifUserComment);
            PropertyItem item2 = source.GetPropertyItem(PropertyTagChrominanceTable);
            PropertyItem item3 = source.GetPropertyItem(PropertyTagLuminanceTable);
            using var bitmap = new Bitmap(1, 1);
            bitmap.SetPropertyItem(item1);
            bitmap.SetPropertyItem(item2);
            bitmap.SetPropertyItem(item3);
            
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(propid));
        }
  
        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void RemovePropertyItem_NoSuchPropertyNotEmptyBitmapJpg_ThrowsArgumentException(int propid)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            Assert.Throws<ArgumentException>(null, () => bitmap.RemovePropertyItem(propid));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void SetPropertyItem_InvokeMemoryBitmap_Success(int propid)
        {
            using var source = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            using var bitmap = new Bitmap(1, 1);

            // Change data.
            PropertyItem item = source.GetPropertyItem(PropertyTagExifUserComment);
            item.Value = Encoding.ASCII.GetBytes("Hello World\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment }, bitmap.PropertyIdList);
            PropertyItem[] items = bitmap.PropertyItems;
            Assert.Single(items);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));

            // New data.
            item.Id = propid;
            item.Value = Encoding.ASCII.GetBytes("New Value\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(2, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(propid, items[1].Id);
            Assert.Equal(10, items[1].Len);
            Assert.Equal(PropertyTagTypeASCII, items[1].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[1].Value));
            
            // Set same.
            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(2, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(propid, items[1].Id);
            Assert.Equal(10, items[1].Len);
            Assert.Equal(PropertyTagTypeASCII, items[1].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[1].Value));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void SetPropertyItem_InvokeBitmapJpg_Success(int propid)
        {
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));

            // Change data.
            PropertyItem item = bitmap.GetPropertyItem(PropertyTagExifUserComment);
            item.Value = Encoding.ASCII.GetBytes("Hello World\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, PropertyTagChrominanceTable, PropertyTagLuminanceTable }, bitmap.PropertyIdList);
            PropertyItem[] items = bitmap.PropertyItems;
            Assert.Equal(3, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(PropertyTagChrominanceTable, items[1].Id);
            Assert.Equal(128, items[1].Len);
            Assert.Equal(PropertyTagTypeShort, items[1].Type);
            Assert.Equal(new byte[]
            {
                0x16, 0x00, 0x17, 0x00, 0x1F, 0x00, 0x3E, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x17, 
                0x00, 0x1B, 0x00, 0x22, 0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x1F, 
                0x00, 0x22, 0x00, 0x49, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x3E, 
                0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00
            }, items[1].Value);
            Assert.Equal(PropertyTagLuminanceTable, items[2].Id);
            Assert.Equal(128, items[2].Len);
            Assert.Equal(PropertyTagTypeShort, items[2].Type);
            Assert.Equal(new byte[]
            {
                0x15, 0x00, 0x0E, 0x00, 0x0D, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x43, 0x00, 0x50, 0x00, 0x0F, 
                0x00, 0x0F, 0x00, 0x12, 0x00, 0x19, 0x00, 0x22, 0x00, 0x4C, 0x00, 0x4F, 0x00, 0x48, 0x00, 0x12, 
                0x00, 0x11, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x4B, 0x00, 0x5B, 0x00, 0x49, 0x00, 0x12, 
                0x00, 0x16, 0x00, 0x1D, 0x00, 0x26, 0x00, 0x43, 0x00, 0x72, 0x00, 0x69, 0x00, 0x51, 0x00, 0x17, 
                0x00, 0x1D, 0x00, 0x30, 0x00, 0x49, 0x00, 0x59, 0x00, 0x8F, 0x00, 0x87, 0x00, 0x65, 0x00, 0x1F, 
                0x00, 0x2E, 0x00, 0x48, 0x00, 0x54, 0x00, 0x6A, 0x00, 0x89, 0x00, 0x95, 0x00, 0x79, 0x00, 0x40, 
                0x00, 0x54, 0x00, 0x66, 0x00, 0x72, 0x00, 0x87, 0x00, 0x9F, 0x00, 0x9E, 0x00, 0x85, 0x00, 0x5F, 
                0x00, 0x79, 0x00, 0x7D, 0x00, 0x81, 0x00, 0x93, 0x00, 0x84, 0x00, 0x87, 0x00, 0x82, 0x00,
            }, items[2].Value);

            // New data.
            item.Id = propid;
            item.Value = Encoding.ASCII.GetBytes("New Value\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, PropertyTagChrominanceTable, PropertyTagLuminanceTable, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(4, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(PropertyTagChrominanceTable, items[1].Id);
            Assert.Equal(128, items[1].Len);
            Assert.Equal(PropertyTagTypeShort, items[1].Type);
            Assert.Equal(new byte[]
            {
                0x16, 0x00, 0x17, 0x00, 0x1F, 0x00, 0x3E, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x17, 
                0x00, 0x1B, 0x00, 0x22, 0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x1F, 
                0x00, 0x22, 0x00, 0x49, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x3E, 
                0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00
            }, items[1].Value);
            Assert.Equal(PropertyTagLuminanceTable, items[2].Id);
            Assert.Equal(128, items[2].Len);
            Assert.Equal(PropertyTagTypeShort, items[2].Type);
            Assert.Equal(new byte[]
            {
                0x15, 0x00, 0x0E, 0x00, 0x0D, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x43, 0x00, 0x50, 0x00, 0x0F, 
                0x00, 0x0F, 0x00, 0x12, 0x00, 0x19, 0x00, 0x22, 0x00, 0x4C, 0x00, 0x4F, 0x00, 0x48, 0x00, 0x12, 
                0x00, 0x11, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x4B, 0x00, 0x5B, 0x00, 0x49, 0x00, 0x12, 
                0x00, 0x16, 0x00, 0x1D, 0x00, 0x26, 0x00, 0x43, 0x00, 0x72, 0x00, 0x69, 0x00, 0x51, 0x00, 0x17, 
                0x00, 0x1D, 0x00, 0x30, 0x00, 0x49, 0x00, 0x59, 0x00, 0x8F, 0x00, 0x87, 0x00, 0x65, 0x00, 0x1F, 
                0x00, 0x2E, 0x00, 0x48, 0x00, 0x54, 0x00, 0x6A, 0x00, 0x89, 0x00, 0x95, 0x00, 0x79, 0x00, 0x40, 
                0x00, 0x54, 0x00, 0x66, 0x00, 0x72, 0x00, 0x87, 0x00, 0x9F, 0x00, 0x9E, 0x00, 0x85, 0x00, 0x5F, 
                0x00, 0x79, 0x00, 0x7D, 0x00, 0x81, 0x00, 0x93, 0x00, 0x84, 0x00, 0x87, 0x00, 0x82, 0x00,
            }, items[2].Value);
            Assert.Equal(propid, items[3].Id);
            Assert.Equal(10, items[3].Len);
            Assert.Equal(PropertyTagTypeASCII, items[3].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[3].Value));

            // Set same.
            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, PropertyTagChrominanceTable, PropertyTagLuminanceTable, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(4, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(PropertyTagChrominanceTable, items[1].Id);
            Assert.Equal(128, items[1].Len);
            Assert.Equal(PropertyTagTypeShort, items[1].Type);
            Assert.Equal(new byte[]
            {
                0x16, 0x00, 0x17, 0x00, 0x1F, 0x00, 0x3E, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x17, 
                0x00, 0x1B, 0x00, 0x22, 0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x1F, 
                0x00, 0x22, 0x00, 0x49, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x3E, 
                0x00, 0x57, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 
                0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00, 0x82, 0x00
            }, items[1].Value);
            Assert.Equal(PropertyTagLuminanceTable, items[2].Id);
            Assert.Equal(128, items[2].Len);
            Assert.Equal(PropertyTagTypeShort, items[2].Type);
            Assert.Equal(new byte[]
            {
                0x15, 0x00, 0x0E, 0x00, 0x0D, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x43, 0x00, 0x50, 0x00, 0x0F, 
                0x00, 0x0F, 0x00, 0x12, 0x00, 0x19, 0x00, 0x22, 0x00, 0x4C, 0x00, 0x4F, 0x00, 0x48, 0x00, 0x12, 
                0x00, 0x11, 0x00, 0x15, 0x00, 0x1F, 0x00, 0x34, 0x00, 0x4B, 0x00, 0x5B, 0x00, 0x49, 0x00, 0x12, 
                0x00, 0x16, 0x00, 0x1D, 0x00, 0x26, 0x00, 0x43, 0x00, 0x72, 0x00, 0x69, 0x00, 0x51, 0x00, 0x17, 
                0x00, 0x1D, 0x00, 0x30, 0x00, 0x49, 0x00, 0x59, 0x00, 0x8F, 0x00, 0x87, 0x00, 0x65, 0x00, 0x1F, 
                0x00, 0x2E, 0x00, 0x48, 0x00, 0x54, 0x00, 0x6A, 0x00, 0x89, 0x00, 0x95, 0x00, 0x79, 0x00, 0x40, 
                0x00, 0x54, 0x00, 0x66, 0x00, 0x72, 0x00, 0x87, 0x00, 0x9F, 0x00, 0x9E, 0x00, 0x85, 0x00, 0x5F, 
                0x00, 0x79, 0x00, 0x7D, 0x00, 0x81, 0x00, 0x93, 0x00, 0x84, 0x00, 0x87, 0x00, 0x82, 0x00,
            }, items[2].Value);
            Assert.Equal(propid, items[3].Id);
            Assert.Equal(10, items[3].Len);
            Assert.Equal(PropertyTagTypeASCII, items[3].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[3].Value));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(-1)]
        public void SetPropertyItem_InvokeBitmapBmp_Success(int propid)
        {
            using var source = new Bitmap(Helpers.GetTestBitmapPath("nature24bits.jpg"));
            using var bitmap = new Bitmap(Helpers.GetTestBitmapPath("almogaver1bit.bmp"));

            // Change data.
            PropertyItem item = source.GetPropertyItem(PropertyTagExifUserComment);
            item.Value = Encoding.ASCII.GetBytes("Hello World\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment }, bitmap.PropertyIdList);
            PropertyItem[] items = bitmap.PropertyItems;
            Assert.Single(items);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));

            // New data.
            item.Id = propid;
            item.Value = Encoding.ASCII.GetBytes("New Value\0");
            item.Len = item.Value.Length;

            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(2, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(propid, items[1].Id);
            Assert.Equal(10, items[1].Len);
            Assert.Equal(PropertyTagTypeASCII, items[1].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[1].Value));
            
            // Set same.
            bitmap.SetPropertyItem(item);

            Assert.Equal(new int[] { PropertyTagExifUserComment, propid }, bitmap.PropertyIdList);
            items = bitmap.PropertyItems;
            Assert.Equal(2, items.Length);
            Assert.Equal(PropertyTagExifUserComment, items[0].Id);
            Assert.Equal(12, items[0].Len);
            Assert.Equal(PropertyTagTypeASCII, items[0].Type);
            Assert.Equal("Hello World\0", Encoding.ASCII.GetString(items[0].Value));
            Assert.Equal(propid, items[1].Id);
            Assert.Equal(10, items[1].Len);
            Assert.Equal(PropertyTagTypeASCII, items[1].Type);
            Assert.Equal("New Value\0", Encoding.ASCII.GetString(items[1].Value));
        }

        public static IEnumerable<object[]> InvalidBytes_TestData()
        {
            // IconTests.Ctor_InvalidBytesInStream_TestData an array of 2 objects, but this test only uses the
            // 1st object.
            foreach (object[] data in IconTests.Ctor_InvalidBytesInStream_TestData())
            {
                yield return new object[] { data[0] };
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromFile_InvalidBytes_ThrowsOutOfMemoryException(byte[] bytes)
        {
            using (var file = TempFile.Create(bytes))
            {
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path));
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path, useEmbeddedColorManagement: true));
            }
        }

        [Fact]
        public void FromFile_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null, useEmbeddedColorManagement: true));
        }

        [Fact]
        public void FromFile_EmptyFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty, useEmbeddedColorManagement: true));
        }

        [Fact]
        public void FromFile_LongSegment_ThrowsException()
        {
            // Throws PathTooLongException on Desktop and FileNotFoundException elsewhere.
            if (PlatformDetection.IsNetFramework)
            {
                string fileName = new string('a', 261);

                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName));
                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
            else
            {
                string fileName = new string('a', 261);

                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName));
                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
        }

        [Fact]
        public void FromFile_NoSuchFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile"));
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile", useEmbeddedColorManagement: true));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromStream_InvalidBytes_ThrowsArgumentException(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream));
                Assert.Equal(0, stream.Position);

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true));
                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true));
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void FromStream_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true, validateImageData: true));
        }

        [Theory]
        [InlineData(PixelFormat.Format1bppIndexed, 1)]
        [InlineData(PixelFormat.Format4bppIndexed, 4)]
        [InlineData(PixelFormat.Format8bppIndexed, 8)]
        [InlineData(PixelFormat.Format16bppArgb1555, 16)]
        [InlineData(PixelFormat.Format16bppGrayScale, 16)]
        [InlineData(PixelFormat.Format16bppRgb555, 16)]
        [InlineData(PixelFormat.Format16bppRgb565, 16)]
        [InlineData(PixelFormat.Format24bppRgb, 24)]
        [InlineData(PixelFormat.Format32bppArgb, 32)]
        [InlineData(PixelFormat.Format32bppPArgb, 32)]
        [InlineData(PixelFormat.Format32bppRgb, 32)]
        [InlineData(PixelFormat.Format48bppRgb, 48)]
        [InlineData(PixelFormat.Format64bppArgb, 64)]
        [InlineData(PixelFormat.Format64bppPArgb, 64)]
        public void GetPixelFormatSize_ReturnsExpected(PixelFormat format, int expectedSize)
        {
            Assert.Equal(expectedSize, Image.GetPixelFormatSize(format));
        }

        [Theory]
        [InlineData(PixelFormat.Format16bppArgb1555, true)]
        [InlineData(PixelFormat.Format32bppArgb, true)]
        [InlineData(PixelFormat.Format32bppPArgb, true)]
        [InlineData(PixelFormat.Format64bppArgb, true)]
        [InlineData(PixelFormat.Format64bppPArgb, true)]
        [InlineData(PixelFormat.Format16bppGrayScale, false)]
        [InlineData(PixelFormat.Format16bppRgb555, false)]
        [InlineData(PixelFormat.Format16bppRgb565, false)]
        [InlineData(PixelFormat.Format1bppIndexed, false)]
        [InlineData(PixelFormat.Format24bppRgb, false)]
        [InlineData(PixelFormat.Format32bppRgb, false)]
        [InlineData(PixelFormat.Format48bppRgb, false)]
        [InlineData(PixelFormat.Format4bppIndexed, false)]
        [InlineData(PixelFormat.Format8bppIndexed, false)]
        public void IsAlphaPixelFormat_ReturnsExpected(PixelFormat format, bool expected)
        {
            Assert.Equal(expected, Image.IsAlphaPixelFormat(format));
        }

        public static IEnumerable<object[]> GetEncoderParameterList_ReturnsExpected_TestData()
        {
            yield return new object[]
            {
                ImageFormat.Tiff,
                new Guid[]
                {
                    Encoder.Compression.Guid,
                    Encoder.ColorDepth.Guid,
                    Encoder.SaveFlag.Guid,
                    new Guid(unchecked((int)0xa219bbc9), unchecked((short)0x0a9d), unchecked((short)0x4005), new byte[] { 0xa3, 0xee, 0x3a, 0x42, 0x1b, 0x8b, 0xb0, 0x6c }) /* Encoder.SaveAsCmyk.Guid */
                }
            };

#if !NETFRAMEWORK
            // NetFX doesn't support pointer-type encoder parameters, and doesn't define Encoder.ImageItems. Skip this test
            // on NetFX.
            yield return new object[]
            {
                ImageFormat.Jpeg,
                new Guid[]
                {
                    Encoder.Transformation.Guid,
                    Encoder.Quality.Guid,
                    Encoder.LuminanceTable.Guid,
                    Encoder.ChrominanceTable.Guid,
                    Encoder.ImageItems.Guid
                }
            };
#endif
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(GetEncoderParameterList_ReturnsExpected_TestData))]
        public void GetEncoderParameterList_ReturnsExpected(ImageFormat format, Guid[] expectedParameters)
        {
            if (PlatformDetection.IsNetFramework)
            {
                throw new SkipTestException("This is a known bug for .NET Framework");
            }

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo codec = codecs.Single(c => c.FormatID == format.Guid);

            using (var bitmap = new Bitmap(1, 1))
            {
                EncoderParameters paramList = bitmap.GetEncoderParameterList(codec.Clsid);

                Assert.Equal(
                    expectedParameters,
                    paramList.Param.Select(p => p.Encoder.Guid));
            }
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, ".NET Framework throws ExternalException")]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Save_InvalidDirectory_ThrowsDirectoryNotFoundException()
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                var badTarget = System.IO.Path.Combine("NoSuchDirectory", "NoSuchFile");
                AssertExtensions.Throws<DirectoryNotFoundException>(() => bitmap.Save(badTarget), $"The directory NoSuchDirectory of the filename {badTarget} does not exist.");
            }
        }
    }
}
