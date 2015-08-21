// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Microsoft.Internal
{
    internal static class ReflectionServices
    {
        public static bool IsMemberInfoForType(this MemberInfo memberInfo)
        {
            return memberInfo is TypeInfo;
        }

        public static bool IsMemberInfoForProperty(this MemberInfo memberInfo)
        {
            return memberInfo is PropertyInfo;
        }

        public static bool IsMemberInfoForConstructor(this MemberInfo memberInfo)
        {
            return memberInfo is ConstructorInfo;
        }

        public static bool IsMemberInfoForMethod(this MemberInfo memberInfo)
        {
            return memberInfo is MethodInfo;
        }
    }
}
