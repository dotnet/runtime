// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using Xunit;

namespace System.ComponentModel.Tests
{
    public class ArrayConverterTests : TypeConverterTestBase
    {
        public override TypeConverter Converter => new ArrayConverter();

        public override bool PropertiesSupported => true;

        public override IEnumerable<ConvertTest> ConvertToTestData()
        {
            yield return ConvertTest.Valid(new int[] { 1, 2 }, "Int32[] Array").WithInvariantRemoteInvokeCulture();
            yield return ConvertTest.Valid(1, "1");

            yield return ConvertTest.CantConvertTo(new int[] { 1, 2 }, typeof(int[]));
            yield return ConvertTest.CantConvertTo(new int[] { 1, 2 }, typeof(InstanceDescriptor));
            yield return ConvertTest.CantConvertTo(new int[] { 1, 2 }, typeof(object));
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Throws NullReferenceException in .NET Framework.")]
        public void GetProperties_NullValue_ReturnsNull()
        {
            Assert.Null(Converter.GetProperties(null));
        }

        [Fact]
        public void GetProperties_NonArrayValue_ReturnsEmpty()
        {
            Assert.Empty(Converter.GetProperties("stringValue"));
        }

        [Fact]
        public void GetProperties_ArrayValue_PropertiesReturnsExpected()
        {
            var array = new int[] { 1, 2, 3 };
            PropertyDescriptorCollection properties = Converter.GetProperties(array);
            Assert.Equal(array.Length, properties.Count);

            for (int i = 0; i < array.Length; i++)
            {
                PropertyDescriptor property = properties[i];
                Assert.Equal(typeof(int[]), property.ComponentType);
                Assert.Equal(typeof(int), property.PropertyType);
                Assert.Equal($"[{i}]", property.Name);
                Assert.Empty(property.Attributes);
            }
        }

        [Fact]
        public void GetProperties_SetValue_GetValueReturnsExpected()
        {
            var array = new int[] { 1, 2, 3 };
            PropertyDescriptorCollection properties = Converter.GetProperties(array);
            for (int i = 0; i < array.Length; i++)
            {
                PropertyDescriptor property = properties[i];
                Assert.Equal(array[i], property.GetValue(array));
                property.SetValue(array, -1);
                Assert.Equal(-1, array[i]);
            }
        }

        [Fact]
        public void GetProperties_SetValueWithHandler_CallsValueChanged()
        {
            var array = new int[] { 1, 2, 3 };
            PropertyDescriptor property = Converter.GetProperties(array)[0];
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(array, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };

            // With handler.
            property.AddValueChanged(array, handler);
            property.SetValue(array, 0);
            Assert.Equal(new int[] { 0, 2, 3 }, array);
            Assert.Equal(1, callCount);

            // Remove handler.
            property.RemoveValueChanged(array, handler);
            property.SetValue(array, 1);
            Assert.Equal(new int[] { 1, 2, 3 }, array);
            Assert.Equal(1, callCount);
        }

        [Fact]
        public void GetProperties_SetValueOutOfRangeWithHandler_CallsValueChanged()
        {
            var array = new int[] { 1, 2, 3 };
            var smallerArray = new int[] { 1, 2 };
            PropertyDescriptor property = Converter.GetProperties(array)[2];
            int callCount = 0;
            EventHandler handler = (sender, e) =>
            {
                Assert.Same(smallerArray, sender);
                Assert.Same(EventArgs.Empty, e);
                callCount++;
            };

            // With handler.
            property.AddValueChanged(smallerArray, handler);
            property.SetValue(smallerArray, 0);
            Assert.Equal(new int[] { 1, 2 }, smallerArray);
            Assert.Equal(new int[] { 1, 2, 3 }, array);
            Assert.Equal(1, callCount);

            // Remove handler.
            property.RemoveValueChanged(smallerArray, handler);
            property.SetValue(smallerArray, 1);
            Assert.Equal(new int[] { 1, 2 }, smallerArray);
            Assert.Equal(new int[] { 1, 2, 3 }, array);
            Assert.Equal(1, callCount);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("nonArrayValue")]
        public void GetProperties_GetSetInvalidValue_Nop(string value)
        {
            var array = new int[] { 1, 2, 3 };
            PropertyDescriptor property = Converter.GetProperties(array)[2];
            Assert.Null(property.GetValue(value));
            property.SetValue(null, -1);
        }

        [Fact]
        public void GetProperties_GetSetDifferentIndex_Nop()
        {
            var array = new object[] { 1, 2, "3" };
            PropertyDescriptor finalProperty = Converter.GetProperties(array)[2];
            Assert.Null(finalProperty.GetValue(new int[2]));
            finalProperty.SetValue(new int[2], -1);
        }
    }
}
