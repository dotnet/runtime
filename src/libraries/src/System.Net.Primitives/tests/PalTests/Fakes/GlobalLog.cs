// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    public static class GlobalLog
    {
        public static void Assert(string message)
        {
        }

        public static void Print(string message)
        {
        }

        public static bool IsEnabled { get { return false; } }
    }
}
