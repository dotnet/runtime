// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// IReflectableType is an interface that is implemented by a Type produced 
// by a ReflectionContext
// 

//
namespace System.Reflection {
    
    using System;
    
    public interface IReflectableType {
        TypeInfo GetTypeInfo();
    }
}
