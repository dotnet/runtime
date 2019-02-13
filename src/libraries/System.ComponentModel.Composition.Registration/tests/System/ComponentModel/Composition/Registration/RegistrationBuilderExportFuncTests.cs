// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using Xunit;

namespace System.ComponentModel.Composition.Registration.Tests
{
    public class RegistrationBuilderExportFuncUnitTests
    {
        public interface IFoo { }
        public class Class1
        {
            [Import]
            public Func<IFoo> Foo { get; set; }
        }
        public class Factory
        {
            [Export]
            public IFoo Create() { return null; }
        }

        [Fact]
        [ActiveIssue(35144, TargetFrameworkMonikers.UapAot)]
        public void RegistrationBuilder_WithExportDelegatesShouldNotThrow()
        {
            var rb = new RegistrationBuilder();
            var cat = new TypeCatalog(new Type[] { typeof(Class1), typeof(Factory) }, rb);

            CompositionService cs = cat.CreateCompositionService();
            var test = new Class1();
            cs.SatisfyImportsOnce(test);
        }
    }
}
