// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    [Flags]
    public enum BindingFlags
    {
        // NOTES: We have lookup masks defined in RuntimeType and Activator.  If we
        //    change the lookup values then these masks may need to change also.

        // a place holder for no flag specified
        Default = 0x00,

        // These flags indicate what to search for when binding
        IgnoreCase = 0x01,          // Ignore the case of Names while searching
        DeclaredOnly = 0x02,        // Only look at the members declared on the Type
        Instance = 0x04,            // Include Instance members in search
        Static = 0x08,              // Include Static members in search
        Public = 0x10,              // Include Public members in search
        NonPublic = 0x20,           // Include Non-Public members in search
        FlattenHierarchy = 0x40,    // Rollup the statics into the class.

        // These flags are used by InvokeMember to determine
        // what type of member we are trying to Invoke.
        // BindingAccess = 0xFF00;
        InvokeMethod = 0x0100,
        CreateInstance = 0x0200,
        GetField = 0x0400,
        SetField = 0x0800,
        GetProperty = 0x1000,
        SetProperty = 0x2000,

        // These flags are also used by InvokeMember but they should only
        // be used when calling InvokeMember on a COM object.
        PutDispProperty = 0x4000,
        PutRefDispProperty = 0x8000,

        ExactBinding = 0x010000,    // Bind with Exact Type matching, No Change type
        SuppressChangeType = 0x020000,

        // DefaultValueBinding will return the set of methods having ArgCount or
        //    more parameters.  This is used for default values, etc.
        OptionalParamBinding = 0x040000,

        // These are a couple of misc attributes used
        IgnoreReturn = 0x01000000,  // This is used in COM Interop
        DoNotWrapExceptions = 0x02000000, // Disables wrapping exceptions in TargetInvocationException
    }
}
