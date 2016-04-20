// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_CopyTo
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            GenericAce gAce = null;
            RawAcl rAcl = null;
            GenericAce[] gAces = null;

            // Case 1, when collection is actually empty
            rAcl = new RawAcl(1, 1);
            gAces = new GenericAce[rAcl.Count];
            rAcl.CopyTo(gAces, 0);

            // Case 2, collection has one ACE
            rAcl = new RawAcl(0, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            gAces = new GenericAce[rAcl.Count];
            rAcl.CopyTo(gAces, 0);

            //Case 3, index = 3
            rAcl = new RawAcl(0, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
            rAcl.InsertAce(0, gAce);
            gAces = new GenericAce[rAcl.Count + 5];
            //initialize to null
            for (int i = 0; i < gAces.Length; i++)
                gAces[i] = null;
            rAcl.CopyTo(gAces, 3);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            GenericAce gAce = null;
            RawAcl rAcl = null;
            GenericAce[] gAces = null;


            // case 1, null array
            Assert.Throws<ArgumentNullException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                rAcl.CopyTo(gAces, 0);
            });

            // case 2, negative index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                gAces = new GenericAce[rAcl.Count];
                rAcl.CopyTo(gAces, -1);
            });

            // case 3, insufficient size array
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                gAces = new GenericAce[0];
                rAcl.CopyTo(gAces, 0);
            });

            // Case 4, insufficient size array
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                rAcl.InsertAce(0, gAce);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BO")), false, null);
                rAcl.InsertAce(0, gAce);

                gAces = new GenericAce[rAcl.Count - 1];
                rAcl.CopyTo(gAces, 0);
            });
            
            //case 5, RawAcl with huge number of Aces
            rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength);
            for (int i = 0; i < 1820; i++)
            {
                //this ace binary length is 36, 1820 * 36 = 65520                        
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
            }
            gAces = new GenericAce[rAcl.Count];
            rAcl.CopyTo(gAces, 0);
        }
    }
}
