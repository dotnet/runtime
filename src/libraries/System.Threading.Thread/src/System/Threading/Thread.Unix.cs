// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.ConstrainedExecution;

namespace System.Threading
{
    public sealed partial class Thread
    {
        public ApartmentState GetApartmentState()
        {
            return ApartmentState.Unknown;
        }

        private static Exception GetApartmentStateChangeFailedException()
        {
            return new PlatformNotSupportedException();
        }

        private bool TrySetApartmentStateUnchecked(ApartmentState state)
        {
            return state == GetApartmentState();
        }
    }
}
