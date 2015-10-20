// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using Xunit;

namespace System.Reflection.Tests
{
    public class EntryPointTests
    {
        [Fact]
        public void CurrentAssemblyDoesNotHaveAnEntryPoint()
        {
            Assert.Null(typeof(EntryPointTests).GetTypeInfo().Assembly.EntryPoint);
        }
    }
}