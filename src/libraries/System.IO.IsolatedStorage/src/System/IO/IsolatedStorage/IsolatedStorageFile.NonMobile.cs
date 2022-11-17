// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {
        private string GetIsolatedStorageRoot()
        {
            StringBuilder root = new StringBuilder(Helper.GetRootDirectory(Scope));
            root.Append(SeparatorExternal);
            root.Append(IdentityHash);

            return root.ToString();
        }
    }
}
