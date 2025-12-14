// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
    public static class MemberReferenceExtensions
    {
        public static string GetDisplayName(this MemberReference member)
        {
            switch (member)
            {
                case TypeReference type:
                    return type.GetDisplayName();

                case MethodReference method:
                    return method.GetDisplayName();

                default:
                    var sb = new StringBuilder();
                    if (member.DeclaringType != null)
                        sb.Append(member.DeclaringType.GetDisplayName()).Append('.');
                    sb.Append(member.Name);
                    return sb.ToString();
            }
        }

        public static string GetNamespaceDisplayName(this MemberReference member)
        {
            var type = member is TypeReference typeReference ? typeReference : member.DeclaringType;
            while (type.DeclaringType != null)
                type = type.DeclaringType;

            return type.Namespace;
        }
    }
}
