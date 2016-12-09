/*++

Copyright (c) 2004  Microsoft Corporation

Module Name:

    ValueCollectionEnumerator.cs

Abstract:

    Implements the ValueCollectionEnumerator<T> class.

History:

    10-May-2004    MattRim     Created

--*/

using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

namespace System.DirectoryServices.AccountManagement
{
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    internal class ValueCollectionEnumerator<T> : IEnumerator<T>, IEnumerator
    // T must be a ValueType
    {

        //
        // Public properties
        //

        public T Current
        {
            get
            {
                GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Current");            
                return inner.Current;
            }
        }

        

        object IEnumerator.Current
        {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="ValueCollectionEnumerator`1<T>.get_Current():T" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get
            {
                return Current;
            }
        }


        //
        // Public methods
        //

        public bool MoveNext()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering MoveNext");        
            return inner.MoveNext();
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="ValueCollectionEnumerator`1<T>.MoveNext():System.Boolean" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        bool IEnumerator.MoveNext()
        {
            return MoveNext();
        }

        public void Reset()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Reset");        
            inner.Reset();
        }

        // <SecurityKernel Critical="True" Ring="0">
        // <SatisfiesLinkDemand Name="ValueCollectionEnumerator`1<T>.Reset():System.Void" />
        // </SecurityKernel>
        [System.Security.SecurityCritical]
        void IEnumerator.Reset()
        {
            Reset();
        }

        public void Dispose()
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Entering Dispose");
            inner.Dispose();
        }


        //
        // Internal constructors
        //
        internal ValueCollectionEnumerator(TrackedCollection<T> trackingList, List<TrackedCollection<T>.ValueEl> combinedValues)
        {
            GlobalDebug.WriteLineIf(GlobalDebug.Info, "ValueCollectionEnumerator", "Ctor");            
            inner = new TrackedCollectionEnumerator<T>("ValueCollectionEnumerator", trackingList, combinedValues);
        }


        //
        // Private implementation
        //

        TrackedCollectionEnumerator<T> inner;
    
    }
}

