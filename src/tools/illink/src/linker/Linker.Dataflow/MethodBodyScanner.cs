// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using LocalVariableStore = System.Collections.Generic.Dictionary<
	Mono.Cecil.Cil.VariableDefinition,
	Mono.Linker.Dataflow.ValueBasicBlockPair>;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	/// <summary>
	/// Tracks information about the contents of a stack slot
	/// </summary>
	readonly struct StackSlot
	{
		public MultiValue Value { get; }

		public StackSlot ()
		{
			Value = new MultiValue (UnknownValue.Instance);
		}

		public StackSlot (SingleValue value)
		{
			Value = new MultiValue (value);
		}

		public StackSlot (MultiValue value)
		{
			Value = value;
		}
	}

	internal abstract partial class MethodBodyScanner
	{
		protected readonly LinkContext _context;
		protected readonly InterproceduralStateLattice InterproceduralStateLattice;
		protected static ValueSetLattice<SingleValue> MultiValueLattice => default;

		protected MethodBodyScanner (LinkContext context)
		{
			this._context = context;
			this.InterproceduralStateLattice = new InterproceduralStateLattice (default, default, context);
		}

		internal MultiValue ReturnValue { get; private set; }

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

		private static void PushUnknown (Stack<StackSlot> stack)
		{
			stack.Push (new StackSlot ());
		}

		private void PushUnknownAndWarnAboutInvalidIL (Stack<StackSlot> stack, MethodBody methodBody, int offset)
		{
			WarnAboutInvalidILInMethod (methodBody, offset);
			PushUnknown (stack);
		}

		private StackSlot PopUnknown (Stack<StackSlot> stack, int count, MethodBody method, int ilOffset)
		{
			if (count < 1)
				throw new InvalidOperationException ();

			StackSlot topOfStack = default;
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
			return new StackSlot (MultiValueLattice.Meet (a.Value, b.Value));
		}

		// Merge stacks together. This may return the first stack, the stack length must be the same for the two stacks.
		private static Stack<StackSlot> MergeStack (Stack<StackSlot> a, Stack<StackSlot> b)
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
			Stack<StackSlot>.Enumerator aEnum = a.GetEnumerator ();
			Stack<StackSlot>.Enumerator bEnum = b.GetEnumerator ();
			while (aEnum.MoveNext () && bEnum.MoveNext ()) {
				newStack.Push (MergeStackElement (aEnum.Current, bEnum.Current));
			}

			// The new stack is reversed. Use the copy constructor to reverse it back
			return new Stack<StackSlot> (newStack);
		}

		private static void ClearStack (ref Stack<StackSlot>? stack)
		{
			stack = null;
		}

		private static void NewKnownStack (Dictionary<int, Stack<StackSlot>> knownStacks, int newOffset, Stack<StackSlot> newStack)
		{
			// No need to merge in empty stacks
			if (newStack.Count == 0) {
				return;
			}

			if (knownStacks.TryGetValue (newOffset, out Stack<StackSlot>? value)) {
				knownStacks[newOffset] = MergeStack (value, newStack);
			} else {
				knownStacks.Add (newOffset, new Stack<StackSlot> (newStack.Reverse ()));
			}
		}

		private struct BasicBlockIterator
		{
			readonly HashSet<int> _methodBranchTargets;
			int _currentBlockIndex;
			bool _foundEndOfPrevBlock;

			public BasicBlockIterator (MethodIL methodIL)
			{
				_methodBranchTargets = methodIL.ComputeBranchTargets ();
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

				if (op.OpCode.IsControlFlowInstruction ()) {
					_foundEndOfPrevBlock = true;
				}

				return CurrentBlockIndex;
			}
		}

		[Conditional ("DEBUG")]
		static void ValidateNoReferenceToReference (LocalVariableStore locals, MethodDefinition method, int ilOffset)
		{
			foreach (var keyValuePair in locals) {
				MultiValue localValue = keyValuePair.Value.Value;
				VariableDefinition localVariable = keyValuePair.Key;
				foreach (var val in localValue.AsEnumerable ()) {
					if (val is LocalVariableReferenceValue localReference && localReference.ReferencedType.IsByReference) {
						string displayName = $"local variable V_{localReference.LocalDefinition.Index}";
						throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (
							$"""In method {method.FullName}, local variable V_{localVariable.Index} references {displayName} of type {localReference.ReferencedType.GetDisplayName ()} which is a reference. Dataflow tracking has failed.""",
							(int) DiagnosticId.LinkerUnexpectedError,
							origin: new MessageOrigin (method, ilOffset)));
					}
				}
			}
		}

		protected static void StoreMethodLocalValue<KeyType> (
			Dictionary<KeyType, ValueBasicBlockPair> valueCollection,
			in MultiValue valueToStore,
			KeyType collectionKey,
			int curBasicBlock,
			int? maxTrackedValues = null)
			where KeyType : notnull
		{
			if (valueCollection.TryGetValue (collectionKey, out ValueBasicBlockPair existingValue)) {
				MultiValue value;
				if (existingValue.BasicBlockIndex == curBasicBlock) {
					// If the previous value was stored in the current basic block, then we can safely
					// overwrite the previous value with the new one.
					value = valueToStore;
				} else {
					// If the previous value came from a previous basic block, then some other use of
					// the local could see the previous value, so we must merge the new value with the
					// old value.
					value = MultiValueLattice.Meet (existingValue.Value, valueToStore);
				}
				valueCollection[collectionKey] = new ValueBasicBlockPair (value, curBasicBlock);
			} else if (maxTrackedValues == null || valueCollection.Count < maxTrackedValues) {
				// We're not currently tracking a value a this index, so store the value now.
				valueCollection[collectionKey] = new ValueBasicBlockPair (valueToStore, curBasicBlock);
			}
		}

		// Scans the method as well as any nested functions (local functions or lambdas) and state machines
		// reachable from it.
		public virtual void InterproceduralScan (MethodIL startingMethodIL)
		{
			MethodDefinition startingMethod = startingMethodIL.Method;

			// Note that the default value of a hoisted local will be MultiValueLattice.Top, not UnknownValue.Instance.
			// This ensures that there are no warnings for the "unassigned state" of a parameter.
			// Definite assignment should ensure that there is no way for this to be an analysis hole.
			var interproceduralState = InterproceduralStateLattice.Top;

			var oldInterproceduralState = interproceduralState.Clone ();
			interproceduralState.TrackMethod (startingMethodIL);

			while (!interproceduralState.Equals (oldInterproceduralState)) {
				oldInterproceduralState = interproceduralState.Clone ();

				// Flow state through all methods encountered so far, as long as there
				// are changes discovered in the hoisted local state on entry to any method.
				Debug.Assert (!oldInterproceduralState.MethodBodies.IsUnknown ());
				foreach (var methodIL in oldInterproceduralState.MethodBodies.GetKnownValues ())
					Scan (methodIL, ref interproceduralState);
			}

#if DEBUG
			// Validate that the compiler-generated callees tracked by the compiler-generated state
			// are the same set of methods that we discovered and scanned above.
			if (_context.CompilerGeneratedState.TryGetCompilerGeneratedCalleesForUserMethod (startingMethod, out List<IMemberDefinition>? compilerGeneratedCallees)) {
				var calleeMethods = compilerGeneratedCallees.OfType<MethodDefinition> ();
				// https://github.com/dotnet/linker/issues/2845
				// Disabled asserts due to a bug
				// Debug.Assert (interproceduralState.Count == 1 + calleeMethods.Count ());
				// foreach (var method in calleeMethods)
				//  Debug.Assert (interproceduralState.Any (kvp => kvp.Key.Method == method));
			} else {
				Debug.Assert (interproceduralState.MethodBodies.GetKnownValues().Count () == 1);
			}
#endif
		}

		void TrackNestedFunctionReference (MethodReference referencedMethod, ref InterproceduralState interproceduralState)
		{
			if (_context.TryResolve (referencedMethod) is not MethodDefinition method)
				return;

			if (!CompilerGeneratedNames.IsLambdaOrLocalFunction (method.Name))
				return;

			interproceduralState.TrackMethod (method);
		}

		protected virtual void Scan (MethodIL methodIL, ref InterproceduralState interproceduralState)
		{
			MethodBody methodBody = methodIL.Body;
			MethodDefinition thisMethod = methodBody.Method;

			LocalVariableStore locals = new (methodIL.Variables.Count);

			Dictionary<int, Stack<StackSlot>> knownStacks = new Dictionary<int, Stack<StackSlot>> ();
			Stack<StackSlot>? currentStack = new Stack<StackSlot> (methodBody.MaxStackSize);

			ScanExceptionInformation (knownStacks, methodIL);

			BasicBlockIterator blockIterator = new BasicBlockIterator (methodIL);

			ReturnValue = default;
			foreach (Instruction operation in methodIL.Instructions) {
				int curBasicBlock = blockIterator.MoveNext (operation);

				if (knownStacks.TryGetValue (operation.Offset, out Stack<StackSlot>? knownValue)) {
					if (currentStack == null) {
						// The stack copy constructor reverses the stack
						currentStack = new Stack<StackSlot> (knownValue.Reverse ());
					} else {
						currentStack = MergeStack (currentStack, knownValue);
					}
				}

				currentStack ??= new Stack<StackSlot> (methodBody.MaxStackSize);

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
				case Code.Shl:
				case Code.Shr:
				case Code.Shr_Un:
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
						int value = (int) operation.Operand;
						ConstIntValue civ = new ConstIntValue (value);
						StackSlot slot = new StackSlot (civ);
						currentStack.Push (slot);
					}
					break;

				case Code.Ldc_I4_S: {
						int value = (sbyte) operation.Operand;
						ConstIntValue civ = new ConstIntValue (value);
						StackSlot slot = new StackSlot (civ);
						currentStack.Push (slot);
					}
					break;

				case Code.Arglist:
				case Code.Sizeof:
				case Code.Ldc_I8:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
					PushUnknown (currentStack);
					break;

				case Code.Ldftn:
					TrackNestedFunctionReference ((MethodReference) operation.Operand, ref interproceduralState);
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
					ScanLdarg (operation, currentStack, thisMethod);
					break;

				case Code.Ldloc:
				case Code.Ldloc_0:
				case Code.Ldloc_1:
				case Code.Ldloc_2:
				case Code.Ldloc_3:
				case Code.Ldloc_S:
				case Code.Ldloca:
				case Code.Ldloca_S:
					ScanLdloc (operation, currentStack, methodIL, locals);
					ValidateNoReferenceToReference (locals, methodBody.Method, operation.Offset);
					break;

				case Code.Ldstr: {
						StackSlot slot = new StackSlot (new KnownStringValue ((string) operation.Operand));
						currentStack.Push (slot);
					}
					break;

				case Code.Ldtoken:
					ScanLdtoken (operation, currentStack);
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
				case Code.Ldsfld:
				case Code.Ldflda:
				case Code.Ldsflda:
					ScanLdfld (operation, currentStack, methodBody, ref interproceduralState);
					break;

				case Code.Newarr: {
						StackSlot count = PopUnknown (currentStack, 1, methodBody, operation.Offset);
						currentStack.Push (new StackSlot (ArrayValue.Create (count.Value, (TypeReference) operation.Operand)));
					}
					break;

				case Code.Stelem_I:
				case Code.Stelem_I1:
				case Code.Stelem_I2:
				case Code.Stelem_I4:
				case Code.Stelem_I8:
				case Code.Stelem_R4:
				case Code.Stelem_R8:
				case Code.Stelem_Any:
				case Code.Stelem_Ref:
					ScanStelem (operation, currentStack, methodBody, curBasicBlock);
					break;

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
				case Code.Ldelem_Any:
				case Code.Ldelem_Ref:
				case Code.Ldelema:
					ScanLdelem (operation, currentStack, methodBody, curBasicBlock);
					break;

				case Code.Cpblk:
				case Code.Initblk:
					PopUnknown (currentStack, 3, methodBody, operation.Offset);
					break;

				case Code.Stfld:
				case Code.Stsfld:
					ScanStfld (operation, currentStack, thisMethod, methodBody, locals, ref interproceduralState);
					break;

				case Code.Cpobj:
					PopUnknown (currentStack, 2, methodBody, operation.Offset);
					break;

				case Code.Stind_I:
				case Code.Stind_I1:
				case Code.Stind_I2:
				case Code.Stind_I4:
				case Code.Stind_I8:
				case Code.Stind_R4:
				case Code.Stind_R8:
				case Code.Stind_Ref:
				case Code.Stobj:
					ScanIndirectStore (operation, currentStack, methodBody, locals, curBasicBlock, ref interproceduralState);
					ValidateNoReferenceToReference (locals, methodBody.Method, operation.Offset);
					break;

				case Code.Initobj:
				case Code.Pop:
					PopUnknown (currentStack, 1, methodBody, operation.Offset);
					break;

				case Code.Starg:
				case Code.Starg_S:
					ScanStarg (operation, currentStack, thisMethod, methodBody);
					break;

				case Code.Stloc:
				case Code.Stloc_S:
				case Code.Stloc_0:
				case Code.Stloc_1:
				case Code.Stloc_2:
				case Code.Stloc_3:
					ScanStloc (operation, currentStack, methodIL, locals, curBasicBlock);
					ValidateNoReferenceToReference (locals, methodBody.Method, operation.Offset);
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
					NewKnownStack (knownStacks, ((Instruction) operation.Operand).Offset, currentStack);
					break;

				case Code.Calli: {
						var signature = (CallSite) operation.Operand;
						if (signature.HasThis && !signature.ExplicitThis) {
							PopUnknown (currentStack, 1, methodBody, operation.Offset);
						}

						// Pop arguments
						if (signature.Parameters.Count > 0)
							PopUnknown (currentStack, signature.Parameters.Count, methodBody, operation.Offset);

						// Pop function pointer
						PopUnknown (currentStack, 1, methodBody, operation.Offset);

						if (!signature.ReturnsVoid ())
							PushUnknown (currentStack);
					}
					break;

				case Code.Call:
				case Code.Callvirt:
				case Code.Newobj:
					TrackNestedFunctionReference ((MethodReference) operation.Operand, ref interproceduralState);
					HandleCall (methodBody, operation, currentStack, locals, ref interproceduralState, curBasicBlock);
					ValidateNoReferenceToReference (locals, methodBody.Method, operation.Offset);
					break;

				case Code.Jmp:
					// Not generated by mainstream compilers
					break;

				case Code.Br:
				case Code.Br_S:
					NewKnownStack (knownStacks, ((Instruction) operation.Operand).Offset, currentStack);
					ClearStack (ref currentStack);
					break;

				case Code.Leave:
				case Code.Leave_S:
					ClearStack (ref currentStack);
					NewKnownStack (knownStacks, ((Instruction) operation.Operand).Offset, new Stack<StackSlot> (methodBody.MaxStackSize));
					break;

				case Code.Endfilter:
				case Code.Endfinally:
				case Code.Rethrow:
				case Code.Throw:
					ClearStack (ref currentStack);
					break;

				case Code.Ret: {

						bool hasReturnValue = !methodBody.Method.ReturnsVoid ();

						if (currentStack.Count != (hasReturnValue ? 1 : 0)) {
							WarnAboutInvalidILInMethod (methodBody, operation.Offset);
						}
						if (hasReturnValue) {
							StackSlot retValue = PopUnknown (currentStack, 1, methodBody, operation.Offset);
							// If the return value is a reference, treat it as the value itself for now
							// We can handle ref return values better later
							ReturnValue = MultiValueLattice.Meet (ReturnValue, DereferenceValue (retValue.Value, locals, ref interproceduralState));
							ValidateNoReferenceToReference (locals, methodBody.Method, operation.Offset);
						}
						ClearStack (ref currentStack);
						break;
					}

				case Code.Switch: {
						PopUnknown (currentStack, 1, methodBody, operation.Offset);
						Instruction[] targets = (Instruction[]) operation.Operand;
						foreach (Instruction target in targets) {
							NewKnownStack (knownStacks, target.Offset, currentStack);
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
					NewKnownStack (knownStacks, ((Instruction) operation.Operand).Offset, currentStack);
					break;
				}
			}
		}

		private static void ScanExceptionInformation (Dictionary<int, Stack<StackSlot>> knownStacks, MethodIL methodIL)
		{
			foreach (ExceptionHandler exceptionClause in methodIL.ExceptionHandlers) {
				Stack<StackSlot> catchStack = new Stack<StackSlot> (1);
				catchStack.Push (new StackSlot ());

				if (exceptionClause.HandlerType == ExceptionHandlerType.Filter) {
					NewKnownStack (knownStacks, exceptionClause.FilterStart.Offset, catchStack);
					NewKnownStack (knownStacks, exceptionClause.HandlerStart.Offset, catchStack);
				}
				if (exceptionClause.HandlerType == ExceptionHandlerType.Catch) {
					NewKnownStack (knownStacks, exceptionClause.HandlerStart.Offset, catchStack);
				}
			}
		}

		protected abstract SingleValue GetMethodParameterValue (ParameterProxy parameter);

		private void ScanLdarg (Instruction operation, Stack<StackSlot> currentStack, MethodDefinition thisMethod)
		{
			Code code = operation.OpCode.Code;

			ParameterIndex paramNum = ParameterHelpers.GetParameterIndex (thisMethod, operation);
			ParameterProxy param = thisMethod.GetParameter (paramNum);
			TypeReference paramType = param.ParameterType;

			bool isByRef = code == Code.Ldarga || code == Code.Ldarga_S;
			isByRef |= paramType.IsByRefOrPointer ();
			isByRef |= param.IsImplicitThis && paramType.IsValueType;

			StackSlot slot = new StackSlot (
				isByRef
				? new ParameterReferenceValue (param)
				: GetMethodParameterValue (param));
			currentStack.Push (slot);
		}

		private void ScanStarg (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodDefinition thisMethod,
			MethodBody methodBody)
		{
			var valueToStore = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			ParameterIndex paramNum = ParameterHelpers.GetParameterIndex (thisMethod, operation);
			ParameterProxy param = new (thisMethod, paramNum);
			var targetValue = GetMethodParameterValue (param);
			if (targetValue is MethodParameterValue targetParameterValue)
				HandleStoreParameter (thisMethod, targetParameterValue, operation, valueToStore.Value);

			// If the targetValue is MethodThisValue do nothing - it should never happen really, and if it does, there's nothing we can track there
		}

		private void ScanLdloc (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodIL methodIL,
			LocalVariableStore locals)
		{
			VariableDefinition localDef = GetLocalDef (operation, methodIL.Variables);
			if (localDef == null) {
				PushUnknownAndWarnAboutInvalidIL (currentStack, methodIL.Body, operation.Offset);
				return;
			}

			bool isByRef = operation.OpCode.Code == Code.Ldloca || operation.OpCode.Code == Code.Ldloca_S;

			StackSlot newSlot;
			if (isByRef) {
				newSlot = new StackSlot (new LocalVariableReferenceValue (localDef));
			} else if (locals.TryGetValue (localDef, out ValueBasicBlockPair localValue))
				newSlot = new StackSlot (localValue.Value);
			else
				newSlot = new StackSlot (UnknownValue.Instance);
			currentStack.Push (newSlot);
		}

		void ScanLdtoken (Instruction operation, Stack<StackSlot> currentStack)
		{
			switch (operation.Operand) {
			case GenericParameter genericParameter:
				var param = new RuntimeTypeHandleForGenericParameterValue (genericParameter);
				currentStack.Push (new StackSlot (param));
				return;
			case TypeReference typeReference when ResolveToTypeDefinition (typeReference) is TypeDefinition resolvedDefinition:
				// Note that Nullable types without a generic argument (i.e. Nullable<>) will be RuntimeTypeHandleValue / SystemTypeValue
				if (typeReference is IGenericInstance instance && resolvedDefinition.IsTypeOf (WellKnownType.System_Nullable_T)) {
					switch (instance.GenericArguments[0]) {
					case GenericParameter genericParam:
						var nullableDam = new RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers (new TypeProxy (resolvedDefinition),
							new RuntimeTypeHandleForGenericParameterValue (genericParam));
						currentStack.Push (new StackSlot (nullableDam));
						return;
					case TypeReference underlyingTypeReference when ResolveToTypeDefinition (underlyingTypeReference) is TypeDefinition underlyingType:
						var nullableType = new RuntimeTypeHandleForNullableSystemTypeValue (new TypeProxy (resolvedDefinition), new SystemTypeValue (underlyingType));
						currentStack.Push (new StackSlot (nullableType));
						return;
					default:
						PushUnknown (currentStack);
						return;
					}
				} else {
					var typeHandle = new RuntimeTypeHandleValue (new TypeProxy (resolvedDefinition));
					currentStack.Push (new StackSlot (typeHandle));
					return;
				}
			case MethodReference methodReference when _context.TryResolve (methodReference) is MethodDefinition resolvedMethod:
				var method = new RuntimeMethodHandleValue (resolvedMethod);
				currentStack.Push (new StackSlot (method));
				return;
			default:
				PushUnknown (currentStack);
				return;
			}
		}

		private void ScanStloc (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodIL methodIL,
			LocalVariableStore locals,
			int curBasicBlock)
		{
			StackSlot valueToStore = PopUnknown (currentStack, 1, methodIL.Body, operation.Offset);
			VariableDefinition localDef = GetLocalDef (operation, methodIL.Variables);
			if (localDef == null) {
				WarnAboutInvalidILInMethod (methodIL.Body, operation.Offset);
				return;
			}

			StoreMethodLocalValue (locals, valueToStore.Value, localDef, curBasicBlock);
		}

		private void ScanIndirectStore (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodBody methodBody,
			LocalVariableStore locals,
			int curBasicBlock,
			ref InterproceduralState ipState)
		{
			StackSlot valueToStore = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			StackSlot destination = PopUnknown (currentStack, 1, methodBody, operation.Offset);

			StoreInReference (destination.Value, valueToStore.Value, methodBody.Method, operation, locals, curBasicBlock, ref ipState);
		}

		/// <summary>
		/// Handles storing the source value in a target <see cref="ReferenceValue"/> or MultiValue of ReferenceValues.
		/// </summary>
		/// <param name="target">A set of <see cref="ReferenceValue"/> that a value is being stored into</param>
		/// <param name="source">The value to store</param>
		/// <param name="method">The method body that contains the operation causing the store</param>
		/// <param name="operation">The instruction causing the store</param>
		/// <exception cref="LinkerFatalErrorException">Throws if <paramref name="target"/> is not a valid target for an indirect store.</exception>
		protected void StoreInReference (MultiValue target, MultiValue source, MethodDefinition method, Instruction operation, LocalVariableStore locals, int curBasicBlock, ref InterproceduralState ipState)
		{
			foreach (var value in target.AsEnumerable ()) {
				switch (value) {
				case LocalVariableReferenceValue localReference:
					StoreMethodLocalValue (locals, source, localReference.LocalDefinition, curBasicBlock);
					break;
				case FieldReferenceValue fieldReference
				when GetFieldValue (fieldReference.FieldDefinition).AsSingleValue () is FieldValue fieldValue:
					HandleStoreField (method, fieldValue, operation, source);
					break;
				case ParameterReferenceValue parameterReference
				when GetMethodParameterValue (parameterReference.Parameter) is MethodParameterValue parameterValue:
					HandleStoreParameter (method, parameterValue, operation, source);
					break;
				case MethodReturnValue methodReturnValue:
					// Ref returns don't have special ReferenceValue values, so assume if the target here is a MethodReturnValue then it must be a ref return value
					HandleStoreMethodReturnValue (method, methodReturnValue, operation, source);
					break;
				case FieldValue fieldValue:
					HandleStoreField (method, fieldValue, operation, DereferenceValue (source, locals, ref ipState));
					break;
				case IValueWithStaticType valueWithStaticType:
					if (valueWithStaticType.StaticType is not null && _context.Annotations.FlowAnnotations.IsTypeInterestingForDataflow (valueWithStaticType.StaticType.Value.Type))
						throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage (
							$"Unhandled StoreReference call. Unhandled attempt to store a value in {value} of type {value.GetType ()}.",
							(int) DiagnosticId.LinkerUnexpectedError,
							origin: new MessageOrigin (method, operation.Offset)));
					// This should only happen for pointer derefs, which can't point to interesting types
					break;
				default:
					// These cases should only be refs to array elements
					// References to array elements are not yet tracked and since we don't allow annotations on arrays, they won't cause problems
					break;
				}
			}

		}

		protected abstract MultiValue GetFieldValue (FieldDefinition field);

		private void ScanLdfld (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodBody methodBody,
			ref InterproceduralState interproceduralState)
		{
			Code code = operation.OpCode.Code;
			if (code == Code.Ldfld || code == Code.Ldflda)
				PopUnknown (currentStack, 1, methodBody, operation.Offset);

			bool isByRef = code == Code.Ldflda || code == Code.Ldsflda;

			FieldDefinition? field = _context.TryResolve ((FieldReference) operation.Operand);
			if (field == null) {
				PushUnknown (currentStack);
				return;
			}

			MultiValue value;
			if (isByRef) {
				value = new FieldReferenceValue (field);
			} else if (CompilerGeneratedState.IsHoistedLocal (field)) {
				value = interproceduralState.GetHoistedLocal (new HoistedLocalKey (field));
			} else {
				value = GetFieldValue (field);
			}
			currentStack.Push (new StackSlot (value));
		}

		protected virtual void HandleStoreField (MethodDefinition method, FieldValue field, Instruction operation, MultiValue valueToStore)
		{
		}

		protected virtual void HandleStoreParameter (MethodDefinition method, MethodParameterValue parameter, Instruction operation, MultiValue valueToStore)
		{
		}

		protected virtual void HandleStoreMethodReturnValue (MethodDefinition method, MethodReturnValue thisParameter, Instruction operation, MultiValue sourceValue)
		{
		}

		private void ScanStfld (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodDefinition thisMethod,
			MethodBody methodBody,
			LocalVariableStore locals,
			ref InterproceduralState interproceduralState)
		{
			StackSlot valueToStoreSlot = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			if (operation.OpCode.Code == Code.Stfld)
				PopUnknown (currentStack, 1, methodBody, operation.Offset);

			FieldDefinition? field = _context.TryResolve ((FieldReference) operation.Operand);
			if (field != null) {
				if (CompilerGeneratedState.IsHoistedLocal (field)) {
					interproceduralState.SetHoistedLocal (new HoistedLocalKey (field), valueToStoreSlot.Value);
					return;
				}

				foreach (var value in GetFieldValue (field).AsEnumerable ()) {
					// GetFieldValue may return different node types, in which case they can't be stored to.
					// At least not yet.
					if (value is not FieldValue fieldValue)
						continue;

					// Incomplete handling of ref fields -- if we're storing a reference to a value, pretend it's just the value
					MultiValue valueToStore = DereferenceValue (valueToStoreSlot.Value, locals, ref interproceduralState);

					HandleStoreField (thisMethod, fieldValue, operation, valueToStore);
				}
			}
		}

		private static VariableDefinition GetLocalDef (Instruction operation, Collection<VariableDefinition> localVariables)
		{
			Code code = operation.OpCode.Code;
			if (code >= Code.Ldloc_0 && code <= Code.Ldloc_3)
				return localVariables[code - Code.Ldloc_0];
			if (code >= Code.Stloc_0 && code <= Code.Stloc_3)
				return localVariables[code - Code.Stloc_0];

			return (VariableDefinition) operation.Operand;
		}

		private ValueNodeList PopCallArguments (
			Stack<StackSlot> currentStack,
			MethodReference methodCalled,
			MethodBody containingMethodBody,
			bool isNewObj, int ilOffset,
			out SingleValue? newObjValue)
		{
			newObjValue = null;

			int countToPop = 0;
			if (!isNewObj && methodCalled.HasThis && !methodCalled.ExplicitThis)
				countToPop++;
			countToPop += methodCalled.GetMetadataParametersCount ();

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

		internal MultiValue DereferenceValue (MultiValue maybeReferenceValue, LocalVariableStore locals, ref InterproceduralState interproceduralState)
		{
			MultiValue dereferencedValue = MultiValueLattice.Top;
			foreach (var value in maybeReferenceValue.AsEnumerable ()) {
				switch (value) {
				case FieldReferenceValue fieldReferenceValue:
					dereferencedValue = MultiValue.Union (
						dereferencedValue,
						CompilerGeneratedState.IsHoistedLocal (fieldReferenceValue.FieldDefinition)
							? interproceduralState.GetHoistedLocal (new HoistedLocalKey (fieldReferenceValue.FieldDefinition))
							: GetFieldValue (fieldReferenceValue.FieldDefinition));
					break;
				case ParameterReferenceValue parameterReferenceValue:
					dereferencedValue = MultiValue.Union (
						dereferencedValue,
						GetMethodParameterValue (parameterReferenceValue.Parameter));
					break;
				case LocalVariableReferenceValue localVariableReferenceValue:
					if (locals.TryGetValue (localVariableReferenceValue.LocalDefinition, out var valueBasicBlockPair))
						dereferencedValue = MultiValue.Union (dereferencedValue, valueBasicBlockPair.Value);
					else
						dereferencedValue = MultiValue.Union (dereferencedValue, UnknownValue.Instance);
					break;
				case ReferenceValue referenceValue:
					throw new NotImplementedException ($"Unhandled dereference of ReferenceValue of type {referenceValue.GetType ().FullName}");
				// Incomplete handling for ref values
				case FieldValue fieldValue:
					dereferencedValue = MultiValue.Union (dereferencedValue, fieldValue);
					break;
				default:
					dereferencedValue = MultiValue.Union (dereferencedValue, value);
					break;
				}
			}
			return dereferencedValue;
		}

		/// <summary>
		/// Assigns a MethodParameterValue to the location of each parameter passed by reference. (i.e. assigns the value to x when passing `ref x` as a parameter)
		/// </summary>
		protected void AssignRefAndOutParameters (
			MethodBody callingMethodBody,
			MethodReference calledMethod,
			ValueNodeList methodArguments,
			Instruction operation,
			LocalVariableStore locals,
			int curBasicBlock,
			ref InterproceduralState ipState)
		{
			if (_context.TryResolve (calledMethod) is MethodDefinition calledMethodDefinition) {
				// We resolved the method and can put the ref/out values into the arguments
				foreach (var parameter in calledMethodDefinition.GetParameters ()) {
					if (parameter.GetReferenceKind () is not (ReferenceKind.Ref or ReferenceKind.Out))
						continue;
					var newByRefValue = _context.Annotations.FlowAnnotations.GetMethodParameterValue (parameter);
					StoreInReference (methodArguments[(int) parameter.Index], newByRefValue, callingMethodBody.Method, operation, locals, curBasicBlock, ref ipState);
				}
			} else {
				// We couldn't resolve the method, so we put unknown values into the ref and out arguments
				// Should be a very cold path, so using Linq.Zip should be okay
				foreach (var (argument, refKind) in methodArguments.Zip (calledMethod.GetParameterReferenceKinds ())) {
					if (refKind is not (ReferenceKind.Ref or ReferenceKind.Out))
						continue;
					StoreInReference (argument, UnknownValue.Instance, callingMethodBody.Method, operation, locals, curBasicBlock, ref ipState);
				}
			}
		}

		private void HandleCall (
			MethodBody callingMethodBody,
			Instruction operation,
			Stack<StackSlot> currentStack,
			LocalVariableStore locals,
			ref InterproceduralState interproceduralState,
			int curBasicBlock)
		{
			MethodReference calledMethod = (MethodReference) operation.Operand;

			bool isNewObj = operation.OpCode.Code == Code.Newobj;

			SingleValue? newObjValue;
			ValueNodeList methodArguments = PopCallArguments (currentStack, calledMethod, callingMethodBody, isNewObj,
														   operation.Offset, out newObjValue);
			var dereferencedMethodParams = new List<MultiValue> ();
			foreach (var argument in methodArguments)
				dereferencedMethodParams.Add (DereferenceValue (argument, locals, ref interproceduralState));
			MultiValue methodReturnValue;
			bool handledFunction = HandleCall (
				callingMethodBody,
				calledMethod,
				operation,
				new ValueNodeList (dereferencedMethodParams),
				out methodReturnValue);

			// Handle the return value or newobj result
			if (!handledFunction) {
				if (isNewObj) {
					if (newObjValue == null)
						methodReturnValue = new MultiValue (UnknownValue.Instance);
					else
						methodReturnValue = newObjValue;
				} else {
					if (!calledMethod.ReturnsVoid ()) {
						methodReturnValue = UnknownValue.Instance;
					}
				}
			}

			if (isNewObj || !calledMethod.ReturnsVoid ())
				currentStack.Push (new StackSlot (methodReturnValue));

			AssignRefAndOutParameters (callingMethodBody, calledMethod, methodArguments, operation, locals, curBasicBlock, ref interproceduralState);

			foreach (var param in methodArguments) {
				foreach (var v in param.AsEnumerable ()) {
					if (v is ArrayValue arr) {
						MarkArrayValuesAsUnknown (arr, curBasicBlock);
					}
				}
			}
		}

		public TypeDefinition? ResolveToTypeDefinition (TypeReference typeReference) => typeReference.ResolveToTypeDefinition (_context);

		public abstract bool HandleCall (
			MethodBody callingMethodBody,
			MethodReference calledMethod,
			Instruction operation,
			ValueNodeList methodParams,
			out MultiValue methodReturnValue);

		// Limit tracking array values to 32 values for performance reasons. There are many arrays much longer than 32 elements in .NET, but the interesting ones for trimming are nearly always less than 32 elements.
		private const int MaxTrackedArrayValues = 32;

		private static void MarkArrayValuesAsUnknown (ArrayValue arrValue, int curBasicBlock)
		{
			// Since we can't know the current index we're storing the value at, clear all indices.
			// That way we won't accidentally think we know the value at a given index when we cannot.
			foreach (var knownIndex in arrValue.IndexValues.Keys) {
				// Don't pass MaxTrackedArrayValues since we are only looking at keys we've already seen.
				StoreMethodLocalValue (arrValue.IndexValues, UnknownValue.Instance, knownIndex, curBasicBlock);
			}
		}

		private void ScanStelem (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodBody methodBody,
			int curBasicBlock)
		{
			StackSlot valueToStore = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			StackSlot indexToStoreAt = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			StackSlot arrayToStoreIn = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			int? indexToStoreAtInt = indexToStoreAt.Value.AsConstInt ();
			foreach (var array in arrayToStoreIn.Value.AsEnumerable ()) {
				if (array is ArrayValue arrValue) {
					if (indexToStoreAtInt == null) {
						MarkArrayValuesAsUnknown (arrValue, curBasicBlock);
					} else {
						// When we know the index, we can record the value at that index.
						StoreMethodLocalValue (arrValue.IndexValues, ArrayValue.SanitizeArrayElementValue (valueToStore.Value), indexToStoreAtInt.Value, curBasicBlock, MaxTrackedArrayValues);
					}
				}
			}
		}

		private void ScanLdelem (
			Instruction operation,
			Stack<StackSlot> currentStack,
			MethodBody methodBody,
			int curBasicBlock)
		{
			StackSlot indexToLoadFrom = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			StackSlot arrayToLoadFrom = PopUnknown (currentStack, 1, methodBody, operation.Offset);
			if (arrayToLoadFrom.Value.AsSingleValue () is not ArrayValue arr) {
				PushUnknown (currentStack);
				return;
			}
			// We don't yet handle arrays of references or pointers
			bool isByRef = operation.OpCode.Code == Code.Ldelema;

			int? index = indexToLoadFrom.Value.AsConstInt ();
			if (index == null) {
				PushUnknown (currentStack);
				if (isByRef) {
					MarkArrayValuesAsUnknown (arr, curBasicBlock);
				}
			}
			// Don't try to track refs to array elements. Set it as unknown, then push unknown to the stack
			else if (isByRef) {
				arr.IndexValues[index.Value] = new ValueBasicBlockPair (UnknownValue.Instance, curBasicBlock);
				PushUnknown (currentStack);
			} else if (arr.IndexValues.TryGetValue (index.Value, out ValueBasicBlockPair arrayIndexValue))
				currentStack.Push (new StackSlot (arrayIndexValue.Value));
			else
				PushUnknown (currentStack);
		}
	}
}
