// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // A symbol table is a helper class used by the symbol manager. There are
    // two symbol tables; a global and a local.

    internal class SYMTBL
    {
        /////////////////////////////////////////////////////////////////////////////////
        // Public

        public SYMTBL()
        {
            _dictionary = new Dictionary<Key, Symbol>();
        }

        public Symbol LookupSym(Name name, ParentSymbol parent, symbmask_t kindmask)
        {
            Key k = new Key(name, parent);
            Symbol sym;

            if (_dictionary.TryGetValue(k, out sym))
            {
                return FindCorrectKind(sym, kindmask);
            }

            return null;
        }

        public void InsertChild(ParentSymbol parent, Symbol child)
        {
            Debug.Assert(child.nextSameName == null);
            Debug.Assert(child.parent == null || child.parent == parent);
            child.parent = parent;

            // Place the child into the hash table.
            InsertChildNoGrow(child);
        }

        private void InsertChildNoGrow(Symbol child)
        {
            Key k = new Key(child.name, child.parent);
            Symbol sym;

            if (_dictionary.TryGetValue(k, out sym))
            {
                // Link onto the end of the symbol chain here.
                while (sym != null && sym.nextSameName != null)
                {
                    sym = sym.nextSameName;
                }

                Debug.Assert(sym != null && sym.nextSameName == null);
                sym.nextSameName = child;
                return;
            }
            else
            {
                _dictionary.Add(k, child);
            }
        }

        private static Symbol FindCorrectKind(Symbol sym, symbmask_t kindmask)
        {
            do
            {
                if ((kindmask & sym.mask()) != 0)
                {
                    return sym;
                }
                sym = sym.nextSameName;
            } while (sym != null);

            return null;
        }

        private readonly Dictionary<Key, Symbol> _dictionary;

        private sealed class Key
        {
            private readonly Name _name;
            private readonly ParentSymbol _parent;

            public Key(Name name, ParentSymbol parent)
            {
                _name = name;
                _parent = parent;
            }

            public override bool Equals(object obj)
            {
                Key k = obj as Key;
                return k != null && _name.Equals(k._name) && _parent.Equals(k._parent);
            }

            public override int GetHashCode()
            {
                return _name.GetHashCode() ^ _parent.GetHashCode();
            }
        }
    }
}
