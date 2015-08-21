// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.Composition.UnitTests
{
    public class OptionalImportTests : ContainerTests
    {
        private class Missing { }

        [Export]
        private class Supplied { }

        [Export]
        private class HasOptionalConstructorParameter
        {
            public Missing Missing { get; set; }
            public Supplied Supplied { get; set; }

            [ImportingConstructor]
            public HasOptionalConstructorParameter([Import(AllowDefault = true)] Missing missing, Supplied supplied)
            {
                Missing = missing;
                Supplied = supplied;
            }
        }

        [Export]
        private class HasOptionalProperty
        {
            private Missing _missing;

            public bool WasMissingSetterCalled { get; set; }

            [Import(AllowDefault = true)]
            public Missing Missing
            {
                get { return _missing; }
                set
                {
                    WasMissingSetterCalled = true;
                    _missing = value;
                }
            }

            [Import]
            public Supplied Supplied { get; set; }
        }

        [Fact]
        public void MissingOptionalConstructorParametersAreSuppliedTheirDefaultValue()
        {
            var cc = CreateContainer(typeof(Supplied), typeof(HasOptionalConstructorParameter));
            var ocp = cc.GetExport<HasOptionalConstructorParameter>();
            Assert.Null(ocp.Missing);
            Assert.NotNull(ocp.Supplied);
        }

        [Fact]
        public void MissingOptionalPropertyImportsAreIgnored()
        {
            var cc = CreateContainer(typeof(Supplied), typeof(HasOptionalProperty));
            var op = cc.GetExport<HasOptionalProperty>();
            Assert.NotNull(op.Supplied);
            Assert.Null(op.Missing);
            Assert.False(op.WasMissingSetterCalled);
        }
    }
}
