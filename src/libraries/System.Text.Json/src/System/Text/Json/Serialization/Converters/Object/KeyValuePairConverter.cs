// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace System.Text.Json.Serialization.Converters
{
    internal sealed class KeyValuePairConverter<TKey, TValue> :
        SmallObjectWithParameterizedConstructorConverter<KeyValuePair<TKey, TValue>, TKey, TValue, object, object>
    {
        private static readonly ConstructorInfo s_constructorInfo =
            typeof(KeyValuePair<TKey, TValue>).GetConstructor(new[] { typeof(TKey), typeof(TValue) })!;

        public KeyValuePairConverter()
        {
            ConstructorInfo = s_constructorInfo;
            Debug.Assert(ConstructorInfo != null);
        }
    }
}
