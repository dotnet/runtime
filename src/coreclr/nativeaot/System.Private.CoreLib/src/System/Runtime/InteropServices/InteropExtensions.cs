// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    ///     Hooks for interop code to access internal functionality in System.Private.CoreLib.dll.
    /// </summary>
    internal static unsafe class InteropExtensions
    {
        internal static bool MightBeBlittable(this RuntimeTypeHandle handle)
        {
            //
            // This is used as the approximate implementation of MethodTable::IsBlittable(). It  will err in the direction of declaring
            // things blittable since it is used for argument validation only.
            //
            return !handle.ToMethodTable()->ContainsGCPointers;
        }

        public static bool IsBlittable(this RuntimeTypeHandle handle)
        {
            return handle.MightBeBlittable();
        }

        public static bool IsGenericType(this RuntimeTypeHandle handle)
        {
            return handle.ToMethodTable()->IsGeneric;
        }

        public static bool IsGenericTypeDefinition(this RuntimeTypeHandle handle)
        {
            return handle.ToMethodTable()->IsGenericTypeDefinition;
        }

        //
        // Returns the raw function pointer for a open static delegate - if the function has a jump stub
        // it returns the jump target. Therefore the function pointer returned
        // by two delegates may NOT be unique
        //
        public static IntPtr GetRawFunctionPointerForOpenStaticDelegate(this Delegate del)
        {
            //If it is not open static then return IntPtr.Zero
            if (!del.IsOpenStatic)
                return IntPtr.Zero;

            IntPtr funcPtr = del.GetFunctionPointer(out RuntimeTypeHandle _, out bool _, out bool _);
            return funcPtr;
        }

        public static int GetValueTypeSize(this RuntimeTypeHandle handle)
        {
            return (int)handle.ToMethodTable()->ValueTypeSize;
        }

        public static bool IsValueType(this RuntimeTypeHandle handle)
        {
            return handle.ToMethodTable()->IsValueType;
        }

        public static bool IsEnum(this RuntimeTypeHandle handle)
        {
            return handle.ToMethodTable()->IsEnum;
        }

        public static bool IsInterface(this RuntimeTypeHandle handle)
        {
            return handle.ToMethodTable()->IsInterface;
        }

        public static bool AreTypesAssignable(RuntimeTypeHandle sourceType, RuntimeTypeHandle targetType)
        {
            return RuntimeImports.AreTypesAssignable(sourceType.ToMethodTable(), targetType.ToMethodTable());
        }

        public static RuntimeTypeHandle GetTypeHandle(this object target)
        {
            return new RuntimeTypeHandle(target.GetMethodTable());
        }
    }
}
