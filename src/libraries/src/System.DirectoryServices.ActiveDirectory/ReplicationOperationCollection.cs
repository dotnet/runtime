//------------------------------------------------------------------------------
// <copyright file="ReplicationOperationCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
     using System;
     using System.Collections;
     using System.Runtime.InteropServices;

     public class ReplicationOperationCollection :ReadOnlyCollectionBase {         
         DirectoryServer server = null;
         Hashtable nameTable = null;
         
         internal ReplicationOperationCollection(DirectoryServer server) 
         {
             this.server = server;
             Hashtable tempNameTable = new Hashtable();
             nameTable = Hashtable.Synchronized(tempNameTable);
         }

         public ReplicationOperation this[int index] {
            get {
                return (ReplicationOperation) InnerList[index];                                                 
            }
         }         

         public bool Contains(ReplicationOperation operation) {
             if(operation == null)
                 throw new ArgumentNullException("operation");
             
             return InnerList.Contains(operation);
         }  

         public int IndexOf(ReplicationOperation operation) {
             if(operation == null)
                 throw new ArgumentNullException("operation");
             
             return InnerList.IndexOf(operation);
         } 

         public void CopyTo(ReplicationOperation[] operations, int index) {
             InnerList.CopyTo(operations, index);
         }

         private int Add(ReplicationOperation operation)
         {
             return InnerList.Add(operation);
         }

         internal void AddHelper(DS_REPL_PENDING_OPS operations, IntPtr info)
         {
             // get the count
             int count = operations.cNumPendingOps;                       
             
             IntPtr addr = (IntPtr)0;   

             for(int i = 0; i < count; i++)
             {
                 addr = IntPtr.Add(info, Marshal.SizeOf(typeof(DS_REPL_PENDING_OPS)) + i * Marshal.SizeOf(typeof(DS_REPL_OP)));
                 ReplicationOperation managedOperation = new ReplicationOperation(addr, server, nameTable);
                 
                 Add(managedOperation); 
             }
         }

         internal ReplicationOperation GetFirstOperation()
         {
             ReplicationOperation op = (ReplicationOperation) InnerList[0];
             InnerList.RemoveAt(0);
             
             return op;
         }
        
     }
}

