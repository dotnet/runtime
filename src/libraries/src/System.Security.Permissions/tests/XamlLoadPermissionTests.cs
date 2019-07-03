// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Configuration;
using System.DirectoryServices;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Web;
using System.Xaml.Permissions;

namespace System.Xaml.Permissions.Tests
{
    public class XamlLoadPermissionTests
    {
        [Fact]
        public static void XamlLoadPermissionCallMethods()
        {
            XamlAccessLevel accessLevel = XamlAccessLevel.AssemblyAccessTo(Assembly.GetExecutingAssembly().GetName());
            XamlLoadPermission xp = new XamlLoadPermission(accessLevel);
            XamlLoadPermission xp2 = new XamlLoadPermission(PermissionState.Unrestricted);
            XamlLoadPermission xp3 = new XamlLoadPermission(Array.Empty<XamlAccessLevel>());
            bool testbool = xp.IsUnrestricted();
            IPermission ip = xp.Copy();
            IPermission ip2 = xp.Intersect(ip);
            IPermission ip3 = xp.Union(ip);
            testbool = xp.IsSubsetOf(ip);
            testbool = xp.Equals(new object());
            testbool = xp.Includes(accessLevel);
            int hash = xp.GetHashCode();
            SecurityElement se = new SecurityElement("");
            xp.FromXml(se);
            se = xp.ToXml();
        }
    }
}
