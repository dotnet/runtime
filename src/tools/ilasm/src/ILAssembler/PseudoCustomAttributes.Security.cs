// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    private const string DynamicSecurityMethodAttribute = "System.Security.DynamicSecurityMethodAttribute";
    private const string SuppressUnmanagedCodeSecurityAttribute = "System.Security.SuppressUnmanagedCodeSecurityAttribute";

    private static bool IsSecurityAttribute(string @namespace, string name) =>
        @namespace == "System.Security"
        && name is "DynamicSecurityMethodAttribute" or "SuppressUnmanagedCodeSecurityAttribute";

    /// <summary>
    /// Handles the two attributes that the native emitter recognizes by name outside the known
    /// attribute table.
    /// </summary>
    private static bool ApplySecurityAttribute(LoweringContext context, string @namespace, string name, out bool keep)
    {
        keep = true;
        string fullName = @namespace.Length == 0 ? name : @namespace + "." + name;

        if (fullName == DynamicSecurityMethodAttribute)
        {
            if (context.Owner is not EntityRegistry.MethodDefinitionEntity method)
            {
                return false;
            }

            method.MethodAttributes |= MethodAttributes.RequireSecObject;
            keep = false;
            return true;
        }

        if (fullName == SuppressUnmanagedCodeSecurityAttribute)
        {
            switch (context.Owner)
            {
                case EntityRegistry.TypeDefinitionEntity type:
                    type.Attributes |= TypeAttributes.HasSecurity;
                    return true;
                case EntityRegistry.MethodDefinitionEntity method:
                    method.MethodAttributes |= MethodAttributes.HasSecurity;
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }
}
