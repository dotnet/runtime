// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Win32
{
    public sealed class TempRegistryKey : IDisposable
    {
        /// <summary>Gets the created key's parent.</summary>
        public RegistryKey Parent { get; }

        /// <summary>Gets the created key's name.</summary>
        public string Name { get; }

        /// <summary>Gets if the key was successfully created./<summary>
        public RegistryKey Key { get; }

        public TempRegistryKey(RegistryKey parent, string name)
        {
            Parent = parent;
            Name = name;

            try
            {
                Key = Parent.CreateSubKey(Name);
            }
            catch
            {
                // Sometimes the key can't be created, such as if the
                // current user doesn't have administrator permissions.
                Key = null;
            }
        }

        ~TempRegistryKey()
        {
            DeleteKey();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            DeleteKey();
        }

        private void DeleteKey()
        {
            try
            {
                Parent.DeleteSubKeyTree(Name);
            }
            catch
            {
                // Ignore exceptions on disposal of the key.
            }
        }
    }
}
