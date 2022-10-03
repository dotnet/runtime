// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    // Functionality related to deterministic ordering of types and members
    //
    // Many places within a compiler need a way to generate deterministically ordered lists
    // that may be a result of non-deterministic processes. Multi-threaded compilation is a good
    // example of such source of nondeterminism. Even though the order of the results of a multi-threaded
    // compilation may be non-deterministic, the output of the compiler needs to be deterministic.
    // The compiler achieves that by sorting the results of the compilation.
    //
    // While it's easy to compare types that are in the same category (e.g. named types within an assembly
    // could be compared by their names or tokens), it's difficult to have a scheme where each category would know
    // how to compare itself to other categories (does "array of pointers to uint" sort before a "byref
    // to an object"?). The nature of the type system potentially allows for an unlimited number of TypeDesc
    // descendants.
    //
    // We solve this problem by only requiring each TypeDesc or MethodDesc descendant to know how
    // to sort itself with respect to other instances of the same type.
    // Comparisons between different categories of types are centralized to a single location that
    // can provide rules to sort them.
    public class TypeSystemComparer : IComparer<TypeDesc>, IComparer<MethodDesc>, IComparer<FieldDesc>, IComparer<MethodSignature>
    {
        public static TypeSystemComparer Instance { get; } = new TypeSystemComparer();

        public int Compare(TypeDesc x, TypeDesc y)
        {
            if (x == y)
            {
                return 0;
            }

            int codeX = x.ClassCode;
            int codeY = y.ClassCode;
            if (codeX == codeY)
            {
                Debug.Assert(x.GetType() == y.GetType());

                int result = x.CompareToImpl(y, this);

                // We did a reference equality check above so an "Equal" result is not expected
                Debug.Assert(result != 0);

                return result;
            }
            else
            {
                Debug.Assert(x.GetType() != y.GetType());
                return codeX > codeY ? -1 : 1;
            }
        }

        internal int CompareWithinClass<T>(T x, T y) where T : TypeDesc
        {
            Debug.Assert(x.GetType() == y.GetType());

            if (x == y)
                return 0;

            int result = x.CompareToImpl(y, this);

            // We did a reference equality check above so an "Equal" result is not expected
            Debug.Assert(result != 0);

            return result;
        }

        public int Compare(MethodDesc x, MethodDesc y)
        {
            if (x == y)
            {
                return 0;
            }

            int codeX = x.ClassCode;
            int codeY = y.ClassCode;
            if (codeX == codeY)
            {
                Debug.Assert(x.GetType() == y.GetType());

                int result = x.CompareToImpl(y, this);

                // We did a reference equality check above so an "Equal" result is not expected
                Debug.Assert(result != 0);

                return result;
            }
            else
            {
                Debug.Assert(x.GetType() != y.GetType());
                return codeX > codeY ? -1 : 1;
            }
        }

        public int Compare(FieldDesc x, FieldDesc y)
        {
            if (x == y)
            {
                return 0;
            }

            int codeX = x.ClassCode;
            int codeY = y.ClassCode;
            if (codeX == codeY)
            {
                Debug.Assert(x.GetType() == y.GetType());

                int result = x.CompareToImpl(y, this);

                // We did a reference equality check above so an "Equal" result is not expected
                Debug.Assert(result != 0);

                return result;
            }
            else
            {
                Debug.Assert(x.GetType() != y.GetType());
                return codeX > codeY ? -1 : 1;
            }
        }

        public int Compare(MethodSignature x, MethodSignature y)
        {
            return x.CompareTo(y, this);
        }
    }
}

