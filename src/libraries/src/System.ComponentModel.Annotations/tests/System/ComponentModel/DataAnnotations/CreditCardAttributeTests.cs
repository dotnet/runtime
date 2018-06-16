// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.DataAnnotations.Tests
{
    public class CreditCardAttributeTests : ValidationAttributeTestBase
    {
        protected override IEnumerable<TestCase> ValidValues()
        {
            yield return new TestCase(new CreditCardAttribute(), null);
            yield return new TestCase(new CreditCardAttribute(), "0000000000000000");
            yield return new TestCase(new CreditCardAttribute(), "1234567890123452");
            yield return new TestCase(new CreditCardAttribute(), "  1 2 3 4 5 6 7 8 9 0  1 2 34 5 2    ");
            yield return new TestCase(new CreditCardAttribute(), "--1-2-3-4-5-6-7-8-9-0--1-2-34-5-2----");
            yield return new TestCase(new CreditCardAttribute(), " - 1- -  2 3 --4 5 6 7 -8- -9- -0 - -1 -2 -3-4- --5-- 2    ");
            yield return new TestCase(new CreditCardAttribute(), "1234-5678-9012-3452");
            yield return new TestCase(new CreditCardAttribute(), "1234 5678 9012 3452");
        }
        
        protected override IEnumerable<TestCase> InvalidValues()
        {
            yield return new TestCase(new CreditCardAttribute(), "0000000000000001");
            yield return new TestCase(new CreditCardAttribute(), 0);
            yield return new TestCase(new CreditCardAttribute(), "000%000000000001");
            yield return new TestCase(new CreditCardAttribute(), "1234567890123452a");
            yield return new TestCase(new CreditCardAttribute(), "1234567890123452\0");
        }

        [Fact]
        public static void DataType_CustomDataType()
        {
            var attribute = new CreditCardAttribute();
            Assert.Equal(DataType.CreditCard, attribute.DataType);
            Assert.Null(attribute.CustomDataType);
        }
    }
}
