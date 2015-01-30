// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//------------------------------------------------------------------------------
//------------------------------------------------------------------------------

namespace System.Runtime.CompilerServices
{
    using System;

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false, Inherited=false)]
    public sealed class DisablePrivateReflectionAttribute : Attribute
    {
        public DisablePrivateReflectionAttribute() {}
    }
}

