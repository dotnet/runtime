﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

                case IMemberDefinition memberDef:
                    var sb = new StringBuilder();
                    if (memberDef.DeclaringType != null)
                        sb.Append(memberDef.DeclaringType.GetDisplayName()).Append('.');
                    sb.Append(memberDef.Name);
                    return sb.ToString();

                default:
                    Debug.Assert(false, "The display name should not use cecil's signature format.");
                    return member.FullName;
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
