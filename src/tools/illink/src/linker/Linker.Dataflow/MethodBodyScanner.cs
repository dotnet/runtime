using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Dataflow
{
	/// <summary>
	/// Tracks information about the contents of a stack slot
	/// </summary>
	class StackSlot
	{
		public ValueNode Value { get; set; }

		/// <summary>
		/// True if the value is on the stack as a byref
		/// </summary>
		public bool IsByRef { get; set; }

		public StackSlot ()
		{
		}

		public StackSlot (ValueNode value, bool isByRef = false) : this ()
		{
			Value = value;
			IsByRef = isByRef;
		}
	}

	abstract partial class MethodBodyScanner
	{
		internal ValueNode MethodReturnValue { private set; get; }

		protected virtual void WarnAboutInvalidILInMethod (MethodBody method, int ilOffset)
		{
		}

		private void CheckForInvalidStack (Stack<StackSlot> stack, int depthRequired, MethodBody method, int ilOffset)
		{
			if (stack.Count < depthRequired) {
				WarnAboutInvalidILInMethod (method, ilOffset);
				while (stack.Count < depthRequired)
					stack.Push (new StackSlot ()); // Push dummy values to avoid crashes.
												   // Analysis of this method will be incorrect.
			}
		}

		private void CheckForInvalidReturnStack (Stack<StackSlot> stack, MethodBody method, int ilOffset)
		{
			int numExpectedValuesOnStack = method.Method.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
			if (stack.Count != numExpectedValuesOnStack) {
				WarnAboutInvalidILInMethod (method, ilOffset);
			}
		}

		private static void PushUnknown (Stack<StackSlot> stack)
		{
			stack.Push (new StackSlot ());
		}

		private void PushUnknownAndWarnAboutInvalidIL (Stack<StackSlot> stack, MethodBody methodBody, int offset, bool invalidateBody)
		{
			WarnAboutInvalidILInMethod (methodBody, offset);
			PushUnknown (stack);
		}

		private StackSlot PopUnknown (Stack<StackSlot> stack, int count, MethodBody method, int ilOffset)
		{
			StackSlot topOfStack = null;
			CheckForInvalidStack (stack, count, method, ilOffset);

			for (int i = 0; i < count; ++i) {
				StackSlot slot = stack.Pop ();
				if (i == 0)
					topOfStack = slot;
			}
			return topOfStack;
		}

		private static StackSlot MergeStackElement (StackSlot a, StackSlot b)
		{
			StackSlot mergedSlot;
			if (b.Value == null) {
				mergedSlot = a;
			} else if (a.Value == null) {
				mergedSlot = b;
			} else {
				mergedSlot = new StackSlot (MergePointValue.MergeValues (a.Value, b.Value));
			}

			return mergedSlot;
		}

		// Merge stacks together. This may return the first stack, the stack length must be the same for the two stacks.
		private Stack<StackSlot> MergeStack (Stack<StackSlot> a, Stack<StackSlot> b, MethodBody method, int ilOffset)
		{
			if (a.Count != b.Count) {
				// Force stacks to be of equal size to avoid crashes.
				// Analysis of this method will be incorrect.
				while (a.Count < b.Count)
					a.Push (new StackSlot ());

				while (b.Count < a.Count)
					b.Push (new StackSlot ());
			}

			Stack<StackSlot> newStack = new Stack<StackSlot> (a.Count);
			IEnumerator<StackSlot> aEnum = a.GetEnumerator ();
			IEnumerator<StackSlot> bEnum = b.GetEnumerator ();
			while (aEnum.MoveNext () && bEnum.MoveNext ()) {
				newStack.Push (MergeStackElement (aEnum.Current, bEnum.Current));
			}

			// The new stack is reversed. Use the copy constructor to reverse it back
			return new Stack<StackSlot> (newStack);
		}

		private static void ClearStack (ref Stack<StackSlot> stack)
		{
			stack = null;
		}

		private void NewKnownStack (Dictionary<int, Stack<StackSlot>> knownStacks, int newOffset, Stack<StackSlot> newStack, MethodBody method)
		{
			// No need to merge in empty stacks
			if (newStack.Count == 0) {
				return;
			}

			if (knownStacks.ContainsKey (newOffset)) {
				knownStacks [newOffset] = MergeStack (knownStacks [newOffset], newStack, method, newOffset);
			} else {
				knownStacks.Add (newOffset, new Stack<StackSlot> (newStack.Reverse ()));
			}
		}

		private struct BasicBlockIterator
		{
			HashSet<int> _methodBranchTargets;
			int _currentBlockIndex;
			bool _foundEndOfPrevBlock;

			public BasicBlockIterator (MethodBody methodBody)
			{
				_methodBranchTargets = methodBody.ComputeBranchTargets ();
				_currentBlockIndex = -1;
				_foundEndOfPrevBlock = true;
			}

			public int CurrentBlockIndex {
				get {
					return _currentBlockIndex;
				}
			}

			public int MoveNext (Instruction op)
			{
				if (_foundEndOfPrevBlock || _methodBranchTargets.Contains (op.Offset)) {
					_currentBlockIndex++;
					_foundEndOfPrevBlock = false;
				}

				if (op.OpCode.IsControlFlowInstruction()) {
					_foundEndOfPrevBlock = true;
				}

				return CurrentBlockIndex;
			}
		}

		public struct ValueBasicBlockPair
		{
			public ValueNode Value;
			public int BasicBlockIndex;
		}

		private void StoreMethodLocalValue<KeyType> (
			Dictionary<KeyType, ValueBasicBlockPair> valueCollection,
			ValueNode valueToStore,
			KeyType collectionKey,
			int curBasicBlock)
		{
			ValueBasicBlockPair newValue = new ValueBasicBlockPair { BasicBlockIndex = curBasicBlock };

			ValueBasicBlockPair existingValue;
			if (valueCollection.TryGetValue (collectionKey, out existingValue)
				&& existingValue.BasicBlockIndex == curBasicBlock) {
				// If the previous value was stored in the current basic block, then we can safely 
				// overwrite the previous value with the new one.
				newValue.Value = valueToStore;
			} else {
				// If the previous value came from a previous basic block, then some other use of 
				// the local could see the previous value, so we must merge the new value with the 
				// old value.
				newValue.Value = MergePointValue.MergeValues (existingValue.Value, valueToStore);
			}
			valueCollection [collectionKey] = newValue;
		}

		public void Scan (MethodBody methodBody)
		{
			MethodDefinition thisMethod = methodBody.Method;

			Dictionary<VariableDefinition, ValueBasicBlockPair> locals = new Dictionary<VariableDefinition, ValueBasicBlockPair> (methodBody.Variables.Count);

			Dictionary<int, Stack<StackSlot>> knownStacks = new Dictionary<int, Stack<StackSlot>> ();
			Stack<StackSlot> currentStack = new Stack<StackSlot> (methodBody.MaxStackSize);

			ScanExceptionInformation (knownStacks, methodBody);

			BasicBlockIterator blockIterator = new BasicBlockIterator (methodBody);

			MethodReturnValue = null;
			foreach (Instruction operation in methodBody.Instructions) {
				int curBasicBlock = blockIterator.MoveNext (operation);

				if (knownStacks.ContainsKey (operation.Offset)) {
					if (currentStack == null) {
						// The stack copy constructor reverses the stack
						currentStack = new Stack<StackSlot> (knownStacks [operation.Offset].Reverse ());
					} else {
						currentStack = MergeStack (currentStack, knownStacks [operation.Offset], methodBody, operation.Offset);
					}
				}

				if (currentStack == null) {
					currentStack = new Stack<StackSlot> (methodBody.MaxStackSize);
				}

				switch (operation.OpCode.Code) {
					case Code.Add:
					case Code.Add_Ovf:
					case Code.Add_Ovf_Un:
					case Code.And:
					case Code.Div:
					case Code.Div_Un:
					case Code.Mul:
					case Code.Mul_Ovf:
					case Code.Mul_Ovf_Un:
					case Code.Or:
					case Code.Rem:
					case Code.Rem_Un:
					case Code.Sub:
					case Code.Sub_Ovf:
					case Code.Sub_Ovf_Un:
					case Code.Xor:
					case Code.Cgt:
					case Code.Cgt_Un:
					case Code.Clt:
					case Code.Clt_Un:
					case Code.Ldelem_I:
					case Code.Ldelem_I1:
					case Code.Ldelem_I2:
					case Code.Ldelem_I4:
					case Code.Ldelem_I8:
					case Code.Ldelem_R4:
					case Code.Ldelem_R8:
					case Code.Ldelem_U1:
					case Code.Ldelem_U2:
					case Code.Ldelem_U4:
					case Code.Shl:
					case Code.Shr:
					case Code.Shr_Un:
					case Code.Ldelem_Any:
					case Code.Ldelem_Ref:
					case Code.Ldelema:
					case Code.Ceq:
						PopUnknown (currentStack, 2, methodBody, operation.Offset);
						PushUnknown (currentStack);
						break;

					case Code.Dup:
						currentStack.Push (currentStack.Peek ());
						break;

					case Code.Ldnull:
						currentStack.Push (new StackSlot (NullValue.Instance));
						break;

					
					case Code.Ldc_I4_0:
					case Code.Ldc_I4_1:
					case Code.Ldc_I4_2:
					case Code.Ldc_I4_3:
					case Code.Ldc_I4_4:
					case Code.Ldc_I4_5:
					case Code.Ldc_I4_6:
					case Code.Ldc_I4_7:
					case Code.Ldc_I4_8: {
							int value = operation.OpCode.Code - Code.Ldc_I4_0;
							ConstIntValue civ = new ConstIntValue (value);
							StackSlot slot = new StackSlot (civ);
							currentStack.Push (slot);
						}
						break;

					case Code.Ldc_I4_M1: {
							ConstIntValue civ = new ConstIntValue (-1);
							StackSlot slot = new StackSlot (civ);
							currentStack.Push (slot);
						}
						break;

					case Code.Ldc_I4: {
							int value = (int)operation.Operand;
							ConstIntValue civ = new ConstIntValue (value);
							StackSlot slot = new StackSlot (civ);
							currentStack.Push (slot);
						}
						break;

					case Code.Ldc_I4_S: {
							int value = (sbyte)operation.Operand;
							ConstIntValue civ = new ConstIntValue (value);
							StackSlot slot = new StackSlot (civ);
							currentStack.Push (slot);
						}
						break;

					case Code.Arglist:
					case Code.Ldftn:
					case Code.Sizeof:
					case Code.Ldc_I8:
					case Code.Ldc_R4:
					case Code.Ldc_R8:
					case Code.Ldsfld:
					case Code.Ldsflda:
						PushUnknown (currentStack);
						break;

					case Code.Ldarg:
					case Code.Ldarg_0:
					case Code.Ldarg_1:
					case Code.Ldarg_2:
					case Code.Ldarg_3:
					case Code.Ldarg_S:
					case Code.Ldarga:
					case Code.Ldarga_S:
						ScanLdarg (operation, currentStack, thisMethod, methodBody);
						break;

					case Code.Ldloc:
					case Code.Ldloc_0:
					case Code.Ldloc_1:
					case Code.Ldloc_2:
					case Code.Ldloc_3:
					case Code.Ldloc_S:
					case Code.Ldloca:
					case Code.Ldloca_S:
						ScanLdloc (operation, currentStack, thisMethod, methodBody, locals);
						break;

					case Code.Ldstr: {
							StackSlot slot = new StackSlot (new KnownStringValue ((string)operation.Operand));
							currentStack.Push (slot);
						}
						break;

					case Code.Ldtoken:
						ScanLdtoken (operation, currentStack, thisMethod, methodBody);
						break;

					case Code.Ldind_I:
					case Code.Ldind_I1:
					case Code.Ldind_I2:
					case Code.Ldind_I4:
					case Code.Ldind_I8:
					case Code.Ldind_R4:
					case Code.Ldind_R8:
					case Code.Ldind_U1:
					case Code.Ldind_U2:
					case Code.Ldind_U4:
					case Code.Ldlen:
					case Code.Ldvirtftn:
					case Code.Localloc:
					case Code.Refanytype:
					case Code.Refanyval:
					case Code.Conv_I1:
					case Code.Conv_I2:
					case Code.Conv_I4:
					case Code.Conv_Ovf_I1:
					case Code.Conv_Ovf_I1_Un:
					case Code.Conv_Ovf_I2:
					case Code.Conv_Ovf_I2_Un:
					case Code.Conv_Ovf_I4:
					case Code.Conv_Ovf_I4_Un:
					case Code.Conv_Ovf_U:
					case Code.Conv_Ovf_U_Un:
					case Code.Conv_Ovf_U1:
					case Code.Conv_Ovf_U1_Un:
					case Code.Conv_Ovf_U2:
					case Code.Conv_Ovf_U2_Un:
					case Code.Conv_Ovf_U4:
					case Code.Conv_Ovf_U4_Un:
					case Code.Conv_U1:
					case Code.Conv_U2:
					case Code.Conv_U4:
					case Code.Conv_I8:
					case Code.Conv_Ovf_I8:
					case Code.Conv_Ovf_I8_Un:
					case Code.Conv_Ovf_U8:
					case Code.Conv_Ovf_U8_Un:
					case Code.Conv_U8:
					case Code.Conv_I:
					case Code.Conv_Ovf_I:
					case Code.Conv_Ovf_I_Un:
					case Code.Conv_U:
					case Code.Conv_R_Un:
					case Code.Conv_R4:
					case Code.Conv_R8:
					case Code.Ldind_Ref:
					case Code.Ldobj:
					case Code.Mkrefany:
					case Code.Unbox:
					case Code.Unbox_Any:
					case Code.Box:
					case Code.Neg:
					case Code.Not:
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						PushUnknown (currentStack);
						break;

					case Code.Isinst:
					case Code.Castclass:
						// We can consider a NOP because the value doesn't change.
						// It might change to NULL, but for the purposes of dataflow analysis
						// it doesn't hurt much.
						break;

					case Code.Ldfld:
					case Code.Ldflda:
						// TODO: model field loads
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						PushUnknown (currentStack);
						break;

					case Code.Newarr: {
							StackSlot count = PopUnknown (currentStack, 1, methodBody, operation.Offset);
							currentStack.Push (new StackSlot (new ArrayValue (count.Value)));
						}
						break;
					
					case Code.Cpblk:
					case Code.Initblk:
					case Code.Stelem_I:
					case Code.Stelem_I1:
					case Code.Stelem_I2:
					case Code.Stelem_I4:
					case Code.Stelem_I8:
					case Code.Stelem_R4:
					case Code.Stelem_R8:
					case Code.Stelem_Any:
					case Code.Stelem_Ref:
						PopUnknown (currentStack, 3, methodBody, operation.Offset);
						break;

					case Code.Stfld: {
							StackSlot valueToStoreSlot = PopUnknown (currentStack, 1, methodBody, operation.Offset);
							StackSlot objectToStoreIntoSlot = PopUnknown (currentStack, 1, methodBody, operation.Offset);
							// TODO: model field stores
						}
						break;

					case Code.Stsfld:
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						// TODO: model field stores
						break;

					case Code.Cpobj:
					case Code.Stind_I:
					case Code.Stind_I1:
					case Code.Stind_I2:
					case Code.Stind_I4:
					case Code.Stind_I8:
					case Code.Stind_R4:
					case Code.Stind_R8:
					case Code.Stind_Ref:
					case Code.Stobj:
						PopUnknown (currentStack, 2, methodBody, operation.Offset);
						break;

					case Code.Initobj:
					case Code.Pop:
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						break;

					case Code.Starg:
					case Code.Starg_S:
						// TODO: might want to track this and ensure ldarg reports the stored value.
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						break;

					case Code.Stloc:
					case Code.Stloc_S:
					case Code.Stloc_0:
					case Code.Stloc_1:
					case Code.Stloc_2:
					case Code.Stloc_3:
						ScanStloc (operation, currentStack, methodBody, locals, curBasicBlock);
						break;

					case Code.Constrained:
					case Code.No:
					case Code.Readonly:
					case Code.Tail:
					case Code.Unaligned:
					case Code.Volatile:
						break;

					case Code.Brfalse:
					case Code.Brfalse_S:
					case Code.Brtrue:
					case Code.Brtrue_S:
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						NewKnownStack (knownStacks, ((Instruction)operation.Operand).Offset, currentStack, methodBody);
						break;

					case Code.Calli:
						// TODO: currently not emitted by any mainstream compilers but we should implement
						break;

					case Code.Call:
					case Code.Callvirt:
					case Code.Newobj:
						HandleCall (methodBody, operation, currentStack);
						break;

					case Code.Jmp:
						// Not generated by mainstream compilers
						break;

					case Code.Br:
					case Code.Br_S:
						NewKnownStack (knownStacks, ((Instruction)operation.Operand).Offset, currentStack, methodBody);
						ClearStack (ref currentStack);
						break;

					case Code.Leave:
					case Code.Leave_S:
						PopUnknown (currentStack, currentStack.Count, methodBody, operation.Offset);
						ClearStack (ref currentStack);
						NewKnownStack (knownStacks, ((Instruction)operation.Operand).Offset, new Stack<StackSlot> (methodBody.MaxStackSize), methodBody);
						break;

					case Code.Endfilter:
					case Code.Endfinally:
					case Code.Rethrow:
					case Code.Throw:
						PopUnknown (currentStack, currentStack.Count, methodBody, operation.Offset);
						ClearStack (ref currentStack);
						break;

					case Code.Ret:
						CheckForInvalidReturnStack (currentStack, methodBody, operation.Offset);
						StackSlot retValue = PopUnknown (currentStack, currentStack.Count, methodBody, operation.Offset);
						if (retValue != null)
							MethodReturnValue = MergePointValue.MergeValues (MethodReturnValue, retValue.Value);

						ClearStack (ref currentStack);
						break;

					case Code.Switch: {
							PopUnknown (currentStack, 1, methodBody, operation.Offset);
							Instruction [] targets = (Instruction [])operation.Operand;
							foreach (Instruction target in targets) {
								NewKnownStack (knownStacks, target.Offset, currentStack, methodBody);
							}
							break;
						}

					case Code.Beq:
					case Code.Beq_S:
					case Code.Bne_Un:
					case Code.Bne_Un_S:
					case Code.Bge:
					case Code.Bge_S:
					case Code.Bge_Un:
					case Code.Bge_Un_S:
					case Code.Bgt:
					case Code.Bgt_S:
					case Code.Bgt_Un:
					case Code.Bgt_Un_S:
					case Code.Ble:
					case Code.Ble_S:
					case Code.Ble_Un:
					case Code.Ble_Un_S:
					case Code.Blt:
					case Code.Blt_S:
					case Code.Blt_Un:
					case Code.Blt_Un_S:
						PopUnknown (currentStack, 2, methodBody, operation.Offset);
						NewKnownStack (knownStacks, ((Instruction)operation.Operand).Offset, currentStack, methodBody);
						break;
				}
			}
		}

		private void ScanExceptionInformation (Dictionary<int, Stack<StackSlot>> knownStacks, MethodBody methodBody)
		{
			foreach (ExceptionHandler exceptionClause in methodBody.ExceptionHandlers) {
				Stack<StackSlot> catchStack = new Stack<StackSlot> (1);
				catchStack.Push (new StackSlot ());

				if (exceptionClause.HandlerType == ExceptionHandlerType.Filter) {
					NewKnownStack (knownStacks, exceptionClause.FilterStart.Offset, catchStack, methodBody);
					NewKnownStack (knownStacks, exceptionClause.HandlerStart.Offset, catchStack, methodBody);
				}
				if (exceptionClause.HandlerType == ExceptionHandlerType.Catch) {
					NewKnownStack (knownStacks, exceptionClause.HandlerStart.Offset, catchStack, methodBody);
				}
			}
		}

		private void ScanLdarg (Instruction operation, Stack<StackSlot> currentStack, MethodDefinition thisMethod, MethodBody methodBody)
		{
			int paramNum;
			if (operation.OpCode.Code >= Code.Ldarg_0 &&
				operation.OpCode.Code <= Code.Ldarg_3) {
				paramNum = operation.OpCode.Code - Code.Ldarg_0;
			} else {
				paramNum = ((ParameterDefinition)operation.Operand).Index;
				if (!thisMethod.IsStatic)
					paramNum += 1;
			}

			// TODO: isbyref
			StackSlot slot = new StackSlot (new MethodParameterValue (paramNum), isByRef: false);
			currentStack.Push (slot);
		}

		private void ScanLdloc (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodDefinition thisMethod,
			MethodBody methodBody,
			Dictionary<VariableDefinition, ValueBasicBlockPair> locals)
		{
			VariableDefinition localDef = GetLocalDef (operation, methodBody.Variables);
			if (localDef == null) {
				PushUnknownAndWarnAboutInvalidIL (currentStack, methodBody, operation.Offset, true);
				return;
			}

			bool isByRef = (operation.OpCode.Code == Code.Ldloca || operation.OpCode.Code == Code.Ldloca_S);

			ValueBasicBlockPair localValue;
			locals.TryGetValue (localDef, out localValue);
			if (localValue.Value != null) {
				ValueNode valueToPush = localValue.Value;
				currentStack.Push (new StackSlot (valueToPush, isByRef));
			} else {
				PushUnknown (currentStack);
			}
		}

		private void ScanLdtoken (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodDefinition thisMethod,
			MethodBody methodBody)
		{
			if (operation.Operand is TypeReference typeReference) {
				var resolvedReference = typeReference.Resolve();
				if (resolvedReference != null)
				{
					StackSlot slot = new StackSlot (new RuntimeTypeHandleValue (resolvedReference));
					currentStack.Push (slot);
					return;
				}
			}

			PushUnknown (currentStack);
		}

		private void ScanStloc (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodBody methodBody,
			Dictionary<VariableDefinition, ValueBasicBlockPair> locals,
			int curBasicBlock)
		{
			StackSlot valueToStore = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			VariableDefinition localDef = GetLocalDef (operation, methodBody.Variables);
			if (localDef == null) {
				WarnAboutInvalidILInMethod (methodBody, operation.Offset);
				return;
			}

			StoreMethodLocalValue (locals, valueToStore.Value, localDef, curBasicBlock);
		}

		private static VariableDefinition GetLocalDef (Instruction operation, Collection<VariableDefinition> localVariables)
		{
			Code code = operation.OpCode.Code;
			if (code >= Code.Ldloc_0 && code <= Code.Ldloc_3)
				return localVariables [code - Code.Ldloc_0];
			if (code >= Code.Stloc_0 && code <= Code.Stloc_3)
				return localVariables [code - Code.Stloc_0];

			return (VariableDefinition)operation.Operand;
		}

		private ValueNodeList PopCallArguments (
			Stack<StackSlot> currentStack,
			MethodReference methodCalled,
			MethodBody containingMethodBody,
			bool isNewObj, int ilOffset,
			out ValueNode newObjValue)
		{
			newObjValue = null;

			int countToPop = 0;
			if (!isNewObj && methodCalled.HasThis && !methodCalled.ExplicitThis)
				countToPop++;
			countToPop += methodCalled.Parameters.Count;

			ValueNodeList methodParams = new ValueNodeList (countToPop);
			for (int iParam = 0; iParam < countToPop; ++iParam) {
				StackSlot slot = PopUnknown (currentStack, 1, containingMethodBody, ilOffset);
				methodParams.Add (slot.Value);
			}

			if (isNewObj) {
				newObjValue = UnknownValue.Instance;
				methodParams.Add (newObjValue);
			}
			methodParams.Reverse ();
			return methodParams;
		}

		private void HandleCall (
			MethodBody callingMethodBody,
			Instruction operation,
			Stack<StackSlot> currentStack)
		{
			MethodReference calledMethod = (MethodReference)operation.Operand;

			bool isNewObj = (operation.OpCode.Code == Code.Newobj);

			ValueNode newObjValue = null;
			ValueNodeList methodParams = PopCallArguments (currentStack, calledMethod, callingMethodBody, isNewObj,
														  operation.Offset, out newObjValue);

			ValueNode methodReturnValue = null;
			bool handledFunction = HandleCall (
				callingMethodBody,
				calledMethod,
				operation,
				methodParams,
				out methodReturnValue);

			// Handle the return value or newobj result
			if (!handledFunction) {
				if (isNewObj) {
					if (newObjValue == null)
						PushUnknown (currentStack);
					else
						methodReturnValue = newObjValue;
				} else {
					if (calledMethod.ReturnType.MetadataType != MetadataType.Void) {
						methodReturnValue = UnknownValue.Instance;
					}
				}
			}

			if (methodReturnValue != null)
				currentStack.Push (new StackSlot (methodReturnValue));
		}

		public abstract bool HandleCall (
			MethodBody callingMethodBody,
			MethodReference calledMethod,
			Instruction operation,
			ValueNodeList methodParams,
			out ValueNode methodReturnValue);
	}
}
