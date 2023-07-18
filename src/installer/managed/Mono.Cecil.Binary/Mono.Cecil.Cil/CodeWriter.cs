//
// CodeWriter.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 - 2007 Jb Evain
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

namespace Mono.Cecil.Cil {

	using System;
	using System.Collections;

	using Mono.Cecil;
	using Mono.Cecil.Binary;
	using Mono.Cecil.Metadata;
	using Mono.Cecil.Signatures;

	sealed class CodeWriter : BaseCodeVisitor {

		ReflectionWriter m_reflectWriter;
		MemoryBinaryWriter m_binaryWriter;
		MemoryBinaryWriter m_codeWriter;

		IDictionary m_localSigCache;
		IDictionary m_standaloneSigCache;

		IDictionary m_stackSizes;

		bool stripped;

		public bool Stripped {
			get { return stripped; }
			set { stripped = value; }
		}

		public CodeWriter (ReflectionWriter reflectWriter, MemoryBinaryWriter writer)
		{
			m_reflectWriter = reflectWriter;
			m_binaryWriter = writer;
			m_codeWriter = new MemoryBinaryWriter ();

			m_localSigCache = new Hashtable ();
			m_standaloneSigCache = new Hashtable ();

			m_stackSizes = new Hashtable ();
		}

		public RVA WriteMethodBody (MethodDefinition meth)
		{
			if (meth.Body == null)
				return RVA.Zero;

			RVA ret = m_reflectWriter.MetadataWriter.GetDataCursor ();
			meth.Body.Accept (this);
			return ret;
		}

		public override void VisitMethodBody (MethodBody body)
		{
			m_codeWriter.Empty ();
		}

		void WriteToken (MetadataToken token)
		{
			if (token.RID == 0)
				m_codeWriter.Write (0);
			else
				m_codeWriter.Write (token.ToUInt ());
		}

		static int GetParameterIndex (MethodBody body, ParameterDefinition p)
		{
			int idx = body.Method.Parameters.IndexOf (p);
			if (idx == -1 && p == body.Method.This)
				return 0;
			if (body.Method.HasThis)
				idx++;

			return idx;
		}

		public override void VisitInstructionCollection (InstructionCollection instructions)
		{
			MethodBody body = instructions.Container;
			long start = m_codeWriter.BaseStream.Position;

			ComputeMaxStack (instructions);

			foreach (Instruction instr in instructions) {

				instr.Offset = (int) (m_codeWriter.BaseStream.Position - start);

				if (instr.OpCode.Size == 1)
					m_codeWriter.Write (instr.OpCode.Op2);
				else {
					m_codeWriter.Write (instr.OpCode.Op1);
					m_codeWriter.Write (instr.OpCode.Op2);
				}

				if (instr.OpCode.OperandType != OperandType.InlineNone &&
					instr.Operand == null)
					throw new ReflectionException ("OpCode {0} have null operand", instr.OpCode.Name);

				switch (instr.OpCode.OperandType) {
				case OperandType.InlineNone :
					break;
				case OperandType.InlineSwitch :
					Instruction [] targets = (Instruction []) instr.Operand;
					for (int i = 0; i < targets.Length + 1; i++)
						m_codeWriter.Write ((uint) 0);
					break;
				case OperandType.ShortInlineBrTarget :
					m_codeWriter.Write ((byte) 0);
					break;
				case OperandType.InlineBrTarget :
					m_codeWriter.Write (0);
					break;
				case OperandType.ShortInlineI :
					if (instr.OpCode == OpCodes.Ldc_I4_S)
						m_codeWriter.Write ((sbyte) instr.Operand);
					else
						m_codeWriter.Write ((byte) instr.Operand);
					break;
				case OperandType.ShortInlineVar :
					m_codeWriter.Write ((byte) body.Variables.IndexOf (
						(VariableDefinition) instr.Operand));
					break;
				case OperandType.ShortInlineParam :
					m_codeWriter.Write ((byte) GetParameterIndex (body, (ParameterDefinition) instr.Operand));
					break;
				case OperandType.InlineSig :
					WriteToken (GetCallSiteToken ((CallSite) instr.Operand));
					break;
				case OperandType.InlineI :
					m_codeWriter.Write ((int) instr.Operand);
					break;
				case OperandType.InlineVar :
					m_codeWriter.Write ((short) body.Variables.IndexOf (
						(VariableDefinition) instr.Operand));
					break;
				case OperandType.InlineParam :
					m_codeWriter.Write ((short) GetParameterIndex (
							body, (ParameterDefinition) instr.Operand));
					break;
				case OperandType.InlineI8 :
					m_codeWriter.Write ((long) instr.Operand);
					break;
				case OperandType.ShortInlineR :
					m_codeWriter.Write ((float) instr.Operand);
					break;
				case OperandType.InlineR :
					m_codeWriter.Write ((double) instr.Operand);
					break;
				case OperandType.InlineString :
					WriteToken (new MetadataToken (TokenType.String,
							m_reflectWriter.MetadataWriter.AddUserString (instr.Operand as string)));
					break;
				case OperandType.InlineField :
				case OperandType.InlineMethod :
				case OperandType.InlineType :
				case OperandType.InlineTok :
					if (instr.Operand is TypeReference)
						WriteToken (GetTypeToken ((TypeReference) instr.Operand));
					else if (instr.Operand is GenericInstanceMethod)
						WriteToken (m_reflectWriter.GetMethodSpecToken (instr.Operand as GenericInstanceMethod));
					else if (instr.Operand is MemberReference)
						WriteToken (m_reflectWriter.GetMemberRefToken ((MemberReference) instr.Operand));
					else if (instr.Operand is IMetadataTokenProvider)
						WriteToken (((IMetadataTokenProvider) instr.Operand).MetadataToken);
					else
						throw new ReflectionException (
							string.Format ("Wrong operand for {0} OpCode: {1}",
								instr.OpCode.OperandType,
								instr.Operand.GetType ().FullName));
					break;
				}
			}

			// patch branches
			long pos = m_codeWriter.BaseStream.Position;

			foreach (Instruction instr in instructions) {
				switch (instr.OpCode.OperandType) {
				case OperandType.InlineSwitch :
					m_codeWriter.BaseStream.Position = instr.Offset + instr.OpCode.Size;
					Instruction [] targets = (Instruction []) instr.Operand;
					m_codeWriter.Write ((uint) targets.Length);
					foreach (Instruction tgt in targets)
						m_codeWriter.Write ((tgt.Offset - (instr.Offset +
							instr.OpCode.Size + (4 * (targets.Length + 1)))));
					break;
				case OperandType.ShortInlineBrTarget :
					m_codeWriter.BaseStream.Position = instr.Offset + instr.OpCode.Size;
					m_codeWriter.Write ((byte) (((Instruction) instr.Operand).Offset -
						(instr.Offset + instr.OpCode.Size + 1)));
					break;
				case OperandType.InlineBrTarget :
					m_codeWriter.BaseStream.Position = instr.Offset + instr.OpCode.Size;
					m_codeWriter.Write(((Instruction) instr.Operand).Offset -
						(instr.Offset + instr.OpCode.Size + 4));
					break;
				}
			}

			m_codeWriter.BaseStream.Position = pos;
		}

		MetadataToken GetTypeToken (TypeReference type)
		{
			return m_reflectWriter.GetTypeDefOrRefToken (type);
		}

		MetadataToken GetCallSiteToken (CallSite cs)
		{
			uint sig;
			int sentinel = cs.GetSentinel ();
			if (sentinel > 0)
				sig = m_reflectWriter.SignatureWriter.AddMethodDefSig (
					m_reflectWriter.GetMethodDefSig (cs));
			else
				sig = m_reflectWriter.SignatureWriter.AddMethodRefSig (
					m_reflectWriter.GetMethodRefSig (cs));

			if (m_standaloneSigCache.Contains (sig))
				return (MetadataToken) m_standaloneSigCache [sig];

			StandAloneSigTable sasTable = m_reflectWriter.MetadataTableWriter.GetStandAloneSigTable ();
			StandAloneSigRow sasRow = m_reflectWriter.MetadataRowWriter.CreateStandAloneSigRow (sig);

			sasTable.Rows.Add(sasRow);

			MetadataToken token = new MetadataToken (TokenType.Signature, (uint) sasTable.Rows.Count);
			m_standaloneSigCache [sig] = token;
			return token;
		}

		static int GetLength (Instruction start, Instruction end, InstructionCollection instructions)
		{
			Instruction last = instructions [instructions.Count - 1];
			return (end == instructions.Outside ? last.Offset + last.GetSize () : end.Offset) - start.Offset;
		}

		static bool IsRangeFat (Instruction start, Instruction end, InstructionCollection instructions)
		{
			return GetLength (start, end, instructions) >= 256 ||
				start.Offset >= 65536;
		}

		static bool IsFat (ExceptionHandlerCollection seh)
		{
			for (int i = 0; i < seh.Count; i++) {
				ExceptionHandler eh = seh [i];
				if (IsRangeFat (eh.TryStart, eh.TryEnd, seh.Container.Instructions))
					return true;

				if (IsRangeFat (eh.HandlerStart, eh.HandlerEnd, seh.Container.Instructions))
					return true;

				switch (eh.Type) {
				case ExceptionHandlerType.Filter :
					if (IsRangeFat (eh.FilterStart, eh.FilterEnd, seh.Container.Instructions))
						return true;
					break;
				}
			}

			return false;
		}

		void WriteExceptionHandlerCollection (ExceptionHandlerCollection seh)
		{
			m_codeWriter.QuadAlign ();

			if (seh.Count < 0x15 && !IsFat (seh)) {
				m_codeWriter.Write ((byte) MethodDataSection.EHTable);
				m_codeWriter.Write ((byte) (seh.Count * 12 + 4));
				m_codeWriter.Write (new byte [2]);
				foreach (ExceptionHandler eh in seh) {
					m_codeWriter.Write ((ushort) eh.Type);
					m_codeWriter.Write ((ushort) eh.TryStart.Offset);
					m_codeWriter.Write ((byte) (eh.TryEnd.Offset - eh.TryStart.Offset));
					m_codeWriter.Write ((ushort) eh.HandlerStart.Offset);
					m_codeWriter.Write ((byte) GetLength (eh.HandlerStart, eh.HandlerEnd, seh.Container.Instructions));
					WriteHandlerSpecific (eh);
				}
			} else {
				m_codeWriter.Write ((byte) (MethodDataSection.FatFormat | MethodDataSection.EHTable));
				WriteFatBlockSize (seh);
				foreach (ExceptionHandler eh in seh) {
					m_codeWriter.Write ((uint) eh.Type);
					m_codeWriter.Write ((uint) eh.TryStart.Offset);
					m_codeWriter.Write ((uint) (eh.TryEnd.Offset - eh.TryStart.Offset));
					m_codeWriter.Write ((uint) eh.HandlerStart.Offset);
					m_codeWriter.Write ((uint) GetLength (eh.HandlerStart, eh.HandlerEnd, seh.Container.Instructions));
					WriteHandlerSpecific (eh);
				}
			}
		}

		void WriteFatBlockSize (ExceptionHandlerCollection seh)
		{
			int size = seh.Count * 24 + 4;
			m_codeWriter.Write ((byte) (size & 0xff));
			m_codeWriter.Write ((byte) ((size >> 8) & 0xff));
			m_codeWriter.Write ((byte) ((size >> 16) & 0xff));
		}

		void WriteHandlerSpecific (ExceptionHandler eh)
		{
			switch (eh.Type) {
			case ExceptionHandlerType.Catch :
				WriteToken (GetTypeToken (eh.CatchType));
				break;
			case ExceptionHandlerType.Filter :
				m_codeWriter.Write ((uint) eh.FilterStart.Offset);
				break;
			default :
				m_codeWriter.Write (0);
				break;
			}
		}

		public override void VisitVariableDefinitionCollection (VariableDefinitionCollection variables)
		{
			MethodBody body = variables.Container as MethodBody;
			if (body == null || stripped)
				return;

			uint sig = m_reflectWriter.SignatureWriter.AddLocalVarSig (
					GetLocalVarSig (variables));

			if (m_localSigCache.Contains (sig)) {
				body.LocalVarToken = (int) m_localSigCache [sig];
				return;
			}

			StandAloneSigTable sasTable = m_reflectWriter.MetadataTableWriter.GetStandAloneSigTable ();
			StandAloneSigRow sasRow = m_reflectWriter.MetadataRowWriter.CreateStandAloneSigRow (
				sig);

			sasTable.Rows.Add (sasRow);
			body.LocalVarToken = sasTable.Rows.Count;
			m_localSigCache [sig] = body.LocalVarToken;
		}

		public override void TerminateMethodBody (MethodBody body)
		{
			long pos = m_binaryWriter.BaseStream.Position;

			if (body.HasVariables || body.HasExceptionHandlers
				|| m_codeWriter.BaseStream.Length >= 64 || body.MaxStack > 8) {

				MethodHeader header = MethodHeader.FatFormat;
				if (body.InitLocals)
					header |= MethodHeader.InitLocals;
				if (body.HasExceptionHandlers)
					header |= MethodHeader.MoreSects;

				m_binaryWriter.Write ((byte) header);
				m_binaryWriter.Write ((byte) 0x30); // (header size / 4) << 4
				m_binaryWriter.Write ((short) body.MaxStack);
				m_binaryWriter.Write ((int) m_codeWriter.BaseStream.Length);
				// the token should be zero if there are no variables
				int token = body.HasVariables ? ((int) TokenType.Signature | body.LocalVarToken) : 0;
				m_binaryWriter.Write (token);

				if (body.HasExceptionHandlers)
					WriteExceptionHandlerCollection (body.ExceptionHandlers);
			} else
				m_binaryWriter.Write ((byte) ((byte) MethodHeader.TinyFormat |
					m_codeWriter.BaseStream.Length << 2));

			m_binaryWriter.Write (m_codeWriter);
			m_binaryWriter.QuadAlign ();

			m_reflectWriter.MetadataWriter.AddData (
				(int) (m_binaryWriter.BaseStream.Position - pos));
		}

		public LocalVarSig.LocalVariable GetLocalVariableSig (VariableDefinition var)
		{
			LocalVarSig.LocalVariable lv = new LocalVarSig.LocalVariable ();
			TypeReference type = var.VariableType;

			lv.CustomMods = m_reflectWriter.GetCustomMods (type);

			if (type is PinnedType) {
				lv.Constraint |= Constraint.Pinned;
				type = (type as PinnedType).ElementType;
			}

			if (type is ReferenceType) {
				lv.ByRef = true;
				type = (type as ReferenceType).ElementType;
			}

			lv.Type = m_reflectWriter.GetSigType (type);

			return lv;
		}

		public LocalVarSig GetLocalVarSig (VariableDefinitionCollection vars)
		{
			LocalVarSig lvs = new LocalVarSig ();
			lvs.CallingConvention |= 0x7;
			lvs.Count = vars.Count;
			lvs.LocalVariables = new LocalVarSig.LocalVariable [lvs.Count];
			for (int i = 0; i < lvs.Count; i++) {
				lvs.LocalVariables [i] = GetLocalVariableSig (vars [i]);
			}

			return lvs;
		}

		void ComputeMaxStack (InstructionCollection instructions)
		{
			int current = 0;
			int max = 0;
			m_stackSizes.Clear ();

			foreach (ExceptionHandler eh in instructions.Container.ExceptionHandlers) {
				switch (eh.Type) {
				case ExceptionHandlerType.Catch :
				case ExceptionHandlerType.Filter :
					m_stackSizes [eh.HandlerStart] = 1;
					max = 1;
					break;
				}
			}

			foreach (Instruction instr in instructions) {

				object savedSize = m_stackSizes [instr];
				if (savedSize != null)
					current = (int) savedSize;

				current -= GetPopDelta (instructions.Container.Method, instr, current);

				if (current < 0)
					current = 0;

				current += GetPushDelta (instr);

				if (current > max)
					max = current;

				// for forward branches, copy the stack size for the instruction that is being branched to
				switch (instr.OpCode.OperandType) {
					case OperandType.InlineBrTarget:
					case OperandType.ShortInlineBrTarget:
						m_stackSizes [instr.Operand] = current;
					break;
					case OperandType.InlineSwitch:
						foreach (Instruction target in (Instruction []) instr.Operand)
							m_stackSizes [target] = current;
					break;
				}

				switch (instr.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Throw:
				case FlowControl.Return:
					// next statement is not reachable from this statement, so reset the stack depth to 0
					current = 0;
					break;
				}
			}

			instructions.Container.MaxStack = max + 1; // you never know
		}

		static int GetPushDelta (Instruction instruction)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPush) {
			case StackBehaviour.Push0:
				return 0;

			case StackBehaviour.Push1:
			case StackBehaviour.Pushi:
			case StackBehaviour.Pushi8:
			case StackBehaviour.Pushr4:
			case StackBehaviour.Pushr8:
			case StackBehaviour.Pushref:
				return 1;

			case StackBehaviour.Push1_push1:
				return 2;

			case StackBehaviour.Varpush:
				if (code.FlowControl != FlowControl.Call)
					break;

				IMethodSignature method = (IMethodSignature) instruction.Operand;
				return IsVoid (method.ReturnType.ReturnType) ? 0 : 1;
			}

			throw new NotSupportedException ();
		}

		static int GetPopDelta (MethodDefinition current, Instruction instruction, int height)
		{
			OpCode code = instruction.OpCode;
			switch (code.StackBehaviourPop) {
			case StackBehaviour.Pop0:
				return 0;
			case StackBehaviour.Popi:
			case StackBehaviour.Popref:
			case StackBehaviour.Pop1:
				return 1;

			case StackBehaviour.Pop1_pop1:
			case StackBehaviour.Popi_pop1:
			case StackBehaviour.Popi_popi:
			case StackBehaviour.Popi_popi8:
			case StackBehaviour.Popi_popr4:
			case StackBehaviour.Popi_popr8:
			case StackBehaviour.Popref_pop1:
			case StackBehaviour.Popref_popi:
				return 2;

			case StackBehaviour.Popi_popi_popi:
			case StackBehaviour.Popref_popi_popi:
			case StackBehaviour.Popref_popi_popi8:
			case StackBehaviour.Popref_popi_popr4:
			case StackBehaviour.Popref_popi_popr8:
			case StackBehaviour.Popref_popi_popref:
				return 3;

			case StackBehaviour.PopAll:
				return height;

			case StackBehaviour.Varpop:
				if (code == OpCodes.Ret)
					return IsVoid (current.ReturnType.ReturnType) ? 0 : 1;

				if (code.FlowControl != FlowControl.Call)
					break;

				IMethodSignature method = (IMethodSignature) instruction.Operand;
				int count = method.HasParameters ? method.Parameters.Count : 0;
				if (method.HasThis && code != OpCodes.Newobj)
					++count;

				return count;
			}

			throw new NotSupportedException ();
		}

		static bool IsVoid (TypeReference type)
		{
			return type.FullName == Constants.Void;
		}
	}
}
