// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// This class defines the various ways the <see cref="JsonSerializer"/> can deal with references on Serialization and Deserialization.
    /// </summary>
    public sealed class ReferenceHandling
    {
        /// <summary>
        /// Preserved JSON complex types will not be written/readen.
        /// </summary>
        public static ReferenceHandling Default => new ReferenceHandling(PreserveReferencesHandling.None);
        /// <summary>
        /// Preserved JSON complex types will be written/readen.
        /// </summary>
        public static ReferenceHandling Preserve => new ReferenceHandling(PreserveReferencesHandling.All);

        /// <summary>
        /// Instances that show circularity will not be written.
        /// </summary>
        public static ReferenceHandling Ignore => new ReferenceHandling(PreserveReferencesHandling.None, PreserveReferencesHandling.None, ReferenceLoopHandling.Ignore);

        // For future, change to public if needed.
        internal PreserveReferencesHandling PreserveHandlingOnSerialize => _preserveHandlingOnSerialize;
        internal PreserveReferencesHandling PreserveHandlingOnDeserialize => _preserveHandlingOnDeserialize;
        internal ReferenceLoopHandling LoopHandling => _loopHandling;

        private PreserveReferencesHandling _preserveHandlingOnSerialize;
        private PreserveReferencesHandling _preserveHandlingOnDeserialize;
        private ReferenceLoopHandling _loopHandling;

        /// <summary>
        /// Creates a new instance of <see cref="ReferenceHandling"/> using the specified <paramref name="handling"/>
        /// </summary>
        /// <param name="handling">The specified behavior for write/read preserved references.</param>
        internal ReferenceHandling(PreserveReferencesHandling handling) : this(handling, handling, ReferenceLoopHandling.Error) { }

        // For future, someone may want to define their own custom Handler with different behaviors of PreserveReferenceHandling on Serialize vs Deserialize and another ReferenceLoopHandling, such as ignore if added in a future.
        private ReferenceHandling(PreserveReferencesHandling preserveHandlingOnSerialize, PreserveReferencesHandling preserveHandlingOnDeserialize, ReferenceLoopHandling loopHandling)
        {
            _preserveHandlingOnSerialize = preserveHandlingOnSerialize;
            _preserveHandlingOnDeserialize = preserveHandlingOnDeserialize;
            _loopHandling = loopHandling;
        }
    }

    internal enum ReferenceLoopHandling
    {
        Error = 0,
        // For future if requested by the community.
        Ignore = 1,
    }


    /// <summary>
    /// Defines behaviors to preserve references of JSON complex types.
    /// </summary>
    internal enum PreserveReferencesHandling
    {
        /// <summary>
        /// Preserved objects and arrays will not be written/readen.
        /// </summary>
        None = 0,
        /// <summary>
        /// Preserved objects and arrays will be written/readen.
        /// </summary>
        All = 1,

        // For future if requested by the community.
        // Objects = 2,
        // Arrays = 3,
    }
}
