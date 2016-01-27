// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited=false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class CustomConstantAttribute : Attribute
    {
        public abstract Object Value { get; }

        internal static object GetRawConstant(CustomAttributeData attr)
        {
            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                    return namedArgument.TypedValue.Value;
            }

            // Return DBNull to indicate that no default value is available.
            // Not to be confused with a null return which indicates a null default value.
            return DBNull.Value;
        }
    }
}

