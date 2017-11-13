// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition.Hosting;
using Xunit;

namespace System.Composition.UnitTests
{
    [Export]
    public class Imported { }

    [Export]
    public class TracksImportSatisfaction
    {
        [Import]
        public Imported Imported { get; set; }

        public Imported SetOnImportsSatisfied { get; set; }

        [OnImportsSatisfied]
        public void OnImportsSatisfied()
        {
            SetOnImportsSatisfied = Imported;
        }
    }
    public class ActivationEventOrderingTests : ContainerTests
    {
        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void OnImportsSatisfiedIsCalledAfterPropertyInjection()
        {
            var cc = CreateContainer(typeof(TracksImportSatisfaction), typeof(Imported));

            var tis = cc.GetExport<TracksImportSatisfaction>();

            Assert.NotNull(tis.SetOnImportsSatisfied);
        }
    }
}
