// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {
        internal IsolatedStorageFile(IsolatedStorageScope scope)
        {
            // In legacy Xamarin root ends with "/.isolated-storage"
            // In .Net 7 we added at the end  "/AppFiles" or "/Files" or "/AssemFiles/".
            // e.g. .Net 7  path = /data/user/0/{packageName}/files/.isolated-storage/{hash}/{hash}/AppFiles/
            // e.g. Xamarin path = /data/user/0/{packageName}/files/.config/.isolated-storage"
            //
            // Since we shipped that behavior as part of .NET 7 we can't change this now or upgraded apps wouldn't find their files anymore.
            // We need to look for an existing directory first before using the legacy Xamarin approach.

            InitStore(scope, null, null);

            StringBuilder sb = new StringBuilder(GetIsolatedStorageRoot());

            string directoryPath = sb.ToString() + SeparatorExternal;
            if (Helper.IsApplication(scope) && Directory.Exists(directoryPath + s_appFiles))
            {
                sb.Append(SeparatorExternal);
                sb.Append(s_appFiles);
                sb.Append(SeparatorExternal);
            }
            else if (Helper.IsDomain(scope) && Directory.Exists(directoryPath + s_files))
            {
                sb.Append(SeparatorExternal);
                sb.Append(s_files);
                sb.Append(SeparatorExternal);
            }
            else if (Directory.Exists(directoryPath + s_assemFiles))
            {
                sb.Append(SeparatorExternal);
                sb.Append(s_assemFiles);
                sb.Append(SeparatorExternal);
            }

            _rootDirectory = sb.ToString();
            Helper.CreateDirectory(_rootDirectory, scope);
        }

        private string GetIsolatedStorageRoot()
        {
            return Helper.GetRootDirectory(Scope);
        }

        private bool IsMatchingScopeDirectory(string _)
        {
            return (Helper.IsApplication(Scope)) || (Helper.IsAssembly(Scope)) || (Helper.IsDomain(Scope));
        }

        private string? GetParentDirectory()
        {
            return Path.GetDirectoryName(RootDirectory);
        }
    }
}
