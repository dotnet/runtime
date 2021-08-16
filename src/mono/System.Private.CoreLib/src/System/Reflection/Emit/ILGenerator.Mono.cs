// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

//
// System.Reflection.Emit/ILGenerator.cs
//
// Author:
//   Paolo Molaro (lupus@ximian.com)
//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
//

#if MONO_FEATURE_SRE
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.SymbolStore;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{

    internal struct ILExceptionBlock
    {
        public const int CATCH = 0;
        public const int FILTER = 1;
        public const int FINALLY = 2;
        public const int FAULT = 4;
        public const int FILTER_START = -1;

#region Sync with MonoILExceptionBlock in object-internals.h
        internal Type? extype;
        internal int type;
        internal int start;
        internal int len;
        internal int filter_offset;
#endregion
    }

    internal struct ILExceptionInfo
    {
#region Sync with MonoILExceptionInfo in object-internals.h
        internal ILExceptionBlock[] handlers;
        internal int start;
        internal int len;
        internal Label end;
#endregion

        internal int NumHandlers()
        {
            return handlers.Length;
        }

        internal void AddCatch(Type? extype, int offset)
        {
            int i;
            End(offset);
            add_block(offset);
            i = handlers.Length - 1;
            handlers[i].type = ILExceptionBlock.CATCH;
            handlers[i].start = offset;
            handlers[i].extype = extype;
        }

        internal void AddFinally(int offset)
        {
            int i;
            End(offset);
            add_block(offset);
            i = handlers.Length - 1;
            handlers[i].type = ILExceptionBlock.FINALLY;
            handlers[i].start = offset;
            handlers[i].extype = null;
        }

        internal void AddFault(int offset)
        {
            int i;
            End(offset);
            add_block(offset);
            i = handlers.Length - 1;
            handlers[i].type = ILExceptionBlock.FAULT;
            handlers[i].start = offset;
            handlers[i].extype = null;
        }

        internal void AddFilter(int offset)
        {
            int i;
            End(offset);
            add_block(offset);
            i = handlers.Length - 1;
            handlers[i].type = ILExceptionBlock.FILTER_START;
            handlers[i].extype = null;
            handlers[i].filter_offset = offset;
        }

        internal void End(int offset)
        {
            if (handlers == null)
                return;
            int i = handlers.Length - 1;
            if (i >= 0)
                handlers[i].len = offset - handlers[i].start;
        }

        internal int LastClauseType()
        {
            if (handlers != null)
                return handlers[handlers.Length - 1].type;
            else
                return ILExceptionBlock.CATCH;
        }

        internal void PatchFilterClause(int start)
        {
            if (handlers != null && handlers.Length > 0)
            {
                handlers[handlers.Length - 1].start = start;
                handlers[handlers.Length - 1].type = ILExceptionBlock.FILTER;
            }
        }

        private void add_block(int offset)
        {
            if (handlers != null)
            {
                int i = handlers.Length;
                ILExceptionBlock[] new_b = new ILExceptionBlock[i + 1];
                Array.Copy(handlers, new_b, i);
                handlers = new_b;
                handlers[i].len = offset - handlers[i].start;
            }
            else
            {
                handlers = new ILExceptionBlock[1];
                len = offset - start;
            }
        }
    }

    internal struct ILTokenInfo
    {
        public MemberInfo member;
        public int code_pos;
    }

    internal interface ITokenGenerator
    {
        int GetToken(string str);

        int GetToken(MemberInfo member, bool create_open_instance);

        int GetToken(MethodBase method, Type[] opt_param_types);

        int GetToken(SignatureHelper helper);
    }

    [StructLayout(LayoutKind.Sequential)]
    public partial class ILGenerator
    {
        private struct LabelFixup
        {
            public int offset;    // The number of bytes between pos and the
                                  // offset of the jump
            public int pos;       // Where offset of the label is placed
            public int label_idx; // The label to jump to
        };

        private struct LabelData
        {
            public LabelData(int addr, int maxStack)
            {
                this.addr = addr;
                this.maxStack = maxStack;
            }

            public int addr;
            public int maxStack;
        }

#region Sync with MonoReflectionILGen in object-internals.h
        private byte[] code;
        private int code_len;
        private int max_stack;
        private int cur_stack;
        private LocalBuilder[]? locals;
        private ILExceptionInfo[]? ex_handlers;
        private int num_token_fixups;
        private object? token_fixups;
#endregion

        private LabelData[]? labels;
        private int num_labels;
        private LabelFixup[]? fixups;
        private int num_fixups;
        internal Module module;
        private int cur_block;
        private Stack? open_blocks;
        private ITokenGenerator token_gen;

        private const int defaultFixupSize = 4;
        private const int defaultLabelsSize = 4;
        private const int defaultExceptionStackSize = 2;

        [DynamicDependency(nameof(token_fixups))]  // Automatically keeps all previous fields too due to StructLayout
        internal ILGenerator(Module m, ITokenGenerator token_gen, int size)
        {
            if (size < 0)
                size = 128;
            code = new byte[size];
            module = m;
            this.token_gen = token_gen;
        }

        private void make_room(int nbytes)
        {
            if (code_len + nbytes < code.Length)
                return;
            byte[] new_code = new byte[(code_len + nbytes) * 2 + 128];
            Array.Copy(code, 0, new_code, 0, code.Length);
            code = new_code;
        }

        private void emit_int(int val)
        {
            code[code_len++] = (byte)(val & 0xFF);
            code[code_len++] = (byte)((val >> 8) & 0xFF);
            code[code_len++] = (byte)((val >> 16) & 0xFF);
            code[code_len++] = (byte)((val >> 24) & 0xFF);
        }

        /* change to pass by ref to avoid copy */
        private void ll_emit(OpCode opcode)
        {
            /*
             * there is already enough room allocated in code.
             */
            if (opcode.Size == 2)
                code[code_len++] = (byte)(opcode.Value >> 8);
            code[code_len++] = (byte)(opcode.Value & 0xff);
            /*
             * We should probably keep track of stack needs here.
             * Or we may want to run the verifier on the code before saving it
             * (this may be needed anyway when the ILGenerator is not used...).
             */
            switch (opcode.StackBehaviourPush)
            {
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                case StackBehaviour.Varpush: /* again we are conservative and assume it pushes 1 */
                    cur_stack++;
                    break;
                case StackBehaviour.Push1_push1:
                    cur_stack += 2;
                    break;
            }
            if (max_stack < cur_stack)
                max_stack = cur_stack;

            /*
             * Note that we adjust for the pop behaviour _after_ setting max_stack.
             */
            switch (opcode.StackBehaviourPop)
            {
                case StackBehaviour.Varpop:
                    break; /* we are conservative and assume it doesn't decrease the stack needs */
                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                    cur_stack--;
                    break;
                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    cur_stack -= 2;
                    break;
                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    cur_stack -= 3;
                    break;
            }
        }

        private static int target_len(OpCode opcode)
        {
            if (opcode.OperandType == OperandType.InlineBrTarget)
                return 4;
            return 1;
        }

        private void InternalEndClause()
        {
            switch (ex_handlers![cur_block].LastClauseType())
            {
                case ILExceptionBlock.CATCH:
                case ILExceptionBlock.FILTER:
                case ILExceptionBlock.FILTER_START:
                    // how could we optimize code size here?
                    Emit(OpCodes.Leave, ex_handlers[cur_block].end);
                    break;
                case ILExceptionBlock.FAULT:
                case ILExceptionBlock.FINALLY:
                    Emit(OpCodes.Endfinally);
                    break;
            }
        }

        public virtual void BeginCatchBlock(Type exceptionType)
        {
            open_blocks ??= new Stack(defaultExceptionStackSize);

            if (open_blocks.Count <= 0)
                throw new NotSupportedException("Not in an exception block");
            if (exceptionType != null && exceptionType.IsUserType)
                throw new NotSupportedException("User defined subclasses of System.Type are not yet supported.");
            if (ex_handlers![cur_block].LastClauseType() == ILExceptionBlock.FILTER_START)
            {
                if (exceptionType != null)
                    throw new ArgumentException("Do not supply an exception type for filter clause");
                Emit(OpCodes.Endfilter);
                ex_handlers[cur_block].PatchFilterClause(code_len);
            }
            else
            {
                InternalEndClause();
                ex_handlers[cur_block].AddCatch(exceptionType, code_len);
            }

            cur_stack = 1; // the exception object is on the stack by default
            if (max_stack < cur_stack)
                max_stack = cur_stack;

            //System.Console.WriteLine ("Begin catch Block: {0} {1}",exceptionType.ToString(), max_stack);
        }

        public virtual void BeginExceptFilterBlock()
        {
            if (open_blocks == null)
                open_blocks = new Stack(defaultExceptionStackSize);

            if (open_blocks.Count <= 0)
                throw new NotSupportedException("Not in an exception block");
            InternalEndClause();

            ex_handlers![cur_block].AddFilter(code_len);
        }

        public virtual Label BeginExceptionBlock()
        {
            //System.Console.WriteLine ("Begin Block");
            if (open_blocks == null)
                open_blocks = new Stack(defaultExceptionStackSize);

            if (ex_handlers != null)
            {
                cur_block = ex_handlers.Length;
                ILExceptionInfo[] new_ex = new ILExceptionInfo[cur_block + 1];
                Array.Copy(ex_handlers, new_ex, cur_block);
                ex_handlers = new_ex;
            }
            else
            {
                ex_handlers = new ILExceptionInfo[1];
                cur_block = 0;
            }
            open_blocks.Push(cur_block);
            ex_handlers[cur_block].start = code_len;
            return ex_handlers[cur_block].end = DefineLabel();
        }

        public virtual void BeginFaultBlock()
        {
            if (open_blocks == null)
                open_blocks = new Stack(defaultExceptionStackSize);

            if (open_blocks.Count <= 0)
                throw new NotSupportedException("Not in an exception block");

            if (ex_handlers![cur_block].LastClauseType() == ILExceptionBlock.FILTER_START)
            {
                Emit(OpCodes.Leave, ex_handlers[cur_block].end);
                ex_handlers[cur_block].PatchFilterClause(code_len);
            }

            InternalEndClause();
            //System.Console.WriteLine ("Begin fault Block");
            ex_handlers[cur_block].AddFault(code_len);
        }

        public virtual void BeginFinallyBlock()
        {
            if (open_blocks == null)
                open_blocks = new Stack(defaultExceptionStackSize);

            if (open_blocks.Count <= 0)
                throw new NotSupportedException("Not in an exception block");

            InternalEndClause();

            if (ex_handlers![cur_block].LastClauseType() == ILExceptionBlock.FILTER_START)
            {
                Emit(OpCodes.Leave, ex_handlers[cur_block].end);
                ex_handlers[cur_block].PatchFilterClause(code_len);
            }

            //System.Console.WriteLine ("Begin finally Block");
            ex_handlers[cur_block].AddFinally(code_len);
        }

        public virtual void BeginScope()
        { }

        public virtual LocalBuilder DeclareLocal(Type localType)
        {
            return DeclareLocal(localType, false);
        }


        public virtual LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            if (localType == null)
                throw new ArgumentNullException(nameof(localType));
            if (localType.IsUserType)
                throw new NotSupportedException("User defined subclasses of System.Type are not yet supported.");
            LocalBuilder res = new LocalBuilder(localType, this);
            res.is_pinned = pinned;

            if (locals != null)
            {
                LocalBuilder[] new_l = new LocalBuilder[locals.Length + 1];
                Array.Copy(locals, new_l, locals.Length);
                new_l[locals.Length] = res;
                locals = new_l;
            }
            else
            {
                locals = new LocalBuilder[1];
                locals[0] = res;
            }
            res.position = (ushort)(locals.Length - 1);
            return res;
        }

        public virtual Label DefineLabel()
        {
            if (labels == null)
                labels = new LabelData[defaultLabelsSize];
            else if (num_labels >= labels.Length)
            {
                LabelData[] t = new LabelData[labels.Length * 2];
                Array.Copy(labels, t, labels.Length);
                labels = t;
            }

            labels[num_labels] = new LabelData(-1, 0);

            return new Label(num_labels++);
        }

        public virtual void Emit(OpCode opcode)
        {
            make_room(2);
            ll_emit(opcode);
        }

        public virtual void Emit(OpCode opcode, byte arg)
        {
            make_room(3);
            ll_emit(opcode);
            code[code_len++] = arg;
        }

        [ComVisible(true)]
        public virtual void Emit(OpCode opcode, ConstructorInfo con)
        {
            int token = token_gen.GetToken(con, true);
            make_room(6);
            ll_emit(opcode);
            emit_int(token);

            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
                cur_stack -= con.GetParametersCount();
        }

        public virtual void Emit(OpCode opcode, double arg)
        {
            byte[] s = BitConverter.GetBytes(arg);
            make_room(10);
            ll_emit(opcode);
            if (BitConverter.IsLittleEndian)
            {
                Array.Copy(s, 0, code, code_len, 8);
                code_len += 8;
            }
            else
            {
                code[code_len++] = s[7];
                code[code_len++] = s[6];
                code[code_len++] = s[5];
                code[code_len++] = s[4];
                code[code_len++] = s[3];
                code[code_len++] = s[2];
                code[code_len++] = s[1];
                code[code_len++] = s[0];
            }
        }

        public virtual void Emit(OpCode opcode, FieldInfo field)
        {
            int token = token_gen.GetToken(field, true);
            make_room(6);
            ll_emit(opcode);
            emit_int(token);
        }

        public virtual void Emit(OpCode opcode, short arg)
        {
            make_room(4);
            ll_emit(opcode);
            code[code_len++] = (byte)(arg & 0xFF);
            code[code_len++] = (byte)((arg >> 8) & 0xFF);
        }

        public virtual void Emit(OpCode opcode, int arg)
        {
            make_room(6);
            ll_emit(opcode);
            emit_int(arg);
        }

        public virtual void Emit(OpCode opcode, long arg)
        {
            make_room(10);
            ll_emit(opcode);
            code[code_len++] = (byte)(arg & 0xFF);
            code[code_len++] = (byte)((arg >> 8) & 0xFF);
            code[code_len++] = (byte)((arg >> 16) & 0xFF);
            code[code_len++] = (byte)((arg >> 24) & 0xFF);
            code[code_len++] = (byte)((arg >> 32) & 0xFF);
            code[code_len++] = (byte)((arg >> 40) & 0xFF);
            code[code_len++] = (byte)((arg >> 48) & 0xFF);
            code[code_len++] = (byte)((arg >> 56) & 0xFF);
        }

        public virtual void Emit(OpCode opcode, Label label)
        {
            int tlen = target_len(opcode);
            make_room(6);
            ll_emit(opcode);
            if (cur_stack > labels![label.m_label].maxStack)
                labels[label.m_label].maxStack = cur_stack;

            if (fixups == null)
                fixups = new LabelFixup[defaultFixupSize];
            else if (num_fixups >= fixups.Length)
            {
                LabelFixup[] newf = new LabelFixup[fixups.Length * 2];
                Array.Copy(fixups, newf, fixups.Length);
                fixups = newf;
            }
            fixups[num_fixups].offset = tlen;
            fixups[num_fixups].pos = code_len;
            fixups[num_fixups].label_idx = label.m_label;
            num_fixups++;
            code_len += tlen;

        }

        public virtual void Emit(OpCode opcode, Label[] labels)
        {
            if (labels == null)
                throw new ArgumentNullException(nameof(labels));

            /* opcode needs to be switch. */
            int count = labels.Length;
            make_room(6 + count * 4);
            ll_emit(opcode);

            for (int i = 0; i < count; ++i)
                if (cur_stack > this.labels![labels[i].m_label].maxStack)
                    this.labels[labels[i].m_label].maxStack = cur_stack;

            emit_int(count);
            if (fixups == null)
                fixups = new LabelFixup[defaultFixupSize + count];
            else if (num_fixups + count >= fixups.Length)
            {
                LabelFixup[] newf = new LabelFixup[count + fixups.Length * 2];
                Array.Copy(fixups, newf, fixups.Length);
                fixups = newf;
            }

            // ECMA 335, Partition III, p94 (7-10)
            //
            // The switch instruction implements a jump table. The format of
            // the instruction is an unsigned int32 representing the number of targets N,
            // followed by N int32 values specifying jump targets: these targets are
            // represented as offsets (positive or negative) from the beginning of the
            // instruction following this switch instruction.
            //
            // We must make sure it gets an offset from the *end* of the last label
            // (eg, the beginning of the instruction following this).
            //
            // remaining is the number of bytes from the current instruction to the
            // instruction that will be emitted.

            for (int i = 0, remaining = count * 4; i < count; ++i, remaining -= 4)
            {
                fixups[num_fixups].offset = remaining;
                fixups[num_fixups].pos = code_len;
                fixups[num_fixups].label_idx = labels[i].m_label;
                num_fixups++;
                code_len += 4;
            }
        }

        public virtual void Emit(OpCode opcode, LocalBuilder local)
        {
            if (local == null)
                throw new ArgumentNullException(nameof(local));
            if (local.ilgen != this)
                throw new ArgumentException(SR.Argument_UnmatchedMethodForLocal, nameof(local));

            uint pos = local.position;
            if ((opcode == OpCodes.Ldloca_S || opcode == OpCodes.Ldloc_S || opcode == OpCodes.Stloc_S) && pos > 255)
                throw new InvalidOperationException(SR.InvalidOperation_BadInstructionOrIndexOutOfBound);

            bool load_addr = false;
            bool is_store = false;
            bool is_load = false;
            make_room(6);

            /* inline the code from ll_emit () to optimize il code size */
            if (opcode.StackBehaviourPop == StackBehaviour.Pop1)
            {
                cur_stack--;
                is_store = true;
            }
            else if (opcode.StackBehaviourPush == StackBehaviour.Push1 || opcode.StackBehaviourPush == StackBehaviour.Pushi)
            {
                cur_stack++;
                is_load = true;
                if (cur_stack > max_stack)
                    max_stack = cur_stack;
                load_addr = opcode.StackBehaviourPush == StackBehaviour.Pushi;
            }
            if (load_addr)
            {
                if (pos < 256)
                {
                    code[code_len++] = (byte)0x12;
                    code[code_len++] = (byte)pos;
                }
                else
                {
                    code[code_len++] = (byte)0xfe;
                    code[code_len++] = (byte)0x0d;
                    code[code_len++] = (byte)(pos & 0xff);
                    code[code_len++] = (byte)((pos >> 8) & 0xff);
                }
            }
            else
            {
                if (is_store)
                {
                    if (pos < 4)
                    {
                        code[code_len++] = (byte)(0x0a + pos);
                    }
                    else if (pos < 256)
                    {
                        code[code_len++] = (byte)0x13;
                        code[code_len++] = (byte)pos;
                    }
                    else
                    {
                        code[code_len++] = (byte)0xfe;
                        code[code_len++] = (byte)0x0e;
                        code[code_len++] = (byte)(pos & 0xff);
                        code[code_len++] = (byte)((pos >> 8) & 0xff);
                    }
                }
                else if (is_load)
                {
                    if (pos < 4)
                    {
                        code[code_len++] = (byte)(0x06 + pos);
                    }
                    else if (pos < 256)
                    {
                        code[code_len++] = (byte)0x11;
                        code[code_len++] = (byte)pos;
                    }
                    else
                    {
                        code[code_len++] = (byte)0xfe;
                        code[code_len++] = (byte)0x0c;
                        code[code_len++] = (byte)(pos & 0xff);
                        code[code_len++] = (byte)((pos >> 8) & 0xff);
                    }
                }
                else
                {
                    ll_emit(opcode);
                }
            }
        }

        public virtual void Emit(OpCode opcode, MethodInfo meth)
        {
            if (meth == null)
                throw new ArgumentNullException(nameof(meth));

            // For compatibility with MS
            if ((meth is DynamicMethod) && ((opcode == OpCodes.Ldftn) || (opcode == OpCodes.Ldvirtftn) || (opcode == OpCodes.Ldtoken)))
                throw new ArgumentException("Ldtoken, Ldftn and Ldvirtftn OpCodes cannot target DynamicMethods.");

            int token = token_gen.GetToken(meth, true);
            make_room(6);
            ll_emit(opcode);
            Type? declaringType = meth.DeclaringType;
            emit_int(token);
            if (meth.ReturnType != typeof(void))
                cur_stack++;

            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
                cur_stack -= meth.GetParametersCount();
        }

        private void Emit(OpCode opcode, MethodInfo method, int token)
        {
            make_room(6);
            ll_emit(opcode);
            emit_int(token);
            if (method.ReturnType != typeof(void))
                cur_stack++;

            if (opcode.StackBehaviourPop == StackBehaviour.Varpop)
                cur_stack -= method.GetParametersCount();
        }

        [CLSCompliant(false)]
        public void Emit(OpCode opcode, sbyte arg)
        {
            make_room(3);
            ll_emit(opcode);
            code[code_len++] = (byte)arg;
        }

        public virtual void Emit(OpCode opcode, SignatureHelper signature)
        {
            int token = token_gen.GetToken(signature);
            make_room(6);
            ll_emit(opcode);
            emit_int(token);
        }

        public virtual void Emit(OpCode opcode, float arg)
        {
            byte[] s = BitConverter.GetBytes(arg);
            make_room(6);
            ll_emit(opcode);
            if (BitConverter.IsLittleEndian)
            {
                Array.Copy(s, 0, code, code_len, 4);
                code_len += 4;
            }
            else
            {
                code[code_len++] = s[3];
                code[code_len++] = s[2];
                code[code_len++] = s[1];
                code[code_len++] = s[0];
            }
        }

        public virtual void Emit(OpCode opcode, string str)
        {
            int token = token_gen.GetToken(str);
            make_room(6);
            ll_emit(opcode);
            emit_int(token);
        }

        public virtual void Emit(OpCode opcode, Type cls)
        {
            if (cls != null && cls.IsByRef)
                throw new ArgumentException("Cannot get TypeToken for a ByRef type.");

            make_room(6);
            ll_emit(opcode);
            int token = token_gen.GetToken(cls!, opcode != OpCodes.Ldtoken);
            emit_int(token);
        }

        // FIXME: vararg methods are not supported
        public virtual void EmitCall(OpCode opcode, MethodInfo methodInfo, Type[]? optionalParameterTypes)
        {
            if (methodInfo == null)
                throw new ArgumentNullException(nameof(methodInfo));
            short value = opcode.Value;
            if (!(value == OpCodes.Call.Value || value == OpCodes.Callvirt.Value))
                throw new NotSupportedException("Only Call and CallVirt are allowed");
            if ((methodInfo.CallingConvention & CallingConventions.VarArgs) == 0)
                optionalParameterTypes = null;
            if (optionalParameterTypes != null)
            {
                if ((methodInfo.CallingConvention & CallingConventions.VarArgs) == 0)
                {
                    throw new InvalidOperationException("Method is not VarArgs method and optional types were passed");
                }

                int token = token_gen.GetToken(methodInfo, optionalParameterTypes);
                Emit(opcode, methodInfo, token);
                return;
            }
            Emit(opcode, methodInfo);
        }

        public virtual void EmitCalli(OpCode opcode, CallingConvention unmanagedCallConv, Type? returnType, Type[]? parameterTypes)
        {
            // GetMethodSigHelper expects a ModuleBuilder or null, and module might be
            // a normal module when using dynamic methods.
            SignatureHelper helper = SignatureHelper.GetMethodSigHelper(module as ModuleBuilder, 0, unmanagedCallConv, returnType, parameterTypes);
            Emit(opcode, helper);
        }

        public virtual void EmitCalli(OpCode opcode, CallingConventions callingConvention, Type returnType, Type[]? parameterTypes, Type[]? optionalParameterTypes)
        {
            if (optionalParameterTypes != null)
                throw new NotImplementedException();

            SignatureHelper helper = SignatureHelper.GetMethodSigHelper(module as ModuleBuilder, callingConvention, 0, returnType, parameterTypes);
            Emit(opcode, helper);
        }

        private const string ConsoleTypeFullName = "System.Console, System.Console";

        public virtual void EmitWriteLine(FieldInfo fld)
        {
            if (fld == null)
                throw new ArgumentNullException(nameof(fld));

            // The MS implementation does not check for valuetypes here but it
            // should. Also, it should check that if the field is not static,
            // then it is a member of this type.
            if (fld.IsStatic)
                Emit(OpCodes.Ldsfld, fld);
            else
            {
                Emit(OpCodes.Ldarg_0);
                Emit(OpCodes.Ldfld, fld);
            }
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            Emit(OpCodes.Call, consoleType.GetMethod("WriteLine", new Type[1] { fld.FieldType })!);
        }

        public virtual void EmitWriteLine(LocalBuilder localBuilder)
        {
            if (localBuilder == null)
                throw new ArgumentNullException(nameof(localBuilder));
            if (localBuilder.LocalType is TypeBuilder)
                throw new ArgumentException("Output streams do not support TypeBuilders.");
            // The MS implementation does not check for valuetypes here but it
            // should.
            Emit(OpCodes.Ldloc, localBuilder);
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            Emit(OpCodes.Call, consoleType.GetMethod("WriteLine", new Type[1] { localBuilder.LocalType })!);
        }

        public virtual void EmitWriteLine(string value)
        {
            Emit(OpCodes.Ldstr, value);
            Type consoleType = Type.GetType(ConsoleTypeFullName, throwOnError: true)!;
            Emit(OpCodes.Call, consoleType.GetMethod("WriteLine", new Type[1] { typeof(string) })!);
        }

        public virtual void EndExceptionBlock()
        {
            if (open_blocks == null)
                open_blocks = new Stack(defaultExceptionStackSize);

            if (open_blocks.Count <= 0)
                throw new NotSupportedException("Not in an exception block");

            if (ex_handlers![cur_block].LastClauseType() == ILExceptionBlock.FILTER_START)
                throw new InvalidOperationException("Incorrect code generation for exception block.");

            InternalEndClause();
            MarkLabel(ex_handlers[cur_block].end);
            ex_handlers[cur_block].End(code_len);
            open_blocks.Pop();
            if (open_blocks.Count > 0)
                cur_block = (int)open_blocks.Peek()!;
        }

        public virtual void EndScope()
        { }

        public virtual void MarkLabel(Label loc)
        {
            if (loc.m_label < 0 || loc.m_label >= num_labels)
                throw new System.ArgumentException("The label is not valid");
            if (labels![loc.m_label].addr >= 0)
                throw new System.ArgumentException("The label was already defined");
            labels[loc.m_label].addr = code_len;
            if (labels[loc.m_label].maxStack > cur_stack)
                cur_stack = labels[loc.m_label].maxStack;
        }

        public virtual void ThrowException([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type excType)
        {
            if (excType == null)
                throw new ArgumentNullException(nameof(excType));
            if (!((excType == typeof(Exception)) ||
                   excType.IsSubclassOf(typeof(Exception))))
                throw new ArgumentException("Type should be an exception type", nameof(excType));
            ConstructorInfo? ctor = excType.GetConstructor(Type.EmptyTypes);
            if (ctor == null)
                throw new ArgumentException("Type should have a default constructor", nameof(excType));
            Emit(OpCodes.Newobj, ctor);
            Emit(OpCodes.Throw);
        }

        // FIXME: "Not implemented"
        public virtual void UsingNamespace(string usingNamespace)
        {
            throw new NotImplementedException();
        }

        internal void label_fixup(MethodBase mb)
        {
            for (int i = 0; i < num_fixups; ++i)
            {
                if (labels![fixups![i].label_idx].addr < 0)
                    throw new ArgumentException(string.Format("Label #{0} is not marked in method `{1}'", fixups[i].label_idx + 1, mb.Name));
                // Diff is the offset from the end of the jump instruction to the address of the label
                int diff = labels[fixups[i].label_idx].addr - (fixups[i].pos + fixups[i].offset);
                if (fixups[i].offset == 1)
                {
                    code[fixups[i].pos] = (byte)((sbyte)diff);
                }
                else
                {
                    int old_cl = code_len;
                    code_len = fixups[i].pos;
                    emit_int(diff);
                    code_len = old_cl;
                }
            }
        }

        // Used by DynamicILGenerator and MethodBuilder.SetMethodBody
        internal void SetCode(byte[]? code, int max_stack)
        {
            // Make a copy to avoid possible security problems
            this.code = code != null ? (byte[])code.Clone() : Array.Empty<byte>();
            this.code_len = this.code.Length;
            this.max_stack = max_stack;
            this.cur_stack = 0;
        }

        internal unsafe void SetCode(byte* code, int code_size, int max_stack)
        {
            // Make a copy to avoid possible security problems
            this.code = new byte[code_size];
            for (int i = 0; i < code_size; ++i)
                this.code[i] = code[i];
            this.code_len = code_size;
            this.max_stack = max_stack;
            this.cur_stack = 0;
        }

        internal ITokenGenerator TokenGenerator
        {
            get
            {
                return token_gen;
            }
        }

        public virtual int ILOffset
        {
            get { return code_len; }
        }
    }

    internal struct SequencePoint
    {
        public int Offset;
        public int Line;
        public int Col;
        public int EndLine;
        public int EndCol;
    }

    internal sealed class Stack
    {
        private object?[] _array;
        private int _size;
        private int _version;

        private const int _defaultCapacity = 10;

        public Stack()
        {
            _array = new object[_defaultCapacity];
            _size = 0;
            _version = 0;
        }

        public Stack(int initialCapacity)
        {
            if (initialCapacity < 0)
                throw new ArgumentOutOfRangeException(nameof(initialCapacity), SR.ArgumentOutOfRange_NeedNonNegNum);

            if (initialCapacity < _defaultCapacity)
                initialCapacity = _defaultCapacity;
            _array = new object[initialCapacity];
            _size = 0;
            _version = 0;
        }

        public int Count
        {
            get
            {
                return _size;
            }
        }

        public object? Peek()
        {
            if (_size == 0)
                throw new InvalidOperationException();

            return _array[_size - 1];
        }

        public object? Pop()
        {
            if (_size == 0)
                throw new InvalidOperationException();

            _version++;
            object? obj = _array[--_size];
            _array[_size] = null;
            return obj;
        }

        public void Push(object obj)
        {
            if (_size == _array.Length)
            {
                object[] newArray = new object[2 * _array.Length];
                Array.Copy(_array, 0, newArray, 0, _size);
                _array = newArray;
            }
            _array[_size++] = obj;
            _version++;
        }
    }
}
#endif
