// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class EqualityExtensions
    {
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
                @this.ExtendedProperties?.Count == other.ExtendedProperties?.Count;
        }

        public static bool IsEqual(this DataTable @this, DataTable other)
        {
            return @this != null &&
                other != null &&
                @this.TableName == other.TableName &&
                @this.Namespace == other.Namespace &&
                @this.Prefix == other.Prefix &&
                @this.CaseSensitive == other.CaseSensitive &&
                @this.Locale.LCID == other.Locale.LCID &&
                @this.MinimumCapacity == other.MinimumCapacity;
        }

        public static bool IsEqual(this Comparer @this, Comparer other)
        {
            CompareInfo GetCompareInfoName(Comparer comparer) => 
                comparer.GetType().GetField("_compareInfo", Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).GetValue(comparer) as CompareInfo;

            return @this != null &&
                other != null &&
                Object.Equals(GetCompareInfoName(@this), GetCompareInfoName(other));
        }
    }
}
