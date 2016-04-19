// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_BinaryLength
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";

            //case 1, empty RawAcl, binarylength should be 8
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            Assert.True(8 == rawAcl.BinaryLength);

            //case 2, RawAcl with one Ace, binarylength should be 8 + the Ace's binarylength
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            Assert.True(8 + gAce.BinaryLength == rawAcl.BinaryLength);

            //case 3, RawAcl with two Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            Assert.True(8 + rawAcl[0].BinaryLength + rawAcl[1].BinaryLength == rawAcl.BinaryLength);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";
            int expectedLength = 0;

            //case 1, RawAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            expectedLength = 8;
            for (int i = 0; i < 1820; i++)
            {    //this ace binary length is 36, 1820 * 36 = 65520        
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
                rawAcl.InsertAce(0, gAce);
                expectedLength += gAce.BinaryLength;
            }
            Assert.True(expectedLength == rawAcl.BinaryLength);
        }
    }
}
