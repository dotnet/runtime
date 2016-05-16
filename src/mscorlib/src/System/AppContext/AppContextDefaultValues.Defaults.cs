// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System
{
    internal static partial class AppContextDefaultValues
    {

        internal static readonly string SwitchNoAsyncCurrentCulture = "Switch.System.Globalization.NoAsyncCurrentCulture";
        internal static readonly string SwitchThrowExceptionIfDisposedCancellationTokenSource = "Switch.System.Threading.ThrowExceptionIfDisposedCancellationTokenSource";
        internal static readonly string SwitchPreserveEventListnerObjectIdentity = "Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity";
#if FEATURE_PATHCOMPAT
        internal static readonly string SwitchUseLegacyPathHandling = "Switch.System.IO.UseLegacyPathHandling";
        internal static readonly string SwitchBlockLongPaths = "Switch.System.IO.BlockLongPaths";
#endif

        // This is a partial method. Platforms can provide an implementation of it that will set override values
        // from whatever mechanism is available on that platform. If no implementation is provided, the compiler is going to remove the calls
        // to it from the code
        // We are going to have an implementation of this method for the Desktop platform that will read the overrides from app.config, registry and
        // the shim database. Additional implementation can be provided for other platforms.
        static partial void PopulateOverrideValuesPartial();

        static partial void PopulateDefaultValuesPartial(string platformIdentifier, string profile, int version)
        {
            // When defining a new switch  you should add it to the last known version.
            // For instance, if you are adding a switch in .NET 4.6 (the release after 4.5.2) you should defined your switch
            // like this:
            //    if (version <= 40502) ...
            // This ensures that all previous versions of that platform (up-to 4.5.2) will get the old behavior by default
            // NOTE: When adding a default value for a switch please make sure that the default value is added to ALL of the existing platforms!
            // NOTE: When adding a new if statement for the version please ensure that ALL previous switches are enabled (ie. don't use else if)
            switch (platformIdentifier)
            {
                case ".NETCore":
                case ".NETFramework":
                    {
                        if (version <= 40502)
                        {
                            AppContext.DefineSwitchDefault(SwitchNoAsyncCurrentCulture, true);
                            AppContext.DefineSwitchDefault(SwitchThrowExceptionIfDisposedCancellationTokenSource, true);
                        }
#if FEATURE_PATHCOMPAT
                        if (version <= 40601)
                        {
                            AppContext.DefineSwitchDefault(SwitchUseLegacyPathHandling, true);
                            AppContext.DefineSwitchDefault(SwitchBlockLongPaths, true);
                        }
#endif
                        break;
                    }
                case "WindowsPhone":
                case "WindowsPhoneApp":
                    {
                        if (version <= 80100)
                        {
                            AppContext.DefineSwitchDefault(SwitchNoAsyncCurrentCulture, true);
                            AppContext.DefineSwitchDefault(SwitchThrowExceptionIfDisposedCancellationTokenSource, true);
                        }
                        break;
                    }
            }

            // At this point we should read the overrides if any are defined
            PopulateOverrideValuesPartial();
        }
    }
}
