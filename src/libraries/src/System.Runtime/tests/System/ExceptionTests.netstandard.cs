// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Xunit;

namespace System.Tests
{
    public  class ExceptionDerivedTests: Exception
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void Exception_SerializeObjectState()
        {
            var excp = new ExceptionDerivedTests();
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState += (exception, eventArgs) => eventArgs.AddSerializedState(null));
            Assert.Throws<PlatformNotSupportedException>( () => excp.SerializeObjectState -= (exception, eventArgs) => eventArgs.AddSerializedState(null));
        }
    }
}
