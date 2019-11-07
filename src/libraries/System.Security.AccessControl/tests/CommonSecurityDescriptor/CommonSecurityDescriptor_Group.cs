// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Security.Principal;
using Xunit;

namespace System.Security.AccessControl.Tests
{
    public partial class CommonSecurityDescriptor_Group
    {
       public static IEnumerable<object[]> CommonSecurityDescriptor_Group_TestData()
       {
           yield return new object[] { null };
           yield return new object[] { "S-1-0-0" };
           yield return new object[] { "S-1-1-0" };
           yield return new object[] { "S-1-2-0" };
           yield return new object[] { "S-1-3-0" };
           yield return new object[] { "S-1-4-0" };
           yield return new object[] { "S-1-5-0" };
           yield return new object[] { "S-1-6-0" };
           yield return new object[] { "S-1-7-0" };
           yield return new object[] { "S-1-8-0" };
           yield return new object[] { "S-1-9-0" };
           yield return new object[] { "S-1-10-0" };
           yield return new object[] { "S-1-11-0" };
           yield return new object[] { "S-1-5-0" };
           yield return new object[] { "S-1-5-0-1-2-3-4-5-6-7" };
           yield return new object[] { "S-1-5-0-1-2-3-4-5-6-7-8-9-10-11-12-13-14" };
           yield return new object[] { "S-1-0-0" };
           yield return new object[] { "S-1-1-0" };
           yield return new object[] { "S-1-2-0" };
           yield return new object[] { "S-1-3-0" };
           yield return new object[] { "S-1-3-1" };
           yield return new object[] { "S-1-3-2" };
           yield return new object[] { "S-1-3-3" };
           yield return new object[] { "S-1-5-1" };
           yield return new object[] { "S-1-5-2" };
           yield return new object[] { "S-1-5-3" };
           yield return new object[] { "S-1-5-4" };
           yield return new object[] { "S-1-5-6" };
           yield return new object[] { "S-1-5-7" };
           yield return new object[] { "S-1-5-8" };
           yield return new object[] { "S-1-5-9" };
           yield return new object[] { "S-1-5-10" };
           yield return new object[] { "S-1-5-11" };
           yield return new object[] { "S-1-5-12" };
           yield return new object[] { "S-1-5-13" };
           yield return new object[] { "S-1-5-14" };
           yield return new object[] { "S-1-5-18" };
           yield return new object[] { "S-1-5-19" };
           yield return new object[] { "S-1-5-20" };
           yield return new object[] { "S-1-5-32" };
           yield return new object[] { "S-1-5-32-544" };
           yield return new object[] { "S-1-5-32-545" };
           yield return new object[] { "S-1-5-32-546" };
           yield return new object[] { "S-1-5-32-547" };
           yield return new object[] { "S-1-5-32-548" };
           yield return new object[] { "S-1-5-32-549" };
           yield return new object[] { "S-1-5-32-550" };
           yield return new object[] { "S-1-5-32-551" };
           yield return new object[] { "S-1-5-32-552" };
           yield return new object[] { "S-1-5-32-554" };
           yield return new object[] { "S-1-5-32-555" };
           yield return new object[] { "S-1-5-32-556-0" };
        }

        [Theory]
        [MemberData(nameof(CommonSecurityDescriptor_Group_TestData))]
        public static void TestGroup(string newGroupStr)
        {
            bool isContainer = false;
            bool isDS = false;
            int controlFlags = 1;
            string ownerStr = "BA";
            string groupStr = "BG";

            CommonSecurityDescriptor commonSecurityDescriptor = null;
            SecurityIdentifier owner = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(ownerStr));
            SecurityIdentifier newGroup = (newGroupStr != null ? new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(newGroupStr)) : null);
            SecurityIdentifier group = new SecurityIdentifier(Utils.TranslateStringConstFormatSidToStandardFormatSid(groupStr));
            SystemAcl sacl = null;
            DiscretionaryAcl dacl = null;
            commonSecurityDescriptor = new CommonSecurityDescriptor(isContainer, isDS, (ControlFlags)controlFlags, owner, group, sacl, dacl);
            commonSecurityDescriptor.Group = newGroup;
            // verify the result, we can use == here as SecurityIdentifier overrides the comparsison
            Assert.True(newGroup == commonSecurityDescriptor.Group);
        }
    }
}