// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Instructs the System.Text.Json source generator to use the specified converter when generating serialization metadata.
    /// </summary>
    /// <remarks>
    /// This attribute is used to specify a <see cref="JsonConverter"/> to be used by the source generator for a type.
    /// If specified, the converter will take precedence over any <see cref="JsonConverterAttribute"/> specified on the type itself,
    /// but is overridden by any <see cref="JsonConverterAttribute"/> specified on a property/field of the type.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
#if BUILDING_SOURCE_GENERATOR
    internal
#else
    public
#endif
    sealed class JsonExternalConverterAttribute : JsonAttribute
    {
#pragma warning disable IDE0060
        /// <summary>
        /// Initializes a new instance of <see cref="JsonExternalConverterAttribute"/>.
        /// </summary>
        /// <param name="converterType">The converter to apply during source generation.</param>
        public JsonExternalConverterAttribute(
    #if !BUILDING_SOURCE_GENERATOR
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    #endif
            Type converterType)
        { }
#pragma warning restore IDE0060
    }
}
