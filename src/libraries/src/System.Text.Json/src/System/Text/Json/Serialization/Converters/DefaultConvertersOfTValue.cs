// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization.Policies;

namespace System.Text.Json.Serialization.Converters
{
    internal static class DefaultConverters<TValue>
    {
        internal static readonly JsonValueConverter<TValue> s_converter = (JsonValueConverter<TValue>)DefaultConverters.Create(typeof(TValue));
    }
}
