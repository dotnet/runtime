// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
