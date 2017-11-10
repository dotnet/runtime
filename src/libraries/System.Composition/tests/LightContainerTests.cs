// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Composition;
using System.Composition.Hosting;
using System.Composition.Convention;
using System.Reflection;
using TestLibrary;
using Xunit;

namespace System.Composition.UnitTests
{
    public class LightContainerTests : ContainerTests
    {
        public interface IA { }

        [Export(typeof(IA))]
        public class A : IA, IDisposable
        {
            public bool IsDisposed;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Export(typeof(IA))]
        public class A2 : IA { }

        [Export]
        public class B
        {
            public IA A;

            [ImportingConstructor]
            public B(IA ia)
            {
                A = ia;
            }
        }

        public class BarePart { }

        public class HasPropertyA
        {
            public IA A { get; set; }
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void CreatesInstanceWithNoDependencies()
        {
            var cc = CreateContainer(typeof(A));
            var x = cc.GetExport<IA>();
            Assert.IsAssignableFrom(typeof(A), x);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void DefaultLifetimeIsNonShared()
        {
            var cc = CreateContainer(typeof(A));
            var x = cc.GetExport<IA>();
            var y = cc.GetExport<IA>();
            Assert.NotSame(x, y);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void Composes()
        {
            var cc = CreateContainer(typeof(A), typeof(B));
            var x = cc.GetExport<B>();
            Assert.IsAssignableFrom(typeof(A), x.A);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void CanSpecifyExportsWithConventionBuilder()
        {
            var rb = new ConventionBuilder();
            rb.ForType<BarePart>().Export();
            var cc = CreateContainer(rb, typeof(BarePart));
            var x = cc.GetExport<BarePart>();
            Assert.NotNull(x);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void CanSpecifyLifetimeWithConventionBuilder()
        {
            var rb = new ConventionBuilder();
            rb.ForType<BarePart>().Export().Shared();
            var cc = CreateContainer(rb, typeof(BarePart));
            var x = cc.GetExport<BarePart>();
            var y = cc.GetExport<BarePart>();
            Assert.Same(x, y);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void InjectsPropertyImports()
        {
            var rb = new ConventionBuilder();
            rb.ForType<HasPropertyA>().ImportProperty(a => a.A).Export();
            var cc = CreateContainer(rb, typeof(HasPropertyA), typeof(A));
            var x = cc.GetExport<HasPropertyA>();
            Assert.IsAssignableFrom(typeof(A), x.A);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void VerifyAssemblyNameCanBeUsedWithContainer()
        {
            var test = new ContainerConfiguration()
                .WithAssembly(typeof(ClassWithDependecy).GetTypeInfo().Assembly)
                .CreateContainer();
            var b = test.GetExport<ClassWithDependecy>();
            Assert.NotNull(b);
            Assert.NotNull(b._dep);
        }

        [Fact]
        [ActiveIssue(24903, TargetFrameworkMonikers.NetFramework)]
        public void VerifyAssemblyWithTwoBaseTypeWithOnlyOneExportedWorks()
        {
            var test = new ContainerConfiguration()
                .WithAssembly(typeof(ClassWithDependecy).GetTypeInfo().Assembly)
                .CreateContainer();
            var b = test.GetExport<ClassWithDependecyAndSameBaseType>();
            Assert.NotNull(b);
            Assert.NotNull(b._dep);
            Assert.Equal(b._dep.GetType(), typeof(TestDependency));
        }
    }
}
