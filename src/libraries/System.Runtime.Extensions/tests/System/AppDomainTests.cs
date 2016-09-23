// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Tests
{
    public class AppDomainTests : RemoteExecutorTestBase
    {
        [Fact]
        public void CurrentDomain_Not_Null()
        {
            Assert.NotNull(AppDomain.CurrentDomain);
        }

        [Fact]
        public void CurrentDomain_Idempotent()
        {
            Assert.Equal(AppDomain.CurrentDomain, AppDomain.CurrentDomain);
        }

        [Fact]
        public void BaseDirectory_Same_As_AppContext()
        {
            Assert.Equal(AppDomain.CurrentDomain.BaseDirectory, AppContext.BaseDirectory);
        } 

        [Fact]
        public void RelativeSearchPath_Is_Null()
        {
            Assert.Null(AppDomain.CurrentDomain.RelativeSearchPath);
        } 
    }
}
