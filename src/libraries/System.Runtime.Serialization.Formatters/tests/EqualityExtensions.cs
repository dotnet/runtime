// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class EqualityExtensions
    {
        private static MethodInfo GetExtensionMethod(Type extendedType)
        {
            if (extendedType.IsGenericType)
            {
                MethodInfo method = typeof(EqualityExtensions).GetMethods()
                        ?.SingleOrDefault(m =>
                            m.Name == "IsEqual" &&
                            m.GetParameters().Length == 2 &&
                            m.GetParameters()[0].ParameterType.Name == extendedType.Name &&
                            m.IsGenericMethodDefinition);
                if (method != null)
                    return method.MakeGenericMethod(extendedType.GenericTypeArguments[0]);
            }

            return typeof(EqualityExtensions).GetMethod("IsEqual", new[] { extendedType, extendedType });
        }

        public static bool CheckEquals(object objA, object objB)
        {
            if (objA == null && objB == null)
                return true;

            if (objA != null && objB != null)
            {
                object equalityResult = null;
                Type objType = objA.GetType();

                // Check if custom equality extension method is available
                MethodInfo customEqualityCheck = GetExtensionMethod(objType);
                if (customEqualityCheck != null)
                {
                    equalityResult = customEqualityCheck.Invoke(objA, new object[] { objA, objB });
                }
                else
                {
                    // Check if object.Equals(object) is overridden and if not check if there is a more concrete equality check implementation
                    bool equalsNotOverridden = objType.GetMethod("Equals", new Type[] { typeof(object) }).DeclaringType == typeof(object);
                    if (equalsNotOverridden)
                    {
                        // If type doesn't override Equals(object) method then check if there is a more concrete implementation
                        // e.g. if type implements IEquatable<T>.
                        MethodInfo equalsMethod = objType.GetMethod("Equals", new Type[] { objType });
                        if (equalsMethod.DeclaringType != typeof(object))
                        {
                            equalityResult = equalsMethod.Invoke(objA, new object[] { objB });
                        }
                    }
                }

                if (equalityResult != null)
                {
                    return (bool)equalityResult;
                }
            }

            if (objA is IEnumerable objAEnumerable && objB is IEnumerable objBEnumerable)
            {
                return CheckSequenceEquals(objAEnumerable, objBEnumerable);
            }

            return objA.Equals(objB);
        }

        public static bool CheckSequenceEquals(this IEnumerable @this, IEnumerable other)
        {
            if (@this == null || other == null)
                return @this == other;

            if (@this.GetType() != other.GetType())
                return false;

            IEnumerator eA = null;
            IEnumerator eB = null;

            try
            {
                eA = (@this as IEnumerable).GetEnumerator();
                eB = (@this as IEnumerable).GetEnumerator();
                while (true)
                {
                    bool moved = eA.MoveNext();
                    if (moved != eB.MoveNext())
                        return false;
                    if (!moved)
                        return true;
                    if (eA.Current == null && eB.Current == null)
                        return true;
                    if (!CheckEquals(eA.Current, eB.Current))
                        return true;
                }
            }
            finally
            {
                (eA as IDisposable)?.Dispose();
                (eB as IDisposable)?.Dispose();
            }
        }

        public static bool IsEqual(this WeakReference @this, WeakReference other)
        {
            if (@this == null || other == null)
                return false;

            if (@this.TrackResurrection != other.TrackResurrection)
                return false;

            // When WeakReference is deserialized, the object it wraps may blip into and out of
            // existence before we get a chance to compare it, since there are no strong references
            // to it such that it can then be immediately collected.  Therefore, if we can get both
            // values, great, compare them.  Otherwise, consider them equal.
            object a = @this.Target;
            object b = other.Target;
            return a != null && b != null ? Equals(a, b) : true;
        }

        public static bool IsEqual<T>(this WeakReference<T> @this, WeakReference<T> other)
            where T : class
        {
            if (@this == null || other == null)
                return false;

            // When WeakReference is deserialized, the object it wraps may blip into and out of
            // existence before we get a chance to compare it, since there are no strong references
            // to it such that it can then be immediately collected.  Therefore, if we can get both
            // values, great, compare them.  Otherwise, consider them equal.
            return @this.TryGetTarget(out T thisTarget) && other.TryGetTarget(out T otherTarget) ?
                Equals(thisTarget, otherTarget) :
                true;
        }

        public static bool IsEqual<T>(this Lazy<T> @this, Lazy<T> other)
        {
            // Force value creation for lazy original object
            T thisVal = @this.Value;
            T otherVal = other.Value;

            return @this != null &&
                other != null &&
                @this.IsValueCreated == other.IsValueCreated &&
                Object.Equals(thisVal, otherVal);
        }

        public static bool IsEqual(this StreamingContext @this, StreamingContext other)
        {
            return @this.State == @other.State &&
                Object.Equals(@this.Context, other.Context);
        }

        public static bool IsEqual(this CookieContainer @this, CookieContainer other)
        {
            return @this != null &&
                other != null &&
                @this.Capacity == other.Capacity &&
                @this.Count == other.Count &&
                @this.MaxCookieSize == other.MaxCookieSize &&
                @this.PerDomainCapacity == other.PerDomainCapacity;
        }

        public static bool IsEqual(this DataSet @this, DataSet other)
        {
            return @this != null &&
                @other != null &&
                @this.DataSetName == other.DataSetName &&
                @this.Namespace == other.Namespace &&
                @this.Prefix == other.Prefix &&
                @this.CaseSensitive == other.CaseSensitive &&
                @this.Locale.LCID == other.Locale.LCID &&
                @this.EnforceConstraints == other.EnforceConstraints &&
                @this.ExtendedProperties?.Count == other.ExtendedProperties?.Count &&
                CheckEquals(@this.ExtendedProperties, other.ExtendedProperties);
        }

        public static bool IsEqual(this DataTable @this, DataTable other)
        {
            Assert.Equal(@this.TableName, other.TableName);
            return @this != null &&
                other != null &&
                @this.RemotingFormat == other.RemotingFormat && 
                @this.TableName == other.TableName &&
                @this.Namespace == other.Namespace &&
                @this.Prefix == other.Prefix &&
                @this.CaseSensitive == other.CaseSensitive &&
                @this.Locale.LCID == other.Locale.LCID &&
                @this.MinimumCapacity == other.MinimumCapacity;
        }

        public static bool IsEqual(this Comparer @this, Comparer other)
        {
            if(@this == null || other == null)
            {
                return false;
            }

            // The compareInfos are internal and get reflection blocked on .NET Native, so use
            // GetObjectData to get them
            SerializationInfo thisInfo = new SerializationInfo(typeof(Comparer), new FormatterConverter());
            @this.GetObjectData(thisInfo, new StreamingContext());
            CompareInfo thisCompareInfo = (CompareInfo)thisInfo.GetValue("CompareInfo", typeof(CompareInfo));

            SerializationInfo otherInfo = new SerializationInfo(typeof(Comparer), new FormatterConverter());
            other.GetObjectData(otherInfo, new StreamingContext());
            CompareInfo otherCompareInfo = (CompareInfo)otherInfo.GetValue("CompareInfo", typeof(CompareInfo));
            
            return Object.Equals(thisCompareInfo, otherCompareInfo);
        }


        public static bool IsEqual(this DictionaryEntry @this, DictionaryEntry other)
        {
            return Object.Equals(@this.Key, other.Key) && Object.Equals(@this.Value, other.Value);
        }

        public static bool IsEqual(this StringDictionary @this, StringDictionary other)
        {
            return @this != null &&
                other != null &&
                @this.Count == other.Count;
        }

        public static bool IsEqual(this ArrayList @this, ArrayList other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Capacity == other.Capacity &&
                @this.Count == other.Count &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            for (int i = 0; i < @this.Count; i++)
            {
                if (!CheckEquals(@this[i], other[i]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this BitArray @this, BitArray other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Length == other.Length &&
                @this.Count == other.Count &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            return CheckSequenceEquals(@this, other);
        }

        public static bool IsEqual(this Dictionary<int, string> @this, Dictionary<int, string> other)
        {
            if (!(@this != null &&
                other != null &&
                CheckEquals(@this.Comparer, other.Comparer) &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values)))
                return false;

            foreach (var kv in @this)
            {
                if (@this[kv.Key] != other[kv.Key])
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this PointEqualityComparer @this, PointEqualityComparer other)
        {
            return @this != null &&
                other != null;
        }

        public static bool IsEqual(this HashSet<Point> @this, HashSet<Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                CheckEquals(@this.Comparer, other.Comparer)))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this LinkedListNode<Point> @this, LinkedListNode<Point> other)
        {
            if (@this == null && other == null)
                return true;

            return @this != null
                && other != null &&
                CheckEquals(@this.Value, other.Value);
        }

        public static bool IsEqual(this LinkedList<Point> @this, LinkedList<Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                IsEqual(@this.First, other.First) &&
                IsEqual(@this.Last, other.Last)))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this List<int> @this, List<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Capacity == other.Capacity &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Queue<int> @this, Queue<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this SortedList<int, Point> @this, SortedList<int, Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Capacity == other.Capacity &&
                CheckEquals(@this.Comparer, other.Comparer) &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values)))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this SortedSet<Point> @this, SortedSet<Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                CheckEquals(@this.Comparer, other.Comparer) &&
                CheckEquals(@this.Min, other.Min) &&
                CheckEquals(@this.Max, other.Max)))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Stack<Point> @this, Stack<Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Hashtable @this, Hashtable other)
        {
            if (!(@this != null &&
                other != null &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsSynchronized == other.IsSynchronized &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values) &&
                @this.Count == other.Count))
                return false;
            
            foreach (var key in @this.Keys)
            {
                if (!CheckEquals(@this[key], other[key]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this Collection<int> @this, Collection<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this ObservableCollection<int> @this, ObservableCollection<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this ReadOnlyCollection<int> @this, ReadOnlyCollection<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this ReadOnlyDictionary<int, string> @this, ReadOnlyDictionary<int, string> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values) &&
                @this.Count == other.Count))
                return false;

            foreach (var kv in @this)
            {
                if (kv.Value != other[kv.Key])
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this ReadOnlyObservableCollection<int> @this, ReadOnlyObservableCollection<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Queue @this, Queue other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this SortedList @this, SortedList other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Capacity == other.Capacity &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values) &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this HybridDictionary @this, HybridDictionary other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsSynchronized == other.IsSynchronized &&
                @this.Values.CheckSequenceEquals(other.Values)))
                return false;

            foreach (var key in @this.Keys)
            {
                if (!CheckEquals(@this[key], other[key]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this ListDictionary @this, ListDictionary other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsSynchronized == other.IsSynchronized &&
                @this.Values.CheckSequenceEquals(other.Values)))
                return false;

            foreach (var key in @this.Keys)
            {
                if (!CheckEquals(@this[key], other[key]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this NameValueCollection @this, NameValueCollection other)
        {
            if (!(@this != null &&
                other != null &&
                @this.AllKeys.CheckSequenceEquals(other.AllKeys) &&
                @this.Count == other.Count &&
                @this.Keys.CheckSequenceEquals(other.Keys)))
                return false;

            foreach (var key in @this.AllKeys)
            {
                if (!CheckEquals(@this[key], other[key]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this OrderedDictionary @this, OrderedDictionary other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.IsReadOnly == other.IsReadOnly &&
                CheckEquals(@this.Keys, other.Keys) &&
                CheckEquals(@this.Values, other.Values)))
                return false;

            foreach (var key in @this.Keys)
            {
                if (!CheckEquals(@this[key], other[key]))
                    return false;
            }

            return true;
        }

        public static bool IsEqual(this StringCollection @this, StringCollection other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Stack @this, Stack other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count &&
                @this.IsSynchronized == other.IsSynchronized))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this BindingList<int> @this, BindingList<int> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.RaiseListChangedEvents == other.RaiseListChangedEvents &&
                @this.AllowNew == other.AllowNew &&
                @this.AllowEdit == other.AllowEdit &&
                @this.AllowRemove == other.AllowRemove &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this BindingList<Point> @this, BindingList<Point> other)
        {
            if (!(@this != null &&
                other != null &&
                @this.RaiseListChangedEvents == other.RaiseListChangedEvents &&
                @this.AllowNew == other.AllowNew &&
                @this.AllowEdit == other.AllowEdit &&
                @this.AllowRemove == other.AllowRemove &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this PropertyCollection @this, PropertyCollection other)
        {
            if (!(@this != null &&
                other != null &&
                @this.IsReadOnly == other.IsReadOnly &&
                @this.IsFixedSize == other.IsFixedSize &&
                @this.IsSynchronized == other.IsSynchronized &&
                @this.Keys.CheckSequenceEquals(other.Keys) &&
                @this.Values.CheckSequenceEquals(other.Values) &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this CompareInfo @this, CompareInfo other)
        {
            return @this != null &&
                other != null &&
                @this.Name == other.Name &&
                @this.LCID == other.LCID &&
                // we do not want to compare Version because it can change when changing OS
                // we do want to make sure that they are either both null or both not null
                (@this.Version != null) == (other.Version != null);
        }

        public static bool IsEqual(this SortVersion @this, SortVersion other)
        {
            return @this != null &&
                other != null &&
                @this.FullVersion == other.FullVersion &&
                @this.SortId == other.SortId;
        }

        public static bool IsEqual(this Cookie @this, Cookie other)
        {
            return @this != null &&
                other != null &&
                @this.Comment == other.Comment &&
                IsEqual(@this.CommentUri, other.CommentUri) &&
                @this.HttpOnly == other.HttpOnly &&
                @this.Discard == other.Discard &&
                @this.Domain == other.Domain &&
                @this.Expired == other.Expired &&
                CheckEquals(@this.Expires, other.Expires) &&
                @this.Name == other.Name &&
                @this.Path == other.Path &&
                @this.Port == other.Port &&
                @this.Secure == other.Secure &&
                // This needs to have m_Timestamp set by reflection in order to roundtrip correctly
                // otherwise this field will change each time you create an object and cause this to fail
                CheckEquals(@this.TimeStamp, other.TimeStamp) &&
                @this.Value == other.Value &&
                @this.Version == other.Version;
        }

        public static bool IsEqual(this CookieCollection @this, CookieCollection other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this BasicISerializableObject @this, BasicISerializableObject other)
        {
            return @this != null &&
                other != null;
        }

        public static bool IsEqual(this DerivedISerializableWithNonPublicDeserializationCtor @this, DerivedISerializableWithNonPublicDeserializationCtor other)
        {
            return @this != null &&
                other != null;
        }

        private static void GetIdsForGraphDFS(Graph<int> n, Dictionary<Graph<int>, int> ids)
        {
            if (!ids.ContainsKey(n))
            {
                ids[n] = ids.Count;
                foreach (var link in n.Links)
                {
                    GetIdsForGraphDFS(link, ids);
                }
            }
        }

        private static Dictionary<int, Graph<int>> InvertDictionary(Dictionary<Graph<int>, int> dict)
        {
            var ret = new Dictionary<int, Graph<int>>();
            foreach (var kv in dict)
            {
                Assert.False(ret.ContainsKey(kv.Value));
                ret[kv.Value] = kv.Key;
            }

            return ret;
        }

        /// <summary>
        /// Flattens the graph
        /// </summary>
        /// <param name="n">node of a graph</param>
        /// <returns>returns ((id -> node), (node -> node[]))</returns>
        private static Tuple<Dictionary<int, Graph<int>>, List<List<int>>> FlattenGraph(Graph<int> n)
        {
            // ref -> id
            var nodes = new Dictionary<Graph<int>, int>(new ReferenceComparer<Graph<int>>());
            GetIdsForGraphDFS(n, nodes);

            // id -> list of ids
            var edges = new List<List<int>>();
            for (int i = 0; i < nodes.Count; i++)
            {
                edges.Add(new List<int>());
            }

            foreach (var kv in nodes)
            {
                List<int> links = edges[kv.Value];
                foreach (var link in kv.Key.Links)
                {
                    links.Add(nodes[link]);
                }
            }

            return new Tuple<Dictionary<int, Graph<int>>, List<List<int>>>(InvertDictionary(nodes), edges);
        }

        public static bool IsEqual(this Graph<int> @this, Graph<int> other)
        {
            var thisFlattened = FlattenGraph(@this);
            var otherFlattened = FlattenGraph(other);

            if (thisFlattened.Item1.Count != otherFlattened.Item1.Count ||
                thisFlattened.Item2.Count != otherFlattened.Item2.Count)
                return false;

            for (int i = 0; i < thisFlattened.Item1.Count; i++)
            {
                if (thisFlattened.Item1[i].Value != otherFlattened.Item1[i].Value)
                    return false;
            }

            return CheckEquals(thisFlattened.Item2, otherFlattened.Item2);
        }

        public static bool IsEqual(this ArraySegment<int> @this, ArraySegment<int> other)
        {
            if (!((@this.Array != null) == (other.Array != null) &&
                @this.Count == other.Count &&
                @this.Offset == other.Offset))
                return false;

            return @this.Array == null || @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this ObjectWithArrays @this, ObjectWithArrays other)
        {
            return @this != null &&
                other != null &&
                CheckEquals(@this.IntArray, other.IntArray) &&
                CheckEquals(@this.StringArray, other.StringArray) &&
                CheckEquals(@this.TreeArray, other.TreeArray) &&
                CheckEquals(@this.ByteArray, other.ByteArray) &&
                CheckEquals(@this.JaggedArray, other.JaggedArray) &&
                CheckEquals(@this.MultiDimensionalArray, other.MultiDimensionalArray);
        }

        public static bool IsEqual(this ObjectWithIntStringUShortUIntULongAndCustomObjectFields @this, ObjectWithIntStringUShortUIntULongAndCustomObjectFields other)
        {
            return @this != null &&
                other != null &&
                @this.Member1 == other.Member1 &&
                @this.Member2 == other.Member2 &&
                @this._member3 == other._member3 &&
                IsEqual(@this.Member4, other.Member4) &&
                IsEqual(@this.Member4shared, other.Member4shared) &&
                IsEqual(@this.Member5, other.Member5) &&
                @this.Member6 == other.Member6 &&
                @this.str1 == other.str1 &&
                @this.str2 == other.str2 &&
                @this.str3 == other.str3 &&
                @this.str4 == other.str4 &&
                @this.u16 == other.u16 &&
                @this.u32 == other.u32 &&
                @this.u64 == other.u64;
        }

        public static bool IsEqual(this Point @this, Point other)
        {
            if (@this == null && other == null)
                return true;

            return @this != null &&
                other != null &&
                @this.X == other.X &&
                @this.Y == other.Y;
        }

        public static bool IsEqual(this SqlGuid @this, SqlGuid other)
        {
            return @this.IsNull == other.IsNull && (@this.IsNull || @this.Value == other.Value);
        }

        public static bool IsEqual(this SealedObjectWithIntStringFields @this, SealedObjectWithIntStringFields other)
        {
            return @this != null &&
                other != null &&
                @this.Member1 == other.Member1 &&
                @this.Member2 == other.Member2 &&
                @this.Member3 == other.Member3;
        }

        public static bool IsEqual(this SimpleKeyedCollection @this, SimpleKeyedCollection other)
        {
            if (!(@this != null &&
                other != null &&
                @this.Comparer.Equals(other.Comparer) &&
                @this.Count == other.Count))
                return false;

            return @this.CheckSequenceEquals(other);
        }

        public static bool IsEqual(this Tree<Colors> @this, Tree<Colors> other)
        {
            if (@this == null && other == null)
                return true;

            return @this != null &&
                other != null &&
                @this.Value == other.Value &&
                IsEqual(@this.Left, other.Left) &&
                IsEqual(@this.Right, other.Right);
        }

        public static bool IsEqual(this TimeZoneInfo @this, TimeZoneInfo other)
        {
            return @this != null &&
                other != null &&
                @this.Id == other.Id &&
                @this.DisplayName == other.DisplayName &&
                @this.StandardName == other.StandardName &&
                @this.DaylightName == other.DaylightName &&
                @this.BaseUtcOffset == other.BaseUtcOffset &&
                @this.SupportsDaylightSavingTime == other.SupportsDaylightSavingTime;
        }

        public static bool IsEqual(this Tuple<int> @this, Tuple<int> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1;
        }

        public static bool IsEqual(this Tuple<int, string> @this, Tuple<int, string> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2;
        }

        public static bool IsEqual(this Tuple<int, string, uint> @this, Tuple<int, string, uint> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3;
        }

        public static bool IsEqual(this Tuple<int, string, uint, long> @this, Tuple<int, string, uint, long> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3 &&
                @this.Item4 == other.Item4;
        }

        public static bool IsEqual(this Tuple<int, string, uint, long, double> @this, Tuple<int, string, uint, long, double> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3 &&
                @this.Item4 == other.Item4 &&
                @this.Item5 == other.Item5;
        }

        public static bool IsEqual(this Tuple<int, string, uint, long, double, float> @this, Tuple<int, string, uint, long, double, float> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3 &&
                @this.Item4 == other.Item4 &&
                @this.Item5 == other.Item5 &&
                @this.Item6 == other.Item6;
        }

        public static bool IsEqual(this Tuple<int, string, uint, long, double, float, decimal> @this, Tuple<int, string, uint, long, double, float, decimal> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3 &&
                @this.Item4 == other.Item4 &&
                @this.Item5 == other.Item5 &&
                @this.Item6 == other.Item6 &&
                @this.Item7 == other.Item7;
        }

        public static bool IsEqual(this Tuple<int, string, uint, long, double, float, decimal, Tuple<Tuple<int>>> @this, Tuple<int, string, uint, long, double, float, decimal, Tuple<Tuple<int>>> other)
        {
            return @this != null &&
                other != null &&
                @this.Item1 == other.Item1 &&
                @this.Item2 == other.Item2 &&
                @this.Item3 == other.Item3 &&
                @this.Item4 == other.Item4 &&
                @this.Item5 == other.Item5 &&
                @this.Item6 == other.Item6 &&
                @this.Item7 == other.Item7 &&
                @this.Rest.Item1.Item1 == other.Rest.Item1.Item1;
        }

        public static bool IsEqual(this Uri @this, Uri other)
        {
            if (@this == null && other == null)
                return true;

            return @this != null &&
                other != null &&
                @this.AbsolutePath == other.AbsolutePath &&
                @this.AbsoluteUri == other.AbsoluteUri &&
                @this.LocalPath == other.LocalPath &&
                @this.Authority == other.Authority &&
                @this.HostNameType == other.HostNameType &&
                @this.IsDefaultPort == other.IsDefaultPort &&
                @this.IsFile == other.IsFile &&
                @this.IsLoopback == other.IsLoopback &&
                @this.PathAndQuery == other.PathAndQuery &&
                @this.Segments.SequenceEqual(other.Segments) &&
                @this.IsUnc == other.IsUnc &&
                @this.Host == other.Host &&
                @this.Port == other.Port &&
                @this.Query == other.Query &&
                @this.Fragment == other.Fragment &&
                @this.Scheme == other.Scheme &&
                @this.DnsSafeHost == other.DnsSafeHost &&
                @this.IdnHost == other.IdnHost &&
                @this.IsAbsoluteUri == other.IsAbsoluteUri &&
                @this.UserEscaped == other.UserEscaped &&
                @this.UserInfo == other.UserInfo;
        }

        public static bool IsEqual(this Version @this, Version other)
        {
            return @this != null &&
                other != null &&
                @this.Major == other.Major &&
                @this.Minor == other.Minor &&
                @this.Build == other.Build &&
                @this.Revision == other.Revision &&
                @this.MajorRevision == other.MajorRevision &&
                @this.MinorRevision == other.MinorRevision;
        }

        public static bool IsEqual(this Exception @this, Exception other)
        {
            return @this != null &&
                other != null &&
                // On full framework, line number may be method body start
                // On Net Native we can't reflect on Exceptions and change its StackTrace
                ((PlatformDetection.IsFullFramework || PlatformDetection.IsNetNative) ? true :
                (@this.StackTrace == other.StackTrace &&
                @this.ToString() == other.ToString())) &&
                @this.Data.CheckSequenceEquals(other.Data) &&
                @this.Message == other.Message &&
                @this.Source == other.Source &&
                // On Net Native we can't reflect on Exceptions and change its HResult
                (PlatformDetection.IsNetNative ? true : @this.HResult == other.HResult) &&
                @this.HelpLink == other.HelpLink &&
                CheckEquals(@this.InnerException, other.InnerException);
        }

        public static bool IsEqual(this AggregateException @this, AggregateException other)
        {
            return IsEqual(@this as Exception, other as Exception) &&
                @this.InnerExceptions.CheckSequenceEquals(other.InnerExceptions);
        }

        public class ReferenceComparer<T> : IEqualityComparer<T> where T: class
        {
            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T x)
            {
                return RuntimeHelpers.GetHashCode(x);
            }
        }
    }
}
