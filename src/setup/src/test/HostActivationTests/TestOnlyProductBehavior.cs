// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class TestOnlyProductBehavior
    {
        private static readonly byte[] OriginalTestOnlyMarker = StringToByteArray("d38cc827-e34f-4453-9df4-1e796e9f1d07");
        private static readonly byte[] EnabledTestOnlyMarker  = StringToByteArray("e38cc827-e34f-4453-9df4-1e796e9f1d07");

        private static byte[] StringToByteArray(string value)
        {
            byte[] result = new byte[value.Length];
            for (int i = 0; i < value.Length; i++)
            {
                result[i] = (byte)value[i];
            }

            return result;
        }

        public static IDisposable Enable(string productBinaryPath)
        {
            if (!File.Exists(productBinaryPath))
            {
                throw new Exception($"Could not find product binary {productBinaryPath} to enable test only behavior on.");
            }

            if (FileUtils.SearchInFile(productBinaryPath, OriginalTestOnlyMarker) == -1)
            {
                // The marker is already enabled (probably by another call to TestOnlyProductBehavior)
                // We allow this to be able to nest the enable calls seamlessly - so that tests don't have to track
                // which helper does what.
                return null;
            }

            TestFileBackup backup = new TestFileBackup(Path.GetDirectoryName(productBinaryPath), Path.GetFileNameWithoutExtension(productBinaryPath));
            backup.Backup(productBinaryPath);
            IDisposable returnDisposable = null;

            try
            {
                FileUtils.SearchAndReplace(
                    productBinaryPath,
                    OriginalTestOnlyMarker,
                    EnabledTestOnlyMarker,
                    terminateWithNul: false);
                returnDisposable = backup;
                backup = null;
            }
            finally
            {
                if (backup != null)
                {
                    backup.Dispose();
                }
            }

            return returnDisposable;
        }
    }
}
