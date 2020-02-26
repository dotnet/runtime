// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    // An event registration token table stores mappings from delegates to event tokens, in order to support
    // sourcing WinRT style events from managed code.
    public sealed class EventRegistrationTokenTable<T> where T : class
    {
        public EventRegistrationTokenTable() => throw new PlatformNotSupportedException();

        // The InvocationList property provides access to a delegate which will invoke every registered event handler
        // in this table.  If the property is set, the new value will replace any existing token registrations.
        public T? InvocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public EventRegistrationToken AddEventHandler(T? handler) => throw new PlatformNotSupportedException();
        // Remove the event handler from the table and
        // Get the delegate associated with an event registration token if it exists
        // If the event registration token is not registered, returns false
        public bool RemoveEventHandler(EventRegistrationToken token, [NotNullWhen(true)] out T? handler) => throw new PlatformNotSupportedException();

        public void RemoveEventHandler(EventRegistrationToken token) => throw new PlatformNotSupportedException();

        public void RemoveEventHandler(T? handler) => throw new PlatformNotSupportedException();

        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T>? refEventTable) => throw new PlatformNotSupportedException();
    }
}
