// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition.ReflectionModel;
using System.Globalization;
using System.Reflection;
using Microsoft.Internal;
using Microsoft.Internal.Collections;
using System.ComponentModel.Composition.Primitives;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ReflectionMemberImportDefinition : ReflectionImportDefinition
    {
        private LazyMemberInfo _importingLazyMember;

        public ReflectionMemberImportDefinition(
            LazyMemberInfo importingLazyMember,
            string contractName, 
            string requiredTypeIdentity,
            IEnumerable<KeyValuePair<string, Type>> requiredMetadata,
            ImportCardinality cardinality, 
            bool isRecomposable, 
            bool isPrerequisite,
            CreationPolicy requiredCreationPolicy,
            IDictionary<string, object> metadata,
            ICompositionElement origin) 
            : base(contractName, requiredTypeIdentity, requiredMetadata, cardinality, isRecomposable, isPrerequisite, requiredCreationPolicy, metadata, origin)
        {
            Assumes.NotNull(contractName);

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

        protected override string GetDisplayName()
        {
            return string.Format(
                CultureInfo.CurrentCulture,
                "{0} (ContractName=\"{1}\")",    // NOLOC
                ImportingLazyMember.ToReflectionMember().GetDisplayName(),
                ContractName);
        }
    }
}
