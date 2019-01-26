// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System
{
    public static partial class Environment
    {
        public static string UserName => "Windows User";
        public static string UserDomainName => "Windows Domain";
        private static string GetFolderPathCore(SpecialFolder folder, SpecialFolderOption option) =>
            WinRTFolderPaths.GetFolderPath(folder, option);
    }
}
