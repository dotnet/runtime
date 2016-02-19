// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//#define BUFFER_BUILDER_TRACING

using System.Text;
using System.Diagnostics;

namespace System.Xml
{
    //
    //  Buffer Builder
    //
    // BufferBuilder is a replacement for StringBuilder for cases when large strings can occur.
    // StringBuilder stores the string that is being built in one large chunk of memory. If it needs more memory,
    // it allocates a new chunk of double size and copies the data into it. This results in bad perf and
    // memory constumption in case the string is very large (>85kB). Large objects are allocated on Large Object 
    // Heap and are not freed by GC as fast as smaller objects.
    // 
    // BufferBuilder uses a StringBuilder as long as the stored string is smaller that 64kB. If the final string
    // should be bigger that that, it stores the data in a list of char[] arrays. A StringBuilder object still needs to be 
    // used in order to create the final string in ToString methods, but this is ok since at that point 
    // we already know the resulting string length and we can initialize the StringBuilder with the correct 
    // capacity. 
    //
    // The BufferBuilder is designed for reusing. The Clear method will clear the state of the builder. 
    // The next string built by BufferBuilder will reuse the string builder and the buffer chunks allocated 
    // in the previous uses. (The string builder it not reused when it was last used to create a string >64kB because
    // setting Length=0 on the string builder makes it allocate the big string again.)
    // When the buffer chunks are not in use, they are stored as WeakReferences so they can be freed by GC 
    // in case memory-pressure situation happens.

#if BUFFER_BUILDER_TRACING
    public class BufferBuilder
    {
#else
    internal class BufferBuilder
    {
#endif
        //
        // Private types
        //
        private struct Buffer
        {
            internal char[] buffer;
            internal WeakReference recycledBuffer;
        }

        //
        // Fields
        //
        private StringBuilder _stringBuilder;

        private Buffer[] _buffers;
        private int _buffersCount;
        private char[] _lastBuffer;
        private int _lastBufferIndex;
        private int _length;

#if BUFFER_BUILDER_TRACING
        // 
        // Tracing
        // 
        public static TextWriter s_TraceOutput = null;
        static int minLength = int.MaxValue;
        static int maxLength;
        static int totalLength;
        static int toStringCount;
        static int totalAppendCount;
#endif

        //
        // Constants
        //
#if DEBUG
        // make it easier to catch buffer-related bugs on debug builds
        private const int BufferSize = 4 * 1024;
#else
        const int BufferSize = 64 * 1024;
#endif
        private const int InitialBufferArrayLength = 4;
        private const int MaxStringBuilderLength = BufferSize;
        private const int DefaultSBCapacity = 16;

        //
        // Constructor
        //
        public BufferBuilder()
        {
#if BUFFER_BUILDER_TRACING
            if (s_TraceOutput != null)
            {
                s_TraceOutput.WriteLine("----------------------------" + Environment.NewLine + 
                "new BufferBuilder()" + Environment.NewLine + 
                "----------------------------");
            }
#endif
        }

        //
        // Properties
        //
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
#if BUFFER_BUILDER_TRACING
                if (s_TraceOutput != null)
                {
                    s_TraceOutput.WriteLine("BufferBuilder.Length = " + value);
                }
#endif

                if (value < 0 || value > _length)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                if (value == 0)
                {
                    Clear();
                }
                else
                {
                    SetLength(value);
                }
            }
        }

        //
        // Public methods
        //

        public void Append(char value)
        {
#if BUFFER_BUILDER_TRACING
            if (s_TraceOutput != null)
            {
                s_TraceOutput.WriteLine("BufferBuilder.Append\tLength = 1\tchar '" + value.ToString() + "'");
                totalAppendCount++;
            }
#endif
            if (_length + 1 <= MaxStringBuilderLength)
            {
                if (_stringBuilder == null)
                {
                    _stringBuilder = new StringBuilder();
                }
                _stringBuilder.Append(value);
            }
            else
            {
                if (_lastBuffer == null)
                {
                    CreateBuffers();
                }
                if (_lastBufferIndex == _lastBuffer.Length)
                {
                    AddBuffer();
                }
                _lastBuffer[_lastBufferIndex++] = value;
            }
            _length++;
        }

#if !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        public void Append(char[] value, int start, int count)
        {
#if BUFFER_BUILDER_TRACING
            if (s_TraceOutput != null)
            {
                s_TraceOutput.WriteLine("BufferBuilder.Append\tLength = " + count + "\t char array \"" + new string(value, start, count) + "\"");
                totalAppendCount++;
            }
#endif
            if (value == null)
            {
                if (start == 0 && count == 0)
                {
                    return;
                }
                throw new ArgumentNullException(nameof(value));
            }
            if (count == 0)
            {
                return;
            }
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0 || start + count > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (_length + count <= MaxStringBuilderLength)
            {
                if (_stringBuilder == null)
                {
                    _stringBuilder = new StringBuilder(count < DefaultSBCapacity ? DefaultSBCapacity : count);
                }
                _stringBuilder.Append(value, start, count);
                _length += count;
            }
            else
            {
                unsafe
                {
                    fixed (char* source = &value[start])
                    {
                        AppendHelper(source, count);
                    }
                }
            }
        }

        public void Append(string value)
        {
            Append(value, 0, value.Length);
        }

#if !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecuritySafeCritical]
#endif
        public void Append(string value, int start, int count)
        {
#if BUFFER_BUILDER_TRACING
            if (s_TraceOutput != null)
            {
                s_TraceOutput.WriteLine("BufferBuilder.Append\tLength = " + count + "\t string fragment \"" + value.Substring(start, count) + "\"");
                totalAppendCount++;
            }
#endif
            if (value == null)
            {
                if (start == 0 && count == 0)
                {
                    return;
                }
                throw new ArgumentNullException(nameof(value));
            }
            if (count == 0)
            {
                return;
            }
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start));
            }
            if (count < 0 || start + count > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }
            if (_length + count <= MaxStringBuilderLength)
            {
                if (_stringBuilder == null)
                {
                    _stringBuilder = new StringBuilder(value, start, count, 0);
                }
                else
                {
                    _stringBuilder.Append(value, start, count);
                }
                _length += count;
            }
            else
            {
                unsafe
                {
                    fixed (char* source = value)
                    {
                        AppendHelper(source + start, count);
                    }
                }
            }
        }

        public void Clear()
        {
            if (_length <= MaxStringBuilderLength)
            {
                if (_stringBuilder != null)
                {
                    _stringBuilder.Length = 0;
                }
            }
            else
            {
                if (_lastBuffer != null)
                {
                    ClearBuffers();
                }
                // destroy the string builder because setting its Length or Capacity to 0 makes it allocate the last string again :-|
                _stringBuilder = null;
            }
            _length = 0;
        }

        internal void ClearBuffers()
        {
            if (_buffers != null)
            {
                // recycle all but the first the buffer
                for (int i = 0; i < _buffersCount; i++)
                {
                    Recycle(ref _buffers[i]);
                }
                _lastBuffer = null;
            }
            else
            {
                // just one buffer allocated with no buffers array -> no recycling
            }
            _lastBufferIndex = 0;
            _buffersCount = 0;
        }

        public override string ToString()
        {
            string returnString;
            if ((_length <= MaxStringBuilderLength) || (_buffersCount == 1 && _lastBufferIndex == 0))
            {
                returnString = (_stringBuilder != null) ? _stringBuilder.ToString() : string.Empty;
            }
            else
            {
                if (_stringBuilder == null)
                {
                    _stringBuilder = new StringBuilder(_length);
                }
                else
                {
                    _stringBuilder.Capacity = _length;
                }
                int charsLeft = _length - _stringBuilder.Length;
                for (int i = 0; i < _buffersCount - 1; i++)
                {
                    char[] buf = _buffers[i].buffer;
                    _stringBuilder.Append(buf, 0, buf.Length);
                    charsLeft -= buf.Length;
                }
                _stringBuilder.Append(_buffers[_buffersCount - 1].buffer, 0, charsLeft);
                ClearBuffers();
                returnString = _stringBuilder.ToString();
            }
#if BUFFER_BUILDER_TRACING
            if (s_TraceOutput != null)
            {
                s_TraceOutput.WriteLine("BufferBuilder.ToString() Length == " + returnString.Length + "\t \"" + returnString + "\"");
                toStringCount++;
                totalLength += returnString.Length;
                if (minLength > returnString.Length)
                {
                    minLength = returnString.Length;
                }
                if (maxLength < returnString.Length)
                {
                    maxLength = returnString.Length;
                }
            }
#endif
            return returnString;
        }



        //
        // Private implementation methods
        //
        private void CreateBuffers()
        {
            Debug.Assert(_lastBuffer == null);
            if (_buffers == null)
            {
                _lastBuffer = new char[BufferSize];
                _buffers = new Buffer[InitialBufferArrayLength];
                _buffers[0].buffer = _lastBuffer;
                _buffersCount = 1;
            }
            else
            {
                AddBuffer();
            }
        }

#if !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecurityCritical]
#endif
        unsafe private void AppendHelper(char* pSource, int count)
        {
            if (_lastBuffer == null)
            {
                CreateBuffers();
            }
            int copyCount = 0;
            while (count > 0)
            {
                if (_lastBufferIndex >= _lastBuffer.Length)
                {
                    AddBuffer();
                }

                copyCount = count;
                int free = _lastBuffer.Length - _lastBufferIndex;
                if (free < copyCount)
                {
                    copyCount = free;
                }

                fixed (char* pLastBuffer = &_lastBuffer[_lastBufferIndex])
                {
                    wstrcpy(pLastBuffer, pSource, copyCount);
                }
                pSource += copyCount;
                _length += copyCount;
                _lastBufferIndex += copyCount;
                count -= copyCount;
            }
        }

        private void AddBuffer()
        {
            Debug.Assert(_buffers != null);

            // check the buffers array it its big enough
            if (_buffersCount + 1 == _buffers.Length)
            {
                Buffer[] newBuffers = new Buffer[_buffers.Length * 2];
                Array.Copy(_buffers, 0, newBuffers, 0, _buffers.Length);
                _buffers = newBuffers;
            }

            // use the recycled buffer if we have one
            char[] newBuffer;
            if (_buffers[_buffersCount].recycledBuffer != null)
            {
                newBuffer = (char[])_buffers[_buffersCount].recycledBuffer.Target;
                if (newBuffer != null)
                {
                    _buffers[_buffersCount].recycledBuffer.Target = null;
                    goto End;
                }
            }
            newBuffer = new char[BufferSize];
        End:
            // add the buffer to the list
            _lastBuffer = newBuffer;
            _buffers[_buffersCount++].buffer = newBuffer;
            _lastBufferIndex = 0;
        }

        private void Recycle(ref Buffer buf)
        {
            // recycled buffers are kept as WeakReferences 
            if (buf.recycledBuffer == null)
            {
                buf.recycledBuffer = new WeakReference(buf.buffer);
            }
            else
            {
                buf.recycledBuffer.Target = buf.buffer;
            }
#if DEBUG
            for (int i = 0; i < buf.buffer.Length; i++)
            {
                buf.buffer[i] = (char)0xCC;
            }
#endif
            buf.buffer = null;
        }

        private void SetLength(int newLength)
        {
            Debug.Assert(newLength <= _length);

            if (newLength == _length)
            {
                return;
            }

            if (_length <= MaxStringBuilderLength)
            {
                _stringBuilder.Length = newLength;
            }
            else
            {
                int newLastIndex = newLength;
                int i;
                for (i = 0; i < _buffersCount; i++)
                {
                    if (newLastIndex < _buffers[i].buffer.Length)
                    {
                        break;
                    }
                    newLastIndex -= _buffers[i].buffer.Length;
                }
                if (i < _buffersCount)
                {
                    _lastBuffer = _buffers[i].buffer;
                    _lastBufferIndex = newLastIndex;
                    i++;
                    int newBuffersCount = i;
                    for (; i < _buffersCount; i++)
                    {
                        Recycle(ref _buffers[i]);
                    }
                    _buffersCount = newBuffersCount;
                }
            }
            _length = newLength;
        }

#if !SILVERLIGHT_DISABLE_SECURITY
        [System.Security.SecurityCritical]
#endif
        internal static unsafe void wstrcpy(char* dmem, char* smem, int charCount)
        {
            if (charCount > 0)
            {
                if ((((int)dmem ^ (int)smem) & 3) == 0)
                {
                    while (((int)dmem & 3) != 0 && charCount > 0)
                    {
                        dmem[0] = smem[0];
                        dmem += 1;
                        smem += 1;
                        charCount -= 1;
                    }
                    if (charCount >= 8)
                    {
                        charCount -= 8;
                        do
                        {
                            ((uint*)dmem)[0] = ((uint*)smem)[0];
                            ((uint*)dmem)[1] = ((uint*)smem)[1];
                            ((uint*)dmem)[2] = ((uint*)smem)[2];
                            ((uint*)dmem)[3] = ((uint*)smem)[3];
                            dmem += 8;
                            smem += 8;
                            charCount -= 8;
                        } while (charCount >= 0);
                    }
                    if ((charCount & 4) != 0)
                    {
                        ((uint*)dmem)[0] = ((uint*)smem)[0];
                        ((uint*)dmem)[1] = ((uint*)smem)[1];
                        dmem += 4;
                        smem += 4;
                    }
                    if ((charCount & 2) != 0)
                    {
                        ((uint*)dmem)[0] = ((uint*)smem)[0];
                        dmem += 2;
                        smem += 2;
                    }
                }
                else
                {
                    if (charCount >= 8)
                    {
                        charCount -= 8;
                        do
                        {
                            dmem[0] = smem[0];
                            dmem[1] = smem[1];
                            dmem[2] = smem[2];
                            dmem[3] = smem[3];
                            dmem[4] = smem[4];
                            dmem[5] = smem[5];
                            dmem[6] = smem[6];
                            dmem[7] = smem[7];
                            dmem += 8;
                            smem += 8;
                            charCount -= 8;
                        }
                        while (charCount >= 0);
                    }
                    if ((charCount & 4) != 0)
                    {
                        dmem[0] = smem[0];
                        dmem[1] = smem[1];
                        dmem[2] = smem[2];
                        dmem[3] = smem[3];
                        dmem += 4;
                        smem += 4;
                    }
                    if ((charCount & 2) != 0)
                    {
                        dmem[0] = smem[0];
                        dmem[1] = smem[1];
                        dmem += 2;
                        smem += 2;
                    }
                }

                if ((charCount & 1) != 0)
                {
                    dmem[0] = smem[0];
                }
            }
        }
#if BUFFER_BUILDER_TRACING
        public static int ToStringCount
        {
            get
            {
                return toStringCount;
            }
        }

        public static double AvgAppendCount
        {
            get
            {
                return toStringCount == 0 ? 0 : (double)totalAppendCount / toStringCount;
            }
        }

        public static int AvgLength
        {
            get
            {
                return toStringCount == 0 ? 0 : totalLength / toStringCount;
            }
        }

        public static int MaxLength
        {
            get
            {
                return maxLength;
            }
        }

        public static int MinLength
        {
            get
            {
                return minLength;
            }
        }
#endif
    }
}
