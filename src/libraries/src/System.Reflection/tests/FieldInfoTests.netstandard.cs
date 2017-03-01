// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Tests
{
    public partial class FieldInfoTests
    {
        struct FieldData
        {
            public object field;
        }

        [Theory]
        [InlineData(222)]
        [InlineData("new value")]
        [InlineData('A')]
        [InlineData(false)]
        [InlineData(4.56f)]
        [InlineData(double.MaxValue)]
        [InlineData(long.MaxValue)]
        [InlineData(byte.MaxValue)]
        [InlineData(null)]
        public static void SetValueDirect_GetValueDirectRoundDataTest(object value)
        {
            FieldData testField = new FieldData { field = -1 };

            FieldInfo info = testField.GetType().GetField("field");
            TypedReference reference = __makeref(testField);
            info.SetValueDirect(reference, value);
            object result = info.GetValueDirect(reference);
            Assert.Equal(value, result);
        }
    }
}
