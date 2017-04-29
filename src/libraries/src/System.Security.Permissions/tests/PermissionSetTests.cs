// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Security.Permissions.Tests
{
    public class PermissionSetTests
    {
        [Fact]
        public static void PermissionSetCallMethods()
        {
            PermissionSet ps = new PermissionSet(new PermissionState());
            ps.Assert();
            bool containspermissions = ps.ContainsNonCodeAccessPermissions();
            PermissionSet ps2 = ps.Copy();
            ps.CopyTo(new int[1], 0);
            ps.Demand();
            ps.Equals(ps2);
            System.Collections.IEnumerator ie = ps.GetEnumerator();
            int hash = ps.GetHashCode();
            PermissionSet ps3 = ps.Intersect(ps2);
            bool isempty = ps.IsEmpty();
            bool issubsetof = ps.IsSubsetOf(ps2);
            bool isunrestricted = ps.IsUnrestricted();
            string s = ps.ToString();
            PermissionSet ps4 = ps.Union(ps2);
            SecurityElement se = new SecurityElement("");
            ps.FromXml(se);
            se = ps.ToXml();
        }

        [Fact]
        public static void PermissionSetAttributeCallMethods()
        {
            PermissionSetAttribute psa = new PermissionSetAttribute(new Permissions.SecurityAction());
            IPermission ip = psa.CreatePermission();
            PermissionSet ps = psa.CreatePermissionSet();
        }

        [Fact]
        public static void NamedPermissionSetCallMethods()
        {
            NamedPermissionSet nps = new NamedPermissionSet("Test");
            PermissionSet ps = nps.Copy();
            NamedPermissionSet nps2 = nps.Copy("Test");
            nps.Equals(nps2);
            int hash = nps.GetHashCode();
            SecurityElement se = new SecurityElement("");
            nps.FromXml(se);
            se = nps.ToXml();
        }
    }
}
