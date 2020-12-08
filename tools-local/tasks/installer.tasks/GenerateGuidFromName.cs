// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Build.Framework;
using System;
using System.Security.Cryptography;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GenerateGuidFromName : BuildTask
    {
        [Required]
        public string Name { get; set; }

        [Output]
        public string GeneratedGuid { get; set; }

        // Generate a Version 5 (SHA1 Name Based) Guid from a name.
        public override bool Execute()
        {
            // Any fixed GUID will do for a namespace.
            Guid namespaceId = new Guid("28F1468D-672B-489A-8E0C-7C5B3030630C");

            using (SHA1 hasher = SHA1.Create())
            {
                var nameBytes = System.Text.Encoding.UTF8.GetBytes(Name ?? string.Empty);
                var namespaceBytes = namespaceId.ToByteArray();

                SwapGuidByteOrder(namespaceBytes);

                var streamToHash = new byte[namespaceBytes.Length + nameBytes.Length];

                Array.Copy(namespaceBytes, streamToHash, namespaceBytes.Length);
                Array.Copy(nameBytes, 0, streamToHash, namespaceBytes.Length, nameBytes.Length);

                var hashResult = hasher.ComputeHash(streamToHash);

                var res = new byte[16];

                Array.Copy(hashResult, res, res.Length);

                unchecked { res[6] = (byte)(0x50 | (res[6] & 0x0F)); }
                unchecked { res[8] = (byte)(0x40 | (res[8] & 0x3F)); }

                SwapGuidByteOrder(res);

                GeneratedGuid = (new Guid(res)).ToString();
            }

            return true;
        }

        // Do a byte order swap, .NET GUIDs store multi byte components in little
        // endian.
        private static void SwapGuidByteOrder(byte[] b)
        {
            Swap(b, 0, 3);
            Swap(b, 1, 2);
            Swap(b, 5, 6);
            Swap(b, 7, 8);
        }

        private static void Swap(byte[] b, int x, int y)
        {
            byte t = b[x];
            b[x] = b[y];
            b[y] = t;
        }
    }
}
