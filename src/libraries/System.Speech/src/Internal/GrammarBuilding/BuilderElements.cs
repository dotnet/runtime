// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Speech.Internal.SrgsParser;
using System.Speech.Recognition;
using System.Text;

namespace System.Speech.Internal.GrammarBuilding
{
    [DebuggerDisplay("{DebugSummary}")]
    internal abstract class BuilderElements : GrammarBuilderBase
    {
        #region Constructors

        internal BuilderElements()
        {
        }

        #endregion

        #region Public Methods
        public override bool Equals(object obj)
        {
            BuilderElements refObj = obj as BuilderElements;
            if (refObj == null)
            {
                return false;
            }

            // Easy out if the number of elements do not match
            if (refObj.Count != Count || refObj.Items.Count != Items.Count)
            {
                return false;
            }

            // Deep recursive search for equality
            for (int i = 0; i < Items.Count; i++)
            {
                if (!Items[i].Equals(refObj.Items[i]))
                {
                    return false;
                }
            }
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Optimization for a element tree
        /// </summary>
        protected void Optimize(Collection<RuleElement> newRules)
        {
            // Create an dictionary of [Count of elements, list of elements]
            SortedDictionary<int, Collection<BuilderElements>> dict = new();
            GetDictionaryElements(dict);

            // The dictionary is sorted from the smallest buckets to the largest.
            // Revert the order in the keys arrays
            int[] keys = new int[dict.Keys.Count];

            int index = keys.Length - 1;
            foreach (int key in dict.Keys)
            {
                keys[index--] = key;
            }

            // Look for each bucket from the largest to the smallest
            for (int i = 0; i < keys.Length && keys[i] >= 3; i++)
            {
                Collection<BuilderElements> gb = dict[keys[i]];
                for (int j = 0; j < gb.Count; j++)
                {
                    RuleElement newRule = null;
                    RuleRefElement ruleRef = null;
                    for (int k = j + 1; k < gb.Count; k++)
                    {
                        if (gb[j] != null && gb[j].Equals(gb[k]))
                        {
                            BuilderElements current = gb[k];
                            BuilderElements parent = current.Parent;
                            if (current is SemanticKeyElement)
                            // if current is already a ruleref. There is no need to create a new one
                            {
                                // Simply set the ruleref of the current element to the ruleref of the org element.
                                parent.Items[parent.Items.IndexOf(current)] = gb[j];
                            }
                            else
                            {
                                // Create a rule to store the common elements
                                if (newRule == null)
                                {
                                    newRule = new RuleElement(current, "_");
                                    newRules.Add(newRule);
                                }

                                // Create a ruleref and attach the
                                if (ruleRef == null)
                                {
                                    ruleRef = new RuleRefElement(newRule);
                                    gb[j].Parent.Items[gb[j].Parent.Items.IndexOf(gb[j])] = ruleRef;
                                }
                                parent.Items[current.Parent.Items.IndexOf(current)] = ruleRef;
                            }
                            //
                            current.RemoveDictionaryElements(dict);
                            gb[k] = null;
                        }
                    }
                }
            }
        }

        #endregion

        #region Internal Methods

        internal void Add(string phrase)
        {
            _items.Add(new GrammarBuilderPhrase(phrase));
        }

        internal void Add(GrammarBuilder builder)
        {
            foreach (GrammarBuilderBase item in builder.InternalBuilder.Items)
            {
                _items.Add(item);
            }
        }

        internal void Add(GrammarBuilderBase item)
        {
            _items.Add(item);
        }

        internal void CloneItems(BuilderElements builders)
        {
            foreach (GrammarBuilderBase item in builders.Items)
            {
                _items.Add(item);
            }
        }

        internal void CreateChildrenElements(IElementFactory elementFactory, IRule parent, IdentifierCollection ruleIds)
        {
            foreach (GrammarBuilderBase builder in Items)
            {
                IElement element = builder.CreateElement(elementFactory, parent, parent, ruleIds);
                if (element != null)
                {
                    element.PostParse(parent);
                    elementFactory.AddElement(parent, element);
                }
            }
        }

        internal void CreateChildrenElements(IElementFactory elementFactory, IItem parent, IRule rule, IdentifierCollection ruleIds)
        {
            foreach (GrammarBuilderBase builder in Items)
            {
                IElement element = builder.CreateElement(elementFactory, parent, rule, ruleIds);
                if (element != null)
                {
                    element.PostParse(parent);
                    elementFactory.AddElement(parent, element);
                }
            }
        }

        internal override int CalcCount(BuilderElements parent)
        {
            base.CalcCount(parent);
            int c = 1;
            foreach (GrammarBuilderBase item in Items)
            {
                c += item.CalcCount(this);
            }
            Count = c;

            return c;
        }

        #endregion

        #region Internal Properties

        internal List<GrammarBuilderBase> Items
        {
            get
            {
                return _items;
            }
        }

        internal override string DebugSummary
        {
            get
            {
                StringBuilder sb = new();

                foreach (GrammarBuilderBase item in _items)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }
                    sb.Append(item.DebugSummary);
                }
                return sb.ToString();
            }
        }

        #endregion

        #region Private Method

        private void GetDictionaryElements(SortedDictionary<int, Collection<BuilderElements>> dict)
        {
            // Recursive search from a matching subtree
            foreach (GrammarBuilderBase item in Items)
            {
                BuilderElements current = item as BuilderElements;

                // Go deeper if the number of children is greater the element to compare against.
                if (current != null)
                {
                    if (!dict.ContainsKey(current.Count))
                    {
                        dict.Add(current.Count, new Collection<BuilderElements>());
                    }
                    dict[current.Count].Add(current);

                    current.GetDictionaryElements(dict);
                }
            }
        }

        private void RemoveDictionaryElements(SortedDictionary<int, Collection<BuilderElements>> dict)
        {
            // Recursive search from a matching subtree
            foreach (GrammarBuilderBase item in Items)
            {
                BuilderElements current = item as BuilderElements;

                // Go deeper if the number of children is greater the element to compare against.
                if (current != null)
                {
                    // Recursively remove all elements
                    current.RemoveDictionaryElements(dict);

                    dict[current.Count].Remove(current);
                }
            }
        }

        #endregion

        #region Private Fields

        // List of builder elements
        private readonly List<GrammarBuilderBase> _items = new();

        #endregion
    }
}
