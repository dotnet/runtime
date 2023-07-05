// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace System.IO.IsolatedStorage
{
    public sealed partial class IsolatedStorageFile : IsolatedStorage, IDisposable
    {
        // Data file notes
        // ===============

        // "identity.dat" is the serialized identity object, such as StrongName or Url. It is used to
        // enumerate stores, which we currently do not support.
        //
        // private const string IDFile = "identity.dat";

        // "info.dat" is used to track disk space usage (against quota). The accounting file for Silverlight
        // stores is "appInfo.dat". .NET Core is always in full trust so we can safely ignore these.
        //
        // private const string InfoFile = "info.dat";
        // private const string AppInfoFile = "appInfo.dat";

        internal IsolatedStorageFile(IsolatedStorageScope scope)
        {
            // Evidence isn't currently available: https://github.com/dotnet/runtime/issues/18208
            // public static IsolatedStorageFile GetStore(IsolatedStorageScope scope, Evidence domainEvidence, Type domainEvidenceType, Evidence assemblyEvidence, Type assemblyEvidenceType) { return default(IsolatedStorageFile); }

            // InitStore will set up the IdentityHash
            InitStore(scope, null, null);

            StringBuilder sb = new StringBuilder(GetIsolatedStorageRoot());
            sb.Append(SeparatorExternal);

            if (Helper.IsApplication(scope))
            {
                sb.Append(s_appFiles);
            }
            else if (Helper.IsDomain(scope))
            {
                sb.Append(s_files);
            }
            else
            {
                sb.Append(s_assemFiles);
            }
            sb.Append(SeparatorExternal);

            _rootDirectory = sb.ToString();
            Helper.CreateDirectory(_rootDirectory, scope);
        }

        private string GetIsolatedStorageRoot()
        {
            StringBuilder root = new StringBuilder(Helper.GetRootDirectory(Scope));
            root.Append(SeparatorExternal);
            root.Append(IdentityHash);

            return root.ToString();
        }

        private bool IsMatchingScopeDirectory(string directory)
        {
            string directoryName = Path.GetFileName(directory);

            return
                (Helper.IsApplication(Scope) && string.Equals(directoryName, s_appFiles, StringComparison.Ordinal))
                || (Helper.IsAssembly(Scope) && string.Equals(directoryName, s_assemFiles, StringComparison.Ordinal))
                || (Helper.IsDomain(Scope) && string.Equals(directoryName, s_files, StringComparison.Ordinal));
        }

        private string? GetParentDirectory()
        {
            return Path.GetDirectoryName(RootDirectory.TrimEnd(Path.DirectorySeparatorChar));
        }
    }
}
