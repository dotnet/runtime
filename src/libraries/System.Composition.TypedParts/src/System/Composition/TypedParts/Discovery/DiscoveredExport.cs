// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting.Core;
using System.Diagnostics;
using System.Reflection;

namespace System.Composition.TypedParts.Discovery
{
    [DebuggerDisplay("{Contract}")]
    internal abstract class DiscoveredExport
    {
        private readonly CompositionContract _exportKey;
        private readonly IDictionary<string, object> _metadata;
        private DiscoveredPart _part;

        public DiscoveredExport(CompositionContract exportKey, IDictionary<string, object> metadata)
        {
            _exportKey = exportKey;
            _metadata = metadata;
        }

        public CompositionContract Contract { get { return _exportKey; } }

        public IDictionary<string, object> Metadata { get { return _metadata; } }

        public DiscoveredPart Part { get { return _part; } set { _part = value; } }

        public ExportDescriptorPromise GetExportDescriptorPromise(
            CompositionContract contract,
            DependencyAccessor definitionAccessor)
        {
            return new ExportDescriptorPromise(
               contract,
               Part.PartType.Name,
               Part.IsShared,
               () => Part.GetDependencies(definitionAccessor),
               deps =>
               {
                   var activator = Part.GetActivator(definitionAccessor, deps);
                   return GetExportDescriptor(activator);
               });
        }

        protected abstract ExportDescriptor GetExportDescriptor(CompositeActivator partActivator);

        public abstract DiscoveredExport CloseGenericExport(TypeInfo closedPartType, Type[] genericArguments);
    }
}
