// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.IsolatedStorage
{
    internal static partial class Helper
    {
        // we're using a different directory name for compatibility with legacy Xamarin
        public const string IsolatedStorageDirectoryName = ".isolated-storage";

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
