﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// Base class for non-enumerable, non-primitive objects where public properties
    /// are (de)serialized as a JSON object.
    /// </summary>
    internal abstract class JsonObjectConverter<T> : JsonResumableConverter<T>
    {
        internal sealed override ClassType ClassType => ClassType.Object;
        internal sealed override Type? ElementType => null;
    }
}
