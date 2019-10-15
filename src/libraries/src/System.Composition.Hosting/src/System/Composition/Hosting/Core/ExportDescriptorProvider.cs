// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// A contributor to the composition.
    /// </summary>
    /// <remarks>Instances of this class are not required to be safe for concurrent access by
    /// multiple threads.</remarks>
    public abstract class ExportDescriptorProvider
    {
        /// <summary>
        /// Constant value provided so that subclasses can avoid creating additional duplicate values.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly IEnumerable<ExportDescriptorPromise> NoExportDescriptors = Enumerable.Empty<ExportDescriptorPromise>();

        /// <summary>
        /// Constant value provided so that subclasses can avoid creating additional duplicate values.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly IDictionary<string, object> NoMetadata = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

        /// <summary>
        /// Constant value provided so that subclasses can avoid creating additional duplicate values.
        /// </summary>
        [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Func<IEnumerable<CompositionDependency>> NoDependencies = () => Enumerable.Empty<CompositionDependency>();

        /// <summary>
        /// Promise export descriptors for the specified export key.
        /// </summary>
        /// <param name="contract">The export key required by another component.</param>
        /// <param name="descriptorAccessor">Accesses the other export descriptors present in the composition.</param>
        /// <returns>Promises for new export descriptors.</returns>
        /// <remarks>
        /// A provider will only be queried once for each unique export key.
        /// The descriptor accessor can only be queried immediately if the descriptor being promised is an adapter, such as
        /// <see cref="Lazy{T}"/>; otherwise, dependencies should only be queried within execution of the function provided
        /// to the <see cref="ExportDescriptorPromise"/>. The actual descriptors provided should not close over or reference any
        /// aspect of the dependency/promise structure, as this should be able to be GC'ed.
        /// </remarks>
        public abstract IEnumerable<ExportDescriptorPromise> GetExportDescriptors(
            CompositionContract contract,
            DependencyAccessor descriptorAccessor);
    }
}
