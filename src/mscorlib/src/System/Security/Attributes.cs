// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using  System.Runtime.InteropServices;

namespace System.Security
{
    // DynamicSecurityMethodAttribute:
    //  Indicates that calling the target method requires space for a security
    //  object to be allocated on the callers stack. This attribute is only ever
    //  set on certain security methods defined within mscorlib.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false )] 
    sealed internal class DynamicSecurityMethodAttribute : System.Attribute
    {
    }

    // SuppressUnmanagedCodeSecurityAttribute:
    //  Indicates that the target P/Invoke method(s) should skip the per-call
    //  security checked for unmanaged code permission.
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class SuppressUnmanagedCodeSecurityAttribute : System.Attribute
    {
    }

    // UnverifiableCodeAttribute:
    //  Indicates that the target module contains unverifiable code.
    [AttributeUsage(AttributeTargets.Module, AllowMultiple = true, Inherited = false )] 
[System.Runtime.InteropServices.ComVisible(true)]
    sealed public class UnverifiableCodeAttribute : System.Attribute
    {
    }

    // AllowPartiallyTrustedCallersAttribute:
    //  Indicates that the Assembly is secure and can be used by untrusted
    //  and semitrusted clients
    //  For v.1, this is valid only on Assemblies, but could be expanded to 
    //  include Module, Method, class
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false )] 
    [System.Runtime.InteropServices.ComVisible(true)]
    sealed public class AllowPartiallyTrustedCallersAttribute : System.Attribute
    {
        private PartialTrustVisibilityLevel _visibilityLevel;
        public AllowPartiallyTrustedCallersAttribute () { }

        public PartialTrustVisibilityLevel PartialTrustVisibilityLevel
        {
            get { return _visibilityLevel; }
            set { _visibilityLevel = value; }
        }
    }

    public enum PartialTrustVisibilityLevel
    {
        VisibleToAllHosts = 0,
        NotVisibleByDefault = 1
    }

#if !FEATURE_CORECLR
    [Obsolete("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
    public enum SecurityCriticalScope
    {
        Explicit = 0,
        Everything = 0x1
    }
#endif // FEATURE_CORECLR

    // SecurityCriticalAttribute
    //  Indicates that the decorated code or assembly performs security critical operations (e.g. Assert, "unsafe", LinkDemand, etc.)
    //  The attribute can be placed on most targets, except on arguments/return values.
    [AttributeUsage(AttributeTargets.Assembly | 
                    AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Enum |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Interface  |
                    AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false )]
    sealed public class SecurityCriticalAttribute     : System.Attribute
    {
#pragma warning disable 618    // We still use SecurityCriticalScope for v2 compat

#if !FEATURE_CORECLR        
         private SecurityCriticalScope  _val;
#endif // FEATURE_CORECLR
        public SecurityCriticalAttribute () {}

#if !FEATURE_CORECLR
        public SecurityCriticalAttribute(SecurityCriticalScope scope)
        {
            _val = scope;
        }

        [Obsolete("SecurityCriticalScope is only used for .NET 2.0 transparency compatibility.")]
        public SecurityCriticalScope Scope {
            get {
                return _val;
            }
        }
#endif // FEATURE_CORECLR

#pragma warning restore 618
    }

    // SecurityTreatAsSafeAttribute:
    // Indicates that the code may contain violations to the security critical rules (e.g. transitions from
    //      critical to non-public transparent, transparent to non-public critical, etc.), has been audited for
    //      security concerns and is considered security clean.
    // At assembly-scope, all rule checks will be suppressed within the assembly and for calls made against the assembly.
    // At type-scope, all rule checks will be suppressed for members within the type and for calls made against the type.
    // At member level (e.g. field and method) the code will be treated as public - i.e. no rule checks for the members.

    [AttributeUsage(AttributeTargets.Assembly |
                    AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Enum |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Interface |
                    AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false )]
    [Obsolete("SecurityTreatAsSafe is only used for .NET 2.0 transparency compatibility.  Please use the SecuritySafeCriticalAttribute instead.")]
    sealed public class SecurityTreatAsSafeAttribute : System.Attribute
    {
        public SecurityTreatAsSafeAttribute () { }
    }

    // SecuritySafeCriticalAttribute: 
    // Indicates that the code may contain violations to the security critical rules (e.g. transitions from
    //      critical to non-public transparent, transparent to non-public critical, etc.), has been audited for
    //      security concerns and is considered security clean. Also indicates that the code is considered SecurityCritical.
    // The effect of this attribute is as if the code was marked [SecurityCritical][SecurityTreatAsSafe].
    // At assembly-scope, all rule checks will be suppressed within the assembly and for calls made against the assembly.
    // At type-scope, all rule checks will be suppressed for members within the type and for calls made against the type.
    // At member level (e.g. field and method) the code will be treated as public - i.e. no rule checks for the members.

    [AttributeUsage(AttributeTargets.Class |
                    AttributeTargets.Struct |
                    AttributeTargets.Enum |
                    AttributeTargets.Constructor |
                    AttributeTargets.Method |
                    AttributeTargets.Field |
                    AttributeTargets.Interface |
                    AttributeTargets.Delegate,
        AllowMultiple = false,
        Inherited = false )]
    sealed public class SecuritySafeCriticalAttribute : System.Attribute
    {
        public SecuritySafeCriticalAttribute () { }
    }

    // SecurityTransparentAttribute:
    // Indicates the assembly contains only transparent code.
    // Security critical actions will be restricted or converted into less critical actions. For example,
    // Assert will be restricted, SuppressUnmanagedCode, LinkDemand, unsafe, and unverifiable code will be converted
    // into Full-Demands.

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false )] 
    sealed public class SecurityTransparentAttribute : System.Attribute
    {
        public SecurityTransparentAttribute () {}
    }

#if !FEATURE_CORECLR
    public enum SecurityRuleSet : byte
    {
        None    = 0,
        Level1  = 1,    // v2.0 transparency model
        Level2  = 2,    // v4.0 transparency model
    }

    // SecurityRulesAttribute
    //
    // Indicates which set of security rules an assembly was authored against, and therefore which set of
    // rules the runtime should enforce on the assembly.  For instance, an assembly marked with
    // [SecurityRules(SecurityRuleSet.Level1)] will follow the v2.0 transparency rules, where transparent code
    // can call a LinkDemand by converting it to a full demand, public critical methods are implicitly
    // treat as safe, and the remainder of the v2.0 rules apply.
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class SecurityRulesAttribute : Attribute
    {
        private SecurityRuleSet m_ruleSet;
        private bool m_skipVerificationInFullTrust = false;

        public SecurityRulesAttribute(SecurityRuleSet ruleSet)
        {
            m_ruleSet = ruleSet;
        }

        // Should fully trusted transparent code skip IL verification
        public bool SkipVerificationInFullTrust
        {
            get { return m_skipVerificationInFullTrust; }
            set { m_skipVerificationInFullTrust = value; }
        }

        public SecurityRuleSet RuleSet
        {
            get { return m_ruleSet; }
        }
    }
#endif // !FEATURE_CORECLR
}
