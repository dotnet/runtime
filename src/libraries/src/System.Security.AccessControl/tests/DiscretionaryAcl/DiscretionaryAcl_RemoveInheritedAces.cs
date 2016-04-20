// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_RemoveInheritedAces
    {
        private static bool TestRemoveInheritedAces(DiscretionaryAcl discretionaryAcl)
        {

            GenericAce ace = null;
            discretionaryAcl.RemoveInheritedAces();
            for (int i = 0; i < discretionaryAcl.Count; i++)
            {
                ace = discretionaryAcl[i];
                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                    return false;
            }
            return true;
        }

        [Fact]
        public static void RemoveInheritedAces_BasicValidationTestCases()
        {
            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;

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
            string sid = "BG";

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

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 2, only have explicit Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //199  has all AceFlags except InheritOnly and Inherited
            gAce = new CommonAce((AceFlags)199, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 3,  non-inherited CommonAce, ObjectAce, CompoundAce, CustomAce
            revision = 127;
            capacity = 5;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = AceFlags.InheritanceFlags;
            accessMask = 1;

            //Access Allowed CommonAce
            gAce = new CommonAce(aceFlag, AceQualifier.AccessAllowed, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
            rawAcl.InsertAce(0, gAce);
            //Access Dennied CommonAce
            gAce = new CommonAce(aceFlag, AceQualifier.AccessDenied, accessMask,
            new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
            rawAcl.InsertAce(0, gAce);
            //CustomAce
            aceType = AceType.MaxDefinedAceType + 1;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(2, gAce);
            //CompoundAce
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())));
            rawAcl.InsertAce(3, gAce);
            //ObjectAce
            aceQualifier = AceQualifier.AccessAllowed;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(2, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                TestRemoveInheritedAces(discretionaryAcl);
            });

            //case 4,  all inherited CommonAce, ObjectAce, CompoundAce, CustomAce
            revision = 127;
            capacity = 5;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = AceFlags.InheritanceFlags | AceFlags.Inherited;
            accessMask = 1;

            //Access Allowed CommonAce
            gAce = new CommonAce(aceFlag, AceQualifier.AccessAllowed, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
            rawAcl.InsertAce(0, gAce);
            //Access Dennied CommonAce
            gAce = new CommonAce(aceFlag, AceQualifier.AccessDenied, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
            rawAcl.InsertAce(0, gAce);
            //CustomAce
            aceType = AceType.MaxDefinedAceType + 1;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            //CompoundAce
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())));
            rawAcl.InsertAce(0, gAce);
            //ObjectAce
            aceQualifier = AceQualifier.AccessAllowed;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 5, only have one inherit Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //215 has all AceFlags except InheritOnly
            gAce = new CommonAce((AceFlags)215, AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 6, have one explicit Ace and one inherited Ace
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //199  has all AceFlags except InheritOnly and Inherited					
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP), AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            //215  has all AceFlags except InheritOnly					
            gAce = new CommonAce((AceFlags)(FlagsForAce.AuditFlags | FlagsForAce.OI | FlagsForAce.CI | FlagsForAce.NP | FlagsForAce.IH), AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(1, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 7, have two inherited Aces
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //215  has all AceFlags except InheritOnly					
            gAce = new CommonAce((AceFlags)215, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            sid = "BA";
            //16 has Inherited
            gAce = new CommonAce((AceFlags)16, AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 8, 1 inherited CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            //215 has all AceFlags except InheritOnly
            aceFlag = (AceFlags)215;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 9,  1 inherited CompoundAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)223; //all flags ored together
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)));
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

            //case 10, 1 inherited ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)223; //all flags ored together
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = true;
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(discretionaryAcl));

        }
    }
}