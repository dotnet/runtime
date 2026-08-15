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
                        // A generic method has no code of its own to compile - rooting the typical
                        // definition would queue a method whose signature and body still refer to
                        // its own type variables. Instantiations that are actually used get rooted
                        // through the callers that use them.
                        if (method.HasInstantiation)
                        {
                            continue;
                        }

                        rootProvider.AddCompilationRoot(method, rootMinimalDependencies: false, "Supported hardware intrinsic method");
                    }
                }
            }
        }
    }
}
