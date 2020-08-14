// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public sealed class EventRegistrationTokenTable<T> where T : class
    {
        public EventRegistrationTokenTable() => throw new PlatformNotSupportedException();

        public T? InvocationList
        {
            get => throw new PlatformNotSupportedException();
            set => throw new PlatformNotSupportedException();
        }

        public EventRegistrationToken AddEventHandler(T? handler) => throw new PlatformNotSupportedException();

        public bool RemoveEventHandler(EventRegistrationToken token, [NotNullWhen(true)] out T? handler) => throw new PlatformNotSupportedException();

        public void RemoveEventHandler(EventRegistrationToken token) => throw new PlatformNotSupportedException();

        public void RemoveEventHandler(T? handler) => throw new PlatformNotSupportedException();

        public static EventRegistrationTokenTable<T> GetOrCreateEventRegistrationTokenTable(ref EventRegistrationTokenTable<T>? refEventTable) => throw new PlatformNotSupportedException();
    }
}
