// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
    public class ILGenerator
    {
        #region Const Members
        private const int DefaultSize = 16;
        private const int DefaultFixupArraySize = 8;
        private const int DefaultLabelArraySize = 4;
        private const int DefaultExceptionArraySize = 2;
        #endregion

        #region Internal Statics
        internal static T[] EnlargeArray<T>(T[] incoming)
        {
            return EnlargeArray(incoming, incoming.Length * 2);
        }

        internal static T[] EnlargeArray<T>(T[] incoming, int requiredSize)
        {
            Debug.Assert(incoming != null);

            T[] temp = new T[requiredSize];
            Array.Copy(incoming, temp, incoming.Length);
            return temp;
        }

        #endregion

        #region Internal Data Members
        private int m_length;
        private byte[] m_ILStream;

        private __LabelInfo[]? m_labelList;
        private int m_labelCount;

        private __FixupData[]? m_fixupData;

        private int m_fixupCount;

        private int[]? m_RelocFixupList;
        private int m_RelocFixupCount;

        private int m_exceptionCount;
        private int m_currExcStackCount;
        private __ExceptionInfo[]? m_exceptions;           // This is the list of all of the exceptions in this ILStream.
        private __ExceptionInfo[]? m_currExcStack;         // This is the stack of exceptions which we're currently in.

        internal ScopeTree m_ScopeTree;            // this variable tracks all debugging scope information

        internal MethodInfo m_methodBuilder;
        internal int m_localCount;
        internal SignatureHelper m_localSignature;

        private int m_curDepth; // Current stack depth, with -1 meaning unknown.
        private int m_targetDepth; // Stack depth at a target of the previous instruction (when it is branching).
        private int m_maxDepth; // Running max of the stack depth.

        // Adjustment to add to m_maxDepth for incorrect/invalid IL. For example, when branch instructions
        // with different stack depths target the same label.
        private long m_depthAdjustment;

        internal int CurrExcStackCount => m_currExcStackCount;

        internal __ExceptionInfo[]? CurrExcStack => m_currExcStack;

        #endregion

        #region Constructor
        // package private constructor. This code path is used when client create
        // ILGenerator through MethodBuilder.
        internal ILGenerator(MethodInfo methodBuilder) : this(methodBuilder, 64)
        {
        }

        internal ILGenerator(MethodInfo methodBuilder, int size)
        {
            Debug.Assert(methodBuilder != null);
            Debug.Assert(methodBuilder is MethodBuilder || methodBuilder is DynamicMethod);

            m_ILStream = new byte[Math.Max(size, DefaultSize)];

            // initialize the scope tree
            m_ScopeTree = new ScopeTree();
            m_methodBuilder = methodBuilder;

            // initialize local signature
            MethodBuilder? mb = m_methodBuilder as MethodBuilder;
            m_localSignature = SignatureHelper.GetLocalVarSigHelper(mb?.GetTypeBuilder().Module);
        }

        #endregion

        #region Internal Members
        internal virtual void RecordTokenFixup()
        {
            if (m_RelocFixupList == null)
            {
                m_RelocFixupList = new int[DefaultFixupArraySize];
            }
            else if (m_RelocFixupList.Length <= m_RelocFixupCount)
            {
                m_RelocFixupList = EnlargeArray(m_RelocFixupList);
            }

            m_RelocFixupList[m_RelocFixupCount++] = m_length;
        }

        internal void InternalEmit(OpCode opcode)
        {
            short opcodeValue = opcode.Value;
            if (opcode.Size != 1)
            {
                BinaryPrimitives.WriteInt16BigEndian(m_ILStream.AsSpan(m_length), opcodeValue);
                m_length += 2;
            }
            else
            {
                m_ILStream[m_length++] = (byte)opcodeValue;
            }

            UpdateStackSize(opcode, opcode.StackChange());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateStackSize(OpCode opcode, int stackchange)
        {
            // Updates internal variables for keeping track of the stack size
            // requirements for the function.  stackchange specifies the amount
            // by which the stacksize needs to be updated.

            if (m_curDepth < 0)
            {
                // Current depth is "unknown". We get here when:
                // * this is unreachable code.
                // * the client uses explicit numeric offsets rather than Labels.
                m_curDepth = 0;
            }

            m_curDepth += stackchange;
            if (m_curDepth < 0)
            {
                // Stack underflow. Assume our previous depth computation was flawed.
                m_depthAdjustment -= m_curDepth;
                m_curDepth = 0;
            }
            else if (m_maxDepth < m_curDepth)
                m_maxDepth = m_curDepth;
            Debug.Assert(m_depthAdjustment >= 0);
            Debug.Assert(m_curDepth >= 0);

            // Record the stack depth at a "target" of this instruction.
            m_targetDepth = m_curDepth;

            // If the current instruction can't fall through, set the depth to unknown.
            if (opcode.EndsUncondJmpBlk())
                m_curDepth = -1;
        }

        private int GetMethodToken(MethodBase method, Type[]? optionalParameterTypes, bool useMethodDef)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMethodTokenInternal(method, optionalParameterTypes, useMethodDef);
        }

        internal SignatureHelper GetMemberRefSignature(
            CallingConventions call,
            Type? returnType,
            Type[]? parameterTypes,
            Type[]? optionalParameterTypes)
        {
            return GetMemberRefSignature(call, returnType, parameterTypes, null, null, optionalParameterTypes);
        }
        internal virtual SignatureHelper GetMemberRefSignature(CallingConventions call, Type? returnType,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers, Type[]? optionalParameterTypes)
        {
            return GetMemberRefSignature(call, returnType, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, optionalParameterTypes, 0);
        }

        private SignatureHelper GetMemberRefSignature(CallingConventions call, Type? returnType,
            Type[]? parameterTypes, Type[][]? requiredCustomModifiers, Type[][]? optionalCustomModifiers, Type[]? optionalParameterTypes, int cGenericParameters)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMemberRefSignature(call, returnType, parameterTypes, requiredCustomModifiers, optionalCustomModifiers, optionalParameterTypes, cGenericParameters);
        }

        internal byte[]? BakeByteArray()
        {
            // BakeByteArray is an internal function designed to be called by MethodBuilder to do
            // all of the fixups and return a new byte array representing the byte stream with labels resolved, etc.

            if (m_currExcStackCount != 0)
            {
                throw new ArgumentException(SR.Argument_UnclosedExceptionBlock);
            }

            if (m_length == 0)
                return null;

            // Allocate space for the new array.
            byte[] newBytes = new byte[m_length];

            // Copy the data from the old array
            Array.Copy(m_ILStream, newBytes, m_length);

            // Do the fixups.
            // This involves iterating over all of the labels and
            // replacing them with their proper values.
            for (int i = 0; i < m_fixupCount; i++)
            {
                __FixupData fixupData = m_fixupData![i];
                int updateAddr = GetLabelPos(fixupData.m_fixupLabel) - (fixupData.m_fixupPos + fixupData.m_fixupInstSize);

                // Handle single byte instructions
                // Throw an exception if they're trying to store a jump in a single byte instruction that doesn't fit.
                if (fixupData.m_fixupInstSize == 1)
                {
                    // Verify that our one-byte arg will fit into a Signed Byte.
                    if (updateAddr < sbyte.MinValue || updateAddr > sbyte.MaxValue)
                    {
                        throw new NotSupportedException(SR.Format(SR.NotSupported_IllegalOneByteBranch, fixupData.m_fixupPos, updateAddr));
                    }

                    // Place the one-byte arg
                    newBytes[fixupData.m_fixupPos] = (byte)updateAddr;
                }
                else
                {
                    // Place the four-byte arg
                    BinaryPrimitives.WriteInt32LittleEndian(newBytes.AsSpan(fixupData.m_fixupPos), updateAddr);
                }
            }
            return newBytes;
        }

        internal __ExceptionInfo[]? GetExceptions()
        {
            if (m_currExcStackCount != 0)
            {
                throw new NotSupportedException(SR.Argument_UnclosedExceptionBlock);
            }

            if (m_exceptionCount == 0)
            {
                return null;
            }

            var temp = new __ExceptionInfo[m_exceptionCount];
            Array.Copy(m_exceptions!, temp, m_exceptionCount);
            SortExceptions(temp);
            return temp;
        }

        internal void EnsureCapacity(int size)
        {
            // Guarantees an array capable of holding at least size elements.
            if (m_length + size >= m_ILStream.Length)
            {
                IncreaseCapacity(size);
            }
        }

        private void IncreaseCapacity(int size)
        {
            byte[] temp = new byte[Math.Max(m_ILStream.Length * 2, m_length + size)];
            Array.Copy(m_ILStream, temp, m_ILStream.Length);
            m_ILStream = temp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void PutInteger4(int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(m_ILStream.AsSpan(m_length), value);
            m_length += 4;
        }

        private int GetLabelPos(Label lbl)
        {
            // Gets the position in the stream of a particular label.
            // Verifies that the label exists and that it has been given a value.

            int index = lbl.GetLabelValue();

            if (index < 0 || index >= m_labelCount || m_labelList is null)
                throw new ArgumentException(SR.Argument_BadLabel);

            int pos = m_labelList[index].m_pos;
            if (pos < 0)
                throw new ArgumentException(SR.Argument_BadLabelContent);

            return pos;
        }

        private void AddFixup(Label lbl, int pos, int instSize)
        {
            // Notes the label, position, and instruction size of a new fixup.  Expands
            // all of the fixup arrays as appropriate.

            if (m_fixupData == null)
            {
                m_fixupData = new __FixupData[DefaultFixupArraySize];
            }
            else if (m_fixupData.Length <= m_fixupCount)
            {
                m_fixupData = EnlargeArray(m_fixupData);
            }

            m_fixupData[m_fixupCount++] = new __FixupData
            {
                m_fixupPos = pos,
                m_fixupLabel = lbl,
                m_fixupInstSize = instSize
            };

            int labelIndex = lbl.GetLabelValue();
            if (labelIndex < 0 || labelIndex >= m_labelCount || m_labelList is null)
                throw new ArgumentException(SR.Argument_BadLabel);

            int depth = m_labelList[labelIndex].m_depth;
            int targetDepth = m_targetDepth;
            Debug.Assert(depth >= -1);
            Debug.Assert(targetDepth >= -1);
            if (depth < targetDepth)
            {
                // Either unknown depth for this label or this branch location has a larger depth than previously recorded.
                // In the latter case, the IL is (likely) invalid, but we just compensate for it.
                if (depth >= 0)
                    m_depthAdjustment += targetDepth - depth;
                m_labelList[labelIndex].m_depth = targetDepth;
            }
        }

        internal int GetMaxStackSize()
        {
            // Limit the computed max stack to 2^16 - 1, since the value is mod`ed by 2^16 by other code.
            Debug.Assert(m_depthAdjustment >= 0);
            return (int)Math.Min(ushort.MaxValue, m_maxDepth + m_depthAdjustment);
        }

        private static void SortExceptions(__ExceptionInfo[] exceptions)
        {
            // In order to call exceptions properly we have to sort them in ascending order by their end position.
            // Just a cheap insertion sort.  We don't expect many exceptions (<10), where InsertionSort beats QuickSort.
            // If we have more exceptions than this in real life, we should consider moving to a QuickSort.

            for (int i = 0; i < exceptions.Length; i++)
            {
                int least = i;
                for (int j = i + 1; j < exceptions.Length; j++)
                {
                    if (exceptions[least].IsInner(exceptions[j]))
                    {
                        least = j;
                    }
                }
                __ExceptionInfo temp = exceptions[i];
                exceptions[i] = exceptions[least];
                exceptions[least] = temp;
            }
        }

        internal int[]? GetTokenFixups()
        {
            if (m_RelocFixupCount == 0)
            {
                Debug.Assert(m_RelocFixupList == null);
                return null;
            }

            int[] narrowTokens = new int[m_RelocFixupCount];
            Array.Copy(m_RelocFixupList!, narrowTokens, m_RelocFixupCount);
            return narrowTokens;
        }
        #endregion

        #region Public Members

        #region Emit
        public virtual void Emit(OpCode opcode)
        {
            EnsureCapacity(3);
            InternalEmit(opcode);
        }

        public virtual void Emit(OpCode opcode, byte arg)
        {
            EnsureCapacity(4);
            InternalEmit(opcode);
            m_ILStream[m_length++] = arg;
        }

        [CLSCompliant(false)]
        public void Emit(OpCode opcode, sbyte arg)
        {
            // Puts opcode onto the stream of instructions followed by arg
            EnsureCapacity(4);
            InternalEmit(opcode);
            m_ILStream[m_length++] = (byte)arg;
        }

        public virtual void Emit(OpCode opcode, short arg)
        {
            // Puts opcode onto the stream of instructions followed by arg
            EnsureCapacity(5);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt16LittleEndian(m_ILStream.AsSpan(m_length), arg);
            m_length += 2;
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            // Special-case several opcodes that have shorter variants for common values.
            if (opcode.Equals(OpCodes.Ldc_I4))
            {
                if (arg >= -1 && arg <= 8)
                {
                    opcode = arg switch
                    {
                        -1 => OpCodes.Ldc_I4_M1,
                        0 => OpCodes.Ldc_I4_0,
                        1 => OpCodes.Ldc_I4_1,
                        2 => OpCodes.Ldc_I4_2,
                        3 => OpCodes.Ldc_I4_3,
                        4 => OpCodes.Ldc_I4_4,
                        5 => OpCodes.Ldc_I4_5,
                        6 => OpCodes.Ldc_I4_6,
                        7 => OpCodes.Ldc_I4_7,
                        _ => OpCodes.Ldc_I4_8,
                    };
                    Emit(opcode);
                    return;
                }

                if (arg >= -128 && arg <= 127)
                {
                    Emit(OpCodes.Ldc_I4_S, (sbyte)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarg))
            {
                if ((uint)arg <= 3)
                {
                    Emit(arg switch
                    {
                        0 => OpCodes.Ldarg_0,
                        1 => OpCodes.Ldarg_1,
                        2 => OpCodes.Ldarg_2,
                        _ => OpCodes.Ldarg_3,
                    });
                    return;
                }

                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarg, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Ldarga))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Ldarga_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Ldarga, (short)arg);
                    return;
                }
            }
            else if (opcode.Equals(OpCodes.Starg))
            {
                if ((uint)arg <= byte.MaxValue)
                {
                    Emit(OpCodes.Starg_S, (byte)arg);
                    return;
                }

                if ((uint)arg <= ushort.MaxValue) // this will be true except on misuse of the opcode
                {
                    Emit(OpCodes.Starg, (short)arg);
                    return;
                }
            }

            // For everything else, put the opcode followed by the arg onto the stream of instructions.
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(arg);
        }

        public virtual void Emit(OpCode opcode, MethodInfo meth)
        {
            ArgumentNullException.ThrowIfNull(meth);

            if (opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj))
            {
                EmitCall(opcode, meth, null);
            }
            else
            {
                // Reflection doesn't distinguish between these two concepts:
                //   1. A generic method definition: Foo`1
                //   2. A generic method definition instantiated over its own generic arguments: Foo`1<!!0>
                // In RefEmit, we always want 1 for Ld* opcodes and 2 for Call* and Newobj.
                bool useMethodDef = opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn);
                int tk = GetMethodToken(meth, null, useMethodDef);

                EnsureCapacity(7);
                InternalEmit(opcode);

                UpdateStackSize(opcode, 0);
                RecordTokenFixup();
                PutInteger4(tk);
            }
        }

        public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention,
            Type? returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes)
        {
            int stackchange = 0;
            if (optionalParameterTypes != null)
            {
                if ((callingConvention & CallingConventions.VarArgs) == 0)
                {
                    // Client should not supply optional parameter in default calling convention
                    throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
                }
            }

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            SignatureHelper sig = GetMemberRefSignature(callingConvention,
                returnType,
                parameterTypes,
                optionalParameterTypes);

            EnsureCapacity(7);
            Emit(OpCodes.Calli);

            // If there is a non-void return type, push one.
            if (returnType != typeof(void))
                stackchange++;
            // Pop off arguments if any.
            if (parameterTypes != null)
                stackchange -= parameterTypes.Length;
            // Pop off vararg arguments.
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            // Pop the this parameter if the method has a this parameter.
            if ((callingConvention & CallingConventions.HasThis) == CallingConventions.HasThis)
                stackchange--;
            // Pop the native function pointer.
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);

            RecordTokenFixup();
            PutInteger4(modBuilder.GetSignatureToken(sig));
        }

        public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
        {
            int stackchange = 0;
            int cParams = 0;

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;

            if (parameterTypes != null)
            {
                cParams = parameterTypes.Length;
            }

            SignatureHelper sig = SignatureHelper.GetMethodSigHelper(
                modBuilder,
                unmanagedCallConv,
                returnType);

            if (parameterTypes != null)
            {
                for (int i = 0; i < cParams; i++)
                {
                    sig.AddArgument(parameterTypes[i]);
                }
            }

            // If there is a non-void return type, push one.
            if (returnType != typeof(void))
                stackchange++;

            // Pop off arguments if any.
            if (parameterTypes != null)
                stackchange -= cParams;

            // Pop the native function pointer.
            stackchange--;
            UpdateStackSize(OpCodes.Calli, stackchange);

            EnsureCapacity(7);
            Emit(OpCodes.Calli);
            RecordTokenFixup();
            PutInteger4(modBuilder.GetSignatureToken(sig));
        }

        public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
        {
            ArgumentNullException.ThrowIfNull(methodInfo);

            if (!(opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj)))
                throw new ArgumentException(SR.Argument_NotMethodCallOpcode, nameof(opcode));

            int stackchange = 0;
            int tk = GetMethodToken(methodInfo, optionalParameterTypes, false);

            EnsureCapacity(7);
            InternalEmit(opcode);

            // Push the return value if there is one.
            if (methodInfo.ReturnType != typeof(void))
                stackchange++;
            // Pop the parameters.
            Type[] parameters = methodInfo.GetParameterTypes();
            if (parameters != null)
                stackchange -= parameters.Length;

            // Pop the this parameter if the method is non-static and the
            // instruction is not newobj.
            if (!(methodInfo is SymbolMethod) && !methodInfo.IsStatic && !opcode.Equals(OpCodes.Newobj))
                stackchange--;
            // Pop the optional parameters off the stack.
            if (optionalParameterTypes != null)
                stackchange -= optionalParameterTypes.Length;
            UpdateStackSize(opcode, stackchange);

            RecordTokenFixup();
            PutInteger4(tk);
        }

        public virtual void Emit(OpCode opcode, SignatureHelper signature)
        {
            ArgumentNullException.ThrowIfNull(signature);

            int stackchange = 0;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            int sig = modBuilder.GetSignatureToken(signature);

            int tempVal = sig;

            EnsureCapacity(7);
            InternalEmit(opcode);

            // The only IL instruction that has VarPop behaviour, that takes a
            // Signature token as a parameter is calli.  Pop the parameters and
            // the native function pointer.  To be conservative, do not pop the
            // this pointer since this information is not easily derived from
            // SignatureHelper.
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                Debug.Assert(opcode.Equals(OpCodes.Calli),
                                "Unexpected opcode encountered for StackBehaviour VarPop.");
                // Pop the arguments..
                stackchange -= signature.ArgumentCount;
                // Pop native function pointer off the stack.
                stackchange--;
                UpdateStackSize(opcode, stackchange);
            }

            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, ConstructorInfo con)
        {
            ArgumentNullException.ThrowIfNull(con);

            int stackchange = 0;

            // Constructors cannot be generic so the value of UseMethodDef doesn't matter.
            int tk = GetMethodToken(con, null, true);

            EnsureCapacity(7);
            InternalEmit(opcode);

            // Make a conservative estimate by assuming a return type and no
            // this parameter.
            if (opcode.StackBehaviourPush == StackBehaviour.Varpush)
            {
                // Instruction must be one of call or callvirt.
                Debug.Assert(opcode.Equals(OpCodes.Call) ||
                                opcode.Equals(OpCodes.Callvirt),
                                "Unexpected opcode encountered for StackBehaviour of VarPush.");
                stackchange++;
            }
            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
            {
                // Instruction must be one of call, callvirt or newobj.
                Debug.Assert(opcode.Equals(OpCodes.Call) ||
                                opcode.Equals(OpCodes.Callvirt) ||
                                opcode.Equals(OpCodes.Newobj),
                                "Unexpected opcode encountered for StackBehaviour of VarPop.");

                Type[] parameters = con.GetParameterTypes();
                if (parameters != null)
                    stackchange -= parameters.Length;
            }
            UpdateStackSize(opcode, stackchange);

            RecordTokenFixup();
            PutInteger4(tk);
        }

        public virtual void Emit(OpCode opcode, Type cls)
        {
            // Puts opcode onto the stream and then the metadata token represented
            // by cls.  The location of cls is recorded so that the token can be
            // patched if necessary when persisting the module to a PE.

            int tempVal;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            if (opcode == OpCodes.Ldtoken && cls != null && cls.IsGenericTypeDefinition)
            {
                // This gets the token for the generic type definition if cls is one.
                tempVal = modBuilder.GetTypeToken(cls);
            }
            else
            {
                // This gets the token for the generic type instantiated on the formal parameters
                // if cls is a generic type definition.
                tempVal = modBuilder.GetTypeTokenInternal(cls!);
            }

            EnsureCapacity(7);
            InternalEmit(opcode);
            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, long arg)
        {
            EnsureCapacity(11);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(m_ILStream.AsSpan(m_length), arg);
            m_length += 8;
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            EnsureCapacity(7);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt32LittleEndian(m_ILStream.AsSpan(m_length), BitConverter.SingleToInt32Bits(arg));
            m_length += 4;
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            EnsureCapacity(11);
            InternalEmit(opcode);
            BinaryPrimitives.WriteInt64LittleEndian(m_ILStream.AsSpan(m_length), BitConverter.DoubleToInt64Bits(arg));
            m_length += 8;
        }

        public virtual void Emit(OpCode opcode, Label label)
        {
            // Puts opcode onto the stream and leaves space to include label
            // when fixups are done.  Labels are created using ILGenerator.DefineLabel and
            // their location within the stream is fixed by using ILGenerator.MarkLabel.
            // If a single-byte instruction (designated by the _S suffix in OpCodes.cs) is used,
            // the label can represent a jump of at most 127 bytes along the stream.
            //
            // opcode must represent a branch instruction (although we don't explicitly
            // verify this).  Since branches are relative instructions, label will be replaced with the
            // correct offset to branch during the fixup process.

            EnsureCapacity(7);

            InternalEmit(opcode);
            if (OpCodes.TakesSingleByteArgument(opcode))
            {
                AddFixup(label, m_length++, 1);
            }
            else
            {
                AddFixup(label, m_length, 4);
                m_length += 4;
            }
        }

        public virtual void Emit(OpCode opcode, Label[] labels)
        {
            ArgumentNullException.ThrowIfNull(labels);

            // Emitting a switch table

            int i;
            int remaining;                  // number of bytes remaining for this switch instruction to be subtracted
            // for computing the offset

            int count = labels.Length;

            EnsureCapacity(count * 4 + 7);
            InternalEmit(opcode);
            PutInteger4(count);
            for (remaining = count * 4, i = 0; remaining > 0; remaining -= 4, i++)
            {
                AddFixup(labels[i], m_length, remaining);
                m_length += 4;
            }
        }

        public virtual void Emit(OpCode opcode, FieldInfo field)
        {
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            int tempVal = modBuilder.GetFieldToken(field);
            EnsureCapacity(7);
            InternalEmit(opcode);
            RecordTokenFixup();
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, string str)
        {
            // Puts the opcode onto the IL stream followed by the metadata token
            // represented by str.  The location of str is recorded for future
            // fixups if the module is persisted to a PE.

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            int tempVal = modBuilder.GetStringConstant(str);
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, LocalBuilder local)
        {
            ArgumentNullException.ThrowIfNull(local);

            // Puts the opcode onto the IL stream followed by the information for local variable local.
            int tempVal = local.GetLocalIndex();
            if (local.GetMethodBuilder() != m_methodBuilder)
            {
                throw new ArgumentException(SR.Argument_UnmatchedMethodForLocal, nameof(local));
            }
            // If the instruction is a ldloc, ldloca a stloc, morph it to the optimal form.
            if (opcode.Equals(OpCodes.Ldloc))
            {
                switch (tempVal)
                {
                    case 0:
                        opcode = OpCodes.Ldloc_0;
                        break;
                    case 1:
                        opcode = OpCodes.Ldloc_1;
                        break;
                    case 2:
                        opcode = OpCodes.Ldloc_2;
                        break;
                    case 3:
                        opcode = OpCodes.Ldloc_3;
                        break;
                    default:
                        if (tempVal <= 255)
                            opcode = OpCodes.Ldloc_S;
                        break;
                }
            }
            else if (opcode.Equals(OpCodes.Stloc))
            {
                switch (tempVal)
                {
                    case 0:
                        opcode = OpCodes.Stloc_0;
                        break;
                    case 1:
                        opcode = OpCodes.Stloc_1;
                        break;
                    case 2:
                        opcode = OpCodes.Stloc_2;
                        break;
                    case 3:
                        opcode = OpCodes.Stloc_3;
                        break;
                    default:
                        if (tempVal <= 255)
                            opcode = OpCodes.Stloc_S;
                        break;
                }
            }
            else if (opcode.Equals(OpCodes.Ldloca))
            {
                if (tempVal <= 255)
                    opcode = OpCodes.Ldloca_S;
            }

            EnsureCapacity(7);
            InternalEmit(opcode);

            if (opcode.OperandType == OperandType.InlineNone)
                return;

            if (!OpCodes.TakesSingleByteArgument(opcode))
            {
                BinaryPrimitives.WriteInt16LittleEndian(m_ILStream.AsSpan(m_length), (short)tempVal);
                m_length += 2;
            }
            else
            {
                // Handle stloc_1, ldloc_1
                if (tempVal > byte.MaxValue)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_BadInstructionOrIndexOutOfBound);
                }
                m_ILStream[m_length++] = (byte)tempVal;
            }
        }
        #endregion

        #region Exceptions
        public virtual Label BeginExceptionBlock()
        {
            // Begin an Exception block.  Creating an Exception block records some information,
            // but does not actually emit any IL onto the stream.  Exceptions should be created and
            // marked in the following form:
            //
            // Emit Some IL
            // BeginExceptionBlock
            // Emit the IL which should appear within the "try" block
            // BeginCatchBlock
            // Emit the IL which should appear within the "catch" block
            // Optional: BeginCatchBlock (this can be repeated an arbitrary number of times
            // EndExceptionBlock

            // Delay init
            m_exceptions ??= new __ExceptionInfo[DefaultExceptionArraySize];
            m_currExcStack ??= new __ExceptionInfo[DefaultExceptionArraySize];

            if (m_exceptionCount >= m_exceptions.Length)
            {
                m_exceptions = EnlargeArray(m_exceptions);
            }

            if (m_currExcStackCount >= m_currExcStack.Length)
            {
                m_currExcStack = EnlargeArray(m_currExcStack);
            }

            Label endLabel = DefineLabel(0);
            __ExceptionInfo exceptionInfo = new __ExceptionInfo(m_length, endLabel);

            // add the exception to the tracking list
            m_exceptions[m_exceptionCount++] = exceptionInfo;

            // Make this exception the current active exception
            m_currExcStack[m_currExcStackCount++] = exceptionInfo;

            // Stack depth for "try" starts at zero.
            m_curDepth = 0;

            return endLabel;
        }

        public virtual void EndExceptionBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            // Pop the current exception block
            __ExceptionInfo current = m_currExcStack![m_currExcStackCount - 1];
            m_currExcStack[--m_currExcStackCount] = null!;

            Label endLabel = current.GetEndLabel();
            int state = current.GetCurrentState();

            if (state == __ExceptionInfo.State_Filter ||
                state == __ExceptionInfo.State_Try)
            {
                throw new InvalidOperationException(SR.Argument_BadExceptionCodeGen);
            }

            if (state == __ExceptionInfo.State_Catch)
            {
                Emit(OpCodes.Leave, endLabel);
            }
            else if (state == __ExceptionInfo.State_Finally || state == __ExceptionInfo.State_Fault)
            {
                Emit(OpCodes.Endfinally);
            }

            // Check if we've already set this label.
            // The only reason why we might have set this is if we have a finally block.

            Label label = m_labelList![endLabel.GetLabelValue()].m_pos != -1
                ? current.m_finallyEndLabel
                : endLabel;

            MarkLabel(label);

            current.Done(m_length);
        }

        public virtual void BeginExceptFilterBlock()
        {
            // Begins an exception filter block.  Emits a branch instruction to the end of the current exception block.

            if (m_currExcStackCount == 0)
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);

            __ExceptionInfo current = m_currExcStack![m_currExcStackCount - 1];

            Emit(OpCodes.Leave, current.GetEndLabel());

            current.MarkFilterAddr(m_length);

            // Stack depth for "filter" starts at one.
            m_curDepth = 1;
        }

        public virtual void BeginCatchBlock(Type? exceptionType)
        {
            // Begins a catch block.  Emits a branch instruction to the end of the current exception block.

            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack![m_currExcStackCount - 1];

            if (current.GetCurrentState() == __ExceptionInfo.State_Filter)
            {
                if (exceptionType != null)
                {
                    throw new ArgumentException(SR.Argument_ShouldNotSpecifyExceptionType);
                }

                Emit(OpCodes.Endfilter);
            }
            else
            {
                // execute this branch if previous clause is Catch or Fault
                ArgumentNullException.ThrowIfNull(exceptionType);

                Emit(OpCodes.Leave, current.GetEndLabel());
            }

            current.MarkCatchAddr(m_length, exceptionType);

            // Stack depth for "catch" starts at one.
            m_curDepth = 1;
        }

        public virtual void BeginFaultBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack![m_currExcStackCount - 1];

            // emit the leave for the clause before this one.
            Emit(OpCodes.Leave, current.GetEndLabel());

            current.MarkFaultAddr(m_length);

            // Stack depth for "fault" starts at zero.
            m_curDepth = 0;
        }

        public virtual void BeginFinallyBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack![m_currExcStackCount - 1];
            int state = current.GetCurrentState();
            Label endLabel = current.GetEndLabel();
            int catchEndAddr = 0;
            if (state != __ExceptionInfo.State_Try)
            {
                // generate leave for any preceding catch clause
                Emit(OpCodes.Leave, endLabel);
                catchEndAddr = m_length;
            }

            MarkLabel(endLabel);

            Label finallyEndLabel = DefineLabel(0);
            current.SetFinallyEndLabel(finallyEndLabel);

            // generate leave for try clause
            Emit(OpCodes.Leave, finallyEndLabel);
            if (catchEndAddr == 0)
                catchEndAddr = m_length;
            current.MarkFinallyAddr(m_length, catchEndAddr);

            // Stack depth for "finally" starts at zero.
            m_curDepth = 0;
        }

        #endregion

        #region Labels
        public virtual Label DefineLabel()
        {
            // We don't know the stack depth at the label yet, so set it to -1.
            return DefineLabel(-1);
        }

        private Label DefineLabel(int depth)
        {
            // Declares a new Label.  This is just a token and does not yet represent any particular location
            // within the stream.  In order to set the position of the label within the stream, you must call
            // Mark Label.
            Debug.Assert(depth >= -1);

            // Delay init the label array in case we dont use it
            m_labelList ??= new __LabelInfo[DefaultLabelArraySize];

            if (m_labelCount >= m_labelList.Length)
            {
                m_labelList = EnlargeArray(m_labelList);
            }
            m_labelList[m_labelCount].m_pos = -1;
            m_labelList[m_labelCount].m_depth = depth;
            return new Label(m_labelCount++);
        }

        public virtual void MarkLabel(Label loc)
        {
            // Defines a label by setting the position where that label is found within the stream.
            // Does not allow a label to be defined more than once.

            int labelIndex = loc.GetLabelValue();

            // This should only happen if a label from another generator is used with this one.
            if (m_labelList is null || labelIndex < 0 || labelIndex >= m_labelList.Length)
            {
                throw new ArgumentException(SR.Argument_InvalidLabel);
            }

            if (m_labelList[labelIndex].m_pos != -1)
            {
                throw new ArgumentException(SR.Argument_RedefinedLabel);
            }

            m_labelList[labelIndex].m_pos = m_length;

            int depth = m_labelList[labelIndex].m_depth;
            if (depth < 0)
            {
                // Unknown depth for this label, indicating that it hasn't been used yet.
                // If m_curDepth is unknown, we're in the Backward branch constraint case. See ECMA-335 III.1.7.5.
                // The m_depthAdjustment field will compensate for violations of this constraint, as we
                // discover them. That is, here we assume a depth of zero. If a (later) branch to this label
                // has a positive stack depth, we'll record that as the new depth and add the delta into
                // m_depthAdjustment.
                if (m_curDepth < 0)
                    m_curDepth = 0;
                m_labelList[labelIndex].m_depth = m_curDepth;
            }
            else if (depth < m_curDepth)
            {
                // A branch location with smaller stack targets this label. In this case, the IL is
                // invalid, but we just compensate for it.
                m_depthAdjustment += m_curDepth - depth;
                m_labelList[labelIndex].m_depth = m_curDepth;
            }
            else if (depth > m_curDepth)
            {
                // Either the current depth is unknown, or a branch location with larger stack targets
                // this label, so the IL is invalid. In either case, just adjust the current depth.
                m_curDepth = depth;
            }
        }

        #endregion

        #region IL Macros
        public virtual void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
        {
            // Emits the il to throw an exception

            ArgumentNullException.ThrowIfNull(excType);

            if (!excType.IsSubclassOf(typeof(Exception)) && excType != typeof(Exception))
            {
                throw new ArgumentException(SR.Argument_NotExceptionType);
            }
            ConstructorInfo? con = excType.GetConstructor(Type.EmptyTypes);
            if (con == null)
            {
                throw new ArgumentException(SR.Argument_MissingDefaultConstructor);
            }
            Emit(OpCodes.Newobj, con);
            Emit(OpCodes.Throw);
        }

        private const string ConsoleTypeFullName = "System.Console, System.Console";

        public virtual void EmitWriteLine(string value)
        {
            // Emits the IL to call Console.WriteLine with a string.

            Emit(OpCodes.Ldstr, value);
            Type[] parameterTypes = new Type[1];
            parameterTypes[0] = typeof(string);
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo mi = consoleType.GetMethod("WriteLine", parameterTypes)!;
            Emit(OpCodes.Call, mi);
        }

        public virtual void EmitWriteLine(LocalBuilder localBuilder)
        {
            // Emits the IL necessary to call WriteLine with lcl.  It is
            // an error to call EmitWriteLine with a lcl which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the locals.

            if (m_methodBuilder == null)
            {
                throw new ArgumentException(SR.InvalidOperation_BadILGeneratorUsage);
            }

            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo prop = consoleType.GetMethod("get_Out")!;
            Emit(OpCodes.Call, prop);
            Emit(OpCodes.Ldloc, localBuilder);
            Type[] parameterTypes = new Type[1];
            Type cls = localBuilder.LocalType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new ArgumentException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = cls;
            MethodInfo? mi = typeof(System.IO.TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(SR.Argument_EmitWriteLineType, nameof(localBuilder));
            }

            Emit(OpCodes.Callvirt, mi);
        }

        public virtual void EmitWriteLine(FieldInfo fld)
        {
            ArgumentNullException.ThrowIfNull(fld);

            // Emits the IL necessary to call WriteLine with fld.  It is
            // an error to call EmitWriteLine with a fld which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the fields.

            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            MethodInfo prop = consoleType.GetMethod("get_Out")!;
            Emit(OpCodes.Call, prop);

            if ((fld.Attributes & FieldAttributes.Static) != 0)
            {
                Emit(OpCodes.Ldsfld, fld);
            }
            else
            {
                Emit(OpCodes.Ldarg_0); // Load the this ref.
                Emit(OpCodes.Ldfld, fld);
            }
            Type[] parameterTypes = new Type[1];
            Type cls = fld.FieldType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new NotSupportedException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = cls;
            MethodInfo? mi = typeof(System.IO.TextWriter).GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(SR.Argument_EmitWriteLineType, nameof(fld));
            }

            Emit(OpCodes.Callvirt, mi);
        }

        #endregion

        #region Debug API
        public virtual LocalBuilder DeclareLocal(Type localType)
        {
            return DeclareLocal(localType, false);
        }

        public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            // Declare a local of type "local". The current active lexical scope
            // will be the scope that local will live.

            MethodBuilder? methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();

            if (methodBuilder.IsTypeCreated())
            {
                // cannot change method after its containing type has been created
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
            }

            ArgumentNullException.ThrowIfNull(localType);

            if (methodBuilder.m_bIsBaked)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MethodBaked);
            }

            // add the localType to local signature
            m_localSignature.AddArgument(localType, pinned);

            return new LocalBuilder(m_localCount++, localType, methodBuilder, pinned);
        }

        public virtual void UsingNamespace(string usingNamespace)
        {
            // Specifying the namespace to be used in evaluating locals and watches
            // for the current active lexical scope.

            ArgumentException.ThrowIfNullOrEmpty(usingNamespace);

            MethodBuilder? methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();

            int index = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
            if (index == -1)
            {
                methodBuilder.m_localSymInfo!.AddUsingNamespace(usingNamespace);
            }
            else
            {
                m_ScopeTree.AddUsingNamespaceToCurrentScope(usingNamespace);
            }
        }

        public virtual void BeginScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Open, m_length);
        }

        public virtual void EndScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Close, m_length);
        }

        public virtual int ILOffset => m_length;

        #endregion

        #endregion
    }

    internal struct __LabelInfo
    {
        internal int m_pos; // Position in the il stream, with -1 meaning unknown.
        internal int m_depth; // Stack depth, with -1 meaning unknown.
    }

    internal struct __FixupData
    {
        internal Label m_fixupLabel;
        internal int m_fixupPos;

        internal int m_fixupInstSize;
    }

    internal sealed class __ExceptionInfo
    {
        internal const int None = 0x0000;  // COR_ILEXCEPTION_CLAUSE_NONE
        internal const int Filter = 0x0001;  // COR_ILEXCEPTION_CLAUSE_FILTER
        internal const int Finally = 0x0002;  // COR_ILEXCEPTION_CLAUSE_FINALLY
        internal const int Fault = 0x0004;  // COR_ILEXCEPTION_CLAUSE_FAULT
        internal const int PreserveStack = 0x0004;  // COR_ILEXCEPTION_CLAUSE_PRESERVESTACK

        internal const int State_Try = 0;
        internal const int State_Filter = 1;
        internal const int State_Catch = 2;
        internal const int State_Finally = 3;
        internal const int State_Fault = 4;
        internal const int State_Done = 5;

        internal int m_startAddr;
        internal int[] m_filterAddr;
        internal int[] m_catchAddr;
        internal int[] m_catchEndAddr;
        internal int[] m_type;
        internal Type[] m_catchClass;
        internal Label m_endLabel;
        internal Label m_finallyEndLabel;
        internal int m_endAddr;
        internal int m_endFinally;
        internal int m_currentCatch;

        private int m_currentState;

        internal __ExceptionInfo(int startAddr, Label endLabel)
        {
            m_startAddr = startAddr;
            m_endAddr = -1;
            m_filterAddr = new int[4];
            m_catchAddr = new int[4];
            m_catchEndAddr = new int[4];
            m_catchClass = new Type[4];
            m_currentCatch = 0;
            m_endLabel = endLabel;
            m_type = new int[4];
            m_endFinally = -1;
            m_currentState = State_Try;
        }

        private void MarkHelper(
            int catchorfilterAddr,      // the starting address of a clause
            int catchEndAddr,           // the end address of a previous catch clause. Only use when finally is following a catch
            Type? catchClass,             // catch exception type
            int type)                   // kind of clause
        {
            int currentCatch = m_currentCatch;
            if (currentCatch >= m_catchAddr.Length)
            {
                m_filterAddr = ILGenerator.EnlargeArray(m_filterAddr);
                m_catchAddr = ILGenerator.EnlargeArray(m_catchAddr);
                m_catchEndAddr = ILGenerator.EnlargeArray(m_catchEndAddr);
                m_catchClass = ILGenerator.EnlargeArray(m_catchClass);
                m_type = ILGenerator.EnlargeArray(m_type);
            }
            if (type == Filter)
            {
                m_type[currentCatch] = type;
                m_filterAddr[currentCatch] = catchorfilterAddr;
                m_catchAddr[currentCatch] = -1;
                if (currentCatch > 0)
                {
                    Debug.Assert(m_catchEndAddr[currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                    m_catchEndAddr[currentCatch - 1] = catchorfilterAddr;
                }
            }
            else
            {
                // catch or Fault clause
                m_catchClass[currentCatch] = catchClass!;
                if (m_type[currentCatch] != Filter)
                {
                    m_type[currentCatch] = type;
                }
                m_catchAddr[currentCatch] = catchorfilterAddr;
                if (currentCatch > 0)
                {
                    if (m_type[currentCatch] != Filter)
                    {
                        Debug.Assert(m_catchEndAddr[currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                        m_catchEndAddr[currentCatch - 1] = catchEndAddr;
                    }
                }
                m_catchEndAddr[currentCatch] = -1;
                m_currentCatch++;
            }

            if (m_endAddr == -1)
            {
                m_endAddr = catchorfilterAddr;
            }
        }

        internal void MarkFilterAddr(int filterAddr)
        {
            m_currentState = State_Filter;
            MarkHelper(filterAddr, filterAddr, null, Filter);
        }

        internal void MarkFaultAddr(int faultAddr)
        {
            m_currentState = State_Fault;
            MarkHelper(faultAddr, faultAddr, null, Fault);
        }

        internal void MarkCatchAddr(int catchAddr, Type? catchException)
        {
            m_currentState = State_Catch;
            MarkHelper(catchAddr, catchAddr, catchException, None);
        }

        internal void MarkFinallyAddr(int finallyAddr, int endCatchAddr)
        {
            if (m_endFinally != -1)
            {
                throw new ArgumentException(SR.Argument_TooManyFinallyClause);
            }

            m_currentState = State_Finally;
            m_endFinally = finallyAddr;
            MarkHelper(finallyAddr, endCatchAddr, null, Finally);
        }

        internal void Done(int endAddr)
        {
            Debug.Assert(m_currentCatch > 0, "m_currentCatch > 0");
            Debug.Assert(m_catchAddr[m_currentCatch - 1] > 0, "m_catchAddr[m_currentCatch-1] > 0");
            Debug.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
            m_catchEndAddr[m_currentCatch - 1] = endAddr;
            m_currentState = State_Done;
        }

        internal int GetStartAddress()
        {
            return m_startAddr;
        }

        internal int GetEndAddress()
        {
            return m_endAddr;
        }

        internal int GetFinallyEndAddress()
        {
            return m_endFinally;
        }

        internal Label GetEndLabel()
        {
            return m_endLabel;
        }

        internal int[] GetFilterAddresses()
        {
            return m_filterAddr;
        }

        internal int[] GetCatchAddresses()
        {
            return m_catchAddr;
        }

        internal int[] GetCatchEndAddresses()
        {
            return m_catchEndAddr;
        }

        internal Type[] GetCatchClass()
        {
            return m_catchClass;
        }

        internal int GetNumberOfCatches()
        {
            return m_currentCatch;
        }

        internal int[] GetExceptionTypes()
        {
            return m_type;
        }

        internal void SetFinallyEndLabel(Label lbl)
        {
            m_finallyEndLabel = lbl;
        }

        internal Label GetFinallyEndLabel()
        {
            return m_finallyEndLabel;
        }

        // Specifies whether exc is an inner exception for "this".  The way
        // its determined is by comparing the end address for the last catch
        // clause for both exceptions.  If they're the same, the start address
        // for the exception is compared.
        // WARNING: This is not a generic function to determine the innerness
        // of an exception.  This is somewhat of a mis-nomer.  This gives a
        // random result for cases where the two exceptions being compared do
        // not having a nesting relation.
        internal bool IsInner(__ExceptionInfo exc)
        {
            Debug.Assert(exc != null);
            Debug.Assert(m_currentCatch > 0, "m_currentCatch > 0");
            Debug.Assert(exc.m_currentCatch > 0, "exc.m_currentCatch > 0");

            int exclast = exc.m_currentCatch - 1;
            int last = m_currentCatch - 1;

            if (exc.m_catchEndAddr[exclast] < m_catchEndAddr[last])
                return true;

            if (exc.m_catchEndAddr[exclast] != m_catchEndAddr[last])
                return false;
            Debug.Assert(exc.GetEndAddress() != GetEndAddress(),
                "exc.GetEndAddress() != GetEndAddress()");

            return exc.GetEndAddress() > GetEndAddress();
        }

        // 0 indicates in a try block
        // 1 indicates in a filter block
        // 2 indicates in a catch block
        // 3 indicates in a finally block
        // 4 indicates Done
        internal int GetCurrentState()
        {
            return m_currentState;
        }
    }

    /// <summary>
    /// Scope Tree is a class that track the scope structure within a method body
    /// It keeps track two parallel array. m_ScopeAction keeps track the action. It can be
    /// OpenScope or CloseScope. m_iOffset records the offset where the action
    /// takes place.
    /// </summary>
    internal enum ScopeAction : sbyte
    {
        Open = -0x1,
        Close = 0x1
    }

    internal sealed class ScopeTree
    {
        internal ScopeTree()
        {
            // initialize data variables
            m_iOpenScopeCount = 0;
            m_iCount = 0;
        }

        /// <summary>
        /// Find the current active lexical scope. For example, if we have
        /// "Open Open Open Close",
        /// we will return 1 as the second BeginScope is currently active.
        /// </summary>
        internal int GetCurrentActiveScopeIndex()
        {
            if (m_iCount == 0)
            {
                return -1;
            }

            int i = m_iCount - 1;

            for (int cClose = 0; cClose > 0 || m_ScopeActions[i] == ScopeAction.Close; i--)
            {
                cClose += (sbyte)m_ScopeActions[i];
            }

            return i;
        }

        internal void AddLocalSymInfoToCurrentScope(
            string strName,
            byte[] signature,
            int slot,
            int startOffset,
            int endOffset)
        {
            int i = GetCurrentActiveScopeIndex();
            m_localSymInfos[i] ??= new LocalSymInfo();
            m_localSymInfos[i]!.AddLocalSymInfo(strName, signature, slot, startOffset, endOffset);
        }

        internal void AddUsingNamespaceToCurrentScope(string strNamespace)
        {
            int i = GetCurrentActiveScopeIndex();
            m_localSymInfos[i] ??= new LocalSymInfo();
            m_localSymInfos[i]!.AddUsingNamespace(strNamespace);
        }

        internal void AddScopeInfo(ScopeAction sa, int iOffset)
        {
            if (sa == ScopeAction.Close && m_iOpenScopeCount <= 0)
            {
                throw new ArgumentException(SR.Argument_UnmatchingSymScope);
            }

            // make sure that arrays are large enough to hold addition info
            EnsureCapacity();

            m_ScopeActions[m_iCount] = sa;
            m_iOffsets[m_iCount] = iOffset;
            m_localSymInfos[m_iCount] = null;
            checked { m_iCount++; }

            m_iOpenScopeCount += -(sbyte)sa;
        }

        /// <summary>
        /// Helper to ensure arrays are large enough
        /// </summary>
        internal void EnsureCapacity()
        {
            if (m_iCount == 0)
            {
                // First time. Allocate the arrays.
                m_iOffsets = new int[InitialSize];
                m_ScopeActions = new ScopeAction[InitialSize];
                m_localSymInfos = new LocalSymInfo[InitialSize];
            }
            else if (m_iCount == m_iOffsets.Length)
            {
                // the arrays are full. Enlarge the arrays
                // It would probably be simpler to just use Lists here.
                int newSize = checked(m_iCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iOffsets, temp, m_iCount);
                m_iOffsets = temp;

                ScopeAction[] tempSA = new ScopeAction[newSize];
                Array.Copy(m_ScopeActions, tempSA, m_iCount);
                m_ScopeActions = tempSA;

                LocalSymInfo[] tempLSI = new LocalSymInfo[newSize];
                Array.Copy(m_localSymInfos, tempLSI, m_iCount);
                m_localSymInfos = tempLSI;
            }
        }

        internal int[] m_iOffsets = null!;                 // array of offsets
        internal ScopeAction[] m_ScopeActions = null!;             // array of scope actions
        internal int m_iCount;                   // how many entries in the arrays are occupied
        internal int m_iOpenScopeCount;          // keep track how many scopes are open
        internal const int InitialSize = 16;
        internal LocalSymInfo?[] m_localSymInfos = null!;            // keep track debugging local information
    }
}
