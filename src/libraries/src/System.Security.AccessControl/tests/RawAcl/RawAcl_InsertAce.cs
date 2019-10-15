// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_InsertAce
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            RawAcl rawAclVerifier = null;
            GenericAce ace = null;
            GenericAce aceVerifier = null;
            int count = 0;
            int index = 0;
            byte revision = 0;
            int capacity = 1;
            int flags = 1;
            int qualifier = 0;
            int accessMask = 1;
            string sid = "BA";
            bool isCallback = false;
            int opaqueSize = 8;
            //test insert at 0
            rawAcl = new RawAcl(revision, capacity);
            rawAclVerifier = new RawAcl(revision, capacity);
            ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
            index = 0;
            //save current count
            count = rawAcl.Count;
            rawAcl.InsertAce(index, ace);
            //verify the count number increase one
            Assert.True(rawAcl.Count == count + 1);
            //verify the inserted ace is equal to the originial ace
            aceVerifier = rawAcl[index];
            Assert.True(ace == aceVerifier);

            //verify right side aces are equal
            Assert.True(Utils.AclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1));

            //insert the same ACE to rawAclVerifier for next test
            rawAclVerifier.InsertAce(index, ace);

            //test insert at Count
            sid = "BA";
            ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
            count = rawAcl.Count;
            index = count;
            rawAcl.InsertAce(index, ace);
            //verify the count number increase one
            Assert.True(rawAcl.Count == count + 1);
            //verify the inserted ace is equal to the originial ace
            aceVerifier = rawAcl[index];
            Assert.True(ace == aceVerifier);

            //verify right side aces are equal
            Assert.True(Utils.AclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1));

            //insert the same ACE to rawAclVerifier for next test
            rawAclVerifier.InsertAce(index, ace);

            //test insert at Count - 1
            sid = "BG";
            ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
            count = rawAcl.Count;
            index = count - 1;
            rawAcl.InsertAce(index, ace);

            //verify the count number increase one
            Assert.True(rawAcl.Count == count + 1);
            //verify the inserted ace is equal to the originial ace
            aceVerifier = rawAcl[index];
            Assert.True(ace == aceVerifier);

            //verify right side aces are equal
            Assert.True(Utils.AclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1));

            //insert the same ACE to rawAclVerifier for next test
            rawAclVerifier.InsertAce(index, ace);

            //test insert at Count /2
            sid = "BO";
            ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
            rawAcl.InsertAce(0, ace);
            rawAclVerifier.InsertAce(0, ace);
            count = rawAcl.Count;
            index = count / 2;
            sid = "SO";
            ace = new CommonAce((AceFlags)flags, (AceQualifier)qualifier, accessMask, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), isCallback, new byte[opaqueSize]);
            rawAcl.InsertAce(index, ace);
            //verify the count number increase one
            Assert.True(rawAcl.Count == count + 1);
            //verify the inserted ace is equal to the originial ace
            aceVerifier = rawAcl[index];
            Assert.True(ace == aceVerifier);

            //verify right side aces are equal
            Assert.True(Utils.AclPartialEqual(rawAcl, rawAclVerifier, index + 1, rawAcl.Count - 1, index, count - 1));
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce genericAce = null;
            string owner = null;
            int index = 0;


            // case 1, no ACE, insert at index -1

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = -1;
                owner = "BA";
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(index, genericAce);
            });


            //case 2, no ACE, insert at  index Count + 1

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = rawAcl.Count + 1;
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(index, genericAce);
            });

            //case 3, one ACE, insert null ACE

            Assert.Throws<ArgumentNullException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = 0;
                owner = "BA";
                genericAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(index, genericAce);
                genericAce = null;
                rawAcl.InsertAce(index, genericAce);
            });
            //case 4, insert a big Ace to make RawAcl of length 64K + 1. RawAcl length = HeaderLength + all ACE's  length
            // = HeaderLength + (HeaderLength + OpaqueLength) * num_of_custom_ace
            // = 8 + ( 4 + OpaqueLength) * num_of_custom_ace

            Assert.Throws<OverflowException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                byte[] opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4];
                GenericAce gAce = new CustomAce(AceType.MaxDefinedAceType + 1, (AceFlags)223, opaque);
                rawAcl.InsertAce(0, gAce);
            });
        }
    }
}
