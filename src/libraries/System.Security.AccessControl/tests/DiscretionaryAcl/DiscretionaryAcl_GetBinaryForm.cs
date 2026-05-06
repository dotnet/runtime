// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class DiscretionaryAcl_GetBinaryForm
    {
        [Fact]
        public static void GetBinaryForm_BasicValidationTestCases()
        {
            DiscretionaryAcl dAcl = null;
            RawAcl rAcl = null;
            GenericAce gAce = null;
            byte[] binaryForm = null;

            //Case 1, array binaryForm is null
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            dAcl = new DiscretionaryAcl(true, false, rAcl);

            Assert.Throws<ArgumentNullException>(() =>
            {
                dAcl.GetBinaryForm(binaryForm, 0);

            });


            //Case 2, offset is negative
            binaryForm = new byte[100];
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            dAcl = new DiscretionaryAcl(true, false, rAcl);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dAcl.GetBinaryForm(binaryForm, -1);
            });


            //Case 3, offset is equal to binaryForm length
            binaryForm = new byte[100];
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            dAcl = new DiscretionaryAcl(true, false, rAcl);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dAcl.GetBinaryForm(binaryForm, binaryForm.Length);
            });

            //Case 4, offset is a big positive number
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            dAcl = new DiscretionaryAcl(true, false, rAcl);
            binaryForm = new byte[dAcl.BinaryLength + 10000];

            dAcl.GetBinaryForm(binaryForm, 10000);
            //get the binaryForm of the original RawAcl
            byte[] verifierBinaryForm = new byte[rAcl.BinaryLength];
            rAcl.GetBinaryForm(verifierBinaryForm, 0);
            Assert.True(Utils.IsBinaryFormEqual(binaryForm, 10000, verifierBinaryForm));

            //Case 5, binaryForm array's size is insufficient
            binaryForm = new byte[4];
            rAcl = new RawAcl(GenericAcl.AclRevision, 1);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            dAcl = new DiscretionaryAcl(true, false, rAcl);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                dAcl.GetBinaryForm(binaryForm, 0);

            });



        }
    }
}
