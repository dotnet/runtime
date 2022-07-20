// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Composition.Hosting.Core
{
    internal sealed class DirectExportDescriptor : ExportDescriptor
    {
        private readonly CompositeActivator _activator;
        private readonly IDictionary<string, object> _metadata;

        public DirectExportDescriptor(CompositeActivator activator, IDictionary<string, object> metadata)
        {
            if (activator is null)
            {
                throw new ArgumentNullException(nameof(activator));
            }
            if (metadata is null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            _activator = activator;
            _metadata = metadata;
        }

        public override CompositeActivator Activator { get { return _activator; } }

        public override IDictionary<string, object> Metadata { get { return _metadata; } }
    }
}
