// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition.Primitives;
using System.Globalization;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionMemberImportDefinition : ReflectionImportDefinition
    {
        private LazyMemberInfo _importingLazyMember;

        public ReflectionMemberImportDefinition(
            LazyMemberInfo importingLazyMember,
            string contractName,
            string? requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>>? requiredMetadata,
            ImportCardinality cardinality,
            bool isRecomposable,
            bool isPrerequisite,
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object?> metadata,
            ICompositionElement? origin)
            : base(contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, isPrerequisite, requiredCreationPolicy, metadata, origin)
        {
            if (contractName == null)
            {
                throw new ArgumentNullException(nameof(contractName));
            }

            _importingLazyMember = importingLazyMember;
        }

        public override ImportingItem ToImportingItem()
        {
            ReflectionWritableMember member = ImportingLazyMember.ToReflectionWriteableMember();
            return new ImportingMember(this, member, new ImportType(member.ReturnType, Cardinality));
        }

        public LazyMemberInfo ImportingLazyMember
        {
            get { return _importingLazyMember; }
        }

        protected override string GetDisplayName() =>
            $"{ImportingLazyMember.ToReflectionMember().GetDisplayName()} (ContractName=\"{ContractName}\")";    // NOLOC
    }
}
