// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace System.Text.Json.Serialization.Policies
{
    internal abstract class JsonEnumerableConverter
    {
        public abstract IEnumerable CreateFromList(ref ReadStack state, IList sourceList, JsonSerializerOptions options);
    }
}
