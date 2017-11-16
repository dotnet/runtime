// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Composition.Convention;
using System.Composition.Runtime;
using System.Composition.UnitTests.Util;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition.Hosting;
using Xunit;

namespace System.Composition.UnitTests
{
    public class DiscoveryTests : ContainerTests
    {
        public interface IRule { }

        public class RuleExportAttribute : ExportAttribute
        {
            public RuleExportAttribute() : base(typeof(IRule)) { }
        }

        [RuleExport]
        public class UnfairRule : IRule { }

        [Export(typeof(IRule))]
        public class IncompatibleRule { }

        public class IncompatibleRuleProperty
        {
            [Export(typeof(IRule))]
            public string Rule { get; set; }
        }

        [Export, PartNotDiscoverable]
        public class NotDiscoverable { }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DiscoversCustomExportAttributes()
        {
            var container = CreateContainer(typeof(UnfairRule));
            var rule = container.GetExport<IRule>();
            Assert.IsAssignableFrom(typeof(UnfairRule), rule);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DiscoversCustomExportAttributesUnderConventions()
        {
            var container = CreateContainer(new ConventionBuilder(), typeof(UnfairRule));
            var rule = container.GetExport<IRule>();
            Assert.IsAssignableFrom(typeof(UnfairRule), rule);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20656", TargetFrameworkMonikers.UapAot)]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void InstanceExportsOfIncompatibleContractsAreDetected()
        {
            var x = Assert.Throws<CompositionFailedException>(() => CreateContainer(typeof(IncompatibleRule)));
            Assert.Equal("Exported contract type 'IRule' is not assignable from part 'IncompatibleRule'.", x.Message);
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20656", TargetFrameworkMonikers.UapAot)]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void PropertyExportsOfIncompatibleContractsAreDetected()
        {
            var x = Assert.Throws<CompositionFailedException>(() => CreateContainer(typeof(IncompatibleRuleProperty)));
            Assert.Equal("Exported contract type 'IRule' is not assignable from property 'Rule' of part 'IncompatibleRuleProperty'.", x.Message);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ANonDiscoverablePartIsIgnored()
        {
            var container = CreateContainer(typeof(NotDiscoverable));
            NotDiscoverable unused;
            Assert.False(container.TryGetExport(null, out unused));
        }

        public interface IBus { }

        [Export(typeof(IBus))]
        public class CloudBus : IBus { }

        public class SpecialCloudBus : CloudBus { }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DoesNotDiscoverExportAttributesFromBase()
        {
            var container = CreateContainer(typeof(SpecialCloudBus));

            IBus bus;
            Assert.False(container.TryGetExport(null, out bus));
        }

        public abstract class BaseController
        {
            [Import]
            public IBus Bus { get; set; }
        }

        [Export]
        public class HomeController : BaseController
        {
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void SatisfiesImportsAppliedToBase()
        {
            var container = CreateContainer(typeof(HomeController), typeof(CloudBus));
            var hc = container.GetExport<HomeController>();
            Assert.IsAssignableFrom(typeof(CloudBus), hc.Bus);
        }

        private class CustomImportAttribute : ImportAttribute { }

        [Export]
        public class MultipleImportsOnProperty
        {
            [Import, CustomImport]
            public string MultiImport { get; set; }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/corefx/issues/20656", TargetFrameworkMonikers.UapAot)]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void MultipleImportAttributesAreDetected()
        {
            var c = new ContainerConfiguration()
                .WithPart<MultipleImportsOnProperty>()
                .CreateContainer();

            var x = Assert.Throws<CompositionFailedException>(() => c.GetExport<MultipleImportsOnProperty>());
            Assert.Equal("Multiple imports have been configured for 'MultiImport'. At most one import can be applied to a single site.", x.Message);
        }
    }
}
