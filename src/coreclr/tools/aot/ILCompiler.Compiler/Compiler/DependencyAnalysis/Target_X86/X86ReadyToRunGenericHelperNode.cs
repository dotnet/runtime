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

            switch (_id)
            {
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
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
                        MetadataType target = (MetadataType)_target;

                        // Look up the index cell
                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg1, _lookupSignature, relocsOnly);

                        ISymbolNode helperEntrypoint;
                        if (TriggersLazyStaticConstructor(factory))
                        {
                            // There is a lazy class constructor. We need the non-GC static base because that's where the
                            // class constructor context lives.
                            GenericLookupResult nonGcRegionLookup = factory.GenericLookup.TypeNonGCStaticBase(target);
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, nonGcRegionLookup, relocsOnly);
                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            AddrMode loadCctor = new AddrMode(encoder.TargetRegister.Result, null, -cctorContextSize, 0, AddrModeSize.Int32);
                            encoder.EmitLEA(encoder.TargetRegister.Result, ref loadCctor);

                            AddrMode storeAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                            encoder.EmitStackDup();
                            encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);

                            helperEntrypoint = factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase);
                        }
                        else
                        {
                            helperEntrypoint = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                        }

                        // First arg: address of the TypeManager slot that provides the helper with
                        // information about module index and the type manager instance (which is used
                        // for initialization on first access).
                        AddrMode loadFromArg1 = new AddrMode(encoder.TargetRegister.Arg1, null, 0, 0, AddrModeSize.Int32);
                        encoder.EmitMOV(encoder.TargetRegister.Arg0, ref loadFromArg1);

                        // Second arg: index of the type in the ThreadStatic section of the modules
                        AddrMode loadFromArg1AndDelta = new AddrMode(encoder.TargetRegister.Arg1, null, factory.Target.PointerSize, 0, AddrModeSize.Int32);
                        encoder.EmitMOV(encoder.TargetRegister.Arg1, ref loadFromArg1AndDelta);

                        encoder.EmitJMP(helperEntrypoint);
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        // This is a weird helper. Codegen populated Arg0 and Arg1 with the values that the constructor
                        // method expects. Codegen also passed us the generic context on stack.
                        // We now need to load the delegate target method on the stack (using a dictionary lookup)
                        // and the optional 4th parameter, and call the ctor.

                        var target = (DelegateCreationInfo)_target;

                        // EmitLoadGenericContext loaded the context from stack into Result
                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Result, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);

                        AddrMode storeAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            AddrMode storeAtEspPlus8 = new AddrMode(Register.ESP, null, 8, 0, AddrModeSize.Int32);
                            encoder.EmitStackDup();
                            encoder.EmitMOV(ref storeAtEspPlus8, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Result, target.Thunk);
                            encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
                            encoder.EmitMOV(ref storeAtEspPlus4, encoder.TargetRegister.Result);
                        }

                        encoder.EmitJMP(target.Constructor);
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
                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);
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
            if (Id == ReadyToRunHelperId.DelegateCtor)
            {
                AddrMode loadAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                encoder.EmitMOV(encoder.TargetRegister.Result, ref loadAtEspPlus4);
            }
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(NodeFactory factory, ref X86Emitter encoder, bool relocsOnly)
        {
            // We start with context register pointing to the MethodTable
            Register contextRegister = encoder.TargetRegister.Arg0;

            // Locate the VTable slot that points to the dictionary
            int vtableSlot = 0;
            if (!relocsOnly)
            {
                // The concrete slot won't be known until we're emitting data - don't ask for it in relocsOnly.
                vtableSlot = VirtualMethodSlotHelper.GetGenericDictionarySlot(factory, (TypeDesc)_dictionaryOwner);
            }

            int pointerSize = factory.Target.PointerSize;
            int slotOffset = EETypeNode.GetVTableOffset(pointerSize) + (vtableSlot * pointerSize);

            // DelegateCtor is special, the context is on stack
            if (Id == ReadyToRunHelperId.DelegateCtor)
            {
                AddrMode loadAtEspPlus4 = new AddrMode(Register.ESP, null, 4, 0, AddrModeSize.Int32);
                encoder.EmitMOV(encoder.TargetRegister.Result, ref loadAtEspPlus4);
                contextRegister = encoder.TargetRegister.Result;
            }

            // Load the dictionary pointer from the VTable
            AddrMode loadDictionary = new AddrMode(contextRegister, null, slotOffset, 0, AddrModeSize.Int32);
            encoder.EmitMOV(contextRegister, ref loadDictionary);
        }
    }
}
