// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// Represents an export descriptor that an available part can provide.
    /// </summary>
    /// <remarks>This type is central to the cycle-checking, adaptation and 
    /// compilation features of the container.</remarks>
    public class ExportDescriptorPromise
    {
        private readonly string _origin;
        private readonly bool _isShared;
        private readonly Lazy<ReadOnlyCollection<CompositionDependency>> _dependencies;
        private readonly Lazy<ExportDescriptor> _descriptor;
        private readonly CompositionContract _contract;

        private bool _creating;

        /// <summary>
        /// Create a promise for an export descriptor.
        /// </summary>
        /// <param name="origin">A description of where the export is being provided from (e.g. the part type).
        /// Used to provide friendly errors.</param>
        /// <param name="isShared">True if the export is shared within some context, otherwise false. Used in cycle
        /// checking.</param>
        /// <param name="dependencies">A function providing dependencies required in order to fulfill the promise.</param>
        /// <param name="getDescriptor">A function providing the promise.</param>
        /// <param name="contract">The contract fulfilled by this promise.</param>
        /// <seealso cref="ExportDescriptorProvider"/>.
        public ExportDescriptorPromise(
            CompositionContract contract,
            string origin,
            bool isShared,
            Func<IEnumerable<CompositionDependency>> dependencies,
            Func<IEnumerable<CompositionDependency>, ExportDescriptor> getDescriptor)
        {
            _contract = contract;
            _origin = origin;
            _isShared = isShared;
            _dependencies = new Lazy<ReadOnlyCollection<CompositionDependency>>(() => new ReadOnlyCollection<CompositionDependency>(dependencies().ToList()), false);
            _descriptor = new Lazy<ExportDescriptor>(() => getDescriptor(_dependencies.Value), false);
        }

        /// <summary>
        /// A description of where the export is being provided from (e.g. the part type).
        /// Used to provide friendly errors.
        /// </summary>
        public string Origin { get { return _origin; } }

        /// <summary>
        /// True if the export is shared within some context, otherwise false. Used in cycle
        /// checking.
        /// </summary>
        public bool IsShared { get { return _isShared; } }

        /// <summary>
        /// The dependencies required in order to fulfill the promise.
        /// </summary>
        public ReadOnlyCollection<CompositionDependency> Dependencies { get { return _dependencies.Value; } }

        /// <summary>
        /// The contract fulfilled by this promise.
        /// </summary>
        public CompositionContract Contract { get { return _contract; } }

        /// <summary>
        /// Retrieve the promised export descriptor.
        /// </summary>
        /// <returns>The export descriptor.</returns>
        public ExportDescriptor GetDescriptor()
        {
            if (_creating && !_descriptor.IsValueCreated)
                return new CycleBreakingExportDescriptor(_descriptor);

            _creating = true;
            try
            {
                ExportDescriptor relay = _descriptor.Value;
                if(relay == null)
                {
                    throw new ArgumentNullException("descriptor");
                }
                return relay;
            }
            finally
            {
                _creating = false;
            }
        }

        /// <summary>
        /// Describes the promise.
        /// </summary>
        /// <returns>A description of the promise.</returns>
        public override string ToString()
        {
            return SR.Format(SR.ExportDescriptor_ToStringFormat, Contract, Origin);
        }
    }
}
