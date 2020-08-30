// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.WindowsRuntime
{
    public static class WindowsRuntimeMarshal
    {
        public static void AddEventHandler<T>(Func<T, EventRegistrationToken> addMethod,
                                              Action<EventRegistrationToken> removeMethod,
                                              T handler) => throw new PlatformNotSupportedException();

        public static void RemoveEventHandler<T>(Action<EventRegistrationToken> removeMethod, T handler) => throw new PlatformNotSupportedException();

        public static void RemoveAllEventHandlers(Action<EventRegistrationToken> removeMethod) => throw new PlatformNotSupportedException();

        public static object GetUniqueObjectForIUnknownWithoutUnboxing(IntPtr unknown) => throw new PlatformNotSupportedException();
    }
}
