// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.DotNet.RemoteExecutor
{
    /// <summary>
    /// Wraps <see cref="RemoteExecutor"/>, using it only when running on UAP, and otherwise
    /// invoking the action synchronously without any additional sandboxing.
    /// </summary>
    /// <remarks>
    /// This is a workaround for UAP's current behavior for CultureInfo.Current{UI}Culture,
    /// which is process-wide rather than thread-specific.  When that behavior is fixed,
    /// all use of this type should be removable.
    /// </remarks>
    public static class RemoteExecutorForUap
    {
        public static RemoteInvokeHandle Invoke(Action action)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(action);
            }

            action();
            return new RemoteInvokeHandle(null, null);
        }

        public static RemoteInvokeHandle Invoke(Action<string> action, string arg)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(action, arg);
            }

            action(arg);
            return new RemoteInvokeHandle(null, null);
        }

        public static RemoteInvokeHandle Invoke(Action<string, string> action, string arg1, string arg2)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(action, arg1, arg2);
            }

            action(arg1, arg2);
            return new RemoteInvokeHandle(null, null);
        }

        public static RemoteInvokeHandle Invoke(Action<string, string, string> action, string arg1, string arg2, string arg3)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(action, arg1, arg2, arg3);
            }

            action(arg1, arg2, arg3);
            return new RemoteInvokeHandle(null, null);
        }

        public static RemoteInvokeHandle Invoke(Action<string, string, string, string> action, string arg1, string arg2, string arg3, string arg4)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(action, arg1, arg2, arg3, arg4);
            }

            action(arg1, arg2, arg3, arg4);
            return new RemoteInvokeHandle(null, null);
        }

        public static RemoteInvokeHandle Invoke(Func<string, string, string, string, string, int> func, string arg1, string arg2, string arg3, string arg4, string arg5)
        {
            if (PlatformDetection.IsUap)
            {
                return RemoteExecutor.Invoke(func, arg1, arg2, arg3, arg4, arg5);
            }

            func(arg1, arg2, arg3, arg4, arg5); // ignore return value for now, though it should really be marshaled out through the RemoteInvokeHandle
            return new RemoteInvokeHandle(null, null);
        }
    }
}
