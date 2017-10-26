// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

#if !ES_BUILD_AGAINST_DOTNET_V35
using Contract = System.Diagnostics.Contracts.Contract;
#else
using Contract = Microsoft.Diagnostics.Contracts.Internal.Contract;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: used when implementing a custom TraceLoggingTypeInfo.
    /// Non-generic base class for TraceLoggingTypeInfo&lt;DataType>. Do not derive
    /// from this class. Instead, derive from TraceLoggingTypeInfo&lt;DataType>.
    /// </summary>
    internal abstract class TraceLoggingTypeInfo
    {
        private readonly string name;
        private readonly EventKeywords keywords;
        private readonly EventLevel level = (EventLevel)(-1);
        private readonly EventOpcode opcode = (EventOpcode)(-1);
        private readonly EventTags tags;
        private readonly Type dataType;
        private readonly Func<object, PropertyValue> propertyValueFactory;

        internal TraceLoggingTypeInfo(Type dataType)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            this.name = dataType.Name;
            this.dataType = dataType;
            this.propertyValueFactory = PropertyValue.GetFactory(dataType);
        }

        internal TraceLoggingTypeInfo(
            Type dataType,
            string name,
            EventLevel level,
            EventOpcode opcode,
            EventKeywords keywords,
            EventTags tags)
        {
            if (dataType == null)
            {
                throw new ArgumentNullException(nameof(dataType));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Statics.CheckName(name);

            this.name = name;
            this.keywords = keywords;
            this.level = level;
            this.opcode = opcode;
            this.tags = tags;
            this.dataType = dataType;
            this.propertyValueFactory = PropertyValue.GetFactory(dataType);
        }

        /// <summary>
        /// Gets the name to use for the event if this type is the top-level type,
        /// or the name to use for an implicitly-named field.
        /// Never null.
        /// </summary>
        public string Name
        {
            get { return this.name; }
        }

        /// <summary>
        /// Gets the event level associated with this type. Any value in the range 0..255
        /// is an associated event level. Any value outside the range 0..255 is invalid and
        /// indicates that this type has no associated event level.
        /// </summary>
        public EventLevel Level
        {
            get { return this.level; }
        }

        /// <summary>
        /// Gets the event opcode associated with this type. Any value in the range 0..255
        /// is an associated event opcode. Any value outside the range 0..255 is invalid and
        /// indicates that this type has no associated event opcode.
        /// </summary>
        public EventOpcode Opcode
        {
            get { return this.opcode; }
        }

        /// <summary>
        /// Gets the keyword(s) associated with this type.
        /// </summary>
        public EventKeywords Keywords
        {
            get { return this.keywords; }
        }

        /// <summary>
        /// Gets the event tags associated with this type.
        /// </summary>
        public EventTags Tags
        {
            get { return this.tags; }
        }

        internal Type DataType
        {
            get { return this.dataType; }
        }

        internal Func<object, PropertyValue> PropertyValueFactory
        {
            get { return this.propertyValueFactory; }
        }

        /// <summary>
        /// When overridden by a derived class, writes the metadata (schema) for
        /// this type. Note that the sequence of operations in WriteMetadata should be
        /// essentially identical to the sequence of operations in
        /// WriteData/WriteObjectData. Otherwise, the metadata and data will not match,
        /// which may cause trouble when decoding the event.
        /// </summary>
        /// <param name="collector">
        /// The object that collects metadata for this object's type. Metadata is written
        /// by calling methods on the collector object. Note that if the type contains
        /// sub-objects, the implementation of this method may need to call the
        /// WriteMetadata method for the type of the sub-object, e.g. by calling
        /// TraceLoggingTypeInfo&lt;SubType&gt;.Instance.WriteMetadata(...).
        /// </param>
        /// <param name="name">
        /// The name of the property that contains an object of this type, or null if this
        /// object is being written as a top-level object of an event. Typical usage
        /// is to pass this value to collector.AddGroup.
        /// </param>
        /// <param name="format">
        /// The format attribute for the field that contains an object of this type.
        /// </param>
        public abstract void WriteMetadata(
            TraceLoggingMetadataCollector collector,
            string name,
            EventFieldFormat format);

        /// <summary>
        /// Refer to TraceLoggingTypeInfo.WriteObjectData for information about this
        /// method.
        /// </summary>
        /// <param name="collector">
        /// Refer to TraceLoggingTypeInfo.WriteObjectData for information about this
        /// method.
        /// </param>
        /// <param name="value">
        /// Refer to TraceLoggingTypeInfo.WriteObjectData for information about this
        /// method.
        /// </param>
        public abstract void WriteData(
            TraceLoggingDataCollector collector,
            PropertyValue value);

        /// <summary>
        /// Fetches the event parameter data for internal serialization. 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual object GetData(object value)
        {
            return value;
        }

        [ThreadStatic] // per-thread cache to avoid synchronization
        private static Dictionary<Type, TraceLoggingTypeInfo> threadCache;

        public static TraceLoggingTypeInfo GetInstance(Type type, List<Type> recursionCheck)
        {
            var cache = threadCache ?? (threadCache = new Dictionary<Type, TraceLoggingTypeInfo>());

            TraceLoggingTypeInfo instance;
            if (!cache.TryGetValue(type, out instance))
            {
                if (recursionCheck == null)
                    recursionCheck = new List<Type>();
                var recursionCheckCount = recursionCheck.Count;
                instance = Statics.CreateDefaultTypeInfo(type, recursionCheck);
                cache[type] = instance;
                recursionCheck.RemoveRange(recursionCheckCount, recursionCheck.Count - recursionCheckCount);
            }
            return instance;
        }
    }
}
