// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class ErrorMessageQualityTests : ContainerTests
    {
        public class Unregistered { }

        [Export]
        public class UserOfUnregistered
        {
            [Import]
            public Unregistered Unregistered { get; set; }
        }

        [Export]
        public class CycleA
        {
            [Import]
            public CycleB B { get; set; }
        }

        [Export]
        public class CycleB
        {
            [Import]
            public CycleC C { get; set; }
        }

        [Export]
        public class CycleC
        {
            [Import]
            public CycleA A { get; set; }
        }

        [Export]
        public class ShouldBeOne { }

        public class ButThereIsAnother
        {
            [Export(typeof(ShouldBeOne))]
            public ShouldBeOne Another { get { return new ShouldBeOne(); } }
        }

        [Export]
        public class RequiresOnlyOne
        {
            [Import]
            public ShouldBeOne One { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void MissingTopLevelExportMessageIsInformative()
        {
            var cc = CreateContainer();
            var x = Assert.Throws<CompositionFailedException>(() => cc.GetExport<Unregistered>());
            Assert.Equal("No export was found for the contract 'Unregistered'.", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void MissingTopLevelNamedExportMessageIsInformative()
        {
            var cc = CreateContainer();
            var x = Assert.Throws<CompositionFailedException>(() => cc.GetExport<Unregistered>("unregistered"));
            Assert.Equal("No export was found for the contract 'Unregistered \"unregistered\"'.", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void MissingDependencyMessageIsInformative()
        {
            var cc = CreateContainer(typeof(UserOfUnregistered));
            var x = Assert.Throws<CompositionFailedException>(() => cc.GetExport<UserOfUnregistered>());
            Assert.Equal("No export was found for the contract 'Unregistered'" + Environment.NewLine +
                            " -> required by import 'Unregistered' of part 'UserOfUnregistered'" + Environment.NewLine +
                            " -> required by initial request for contract 'UserOfUnregistered'", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void CycleMessageIsInformative()
        {
            var cc = CreateContainer(typeof(CycleA), typeof(CycleB), typeof(CycleC));
            var x = Assert.Throws<CompositionFailedException>(() => cc.GetExport<CycleA>());
            Assert.Equal("Detected an unsupported cycle for part 'CycleA'." +
                            " To construct a valid cycle, at least one part in the cycle must be shared, and at least one import in the cycle must be non-prerequisite (e.g. a property)." + Environment.NewLine +
                            " -> required by import 'A' of part 'CycleC'" + Environment.NewLine +
                            " -> required by import 'C' of part 'CycleB'" + Environment.NewLine +
                            " -> required by import 'B' of part 'CycleA'" + Environment.NewLine +
                            " -> required by initial request for contract 'CycleA'", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50919", TestPlatforms.Android)]
        public void CardinalityViolationMessageIsInformative()
        {
            var cc = CreateContainer(typeof(ShouldBeOne), typeof(ButThereIsAnother), typeof(RequiresOnlyOne));
            var x = Assert.Throws<CompositionFailedException>(() => cc.GetExport<RequiresOnlyOne>());
            Assert.Equal("Only one export for the contract 'ShouldBeOne' is allowed, but the following parts: 'ButThereIsAnother', 'ShouldBeOne' export it." + Environment.NewLine +
                            " -> required by import 'One' of part 'RequiresOnlyOne'" + Environment.NewLine +
                            " -> required by initial request for contract 'RequiresOnlyOne'", x.Message);
        }
    }
}
