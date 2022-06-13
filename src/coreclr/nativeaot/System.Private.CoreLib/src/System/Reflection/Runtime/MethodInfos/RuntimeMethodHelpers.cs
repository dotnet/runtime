// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal static class RuntimeMethodHelpers
    {
        //
        // Returns the ParameterInfo objects for the method parameters and return parameter.
        //
        // The ParameterInfo objects will report "contextMethod" as their Member property and use it to get type variable information from
        // the contextMethod's declaring type. The actual metadata, however, comes from "this."
        //
        // The methodTypeArguments provides the fill-ins for any method type variable elements in the parameter type signatures.
        //
        // Does not array-copy.
        //
        internal static RuntimeParameterInfo[] GetRuntimeParameters<TRuntimeMethodCommon>(ref TRuntimeMethodCommon runtimeMethodCommon, MethodBase contextMethod, RuntimeTypeInfo[] methodTypeArguments, out RuntimeParameterInfo returnParameter)
            where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
        {
            TypeContext typeContext = contextMethod.DeclaringType.CastToRuntimeTypeInfo().TypeContext;
            typeContext = new TypeContext(typeContext.GenericTypeArguments, methodTypeArguments);
            QSignatureTypeHandle[] typeSignatures = runtimeMethodCommon.QualifiedMethodSignature;
            int count = typeSignatures.Length;

            VirtualRuntimeParameterInfoArray result = new VirtualRuntimeParameterInfoArray(count);
            runtimeMethodCommon.FillInMetadataDescribedParameters(ref result, typeSignatures, contextMethod, typeContext);

            for (int i = 0; i < count; i++)
            {
                if (result[i] == null)
                {
                    result[i] =
                        RuntimeThinMethodParameterInfo.GetRuntimeThinMethodParameterInfo(
                            contextMethod,
                            i - 1,
                            typeSignatures[i],
                            typeContext);
                }
            }

            returnParameter = result.First;
            return result.Remainder;
        }

        // Compute the ToString() value in a pay-to-play-safe way.
        internal static string ComputeToString<TRuntimeMethodCommon>(ref TRuntimeMethodCommon runtimeMethodCommon, MethodBase contextMethod, RuntimeTypeInfo[] methodTypeArguments)
            where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
        {
            RuntimeParameterInfo returnParameter;
            RuntimeParameterInfo[] parameters = GetRuntimeParameters(ref runtimeMethodCommon, contextMethod, methodTypeArguments, out returnParameter);
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
                string parameterTypeString = parameters[i].ParameterTypeString;

                // Legacy: Why use "ByRef" for by ref parameters? What language is this?
                // VB uses "ByRef" but it should precede (not follow) the parameter name.
                // Why don't we just use "&"?
                if (parameterTypeString.EndsWith("&"))
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
            sb.Append(returnParameter == null ? "Void" : returnParameter.ParameterTypeString);  // ConstructorInfos allowed to pass in null rather than craft a ReturnParameterInfo that's always of type void.
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
                    string? name = methodTypeArgument.InternalNameIfAvailable;
                    if (name == null)
                        name = Type.DefaultTypeNameWhenMissingMetadata;
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
