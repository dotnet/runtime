// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public abstract class KnownAce_Tests : GenericAce_Tests
    {

    }

    public class CompoundAce_Tests : KnownAce_Tests
    {
        private static object[] CompoundAce_CreateTestData(int intFlags, int accessMask, int intCompoundAceType, string stringsid, int offset)
        {
            CompoundAceType compoundAceType = (CompoundAceType)intCompoundAceType;
            AceFlags flags = (AceFlags)intFlags;
            SecurityIdentifier sid = new SecurityIdentifier(stringsid);

            CompoundAce ace = new CompoundAce(flags, accessMask, compoundAceType, sid);
            Assert.Equal(flags, ace.AceFlags);
            Assert.Equal(accessMask, ace.AccessMask);
            Assert.Equal(compoundAceType, ace.CompoundAceType);
            Assert.Equal(sid, ace.SecurityIdentifier);

            byte[] binaryForm = new byte[ace.BinaryLength + offset];
            binaryForm[offset + 0] = (byte)(AceType.AccessAllowedCompound);
            binaryForm[offset + 1] = (byte)flags;
            binaryForm[offset + 2] = (byte)(ace.BinaryLength >> 0);
            binaryForm[offset + 3] = (byte)(ace.BinaryLength >> 8);

            int baseOffset = offset + 4;
            int offsetLocal = 0;

            unchecked
            {
                binaryForm[baseOffset + 0] = (byte)(accessMask >> 0);
                binaryForm[baseOffset + 1] = (byte)(accessMask >> 8);
                binaryForm[baseOffset + 2] = (byte)(accessMask >> 16);
                binaryForm[baseOffset + 3] = (byte)(accessMask >> 24);
            }
            offsetLocal += 4;

            binaryForm[baseOffset + offsetLocal + 0] = (byte)((ushort)compoundAceType >> 0);
            binaryForm[baseOffset + offsetLocal + 1] = (byte)((ushort)compoundAceType >> 8);
            binaryForm[baseOffset + offsetLocal + 2] = 0;
            binaryForm[baseOffset + offsetLocal + 3] = 0;
            offsetLocal += 4;

            sid.GetBinaryForm(binaryForm, baseOffset + offsetLocal);

            return new object[] { ace, binaryForm, offset };
        }

        public static IEnumerable<object[]> CompoundAce_TestObjects()
        {
            yield return CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(1, -1, 1, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(2, 2, 0, "S-1-5-10", 0);
            yield return CompoundAce_CreateTestData(4, 4, 2, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(15, 1, 4, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0);
            yield return CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0);
        }

        [Fact]
        public void CompoundAce_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("securityIdentifier", () => new CompoundAce((AceFlags)0, 1, (CompoundAceType)1, null));
        }

        [Fact]
        public void CompoundAce_CreateBinaryForm_Invalid()
        {
            CompoundAce ace = (CompoundAce)CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => CompoundAce.CreateFromBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => CompoundAce.CreateFromBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CompoundAce.CreateFromBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CompoundAce.CreateFromBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Fact]
        public void CompoundAce_GetBinaryForm_Invalid()
        {
            CompoundAce ace = (CompoundAce)CompoundAce_CreateTestData(0, 1, 1, "S-1-5-11", 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => ace.GetBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ace.GetBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Theory]
        [MemberData(nameof(CompoundAce_TestObjects))]
        public void CompoundAce_GetBinaryForm(GenericAce testAce, byte[] expectedBinaryForm, int testOffset)
        {
            byte[] resultBinaryForm = new byte[testAce.BinaryLength + testOffset];
            testAce.GetBinaryForm(resultBinaryForm, testOffset);
            GenericAce_VerifyBinaryForms(expectedBinaryForm, resultBinaryForm, testOffset);
        }

        [Theory]
        [MemberData(nameof(CompoundAce_TestObjects))]
        public void CompoundAce_CreateFromBinaryForm(GenericAce expectedAce, byte[] testBinaryForm, int testOffset)
        {
            GenericAce resultAce = CompoundAce.CreateFromBinaryForm(testBinaryForm, testOffset);
            GenericAce_VerifyAces(expectedAce, resultAce);
        }
    }
}
