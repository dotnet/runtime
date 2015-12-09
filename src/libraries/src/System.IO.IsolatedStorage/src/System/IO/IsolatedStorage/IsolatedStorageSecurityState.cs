// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using Windows.Storage;

namespace System.IO.IsolatedStorage
{
    [SecurityCritical]
    internal class IsolatedStorageSecurityState
    {
        private string m_RootUserDirectory;

        internal static string GetRootUserDirectory()
        {
            IsolatedStorageSecurityState state = new IsolatedStorageSecurityState();
            state.EnsureState();
            return state.RootUserDirectory;
        }

        [SecurityCritical]
        private IsolatedStorageSecurityState()
        {
        }

        private String RootUserDirectory
        {
            get
            {
                return m_RootUserDirectory;
            }
            set
            {
                m_RootUserDirectory = value;
            }
        }

        [SecurityCritical]
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