// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    //
    // Support for tooling-friendly NotImplementedExceptions.
    //
    internal static class NotImplemented
    {
        /// <summary>
        /// Permanent NotImplementedException with no message shown to user.
        /// </summary>
        internal static Exception ByDesign => new NotImplementedException();

        /// <summary>
        /// Permanent NotImplementedException with localized message shown to user.
        /// </summary>
        internal static Exception ByDesignWithMessage(string message)
        {
            return new NotImplementedException(message);
        }
    }
}
