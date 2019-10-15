// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_GetBinaryForm
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            SystemAcl sAcl = null;
            RawAcl rAcl = null;
            GenericAce gAce = null;
            byte[] binaryForm = null;

            //Case 1, array binaryForm is null

            Assert.Throws<ArgumentNullException>(() =>
            {
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                sAcl = new SystemAcl(false, false, rAcl);
                sAcl.GetBinaryForm(binaryForm, 0);
            });
            //Case 2, offset is negative

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[100];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                sAcl = new SystemAcl(false, false, rAcl);
                sAcl.GetBinaryForm(binaryForm, -1);
            });
            //Case 3, offset is equal to binaryForm length

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[100];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                sAcl = new SystemAcl(false, false, rAcl);
                sAcl.GetBinaryForm(binaryForm, binaryForm.Length);
            });
            //Case 4, offset is a big possitive number

            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            sAcl = new SystemAcl(false, false, rAcl);
            binaryForm = new byte[sAcl.BinaryLength + 10000];
            sAcl.GetBinaryForm(binaryForm, 10000);
            //get the binaryForm of the original RawAcl
            byte[] verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(verifierBinaryForm, 0);
            Assert.True(Utils.IsBinaryFormEqual(binaryForm, 10000, verifierBinaryForm));

            //Case 5, binaryForm array's size is insufficient

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                binaryForm = new byte[4];
                rAcl = new RawAcl(GenericAcl.AclRevision, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                sAcl = new SystemAcl(false, false, rAcl);
                sAcl.GetBinaryForm(binaryForm, 0);
            });
        }
    }
}