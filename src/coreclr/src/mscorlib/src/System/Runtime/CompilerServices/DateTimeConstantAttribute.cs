// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Diagnostics.Contracts;

namespace System.Runtime.CompilerServices
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, Inherited=false)]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class DateTimeConstantAttribute : CustomConstantAttribute
    {
        public DateTimeConstantAttribute(long ticks)
        {
            date = new System.DateTime(ticks);
        }

        public override Object Value
        {
            get
            {
                return date;
            }
        }

        internal static DateTime GetRawDateTimeConstant(CustomAttributeData attr)
        {
            Contract.Requires(attr.Constructor.DeclaringType == typeof(DateTimeConstantAttribute));
            Contract.Requires(attr.ConstructorArguments.Count == 1);

            foreach (CustomAttributeNamedArgument namedArgument in attr.NamedArguments)
            {
                if (namedArgument.MemberInfo.Name.Equals("Value"))
                {
                    return new DateTime((long)namedArgument.TypedValue.Value);
                }
            }

            // Look at the ctor argument if the "Value" property was not explicitly defined.
            return new DateTime((long)attr.ConstructorArguments[0].Value);
        }

        private System.DateTime date;
    }
}

