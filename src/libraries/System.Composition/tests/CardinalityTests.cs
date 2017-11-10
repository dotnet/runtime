// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class CardinalityTests : ContainerTests
    {
        public interface ILog { }

        [Export(typeof(ILog))]
        public class LogA : ILog { }

        [Export(typeof(ILog))]
        public class LogB : ILog { }

        [Export]
        public class UsesLog
        {
            [ImportingConstructor]
            public UsesLog(ILog log) { }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void RequestingOneWhereMultipleArePresentFails()
        {
            var c = CreateContainer(typeof(LogA), typeof(LogB));
            var x = Assert.Throws<CompositionFailedException>(() =>
                c.GetExport<ILog>());
            Assert.True(x.Message.Contains("LogA"));
            Assert.True(x.Message.Contains("LogB"));
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ImportingOneWhereMultipleArePresentFails()
        {
            var c = CreateContainer(typeof(LogA), typeof(LogB), typeof(UsesLog));
            var x = Assert.Throws<CompositionFailedException>(() =>
                c.GetExport<UsesLog>());
            Assert.True(x.Message.Contains("LogA"));
            Assert.True(x.Message.Contains("LogB"));
        }
    }
}
