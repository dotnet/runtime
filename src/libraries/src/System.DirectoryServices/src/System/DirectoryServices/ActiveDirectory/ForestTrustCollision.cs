//------------------------------------------------------------------------------
// <copyright file="ForestTrustRelationshipCollision.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

/*
 */

namespace System.DirectoryServices.ActiveDirectory {
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;

    public class ForestTrustRelationshipCollision {
        ForestTrustCollisionType type;
        TopLevelNameCollisionOptions tlnFlag;
        DomainCollisionOptions domainFlag;
        string record  = null;
        
        internal ForestTrustRelationshipCollision(ForestTrustCollisionType collisionType, TopLevelNameCollisionOptions TLNFlag, DomainCollisionOptions domainFlag, string record)
        {
            this.type = collisionType;
            this.tlnFlag = TLNFlag;
            this.domainFlag = domainFlag;
            this.record = record;
        }

        public ForestTrustCollisionType CollisionType {
            get {
                return this.type;                
            }
        }

        public TopLevelNameCollisionOptions TopLevelNameCollisionOption {
            get {
                return this.tlnFlag;
            }
        }

        public DomainCollisionOptions DomainCollisionOption {
            get {
                return this.domainFlag;
            }
        }

        public string CollisionRecord {
            get {
                return this.record;
            }
        }
    }

    public class ForestTrustRelationshipCollisionCollection :ReadOnlyCollectionBase {
        internal ForestTrustRelationshipCollisionCollection() {}

        public ForestTrustRelationshipCollision this[int index] {
            get {
                return (ForestTrustRelationshipCollision) InnerList[index];                                                 
            }
         }

         public bool Contains(ForestTrustRelationshipCollision collision) {
             if(collision == null)
                 throw new ArgumentNullException("collision");
             
             return InnerList.Contains(collision);
         }  

         public int IndexOf(ForestTrustRelationshipCollision collision) {
             if(collision == null)
                 throw new ArgumentNullException("collision");
             
             return InnerList.IndexOf(collision);
         }

         public void CopyTo(ForestTrustRelationshipCollision[] array, int index) {
             InnerList.CopyTo(array, index);
         }

         internal int Add(ForestTrustRelationshipCollision collision)
         {
             return InnerList.Add(collision);
         }
    }
}
