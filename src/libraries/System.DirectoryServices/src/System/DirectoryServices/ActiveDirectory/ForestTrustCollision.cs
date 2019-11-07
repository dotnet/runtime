// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;

namespace System.DirectoryServices.ActiveDirectory
{
    public class ForestTrustRelationshipCollision
    {
        internal ForestTrustRelationshipCollision(ForestTrustCollisionType collisionType, TopLevelNameCollisionOptions TLNFlag, DomainCollisionOptions domainFlag, string record)
        {
            CollisionType = collisionType;
            TopLevelNameCollisionOption = TLNFlag;
            DomainCollisionOption = domainFlag;
            CollisionRecord = record;
        }

        public ForestTrustCollisionType CollisionType { get; }

        public TopLevelNameCollisionOptions TopLevelNameCollisionOption { get; }

        public DomainCollisionOptions DomainCollisionOption { get; }

        public string CollisionRecord { get; }
    }

    public class ForestTrustRelationshipCollisionCollection : ReadOnlyCollectionBase
    {
        internal ForestTrustRelationshipCollisionCollection() { }

        public ForestTrustRelationshipCollision this[int index]
        {
            get => (ForestTrustRelationshipCollision)InnerList[index];
        }

        public bool Contains(ForestTrustRelationshipCollision collision)
        {
            if (collision == null)
                throw new ArgumentNullException(nameof(collision));

            return InnerList.Contains(collision);
        }

        public int IndexOf(ForestTrustRelationshipCollision collision)
        {
            if (collision == null)
                throw new ArgumentNullException(nameof(collision));

            return InnerList.IndexOf(collision);
        }

        public void CopyTo(ForestTrustRelationshipCollision[] array, int index)
        {
            InnerList.CopyTo(array, index);
        }

        internal int Add(ForestTrustRelationshipCollision collision) => InnerList.Add(collision);
    }
}
