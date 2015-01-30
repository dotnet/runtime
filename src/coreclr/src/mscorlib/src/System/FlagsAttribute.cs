// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
namespace System {
    
    using System;
    // Custom attribute to indicate that the enum
    // should be treated as a bitfield (or set of flags).
    // An IDE may use this information to provide a richer
    // development experience.
    [Serializable]
    [AttributeUsage(AttributeTargets.Enum, Inherited = false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public class FlagsAttribute : Attribute
    {
        public FlagsAttribute()
        {
        }
    }
}
