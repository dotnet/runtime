// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
    /// the type. Implemented using Delegate.CreateDelegate(property.Getter).
    /// </summary>
    /// <typeparam name="ContainerType">
    /// Type from which to read values.
    /// </typeparam>
    internal sealed class InvokeTypeInfo<ContainerType>
        : TraceLoggingTypeInfo<ContainerType>
    {
        private readonly PropertyAnalysis[] properties;
        private readonly PropertyAccessor<ContainerType>[] accessors;

        public InvokeTypeInfo(
            TypeAnalysis typeAnalysis)
            : base(
                typeAnalysis.name,
                typeAnalysis.level,
                typeAnalysis.opcode,
                typeAnalysis.keywords,
                typeAnalysis.tags)
        {
            if (typeAnalysis.properties.Length != 0)
            {
                this.properties = typeAnalysis.properties;
                this.accessors = new PropertyAccessor<ContainerType>[this.properties.Length];
                for (int i = 0; i < this.accessors.Length; i++)
                {
                    this.accessors[i] = PropertyAccessor<ContainerType>.Create(this.properties[i]);
                }
            }
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

        public override void WriteData(
            TraceLoggingDataCollector collector,
            ref ContainerType value)
        {
            if (this.accessors != null)
            {
                foreach (var accessor in this.accessors)
                {
                    accessor.Write(collector, ref value);
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
                    var propertyValue = accessors[i].GetData((ContainerType)value);
                    membersNames.Add(properties[i].name);
                    memebersValues.Add(properties[i].typeInfo.GetData(propertyValue));
                }
                return new EventPayload(membersNames, memebersValues);
            }

            return null;
        }

        public override void WriteObjectData(
            TraceLoggingDataCollector collector,
            object valueObj)
        {
            if (this.accessors != null)
            {
                var value = valueObj == null
                    ? default(ContainerType)
                    : (ContainerType)valueObj;
                this.WriteData(collector, ref value);
            }
        }
    }
}
