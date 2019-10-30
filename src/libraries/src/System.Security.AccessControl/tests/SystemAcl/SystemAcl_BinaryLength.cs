// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_BinaryLength
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";
            int expectedLength = 0;
            //case 1, empty systemAcl, binarylength should be 8
            capacity = 1;
            systemAcl = new SystemAcl(false, false, capacity);
            expectedLength = 8;
            Assert.True(expectedLength == systemAcl.BinaryLength);

            //case 2, SystemAcl with one Ace, binarylength should be 8 + the Ace's binarylength
            expectedLength = 8;
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            expectedLength += gAce.BinaryLength;
            rawAcl.InsertAce(0, gAce);
            systemAcl = new SystemAcl(true, false, rawAcl);
            Assert.True(expectedLength == systemAcl.BinaryLength);

            //case 3, SystemAcl with two Aces
            expectedLength = 8;
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            expectedLength += gAce.BinaryLength;
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 2, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            expectedLength += gAce.BinaryLength;
            rawAcl.InsertAce(0, gAce);
            systemAcl = new SystemAcl(false, false, rawAcl);
            Assert.True(expectedLength == systemAcl.BinaryLength);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BA";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
            int expectedLength = 0;

            //case 1, SystemAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            expectedLength = 8;
            for (int i = 0; i < 1820; i++)
            {
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                rawAcl.InsertAce(0, gAce);
                expectedLength += gAce.BinaryLength;
            }
            systemAcl = new SystemAcl(false, false, rawAcl);
            Assert.True(expectedLength == systemAcl.BinaryLength);
        }
    }
}