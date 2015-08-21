// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition.TypedParts.Discovery;
using System.Linq;
using System.Reflection;

namespace System.Composition.Debugging
{
    internal class DiscoveredPartDebuggerProxy
    {
        private readonly DiscoveredPart _discoveredPart;

        public DiscoveredPartDebuggerProxy(DiscoveredPart discoveredPart)
        {
            _discoveredPart = discoveredPart;
        }

        public Type PartType
        {
            get { return _discoveredPart.PartType.AsType(); }
        }

        public DiscoveredExport[] Exports
        {
            get { return _discoveredPart.DiscoveredExports.ToArray(); }
        }

        public IDictionary<string, object> PartMetadata
        {
            get { return _discoveredPart.GetPartMetadata(PartType.GetTypeInfo()); }
        }
    }
}
