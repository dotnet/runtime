// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.Net
{
    internal class InternalException : Exception
    {
        internal InternalException()
        {
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Assert("InternalException thrown.");
            }
            Debug.Fail("InternalException thrown.");
        }
    }
}
