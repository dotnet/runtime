// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: used when implementing a custom TraceLoggingTypeInfo.
    /// An instance of this type is provided to the TypeInfo.WriteMetadata method.
    /// </summary>
    internal sealed class TraceLoggingMetadataCollector
    {
        private readonly Impl impl;
        private readonly FieldMetadata? currentGroup;
        private int bufferedArrayFieldCount = int.MinValue;

        /// <summary>
        /// Creates a root-level collector.
        /// </summary>
        internal TraceLoggingMetadataCollector()
        {
            this.impl = new Impl();
        }

        /// <summary>
        /// Creates a collector for a group.
        /// </summary>
        /// <param name="other">Parent collector</param>
        /// <param name="group">The field that starts the group</param>
        private TraceLoggingMetadataCollector(
            TraceLoggingMetadataCollector other,
            FieldMetadata group)
        {
            this.impl = other.impl;
            this.currentGroup = group;
        }

        /// <summary>
        /// The field tags to be used for the next field.
        /// This will be reset to None each time a field is written.
        /// </summary>
        internal EventFieldTags Tags
        {
            get;
            set;
        }

        internal int ScratchSize => this.impl.scratchSize;

        internal int DataCount => this.impl.dataCount;

        internal int PinCount => this.impl.pinCount;

        private bool BeginningBufferedArray => this.bufferedArrayFieldCount == 0;

        /// <summary>
        /// Call this method to add a group to the event and to return
        /// a new metadata collector that can be used to add fields to the
        /// group. After all of the fields in the group have been written,
        /// switch back to the original metadata collector to add fields
        /// outside of the group.
        /// Special-case: if name is null, no group is created, and AddGroup
        /// returns the original metadata collector. This is useful when
        /// adding the top-level group for an event.
        /// Note: do not use the original metadata collector while the group's
        /// metadata collector is in use, and do not use the group's metadata
        /// collector after switching back to the original.
        /// </summary>
        /// <param name="name">
        /// The name of the group. If name is null, the call to AddGroup is a
        /// no-op (collector.AddGroup(null) returns collector).
        /// </param>
        /// <returns>
        /// A new metadata collector that can be used to add fields to the group.
        /// </returns>
        public TraceLoggingMetadataCollector AddGroup(string? name)
        {
            TraceLoggingMetadataCollector result = this;

            if (name != null || // Normal.
                this.BeginningBufferedArray) // Error, FieldMetadata's constructor will throw the appropriate exception.
            {
                var newGroup = new FieldMetadata(
                    name!,
                    TraceLoggingDataType.Struct,
                    this.Tags,
                    this.BeginningBufferedArray);
                this.AddField(newGroup);
                result = new TraceLoggingMetadataCollector(this, newGroup);
            }

            return result;
        }

        /// <summary>
        /// Adds a scalar field to an event.
        /// </summary>
        /// <param name="name">
        /// The name to use for the added field. This value must not be null.
        /// </param>
        /// <param name="type">
        /// The type code for the added field. This must be a fixed-size type
        /// (e.g. string types are not supported).
        /// </param>
        public void AddScalar(string name, TraceLoggingDataType type)
        {
            int size;
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Int8:
                case TraceLoggingDataType.UInt8:
                case TraceLoggingDataType.Char8:
                    size = 1;
                    break;
                case TraceLoggingDataType.Int16:
                case TraceLoggingDataType.UInt16:
                case TraceLoggingDataType.Char16:
                    size = 2;
                    break;
                case TraceLoggingDataType.Int32:
                case TraceLoggingDataType.UInt32:
                case TraceLoggingDataType.HexInt32:
                case TraceLoggingDataType.Float:
                case TraceLoggingDataType.Boolean32:
                    size = 4;
                    break;
                case TraceLoggingDataType.Int64:
                case TraceLoggingDataType.UInt64:
                case TraceLoggingDataType.HexInt64:
                case TraceLoggingDataType.Double:
                case TraceLoggingDataType.FileTime:
                    size = 8;
                    break;
                case TraceLoggingDataType.Guid:
                case TraceLoggingDataType.SystemTime:
                    size = 16;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            this.impl.AddScalar(size);
            this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
        }

        /// <summary>
        /// Adds a binary-format field to an event.
        /// Compatible with core types: Binary, CountedUtf16String, CountedMbcsString.
        /// Compatible with dataCollector methods: AddBinary(string), AddArray(Any8bitType[]).
        /// </summary>
        /// <param name="name">
        /// The name to use for the added field. This value must not be null.
        /// </param>
        /// <param name="type">
        /// The type code for the added field. This must be a Binary or CountedString type.
        /// </param>
        public void AddBinary(string name, TraceLoggingDataType type)
        {
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Binary:
                case TraceLoggingDataType.CountedMbcsString:
                case TraceLoggingDataType.CountedUtf16String:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
        }

        /// <summary>
        /// Adds a null-terminated string field to an event.
        /// Compatible with core types: Utf16String, MbcsString.
        /// Compatible with dataCollector method: AddNullTerminatedString(string).
        /// </summary>
        /// <param name="name">
        /// The name to use for the added field. This value must not be null.
        /// </param>
        /// <param name="type">
        /// The type code for the added field. This must be a null-terminated string type.
        /// </param>
        public void AddNullTerminatedString(string name, TraceLoggingDataType type)
        {
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Utf16String:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, this.BeginningBufferedArray));
        }

        /// <summary>
        /// Adds an array field to an event.
        /// </summary>
        /// <param name="name">
        /// The name to use for the added field. This value must not be null.
        /// </param>
        /// <param name="type">
        /// The type code for the added field. This must be a fixed-size type.
        /// </param>
        public void AddArray(string name, TraceLoggingDataType type)
        {
            switch ((TraceLoggingDataType)((int)type & Statics.InTypeMask))
            {
                case TraceLoggingDataType.Int8:
                case TraceLoggingDataType.UInt8:
                case TraceLoggingDataType.Int16:
                case TraceLoggingDataType.UInt16:
                case TraceLoggingDataType.Int32:
                case TraceLoggingDataType.UInt32:
                case TraceLoggingDataType.Int64:
                case TraceLoggingDataType.UInt64:
                case TraceLoggingDataType.Float:
                case TraceLoggingDataType.Double:
                case TraceLoggingDataType.Boolean32:
                case TraceLoggingDataType.Guid:
                case TraceLoggingDataType.FileTime:
                case TraceLoggingDataType.HexInt32:
                case TraceLoggingDataType.HexInt64:
                case TraceLoggingDataType.Char16:
                case TraceLoggingDataType.Char8:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            if (this.BeginningBufferedArray)
            {
                throw new NotSupportedException(SR.EventSource_NotSupportedNestedArraysEnums);
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(name, type, this.Tags, true));
        }

        public void BeginBufferedArray()
        {
            if (this.bufferedArrayFieldCount >= 0)
            {
                throw new NotSupportedException(SR.EventSource_NotSupportedNestedArraysEnums);
            }

            this.bufferedArrayFieldCount = 0;
            this.impl.BeginBuffered();
        }

        public void EndBufferedArray()
        {
            if (this.bufferedArrayFieldCount != 1)
            {
                throw new InvalidOperationException(SR.EventSource_IncorrentlyAuthoredTypeInfo);
            }

            this.bufferedArrayFieldCount = int.MinValue;
            this.impl.EndBuffered();
        }

        /// <summary>
        /// Adds a custom-serialized field to an event.
        /// </summary>
        /// <param name="name">
        /// The name to use for the added field. This value must not be null.
        /// </param>
        /// <param name="type">The encoding type for the field.</param>
        /// <param name="metadata">Additional information needed to decode the field, if any.</param>
        public void AddCustom(string name, TraceLoggingDataType type, byte[] metadata)
        {
            if (this.BeginningBufferedArray)
            {
                throw new NotSupportedException(SR.EventSource_NotSupportedCustomSerializedData);
            }

            this.impl.AddScalar(2);
            this.impl.AddNonscalar();
            this.AddField(new FieldMetadata(
                name,
                type,
                this.Tags,
                metadata));
        }

        internal byte[] GetMetadata()
        {
            int size = this.impl.Encode(null);
            var metadata = new byte[size];
            this.impl.Encode(metadata);
            return metadata;
        }

        private void AddField(FieldMetadata fieldMetadata)
        {
            this.Tags = EventFieldTags.None;
            this.bufferedArrayFieldCount++;
            this.impl.fields.Add(fieldMetadata);

            if (this.currentGroup != null)
            {
                this.currentGroup.IncrementStructFieldCount();
            }
        }

        private sealed class Impl
        {
            internal readonly List<FieldMetadata> fields = new List<FieldMetadata>();
            internal short scratchSize;
            internal sbyte dataCount;
            internal sbyte pinCount;
            private int bufferNesting;
            private bool scalar;

            public void AddScalar(int size)
            {
                if (this.bufferNesting == 0)
                {
                    if (!this.scalar)
                    {
                        this.dataCount = checked((sbyte)(this.dataCount + 1));
                    }

                    this.scalar = true;
                    this.scratchSize = checked((short)(this.scratchSize + size));
                }
            }

            public void AddNonscalar()
            {
                if (this.bufferNesting == 0)
                {
                    this.scalar = false;
                    this.pinCount = checked((sbyte)(this.pinCount + 1));
                    this.dataCount = checked((sbyte)(this.dataCount + 1));
                }
            }

            public void BeginBuffered()
            {
                if (this.bufferNesting == 0)
                {
                    this.AddNonscalar();
                }

                this.bufferNesting++;
            }

            public void EndBuffered()
            {
                this.bufferNesting--;
            }

            public int Encode(byte[]? metadata)
            {
                int size = 0;

                foreach (FieldMetadata field in this.fields)
                {
                    field.Encode(ref size, metadata);
                }

                return size;
            }
        }
    }
}
