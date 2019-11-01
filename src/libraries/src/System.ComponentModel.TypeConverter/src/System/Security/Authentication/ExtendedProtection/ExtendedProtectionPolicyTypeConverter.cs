// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;

namespace System.Security.Authentication.ExtendedProtection
{
    public class ExtendedProtectionPolicyTypeConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                if (value is ExtendedProtectionPolicy policy)
                {
                    Type[] parameterTypes;
                    object[] parameterValues;

                    if (policy.PolicyEnforcement == PolicyEnforcement.Never)
                    {
                        parameterTypes = new Type[] { typeof(PolicyEnforcement) };
                        parameterValues = new object[] { PolicyEnforcement.Never };
                    }
                    else
                    {
                        parameterTypes = new Type[] { typeof(PolicyEnforcement), typeof(ProtectionScenario), typeof(ICollection) };

                        object[] customServiceNames = null;
                        if (policy.CustomServiceNames?.Count > 0)
                        {
                            customServiceNames = new object[policy.CustomServiceNames.Count];
                            ((ICollection)policy.CustomServiceNames).CopyTo(customServiceNames, 0);
                        }

                        parameterValues = new object[] { policy.PolicyEnforcement, policy.ProtectionScenario, customServiceNames };
                    }

                    ConstructorInfo constructor = typeof(ExtendedProtectionPolicy).GetConstructor(parameterTypes);
                    return new InstanceDescriptor(constructor, parameterValues);
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
