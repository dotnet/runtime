// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.Configuration;
using System.DirectoryServices;
using System.Reflection;
using System.Web;

namespace System.Security.Permissions.Tests
{
    public class DirectoryServicesPermissionTests
    {
        [Fact]
        public static void DirectoryServicesPermissionCallMethods()
        {
            DirectoryServicesPermission dsp = new DirectoryServicesPermission(new PermissionState());
            DirectoryServicesPermission other = new DirectoryServicesPermission();
            other = new DirectoryServicesPermission(default(DirectoryServicesPermissionAccess), "test");
            DirectoryServicesPermissionEntryCollection pe = other.PermissionEntries;
        }

        [Fact]
        public static void DirectoryServicesPermissionAttributeCallMethods()
        {
            var dpa = new DirectoryServicesPermissionAttribute(new SecurityAction());
            DirectoryServicesPermissionAccess pa = dpa.PermissionAccess;
            string path = dpa.Path;
            IPermission ip = dpa.CreatePermission();
        }
    }
}
