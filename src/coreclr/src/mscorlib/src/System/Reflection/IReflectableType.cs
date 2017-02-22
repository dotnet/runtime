// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// IReflectableType is an interface that is implemented by a Type produced 
// by a ReflectionContext
// 

//

using System;

namespace System.Reflection
{
    public interface IReflectableType
    {
        TypeInfo GetTypeInfo();
    }
}
