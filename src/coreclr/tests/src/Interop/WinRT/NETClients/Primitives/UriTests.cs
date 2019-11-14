// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using TestLibrary;

namespace NetClient
{
    public class UriTests
    {
        public static void RunTest()
        {
            Component.Contracts.IUriTesting target = (Component.Contracts.IUriTesting)WinRTNativeComponent.GetObjectFromNativeComponent("Component.Contracts.UriTesting");
            Uri managedUri = new Uri("https://dot.net");
            Assert.AreEqual(managedUri.ToString(), target.GetFromUri(managedUri));
            Assert.AreEqual(managedUri, target.CreateUriFromString(managedUri.ToString()));
        }
    }
}
