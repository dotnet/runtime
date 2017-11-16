// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Resources;
using Encoding = System.Text.Encoding;

#if ES_BUILD_STANDALONE
using Environment = Microsoft.Diagnostics.Tracing.Internal.Environment;
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: Contains the information needed to generate tracelogging
    /// metadata for an event field.
    /// </summary>
    internal class FieldMetadata
    {
        /// <summary>
        /// Name of the field
        /// </summary>
        private readonly string name;

        /// <summary>
        /// The number of bytes in the UTF8 Encoding of 'name' INCLUDING a null terminator.  
        /// </summary>
        private readonly int nameSize;
        private readonly EventFieldTags tags;
        private readonly byte[] custom;

        /// <summary>
        /// ETW supports fixed sized arrays. If inType has the InTypeFixedCountFlag then this is the
        /// statically known count for the array. It is also used to encode the number of bytes of
        /// custom meta-data if InTypeCustomCountFlag set.
        /// </summary>
        private readonly ushort fixedCount;

        private byte inType;
        private byte outType;

        /// <summary>
        /// Scalar or variable-length array.
        /// </summary>
        public FieldMetadata(
            string name,
            TraceLoggingDataType type,
            EventFieldTags tags,
            bool variableCount)
            : this(
                name,
                type,
                tags,
                variableCount ? Statics.InTypeVariableCountFlag : (byte)0,
                0,
                null)
        {
            return;
        }

        /// <summary>
        /// Fixed-length array.
        /// </summary>
        public FieldMetadata(
            string name,
            TraceLoggingDataType type,
            EventFieldTags tags,
            ushort fixedCount)
            : this(
                name,
                type,
                tags,
                Statics.InTypeFixedCountFlag,
                fixedCount,
                null)
        {
            return;
        }

        /// <summary>
        /// Custom serializer
        /// </summary>
        public FieldMetadata(
            string name,
            TraceLoggingDataType type,
            EventFieldTags tags,
            byte[] custom)
            : this(
                name,
                type,
                tags,
                Statics.InTypeCustomCountFlag,
                checked((ushort)(custom == null ? 0 : custom.Length)),
                custom)
        {
            return;
        }

        private FieldMetadata(
            string name,
            TraceLoggingDataType dataType,
            EventFieldTags tags,
            byte countFlags,
            ushort fixedCount = 0,
            byte[] custom = null)
        {
            if (name == null)
            {
                throw new ArgumentNullException(
                    nameof(name),
                    "This usually means that the object passed to Write is of a type that"
                    + " does not support being used as the top-level object in an event,"
                    + " e.g. a primitive or built-in type.");
            }

            Statics.CheckName(name);
            var coreType = (int)dataType & Statics.InTypeMask;
            this.name = name;
            this.nameSize = Encoding.UTF8.GetByteCount(this.name) + 1;
            this.inType = (byte)(coreType | countFlags);
            this.outType = (byte)(((int)dataType >> 8) & Statics.OutTypeMask);
            this.tags = tags;
            this.fixedCount = fixedCount;
            this.custom = custom;

            if (countFlags != 0)
            {
                if (coreType == (int)TraceLoggingDataType.Nil)
                {
                    throw new NotSupportedException(SR.EventSource_NotSupportedArrayOfNil);
                }
                if (coreType == (int)TraceLoggingDataType.Binary)
                {
                    throw new NotSupportedException(SR.EventSource_NotSupportedArrayOfBinary);
                }
#if !BROKEN_UNTIL_M3
                if (coreType == (int)TraceLoggingDataType.Utf16String ||
                    coreType == (int)TraceLoggingDataType.MbcsString)
                {
                    throw new NotSupportedException(SR.EventSource_NotSupportedArrayOfNullTerminatedString);
                }
#endif
            }

            if (((int)this.tags & 0xfffffff) != 0)
            {
                this.outType |= Statics.OutTypeChainFlag;
            }

            if (this.outType != 0)
            {
                this.inType |= Statics.InTypeChainFlag;
            }
        }

        public void IncrementStructFieldCount()
        {
            this.inType |= Statics.InTypeChainFlag;
            this.outType++;
            if ((this.outType & Statics.OutTypeMask) == 0)
            {
                throw new NotSupportedException(SR.EventSource_TooManyFields);
            }
        }

        /// <summary>
        /// This is the main routine for FieldMetaData.  Basically it will serialize the data in
        /// this structure as TraceLogging style meta-data into the array 'metaArray' starting at
        /// 'pos' (pos is updated to reflect the bytes written).  
        /// 
        /// Note that 'metaData' can be null, in which case it only updates 'pos'.  This is useful
        /// for a 'two pass' approach where you figure out how big to make the array, and then you
        /// fill it in.   
        /// </summary>
        public void Encode(ref int pos, byte[] metadata)
        {
            // Write out the null terminated UTF8 encoded name
            if (metadata != null)
            {
                Encoding.UTF8.GetBytes(this.name, 0, this.name.Length, metadata, pos);
            }
            pos += this.nameSize;

            // Write 1 byte for inType
            if (metadata != null)
            {
                metadata[pos] = this.inType;
            }
            pos += 1;

            // If InTypeChainFlag set, then write out the outType
            if (0 != (this.inType & Statics.InTypeChainFlag))
            {
                if (metadata != null)
                {
                    metadata[pos] = this.outType;
                }
                pos += 1;

                // If OutTypeChainFlag set, then write out tags
                if (0 != (this.outType & Statics.OutTypeChainFlag))
                {
                    Statics.EncodeTags((int)this.tags, ref pos, metadata);
                }
            }

            // If InTypeFixedCountFlag set, write out the fixedCount (2 bytes little endian)
            if (0 != (this.inType & Statics.InTypeFixedCountFlag))
            {
                if (metadata != null)
                {
                    metadata[pos + 0] = unchecked((byte)this.fixedCount);
                    metadata[pos + 1] = (byte)(this.fixedCount >> 8);
                }
                pos += 2;

                // If InTypeCustomCountFlag set, write out the blob of custom meta-data.  
                if (Statics.InTypeCustomCountFlag == (this.inType & Statics.InTypeCountMask) &&
                    this.fixedCount != 0)
                {
                    if (metadata != null)
                    {
                        Buffer.BlockCopy(this.custom, 0, metadata, pos, this.fixedCount);
                    }
                    pos += this.fixedCount;
                }
            }
        }
    }
}
