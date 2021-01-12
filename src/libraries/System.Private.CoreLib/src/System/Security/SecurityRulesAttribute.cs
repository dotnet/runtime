// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security
{
    // Has no effect in .NET Core
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
