// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    internal sealed class DiscoveredInstanceExport : DiscoveredExport
    {
        public DiscoveredInstanceExport(CompositionContract contract, IDictionary<string, object> metadata)
            : base(contract, metadata)
        {
        }

        protected override ExportDescriptor GetExportDescriptor(CompositeActivator partActivator)
        {
            return ExportDescriptor.Create(partActivator, Metadata);
        }

        public override DiscoveredExport CloseGenericExport(TypeInfo closedPartType, Type[] genericArguments)
        {
            var closedContractType = Contract.ContractType.MakeGenericType(genericArguments);
            var newContract = Contract.ChangeType(closedContractType);
            return new DiscoveredInstanceExport(newContract, Metadata);
        }
    }
}
