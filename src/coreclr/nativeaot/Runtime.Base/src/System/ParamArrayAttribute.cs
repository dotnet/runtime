// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // Attribute to indicate array of arguments for variable number of args.

    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    internal class ParamArrayAttribute : Attribute
    {
        public ParamArrayAttribute()
        {
        }
    }
}
