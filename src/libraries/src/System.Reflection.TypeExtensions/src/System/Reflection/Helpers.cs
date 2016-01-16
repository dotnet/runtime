// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection
{
    internal static class Helpers
    {
        internal const BindingFlags DefaultLookup = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        internal static readonly MethodInfo[] EmptyMethodArray = new MethodInfo[0];
        internal static readonly Type[] EmptyTypeArray = new Type[0];
        internal static readonly MemberInfo[] EmptyMemberArray = new MemberInfo[0];

        internal static MethodInfo FilterAccessor(MethodInfo accessor, bool nonPublic)
        {
            if (nonPublic || (accessor != null && accessor.IsPublic))
            {
                return accessor;
            }

            return null;
        }
    }
}
