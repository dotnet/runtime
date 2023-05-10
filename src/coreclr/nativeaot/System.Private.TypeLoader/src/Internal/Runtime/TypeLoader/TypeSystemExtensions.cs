// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;

using Internal.Runtime.Augments;
using Internal.TypeSystem;

namespace Internal.TypeSystem
{
    internal static class TypeDescExtensions
    {
        public static bool CanShareNormalGenericCode(this TypeDesc type)
        {
            return (type != type.ConvertToCanonForm(CanonicalFormKind.Specific));
        }

        public static bool IsGeneric(this TypeDesc type)
        {
            DefType typeAsDefType = type as DefType;
            return typeAsDefType != null && typeAsDefType.HasInstantiation;
        }

        public static bool IsWellKnownType(this TypeDesc type, WellKnownType wellKnownType)
        {
            return type == type.Context.GetWellKnownType(wellKnownType, false);
        }

        public static ByRefType MakeByRefType(this TypeDesc type)
        {
            return type.Context.GetByRefType(type);
        }

        public static TypeDesc GetParameterType(this TypeDesc type)
        {
            ParameterizedType paramType = (ParameterizedType)type;
            return paramType.ParameterType;
        }
    }

    internal static class MethodDescExtensions
    {
        public static bool CanShareNormalGenericCode(this InstantiatedMethod method)
        {
            return (method != method.GetCanonMethodTarget(CanonicalFormKind.Specific));
        }
    }

    internal static class RuntimeHandleExtensions
    {
        public static bool IsNull(this RuntimeTypeHandle rtth)
            => RuntimeTypeHandle.ToIntPtr(rtth) == 0;

        public static unsafe bool IsDynamic(this RuntimeFieldHandle rtfh)
            => (RuntimeFieldHandle.ToIntPtr(rtfh) & 1) != 0;

        public static unsafe bool IsDynamic(this RuntimeMethodHandle rtmh)
            => (RuntimeMethodHandle.ToIntPtr(rtmh) & 1) != 0;
    }
}
