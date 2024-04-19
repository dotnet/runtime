// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    internal static unsafe class InteropExtensions
    {
        public static bool IsBlittable(this RuntimeTypeHandle handle)
        {
            //
            // This is used as the approximate implementation of MethodTable::IsBlittable(). It  will err in the direction of declaring
            // things blittable since it is used for argument validation only.
            //
            return !handle.ToMethodTable()->ContainsGCPointers;
        }
    }
}
