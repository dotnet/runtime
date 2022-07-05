// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Represents the JavaScript host environment where the .NET runtime is currently operating.
    /// </summary>
    [SupportedOSPlatform("browser")]
    public static partial class JSHost
    {
        /// <summary>
        /// Returns a proxy for the <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/globalThis">globalThis</see> JavaScript host object.
        /// </summary>
        public static JSObject GlobalThis
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Returns proxy of the runtime module export in JavaScript.
        /// </summary>
        public static JSObject DotnetInstance
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Download and instantiate ES6 module from provided URL.
        /// It will use <see href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/import">dynamic import</see> of underlying JavaScript engine.
        /// The instance of the module would be downloaded only once per <paramref name="moduleName"/> and cached.
        /// </summary>
        /// <param name="moduleName">Globally unique identifier of the ES6 module, which is used by <see cref="JSImportAttribute(string, string)"/>.</param>
        /// <param name="moduleUrl"></param>
        /// <param name="cancellationToken">A cancellation token to observe.</param>
        /// <returns>Proxy object of the module exports.</returns>
        public static Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
