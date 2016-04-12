// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Internal;

namespace System.Composition.Hosting.Core
{
    /// <summary>
    /// Represents a single logical graph-building operation.
    /// </summary>
    /// <remarks>Instances of this class are not safe for access by multiple threads.</remarks>
    public sealed class CompositionOperation : IDisposable
    {
        private List<Action> _nonPrerequisiteActions;
        private List<Action> _postCompositionActions;
        private object _sharingLock;

        // Construct using Run() method.
        private CompositionOperation() { }

        /// <summary>
        /// Execute a new composition operation starting within the specified lifetime
        /// context, for the specified activator.
        /// </summary>
        /// <param name="outermostLifetimeContext">Context in which to begin the operation (the operation can flow
        /// to the parents of the context if required).</param>
        /// <param name="compositionRootActivator">Activator that will drive the operation.</param>
        /// <returns>The composed object graph.</returns>
        public static object Run(LifetimeContext outermostLifetimeContext, CompositeActivator compositionRootActivator)
        {
            Requires.NotNull(outermostLifetimeContext, nameof(outermostLifetimeContext));
            Requires.NotNull(compositionRootActivator, nameof(compositionRootActivator));

            using (var operation = new CompositionOperation())
            {
                var result = compositionRootActivator(outermostLifetimeContext, operation);
                operation.Complete();
                return result;
            }
        }

        /// <summary>
        /// Called during the activation process to specify an action that can run after all
        /// prerequisite part dependencies have been satisfied.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void AddNonPrerequisiteAction(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (_nonPrerequisiteActions == null)
                _nonPrerequisiteActions = new List<Action>();

            _nonPrerequisiteActions.Add(action);
        }

        /// <summary>
        /// Called during the activation process to specify an action that must run only after
        /// all composition has completed. See OnImportsSatisfiedAttribute.
        /// </summary>
        /// <param name="action">Action to run.</param>
        public void AddPostCompositionAction(Action action)
        {
            Requires.NotNull(action, nameof(action));

            if (_postCompositionActions == null)
                _postCompositionActions = new List<Action>();

            _postCompositionActions.Add(action);
        }

        internal void EnterSharingLock(object sharingLock)
        {
            Assumes.NotNull(sharingLock, "Sharing lock is required");

            if (_sharingLock == null)
            {
                _sharingLock = sharingLock;
                Monitor.Enter(sharingLock);
            }

            Assumes.IsTrue(_sharingLock == sharingLock, "Sharing lock already taken in this operation.");
        }

        private void Complete()
        {
            while (_nonPrerequisiteActions != null)
                RunAndClearActions();

            if (_postCompositionActions != null)
            {
                foreach (var action in _postCompositionActions)
                    action();

                _postCompositionActions = null;
            }
        }

        private void RunAndClearActions()
        {
            var currentActions = _nonPrerequisiteActions;
            _nonPrerequisiteActions = null;

            foreach (var action in currentActions)
                action();
        }

        /// <summary>
        /// Release locks held during the operation.
        /// </summary>
        public void Dispose()
        {
            if (_sharingLock != null)
                Monitor.Exit(_sharingLock);
        }
    }
}
