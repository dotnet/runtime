// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//

using System;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Reflection.Emit
{
    public class ILGenerator
    {
        #region Const Members
        private const int defaultSize = 16;
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
            Array.Copy(incoming, 0, temp, 0, incoming.Length);
            return temp;
        }

        private static byte[] EnlargeArray(byte[] incoming)
        {
            return EnlargeArray(incoming, incoming.Length * 2);
        }

        private static byte[] EnlargeArray(byte[] incoming, int requiredSize)
        {
            Debug.Assert(incoming != null);

            byte[] temp = new byte[requiredSize];
            Buffer.BlockCopy(incoming, 0, temp, 0, incoming.Length);
            return temp;
        }
        #endregion

        #region Internal Data Members
        private int m_length;
        private byte[] m_ILStream;

        private int[] m_labelList;
        private int m_labelCount;

        private __FixupData[] m_fixupData;

        private int m_fixupCount;

        private int[] m_RelocFixupList;
        private int m_RelocFixupCount;

        private int m_exceptionCount;
        private int m_currExcStackCount;
        private __ExceptionInfo[] m_exceptions;           //This is the list of all of the exceptions in this ILStream.
        private __ExceptionInfo[] m_currExcStack;         //This is the stack of exceptions which we're currently in.

        internal ScopeTree m_ScopeTree;            // this variable tracks all debugging scope information
        internal LineNumberInfo m_LineNumberInfo;       // this variable tracks all line number information

        internal MethodInfo m_methodBuilder;
        internal int m_localCount;
        internal SignatureHelper m_localSignature;

        private int m_maxStackSize = 0;     // Maximum stack size not counting the exceptions.

        private int m_maxMidStack = 0;      // Maximum stack size for a given basic block.
        private int m_maxMidStackCur = 0;   // Running count of the maximum stack size for the current basic block.

        internal int CurrExcStackCount
        {
            get { return m_currExcStackCount; }
        }

        internal __ExceptionInfo[] CurrExcStack
        {
            get { return m_currExcStack; }
        }
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

            if (size < defaultSize)
            {
                m_ILStream = new byte[defaultSize];
            }
            else
            {
                m_ILStream = new byte[size];
            }

            m_length = 0;

            m_labelCount = 0;
            m_fixupCount = 0;
            m_labelList = null;

            m_fixupData = null;

            m_exceptions = null;
            m_exceptionCount = 0;
            m_currExcStack = null;
            m_currExcStackCount = 0;

            m_RelocFixupList = null;
            m_RelocFixupCount = 0;

            // initialize the scope tree
            m_ScopeTree = new ScopeTree();
            m_LineNumberInfo = new LineNumberInfo();
            m_methodBuilder = methodBuilder;

            // initialize local signature
            m_localCount = 0;
            MethodBuilder mb = m_methodBuilder as MethodBuilder;
            if (mb == null)
                m_localSignature = SignatureHelper.GetLocalVarSigHelper(null);
            else
                m_localSignature = SignatureHelper.GetLocalVarSigHelper(mb.GetTypeBuilder().Module);
        }

        #endregion

        #region Internal Members
        internal virtual void RecordTokenFixup()
        {
            if (m_RelocFixupList == null)
                m_RelocFixupList = new int[DefaultFixupArraySize];
            else if (m_RelocFixupList.Length <= m_RelocFixupCount)
                m_RelocFixupList = EnlargeArray(m_RelocFixupList);

            m_RelocFixupList[m_RelocFixupCount++] = m_length;
        }

        internal void InternalEmit(OpCode opcode)
        {
            if (opcode.Size != 1)
            {
                m_ILStream[m_length++] = (byte)(opcode.Value >> 8);
            }

            m_ILStream[m_length++] = (byte)opcode.Value;

            UpdateStackSize(opcode, opcode.StackChange());
        }

        internal void UpdateStackSize(OpCode opcode, int stackchange)
        {
            // Updates internal variables for keeping track of the stack size
            // requirements for the function.  stackchange specifies the amount
            // by which the stacksize needs to be updated.

            // Special case for the Return.  Returns pops 1 if there is a
            // non-void return value.

            // Update the running stacksize.  m_maxMidStack specifies the maximum
            // amount of stack required for the current basic block irrespective of
            // where you enter the block.
            m_maxMidStackCur += stackchange;
            if (m_maxMidStackCur > m_maxMidStack)
                m_maxMidStack = m_maxMidStackCur;
            else if (m_maxMidStackCur < 0)
                m_maxMidStackCur = 0;

            // If the current instruction signifies end of a basic, which basically
            // means an unconditional branch, add m_maxMidStack to m_maxStackSize.
            // m_maxStackSize will eventually be the sum of the stack requirements for
            // each basic block.
            if (opcode.EndsUncondJmpBlk())
            {
                m_maxStackSize += m_maxMidStack;
                m_maxMidStack = 0;
                m_maxMidStackCur = 0;
            }
        }

        private int GetMethodToken(MethodBase method, Type[] optionalParameterTypes, bool useMethodDef)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMethodTokenInternal(method, optionalParameterTypes, useMethodDef);
        }

        internal virtual SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType,
            Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            return GetMemberRefSignature(call, returnType, parameterTypes, optionalParameterTypes, 0);
        }

        private SignatureHelper GetMemberRefSignature(CallingConventions call, Type returnType,
            Type[] parameterTypes, Type[] optionalParameterTypes, int cGenericParameters)
        {
            return ((ModuleBuilder)m_methodBuilder.Module).GetMemberRefSignature(call, returnType, parameterTypes, optionalParameterTypes, cGenericParameters);
        }

        internal byte[] BakeByteArray()
        {
            // BakeByteArray is an internal function designed to be called by MethodBuilder to do
            // all of the fixups and return a new byte array representing the byte stream with labels resolved, etc.

            int newSize;
            int updateAddr;
            byte[] newBytes;

            if (m_currExcStackCount != 0)
            {
                throw new ArgumentException(SR.Argument_UnclosedExceptionBlock);
            }
            if (m_length == 0)
                return null;

            //Calculate the size of the new array.
            newSize = m_length;

            //Allocate space for the new array.
            newBytes = new byte[newSize];

            //Copy the data from the old array
            Buffer.BlockCopy(m_ILStream, 0, newBytes, 0, newSize);

            //Do the fixups.
            //This involves iterating over all of the labels and
            //replacing them with their proper values.
            for (int i = 0; i < m_fixupCount; i++)
            {
                updateAddr = GetLabelPos(m_fixupData[i].m_fixupLabel) - (m_fixupData[i].m_fixupPos + m_fixupData[i].m_fixupInstSize);

                //Handle single byte instructions
                //Throw an exception if they're trying to store a jump in a single byte instruction that doesn't fit.
                if (m_fixupData[i].m_fixupInstSize == 1)
                {
                    //Verify that our one-byte arg will fit into a Signed Byte.
                    if (updateAddr < sbyte.MinValue || updateAddr > sbyte.MaxValue)
                    {
                        throw new NotSupportedException(SR.Format(SR.NotSupported_IllegalOneByteBranch, m_fixupData[i].m_fixupPos, updateAddr));
                    }

                    //Place the one-byte arg
                    if (updateAddr < 0)
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)(256 + updateAddr);
                    }
                    else
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)updateAddr;
                    }
                }
                else
                {
                    //Place the four-byte arg
                    PutInteger4InArray(updateAddr, m_fixupData[i].m_fixupPos, newBytes);
                }
            }
            return newBytes;
        }

        internal __ExceptionInfo[] GetExceptions()
        {
            __ExceptionInfo[] temp;
            if (m_currExcStackCount != 0)
            {
                throw new NotSupportedException(SR.Argument_UnclosedExceptionBlock);
            }

            if (m_exceptionCount == 0)
            {
                return null;
            }

            temp = new __ExceptionInfo[m_exceptionCount];
            Array.Copy(m_exceptions, 0, temp, 0, m_exceptionCount);
            SortExceptions(temp);
            return temp;
        }

        internal void EnsureCapacity(int size)
        {
            // Guarantees an array capable of holding at least size elements.
            if (m_length + size >= m_ILStream.Length)
            {
                if (m_length + size >= 2 * m_ILStream.Length)
                {
                    m_ILStream = EnlargeArray(m_ILStream, m_length + size);
                }
                else
                {
                    m_ILStream = EnlargeArray(m_ILStream);
                }
            }
        }

        internal void PutInteger4(int value)
        {
            m_length = PutInteger4InArray(value, m_length, m_ILStream);
        }

        private static int PutInteger4InArray(int value, int startPos, byte[] array)
        {
            // Puts an Int32 onto the stream. This is an internal routine, so it does not do any error checking.

            array[startPos++] = (byte)value;
            array[startPos++] = (byte)(value >> 8);
            array[startPos++] = (byte)(value >> 16);
            array[startPos++] = (byte)(value >> 24);
            return startPos;
        }

        private int GetLabelPos(Label lbl)
        {
            // Gets the position in the stream of a particular label.
            // Verifies that the label exists and that it has been given a value.

            int index = lbl.GetLabelValue();

            if (index < 0 || index >= m_labelCount)
                throw new ArgumentException(SR.Argument_BadLabel);

            if (m_labelList[index] < 0)
                throw new ArgumentException(SR.Argument_BadLabelContent);

            return m_labelList[index];
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

            m_fixupData[m_fixupCount].m_fixupPos = pos;
            m_fixupData[m_fixupCount].m_fixupLabel = lbl;
            m_fixupData[m_fixupCount].m_fixupInstSize = instSize;

            m_fixupCount++;
        }

        internal int GetMaxStackSize()
        {
            return m_maxStackSize;
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

        internal int[] GetTokenFixups()
        {
            if (m_RelocFixupCount == 0)
            {
                Debug.Assert(m_RelocFixupList == null);
                return null;
            }

            int[] narrowTokens = new int[m_RelocFixupCount];
            Array.Copy(m_RelocFixupList, 0, narrowTokens, 0, m_RelocFixupCount);
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
            if (arg < 0)
            {
                m_ILStream[m_length++] = (byte)(256 + arg);
            }
            else
            {
                m_ILStream[m_length++] = (byte)arg;
            }
        }

        public virtual void Emit(OpCode opcode, short arg)
        {
            // Puts opcode onto the stream of instructions followed by arg
            EnsureCapacity(5);
            InternalEmit(opcode);
            m_ILStream[m_length++] = (byte)arg;
            m_ILStream[m_length++] = (byte)(arg >> 8);
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            // Puts opcode onto the stream of instructions followed by arg
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(arg);
        }

        public virtual void Emit(OpCode opcode, MethodInfo meth)
        {
            if (meth == null)
                throw new ArgumentNullException(nameof(meth));

            if (opcode.Equals(OpCodes.Call) || opcode.Equals(OpCodes.Callvirt) || opcode.Equals(OpCodes.Newobj))
            {
                EmitCall(opcode, meth, null);
            }
            else
            {
                int stackchange = 0;

                // Reflection doesn't distinguish between these two concepts:
                //   1. A generic method definition: Foo`1
                //   2. A generic method definition instantiated over its own generic arguments: Foo`1<!!0>
                // In RefEmit, we always want 1 for Ld* opcodes and 2 for Call* and Newobj.
                bool useMethodDef = opcode.Equals(OpCodes.Ldtoken) || opcode.Equals(OpCodes.Ldftn) || opcode.Equals(OpCodes.Ldvirtftn);
                int tk = GetMethodToken(meth, null, useMethodDef);

                EnsureCapacity(7);
                InternalEmit(opcode);

                UpdateStackSize(opcode, stackchange);
                RecordTokenFixup();
                PutInteger4(tk);
            }
        }


        public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention,
            Type returnType, Type[] parameterTypes, Type[] optionalParameterTypes)
        {
            int stackchange = 0;
            SignatureHelper sig;
            if (optionalParameterTypes != null)
            {
                if ((callingConvention & CallingConventions.VarArgs) == 0)
                {
                    // Client should not supply optional parameter in default calling convention
                    throw new InvalidOperationException(SR.InvalidOperation_NotAVarArgCallingConvention);
                }
            }

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            sig = GetMemberRefSignature(callingConvention,
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
            PutInteger4(modBuilder.GetSignatureToken(sig).Token);
        }

        public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type returnType, Type[] parameterTypes)
        {
            int stackchange = 0;
            int cParams = 0;
            int i;
            SignatureHelper sig;

            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;

            if (parameterTypes != null)
            {
                cParams = parameterTypes.Length;
            }

            sig = SignatureHelper.GetMethodSigHelper(
                modBuilder,
                unmanagedCallConv,
                returnType);

            if (parameterTypes != null)
            {
                for (i = 0; i < cParams; i++)
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
            PutInteger4(modBuilder.GetSignatureToken(sig).Token);
        }

        public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[] optionalParameterTypes)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));

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
            if (!(methodInfo is SymbolMethod) && methodInfo.IsStatic == false && !(opcode.Equals(OpCodes.Newobj)))
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
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));

            int stackchange = 0;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            SignatureToken sig = modBuilder.GetSignatureToken(signature);

            int tempVal = sig.Token;

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
            if (con == null)
                throw new ArgumentNullException(nameof(con));

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

            int tempVal = 0;
            ModuleBuilder modBuilder = (ModuleBuilder)m_methodBuilder.Module;
            if (opcode == OpCodes.Ldtoken && cls != null && cls.IsGenericTypeDefinition)
            {
                // This gets the token for the generic type definition if cls is one.
                tempVal = modBuilder.GetTypeToken(cls).Token;
            }
            else
            {
                // This gets the token for the generic type instantiated on the formal parameters
                // if cls is a generic type definition.
                tempVal = modBuilder.GetTypeTokenInternal(cls).Token;
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
            m_ILStream[m_length++] = (byte)arg;
            m_ILStream[m_length++] = (byte)(arg >> 8);
            m_ILStream[m_length++] = (byte)(arg >> 16);
            m_ILStream[m_length++] = (byte)(arg >> 24);
            m_ILStream[m_length++] = (byte)(arg >> 32);
            m_ILStream[m_length++] = (byte)(arg >> 40);
            m_ILStream[m_length++] = (byte)(arg >> 48);
            m_ILStream[m_length++] = (byte)(arg >> 56);
        }

        public unsafe virtual void Emit(OpCode opcode, float arg)
        {
            EnsureCapacity(7);
            InternalEmit(opcode);
            uint tempVal = *(uint*)&arg;
            m_ILStream[m_length++] = (byte)tempVal;
            m_ILStream[m_length++] = (byte)(tempVal >> 8);
            m_ILStream[m_length++] = (byte)(tempVal >> 16);
            m_ILStream[m_length++] = (byte)(tempVal >> 24);
        }

        public unsafe virtual void Emit(OpCode opcode, double arg)
        {
            EnsureCapacity(11);
            InternalEmit(opcode);
            ulong tempVal = *(ulong*)&arg;
            m_ILStream[m_length++] = (byte)tempVal;
            m_ILStream[m_length++] = (byte)(tempVal >> 8);
            m_ILStream[m_length++] = (byte)(tempVal >> 16);
            m_ILStream[m_length++] = (byte)(tempVal >> 24);
            m_ILStream[m_length++] = (byte)(tempVal >> 32);
            m_ILStream[m_length++] = (byte)(tempVal >> 40);
            m_ILStream[m_length++] = (byte)(tempVal >> 48);
            m_ILStream[m_length++] = (byte)(tempVal >> 56);
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

            int tempVal = label.GetLabelValue();
            EnsureCapacity(7);


            InternalEmit(opcode);
            if (OpCodes.TakesSingleByteArgument(opcode))
            {
                AddFixup(label, m_length, 1);
                m_length++;
            }
            else
            {
                AddFixup(label, m_length, 4);
                m_length += 4;
            }
        }

        public virtual void Emit(OpCode opcode, Label[] labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            // Emitting a switch table

            int i;
            int remaining;                  // number of bytes remaining for this switch instruction to be substracted
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
            int tempVal = modBuilder.GetFieldToken(field).Token;
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
            int tempVal = modBuilder.GetStringConstant(str).Token;
            EnsureCapacity(7);
            InternalEmit(opcode);
            PutInteger4(tempVal);
        }

        public virtual void Emit(OpCode opcode, LocalBuilder local)
        {
            // Puts the opcode onto the IL stream followed by the information for local variable local.

            if (local == null)
            {
                throw new ArgumentNullException(nameof(local));
            }
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
            else if (!OpCodes.TakesSingleByteArgument(opcode))
            {
                m_ILStream[m_length++] = (byte)tempVal;
                m_ILStream[m_length++] = (byte)(tempVal >> 8);
            }
            else
            {
                //Handle stloc_1, ldloc_1
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
            if (m_exceptions == null)
            {
                m_exceptions = new __ExceptionInfo[DefaultExceptionArraySize];
            }

            if (m_currExcStack == null)
            {
                m_currExcStack = new __ExceptionInfo[DefaultExceptionArraySize];
            }

            if (m_exceptionCount >= m_exceptions.Length)
            {
                m_exceptions = EnlargeArray(m_exceptions);
            }

            if (m_currExcStackCount >= m_currExcStack.Length)
            {
                m_currExcStack = EnlargeArray(m_currExcStack);
            }

            Label endLabel = DefineLabel();
            __ExceptionInfo exceptionInfo = new __ExceptionInfo(m_length, endLabel);

            // add the exception to the tracking list
            m_exceptions[m_exceptionCount++] = exceptionInfo;

            // Make this exception the current active exception
            m_currExcStack[m_currExcStackCount++] = exceptionInfo;
            return endLabel;
        }

        public virtual void EndExceptionBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }

            // Pop the current exception block
            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            m_currExcStack[m_currExcStackCount - 1] = null;
            m_currExcStackCount--;

            Label endLabel = current.GetEndLabel();
            int state = current.GetCurrentState();

            if (state == __ExceptionInfo.State_Filter ||
                state == __ExceptionInfo.State_Try)
            {
                throw new InvalidOperationException(SR.Argument_BadExceptionCodeGen);
            }

            if (state == __ExceptionInfo.State_Catch)
            {
                this.Emit(OpCodes.Leave, endLabel);
            }
            else if (state == __ExceptionInfo.State_Finally || state == __ExceptionInfo.State_Fault)
            {
                this.Emit(OpCodes.Endfinally);
            }

            //Check if we've already set this label.
            //The only reason why we might have set this is if we have a finally block.
            if (m_labelList[endLabel.GetLabelValue()] == -1)
            {
                MarkLabel(endLabel);
            }
            else
            {
                MarkLabel(current.GetFinallyEndLabel());
            }

            current.Done(m_length);
        }

        public virtual void BeginExceptFilterBlock()
        {
            // Begins an exception filter block.  Emits a branch instruction to the end of the current exception block.

            if (m_currExcStackCount == 0)
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);

            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];

            Label endLabel = current.GetEndLabel();
            this.Emit(OpCodes.Leave, endLabel);

            current.MarkFilterAddr(m_length);
        }

        public virtual void BeginCatchBlock(Type exceptionType)
        {
            // Begins a catch block.  Emits a branch instruction to the end of the current exception block.

            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];

            if (current.GetCurrentState() == __ExceptionInfo.State_Filter)
            {
                if (exceptionType != null)
                {
                    throw new ArgumentException(SR.Argument_ShouldNotSpecifyExceptionType);
                }

                this.Emit(OpCodes.Endfilter);
            }
            else
            {
                // execute this branch if previous clause is Catch or Fault
                if (exceptionType == null)
                {
                    throw new ArgumentNullException(nameof(exceptionType));
                }

                Label endLabel = current.GetEndLabel();
                this.Emit(OpCodes.Leave, endLabel);
            }

            current.MarkCatchAddr(m_length, exceptionType);
        }

        public virtual void BeginFaultBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];

            // emit the leave for the clause before this one.
            Label endLabel = current.GetEndLabel();
            this.Emit(OpCodes.Leave, endLabel);

            current.MarkFaultAddr(m_length);
        }

        public virtual void BeginFinallyBlock()
        {
            if (m_currExcStackCount == 0)
            {
                throw new NotSupportedException(SR.Argument_NotInExceptionBlock);
            }
            __ExceptionInfo current = m_currExcStack[m_currExcStackCount - 1];
            int state = current.GetCurrentState();
            Label endLabel = current.GetEndLabel();
            int catchEndAddr = 0;
            if (state != __ExceptionInfo.State_Try)
            {
                // generate leave for any preceeding catch clause
                this.Emit(OpCodes.Leave, endLabel);
                catchEndAddr = m_length;
            }

            MarkLabel(endLabel);


            Label finallyEndLabel = this.DefineLabel();
            current.SetFinallyEndLabel(finallyEndLabel);

            // generate leave for try clause
            this.Emit(OpCodes.Leave, finallyEndLabel);
            if (catchEndAddr == 0)
                catchEndAddr = m_length;
            current.MarkFinallyAddr(m_length, catchEndAddr);
        }

        #endregion

        #region Labels
        public virtual Label DefineLabel()
        {
            // Declares a new Label.  This is just a token and does not yet represent any particular location
            // within the stream.  In order to set the position of the label within the stream, you must call
            // Mark Label.

            // Delay init the lable array in case we dont use it
            if (m_labelList == null)
            {
                m_labelList = new int[DefaultLabelArraySize];
            }

            if (m_labelCount >= m_labelList.Length)
            {
                m_labelList = EnlargeArray(m_labelList);
            }
            m_labelList[m_labelCount] = -1;
            return new Label(m_labelCount++);
        }

        public virtual void MarkLabel(Label loc)
        {
            // Defines a label by setting the position where that label is found within the stream.
            // Does not allow a label to be defined more than once.

            int labelIndex = loc.GetLabelValue();

            //This should never happen.
            if (labelIndex < 0 || labelIndex >= m_labelList.Length)
            {
                throw new ArgumentException(SR.Argument_InvalidLabel);
            }

            if (m_labelList[labelIndex] != -1)
            {
                throw new ArgumentException(SR.Argument_RedefinedLabel);
            }

            m_labelList[labelIndex] = m_length;
        }

        #endregion

        #region IL Macros
        public virtual void ThrowException(Type excType)
        {
            // Emits the il to throw an exception

            if (excType == null)
            {
                throw new ArgumentNullException(nameof(excType));
            }

            if (!excType.IsSubclassOf(typeof(Exception)) && excType != typeof(Exception))
            {
                throw new ArgumentException(SR.Argument_NotExceptionType);
            }
            ConstructorInfo con = excType.GetConstructor(Type.EmptyTypes);
            if (con == null)
            {
                throw new ArgumentException(SR.Argument_MissingDefaultConstructor);
            }
            this.Emit(OpCodes.Newobj, con);
            this.Emit(OpCodes.Throw);
        }

        private static Type GetConsoleType()
        {
            return Type.GetType("System.Console, System.Console", throwOnError: true);
        }

        public virtual void EmitWriteLine(string value)
        {
            // Emits the IL to call Console.WriteLine with a string.

            Emit(OpCodes.Ldstr, value);
            Type[] parameterTypes = new Type[1];
            parameterTypes[0] = typeof(string);
            MethodInfo mi = GetConsoleType().GetMethod("WriteLine", parameterTypes);
            Emit(OpCodes.Call, mi);
        }

        public virtual void EmitWriteLine(LocalBuilder localBuilder)
        {
            // Emits the IL necessary to call WriteLine with lcl.  It is
            // an error to call EmitWriteLine with a lcl which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the locals.

            object cls;
            if (m_methodBuilder == null)
            {
                throw new ArgumentException(SR.InvalidOperation_BadILGeneratorUsage);
            }

            MethodInfo prop = GetConsoleType().GetMethod("get_Out");
            Emit(OpCodes.Call, prop);
            Emit(OpCodes.Ldloc, localBuilder);
            Type[] parameterTypes = new Type[1];
            cls = localBuilder.LocalType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new ArgumentException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = (Type)cls;
            MethodInfo mi = prop.ReturnType.GetMethod("WriteLine", parameterTypes);
            if (mi == null)
            {
                throw new ArgumentException(SR.Argument_EmitWriteLineType, nameof(localBuilder));
            }

            Emit(OpCodes.Callvirt, mi);
        }

        public virtual void EmitWriteLine(FieldInfo fld)
        {
            // Emits the IL necessary to call WriteLine with fld.  It is
            // an error to call EmitWriteLine with a fld which is not of
            // one of the types for which Console.WriteLine implements overloads. (e.g.
            // we do *not* call ToString on the fields.

            object cls;

            if (fld == null)
            {
                throw new ArgumentNullException(nameof(fld));
            }

            MethodInfo prop = GetConsoleType().GetMethod("get_Out");
            Emit(OpCodes.Call, prop);

            if ((fld.Attributes & FieldAttributes.Static) != 0)
            {
                Emit(OpCodes.Ldsfld, fld);
            }
            else
            {
                Emit(OpCodes.Ldarg, (short)0); //Load the this ref.
                Emit(OpCodes.Ldfld, fld);
            }
            Type[] parameterTypes = new Type[1];
            cls = fld.FieldType;
            if (cls is TypeBuilder || cls is EnumBuilder)
            {
                throw new NotSupportedException(SR.NotSupported_OutputStreamUsingTypeBuilder);
            }
            parameterTypes[0] = (Type)cls;
            MethodInfo mi = prop.ReturnType.GetMethod("WriteLine", parameterTypes);
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

            LocalBuilder localBuilder;

            MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();

            if (methodBuilder.IsTypeCreated())
            {
                // cannot change method after its containing type has been created
                throw new InvalidOperationException(SR.InvalidOperation_TypeHasBeenCreated);
            }

            if (localType == null)
            {
                throw new ArgumentNullException(nameof(localType));
            }

            if (methodBuilder.m_bIsBaked)
            {
                throw new InvalidOperationException(SR.InvalidOperation_MethodBaked);
            }

            // add the localType to local signature
            m_localSignature.AddArgument(localType, pinned);

            localBuilder = new LocalBuilder(m_localCount, localType, methodBuilder, pinned);
            m_localCount++;
            return localBuilder;
        }

        public virtual void UsingNamespace(string usingNamespace)
        {
            // Specifying the namespace to be used in evaluating locals and watches
            // for the current active lexical scope.

            if (usingNamespace == null)
                throw new ArgumentNullException(nameof(usingNamespace));

            if (usingNamespace.Length == 0)
                throw new ArgumentException(SR.Argument_EmptyName, nameof(usingNamespace));

            int index;
            MethodBuilder methodBuilder = m_methodBuilder as MethodBuilder;
            if (methodBuilder == null)
                throw new NotSupportedException();

            index = methodBuilder.GetILGenerator().m_ScopeTree.GetCurrentActiveScopeIndex();
            if (index == -1)
            {
                methodBuilder.m_localSymInfo.AddUsingNamespace(usingNamespace);
            }
            else
            {
                m_ScopeTree.AddUsingNamespaceToCurrentScope(usingNamespace);
            }
        }

        public virtual void MarkSequencePoint(
            ISymbolDocumentWriter document,
            int startLine,       // line number is 1 based
            int startColumn,     // column is 0 based
            int endLine,         // line number is 1 based
            int endColumn)       // column is 0 based
        {
            if (startLine == 0 || startLine < 0 || endLine == 0 || endLine < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startLine));
            }
            m_LineNumberInfo.AddLineNumberInfo(document, m_length, startLine, startColumn, endLine, endColumn);
        }

        public virtual void BeginScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Open, m_length);
        }

        public virtual void EndScope()
        {
            m_ScopeTree.AddScopeInfo(ScopeAction.Close, m_length);
        }

        public virtual int ILOffset
        {
            get
            {
                return m_length;
            }
        }

        #endregion

        #endregion
    }

    internal struct __FixupData
    {
        internal Label m_fixupLabel;
        internal int m_fixupPos;

        internal int m_fixupInstSize;
    }

    internal sealed class __ExceptionInfo
    {
        internal const int None = 0x0000;  //COR_ILEXCEPTION_CLAUSE_NONE
        internal const int Filter = 0x0001;  //COR_ILEXCEPTION_CLAUSE_FILTER
        internal const int Finally = 0x0002;  //COR_ILEXCEPTION_CLAUSE_FINALLY
        internal const int Fault = 0x0004;  //COR_ILEXCEPTION_CLAUSE_FAULT
        internal const int PreserveStack = 0x0004;  //COR_ILEXCEPTION_CLAUSE_PRESERVESTACK

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
            Type catchClass,             // catch exception type
            int type)                   // kind of clause
        {
            if (m_currentCatch >= m_catchAddr.Length)
            {
                m_filterAddr = ILGenerator.EnlargeArray(m_filterAddr);
                m_catchAddr = ILGenerator.EnlargeArray(m_catchAddr);
                m_catchEndAddr = ILGenerator.EnlargeArray(m_catchEndAddr);
                m_catchClass = ILGenerator.EnlargeArray(m_catchClass);
                m_type = ILGenerator.EnlargeArray(m_type);
            }
            if (type == Filter)
            {
                m_type[m_currentCatch] = type;
                m_filterAddr[m_currentCatch] = catchorfilterAddr;
                m_catchAddr[m_currentCatch] = -1;
                if (m_currentCatch > 0)
                {
                    Debug.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                    m_catchEndAddr[m_currentCatch - 1] = catchorfilterAddr;
                }
            }
            else
            {
                // catch or Fault clause
                m_catchClass[m_currentCatch] = catchClass;
                if (m_type[m_currentCatch] != Filter)
                {
                    m_type[m_currentCatch] = type;
                }
                m_catchAddr[m_currentCatch] = catchorfilterAddr;
                if (m_currentCatch > 0)
                {
                    if (m_type[m_currentCatch] != Filter)
                    {
                        Debug.Assert(m_catchEndAddr[m_currentCatch - 1] == -1, "m_catchEndAddr[m_currentCatch-1] == -1");
                        m_catchEndAddr[m_currentCatch - 1] = catchEndAddr;
                    }
                }
                m_catchEndAddr[m_currentCatch] = -1;
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

        internal void MarkCatchAddr(int catchAddr, Type catchException)
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
            else
            {
                m_currentState = State_Finally;
                m_endFinally = finallyAddr;
            }
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
            else if (exc.m_catchEndAddr[exclast] == m_catchEndAddr[last])
            {
                Debug.Assert(exc.GetEndAddress() != GetEndAddress(),
                                "exc.GetEndAddress() != GetEndAddress()");
                if (exc.GetEndAddress() > GetEndAddress())
                    return true;
            }
            return false;
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
    internal enum ScopeAction
    {
        Open = 0x0,
        Close = 0x1,
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
            int cClose = 0;
            int i = m_iCount - 1;

            if (m_iCount == 0)
            {
                return -1;
            }
            for (; cClose > 0 || m_ScopeActions[i] == ScopeAction.Close; i--)
            {
                if (m_ScopeActions[i] == ScopeAction.Open)
                {
                    cClose--;
                }
                else
                    cClose++;
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
            if (m_localSymInfos[i] == null)
            {
                m_localSymInfos[i] = new LocalSymInfo();
            }
            m_localSymInfos[i].AddLocalSymInfo(strName, signature, slot, startOffset, endOffset);
        }

        internal void AddUsingNamespaceToCurrentScope(
            string strNamespace)
        {
            int i = GetCurrentActiveScopeIndex();
            if (m_localSymInfos[i] == null)
            {
                m_localSymInfos[i] = new LocalSymInfo();
            }
            m_localSymInfos[i].AddUsingNamespace(strNamespace);
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
            if (sa == ScopeAction.Open)
            {
                m_iOpenScopeCount++;
            }
            else
                m_iOpenScopeCount--;
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
                Array.Copy(m_iOffsets, 0, temp, 0, m_iCount);
                m_iOffsets = temp;

                ScopeAction[] tempSA = new ScopeAction[newSize];
                Array.Copy(m_ScopeActions, 0, tempSA, 0, m_iCount);
                m_ScopeActions = tempSA;

                LocalSymInfo[] tempLSI = new LocalSymInfo[newSize];
                Array.Copy(m_localSymInfos, 0, tempLSI, 0, m_iCount);
                m_localSymInfos = tempLSI;
            }
        }

        internal void EmitScopeTree(ISymbolWriter symWriter)
        {
            int i;
            for (i = 0; i < m_iCount; i++)
            {
                if (m_ScopeActions[i] == ScopeAction.Open)
                {
                    symWriter.OpenScope(m_iOffsets[i]);
                }
                else
                {
                    symWriter.CloseScope(m_iOffsets[i]);
                }
                if (m_localSymInfos[i] != null)
                {
                    m_localSymInfos[i].EmitLocalSymInfo(symWriter);
                }
            }
        }

        internal int[] m_iOffsets;                 // array of offsets
        internal ScopeAction[] m_ScopeActions;             // array of scope actions
        internal int m_iCount;                   // how many entries in the arrays are occupied
        internal int m_iOpenScopeCount;          // keep track how many scopes are open
        internal const int InitialSize = 16;
        internal LocalSymInfo[] m_localSymInfos;            // keep track debugging local information
    }


    /// <summary>
    /// This class tracks the line number info
    /// </summary>
    internal sealed class LineNumberInfo
    {
        internal LineNumberInfo()
        {
            // initialize data variables
            m_DocumentCount = 0;
            m_iLastFound = 0;
        }

        internal void AddLineNumberInfo(
            ISymbolDocumentWriter document,
            int iOffset,
            int iStartLine,
            int iStartColumn,
            int iEndLine,
            int iEndColumn)
        {
            int i;

            // make sure that arrays are large enough to hold addition info
            i = FindDocument(document);

            Debug.Assert(i < m_DocumentCount, "Bad document look up!");
            m_Documents[i].AddLineNumberInfo(document, iOffset, iStartLine, iStartColumn, iEndLine, iEndColumn);
        }

        // Find a REDocument representing document. If we cannot find one, we will add a new entry into
        // the REDocument array.
        private int FindDocument(ISymbolDocumentWriter document)
        {
            int i;

            // This is an optimization. The chance that the previous line is coming from the same
            // document is very high.
            if (m_iLastFound < m_DocumentCount && m_Documents[m_iLastFound].m_document == document)
                return m_iLastFound;

            for (i = 0; i < m_DocumentCount; i++)
            {
                if (m_Documents[i].m_document == document)
                {
                    m_iLastFound = i;
                    return m_iLastFound;
                }
            }

            // cannot find an existing document so add one to the array
            EnsureCapacity();
            m_iLastFound = m_DocumentCount;
            m_Documents[m_iLastFound] = new REDocument(document);
            checked { m_DocumentCount++; }
            return m_iLastFound;
        }

        /// <summary>
        /// Helper to ensure arrays are large enough
        /// </summary>
        private void EnsureCapacity()
        {
            if (m_DocumentCount == 0)
            {
                // First time. Allocate the arrays.
                m_Documents = new REDocument[InitialSize];
            }
            else if (m_DocumentCount == m_Documents.Length)
            {
                // the arrays are full. Enlarge the arrays
                REDocument[] temp = new REDocument[m_DocumentCount * 2];
                Array.Copy(m_Documents, 0, temp, 0, m_DocumentCount);
                m_Documents = temp;
            }
        }

        internal void EmitLineNumberInfo(ISymbolWriter symWriter)
        {
            for (int i = 0; i < m_DocumentCount; i++)
                m_Documents[i].EmitLineNumberInfo(symWriter);
        }

        private int m_DocumentCount;         // how many documents that we have right now
        private REDocument[] m_Documents;             // array of documents
        private const int InitialSize = 16;
        private int m_iLastFound;
    }


    /// <summary>
    /// This class tracks the line number info
    /// </summary>
    internal sealed class REDocument
    {
        internal REDocument(ISymbolDocumentWriter document)
        {
            // initialize data variables
            m_iLineNumberCount = 0;
            m_document = document;
        }

        internal void AddLineNumberInfo(
            ISymbolDocumentWriter document,
            int iOffset,
            int iStartLine,
            int iStartColumn,
            int iEndLine,
            int iEndColumn)
        {
            Debug.Assert(document == m_document, "Bad document look up!");

            // make sure that arrays are large enough to hold addition info
            EnsureCapacity();

            m_iOffsets[m_iLineNumberCount] = iOffset;
            m_iLines[m_iLineNumberCount] = iStartLine;
            m_iColumns[m_iLineNumberCount] = iStartColumn;
            m_iEndLines[m_iLineNumberCount] = iEndLine;
            m_iEndColumns[m_iLineNumberCount] = iEndColumn;
            checked { m_iLineNumberCount++; }
        }

        /// <summary>
        /// Helper to ensure arrays are large enough
        /// </summary>
        private void EnsureCapacity()
        {
            if (m_iLineNumberCount == 0)
            {
                // First time. Allocate the arrays.
                m_iOffsets = new int[InitialSize];
                m_iLines = new int[InitialSize];
                m_iColumns = new int[InitialSize];
                m_iEndLines = new int[InitialSize];
                m_iEndColumns = new int[InitialSize];
            }
            else if (m_iLineNumberCount == m_iOffsets.Length)
            {
                // the arrays are full. Enlarge the arrays
                // It would probably be simpler to just use Lists here
                int newSize = checked(m_iLineNumberCount * 2);
                int[] temp = new int[newSize];
                Array.Copy(m_iOffsets, 0, temp, 0, m_iLineNumberCount);
                m_iOffsets = temp;

                temp = new int[newSize];
                Array.Copy(m_iLines, 0, temp, 0, m_iLineNumberCount);
                m_iLines = temp;

                temp = new int[newSize];
                Array.Copy(m_iColumns, 0, temp, 0, m_iLineNumberCount);
                m_iColumns = temp;

                temp = new int[newSize];
                Array.Copy(m_iEndLines, 0, temp, 0, m_iLineNumberCount);
                m_iEndLines = temp;

                temp = new int[newSize];
                Array.Copy(m_iEndColumns, 0, temp, 0, m_iLineNumberCount);
                m_iEndColumns = temp;
            }
        }

        internal void EmitLineNumberInfo(ISymbolWriter symWriter)
        {
            int[] iOffsetsTemp;
            int[] iLinesTemp;
            int[] iColumnsTemp;
            int[] iEndLinesTemp;
            int[] iEndColumnsTemp;

            if (m_iLineNumberCount == 0)
                return;
            // reduce the array size to be exact
            iOffsetsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iOffsets, 0, iOffsetsTemp, 0, m_iLineNumberCount);

            iLinesTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iLines, 0, iLinesTemp, 0, m_iLineNumberCount);

            iColumnsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iColumns, 0, iColumnsTemp, 0, m_iLineNumberCount);

            iEndLinesTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iEndLines, 0, iEndLinesTemp, 0, m_iLineNumberCount);

            iEndColumnsTemp = new int[m_iLineNumberCount];
            Array.Copy(m_iEndColumns, 0, iEndColumnsTemp, 0, m_iLineNumberCount);

            symWriter.DefineSequencePoints(m_document, iOffsetsTemp, iLinesTemp, iColumnsTemp, iEndLinesTemp, iEndColumnsTemp);
        }

        private int[] m_iOffsets;                 // array of offsets
        private int[] m_iLines;                   // array of offsets
        private int[] m_iColumns;                 // array of offsets
        private int[] m_iEndLines;                // array of offsets
        private int[] m_iEndColumns;              // array of offsets
        internal ISymbolDocumentWriter m_document;       // The ISymbolDocumentWriter that this REDocument is tracking.
        private int m_iLineNumberCount;         // how many entries in the arrays are occupied
        private const int InitialSize = 16;
    }       // end of REDocument
}
