// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    public class PrincipalSearchResult<T> : IEnumerable<T>, IEnumerable, IDisposable
    {
        //
        // Public methods
        //

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalSearchResult`1<T>.CheckDisposed():System.Void" />
        // <SatisfiesLinkDemand Name="FindResultEnumerator`1<T>..ctor(System.DirectoryServices.AccountManagement.ResultSet)" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        public IEnumerator<T> GetEnumerator()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Entering GetEnumerator");

            CheckDisposed();

            return new FindResultEnumerator<T>(_resultSet);
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalSearchResult`1<T>.GetEnumerator():System.Collections.Generic.IEnumerator`1<T>" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Dispose: disposing");

                if (_resultSet != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Dispose: disposing resultSet");

                    lock (_resultSet)
                    {
                        _resultSet.Dispose();
                    }
                }

                _disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        //
        // Internal Constructors
        //

        // Note that resultSet can be null
        internal PrincipalSearchResult(ResultSet resultSet)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Ctor");

            _resultSet = resultSet;
        }

        //
        // Private Implementation
        //

        //
        // SYNCHRONIZATION
        //   Access to:
        //      resultSet
        //   must be synchronized, since multiple enumerators could be iterating over us at once.
        //   Synchronize by locking on resultSet (if resultSet is non-null).

        // The ResultSet returned by the query.
        private ResultSet _resultSet;

        private bool _disposed = false;

        private void CheckDisposed()
        {
            if (_disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalSearchResult", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException("PrincipalSearchResult");
            }
        }
    }
}
