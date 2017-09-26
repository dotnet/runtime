// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Utility class which lazily loads function pointers and throws a DllNotFoundException or
    /// an EntryPointNotFoundException at usage-time if the function pointer can not be loaded.
    /// </summary>
    /// <typeparam name="T">The type of the function to wrap.</typeparam>
    internal class FunctionWrapper<T> where T : class
    {
        private Lazy<FunctionLoadResult<T>> _lazyDelegate;

        public FunctionWrapper(Lazy<FunctionLoadResult<T>> lazyDelegate)
        {
            _lazyDelegate = lazyDelegate;
        }

        public T Delegate
        {
            get
            {
                FunctionLoadResult<T> loadResult = _lazyDelegate.Value;
                switch (loadResult.ResultKind)
                {
                    case FunctionLoadResultKind.Success:
                        return loadResult.Delegate;
                    case FunctionLoadResultKind.LibraryNotFound:
                        throw new DllNotFoundException();
                    case FunctionLoadResultKind.FunctionNotFound:
                        throw new EntryPointNotFoundException();
                    default:
                        Debug.Fail("Illegal FunctionLoadResultKind: " + loadResult.ResultKind);
                        return null;
                }
            }
        }
    }

    public enum FunctionLoadResultKind { Success, LibraryNotFound, FunctionNotFound }

    public struct FunctionLoadResult<T>
    {
        public FunctionLoadResultKind ResultKind { get; }
        public T Delegate { get; }
        public FunctionLoadResult(FunctionLoadResultKind kind, T del) { ResultKind = kind; Delegate = del; }
    }

    internal static partial class FunctionWrapper
    {
        public static FunctionWrapper<T> Load<T>(IntPtr nativeLibraryHandle, string name) where T : class
        {
            Lazy<FunctionLoadResult<T>> lazyDelegate = new Lazy<FunctionLoadResult<T>>(() =>
            {
                if (nativeLibraryHandle == IntPtr.Zero)
                {
                    return new FunctionLoadResult<T>(FunctionLoadResultKind.LibraryNotFound, null);
                }

                IntPtr funcPtr = LoadFunctionPointer(nativeLibraryHandle, name);
                if (funcPtr == IntPtr.Zero)
                {
                    return new FunctionLoadResult<T>(FunctionLoadResultKind.FunctionNotFound, null);
                }
                else
                {
                    return new FunctionLoadResult<T>(FunctionLoadResultKind.Success, Marshal.GetDelegateForFunctionPointer<T>(funcPtr));
                }
            });

            return new FunctionWrapper<T>(lazyDelegate);
        }
    }
}
