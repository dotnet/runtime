// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace System.Text.Json.Serialization
{
    public sealed class JsonSerializerOptions
    {
        internal const int BufferSizeUnspecified = -1;
        internal const int BufferSizeDefault = 16 * 1024;

        private ClassMaterializer _classMaterializerStrategy;
        private int _defaultBufferSize = BufferSizeUnspecified;

        private static readonly ConcurrentDictionary<Type, JsonClassInfo> s_classes = new ConcurrentDictionary<Type, JsonClassInfo>();

        public JsonSerializerOptions() { }

        internal JsonClassInfo GetOrAddClass(Type classType)
        {
            JsonClassInfo result;

            if (!s_classes.TryGetValue(classType, out result))
            {
                result = s_classes.GetOrAdd(classType, new JsonClassInfo(classType, this));
            }

            return result;
        }

        public JsonReaderOptions ReaderOptions { get; set; }
        public JsonWriterOptions WriterOptions { get; set; }

        public int DefaultBufferSize
        {
            get
            {
                return _defaultBufferSize;
            }
            set
            {
                if (value == 0 || value < BufferSizeUnspecified)
                {
                    throw new ArgumentException(SR.SerializationInvalidBufferSize);
                }

                _defaultBufferSize = value;

                if (_defaultBufferSize == BufferSizeUnspecified)
                {
                    EffectiveBufferSize = BufferSizeDefault;
                }
                else
                {
                    EffectiveBufferSize = _defaultBufferSize;
                }
            }
        }

        public bool IgnoreNullPropertyValueOnWrite { get; set; }
        public bool IgnoreNullPropertyValueOnRead { get; set; }

        // Used internally for performance to avoid checking BufferSizeUnspecified.
        internal int EffectiveBufferSize { get; private set; } = BufferSizeDefault;

        internal ClassMaterializer ClassMaterializerStrategy
        {
            get
            {
                if (_classMaterializerStrategy == null)
                {
#if BUILDING_INBOX_LIBRARY
                    _classMaterializerStrategy = new ReflectionEmitMaterializer();
#else
                    // todo: should we attempt to detect here, or at least have a #define like #SUPPORTS_IL_EMIT
                    _classMaterializerStrategy = new ReflectionMaterializer();
#endif
                }

                return _classMaterializerStrategy;
            }
        }
    }
}
