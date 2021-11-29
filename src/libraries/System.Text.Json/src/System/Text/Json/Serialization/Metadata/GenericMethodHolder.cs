// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        /// Returns true if <param name="value"/> contains only default values.
        /// </summary>
        public abstract bool IsDefaultValue(object value);
    }

    /// <summary>
    /// Generic methods for {T}.
    /// </summary>
    internal sealed class GenericMethodHolder<T> : GenericMethodHolder
    {
        public override bool IsDefaultValue(object value)
        {
            // For performance, we only want to call this method for non-nullable value types.
            // Nullable types should be checked againt 'null' before calling this method.
            Debug.Assert(!value.GetType().CanBeNull());

            return EqualityComparer<T>.Default.Equals(default, (T)value);
        }
    }
}
