// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    [Export]
    public class WorkingType
    {
    }

    [Export]
    public class Constants
    {
        [Export("Seven")]
        int seven;

        public Constants()
        {
            seven = 7;
            if (seven == default)
                throw new ArgumentException();
        }
    }

    [Export]
    public class ExportWithIndexer
    {
        private int[] data = new int[10];

        [Import("Seven")]
        public int this[int index]
        {
            get { return data[index]; }
            set { data[index] = value; }
        }
    }
    
    public class ImportAttributeTests
    {
        [Fact]
        public void Constructor1_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ImportAttribute();

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor2_NullAsContractNameArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ImportAttribute((string)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor3_NullAsContractTypeArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ImportAttribute((Type)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor4_NullAsContractTypeArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ImportAttribute((string)null, (Type)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor2_ValueAsContractNameArgument_ShouldSetContractNameProperty()
        {
            var expectations = Expectations.GetContractNamesWithEmpty();

            foreach (var e in expectations)
            {
                var attribute = new ImportAttribute(e);

                Assert.Equal(e, attribute.ContractName);
            }
        }

        [Fact]
        public void Constructor1_ShouldSetAllowDefaultPropertyToFalse()
        {
            var attribute = new ImportAttribute();

            Assert.False(attribute.AllowDefault);
        }

        [Fact]
        public void Constructor2_ShouldSetAllowDefaultPropertyToFalse()
        {
            var attribute = new ImportAttribute("ContractName");

            Assert.False(attribute.AllowDefault);
        }

        [Fact]
        public void Constructor3_ShouldSetAllowDefaultPropertyToFalse()
        {
            var attribute = new ImportAttribute(typeof(String));

            Assert.False(attribute.AllowDefault);
        }

        [Fact]
        public void Constructor1_ShouldSetAllowRecompositionPropertyToFalse()
        {
            var attribute = new ImportAttribute();

            Assert.False(attribute.AllowRecomposition);
        }

        [Fact]
        public void Constructor2_ShouldSetAllowRecompositionPropertyToFalse()
        {
            var attribute = new ImportAttribute("ContractName");

            Assert.False(attribute.AllowRecomposition);
        }

        [Fact]
        public void Constructor3_ShouldSetAllowRecompositionPropertyToFalse()
        {
            var attribute = new ImportAttribute(typeof(String));

            Assert.False(attribute.AllowRecomposition);
        }

        [Fact]
        public void AllowDefault_ValueAsValueArgument_ShouldSetProperty()
        {
            var expectations = Expectations.GetBooleans();

            var attribute = new ImportAttribute();

            foreach (var e in expectations)
            {
                attribute.AllowDefault = e;
                Assert.Equal(e, attribute.AllowDefault);
            }
        }

        [Fact]
        public void AllowRecomposition_ValueAsValueArgument_ShouldSetProperty()
        {
            var expectations = Expectations.GetBooleans();

            var attribute = new ImportAttribute();

            foreach (var e in expectations)
            {
                attribute.AllowRecomposition = e;
                Assert.Equal(e, attribute.AllowRecomposition);
            }
        }

        [Fact]
        public void ImportIndexers_ShouldThrowSomething()
        {
            var con = new CompositionContainer(
                new TypeCatalog(typeof(WorkingType), typeof(Constants), typeof(ExportWithIndexer))
            );

            var v1 = con.GetExportedValue<WorkingType>();

            ExceptionAssert.Throws<CompositionException>(RetryMode.DoNotRetry, () =>
            {
                var v2 = con.GetExportedValue<ExportWithIndexer>();
                Console.WriteLine(v2.ToString());
            });
        }
    }
}
