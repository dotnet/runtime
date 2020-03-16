// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Testing;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class DockerTests
    {
        [ConditionalFact]
        [DockerOnly]
        [Trait("Docker", "true")]
        public void DoesNotRunOnWindows()
        {
            Assert.False(RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }
    }
}
