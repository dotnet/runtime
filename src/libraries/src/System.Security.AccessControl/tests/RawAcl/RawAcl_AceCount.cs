// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_AceCount
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";

            //case 1, empty RawAcl
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            Assert.True(0 == rawAcl.Count);


            //case 2, RawAcl with one Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            Assert.True(1 == rawAcl.Count);

            //case 3, RawAcl with two Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            Assert.True(2 == rawAcl.Count);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            SecurityIdentifier sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA"));

            //case 1, RawAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            for (int i = 0; i < 1820; i++)
            {
                //this ace binary length is 36, 1820 * 36 = 65520                        
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, sid, false, null);
                rawAcl.InsertAce(0, gAce);
            }
            Assert.True(1820 == rawAcl.Count);
        }
    }
}
