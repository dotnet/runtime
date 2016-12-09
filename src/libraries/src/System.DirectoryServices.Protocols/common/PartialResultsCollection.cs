//------------------------------------------------------------------------------
// <copyright file="PartialResultsCollection" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.DirectoryServices.Protocols {
    using System;
    using System.Collections;

    public class PartialResultsCollection :ReadOnlyCollectionBase {
        internal PartialResultsCollection() {}

        public object this[int index] {
            get {
                return InnerList[index];                                                 
            }
        }

        internal int Add(object value) {
            return InnerList.Add(value);
        }

        public bool Contains(object value) {
            return InnerList.Contains(value);
        }  

        public int IndexOf(object value) {
            return InnerList.IndexOf(value);
        } 

        public void CopyTo(object[] values, int index) {
            InnerList.CopyTo(values, index);
        }

        
    }
}
