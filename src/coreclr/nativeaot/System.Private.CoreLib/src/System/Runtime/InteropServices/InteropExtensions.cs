// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    /// <summary>
    ///     Hooks for System.Private.Interop.dll code to access internal functionality in System.Private.CoreLib.dll.
    ///
    ///     Methods added to InteropExtensions should also be added to the System.Private.CoreLib.InteropServices contract
    ///     in order to be accessible from System.Private.Interop.dll.
    /// </summary>
    [CLSCompliant(false)]
    [ReflectionBlocked]
    public static class InteropExtensions
    {
        internal static bool MightBeBlittable(this EETypePtr eeType)
        {
            //
            // This is used as the approximate implementation of MethodTable::IsBlittable(). It  will err in the direction of declaring
            // things blittable since it is used for argument validation only.
            //
            return !eeType.ContainsGCPointers;
        }

        public static bool IsBlittable(this RuntimeTypeHandle handle)
        {
            return handle.ToEETypePtr().MightBeBlittable();
        }

        public static bool IsBlittable(this object obj)
        {
            return obj.GetEETypePtr().MightBeBlittable();
        }

        public static bool IsGenericType(this RuntimeTypeHandle handle)
        {
            EETypePtr eeType = handle.ToEETypePtr();
            return eeType.IsGeneric;
        }

        public static bool IsGenericTypeDefinition(this RuntimeTypeHandle handle)
        {
            EETypePtr eeType = handle.ToEETypePtr();
            return eeType.IsGenericTypeDefinition;
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
            return (int)handle.ToEETypePtr().ValueTypeSize;
        }

        public static bool IsValueType(this RuntimeTypeHandle handle)
        {
            return handle.ToEETypePtr().IsValueType;
        }

        public static bool IsEnum(this RuntimeTypeHandle handle)
        {
            return handle.ToEETypePtr().IsEnum;
        }

        public static bool IsInterface(this RuntimeTypeHandle handle)
        {
            return handle.ToEETypePtr().IsInterface;
        }

        public static bool AreTypesAssignable(RuntimeTypeHandle sourceType, RuntimeTypeHandle targetType)
        {
            return RuntimeImports.AreTypesAssignable(sourceType.ToEETypePtr(), targetType.ToEETypePtr());
        }

        public static RuntimeTypeHandle GetTypeHandle(this object target)
        {
            return new RuntimeTypeHandle(target.GetEETypePtr());
        }
    }
}
