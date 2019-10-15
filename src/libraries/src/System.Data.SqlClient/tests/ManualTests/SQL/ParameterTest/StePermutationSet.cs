// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace System.Data.SqlClient.ManualTesting.Tests
{
    public class StePermutation : Dictionary<SteAttributeKey, object>
    {
        public override string ToString()
        {
            System.Text.StringBuilder b = new System.Text.StringBuilder();
            bool needSeparator = false;
            foreach (KeyValuePair<SteAttributeKey, object> pair in this)
            {
                if (!needSeparator)
                {
                    needSeparator = true;
                }
                else
                {
                    b.Append(", ");
                }
                b.Append(pair.Key.GetType().Name);
                b.Append("=");
                b.Append(pair.Value.ToString());
            }

            return b.ToString();
        }
    }

    public abstract class StePermutationGenerator : IEnumerable<StePermutation>
    {
        // standard GetEnumerator, implemented in terms of specialized enumerator
        public IEnumerator<StePermutation> GetEnumerator()
        {
            return this.GetEnumerator(this.DefaultKeys);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator(this.DefaultKeys);
        }

        public abstract IEnumerable<SteAttributeKey> DefaultKeys
        {
            get;
        }

        // specialized GetEnumerator where attribute keys can be restricted to only those desired
        public abstract IEnumerator<StePermutation> GetEnumerator(IEnumerable<SteAttributeKey> keysOfInterest);
    }


    // Simple generator of permutations based on cross product of lists of keyed attributes
    //  (iterates through combinations of picking one of each attribute in the set)
    public class SteSimplePermutationGenerator : StePermutationGenerator
    {
        // Internal enumerator
        private class SteSimplePermEnumerator : IEnumerator<StePermutation>
        {
            private enum LogicalPosition
            {
                BeforeElements,          // Position is prior to first element and there is at least one element
                OnElement,         // Position is on an element
                AfterElements             // Position is after final element
            }

            private SteSimplePermutationGenerator _parent;            // Source of enumeration elements
            private List<SteAttributeKey> _keysOfInterest;    // attribute keys to use to generate permutations
            private IEnumerator[] _position;          // One enumerator for each non-empty attribute list in parent
            private LogicalPosition _logicalPosition;   // Logical positioning of self

            // NOTE: enumerators in _position always point to valid attributes, to make code for Current be simple.
            //  This means _logicalPosition is required to distinguish times when the overall enumerator should appear
            //  to be positioned either before or after the elements.

            public SteSimplePermEnumerator(SteSimplePermutationGenerator parent, IEnumerable<SteAttributeKey> keysOfInterest)
            {
                _parent = parent;

                // prune keys to ones that are available
                _keysOfInterest = new List<SteAttributeKey>();
                foreach (SteAttributeKey key in keysOfInterest)
                {
                    ArrayList list;
                    if (_parent.AttributeLists.TryGetValue(key, out list) && list.Count > 0)
                    {
                        _keysOfInterest.Add(key);
                    }
                }
                _position = new IEnumerator[_keysOfInterest.Count];
                Reset();
            }

            // Move to next position
            public bool MoveNext()
            {
                if (LogicalPosition.BeforeElements == _logicalPosition)
                {
                    // contained enumerator already pointing to first element, so
                    //  we just need to update logical position
                    _logicalPosition = LogicalPosition.OnElement;
                }
                else if (LogicalPosition.OnElement == _logicalPosition)
                {
                    // Move by advancing each attribute list in turn until one
                    //  arrives at a new value.  As each hits the end of the
                    //  list, restart it and move it to the first element.
                    bool foundOne = false;
                    foreach (IEnumerator attributeList in _position)
                    {
                        if (!attributeList.MoveNext())
                        {
                            attributeList.Reset();
                            attributeList.MoveNext();  // Since we don't have empty lists, this should always succeed.
                        }
                        else
                        {
                            foundOne = true;
                            break;
                        }
                    }

                    if (!foundOne)
                    {
                        _logicalPosition = LogicalPosition.AfterElements;
                    }
                }
                // else logical position is after all elements, so we leave it there and don't do anything

                return LogicalPosition.AfterElements != _logicalPosition;
            }

            // Standard enumerator "Current" property
            //  Returns null if not currently on an element
            //  Otherwise, builds a permutation based on current element of each contained enumerator
            public StePermutation Current
            {
                get
                {
                    StePermutation result = null;
                    if (LogicalPosition.OnElement == _logicalPosition)
                    {
                        result = new StePermutation();
                        for (int i = 0; i < _position.Length; i++)
                        {
                            result.Add(_keysOfInterest[i], _position[i].Current);
                        }
                    }

                    return result;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return this.Current;
                }
            }

            // Standard enumerator restart method
            public void Reset()
            {
                for (int i = 0; i < _keysOfInterest.Count; i++)
                {
                    ArrayList list = _parent.AttributeLists[_keysOfInterest[i]];
                    _position[i] = list.GetEnumerator();
                    if (!_position[i].MoveNext())
                    {
                        throw new ApplicationException("Internal error: No empty lists allowed at this point!");
                    }
                }

                if (0 < _position.Length)
                {
                    _logicalPosition = LogicalPosition.BeforeElements;
                }
                else
                {
                    _logicalPosition = LogicalPosition.AfterElements;
                }
            }

            public void Dispose()
            {
                _logicalPosition = LogicalPosition.AfterElements;
                _position = null;
            }
        }

        // permutationGenerator private fields
        private Dictionary<SteAttributeKey, ArrayList> _permutationBase;

        // generator's ctor
        public SteSimplePermutationGenerator()
        {
            _permutationBase = new Dictionary<SteAttributeKey, ArrayList>();
        }

        // Default key list for generator
        public override IEnumerable<SteAttributeKey> DefaultKeys
        {
            get
            {
                return _permutationBase.Keys;
            }
        }

        // Add a new attribute to the set
        public void Add(SteAttributeKey key, object attribute)
        {
            ArrayList targetList;
            if (!_permutationBase.TryGetValue(key, out targetList))
            {
                targetList = new ArrayList();
                _permutationBase.Add(key, targetList);
            }

            targetList.Add(attribute);
        }

        // specialized GetEnumerator where attribute keys can be restricted to only those desired
        public override IEnumerator<StePermutation> GetEnumerator(IEnumerable<SteAttributeKey> keysOfInterest)
        {
            return new SteSimplePermEnumerator(this, keysOfInterest);
        }

        // Access to internal dictionary, just in case it's useful
        public Dictionary<SteAttributeKey, ArrayList> AttributeLists
        {
            get
            {
                return _permutationBase;
            }
        }
    }
}