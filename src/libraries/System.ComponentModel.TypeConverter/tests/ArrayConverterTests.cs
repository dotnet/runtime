// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class ArrayConverterTests : ConverterTestBase
    {
        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[1, 3]
                {
                    { new int[2] { 1, 2 }, "Int32[] Array", null }
                },
                new ArrayConverter());
        }
    }
}
