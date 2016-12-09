/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    FindResult.cs

Abstract:

    Implements the FindResult<T> class.

History:

    06-May-2004    MattRim     Created

--*/

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

            return new FindResultEnumerator<T>(resultSet);
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="PrincipalSearchResult`1<T>.GetEnumerator():System.Collections.Generic.IEnumerator`1<T>" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator) GetEnumerator();
        }

        public void Dispose()
        {
            if (!this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Dispose: disposing");
            
                if (this.resultSet != null)
                {
                    GlobalDebug.WriteLineIf(GlobalDebug.Info, "PrincipalSearchResult", "Dispose: disposing resultSet");
                
                    lock (this.resultSet)
                    {
                        this.resultSet.Dispose();
                    }
                }

                this.disposed = true;
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
        
            this.resultSet = resultSet;
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
        ResultSet resultSet;

        bool disposed = false;

        void CheckDisposed()
        {
            if (this.disposed)
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Warn, "PrincipalSearchResult", "CheckDisposed: accessing disposed object");
                throw new ObjectDisposedException("PrincipalSearchResult");
            }
        }
        
    }
}
