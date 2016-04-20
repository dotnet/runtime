// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_RemoveInheritedAces
    {
        [Fact]
        public static void BasicValidationTestCases()
        {
            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

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
            string sid = "BA";
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
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));


            //case 2, only have explicit Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //199 has all AceFlags except InheritOnly and Inherited
            gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));

            //case 3, only have one inherit Ace
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //215 has all AceFlags except InheritOnly
            gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));


            //case 4, have one explicit Ace and one inherited Ace
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //199 has all AceFlags except InheritOnly and Inherited                    
            gAce = new CommonAce((AceFlags)199, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            //215 has all AceFlags except InheritOnly                    
            gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(1, gAce);
            isContainer = true;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));

            //case 5, have two inherited Aces
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //215 has all AceFlags except InheritOnly                    
            gAce = new CommonAce((AceFlags)215, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), false, null);
            rawAcl.InsertAce(0, gAce);
            sid = "BA";
            //16 has Inherited
            gAce = new CommonAce((AceFlags)16, AceQualifier.SystemAudit, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));

            //case 6, 1 inherited CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = (AceFlags)208; //SuccessfulAccess | FailedAccess | Inherited
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);

            Assert.True(TestRemoveInheritedAces(systemAcl));

            //case 7,  1 inherited CompoundAce
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
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));

            //case 8, 1 inherited ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)223; //all flags ored together
            aceQualifier = AceQualifier.SystemAudit;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = true;
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveInheritedAces(systemAcl));

        }
        
        private static bool TestRemoveInheritedAces(SystemAcl systemAcl)
        {
            GenericAce ace = null;
            systemAcl.RemoveInheritedAces();
            for (int i = 0; i < systemAcl.Count; i++)
            {
                ace = systemAcl[i];
                if ((ace.AceFlags & AceFlags.Inherited) != 0)
                    return false;
            }
            return true;
        }

    }
}