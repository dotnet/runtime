// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions
{
    using System;
    using System.Security.AccessControl;
    using System.Security.Util;
    using System.IO;
    using System.Globalization;
    using System.Runtime.Serialization;

[Serializable]
    [Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    internal enum RegistryPermissionAccess
    {
        NoAccess = 0x00,
        Read = 0x01,
        Write = 0x02,
        Create = 0x04,
        AllAccess = 0x07,
    }
    
[System.Runtime.InteropServices.ComVisible(true)]
    [Serializable]
    sealed internal class RegistryPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
    {
        private StringExpressionSet m_read;
        private StringExpressionSet m_write;
        private StringExpressionSet m_create;
        [OptionalField(VersionAdded = 2)]
        private StringExpressionSet m_viewAcl;
        [OptionalField(VersionAdded = 2)]
        private StringExpressionSet m_changeAcl;
        private bool m_unrestricted;
    

        public RegistryPermission(PermissionState state)
        {
            if (state == PermissionState.Unrestricted)
            {
                m_unrestricted = true;
            }
            else if (state == PermissionState.None)
            {
                m_unrestricted = false;
            }
            else
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }
        
        public RegistryPermission( RegistryPermissionAccess access, String pathList )
        {
            SetPathList( access, pathList );
        }

        public void SetPathList( RegistryPermissionAccess access, String pathList )
        {
            VerifyAccess( access );

            m_unrestricted = false;

            if ((access & RegistryPermissionAccess.Read) != 0)
                m_read = null;
            
            if ((access & RegistryPermissionAccess.Write) != 0)
                m_write = null;
    
            if ((access & RegistryPermissionAccess.Create) != 0)
                m_create = null;
            
            AddPathList( access, pathList );
        }

        public void AddPathList( RegistryPermissionAccess access, String pathList )
        {
            AddPathList( access, AccessControlActions.None, pathList );
        }

        public void AddPathList( RegistryPermissionAccess access, AccessControlActions control, String pathList )
        {
            VerifyAccess( access );
            
            if ((access & RegistryPermissionAccess.Read) != 0)
            {
                if (m_read == null)
                    m_read = new StringExpressionSet();
                m_read.AddExpressions( pathList );
            }
            
            if ((access & RegistryPermissionAccess.Write) != 0)
            {
                if (m_write == null)
                    m_write = new StringExpressionSet();
                m_write.AddExpressions( pathList );
            }
    
            if ((access & RegistryPermissionAccess.Create) != 0)
            {
                if (m_create == null)
                    m_create = new StringExpressionSet();
                m_create.AddExpressions( pathList );
            }
        }
        
        private void VerifyAccess( RegistryPermissionAccess access )
        {
            if ((access & ~RegistryPermissionAccess.AllAccess) != 0)
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access));
        }
        
        private bool IsEmpty()
        {
            return (!m_unrestricted &&
                    (this.m_read == null || this.m_read.IsEmpty()) &&
                    (this.m_write == null || this.m_write.IsEmpty()) &&
                    (this.m_create == null || this.m_create.IsEmpty()) &&
                    (this.m_viewAcl == null || this.m_viewAcl.IsEmpty()) &&
                    (this.m_changeAcl == null || this.m_changeAcl.IsEmpty()));
        }
        
        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public bool IsUnrestricted()
        {
            return m_unrestricted;
        }
        
        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------
        
        public override bool IsSubsetOf(IPermission target)
        {
            if (target == null)
            {
                return this.IsEmpty();
            }

            RegistryPermission operand = target as RegistryPermission;
            if (operand == null)
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));

            if (operand.IsUnrestricted())
                return true;
            else if (this.IsUnrestricted())
                return false;
            else
                return ((this.m_read == null || this.m_read.IsSubsetOf( operand.m_read )) &&
                        (this.m_write == null || this.m_write.IsSubsetOf( operand.m_write )) &&
                        (this.m_create == null || this.m_create.IsSubsetOf( operand.m_create )) &&
                        (this.m_viewAcl == null || this.m_viewAcl.IsSubsetOf( operand.m_viewAcl )) &&
                        (this.m_changeAcl == null || this.m_changeAcl.IsSubsetOf( operand.m_changeAcl )));
        }
        
        public override IPermission Intersect(IPermission target)
        {
            if (target == null)
            {
                return null;
            }
            else if (!VerifyType(target))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            else if (this.IsUnrestricted())
            {
                return target.Copy();
            }
            
            RegistryPermission operand = (RegistryPermission)target;
            if (operand.IsUnrestricted())
            {
                return this.Copy();
            }
            
            
            StringExpressionSet intersectRead = this.m_read == null ? null : this.m_read.Intersect( operand.m_read );
            StringExpressionSet intersectWrite = this.m_write == null ? null : this.m_write.Intersect( operand.m_write );
            StringExpressionSet intersectCreate = this.m_create == null ? null : this.m_create.Intersect( operand.m_create );
            StringExpressionSet intersectViewAcl = this.m_viewAcl == null ? null : this.m_viewAcl.Intersect( operand.m_viewAcl );
            StringExpressionSet intersectChangeAcl = this.m_changeAcl == null ? null : this.m_changeAcl.Intersect( operand.m_changeAcl );
            
            if ((intersectRead == null || intersectRead.IsEmpty()) &&
                (intersectWrite == null || intersectWrite.IsEmpty()) &&
                (intersectCreate == null || intersectCreate.IsEmpty()) &&
                (intersectViewAcl == null || intersectViewAcl.IsEmpty()) &&
                (intersectChangeAcl == null || intersectChangeAcl.IsEmpty()))
            {
                return null;
            }
            
            RegistryPermission intersectPermission = new RegistryPermission(PermissionState.None);
            intersectPermission.m_unrestricted = false;
            intersectPermission.m_read = intersectRead;
            intersectPermission.m_write = intersectWrite;
            intersectPermission.m_create = intersectCreate;
            intersectPermission.m_viewAcl = intersectViewAcl;
            intersectPermission.m_changeAcl = intersectChangeAcl;
            
            return intersectPermission;
        }
        
        public override IPermission Union(IPermission other)
        {
            if (other == null)
            {
                return this.Copy();
            }
            else if (!VerifyType(other))
            {
                throw new 
                    ArgumentException(
                                    Environment.GetResourceString("Argument_WrongType", this.GetType().FullName)
                                     );
            }
            
            RegistryPermission operand = (RegistryPermission)other;
            
            if (this.IsUnrestricted() || operand.IsUnrestricted())
            {
                return new RegistryPermission( PermissionState.Unrestricted );
            }
    
            StringExpressionSet unionRead = this.m_read == null ? operand.m_read : this.m_read.Union( operand.m_read );
            StringExpressionSet unionWrite = this.m_write == null ? operand.m_write : this.m_write.Union( operand.m_write );
            StringExpressionSet unionCreate = this.m_create == null ? operand.m_create : this.m_create.Union( operand.m_create );
            StringExpressionSet unionViewAcl = this.m_viewAcl == null ? operand.m_viewAcl : this.m_viewAcl.Union( operand.m_viewAcl );
            StringExpressionSet unionChangeAcl = this.m_changeAcl == null ? operand.m_changeAcl : this.m_changeAcl.Union( operand.m_changeAcl );
            
            if ((unionRead == null || unionRead.IsEmpty()) &&
                (unionWrite == null || unionWrite.IsEmpty()) &&
                (unionCreate == null || unionCreate.IsEmpty()) &&
                (unionViewAcl == null || unionViewAcl.IsEmpty()) &&
                (unionChangeAcl == null || unionChangeAcl.IsEmpty()))
            {
                return null;
            }
            
            RegistryPermission unionPermission = new RegistryPermission(PermissionState.None);
            unionPermission.m_unrestricted = false;
            unionPermission.m_read = unionRead;
            unionPermission.m_write = unionWrite;
            unionPermission.m_create = unionCreate;
            unionPermission.m_viewAcl = unionViewAcl;
            unionPermission.m_changeAcl = unionChangeAcl;
            
            return unionPermission;
        }
            
        
        public override IPermission Copy()
        {
            RegistryPermission copy = new RegistryPermission(PermissionState.None);
            if (this.m_unrestricted)
            {
                copy.m_unrestricted = true;
            }
            else
            {
                copy.m_unrestricted = false;
                if (this.m_read != null)
                {
                    copy.m_read = this.m_read.Copy();
                }
                if (this.m_write != null)
                {
                    copy.m_write = this.m_write.Copy();
                }
                if (this.m_create != null)
                {
                    copy.m_create = this.m_create.Copy();
                }
                if (this.m_viewAcl != null)
                {
                    copy.m_viewAcl = this.m_viewAcl.Copy();
                }
                if (this.m_changeAcl != null)
                {
                    copy.m_changeAcl = this.m_changeAcl.Copy();
                }
            }
            return copy;   
        }

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex()
        {
            return RegistryPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex()
        {
            return BuiltInPermissionIndex.RegistryPermissionIndex;
        }

    }
}
