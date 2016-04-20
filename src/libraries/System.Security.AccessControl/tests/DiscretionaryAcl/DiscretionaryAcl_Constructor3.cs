// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public class DiscretionaryAcl_Constructor3
    {
        public static IEnumerable<object[]> DiscretionaryACL_Constructor3()
        {
            yield return new object[] { false, false, "0:1:1:BA:false:0#0:1:0:BG:false:0#16:1:2:BO:false:0                 ", "0:1:1:BA:false:0#16:1:2:BO:false:0                 ", true };
            yield return new object[] { false, false, "9:1:1:BA:false:0#0:0:1:BG:false:0#16:1:2:BO:false:0                 ", "0:0:1:BG:false:0#16:1:2:BO:false:0                 ", true };
            yield return new object[] { true, false, "9:1:1:BA:false:0#0:1:1:BG:false:0#16:1:2:BO:false:0                 ", "9:1:1:BA:false:0#0:1:1:BG:false:0#16:1:2:BO:false:0", true };
            yield return new object[] { false, false, "7:1:1:BA:false:0                                                    ", "0:1:1:BA:false:0                                   ", true };
            yield return new object[] { true, false, "7:1:1:BA:false:0                                                    ", "7:1:1:BA:false:0                                   ", true };
            yield return new object[] { false, false, "0:1:0:BG:false:0#8:1:1:BA:false:0#0:1:1:BO:false:0                  ", "0:1:1:BO:false:0                                   ", true };
            yield return new object[] { true, false, "0:1:1:BA:false:0#16:1:2:BO:false:0#8:0:1:BG:false:0                 ", "0:1:1:BA:false:0#16:1:2:BO:false:0                 ", true };
            yield return new object[] { true, false, "4:1:1:BA:false:0                                                    ", "0:1:1:BA:false:0                                   ", true };
            yield return new object[] { true, false, "192:1:1:BA:false:0                                                  ", "0:1:1:BA:false:0                                   ", true };
            yield return new object[] { false, false, "0:2:1:BA:false:0#0:1:1:BG:false:0                                   ", "0:1:1:BG:false:0                                   ", true };
            yield return new object[] { false, false, "0:3:1:BA:false:0#0:1:1:BG:false:0                                   ", "0:1:1:BG:false:0                                   ", true };
            yield return new object[] { false, false, "0:1:1:BA:false:0#0:0:2:BG:false:0#16:1:3:BO:false:0#0:2:1:AN:false:0", "0:1:1:BA:false:0#0:0:2:BG:false:0#16:1:3:BO:false:0", true };
            yield return new object[] { false, false, "0:0:1:BA:false:0                                                    ", "0:0:1:BA:false:0                                   ", true };
            yield return new object[] { false, false, "0:1:3:BO:false:0                                                    ", "0:1:3:BO:false:0                                   ", true };
            yield return new object[] { false, false, "16:0:2:BG:false:0                                                   ", "16:0:2:BG:false:0                                  ", true };
            yield return new object[] { false, false, "0:1:3:BO:false:0#0:0:1:BA:false:0                                   ", "0:1:3:BO:false:0#0:0:1:BA:false:0                  ", true };
            yield return new object[] { false, false, "0:0:1:BA:false:0#0:1:3:BO:false:0                                   ", "0:0:1:BA:false:0#0:1:3:BO:false:0                  ", false };
            yield return new object[] { false, false, "0:1:3:BO:false:0#16:0:2:BG:false:0                                  ", "0:1:3:BO:false:0#16:0:2:BG:false:0                 ", true };
            yield return new object[] { false, false, "16:0:2:BG:false:0#0:1:3:BO:false:0                                  ", "16:0:2:BG:false:0#0:1:3:BO:false:0                 ", false };
            yield return new object[] { false, false, "0:0:1:BA:false:0#16:0:2:BG:false:0                                  ", "0:0:1:BA:false:0#16:0:2:BG:false:0                 ", true };
            yield return new object[] { false, false, "16:0:2:BG:false:0#0:0:1:BA:false:0                                  ", "16:0:2:BG:false:0#0:0:1:BA:false:0                 ", false };
            yield return new object[] { false, false, "0:1:3:BO:false:0#0:0:1:BA:false:0#16:0:2:BG:false:0                 ", "0:1:3:BO:false:0#0:0:1:BA:false:0#16:0:2:BG:false:0", true };
            yield return new object[] { false, false, "16:0:2:BG:false:0#0:0:1:BA:false:0#0:1:3:BO:false:0                 ", "16:0:2:BG:false:0#0:0:1:BA:false:0#0:1:3:BO:false:0", false };
            yield return new object[] { false, false, "16:0:2:BG:false:0#0:1:3:BO:false:0#0:0:1:BA:false:0                 ", "16:0:2:BG:false:0#0:1:3:BO:false:0#0:0:1:BA:false:0", false };
            yield return new object[] { false, false, "0:0:1:BA:false:0#16:0:2:BG:false:0#0:1:3:BO:false:0                 ", "0:0:1:BA:false:0#16:0:2:BG:false:0#0:1:3:BO:false:0", false };
            yield return new object[] { false, false, "0:0:1:BA:false:0#0:1:3:BO:false:0#16:0:2:BG:false:0                 ", "0:0:1:BA:false:0#0:1:3:BO:false:0#16:0:2:BG:false:0", false };
            yield return new object[] { false, false, "0:1:3:BO:false:0#16:0:2:BG:false:0#0:0:1:BA:false:0                 ", "0:1:3:BO:false:0#16:0:2:BG:false:0#0:0:1:BA:false:0", false };
            yield return new object[] { false, false, "16:1:2:BG:false:0#16:0:1:BA:false:0                                 ", "16:1:2:BG:false:0#16:0:1:BA:false:0                ", true };
            yield return new object[] { false, false, "0:1:1:BG:false:0#0:2:1:BA:false:0#0:2:1:BO:false:0                  ", "0:1:1:BG:false:0                                   ", true };
            yield return new object[] { false, false, "0:0:1:BG:false:0#0:1:1:BA:false:0#16:1:2:BA:false:0                 ", "0:0:1:BG:false:0#0:1:1:BA:false:0#16:1:2:BA:false:0", false };
            yield return new object[] { false, false, "0:1:1:BA:false:0#0:0:2:BA:false:0#0:0:1:BG:false:0                  ", "0:1:1:BA:false:0#0:0:2:BA:false:0#0:0:1:BG:false:0 ", true };
            yield return new object[] { false, false, "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:2:BO:false:0                  ", "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:2:BO:false:0 ", false };
            yield return new object[] { false, false, "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:2:BA:false:0                  ", "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:2:BA:false:0 ", false };
            yield return new object[] { false, false, "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:1:BA:false:0                  ", "0:0:1:BG:false:0#0:1:1:BA:false:0#0:1:1:BA:false:0 ", false };
            yield return new object[] { true, false, "0:1:1:BO:false:0#1:0:1:BA:false:0#10:0:1:BA:false:0#0:0:2:BG:false:0", "0:1:1:BO:false:0#3:0:1:BA:false:0#0:0:2:BG:false:0 ", true };
            yield return new object[] { true, false, "0:1:1:BO:false:0#1:0:1:BA:false:0#15:0:1:BA:false:0                 ", "0:1:1:BO:false:0#1:0:1:BA:false:0#15:0:1:BA:false:0", true };
        }

        private static bool VerifyACL(DiscretionaryAcl discretionaryAcl, bool isContainer, bool isDS, bool wasCanonicalInitially, RawAcl rawAcl)
        {
            bool result = true;
            byte[] dAclBinaryForm = null;
            byte[] rAclBinaryForm = null;

            if (discretionaryAcl.IsContainer == isContainer &&
                discretionaryAcl.IsDS == isDS &&
                discretionaryAcl.Revision == rawAcl.Revision &&
                discretionaryAcl.Count == rawAcl.Count &&
                discretionaryAcl.BinaryLength == rawAcl.BinaryLength &&
                discretionaryAcl.IsCanonical == wasCanonicalInitially)
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
            {

                result = false;
            }

            return result;
        }

        [Theory]
        [MemberData(nameof(DiscretionaryACL_Constructor3))]
        public static void Constructor3(bool isContainer, bool isDS, string initialRaqAclStr, string verifierRawAclStr, bool wasCanonicalInitially)
        {
            RawAcl rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);
            DiscretionaryAcl discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = Utils.CreateRawAclFromString(verifierRawAclStr);

            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, wasCanonicalInitially, rawAcl));
        }

        [Fact]
        public static void Constructor3_AdditionalTestCases()
        {
            bool isContainer = false;
            bool isDS = false;

            RawAcl rawAcl = null;
            DiscretionaryAcl discretionaryAcl = null;

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
            string sid = "BG";

            //CommonAce constructor additional parameters
            AceQualifier aceQualifier = 0;

            //ObjectAce constructor additional parameters
            ObjectAceFlags objectAceFlag = 0;
            Guid objectAceType;
            Guid inheritedObjectAceType;

            //case 1, an AccessAllowed ACE with a zero access mask is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //drop the Ace from rawAcl
            rawAcl.RemoveAce(0);

            //the only ACE is a meaningless ACE, will be removed
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));

            //case 2, an inherit-only AccessDenied ACE on an object ACL is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //15 has all inheritance AceFlags but Inherited
            gAce = new CommonAce((AceFlags)15, AceQualifier.AccessDenied, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl.RemoveAce(0);

            //the only ACE is a meaningless ACE, will be removed
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));

            //case 3, an inherit-only AccessAllowed ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed
            revision = 0;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            //8 has inheritOnly
            gAce = new CommonAce((AceFlags)8, AceQualifier.AccessAllowed, 1,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl.RemoveAce(0);

            //the only ACE is a meaningless ACE, will be removed
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));

            //case 4, 1 CustomAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = AceFlags.None;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(0, gAce);
            isContainer = false;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, false, rawAcl));

            //case 5, 1 CompoundAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            // 2 has ContainerInherit
            aceFlag = (AceFlags)2;
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)));
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, false, rawAcl));


            //case 6, 1 ObjectAce
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            aceFlag = (AceFlags)15; //all inheritance flags ored together but Inherited
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid)), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(0, gAce);
            isContainer = true;
            isDS = true;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));


            //case 7, no Ace
            revision = 127;
            capacity = 1;
            rawAcl = new RawAcl(revision, capacity);
            isContainer = true;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));


            //case 8, all Aces from case 1, and 3 to 6 
            revision = 127;
            capacity = 5;
            sid = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid("BG")).ToString();
            rawAcl = new RawAcl(revision, capacity);
            //0 access Mask
            gAce = new CommonAce(AceFlags.None, AceQualifier.AccessAllowed, 0,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 1.ToString())), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);

            //an inherit-only AccessAllowed ACE without ContainerInherit or ObjectInherit flags on a container object is meaningless, will be removed

            gAce = new CommonAce((AceFlags)8, AceQualifier.AccessAllowed, 1,
            new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 2.ToString())), false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);

            // ObjectAce
            aceFlag = (AceFlags)15; //all inheritance flags ored together but Inherited
            aceQualifier = AceQualifier.AccessAllowed;
            accessMask = 1;
            objectAceFlag = ObjectAceFlags.ObjectAceTypePresent | ObjectAceFlags.InheritedObjectAceTypePresent;
            objectAceType = new Guid("11111111-1111-1111-1111-111111111111");
            inheritedObjectAceType = new Guid("22222222-2222-2222-2222-222222222222");
            gAce = new ObjectAce(aceFlag, aceQualifier, accessMask,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 3.ToString())), objectAceFlag, objectAceType, inheritedObjectAceType, false, null);
            rawAcl.InsertAce(rawAcl.Count, gAce);

            // CustomAce
            aceType = AceType.MaxDefinedAceType + 1;
            aceFlag = AceFlags.None;
            opaque = null;
            gAce = new CustomAce(aceType, aceFlag, opaque);
            rawAcl.InsertAce(rawAcl.Count, gAce);

            // CompoundAce					
            aceFlag = (AceFlags)2;
            accessMask = 1;
            compoundAceType = CompoundAceType.Impersonation;
            gAce = new CompoundAce(aceFlag, accessMask, compoundAceType,
                new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(sid + 4.ToString())));
            rawAcl.InsertAce(rawAcl.Count, gAce);

            isContainer = true;
            isDS = false;

            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl.RemoveAce(0);
            rawAcl.RemoveAce(0);

            //Mark changes design to make ACL with any CustomAce, CompoundAce uncanonical

            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, false, rawAcl));



            discretionaryAcl = null;
            isContainer = false;
            isDS = false;
            rawAcl = null;
            //case 1, rawAcl = null
            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, 1);
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));



            discretionaryAcl = new DiscretionaryAcl(isContainer, isDS, rawAcl);
            rawAcl = new RawAcl(isDS ? GenericAcl.AclRevisionDS : GenericAcl.AclRevision, 1);
            Assert.True(VerifyACL(discretionaryAcl, isContainer, isDS, true, rawAcl));


        }
    }
}