// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace System.Runtime.Serialization.Formatters.Tests
{
    internal static class PlatformExtensions
    {
        private static bool IsNetFxPatchedVersion(int build)
        {
            string versionRaw = RuntimeInformation.FrameworkDescription.Replace(".NET Framework", "").Trim();
            if (Version.TryParse(versionRaw, out Version version))
            {
                return version.Build >= build;
            }

            return false;
        }

        public static int GetPlatformIndex(this TypeSerializableValue[] blobs)
        {
            List<TypeSerializableValue> blobList = blobs.ToList();
            int index;

            // .NET Framework
            if (PlatformDetection.IsFullFramework)
            {
                // Check if a specialized blob for >=netfx472 build 3260 is present and return if found.
                if (IsNetFxPatchedVersion(3260))
                {
                    index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netfx472_3260);

                    if (index >= 0)
                        return index;
                }

                index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netfx472);
                if (index >= 0)
                    return index;

                index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netfx471);
                if (index >= 0)
                    return index;

                // If no newer blob for >=netfx471 is present use existing one. 
                // If no netfx blob is present then -1 will be returned.
                return blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netfx461);
            }

            // Check if a specialized blob for >=netcoreapp3.0 is present and return if found.
            index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netcoreapp30);
            if (index >= 0)
                return index;

            // Check if a specialized blob for netcoreapp2.1 is present and return if found.
            index = blobList.FindIndex(b => b.Platform == TargetFrameworkMoniker.netcoreapp21);
            if (index >= 0)
                return index;

            // If no newer blob for >=netcoreapp2.1 is present use existing one.
            // If no netcoreapp blob is present then -1 will be returned.
            return blobList.FindIndex((b => b.Platform == TargetFrameworkMoniker.netcoreapp20));
        }
    }
}
