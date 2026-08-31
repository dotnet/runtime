// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.TypeInfos;
using System.Text;

using Internal.Reflection.Core;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    /// <summary>
    /// These api's are to be implemented by parsing metadata.
    /// </summary>
    /// <typeparam name="TRuntimeMethodCommon"></typeparam>
    internal interface IRuntimeMethodCommon<TRuntimeMethodCommon> where TRuntimeMethodCommon : IRuntimeMethodCommon<TRuntimeMethodCommon>, IEquatable<TRuntimeMethodCommon>
    {
        MethodAttributes Attributes { get; }
        CallingConventions CallingConvention { get; }

        RuntimeTypeInfo ContextTypeInfo { get; }
        RuntimeTypeInfo DeclaringType { get; }
        RuntimeNamedTypeInfo DefiningTypeInfo { get; }
        MethodImplAttributes MethodImplementationFlags { get; }
        Module Module { get; }

        /// <summary>
        /// Return an array of the types of the return value and parameter types.
        /// </summary>
        QSignatureTypeHandle[] QualifiedMethodSignature { get; }
        IEnumerable<CustomAttributeData> TrueCustomAttributes { get; }

        /// <summary>
        /// Parse the metadata that describes parameters, and for each parameter for which there is specific metadata
        /// construct a RuntimeParameterInfo and fill in the VirtualRuntimeParameterInfoArray. Do remember to use contextMethod
        /// instead of using the one internal to the RuntimeMethodCommon, as the runtime may pass in a subtly different context.
        /// </summary>
        void FillInMetadataDescribedParameters(ref VirtualRuntimeParameterInfoArray result, QSignatureTypeHandle[] parameterTypes, MethodBase contextMethod, TypeContext typeContext);

        string Name { get; }

        MethodBaseInvoker GetUncachedMethodInvoker(RuntimeTypeInfo[] methodArguments, MemberInfo exceptionPertainant, out Exception exception);

        bool IsGenericMethodDefinition { get; }
        int GenericParameterCount { get; }

        bool HasSameMetadataDefinitionAs(TRuntimeMethodCommon other);

        TRuntimeMethodCommon RuntimeMethodCommonOfUninstantiatedMethod { get; }

        RuntimeTypeInfo[] GetGenericTypeParametersWithSpecifiedOwningMethod(RuntimeNamedMethodInfo<TRuntimeMethodCommon> owningMethod);

        int MetadataToken { get; }

        /// <summary>
        /// Retrieves the RuntimeMethodHandle for the given method. Non-null generic args should only be passed for instantiated
        /// generic methods.
        /// </summary>
        RuntimeMethodHandle GetRuntimeMethodHandle(Type[] genericArgs);
    }
}
