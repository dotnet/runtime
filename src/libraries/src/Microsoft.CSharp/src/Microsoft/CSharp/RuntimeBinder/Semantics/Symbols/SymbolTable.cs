// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    // A symbol table is a helper class used by the symbol manager. There are
    // two symbol tables; a global and a local.

    internal sealed class SYMTBL
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
            switch (child.getKind())
            {
                case SYMKIND.SK_Scope:
                case SYMKIND.SK_LocalVariableSymbol:
                    return;
            }

            Key k = new Key(child.name, child.parent);
            Symbol sym;

            if (_dictionary.TryGetValue(k, out sym))
            {
                // Link onto the end of the symbol chain here.
                while (sym?.nextSameName != null)
                {
                    sym = sym.nextSameName;
                }

                Debug.Assert(sym != null && sym.nextSameName == null);
                sym.nextSameName = child;
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

        private sealed class Key : IEquatable<Key>
        {
            private readonly Name _name;
            private readonly ParentSymbol _parent;

            public Key(Name name, ParentSymbol parent)
            {
                _name = name;
                _parent = parent;
            }

            public bool Equals(Key other) => other != null && _name.Equals(other._name) && _parent.Equals(other._parent);

#if  DEBUG 
            [ExcludeFromCodeCoverage] // Typed overload should always be the method called.
#endif
            public override bool Equals(object obj)
            {
                Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
                return Equals(obj as Key);
            }

            public override int GetHashCode()
            {
                return _name.GetHashCode() ^ _parent.GetHashCode();
            }
        }
    }
}
