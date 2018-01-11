// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.ReflectionModel;
using System.Linq.Expressions;
using System.Reflection;
using System.UnitTesting;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class ConstraintServicesTests
    {
        [Fact]
        public void TypeIdentityConstraint_ValidMatchingExportDef_ShouldMatch()
        {
            var contractName = "MyContract";
            var typeIdentity = AttributedModelServices.GetTypeIdentity(typeof(ConstraintServicesTests));
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity);

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, typeIdentity, null, CreationPolicy.Any);

            var predicate = constraint.Compile();

            Assert.True(predicate(exportDefinition));
        }

        [Fact]
        public void TypeIdentityConstraint_ValidNonMatchingExportDef_ShouldNotMatch()
        {
            var contractName = "MyContract";
            var typeIdentity = AttributedModelServices.GetTypeIdentity(typeof(ConstraintServicesTests));
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.ExportTypeIdentityMetadataName, typeIdentity + "Another Identity");

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, typeIdentity, null, CreationPolicy.Any);

            var predicate = constraint.Compile();

            Assert.False(predicate(exportDefinition));
        }

        [Fact]
        public void TypeIdentityConstraint_InvalidExportDef_ShouldNotMatch()
        {
            var contractName = "MyContract";
            var typeIdentity = AttributedModelServices.GetTypeIdentity(typeof(ConstraintServicesTests));
            var metadata = new Dictionary<string, object>();

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, typeIdentity, null, CreationPolicy.Any);

            var predicate = constraint.Compile();

            Assert.False(predicate(exportDefinition));
        }

        [Fact]
        public void CreationPolicyConstraint_ValidMatchingCreationPolicy_ShouldMatch()
        {
            var contractName = "MyContract";
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.PartCreationPolicyMetadataName, CreationPolicy.Shared);

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, null, null, CreationPolicy.Shared);

            var predicate = constraint.Compile();

            Assert.True(predicate(exportDefinition));
        }

        [Fact]
        public void CreationPolicyConstraint_ValidNonMatchingCreationPolicy_ShouldNotMatch()
        {
            var contractName = "MyContract";
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.PartCreationPolicyMetadataName, CreationPolicy.NonShared);

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, null, null, CreationPolicy.Shared);

            var predicate = constraint.Compile();

            Assert.False(predicate(exportDefinition));
        }

        [Fact]
        public void CreationPolicyConstraint_InvalidCreationPolicy_ShouldNotMatch()
        {
            var contractName = "MyContract";
            var metadata = new Dictionary<string, object>();
            metadata.Add(CompositionConstants.PartCreationPolicyMetadataName, "Shared");

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, null, null, CreationPolicy.Shared);

            var predicate = constraint.Compile();

            Assert.False(predicate(exportDefinition));
        }

        [Fact]
        public void CreationPolicyConstraint_NoCreationPolicy_ShouldNotMatch()
        {
            var contractName = "MyContract";
            var metadata = new Dictionary<string, object>();

            var exportDefinition = new ExportDefinition(contractName, metadata);

            var constraint = ConstraintServices.CreateConstraint(contractName, null, null, CreationPolicy.Shared);

            var predicate = constraint.Compile();

            Assert.True(predicate(exportDefinition));
        }

        [Fact]
        public void PartCreatorConstraint_ShouldMatchPartCreatorExportDefinition()
        {
            var partCreatorImportDef = ReflectionModelServices.CreateImportDefinition(
                new LazyMemberInfo(MemberTypes.Field, () => new MemberInfo[] { typeof(ConstraintServicesTests) }),
                "Foo",
                "Foo",
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("MDKey", typeof(string)) },
                ImportCardinality.ZeroOrMore,
                false,
                CreationPolicy.Any,
                MetadataServices.EmptyMetadata,
                true, // IsPartCreator
                null);

            var metadata = new Dictionary<string, object>();
            metadata["MDKey"] = "MDValue";
            metadata[CompositionConstants.ExportTypeIdentityMetadataName] = "Foo";

            var productExportDefinition = new ExportDefinition("Foo", metadata);

            metadata = new Dictionary<string, object>(metadata);
            metadata[CompositionConstants.ExportTypeIdentityMetadataName] = CompositionConstants.PartCreatorTypeIdentity;
            metadata[CompositionConstants.ProductDefinitionMetadataName] = productExportDefinition;

            var exportDefinition = new ExportDefinition(CompositionConstants.PartCreatorContractName, metadata);

            var predicate = partCreatorImportDef.Constraint.Compile();
            Assert.True(partCreatorImportDef.IsConstraintSatisfiedBy(exportDefinition));
            Assert.True(predicate(exportDefinition));
        }

        [Fact]
        public void TryParseConstraint_ConstraintFromCreateConstraintAsConstraintArgument1_CanParse()
        {
            var expectations = Expectations.GetContractNamesWithEmpty();

            foreach (var e in expectations)
            {
                var constraint = ConstraintServices.CreateConstraint((string)e, null, null, CreationPolicy.Any);

                AssertCanParse(constraint, e, new Dictionary<string, Type>());
            }
        }

        [Fact]
        public void TryParseConstraint_ConstraintFromCreateConstraintAsConstraintArgument3_CanParse()
        {
            var contractNames = Expectations.GetContractNames();
            var metadataValues = Expectations.GetRequiredMetadata();

            foreach (var contractName in contractNames)
            {
                foreach (var metadataValue in metadataValues)
                {
                    var constraint = ConstraintServices.CreateConstraint(contractName, null, metadataValue, CreationPolicy.Any);

                    AssertCanParse(constraint, contractName, metadataValue);
                }
            }
        }

        [Fact]
        public void TryParseConstraint_ContractNameOperatorEqualsAsConstraintArgument_CanParse()
        {
            var expectations = new ExpectationCollection<Expression<Func<ExportDefinition, bool>>, string>();
            expectations.Add(item => item.ContractName == "", "");
            expectations.Add(item => item.ContractName == " ", " ");
            expectations.Add(item => item.ContractName == "   ", "   ");
            expectations.Add(item => item.ContractName == "ContractName", "ContractName");
            expectations.Add(item => item.ContractName == "contractName", "contractName");
            expectations.Add(item => item.ContractName == "{ContractName}", "{ContractName}");
            expectations.Add(item => item.ContractName == "{ContractName}Name", "{ContractName}Name");
            expectations.Add(item => item.ContractName == "System.Windows.Forms.Control", "System.Windows.Forms.Control");
            expectations.Add(item => item.ContractName == "{System.Windows.Forms}Control", "{System.Windows.Forms}Control");

            foreach (var e in expectations)
            {
                AssertCanParse(e.Input, e.Output, new Dictionary<string, Type>());
            }
        }

        [Fact]
        public void TryParseConstraint_MetadataContainsKeyAsConstraintArgument_CanParse()
        {
            var expectations = new ExpectationCollection<Expression<Func<ExportDefinition, bool>>, Dictionary<string, Type>>();
            expectations.Add(
                item => typeof(string).IsInstanceOfType(item.Metadata[""]),
                new Dictionary<string, Type> { { "", typeof(string) } });
            expectations.Add(
                item => typeof(string).IsInstanceOfType(item.Metadata["value"]),
                new Dictionary<string, Type> { { "value", typeof(string) } });
            expectations.Add(
                item => typeof(string).IsInstanceOfType(item.Metadata["Value"]),
                new Dictionary<string, Type> { { "Value", typeof(string) } });
            expectations.Add(
                item => typeof(string).IsInstanceOfType(item.Metadata["Value"]) && typeof(int).IsInstanceOfType(item.Metadata["value"]),
                new Dictionary<string, Type> { { "Value", typeof(string) }, { "value", typeof(int) } });
            expectations.Add(
                item => typeof(string).IsInstanceOfType(item.Metadata["Value"]) && typeof(int).IsInstanceOfType(item.Metadata["value"]) && typeof(object).IsInstanceOfType(item.Metadata["Metadata"]),
                new Dictionary<string, Type> { { "Value", typeof(string) }, { "value", typeof(int) }, { "Metadata", typeof(object) } });

            foreach (var e in expectations)
            {
                AssertCanParse(e.Input, (string)null, e.Output);
            }
        }

        [Fact]
        public void TryParseConstraint_ContractNameOperatorEqualsAndMetadataContainsKeyAsConstraintArgumen_CanParse()
        {
            var expectations = new ExpectationCollection<Expression<Func<ExportDefinition, bool>>, KeyValuePair<string, Type>[]>();
            expectations.Add(
                item => item.ContractName == "ContractName" && typeof(string).IsInstanceOfType(item.Metadata[""]),
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("", typeof(string)) });
            expectations.Add(
                item => item.ContractName == "ContractName" && typeof(string).IsInstanceOfType(item.Metadata["value"]),
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("value", typeof(string)) });
            expectations.Add(
                item => item.ContractName == "ContractName" && typeof(string).IsInstanceOfType(item.Metadata["Value"]),
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Value", typeof(string)) });
            expectations.Add(
                item => item.ContractName == "ContractName" && typeof(string).IsInstanceOfType(item.Metadata["Value"]) && typeof(int).IsInstanceOfType(item.Metadata["value"]),
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Value", typeof(string)), new KeyValuePair<string, Type>("value", typeof(int)) });
            expectations.Add(
                item => item.ContractName == "ContractName" && typeof(string).IsInstanceOfType(item.Metadata["Value"]) && typeof(int).IsInstanceOfType(item.Metadata["value"]) && typeof(object).IsInstanceOfType(item.Metadata["Metadata"]),
                new KeyValuePair<string, Type>[] { new KeyValuePair<string, Type>("Value", typeof(string)), new KeyValuePair<string, Type>("value", typeof(int)), new KeyValuePair<string, Type>("Metadata", typeof(object)) });

            foreach (var e in expectations)
            {
                AssertCanParse(e.Input, "ContractName", e.Output);
            }
        }

        [Fact]
        public void TryParseConstraint_ContractNameReverseOperatorEqualsAsConstraintArgument_CanParse()
        {
            var expectations = new ExpectationCollection<Expression<Func<ExportDefinition, bool>>, string>();
            expectations.Add(item => "" == item.ContractName, "");
            expectations.Add(item => " " == item.ContractName, " ");
            expectations.Add(item => "   " == item.ContractName, "   ");
            expectations.Add(item => "ContractName" == item.ContractName, "ContractName");
            expectations.Add(item => "contractName" == item.ContractName, "contractName");
            expectations.Add(item => "{ContractName}" == item.ContractName, "{ContractName}");
            expectations.Add(item => "{ContractName}Name" == item.ContractName, "{ContractName}Name");
            expectations.Add(item => "System.Windows.Forms.Control" == item.ContractName, "System.Windows.Forms.Control");
            expectations.Add(item => "{System.Windows.Forms}Control" == item.ContractName, "{System.Windows.Forms}Control");

            foreach (var e in expectations)
            {
                AssertCanParse(e.Input, e.Output, new Dictionary<string, Type>());
            }
        }

        private static void AssertCanParse(Expression<Func<ExportDefinition, bool>> constraint, string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
        {
            Assert.NotNull(constraint);

            string contractNameResult = null;
            IEnumerable<KeyValuePair<string, Type>> requiredMetadataResult = null;
            bool success = ContraintParser.TryParseConstraint(constraint, out contractNameResult, out requiredMetadataResult);

            Assert.True(success);
            Assert.Equal(contractName, contractNameResult);
            EnumerableAssert.AreEqual(requiredMetadata, requiredMetadataResult);
        }

        private static void AssertCanNotParse(Expression<Func<ExportDefinition, bool>> constraint)
        {
            Assert.NotNull(constraint);

            string contractNameResult;
            IEnumerable<KeyValuePair<string, Type>> requiredMetadataResult;

            var success = ContraintParser.TryParseConstraint(constraint, out contractNameResult, out requiredMetadataResult);
            Assert.False(success);
            Assert.Null(contractNameResult);
            Assert.Null(requiredMetadataResult);
        }
    }
}
