// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    internal partial class CultureData
    {
        private bool InitCultureDataCore() => NlsInitCultureData();

        internal bool IsWin32Installed => true;

        internal static unsafe CultureData GetCurrentRegionData() => NlsGetCurrentRegionData();
    }
}
