//------------------------------------------------------------------------------
// <copyright file="ReplicationFailureCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
     using System;
     using System.Collections;
     using System.Runtime.InteropServices;

     public class ReplicationFailureCollection :ReadOnlyCollectionBase { 
         DirectoryServer server = null;
         Hashtable nameTable = null;
         
         internal ReplicationFailureCollection(DirectoryServer server) 
         {
             this.server = server;
             Hashtable tempNameTable = new Hashtable();
             nameTable = Hashtable.Synchronized(tempNameTable);
         }

         public ReplicationFailure this[int index] {
            get {
                return (ReplicationFailure) InnerList[index];                                                 
            }
         }
         
         public bool Contains(ReplicationFailure failure) {
             if(failure == null)
                 throw new ArgumentNullException("failure");
             
             return InnerList.Contains(failure);
         }  

         public int IndexOf(ReplicationFailure failure) {
             if(failure == null)
                 throw new ArgumentNullException("failure");
             
             return InnerList.IndexOf(failure);
         } 

         public void CopyTo(ReplicationFailure[] failures, int index) {
             InnerList.CopyTo(failures, index);
         }

         private int Add(ReplicationFailure failure)
         {
             return InnerList.Add(failure);
         }

         internal void AddHelper(DS_REPL_KCC_DSA_FAILURES failures, IntPtr info)
         {
             // get the count
             int count = failures.cNumEntries;
             
             IntPtr addr = (IntPtr)0;             

             for(int i = 0; i < count; i++)
             {                                                       
                 addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_KCC_DSA_FAILURE)));                 

                 ReplicationFailure managedFailure = new ReplicationFailure(addr, server, nameTable);

                 // in certain scenario, KCC returns some failure records that we need to process it first before returning
                 if(managedFailure.LastErrorCode == 0)
                 {
                     // we change the error code to some generic one
                     managedFailure.lastResult = ExceptionHelper.ERROR_DS_UNKNOWN_ERROR;
                 }                 
                 
                 Add(managedFailure); 
             }
         }
        
     }
}
