// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Composition.Hosting;
using System.Composition.Hosting.Core;
using System.Composition.TypedParts;
using System.Composition.TypedParts.Discovery;
using System.Composition.TypedParts.Util;
using System.Diagnostics;
using System.Reflection;

namespace System.Composition.Debugging
{
    internal sealed class ContainerConfigurationDebuggerProxy
    {
        private readonly ContainerConfiguration _configuration;
        private DiscoveredPart[] _discoveredParts;
        private Type[] _ignoredTypes;

        public ContainerConfigurationDebuggerProxy(ContainerConfiguration configuration)
        {
            _configuration = configuration;
        }

        [DebuggerDisplay("Added Providers")]
        public ExportDescriptorProvider[] AddedExportDescriptorProviders
        {
            get { return _configuration.DebugGetAddedExportDescriptorProviders(); }
        }

        [DebuggerDisplay("Discovered Parts")]
        public DiscoveredPart[] DiscoveredParts
        {
            get
            {
                InitDiscovery();
                return _discoveredParts;
            }
        }

        [DebuggerDisplay("Ignored Types")]
        public Type[] IgnoredTypes
        {
            get
            {
                InitDiscovery();
                return _ignoredTypes;
            }
        }

        private void InitDiscovery()
        {
            if (_discoveredParts != null)
                return;

            var types = _configuration.DebugGetRegisteredTypes();
            var defaultAttributeContext = _configuration.DebugGetDefaultAttributeContext() ?? new DirectAttributeContext();
            var discovered = new List<DiscoveredPart>();
            var ignored = new List<Type>();

            foreach (var typeSet in types)
            {
                var ac = typeSet.Item2 ?? defaultAttributeContext;
                var activationFeatures = TypedPartExportDescriptorProvider.DebugGetActivationFeatures(ac);
                var inspector = new TypeInspector(ac, activationFeatures);

                foreach (var type in typeSet.Item1)
                {
                    DiscoveredPart part;
                    if (inspector.InspectTypeForPart(type.GetTypeInfo(), out part))
                        discovered.Add(part);
                    else
                        ignored.Add(type);
                }
            }

            _discoveredParts = discovered.ToArray();
            _ignoredTypes = ignored.ToArray();
        }
    }
}
