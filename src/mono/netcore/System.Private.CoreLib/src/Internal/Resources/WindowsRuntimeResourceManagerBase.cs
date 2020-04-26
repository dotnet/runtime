// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace Internal.Resources
{
    public abstract class WindowsRuntimeResourceManagerBase
    {
        public abstract bool Initialize(string libpath, string reswFilename, out PRIExceptionInfo? exceptionInfo);

        public abstract string? GetString(string stringName, string? startingCulture, string? neutralResourcesCulture);

        public abstract CultureInfo? GlobalResourceContextBestFitCultureInfo
        {
            get;
        }

        public abstract bool SetGlobalResourceContextDefaultCulture(CultureInfo ci);

        public static bool IsValidCulture(string? cultureName) => throw new PlatformNotSupportedException();
    }
}
