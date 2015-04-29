// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

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
