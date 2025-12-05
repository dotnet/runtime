// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.JitInterface;
using System.Diagnostics;

namespace ILCompiler
{
    /// <summary>
    /// Root all methods on supported hardware intrinsic classes.
    /// </summary>
    public class ReadyToRunHardwareIntrinsicRootProvider(ReadyToRunCompilerContext context) : ICompilationRootProvider
    {
        public void AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            InstructionSetSupport specifiedInstructionSet = context.InstructionSetSupport;
            TargetArchitecture targetArch = context.Target.Architecture;

            // Hardware intrinsics can only live in the system module.
            foreach (MetadataType type in context.SystemModule.GetAllTypes())
            {
                InstructionSet instructionSet = InstructionSetParser.LookupPlatformIntrinsicInstructionSet(targetArch, type);

                if (instructionSet == InstructionSet.ILLEGAL)
                {
                    // Not a HardwareIntrinsics type for our platform.
                    continue;
                }

                if (specifiedInstructionSet.IsInstructionSetSupported(instructionSet))
                {
                    foreach (MethodDesc method in type.GetMethods())
                    {
                        rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, "Hardware intrinsic method fallback implementation");
                    }
                }
                else
                {
                    MethodDesc isSupportedMethod = type.GetMethod("get_IsSupported"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.Boolean), []));
                    if (isSupportedMethod is not null)
                    {
                        rootProvider.AddCompilationRoot(isSupportedMethod, rootMinimalDependencies: false, "IsSupported getter for unsupported hardware intrinsic");
                    }
                }
            }
        }
    }
}
