// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** This file exists to contain miscellaneous module-level attributes
** and other miscellaneous stuff.
**
**
** 
===========================================================*/

#nullable disable // Code in this file isn't actually executed
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security;
using System.StubHelpers;
using System.Threading.Tasks;

#if FEATURE_COMINTEROP
using System.Runtime.InteropServices.WindowsRuntime;
#endif // FEATURE_COMINTEROP

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
[assembly: DefaultDllImportSearchPathsAttribute(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]

// Add Serviceable attribute to the assembly metadata
[assembly: AssemblyMetadata("Serviceable", "True")]

namespace System
{
    static class CommonlyUsedGenericInstantiations
    {
        // This method is purely an aid for NGen to statically deduce which
        // instantiations to save in the ngen image.
        // Otherwise, the JIT-compiler gets used, which is bad for working-set.
        // Note that IBC can provide this information too.
        // However, this helps in keeping the JIT-compiler out even for
        // test scenarios which do not use IBC.
        // This can be removed after V2, when we implement other schemes
        // of keeping the JIT-compiler out for generic instantiations.

        // Method marked as NoOptimization as we don't want the JIT to
        // inline any methods or take any short-circuit paths since the 
        // instantiation closure process is driven by "fixup" references 
        // left in the final code stream.
        [MethodImplAttribute(MethodImplOptions.NoOptimization)]
        static CommonlyUsedGenericInstantiations()
        {
            // Make absolutely sure we include some of the most common 
            // instantiations here in mscorlib's ngen image.
            // Note that reference type instantiations are already included
            // automatically for us.

            // Need to sort non null, len > 1 array or paths will short-circuit
            Array.Sort<double>(new double[1]);
            Array.Sort<int>(new int[1]);
            Array.Sort<IntPtr>(new IntPtr[1]);

            new ArraySegment<byte>(new byte[1], 0, 0);

            new Dictionary<char, object>();
            new Dictionary<Guid, byte>();
            new Dictionary<Guid, object>();
            new Dictionary<Guid, Guid>(); // Added for Visual Studio 2010
            new Dictionary<short, IntPtr>();
            new Dictionary<int, byte>();
            new Dictionary<int, int>();
            new Dictionary<int, object>();
            new Dictionary<IntPtr, bool>();
            new Dictionary<IntPtr, short>();
            new Dictionary<object, bool>();
            new Dictionary<object, char>();
            new Dictionary<object, Guid>();
            new Dictionary<object, int>();
            new Dictionary<object, long>(); // Added for Visual Studio 2010
            new Dictionary<uint, WeakReference>();  // NCL team needs this
            new Dictionary<object, uint>();
            new Dictionary<uint, object>();
            new Dictionary<long, object>();

            // to genereate mdil for Dictionary instantiation when key is user defined value type
            new Dictionary<Guid, int>();

            // Microsoft.Windows.Design
            new Dictionary<System.Reflection.MemberTypes, object>();
            new EnumEqualityComparer<System.Reflection.MemberTypes>();

            // Microsoft.Expression.DesignModel
            new Dictionary<object, KeyValuePair<object, object>>();
            new Dictionary<KeyValuePair<object, object>, object>();

            NullableHelper<bool>();
            NullableHelper<byte>();
            NullableHelper<char>();
            NullableHelper<DateTime>();
            NullableHelper<decimal>();
            NullableHelper<double>();
            NullableHelper<Guid>();
            NullableHelper<short>();
            NullableHelper<int>();
            NullableHelper<long>();
            NullableHelper<float>();
            NullableHelper<TimeSpan>();
            NullableHelper<DateTimeOffset>();  // For SQL

            new List<bool>();
            new List<byte>();
            new List<char>();
            new List<DateTime>();
            new List<decimal>();
            new List<double>();
            new List<Guid>();
            new List<short>();
            new List<int>();
            new List<long>();
            new List<TimeSpan>();
            new List<sbyte>();
            new List<float>();
            new List<ushort>();
            new List<uint>();
            new List<ulong>();
            new List<IntPtr>();
            new List<KeyValuePair<object, object>>();
            new List<GCHandle>();  // NCL team needs this
            new List<DateTimeOffset>();

            new KeyValuePair<char, ushort>('\0', ushort.MinValue);
            new KeyValuePair<ushort, double>(ushort.MinValue, double.MinValue);
            new KeyValuePair<object, int>(string.Empty, int.MinValue);
            new KeyValuePair<int, int>(int.MinValue, int.MinValue);
            SZArrayHelper<bool>(null);
            SZArrayHelper<byte>(null);
            SZArrayHelper<DateTime>(null);
            SZArrayHelper<decimal>(null);
            SZArrayHelper<double>(null);
            SZArrayHelper<Guid>(null);
            SZArrayHelper<short>(null);
            SZArrayHelper<int>(null);
            SZArrayHelper<long>(null);
            SZArrayHelper<TimeSpan>(null);
            SZArrayHelper<sbyte>(null);
            SZArrayHelper<float>(null);
            SZArrayHelper<ushort>(null);
            SZArrayHelper<uint>(null);
            SZArrayHelper<ulong>(null);
            SZArrayHelper<DateTimeOffset>(null);

            SZArrayHelper<CustomAttributeTypedArgument>(null);
            SZArrayHelper<CustomAttributeNamedArgument>(null);

#pragma warning disable 4014
            // This is necessary to generate MDIL for AsyncVoidMethodBuilder
            AsyncHelper<int>();
            AsyncHelper2<int>();
            AsyncHelper3();
#pragma warning restore 4014
        }

        private static T NullableHelper<T>() where T : struct
        {
            Nullable.Compare<T>(null, null);
            Nullable.Equals<T>(null, null);
            Nullable<T> nullable = new Nullable<T>();
            return nullable.GetValueOrDefault();
        }

        private static void SZArrayHelper<T>(SZArrayHelper oSZArrayHelper)
        {
            // Instantiate common methods for IList implementation on Array
            oSZArrayHelper.get_Count<T>();
            oSZArrayHelper.get_Item<T>(0);
            oSZArrayHelper.GetEnumerator<T>();
        }

        // System.Runtime.CompilerServices.AsyncVoidMethodBuilder
        // System.Runtime.CompilerServices.TaskAwaiter
        private static async void AsyncHelper<T>()
        {
            await Task.Delay(1);
        }
        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon]
        // System.Runtime.CompilerServices.TaskAwaiter'[System.__Canon]
        private static async Task<string> AsyncHelper2<T>()
        {
            return await Task.FromResult<string>("");
        }

        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder
        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder'1[VoidTaskResult]
        private static async Task AsyncHelper3()
        {
            await Task.FromResult<string>("");
        }

#if FEATURE_COMINTEROP

        // Similar to CommonlyUsedGenericInstantiations but for instantiations of marshaling stubs used
        // for WinRT redirected interfaces. Note that we do care about reference types here as well because,
        // say, IList<string> and IList<object> cannot share marshaling stubs.
        // The methods below "call" most commonly used stub methods on redirected interfaces and take arguments
        // typed as matching instantiations of mscorlib copies of WinRT interfaces (IIterable<T>, IVector<T>,
        // IMap<K, V>, ...) which is necessary to generate all required IL stubs.

        [MethodImplAttribute(MethodImplOptions.NoOptimization)]
        private static void CommonlyUsedWinRTRedirectedInterfaceStubs()
        {
            WinRT_IEnumerable<byte>(null, null, null);
            WinRT_IEnumerable<char>(null, null, null);
            WinRT_IEnumerable<short>(null, null, null);
            WinRT_IEnumerable<ushort>(null, null, null);
            WinRT_IEnumerable<int>(null, null, null);
            WinRT_IEnumerable<uint>(null, null, null);
            WinRT_IEnumerable<long>(null, null, null);
            WinRT_IEnumerable<ulong>(null, null, null);
            WinRT_IEnumerable<float>(null, null, null);
            WinRT_IEnumerable<double>(null, null, null);

            // The underlying WinRT types for shared instantiations have to be referenced explicitly. 
            // They are not guaranteeed to be created indirectly because of generic code sharing.
            WinRT_IEnumerable<string>(null, null, null); typeof(IIterable<string>).ToString(); typeof(IIterator<string>).ToString();
            WinRT_IEnumerable<object>(null, null, null); typeof(IIterable<object>).ToString(); typeof(IIterator<object>).ToString();

            WinRT_IList<int>(null, null, null, null);
            WinRT_IList<string>(null, null, null, null); typeof(IVector<string>).ToString();
            WinRT_IList<object>(null, null, null, null); typeof(IVector<object>).ToString();

            WinRT_IReadOnlyList<int>(null, null, null);
            WinRT_IReadOnlyList<string>(null, null, null); typeof(IVectorView<string>).ToString();
            WinRT_IReadOnlyList<object>(null, null, null); typeof(IVectorView<object>).ToString();

            WinRT_IDictionary<string, int>(null, null, null, null); typeof(IMap<string, int>).ToString();
            WinRT_IDictionary<string, string>(null, null, null, null); typeof(IMap<string, string>).ToString();
            WinRT_IDictionary<string, object>(null, null, null, null); typeof(IMap<string, object>).ToString();
            WinRT_IDictionary<object, object>(null, null, null, null); typeof(IMap<object, object>).ToString();

            WinRT_IReadOnlyDictionary<string, int>(null, null, null, null); typeof(IMapView<string, int>).ToString();
            WinRT_IReadOnlyDictionary<string, string>(null, null, null, null); typeof(IMapView<string, string>).ToString();
            WinRT_IReadOnlyDictionary<string, object>(null, null, null, null); typeof(IMapView<string, object>).ToString();
            WinRT_IReadOnlyDictionary<object, object>(null, null, null, null); typeof(IMapView<object, object>).ToString();

            WinRT_Nullable<bool>();
            WinRT_Nullable<byte>();
            WinRT_Nullable<int>();
            WinRT_Nullable<uint>();
            WinRT_Nullable<long>();
            WinRT_Nullable<ulong>();
            WinRT_Nullable<float>();
            WinRT_Nullable<double>();
        }

        private static void WinRT_IEnumerable<T>(IterableToEnumerableAdapter iterableToEnumerableAdapter, EnumerableToIterableAdapter enumerableToIterableAdapter, IIterable<T> iterable)
        {
            // instantiate stubs for the one method on IEnumerable<T> and the one method on IIterable<T>
            iterableToEnumerableAdapter.GetEnumerator_Stub<T>();
            enumerableToIterableAdapter.First_Stub<T>();
        }

        private static void WinRT_IList<T>(VectorToListAdapter vectorToListAdapter, VectorToCollectionAdapter vectorToCollectionAdapter, ListToVectorAdapter listToVectorAdapter, IVector<T> vector)
        {
            WinRT_IEnumerable<T>(null, null, null);

            // instantiate stubs for commonly used methods on IList<T> and ICollection<T>
            vectorToListAdapter.Indexer_Get<T>(0);
            vectorToListAdapter.Indexer_Set<T>(0, default);
            vectorToListAdapter.Insert<T>(0, default);
            vectorToListAdapter.RemoveAt<T>(0);
            vectorToCollectionAdapter.Count<T>();
            vectorToCollectionAdapter.Add<T>(default);
            vectorToCollectionAdapter.Clear<T>();

            // instantiate stubs for commonly used methods on IVector<T>
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
            listToVectorAdapter.SetAt<T>(0, default);
            listToVectorAdapter.InsertAt<T>(0, default);
            listToVectorAdapter.RemoveAt<T>(0);
            listToVectorAdapter.Append<T>(default);
            listToVectorAdapter.RemoveAtEnd<T>();
            listToVectorAdapter.Clear<T>();
        }

        private static void WinRT_IReadOnlyCollection<T>(VectorViewToReadOnlyCollectionAdapter vectorViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<T>(null, null, null);

            // instantiate stubs for commonly used methods on IReadOnlyCollection<T>
            vectorViewToReadOnlyCollectionAdapter.Count<T>();
        }

        private static void WinRT_IReadOnlyList<T>(IVectorViewToIReadOnlyListAdapter vectorToListAdapter, IReadOnlyListToIVectorViewAdapter listToVectorAdapter, IVectorView<T> vectorView)
        {
            WinRT_IEnumerable<T>(null, null, null);
            WinRT_IReadOnlyCollection<T>(null);

            // instantiate stubs for commonly used methods on IReadOnlyList<T>
            vectorToListAdapter.Indexer_Get<T>(0);

            // instantiate stubs for commonly used methods on IVectorView<T>
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
        }

        private static void WinRT_IDictionary<K, V>(MapToDictionaryAdapter mapToDictionaryAdapter, MapToCollectionAdapter mapToCollectionAdapter, DictionaryToMapAdapter dictionaryToMapAdapter, IMap<K, V> map)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);

            // instantiate stubs for commonly used methods on IDictionary<K, V> and ICollection<KeyValuePair<K, V>>
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default);
            mapToDictionaryAdapter.Indexer_Set<K, V>(default, default);
            mapToDictionaryAdapter.ContainsKey<K, V>(default);
            mapToDictionaryAdapter.Add<K, V>(default, default);
            mapToDictionaryAdapter.Remove<K, V>(default);
            mapToDictionaryAdapter.TryGetValue<K, V>(default, out dummy);
            mapToCollectionAdapter.Count<K, V>();
            mapToCollectionAdapter.Add<K, V>(new KeyValuePair<K, V>(default, default));
            mapToCollectionAdapter.Clear<K, V>();

            // instantiate stubs for commonly used methods on IMap<K, V>
            dictionaryToMapAdapter.Lookup<K, V>(default);
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default);
            dictionaryToMapAdapter.Insert<K, V>(default, default);
            dictionaryToMapAdapter.Remove<K, V>(default);
            dictionaryToMapAdapter.Clear<K, V>();
        }

        private static void WinRT_IReadOnlyDictionary<K, V>(IMapViewToIReadOnlyDictionaryAdapter mapToDictionaryAdapter, IReadOnlyDictionaryToIMapViewAdapter dictionaryToMapAdapter, IMapView<K, V> mapView, MapViewToReadOnlyCollectionAdapter mapViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);
            WinRT_IReadOnlyCollection<KeyValuePair<K, V>>(null);

            // instantiate stubs for commonly used methods on IReadOnlyDictionary<K, V>
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default);
            mapToDictionaryAdapter.ContainsKey<K, V>(default);
            mapToDictionaryAdapter.TryGetValue<K, V>(default, out dummy);

            // instantiate stubs for commonly used methods in IReadOnlyCollection<T>
            mapViewToReadOnlyCollectionAdapter.Count<K, V>();

            // instantiate stubs for commonly used methods on IMapView<K, V>
            dictionaryToMapAdapter.Lookup<K, V>(default);
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default);
        }

        private static void WinRT_Nullable<T>() where T : struct
        {
            Nullable<T> nullable = new Nullable<T>();
            NullableMarshaler.ConvertToNative(ref nullable);
            NullableMarshaler.ConvertToManagedRetVoid(IntPtr.Zero, ref nullable);
        }

#endif // FEATURE_COMINTEROP
    }
}
