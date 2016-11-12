// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ConstrainedExecution;

namespace System.Threading
{
    public sealed partial class Thread : CriticalFinalizerObject
    {
        public ApartmentState GetApartmentState()
        {
            return _runtimeThread.GetApartmentState();
        }

        private static Exception GetApartmentStateChangeFailedException()
        {
            return new InvalidOperationException(SR.Thread_ApartmentState_ChangeFailed);
        }

        private bool TrySetApartmentStateUnchecked(ApartmentState state)
        {
            return _runtimeThread.TrySetApartmentState(state);
        }
    }
}
