// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MS.Internal.Xml;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This class keeps a list of whitespace rules in order to determine whether whitespace children of particular
    /// elements should be stripped.
    /// </summary>
    internal sealed class WhitespaceRuleLookup
    {
        private readonly Hashtable _qnames;
        private readonly ArrayList _wildcards;
        private readonly InternalWhitespaceRule _ruleTemp;
        private XmlNameTable _nameTable;

        public WhitespaceRuleLookup()
        {
            _qnames = new Hashtable();
            _wildcards = new ArrayList();
        }

        /// <summary>
        /// Create a new lookup internal class from the specified WhitespaceRules.
        /// </summary>
        public WhitespaceRuleLookup(IList<WhitespaceRule> rules) : this()
        {
            WhitespaceRule rule;
            InternalWhitespaceRule ruleInternal;
            Debug.Assert(rules != null);

            for (int i = rules.Count - 1; i >= 0; i--)
            {
                // Make a copy of each rule
                rule = rules[i];
                ruleInternal = new InternalWhitespaceRule(rule.LocalName, rule.NamespaceName, rule.PreserveSpace, -i);

                if (rule.LocalName == null || rule.NamespaceName == null)
                {
                    // Wildcard, so add to wildcards array
                    _wildcards.Add(ruleInternal);
                }
                else
                {
                    // Exact name, so add to hashtable
                    _qnames[ruleInternal] = ruleInternal;
                }
            }

            // Create a temporary (not thread-safe) InternalWhitespaceRule used for lookups
            _ruleTemp = new InternalWhitespaceRule();
        }

        /// <summary>
        /// Atomize all names contained within the whitespace rules with respect to "nameTable".
        /// </summary>
        public void Atomize(XmlNameTable nameTable)
        {
            // If names are already atomized with respect to "nameTable", no need to do it again
            if (nameTable != _nameTable)
            {
                _nameTable = nameTable;

                foreach (InternalWhitespaceRule rule in _qnames.Values)
                    rule.Atomize(nameTable);

                foreach (InternalWhitespaceRule rule in _wildcards)
                    rule.Atomize(nameTable);
            }
        }

        /// <summary>
        /// Return true if elements of the specified name should have whitespace children stripped.
        /// NOTE: This method is not thread-safe.  Different threads should create their own copy of the
        /// WhitespaceRuleLookup object.  This allows all names to be atomized according to a private NameTable.
        /// </summary>
        public bool ShouldStripSpace(string localName, string namespaceName)
        {
            InternalWhitespaceRule qnameRule, wildcardRule;
            Debug.Assert(_nameTable != null && _ruleTemp != null);
            Debug.Assert(localName != null && (object)_nameTable.Get(localName) == (object)localName);
            Debug.Assert(namespaceName != null && (object)_nameTable.Get(namespaceName) == (object)namespaceName);

            _ruleTemp.Init(localName, namespaceName, false, 0);

            // Lookup name in qnames table
            // If found, the name will be stripped unless there is a preserve wildcard with higher priority
            qnameRule = _qnames[_ruleTemp] as InternalWhitespaceRule;

            for (int pos = _wildcards.Count; pos-- != 0;)
            {
                wildcardRule = _wildcards[pos] as InternalWhitespaceRule;

                if (qnameRule != null)
                {
                    // If qname priority is greater than any subsequent wildcard's priority, then we're done
                    if (qnameRule.Priority > wildcardRule.Priority)
                        return !qnameRule.PreserveSpace;

                    // Don't bother to consider wildcards with the same PreserveSpace flag
                    if (qnameRule.PreserveSpace == wildcardRule.PreserveSpace)
                        continue;
                }

                if (wildcardRule.LocalName == null || (object)wildcardRule.LocalName == (object)localName)
                {
                    if (wildcardRule.NamespaceName == null || (object)wildcardRule.NamespaceName == (object)namespaceName)
                    {
                        // Found wildcard match, so we're done (since wildcards are in priority order)
                        return !wildcardRule.PreserveSpace;
                    }
                }
            }

            return (qnameRule != null && !qnameRule.PreserveSpace);
        }

        private sealed class InternalWhitespaceRule : WhitespaceRule
        {
            private int _priority;       // Relative priority of this test
            private int _hashCode;       // Cached hashcode

            public InternalWhitespaceRule()
            {
            }

            public InternalWhitespaceRule(string localName, string namespaceName, bool preserveSpace, int priority)
            {
                Init(localName, namespaceName, preserveSpace, priority);
            }

            public void Init(string localName, string namespaceName, bool preserveSpace, int priority)
            {
                base.Init(localName, namespaceName, preserveSpace);
                _priority = priority;

                if (localName != null && namespaceName != null)
                {
                    _hashCode = localName.GetHashCode();
                }
            }

            public void Atomize(XmlNameTable nameTable)
            {
                if (LocalName != null)
                    LocalName = nameTable.Add(LocalName);

                if (NamespaceName != null)
                    NamespaceName = nameTable.Add(NamespaceName);
            }

            public int Priority
            {
                get { return _priority; }
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }

            public override bool Equals(object obj)
            {
                Debug.Assert(obj is InternalWhitespaceRule);
                InternalWhitespaceRule that = obj as InternalWhitespaceRule;

                Debug.Assert(LocalName != null && that.LocalName != null);
                Debug.Assert(NamespaceName != null && that.NamespaceName != null);

                // string == operator compares object references first and if they are not the same compares contents
                // of the compared strings. As a result we do not have to cast strings to objects to force reference
                // comparison for atomized LocalNames and NamespaceNames.
                return LocalName == that.LocalName && NamespaceName == that.NamespaceName;
            }
        }
    }
}
