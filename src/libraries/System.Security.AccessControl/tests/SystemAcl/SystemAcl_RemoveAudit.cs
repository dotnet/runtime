// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_RemoveAudit
    {
        public static IEnumerable<object[]> SystemAcl_RemoveAudit_TestData()
        {
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#79:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 0, 1, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#79:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 0, 2, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#79:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 0, 3, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#79:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 1, 0, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#77:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 1, 1, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#77:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 1, 2, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#69:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 1, 3, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#69:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 0, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#78:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 1, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#78:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 2, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#70:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 3, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#70:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 3, 0, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 3, 1, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 3, 2, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#64:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 3, 3, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#64:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BO", 1, 3, 3, "199:2:3:BA:false:0#199:2:3:BG:false:0#199:2:3:BO:false:0", "199:2:3:BA:false:0#199:2:3:BG:false:0#199:2:2:BO:false:0#135:2:1:BO:false:0#64:2:1:BO:false:0", true };
            yield return new object[] { true, false, 1, "BG", 1, 3, 3, "199:2:3:BA:false:0#199:2:3:BG:false:0#199:2:3:BO:false:0", "199:2:3:BA:false:0#199:2:2:BG:false:0#135:2:1:BG:false:0#64:2:1:BG:false:0#199:2:3:BO:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 3, 3, "199:2:3:BA:false:0#199:2:3:BA:false:0#199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#64:2:1:BA:false:0#199:2:2:BA:false:0#135:2:1:BA:false:0#64:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 5, 3, 3, "199:2:3:BA:false:0", "199:2:2:BA:false:0#135:2:1:BA:false:0#64:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BO", 1, 2, 3, "199:2:3:BA:false:0", "199:2:3:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 3, "215:2:3:BA:false:0", "215:2:3:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 4, 2, 3, "199:2:3:BA:false:0", "199:2:3:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 3, "135:2:3:BA:false:0", "135:2:3:BA:false:0", true };
            yield return new object[] { true, false, 3, "BA", 1, 0, 0, "199:2:3:BA:false:0", "199:2:2:BA:false:0#207:2:1:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 3, 0, 0, "199:2:3:BA:false:0", "135:2:3:BA:false:0#79:2:3:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 1, 3, "202:2:3:BA:false:0", "202:2:3:BA:false:0", false };
        }

        [Theory]
        [MemberData(nameof(SystemAcl_RemoveAudit_TestData))]
        public static void BasicValidationTestCases(bool isContainer, bool isDS, int auditFlags, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr, bool removePossible)
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

            //create a systemAcl
            rawAcl = Utils.CreateRawAclFromString(initialRawAclStr);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible));
        }

        private static bool TestRemoveAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, bool removePossible)
        {
            bool result = true;
            bool isRemoved = false;
            byte[] sAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            isRemoved = systemAcl.RemoveAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
            if ((isRemoved == removePossible) &&
                (systemAcl.Count == rawAcl.Count) &&
                (systemAcl.BinaryLength == rawAcl.BinaryLength))
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
            bool removePossible = false;
            byte[] opaque = null;

            //Case 1, null sid
            Assert.Throws<ArgumentNullException>(() =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);
            });

            //Case 2, SystemAudit Ace but non AuditFlags
            AssertExtensions.Throws<ArgumentException>("auditFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAudit(AuditFlags.None,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 3, 0 accessMask
            AssertExtensions.Throws<ArgumentException>("accessMask", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.RemoveAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 0, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 4, remove one audit ACE from the SystemAcl with no ACE
            isContainer = true;
            isDS = false;
            auditFlags = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            removePossible = true;
            rawAcl = new RawAcl(0, 1);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible));

            //Case 5, remove the last one ACE from the SystemAcl
            isContainer = true;
            isDS = false;
            auditFlags = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            removePossible = true;
            rawAcl = new RawAcl(0, 1);
            //79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)79, AceQualifier.SystemAudit, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //remove the ace to create the validation rawAcl
            rawAcl.RemoveAce(rawAcl.Count - 1);
            Assert.True(TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible));

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
                gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque); ;
                rawAcl.InsertAce(0, gAce);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                TestRemoveAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, true);
            });
            //Case 7, remove split cause overflow
            // Test case no longer relevant in CoreCLR
            // Non-canonical ACLs cannot be modified
        }
    }
}
