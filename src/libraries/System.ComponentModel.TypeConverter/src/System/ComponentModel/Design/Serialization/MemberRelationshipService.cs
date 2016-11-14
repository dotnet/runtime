// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Permissions;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    ///    A member relationship service is used by a serializer to announce that one
    ///    property is related to a property on another object. Consider a code
    ///    based serialization scheme where code is of the following form:
    /// 
    ///    object1.Property1 = object2.Property2
    /// 
    ///    Upon interpretation of this code, Property1 on object1 will be
    ///    set to the return value of object2.Property2.  But the relationship
    ///    between these two objects is lost.  Serialization schemes that
    ///    wish to maintain this relationship may install a MemberRelationshipService
    ///    into the serialization manager.  When an object is deserialized
    ///    this serivce will be notified of these relationships.  It is up to the service
    ///    to act on these notifications if it wishes.  During serialization, the
    ///    service is also consulted.  If a relationship exists the same
    ///    relationship is maintained by the serializer.
    /// </summary>
    public abstract class MemberRelationshipService
    {
        private Dictionary<RelationshipEntry, RelationshipEntry> _relationships = new Dictionary<RelationshipEntry, RelationshipEntry>();

        /// <summary>
        ///    Returns the the current relationship associated with the source, or MemberRelationship.Empty if
        ///    there is no relationship.  Also sets a relationship between two objects.  Empty
        ///    can also be passed as the property value, in which case the relationship will
        ///    be cleared.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1043:UseIntegralOrStringArgumentForIndexers")]
        public MemberRelationship this[MemberRelationship source]
        {
            get
            {
                if (source.Owner == null) throw new ArgumentNullException("Owner");
                if (source.Member == null) throw new ArgumentNullException("Member");

                return GetRelationship(source);
            }
            set
            {
                if (source.Owner == null) throw new ArgumentNullException("Owner");
                if (source.Member == null) throw new ArgumentNullException("Member");

                SetRelationship(source, value);
            }
        }

        /// <summary>
        ///    Returns the the current relationship associated with the source, or null if
        ///    there is no relationship.  Also sets a relationship between two objects.  Null
        ///    can be passed as the property value, in which case the relationship will
        ///    be cleared.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1023:IndexersShouldNotBeMultidimensional")]
        public MemberRelationship this[object sourceOwner, MemberDescriptor sourceMember]
        {
            get
            {
                if (sourceOwner == null) throw new ArgumentNullException(nameof(sourceOwner));
                if (sourceMember == null) throw new ArgumentNullException(nameof(sourceMember));

                return GetRelationship(new MemberRelationship(sourceOwner, sourceMember));
            }
            set
            {
                if (sourceOwner == null) throw new ArgumentNullException(nameof(sourceOwner));
                if (sourceMember == null) throw new ArgumentNullException(nameof(sourceMember));

                SetRelationship(new MemberRelationship(sourceOwner, sourceMember), value);
            }
        }

        /// <summary>
        ///    This is the implementation API for returning relationships.  The default implementation stores the 
        ///    relationship in a table.  Relationships are stored weakly, so they do not keep an object alive.
        /// </summary>
        protected virtual MemberRelationship GetRelationship(MemberRelationship source)
        {
            RelationshipEntry retVal;

            if (_relationships != null && _relationships.TryGetValue(new RelationshipEntry(source), out retVal) && retVal.Owner.IsAlive)
            {
                return new MemberRelationship(retVal.Owner.Target, retVal.Member);
            }

            return MemberRelationship.Empty;
        }

        /// <summary>
        ///    This is the implementation API for returning relationships.  The default implementation stores the 
        ///    relationship in a table.  Relationships are stored weakly, so they do not keep an object alive.  Empty can be
        ///    passed in for relationship to remove the relationship.
        /// </summary>
        protected virtual void SetRelationship(MemberRelationship source, MemberRelationship relationship)
        {
            if (!relationship.IsEmpty && !SupportsRelationship(source, relationship))
            {
                string sourceName = TypeDescriptor.GetComponentName(source.Owner);
                string relName = TypeDescriptor.GetComponentName(relationship.Owner);
                if (sourceName == null)
                {
                    sourceName = source.Owner.ToString();
                }
                if (relName == null)
                {
                    relName = relationship.Owner.ToString();
                }
                throw new ArgumentException(SR.Format(SR.MemberRelationshipService_RelationshipNotSupported, sourceName, source.Member.Name, relName, relationship.Member.Name));
            }

            if (_relationships == null)
            {
                _relationships = new Dictionary<RelationshipEntry, RelationshipEntry>();
            }

            _relationships[new RelationshipEntry(source)] = new RelationshipEntry(relationship);
        }

        /// <summary>
        ///    Returns true if the provided relatinoship is supported.
        /// </summary>
        public abstract bool SupportsRelationship(MemberRelationship source, MemberRelationship relationship);

        /// <summary>
        ///    Used as storage in our relationship table
        /// </summary>
        private struct RelationshipEntry
        {
            internal WeakReference Owner;
            internal MemberDescriptor Member;
            private int _hashCode;

            internal RelationshipEntry(MemberRelationship rel)
            {
                Owner = new WeakReference(rel.Owner);
                Member = rel.Member;
                _hashCode = rel.Owner == null ? 0 : rel.Owner.GetHashCode();
            }


            public override bool Equals(object o)
            {
                if (o is RelationshipEntry)
                {
                    RelationshipEntry e = (RelationshipEntry)o;
                    return this == e;
                }

                return false;
            }

            public static bool operator ==(RelationshipEntry re1, RelationshipEntry re2)
            {
                object owner1 = (re1.Owner.IsAlive ? re1.Owner.Target : null);
                object owner2 = (re2.Owner.IsAlive ? re2.Owner.Target : null);
                return owner1 == owner2 && re1.Member.Equals(re2.Member);
            }

            public static bool operator !=(RelationshipEntry re1, RelationshipEntry re2)
            {
                return !(re1 == re2);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }
    }

    /// <summary>
    ///    This class represents a single relationship between an object and a member.
    /// </summary>
    public struct MemberRelationship
    {
        private object _owner;
        private MemberDescriptor _member;

        public static readonly MemberRelationship Empty = new MemberRelationship();

        /// <summary>
        ///    Creates a new member relationship.
        /// </summary>
        public MemberRelationship(object owner, MemberDescriptor member)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (member == null) throw new ArgumentNullException(nameof(member));

            _owner = owner;
            _member = member;
        }

        /// <summary>
        ///    Returns true if this relationship is empty.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _owner == null;
            }
        }

        /// <summary>
        ///    The member in this relationship.
        /// </summary>
        public MemberDescriptor Member
        {
            get
            {
                return _member;
            }
        }

        /// <summary>
        ///    The object owning the member.
        /// </summary>
        public object Owner
        {
            get
            {
                return _owner;
            }
        }

        /// <summary>
        ///    Infrastructure support to make this a first class struct
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!(obj is MemberRelationship))
                return false;

            MemberRelationship rel = (MemberRelationship)obj;
            return rel.Owner == Owner && rel.Member == Member;
        }

        /// <summary>
        ///    Infrastructure support to make this a first class struct
        /// </summary>
        public override int GetHashCode()
        {
            if (_owner == null) return base.GetHashCode();
            return _owner.GetHashCode() ^ _member.GetHashCode();
        }
        /// <summary>
        ///    Infrastructure support to make this a first class struct
        /// </summary>
        public static bool operator ==(MemberRelationship left, MemberRelationship right)
        {
            return left.Owner == right.Owner && left.Member == right.Member;
        }

        /// <summary>
        ///    Infrastructure support to make this a first class struct
        /// </summary>
        public static bool operator !=(MemberRelationship left, MemberRelationship right)
        {
            return !(left == right);
        }
    }
}
