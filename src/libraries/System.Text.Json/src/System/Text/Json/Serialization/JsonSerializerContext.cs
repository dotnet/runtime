// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// todo
    /// </summary>
    public partial class JsonSerializerContext : IDisposable
    {
        private JsonSerializerOptions? _userSpecifiedOptions;
        internal JsonSerializerOptions _options;

        /// <summary>
        /// todo
        /// </summary>
        public JsonSerializerContext()
        {
            _options = JsonSerializerOptions.s_defaultOptions;
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="options"></param>
        public JsonSerializerContext(JsonSerializerOptions options)
        {
            _userSpecifiedOptions = _options = options;
        }

        /// <summary>
        /// todo
        /// </summary>
        public JsonSerializerOptions? GetOptions()
        {
            return _userSpecifiedOptions;
        }

        /// <summary>
        /// todo
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// todo
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            // todo: code-gen _options.RemoveMetadataForType(type)
            // likely need a reference count mechanism to handle context that share the same types.
            if (disposing)
            {
            }
        }
    }
}
