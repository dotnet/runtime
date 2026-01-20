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
        }
    }
}
