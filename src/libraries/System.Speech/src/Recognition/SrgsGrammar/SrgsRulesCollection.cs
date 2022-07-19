// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Speech.Internal;

namespace System.Speech.Recognition.SrgsGrammar
{
    [Serializable]
    public sealed class SrgsRulesCollection : KeyedCollection<string, SrgsRule>
    {
        public void Add(params SrgsRule[] rules)
        {
            Helpers.ThrowIfNull(rules, nameof(rules));

            for (int iRule = 0; iRule < rules.Length; iRule++)
            {
                if (rules[iRule] == null)
                {
                    throw new ArgumentNullException(nameof(rules), SR.Get(SRID.ParamsEntryNullIllegal));
                }
                base.Add(rules[iRule]);
            }
        }
        protected override string GetKeyForItem(SrgsRule rule)
        {
            ArgumentNullException.ThrowIfNull(rule);

            return rule.Id;
        }
    }
}
