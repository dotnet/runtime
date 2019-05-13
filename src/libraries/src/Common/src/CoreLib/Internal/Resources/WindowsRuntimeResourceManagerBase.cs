// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace Internal.Resources
{
    // This is implemented in System.Runtime.WindowsRuntime as System.Resources.WindowsRuntimeResourceManager,
    // allowing us to ask for a WinRT-specific ResourceManager.
    public abstract class WindowsRuntimeResourceManagerBase
    {
        public abstract bool Initialize(string libpath, string reswFilename, out PRIExceptionInfo? exceptionInfo);

        public abstract string GetString(string stringName, string? startingCulture, string? neutralResourcesCulture);

        public abstract CultureInfo? GlobalResourceContextBestFitCultureInfo
        {
            get;
        }

        public abstract bool SetGlobalResourceContextDefaultCulture(CultureInfo ci);

        /// <summary>
        /// Check whether CultureData exists for specified cultureName
        /// This API is used for WindowsRuntimeResourceManager in System.Runtime.WindowsRuntime
        /// </summary>
        public static bool IsValidCulture(string? cultureName)
        {
            return CultureData.GetCultureData(cultureName, /* useUserOverride */ true) != null;
        }
    }
}
