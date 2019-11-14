// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class PropertyExportTests : ContainerTests
    {
        public class Messenger
        {
            [Export]
            public string Message { get { return "Helo!"; } }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void CanExportProperty()
        {
            var cc = CreateContainer(typeof(Messenger));

            var x = cc.GetExport<string>();

            Assert.Equal("Helo!", x);
        }

        [Export, Shared]
        public class SelfObsessed
        {
            [Export]
            public SelfObsessed Self { get { return this; } }
        }

        [Export]
        public class Selfless
        {
            [ImportMany]
            public IList<SelfObsessed> Values { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ExportedPropertiesShareTheSameSharedPartInstance()
        {
            var cc = CreateContainer(typeof(SelfObsessed), typeof(Selfless));
            var sl = cc.GetExport<Selfless>();
            Assert.Same(sl.Values[0], sl.Values[1]);
        }
    }
}
