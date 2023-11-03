// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        // we're using a different directory name for compatibility with legacy Xamarin
        public const string IsolatedStorageDirectoryName = ".isolated-storage";

        internal static string GetDataDirectory(IsolatedStorageScope scope)
        {
            // In legacy Xamarin for Roaming Scope we were using Environment.SpecialFolder.LocalApplicationData
            // In .Net 7 for Roaming Scope we are using Environment.SpecialFolder.ApplicationData
            // e.g. Android .Net 7 path = /data/user/0/{packageName}/files/.isolated-storage/{hash}/{hash}/AppFiles/
            // e.g. Android Xamarin path = /data/user/0/{packageName}/files/.config/.isolated-storage/
            // e.g. iOS .Net 7 path = /Users/userName/{packageName}/Documents/.isolated-storage/{hash}/{hash}/AppFiles/
            // e.g. iOS Xamarin path = /Users/userName/{packageName}/Documents/.config/.isolated-storage/
            //
            // Since we shipped that behavior as part of .NET 7 we can't change this now or upgraded apps wouldn't find their files anymore.
            // We need to look for an existing directory first before using the legacy Xamarin approach.

            Environment.SpecialFolder specialFolder =
            IsMachine(scope) ? Environment.SpecialFolder.CommonApplicationData : // e.g. /usr/share;
            IsRoaming(scope) ? Environment.SpecialFolder.ApplicationData : // e.g. /data/user/0/{packageName}/files/.config;
            Environment.SpecialFolder.LocalApplicationData; // e.g. /data/user/0/{packageName}/files;

            string dataDirectory = Environment.GetFolderPath(specialFolder);
            dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);
            if (Directory.Exists(dataDirectory))
            {
                return dataDirectory;
            }
            // Otherwise return legacy xamarin path
            else
            {
                // In .Net 7 for Android SpecialFolder.LocalApplicationData returns "/data/user/0/{packageName}/files"
                // while in Xamarin it was "/data/user/0/{packageName}/files/.local/share"
                // For Android we need to hardcode Xamarin path for compatibility with legacy Xamarin
                if (OperatingSystem.IsAndroid() && IsRoaming(scope))
                {
                    dataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + ".local/share";
                    Directory.CreateDirectory(dataDirectory);
                }
                else
                {
                    specialFolder =
                    IsMachine(scope) ? Environment.SpecialFolder.CommonApplicationData :
                    IsRoaming(scope) ? Environment.SpecialFolder.LocalApplicationData :
                    Environment.SpecialFolder.ApplicationData;

                    dataDirectory = Environment.GetFolderPath(specialFolder, Environment.SpecialFolderOption.Create);
                }

                dataDirectory = Path.Combine(dataDirectory, IsolatedStorageDirectoryName);
            }

            return dataDirectory;
        }

        internal static string GetRandomDirectory(string rootDirectory, IsolatedStorageScope _)
        {
            // In legacy Xamarin we didn't have a random directory inside of the isolated storage root for each app,
            // we tried to preserve that in https://github.com/dotnet/runtime/pull/75541 but the fix wasn't complete enough
            // and we still created random directories when not using the Roaming scope.
            //
            // Since we shipped that behavior as part of .NET 7 we can't change this now or upgraded apps wouldn't find their files anymore.
            // We need to look for an existing random directory first before using the plain root directory.
            return GetExistingRandomDirectory(rootDirectory) ?? rootDirectory;
        }
    }
}
