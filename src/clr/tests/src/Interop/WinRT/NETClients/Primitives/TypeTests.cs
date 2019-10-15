// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using TestLibrary;

namespace NetClient
{
    public class TypeTests
    {
        public static void RunTest()
        {
            Component.Contracts.ITypeTesting target = (Component.Contracts.ITypeTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.TypeTesting");
            
            // Non-WinRT managed types pass their assembly-qualified name
            Assert.AreEqual(typeof(TypeTests).AssemblyQualifiedName, target.GetTypeName(typeof(TypeTests)));

            // WinRT types pass their full name (not assembly-qualified)
            Assert.AreEqual(typeof(Component.Contracts.ITypeTesting).FullName, target.GetTypeName(typeof(Component.Contracts.ITypeTesting)));

            // Projected types pass the name of the type they are projected from
            Assert.AreEqual("Windows.UI.Xaml.Interop.TypeName", target.GetTypeName(typeof(Type)));
        }
    }
}
