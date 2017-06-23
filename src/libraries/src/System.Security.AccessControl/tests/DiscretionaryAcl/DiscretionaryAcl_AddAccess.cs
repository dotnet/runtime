// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_AddAccess
    {
        public static IEnumerable<object[]> DiscretionaryACL_AddAccess()
        {
            yield return new object[] { true, false, 1, "BO", 1, 3, 3, "15:1:1:BA:false:0                                    ", "15:1:1:BA:false:0#15:1:1:BO:false:0                  " };
            yield return new object[] { true, false, 0, "BA", 1, 0, 0, "16:0:1:BA:false:0                                    ", "0:0:1:BA:false:0#16:0:1:BA:false:0                   " };
            yield return new object[] { true, false, 0, "BA", 1, 0, 0, "0:1:1:BA:false:0                                     ", "0:1:1:BA:false:0#0:0:1:BA:false:0                    " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "0:0:1:BA:false:0                                     ", "0:1:1:BA:false:0#0:0:1:BA:false:0                    " };
            yield return new object[] { true, false, 0, "BA", 2, 2, 2, "0:0:3:BA:false:0                                     ", "0:0:3:BA:false:0#9:0:2:BA:false:0                    " };
            yield return new object[] { false, false, 1, "SY", 2, 0, 0, "0:1:1:SY:false:0                                     ", "0:1:3:SY:false:0                                    " };
            yield return new object[] { true, false, 1, "SY", 1, 0, 0, "10:1:1:SY:false:0                                    ", "2:1:1:SY:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 1, 0, "10:1:1:SY:false:0                                    ", "2:1:1:SY:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 1, 1, "10:1:1:SY:false:0                                    ", "2:1:1:SY:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 1, 2, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0                                    " };
            yield return new object[] { true, false, 1, "SY", 1, 1, 3, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0                                    " };
            yield return new object[] { true, false, 1, "SY", 1, 2, 0, "10:1:1:SY:false:0                                    ", "3:1:1:SY:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 2, 1, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0#5:1:1:SY:false:0                   " };
            yield return new object[] { true, false, 1, "SY", 1, 2, 2, "10:1:1:SY:false:0                                    ", "11:1:1:SY:false:0                                    " };
            yield return new object[] { true, false, 1, "SY", 1, 2, 3, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0#13:1:1:SY:false:0                  " };
            yield return new object[] { true, false, 1, "SY", 1, 3, 0, "10:1:1:SY:false:0                                    ", "3:1:1:SY:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 3, 1, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0#7:1:1:SY:false:0                   " };
            yield return new object[] { true, false, 1, "SY", 1, 3, 2, "10:1:1:SY:false:0                                    ", "11:1:1:SY:false:0                                    " };
            yield return new object[] { true, false, 1, "SY", 1, 3, 3, "10:1:1:SY:false:0                                    ", "10:1:1:SY:false:0#15:1:1:SY:false:0                  " };
            yield return new object[] { true, false, 1, "BO", 1, 1, 1, "10:1:1:BA:false:0#10:1:1:BG:false:0#10:1:1:BO:false:0", "10:1:1:BA:false:0#10:1:1:BG:false:0#2:1:1:BO:false:0 " };
            yield return new object[] { true, false, 1, "BG", 1, 1, 1, "10:1:1:BA:false:0#10:1:1:BG:false:0#10:1:1:BO:false:0", "10:1:1:BA:false:0#2:1:1:BG:false:0#10:1:1:BO:false:0 " };
            yield return new object[] { true, false, 1, "BA", 1, 3, 0, "3:1:1:BA:false:0                                     ", "3:1:1:BA:false:0                                     " };
            yield return new object[] { true, false, 1, "SY", 1, 3, 3, "9:1:1:SY:false:0                                     ", "9:1:1:SY:false:0#15:1:1:SY:false:0                   " };
        }

        private static bool TestAddAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;
            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            discretionaryAcl.AddAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
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
        [MemberData(nameof(DiscretionaryACL_AddAccess))]
        public static void AddAccess(bool isContainer, bool isDS, int accessControlType, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            RawAcl rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
        }

        [Fact]
        public static void AddAccess_AdditionalTestCases()
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

                discretionaryAcl.AddAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.ContainerInherit, PropagationFlags.None);

            });

            //Case 2, non-Container, but PropagationFlags is not None
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = false;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.AddAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);


            });

            //Case 3, Container, InheritanceFlags is None, PropagationFlags is InheritOnly
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.AddAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.InheritOnly);


            });

            //Case 4, Container, InheritanceFlags is None, PropagationFlags is NoPropagateInherit
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.AddAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit);


            });

            //Case 5, Container, InheritanceFlags is None, PropagationFlags is NoPropagateInherit | InheritOnly
            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

                discretionaryAcl.AddAccess(AccessControlType.Allow,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")), 1, InheritanceFlags.None, PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly);


            });

            //Case 6, accessMask = 0
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
                discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 7, null sid
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
                discretionaryAcl.AddAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 8, add one Access ACE to the DiscretionaryAcl with no ACE
            isContainer = true;
            isDS = false;
            accessControlType = 0;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)15, AceQualifier.AccessAllowed, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            Assert.True(TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
            ;


            //Case 9, Container, InheritOnly ON, but ContainerInherit and ObjectInherit are both OFF
            //add meaningless Access ACE to the DiscretionaryAcl with no ACE, ace should not
            //be added. There are mutiple type of meaningless Ace, but as both AddAccess and Constructor3
            //call the same method to check the meaninglessness, only some sanitory cases are enough.
            //bug# 288116


            AssertExtensions.Throws<ArgumentException>("propagationFlags", () =>
            {
                isContainer = true;
                isDS = false;

                inheritanceFlags = 0;//InheritanceFlags.None
                propagationFlags = 2; //PropagationFlags.InheritOnly

                accessControlType = 0;
                sid = "BA";
                accessMask = 1;

                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);
            });


            //Case 10, add Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
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
                discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    accessMask,
                    (InheritanceFlags)inheritanceFlags,
                    (PropagationFlags)propagationFlags);

            });


            //Case 11, all the ACEs in the Dacl are non-qualified ACE, no merge

            Assert.Throws<InvalidOperationException>(() =>
            {
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

                TestAddAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

            });

            //Case 12, add Ace to exceed binary length boundary, throw exception
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
            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                discretionaryAcl.AddAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);

            });

        }
    }
}
