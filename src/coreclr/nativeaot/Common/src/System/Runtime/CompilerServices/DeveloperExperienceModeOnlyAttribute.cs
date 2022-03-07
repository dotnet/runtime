// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.CompilerServices
{
    //
    // Attach to classes that contain code only used in ILC /BuildType:chk builds.
    //
    // Any class attributed with this must have the following properties:
    //
    //  - Class must be declared "static"
    //
    //  - All public/internal methods must have a return type of:
    //
    //       void
    //       bool
    //       any non-value type
    //
    //  - All fields must be private.
    //
    //  - Class constructor must not have externally visible side effects.
    //
    //
    // On /BuildType:ret builds, ILC will run a special transform that
    // turns all of the public and internal method bodies into
    // the equivalent of:
    //
    //    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //    T Foo()
    //    {
    //       return default(T);
    //    }
    //
    // It also removes all fields and private methods (including the class constructor.)
    //
    // The method semantics must be defined so that ret builds have
    // the desired behavior with these implementations.
    //
    //
    [AttributeUsage(AttributeTargets.Class)]
    internal sealed class DeveloperExperienceModeOnlyAttribute : Attribute
    {
    }
}
