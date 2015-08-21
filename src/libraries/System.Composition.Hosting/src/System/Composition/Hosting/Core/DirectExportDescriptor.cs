// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.Internal;

namespace System.Composition.Hosting.Core
{
    internal class DirectExportDescriptor : ExportDescriptor
    {
        private readonly CompositeActivator _activator;
        private readonly IDictionary<string, object> _metadata;

        public DirectExportDescriptor(CompositeActivator activator, IDictionary<string, object> metadata)
        {
            Requires.NotNull(activator, "activator");
            Requires.NotNull(metadata, "metadata");

            _activator = activator;
            _metadata = metadata;
        }

        public override CompositeActivator Activator { get { return _activator; } }

        public override IDictionary<string, object> Metadata { get { return _metadata; } }
    }
}
