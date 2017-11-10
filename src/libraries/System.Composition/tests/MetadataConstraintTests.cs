// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class MetadataConstraintTests : ContainerTests
    {
        [Export, ExportMetadata("SettingName", "TheName")]
        public class SomeSetting { }

        [Export]
        public class SomeSettingUser
        {
            [Import, ImportMetadataConstraint("SettingName", "TheName")]
            public SomeSetting Setting { get; set; }
        }

        [Export]
        public class ManySettingUser
        {
            [ImportMany, ImportMetadataConstraint("SettingName", "TheName")]
            public IEnumerable<SomeSetting> Settings { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void AnImportMetadataConstraintMatchesMetadataOnTheExport()
        {
            var cc = CreateContainer(typeof(SomeSetting), typeof(SomeSettingUser));
            var ssu = cc.GetExport<SomeSettingUser>();
            Assert.NotNull(ssu.Setting);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void AnImportMetadataConstraintMatchesMetadataOnTheExportEvenIfDiscoveryHasCompletedForTheExport()
        {
            var cc = CreateContainer(typeof(SomeSetting), typeof(SomeSettingUser));
            cc.GetExport<SomeSetting>();
            var ssu = cc.GetExport<SomeSettingUser>();
            Assert.NotNull(ssu.Setting);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ImportMetadataConstraintsComposeWithOtherRelationshipTypes()
        {
            var cc = CreateContainer(typeof(SomeSetting), typeof(ManySettingUser));
            var ssu = cc.GetExport<ManySettingUser>();
            Assert.Equal(1, ssu.Settings.Count());
        }

        [Export, ExportMetadata("SettingName", "TheName")]
        public class SomeSetting<T> { }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ConstraintsCanBeAppliedToGenerics()
        {
            var contract = new CompositionContract(typeof(SomeSetting<string>), null, new Dictionary<string, object>
            {
                { "SettingName", "TheName" }
            });

            var c = CreateContainer(typeof(SomeSetting<>));
            object s;
            Assert.True(c.TryGetExport(contract, out s));
        }

        [Export, ExportMetadata("Items", new[] { 1, 2, 3 })]
        public class Presenter { }

        [Export]
        public class Controller
        {
            [Import, ImportMetadataConstraint("Items", new[] { 1, 2, 3 })]
            public Presenter Presenter { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void ItemEqualityIsUsedWhenMatchingMetadataValuesThatAreArrays()
        {
            var c = CreateContainer(typeof(Presenter), typeof(Controller));
            var ctrl = c.GetExport<Controller>();
            Assert.IsAssignableFrom(typeof(Presenter), ctrl.Presenter);
        }
    }
}
