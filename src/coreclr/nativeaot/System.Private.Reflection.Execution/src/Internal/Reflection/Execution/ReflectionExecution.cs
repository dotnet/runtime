// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//    Internal.Reflection.Execution
//    -------------------------------------------------
//      Why does this exist?:
//        Unlike the desktop, RH uses Internal.Reflection.Core for
//        "classic reflection" emulation as well as LMR, using
//        the Internal.Reflection.Core.Execution contract.
//
//        Internal.Reflection.Core.Execution has an abstract model
//        for an "execution engine" - this contract provides the
//        concrete implementation of this model for Redhawk.
//
//
//      Implemented by:
//        Reflection.Execution.dll on RH
//        N/A on desktop:
//
//      Consumed by:
//        Redhawk app's directly via an under-the-hood ILTransform.
//        System.Private.CoreLib.dll, via a callback (see Internal.System.Runtime.Augment)
//

using global::Internal.Metadata.NativeFormat;
using global::Internal.Reflection.Core;
using global::Internal.Reflection.Core.Execution;
using global::Internal.Runtime.Augments;
using global::System;
using global::System.Collections.Generic;
using global::System.Reflection;
using global::System.Reflection.Runtime.General;

using Debug = System.Diagnostics.Debug;

namespace Internal.Reflection.Execution
{
    public static class ReflectionExecution
    {
        /// <summary>
        /// Eager initialization of runtime reflection support. As part of ExecutionEnvironmentImplementation
        /// initialization it enumerates the modules and registers the ones containing EmbeddedMetadata reflection blobs
        /// in its _moduleToMetadataReader map.
        /// </summary>
        internal static void Initialize()
        {
            // Initialize Reflection.Core's one and only ExecutionDomain.
            var executionEnvironment = new ExecutionEnvironmentImplementation();
            ReflectionCoreExecution.InitializeExecutionDomain(executionEnvironment);

            ExecutionEnvironment = executionEnvironment;
        }

        public static bool TryGetMethodMetadataFromStartAddress(IntPtr methodStartAddress, out MetadataReader reader, out TypeDefinitionHandle typeHandle, out MethodHandle methodHandle)
        {
            reader = null;
            typeHandle = default(TypeDefinitionHandle);
            methodHandle = default(MethodHandle);

            // If ExecutionEnvironment is null, reflection must be disabled.
            if (ExecutionEnvironment == null)
                return false;

            RuntimeTypeHandle declaringTypeHandle = default(RuntimeTypeHandle);
            if (!ExecutionEnvironment.TryGetMethodForStartAddress(methodStartAddress,
                ref declaringTypeHandle, out QMethodDefinition qMethodDefinition))
                return false;

            if (!qMethodDefinition.IsNativeFormatMetadataBased)
                return false;

            QTypeDefinition qTypeDefinition = ExecutionEnvironment.GetMetadataForNamedType(declaringTypeHandle);

            Debug.Assert(qTypeDefinition.IsNativeFormatMetadataBased);
            Debug.Assert(qTypeDefinition.NativeFormatReader == qMethodDefinition.NativeFormatReader);

            reader = qTypeDefinition.NativeFormatReader;
            typeHandle = qTypeDefinition.NativeFormatHandle;
            methodHandle = qMethodDefinition.NativeFormatHandle;

            return true;
        }

        public static MethodBase GetMethodBaseFromStartAddressIfAvailable(IntPtr methodStartAddress)
        {
            RuntimeTypeHandle declaringTypeHandle = default(RuntimeTypeHandle);
            if (!ExecutionEnvironment.TryGetMethodForStartAddress(methodStartAddress,
                ref declaringTypeHandle, out QMethodDefinition qMethodDefinition))
            {
                return null;
            }

            // We don't use the type argument handles as we want the uninstantiated method info
            return ExecutionDomain.GetMethod(declaringTypeHandle, qMethodDefinition, genericMethodTypeArgumentHandles: null);
        }

        internal static ExecutionEnvironmentImplementation ExecutionEnvironment { get; private set; }
    }
}
