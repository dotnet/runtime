// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;

namespace System.Composition
{
    /// <summary>
    /// Applied to an import for ExportFactory{T}, this attribute marks the
    /// boundary of a sharing scope. The ExportLifetimeContext{T} instances
    /// returned from the factory will be boundaries for sharing of components that are bounded
    /// by the listed boundary names.
    /// </summary>
    /// <example>
    /// [Import, SharingBoundary("HttpRequest")]
    /// public ExportFactory&lt;HttpRequestHandler&gt; HandlerFactory { get; set; }
    /// </example>
    /// <seealso cref="SharedAttribute" />
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
    [MetadataAttribute]
    [CLSCompliant(false)]
    public sealed class SharingBoundaryAttribute : Attribute
    {
        private readonly string[] _sharingBoundaryNames;

        /// <summary>
        /// Construct a <see cref="SharingBoundaryAttribute"/> for the specified boundary names.
        /// </summary>
        /// <param name="sharingBoundaryNames">Boundaries implemented by the created ExportLifetimeContext{T}s.</param>
        public SharingBoundaryAttribute(params string[] sharingBoundaryNames)
        {
            _sharingBoundaryNames = sharingBoundaryNames ?? throw new ArgumentNullException(nameof(sharingBoundaryNames));
        }

        /// <summary>
        /// Boundaries implemented by the created ExportLifetimeContext{T}s.
        /// </summary>
        public ReadOnlyCollection<string> SharingBoundaryNames => new ReadOnlyCollection<string>(_sharingBoundaryNames);
    }
}
