// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Principal;

namespace System.Security.AccessControl
{

    public enum AccessControlType
    {
        Allow = 0,
        Deny = 1,
    }


    public abstract class AuthorizationRule
    {
        #region Private Members

        private readonly IdentityReference _identity;
        private readonly int _accessMask;
        private readonly bool _isInherited;
        private readonly InheritanceFlags _inheritanceFlags;
        private readonly PropagationFlags _propagationFlags;

        #endregion

        #region Constructors

        protected internal AuthorizationRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags)
        {
            ArgumentNullException.ThrowIfNull(identity);

            if (accessMask == 0)
            {
                throw new ArgumentException(SR.Argument_ArgumentZero, nameof(accessMask));
            }

            if (inheritanceFlags < InheritanceFlags.None || inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inheritanceFlags), SR.Format(SR.Argument_InvalidEnumValue, inheritanceFlags, "InheritanceFlags"));
            }

            if (propagationFlags < PropagationFlags.None || propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(propagationFlags), SR.Format(SR.Argument_InvalidEnumValue, inheritanceFlags, "PropagationFlags"));
            }

            if (identity.IsValidTargetType(typeof(SecurityIdentifier)) == false)
            {
                throw new ArgumentException(
                    SR.Arg_MustBeIdentityReferenceType,
                    nameof(identity));
            }

            _identity = identity;
            _accessMask = accessMask;
            _isInherited = isInherited;
            _inheritanceFlags = inheritanceFlags;

            if (inheritanceFlags != 0)
            {
                _propagationFlags = propagationFlags;
            }
            else
            {
                _propagationFlags = 0;
            }
        }

        #endregion

        #region Properties

        public IdentityReference IdentityReference
        {
            get { return _identity; }
        }

        protected internal int AccessMask
        {
            get { return _accessMask; }
        }

        public bool IsInherited
        {
            get { return _isInherited; }
        }

        public InheritanceFlags InheritanceFlags
        {
            get { return _inheritanceFlags; }
        }

        public PropagationFlags PropagationFlags
        {
            get { return _propagationFlags; }
        }

        #endregion
    }


    public abstract class AccessRule : AuthorizationRule
    {
        #region Private Methods

        private readonly AccessControlType _type;

        #endregion

        #region Constructors

        protected AccessRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
        {
            if (type != AccessControlType.Allow &&
                type != AccessControlType.Deny)
            {
                throw new ArgumentOutOfRangeException(nameof(type), SR.ArgumentOutOfRange_Enum);
            }

            if (inheritanceFlags < InheritanceFlags.None || inheritanceFlags > (InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(inheritanceFlags), SR.Format(SR.Argument_InvalidEnumValue, inheritanceFlags, "InheritanceFlags"));
            }

            if (propagationFlags < PropagationFlags.None || propagationFlags > (PropagationFlags.NoPropagateInherit | PropagationFlags.InheritOnly))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(propagationFlags), SR.Format(SR.Argument_InvalidEnumValue, inheritanceFlags, "PropagationFlags"));
            }

            _type = type;
        }

        #endregion

        #region Properties

        public AccessControlType AccessControlType
        {
            get { return _type; }
        }

        #endregion
    }


    public abstract class ObjectAccessRule : AccessRule
    {
        #region Private Members

        private readonly Guid _objectType;
        private readonly Guid _inheritedObjectType;
        private readonly ObjectAceFlags _objectFlags = ObjectAceFlags.None;

        #endregion

        #region Constructors

        protected ObjectAccessRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AccessControlType type)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, type)
        {
            if ((!objectType.Equals(Guid.Empty)) && ((accessMask & ObjectAce.AccessMaskWithObjectType) != 0))
            {
                _objectType = objectType;
                _objectFlags |= ObjectAceFlags.ObjectAceTypePresent;
            }
            else
            {
                _objectType = Guid.Empty;
            }

            if ((!inheritedObjectType.Equals(Guid.Empty)) && ((inheritanceFlags & InheritanceFlags.ContainerInherit) != 0))
            {
                _inheritedObjectType = inheritedObjectType;
                _objectFlags |= ObjectAceFlags.InheritedObjectAceTypePresent;
            }
            else
            {
                _inheritedObjectType = Guid.Empty;
            }
        }

        #endregion

        #region Properties

        public Guid ObjectType
        {
            get { return _objectType; }
        }

        public Guid InheritedObjectType
        {
            get { return _inheritedObjectType; }
        }

        public ObjectAceFlags ObjectFlags
        {
            get { return _objectFlags; }
        }

        #endregion
    }


    public abstract class AuditRule : AuthorizationRule
    {
        #region Private Members

        private readonly AuditFlags _flags;

        #endregion

        #region Constructors

        protected AuditRule(
            IdentityReference identity,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags auditFlags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags)
        {
            if (auditFlags == AuditFlags.None)
            {
                throw new ArgumentException(SR.Arg_EnumAtLeastOneFlag, nameof(auditFlags));
            }
            else if ((auditFlags & ~(AuditFlags.Success | AuditFlags.Failure)) != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(auditFlags), SR.ArgumentOutOfRange_Enum);
            }

            _flags = auditFlags;
        }

        #endregion

        #region Public Properties

        public AuditFlags AuditFlags
        {
            get { return _flags; }
        }

        #endregion
    }


    public abstract class ObjectAuditRule : AuditRule
    {
        #region Private Members

        private readonly Guid _objectType;
        private readonly Guid _inheritedObjectType;
        private readonly ObjectAceFlags _objectFlags = ObjectAceFlags.None;

        #endregion

        #region Constructors

        protected ObjectAuditRule(IdentityReference identity, int accessMask, bool isInherited, InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags, Guid objectType, Guid inheritedObjectType, AuditFlags auditFlags)
            : base(identity, accessMask, isInherited, inheritanceFlags, propagationFlags, auditFlags)
        {

            if ((!objectType.Equals(Guid.Empty)) && ((accessMask & ObjectAce.AccessMaskWithObjectType) != 0))
            {
                _objectType = objectType;
                _objectFlags |= ObjectAceFlags.ObjectAceTypePresent;
            }
            else
            {
                _objectType = Guid.Empty;
            }

            if ((!inheritedObjectType.Equals(Guid.Empty)) && ((inheritanceFlags & InheritanceFlags.ContainerInherit) != 0))
            {
                _inheritedObjectType = inheritedObjectType;
                _objectFlags |= ObjectAceFlags.InheritedObjectAceTypePresent;
            }
            else
            {
                _inheritedObjectType = Guid.Empty;
            }
        }

        #endregion

        #region Public Properties

        public Guid ObjectType
        {
            get { return _objectType; }
        }

        public Guid InheritedObjectType
        {
            get { return _inheritedObjectType; }
        }

        public ObjectAceFlags ObjectFlags
        {
            get { return _objectFlags; }
        }


        #endregion
    }


    public sealed class AuthorizationRuleCollection : ReadOnlyCollectionBase
    {
        #region Constructors

        public AuthorizationRuleCollection()
            : base()
        {
        }

        #endregion

        #region Public methods

        public void AddRule(AuthorizationRule? rule)
        {
            InnerList.Add(rule);
        }

        #endregion

        #region ICollection Members

        public void CopyTo(AuthorizationRule[] rules, int index)
        {
            ((ICollection)this).CopyTo(rules, index);
        }

        #endregion

        #region Public properties

        public AuthorizationRule? this[int index]
        {
            get { return InnerList[index] as AuthorizationRule; }
        }

        #endregion
    }
}
