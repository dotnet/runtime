// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class RawAcl_Constructor
    {
       public static IEnumerable<object[]> RawAcl_Constructor_TestData()
       {
           yield return new object[] { 0, 1 };
           yield return new object[] { 127, 0 };
           yield return new object[] { 255, 255 };
        }

        [Theory]
        [MemberData(nameof(RawAcl_Constructor_TestData))]
        public static void TestConstructor(byte revision, int capacity)
        {
            RawAcl rawAcl = new RawAcl(revision, capacity);
            Assert.True(revision == rawAcl.Revision && 0 == rawAcl.Count && 8 == rawAcl.BinaryLength);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            byte revision = 0;
            int capacity = 0;

            //case 1, capacity = -1
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                revision = 0;
                capacity = -1;
                rawAcl = new RawAcl(revision, capacity);
            });

            //case 2, capacity = Int32.MaxValue/2 
            Assert.Throws<OutOfMemoryException>(() =>
            {
                revision = 0;
                capacity = Int32.MaxValue / 2;
                TestConstructor(revision, capacity);
            });

            //case 3, capacity = Int32.MaxValue
            Assert.Throws<OutOfMemoryException>(() =>
            {
                revision = 0;
                capacity = Int32.MaxValue;
                TestConstructor(revision, capacity);
            });
        }

    }
}