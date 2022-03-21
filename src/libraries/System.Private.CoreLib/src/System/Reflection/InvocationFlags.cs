// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    //
    // Invocation cached flags. Those are used in unmanaged code as well
    // so be careful if you change them
    //
    [Flags]
    internal enum InvocationFlags : uint
    {
        Unknown = 0x00000000,
        Initialized = 0x00000001,
        // it's used for both method and field to signify that no access is allowed
        NoInvoke = 0x00000002,
        // Set for static ctors, to ensure that the static ctor is run as a static ctor before it is explicitly executed via reflection
        RunClassConstructor = 0x00000004,
        // Set for static ctors and ctors on abstract types, which
        // can be invoked only if the "this" object is provided (even if it's null).
        NoConstructorInvoke = 0x00000008,
        // because field and method are different we can reuse the same bits
        // method
        IsConstructor = 0x00000010,
        /* unused 0x00000020 */
        /* unused 0x00000040 */
        IsDelegateConstructor = 0x00000080,
        ContainsStackPointers = 0x00000100,
        // field
        SpecialField = 0x00000010,
        FieldSpecialCast = 0x00000020,
    }
}
