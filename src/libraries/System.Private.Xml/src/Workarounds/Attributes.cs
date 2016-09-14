// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    internal enum SecurityRuleSet : byte
    {
        None = 0,
        Level1 = 1,    // v2.0 transparency model
        Level2 = 2,    // v4.0 transparency model
    }

    // SecurityRulesAttribute
    //
    // Indicates which set of security rules an assembly was authored against, and therefore which set of
    // rules the runtime should enforce on the assembly.  For instance, an assembly marked with
    // [SecurityRules(SecurityRuleSet.Level1)] will follow the v2.0 transparency rules, where transparent code
    // can call a LinkDemand by converting it to a full demand, public critical methods are implicitly
    // treat as safe, and the remainder of the v2.0 rules apply.
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    internal sealed class SecurityRulesAttribute : Attribute
    {
        private SecurityRuleSet _ruleSet;
        private bool _skipVerificationInFullTrust = false;

        public SecurityRulesAttribute(SecurityRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
        }

        // Should fully trusted transparent code skip IL verification
        public bool SkipVerificationInFullTrust
        {
            get { return _skipVerificationInFullTrust; }
            set { _skipVerificationInFullTrust = value; }
        }

        public SecurityRuleSet RuleSet
        {
            get { return _ruleSet; }
        }
    }
}
