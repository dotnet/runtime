// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: An implementation of TraceLoggingTypeInfo that works
    /// for arbitrary types. It writes all public instance properties of
    /// the type.
    /// </summary>
    /// <typeparam name="ContainerType">
    /// Type from which to read values.
    /// </typeparam>
    internal sealed class InvokeTypeInfo : TraceLoggingTypeInfo
    {
        private readonly PropertyAnalysis[] properties;

        public InvokeTypeInfo(
            Type type,
            TypeAnalysis typeAnalysis)
            : base(
                type,
                typeAnalysis.name,
                typeAnalysis.level,
                typeAnalysis.opcode,
                typeAnalysis.keywords,
                typeAnalysis.tags)
        {
            if (typeAnalysis.properties.Length != 0)
                this.properties = typeAnalysis.properties;
        }

        public override void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format)
        {
            var groupCollector = collector.AddGroup(name);
            if (this.properties != null)
            {
                foreach (var property in this.properties)
                {
                    var propertyFormat = EventFieldFormat.Default;
                    var propertyAttribute = property.fieldAttribute;
                    if (propertyAttribute != null)
                    {
                        groupCollector.Tags = propertyAttribute.Tags;
                        propertyFormat = propertyAttribute.Format;
                    }

                    property.typeInfo.WriteMetadata(
                        groupCollector,
                        property.name,
                        propertyFormat);
                }
            }
        }

        public override void WriteData(TraceLoggingDataCollector collector, PropertyValue value)
        {
            if (this.properties != null)
            {
                foreach (var property in this.properties)
                {
                    property.typeInfo.WriteData(collector, property.getter(value));
                }
            }
        }

        public override object GetData(object value)
        {
            if (this.properties != null)
            {
                var membersNames = new List<string>();
                var memebersValues = new List<object>();
                for (int i = 0; i < this.properties.Length; i++)
                {
                    var propertyValue = properties[i].propertyInfo.GetValue(value);
                    membersNames.Add(properties[i].name);
                    memebersValues.Add(properties[i].typeInfo.GetData(propertyValue));
                }
                return new EventPayload(membersNames, memebersValues);
            }

            return null;
        }
    }
}
