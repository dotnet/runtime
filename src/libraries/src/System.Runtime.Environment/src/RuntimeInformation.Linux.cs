// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.InteropServices
{
    public static class RuntimeInformation
    {
        public static bool IsOperatingSystem(OSName osName)
        {
            return OSName.Linux == osName;
        }
    }
}
