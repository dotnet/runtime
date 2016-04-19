// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class DiscretionaryAcl_Index
    {
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
                return false;
        }

        [Fact]
        public static void Index_BasicValidation()
        {
            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;

            GenericAce gAce = null;
            GenericAce verifierGAce = null;
            string owner1 = "BO";
            string owner2 = "BA";
            string owner3 = "BG";
            int index = 0;

            // case 1, only one ACE, get at index 0
            rawAcl = new RawAcl(1, 1);
            index = 0;
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
            verifierGAce = discretionaryAcl[index];

            Assert.True(TestIndex(gAce, verifierGAce));
            {
            }

            //case 2, two ACEs, index at Count -1
            rawAcl = new RawAcl(1, 2);
            //215 has all AceFlags but InheriteOnly
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, gAce);
            //199 has all AceFlags but InheritedOnly, Inherited
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, gAce);
            discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
            gAce = rawAcl[1];
            //the discretionaryAcl is non-container, all AceFlags except Inherited will be stripped
            gAce.AceFlags = (AceFlags)FlagsForAce.None;
            index = discretionaryAcl.Count - 1;

            verifierGAce = discretionaryAcl[index];

            Assert.True(TestIndex(gAce, verifierGAce));
            {
            }

            //case 3, only three ACEs, index at Count/2
            rawAcl = new RawAcl(1, 3);

            //215 has all AceFlags except InheritOnly					
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner1)), false, null);
            rawAcl.InsertAce(0, gAce);

            //199 has all AceFlags except InheritOnly and Inherited				
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner2)), false, null);
            rawAcl.InsertAce(1, gAce);
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner3)), false, null);
            rawAcl.InsertAce(2, gAce);
            discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
            gAce = rawAcl[1];
            //the systemAcl is non-container, all AceFlags will be stripped
            gAce.AceFlags = AceFlags.None;
            index = discretionaryAcl.Count / 2;
            verifierGAce = discretionaryAcl[index];

            Assert.True(TestIndex(gAce, verifierGAce));
            {
            }

        }

        [Fact]
        public static void Index_AdditionalTests()
        {
            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;
            GenericAce gAce = null;
            GenericAce verifierGAce = null;
            string owner = null;
            int index = 0;

            // case 1, no ACE, get index at -1
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                index = -1;
                discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                verifierGAce = discretionaryAcl[index];
            });

            //case 2, get index at Count
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                index = discretionaryAcl.Count;
                verifierGAce = discretionaryAcl[index];
            });

            //case 3, set index at -1
            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                index = -1;
                owner = "BG";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                discretionaryAcl[index] = gAce;

            });

            //case 4, set index at Count
            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                discretionaryAcl = new DiscretionaryAcl(true, false, rawAcl);
                index = discretionaryAcl.Count;
                owner = "BG";
                gAce = new CommonAce(AceFlags.SuccessfulAccess, AceQualifier.SystemAudit, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                discretionaryAcl[index] = gAce;

            });

            //case 5, set null Ace
            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, gAce);
                discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                index = 0;
                gAce = null;
                discretionaryAcl[index] = gAce;

            });

            //case 6, set index at 0
            //case 5, set null Ace
            Assert.Throws<NotSupportedException>(() =>
            {
                rawAcl = new RawAcl(1, 1);
                owner = "BG";
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                rawAcl.InsertAce(0, gAce);

                discretionaryAcl = new DiscretionaryAcl(false, false, rawAcl);
                index = 0;
                owner = "BA";
                gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(owner)), false, null);
                discretionaryAcl[index] = gAce;
            });
        }
    }
}