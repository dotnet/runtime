// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// The NETNative_SystemNetHttp #define is used in some source files to indicate we are compiling classes
// directly into the .NET Native System.Net.Http.dll implementation assembly in order to use internal class
// methods. Internal methods are needed in order to map cookie response headers from the WinRT Windows.Web.Http APIs.
// Windows.Web.Http is used underneath the System.Net.Http classes on .NET Native. Having other similarly
// named classes would normally conflict with the public System.Net namespace classes that are also in the 
// System.Private.Networking dll. So, we need to move the classes to a different namespace. Thoses classes are never
// exposed up to user code so there isn't a problem.  In the future, we might expose some of the internal methods
// as new public APIs and then we can remove this duplicate source code inclusion in the binaries.
#if NETNative_SystemNetHttp
namespace System.Net.Internal
#else
namespace System.Net
#endif
{
    public class CookieException : FormatException
    {
        public CookieException() : base()
        {
        }

        internal CookieException(string message) : base(message)
        {
        }

        internal CookieException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
