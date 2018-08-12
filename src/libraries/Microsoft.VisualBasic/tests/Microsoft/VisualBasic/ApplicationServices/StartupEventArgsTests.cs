// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using Xunit;

namespace Microsoft.VisualBasic.ApplicationServices.Tests
{
    public class StartupEventArgsTests
    {
        [Fact]
        public void Ctor_ReadOnlyCollection()
        {
            var collection = new ReadOnlyCollection<string>(new string[] { "a" });
            var args = new StartupEventArgs(collection);
            Assert.Same(collection, args.CommandLine);
        }

        [Fact]
        public void Ctor_NullCommandLine_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("list", () => new StartupEventArgs(null));
        }
    }
}
