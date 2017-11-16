// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.UnitTests;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.Lightweight.UnitTests
{
    public class LooseImportsTests : ContainerTests
    {
        [Export]
        public class Transaction { }

        public class SaveChangesAttribute
        {
            [Import]
            public Transaction Transaction { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void SatisfyImportsSetsLooseImportsOnAttributedPart()
        {
            var container = CreateContainer(typeof(Transaction));
            var hasLoose = new SaveChangesAttribute();
            container.SatisfyImports(hasLoose);
            Assert.NotNull(hasLoose.Transaction);
        }
    }
}
