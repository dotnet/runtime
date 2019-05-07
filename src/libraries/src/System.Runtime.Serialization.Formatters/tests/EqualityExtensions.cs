// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;
using Xunit;

// System.Text.Json is a .NET Core 3.0 specific library
#if netcoreapp
using System.Text.Json;
#endif

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class EqualityExtensions
    {
        private static MethodInfo GetExtensionMethod(Type extendedType)
        {
            if (extendedType.IsGenericType)
            {
                IEnumerable<MethodInfo> x = typeof(EqualityExtensions).GetMethods()
                    ?.Where(m =>
                        m.Name == "IsEqual" &&
                        m.GetParameters().Length == 3 &&
                        m.IsGenericMethodDefinition);

                MethodInfo method = typeof(EqualityExtensions).GetMethods()
                    ?.SingleOrDefault(m =>
                        m.Name == "IsEqual" &&
                        m.GetParameters().Length == 3 &&
                        m.GetParameters()[0].ParameterType.Name == extendedType.Name &&
                        m.IsGenericMethodDefinition);

                // If extension method found, make it generic and return
                if (method != null)
                    return method.MakeGenericMethod(extendedType.GenericTypeArguments[0]);
            }

            return typeof(EqualityExtensions).GetMethod("IsEqual", new[] { extendedType, extendedType, typeof(bool) });
        }

        public static void CheckEquals(object objA, object objB, bool isSamePlatform)
        {
            if (objA == null && objB == null)
                return;

            if (objA != null && objB != null)
            {
                object equalityResult = null;
                Type objType = objA.GetType();

                // Check if custom equality extension method is available
                MethodInfo customEqualityCheck = GetExtensionMethod(objType);
                if (customEqualityCheck != null)
                {
                    customEqualityCheck.Invoke(objA, new object[] { objA, objB, isSamePlatform });
                    return;
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
                            Assert.True((bool)equalityResult);
                            return;
                        }
                    }
                }
            }

            if (objA is IEnumerable objAEnumerable && objB is IEnumerable objBEnumerable)
            {
                CheckSequenceEquals(objAEnumerable, objBEnumerable, isSamePlatform);
                return;
            }

            Assert.True(objA.Equals(objB));
        }

        public static void CheckSequenceEquals(this IEnumerable @this, IEnumerable other, bool isSamePlatform)
        {
            if (@this == null || other == null)
            {
                Assert.Equal(@this, other);
            }

            Assert.Equal(@this.GetType(), other.GetType());
            IEnumerator eA = null;
            IEnumerator eB = null;

            try
            {
                eA = (@this as IEnumerable).GetEnumerator();
                eB = (other as IEnumerable).GetEnumerator();
                while (true)
                {
                    bool moved = eA.MoveNext();
                    if (moved != eB.MoveNext())
                        return;
                    if (!moved)
                        return;
                    if (eA.Current == null && eB.Current == null)
                        return;
                    CheckEquals(eA.Current, eB.Current, isSamePlatform);
                }
            }
            finally
            {
                (eA as IDisposable)?.Dispose();
                (eB as IDisposable)?.Dispose();
            }
        }

        public static void IsEqual(this WeakReference @this, WeakReference other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.TrackResurrection, other.TrackResurrection);

            // When WeakReference is deserialized, the object it wraps may blip into and out of
            // existence before we get a chance to compare it, since there are no strong references
            // to it such that it can then be immediately collected.  Therefore, if we can get both
            // values, great, compare them.  Otherwise, consider them equal.
            object a = @this.Target;
            object b = other.Target;

            if (a != null && b != null)
            {
                Assert.Equal(a, b);
            }
        }

        public static void IsEqual<T>(this WeakReference<T> @this, WeakReference<T> other, bool isSamePlatform)
            where T : class
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);

            // When WeakReference is deserialized, the object it wraps may blip into and out of
            // existence before we get a chance to compare it, since there are no strong references
            // to it such that it can then be immediately collected.  Therefore, if we can get both
            // values, great, compare them.  Otherwise, consider them equal.
            if (@this.TryGetTarget(out T thisTarget) && other.TryGetTarget(out T otherTarget))
            {
                Assert.Equal(thisTarget, otherTarget);
            }
        }

        public static void IsEqual<T>(this Lazy<T> @this, Lazy<T> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);

            // Force value creation for lazy original object
            T thisVal = @this.Value;
            T otherVal = other.Value;

            Assert.Equal(@this.IsValueCreated, other.IsValueCreated);
            CheckEquals(thisVal, otherVal, isSamePlatform);
        }

        public static void IsEqual(this StreamingContext @this, StreamingContext other, bool isSamePlatform)
        {
            Assert.Equal(@this.State, other.State);
            CheckEquals(@this.Context, other.Context, isSamePlatform);
        }

        public static void IsEqual(this CookieContainer @this, CookieContainer other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Capacity, other.Capacity);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.MaxCookieSize, other.MaxCookieSize);
            Assert.Equal(@this.PerDomainCapacity, other.PerDomainCapacity);
        }

        public static void IsEqual(this DataSet @this, DataSet other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.DataSetName, other.DataSetName);
            Assert.Equal(@this.Namespace, other.Namespace);
            Assert.Equal(@this.Prefix, other.Prefix);
            Assert.Equal(@this.CaseSensitive, other.CaseSensitive);
            Assert.Equal(@this.Locale.LCID, other.Locale.LCID);
            Assert.Equal(@this.EnforceConstraints, other.EnforceConstraints);
            Assert.Equal(@this.ExtendedProperties?.Count, other.ExtendedProperties?.Count);
            CheckEquals(@this.ExtendedProperties, other.ExtendedProperties, isSamePlatform);
        }

        public static void IsEqual(this DataTable @this, DataTable other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.RemotingFormat, other.RemotingFormat);
            Assert.Equal(@this.TableName, other.TableName);
            Assert.Equal(@this.Namespace, other.Namespace);
            Assert.Equal(@this.Prefix, other.Prefix);
            Assert.Equal(@this.CaseSensitive, other.CaseSensitive);
            Assert.Equal(@this.Locale.LCID, other.Locale.LCID);
            Assert.Equal(@this.MinimumCapacity, other.MinimumCapacity);
        }

        public static void IsEqual(this DateTime @this, DateTime other, bool isSamePlatform)
        {
            // DateTime's Equals ignores Kind
            Assert.Equal(@this.Kind, other.Kind);
            Assert.Equal(@this.Ticks, other.Ticks);
        }

        public static void IsEqual(this Comparer @this, Comparer other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);

            // The compareInfos are internal and get reflection blocked on .NET Native, so use
            // GetObjectData to get them
            SerializationInfo thisInfo = new SerializationInfo(typeof(Comparer), new FormatterConverter());
            @this.GetObjectData(thisInfo, new StreamingContext());
            CompareInfo thisCompareInfo = (CompareInfo)thisInfo.GetValue("CompareInfo", typeof(CompareInfo));

            SerializationInfo otherInfo = new SerializationInfo(typeof(Comparer), new FormatterConverter());
            other.GetObjectData(otherInfo, new StreamingContext());
            CompareInfo otherCompareInfo = (CompareInfo)otherInfo.GetValue("CompareInfo", typeof(CompareInfo));

            Assert.Equal(thisCompareInfo, otherCompareInfo);
        }

        public static void IsEqual(this DictionaryEntry @this, DictionaryEntry other, bool isSamePlatform)
        {
            CheckEquals(@this.Key, other.Key, isSamePlatform);
            CheckEquals(@this.Value, other.Value, isSamePlatform);
        }

        public static void IsEqual(this StringDictionary @this, StringDictionary other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
        }

        public static void IsEqual(this ArrayList @this, ArrayList other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.Capacity, other.Capacity);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);

            for (int i = 0; i < @this.Count; i++)
            {
                CheckEquals(@this[i], other[i], isSamePlatform);
            }
        }

        public static void IsEqual(this BitArray @this, BitArray other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Length, other.Length);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            CheckSequenceEquals(@this, other, isSamePlatform);
        }

        public static void IsEqual(this Dictionary<int, string> @this, Dictionary<int, string> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            CheckEquals(@this.Comparer, other.Comparer, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);

            foreach (KeyValuePair<int, string> kv in @this)
            {
                Assert.Equal(@this[kv.Key], other[kv.Key]);
            }
        }

        public static void IsEqual(this PointEqualityComparer @this, PointEqualityComparer other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
        }

        public static void IsEqual(this HashSet<Point> @this, HashSet<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            CheckEquals(@this.Comparer, other.Comparer, isSamePlatform);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this LinkedListNode<Point> @this, LinkedListNode<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            CheckEquals(@this.Value, other.Value, isSamePlatform);
        }

        public static void IsEqual(this LinkedList<Point> @this, LinkedList<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            CheckEquals(@this.First, other.First, isSamePlatform);
            CheckEquals(@this.Last, other.Last, isSamePlatform);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this List<int> @this, List<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.Capacity, other.Capacity);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Queue<int> @this, Queue<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this SortedList<int, Point> @this, SortedList<int, Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Capacity, other.Capacity);
            CheckEquals(@this.Comparer, other.Comparer, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this SortedSet<Point> @this, SortedSet<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            CheckEquals(@this.Comparer, other.Comparer, isSamePlatform);
            CheckEquals(@this.Min, other.Min, isSamePlatform);
            CheckEquals(@this.Max, other.Max, isSamePlatform);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Stack<Point> @this, Stack<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Hashtable @this, Hashtable other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);

            foreach (var key in @this.Keys)
            {
                CheckEquals(@this[key], other[key], isSamePlatform);
            }
        }

        public static void IsEqual(this Collection<int> @this, Collection<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this ObservableCollection<int> @this, ObservableCollection<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this ReadOnlyCollection<int> @this, ReadOnlyCollection<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this ReadOnlyDictionary<int, string> @this, ReadOnlyDictionary<int, string> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);

            foreach (KeyValuePair<int, string> kv in @this)
            {
                Assert.Equal(kv.Value, other[kv.Key]);
            }
        }

        public static void IsEqual(this ReadOnlyObservableCollection<int> @this, ReadOnlyObservableCollection<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Queue @this, Queue other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this SortedList @this, SortedList other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Capacity, other.Capacity);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this HybridDictionary @this, HybridDictionary other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);

            foreach (var key in @this.Keys)
            {
                CheckEquals(@this[key], other[key], isSamePlatform);
            }
        }

        public static void IsEqual(this ListDictionary @this, ListDictionary other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);

            foreach (var key in @this.Keys)
            {
                CheckEquals(@this[key], other[key], isSamePlatform);
            }
        }

        public static void IsEqual(this NameValueCollection @this, NameValueCollection other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            @this.AllKeys.CheckSequenceEquals(other.AllKeys, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);

            foreach (var key in @this.AllKeys)
            {
                CheckEquals(@this[key], other[key], isSamePlatform);
            }
        }

        public static void IsEqual(this OrderedDictionary @this, OrderedDictionary other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            CheckEquals(@this.Keys, other.Keys, isSamePlatform);
            CheckEquals(@this.Values, other.Values, isSamePlatform);

            foreach (var key in @this.Keys)
            {
                CheckEquals(@this[key], other[key], isSamePlatform);
            }
        }

        public static void IsEqual(this StringCollection @this, StringCollection other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Stack @this, Stack other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this BindingList<int> @this, BindingList<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.RaiseListChangedEvents, other.RaiseListChangedEvents);
            Assert.Equal(@this.AllowNew, other.AllowNew);
            Assert.Equal(@this.AllowEdit, other.AllowEdit);
            Assert.Equal(@this.AllowRemove, other.AllowRemove);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this BindingList<Point> @this, BindingList<Point> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.RaiseListChangedEvents, other.RaiseListChangedEvents);
            Assert.Equal(@this.AllowNew, other.AllowNew);
            Assert.Equal(@this.AllowEdit, other.AllowEdit);
            Assert.Equal(@this.AllowRemove, other.AllowRemove);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this PropertyCollection @this, PropertyCollection other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.IsReadOnly, other.IsReadOnly);
            Assert.Equal(@this.IsFixedSize, other.IsFixedSize);
            Assert.Equal(@this.IsSynchronized, other.IsSynchronized);
            @this.Keys.CheckSequenceEquals(other.Keys, isSamePlatform);
            @this.Values.CheckSequenceEquals(other.Values, isSamePlatform);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this CompareInfo @this, CompareInfo other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Name, other.Name);
            Assert.Equal(@this.LCID, other.LCID);
            // we do not want to compare Version because it can change when changing OS
            // we do want to make sure that they are either both null or both not null
            Assert.True((@this.Version != null) == (other.Version != null));
        }

        public static void IsEqual(this SortVersion @this, SortVersion other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.FullVersion, other.FullVersion);
            Assert.Equal(@this.SortId, other.SortId);
        }

        public static void IsEqual(this Cookie @this, Cookie other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Comment, other.Comment);
            IsEqual(@this.CommentUri, other.CommentUri, isSamePlatform);
            Assert.Equal(@this.HttpOnly, other.HttpOnly);
            Assert.Equal(@this.Discard, other.Discard);
            Assert.Equal(@this.Domain, other.Domain);
            Assert.Equal(@this.Expired, other.Expired);
            CheckEquals(@this.Expires, other.Expires, isSamePlatform);
            Assert.Equal(@this.Name, other.Name);
            Assert.Equal(@this.Path, other.Path);
            Assert.Equal(@this.Port, other.Port);
            Assert.Equal(@this.Secure, other.Secure);
            // This needs to have m_Timestamp set by reflection in order to roundtrip correctly
            // otherwise this field will change each time you create an object and cause this to fail
            CheckEquals(@this.TimeStamp, other.TimeStamp, isSamePlatform);
            Assert.Equal(@this.Value, other.Value);
            Assert.Equal(@this.Version, other.Version);
        }

        public static void IsEqual(this CookieCollection @this, CookieCollection other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this BasicISerializableObject @this, BasicISerializableObject other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
        }

        public static void IsEqual(this DerivedISerializableWithNonPublicDeserializationCtor @this, DerivedISerializableWithNonPublicDeserializationCtor other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
        }

        private static void GetIdsForGraphDFS(Graph<int> n, Dictionary<Graph<int>, int> ids)
        {
            if (!ids.ContainsKey(n))
            {
                ids[n] = ids.Count;
                foreach (Graph<int> link in n.Links)
                {
                    GetIdsForGraphDFS(link, ids);
                }
            }
        }

        private static Dictionary<int, Graph<int>> InvertDictionary(Dictionary<Graph<int>, int> dict)
        {
            var ret = new Dictionary<int, Graph<int>>();
            foreach (KeyValuePair<Graph<int>, int> kv in dict)
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

            foreach (KeyValuePair<Graph<int>, int> kv in nodes)
            {
                List<int> links = edges[kv.Value];
                foreach (Graph<int> link in kv.Key.Links)
                {
                    links.Add(nodes[link]);
                }
            }

            return new Tuple<Dictionary<int, Graph<int>>, List<List<int>>>(InvertDictionary(nodes), edges);
        }

        public static void IsEqual(this Graph<int> @this, Graph<int> other, bool isSamePlatform)
        {
            Tuple<Dictionary<int, Graph<int>>, List<List<int>>> thisFlattened = FlattenGraph(@this);
            Tuple<Dictionary<int, Graph<int>>, List<List<int>>> otherFlattened = FlattenGraph(other);

            Assert.Equal(thisFlattened.Item1.Count, otherFlattened.Item1.Count);
            Assert.Equal(thisFlattened.Item2.Count, otherFlattened.Item2.Count);
            Assert.Equal(thisFlattened.Item1.Values, otherFlattened.Item1.Values);
            CheckEquals(thisFlattened.Item2, otherFlattened.Item2, isSamePlatform);
        }

        public static void IsEqual(this ArraySegment<int> @this, ArraySegment<int> other, bool isSamePlatform)
        {
            Assert.True((@this.Array != null) == (other.Array != null));
            Assert.Equal(@this.Count, other.Count);
            Assert.Equal(@this.Offset, other.Offset);
            if (@this.Array != null)
            {
                @this.CheckSequenceEquals(other, isSamePlatform);
            }
        }

        public static void IsEqual(this ObjectWithArrays @this, ObjectWithArrays other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            CheckEquals(@this.IntArray, other.IntArray, isSamePlatform);
            CheckEquals(@this.StringArray, other.StringArray, isSamePlatform);
            //CheckEquals(@this.TreeArray, other.TreeArray);
            CheckEquals(@this.ByteArray, other.ByteArray, isSamePlatform);
            CheckEquals(@this.JaggedArray, other.JaggedArray, isSamePlatform);
            CheckEquals(@this.MultiDimensionalArray, other.MultiDimensionalArray, isSamePlatform);
        }

        public static void IsEqual(this ObjectWithIntStringUShortUIntULongAndCustomObjectFields @this, ObjectWithIntStringUShortUIntULongAndCustomObjectFields other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Member1, other.Member1);
            Assert.Equal(@this.Member2, other.Member2);
            Assert.Equal(@this._member3, other._member3);
            IsEqual(@this.Member4, other.Member4, isSamePlatform);
            IsEqual(@this.Member4shared, other.Member4shared, isSamePlatform);
            IsEqual(@this.Member5, other.Member5, isSamePlatform);
            Assert.Equal(@this.Member6, other.Member6);
            Assert.Equal(@this.str1, other.str1);
            Assert.Equal(@this.str2, other.str2);
            Assert.Equal(@this.str3, other.str3);
            Assert.Equal(@this.str4, other.str4);
            Assert.Equal(@this.u16, other.u16);
            Assert.Equal(@this.u32, other.u32);
            Assert.Equal(@this.u64, other.u64);
        }

        public static void IsEqual(this Point @this, Point other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.X, other.X);
            Assert.Equal(@this.Y, other.Y);
        }

        public static void IsEqual(this SqlGuid @this, SqlGuid other, bool isSamePlatform)
        {
            Assert.Equal(@this.IsNull, other.IsNull);
            Assert.True(@this.IsNull || @this.Value == other.Value);
        }

        public static void IsEqual(this SealedObjectWithIntStringFields @this, SealedObjectWithIntStringFields other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Member1, other.Member1);
            Assert.Equal(@this.Member2, other.Member2);
            Assert.Equal(@this.Member3, other.Member3);
        }

        public static void IsEqual(this SimpleKeyedCollection @this, SimpleKeyedCollection other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Comparer, other.Comparer);
            Assert.Equal(@this.Count, other.Count);
            @this.CheckSequenceEquals(other, isSamePlatform);
        }

        public static void IsEqual(this Tree<Colors> @this, Tree<Colors> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Value, other.Value);
            IsEqual(@this.Left, other.Left, isSamePlatform);
            IsEqual(@this.Right, other.Right, isSamePlatform);
        }

        public static void IsEqual(this TimeZoneInfo @this, TimeZoneInfo other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Id, other.Id);
            Assert.Equal(@this.DisplayName, other.DisplayName);
            Assert.Equal(@this.StandardName, other.StandardName);
            Assert.Equal(@this.DaylightName, other.DaylightName);
            Assert.Equal(@this.BaseUtcOffset, other.BaseUtcOffset);
            Assert.Equal(@this.SupportsDaylightSavingTime, other.SupportsDaylightSavingTime);
        }

        public static void IsEqual(this Tuple<int> @this, Tuple<int> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
        }

        public static void IsEqual(this Tuple<int, string> @this, Tuple<int, string> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
        }

        public static void IsEqual(this Tuple<int, string, uint> @this, Tuple<int, string, uint> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
        }

        public static void IsEqual(this Tuple<int, string, uint, long> @this, Tuple<int, string, uint, long> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
            Assert.Equal(@this.Item4, other.Item4);
        }

        public static void IsEqual(this Tuple<int, string, uint, long, double> @this, Tuple<int, string, uint, long, double> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
            Assert.Equal(@this.Item4, other.Item4);
            Assert.Equal(@this.Item5, other.Item5);
        }

        public static void IsEqual(this Tuple<int, string, uint, long, double, float> @this, Tuple<int, string, uint, long, double, float> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
            Assert.Equal(@this.Item4, other.Item4);
            Assert.Equal(@this.Item5, other.Item5);
            Assert.Equal(@this.Item6, other.Item6);
        }

        public static void IsEqual(this Tuple<int, string, uint, long, double, float, decimal> @this, Tuple<int, string, uint, long, double, float, decimal> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
            Assert.Equal(@this.Item4, other.Item4);
            Assert.Equal(@this.Item5, other.Item5);
            Assert.Equal(@this.Item6, other.Item6);
            Assert.Equal(@this.Item7, other.Item7);
        }

        public static void IsEqual(this Tuple<int, string, uint, long, double, float, decimal, Tuple<Tuple<int>>> @this, Tuple<int, string, uint, long, double, float, decimal, Tuple<Tuple<int>>> other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Item1, other.Item1);
            Assert.Equal(@this.Item2, other.Item2);
            Assert.Equal(@this.Item3, other.Item3);
            Assert.Equal(@this.Item4, other.Item4);
            Assert.Equal(@this.Item5, other.Item5);
            Assert.Equal(@this.Item6, other.Item6);
            Assert.Equal(@this.Item7, other.Item7);
            Assert.Equal(@this.Rest.Item1.Item1, other.Rest.Item1.Item1);
        }

        public static void IsEqual(this Uri @this, Uri other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.AbsolutePath, other.AbsolutePath);
            Assert.Equal(@this.AbsoluteUri, other.AbsoluteUri);
            Assert.Equal(@this.LocalPath, other.LocalPath);
            Assert.Equal(@this.Authority, other.Authority);
            Assert.Equal(@this.HostNameType, other.HostNameType);
            Assert.Equal(@this.IsDefaultPort, other.IsDefaultPort);
            Assert.Equal(@this.IsFile, other.IsFile);
            Assert.Equal(@this.IsLoopback, other.IsLoopback);
            Assert.Equal(@this.PathAndQuery, other.PathAndQuery);
            Assert.True(@this.Segments.SequenceEqual(other.Segments));
            Assert.Equal(@this.IsUnc, other.IsUnc);
            Assert.Equal(@this.Host, other.Host);
            Assert.Equal(@this.Port, other.Port);
            Assert.Equal(@this.Query, other.Query);
            Assert.Equal(@this.Fragment, other.Fragment);
            Assert.Equal(@this.Scheme, other.Scheme);
            Assert.Equal(@this.DnsSafeHost, other.DnsSafeHost);
            Assert.Equal(@this.IdnHost, other.IdnHost);
            Assert.Equal(@this.IsAbsoluteUri, other.IsAbsoluteUri);
            Assert.Equal(@this.UserEscaped, other.UserEscaped);
            Assert.Equal(@this.UserInfo, other.UserInfo);
        }

        public static void IsEqual(this Version @this, Version other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Major, other.Major);
            Assert.Equal(@this.Minor, other.Minor);
            Assert.Equal(@this.Build, other.Build);
            Assert.Equal(@this.Revision, other.Revision);
            Assert.Equal(@this.MajorRevision, other.MajorRevision);
            Assert.Equal(@this.MinorRevision, other.MinorRevision);
        }

        public static void IsEqual(this Exception @this, Exception other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            @this.Data.CheckSequenceEquals(other.Data, isSamePlatform);

            // Different by design for those exceptions
            if (!((@this is SecurityException ||
                @this is XmlSyntaxException ||
                @this is ThreadAbortException ||
                @this is SqlException) && !isSamePlatform))
            {
                if (!(@this is ActiveDirectoryServerDownException ||
                    @this is SocketException ||
                    @this is NetworkInformationException))
                {
                    Assert.Equal(@this.Message, other.Message);
                }
            }

            // Different by design for those exceptions
            if (!(@this is SqlException))
            {
                Assert.Equal(@this.Source, other.Source);
            }

            Assert.Equal(@this.HelpLink, other.HelpLink);

            // Different by design for those exceptions
            if (!(@this is XmlSyntaxException && !isSamePlatform))
            {
                CheckEquals(@this.InnerException, other.InnerException, isSamePlatform);
            }

            if (!PlatformDetection.IsFullFramework && !PlatformDetection.IsNetNative)
            {
                // Different by design for those exceptions
                if (!((@this is NetworkInformationException || @this is SocketException) && !isSamePlatform))
                {
                    Assert.Equal(@this.StackTrace, other.StackTrace);
                }


                // Different by design for those exceptions
                if (!((@this is SecurityException ||
                    @this is XmlSyntaxException ||
                    @this is ThreadAbortException) && !isSamePlatform))
                {
                    if (!(@this is ActiveDirectoryServerDownException ||
                        @this is SqlException ||
                        @this is NetworkInformationException ||
                        @this is SocketException))
                    {
                        Assert.Equal(@this.ToString(), other.ToString());
                    }
                }
            }

            if (!PlatformDetection.IsNetNative)
            {
                // Different by design for those exceptions
                if (!((@this is NetworkInformationException || @this is SocketException) && !isSamePlatform))
                {
                    Assert.Equal(@this.HResult, other.HResult);
                }
            }
        }

        public static void IsEqual(this AggregateException @this, AggregateException other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            IsEqual(@this as Exception, other as Exception, isSamePlatform);
            @this.InnerExceptions.CheckSequenceEquals(other.InnerExceptions, isSamePlatform);
        }

#if netcoreapp
        public static void IsEqual(this JsonException @this, JsonException other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            IsEqual(@this as Exception, other as Exception, isSamePlatform);
            Assert.Equal(@this.Path, other.Path);
            Assert.Equal(@this.LineNumber, other.LineNumber);
            Assert.Equal(@this.BytePositionInLine, other.BytePositionInLine);
        }
#endif

        public static void IsEqual(this EventArgs @this, EventArgs other, bool isSamePlatform)
        {
            Assert.NotNull(@this);
            Assert.NotNull(other);
        }

        public static void IsEqual(this Bitmap @this, Bitmap other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Width, other.Width);
            Assert.Equal(@this.Height, other.Height);
            Assert.Equal(@this.Flags, other.Flags);
            Assert.Equal(@this.HorizontalResolution, other.HorizontalResolution);
            Assert.Equal(@this.PhysicalDimension, other.PhysicalDimension);
            Assert.Equal(@this.PixelFormat, other.PixelFormat);
            Assert.Equal(@this.RawFormat, other.RawFormat);
            Assert.Equal(@this.VerticalResolution, other.VerticalResolution);
        }

        public static void IsEqual(this Metafile @this, Metafile other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Width, other.Width);
            Assert.Equal(@this.Height, other.Height);
        }

        public static void IsEqual(this Icon @this, Icon other, bool isSamePlatform)
        {
            if (@this == null && other == null)
                return;

            Assert.NotNull(@this);
            Assert.NotNull(other);
            Assert.Equal(@this.Width, other.Width);
            Assert.Equal(@this.Height, other.Height);
        }

        public class ReferenceComparer<T> : IEqualityComparer<T> where T : class
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
