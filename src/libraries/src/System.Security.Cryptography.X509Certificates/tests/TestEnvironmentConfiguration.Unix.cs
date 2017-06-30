// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Security.Cryptography.X509Certificates.Tests
{
    internal static partial class TestEnvironmentConfiguration
    {
        static partial void DetermineCanModifyStores(ref bool canModify)
        {
            try
            {
                canModify = DetermineCanModifyStores();
            }
            catch
            {
                // This is a little counterintuitive. If the capability probe fails,
                // assert that the feature works.  Then we'll hopefully get diagnosable
                // errors out of the test failures.
                canModify = true;
            }
        }

        private static bool DetermineCanModifyStores()
        {
            // Check the directory permissions and whether the filesystem supports chmod.
            // The only real expected failure from this method is that at the very end
            // `stat.Mode == mode` will fail, because fuseblk (NTFS) returns success on chmod,
            // but is a no-op.

            uint userId = Interop.Sys.GetEUid();
            string certStoresFeaturePath = PersistedFiles.GetUserFeatureDirectory("cryptography", "x509stores");

            Directory.CreateDirectory(certStoresFeaturePath);

            // Check directory permissions:

            Interop.Sys.FileStatus dirStat;
            if (Interop.Sys.Stat(certStoresFeaturePath, out dirStat) != 0)
            {
                return false;
            }

            if (dirStat.Uid != userId)
            {
                return false;
            }

            if ((dirStat.Mode & (int)Interop.Sys.Permissions.S_IRWXU) != (int)Interop.Sys.Permissions.S_IRWXU)
            {
                return false;
            }

            string probeFilename =
                Path.Combine(certStoresFeaturePath, $"{Guid.NewGuid().ToString("N")}.chmod");

            try
            {
                using (FileStream stream = new FileStream(probeFilename, FileMode.Create))
                {
                    Interop.Sys.FileStatus stat;
                    if (Interop.Sys.FStat(stream.SafeFileHandle, out stat) != 0)
                    {
                        return false;
                    }

                    if (stat.Uid != userId)
                    {
                        return false;
                    }

                    // The product code here has a lot of stuff it does.
                    // This capabilities probe will just check that chmod works.
                    int mode = stat.Mode;

                    // Flip all of the O bits.
                    mode ^= (int)Interop.Sys.Permissions.S_IRWXO;

                    if (Interop.Sys.FChMod(stream.SafeFileHandle, mode) < 0)
                    {
                        return false;
                    }

                    // Verify the chmod applied.
                    if (Interop.Sys.FStat(stream.SafeFileHandle, out stat) != 0)
                    {
                        return false;
                    }

                    // On fuseblk (NTFS) this will return false, because the fchmod
                    // call returned success without being able to actually apply
                    // mode-bits.
                    return stat.Mode == mode;
                }
            }
            finally
            {
                try
                {
                    File.Delete(probeFilename);
                }
                catch
                {
                    // Ignore any failure on delete.
                }
            }
        }
    }
}
