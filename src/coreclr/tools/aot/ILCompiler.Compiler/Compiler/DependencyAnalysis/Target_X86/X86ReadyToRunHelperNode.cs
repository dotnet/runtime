// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.X86;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// X86 specific portions of ReadyToRunHelperNode
    /// </summary>
    public partial class ReadyToRunHelperNode
    {
        protected override void EmitCode(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            switch (Id)
            {
                case ReadyToRunHelperId.VirtualCall:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        MetadataType target = (MetadataType)Target;
                        bool hasLazyStaticConstructor = factory.PreinitializationManager.HasLazyStaticConstructor(target);
                        encoder.EmitMOV(encoder.TargetRegister.Result, factory.TypeNonGCStaticsSymbol(target));

                        if (!hasLazyStaticConstructor)
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, factory.TypeNonGCStaticsSymbol(target), -NonGCStaticsNode.GetClassConstructorContextSize(factory.Target));

                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, factory.Target.PointerSize, 0, AddrModeSize.Int32);
                            encoder.EmitCMP(ref initialized, 1);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                case ReadyToRunHelperId.ResolveVirtualFunction:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
