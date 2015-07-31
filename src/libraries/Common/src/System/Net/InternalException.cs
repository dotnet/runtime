// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal class InternalException : Exception
    {
        internal InternalException()
        {
            GlobalLog.Assert("InternalException thrown.");
        }
    }
}
