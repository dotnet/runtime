// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public abstract class QualifiedAce_Tests : KnownAce_Tests
    {

    }

    public class CommonAce_Tests : QualifiedAce_Tests
    {
        private static object[] CommonAce_CreateTestData(int intFlags, int intQualifier, int accessMask, string stringsid, bool isCallback, int opaqueLength, int offset)
        {
            AceFlags flags = (AceFlags)intFlags;
            AceQualifier qualifier = (AceQualifier)intQualifier;
            SecurityIdentifier sid = new SecurityIdentifier(stringsid);
            byte[] opaque = new byte[opaqueLength];

            CommonAce ace = new CommonAce(flags, qualifier, accessMask, sid, isCallback, opaque);
            Assert.Equal(flags, ace.AceFlags);
            Assert.Equal(accessMask, ace.AccessMask);
            Assert.Equal(sid, ace.SecurityIdentifier);
            Assert.Equal(opaque, ace.GetOpaque());
            Assert.Equal(qualifier, ace.AceQualifier);
            Assert.Equal(isCallback, ace.IsCallback);

            byte[] binaryForm = new byte[ace.BinaryLength + offset];
            switch (qualifier)
            {
                case AceQualifier.AccessAllowed:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.AccessAllowedCallback : (byte)AceType.AccessAllowed;
                    break;
                case AceQualifier.AccessDenied:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.AccessDeniedCallback : (byte)AceType.AccessDenied;
                    break;
                case AceQualifier.SystemAudit:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.SystemAuditCallback : (byte)AceType.SystemAudit;
                    break;
                case AceQualifier.SystemAlarm:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.SystemAlarmCallback : (byte)AceType.SystemAlarm;
                    break;
                default:
                    return null;
            }
            binaryForm[offset + 1] = (byte)flags;
            binaryForm[offset + 2] = (byte)(ace.BinaryLength >> 0);
            binaryForm[offset + 3] = (byte)(ace.BinaryLength >> 8);

            int baseOffset = offset + 4;
            int offsetLocal = 0;

            binaryForm[baseOffset + 0] = (byte)(accessMask >> 0);
            binaryForm[baseOffset + 1] = (byte)(accessMask >> 8);
            binaryForm[baseOffset + 2] = (byte)(accessMask >> 16);
            binaryForm[baseOffset + 3] = (byte)(accessMask >> 24);
            offsetLocal += 4;

            sid.GetBinaryForm(binaryForm, baseOffset + offsetLocal);
            offsetLocal += sid.BinaryLength;
            opaque.CopyTo(binaryForm, baseOffset + offsetLocal);

            return new object[] { ace, binaryForm, offset };
        }

        public static IEnumerable<object[]> CommonAce_TestObjects()
        {
            yield return CommonAce_CreateTestData(0, 0, 1, "S-1-5-11", false, 4, 0);
            yield return CommonAce_CreateTestData(1, 1, 0, "S-1-5-11", false, 8, 0);
            yield return CommonAce_CreateTestData(2, 2, 2, "S-1-5-11", true, 8, 0);
            yield return CommonAce_CreateTestData(4, 3, 5, "S-1-5-11", true, 16, 0);
            yield return CommonAce_CreateTestData(8, 3, 5, "S-1-5-11", true, 16, 0);
            yield return CommonAce_CreateTestData(16, 3, 5, "S-1-5-11", true, 16, 0);
            yield return CommonAce_CreateTestData(32, 3, 5, "S-1-5-11", true, 4, 0);
            yield return CommonAce_CreateTestData(64, 3, 5, "S-1-5-11", true, 4, 0);
            yield return CommonAce_CreateTestData(128, 3, 5, "S-1-5-11", true, 16, 0);
            yield return CommonAce_CreateTestData(1, 1, 0, "S-1-5-11", false, 8, 0);
            yield return CommonAce_CreateTestData(1, 1, 0, "S-1-5-11", false, 8, 0);
            yield return CommonAce_CreateTestData(1, 1, 0, "S-1-5-11", false, 8, 0);
        }

        [Fact]
        public void CommonAce_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("securityIdentifier", () => new CommonAce((AceFlags)0, (AceQualifier)0, 1, null, true, new byte[4]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("qualifier", () => CommonAce_CreateTestData(8, 4, 1, "S-1-5-11", true, 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("qualifier", () => CommonAce_CreateTestData(8, -1, 1, "S-1-5-11", true, 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("opaque", () => CommonAce_CreateTestData(2, 1, 2, "S-1-5-11", true, 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("opaque", () => CommonAce_CreateTestData(2, 1, 2, "S-1-5-11", true, 17, 0));

        }

        [Fact]
        public void CommonAce_CreateBinaryForm_Invalid()
        {
            CommonAce ace = (CommonAce)CommonAce_CreateTestData(0, 0, 1, "S-1-5-11", false, 4, 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => CommonAce.CreateFromBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => CommonAce.CreateFromBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CommonAce.CreateFromBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => CommonAce.CreateFromBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Fact]
        public void CommonAce_GetBinaryForm_Invalid()
        {
            CommonAce ace = (CommonAce)CommonAce_CreateTestData(0, 0, 1, "S-1-5-11", false, 4, 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => ace.GetBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ace.GetBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Theory]
        [MemberData(nameof(CommonAce_TestObjects))]
        public void CommonAce_GetBinaryForm(GenericAce testAce, byte[] expectedBinaryForm, int testOffset)
        {
            byte[] resultBinaryForm = new byte[testAce.BinaryLength + testOffset];
            testAce.GetBinaryForm(resultBinaryForm, testOffset);
            GenericAce_VerifyBinaryForms(expectedBinaryForm, resultBinaryForm, testOffset);
        }

        [Theory]
        [MemberData(nameof(CommonAce_TestObjects))]
        public void CommonAce_CreateFromBinaryForm(GenericAce expectedAce, byte[] testBinaryForm, int testOffset)
        {
            GenericAce resultAce = CommonAce.CreateFromBinaryForm(testBinaryForm, testOffset);
            GenericAce_VerifyAces(expectedAce, resultAce);
        }
    }
}
