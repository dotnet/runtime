// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class FrameDimensionTests
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Guid()
        {
            Guid guid = Guid.NewGuid();
            FrameDimension fd = new FrameDimension(guid);
            Assert.Equal(guid, fd.Guid);
        }

        public static IEnumerable<object[]> ImageFormatGuidTestData
        {
            get
            {
                yield return new object[] { new Guid("{6aedbd6d-3fb5-418a-83a6-7f45229dc872}"), FrameDimension.Time };
                yield return new object[] { new Guid("{84236f7b-3bd3-428f-8dab-4ea1439ca315}"), FrameDimension.Resolution };
                yield return new object[] { new Guid("{7462dc86-6180-4c7e-8e3f-ee7333a7a483}"), FrameDimension.Page };
                yield return new object[] { new Guid("48749428-316f-496a-ab30-c819a92b3137"), new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")) };
            }
        }

        public static IEnumerable<object[]> FrameDimensionEqualsTestData
        {
            get
            {
                yield return new object[] { new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")), new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")), true };
                yield return new object[] { new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")), new FrameDimension(new Guid("b96b3cad-0728-11d3-9d7b-0000f81ef32e")), false };
                yield return new object[] { new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")), null, false };
                yield return new object[] { new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")), new object(), false };
            }
        }

        public static IEnumerable<object[]> FrameDimensionToStringTestData
        {
            get
            {
                yield return new object[] { "Time", FrameDimension.Time };
                yield return new object[] { "Resolution", FrameDimension.Resolution };
                yield return new object[] { "Page", FrameDimension.Page };
                yield return new object[] { "[FrameDimension: 48749428-316f-496a-ab30-c819a92b3137]", new FrameDimension(new Guid("48749428-316f-496a-ab30-c819a92b3137")) };
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(ImageFormatGuidTestData))]
        public void Guid_ReturnsExpected(Guid expected, FrameDimension frameDimension)
        {
            Assert.Equal(expected, frameDimension.Guid);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(FrameDimensionEqualsTestData))]
        public void Equals_Object_ReturnsExpected(FrameDimension frameDimension, object obj, bool result)
        {
            Assert.Equal(result, frameDimension.Equals(obj));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHashCode_Success()
        {
            Guid guid = Guid.NewGuid();
            Assert.Equal(guid.GetHashCode(), new FrameDimension(guid).GetHashCode());
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(FrameDimensionToStringTestData))]
        public void ToString_ReturnsExpected(string expected, FrameDimension imageFormat)
        {
            Assert.Equal(expected, imageFormat.ToString());
        }
    }
}
