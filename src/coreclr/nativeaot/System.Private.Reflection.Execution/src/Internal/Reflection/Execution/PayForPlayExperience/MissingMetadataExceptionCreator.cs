// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Text;
using global::System.Reflection;

namespace Internal.Reflection.Execution.PayForPlayExperience
{
    public static class MissingMetadataExceptionCreator
    {
        internal static NotSupportedException Create(Type pertainant)
        {
            return new NotSupportedException(SR.Format(SR.Reflection_InsufficientMetadata_EdbNeeded, pertainant));
        }

        public static string ComputeUsefulPertainantIfPossible(MemberInfo memberInfo)
        {
            {
                StringBuilder friendlyName = new StringBuilder(memberInfo.DeclaringType.ToString());
                friendlyName.Append('.');
                friendlyName.Append(memberInfo.Name);
                if (memberInfo is MethodBase method)
                {
                    bool first;

                    // write out generic parameters
                    if (method.IsConstructedGenericMethod)
                    {
                        first = true;
                        friendlyName.Append('[');
                        foreach (Type genericParameter in method.GetGenericArguments())
                        {
                            if (!first)
                                friendlyName.Append(',');

                            first = false;
                            friendlyName.Append(genericParameter);
                        }
                        friendlyName.Append(']');
                    }

                    // write out actual parameters
                    friendlyName.Append('(');
                    first = true;
                    foreach (ParameterInfo parameter in method.GetParametersNoCopy())
                    {
                        if (!first)
                            friendlyName.Append(',');

                        first = false;
                        friendlyName.Append(parameter.ParameterType);
                    }
                    friendlyName.Append(')');
                }

                return friendlyName.ToString();
            }
        }
    }
}
