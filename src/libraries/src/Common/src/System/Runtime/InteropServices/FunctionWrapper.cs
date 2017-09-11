// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Utility class which lazily loads function pointers and throws an EntryPointNotFoundException at
    /// usage-time if the function pointer can not be loaded.
    /// </summary>
    /// <typeparam name="T">The type of the function to wrap.</typeparam>
    internal class FunctionWrapper<T> where T : class
    {
        private Lazy<T> _lazyDelegate;

        public FunctionWrapper(Lazy<T> lazyDelegate)
        {
            _lazyDelegate = lazyDelegate;
        }

        public T Delegate => _lazyDelegate.Value ?? throw new EntryPointNotFoundException();
    }

    internal static partial class FunctionWrapper
    {
        public static FunctionWrapper<T> Load<T>(IntPtr nativeLibraryHandle, string name) where T : class
        {
            if (nativeLibraryHandle == IntPtr.Zero)
            {
                throw new DllNotFoundException();
            }

            Lazy<T> lazyDelegate = new Lazy<T>(() =>
            {
                IntPtr funcPtr = LoadFunctionPointer(nativeLibraryHandle, name);
                if (funcPtr == IntPtr.Zero)
                {
                    return null;
                }
                else
                {
                    return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
                }
            });

            return new FunctionWrapper<T>(lazyDelegate);
        }
    }
}
