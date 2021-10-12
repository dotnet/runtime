// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Reflection;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Allows virtual dispatch to GenericMethodHolder{T}.
    /// </summary>
    internal abstract class GenericMethodHolder
    {
        /// <summary>
        /// Returns the default value for the specified type.
        /// </summary>
        public abstract object? DefaultValue { get; }

        /// <summary>
        /// Returns true if <param name="value"/> contains only default values.
        /// </summary>
        public abstract bool IsDefaultValue(object value);

        /// <summary>
        /// Creates a holder instance representing a type.
        /// </summary>
        public static GenericMethodHolder CreateHolder(Type type)
        {
            Type holderType = typeof(GenericMethodHolder<>).MakeGenericType(type);
            return (GenericMethodHolder)Activator.CreateInstance(holderType)!;
        }
    }

    /// <summary>
    /// Generic methods for {T}.
    /// </summary>
    internal sealed class GenericMethodHolder<T> : GenericMethodHolder
    {
        public override object? DefaultValue => default(T);

        public override bool IsDefaultValue(object value)
        {
            // For performance, we only want to call this method for non-nullable value types.
            // Nullable types should be checked againt 'null' before calling this method.
            Debug.Assert(!value.GetType().CanBeNull());

            return EqualityComparer<T>.Default.Equals(default, (T)value);
        }
    }
}
