// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.NET.HostModel.AppHost;
using Microsoft.NET.HostModel.MachO.CodeSign.Tests;
using System;
using System.IO;
using Microsoft.NET.HostModel.MachO;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public static class TestOnlyProductBehavior
    {
        private static readonly byte[] OriginalTestOnlyMarker = StringToByteArray("d38cc827-e34f-4453-9df4-1e796e9f1d07");
        private static readonly byte[] EnabledTestOnlyMarker = StringToByteArray("e38cc827-e34f-4453-9df4-1e796e9f1d07");

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

            if (BinaryUtils.SearchInFile(productBinaryPath, OriginalTestOnlyMarker) == -1)
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
                // For some reason, tests are crashing when trying to modify the product binary on macOS if it is signed.
                // So we remove the signature, modify the binary, and then re-sign it.
                // We shouldn't need to worry about preserving entitlements here
                if (Codesign.IsAvailable && Codesign.Run("--remove-signature", productBinaryPath).ExitCode != 0)
                {
                    throw new Exception($"Failed to remove the signature from the product binary {productBinaryPath} before enabling test only behavior.");
                }
                BinaryUtils.SearchAndReplace(
                    productBinaryPath,
                    OriginalTestOnlyMarker,
                    EnabledTestOnlyMarker);
                if (Codesign.IsAvailable && Codesign.Run("--sign -", productBinaryPath).ExitCode != 0)
                {
                    throw new Exception($"Failed to re-sign the product binary {productBinaryPath} after enabling test only behavior.");
                }
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
