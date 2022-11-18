// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // The IComparable interface is implemented by classes that support an
    // ordering of instances of the class. The ordering represented by
    // IComparable can be used to sort arrays and collections of objects
    // that implement the interface.
    //
    public interface IComparable
    {
        // Interface does not need to be marked with the serializable attribute

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Value</term>
        ///             <description>Meaning</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance precedes <paramref name="obj" /> in the sort order.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance occurs in the same position in the sort order as <paramref name="obj" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance follows <paramref name="obj" /> in the sort order.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="obj" /> is not the same type as this instance.</exception>
        int CompareTo(object? obj);
    }

    // Generic version of IComparable.

    public interface IComparable<in T>
    {
        // Interface does not need to be marked with the serializable attribute

        /// <summary>Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.</summary>
        /// <param name="other">An object to compare with this instance.</param>
        /// <returns>
        ///     <para>A value that indicates the relative order of the objects being compared. The return value has these meanings:</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Value</term>
        ///             <description>Meaning</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance precedes <paramref name="other" /> in the sort order.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance occurs in the same position in the sort order as <paramref name="other" />.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance follows <paramref name="other" /> in the sort order.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        int CompareTo(T? other);
    }
}
