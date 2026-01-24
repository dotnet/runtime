// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private ContinuationTypeHashtable _continuationTypeHashtable;

        public MetadataType GetContinuationType(GCPointerMap pointerMap)
        {
            var cont = _continuationTypeHashtable.GetOrCreateValue(pointerMap);
            _validTypes.TryAdd(cont);
            return cont;
        }

        private sealed class ContinuationTypeHashtable : LockFreeReaderHashtable<GCPointerMap, AsyncContinuationType>
        {
            private readonly CompilerTypeSystemContext _context;

            public ContinuationTypeHashtable(CompilerTypeSystemContext context)
                => _context = context;

            protected override int GetKeyHashCode(GCPointerMap key)
                => key.GetHashCode();
            protected override int GetValueHashCode(AsyncContinuationType value)
                => value.PointerMap.GetHashCode();
            protected override bool CompareKeyToValue(GCPointerMap key, AsyncContinuationType value)
                => key.Equals(value.PointerMap);
            protected override bool CompareValueToValue(AsyncContinuationType value1, AsyncContinuationType value2)
                => value1.PointerMap.Equals(value2.PointerMap);
            protected override AsyncContinuationType CreateValueFromKey(GCPointerMap key)
            {
                return new AsyncContinuationType(_context.ContinuationType, key);
            }
        }
    }
}
