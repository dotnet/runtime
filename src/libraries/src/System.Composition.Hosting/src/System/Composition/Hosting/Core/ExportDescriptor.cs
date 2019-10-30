// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// Describes an export of a part known to the composition engine. This is the only runtime
    /// overhead that is maintained per-part; all other part-specific information must be discarded once
    /// its export descriptors have been retrieved.
    /// </summary>
    public abstract class ExportDescriptor
    {
        /// <summary>
        /// The activator used to retrieve instances of the export.
        /// </summary>
        public abstract CompositeActivator Activator { get; }

        /// <summary>
        /// The Export Metadata associated with the export.
        /// </summary>
        public abstract IDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Construct an <see cref="ExportDescriptor"/>.
        /// </summary>
        /// <param name="activator">The activator used to retrieve instances of the export.</param>
        /// <param name="metadata">The Export Metadata associated with the export.</param>
        /// <returns>The export descriptor.</returns>
        public static ExportDescriptor Create(CompositeActivator activator, IDictionary<string, object> metadata)
        {
            return new DirectExportDescriptor(activator, metadata);
        }
    }
}
