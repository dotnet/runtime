//------------------------------------------------------------------------------
// <copyright file="TrustRelationshipInformationCollection.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

  namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Globalization;

    public class TrustRelationshipInformationCollection :ReadOnlyCollectionBase{
        internal TrustRelationshipInformationCollection() {}

        internal TrustRelationshipInformationCollection(DirectoryContext context, string source, ArrayList trusts)
        {
            for(int i = 0; i < trusts.Count; i++)
            {
                TrustObject obj = (TrustObject) trusts[i];
                // we don't need self and forest trust
                if((obj.TrustType == TrustType.Forest) || ((int) obj.TrustType == 7))
                {
                    continue;
                }

                TrustRelationshipInformation info = new TrustRelationshipInformation(context, source, obj);
                Add(info);
            }
        }

        public TrustRelationshipInformation this[int index] {
            get {
                return (TrustRelationshipInformation) InnerList[index];                                                 
            }
         }

         public bool Contains(TrustRelationshipInformation information) {
             if(information == null)
                 throw new ArgumentNullException("information");
             
             return InnerList.Contains(information);
         }  

         public int IndexOf(TrustRelationshipInformation information) {
             if(information == null)
                 throw new ArgumentNullException("information");
            
             return InnerList.IndexOf(information);
         }

         public void CopyTo(TrustRelationshipInformation[] array, int index) {
             InnerList.CopyTo(array, index);
         }
         
         internal int Add(TrustRelationshipInformation info)
         {
             return InnerList.Add(info);
         } 

    }
}
