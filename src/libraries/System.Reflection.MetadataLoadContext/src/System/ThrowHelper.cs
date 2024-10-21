// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This file defines an internal static class used to throw exceptions in the
// the System.Reflection.MetadataLoadContext code.

using System.Reflection;
using System.Reflection.TypeLoading;

namespace System
{
    internal static class ThrowHelper
    {
        internal static AmbiguousMatchException GetAmbiguousMatchException(RoDefinitionType roDefinitionType)
        {
            return new AmbiguousMatchException(SR.Format(SR.Arg_AmbiguousMatchException_RoDefinitionType, roDefinitionType.FullName));
        }

        internal static AmbiguousMatchException GetAmbiguousMatchException(MemberInfo memberInfo)
        {
            Type? declaringType = memberInfo.DeclaringType;
            return new AmbiguousMatchException(SR.Format(SR.Arg_AmbiguousMatchException_MemberInfo, declaringType, memberInfo));
        }
    }
}
