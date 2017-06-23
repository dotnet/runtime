// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_SetAccess
    {
        public static IEnumerable<object[]> DiscretionaryACL_SetAccess()
        {
            yield return new object[] { true, false, 0, "BA", 2, 3, 3, "0:0:1:BA:false:0                                    ", "15:0:2:BA:false:0                                  " };
            yield return new object[] { true, false, 0, "BO", 2, 3, 3, "0:0:1:BA:false:0#0:0:1:BG:false:0#0:0:1:BO:false:0  ", "0:0:1:BA:false:0#0:0:1:BG:false:0#15:0:2:BO:false:0" };
            yield return new object[] { true, false, 0, "BG", 2, 3, 3, "0:0:1:BA:false:0#0:0:1:BG:false:0#0:0:1:BO:false:0  ", "0:0:1:BA:false:0#15:0:2:BG:false:0#0:0:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BA", 2, 3, 3, "1:1:1:BA:false:0#1:1:1:BA:false:0#1:1:1:BA:false:0  ", "15:1:2:BA:false:0                                  " };
            yield return new object[] { true, false, 1, "BA", 2, 3, 3, "1:1:1:BA:false:0#2:1:5:BA:false:0#7:1:100:BA:false:0", "15:1:2:BA:false:0                                  " };
            yield return new object[] { true, false, 0, "BO", 2, 3, 3, "0:0:1:BA:false:0                                    ", "0:0:1:BA:false:0#15:0:2:BO:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 2, 3, 3, "0:0:1:BA:false:0                                    ", "15:1:2:BA:false:0#0:0:1:BA:false:0                 " };
            yield return new object[] { true, false, 0, "BA", 2, 3, 3, "0:1:1:BA:false:0                                    ", "0:1:1:BA:false:0#15:0:2:BA:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "16:1:1:BA:false:0                                   ", "0:1:1:BA:false:0#16:1:1:BA:false:0                 " };
            yield return new object[] { true, false, 0, "BA", 2, 3, 3, "15:0:2:BA:false:0                                   ", "15:0:2:BA:false:0                                  " };
        }

        private static bool TestSetAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;

            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            discretionaryAcl.SetAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
            if (discretionaryAcl.Count == rawAcl.Count &&
                discretionaryAcl.BinaryLength == rawAcl.BinaryLength)
            {
                dAclBinaryForm = new byte[discretionaryAcl.BinaryLength];
                rAclBinaryForm = new byte[rawAcl.BinaryLength];
                discretionaryAcl.GetBinaryForm(dAclBinaryForm, 0);
                rawAcl.GetBinaryForm(rAclBinaryForm, 0);
                if (!Utils.IsBinaryFormEqual(dAclBinaryForm, rAclBinaryForm))
                    result = false;

                //redundant index check

                for (int i = 0; i < discretionaryAcl.Count; i++)
                {
                    if (!Utils.IsAceEqual(discretionaryAcl[i], rawAcl[i]))
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
        [MemberData(nameof(DiscretionaryACL_SetAccess))]
        public static void SetAccess(bool isContainer, bool isDS, int accessControlType, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            RawAcl rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
        }

        [Fact]
        public static void SetAccess_AdditionalTestCases()
        {
            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;
            bool isContainer = false;
            bool isDS = false;

            int accessControlType = 0;
            string sid = null;
            int accessMask = 1;
            int inheritanceFlags = 0;
            int propagationFlags = 0;
            GenericAce gAce = null;
            byte[] opaque = null;
            //Case 1, non-Container, but InheritanceFlags is not None
            AssertExtensions.Throws<ArgumentException>("inheritanceFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.SetAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);


            });

            //Case 2, non-Container, but PropagationFlags is not None
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.SetAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);


            });

            //Case 3, set one allowed ACE to the DiscretionaryAcl with no ACE
            isContainer = true;
            isDS = false;
            accessControlType = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            Assert.True(TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
            ;


            //Case 4, accessMask = 0
            AssertExtensions.Throws<ArgumentException>("accessMask", () =>
            {
                isContainer = true;
                isDS = false;
                accessControlType = 1;
                sid = "BA";
                accessMask = 0;
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 5, null sid
            Assert.Throws<ArgumentNullException>(() =>
            {
                isContainer = true;
                isDS = false;
                accessControlType = 1;
                accessMask = 1;
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                discretionaryAcl.SetAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case6, all the ACEs in the Dacl are non-qualified ACE, no replacement

            isContainer = true;
            isDS = false;

            inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
            propagationFlags = 2; //PropagationFlags.InheritOnly

            accessControlType = 0;
            sid = "BA";
            accessMask = 1;

            rawAcl = new RawAcl(0, 1);
            opaque = new byte[4];
            gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                    AceQualifier.AccessAllowed,
                                    accessMask,
                                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                                    false,
                                    null);
            rawAcl.InsertAce(0, gAce);

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);


            });


            //Case 7, set without replacement, exceed binary length boundary, throw exception

            isContainer = true;
            isDS = false;

            inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
            propagationFlags = 2; //PropagationFlags.InheritOnly

            accessControlType = 0;
            sid = "BA";
            accessMask = 1;

            rawAcl = new RawAcl(0, 1);
            opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 16];
            gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                AceFlags.InheritanceFlags, opaque); ;
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                    AceQualifier.AccessAllowed,
                                    accessMask,
                                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                                    false,
                                    null);

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });

            //Case 8, set without replacement, not exceed binary length boundary

            isContainer = true;
            isDS = false;

            inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
            propagationFlags = 2; //PropagationFlags.InheritOnly

            accessControlType = 0;
            sid = "BA";
            accessMask = 1;

            rawAcl = new RawAcl(0, 1);
            opaque = new byte[GenericAcl.MaxBinaryLength + 1 - 8 - 4 - 28];
            gAce = new CustomAce(AceType.MaxDefinedAceType + 1,
                AceFlags.InheritanceFlags, opaque); ;
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            gAce = new CommonAce(AceFlags.ContainerInherit | AceFlags.InheritOnly,
                                    AceQualifier.AccessAllowed,
                                    accessMask + 1,
                                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                                    false,
                                    null);
            rawAcl.InsertAce(0, gAce);

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {
                TestSetAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask + 1, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });


            //Case 9, set Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
            // should throw appropriate exception for wrong parameter, bug#287188
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                isContainer = true;
                isDS = false;

                inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
                propagationFlags = 2; //PropagationFlags.InheritOnly

                accessControlType = 100;
                sid = "BA";
                accessMask = 1;

                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.SetAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    accessMask,
                    (InheritanceFlags)inheritanceFlags,
                    (PropagationFlags)propagationFlags);
            });
        }
    }
}
