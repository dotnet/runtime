// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_CreateFromBinaryForm
    {
        [Fact]
        public static void BasicValidationTestCases()
        {

            RawAcl rawAcl = null;
            byte[] binaryForm = null;
            int offset = 0;

            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            //CustomAce constructor parameters
            AceType aceType = AceType.AccessAllowed;
            AceFlags aceFlag = AceFlags.None;
            byte[] opaque = null;
            //CompoundAce constructor additional parameters
            int accessMask = 0;
            CompoundAceType compoundAceType = CompoundAceType.Impersonation;
            string sid = "BA";
            //CommonAce constructor additional parameters
            AceQualifier aceQualifier = 0;
            //ObjectAce constructor additional parameters
            ObjectAceFlags objectAceFlag = 0;
            Guid objectAceType;
            Guid inheritedObjectAceType;

            //case 1, a valid binary representation with revision 0, 1 SystemAudit CommonAce
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));

            //case 2, a valid binary representation with revision 255, 1 AccessAllowed CommonAce
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));


            //case 3, a valid binary representation with revision 127, 1 CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = (AceFlags)223; //all flags ored together
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));

            //case 4, a valid binary representation with revision 1, 1 CompoundAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)223; //all flags ored together
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)));
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));


            //case 5, a valid binary representation with revision 1, 1 ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)223; //all flags ored together
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));

            //case 6, a valid binary representation with revision 1, no Ace
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 0, rawAcl.BinaryLength));

            //case 7, a valid binary representation with revision 1, and all Aces from case 1 to 5
            revision = 127;
            capacity = 5;
            rawAcl = new RawAcl(revision, capacity);
            //SystemAudit CommonAce
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            //Access Allowed CommonAce
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            //CustomAce
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = (AceFlags)223; //all flags ored together
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            //CompoundAce
            aceFlag = (AceFlags)223; //all flags ored together
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)));
            rawAcl.InsertAce(0, gAce);
            //ObjectAce
            aceFlag = (AceFlags)223; //all flags ored together
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 5, rawAcl.BinaryLength));

        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            byte[] binaryForm = null;
            int offset = 0;


            //case 1, binaryForm is null
            Assert.Throws<ArgumentNullException>(() =>
            {
                binaryForm = null;
                offset = 0;
                rawAcl = new RawAcl(binaryForm, offset);
            });

            //case 2, binaryForm is empty
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[0];
                offset = 0;
                rawAcl = new RawAcl(binaryForm, offset);
            });

            //case 3, negative offset                 
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[100];
                offset = -1;
                rawAcl = new RawAcl(binaryForm, offset);
            });

            //case 4, binaryForm length less than GenericAcl.HeaderLength
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[4];
                offset = 0;
                rawAcl = new RawAcl(binaryForm, offset);
            });

            //case 5, a RawAcl of length 64K. RawAcl length = HeaderLength + all ACE's  length
            // = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
            // = 8 + ( 4 + OpaqueLength) * num_of_custom_ace
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";
            //CustomAce constructor parameters
            AceType aceType = 0;
            AceFlags aceFlag = 0;
            byte[] opaque = null;
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = (AceFlags)223; //all flags ored together
            opaque = new byte[GenericAcl.MaxBinaryLength - 3 - 8 - 4];//GenericAcl.MaxBinaryLength = 65535, is not multiple of 4
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            binaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(binaryForm, 0);
            Assert.True(TestCreateFromBinaryForm(binaryForm, offset, revision, 1, rawAcl.BinaryLength));

            //case 6, a RawAcl of length 64K + 1. RawAcl length = HeaderLength + all ACE's  length
            // = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
            // = 8 + ( 4 + OpaqueLength) * num_of_custom_ace

            gAce = null;
            sid = "BA";
            //CustomAce constructor parameters
            aceType = 0;
            aceFlag = 0;
            binaryForm = new byte[65536];

            AssertExtensions.Throws<ArgumentException>("binaryForm", () =>
            {
                revision = 127;
                capacity = 1;
                rawAcl = new RawAcl(revision, capacity);
                rawAcl.GetBinaryForm(binaryForm, 0);
                //change the length bytes to 65535
                binaryForm[2] = 0xf;
                binaryForm[3] = 0xf;
                //change the aceCount to 1
                binaryForm[4] = 1;
                aceType = AceType.MaxDefinedAceType + 1;
                aceFlag = (AceFlags)223; //all flags ored together
                opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];//GenericAcl.MaxBinaryLength = 65535, is not multiple of 4
                gAce = new CustomAce(aceType, aceFlag, opaque);
                gAce.GetBinaryForm(binaryForm, 8);
                TestCreateFromBinaryForm(binaryForm, 0, revision, 1, binaryForm.Length);
            });


            //case 7, a valid binary representation with revision 255, 256 Access
            //CommonAce to test the correctness of  the process of the AceCount in the header
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            for (int i = 0; i < 256; i++)
            {
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                rawAcl.InsertAce(0, gAce);
            }
            binaryForm = new byte[rawAcl.BinaryLength + 1000];
            rawAcl.GetBinaryForm(binaryForm, 1000);
            Assert.True(TestCreateFromBinaryForm(binaryForm, 1000, revision, 256, rawAcl.BinaryLength));

            //case 8, array containing garbage
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);
            });

            //case 9, array containing garbage
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                //binary form shows the length will be 1, actual length is 12
                binaryForm = new byte[] { 1, 1, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
                TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);
            });

            //case 10, array containing garbage
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[] { 1, 1, 12, 0, 1, 1, 1, 1, 1, 1, 1, 1 };
                TestCreateFromBinaryForm(binaryForm, offset, revision, 1, 12);
            });
        }

        private static bool TestCreateFromBinaryForm(byte[] binaryForm, int offset, byte revision, int aceCount, int length)
        {
            RawAcl rawAcl = null;
            byte[] verifierBinaryForm = null;
            rawAcl = new RawAcl(binaryForm, offset);
            verifierBinaryForm = new byte[rawAcl.BinaryLength];
            rawAcl.GetBinaryForm(verifierBinaryForm, 0);
            Assert.True(((revision == rawAcl.Revision) &&
                Utils.IsBinaryFormEqual(binaryForm, offset, verifierBinaryForm) &&
                (aceCount == rawAcl.Count) &&
                (length == rawAcl.BinaryLength)));
            return true;
        }

    }
}
