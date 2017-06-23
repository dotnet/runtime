// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_Constructor
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_Constructor_TestData()
       {
           yield return new object[] { false, false, 20   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { false, true , 20   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , false, 20   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 1    , "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , -1   , "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 0    , "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 65536, "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 65535, "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 32768, "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "BA"     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "BA"     , "BG"          , "64:2:1:BA:false:0", null };
           yield return new object[] { true , true , 20   , "BA"     , "BG"          , null               , "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "BA"     , "BG"          , null               , null };
           yield return new object[] { true , true , 20   , "BA"     , null          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "BA"     , null          , "64:2:1:BA:false:0", null };
           yield return new object[] { true , true , 20   , "BA"     , null          , null               , "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , "BA"     , null          , null               , null };
           yield return new object[] { true , true , 20   , null     , "BG"          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , null     , "BG"          , "64:2:1:BA:false:0", null };
           yield return new object[] { true , true , 20   , null     , "BG"          , null               , "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , null     , "BG"          , null               , null };
           yield return new object[] { true , true , 20   , null     , null          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , null     , null          , "64:2:1:BA:false:0", null };
           yield return new object[] { true , true , 20   , null     , null          , null               , "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 20   , null     , null          , null               , null };
           yield return new object[] { true , true , 1    , null     , null          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 16   , "BA"     , "BG"          , null               , "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 4    , "BA"     , "BG"          , "64:2:1:BA:false:0", null };
           yield return new object[] { true , true , 1    , "S-1-5-0", "S-1-5-32-544", null               , null };
           yield return new object[] { true , true , 16   , null     , null          , null               , null };
           yield return new object[] { true , true , 16   , null     , null          , "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 16   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", "0:1:1:BO:false:0" };
           yield return new object[] { true , true , 16   , "S-1-5-0", "S-1-5-32-544", "64:2:1:BA:false:0", null };
        }

        [Fact]
        public static void AdditionalTestCases()
        {
            //test cases include the exceptions from the constructor
            SystemAcl sacl = null;
            DiscretionaryAcl dacl = null;
            CommonSecurityDescriptor sd = null;

            // test case 1: SACL is not null, SACL.IsContainer is true, but isContainer parameter is false
            sacl = new SystemAcl(true, true, 10);
            AssertExtensions.Throws<ArgumentException>("systemAcl", () =>
            {
                sd = new CommonSecurityDescriptor(false, true, ControlFlags.SystemAclPresent, null, null, sacl, null);
            });
            
            // test case 2: SACL is not null, SACL.IsContainer is false, but isContainer parameter is true
            sacl = new SystemAcl(false, true, 10);
            AssertExtensions.Throws<ArgumentException>("systemAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, true, ControlFlags.SystemAclPresent, null, null, sacl, null);
            });
            
            // test case 3: DACL is not null, DACL.IsContainer is true, but isContainer parameter is false
            dacl = new DiscretionaryAcl(true, true, 10);
            AssertExtensions.Throws<ArgumentException>("discretionaryAcl", () =>
            {
                sd = new CommonSecurityDescriptor(false, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
            });
            
            // test case 4: DACL is not null, DACL.IsContainer is false, but isContainer parameter is true
            dacl = new DiscretionaryAcl(false, true, 10);
            AssertExtensions.Throws<ArgumentException>("discretionaryAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
            });
            
            // test case 5: SACL is not null, SACL.IsDS is true, but isDS parameter is false
            sacl = new SystemAcl(true, true, 10);
            AssertExtensions.Throws<ArgumentException>("systemAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, false, ControlFlags.SystemAclPresent, null, null, sacl, null);
            });
            
            // test case 6: SACL is not null, SACL.IsDS is false, but isDS parameter is true
            sacl = new SystemAcl(true, false, 10);
            AssertExtensions.Throws<ArgumentException>("systemAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, true, ControlFlags.SystemAclPresent, null, null, sacl, null);
            });
            
            // test case 7: DACL is not null, DACL.IsDS is true, but isDS parameter is false
            dacl = new DiscretionaryAcl(true, true, 10);
            AssertExtensions.Throws<ArgumentException>("discretionaryAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, false, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
            });

            // test case 8: DACL is not null, DACL.IsDS is false, but isDS parameter is true
            dacl = new DiscretionaryAcl(true, false, 10);
            AssertExtensions.Throws<ArgumentException>("discretionaryAcl", () =>
            {
                sd = new CommonSecurityDescriptor(true, true, ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
            });
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_Constructor_TestData))]
        public static void TestConstructor(bool isContainer, bool isDS, int flags, string ownerStr, string groupStr, string saclStr, string daclStr)
        {
            ControlFlags controlFlags = ControlFlags.OwnerDefaulted;
            SecurityIdentifier owner = null;
            SecurityIdentifier group = null;
            RawAcl rawAcl = null;
            SystemAcl sacl = null;
            DiscretionaryAcl dacl = null;

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
            Assert.True(VerifyResult(isContainer, isDS, controlFlags, owner, group, sacl, dacl));
        }

        private static bool VerifyResult(bool isContainer, bool isDS, ControlFlags controlFlags, SecurityIdentifier owner, SecurityIdentifier group, SystemAcl sacl, DiscretionaryAcl dacl)
        {
            CommonSecurityDescriptor commonSecurityDescriptor = null;
            bool result = false;
            try
            {

                commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, controlFlags, owner, group, sacl, dacl);
                // verify the result
                if ((isContainer == commonSecurityDescriptor.IsContainer) &&
                    (isDS == commonSecurityDescriptor.IsDS) &&
                    ((((sacl != null) ? (controlFlags | ControlFlags.SystemAclPresent) : (controlFlags & (~ControlFlags.SystemAclPresent)))
                    | ControlFlags.SelfRelative | ControlFlags.DiscretionaryAclPresent) == commonSecurityDescriptor.ControlFlags) &&
                    (owner == commonSecurityDescriptor.Owner) &&
                    (group == commonSecurityDescriptor.Group) &&
                    (sacl == commonSecurityDescriptor.SystemAcl) &&
                    (Utils.ComputeBinaryLength(commonSecurityDescriptor, dacl != null) == commonSecurityDescriptor.BinaryLength))
                {
                    if (dacl == null)
                    {
                        //check the contructor created an empty Dacl with correct IsContainer and isDS info
                        if (isContainer == commonSecurityDescriptor.DiscretionaryAcl.IsContainer &&
                            isDS == commonSecurityDescriptor.DiscretionaryAcl.IsDS &&
                            commonSecurityDescriptor.DiscretionaryAcl.Count == 1 &&
                            Utils.VerifyDaclWithCraftedAce(isContainer, isDS, commonSecurityDescriptor.DiscretionaryAcl))
                        {
                            result = true;
                        }
                        else
                        {
                            result = false;
                        }
                    }
                    else if (dacl == commonSecurityDescriptor.DiscretionaryAcl)
                    {
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }
                }
                else
                {
                    result = false;
                }

            }
            catch (ArgumentException)
            {
                if ((sacl != null && sacl.IsContainer != isContainer) ||
                    (sacl != null && sacl.IsDS != isDS) ||
                    (dacl != null && dacl.IsContainer != isContainer) ||
                    (dacl != null && dacl.IsDS != isDS))
                    result = true;
                else
                {
                    // unexpected exception
                    result = false;
                }
            }
            return result;
        }
    }
}
