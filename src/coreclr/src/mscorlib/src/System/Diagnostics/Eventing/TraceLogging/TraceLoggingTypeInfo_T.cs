// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using Interlocked = System.Threading.Interlocked;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: used when implementing a custom TraceLoggingTypeInfo.
    /// Implementations of this type provide the behaviors that TraceLogging
    /// uses to turn objects into event data. TraceLogging provides default
    /// implementations of this type, but custom implementations can be used
    /// when the default TraceLogging implementation is insufficient.
    /// </summary>
    /// <typeparam name="DataType">
    /// The type of object that is handled by this implementation.
    /// </typeparam>
    internal abstract class TraceLoggingTypeInfo<DataType>
        : TraceLoggingTypeInfo
    {
        private static TraceLoggingTypeInfo<DataType> instance;

        /// <summary>
        /// Initializes a new instance of the TraceLoggingTypeInfo class with
        /// default settings. Uses typeof(DataType).Name for EventName and FieldName.
        /// Marks Level and Opcode as unset. Sets Keywords and Traits to 0.
        /// </summary>
        protected TraceLoggingTypeInfo()
            : base(typeof(DataType))
        {
            return;
        }

        /// <summary>
        /// Initializes a new instance of the TraceLoggingTypeInfo class, using
        /// the specified values for the EventName, Level, Opcode, Keywords,
        /// FieldName, and Traits properties.
        /// </summary>
        /// <param name="name">
        /// The value for the Name property. Must not contain '\0' characters.
        /// Must not be null.
        /// </param>
        /// <param name="level">
        /// The value for the Level property, or -1 to mark Level as unset.
        /// </param>
        /// <param name="opcode">
        /// The value for the Opcode property, or -1 to mark Opcode as unset.
        /// </param>
        /// <param name="keywords">
        /// The value for the Keywords property.
        /// </param>
        /// <param name="tags">
        /// The value for the Tags property.
        /// </param>
        protected TraceLoggingTypeInfo(
            string name,
            EventLevel level,
            EventOpcode opcode,
            EventKeywords keywords,
            EventTags tags)
        : base(
            typeof(DataType),
            name,
            level,
            opcode,
            keywords,
            tags)
        {
            return;
        }

        /// <summary>
        /// Gets the type info that will be used for handling instances of
        /// DataType. If the instance has not already been set, this will
        /// call TrySetInstance(automaticSerializer) to set one, where
        /// automaticSerializer is the value returned from CreateDefault(),
        /// or a do-nothing serializer if CreateDefault() fails.
        /// </summary>
        public static TraceLoggingTypeInfo<DataType> Instance
        {
            get
            {
                return instance ?? InitInstance();
            }
        }

        /// <summary>
        /// When overridden by a derived class, writes the data (fields) for an instance
        /// of DataType. Note that the sequence of operations in WriteData should be
        /// essentially identical to the sequence of operations in WriteMetadata. Otherwise,
        /// the metadata and data will not match, which may cause trouble when decoding the
        /// event.
        /// </summary>
        /// <param name="collector">
        /// The object that collects the data for the instance. Data is written by calling
        /// methods on the collector object. Note that if the type contains sub-objects,
        /// the implementation of this method may need to call the WriteData method
        /// for the sub-object, e.g. by calling
        /// TraceLoggingTypeInfo&lt;SubType&gt;.Instance.WriteData(...).
        /// </param>
        /// <param name="value">
        /// The value for which data is to be written.
        /// </param>
        public abstract void WriteData(
            TraceLoggingDataCollector collector,
            ref DataType value);

        /// <summary>
        /// When overridden in a derived class, writes the data (fields) for an instance
        /// of DataType. The default implementation of WriteObjectData calls
        /// WriteData(collector, (DataType)value). Normally, you will override WriteData
        /// and not WriteObjectData. However, if your implementation of WriteData has to
        /// cast the value to object, it may be more efficient to reverse this calling
        /// pattern, i.e. to implement WriteObjectData, and then implement WriteData as a
        /// call to WriteObjectData.
        /// </summary>
        /// <param name="collector">
        /// The object that collects the data for the instance. Data is written by calling
        /// methods on the collector object. Note that if the type contains sub-objects,
        /// the implementation of this method may need to call the WriteData method
        /// for the sub-object, e.g. by calling
        /// TraceLoggingTypeInfo&lt;SubType&gt;.Instance.WriteData(...).
        /// </param>
        /// <param name="value">
        /// The value for which data is to be written. Note that this value may be null
        /// (even for value types) if the property from which the value was read is
        /// missing or null.
        /// </param>
        public override void WriteObjectData(
            TraceLoggingDataCollector collector,
            object value)
        {
            var val = value == null ? default(DataType) : (DataType)value;
            this.WriteData(collector, ref val);
        }

        internal static TraceLoggingTypeInfo<DataType> GetInstance(List<Type> recursionCheck)
        {
            if (instance == null)
            {
                var recursionCheckCount = recursionCheck.Count;
                var newInstance = Statics.CreateDefaultTypeInfo<DataType>(recursionCheck);
                Interlocked.CompareExchange(ref instance, newInstance, null);
                recursionCheck.RemoveRange(recursionCheckCount, recursionCheck.Count - recursionCheckCount);
            }

            return instance;
        }

        private static TraceLoggingTypeInfo<DataType> InitInstance()
        {
            return GetInstance(new List<Type>());
        }
    }
}
