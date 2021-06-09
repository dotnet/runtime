// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Linq.Expressions;
using System.Text;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal sealed class PartCreatorMemberImportDefinition : ReflectionMemberImportDefinition, IPartCreatorImportDefinition
    {
        private readonly ContractBasedImportDefinition _productImportDefinition;

        public PartCreatorMemberImportDefinition(
            LazyMemberInfo importingLazyMember,
            ICompositionElement? origin,
            ContractBasedImportDefinition productImportDefinition)
            : base(importingLazyMember, CompositionConstants.PartCreatorContractName, CompositionConstants.PartCreatorTypeIdentity,
                productImportDefinition.RequiredMetadata, productImportDefinition.Cardinality, productImportDefinition.IsRecomposable, false, productImportDefinition.RequiredCreationPolicy, MetadataServices.EmptyMetadata, origin)
        {
            if (productImportDefinition == null)
            {
                throw new ArgumentNullException(nameof(productImportDefinition));
            }
            _productImportDefinition = productImportDefinition;
        }

        public ContractBasedImportDefinition ProductImportDefinition { get { return _productImportDefinition; } }
        public override bool IsConstraintSatisfiedBy(ExportDefinition exportDefinition)
        {
            if (!base.IsConstraintSatisfiedBy(exportDefinition))
            {
                return false;
            }

            return PartCreatorExportDefinition.IsProductConstraintSatisfiedBy(_productImportDefinition, exportDefinition);
        }

        public override Expression<Func<ExportDefinition, bool>> Constraint
        {
            get
            {
                return ConstraintServices.CreatePartCreatorConstraint(base.Constraint, _productImportDefinition);
            }
        }

        public override string ToString()
        {
            return "\n\tExportFactory of: " + ProductImportDefinition.ToString();
        }
    }
}
