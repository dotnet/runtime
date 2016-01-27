// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    using System.IO;
    using System.Threading;
    using System.Security;
    using System.Security.Util;
    using System.Security.Permissions;
    using System.Runtime.CompilerServices;
    using System.Collections;
    using System.Text;
    using System;
    using  System.Diagnostics;
    using System.Diagnostics.Contracts;
    using IUnrestrictedPermission = System.Security.Permissions.IUnrestrictedPermission;

    [Serializable]
#if !FEATURE_CORECLR
    [SecurityPermissionAttribute( SecurityAction.InheritanceDemand, ControlEvidence = true, ControlPolicy = true )]
#endif
    [System.Runtime.InteropServices.ComVisible(true)]
    abstract public class CodeAccessPermission
        : IPermission, ISecurityEncodable, IStackWalk
    {
        // Static methods for manipulation of stack
        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void RevertAssert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAssert(ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public static void RevertDeny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertDeny(ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void RevertPermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertPermitOnly(ref stackMark);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public static void RevertAll()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.RevertAll(ref stackMark);
        }

        //
        // Standard implementation of IPermission methods for
        // code-access permissions.
        //

        // Mark this method as requiring a security object on the caller's frame
        // so the caller won't be inlined (which would mess up stack crawling).
        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Demand()
        {
            if (!this.CheckDemand( null ))
            {
                StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
                CodeAccessSecurityEngine.Check(this, ref stackMark);
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        internal static void Demand(PermissionType permissionType)
        {
            //    The intent of the method is to be an internal mscorlib helper that Demands a specific permissiontype
            //    without having to create objects.
            //    The security annotation fxcop rule that flags all methods with a Demand() has logic
            //    which checks for methods named Demand in types that implement IPermission or IStackWalk. 
            Contract.Assert(new StackFrame().GetMethod().Name.Equals("Demand"), "This method needs to be named Demand");
            
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCallersCaller;
            CodeAccessSecurityEngine.SpecialDemand(permissionType, ref stackMark);
        }

        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void Assert()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.Assert(this, ref stackMark);
        }


        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable    
        static internal void Assert(bool allPossible)
        {
            //    The intent of the method is to be an internal mscorlib helper that easily asserts for all possible permissions
            //    without having to new a PermissionSet.
            //    The security annotation fxcop rule that flags all methods with an Assert() has logic
            //    which checks for methods named Assert in types that implement IPermission or IStackWalk. 
            Contract.Assert(new StackFrame().GetMethod().Name.Equals("Assert"), "This method needs to be named Assert");
            
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            SecurityRuntime.AssertAllPossible(ref stackMark);
        }
    
        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        [Obsolete("Deny is obsolete and will be removed in a future release of the .NET Framework. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
        public void Deny()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.Deny(this, ref stackMark);
        }
        
        // Metadata for this method should be flaged with REQ_SQ so that
        // EE can allocate space on the stack frame for FrameSecurityDescriptor

        [System.Security.SecuritySafeCritical]  // auto-generated
        [DynamicSecurityMethodAttribute()]
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        public void PermitOnly()
        {
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CodeAccessSecurityEngine.PermitOnly(this, ref stackMark);
        }

        // IPermission interfaces

        // We provide a default implementation of Union here.
        // Any permission that doesn't provide its own representation 
        // of Union will get this one and trigger CompoundPermission
        // We can take care of simple cases here...

        public virtual IPermission Union(IPermission other) {
            // The other guy could be null
            if (other == null) return(this.Copy());
            
            // otherwise we don't support it.
            throw new NotSupportedException(Environment.GetResourceString( "NotSupported_SecurityPermissionUnion" ));
        }
        
#if FEATURE_CAS_POLICY
        static internal SecurityElement CreatePermissionElement( IPermission perm, String permname )
        {
            SecurityElement root = new SecurityElement( "IPermission" );
            XMLUtil.AddClassAttribute( root, perm.GetType(), permname );
            // If you hit this assert then most likely you are trying to change the name of this class. 
            // This is ok as long as you change the hard coded string above and change the assert below.
            Contract.Assert( perm.GetType().FullName.Equals( permname ), "Incorrect class name passed in! Was: " + permname + " Should be " + perm.GetType().FullName);

            root.AddAttribute( "version", "1" );
            return root;
        }
        
        static internal void ValidateElement( SecurityElement elem, IPermission perm )
        {
            if (elem == null)
                throw new ArgumentNullException( "elem" );
            Contract.EndContractBlock();
                
            if (!XMLUtil.IsPermissionElement( perm, elem ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_NotAPermissionElement"));
                
            String version = elem.Attribute( "version" );
            
            if (version != null && !version.Equals( "1" ))
                throw new ArgumentException( Environment.GetResourceString( "Argument_InvalidXMLBadVersion") );
        }

        abstract public SecurityElement ToXml();
        abstract public void FromXml( SecurityElement elem );

        //
        // Unimplemented interface methods 
        // (as a reminder only)
        //

        public override String ToString()
        {
            return ToXml().ToString();
        }
#endif // FEATURE_CAS_POLICY

        //
        // HELPERS FOR IMPLEMENTING ABSTRACT METHODS
        //

        //
        // Protected helper
        //

        internal bool VerifyType(IPermission perm)
        {
            // if perm is null, then obviously not of the same type
            if ((perm == null) || (perm.GetType() != this.GetType())) {
                return(false);
            } else {
                return(true);
            }
        }

        // The IPermission Interface
        public abstract IPermission Copy();
        public abstract IPermission Intersect(IPermission target);
        public abstract bool IsSubsetOf(IPermission target);

        [System.Runtime.InteropServices.ComVisible(false)]
        public override bool Equals(Object obj)
        {
            IPermission perm = obj as IPermission;
            if(obj != null && perm == null)
                return false;
            try {
                if(!this.IsSubsetOf(perm))
                    return false;
                if(perm != null && !perm.IsSubsetOf(this))
                    return false;
            }
            catch (ArgumentException)
            {
                // Any argument exception implies inequality
                // Note that we require a try/catch block here because we have to deal with
                // custom permissions that may throw exceptions indiscriminately.
                return false;
            }
            return true;
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public override int GetHashCode()
        {
            // This implementation is only to silence a compiler warning.
            return base.GetHashCode();
        }


        internal bool CheckDemand(CodeAccessPermission grant)
        {
            Contract.Assert( grant == null || grant.GetType().Equals( this.GetType() ), "CheckDemand not defined for permissions of different type" );
            return IsSubsetOf( grant );
        }

        internal bool CheckPermitOnly(CodeAccessPermission permitted)
        {
            Contract.Assert( permitted == null || permitted.GetType().Equals( this.GetType() ), "CheckPermitOnly not defined for permissions of different type" );
            return IsSubsetOf( permitted );
        }

        internal bool CheckDeny(CodeAccessPermission denied)
        {
            Contract.Assert( denied == null || denied.GetType().Equals( this.GetType() ), "CheckDeny not defined for permissions of different type" );
            IPermission intersectPerm = Intersect(denied);
            return (intersectPerm == null || intersectPerm.IsSubsetOf(null));
        }

        internal bool CheckAssert(CodeAccessPermission asserted)
        {
            Contract.Assert( asserted == null || asserted.GetType().Equals( this.GetType() ), "CheckPermitOnly not defined for permissions of different type" );
            return IsSubsetOf( asserted );
        }
    }
}
