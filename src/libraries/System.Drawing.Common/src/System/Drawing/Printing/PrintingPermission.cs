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
    using System.Globalization;
    using System.Diagnostics.CodeAnalysis;    
    
    /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission"]/*' />
    /// <devdoc>
    ///    <para> Controls the ability to use the printer. This class cannot be inherited.</para>
    /// </devdoc>
    [Serializable] 
    public sealed class PrintingPermission : CodeAccessPermission, IUnrestrictedPermission {
        private PrintingPermissionLevel printingLevel;

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.PrintingPermission"]/*' />
        /// <devdoc>
        /// <para>Initializes a new instance of the PrintingPermission class with either fully restricted
        ///    or unrestricted access, as specified.</para>
        /// </devdoc>
        public PrintingPermission(PermissionState state) {
            if (state == PermissionState.Unrestricted) {
                printingLevel = PrintingPermissionLevel.AllPrinting;
            }
            else if (state == PermissionState.None) {
                printingLevel = PrintingPermissionLevel.NoPrinting;
            }
            else {
                throw new ArgumentException(SR.Format(SR.InvalidPermissionState));
            }
        }    

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.PrintingPermission1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public PrintingPermission(PrintingPermissionLevel printingLevel) {
            VerifyPrintingLevel(printingLevel);

            this.printingLevel = printingLevel;
        }

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.Level"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public PrintingPermissionLevel Level {
            get {
                return printingLevel;
            }
            
            set {
                VerifyPrintingLevel(value);
                printingLevel = value;
            }
        }

        private static void VerifyPrintingLevel(PrintingPermissionLevel level) {
            if (level < PrintingPermissionLevel.NoPrinting || level > PrintingPermissionLevel.AllPrinting) {
                throw new ArgumentException(SR.Format(SR.InvalidPermissionLevel));
            }
        }


        //------------------------------------------------------
        //
        // CODEACCESSPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.IsUnrestricted"]/*' />
        /// <devdoc>
        ///    <para> Gets a
        ///       value indicating whether permission is unrestricted.</para>
        /// </devdoc>
        public bool IsUnrestricted() {
            return printingLevel == PrintingPermissionLevel.AllPrinting;
        }

        //------------------------------------------------------
        //
        // IPERMISSION IMPLEMENTATION
        //
        //------------------------------------------------------

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.IsSubsetOf"]/*' />
        /// <devdoc>
        ///    <para>Determines whether the current permission object is a subset of
        ///       the specified permission.</para>
        /// </devdoc>
        public override bool IsSubsetOf(IPermission target) {
            if (target == null) {
                return printingLevel == PrintingPermissionLevel.NoPrinting;
            }
            
            PrintingPermission operand = target as PrintingPermission;
            if(operand == null) {
                throw new ArgumentException(SR.Format(SR.TargetNotPrintingPermission));
            }
            return this.printingLevel <= operand.printingLevel;
        }

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.Intersect"]/*' />
        /// <devdoc>
        ///    <para>Creates and returns a permission that is the intersection of the current 
        ///       permission object and a target permission object.</para>
        /// </devdoc>
        public override IPermission Intersect(IPermission target) {
            if (target == null) {
                return null;
            }
            
            PrintingPermission operand = target as PrintingPermission;
            if(operand == null) {
                throw new ArgumentException(SR.Format(SR.TargetNotPrintingPermission));
            }
            PrintingPermissionLevel isectLevels = printingLevel < operand.printingLevel ? printingLevel : operand.printingLevel;
            if (isectLevels == PrintingPermissionLevel.NoPrinting)
                return null;
            else
                return new PrintingPermission(isectLevels);
        }

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.Union"]/*' />
        /// <devdoc>
        ///    <para>Creates a permission that is the union of the permission object
        ///       and the target parameter permission object.</para>
        /// </devdoc>
        public override IPermission Union(IPermission target) {
            if (target == null) {
                return this.Copy();
            }
            
            PrintingPermission operand = target as PrintingPermission;
            if(operand == null) {
                throw new ArgumentException(SR.Format(SR.TargetNotPrintingPermission));
            }
            PrintingPermissionLevel isectLevels = printingLevel > operand.printingLevel ? printingLevel : operand.printingLevel;
            if (isectLevels == PrintingPermissionLevel.NoPrinting)
                return null;
            else
                return new PrintingPermission(isectLevels);
        }        

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.Copy"]/*' />
        /// <devdoc>
        ///    <para>Creates and returns an identical copy of the current permission
        ///       object.</para>
        /// </devdoc>        
        [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
        public override IPermission Copy() {
            return new PrintingPermission(this.printingLevel);
        }


        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.ToXml"]/*' />
        /// <devdoc>
        ///    <para>Creates an XML encoding of the security object and its current
        ///       state.</para>
        /// </devdoc>
        public override SecurityElement ToXml() {
            SecurityElement securityElement = new SecurityElement("IPermission");

            securityElement.AddAttribute("class", this.GetType().FullName + ", " + this.GetType().Module.Assembly.FullName.Replace('\"', '\''));
            securityElement.AddAttribute("version", "1");
            if (!IsUnrestricted()) {
                securityElement.AddAttribute("Level", Enum.GetName(typeof(PrintingPermissionLevel), printingLevel));
            }
            else {
                securityElement.AddAttribute("Unrestricted", "true");
            }
            return securityElement;
        }

        /// <include file='doc\PrintingPermission.uex' path='docs/doc[@for="PrintingPermission.FromXml"]/*' />
        /// <devdoc>
        ///    <para>Reconstructs a security object with a specified state from an XML
        ///       encoding.</para>
        /// </devdoc>        
        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override void FromXml(SecurityElement esd) {
            if (esd == null) {
                throw new ArgumentNullException("esd");
            }

            String className = esd.Attribute("class");

            if (className == null || className.IndexOf(this.GetType().FullName) == -1) {
                throw new ArgumentException(SR.Format(SR.InvalidClassName));
            }

            String unrestricted = esd.Attribute("Unrestricted");

            if (unrestricted != null && String.Equals(unrestricted, "true", StringComparison.OrdinalIgnoreCase))
            {
                printingLevel = PrintingPermissionLevel.AllPrinting;
                return;
            }

            printingLevel = PrintingPermissionLevel.NoPrinting;

            String printing = esd.Attribute("Level");
            
            if (printing != null)
            {
                printingLevel = (PrintingPermissionLevel)Enum.Parse(typeof(PrintingPermissionLevel), printing);
            }
        }
    }
}

