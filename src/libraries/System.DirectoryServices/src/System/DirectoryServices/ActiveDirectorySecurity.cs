// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Security.AccessControl;
using System.Security.Principal;

namespace System.DirectoryServices
{
    [Flags]
    public enum ActiveDirectoryRights
    {
        Delete = 0x10000,
        ReadControl = 0x20000,
        WriteDacl = 0x40000,
        WriteOwner = 0x80000,
        Synchronize = 0x100000,
        AccessSystemSecurity = 0x1000000,
        GenericRead = ReadControl | ListChildren | ReadProperty | ListObject,
        GenericWrite = ReadControl | Self | WriteProperty,
        GenericExecute = ReadControl | ListChildren,
        GenericAll = Delete | ReadControl | WriteDacl | WriteOwner | CreateChild | DeleteChild | ListChildren | Self | ReadProperty | WriteProperty | DeleteTree | ListObject | ExtendedRight,
        CreateChild = 0x1,
        DeleteChild = 0x2,
        ListChildren = 0x4,
        Self = 0x8,
        ReadProperty = 0x10,
        WriteProperty = 0x20,
        DeleteTree = 0x40,
        ListObject = 0x80,
        ExtendedRight = 0x100
    }

    public enum ActiveDirectorySecurityInheritance
    {
        None = 0,
        All = 1,
        Descendents = 2,
        SelfAndChildren = 3,
        Children = 4
    }

    public enum PropertyAccess
    {
        Read = 0,
        Write = 1
    }

    public class ActiveDirectorySecurity : DirectoryObjectSecurity
    {
        private readonly SecurityMasks _securityMaskUsedInRetrieval = SecurityMasks.Owner | SecurityMasks.Group | SecurityMasks.Dacl | SecurityMasks.Sacl;

        #region Constructors

        public ActiveDirectorySecurity()
        {
        }

        internal ActiveDirectorySecurity(byte[] sdBinaryForm, SecurityMasks securityMask)
            : base(new CommonSecurityDescriptor(true, true, sdBinaryForm, 0))
        {
            _securityMaskUsedInRetrieval = securityMask;
        }

        #endregion

        #region Public methods

        //
        // DiscretionaryAcl related methods
        //

        public void AddAccessRule(ActiveDirectoryAccessRule rule)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            base.AddAccessRule(rule);
        }

        public void SetAccessRule(ActiveDirectoryAccessRule rule)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            base.SetAccessRule(rule);
        }

        public void ResetAccessRule(ActiveDirectoryAccessRule rule)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            base.ResetAccessRule(rule);
        }

        public void RemoveAccess(IdentityReference identity, AccessControlType type)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            //
            // Create a new rule
            //
            ActiveDirectoryAccessRule rule = new ActiveDirectoryAccessRule(
                                      identity,
                                      ActiveDirectoryRights.GenericRead, // will be ignored
                                      type,
                                      ActiveDirectorySecurityInheritance.None);

            base.RemoveAccessRuleAll(rule);
        }

        public bool RemoveAccessRule(ActiveDirectoryAccessRule rule)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            return base.RemoveAccessRule(rule);
        }

        public void RemoveAccessRuleSpecific(ActiveDirectoryAccessRule rule)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            base.RemoveAccessRuleSpecific(rule);
        }

        public override bool ModifyAccessRule(AccessControlModification modification, AccessRule rule, out bool modified)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            return base.ModifyAccessRule(modification, rule, out modified);
        }

        public override void PurgeAccessRules(IdentityReference identity)
        {
            if (!DaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifyDacl);
            }

            base.PurgeAccessRules(identity);
        }

        //
        // SystemAcl related methods
        //
        public void AddAuditRule(ActiveDirectoryAuditRule rule)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            base.AddAuditRule(rule);
        }

        public void SetAuditRule(ActiveDirectoryAuditRule rule)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            base.SetAuditRule(rule);
        }

        public void RemoveAudit(IdentityReference identity)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            //
            // Create a new rule
            //
            ActiveDirectoryAuditRule rule = new ActiveDirectoryAuditRule(
                                     identity,
                                     ActiveDirectoryRights.GenericRead, // will be ignored
                                     AuditFlags.Success | AuditFlags.Failure,
                                     ActiveDirectorySecurityInheritance.None);

            base.RemoveAuditRuleAll(rule);
        }

        public bool RemoveAuditRule(ActiveDirectoryAuditRule rule)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            return base.RemoveAuditRule(rule);
        }

        public void RemoveAuditRuleSpecific(ActiveDirectoryAuditRule rule)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            base.RemoveAuditRuleSpecific(rule);
        }

        public override bool ModifyAuditRule(AccessControlModification modification, AuditRule rule, out bool modified)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            return base.ModifyAuditRule(modification, rule, out modified);
        }

        public override void PurgeAuditRules(IdentityReference identity)
        {
            if (!SaclRetrieved())
            {
                throw new InvalidOperationException(SR.CannotModifySacl);
            }

            base.PurgeAuditRules(identity);
        }

        #endregion

        #region Factories

        public sealed override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type)
        {
            return new ActiveDirectoryAccessRule(
                identityReference,
                accessMask,
                type,
                Guid.Empty,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                Guid.Empty);
        }

        public sealed override AccessRule AccessRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AccessControlType type,
            Guid objectGuid,
            Guid inheritedObjectGuid)
        {
            return new ActiveDirectoryAccessRule(
                identityReference,
                accessMask,
                type,
                objectGuid,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                inheritedObjectGuid);
        }

        public sealed override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags)
        {
            return new ActiveDirectoryAuditRule(
                identityReference,
                accessMask,
                flags,
                Guid.Empty,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                Guid.Empty);
        }

        public sealed override AuditRule AuditRuleFactory(
            IdentityReference identityReference,
            int accessMask,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            AuditFlags flags,
            Guid objectGuid,
            Guid inheritedObjectGuid)
        {
            return new ActiveDirectoryAuditRule(
                identityReference,
                accessMask,
                flags,
                objectGuid,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                inheritedObjectGuid);
        }

        internal bool IsModified()
        {
            ReadLock();

            try
            {
                return (OwnerModified || GroupModified || AccessRulesModified || AuditRulesModified);
            }
            finally
            {
                ReadUnlock();
            }
        }

        private bool DaclRetrieved()
        {
            return ((_securityMaskUsedInRetrieval & SecurityMasks.Dacl) != 0);
        }

        private bool SaclRetrieved()
        {
            return ((_securityMaskUsedInRetrieval & SecurityMasks.Sacl) != 0);
        }

        #endregion

        #region some overrides

        public override Type AccessRightType => typeof(ActiveDirectoryRights);

        public override Type AccessRuleType => typeof(ActiveDirectoryAccessRule);

        public override Type AuditRuleType => typeof(ActiveDirectoryAuditRule);

        #endregion

    }

    internal sealed class ActiveDirectoryRightsTranslator
    {
        #region Access mask to rights translation

        internal static int AccessMaskFromRights(ActiveDirectoryRights adRights) => (int)adRights;

        internal static ActiveDirectoryRights RightsFromAccessMask(int accessMask)
        {
            return (ActiveDirectoryRights)accessMask;
        }

        #endregion
    }

    internal sealed class PropertyAccessTranslator
    {
        #region PropertyAccess to access mask translation

        internal static int AccessMaskFromPropertyAccess(PropertyAccess access)
        {
            int accessMask = 0;

            if (access < PropertyAccess.Read || access > PropertyAccess.Write)
            {
                throw new InvalidEnumArgumentException(nameof(access), (int)access, typeof(PropertyAccess));
            }

            switch (access)
            {
                case PropertyAccess.Read:
                    {
                        accessMask = ActiveDirectoryRightsTranslator.AccessMaskFromRights(ActiveDirectoryRights.ReadProperty);
                        break;
                    }

                case PropertyAccess.Write:
                    {
                        accessMask = ActiveDirectoryRightsTranslator.AccessMaskFromRights(ActiveDirectoryRights.WriteProperty);
                        break;
                    }

                default:

                    //
                    // This should not happen. Indicates a problem with the
                    // internal logic.
                    //
                    Debug.Fail("Invalid PropertyAccess value");
                    throw new ArgumentException(nameof(access));
            }
            return accessMask;
        }

        #endregion
    }

    internal sealed class ActiveDirectoryInheritanceTranslator
    {
        #region ActiveDirectorySecurityInheritance to Inheritance/Propagation flags translation

        //
        //  InheritanceType     InheritanceFlags        PropagationFlags
        // ------------------------------------------------------------------------------
        //  None                None                    None
        //  All                 ContainerInherit        None
        //  Descendents         ContainerInherit        InheritOnly
        //  SelfAndChildren     ContainerInherit        NoPropagateInherit
        //  Children            ContainerInherit        InheritOnly | NoPropagateInherit
        //
        internal static InheritanceFlags[] ITToIF = new InheritanceFlags[] {
            InheritanceFlags.None,
            InheritanceFlags.ContainerInherit,
            InheritanceFlags.ContainerInherit,
            InheritanceFlags.ContainerInherit,
            InheritanceFlags.ContainerInherit
        };

        internal static PropagationFlags[] ITToPF = new PropagationFlags[] {
            PropagationFlags.None,
            PropagationFlags.None,
            PropagationFlags.InheritOnly,
            PropagationFlags.NoPropagateInherit,
            PropagationFlags.InheritOnly | PropagationFlags.NoPropagateInherit
        };

        internal static InheritanceFlags GetInheritanceFlags(ActiveDirectorySecurityInheritance inheritanceType)
        {
            if (inheritanceType < ActiveDirectorySecurityInheritance.None || inheritanceType > ActiveDirectorySecurityInheritance.Children)
            {
                throw new InvalidEnumArgumentException(nameof(inheritanceType), (int)inheritanceType, typeof(ActiveDirectorySecurityInheritance));
            }

            return ITToIF[(int)inheritanceType];
        }

        internal static PropagationFlags GetPropagationFlags(ActiveDirectorySecurityInheritance inheritanceType)
        {
            if (inheritanceType < ActiveDirectorySecurityInheritance.None || inheritanceType > ActiveDirectorySecurityInheritance.Children)
            {
                throw new InvalidEnumArgumentException(nameof(inheritanceType), (int)inheritanceType, typeof(ActiveDirectorySecurityInheritance));
            }

            return ITToPF[(int)inheritanceType];
        }

        internal static ActiveDirectorySecurityInheritance GetEffectiveInheritanceFlags(InheritanceFlags inheritanceFlags, PropagationFlags propagationFlags)
        {
            ActiveDirectorySecurityInheritance inheritanceType = ActiveDirectorySecurityInheritance.None;

            if ((inheritanceFlags & InheritanceFlags.ContainerInherit) != 0)
            {
                switch (propagationFlags)
                {
                    case PropagationFlags.None:
                        {
                            inheritanceType = ActiveDirectorySecurityInheritance.All;
                            break;
                        }

                    case PropagationFlags.InheritOnly:
                        {
                            inheritanceType = ActiveDirectorySecurityInheritance.Descendents;
                            break;
                        }

                    case PropagationFlags.NoPropagateInherit:
                        {
                            inheritanceType = ActiveDirectorySecurityInheritance.SelfAndChildren;
                            break;
                        }

                    case PropagationFlags.InheritOnly | PropagationFlags.NoPropagateInherit:
                        {
                            inheritanceType = ActiveDirectorySecurityInheritance.Children;
                            break;
                        }

                    default:

                        //
                        // This should not happen. Indicates a problem with the
                        // internal logic.
                        //
                        Debug.Fail("Invalid PropagationFlags value");
                        throw new ArgumentException(nameof(propagationFlags));
                }
            }

            return inheritanceType;
        }

        #endregion
    }

    public class ActiveDirectoryAccessRule : ObjectAccessRule
    {
        #region Constructors

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                Guid.Empty,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty
                )
        {
        }

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type,
            Guid objectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                objectType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type,
            Guid objectType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                objectType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public ActiveDirectoryAccessRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AccessControlType type,
            Guid objectType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                type,
                objectType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        internal ActiveDirectoryAccessRule(
            IdentityReference identity,
            int accessMask,
            AccessControlType type,
            Guid objectType,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            Guid inheritedObjectType
            )
            : base(identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                objectType,
                inheritedObjectType,
                type)
        {
        }

        #endregion constructors

        #region Public properties

        public ActiveDirectoryRights ActiveDirectoryRights
        {
            get => ActiveDirectoryRightsTranslator.RightsFromAccessMask(base.AccessMask);
        }

        public ActiveDirectorySecurityInheritance InheritanceType
        {
            get => ActiveDirectoryInheritanceTranslator.GetEffectiveInheritanceFlags(InheritanceFlags, PropagationFlags);
        }

        #endregion
    }

    public sealed class ListChildrenAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public ListChildrenAccessRule(
            IdentityReference identity,
            AccessControlType type)
            : base(
                identity,
                (int)ActiveDirectoryRights.ListChildren,
                type,
                Guid.Empty,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty
                )
        {
        }

        public ListChildrenAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ListChildren,
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ListChildrenAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ListChildren,
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public sealed class CreateChildAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public CreateChildAccessRule(
             IdentityReference identity,
             AccessControlType type)
             : base(
                 identity,
                 (int)ActiveDirectoryRights.CreateChild,
                 type,
                 Guid.Empty, // all child objects
                 false,
                 InheritanceFlags.None,
                 PropagationFlags.None,
                 Guid.Empty)
        {
        }

        public CreateChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid childType)
            : base(
                identity,
                (int)ActiveDirectoryRights.CreateChild,
                type,
                childType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public CreateChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.CreateChild,
                type,
                Guid.Empty, // all child objects
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public CreateChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid childType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.CreateChild,
                type,
                childType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public CreateChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.CreateChild,
                type,
                Guid.Empty, // all child objects
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public CreateChildAccessRule(
            IdentityReference identity, AccessControlType type,
            Guid childType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.CreateChild,
                type,
                childType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public sealed class DeleteChildAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public DeleteChildAccessRule(
            IdentityReference identity,
            AccessControlType type)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                Guid.Empty, // all child objects
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public DeleteChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid childType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                childType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public DeleteChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                Guid.Empty, // all child objects
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public DeleteChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid childType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                childType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public DeleteChildAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                Guid.Empty, // all child objects
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public DeleteChildAccessRule(
            IdentityReference identity, AccessControlType type,
            Guid childType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteChild,
                type,
                childType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }
        #endregion constructors
    }

    public sealed class PropertyAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                Guid.Empty, // all properties
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertyType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertyType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                Guid.Empty, // all properties
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertyType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertyType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                Guid.Empty, // all properties
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public PropertyAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertyType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertyType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public sealed class PropertySetAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public PropertySetAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertySetType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertySetType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public PropertySetAccessRule(
            IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertySetType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertySetType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public PropertySetAccessRule(IdentityReference identity,
            AccessControlType type,
            PropertyAccess access,
            Guid propertySetType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)PropertyAccessTranslator.AccessMaskFromPropertyAccess(access),
                type,
                propertySetType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public sealed class ExtendedRightAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public ExtendedRightAccessRule(
            IdentityReference identity,
            AccessControlType type)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                Guid.Empty, // all extended rights
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public ExtendedRightAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid extendedRightType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                extendedRightType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public ExtendedRightAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                Guid.Empty, // all extended rights
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ExtendedRightAccessRule(
            IdentityReference identity,
            AccessControlType type,
            Guid extendedRightType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                extendedRightType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ExtendedRightAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                Guid.Empty, // all extended rights
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public ExtendedRightAccessRule(IdentityReference identity,
            AccessControlType type,
            Guid extendedRightType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.ExtendedRight,
                type,
                extendedRightType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public sealed class DeleteTreeAccessRule : ActiveDirectoryAccessRule
    {
        #region Constructors

        public DeleteTreeAccessRule(
            IdentityReference identity,
            AccessControlType type)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteTree,
                type,
                Guid.Empty,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public DeleteTreeAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteTree,
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public DeleteTreeAccessRule(
            IdentityReference identity,
            AccessControlType type,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : base(
                identity,
                (int)ActiveDirectoryRights.DeleteTree,
                type,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        #endregion constructors
    }

    public class ActiveDirectoryAuditRule : ObjectAuditRule
    {
        #region Constructors

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                Guid.Empty,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty
                )
        {
        }

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags,
            Guid objectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                objectType,
                false,
                InheritanceFlags.None,
                PropagationFlags.None,
                Guid.Empty)
        {
        }

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags,
            ActiveDirectorySecurityInheritance inheritanceType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags,
            Guid objectType,
            ActiveDirectorySecurityInheritance inheritanceType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                objectType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                Guid.Empty)
        {
        }

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                Guid.Empty,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        public ActiveDirectoryAuditRule(
            IdentityReference identity,
            ActiveDirectoryRights adRights,
            AuditFlags auditFlags,
            Guid objectType,
            ActiveDirectorySecurityInheritance inheritanceType,
            Guid inheritedObjectType)
            : this(
                identity,
                ActiveDirectoryRightsTranslator.AccessMaskFromRights(adRights),
                auditFlags,
                objectType,
                false,
                ActiveDirectoryInheritanceTranslator.GetInheritanceFlags(inheritanceType),
                ActiveDirectoryInheritanceTranslator.GetPropagationFlags(inheritanceType),
                inheritedObjectType)
        {
        }

        internal ActiveDirectoryAuditRule(
            IdentityReference identity,
            int accessMask,
            AuditFlags auditFlags,
            Guid objectGuid,
            bool isInherited,
            InheritanceFlags inheritanceFlags,
            PropagationFlags propagationFlags,
            Guid inheritedObjectType
            )
            : base(identity,
                accessMask,
                isInherited,
                inheritanceFlags,
                propagationFlags,
                objectGuid,
                inheritedObjectType,
                auditFlags)
        {
        }

        #endregion constructors

        #region Public properties

        public ActiveDirectoryRights ActiveDirectoryRights
        {
            get => ActiveDirectoryRightsTranslator.RightsFromAccessMask(AccessMask);
        }

        public ActiveDirectorySecurityInheritance InheritanceType
        {
            get => ActiveDirectoryInheritanceTranslator.GetEffectiveInheritanceFlags(InheritanceFlags, PropagationFlags);
        }

        #endregion
    }
}
