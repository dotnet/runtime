// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// This API supports the .NET Framework infrastructure and is not intended to be used directly from your code.
    /// Represents a cache of runtime binding rules.
    /// </summary>
    /// <typeparam name="T">The delegate type.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never), DebuggerStepThrough]
    public class RuleCache<T> where T : class
    {
        private T[] _rules = Array.Empty<T>();
        private readonly Object _cacheLock = new Object();

        private const int MaxRules = 128;

        internal RuleCache() { }

        internal T[] GetRules()
        {
            return _rules;
        }

        // move the rule +2 up.
        // this is called on every successful rule.
        internal void MoveRule(T rule, int i)
        {
            // limit search to MaxSearch elements.
            // Rule should not get too far unless it has been already moved up.
            // need a lock to make sure we are moving the right rule and not loosing any.
            lock (_cacheLock)
            {
                const int MaxSearch = 8;
                int count = _rules.Length - i;
                if (count > MaxSearch)
                {
                    count = MaxSearch;
                }

                int oldIndex = -1;
                int max = Math.Min(_rules.Length, i + count);
                for (int index = i; index < max; index++)
                {
                    if (_rules[index] == rule)
                    {
                        oldIndex = index;
                        break;
                    }
                }
                if (oldIndex < 2)
                {
                    return;
                }
                T oldRule = _rules[oldIndex];
                _rules[oldIndex] = _rules[oldIndex - 1];
                _rules[oldIndex - 1] = _rules[oldIndex - 2];
                _rules[oldIndex - 2] = oldRule;
            }
        }

        internal void AddRule(T newRule)
        {
            // need a lock to make sure we are not loosing rules.
            lock (_cacheLock)
            {
                _rules = AddOrInsert(_rules, newRule);
            }
        }

        internal void ReplaceRule(T oldRule, T newRule)
        {
            // need a lock to make sure we are replacing the right rule
            lock (_cacheLock)
            {
                int i = Array.IndexOf(_rules, oldRule);
                if (i >= 0)
                {
                    _rules[i] = newRule;
                    return; // DONE
                }

                // could not find it.
                _rules = AddOrInsert(_rules, newRule);
            }
        }


        // Adds to end or inserts items at InsertPosition
        private const int InsertPosition = MaxRules / 2;

        private static T[] AddOrInsert(T[] rules, T item)
        {
            if (rules.Length < InsertPosition)
            {
                return rules.AddLast(item);
            }

            T[] newRules;

            int newLength = rules.Length + 1;
            if (newLength > MaxRules)
            {
                newLength = MaxRules;
                newRules = rules;
            }
            else
            {
                newRules = new T[newLength];
            }

            Array.Copy(rules, 0, newRules, 0, InsertPosition);
            newRules[InsertPosition] = item;
            Array.Copy(rules, InsertPosition, newRules, InsertPosition + 1, newLength - InsertPosition - 1);
            return newRules;
        }
    }
}
