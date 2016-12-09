//------------------------------------------------------------------------------
// <copyright file="ReadOnlySiteLinkBridgeCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.DirectoryServices;
    using System.Globalization;
    
    public class ReadOnlySiteLinkBridgeCollection :ReadOnlyCollectionBase {
        internal ReadOnlySiteLinkBridgeCollection() {}

        public ActiveDirectorySiteLinkBridge this[int index] {
            get {
                return (ActiveDirectorySiteLinkBridge) InnerList[index];                                                 
            }
         }

         public bool Contains(ActiveDirectorySiteLinkBridge bridge) {
             if(bridge == null)
                throw new ArgumentNullException("bridge");

             string dn = (string) PropertyManager.GetPropertyValue(bridge.context, bridge.cachedEntry, PropertyManager.DistinguishedName);             

             for(int i = 0; i < InnerList.Count; i++)
             {
                 ActiveDirectorySiteLinkBridge tmp = (ActiveDirectorySiteLinkBridge) InnerList[i];
                 string tmpDn = (string) PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);
                 
                 if(Utils.Compare(tmpDn, dn) == 0)
                 {
                     return true;
                 }
             }
             return false;
         }  

         public int IndexOf(ActiveDirectorySiteLinkBridge bridge) {
             if(bridge == null)
                throw new ArgumentNullException("bridge");

             string dn = (string) PropertyManager.GetPropertyValue(bridge.context, bridge.cachedEntry, PropertyManager.DistinguishedName);

             for(int i = 0; i < InnerList.Count; i++)
             {
                 ActiveDirectorySiteLinkBridge tmp = (ActiveDirectorySiteLinkBridge) InnerList[i];
                 string tmpDn = (string) PropertyManager.GetPropertyValue(tmp.context, tmp.cachedEntry, PropertyManager.DistinguishedName);
                 
                 if(Utils.Compare(tmpDn, dn) == 0)
                 {
                     return i;
                 }
             }
             return -1;
         } 

         public void CopyTo(ActiveDirectorySiteLinkBridge[] bridges, int index) {
             InnerList.CopyTo(bridges, index);
         }

         internal int Add(ActiveDirectorySiteLinkBridge bridge)
         {
             return InnerList.Add(bridge);
         }

         internal void Clear()
         {
             InnerList.Clear();
         }
        
    }
}
