// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text.Json
{
    internal class JsonPreservedReference<T>
    {
#nullable disable
        public T Values { get; set; }
#nullable enable
    }
}
