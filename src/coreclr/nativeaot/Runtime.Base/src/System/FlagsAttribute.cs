// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // Custom attribute to indicate that the enum
    // should be treated as a bitfield (or set of flags).
    // An IDE may use this information to provide a richer
    // development experience.
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    public class FlagsAttribute : Attribute
    {
        public FlagsAttribute()
        {
        }
    }
}
