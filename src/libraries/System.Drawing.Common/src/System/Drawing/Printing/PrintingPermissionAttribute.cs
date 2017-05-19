/*
 * Copyright (c) 2000 Microsoft Corporation.  All Rights Reserved.
 * Microsoft Confidential.
 */

namespace System.Drawing.Printing {
    using System;
    using System.Security;
    using System.Security.Permissions;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Reflection;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\PrintingPermissionAttribute.uex' path='docs/doc[@for="PrintingPermissionAttribute"]/*' />
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public sealed class PrintingPermissionAttribute : CodeAccessSecurityAttribute {
        PrintingPermissionLevel level;

        /// <include file='doc\PrintingPermissionAttribute.uex' path='docs/doc[@for="PrintingPermissionAttribute.PrintingPermissionAttribute"]/*' />        
        public PrintingPermissionAttribute(SecurityAction action) : base(action) {
        }


        /// <include file='doc\PrintingPermissionAttribute.uex' path='docs/doc[@for="PrintingPermissionAttribute.Level"]/*' />
        public PrintingPermissionLevel Level {
            get {
                return level;
            }
            
            set {
                if (value < PrintingPermissionLevel.NoPrinting || value > PrintingPermissionLevel.AllPrinting) {
                    throw new ArgumentException(SR.Format(SR.PrintingPermissionAttributeInvalidPermissionLevel), "value");
                }
                level = value;
            }
        }

        /// <include file='doc\PrintingPermissionAttribute.uex' path='docs/doc[@for="PrintingPermissionAttribute.CreatePermission"]/*' />        
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        public override IPermission CreatePermission() {
            if (Unrestricted) {
                return new PrintingPermission(PermissionState.Unrestricted);
            }
            else {
                return new PrintingPermission(level);
            }
        }
    }
}
