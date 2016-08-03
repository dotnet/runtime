// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace DPStressHarness
{
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class GlobalExceptionHandlerAttribute : Attribute
    {
        public GlobalExceptionHandlerAttribute()
        {
        }
    }
}
