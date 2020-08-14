// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Xaml.Permissions;
using Xunit;

namespace System.Security.Permissions.Tests
{
    public class XamlAccessLevelTests
    {
        [Fact]
        public static void XamlAccessLevelTestsCallMethods()
        {
            Assembly execAssembly = Assembly.GetExecutingAssembly();
            Type type = typeof(int);
            XamlAccessLevel accessLevel = XamlAccessLevel.AssemblyAccessTo(execAssembly);
            XamlAccessLevel accessLevel2 = XamlAccessLevel.AssemblyAccessTo(execAssembly.GetName());
            XamlAccessLevel accessLevel3 = XamlAccessLevel.PrivateAccessTo(type);
            XamlAccessLevel accessLevel4 = XamlAccessLevel.PrivateAccessTo(type.AssemblyQualifiedName);
            AssemblyName an = accessLevel.AssemblyAccessToAssemblyName;
        }
    }
}
