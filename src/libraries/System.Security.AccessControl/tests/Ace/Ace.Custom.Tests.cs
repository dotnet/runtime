// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class CustomAce_Tests : GenericAce_Tests
    {
        private static object[] CustomAce_CreateTestData(int intType, int intFlags, int opaqueLength, int offset)
        {
            byte[] opaque = new byte[opaqueLength];
            AceType type = (AceType)intType;
            AceFlags flags = (AceFlags)intFlags;

            CustomAce ace = new CustomAce(type, flags, opaque);
            Assert.Equal(type, ace.AceType);
            Assert.Equal(flags, ace.AceFlags);
            Assert.Equal(opaque, ace.GetOpaque());

            byte[] binaryForm = new byte[ace.BinaryLength + offset];
            binaryForm[offset + 0] = (byte)type;
            binaryForm[offset + 1] = (byte)flags;
            binaryForm[offset + 2] = (byte)(ace.BinaryLength >> 0);
            binaryForm[offset + 3] = (byte)(ace.BinaryLength >> 8);
            opaque.CopyTo(binaryForm, 4 + offset);

            return new object[] { ace, binaryForm, offset };
        }

        public static IEnumerable<object[]> CustomAce_TestObjects()
        {
            yield return CustomAce_CreateTestData(19, 0, 4, 0);
            yield return CustomAce_CreateTestData(18, 1, 8, 0);
            yield return CustomAce_CreateTestData(17, 1, 4, 0);
        }

        [Fact]
        public void CustomAce_Constructor_Invalid()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomAce((AceType)16, (AceFlags)15, new byte[1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomAce((AceType)19, (AceFlags)1, new byte[1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CustomAce((AceType)0, (AceFlags)2, new byte[4]));

            foreach (AceType type in Enum.GetValues(typeof(AceType)))
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new CustomAce(type, (AceFlags)1, new byte[4]));
            }
        }

        [Fact]
        public void CustomAce_CreateBinaryForm_Invalid()
        {
            GenericAce ace = new CustomAce((AceType)19, (AceFlags)0, new byte[4]);
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => CustomAce.CreateFromBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => CustomAce.CreateFromBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CustomAce.CreateFromBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CustomAce.CreateFromBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Fact]
        public void CustomAce_GetBinaryForm_Invalid()
        {
            GenericAce ace = new CustomAce((AceType)19, (AceFlags)0, new byte[4]);
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => ace.GetBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ace.GetBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Theory]
        [MemberData(nameof(CustomAce_TestObjects))]
        public void CustomAce_GetBinaryForm(GenericAce testAce, byte[] expectedBinaryForm, int testOffset)
        {
            byte[] resultBinaryForm = new byte[testAce.BinaryLength + testOffset];
            testAce.GetBinaryForm(resultBinaryForm, testOffset);
            GenericAce_VerifyBinaryForms(expectedBinaryForm, resultBinaryForm, testOffset);
        }

        [Theory]
        [MemberData(nameof(CustomAce_TestObjects))]
        public void CustomAce_CreateFromBinaryForm(GenericAce expectedAce, byte[] testBinaryForm, int testOffset)
        {
            GenericAce resultAce = CustomAce.CreateFromBinaryForm(testBinaryForm, testOffset);
            GenericAce_VerifyAces(expectedAce, resultAce);
        }
    }
}
