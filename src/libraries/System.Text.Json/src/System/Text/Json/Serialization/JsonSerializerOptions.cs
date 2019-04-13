// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Provides options to be used with <see cref="JsonSerializer"/>.
    /// </summary>
    public sealed class JsonSerializerOptions
    {
        internal const int BufferSizeDefault = 16 * 1024;

        internal static readonly JsonSerializerOptions s_defaultOptions = new JsonSerializerOptions();

        private readonly ConcurrentDictionary<Type, JsonClassInfo> _classes = new ConcurrentDictionary<Type, JsonClassInfo>();
        private ClassMaterializer _classMaterializerStrategy;
        private JsonCommentHandling _readCommentHandling;
        private int _defaultBufferSize = BufferSizeDefault;
        private int _maxDepth;
        private bool _allowTrailingCommas;
        private bool _haveTypesBeenCreated;
        private bool _ignoreNullValues;
        private bool _ignoreReadOnlyProperties;
        private bool _writeIndented;

        /// <summary>
        /// Constructs a new <see cref="JsonSerializerOptions"/> instance.
        /// </summary>
        public JsonSerializerOptions() { }

        /// <summary>
        /// Defines whether an extra comma at the end of a list of JSON values in an object or array
        /// is allowed (and ignored) within the JSON payload being read.
        /// By default, it's set to false, and <exception cref="JsonReaderException"/> is thrown if a trailing comma is encountered.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool AllowTrailingCommas
        {
            get
            {
                return _allowTrailingCommas;
            }
            set
            {
                VerifyMutable();
                _allowTrailingCommas = value;
            }
        }

        /// <summary>
        /// The default buffer size in bytes used when creating temporary buffers.
        /// </summary>
        /// <remarks>The default size is 16K.</remarks>
        /// <exception cref="System.ArgumentException">Thrown when the buffer size is less than 1.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public int DefaultBufferSize
        {
            get
            {
                return _defaultBufferSize;
            }
            set
            {
                VerifyMutable();

                if (value < 1)
                {
                    throw new ArgumentException(SR.SerializationInvalidBufferSize);
                }

                _defaultBufferSize = value;
            }
        }

        /// <summary>
        /// Determines whether null values are ignored during serialization and deserialization.
        /// The default value is false.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool IgnoreNullValues
        {
            get
            {
                return _ignoreNullValues;
            }
            set
            {
                VerifyMutable();
                _ignoreNullValues = value;
            }
        }

        /// <summary>
        /// Determines whether read-only properties are ignored during serialization and deserialization.
        /// A property is read-only if it contains a public getter but not a public setter.
        /// The default value is false.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool IgnoreReadOnlyProperties
        {
            get
            {
                return _ignoreReadOnlyProperties;
            }
            set
            {
                VerifyMutable();
                _ignoreReadOnlyProperties = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum depth allowed when reading or writing JSON, with the default (i.e. 0) indicating a max depth of 64.
        /// Reading past this depth will throw a <exception cref="JsonReaderException"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public int MaxDepth
        {
            get
            {
                return _maxDepth;
            }
            set
            {
                VerifyMutable();
                _maxDepth = value;
            }
        }

        /// <summary>
        /// Defines how the comments are handled during deserialization.
        /// By default <exception cref="JsonReaderException"/> is thrown if a comment is encountered.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public JsonCommentHandling ReadCommentHandling
        {
            get
            {
                return _readCommentHandling;
            }
            set
            {
                VerifyMutable();
                _readCommentHandling = value;
            }
        }

        /// <summary>
        /// Defines whether JSON should pretty print which includes:
        /// indenting nested JSON tokens, adding new lines, and adding white space between property names and values.
        /// By default, the JSON is written without any extra white space.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown if this property is set after serialization or deserialization has occurred.
        /// </exception>
        public bool WriteIndented
        {
            get
            {
                return _writeIndented;
            }
            set
            {
                VerifyMutable();
                _writeIndented = value;
            }
        }

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

        internal JsonClassInfo GetOrAddClass(Type classType)
        {
            _haveTypesBeenCreated = true;

            // todo: for performance, consider obtaining the type from s_defaultOptions and then cloning.
            if (!_classes.TryGetValue(classType, out JsonClassInfo result))
            {
                result = _classes.GetOrAdd(classType, new JsonClassInfo(classType, this));
            }

            return result;
        }

        internal JsonReaderOptions GetReaderOptions()
        {
            return new JsonReaderOptions
            {
                AllowTrailingCommas = AllowTrailingCommas,
                CommentHandling = ReadCommentHandling,
                MaxDepth = MaxDepth
            };
        }

        internal JsonWriterOptions GetWriterOptions()
        {
            return new JsonWriterOptions
            {
                Indented = WriteIndented
            };
        }

        private void VerifyMutable()
        {
            // The default options are hidden and thus should be immutable.
            Debug.Assert(this != s_defaultOptions);

            if (_haveTypesBeenCreated)
            {
                ThrowHelper.ThrowInvalidOperationException_SerializerOptionsImmutable();
            }
        }
    }
}
