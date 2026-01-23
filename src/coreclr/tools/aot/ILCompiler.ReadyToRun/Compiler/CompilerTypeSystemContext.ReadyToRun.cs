// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private ContinuationTypeHashtable _continuationTypeHashtable;

        public MetadataType GetContinuationType(GCPointerMap pointerMap, MethodDesc owningMethod)
        {
            var cont = _continuationTypeHashtable.GetOrCreateValue(new(pointerMap, owningMethod));
            _validTypes.TryAdd(cont);
            return cont;
        }

        private readonly struct ContinuationTypeHashtableKey : IEquatable<ContinuationTypeHashtableKey>
        {
            public GCPointerMap PointerMap { get; }
            public MethodDesc OwningMethod { get; }
            public ContinuationTypeHashtableKey(GCPointerMap pointerMap, MethodDesc owningMethod)
            {
                PointerMap = pointerMap;
                Debug.Assert(owningMethod.IsAsyncCall());
                OwningMethod = owningMethod;
            }
            public bool Equals(ContinuationTypeHashtableKey other)
            {
                return PointerMap.Equals(other.PointerMap) && OwningMethod.Equals(other.OwningMethod);
            }
            public override int GetHashCode()
            {
                return HashCode.Combine(PointerMap.GetHashCode(), OwningMethod.GetHashCode());
            }
        }

        private sealed class ContinuationTypeHashtable : LockFreeReaderHashtable<ContinuationTypeHashtableKey, AsyncContinuationType>
        {
            private readonly CompilerTypeSystemContext _context;

            public ContinuationTypeHashtable(CompilerTypeSystemContext context)
                => _context = context;

            protected override int GetKeyHashCode(ContinuationTypeHashtableKey key)
                => HashCode.Combine(key.PointerMap.GetHashCode(), key.OwningMethod.GetHashCode());
            protected override int GetValueHashCode(AsyncContinuationType value)
                => HashCode.Combine(value.PointerMap.GetHashCode(), value.OwningMethod.GetHashCode());
            protected override bool CompareKeyToValue(ContinuationTypeHashtableKey key, AsyncContinuationType value)
                => key.PointerMap.Equals(value.PointerMap) && key.OwningMethod.Equals(value.OwningMethod);
            protected override bool CompareValueToValue(AsyncContinuationType value1, AsyncContinuationType value2)
                => value1.PointerMap.Equals(value2.PointerMap) && value1.OwningMethod.Equals(value2.OwningMethod);
            protected override AsyncContinuationType CreateValueFromKey(ContinuationTypeHashtableKey key)
            {
                return new AsyncContinuationType(_context.ContinuationType, key.PointerMap, key.OwningMethod);
            }
        }
    }
}
