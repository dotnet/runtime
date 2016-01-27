// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Permissions {
    using System;
    using System.Text;
    using System.Security;
    using System.Security.Util;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Collections;
    using System.Globalization;
    using System.Diagnostics.Contracts;

[Serializable]
[Flags]
[System.Runtime.InteropServices.ComVisible(true)]
    public enum FileDialogPermissionAccess {
        None = 0x00,
    
        Open = 0x01,
    
        Save = 0x02,
        
        OpenSave = Open | Save
    
    }

    [Serializable] 
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class FileDialogPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission {
        FileDialogPermissionAccess access;

        public FileDialogPermission(PermissionState state) {
            if (state == PermissionState.Unrestricted) {
                SetUnrestricted(true);
            }
            else if (state == PermissionState.None) {
                SetUnrestricted(false);
                Reset();
            }
            else {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
            }
        }    

        public FileDialogPermission(FileDialogPermissionAccess access) {
            VerifyAccess(access);
            this.access = access;
        }

        public FileDialogPermissionAccess Access {
            get {
                return access;
            }

            set {
                VerifyAccess(value);
                access = value;
            }
        }

        public override IPermission Copy() {
            return new FileDialogPermission(this.access);
        }

#if FEATURE_CAS_POLICY
        public override void FromXml(SecurityElement esd) {
            CodeAccessPermission.ValidateElement(esd, this);
            if (XMLUtil.IsUnrestricted(esd)) {
                SetUnrestricted(true);
                return;
            }

            access = FileDialogPermissionAccess.None;

            string accessXml = esd.Attribute("Access");
            if (accessXml != null)
                access = (FileDialogPermissionAccess)Enum.Parse(typeof(FileDialogPermissionAccess), accessXml);
        }
#endif // FEATURE_CAS_POLICY

        /// <internalonly/>
        int IBuiltInPermission.GetTokenIndex() {
            return FileDialogPermission.GetTokenIndex();
        }

        internal static int GetTokenIndex() {
            return BuiltInPermissionIndex.FileDialogPermissionIndex;
        }

        public override IPermission Intersect(IPermission target) {
            if (target == null) {
                return null;
            }
            else if (!VerifyType(target)) {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            FileDialogPermission operand = (FileDialogPermission)target;

            FileDialogPermissionAccess intersectAccess = access & operand.Access;

            if (intersectAccess == FileDialogPermissionAccess.None)
                return null;
            else
                return new FileDialogPermission(intersectAccess);
        }

        public override bool IsSubsetOf(IPermission target) {
            if (target == null) {
                // Only safe subset if this is empty
                return access == FileDialogPermissionAccess.None;
            }

            try {
                FileDialogPermission operand = (FileDialogPermission)target;
                if (operand.IsUnrestricted()) {
                    return true;
                }
                else if (this.IsUnrestricted()) {
                    return false;
                }
                else {
                    int open = (int)(access & FileDialogPermissionAccess.Open);
                    int save = (int)(access & FileDialogPermissionAccess.Save);
                    int openTarget = (int)(operand.Access & FileDialogPermissionAccess.Open);
                    int saveTarget = (int)(operand.Access & FileDialogPermissionAccess.Save);

                    return open <= openTarget && save <= saveTarget;
                }
            }
            catch (InvalidCastException) {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

        }

        public bool IsUnrestricted() {
            return access == FileDialogPermissionAccess.OpenSave;
        }

        void Reset() {
            access = FileDialogPermissionAccess.None;
        }

        void SetUnrestricted( bool unrestricted ) {
            if (unrestricted) {
                access = FileDialogPermissionAccess.OpenSave;
            }
        }

#if FEATURE_CAS_POLICY
        public override SecurityElement ToXml() {
            SecurityElement esd = CodeAccessPermission.CreatePermissionElement( this, "System.Security.Permissions.FileDialogPermission" );
            if (!IsUnrestricted()) {
                if (access != FileDialogPermissionAccess.None) {
                    esd.AddAttribute("Access", Enum.GetName(typeof(FileDialogPermissionAccess), access));
                }
            }
            else {
                esd.AddAttribute("Unrestricted", "true");
            }
            return esd;
        }
#endif // FEATURE_CAS_POLICY

        public override IPermission Union(IPermission target) {
            if (target == null) {
                return this.Copy();
            }
            else if (!VerifyType(target)) {
                throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", this.GetType().FullName));
            }

            FileDialogPermission operand = (FileDialogPermission)target;
            return new FileDialogPermission(access | operand.Access);
        }        

        static void VerifyAccess(FileDialogPermissionAccess access) {
            if ((access & ~FileDialogPermissionAccess.OpenSave) != 0 ) {
                throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", (int)access));
            }
            Contract.EndContractBlock();
        }
    }
}
