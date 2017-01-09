// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.DirectoryServices.ActiveDirectory
{
    using System;
    using System.Runtime.InteropServices;
    using System.Collections;
    using System.Collections.Specialized;
    using System.Diagnostics;

    public class ForestTrustRelationshipCollision
    {
        private ForestTrustCollisionType _type;
        private TopLevelNameCollisionOptions _tlnFlag;
        private DomainCollisionOptions _domainFlag;
        private string _record = null;

        internal ForestTrustRelationshipCollision(ForestTrustCollisionType collisionType, TopLevelNameCollisionOptions TLNFlag, DomainCollisionOptions domainFlag, string record)
        {
            _type = collisionType;
            _tlnFlag = TLNFlag;
            _domainFlag = domainFlag;
            _record = record;
        }

        public ForestTrustCollisionType CollisionType
        {
            get
            {
                return _type;
            }
        }

        public TopLevelNameCollisionOptions TopLevelNameCollisionOption
        {
            get
            {
                return _tlnFlag;
            }
        }

        public DomainCollisionOptions DomainCollisionOption
        {
            get
            {
                return _domainFlag;
            }
        }

        public string CollisionRecord
        {
            get
            {
                return _record;
            }
        }
    }

    public class ForestTrustRelationshipCollisionCollection : ReadOnlyCollectionBase
    {
        internal ForestTrustRelationshipCollisionCollection() { }

        public ForestTrustRelationshipCollision this[int index]
        {
            get
            {
                return (ForestTrustRelationshipCollision)InnerList[index];
            }
        }

        public bool Contains(ForestTrustRelationshipCollision collision)
        {
            if (collision == null)
                throw new ArgumentNullException("collision");

            return InnerList.Contains(collision);
        }

        public int IndexOf(ForestTrustRelationshipCollision collision)
        {
            if (collision == null)
                throw new ArgumentNullException("collision");

            return InnerList.IndexOf(collision);
        }

        public void CopyTo(ForestTrustRelationshipCollision[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        internal int Add(ForestTrustRelationshipCollision collision)
        {
            return InnerList.Add(collision);
        }
    }
}
