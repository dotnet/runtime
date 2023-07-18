//
// MethodBody.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
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

	using Mono.Cecil;

	internal sealed class MethodBody : IVariableDefinitionProvider, IScopeProvider, ICodeVisitable {

		MethodDefinition m_method;
		int m_maxStack;
		int m_codeSize;
		bool m_initLocals;
		int m_localVarToken;

		InstructionCollection m_instructions;
		ExceptionHandlerCollection m_exceptions;
		VariableDefinitionCollection m_variables;
		ScopeCollection m_scopes;

		private CilWorker m_cilWorker;

		public MethodDefinition Method {
			get { return m_method; }
		}

		public int MaxStack {
			get { return m_maxStack; }
			set { m_maxStack = value; }
		}

		public int CodeSize {
			get { return m_codeSize; }
			set { m_codeSize = value; }
		}

		public bool InitLocals {
			get { return m_initLocals; }
			set { m_initLocals = value; }
		}

		public int LocalVarToken {
			get { return m_localVarToken; }
			set { m_localVarToken = value; }
		}

		public CilWorker CilWorker {
			get {
				if (m_cilWorker == null)
					m_cilWorker = new CilWorker (this);
				return m_cilWorker;
			}
			set { m_cilWorker = value; }
		}

		public InstructionCollection Instructions {
			get { return m_instructions; }
		}

		public bool HasExceptionHandlers {
			get { return m_exceptions != null && m_exceptions.Count > 0; }
		}

		public ExceptionHandlerCollection ExceptionHandlers {
			get {
				if (m_exceptions == null)
					m_exceptions = new ExceptionHandlerCollection (this);
				return m_exceptions;
			}
		}

		public bool HasVariables {
			get { return m_variables != null && m_variables.Count > 0; }
		}

		public VariableDefinitionCollection Variables {
			get {
				if (m_variables == null)
					m_variables = new VariableDefinitionCollection (this);
				return m_variables;
			}
		}

		public bool HasScopes {
			get { return m_scopes != null && m_scopes.Count > 0; }
		}

		public ScopeCollection Scopes {
			get {
				if (m_scopes == null)
					m_scopes = new ScopeCollection (this);
				return m_scopes;
			}
		}

		public MethodBody (MethodDefinition meth)
		{
			m_method = meth;
			// there is always a RET instruction (if a body is present)
			m_instructions = new InstructionCollection (this);
		}

		internal static Instruction GetInstruction (MethodBody oldBody, MethodBody newBody, Instruction i)
		{
			int pos = oldBody.Instructions.IndexOf (i);
			if (pos > -1 && pos < newBody.Instructions.Count)
				return newBody.Instructions [pos];

			return newBody.Instructions.Outside;
		}

		internal static MethodBody Clone (MethodBody body, MethodDefinition parent, ImportContext context)
		{
			MethodBody nb = new MethodBody (parent);
			nb.MaxStack = body.MaxStack;
			nb.InitLocals = body.InitLocals;
			nb.CodeSize = body.CodeSize;

			CilWorker worker = nb.CilWorker;

			if (body.HasVariables) {
				foreach (VariableDefinition var in body.Variables)
					nb.Variables.Add (new VariableDefinition (
						var.Name, var.Index, parent,
						context.Import (var.VariableType)));
			}

			foreach (Instruction instr in body.Instructions) {
				Instruction ni = new Instruction (instr.OpCode);

				switch (instr.OpCode.OperandType) {
				case OperandType.InlineParam :
				case OperandType.ShortInlineParam :
					if (instr.Operand == body.Method.This)
						ni.Operand = nb.Method.This;
					else {
						int param = body.Method.Parameters.IndexOf ((ParameterDefinition) instr.Operand);
						ni.Operand = parent.Parameters [param];
					}
					break;
				case OperandType.InlineVar :
				case OperandType.ShortInlineVar :
					int var = body.Variables.IndexOf ((VariableDefinition) instr.Operand);
					ni.Operand = nb.Variables [var];
					break;
				case OperandType.InlineField :
					ni.Operand = context.Import ((FieldReference) instr.Operand);
					break;
				case OperandType.InlineMethod :
					ni.Operand = context.Import ((MethodReference) instr.Operand);
					break;
				case OperandType.InlineType :
					ni.Operand = context.Import ((TypeReference) instr.Operand);
					break;
				case OperandType.InlineTok :
					if (instr.Operand is TypeReference)
						ni.Operand = context.Import ((TypeReference) instr.Operand);
					else if (instr.Operand is FieldReference)
						ni.Operand = context.Import ((FieldReference) instr.Operand);
					else if (instr.Operand is MethodReference)
						ni.Operand = context.Import ((MethodReference) instr.Operand);
					break;
				case OperandType.ShortInlineBrTarget :
				case OperandType.InlineBrTarget :
				case OperandType.InlineSwitch :
					break;
				default :
					ni.Operand = instr.Operand;
					break;
				}

				worker.Append (ni);
			}

			for (int i = 0; i < body.Instructions.Count; i++) {
				Instruction instr = nb.Instructions [i];
				Instruction oldi = body.Instructions [i];

				if (instr.OpCode.OperandType == OperandType.InlineSwitch) {
					Instruction [] olds = (Instruction []) oldi.Operand;
					Instruction [] targets = new Instruction [olds.Length];

					for (int j = 0; j < targets.Length; j++)
						targets [j] = GetInstruction (body, nb, olds [j]);

					instr.Operand = targets;
				} else if (instr.OpCode.OperandType == OperandType.ShortInlineBrTarget || instr.OpCode.OperandType == OperandType.InlineBrTarget)
					instr.Operand = GetInstruction (body, nb, (Instruction) oldi.Operand);
			}

			if (!body.HasExceptionHandlers)
				return nb;

			foreach (ExceptionHandler eh in body.ExceptionHandlers) {
				ExceptionHandler neh = new ExceptionHandler (eh.Type);
				neh.TryStart = GetInstruction (body, nb, eh.TryStart);
				neh.TryEnd = GetInstruction (body, nb, eh.TryEnd);
				neh.HandlerStart = GetInstruction (body, nb, eh.HandlerStart);
				neh.HandlerEnd = GetInstruction (body, nb, eh.HandlerEnd);

				switch (eh.Type) {
				case ExceptionHandlerType.Catch :
					neh.CatchType = context.Import (eh.CatchType);
					break;
				case ExceptionHandlerType.Filter :
					neh.FilterStart = GetInstruction (body, nb, eh.FilterStart);
					neh.FilterEnd = GetInstruction (body, nb, eh.FilterEnd);
					break;
				}

				nb.ExceptionHandlers.Add (neh);
			}

			return nb;
		}

		public void Simplify ()
		{
			foreach (Instruction i in this.Instructions) {
				if (i.OpCode.OpCodeType != OpCodeType.Macro)
					continue;

				switch (i.OpCode.Code) {
				case Code.Ldarg_0 :
					Modify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 0));
					break;
				case Code.Ldarg_1 :
					Modify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 1));
					break;
				case Code.Ldarg_2 :
					Modify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 2));
					break;
				case Code.Ldarg_3 :
					Modify (i, OpCodes.Ldarg,
						CodeReader.GetParameter (this, 3));
					break;
				case Code.Ldloc_0 :
					Modify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 0));
					break;
				case Code.Ldloc_1 :
					Modify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 1));
					break;
				case Code.Ldloc_2 :
					Modify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 2));
					break;
				case Code.Ldloc_3 :
					Modify (i, OpCodes.Ldloc,
						CodeReader.GetVariable (this, 3));
					break;
				case Code.Stloc_0 :
					Modify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 0));
					break;
				case Code.Stloc_1 :
					Modify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 1));
					break;
				case Code.Stloc_2 :
					Modify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 2));
					break;
				case Code.Stloc_3 :
					Modify (i, OpCodes.Stloc,
						CodeReader.GetVariable (this, 3));
					break;
				case Code.Ldarg_S :
					i.OpCode = OpCodes.Ldarg;
					break;
				case Code.Ldarga_S :
					i.OpCode = OpCodes.Ldarga;
					break;
				case Code.Starg_S :
					i.OpCode = OpCodes.Starg;
					break;
				case Code.Ldloc_S :
					i.OpCode = OpCodes.Ldloc;
					break;
				case Code.Ldloca_S :
					i.OpCode = OpCodes.Ldloca;
					break;
				case Code.Stloc_S :
					i.OpCode = OpCodes.Stloc;
					break;
				case Code.Ldc_I4_M1 :
					Modify (i, OpCodes.Ldc_I4, -1);
					break;
				case Code.Ldc_I4_0 :
					Modify (i, OpCodes.Ldc_I4, 0);
					break;
				case Code.Ldc_I4_1 :
					Modify (i, OpCodes.Ldc_I4, 1);
					break;
				case Code.Ldc_I4_2 :
					Modify (i, OpCodes.Ldc_I4, 2);
					break;
				case Code.Ldc_I4_3 :
					Modify (i, OpCodes.Ldc_I4, 3);
					break;
				case Code.Ldc_I4_4 :
					Modify (i, OpCodes.Ldc_I4, 4);
					break;
				case Code.Ldc_I4_5 :
					Modify (i, OpCodes.Ldc_I4, 5);
					break;
				case Code.Ldc_I4_6 :
					Modify (i, OpCodes.Ldc_I4, 6);
					break;
				case Code.Ldc_I4_7 :
					Modify (i, OpCodes.Ldc_I4, 7);
					break;
				case Code.Ldc_I4_8 :
					Modify (i, OpCodes.Ldc_I4, 8);
					break;
				case Code.Ldc_I4_S :
					i.OpCode = OpCodes.Ldc_I4;
					i.Operand = (int) (sbyte) i.Operand;
					break;
				case Code.Br_S :
					i.OpCode = OpCodes.Br;
					break;
				case Code.Brfalse_S :
					i.OpCode = OpCodes.Brfalse;
					break;
				case Code.Brtrue_S :
					i.OpCode = OpCodes.Brtrue;
					break;
				case Code.Beq_S :
					i.OpCode = OpCodes.Beq;
					break;
				case Code.Bge_S :
					i.OpCode = OpCodes.Bge;
					break;
				case Code.Bgt_S :
					i.OpCode = OpCodes.Bgt;
					break;
				case Code.Ble_S :
					i.OpCode = OpCodes.Ble;
					break;
				case Code.Blt_S :
					i.OpCode = OpCodes.Blt;
					break;
				case Code.Bne_Un_S :
					i.OpCode = OpCodes.Bne_Un;
					break;
				case Code.Bge_Un_S :
					i.OpCode = OpCodes.Bge_Un;
					break;
				case Code.Bgt_Un_S :
					i.OpCode = OpCodes.Bgt_Un;
					break;
				case Code.Ble_Un_S :
					i.OpCode = OpCodes.Ble_Un;
					break;
				case Code.Blt_Un_S :
					i.OpCode = OpCodes.Blt_Un;
					break;
				case Code.Leave_S :
					i.OpCode = OpCodes.Leave;
					break;
				}
			}
		}

		public void Optimize ()
		{
			foreach (Instruction instr in m_instructions) {
				int index;
				switch (instr.OpCode.Code) {
				case Code.Ldarg:
					index = m_method.Parameters.IndexOf ((ParameterDefinition) instr.Operand);
					if (index == -1 && instr.Operand == m_method.This)
						index = 0;
					else if (m_method.HasThis)
						index++;

					switch (index) {
					case 0:
						Modify (instr, OpCodes.Ldarg_0, null);
						break;
					case 1:
						Modify (instr, OpCodes.Ldarg_1, null);
						break;
					case 2:
						Modify (instr, OpCodes.Ldarg_2, null);
						break;
					case 3:
						Modify (instr, OpCodes.Ldarg_3, null);
						break;
					default:
						if (index < 256)
							Modify (instr, OpCodes.Ldarg_S, instr.Operand);
						break;
					}
					break;
				case Code.Ldloc:
					index = m_variables.IndexOf ((VariableDefinition) instr.Operand);
					switch (index) {
					case 0:
						Modify (instr, OpCodes.Ldloc_0, null);
						break;
					case 1:
						Modify (instr, OpCodes.Ldloc_1, null);
						break;
					case 2:
						Modify (instr, OpCodes.Ldloc_2, null);
						break;
					case 3:
						Modify (instr, OpCodes.Ldloc_3, null);
						break;
					default:
						if (index < 256)
							Modify (instr, OpCodes.Ldloc_S, instr.Operand);
						break;
					}
					break;
				case Code.Stloc:
					index = m_variables.IndexOf ((VariableDefinition) instr.Operand);
					switch (index) {
					case 0:
						Modify (instr, OpCodes.Stloc_0, null);
						break;
					case 1:
						Modify (instr, OpCodes.Stloc_1, null);
						break;
					case 2:
						Modify (instr, OpCodes.Stloc_2, null);
						break;
					case 3:
						Modify (instr, OpCodes.Stloc_3, null);
						break;
					default:
						if (index < 256)
							Modify (instr, OpCodes.Stloc_S, instr.Operand);
						break;
					}
					break;
				case Code.Ldarga:
					index = m_method.Parameters.IndexOf ((ParameterDefinition) instr.Operand);
					if (index == -1 && instr.Operand == m_method.This)
						index = 0;
					else if (m_method.HasThis)
						index++;
					if (index < 256)
						Modify (instr, OpCodes.Ldarga_S, instr.Operand);
					break;
				case Code.Ldloca:
					if (m_variables.IndexOf ((VariableDefinition) instr.Operand) < 256)
						Modify (instr, OpCodes.Ldloca_S, instr.Operand);
					break;
				case Code.Ldc_I4:
					int i = (int) instr.Operand;
					switch (i) {
					case -1:
						Modify (instr, OpCodes.Ldc_I4_M1, null);
						break;
					case 0:
						Modify (instr, OpCodes.Ldc_I4_0, null);
						break;
					case 1:
						Modify (instr, OpCodes.Ldc_I4_1, null);
						break;
					case 2:
						Modify (instr, OpCodes.Ldc_I4_2, null);
						break;
					case 3:
						Modify (instr, OpCodes.Ldc_I4_3, null);
						break;
					case 4:
						Modify (instr, OpCodes.Ldc_I4_4, null);
						break;
					case 5:
						Modify (instr, OpCodes.Ldc_I4_5, null);
						break;
					case 6:
						Modify (instr, OpCodes.Ldc_I4_6, null);
						break;
					case 7:
						Modify (instr, OpCodes.Ldc_I4_7, null);
						break;
					case 8:
						Modify (instr, OpCodes.Ldc_I4_8, null);
						break;
					default:
						if (i >= -128 && i < 128)
							Modify (instr, OpCodes.Ldc_I4_S, (sbyte) i);
						break;
					}
					break;
				}
			}

			OptimizeBranches ();
		}

		void OptimizeBranches ()
		{
			ComputeOffsets ();

			foreach (Instruction instr in m_instructions) {
				if (instr.OpCode.OperandType != OperandType.InlineBrTarget)
					continue;

				if (OptimizeBranch (instr))
					ComputeOffsets ();
			}
		}

		static bool OptimizeBranch (Instruction instr)
		{
			int offset = ((Instruction) instr.Operand).Offset - (instr.Offset + instr.OpCode.Size + 4);
			if (! (offset >= -128 && offset <= 127))
				return false;

			switch (instr.OpCode.Code) {
			case Code.Br:
				instr.OpCode = OpCodes.Br_S;
				break;
			case Code.Brfalse:
				instr.OpCode = OpCodes.Brfalse_S;
				break;
			case Code.Brtrue:
				instr.OpCode = OpCodes.Brtrue_S;
				break;
			case Code.Beq:
				instr.OpCode = OpCodes.Beq_S;
				break;
			case Code.Bge:
				instr.OpCode = OpCodes.Bge_S;
				break;
			case Code.Bgt:
				instr.OpCode = OpCodes.Bgt_S;
				break;
			case Code.Ble:
				instr.OpCode = OpCodes.Ble_S;
				break;
			case Code.Blt:
				instr.OpCode = OpCodes.Blt_S;
				break;
			case Code.Bne_Un:
				instr.OpCode = OpCodes.Bne_Un_S;
				break;
			case Code.Bge_Un:
				instr.OpCode = OpCodes.Bge_Un_S;
				break;
			case Code.Bgt_Un:
				instr.OpCode = OpCodes.Bgt_Un_S;
				break;
			case Code.Ble_Un:
				instr.OpCode = OpCodes.Ble_Un_S;
				break;
			case Code.Blt_Un:
				instr.OpCode = OpCodes.Blt_Un_S;
				break;
			case Code.Leave:
				instr.OpCode = OpCodes.Leave_S;
				break;
			}

			return true;
		}

		void ComputeOffsets ()
		{
			int offset = 0;

			foreach (Instruction instr in m_instructions) {
				instr.Offset = offset;
				offset += instr.GetSize ();
			}
		}

		static void Modify (Instruction i, OpCode op, object operand)
		{
			i.OpCode = op;
			i.Operand = operand;
		}

		public void Accept (ICodeVisitor visitor)
		{
			visitor.VisitMethodBody (this);
			if (HasVariables)
				m_variables.Accept (visitor);
			m_instructions.Accept (visitor);
			if (HasExceptionHandlers)
				m_exceptions.Accept (visitor);
			if (HasScopes)
				m_scopes.Accept (visitor);

			visitor.TerminateMethodBody (this);
		}
	}
}
