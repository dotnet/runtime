// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    //
    // Invocation cached flags. Those are used in unmanaged code as well
    // so be careful if you change them
    //
    [Flags]
    internal enum INVOCATION_FLAGS : uint
    {
        INVOCATION_FLAGS_UNKNOWN = 0x00000000,
        INVOCATION_FLAGS_INITIALIZED = 0x00000001,
        // it's used for both method and field to signify that no access is allowed
        INVOCATION_FLAGS_NO_INVOKE = 0x00000002,
        // Set for static ctors, to ensure that the static ctor is run as a static ctor before it is explicitly executed via reflection
        INVOCATION_FLAGS_RUN_CLASS_CONSTRUCTOR = 0x00000004,
        // Set for static ctors and ctors on abstract types, which
        // can be invoked only if the "this" object is provided (even if it's null).
        INVOCATION_FLAGS_NO_CTOR_INVOKE = 0x00000008,
        // because field and method are different we can reuse the same bits
        // method
        INVOCATION_FLAGS_IS_CTOR = 0x00000010,
        /* unused 0x00000020 */
        /* unused 0x00000040 */
        INVOCATION_FLAGS_IS_DELEGATE_CTOR = 0x00000080,
        INVOCATION_FLAGS_CONTAINS_STACK_POINTERS = 0x00000100,
        // field
        INVOCATION_FLAGS_SPECIAL_FIELD = 0x00000010,
        INVOCATION_FLAGS_FIELD_SPECIAL_CAST = 0x00000020,

        // temporary flag used for flagging invocation of method vs ctor
        // this flag never appears on the instance m_invocationFlag and is simply
        // passed down from within ConstructorInfo.Invoke()
        INVOCATION_FLAGS_CONSTRUCTOR_INVOKE = 0x10000000,
    }
}
