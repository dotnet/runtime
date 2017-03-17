// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_GetSddlForm
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_GetSddlForm_TestData()
       {
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, false, false, false , "" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, false, false, true , "D:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, false, true , false, "S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, false, true , true , "D:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, true , false, false, "G:BG" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, true , false, true , "G:BGD:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, true , true , false, "G:BGS:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", false, true , true , true , "G:BGD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , false, false, false, "O:BA" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , false, false, true , "O:BAD:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , false, true , false, "O:BAS:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , false, true , true , "O:BAD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , false, false, "O:BAG:BG" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , false, true , "O:BAG:BGD:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , false, "O:BAG:BGS:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , true , "O:BAG:BGD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 0 , null, null, null                  , null                 , true , true , true , true , "" };
           yield return new object[] { true, false, 4 , "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , true , "O:BAG:BGD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 4 , "BA", "BG", null                  , "0:1:4096:BO:false:0", true , true , true , true , "O:BAG:BGD:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 20, "BA", "BG", null                  , "0:1:4096:BO:false:0", true , true , true , true , "O:BAG:BGD:(D;;0x1000;;;BO)" };
           yield return new object[] { true, false, 16, "BA", "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , true , "O:BAG:BGD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 16, "BA", "BG", "64:2:4096:BA:false:0", null                 , true , true , true , true , "O:BAG:BGS:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", "BG", "64:2:4096:BA:false:0", null                 , true , true , true , true , "O:BAG:BGS:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, null, "BG", "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , true , "G:BGD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
           yield return new object[] { true, false, 20, "BA", null, "64:2:4096:BA:false:0", "0:1:4096:BO:false:0", true , true , true , true , "O:BAD:(D;;0x1000;;;BO)S:(AU;SA;0x1000;;;BA)" };
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_GetSddlForm_TestData))]
        public static void TestGetSddlForm(bool isContainer, bool isDS, int flags, string ownerStr, string groupStr, string saclStr, string daclStr, bool getOwner, bool getGroup, bool getSacl, bool getDacl, string expectedSddl)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            string resultSddl = null;

            ControlFlags controlFlags = ControlFlags.OwnerDefaulted;
            SecurityIdentifier owner = null;
            SecurityIdentifier group = null;
            RawAcl rawAcl = null;
            SystemAcl sacl = null;
            DiscretionaryAcl dacl = null;
            AccessControlSections accControlSections = AccessControlSections.None;

            controlFlags = (ControlFlags)flags;
            owner = (ownerStr != null) ? new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(ownerStr)) : null;
            group = (groupStr != null) ? new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(groupStr)) : null;

            rawAcl = (saclStr != null) ? Utils.CreateRawAclFromString(saclStr) : null;
            if (rawAcl == null)
                sacl = null;
            else
                sacl = new SystemAcl(isContainer, isDS, rawAcl);

            rawAcl = (daclStr != null) ? Utils.CreateRawAclFromString(daclStr) : null;
            if (rawAcl == null)
                dacl = null;
            else
                dacl = new DiscretionaryAcl(isContainer, isDS, rawAcl);

            commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, controlFlags, owner, group, sacl, dacl);
            if (getOwner)
                accControlSections |= AccessControlSections.Owner;
            if (getGroup)
                accControlSections |= AccessControlSections.Group;
            if (getSacl)
                accControlSections |= AccessControlSections.Audit;
            if (getDacl)
                accControlSections |= AccessControlSections.Access;

            resultSddl = commonSecurityDescriptor.GetSddlForm(accControlSections);
            Assert.True(string.Compare(expectedSddl, resultSddl, StringComparison.CurrentCultureIgnoreCase) == 0);
        }

    }
}
