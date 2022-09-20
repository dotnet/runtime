// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {

        private static void InitializeIsoStorageDirectoryName()
        {
            Helper.IsolatedStorageDirectoryName = ".isolated-storage";
        }

        private string GetIsolatedStorageRoot()
        {
            return Helper.GetRootDirectory(Scope);
        }
    }
}
