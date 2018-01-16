// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security;

#if ES_BUILD_STANDALONE
using Environment = Microsoft.Diagnostics.Tracing.Internal.Environment;
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// TraceLogging: This is the implementation of the DataCollector
    /// functionality. To enable safe access to the DataCollector from
    /// untrusted code, there is one thread-local instance of this structure
    /// per thread. The instance must be Enabled before any data is written to
    /// it. The instance must be Finished before the data is passed to
    /// EventWrite. The instance must be Disabled before the arrays referenced
    /// by the pointers are freed or unpinned.
    /// </summary>
    internal unsafe struct DataCollector
    {
        [ThreadStatic]
        internal static DataCollector ThreadInstance;

        private byte* scratchEnd;
        private EventSource.EventData* datasEnd;
        private GCHandle* pinsEnd;
        private EventSource.EventData* datasStart;
        private byte* scratch;
        private EventSource.EventData* datas;
        private GCHandle* pins;
        private byte[] buffer;
        private int bufferPos;
        private int bufferNesting;          // We may merge many fields int a single blob.   If we are doing this we increment this. 
        private bool writingScalars;

        internal void Enable(
            byte* scratch,
            int scratchSize,
            EventSource.EventData* datas,
            int dataCount,
            GCHandle* pins,
            int pinCount)
        {
            this.datasStart = datas;
            this.scratchEnd = scratch + scratchSize;
            this.datasEnd = datas + dataCount;
            this.pinsEnd = pins + pinCount;
            this.scratch = scratch;
            this.datas = datas;
            this.pins = pins;
            this.writingScalars = false;
        }

        internal void Disable()
        {
            this = new DataCollector();
        }

        /// <summary>
        /// Completes the list of scalars. Finish must be called before the data
        /// descriptor array is passed to EventWrite.
        /// </summary>
        /// <returns>
        /// A pointer to the next unused data descriptor, or datasEnd if they were
        /// all used. (Descriptors may be unused if a string or array was null.)
        /// </returns>
        internal EventSource.EventData* Finish()
        {
            this.ScalarsEnd();
            return this.datas;
        }

        internal void AddScalar(void* value, int size)
        {
            var pb = (byte*)value;
            if (this.bufferNesting == 0)
            {
                var scratchOld = this.scratch;
                var scratchNew = scratchOld + size;
                if (this.scratchEnd < scratchNew)
                {
                    throw new IndexOutOfRangeException(SR.EventSource_AddScalarOutOfRange);
                }

                this.ScalarsBegin();
                this.scratch = scratchNew;

                for (int i = 0; i != size; i++)
                {
                    scratchOld[i] = pb[i];
                }
            }
            else
            {
                var oldPos = this.bufferPos;
                this.bufferPos = checked(this.bufferPos + size);
                this.EnsureBuffer();
                for (int i = 0; i != size; i++, oldPos++)
                {
                    this.buffer[oldPos] = pb[i];
                }
            }
        }

        internal void AddBinary(string value, int size)
        {
            if (size > ushort.MaxValue)
            {
                size = ushort.MaxValue - 1;
            }

            if (this.bufferNesting != 0)
            {
                this.EnsureBuffer(size + 2);
            }

            this.AddScalar(&size, 2);

            if (size != 0)
            {
                if (this.bufferNesting == 0)
                {
                    this.ScalarsEnd();
                    this.PinArray(value, size);
                }
                else
                {
                    var oldPos = this.bufferPos;
                    this.bufferPos = checked(this.bufferPos + size);
                    this.EnsureBuffer();
                    fixed (void* p = value)
                    {
                        Marshal.Copy((IntPtr)p, buffer, oldPos, size);
                    }
                }
            }
        }

        internal void AddBinary(Array value, int size)
        {
            this.AddArray(value, size, 1);
        }

        internal void AddArray(Array value, int length, int itemSize)
        {
            if (length > ushort.MaxValue)
            {
                length = ushort.MaxValue;
            }

            var size = length * itemSize;
            if (this.bufferNesting != 0)
            {
                this.EnsureBuffer(size + 2);
            }

            this.AddScalar(&length, 2);

            if (length != 0)
            {
                if (this.bufferNesting == 0)
                {
                    this.ScalarsEnd();
                    this.PinArray(value, size);
                }
                else
                {
                    var oldPos = this.bufferPos;
                    this.bufferPos = checked(this.bufferPos + size);
                    this.EnsureBuffer();
                    Buffer.BlockCopy(value, 0, this.buffer, oldPos, size);
                }
            }
        }

        /// <summary>
        /// Marks the start of a non-blittable array or enumerable.
        /// </summary>
        /// <returns>Bookmark to be passed to EndBufferedArray.</returns>
        internal int BeginBufferedArray()
        {
            this.BeginBuffered();
            this.bufferPos += 2; // Reserve space for the array length (filled in by EndEnumerable)
            return this.bufferPos;
        }

        /// <summary>
        /// Marks the end of a non-blittable array or enumerable.
        /// </summary>
        /// <param name="bookmark">The value returned by BeginBufferedArray.</param>
        /// <param name="count">The number of items in the array.</param>
        internal void EndBufferedArray(int bookmark, int count)
        {
            this.EnsureBuffer();
            this.buffer[bookmark - 2] = unchecked((byte)count);
            this.buffer[bookmark - 1] = unchecked((byte)(count >> 8));
            this.EndBuffered();
        }

        /// <summary>
        /// Marks the start of dynamically-buffered data.
        /// </summary>
        internal void BeginBuffered()
        {
            this.ScalarsEnd();
            this.bufferNesting += 1;
        }

        /// <summary>
        /// Marks the end of dynamically-buffered data.
        /// </summary>
        internal void EndBuffered()
        {
            this.bufferNesting -= 1;

            if (this.bufferNesting == 0)
            {
                /*
                TODO (perf): consider coalescing adjacent buffered regions into a
                single buffer, similar to what we're already doing for adjacent
                scalars. In addition, if a type contains a buffered region adjacent
                to a blittable array, and the blittable array is small, it would be
                more efficient to buffer the array instead of pinning it.
                */

                this.EnsureBuffer();
                this.PinArray(this.buffer, this.bufferPos);
                this.buffer = null;
                this.bufferPos = 0;
            }
        }

        private void EnsureBuffer()
        {
            var required = this.bufferPos;
            if (this.buffer == null || this.buffer.Length < required)
            {
                this.GrowBuffer(required);
            }
        }

        private void EnsureBuffer(int additionalSize)
        {
            var required = this.bufferPos + additionalSize;
            if (this.buffer == null || this.buffer.Length < required)
            {
                this.GrowBuffer(required);
            }
        }

        private void GrowBuffer(int required)
        {
            var newSize = this.buffer == null ? 64 : this.buffer.Length;

            do
            {
                newSize *= 2;
            }
            while (newSize < required);

            Array.Resize(ref this.buffer, newSize);
        }

        private void PinArray(object value, int size)
        {
            var pinsTemp = this.pins;
            if (this.pinsEnd <= pinsTemp)
            {
                throw new IndexOutOfRangeException(SR.EventSource_PinArrayOutOfRange);
            }

            var datasTemp = this.datas;
            if (this.datasEnd <= datasTemp)
            {
                throw new IndexOutOfRangeException(SR.EventSource_DataDescriptorsOutOfRange);
            }

            this.pins = pinsTemp + 1;
            this.datas = datasTemp + 1;

            *pinsTemp = GCHandle.Alloc(value, GCHandleType.Pinned);
            datasTemp->DataPointer = pinsTemp->AddrOfPinnedObject();
            datasTemp->m_Size = size;
        }

        private void ScalarsBegin()
        {
            if (!this.writingScalars)
            {
                var datasTemp = this.datas;
                if (this.datasEnd <= datasTemp)
                {
                    throw new IndexOutOfRangeException(SR.EventSource_DataDescriptorsOutOfRange);
                }

                datasTemp->DataPointer = (IntPtr) this.scratch;
                this.writingScalars = true;
            }
        }

        private void ScalarsEnd()
        {
            if (this.writingScalars)
            {
                var datasTemp = this.datas;
                datasTemp->m_Size = checked((int)(this.scratch - (byte*)datasTemp->m_Ptr));
                this.datas = datasTemp + 1;
                this.writingScalars = false;
            }
        }
    }
}
