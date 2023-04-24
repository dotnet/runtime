// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/45557,
// derived from Roslyn failure case.
//
// Bug was a GC hole with STOREIND of LCL_VAR_ADDR/LCL_FLD_ADDR.
// We were updating the GC liveness of the ADDR node, but
// genUpdateLife() expects to get the parent node, so no
// liveness was ever updated.
//
// The bad code cases in the libraries were related to uses of
// System.Collections.Immutable.ImmutableArray
// where we struct promote fields who are themselves single-element
// gc ref structs that are kept on the stack and not in registers.
// In all cases, the liveness of the stack local was not reflected
// in codegen's GC sets, but it was reflected in the emitter's GC
// sets, so it was marked as a GC lifetime. However, that lifetime
// would get cut short if we hit a call site before the last use,
// as calls (sometimes) carry the full set of live variables across
// the call. So, variables not in this set (including the
// "accidental" emitter-created GC lifetimes here) would get killed,
// leaving a hole between the intermediate call and actual stack
// local last use.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_45557
{
    internal readonly struct ObjectBinderSnapshot
    {
        private readonly Dictionary<Type, int> _typeToIndex;
        private readonly ImmutableArray<Type> _types;
        private readonly ImmutableArray<Func<Object, Object>> _typeReaders;
 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]          // this needs to get inlined to cause the failure
        public ObjectBinderSnapshot(
            Dictionary<Type, int> typeToIndex,
            List<Type> types,
            List<Func<Object, Object>> typeReaders)
        {
            _typeToIndex = new Dictionary<Type, int>(typeToIndex);
            _types = types.ToImmutableArray();                      // stack variable here would go live
            _typeReaders = typeReaders.ToImmutableArray();          // it would get erroneously killed in GC info here
            GC.Collect();                                           // try to cause a crash by collecting the variable
            Console.WriteLine($"{_types.Length}");                  // use the collected variable; should crash (most of the time, depending on GC behavior)
        }

        public string SomeValue => _types.ToString();
    }

    internal static class ObjectBinder
    {
        private static readonly object s_gate = new();
 
        private static ObjectBinderSnapshot? s_lastSnapshot = null;
 
        private static readonly Dictionary<Type, int> s_typeToIndex = new();
        private static readonly List<Type> s_types = new();
        private static readonly List<Func<Object, Object>> s_typeReaders = new();
 
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ObjectBinderSnapshot GetSnapshot()
        {
            lock (s_gate)
            {
                if (s_lastSnapshot == null)
                {
                    s_lastSnapshot = new ObjectBinderSnapshot(s_typeToIndex, s_types, s_typeReaders);
                }
 
                return s_lastSnapshot.Value;
            }
        }
    }

    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            ObjectBinderSnapshot o = ObjectBinder.GetSnapshot();
            Console.WriteLine($"Test output: {o.SomeValue}");

            return 100; // success if we got here without crashing
        }
    }
}
