// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
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
        public SecurityRulesAttribute(SecurityRuleSet ruleSet)
        {
            RuleSet = ruleSet;
        }

        // Should fully trusted transparent code skip IL verification
        public bool SkipVerificationInFullTrust { get; set; }

        public SecurityRuleSet RuleSet { get; }
    }
}
