//------------------------------------------------------------------------------
// <copyright file="AttributeMetaData.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
     using System;
     using System.Collections;
     using System.Runtime.InteropServices;
     using System.Diagnostics;

     public class AttributeMetadata {

         private string pszAttributeName = null;  
         private int dwVersion;  
         private DateTime ftimeLastOriginatingChange;  
         private Guid uuidLastOriginatingDsaInvocationID;  
         private long usnOriginatingChange;  
         private long usnLocalChange;  
         private string pszLastOriginatingDsaDN = null;

         private string originatingServerName = null;
         private DirectoryServer server = null;         
         private Hashtable nameTable = null;
         bool advanced = false;

         internal AttributeMetadata(IntPtr info, bool advanced, DirectoryServer server, Hashtable table) 
         { 
             if(advanced)
             {
                 DS_REPL_ATTR_META_DATA_2 attrMetaData = new DS_REPL_ATTR_META_DATA_2();
                 Marshal.PtrToStructure(info, attrMetaData);
                 Debug.Assert(attrMetaData != null);

                 pszAttributeName = Marshal.PtrToStringUni(attrMetaData.pszAttributeName);
                 dwVersion = attrMetaData.dwVersion; 
                 long ftimeChangeValue = (long)((uint)attrMetaData.ftimeLastOriginatingChange1 + (((long)attrMetaData.ftimeLastOriginatingChange2) << 32 ));
                 ftimeLastOriginatingChange = DateTime.FromFileTime(ftimeChangeValue);
                 uuidLastOriginatingDsaInvocationID = attrMetaData.uuidLastOriginatingDsaInvocationID;
                 usnOriginatingChange = attrMetaData.usnOriginatingChange;
                 usnLocalChange = attrMetaData.usnLocalChange;
                 pszLastOriginatingDsaDN = Marshal.PtrToStringUni(attrMetaData.pszLastOriginatingDsaDN);
             }
             else
             {
                 DS_REPL_ATTR_META_DATA attrMetaData = new DS_REPL_ATTR_META_DATA();
                 Marshal.PtrToStructure(info, attrMetaData);
                 Debug.Assert(attrMetaData != null);

                 pszAttributeName = Marshal.PtrToStringUni(attrMetaData.pszAttributeName);
                 dwVersion = attrMetaData.dwVersion;                                  
                 long ftimeChangeValue = (long)((uint)attrMetaData.ftimeLastOriginatingChange1 + (((long)attrMetaData.ftimeLastOriginatingChange2) << 32 ));
                 ftimeLastOriginatingChange = DateTime.FromFileTime(ftimeChangeValue);                 
                 uuidLastOriginatingDsaInvocationID = attrMetaData.uuidLastOriginatingDsaInvocationID;
                 usnOriginatingChange = attrMetaData.usnOriginatingChange;
                 usnLocalChange = attrMetaData.usnLocalChange;
             }
             this.server = server;
             this.nameTable = table;
             this.advanced = advanced;
         }      

         public string Name {
             get {
                 return pszAttributeName;
             }
         }

         public int Version {
             get {
                 return dwVersion;
             }
         }

         public DateTime LastOriginatingChangeTime {
             get {
                 return ftimeLastOriginatingChange;
             }
         }

         public Guid LastOriginatingInvocationId {
             get {
                 return uuidLastOriginatingDsaInvocationID;
             }
         }

         public long OriginatingChangeUsn {
             get {
                 return usnOriginatingChange;
             }
         }

         public long LocalChangeUsn {
             get {
                 return usnLocalChange;
             }
         }

         public string OriginatingServer {
             get {
                 if(originatingServerName == null)
                 {
                     // check whether we have got it before
                     if(nameTable.Contains(LastOriginatingInvocationId))
                     {                         
                         originatingServerName = (string) nameTable[LastOriginatingInvocationId];
                     }
                     // do the translation for downlevel platform or kcc is able to do the name translation
                     else if(!advanced || (advanced && pszLastOriginatingDsaDN != null))
                     {
                     
                         originatingServerName = Utils.GetServerNameFromInvocationID(pszLastOriginatingDsaDN, LastOriginatingInvocationId, server);
                         
                         // add it to the hashtable
                         nameTable.Add(LastOriginatingInvocationId, originatingServerName);
                     }
                     
                 }

                 return originatingServerName;
             }
         }
        
     }
}
