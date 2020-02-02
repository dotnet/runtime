// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    /// <summary>
    /// JSON objects that contain metadata properties and the nested JSON array are wrapped into this class.
    /// </summary>
    /// <typeparam name="T">The original type of the enumerable.</typeparam>
    internal class JsonPreservableArrayReference<T>
    {
        /// <summary>
        ///  The actual enumerable instance being preserved is extracted when we finish processing the JSON object on HandleEndObject.
        /// </summary>
        public T Values { get; set; } = default!;
    }
}
