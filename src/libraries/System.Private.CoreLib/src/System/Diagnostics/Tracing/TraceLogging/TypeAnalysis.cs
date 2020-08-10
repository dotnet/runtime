// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if ES_BUILD_STANDALONE
using System;
#endif
using System.Collections.Generic;
using System.Reflection;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: stores the per-type information obtained by reflecting over a type.
    /// </summary>
    internal sealed class TypeAnalysis
    {
        internal readonly PropertyAnalysis[] properties;
        internal readonly string? name;
        internal readonly EventKeywords keywords;
        internal readonly EventLevel level = (EventLevel)(-1);
        internal readonly EventOpcode opcode = (EventOpcode)(-1);
        internal readonly EventTags tags;

        public TypeAnalysis(
            Type dataType,
            EventDataAttribute? eventAttrib,
            List<Type> recursionCheck)
        {
            var propertyList = new List<PropertyAnalysis>();

            foreach (PropertyInfo propertyInfo in dataType.GetProperties())
            {
                if (Statics.HasCustomAttribute(propertyInfo, typeof(EventIgnoreAttribute)))
                {
                    continue;
                }

                if (!propertyInfo.CanRead ||
                    propertyInfo.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                MethodInfo? getterInfo = propertyInfo.GetGetMethod();
                if (getterInfo == null)
                {
                    continue;
                }

                if (getterInfo.IsStatic || !getterInfo.IsPublic)
                {
                    continue;
                }

                Type propertyType = propertyInfo.PropertyType;
                var propertyTypeInfo = TraceLoggingTypeInfo.GetInstance(propertyType, recursionCheck);
                EventFieldAttribute? fieldAttribute = Statics.GetCustomAttribute<EventFieldAttribute>(propertyInfo);

                string propertyName =
                    fieldAttribute != null && fieldAttribute.Name != null
                    ? fieldAttribute.Name
                    : Statics.ShouldOverrideFieldName(propertyInfo.Name)
                    ? propertyTypeInfo.Name
                    : propertyInfo.Name;
                propertyList.Add(new PropertyAnalysis(
                    propertyName,
                    propertyInfo,
                    propertyTypeInfo,
                    fieldAttribute));
            }

            this.properties = propertyList.ToArray();

            foreach (PropertyAnalysis property in this.properties)
            {
                TraceLoggingTypeInfo typeInfo = property.typeInfo;
                this.level = (EventLevel)Statics.Combine((int)typeInfo.Level, (int)this.level);
                this.opcode = (EventOpcode)Statics.Combine((int)typeInfo.Opcode, (int)this.opcode);
                this.keywords |= typeInfo.Keywords;
                this.tags |= typeInfo.Tags;
            }

            if (eventAttrib != null)
            {
                this.level = (EventLevel)Statics.Combine((int)eventAttrib.Level, (int)this.level);
                this.opcode = (EventOpcode)Statics.Combine((int)eventAttrib.Opcode, (int)this.opcode);
                this.keywords |= eventAttrib.Keywords;
                this.tags |= eventAttrib.Tags;
                this.name = eventAttrib.Name;
            }

            if (this.name == null)
            {
                this.name = dataType.Name;
            }
        }
    }
}
