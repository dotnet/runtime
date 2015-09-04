// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class ExceptionCheck
    {
        internal static bool IsFatal(Exception exception)
        {
            return exception != null && (exception is OutOfMemoryException);
        }
    }
}
