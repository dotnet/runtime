// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_Purge
    {
        private static bool TestPurge(DiscretionaryAcl discretionaryAcl, SecurityIdentifier sid, int aceCount)
        {

            KnownAce ace = null;

            discretionaryAcl.Purge(sid);
            if (aceCount != discretionaryAcl.Count)
                return false;
            for (int i = 0; i < discretionaryAcl.Count; i++)
            {
                ace = discretionaryAcl[i] as KnownAce;
                if (ace != null && ((ace.AceFlags & AceFlags.Inherited) == 0))
                {
                    if (ace.SecurityIdentifier == sid)
                        return false;
                }
            }
            return true;
        }

        [Fact]
        public static void Purge_BasicValidationTestCases()
        {
            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;
            int aceCount = 0;
            SecurityIdentifier sid = null;

            GenericAce gAce = null;
            byte revision = 0;
            int capacity = 0;

            //CustomAce constructor parameters
            AceType aceType = AceType.AccessAllowed;
            AceFlags aceFlag = AceFlags.None;
            byte[] opaque = null;

            //CompoundAce constructor additional parameters
            int accessMask = 0;
            CompoundAceType compoundAceType = CompoundAceType.Impersonation;
            string sidStr = "LA";

            //CommonAce constructor additional parameters
            AceQualifier aceQualifier = 0;

            //ObjectAce constructor additional parameters
            ObjectAceFlags objectAceFlag = 0;
            Guid objectAceType;
            Guid inheritedObjectAceType;

            //case 1, no Ace
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 0;
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

            //case 2, only have 1 explicit Ace of the sid
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));
            //199 has all aceflags but inheritonly and inherited					
            gAce = new CommonAce((AceFlags)199, AceQualifier.AccessAllowed, 1, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 0;

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

            //case 3, only have 1 explicit Ace of different sid
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //199 has all aceflags but inheritedonly and inherited
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));
            gAce = new CommonAce((AceFlags)199, AceQualifier.AccessDenied, 1, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 1;
            sidStr = "BA";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

            //case 4, only have 1 inherited Ace of the sid
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));
            //215 has all aceflags but inheritedonly				
            gAce = new CommonAce((AceFlags)215, AceQualifier.AccessAllowed, 1, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 1;

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

            //case 5, have one explicit Ace and one inherited Ace of the sid
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));
            //199 has all aceflags but inheritedonly and inherited
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            //215 has all aceflags but inheritedonly
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 2, sid, false, null);
            rawAcl.InsertAce(1, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 1;

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

            //case 6, have two explicit Aces of the sid
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            sidStr = "BG";
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sidStr));
            //207 has all AceFlags but inherited				
            gAce = new CommonAce((AceFlags)207, AceQualifier.AccessAllowed, 1, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessDenied, 2, sid, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 0;

            Assert.True(TestPurge(discretionaryAcl, sid, 0));

            //case 7, 1 explicit CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            //199 has all AceFlags except InheritOnly and Inherited
            aceFlag = (AceFlags)199;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG"));
            aceCount = 1;

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                TestPurge(discretionaryAcl, sid, aceCount);

            });


            //case 8,  1 explicit CompoundAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //207 has all AceFlags but inherited				
            aceFlag = (AceFlags)207;
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG"));
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType, sid);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 0;

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                TestPurge(discretionaryAcl, sid, aceCount);

            });

            //case 9, 1 explicit ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG"));
            //207 has all AceFlags but inherited						
            aceFlag = (AceFlags)207;
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask, sid, objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = true;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            aceCount = 0;

            Assert.True(TestPurge(discretionaryAcl, sid, aceCount));

        }

        [Fact]
        public static void Purge_AdditionalTestCases()
        {
            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;

            byte revision = 0;
            int capacity = 0;

            //case 1, null Sid
            Assert.Throws<ArgumentNullException>(() =>
            {
                revision = 127;
                capacity = 1;
                rawAcl = new RawAcl(revision, capacity);
                isContainer = true;
                isDS = false;
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.Purge(null);
            });
        }
    }
}
