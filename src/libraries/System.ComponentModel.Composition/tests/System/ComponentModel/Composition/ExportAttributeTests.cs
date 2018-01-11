// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Hosting;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    class ExportImplementer
    {
        [Export]
        public ExportOnIndexer this[int index]
        {
            get { return new ExportOnIndexer(); }
        }
    }

    public class ExportOnIndexer { }
    
    public class ExportAttributeTests
    {
        [Fact]
        public void Constructor1_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ExportAttribute();

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor2_NullAsContractNameArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ExportAttribute((string)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor3_NullAsContractTypeArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ExportAttribute((Type)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor4_NullAsContractTypeArgument_ShouldSetContractNamePropertyToEmptyString()
        {
            var attribute = new ExportAttribute((string)null, (Type)null);

            Assert.Null(attribute.ContractName);
            Assert.Null(attribute.ContractType);
        }

        [Fact]
        public void Constructor2_ValueAsContractNameArgument_ShouldSetContractNameProperty()
        {
            var expectations = Expectations.GetContractNamesWithEmpty();
            
            foreach (var e in expectations)
            {
                var attribute = new ExportAttribute(e);

                Assert.Equal(e, attribute.ContractName);
            }
        }

        [Fact]
        public void ExportIndexers_ShouldThrowSomething()
        {
            var con = new CompositionContainer(
                new TypeCatalog(typeof(WorkingType), typeof(Constants), typeof(ExportImplementer), typeof(ExportOnIndexer))
            );

            var v1 = con.GetExportedValue<WorkingType>();

            ExceptionAssert.Throws<CompositionException>(RetryMode.DoNotRetry, () =>
            {
                var v2 = con.GetExportedValue<ExportOnIndexer>();
                Console.WriteLine(v2.ToString());
            });
        }
    
    }
}
