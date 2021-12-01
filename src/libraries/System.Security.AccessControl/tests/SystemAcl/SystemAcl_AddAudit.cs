// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class SystemAcl_AddAudit
    {
        public static IEnumerable<object[]> SystemAcl_AddAudit_TestData()
       {
           yield return new object[] { true, false, 1, "SY", 1, 0, 0, "64:2:1:BA:false:0", "64:2:1:SY:false:0#64:2:1:BA:false:0" };
           yield return new object[] { true, false, 1, "BA", 1, 0, 0, "80:2:1:BA:false:0", "64:2:1:BA:false:0#80:2:1:BA:false:0" };
           yield return new object[] { true, false, 2, "BA", 2, 2, 2, "64:2:1:BA:false:0", "64:2:1:BA:false:0#137:2:2:BA:false:0" };
           yield return new object[] { false, false, 1, "SY", 2, 0, 0, "64:2:1:SY:false:0", "64:2:3:SY:false:0" };
           yield return new object[] { true, false, 2, "BA", 1, 0, 0, "64:2:1:BA:false:0", "192:2:1:BA:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 0, 0, "69:2:1:SY:false:0", "69:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 1, 0, "69:2:1:SY:false:0", "69:2:1:SY:false:0#66:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 1, 1, "69:2:1:SY:false:0", "71:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 1, 2, "69:2:1:SY:false:0", "69:2:1:SY:false:0#74:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 1, 3, "69:2:1:SY:false:0", "71:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 2, 0, "69:2:1:SY:false:0", "65:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 2, 1, "69:2:1:SY:false:0", "69:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 2, 2, "69:2:1:SY:false:0", "65:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 2, 3, "69:2:1:SY:false:0", "69:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 3, 0, "69:2:1:SY:false:0", "67:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 3, 1, "69:2:1:SY:false:0", "71:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 3, 2, "69:2:1:SY:false:0", "67:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 3, 3, "69:2:1:SY:false:0", "71:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "BO", 1, 3, 3, "69:2:1:BA:false:0#69:2:1:BG:false:0#69:2:1:BO:false:0", "69:2:1:BA:false:0#69:2:1:BG:false:0#71:2:1:BO:false:0" };
           yield return new object[] { true, false, 1, "BG", 1, 3, 3, "69:2:1:BA:false:0#69:2:1:BG:false:0#69:2:1:BO:false:0", "69:2:1:BA:false:0#71:2:1:BG:false:0#69:2:1:BO:false:0 " };
           yield return new object[] { true, false, 1, "SY", 1, 0, 0, "73:2:1:SY:false:0", "65:2:1:SY:false:0" };
           yield return new object[] { true, false, 1, "BA", 1, 1, 1, "70:2:1:BA:false:0", "70:2:1:BA:false:0" };
           yield return new object[] { true, false, 1, "SY", 1, 1, 1, "73:2:1:SY:false:0", "73:2:1:SY:false:0#70:2:1:SY:false:0" };
        }

        private static bool TestAddAudit(SystemAcl systemAcl, RawAcl rawAcl, AuditFlags auditFlag, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;
            byte[] sAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            systemAcl.AddAudit(auditFlag, sid, accessMask, inheritanceFlags, propagationFlags);
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

        [Theory]
        [MemberData(nameof(SystemAcl_AddAudit_TestData))]
        public static void BasicValidationTestCases(bool isContainer, bool isDS, int auditFlags, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            //create a systemAcl
            RawAcl rawAcl = Utils.CreateRawAclFromString(initialRawAclStr);
            SystemAcl systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
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
                systemAcl.AddAudit(AuditFlags.Success, null, 1, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 2, SystemAudit Ace but non AuditFlags
            AssertExtensions.Throws<ArgumentException>("auditFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.None,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 3, 0 accessMask
            AssertExtensions.Throws<ArgumentException>("accessMask", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 0, InheritanceFlags.None, PropagationFlags.None);

            });

            //Case 4, non-Container, but InheritanceFlags is not None
            AssertExtensions.Throws<ArgumentException>("inheritanceFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);

            });

            //Case 5, non-Container, but PropagationFlags is not None
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);

            });

            //Case 6, Container, but InheritanceFlags is None, but PropagationFlags is InheritOnly
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);

            });

            //Case 7, Container, but InheritanceFlags is None, but PropagationFlags is NoPropagateInherit
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit);

            });

            //Case 8, Container, but InheritanceFlags is None, but PropagationFlags is NoPropagateInherit | InheritOnly
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                systemAcl = new SystemAcl(isContainer, isDS, rawAcl);
                systemAcl.AddAudit(AuditFlags.Success,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BA")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly);

            });

            //Case 9, add one audit ACE to the SystemAcl has no ACE
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
            Assert.True(TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));

            //Case 10, all the ACEs in the Sacl are non-qualified ACE, no merge
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
                TestAddAudit(systemAcl, rawAcl, (AuditFlags)auditFlags,
    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });

            //Case 11, add Ace to exceed binary length boundary, throw exception
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
                systemAcl.AddAudit((AuditFlags)auditFlags,
        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });
        }
    }
}
