// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial class JSObject
    {
        internal nint JSHandle;
        internal JSProxyContext ProxyContext;

        public SynchronizationContext SynchronizationContext
        {
            get
            {
#if FEATURE_WASM_MANAGED_THREADS
                return ProxyContext.SynchronizationContext;
#else
                throw new PlatformNotSupportedException();
#endif
            }
        }

        internal bool _isDisposed;

        internal JSObject(IntPtr jsHandle, JSProxyContext ctx)
        {
            ProxyContext = ctx;
            JSHandle = jsHandle;
        }

        /// <inheritdoc />
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is JSObject other && JSHandle == other.JSHandle;

        /// <inheritdoc />
        public override int GetHashCode() => (int)JSHandle;

        /// <inheritdoc />
        public override string ToString() => $"(js-obj js '{JSHandle}')";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void AssertNotDisposed()
        {
            lock (ProxyContext)
            {
                ObjectDisposedException.ThrowIf(IsDisposed, this);
            }
        }

        internal void DisposeImpl(bool skipJsCleanup = false)
        {
            if (!_isDisposed)
            {
                JSProxyContext.ReleaseCSOwnedObject(this, skipJsCleanup);
            }
        }

        ~JSObject()
        {
            DisposeImpl();
        }

        /// <summary>
        /// Releases any resources used by the proxy and discards the reference to its target JavaScript object.
        /// </summary>
        public void Dispose()
        {
            DisposeImpl();
        }
    }
}
