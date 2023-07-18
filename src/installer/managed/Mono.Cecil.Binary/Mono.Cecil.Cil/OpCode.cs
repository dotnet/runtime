//
// OpCode.cs
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

	internal struct OpCode {
		short m_value;
		byte m_code;
		byte m_flowControl;
		byte m_opCodeType;
		byte m_operandType;
		byte m_stackBehaviourPop;
		byte m_stackBehaviourPush;

		public string Name {
			get {
				int index = (Size == 1) ? Op2 : (Op2 + 256);
				return OpCodeNames.names [index];
			}
		}

		public int Size {
			get { return ((m_value & 0xff00) == 0xff00) ? 1 : 2; }
		}

		public byte Op1 {
			get { return (byte) (m_value >> 8); }
		}

		public byte Op2 {
			get { return (byte) m_value; }
		}

		public short Value {
			get { return (Size == 1) ? Op2 : m_value; }
		}

		public Code Code {
			get { return (Code) m_code; }
		}

		public FlowControl FlowControl {
			get { return (FlowControl) m_flowControl; }
		}

		public OpCodeType OpCodeType {
			get { return (OpCodeType) m_opCodeType; }
		}

		public OperandType OperandType {
			get { return (OperandType) m_operandType; }
		}

		public StackBehaviour StackBehaviourPop {
			get { return (StackBehaviour) m_stackBehaviourPop; }
		}

		public StackBehaviour StackBehaviourPush {
			get { return (StackBehaviour) m_stackBehaviourPush; }
		}

		internal OpCode (byte op1, byte op2,
			Code code, FlowControl flowControl,
			OpCodeType opCodeType, OperandType operandType,
			StackBehaviour pop, StackBehaviour push)
		{
			m_value = (short) ((op1 << 8) | op2);
			m_code = (byte) code;
			m_flowControl = (byte) flowControl;
			m_opCodeType = (byte) opCodeType;
			m_operandType = (byte) operandType;
			m_stackBehaviourPop = (byte) pop;
			m_stackBehaviourPush = (byte) push;

			if (op1 == 0xff)
				OpCodes.OneByteOpCode [op2] = this;
			else
				OpCodes.TwoBytesOpCode [op2] = this;
		}

		public override int GetHashCode ()
		{
			return m_value;
		}

		public override bool Equals (object obj)
		{
			if (!(obj is OpCode))
				return false;
			OpCode v = (OpCode) obj;
			return v.m_value == m_value;
		}

		public bool Equals (OpCode opcode)
		{
			return (m_value == opcode.m_value);
		}

		public static bool operator == (OpCode one, OpCode other)
		{
			return (one.m_value == other.m_value);
		}

		public static bool operator != (OpCode one, OpCode other)
		{
			return (one.m_value != other.m_value);
		}

		public override string ToString ()
		{
			return Name;
		}
	}
}
