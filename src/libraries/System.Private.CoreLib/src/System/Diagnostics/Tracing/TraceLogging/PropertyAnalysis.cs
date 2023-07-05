// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System.Diagnostics.Tracing
{
    /// <summary>
    /// TraceLogging: stores the per-property information obtained by
    /// reflecting over a type.
    /// </summary>
    internal sealed class PropertyAnalysis
    {
        internal readonly string name;
        internal readonly PropertyInfo propertyInfo;
        internal readonly Func<PropertyValue, PropertyValue> getter;
        internal readonly TraceLoggingTypeInfo typeInfo;
        internal readonly EventFieldAttribute? fieldAttribute;

        public PropertyAnalysis(
            string name,
            PropertyInfo propertyInfo,
            TraceLoggingTypeInfo typeInfo,
            EventFieldAttribute? fieldAttribute)
        {
            this.name = name;
            this.propertyInfo = propertyInfo;
            this.getter = PropertyValue.GetPropertyGetter(propertyInfo);
            this.typeInfo = typeInfo;
            this.fieldAttribute = fieldAttribute;
        }
    }
}
