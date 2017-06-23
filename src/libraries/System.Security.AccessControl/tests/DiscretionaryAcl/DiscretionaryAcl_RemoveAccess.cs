// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_RemoveAccess
    {
        public static IEnumerable<object[]> DiscretionaryACL_RemoveAccess()
        {
            yield return new object[] { true, false, 0, "BA", 1, 0, 0, "3:0:3:BA:false:0", "3:0:2:BA:false:0#11:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 0, 1, "3:0:3:BA:false:0", "3:0:2:BA:false:0#11:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 0, 2, "3:0:3:BA:false:0", "3:0:2:BA:false:0#11:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 0, 3, "3:0:3:BA:false:0", "3:0:2:BA:false:0#11:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 1, 0, "3:0:3:BA:false:0", "3:0:2:BA:false:0#9:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 1, 1, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false, };
            yield return new object[] { true, false, 0, "BA", 1, 1, 2, "3:0:3:BA:false:0", "3:0:2:BA:false:0#1:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 1, 3, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false };
            yield return new object[] { true, false, 0, "BA", 1, 2, 0, "3:0:3:BA:false:0", "3:0:2:BA:false:0#10:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 1, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false, };
            yield return new object[] { true, false, 0, "BA", 1, 2, 2, "3:0:3:BA:false:0", "3:0:2:BA:false:0#2:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 3, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false };
            yield return new object[] { true, false, 0, "BA", 1, 3, 0, "3:0:3:BA:false:0", "3:0:2:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 3, 1, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false };
            yield return new object[] { true, false, 0, "BA", 1, 3, 2, "3:0:3:BA:false:0", "3:0:2:BA:false:0#0:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 3, 3, "3:0:3:BA:false:0", "3:0:3:BA:false:0", false };
            yield return new object[] { true, false, 0, "BO", 1, 2, 3, "7:1:3:BO:false:0#7:0:3:BA:false:0#7:0:3:BO:false:0", "7:1:3:BO:false:0#7:0:3:BA:false:0#7:0:2:BO:false:0#6:0:1:BO:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 3, "7:1:3:BO:false:0#7:0:3:BA:false:0#7:0:3:BO:false:0", "7:1:3:BO:false:0#7:0:2:BA:false:0#6:0:1:BA:false:0#7:0:3:BO:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 3, "7:0:3:BA:false:0#7:0:3:BA:false:0#7:0:3:BA:false:0", "7:0:2:BA:false:0#6:0:1:BA:false:0#7:0:2:BA:false:0#6:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 5, 2, 3, "7:0:3:BA:false:0", "7:0:2:BA:false:0#6:0:1:BA:false:0", true };
            yield return new object[] { true, false, 0, "BO", 1, 2, 3, "7:0:3:BA:false:0", "7:0:3:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 3, "23:0:3:BA:false:0", "23:0:3:BA:false:0", true, };
            yield return new object[] { true, false, 0, "BA", 4, 2, 3, "7:0:3:BA:false:0", "7:0:3:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 1, 2, 3, "7:1:3:BA:false:0", "7:1:3:BA:false:0", true };
            yield return new object[] { true, false, 1, "BA", 1, 2, 3, "7:0:3:BA:false:0", "7:0:3:BA:false:0", true };
            yield return new object[] { true, false, 0, "BA", 3, 0, 0, "3:0:3:BA:false:0", "11:0:3:BA:false:0", true };
        }

        private static bool TestRemoveAccess(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, bool removePossible)
        {
            bool result = true;
            bool isRemoved = false;
            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;
            isRemoved = discretionaryAcl.RemoveAccess(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
            if ((isRemoved == removePossible) &&
                (discretionaryAcl.Count == rawAcl.Count) &&
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
        [MemberData(nameof(DiscretionaryACL_RemoveAccess))]
        public static void RemoveAccess(bool isContainer, bool isDS, int accessControlType, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr, bool removePossible)
        {
            RawAcl rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible));
        }

        [Fact]
        public static void RemoveAccess_AdditionalTestCases()
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
            bool removePossible = false;
            byte[] opaque = null;
            //Case 1, remove one ACE from the DiscretionaryAcl with no ACE
            isContainer = true;
            isDS = false;
            accessControlType = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            removePossible = true;
            rawAcl = new RawAcl(0, 1);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible))
            ;


            //Case 2, remove the last ACE from the DiscretionaryAcl
            isContainer = true;
            isDS = false;
            accessControlType = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            removePossible = true;
            rawAcl = new RawAcl(0, 1);
            //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //remove the ace to create the validation rawAcl
            rawAcl.RemoveAce(rawAcl.Count - 1);
            Assert.True(TestRemoveAccess(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags, removePossible))
            ;


            //Case 3, accessMask = 0
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
                discretionaryAcl.RemoveAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 4, null sid
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
                discretionaryAcl.RemoveAccess((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);


            });

            //Case 5, all the ACEs in the Dacl are non-qualified ACE, no remove

            isContainer = true;
            isDS = false;

            inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
            propagationFlags = 2; //PropagationFlags.InheritOnly

            accessControlType = 0;
            sid = "BA";
            accessMask = 1;
            removePossible = true;

            rawAcl = new RawAcl(0, 1);
            opaque = new byte[4];
            gAce = new CustomAce(AceType.MaxDefinedAceType + 1, AceFlags.InheritanceFlags, opaque); ;
            rawAcl.InsertAce(0, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {

                TestRemoveAccess(discretionaryAcl, rawAcl,
                    (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    accessMask,
                    (InheritanceFlags)inheritanceFlags,
                    (PropagationFlags)propagationFlags, removePossible);

            });

            //Case 7, Remove Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
            // should throw appropriate exception for wrong parameter, bug#287188


            isContainer = true;
            isDS = false;

            inheritanceFlags = 1;//InheritanceFlags.ContainerInherit
            propagationFlags = 2; //PropagationFlags.InheritOnly

            accessControlType = 100;
            sid = "BA";
            accessMask = 1;

            rawAcl = new RawAcl(0, 1);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                discretionaryAcl.RemoveAccess((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    accessMask,
                    (InheritanceFlags)inheritanceFlags,
                    (PropagationFlags)propagationFlags);

            });


        }
    }
}
