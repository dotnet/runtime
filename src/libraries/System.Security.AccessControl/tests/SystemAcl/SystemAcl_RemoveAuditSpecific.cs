// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_RemoveAuditSpecific
    {
        public static IEnumerable<object[]> SystemAcl_RemoveAuditSpecific_TestData()
        {
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "64:2:1:BA:false:0#64:2:1:BG:false:0#64:2:1:BO:false:0", "64:2:1:BG:false:0#64:2:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BO", 1, 0, 0, "64:2:1:BA:false:0#64:2:1:BG:false:0#64:2:1:BO:false:0", "64:2:1:BA:false:0#64:2:1:BG:false:0 " };
            yield return new object[] { true, false, 1, "BG", 1, 0, 0, "64:2:1:BA:false:0#64:2:1:BG:false:0#64:2:1:BO:false:0", "64:2:1:BA:false:0#64:2:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "64:2:1:BA:false:0#64:2:1:BA:false:0#64:2:1:BA:false:0#64:2:1:BO:false:0", "64:2:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "80:2:1:BA:false:0", "80:2:1:BA:false:0" };
            yield return new object[] { true, false, 2, "BA", 1, 0, 0, "64:2:1:BA:false:0", "64:2:1:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "128:2:1:BA:false:0", "128:2:1:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "64:2:1:BO:false:0", "64:2:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 0, 0, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 0, 1, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 0, 3, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 0, 4, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 1, 0, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 3, 1, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 3, 4, 1, "79:2:3:BA:false:0", "79:2:3:BA:false:0" };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "64:2:3:BA:false:0", "64:2:3:BA:false:0" };
        }

        [Theory]
        [MemberData(nameof(SystemAcl_RemoveAuditSpecific_TestData))]
        public static void BasicValidationTestCases(bool isContainer, bool isDS, int auditFlags, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

            //create a systemAcl
            rawAcl = Utils.CreateRawAclFromString(initialRawAclStr);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags,
new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
        }

        private static bool TestRemoveAuditSpecific(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;
            byte[] sAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            systemAcl.RemoveAuditSpecific(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
            if (systemAcl.Count == rawAcl.Count &&
                systemAcl.BinaryLength == rawAcl.BinaryLength)
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
                result = false;
            return result;
        }


        [Fact]

        public static void AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;
            bool isContainer = false;
            bool isDS = false;

            int auditFlags = 0;
            string sid = null;
            int accessMask = 1;
            int inheritanceFlags = 0;
            int propagationFlags = 0;
            GenericAce gAce = null;
            byte[] opaque = null;


            //Case 1, remove one audit ACE from the SystemAcl with no ACE
            isContainer = true;
            isDS = false;
            auditFlags = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));


            //Case 2, remove the last one audit ACE from the SystemAcl
            isContainer = true;
            isDS = false;
            auditFlags = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            //79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)79, AceQualifier.SystemAudit, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //remove the ace to create the validation rawAcl
            rawAcl.RemoveAce(rawAcl.Count - 1);
            Assert.True(TestRemoveAuditSpecific(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));

            //Case 3, accessMask = 0

            AssertExtensions.Throws<ArgumentException>("accessMask", () =>
            {
                isContainer = true;
                isDS = false;
                auditFlags = 1;
                sid = "BA";
                accessMask = 0;
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAuditSpecific((AuditFlags)auditFlags,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

            });

            //Case 4, Audit Qualifier None

            AssertExtensions.Throws<ArgumentException>("auditFlags", () =>
            {
                isContainer = true;
                isDS = false;
                auditFlags = 0;
                sid = "BA";
                accessMask = 1;
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAuditSpecific((AuditFlags)auditFlags,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

            });
            //Case 5, null sid


            Assert.Throws<ArgumentNullException>(() =>
            {
                isContainer = true;
                isDS = false;
                auditFlags = 1;
                accessMask = 1;
                sid = "BA";
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAuditSpecific((AuditFlags)auditFlags, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

            });
            //Case 6, all the ACEs in the Sacl are non-qualified ACE, no remove

            Assert.Throws<InvalidOperationException>(() =>
            {
                isContainer = true;
                isDS = false;
                inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                propagationFlags = 2; //PropagationFlags.InheritOnly

                auditFlags = 3;
                sid = "BA";
                accessMask = 1;
                rawAcl = new RawAcl(0, 1);
                opaque = new byte[4];
                gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                    AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);
                rawAcl.InsertAce(0, gAce);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                TestRemoveAuditSpecific(systemAcl,
    rawAcl,
    (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
    accessMask,
    (InheritanceFlags)inheritanceFlags,
    (PropagationFlags)propagationFlags);
            });
        }
    }
}
