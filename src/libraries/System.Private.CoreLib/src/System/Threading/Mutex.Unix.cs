// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System.Threading
{
    public sealed partial class Mutex
    {
        [FeatureSwitchDefinition("System.Threading.Mutex.UnsafeDefaultMutexScopeGlobal")]
        private static bool UnsafeDefaultMutexScopeGlobal { get; } =
            AppContext.TryGetSwitch("System.Threading.Mutex.UnsafeDefaultMutexScopeGlobal", out bool enable) ? enable : false;

        private void CreateMutexCore(bool initiallyOwned) => SafeWaitHandle = WaitSubsystem.NewMutex(initiallyOwned);

        private void CreateMutexCore(
            bool initiallyOwned,
            string? name,
            NamedWaitHandleOptionsInternal options,
            out bool createdNew)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = BuildNameForOptions(name, options);

                SafeWaitHandle? safeWaitHandle = WaitSubsystem.CreateNamedMutex(initiallyOwned, name, isUserScope: CalculateUserScope(options), out createdNew);
                if (safeWaitHandle == null)
                {
                    throw new WaitHandleCannotBeOpenedException(SR.Format(SR.Threading_WaitHandleCannotBeOpenedException_InvalidHandle, name));
                }
                SafeWaitHandle = safeWaitHandle;
                return;
            }

            SafeWaitHandle = WaitSubsystem.NewMutex(initiallyOwned);
            createdNew = true;
        }

        private static OpenExistingResult OpenExistingWorker(
            string name,
            NamedWaitHandleOptionsInternal options,
            out Mutex? result)
        {
            ArgumentException.ThrowIfNullOrEmpty(name);

            name = BuildNameForOptions(name, options);

            OpenExistingResult status = WaitSubsystem.OpenNamedMutex(name, isUserScope: CalculateUserScope(options), out SafeWaitHandle? safeWaitHandle);
            result = status == OpenExistingResult.Success ? new Mutex(safeWaitHandle!) : null;
            return status;
        }

        private static string BuildNameForOptions(string name, NamedWaitHandleOptionsInternal options)
        {
            if (options.WasSpecified)
            {
                name = options.GetNameWithSessionPrefix(name);
            }

            if (name.StartsWith(NamedWaitHandleOptionsInternal.CurrentSessionPrefix) &&
                name.Length > NamedWaitHandleOptionsInternal.CurrentSessionPrefix.Length)
            {
                name = name.Substring(NamedWaitHandleOptionsInternal.CurrentSessionPrefix.Length);
            }

            return name;
        }

        private static bool CalculateUserScope(NamedWaitHandleOptionsInternal options)
        {
            return options.WasSpecified ? options.CurrentUserOnly : !UnsafeDefaultMutexScopeGlobal;
        }

        public void ReleaseMutex()
        {
            // The field value is modifiable via the public <see cref="WaitHandle.SafeWaitHandle"/> property, save it locally
            // to ensure that one instance is used in all places in this method
            SafeWaitHandle waitHandle = SafeWaitHandle;
            if (waitHandle.IsInvalid)
            {
                ThrowInvalidHandleException();
            }

            waitHandle.DangerousAddRef();
            try
            {
                WaitSubsystem.ReleaseMutex(waitHandle.DangerousGetHandle());
            }
            finally
            {
                waitHandle.DangerousRelease();
            }
        }
    }
}
