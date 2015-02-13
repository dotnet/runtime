// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace System.ComponentModel.Tests
{
    public class CollectionConverterTests : ConverterTestBase
    {
        private static TypeConverter s_converter = new CollectionConverter();

        [Fact]
        public static void ConvertTo_WithContext()
        {
            ConvertTo_WithContext(new object[1, 3]
                {
                    { new Collection1(), "(Collection)", null }
                },
                CollectionConverterTests.s_converter);
        }
    }
}
