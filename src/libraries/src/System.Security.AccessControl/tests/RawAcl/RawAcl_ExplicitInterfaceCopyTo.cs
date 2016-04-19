// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class RawAcl_ExplicitInterfaceCopyTo
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            ICollection myCollection = null;
            GenericAce gAce = null;
            RawAcl rAcl = null;
            GenericAce[] gAces = null;

            //Case 1, when collection is actually empty
            rAcl = new RawAcl(1, 1);
            gAces = new GenericAce[rAcl.Count];
            myCollection = (ICollection)rAcl;
            myCollection.CopyTo(gAces, 0);

            //Case 2, collection has one ACE
            rAcl = new RawAcl(0, 1);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rAcl.InsertAce(0, gAce);
            gAces = new GenericAce[rAcl.Count];
            myCollection = (ICollection)rAcl;
            myCollection.CopyTo(gAces, 0);

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
            myCollection = (ICollection)rAcl;
            myCollection.CopyTo(gAces, 3);
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            ICollection myCollection = null;
            GenericAce gAce = null;
            RawAcl rAcl = null;
            GenericAce[] gAces = null;


            // Case 1, null array
            Assert.Throws<ArgumentNullException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                myCollection = (ICollection)rAcl;
                myCollection.CopyTo(gAces, 0);
            });

            // Case 2, negative index
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                gAces = new GenericAce[rAcl.Count];
                myCollection = (ICollection)rAcl;
                myCollection.CopyTo(gAces, -1);
            });

            // Case 3, 0 size array
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                gAces = new GenericAce[0];
                myCollection = (ICollection)rAcl;
                myCollection.CopyTo(gAces, 0);
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
                myCollection = (ICollection)rAcl;
                myCollection.CopyTo(gAces, 0);
            });
            
            //Case 5, RawAcl with huge number of Aces
            rAcl = new RawAcl(0, GenericAcl.MaxBinaryLength);
            for (int i = 0; i < 1820; i++)
            {
                //this ace binary length is 36, 1820 * 36 = 65520
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, i + 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
            }
            gAces = new GenericAce[rAcl.Count];
            myCollection = (ICollection)rAcl;
            myCollection.CopyTo(gAces, 0);

            // Case 6, test ICollection.CopyTo, array rank is not one. all the other cases are tested by type-friendly version CopyTo
            //on my machine, a BCL assert as resource Rank_MutiDimNotSupported not found
            Assert.Throws<RankException>(() =>
            {
                rAcl = new RawAcl(0, 1);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
                rAcl.InsertAce(0, gAce);
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
                rAcl.InsertAce(0, gAce);
                GenericAce[,] gAces2 = new GenericAce[1, 2];
                myCollection = (ICollection)rAcl;
                myCollection.CopyTo(gAces2, 0);
            });
        }
    }
}
