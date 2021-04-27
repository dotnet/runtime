// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_SetAudit
    {
       public static IEnumerable<object[]> SystemAcl_SetAudit_TestData()
       {
           yield return new object[] { true, false, 2, "BA", 2, 3, 3, "64:2:1:BA:false:0", "143:2:2:BA:false:0" };
           yield return new object[] { true, false, 2, "BO", 2, 3, 3, "64:2:1:BA:false:0#64:2:1:BG:false:0#64:2:1:BO:false:0", "64:2:1:BA:false:0#64:2:1:BG:false:0#143:2:2:BO:false:0" };
           yield return new object[] { true, false, 2, "BG", 2, 3, 3, "64:2:1:BA:false:0#64:2:1:BG:false:0#64:2:1:BO:false:0", "64:2:1:BA:false:0#143:2:2:BG:false:0#64:2:1:BO:false:0 " };
           yield return new object[] { true, false, 2, "BA", 2, 3, 3, "64:2:1:BA:false:0#128:2:2:BA:false:0#64:2:1:BA:false:0", "143:2:2:BA:false:0" };
           yield return new object[] { true, false, 2, "BO", 2, 3, 3, "64:2:1:BA:false:0", "64:2:1:BA:false:0#143:2:2:BO:false:0 " };
           yield return new object[] { true, false, 1, "BA", 1, 0, 0, "80:2:1:BA:false:0", "64:2:1:BA:false:0#80:2:1:BA:false:0 " };
           yield return new object[] { true, false, 2, "BA", 2, 3, 3, "143:2:2:BA:false:0", "143:2:2:BA:false:0" };
        }

        [Theory]
        [MemberData(nameof(SystemAcl_SetAudit_TestData))]
        public static void BasicValidationTestCases(bool isContainer, bool isDS, int auditFlags, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            RawAcl rawAcl = null;
            SystemAcl systemAcl = null;

            //create a systemAcl
            rawAcl = Utils.CreateRawAclFromString(initialRawAclStr);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            Assert.True(TestSetAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
        }

        private static bool TestSetAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;
            byte[] sAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            systemAcl.SetAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
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

            //Case 1, null sid
            Assert.Throws<ArgumentNullException>(() =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.SetAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);
            });

            //Case 2, SystemAudit Ace but non AuditFlags
            AssertExtensions.Throws<ArgumentException>("auditFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.SetAudit(AuditFlags.None,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 3, 0 accessMask
            AssertExtensions.Throws<ArgumentException>("accessMask", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.SetAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 0, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 4, non-Container, but InheritanceFlags is not None
            AssertExtensions.Throws<ArgumentException>("inheritanceFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.SetAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);
            });


            //Case 5, non-Container, but PropagationFlags is not None
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.SetAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);

            });

            //Case 6, set one audit ACE to the SystemAcl with no ACE
            isContainer = true;
            isDS = false;
            auditFlags = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            //79 = AceFlags.SuccessfulAccess | AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)79, AceQualifier.SystemAudit, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            Assert.True(TestSetAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));

            //Case 7, all the ACEs in the Sacl are non-qualified ACE, no merge

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
                gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);
                rawAcl.InsertAce(0, gAce);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly | AceFlags.AuditFlags,
                        AceQualifier.SystemAudit,
                        accessMask,
                        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                        false,
                        null);
                rawAcl.InsertAce(0, gAce);
                //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                TestSetAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });

            //Case 8, Set Ace to exceed binary length boundary, throw exception
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
                opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
                gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                    AceFlags.InheritanceFlags | AceFlags.AuditFlags, opaque);
                rawAcl.InsertAce(0, gAce);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
                //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
                systemAcl.SetAudit((AuditFlags)auditFlags,
        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });
        }
    }
}
