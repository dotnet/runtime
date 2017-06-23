// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_RemoveAccessSpecific
    {
        public static IEnumerable<object[]> DiscretionaryACL_RemoveAccessSpecific()
        {
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "0:1:1:BA:false:0#0:1:1:BG:false:0#0:1:1:BO:false:0                 ", "0:1:1:BG:false:0#0:1:1:BO:false:0" };
            yield return new object[] { true, false, 1, "BO", 1, 0, 0, "0:1:1:BA:false:0#0:1:1:BG:false:0#0:1:1:BO:false:0                 ", "0:1:1:BA:false:0#0:1:1:BG:false:0" };
            yield return new object[] { true, false, 1, "BG", 1, 0, 0, "0:1:1:BA:false:0#0:1:1:BG:false:0#0:1:1:BO:false:0                 ", "0:1:1:BA:false:0#0:1:1:BO:false:0" };
            yield return new object[] { true, false, 0, "BA", 1, 0, 0, "0:0:1:BA:false:0#0:0:1:BA:false:0#0:0:1:BA:false:0#0:0:1:BO:false:0", "0:0:1:BO:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "16:1:1:BA:false:0                                                  ", "16:1:1:BA:false:0                " };
            yield return new object[] { true, false, 0, "BA", 1, 0, 0, "0:1:1:BA:false:0                                                   ", "0:1:1:BA:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "0:0:1:BA:false:0                                                   ", "0:0:1:BA:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "0:1:1:BO:false:0                                                   ", "0:1:1:BO:false:0                 " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 0, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 1, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 3, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 0, 4, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 1, 0, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 3, 1, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 4, 1, "15:1:1:BA:false:0                                                  ", "15:1:1:BA:false:0                " };
            yield return new object[] { true, false, 1, "BA", 1, 3, 3, "15:1:3:BA:false:0                                                  ", "15:1:3:BA:false:0                " };
        }

        private static bool TestRemoveAccessSpecific(DiscretionaryAcl discretionaryAcl, RawAcl rawAcl, AccessControlType accessControlType, SecurityIdentifier sid, int accessMask, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            bool result = true;

            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            discretionaryAcl.RemoveAccessSpecific(accessControlType, sid, accessMask, inheritanceFlags, propagationFlags);
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
        [MemberData(nameof(DiscretionaryACL_RemoveAccessSpecific))]
        public static void RemoveAccessSpecific(bool isContainer, bool isDS, int accessControlType, string sid, int accessMask, int inheritanceFlags, int propagationFlags, string initialRawAclStr, string verifierRawAclStr)
        {
            RawAcl rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType, new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags));
        }

        [Fact]
        public static void RemoveAccessSpecific_AdditionalTestCases()
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
            //Case 1, remove one ACE from the DiscretionaryAcl with no ACE
            isContainer = true;
            isDS = false;
            accessControlType = 1;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            Assert.True(TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
            ;


            //Case 2, remove the last one ACE from the DiscretionaryAcl
            isContainer = true;
            isDS = false;
            accessControlType = 0;
            sid = "BA";
            accessMask = 1;
            inheritanceFlags = 3;
            propagationFlags = 3;
            rawAcl = new RawAcl(0, 1);
            //15 = AceFlags.ObjectInherit |AceFlags.ContainerInherit | AceFlags.NoPropagateInherit | AceFlags.InheritOnly
            gAce = new CommonAce((AceFlags)15, AceQualifier.AccessAllowed, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //remove the ace to create the validation rawAcl
            rawAcl.RemoveAce(rawAcl.Count - 1);
            Assert.True(TestRemoveAccessSpecific(discretionaryAcl, rawAcl, (AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags))
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
                discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 4, null sid
            Assert.Throws<ArgumentNullException>(() =>
            {
                isContainer = true;
                isDS = false;
                accessControlType = 1;
                accessMask = 1;
                sid = "BA";
                inheritanceFlags = 3;
                propagationFlags = 3;
                rawAcl = new RawAcl(0, 1);
                discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
                discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType, null, accessMask, (InheritanceFlags)inheritanceFlags, (PropagationFlags)propagationFlags);



            });

            //Case 5, all the ACEs in the Dacl are non-qualified ACE, no remove

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

            //After Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical and
            //forbid the modification on uncanonical ACL, this case will throw InvalidOperationException
            Assert.Throws<InvalidOperationException>(() =>
            {
                TestRemoveAccessSpecific
                        (discretionaryAcl, rawAcl,
                        (AccessControlType)accessControlType,
                        new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                        accessMask,
                        (InheritanceFlags)inheritanceFlags,
                        (PropagationFlags)propagationFlags);

            });

            //Case 7, Remove Specific Ace of NOT(AccessControlType.Allow |AccessControlType.Denied) to the DiscretionaryAcl with no ACE, 
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

                discretionaryAcl.RemoveAccessSpecific((AccessControlType)accessControlType,
                    new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)),
                    accessMask,
                    (InheritanceFlags)inheritanceFlags,
                    (PropagationFlags)propagationFlags);


            });
        }
    }
}
