// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Text;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal static class RuntimeMethodHelpers
    {
        // Compute the ToString() value in a pay-to-play-safe way.
        internal static string ComputeToString<TRuntimeMethodCommon>(ref TRuntimeMethodCommon runtimeMethodCommon, MethodBase contextMethod, RuntimeTypeInfo[] methodTypeArguments)
            where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
        {
            RuntimeParameterInfo returnParameter = RuntimeParameterInfo.GetReturnParameter(ref runtimeMethodCommon, contextMethod, methodTypeArguments);
            RuntimeParameterInfo[] parameters = RuntimeParameterInfo.GetParameters(ref runtimeMethodCommon, contextMethod, methodTypeArguments);
            return ComputeToString(contextMethod, methodTypeArguments, parameters, returnParameter);
        }

        // Used by method and property ToString() methods to display the list of parameter types. Replicates the behavior of MethodBase.ConstructParameters()
        // but in a pay-to-play-safe way.
        internal static string ComputeParametersString(RuntimeParameterInfo[] parameters)
        {
            StringBuilder sb = new StringBuilder(30);
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                string parameterTypeString = parameters[i].ParameterType.FormatTypeName();

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (parameterTypeString.EndsWith('&'))
                {
                    sb.Append(parameterTypeString, 0, parameterTypeString.Length - 1);
                    sb.Append(" ByRef");
                }
                else
                {
                    sb.Append(parameterTypeString);
                }
            }
            return sb.ToString();
        }

        internal static string ComputeToString(MethodBase contextMethod, RuntimeTypeInfo[] methodTypeArguments, RuntimeParameterInfo[] parameters, RuntimeParameterInfo returnParameter)
        {
            StringBuilder sb = new StringBuilder(30);
            sb.Append(returnParameter == null ? "Void" : returnParameter.ParameterType.FormatTypeName());  // ConstructorInfos allowed to pass in null rather than craft a ReturnParameterInfo that's always of type void.
            sb.Append(' ');
            sb.Append(contextMethod.Name);
            if (methodTypeArguments.Length != 0)
            {
                string sep = "";
                sb.Append('[');
                foreach (RuntimeTypeInfo methodTypeArgument in methodTypeArguments)
                {
                    sb.Append(sep);
                    sep = ",";
                    sb.Append(methodTypeArgument.Name);
                }
                sb.Append(']');
            }
            sb.Append('(');
            sb.Append(ComputeParametersString(parameters));
            sb.Append(')');

            return sb.ToString();
        }
    }
}
