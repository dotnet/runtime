// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading
{
    public class HostExecutionContextManager
    {
        /// <summary>
        /// Normally, the current <see cref="HostExecutionContext"/> would be stored on the <see cref="ExecutionContext"/>.
        /// Since this feature is not fully hooked up, this class just imitates the behavior of the desktop framework, while
        /// separating itself from the <see cref="ExecutionContext"/> to minimize unnecessary additions there.
        /// </summary>
        [ThreadStatic]
        private static HostExecutionContext? t_currentContext;

        public virtual HostExecutionContext? Capture()
        {
            // Not hosted, so always capture null
            return null;
        }

        public virtual object SetHostExecutionContext(HostExecutionContext hostExecutionContext)
        {
            if (hostExecutionContext == null)
            {
                throw new InvalidOperationException(SR.HostExecutionContextManager_InvalidOperation_NotNewCaptureContext);
            }

            var switcher = new HostExecutionContextSwitcher(hostExecutionContext);
            t_currentContext = hostExecutionContext;
            return switcher;
        }

        public virtual void Revert(object previousState)
        {
            var switcher = previousState as HostExecutionContextSwitcher;
            if (switcher == null)
            {
                throw new InvalidOperationException(
                    SR.HostExecutionContextManager_InvalidOperation_CannotOverrideSetWithoutRevert);
            }

            if (t_currentContext != switcher._currentContext || switcher._asyncLocal == null || !switcher._asyncLocal.Value)
            {
                throw new InvalidOperationException(
                    SR.HostExecutionContextManager_InvalidOperation_CannotUseSwitcherOtherThread);
            }
            switcher._asyncLocal = null; // cannot be reused

            // Revert always reverts to a null host execution context when not hosted
            t_currentContext = null;
        }

        private sealed class HostExecutionContextSwitcher
        {
            public readonly HostExecutionContext _currentContext;
            public AsyncLocal<bool>? _asyncLocal;

            public HostExecutionContextSwitcher(HostExecutionContext currentContext)
            {
                _currentContext = currentContext;

                // Tie this instance with the current execution context for Revert validation (it must fail if an incompatible
                // execution context is applied to the thread)
                _asyncLocal = new AsyncLocal<bool>();
                _asyncLocal.Value = true;
            }
        }
    }
}
