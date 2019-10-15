// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace System
{
    public static class TypeExtensions
    {
        public static MemberInfo GetSingleMember(this Type type, string name)
        {
            Assert.NotNull(type);
            Assert.NotNull(name);

            return type.GetMember(name).Single();
        }
    }
}
