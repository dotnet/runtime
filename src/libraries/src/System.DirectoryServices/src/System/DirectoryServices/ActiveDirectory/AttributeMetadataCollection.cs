//------------------------------------------------------------------------------
// <copyright file="AttributeMetadataCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

 namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Collections;
    using System.Globalization;

    public class AttributeMetadataCollection :ReadOnlyCollectionBase {

        internal AttributeMetadataCollection() {}

        public AttributeMetadata this[int index] {
            get {
                return (AttributeMetadata) InnerList[index];                                                 
            }
        }

        public bool Contains(AttributeMetadata metadata) 
        {
            if(metadata == null)
                throw new ArgumentNullException("metadata");

            for(int i = 0; i < InnerList.Count; i++)
            {
                AttributeMetadata tmp = (AttributeMetadata) InnerList[i];
                string name = tmp.Name;

                if(Utils.Compare(name, metadata.Name) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        public int IndexOf(AttributeMetadata metadata) {
             if(metadata == null)
                 throw new ArgumentNullException("metadata");             
            
             for(int i = 0; i < InnerList.Count; i++)
             {
                 AttributeMetadata tmp = (AttributeMetadata) InnerList[i];
                 
                 if(Utils.Compare(tmp.Name, metadata.Name) == 0)
                 {
                     return i;
                 }
             }
             return -1;
         } 

        public void CopyTo(AttributeMetadata[] metadata, int index) 
        {
            InnerList.CopyTo(metadata, index);
        }

        internal int Add(AttributeMetadata metadata)
        {
            return InnerList.Add(metadata);
        }
                     
    }
}
