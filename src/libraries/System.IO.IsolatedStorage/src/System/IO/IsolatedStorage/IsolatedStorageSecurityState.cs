// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Windows.Storage;

namespace System.IO.IsolatedStorage
{
    internal class IsolatedStorageSecurityState
    {
        private string _rootUserDirectory;

        internal static string GetRootUserDirectory()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.EnsureState();
            return state.RootUserDirectory;
        }

        private IsolatedStorageSecurityState()
        {
        }

        private string RootUserDirectory
        {
            get
            {
                return _rootUserDirectory;
            }
            set
            {
                _rootUserDirectory = value;
            }
        }

        private void EnsureState()
        {
            try
            {
                RootUserDirectory = ApplicationData.Current.LocalFolder.Path;
            }
            catch (Exception)
            {
                throw new IsolatedStorageException(SR.IsolatedStorage_Operation);
            }
        }
    }
}
