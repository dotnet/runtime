// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ILLink.Shared.DataFlow;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    /// <summary>
    /// Classification helpers for the analysis that determines whether delegates stored into fields
    /// can ever be observed. See <see cref="DelegateFlowPatterns"/> for the IL patterns that are understood.
    /// </summary>
    internal sealed class DelegateFlowAnalysis
    {
        public enum AccessorKind : byte
        {
            None,

            // Instance method with the exact body `ldarg.0; ldfld F; ret` that returns a delegate.
            Getter,

            // Instance method with the exact body `ldarg.0; ldarg.1; stfld F; ret` that takes a delegate.
            Setter,
        }

        private readonly ILProvider _ilProvider;
        private readonly ConcurrentDictionary<MethodDesc, (AccessorKind Kind, FieldDesc Field)> _accessors = new();

        public DelegateFlowAnalysis(ILProvider ilProvider)
        {
            _ilProvider = ilProvider;
        }

        /// <summary>
        /// Gets whether the given field is one whose contents the analysis tracks. These are fields of delegate type
        /// on reference types (or static fields) that can't be reinterpreted by overlapping fields.
        /// </summary>
        public static bool IsTrackedField(FieldDesc typicalField)
        {
            Debug.Assert(typicalField.IsTypicalFieldDefinition);

            if (typicalField.IsLiteral || typicalField.HasRva || typicalField.IsThreadStatic)
                return false;

            try
            {
                if (!typicalField.FieldType.IsDelegate)
                    return false;
            }
            catch (TypeSystemException)
            {
                // Reflection can make fields of types that don't load visible to us.
                return false;
            }

            if (!typicalField.IsStatic)
            {
                if (typicalField.OwningType is not MetadataType owningType
                    || owningType.IsValueType
                    || owningType.IsExplicitLayout)
                {
                    return false;
                }
            }

            return true;
        }

        public (AccessorKind Kind, FieldDesc Field) GetAccessor(MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            // Only methods from metadata can be accessors. Compiler generated methods can have signatures that are expensive or impossible to compute.
            if (typicalMethod is not EcmaMethod)
                return default;

            // Cheap filter before looking at IL: this gets called for many methods.
            bool maybeGetter;
            bool maybeSetter;
            try
            {
                MethodSignature signature = typicalMethod.Signature;
                if (signature.IsStatic
                    || signature.GenericParameterCount != 0
                    || typicalMethod.IsVirtual
                    || typicalMethod.OwningType.IsValueType
                    || typicalMethod.OwningType.IsInterface)
                {
                    return default;
                }

                maybeGetter = signature.Length == 0 && signature.ReturnType.IsDelegate;
                maybeSetter = signature.Length == 1 && signature[0].IsDelegate && signature.ReturnType.IsVoid;
            }
            catch (TypeSystemException)
            {
                return default;
            }

            if (!maybeGetter && !maybeSetter)
                return default;

            if (_accessors.TryGetValue(typicalMethod, out var result))
                return result;

            result = ComputeAccessor(typicalMethod, maybeGetter);
            _accessors.TryAdd(typicalMethod, result);
            return result;
        }

        private (AccessorKind Kind, FieldDesc Field) ComputeAccessor(MethodDesc typicalMethod, bool isGetter)
        {
            MethodIL methodIL;
            try
            {
                methodIL = _ilProvider.GetMethodIL(typicalMethod);
            }
            catch (TypeSystemException)
            {
                return default;
            }

            if (methodIL == null)
                return default;

            byte[] body = methodIL.GetILBytes();

            int fieldToken;
            if (isGetter)
            {
                // ldarg.0; ldfld <token>; ret
                if (body.Length != 7
                    || body[0] != (byte)ILOpcode.ldarg_0
                    || body[1] != (byte)ILOpcode.ldfld
                    || body[6] != (byte)ILOpcode.ret)
                {
                    return default;
                }

                fieldToken = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(2));
            }
            else
            {
                // ldarg.0; ldarg.1; stfld <token>; ret
                if (body.Length != 8
                    || body[0] != (byte)ILOpcode.ldarg_0
                    || body[1] != (byte)ILOpcode.ldarg_1
                    || body[2] != (byte)ILOpcode.stfld
                    || body[7] != (byte)ILOpcode.ret)
                {
                    return default;
                }

                fieldToken = BinaryPrimitives.ReadInt32LittleEndian(body.AsSpan(3));
            }

            if (methodIL.GetObject(fieldToken, NotFoundBehavior.ReturnNull) is not FieldDesc field)
                return default;

            field = field.GetTypicalFieldDefinition();
            if (field.IsStatic || !IsTrackedField(field))
                return default;

            return (isGetter ? AccessorKind.Getter : AccessorKind.Setter, field);
        }

        public static FieldDesc GetTrackedField(MethodIL methodIL, int token)
        {
            if (methodIL.GetObject(token, NotFoundBehavior.ReturnNull) is not FieldDesc field)
                return null;

            field = field.GetTypicalFieldDefinition();
            return IsTrackedField(field) ? field : null;
        }
    }

    /// <summary>
    /// The result of looking for IL patterns in a method body that let the delegate flow analysis
    /// understand where delegates created by that method go.
    ///
    /// The following patterns are recognized. In all of them, the consumer is a store into a tracked field
    /// (`stfld` or `stsfld`), or a call to a trivial setter of a tracked field, and the instruction following
    /// the producer is not a branch target.
    ///
    /// Delegate creation, in the form the C# compiler uses to cache lambdas and method groups. F is a field of a compiler
    /// generated class, and the receiver is `ldnull` for a static target or the `&lt;&gt;9` singleton of a lambda container
    /// class, so that dropping the creation can't change behavior:
    ///   ldsfld F; dup; brtrue L; pop; (ldsfld C::&lt;&gt;9 | ldnull); ldftn M; newobj D::.ctor; dup; stsfld F; L: consumer
    ///
    /// Copy from one tracked field to another:
    ///   (ldfld G | ldsfld G | call trivial getter of G); consumer
    ///
    /// Clearing a tracked field (not a read of it):
    ///   ldflda F; ldnull; call Volatile.Write&lt;T&gt;(ref T, T)
    /// </summary>
    internal sealed class DelegateFlowPatterns
    {
        public enum EventKind : byte
        {
            // At a `ldftn` that starts a delegate creation. Target is the target method. Dest is the destination field,
            // Source is the field of the cache of the C# lambda caching pattern.
            Site,

            // At the load of a tracked field (Source) whose value is consumed by a store to Dest.
            FieldForward,

            // At a call to a trivial getter (Getter) whose result is consumed by a store to Dest.
            GetterForward,

            // At the `ldflda` of a tracked field that is cleared with Volatile.Write. Not a read of the field.
            VolatileNullWrite,
        }

        public readonly struct Event(EventKind kind, MethodDesc target, MethodDesc getter, FieldDesc source, FieldDesc dest)
        {
            public EventKind Kind { get; } = kind;
            public MethodDesc Target { get; } = target;
            public MethodDesc Getter { get; } = getter;
            public FieldDesc Source { get; } = source;
            public FieldDesc Dest { get; } = dest;
        }

        private readonly Dictionary<int, Event> _events = new();

        public bool TryGetEvent(int offset, out Event e) => _events.TryGetValue(offset, out e);

        // Whether the method has any delegate creation that is conditional on the delegate being observed.
        public bool HasSites { get; private set; }

        public IReadOnlyDictionary<int, Event> Events => _events;

        private struct Instr
        {
            public int Offset;
            public ILOpcode Opcode;
            public int Token;
            public int BranchDestination;
        }

        [ThreadStatic]
        private static List<Instr> t_instructions;

        public static DelegateFlowPatterns Analyze(DelegateFlowAnalysis analysis, MethodIL methodIL)
        {
            // Classifying the callees can end up analyzing other methods on this thread, so the list is taken out while in use.
            List<Instr> instructions = t_instructions ?? new List<Instr>();
            t_instructions = null;
            try
            {
                return AnalyzeCore(analysis, methodIL, instructions);
            }
            catch (TypeSystemException)
            {
                // The method body might refer to things that don't resolve in code that is never reached. Be conservative.
                return null;
            }
            finally
            {
                instructions.Clear();
                t_instructions = instructions;
            }
        }

        private static DelegateFlowPatterns AnalyzeCore(DelegateFlowAnalysis analysis, MethodIL methodIL, List<Instr> instructions)
        {
            byte[] body = methodIL.GetILBytes();
            if (body.Length < 7)
                return null;

            List<int> switchTargets = null;

            var reader = new ILReader(body);
            while (reader.HasNext)
            {
                var instr = new Instr { Offset = reader.Offset, BranchDestination = -1 };
                ILOpcode opcode = reader.ReadILOpcode();
                instr.Opcode = opcode;

                switch (opcode)
                {
                    case ILOpcode.ldfld:
                    case ILOpcode.ldsfld:
                    case ILOpcode.ldflda:
                    case ILOpcode.stfld:
                    case ILOpcode.stsfld:
                    case ILOpcode.call:
                    case ILOpcode.callvirt:
                    case ILOpcode.newobj:
                    case ILOpcode.ldftn:
                        instr.Token = reader.ReadILToken();
                        break;

                    case ILOpcode.switch_:
                        {
                            uint count = reader.ReadILUInt32();
                            int jmpBase = reader.Offset + (int)(4 * count);
                            switchTargets ??= new List<int>();
                            for (uint i = 0; i < count; i++)
                                switchTargets.Add((int)reader.ReadILUInt32() + jmpBase);
                        }
                        break;

                    default:
                        if ((opcode >= ILOpcode.br_s && opcode <= ILOpcode.blt_un) || opcode == ILOpcode.leave || opcode == ILOpcode.leave_s)
                            instr.BranchDestination = reader.ReadBranchDestination(opcode);
                        else
                            reader.Skip(opcode);
                        break;
                }

                instructions.Add(instr);
            }

            int instructionCount = instructions.Count;

            // Most methods have nothing that could match. Find out before doing any real work.
            bool hasCandidate = false;
            for (int i = 0; i < instructionCount && !hasCandidate; i++)
                hasCandidate = IsCandidate(instructions, i);

            if (!hasCandidate)
                return null;

            bool[] isTarget = new bool[body.Length + 1];

            if (switchTargets != null)
            {
                foreach (int destination in switchTargets)
                {
                    if ((uint)destination >= (uint)isTarget.Length)
                        return null;
                    isTarget[destination] = true;
                }
            }

            for (int i = 0; i < instructionCount; i++)
            {
                int destination = instructions[i].BranchDestination;
                if (destination < 0)
                    continue;
                if (destination >= isTarget.Length)
                    return null;
                isTarget[destination] = true;
            }

            foreach (ILExceptionRegion region in methodIL.GetExceptionRegions())
            {
                if ((uint)region.TryOffset >= (uint)isTarget.Length || (uint)region.HandlerOffset >= (uint)isTarget.Length)
                    return null;

                isTarget[region.TryOffset] = true;
                isTarget[region.HandlerOffset] = true;
                if (region.Kind == ILExceptionRegionKind.Filter && (uint)region.FilterOffset < (uint)isTarget.Length)
                    isTarget[region.FilterOffset] = true;
            }

            DelegateFlowPatterns result = null;

            for (int i = 0; i < instructionCount; i++)
            {
                Instr instr = instructions[i];

                switch (instr.Opcode)
                {
                    case ILOpcode.ldftn:
                        TryMatchCreation(analysis, methodIL, instructions, isTarget, i, ref result);
                        break;

                    case ILOpcode.ldfld:
                    case ILOpcode.ldsfld:
                        {
                            if (!IsCandidate(instructions, i) || isTarget[instructions[i + 1].Offset])
                                break;

                            FieldDesc source = DelegateFlowAnalysis.GetTrackedField(methodIL, instr.Token);
                            if (source == null)
                                break;

                            if (TryGetStoreDestination(analysis, methodIL, instructions[i + 1], out FieldDesc dest))
                            {
                                result ??= new DelegateFlowPatterns();
                                result._events[instr.Offset] = new Event(EventKind.FieldForward, null, null, source, dest);
                            }
                        }
                        break;

                    case ILOpcode.call:
                    case ILOpcode.callvirt:
                        {
                            if (!IsCandidate(instructions, i) || isTarget[instructions[i + 1].Offset])
                                break;

                            if (methodIL.GetObject(instr.Token, NotFoundBehavior.ReturnNull) is not MethodDesc method)
                                break;

                            var (kind, field) = analysis.GetAccessor(method);
                            if (kind != DelegateFlowAnalysis.AccessorKind.Getter)
                                break;

                            if (TryGetStoreDestination(analysis, methodIL, instructions[i + 1], out FieldDesc dest))
                            {
                                result ??= new DelegateFlowPatterns();
                                result._events[instr.Offset] = new Event(EventKind.GetterForward, null, method.GetTypicalMethodDefinition(), field, dest);
                            }
                        }
                        break;

                    case ILOpcode.ldflda:
                        {
                            if (!IsCandidate(instructions, i)
                                || isTarget[instructions[i + 1].Offset]
                                || isTarget[instructions[i + 2].Offset])
                            {
                                break;
                            }

                            FieldDesc field = DelegateFlowAnalysis.GetTrackedField(methodIL, instr.Token);
                            if (field == null)
                                break;

                            if (methodIL.GetObject(instructions[i + 2].Token, NotFoundBehavior.ReturnNull) is MethodDesc callee
                                && callee.OwningType is MetadataType calleeType
                                && calleeType.Module == calleeType.Context.SystemModule
                                && calleeType.Name == "Volatile"u8
                                && calleeType.Namespace == "System.Threading"u8
                                && callee.Name == "Write"u8
                                && callee.Signature.Length == 2
                                && callee.HasInstantiation)
                            {
                                result ??= new DelegateFlowPatterns();
                                result._events[instr.Offset] = new Event(EventKind.VolatileNullWrite, null, null, field, null);
                            }
                        }
                        break;
                }
            }

            return result;
        }

        // Whether the instruction at the given index starts something that could be one of the patterns, judging by opcodes alone.
        private static bool IsCandidate(List<Instr> instructions, int i)
        {
            int count = instructions.Count;
            switch (instructions[i].Opcode)
            {
                case ILOpcode.ldftn:
                    return i + 1 < count && instructions[i + 1].Opcode == ILOpcode.newobj;

                case ILOpcode.ldfld:
                case ILOpcode.ldsfld:
                case ILOpcode.call:
                case ILOpcode.callvirt:
                    return i + 1 < count && instructions[i + 1].Opcode is ILOpcode.stfld or ILOpcode.stsfld or ILOpcode.call or ILOpcode.callvirt;

                case ILOpcode.ldflda:
                    return i + 2 < count && instructions[i + 1].Opcode == ILOpcode.ldnull && instructions[i + 2].Opcode == ILOpcode.call;

                default:
                    return false;
            }
        }


        private static void TryMatchCreation(DelegateFlowAnalysis analysis, MethodIL methodIL, List<Instr> instructions, bool[] isTarget, int i, ref DelegateFlowPatterns result)
        {
            int count = instructions.Count;
            Instr ldftn = instructions[i];

            // Delegate creations in static constructors are left alone.
            if (methodIL.OwningMethod.IsStaticConstructor)
                return;

            if (i < 1 || i + 2 >= count || isTarget[ldftn.Offset])
                return;

            Instr newobj = instructions[i + 1];
            if (newobj.Opcode != ILOpcode.newobj || isTarget[newobj.Offset])
                return;

            if (methodIL.GetObject(ldftn.Token, NotFoundBehavior.ReturnNull) is not MethodDesc target
                || methodIL.GetObject(newobj.Token, NotFoundBehavior.ReturnNull) is not MethodDesc ctor
                || !ctor.OwningType.IsDelegate
                || !ctor.IsConstructor
                || !IsInfallibleReceiver(methodIL, instructions[i - 1], target))
            {
                return;
            }

            // Only the pattern the C# compiler uses to cache delegates is matched, with the cache in a compiler generated class.
            // A delegate creation that type preinitialization can interpret must not be dropped: the scanner doesn't see the code
            // preinitialization runs, so it can't tell whether the delegate is observed, and preinitialization would otherwise
            // embed a delegate to a method that was not scanned. Preinitialization gives up when it has to store into the static
            // of another type, which is the case for the cache of every creation matched here. A creation without a cache, or
            // with a cache in a class that could run user code in its static constructor, is left alone.
            if (i >= 5
                && i + 4 < count
                && instructions[i - 5].Opcode == ILOpcode.ldsfld
                && instructions[i - 4].Opcode == ILOpcode.dup
                && instructions[i - 3].Opcode is ILOpcode.brtrue or ILOpcode.brtrue_s
                && instructions[i - 2].Opcode == ILOpcode.pop
                && instructions[i - 1].Opcode is ILOpcode.ldsfld or ILOpcode.ldnull
                && instructions[i + 2].Opcode == ILOpcode.dup
                && instructions[i + 3].Opcode == ILOpcode.stsfld
                && instructions[i - 3].BranchDestination == instructions[i + 4].Offset
                && !isTarget[instructions[i - 4].Offset]
                && !isTarget[instructions[i - 3].Offset]
                && !isTarget[instructions[i - 2].Offset]
                && !isTarget[instructions[i - 1].Offset]
                && !isTarget[ldftn.Offset]
                && !isTarget[instructions[i + 2].Offset]
                && !isTarget[instructions[i + 3].Offset])
            {
                FieldDesc cache = DelegateFlowAnalysis.GetTrackedField(methodIL, instructions[i - 5].Token);
                FieldDesc storedCache = DelegateFlowAnalysis.GetTrackedField(methodIL, instructions[i + 3].Token);
                if (cache != null && cache == storedCache
                    && cache.OwningType is MetadataType cacheOwner
                    && (CompilerGeneratedNames.IsLambdaDisplayClass(cacheOwner.Name.AsSpan()) || (cacheOwner.Name.AsSpan().StartsWith("<>O"u8) && !cacheOwner.HasStaticConstructor))
                    && TryGetStoreDestination(analysis, methodIL, instructions[i + 4], out FieldDesc dest))
                {
                    result ??= new DelegateFlowPatterns();

                    // The value read from the cache by the first instruction flows to the same place as the new delegate.
                    result._events[instructions[i - 5].Offset] = new Event(EventKind.FieldForward, null, null, cache, dest);
                    result._events[ldftn.Offset] = new Event(EventKind.Site, target.GetTypicalMethodDefinition(), null, cache, dest);
                    result.HasSites = true;
                }
            }
        }

        // Removing the delegate creation also removes the checks the runtime does when binding the delegate to its receiver,
        // and the use of the receiver. This is only fine if the creation can't throw and has no observable effect:
        // - `ldnull` with a static target (an open static delegate), or
        // - the singleton instance of the container class the C# compiler uses for lambdas that capture nothing
        //   (`ldsfld <>c::<>9`, which is never null), with a target in that class.
        // Any other receiver, or a prefix such as `constrained.` in front of the `ldftn`, is left alone.
        private static bool IsInfallibleReceiver(MethodIL methodIL, in Instr receiver, MethodDesc target)
        {
            switch (receiver.Opcode)
            {
                case ILOpcode.ldnull:
                    return target.Signature.IsStatic;

                case ILOpcode.ldsfld:
                    return !target.Signature.IsStatic
                        && methodIL.GetObject(receiver.Token, NotFoundBehavior.ReturnNull) is FieldDesc field
                        && field.IsStatic
                        && field.IsInitOnly
                        && field.Name == "<>9"u8
                        && field.OwningType is MetadataType owningType
                        && owningType.HasStaticConstructor
                        && CompilerGeneratedNames.IsLambdaDisplayClass(owningType.Name.AsSpan())
                        && field.GetTypicalFieldDefinition().OwningType == target.GetTypicalMethodDefinition().OwningType;

                default:
                    return false;
            }
        }

        private static bool TryGetStoreDestination(DelegateFlowAnalysis analysis, MethodIL methodIL, in Instr consumer, out FieldDesc dest)
        {
            dest = null;

            switch (consumer.Opcode)
            {
                case ILOpcode.stfld:
                case ILOpcode.stsfld:
                    dest = DelegateFlowAnalysis.GetTrackedField(methodIL, consumer.Token);
                    return dest != null;

                case ILOpcode.call:
                case ILOpcode.callvirt:
                    if (methodIL.GetObject(consumer.Token, NotFoundBehavior.ReturnNull) is MethodDesc method)
                    {
                        var (kind, field) = analysis.GetAccessor(method);
                        if (kind == DelegateFlowAnalysis.AccessorKind.Setter)
                        {
                            dest = field;
                            return true;
                        }
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
