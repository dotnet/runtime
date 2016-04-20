// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_Constructor3
    {
        public static IEnumerable<object[]> SystemAcl_Constructor3_TestData()
        {
            yield return new object[] { false, false, "64:2:1:BA:false:0#128:2:0:BG:false:0#80:2:2:BO:false:0", "64:2:1:BA:false:0#80:2:2:BO:false:0", true };
            yield return new object[] { false, false, "73:2:1:BA:false:0#64:2:1:BG:false:0#80:2:2:BO:false:0", "64:2:1:BG:false:0#80:2:2:BO:false:0", true };
            yield return new object[] { true, false, "73:2:1:BA:false:0#64:2:1:BG:false:0#80:2:2:BO:false:0", "73:2:1:BA:false:0#64:2:1:BG:false:0#80:2:2:BO:false:0", true };
            yield return new object[] { false, false, "199:2:1:BA:false:0", "192:2:1:BA:false:0", true };
            yield return new object[] { true, false, "199:2:1:BA:false:0", "199:2:1:BA:false:0", true };
            yield return new object[] { false, false, "128:2:0:BG:false:0#72:2:1:BA:false:0#64:2:1:BO:false:0", "64:2:1:BO:false:0", true };
            yield return new object[] { true, false, "64:2:1:BA:false:0#80:2:2:BO:false:0#136:2:1:BG:false:0", "64:2:1:BA:false:0#80:2:2:BO:false:0", true };
            yield return new object[] { true, false, "196:2:1:BA:false:0", "192:2:1:BA:false:0", true };
            yield return new object[] { true, false, "15:2:1:BA:false:0#64:2:1:BG:false:0", "64:2:1:BG:false:0", true };
            yield return new object[] { false, false, "192:0:1:BA:false:0#64:2:1:BG:false:0", "64:2:1:BG:false:0", true };
            yield return new object[] { false, false, "192:1:1:BA:false:0#64:2:1:BG:false:0", "64:2:1:BG:false:0", true };
            yield return new object[] { false, false, "192:3:1:BA:false:0#64:2:1:BG:false:0", "64:2:1:BG:false:0", true };
            yield return new object[] { false, false, "192:2:1:BA:false:0#80:2:2:BG:false:0", "192:2:1:BA:false:0#80:2:2:BG:false:0", true };
            yield return new object[] { false, false, "80:2:2:BG:false:0#192:2:1:BA:false:0", "80:2:2:BG:false:0#192:2:1:BA:false:0", false };
            yield return new object[] { false, false, "80:2:2:BG:false:0#208:2:1:BA:false:0", "80:2:2:BG:false:0#208:2:1:BA:false:0", true };
            yield return new object[] { false, false, "192:0:1:BA:false:0#192:0:2:BA:false:0#64:2:1:BG:false:0#64:0:3:BG:false:0", "64:2:1:BG:false:0", true };
            yield return new object[] { false, false, "64:2:1:BG:false:0#192:2:1:BA:false:0#208:2:2:BA:false:0", "192:2:1:BA:false:0#64:2:1:BG:false:0#208:2:2:BA:false:0", true };
            yield return new object[] { false, false, "64:1:1:BG:false:0#64:2:2:BA:false:0#192:2:1:BG:false:0", "64:2:2:BA:false:0#192:2:1:BG:false:0", true };
            yield return new object[] { false, false, "128:2:1:BG:false:0#64:2:1:BA:false:0#64:2:2:BO:false:0", "64:2:1:BA:false:0#128:2:1:BG:false:0#64:2:2:BO:false:0", true };
            yield return new object[] { false, false, "128:2:1:BG:false:0#64:2:1:BA:false:0#64:2:2:BA:false:0", "64:2:3:BA:false:0#128:2:1:BG:false:0", true };
            yield return new object[] { false, false, "128:2:1:BG:false:0#64:2:1:BA:false:0#64:2:1:BA:false:0", "64:2:1:BA:false:0#128:2:1:BG:false:0", true };
            yield return new object[] { true, false, "128:2:1:BG:false:0#65:2:1:BA:false:0#129:2:1:BA:false:0", "193:2:1:BA:false:0#128:2:1:BG:false:0", true };
            yield return new object[] { true, false, "192:2:1:BO:false:0#66:2:1:BA:false:0#70:2:1:BA:false:0", "66:2:1:BA:false:0#192:2:1:BO:false:0", true };
            yield return new object[] { true, false, "64:2:1:BO:false:0#130:2:1:BA:false:0#135:2:1:BA:false:0", "135:2:1:BA:false:0#130:2:1:BA:false:0#64:2:1:BO:false:0", true };
        }

        private static bool TestConstructor(SystemAcl systemAcl, bool isContainer, bool isDS, bool wasCanonicalInitially, RawAcl rawAcl)
        {
            bool result = true;
            byte[] sAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            if (systemAcl.IsContainer == isContainer &&
                systemAcl.IsDS == isDS &&
                systemAcl.Revision == rawAcl.Revision &&
                systemAcl.Count == rawAcl.Count &&
                systemAcl.BinaryLength == rawAcl.BinaryLength &&
                systemAcl.IsCanonical == wasCanonicalInitially)
            {
                sAclBinaryForm = new byte[systemAcl.BinaryLength];
                rAclBinaryForm = new byte[rawAcl.BinaryLength];
                systemAcl.GetBinaryForm(sAclBinaryForm, 0);
                rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                if (!Utils.IsBinaryFormEqual(sAclBinaryForm, rAclBinaryForm))
                    result = false;
                //redundant index check
                for (int i = 0; i < systemAcl.Count; i++)
                {
                    if (!Utils.IsAceEqual(systemAcl[i], rawAcl[i]))
                    {
                        result = false;
                        break;
                    }
                }

            }
            else
            {

                result = false;
            }
            return result;
        }

        [Theory]
        [MemberData(nameof(SystemAcl_Constructor3_TestData))]
        public static void BasicValidationTestCases(bool isContainer, bool isDS, string initialRawAclStr, string verifierRawAclStr, bool wasCanonicalInitially)
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

            //create a systemAcl
            rawAcl = Utils.CreateRawAclFromString(initialRawAclStr);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestConstructor(systemAcl, isContainer, isDS, wasCanonicalInitially, rawAcl));
        }

        [Fact]
        public static void AdditionalTestCases()
        {

            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            SystemAcl sAcl = null;

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
            //case 1, an SystemAudit ACE with a zero access mask is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.AuditFlags,
                    AceQualifier.SystemAudit,
                    0,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    false,
                    null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //the only ACE is a meaningless ACE, will be removed
            //drop the ace from the rawAcl
            rawAcl.RemoveAce(0);
            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));


            //case 2, an inherit-only SystemAudit ACE on an object ACL is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.InheritanceFlags | AceFlags.AuditFlags,
                    AceQualifier.SystemAudit,
                    1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //the only ACE is a meaningless ACE, will be removed
            rawAcl.RemoveAce(0);

            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));

            //case 3, an inherit-only SystemAudit ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //200 has inheritOnly, SuccessfulAccess and FailedAccess
            gAce = new CommonAce((AceFlags)200,
                    AceQualifier.SystemAudit,
                    1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    false,
                    null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //the only ACE is a meaningless ACE, will be removed
            rawAcl.RemoveAce(0);
            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));

            //case 4, a SystemAudit ACE without Success or Failure Flags is meaningless, will be removed
            revision = 255;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None,
        AceQualifier.SystemAudit,
        1,
        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
        false,
        null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //audit ACE does not specify either Success or Failure Flags is removed
            rawAcl.RemoveAce(0);
            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));

            //case 5, a CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = AceFlags.AuditFlags;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical
            Assert.True(TestConstructor(sAcl, isContainer, isDS, false, rawAcl));

            //case 6, a CompoundAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = AceFlags.AuditFlags;
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag,
                accessMask,
                compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)));
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical
            Assert.True(TestConstructor(sAcl, isContainer, isDS, false, rawAcl));

            //case 7, a ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = AceFlags.InheritanceFlags | AceFlags.AuditFlags;
            aceQualifier = AceQualifier.SystemAudit;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag,
                aceQualifier,
                accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                objectAceFlag, objectAceType,
                inheritedObjectAceType,
                false,
                null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = true;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));

            //case 8, no Ace
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            isContainer = true;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestConstructor(sAcl, isContainer, isDS, true, rawAcl));

            //case 9, Aces from case 1 to 7 
            revision = 127;
            capacity = 5;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)).ToString();
            rawAcl = new RawAcl(revision, capacity);
            //an SystemAudit ACE with a zero access mask
            //is meaningless, will be removed                    
            gAce = new CommonAce(AceFlags.AuditFlags,
                    AceQualifier.SystemAudit,
                    0,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid) + 1.ToString()),
                    false,
                    null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            //an inherit-only SystemAudit ACE without ContainerInherit or ObjectInherit flags on a container object
            //is meaningless, will be removed
            //200 has inheritOnly, SuccessfulAccess and FailedAccess
            gAce = new CommonAce((AceFlags)200,
                    AceQualifier.SystemAudit,
                    1,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid) + 2.ToString()),
                    false,
                    null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            //a SystemAudit ACE without Success or Failure Flags
            //is meaningless, will be removed                    
            gAce = new CommonAce(AceFlags.None,
        AceQualifier.SystemAudit,
        1,
        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid) + 3.ToString()),
        false,
        null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            //a ObjectAce
            aceFlag = AceFlags.InheritanceFlags | AceFlags.AuditFlags;
            aceQualifier = AceQualifier.SystemAudit;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag,
                aceQualifier,
                accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid) + 4.ToString()),
                objectAceFlag, objectAceType,
                inheritedObjectAceType,
                false,
                null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            // a CustomAce
            gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                AceFlags.AuditFlags,
                null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            //a CompoundAce
            gAce = new CompoundAce(AceFlags.AuditFlags,
                1,
                CompoundAceType.Impersonation,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid) + 5.ToString()));
            rawAcl.InsertAce(rawAcl.Count, gAce);

            isContainer = true;
            isDS = false;
            sAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //the first 3 Aces will be removed by SystemAcl constructor
            rawAcl.RemoveAce(0);
            rawAcl.RemoveAce(0);
            rawAcl.RemoveAce(0);
            //Mark changed design to make ACL with any CustomAce, CompoundAce uncanonical
            Assert.True(TestConstructor(sAcl, isContainer, isDS, false, rawAcl));

        }

        [Fact]
        public static void Additional2TestCases()
        {
            SystemAcl systemAcl = null;
            bool isContainer = false;
            bool isDS = false;
            RawAcl rawAcl = null;

            //case 1, rawAcl = null
            Assert.Throws<ArgumentNullException>(() =>
            {
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            });
        }
    }
}