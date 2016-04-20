// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_Index
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

            GenericAce gAce = null;
            GenericAce verifierGAce = null;
            string owner1 = "BO";
            string owner2 = "BA";
            string owner3 = "BG";
            int index = 0;

            // case 1, only one ACE, get at index 0
            rawAcl = new RawAcl(1, 1);
            index = 0;
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, gAce);
            systemAcl = new SystemAcl(false, false, rawAcl);
            verifierGAce = systemAcl[index];
            Assert.True(TestIndex(gAce, verifierGAce));

            //case 2, two ACEs, index at Count -1
            rawAcl = new RawAcl(1, 2);
            //208 has SuccessfulAccess, FailedAccess and Inherited
            gAce = new CommonAce((AceFlags)208, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, gAce);
            //gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(owner2), false, null);                    
            gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(1, gAce);
            systemAcl = new SystemAcl(false, false, rawAcl);
            gAce = rawAcl[1];
            index = systemAcl.Count - 1;

            verifierGAce = systemAcl[index];
            Assert.True(TestIndex(gAce, verifierGAce));

            //case 3, only three ACEs, index at Count/2
            rawAcl = new RawAcl(1, 3);
            //215 has all AceFlags except InheritOnly                    
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(0, gAce);
            //208 has SuccessfulAccess, FailedAccess and Inherited
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.IH), AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(1, gAce);

            gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
            rawAcl.InsertAce(2, gAce);
            systemAcl = new SystemAcl(false, false, rawAcl);
            gAce = rawAcl[1];
            index = systemAcl.Count / 2;
            verifierGAce = systemAcl[index];
            Assert.True(TestIndex(gAce, verifierGAce));
            //case 4, 3 ACEs, test merge, index at Count -1
            rawAcl = new RawAcl(1, 2);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(1, gAce);
            gAce = new CommonAce(AceFlags.FailedAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(2, gAce);
            systemAcl = new SystemAcl(false, false, rawAcl);
            gAce = rawAcl[1];
            gAce.AceFlags = AceFlags.SuccessfulAccess | AceFlags.FailedAccess;
            index = systemAcl.Count - 1;

            verifierGAce = systemAcl[index];
            Assert.True(TestIndex(gAce, verifierGAce));
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;
            GenericAce gAce = null;
            GenericAce verifierGAce = null;
            string owner = null;
            int index = 0;


            // case 1, no ACE, get index at -1


            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = -1;
                systemAcl = new SystemAcl(false, false, rawAcl);
                verifierGAce = systemAcl[index];
            });

            //case 2, no ACE, get index at Count


            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                systemAcl = new SystemAcl(false, false, rawAcl);
                index = systemAcl.Count;
                verifierGAce = systemAcl[index];
            });

            //case 3, no ACE, set index at -1


            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                systemAcl = new SystemAcl(false, false, rawAcl);
                index = -1;
                owner = "BA";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                systemAcl[index] = gAce;
            });

            //case 4, no ACE, set index at Count


            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                systemAcl = new SystemAcl(true, false, rawAcl);
                index = systemAcl.Count;
                owner = "BA";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                systemAcl[index] = gAce;
            });
            //case 5, set null Ace

            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, gAce);
                systemAcl = new SystemAcl(false, false, rawAcl);
                index = 0;
                gAce = null;
                systemAcl[index] = gAce;
            });
            //case 6, set index at 0


            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                owner = "BA";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, gAce);

                systemAcl = new SystemAcl(false, false, rawAcl);
                index = 0;
                owner = "BA";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                systemAcl[index] = gAce;
            });
        }

        private static bool TestIndex(GenericAce gAce, GenericAce verifierGAce)
        {
            if (Utils.IsAceEqual(gAce, verifierGAce))
            {//as operator == and != are overridden to by value, can not use != to test these two are not same object any more
                gAce.AceFlags = AceFlags.InheritanceFlags | AceFlags.Inherited | AceFlags.AuditFlags;
                if (gAce != verifierGAce)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

    }
}