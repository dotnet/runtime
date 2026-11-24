// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.JitInterface;
using Internal.Text;
using System.Diagnostics;

namespace ILCompiler
{
    /// <summary>
    /// Root supported hardware intrinsic methods and portable hardware acceleration queries.
    /// </summary>
    public class ReadyToRunHardwareIntrinsicRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            RootVectorAccelerationQuery("System.Numerics"u8, "Vector"u8);
            RootVectorAccelerationQuery("System.Runtime.Intrinsics"u8, "Vector64"u8);
            RootVectorAccelerationQuery("System.Runtime.Intrinsics"u8, "Vector128"u8);
            RootVectorAccelerationQuery("System.Runtime.Intrinsics"u8, "Vector256"u8);
            RootVectorAccelerationQuery("System.Runtime.Intrinsics"u8, "Vector512"u8);

            InstructionSetSupport specifiedInstructionSet = context.InstructionSetSupport;
            TargetArchitecture targetArch = context.Target.Architecture;

            foreach (InstructionSet instructionSet in specifiedInstructionSet.SupportedFlags)
            {
                foreach (MetadataType hardwareIntrinsicType in InstructionSetParser.LookupPlatformIntrinsicTypes(context, instructionSet))
                {
                    foreach (MethodDesc method in hardwareIntrinsicType.GetMethods())
                    {
                        rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, "Supported hardware intrinsic method");
                    }
                }
            }

            void RootVectorAccelerationQuery(Utf8Span typeNamespace, Utf8Span typeName)
            {
                // The interpreter reports no acceleration, so these queries must be compiled even
                // when absent from a partial profile. Trimming may have removed unused queries.
                MethodDesc method = context.SystemModule.GetType(typeNamespace, typeName, throwIfNotFound: false)
                    ?.GetMethod("get_IsHardwareAccelerated"u8, null);
                if (method is not null)
                {
                    rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, "Vector hardware acceleration query");
                }
            }
        }
    }
}
