//------------------------------------------------------------------------------
// <copyright file="ReplicationNeighborCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
     using System;
     using System.Collections;
     using System.Runtime.InteropServices;

     public class ReplicationNeighborCollection :ReadOnlyCollectionBase {         
         DirectoryServer server = null;
         private Hashtable nameTable = null;
         
         internal ReplicationNeighborCollection(DirectoryServer server) 
         {
             this.server = server;
             Hashtable tempNameTable = new Hashtable();
             nameTable = Hashtable.Synchronized(tempNameTable);
         }

         public ReplicationNeighbor this[int index] {
            get {
                return (ReplicationNeighbor) InnerList[index];                                                 
            }
         }
         
         public bool Contains(ReplicationNeighbor neighbor) {
             if(neighbor == null)
                 throw new ArgumentNullException("neighbor");
             
             return InnerList.Contains(neighbor);
         }  

         public int IndexOf(ReplicationNeighbor neighbor) {
             if(neighbor == null)
                 throw new ArgumentNullException("neighbor");
             
             return InnerList.IndexOf(neighbor);
         } 

         public void CopyTo(ReplicationNeighbor[] neighbors, int index) {
             InnerList.CopyTo(neighbors, index);
         }

         private int Add(ReplicationNeighbor neighbor)
         {
             return InnerList.Add(neighbor);
         }

         internal void AddHelper(DS_REPL_NEIGHBORS neighbors, IntPtr info)
         {
             // get the count
             int count = neighbors.cNumNeighbors;
             
             IntPtr addr = (IntPtr)0;             

             for(int i = 0; i < count; i++)
             {                                                       
                 addr = IntPtr.Add(info, Marshal.SizeOf(typeof(int)) * 2 + i * Marshal.SizeOf(typeof(DS_REPL_NEIGHBOR)));                 

                 ReplicationNeighbor managedNeighbor = new ReplicationNeighbor(addr, server, nameTable);
                 
                 Add(managedNeighbor); 
             }
         }
        
     }
}


