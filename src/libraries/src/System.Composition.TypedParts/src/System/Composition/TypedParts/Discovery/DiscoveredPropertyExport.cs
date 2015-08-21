// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Composition.Runtime;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    internal class DiscoveredPropertyExport : DiscoveredExport
    {
        private static readonly MethodInfo s_activatorInvoke = typeof(CompositeActivator).GetRuntimeMethod("Invoke", new[] { typeof(LifetimeContext), typeof(CompositionOperation) });

        private readonly PropertyInfo _property;

        public DiscoveredPropertyExport(CompositionContract contract, IDictionary<string, object> metadata, PropertyInfo property)
            : base(contract, metadata)
        {
            _property = property;
        }

        protected override ExportDescriptor GetExportDescriptor(CompositeActivator partActivator)
        {
            var args = new[] { Expression.Parameter(typeof(LifetimeContext)), Expression.Parameter(typeof(CompositionOperation)) };

            var activator = Expression.Lambda<CompositeActivator>(
                Expression.Property(
                    Expression.Convert(Expression.Call(Expression.Constant(partActivator), s_activatorInvoke, args), _property.DeclaringType),
                    _property),
                args);

            return ExportDescriptor.Create(activator.Compile(), Metadata);
        }

        public override DiscoveredExport CloseGenericExport(TypeInfo closedPartType, Type[] genericArguments)
        {
            var closedContractType = Contract.ContractType.MakeGenericType(genericArguments);
            var newContract = Contract.ChangeType(closedContractType);
            var property = closedPartType.AsType().GetRuntimeProperty(_property.Name);
            return new DiscoveredPropertyExport(newContract, Metadata, property);
        }
    }
}
