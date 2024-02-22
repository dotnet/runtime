// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.X86;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunGenericHelperNode
    {
        protected Register GetContextRegister(ref /* readonly */ X86Emitter encoder)
        {
            return encoder.TargetRegister.Arg0;
        }

        protected void EmitDictionaryLookup(NodeFactory factory, ref X86Emitter encoder, Register context, Register result, GenericLookupResult lookup, bool relocsOnly)
        {
            // INVARIANT: must not trash context register

            // Find the generic dictionary slot
            int dictionarySlot = 0;
            if (!relocsOnly)
            {
                // The concrete slot won't be known until we're emitting data - don't ask for it in relocsOnly.
                if (!factory.GenericDictionaryLayout(_dictionaryOwner).TryGetSlotForEntry(lookup, out dictionarySlot))
                {
                    encoder.EmitZeroReg(result);
                    return;
                }
            }

            // Load the generic dictionary cell
            AddrMode loadEntry = new AddrMode(
                context, null, dictionarySlot * factory.Target.PointerSize, 0, AddrModeSize.Int32);
            encoder.EmitMOV(result, ref loadEntry);

            // If there's any invalid entries, we need to test for them
            //
            // Skip this in relocsOnly to make it easier to weed out bugs - the _hasInvalidEntries
            // flag can change over the course of compilation and the bad slot helper dependency
            // should be reported by someone else - the system should not rely on it coming from here.
            if (!relocsOnly && _hasInvalidEntries)
            {
                AddrMode resultAddr = new AddrMode(Register.RegDirect | result, null, 0, 0, AddrModeSize.Int32);
                encoder.EmitCMP(ref resultAddr, 0);
                encoder.EmitJE(GetBadSlotHelper(factory));
            }
        }

        protected sealed override void EmitCode(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            // First load the generic context into the context register.
            EmitLoadGenericContext(factory, ref encoder, relocsOnly);

            Register contextRegister = GetContextRegister(ref encoder);

            switch (_id)
            {
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg0);

                        if (!TriggersLazyStaticConstructor(factory))
                        {
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);
                            encoder.EmitRET();
                        }
                        else
                        {
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg0, _lookupSignature, relocsOnly);
                            encoder.EmitMOV(encoder.TargetRegister.Result, encoder.TargetRegister.Arg0);

                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, -cctorContextSize, 0, AddrModeSize.Int32);
                            encoder.EmitCMP(ref initialized, 0);
                            encoder.EmitRETIfEqual();

                            AddrMode loadCctor = new AddrMode(encoder.TargetRegister.Arg0, null, -cctorContextSize, 0, AddrModeSize.Int32);
                            encoder.EmitLEA(encoder.TargetRegister.Arg0, ref loadCctor);
                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg0);

                        MetadataType target = (MetadataType)_target;

                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);

                        AddrMode loadFromResult = new AddrMode(encoder.TargetRegister.Result, null, 0, 0, AddrModeSize.Int32);
                        encoder.EmitMOV(encoder.TargetRegister.Result, ref loadFromResult);

                        if (!TriggersLazyStaticConstructor(factory))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            GenericLookupResult nonGcRegionLookup = factory.GenericLookup.TypeNonGCStaticBase(target);
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg0, nonGcRegionLookup, relocsOnly);

                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            AddrMode initialized = new AddrMode(encoder.TargetRegister.Arg0, null, -cctorContextSize, 0, AddrModeSize.Int32);
                            encoder.EmitCMP(ref initialized, 0);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            AddrMode loadCctor = new AddrMode(encoder.TargetRegister.Arg0, null, -cctorContextSize, 0, AddrModeSize.Int32);
                            encoder.EmitLEA(encoder.TargetRegister.Arg0, ref loadCctor);

                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        encoder.EmitINT3();
                    }
                    break;

                // These are all simple: just get the thing from the dictionary and we're done
                case ReadyToRunHelperId.TypeHandle:
                case ReadyToRunHelperId.MethodHandle:
                case ReadyToRunHelperId.FieldHandle:
                case ReadyToRunHelperId.MethodDictionary:
                case ReadyToRunHelperId.MethodEntry:
                case ReadyToRunHelperId.VirtualDispatchCell:
                case ReadyToRunHelperId.DefaultConstructor:
                case ReadyToRunHelperId.ObjectAllocator:
                case ReadyToRunHelperId.TypeHandleForCasting:
                case ReadyToRunHelperId.ConstrainedDirectCall:
                    {
                        EmitDictionaryLookup(factory, ref encoder, contextRegister, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);
                        encoder.EmitRET();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void EmitLoadGenericContext(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            // Assume generic context is already loaded in the context register.
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            // We start with context register pointing to the MethodTable
            Register contextRegister = GetContextRegister(ref encoder);

            // Locate the VTable slot that points to the dictionary
            int vtableSlot = 0;
            if (!relocsOnly)
            {
                // The concrete slot won't be known until we're emitting data - don't ask for it in relocsOnly.
                vtableSlot = VirtualMethodSlotHelper.GetGenericDictionarySlot(factory, (TypeDesc)_dictionaryOwner);
            }

            int pointerSize = factory.Target.PointerSize;
            int slotOffset = EETypeNode.GetVTableOffset(pointerSize) + (vtableSlot * pointerSize);

            // Load the dictionary pointer from the VTable
            AddrMode loadDictionary = new AddrMode(contextRegister, null, slotOffset, 0, AddrModeSize.Int32);
            encoder.EmitMOV(contextRegister, ref loadDictionary);
        }
    }
}
