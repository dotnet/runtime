// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_BinaryLength
    {
        [Fact]
        public static void BinaryLength_BasicValidation()
        {
            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";

            //case 1, empty discretionaryAcl, binarylength should be 8
            capacity = 1;
            discretionaryAcl = new DiscretionaryAcl(false, false, capacity);
            Assert.True(8 == discretionaryAcl.BinaryLength);

            //case 2, discretionaryAcl with one Ace, binarylength should be 8 + the Ace's binarylength
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(true, false, rawAcl);
            Assert.True(8 + gAce.BinaryLength == discretionaryAcl.BinaryLength);

            //case 3, DiscretionaryAcl with two Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 2,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
            Assert.True(8 + discretionaryAcl[0].BinaryLength + discretionaryAcl[1].BinaryLength == discretionaryAcl.BinaryLength);


        }

        [Fact]
        public static void BinaryLength_AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;
            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;
            string sid = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
            int expectedLength = 0;

            //case 1, DiscretionaryAcl with huge number of Aces
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            expectedLength = 8;

            for (int i = 0; i < 1820; i++)
            {
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, i + 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + i.ToString())), false, null);
                rawAcl.InsertAce(0, gAce);
                expectedLength += gAce.BinaryLength;
            }
            discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
            Assert.True(expectedLength == discretionaryAcl.BinaryLength);

        }
    }
}