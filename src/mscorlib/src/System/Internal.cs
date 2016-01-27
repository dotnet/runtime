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
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Reflection;
using System.Security;
using System.StubHelpers;
using System.Threading.Tasks;

#if FEATURE_COMINTEROP

using System.Runtime.InteropServices.WindowsRuntime;

[assembly:Guid("BED7F4EA-1A96-11d2-8F08-00A0C9A6186D")]

// The following attribute are required to ensure COM compatibility.
[assembly:System.Runtime.InteropServices.ComCompatibleVersion(1, 0, 3300, 0)]
[assembly:System.Runtime.InteropServices.TypeLibVersion(2, 4)]

#endif // FEATURE_COMINTEROP

[assembly:DefaultDependencyAttribute(LoadHint.Always)]
// mscorlib would like to have its literal strings frozen if possible
[assembly: System.Runtime.CompilerServices.StringFreezingAttribute()]

namespace System
{
    static class Internal
    {
        // This method is purely an aid for NGen to statically deduce which
        // instantiations to save in the ngen image.
        // Otherwise, the JIT-compiler gets used, which is bad for working-set.
        // Note that IBC can provide this information too.
        // However, this helps in keeping the JIT-compiler out even for
        // test scenarios which do not use IBC.
        // This can be removed after V2, when we implement other schemes
        // of keeping the JIT-compiler out for generic instantiations.

        static void CommonlyUsedGenericInstantiations()
        {
            // Make absolutely sure we include some of the most common 
            // instantiations here in mscorlib's ngen image.
            // Note that reference type instantiations are already included
            // automatically for us.

            System.Array.Sort<double>(null);
            System.Array.Sort<int>(null);
            System.Array.Sort<IntPtr>(null);
            
            new ArraySegment<byte>(new byte[1], 0, 0);

            new Dictionary<Char, Object>();
            new Dictionary<Guid, Byte>();
            new Dictionary<Guid, Object>();
            new Dictionary<Guid, Guid>(); // Added for Visual Studio 2010
            new Dictionary<Int16, IntPtr>();
            new Dictionary<Int32, Byte>();
            new Dictionary<Int32, Int32>();
            new Dictionary<Int32, Object>();
            new Dictionary<IntPtr, Boolean>();
            new Dictionary<IntPtr, Int16>();
            new Dictionary<Object, Boolean>();
            new Dictionary<Object, Char>();
            new Dictionary<Object, Guid>();
            new Dictionary<Object, Int32>();
            new Dictionary<Object, Int64>(); // Added for Visual Studio 2010
            new Dictionary<uint, WeakReference>();  // NCL team needs this
            new Dictionary<Object, UInt32>();
            new Dictionary<UInt32, Object>();
            new Dictionary<Int64, Object>();
#if FEATURE_CORECLR
            // to genereate mdil for Dictionary instantiation when key is user defined value type
            new Dictionary<Guid, Int32>();
#endif

        // Microsoft.Windows.Design
            new Dictionary<System.Reflection.MemberTypes, Object>();
            new EnumEqualityComparer<System.Reflection.MemberTypes>();

        // Microsoft.Expression.DesignModel
            new Dictionary<Object, KeyValuePair<Object,Object>>();
            new Dictionary<KeyValuePair<Object,Object>, Object>();

            NullableHelper<Boolean>();
            NullableHelper<Byte>();
            NullableHelper<Char>();
            NullableHelper<DateTime>(); 
            NullableHelper<Decimal>(); 
            NullableHelper<Double>();
            NullableHelper<Guid>();
            NullableHelper<Int16>();
            NullableHelper<Int32>();
            NullableHelper<Int64>();
            NullableHelper<Single>();
            NullableHelper<TimeSpan>();
            NullableHelper<DateTimeOffset>();  // For SQL

            new List<Boolean>();
            new List<Byte>();
            new List<Char>();
            new List<DateTime>();
            new List<Decimal>();
            new List<Double>();
            new List<Guid>();
            new List<Int16>();
            new List<Int32>();
            new List<Int64>();
            new List<TimeSpan>();
            new List<SByte>();
            new List<Single>();
            new List<UInt16>();
            new List<UInt32>();
            new List<UInt64>();
            new List<IntPtr>();
            new List<KeyValuePair<Object, Object>>();
            new List<GCHandle>();  // NCL team needs this
            new List<DateTimeOffset>();

            new KeyValuePair<Char, UInt16>('\0', UInt16.MinValue);
            new KeyValuePair<UInt16, Double>(UInt16.MinValue, Double.MinValue);
            new KeyValuePair<Object, Int32>(String.Empty, Int32.MinValue);
            new KeyValuePair<Int32, Int32>(Int32.MinValue, Int32.MinValue);            
            SZArrayHelper<Boolean>(null);
            SZArrayHelper<Byte>(null);
            SZArrayHelper<DateTime>(null);
            SZArrayHelper<Decimal>(null);
            SZArrayHelper<Double>(null);
            SZArrayHelper<Guid>(null);
            SZArrayHelper<Int16>(null);
            SZArrayHelper<Int32>(null);
            SZArrayHelper<Int64>(null);
            SZArrayHelper<TimeSpan>(null);
            SZArrayHelper<SByte>(null);
            SZArrayHelper<Single>(null);
            SZArrayHelper<UInt16>(null);
            SZArrayHelper<UInt32>(null);
            SZArrayHelper<UInt64>(null);
            SZArrayHelper<DateTimeOffset>(null);

            SZArrayHelper<CustomAttributeTypedArgument>(null);
            SZArrayHelper<CustomAttributeNamedArgument>(null);

#if FEATURE_CORECLR
#pragma warning disable 4014
            // This is necessary to generate MDIL for AsyncVoidMethodBuilder
            AsyncHelper<int>();
            AsyncHelper2<int>();
            AsyncHelper3();
#pragma warning restore 4014
#endif
        }

        static T NullableHelper<T>() where T : struct
        {
            Nullable.Compare<T>(null, null);    
            Nullable.Equals<T>(null, null); 
            Nullable<T> nullable = new Nullable<T>();
            return nullable.GetValueOrDefault();
        }       

        static void SZArrayHelper<T>(SZArrayHelper oSZArrayHelper)
        {
            // Instantiate common methods for IList implementation on Array
            oSZArrayHelper.get_Count<T>();
            oSZArrayHelper.get_Item<T>(0);
            oSZArrayHelper.GetEnumerator<T>();
        }

#if FEATURE_CORECLR
        // System.Runtime.CompilerServices.AsyncVoidMethodBuilder
        // System.Runtime.CompilerServices.TaskAwaiter
        static async void AsyncHelper<T>()
        {
            await Task.Delay(1);
        }
        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder`1[System.__Canon]
        // System.Runtime.CompilerServices.TaskAwaiter'[System.__Canon]
        static async Task<String> AsyncHelper2<T>()
        {
            return await Task.FromResult<string>("");
        }

        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder
        // System.Runtime.CompilerServices.AsyncTaskMethodBuilder'1[VoidTaskResult]
        static async Task AsyncHelper3()
        {
            await Task.FromResult<string>("");
        }
#endif

#if FEATURE_COMINTEROP

        // Similar to CommonlyUsedGenericInstantiations but for instantiations of marshaling stubs used
        // for WinRT redirected interfaces. Note that we do care about reference types here as well because,
        // say, IList<string> and IList<object> cannot share marshaling stubs.
        // The methods below "call" most commonly used stub methods on redirected interfaces and take arguments
        // typed as matching instantiations of mscorlib copies of WinRT interfaces (IIterable<T>, IVector<T>,
        // IMap<K, V>, ...) which is necessary to generate all required IL stubs.

        [SecurityCritical]
        static void CommonlyUsedWinRTRedirectedInterfaceStubs()
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

        [SecurityCritical]
        static void WinRT_IEnumerable<T>(IterableToEnumerableAdapter iterableToEnumerableAdapter, EnumerableToIterableAdapter enumerableToIterableAdapter, IIterable<T> iterable)
        {
            // instantiate stubs for the one method on IEnumerable<T> and the one method on IIterable<T>
            iterableToEnumerableAdapter.GetEnumerator_Stub<T>();
            enumerableToIterableAdapter.First_Stub<T>();
        }

        [SecurityCritical]
        static void WinRT_IList<T>(VectorToListAdapter vectorToListAdapter, VectorToCollectionAdapter vectorToCollectionAdapter, ListToVectorAdapter listToVectorAdapter, IVector<T> vector)
        {
            WinRT_IEnumerable<T>(null, null, null);

            // instantiate stubs for commonly used methods on IList<T> and ICollection<T>
            vectorToListAdapter.Indexer_Get<T>(0);
            vectorToListAdapter.Indexer_Set<T>(0, default(T));
            vectorToListAdapter.Insert<T>(0, default(T));
            vectorToListAdapter.RemoveAt<T>(0);
            vectorToCollectionAdapter.Count<T>();
            vectorToCollectionAdapter.Add<T>(default(T));
            vectorToCollectionAdapter.Clear<T>();

            // instantiate stubs for commonly used methods on IVector<T>
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
            listToVectorAdapter.SetAt<T>(0, default(T));
            listToVectorAdapter.InsertAt<T>(0, default(T));
            listToVectorAdapter.RemoveAt<T>(0);
            listToVectorAdapter.Append<T>(default(T));
            listToVectorAdapter.RemoveAtEnd<T>();
            listToVectorAdapter.Clear<T>();
        }

        [SecurityCritical]
        static void WinRT_IReadOnlyCollection<T>(VectorViewToReadOnlyCollectionAdapter vectorViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<T>(null, null, null);

            // instantiate stubs for commonly used methods on IReadOnlyCollection<T>
            vectorViewToReadOnlyCollectionAdapter.Count<T>();
        }

        [SecurityCritical]
        static void WinRT_IReadOnlyList<T>(IVectorViewToIReadOnlyListAdapter vectorToListAdapter, IReadOnlyListToIVectorViewAdapter listToVectorAdapter, IVectorView<T> vectorView)
        {
            WinRT_IEnumerable<T>(null, null, null);
            WinRT_IReadOnlyCollection<T>(null);

            // instantiate stubs for commonly used methods on IReadOnlyList<T>
            vectorToListAdapter.Indexer_Get<T>(0);

            // instantiate stubs for commonly used methods on IVectorView<T>
            listToVectorAdapter.GetAt<T>(0);
            listToVectorAdapter.Size<T>();
        }

        [SecurityCritical]
        static void WinRT_IDictionary<K, V>(MapToDictionaryAdapter mapToDictionaryAdapter, MapToCollectionAdapter mapToCollectionAdapter, DictionaryToMapAdapter dictionaryToMapAdapter, IMap<K, V> map)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);

            // instantiate stubs for commonly used methods on IDictionary<K, V> and ICollection<KeyValuePair<K, V>>
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default(K));
            mapToDictionaryAdapter.Indexer_Set<K, V>(default(K), default(V));
            mapToDictionaryAdapter.ContainsKey<K, V>(default(K));
            mapToDictionaryAdapter.Add<K, V>(default(K), default(V));
            mapToDictionaryAdapter.Remove<K, V>(default(K));
            mapToDictionaryAdapter.TryGetValue<K, V>(default(K), out dummy);
            mapToCollectionAdapter.Count<K, V>();
            mapToCollectionAdapter.Add<K, V>(new KeyValuePair<K, V>(default(K), default(V)));
            mapToCollectionAdapter.Clear<K, V>();

            // instantiate stubs for commonly used methods on IMap<K, V>
            dictionaryToMapAdapter.Lookup<K, V>(default(K));
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default(K));
            dictionaryToMapAdapter.Insert<K, V>(default(K), default(V));
            dictionaryToMapAdapter.Remove<K, V>(default(K));
            dictionaryToMapAdapter.Clear<K, V>();
        }

        [SecurityCritical]
        static void WinRT_IReadOnlyDictionary<K, V>(IMapViewToIReadOnlyDictionaryAdapter mapToDictionaryAdapter, IReadOnlyDictionaryToIMapViewAdapter dictionaryToMapAdapter, IMapView<K, V> mapView, MapViewToReadOnlyCollectionAdapter mapViewToReadOnlyCollectionAdapter)
        {
            WinRT_IEnumerable<KeyValuePair<K, V>>(null, null, null);
            WinRT_IReadOnlyCollection<KeyValuePair<K, V>>(null);

            // instantiate stubs for commonly used methods on IReadOnlyDictionary<K, V>
            V dummy;
            mapToDictionaryAdapter.Indexer_Get<K, V>(default(K));
            mapToDictionaryAdapter.ContainsKey<K, V>(default(K));
            mapToDictionaryAdapter.TryGetValue<K, V>(default(K), out dummy);

            // instantiate stubs for commonly used methods in IReadOnlyCollection<T>
            mapViewToReadOnlyCollectionAdapter.Count<K, V>();

            // instantiate stubs for commonly used methods on IMapView<K, V>
            dictionaryToMapAdapter.Lookup<K, V>(default(K));
            dictionaryToMapAdapter.Size<K, V>();
            dictionaryToMapAdapter.HasKey<K, V>(default(K));
        }

        [SecurityCritical]
        static void WinRT_Nullable<T>() where T : struct
        {
            Nullable<T> nullable = new Nullable<T>();
            NullableMarshaler.ConvertToNative(ref nullable);
            NullableMarshaler.ConvertToManagedRetVoid(IntPtr.Zero, ref nullable);
        }

#endif // FEATURE_COMINTEROP
    }
}
