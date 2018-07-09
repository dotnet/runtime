// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition.Hosting.Util;
using System.Diagnostics;
using System.Threading;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// Represents a node in the lifetime tree. A <see cref="LifetimeContext"/> is the unit of
    /// sharing for shared parts, controls the disposal of bound parts, and can be used to retrieve
    /// instances either as part of an existing <see cref="CompositionOperation"/> or as the basis of a new
    /// composition operation. An individual lifetime context can be marked to contain parts that are
    /// constrained by particular sharing boundaries.
    /// </summary>
    /// <remarks>
    /// Contains two pieces of _independently protected_ shared state. Shared part instances is
    /// lock-free-readable and does not result in issues if added to during disposal. It is protected
    /// by being locked itself. Activation logic is unavoidably called under this lock.
    /// Bound part instances is always protected, by locking [this], and should never be written to
    /// after disposal and so is set to null under a lock in Dispose(). If it were allowed it would result in
    /// disposable parts not being released. Dispose methods on parts are called outside the lock.
    /// </remarks>
    /// <seealso cref="Export{T}"/>
    public sealed class LifetimeContext : CompositionContext, IDisposable
    {
        private readonly LifetimeContext _root;
        private readonly LifetimeContext _parent;

        // Protects the two members holding shared instances
        private readonly object _sharingLock = new object();
        private SmallSparseInitonlyArray _sharedPartInstances, _instancesUndergoingInitialization;

        // Protects the member holding bound instances.
        private readonly object _boundPartLock = new object();
        private List<IDisposable> _boundPartInstances = new List<IDisposable>(0);

        private readonly string[] _sharingBoundaries;
        private readonly ExportDescriptorRegistry _partRegistry;

        private static int s_nextSharingId = -1;

        /// <summary>
        /// Generates an identifier that can be used to locate shared part instances.
        /// </summary>
        /// <returns>A new unique identifier.</returns>
        public static int AllocateSharingId()
        {
            return Interlocked.Increment(ref s_nextSharingId);
        }

        internal LifetimeContext(ExportDescriptorRegistry partRegistry, string[] sharingBoundaries)
        {
            _root = this;
            _sharingBoundaries = sharingBoundaries;
            _partRegistry = partRegistry;
        }

        internal LifetimeContext(LifetimeContext parent, string[] sharingBoundaries)
        {
            _parent = parent;
            _root = parent._root;
            _sharingBoundaries = sharingBoundaries;
            _partRegistry = parent._partRegistry;
        }

        /// <summary>
        /// Find the broadest lifetime context within all of the specified sharing boundaries.
        /// </summary>
        /// <param name="sharingBoundary">The sharing boundary to find a lifetime context within.</param>
        /// <returns>The broadest lifetime context within all of the specified sharing boundaries.</returns>
        /// <remarks>Currently, the root cannot be a boundary.</remarks>
        public LifetimeContext FindContextWithin(string sharingBoundary)
        {
            if (sharingBoundary == null)
                return _root;

            var toCheck = this;
            while (toCheck != null)
            {
                foreach (var implemented in toCheck._sharingBoundaries)
                {
                    if (implemented == sharingBoundary)
                        return toCheck;
                }

                toCheck = toCheck._parent;
            }

            // To generate acceptable error messages here we're going to need to pass in a description
            // of the component, or otherwise find a way to get one.
            string message = SR.Format(SR.Component_NotCreatableOutsideSharingBoundary, sharingBoundary);
            var ex = new CompositionFailedException(message);
            Debug.WriteLine(SR.Diagnostic_ThrowingException, ex.ToString());
            throw ex;
        }

        /// <summary>
        /// Release the lifetime context and any disposable part instances
        /// that are bound to it.
        /// </summary>
        public void Dispose()
        {
            IEnumerable<IDisposable> toDispose = null;
            lock (_boundPartLock)
            {
                if (_boundPartInstances != null)
                {
                    toDispose = _boundPartInstances;
                    _boundPartInstances = null;
                }
            }

            if (toDispose != null)
            {
                foreach (var instance in toDispose)
                    instance.Dispose();
            }
        }

        /// <summary>
        /// Bind the lifetime of a disposable part to the current
        /// lifetime context.
        /// </summary>
        /// <param name="instance">The disposable part to bind.</param>
        public void AddBoundInstance(IDisposable instance)
        {
            lock (_boundPartLock)
            {
                if (_boundPartInstances == null)
                    throw new ObjectDisposedException(ToString());

                _boundPartInstances.Add(instance);
            }
        }

        /// <summary>
        /// Either retrieve an existing part instance with the specified sharing id, or
        /// create and share a new part instance using <paramref name="creator"/> within
        /// <paramref name="operation"/>.
        /// </summary>
        /// <param name="sharingId">Sharing id for the part in question.</param>
        /// <param name="operation">Operation in which to activate a new part instance if necessary.</param>
        /// <param name="creator">Activator that can activate a new part instance if necessary.</param>
        /// <returns>The part instance corresponding to <paramref name="sharingId"/> within this lifetime context.</returns>
        /// <remarks>This method is lock-free if the part instance already exists. If the part instance must be created,
        /// a lock will be taken that will serialize other writes via this method (concurrent reads will continue to
        /// be safe and lock-free). It is important that the composition, and thus lock acquisition, is strictly
        /// leaf-to-root in the lifetime tree.</remarks>
        public object GetOrCreate(int sharingId, CompositionOperation operation, CompositeActivator creator)
        {
            object result;
            if (_sharedPartInstances != null && _sharedPartInstances.TryGetValue(sharingId, out result))
                return result;

            // Remains locked for the rest of the operation.
            operation.EnterSharingLock(_sharingLock);

            if (_sharedPartInstances == null)
            {
                _sharedPartInstances = new SmallSparseInitonlyArray();
                _instancesUndergoingInitialization = new SmallSparseInitonlyArray();
            }
            else if (_sharedPartInstances.TryGetValue(sharingId, out result))
            {
                return result;
            }

            // Already being initialized _on the same thread_.
            if (_instancesUndergoingInitialization.TryGetValue(sharingId, out result))
                return result;

            result = creator(this, operation);

            _instancesUndergoingInitialization.Add(sharingId, result);

            operation.AddPostCompositionAction(() =>
            {
                _sharedPartInstances.Add(sharingId, result);
            });

            return result;
        }

        /// <summary>
        /// Retrieve the single <paramref name="contract"/> instance from the
        /// <see cref="CompositionContext"/>.
        /// </summary>
        /// <param name="contract">The contract to retrieve.</param>
        /// <returns>An instance of the export.</returns>
        /// <param name="export">The export if available, otherwise, null.</param>
        /// <exception cref="CompositionFailedException" />
        public override bool TryGetExport(CompositionContract contract, out object export)
        {
            ExportDescriptor defaultForExport;
            if (!_partRegistry.TryGetSingleForExport(contract, out defaultForExport))
            {
                export = null;
                return false;
            }

            export = CompositionOperation.Run(this, defaultForExport.Activator);
            return true;
        }

        /// <summary>
        /// Describes this lifetime context.
        /// </summary>
        /// <returns>A string description.</returns>
        public override string ToString()
        {
            if (_parent == null)
                return "Root Lifetime Context";

            if (_sharingBoundaries.Length == 0)
                return "Non-sharing Lifetime Context";

            return "Boundary for " + string.Join(", ", _sharingBoundaries);
        }
    }
}
