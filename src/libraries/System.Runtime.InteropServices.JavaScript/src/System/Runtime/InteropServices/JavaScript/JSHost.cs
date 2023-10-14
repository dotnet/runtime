﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Threading;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents the JavaScript host environment where the .NET runtime is currently operating.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public static partial class JSHost
    {
        /// <summary>
        /// Returns a proxy for the <see href="https://developer.mozilla.org/docs/Web/JavaScript/Reference/Global_Objects/globalThis">globalThis</see> JavaScript host object.
        /// </summary>
        public static JSObject GlobalThis
        {
            get
            {
#if FEATURE_WASM_THREADS
                JSSynchronizationContext.AssertWebWorkerContext();
#endif
                return JavaScriptImports.GetGlobalThis();
            }
        }

        /// <summary>
        /// Returns a proxy for the JavaScript module that contains the .NET runtime.
        /// </summary>
        public static JSObject DotnetInstance
        {
            get
            {
#if FEATURE_WASM_THREADS
                JSSynchronizationContext.AssertWebWorkerContext();
#endif
                return JavaScriptImports.GetDotnetInstance();
            }
        }

        /// <summary>
        /// Downloads and instantiates an ES6 module from the provided URL, via the JavaScript host's <see href="https://developer.mozilla.org/docs/Web/JavaScript/Reference/Operators/import">dynamic import API</see>.
        /// If a module with the provided <paramref name="moduleName" /> has previously been instantiated, it will be returned instead.
        /// </summary>
        /// <param name="moduleName">Globally unique identifier of the ES6 module, which is used by <see cref="JSImportAttribute(string, string)"/>.</param>
        /// <param name="moduleUrl">The location of the module file.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A proxy for the JavaScript object that contains the module's exports.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken = default)
        {
#if FEATURE_WASM_THREADS
            JSSynchronizationContext.AssertWebWorkerContext();
#endif
            return JSHostImplementation.ImportAsync(moduleName, moduleUrl, cancellationToken);
        }

        public static SynchronizationContext? CurrentOrMainJSSynchronizationContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
#if FEATURE_WASM_THREADS
                return JSSynchronizationContext.CurrentJSSynchronizationContext ?? JSSynchronizationContext.MainJSSynchronizationContext ?? null;
#else
                return null;
#endif
            }
        }
    }
}
