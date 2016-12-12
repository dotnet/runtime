/*
Copyright (c) 2004  Microsoft Corporation

Module Name:

    DsPropertyCollection.cs

Abstract:

    This class wraps the PropertyCollection and ResultPropertyCollection collection from S.DS.
    This provides a consistent interface for the internal functions that read properties while still allowing
    use of either a DirectoryEntry or SearchResult as the data source.

History:

    20-Feb-2007    TQuerec     Created

--*/

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.DirectoryServices;
using System.Text;
using System.Net;
using System.Collections;

namespace System.DirectoryServices.AccountManagement
{
    internal class dSPropertyCollection
    {
        PropertyCollection pc;
        ResultPropertyCollection rp;
        
        private dSPropertyCollection() { }
        internal dSPropertyCollection(PropertyCollection pc) { this.pc = pc; }
        internal dSPropertyCollection(ResultPropertyCollection rp) { this.rp = rp; }

        public dSPropertyValueCollection this[string propertyName] {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="PropertyCollection.get_Item(System.String):System.DirectoryServices.PropertyValueCollection" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get 
            {    
                if(propertyName == null)
                    throw new ArgumentNullException("propertyName");

                if ( null != pc )
                {
                    return new dSPropertyValueCollection( pc[propertyName] );
                }
                else
                {
                    return new dSPropertyValueCollection( rp[propertyName] );
                }                                    
            }
        }
        
    }

    internal class dSPropertyValueCollection 
    {
        PropertyValueCollection pc;
        ResultPropertyValueCollection rc;
        
        private dSPropertyValueCollection() { }
        internal dSPropertyValueCollection(PropertyValueCollection pc) { this.pc = pc; }
        internal dSPropertyValueCollection(ResultPropertyValueCollection rc) { this.rc = rc; }

        public object this[int index] {
            // <SecurityKernel Critical="True" Ring="0">
            // <SatisfiesLinkDemand Name="PropertyValueCollection.get_Item(System.Int32):System.Object" />
            // </SecurityKernel>
            [System.Security.SecurityCritical]
            get 
            {
                if ( pc != null )
                {
                    return pc[index];
                }
                else
                {
                    return rc[index];                
                }                
            }
        }   
        public int Count
        {
            get
            {
                return ( pc != null ? pc.Count : rc.Count );
            }
        }     

        public IEnumerator GetEnumerator()
        {
            return ( pc != null ? pc.GetEnumerator() : rc.GetEnumerator() );
        }

    }

}
