// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization.Metadata
{
    /// <summary>
    /// Helper class used for calculating the default value for a given System.Type instance.
    /// </summary>
    internal sealed class DefaultValueHolder
    {
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
                    Justification = "GetUninitializedObject is only called on a struct. You can always create an instance of a struct.")]
        private DefaultValueHolder(Type type)
        {
            if (type.IsValueType && Nullable.GetUnderlyingType(type) == null)
            {
#if NETCOREAPP
                DefaultValue = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
#else
                DefaultValue = System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
#endif
            }
        }

        /// <summary>
        /// Returns the default value for the specified type.
        /// </summary>
        public object? DefaultValue { get; }

        /// <summary>
        /// Returns true if <param name="value"/> contains only default values.
        /// </summary>
        public bool IsDefaultValue(object value) => DefaultValue is null ? value is null : DefaultValue.Equals(value);

        /// <summary>
        /// Creates a holder instance representing a type.
        /// </summary>
        public static DefaultValueHolder CreateHolder(Type type) => new DefaultValueHolder(type);
    }
}
