// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class BSYMMGR
    {
        // Special nullable members.
        public PropertySymbol propNubValue;
        public MethodSymbol methNubCtor;

        private readonly SymFactory _symFactory;

        private SYMTBL tableGlobal;

        // The hash table for type arrays.
        private Dictionary<TypeArrayKey, TypeArray> tableTypeArrays;

        private static readonly TypeArray s_taEmpty = new TypeArray(Array.Empty<CType>());

        public BSYMMGR()
        {
            this.tableGlobal = new SYMTBL();
            _symFactory = new SymFactory(this.tableGlobal);

            this.tableTypeArrays = new Dictionary<TypeArrayKey, TypeArray>();

            ////////////////////////////////////////////////////////////////////////////////
            // Build the data structures needed to make FPreLoad fast. Make sure the 
            // namespaces are created. Compute and sort hashes of the NamespaceSymbol * value and type
            // name (sans arity indicator).

            for (int i = 0; i < (int)PredefinedType.PT_COUNT; ++i)
            {
                NamespaceSymbol ns = NamespaceSymbol.Root;
                string name = PredefinedTypeFacts.GetName((PredefinedType)i);
                int start = 0;
                while (start < name.Length)
                {
                    int iDot = name.IndexOf('.', start);
                    if (iDot == -1)
                        break;
                    string sub = (iDot > start) ? name.Substring(start, iDot - start) : name.Substring(start);
                    Name nm = NameManager.Add(sub);
                    ns = LookupGlobalSymCore(nm, ns, symbmask_t.MASK_NamespaceSymbol) as NamespaceSymbol ?? _symFactory.CreateNamespace(nm, ns);
                    start += sub.Length + 1;
                }
            }
        }

        public SYMTBL GetSymbolTable()
        {
            return tableGlobal;
        }

        public static TypeArray EmptyTypeArray()
        {
            return s_taEmpty;
        }

        public BetterType CompareTypes(TypeArray ta1, TypeArray ta2)
        {
            if (ta1 == ta2)
            {
                return BetterType.Same;
            }
            if (ta1.Count != ta2.Count)
            {
                // The one with more parameters is more specific.
                return ta1.Count > ta2.Count ? BetterType.Left : BetterType.Right;
            }

            BetterType nTot = BetterType.Neither;

            for (int i = 0; i < ta1.Count; i++)
            {
                CType type1 = ta1[i];
                CType type2 = ta2[i];
                BetterType nParam = BetterType.Neither;

            LAgain:
                if (type1.GetTypeKind() != type2.GetTypeKind())
                {
                    if (type1 is TypeParameterType)
                    {
                        nParam = BetterType.Right;
                    }
                    else if (type2 is TypeParameterType)
                    {
                        nParam = BetterType.Left;
                    }
                }
                else
                {
                    switch (type1.GetTypeKind())
                    {
                        default:
                            Debug.Assert(false, "Bad kind in CompareTypes");
                            break;
                        case TypeKind.TK_TypeParameterType:
                            break;

                        case TypeKind.TK_PointerType:
                        case TypeKind.TK_ParameterModifierType:
                        case TypeKind.TK_ArrayType:
                        case TypeKind.TK_NullableType:
                            type1 = type1.GetBaseOrParameterOrElementType();
                            type2 = type2.GetBaseOrParameterOrElementType();
                            goto LAgain;

                        case TypeKind.TK_AggregateType:
                            nParam = CompareTypes(((AggregateType)type1).GetTypeArgsAll(), ((AggregateType)type2).GetTypeArgsAll());
                            break;
                    }
                }

                if (nParam == BetterType.Right || nParam == BetterType.Left)
                {
                    if (nTot == BetterType.Same || nTot == BetterType.Neither)
                    {
                        nTot = nParam;
                    }
                    else if (nParam != nTot)
                    {
                        return BetterType.Neither;
                    }
                }
            }

            return nTot;
        }

        public SymFactory GetSymFactory()
        {
            return _symFactory;
        }

        public Symbol LookupGlobalSymCore(Name name, ParentSymbol parent, symbmask_t kindmask)
        {
            return tableGlobal.LookupSym(name, parent, kindmask);
        }

        public Symbol LookupAggMember(Name name, AggregateSymbol agg, symbmask_t mask)
        {
            return tableGlobal.LookupSym(name, agg, mask);
        }

        public static Symbol LookupNextSym(Symbol sym, ParentSymbol parent, symbmask_t kindmask)
        {
            Debug.Assert(sym.parent == parent);

            sym = sym.nextSameName;
            Debug.Assert(sym == null || sym.parent == parent);

            // Keep traversing the list of symbols with same name and parent.
            while (sym != null)
            {
                if ((kindmask & sym.mask()) > 0)
                    return sym;

                sym = sym.nextSameName;
                Debug.Assert(sym == null || sym.parent == parent);
            }

            return null;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Allocate a type array; used to represent a parameter list.
        // We use a hash table to make sure that allocating the same type array twice 
        // returns the same value. This does two things:
        //
        // 1) Save a lot of memory.
        // 2) Make it so parameter lists can be compared by a simple pointer comparison
        // 3) Allow us to associate a token with each signature for faster metadata emit

        private readonly struct TypeArrayKey : IEquatable<TypeArrayKey>
        {
            private readonly CType[] _types;
            private readonly int _hashCode;

            public TypeArrayKey(CType[] types)
            {
                _types = types;
                int hashCode = 0x162A16FE;
                foreach (CType type in types)
                {
                    hashCode = (hashCode << 5) - hashCode;
                    if (type != null)
                    {
                        hashCode ^= type.GetHashCode();
                    }
                }

                _hashCode = hashCode;
            }

            public bool Equals(TypeArrayKey other)
            {
                CType[] types = _types;
                CType[] otherTypes = other._types;
                if (otherTypes == types)
                {
                    return true;
                }

                if (other._hashCode != _hashCode || otherTypes.Length != types.Length)
                {
                    return false;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] != otherTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

#if  DEBUG 
            [ExcludeFromCodeCoverage] // Typed overload should always be the method called.
#endif
            public override bool Equals(object obj)
            {
                Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
                return obj is TypeArrayKey && Equals((TypeArrayKey)obj);
            }

            public override int GetHashCode()
            {
                return _hashCode;
            }
        }

        public TypeArray AllocParams(int ctype, CType[] prgtype)
        {
            if (ctype == 0)
            {
                return s_taEmpty;
            }
            Debug.Assert(ctype == prgtype.Length);
            return AllocParams(prgtype);
        }

        public TypeArray AllocParams(int ctype, TypeArray array, int offset)
        {
            if (ctype == 0)
            {
                return s_taEmpty;
            }

            if (ctype == array.Count)
            {
                return array;
            }

            CType[] types = array.Items;
            CType[] newTypes = new CType[ctype];
            Array.ConstrainedCopy(types, offset, newTypes, 0, ctype);
            return AllocParams(newTypes);
        }

        public TypeArray AllocParams(params CType[] types)
        {
            if (types == null || types.Length == 0)
            {
                return s_taEmpty;
            }
            TypeArrayKey key = new TypeArrayKey(types);
            TypeArray result;
            if (!tableTypeArrays.TryGetValue(key, out result))
            {
                result = new TypeArray(types);
                tableTypeArrays.Add(key, result);
            }
            return result;
        }

        private TypeArray ConcatParams(CType[] prgtype1, CType[] prgtype2)
        {
            CType[] combined = new CType[prgtype1.Length + prgtype2.Length];
            Array.Copy(prgtype1, 0, combined, 0, prgtype1.Length);
            Array.Copy(prgtype2, 0, combined, prgtype1.Length, prgtype2.Length);
            return AllocParams(combined);
        }

        public TypeArray ConcatParams(TypeArray pta1, TypeArray pta2)
        {
            return ConcatParams(pta1.Items, pta2.Items);
        }
    }
}

