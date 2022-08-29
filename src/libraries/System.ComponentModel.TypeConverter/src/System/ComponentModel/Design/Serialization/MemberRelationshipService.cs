// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// A member relationship service is used by a serializer to announce that one
    /// property is related to a property on another object. Consider a code
    /// based serialization scheme where code is of the following form:
    ///
    /// object1.Property1 = object2.Property2
    ///
    /// Upon interpretation of this code, Property1 on object1 will be
    /// set to the return value of object2.Property2. But the relationship
    /// between these two objects is lost. Serialization schemes that
    /// wish to maintain this relationship may install a MemberRelationshipService
    /// into the serialization manager. When an object is deserialized
    /// this service will be notified of these relationships. It is up to the service
    /// to act on these notifications if it wishes. During serialization, the
    /// service is also consulted. If a relationship exists the same
    /// relationship is maintained by the serializer.
    /// </summary>
    public abstract class MemberRelationshipService
    {
        private readonly Dictionary<RelationshipEntry, RelationshipEntry> _relationships = new Dictionary<RelationshipEntry, RelationshipEntry>();

        /// <summary>
        /// Returns the current relationship associated with the source, or MemberRelationship.Empty if
        /// there is no relationship. Also sets a relationship between two objects. Empty
        /// can also be passed as the property value, in which case the relationship will
        /// be cleared.
        /// </summary>
        public MemberRelationship this[MemberRelationship source]
        {
            get
            {
                // The Owner and Member properties can be null if the MemberRelationship is constructed
                // using the default constructor. However there is no situation in which one is null
                // and not the other as the main constructor performs argument validation.
                if (source.Owner == null)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "source.Owner"), nameof(source));
                }

                Debug.Assert(source.Member != null);
                return GetRelationship(source);
            }
            set
            {
                // The Owner and Member properties can be null if the MemberRelationship is constructed
                // using the default constructor. However there is no situation in which one is null
                // and not the other as the main constructor performs argument validation.
                if (source.Owner == null)
                {
                    throw new ArgumentException(SR.Format(SR.InvalidNullArgument, "source.Owner"), nameof(source));
                }

                Debug.Assert(source.Member != null);
                SetRelationship(source, value);
            }
        }

        /// <summary>
        /// Returns the current relationship associated with the source, or null if there is no relationship.
        /// Also sets a relationship between two objects. Null can be passed as the property value, in which
        /// case the relationship will be cleared.
        /// </summary>
        public MemberRelationship this[object sourceOwner, MemberDescriptor sourceMember]
        {
            get
            {
                ArgumentNullException.ThrowIfNull(sourceOwner);
                ArgumentNullException.ThrowIfNull(sourceMember);

                return GetRelationship(new MemberRelationship(sourceOwner, sourceMember));
            }
            set
            {
                ArgumentNullException.ThrowIfNull(sourceOwner);
                ArgumentNullException.ThrowIfNull(sourceMember);

                SetRelationship(new MemberRelationship(sourceOwner, sourceMember), value);
            }
        }

        /// <summary>
        /// This is the implementation API for returning relationships. The default implementation stores the
        /// relationship in a table. Relationships are stored weakly, so they do not keep an object alive.
        /// </summary>
        protected virtual MemberRelationship GetRelationship(MemberRelationship source)
        {
            if (_relationships.TryGetValue(new RelationshipEntry(source), out RelationshipEntry retVal) && retVal._owner.IsAlive)
            {
                return new MemberRelationship(retVal._owner.Target!, retVal._member);
            }

            return MemberRelationship.Empty;
        }

        /// <summary>
        /// This is the implementation API for returning relationships. The default implementation stores the
        /// relationship in a table. Relationships are stored weakly, so they do not keep an object alive. Empty can be
        /// passed in for relationship to remove the relationship.
        /// </summary>
        protected virtual void SetRelationship(MemberRelationship source, MemberRelationship relationship)
        {
            if (!relationship.IsEmpty && !SupportsRelationship(source, relationship))
            {
                ThrowRelationshipNotSupported(source, relationship);
            }

            _relationships[new RelationshipEntry(source)] = new RelationshipEntry(relationship);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "GetComponentName is only used to create a nice exception message, and has a fallback when null is returned.")]
        private static void ThrowRelationshipNotSupported(MemberRelationship source, MemberRelationship relationship)
        {
            string? sourceName = TypeDescriptor.GetComponentName(source.Owner!) ?? source.Owner!.ToString();
            string? relName = TypeDescriptor.GetComponentName(relationship.Owner!) ?? relationship.Owner!.ToString();
            throw new ArgumentException(SR.Format(SR.MemberRelationshipService_RelationshipNotSupported, sourceName, source.Member.Name, relName, relationship.Member.Name));
        }

        /// <summary>
        /// Returns true if the provided relationship is supported.
        /// </summary>
        public abstract bool SupportsRelationship(MemberRelationship source, MemberRelationship relationship);

        /// <summary>
        /// Used as storage in our relationship table
        /// </summary>
        private struct RelationshipEntry : IEquatable<RelationshipEntry>
        {
            internal WeakReference _owner;
            internal MemberDescriptor _member;
            private readonly int _hashCode;

            internal RelationshipEntry(MemberRelationship rel)
            {
                _owner = new WeakReference(rel.Owner);
                _member = rel.Member;
                _hashCode = rel.Owner == null ? 0 : rel.Owner.GetHashCode();
            }

            public override bool Equals([NotNullWhen(true)] object? o)
            {
                Debug.Assert(o is RelationshipEntry, "This is only called indirectly from a dictionary only containing RelationshipEntry structs.");
                return Equals((RelationshipEntry)o);
            }

            public bool Equals(RelationshipEntry other)
            {
                object? owner1 = (_owner.IsAlive ? _owner.Target : null);
                object? owner2 = (other._owner.IsAlive ? other._owner.Target : null);
                return owner1 == owner2 && _member.Equals(other._member);
            }

            public static bool operator ==(RelationshipEntry re1, RelationshipEntry re2) => re1.Equals(re2);

            public static bool operator !=(RelationshipEntry re1, RelationshipEntry re2) => !re1.Equals(re2);

            public override int GetHashCode() => _hashCode;
        }
    }

    /// <summary>
    /// This class represents a single relationship between an object and a member.
    /// </summary>
    public readonly struct MemberRelationship : IEquatable<MemberRelationship>
    {
        public static readonly MemberRelationship Empty;

        /// <summary>
        /// Creates a new member relationship.
        /// </summary>
        public MemberRelationship(object owner, MemberDescriptor member)
        {
            ArgumentNullException.ThrowIfNull(owner);
            ArgumentNullException.ThrowIfNull(member);

            Owner = owner;
            Member = member;
        }

        /// <summary>
        /// Returns true if this relationship is empty.
        /// </summary>
        public bool IsEmpty => Owner == null;

        /// <summary>
        /// The member in this relationship.
        /// </summary>
        public MemberDescriptor Member { get; }

        /// <summary>
        /// The object owning the member.
        /// </summary>
        public object? Owner { get; }

        /// <summary>
        /// Infrastructure support to make this a first class struct
        /// </summary>
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is MemberRelationship rel && Equals(rel);

        /// <summary>
        /// Infrastructure support to make this a first class struct
        /// </summary>
        public bool Equals(MemberRelationship other) => other.Owner == Owner && other.Member == Member;

        /// <summary>
        /// Infrastructure support to make this a first class struct
        /// </summary>
        public override int GetHashCode() => Owner is null ? base.GetHashCode() : Owner.GetHashCode() ^ Member.GetHashCode();

        /// <summary>
        /// Infrastructure support to make this a first class struct
        /// </summary>
        public static bool operator ==(MemberRelationship left, MemberRelationship right) => left.Equals(right);

        /// <summary>
        /// Infrastructure support to make this a first class struct
        /// </summary>
        public static bool operator !=(MemberRelationship left, MemberRelationship right) => !left.Equals(right);
    }
}
