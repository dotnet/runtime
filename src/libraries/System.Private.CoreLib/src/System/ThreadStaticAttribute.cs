// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    /// <summary>
    /// Indicates that the value of a static field is unique for each thread.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class ThreadStaticAttribute : Attribute
    {
        public ThreadStaticAttribute()
        {
        }
    }
}
