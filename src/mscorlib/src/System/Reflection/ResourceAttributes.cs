// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// ResourceAttributes is an enum which defines the attributes that may be associated
// 
//  with a manifest resource.  The values here are defined in Corhdr.h.
//
//

using System;

namespace System.Reflection
{
    [Serializable]
    [Flags]
    public enum ResourceAttributes
    {
        Public = 0x0001,
        Private = 0x0002,
    }
}
