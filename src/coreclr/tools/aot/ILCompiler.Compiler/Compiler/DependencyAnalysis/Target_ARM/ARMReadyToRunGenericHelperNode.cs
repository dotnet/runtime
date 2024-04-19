// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using ILCompiler.DependencyAnalysis.ARM;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    public partial class ReadyToRunGenericHelperNode
    {
        protected Register GetContextRegister(ref /* readonly */ ARMEmitter encoder)
        {
            if (_id == ReadyToRunHelperId.DelegateCtor)
                return encoder.TargetRegister.Arg2;
            else
                return encoder.TargetRegister.Arg0;
        }

        protected void EmitDictionaryLookup(NodeFactory factory, ref ARMEmitter encoder, Register context, Register result, GenericLookupResult lookup, bool relocsOnly)
        {
            // INVARIANT: must not trash context register

            // Find the generic dictionary slot
            int dictionarySlot = 0;
            if (!relocsOnly)
            {
                // The concrete slot won't be known until we're emitting data - don't ask for it in relocsOnly.
                if (!factory.GenericDictionaryLayout(_dictionaryOwner).TryGetSlotForEntry(lookup, out dictionarySlot))
                {
                    encoder.EmitMOV(result, 0);
                    return;
                }
            }

            // Load the generic dictionary cell
            encoder.EmitLDR(result, context, dictionarySlot * factory.Target.PointerSize);

            // If there's any invalid entries, we need to test for them
            //
            // Skip this in relocsOnly to make it easier to weed out bugs - the _hasInvalidEntries
            // flag can change over the course of compilation and the bad slot helper dependency
            // should be reported by someone else - the system should not rely on it coming from here.
            if (!relocsOnly && _hasInvalidEntries)
            {
                encoder.EmitCMP(result, 0);
                encoder.EmitRETIfNotEqual();
                encoder.EmitJMP(GetBadSlotHelper(factory));
            }
        }

        protected sealed override void EmitCode(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
        {
            // First load the generic context into the context register.
            EmitLoadGenericContext(factory, ref encoder, relocsOnly);

            Register contextRegister = GetContextRegister(ref encoder);

            switch (_id)
            {
                case ReadyToRunHelperId.GetNonGCStaticBase:
                    {
                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg0);

                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);

                        if (!TriggersLazyStaticConstructor(factory))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            encoder.EmitLDR(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg0, (short)-cctorContextSize);
                            encoder.EmitCMP(encoder.TargetRegister.Arg1, 0);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitSUB(encoder.TargetRegister.Arg0, (byte)cctorContextSize);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnNonGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetGCStaticBase:
                    {
                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg0);

                        encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg0);
                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);
                        encoder.EmitLDR(encoder.TargetRegister.Result, encoder.TargetRegister.Result);

                        MetadataType target = (MetadataType)_target;
                        if (!TriggersLazyStaticConstructor(factory))
                        {
                            encoder.EmitRET();
                        }
                        else
                        {
                            // We need to trigger the cctor before returning the base. It is stored at the beginning of the non-GC statics region.
                            GenericLookupResult nonGcRegionLookup = factory.GenericLookup.TypeNonGCStaticBase(target);
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg2, nonGcRegionLookup, relocsOnly);

                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            encoder.EmitLDR(encoder.TargetRegister.Arg3, encoder.TargetRegister.Arg2, ((short)(factory.Target.PointerSize - cctorContextSize)));
                            encoder.EmitCMP(encoder.TargetRegister.Arg3, 1);
                            encoder.EmitRETIfEqual();

                            encoder.EmitMOV(encoder.TargetRegister.Arg1, encoder.TargetRegister.Result);
                            encoder.EmitMOV(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg2);
                            encoder.EmitSUB(encoder.TargetRegister.Arg0, cctorContextSize);
                            encoder.EmitJMP(factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnGCStaticBase));
                        }
                    }
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    {
                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg0);

                        MetadataType target = (MetadataType)_target;

                        // Look up the index cell
                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg1, _lookupSignature, relocsOnly);

                        ISymbolNode helperEntrypoint;
                        if (TriggersLazyStaticConstructor(factory))
                        {
                            // There is a lazy class constructor. We need the non-GC static base because that's where the
                            // class constructor context lives.
                            GenericLookupResult nonGcRegionLookup = factory.GenericLookup.TypeNonGCStaticBase(target);
                            EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg2, nonGcRegionLookup, relocsOnly);
                            int cctorContextSize = NonGCStaticsNode.GetClassConstructorContextSize(factory.Target);
                            encoder.EmitSUB(encoder.TargetRegister.Arg2, cctorContextSize);

                            helperEntrypoint = factory.HelperEntrypoint(HelperEntrypoint.EnsureClassConstructorRunAndReturnThreadStaticBase);
                        }
                        else
                        {
                            helperEntrypoint = factory.HelperEntrypoint(HelperEntrypoint.GetThreadStaticBaseForType);
                        }

                        // First arg: address of the TypeManager slot that provides the helper with
                        // information about module index and the type manager instance (which is used
                        // for initialization on first access).
                        encoder.EmitLDR(encoder.TargetRegister.Arg0, encoder.TargetRegister.Arg1);

                        // Second arg: index of the type in the ThreadStatic section of the modules
                        encoder.EmitLDR(encoder.TargetRegister.Arg1, encoder.TargetRegister.Arg1, factory.Target.PointerSize);

                        encoder.EmitJMP(helperEntrypoint);
                    }
                    break;

                case ReadyToRunHelperId.DelegateCtor:
                    {
                        // This is a weird helper. Codegen populated Arg0 and Arg1 with the values that the constructor
                        // method expects. Codegen also passed us the generic context in Arg2.
                        // We now need to load the delegate target method into Arg2 (using a dictionary lookup)
                        // and the optional 4th parameter, and call the ctor.

                        Debug.Assert(contextRegister == encoder.TargetRegister.Arg2);

                        var target = (DelegateCreationInfo)_target;

                        EmitDictionaryLookup(factory, ref encoder, encoder.TargetRegister.Arg2, encoder.TargetRegister.Arg2, _lookupSignature, relocsOnly);

                        if (target.Thunk != null)
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 3);
                            encoder.EmitMOV(encoder.TargetRegister.Arg3, target.Thunk);
                        }
                        else
                        {
                            Debug.Assert(target.Constructor.Method.Signature.Length == 2);
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
                        EmitDictionaryLookup(factory, ref encoder, contextRegister, encoder.TargetRegister.Result, _lookupSignature, relocsOnly);
                        encoder.EmitRET();
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual void EmitLoadGenericContext(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
        {
            // Assume generic context is already loaded in the context register.
        }
    }

    public partial class ReadyToRunGenericLookupFromTypeNode
    {
        protected override void EmitLoadGenericContext(NodeFactory factory, ref ARMEmitter encoder, bool relocsOnly)
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
            encoder.EmitLDR(contextRegister, contextRegister, slotOffset);
        }
    }
}
