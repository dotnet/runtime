// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class ObjectAce_Tests : QualifiedAce_Tests
    {
        protected static void VerifyObjectAce(ObjectAce ace, AceFlags aceFlags, AceQualifier qualifier, int accessMask, SecurityIdentifier sid, ObjectAceFlags flags, Guid type, Guid inheritedType, bool isCallback, byte[] opaque)
        {
            Assert.Equal(aceFlags, ace.AceFlags);
            Assert.Equal(accessMask, ace.AccessMask);
            Assert.Equal(sid, ace.SecurityIdentifier);
            Assert.Equal(opaque, ace.GetOpaque());
            Assert.Equal(qualifier, ace.AceQualifier);
            Assert.Equal(isCallback, ace.IsCallback);
            Assert.Equal(flags, ace.ObjectAceFlags);
            Assert.Equal(type, ace.ObjectAceType);
            Assert.Equal(inheritedType, ace.InheritedObjectAceType);
        }

        private static object[] ObjectAce_CreateTestData(int intFlags, int intQualifier, int accessMask, string stringsid, int intObjectAceFlags, string stringType, string stringInheritedType, bool isCallback, int opaqueLength, int offset)
        {
            AceFlags aceFlags = (AceFlags)intFlags;
            AceQualifier qualifier = (AceQualifier)intQualifier;
            SecurityIdentifier sid = new SecurityIdentifier(stringsid);
            ObjectAceFlags flags = (ObjectAceFlags)intObjectAceFlags;
            Guid type = new Guid(stringType);
            Guid inheritedType = new Guid(stringInheritedType);
            byte[] opaque = new byte[opaqueLength];

            ObjectAce ace = new ObjectAce(aceFlags, qualifier, accessMask, sid, flags, type, inheritedType, isCallback, opaque);
            VerifyObjectAce(ace, aceFlags, qualifier, accessMask, sid, flags, type, inheritedType, isCallback, opaque);

            byte[] binaryForm = new byte[ace.BinaryLength + offset];
            switch (qualifier)
            {
                case AceQualifier.AccessAllowed:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.AccessAllowedCallbackObject : (byte)AceType.AccessAllowedObject;
                    break;
                case AceQualifier.AccessDenied:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.AccessDeniedCallbackObject : (byte)AceType.AccessDeniedObject;
                    break;
                case AceQualifier.SystemAudit:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.SystemAuditCallbackObject : (byte)AceType.SystemAuditObject;
                    break;
                case AceQualifier.SystemAlarm:
                    binaryForm[offset + 0] = isCallback ? (byte)AceType.SystemAlarmCallbackObject : (byte)AceType.SystemAlarmObject;
                    break;
                default:
                    return null;
            }
            binaryForm[offset + 1] = (byte)aceFlags;
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

            binaryForm[baseOffset + offsetLocal + 0] = (byte)(((uint)flags) >> 0);
            binaryForm[baseOffset + offsetLocal + 1] = (byte)(((uint)flags) >> 8);
            binaryForm[baseOffset + offsetLocal + 2] = (byte)(((uint)flags) >> 16);
            binaryForm[baseOffset + offsetLocal + 3] = (byte)(((uint)flags) >> 24);

            offsetLocal += 4;

            if ((flags & ObjectAceFlags.ObjectAceTypePresent) != 0)
            {
                type.ToByteArray().CopyTo(binaryForm, baseOffset + offsetLocal);
                offsetLocal += 16;
            }

            if ((flags & ObjectAceFlags.InheritedObjectAceTypePresent) != 0)
            {
                inheritedType.ToByteArray().CopyTo(binaryForm, baseOffset + offsetLocal);
                offsetLocal += 16;
            }

            sid.GetBinaryForm(binaryForm, baseOffset + offsetLocal);
            offsetLocal += sid.BinaryLength;
            opaque.CopyTo(binaryForm, baseOffset + offsetLocal);

            return new object[] { ace, binaryForm, offset };
        }


        public static IEnumerable<object[]> ObjectAce_TestObjects()
        {
            yield return ObjectAce_CreateTestData(0, 0, 1, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", false, 8, 0);
            yield return ObjectAce_CreateTestData(1, 1, 0, "S-1-5-11", 2, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", false, 8, 0);
            yield return ObjectAce_CreateTestData(2, 2, -1, "S-1-5-11", 3, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(4, 3, 0, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(8, 3, 1, "S-1-5-11", 2, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(16, 0, 3, "S-1-5-11", 0, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(32, 1, 3, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(64, 2, 3, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(128, 3, 3, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(15, 3, 3, "S-1-5-11", 2, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(192, 3, 3, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(2, 1, 999999, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0);
            yield return ObjectAce_CreateTestData(2, 1, 2, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 8, 0);
        }

        [Fact]
        public void ObjectAce_Constructor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("securityIdentifier", () => new ObjectAce((AceFlags)0, (AceQualifier)0, 1, null, (ObjectAceFlags)1, new Guid("{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}"), new Guid("{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}"), true, new byte[4]));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("qualifier", () => ObjectAce_CreateTestData(8, 4, 1, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("qualifier", () => ObjectAce_CreateTestData(8, -1, 1, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 4, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("opaque", () => ObjectAce_CreateTestData(2, 1, 2, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("opaque", () => ObjectAce_CreateTestData(2, 1, 2, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", true, 17, 0));
        }

        [Fact]
        public void ObjectAce_CreateBinaryForm_Invalid()
        {
            ObjectAce ace = (ObjectAce)ObjectAce_CreateTestData(0, 0, 1, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", false, 8, 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => ObjectAce.CreateFromBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ObjectAce.CreateFromBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => ObjectAce.CreateFromBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentException>("binaryForm", () => ObjectAce.CreateFromBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Fact]
        public void ObjectAce_GetBinaryForm_Invalid()
        {
            ObjectAce ace = (ObjectAce)ObjectAce_CreateTestData(0, 0, 1, "S-1-5-11", 1, "{73D03E18-5C03-422c-9EC7-C4C5B1CB1612}", "{3CFD5DF9-9146-43c8-BF99-78E7E2DE4BAF}", false, 8, 0)[0];
            AssertExtensions.Throws<ArgumentNullException>("binaryForm", () => ace.GetBinaryForm(null, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => ace.GetBinaryForm(new byte[1], -1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength + 1], 2));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("binaryForm", () => ace.GetBinaryForm(new byte[ace.BinaryLength], 1));
        }

        [Theory]
        [MemberData(nameof(ObjectAce_TestObjects))]
        public void ObjectAce_GetBinaryForm(GenericAce testAce, byte[] expectedBinaryForm, int testOffset)
        {
            byte[] resultBinaryForm = new byte[testAce.BinaryLength + testOffset];
            testAce.GetBinaryForm(resultBinaryForm, testOffset);
            GenericAce_VerifyBinaryForms(expectedBinaryForm, resultBinaryForm, testOffset);
        }

        [Theory]
        [MemberData(nameof(ObjectAce_TestObjects))]
        public void ObjectAce_CreateFromBinaryForm(GenericAce expectedAce, byte[] testBinaryForm, int testOffset)
        {
            GenericAce resultAce = ObjectAce.CreateFromBinaryForm(testBinaryForm, testOffset);
            GenericAce_VerifyAces(expectedAce, resultAce);
        }
    }
}
