// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    // Implement ICloneable internally.  
    // We aren't concerned with exposing this type for lightup on platforms that support ICloneable.
    // ICloneable is an unsupported type.
    internal interface ICloneable
    {
        object Clone();
    }
}
