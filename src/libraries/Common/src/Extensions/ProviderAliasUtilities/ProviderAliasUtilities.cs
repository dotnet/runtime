// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Reflection;

namespace Microsoft.Extensions.Logging
{
    internal static class ProviderAliasUtilities
    {
        private const string AliasAttibuteTypeFullName = "Microsoft.Extensions.Logging.ProviderAliasAttribute";

        internal static string GetAlias(Type providerType)
        {
            foreach (CustomAttributeData attributeData in CustomAttributeData.GetCustomAttributes(providerType))
            {
                if (attributeData.AttributeType.FullName == AliasAttibuteTypeFullName)
                {
                    foreach (CustomAttributeTypedArgument arg in attributeData.ConstructorArguments)
                    {
                        Debug.Assert(arg.ArgumentType == typeof(string));

                        return arg.Value?.ToString();
                    }
                }
            }

            return null;
        }
    }
}
