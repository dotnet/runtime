// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Runtime.CompilerServices;

namespace System
{
    internal partial class DefaultBinder : Binder
    {
        // CanChangePrimitive
        // This will determine if the source can be converted to the target type
        private static bool CanChangePrimitive(Type source, Type target) => CanConvertPrimitive((RuntimeType)source, (RuntimeType)target);

        // CanChangePrimitiveObjectToType
        private static bool CanChangePrimitiveObjectToType(object source, Type type) => CanConvertPrimitiveObjectToType(source, (RuntimeType)type);

        // CanConvertPrimitive
        // This will determine if the source can be converted to the target type
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CanConvertPrimitive(RuntimeType source, RuntimeType target);

        // CanConvertPrimitiveObjectToType
        // This method will determine if the primitive object can be converted
        //  to a type.
        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool CanConvertPrimitiveObjectToType(object source, RuntimeType type);
    }
}
