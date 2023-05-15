// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// Evaluates simple properties or methods for constant expressions and
	// then uses this information to remove unreachable conditional blocks and
	// inline collected constants.
	//
	public class UnreachableBlocksOptimizer
	{
		readonly LinkContext _context;
		readonly Dictionary<MethodDefinition, MethodResult?> _cache_method_results = new (2048);
		readonly Stack<MethodDefinition> _resursion_guard = new ();

		MethodDefinition? IntPtrSize, UIntPtrSize;

		public UnreachableBlocksOptimizer (LinkContext context)
		{
			_context = context;
		}

		/// <summary>
		/// Processes the specified method and perform all branch removal optimizations on it.
		/// When this returns it's guaranteed that the method has been optimized (if possible).
		/// </summary>
		/// <param name="method">The method to process</param>
		public void ProcessMethod (MethodDefinition method)
		{
			if (!IsMethodSupported (method))
				return;

			if (_context.Annotations.GetAction (method.Module.Assembly) != AssemblyAction.Link)
				return;

			var reducer = new BodyReducer (method.Body, _context);

			try {
				//
				// If no external dependency can be extracted into constant there won't be
				// anything to optimize in the method
				//
				if (!reducer.ApplyTemporaryInlining (this))
					return;

				//
				// This is the main step which evaluates if any expression can
				// produce folded branches. When it finds them the unreachable
				// branch is removed.
				//
				if (reducer.RewriteBody ())
					_context.LogMessage ($"Reduced '{reducer.InstructionsReplaced}' instructions in conditional branches for [{method.DeclaringType.Module.Assembly.Name}] method '{method.GetDisplayName ()}'.");

				//
				// Note: The inliner cannot run before reducer rewrites body as it
				// would require another recomputing offsets due to instructions replacement
				// done by inliner
				//
				var inliner = new CallInliner (method.Body, this);
				inliner.RewriteBody ();
			} catch (Exception e) {
				throw new InternalErrorException ($"Could not process the body of method '{method.GetDisplayName ()}'.", e);
			}
		}

		static bool IsMethodSupported (MethodDefinition method)
		{
			if (!method.HasBody)
				return false;

			//
			// Block methods which rewrite does not support
			//
			switch (method.ReturnType.MetadataType) {
			case MetadataType.ByReference:
			case MetadataType.FunctionPointer:
				return false;
			}

			return true;
		}

		static bool HasJumpIntoTargetRange (Collection<Instruction> instructions, int firstInstr, int lastInstr, Func<Instruction, int>? mapping = null)
		{
			foreach (var instr in instructions) {
				switch (instr.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Cond_Branch:
					if (instr.Operand is Instruction target) {
						int index = mapping == null ? instructions.IndexOf (target) : mapping (target);
						if (index >= firstInstr && index <= lastInstr)
							return true;
					} else {
						foreach (var rtarget in (Instruction[]) instr.Operand) {
							int index = mapping == null ? instructions.IndexOf (rtarget) : mapping (rtarget);
							if (index >= firstInstr && index <= lastInstr)
								return true;
						}
					}

					break;
				}
			}

			return false;
		}

		static bool IsSideEffectFreeLoad (Instruction instr)
		{
			switch (instr.OpCode.Code) {
			case Code.Ldarg:
			case Code.Ldloc:
			case Code.Ldloc_0:
			case Code.Ldloc_1:
			case Code.Ldloc_2:
			case Code.Ldloc_3:
			case Code.Ldloc_S:
			case Code.Ldc_I4_0:
			case Code.Ldc_I4_1:
			case Code.Ldc_I4_2:
			case Code.Ldc_I4_3:
			case Code.Ldc_I4_4:
			case Code.Ldc_I4_5:
			case Code.Ldc_I4_6:
			case Code.Ldc_I4_7:
			case Code.Ldc_I4_8:
			case Code.Ldc_I4:
			case Code.Ldc_I4_S:
			case Code.Ldc_I4_M1:
			case Code.Ldc_I8:
			case Code.Ldc_R4:
			case Code.Ldc_R8:
			case Code.Ldnull:
			case Code.Ldstr:
				return true;
			}

			return false;
		}

		static bool IsComparisonAlwaysTrue (OpCode opCode, int left, int right)
		{
			switch (opCode.Code) {
			case Code.Beq:
			case Code.Beq_S:
			case Code.Ceq:
				return left == right;
			case Code.Bne_Un:
			case Code.Bne_Un_S:
				return left != right;
			case Code.Bge:
			case Code.Bge_S:
				return left >= right;
			case Code.Bge_Un:
			case Code.Bge_Un_S:
				return (uint) left >= (uint) right;
			case Code.Bgt:
			case Code.Bgt_S:
			case Code.Cgt:
				return left > right;
			case Code.Bgt_Un:
			case Code.Bgt_Un_S:
				return (uint) left > (uint) right;
			case Code.Ble:
			case Code.Ble_S:
				return left <= right;
			case Code.Ble_Un:
			case Code.Ble_Un_S:
				return (uint) left <= (uint) right;
			case Code.Blt:
			case Code.Blt_S:
			case Code.Clt:
				return left < right;
			case Code.Blt_Un:
			case Code.Blt_Un_S:
				return (uint) left < (uint) right;
			}

			throw new NotImplementedException (opCode.ToString ());
		}

		MethodResult? AnalyzeMethodForConstantResult (in CalleePayload callee, Stack<MethodDefinition> callStack)
		{
			MethodDefinition method = callee.Method;

			if (method.ReturnType.MetadataType == MetadataType.Void)
				return null;

			if (!method.HasBody)
				return null;

			switch (_context.Annotations.GetAction (method)) {
			case MethodAction.ConvertToThrow:
				return null;
			case MethodAction.ConvertToStub:
				Instruction? constant = CodeRewriterStep.CreateConstantResultInstruction (_context, method);
				return constant == null ? null : new MethodResult (constant, !HasSideEffects (method));
			}

			if (method.IsIntrinsic () || method.NoInlining)
				return null;

			if (!_context.IsOptimizationEnabled (CodeOptimizations.IPConstantPropagation, method))
				return null;

			var analyzer = new ConstantExpressionMethodAnalyzer (this);
			if (analyzer.Analyze (callee, callStack))
				return new MethodResult (analyzer.Result, analyzer.SideEffectFreeResult);

			return null;
		}

		static bool HasSideEffects (MethodDefinition method)
		{
			return !method.DeclaringType.IsBeforeFieldInit;
		}

		//
		// Return expression with a value when method implementation can be
		// interpreted during trimming
		//
		MethodResult? TryGetMethodCallResult (in CalleePayload callee)
		{
			_resursion_guard.Clear ();
			return TryGetMethodCallResult (callee, _resursion_guard);
		}

		MethodResult? TryGetMethodCallResult (in CalleePayload callee, Stack<MethodDefinition> callStack)
		{
			MethodResult? value;

			MethodDefinition method = callee.Method;
			if (!method.HasMetadataParameters () || callee.HasUnknownArguments) {
				if (!_cache_method_results.TryGetValue (method, out value) && !IsDeepStack (callStack)) {
					value = AnalyzeMethodForConstantResult (callee, callStack);
					_cache_method_results.Add (method, value);
				}

				return value;
			}

			return AnalyzeMethodForConstantResult (callee, callStack);

			static bool IsDeepStack (Stack<MethodDefinition> callStack) => callStack.Count > 100;
		}

		Instruction? GetSizeOfResult (TypeReference type)
		{
			MethodDefinition? sizeOfImpl = null;

			//
			// sizeof (IntPtr) and sizeof (UIntPtr) are just aliases for IntPtr.Size and UIntPtr.Size
			// which are simple static properties commonly overwritten. Instead of forcing C# code style
			// we handle both via static get_Size method
			//
			if (type.MetadataType == MetadataType.UIntPtr) {
				sizeOfImpl = (UIntPtrSize ??= FindSizeMethod (_context.TryResolve (type)));
			} else if (type.MetadataType == MetadataType.IntPtr) {
				sizeOfImpl = (IntPtrSize ??= FindSizeMethod (_context.TryResolve (type)));
			}

			if (sizeOfImpl == null)
				return null;

			return TryGetMethodCallResult (new CalleePayload (sizeOfImpl, Array.Empty<Instruction> ()))?.Instruction;
		}

		static Instruction? EvaluateIntrinsicCall (MethodReference method, Instruction[] arguments)
		{
			//
			// In theory any pure method could be executed via reflection but
			// that would require loading all code path dependencies.
			// For now we handle only few methods that help with core framework trimming
			//
			object? left, right;
			if (method.DeclaringType.MetadataType == MetadataType.String) {
				switch (method.Name) {
				case "op_Equality":
				case "op_Inequality":
				case "Concat":
					if (arguments.Length != 2)
						return null;

					if (!GetConstantValue (arguments[0], out left) ||
						!GetConstantValue (arguments[1], out right))
						return null;

					if (left is string sleft && right is string sright) {
						if (method.Name.Length == 6) // Concat case
							return Instruction.Create (OpCodes.Ldstr, string.Concat (sleft, sright));

						bool result = method.Name.Length == 11 ? sleft == sright : sleft != sright;
						return Instruction.Create (OpCodes.Ldc_I4, result ? 1 : 0); // op_Equality / op_Inequality
					}

					break;
				}
			}

			return null;
		}

		static Instruction[]? GetArgumentsOnStack (MethodDefinition method, Collection<Instruction> instructions, int index)
		{
			if (!method.HasMetadataParameters ())
				return Array.Empty<Instruction> ();

			Instruction[]? result = null;
			for (int i = method.GetMetadataParametersCount (), pos = 0; i != 0; --i, ++pos) {
				Instruction instr = instructions[index - i];
				if (!IsConstantValue (instr))
					return null;

				result ??= new Instruction[method.GetMetadataParametersCount ()];

				result[pos] = instr;
			}

			if (result != null && HasJumpIntoTargetRange (instructions, index - method.GetMetadataParametersCount () + 1, index))
				return null;

			return result;

			static bool IsConstantValue (Instruction instr)
			{
				switch (instr.OpCode.Code) {
				case Code.Ldc_I4_0:
				case Code.Ldc_I4_1:
				case Code.Ldc_I4_2:
				case Code.Ldc_I4_3:
				case Code.Ldc_I4_4:
				case Code.Ldc_I4_5:
				case Code.Ldc_I4_6:
				case Code.Ldc_I4_7:
				case Code.Ldc_I4_8:
				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
				case Code.Ldc_I4_M1:
				case Code.Ldc_I8:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
				case Code.Ldnull:
				case Code.Ldstr:
					return true;
				}

				return false;
			}
		}

		static bool GetConstantValue (Instruction instruction, out object? value)
		{
			switch (instruction.OpCode.Code) {
			case Code.Ldc_I4_0:
				value = 0;
				return true;
			case Code.Ldc_I4_1:
				value = 1;
				return true;
			case Code.Ldc_I4_2:
				value = 2;
				return true;
			case Code.Ldc_I4_3:
				value = 3;
				return true;
			case Code.Ldc_I4_4:
				value = 4;
				return true;
			case Code.Ldc_I4_5:
				value = 5;
				return true;
			case Code.Ldc_I4_6:
				value = 6;
				return true;
			case Code.Ldc_I4_7:
				value = 7;
				return true;
			case Code.Ldc_I4_8:
				value = 8;
				return true;
			case Code.Ldc_I4_M1:
				value = -1;
				return true;
			case Code.Ldc_I4:
				value = (int) instruction.Operand;
				return true;
			case Code.Ldc_I4_S:
				value = (int) (sbyte) instruction.Operand;
				return true;
			case Code.Ldc_I8:
				value = (long) instruction.Operand;
				return true;
			case Code.Ldstr:
				value = (string) instruction.Operand;
				return true;
			case Code.Ldnull:
				value = null;
				return true;
			default:
				value = null;
				return false;
			}
		}

		static MethodDefinition? FindSizeMethod (TypeDefinition? type)
		{
			if (type == null)
				return null;

			return type.Methods.First (l => !l.HasMetadataParameters () && l.IsStatic && l.Name == "get_Size");
		}

		readonly struct CallInliner
		{
			readonly MethodBody body;
			readonly UnreachableBlocksOptimizer optimizer;

			public CallInliner (MethodBody body, UnreachableBlocksOptimizer optimizer)
			{
				this.body = body;
				this.optimizer = optimizer;
			}

			public bool RewriteBody ()
			{
				bool changed = false;
				LinkerILProcessor processor = body.GetLinkerILProcessor ();
#pragma warning disable RS0030 // This optimizer is the reason for the banned API, so it needs to use the Cecil directly
				Collection<Instruction> instrs = body.Instructions;
#pragma warning restore RS0030

				for (int i = 0; i < instrs.Count; ++i) {
					Instruction instr = instrs[i];
					switch (instr.OpCode.Code) {

					case Code.Call:
					case Code.Callvirt:
						MethodDefinition? md = optimizer._context.TryResolve ((MethodReference) instr.Operand);
						if (md == null)
							continue;

						if (md.IsVirtual)
							continue;

						if (md.CallingConvention == MethodCallingConvention.VarArg)
							break;

						if (md.NoInlining)
							break;

						var cpl = new CalleePayload (md, GetArgumentsOnStack (md, instrs, i));
						MethodResult? call_result = optimizer.TryGetMethodCallResult (cpl);
						if (call_result is not MethodResult result)
							break;

						if (!result.IsSideEffectFree) {
							optimizer._context.LogMessage ($"Cannot inline constant result of '{md.GetDisplayName ()}' call due to presence of side effects");
							break;
						}

						if (!md.IsStatic) {
							if (!md.HasMetadataParameters () && CanInlineInstanceCall (instrs, i)) {
								processor.Replace (i - 1, Instruction.Create (OpCodes.Nop));
								processor.Replace (i, result.GetPrototype ()!);
								changed = true;
							}

							continue;
						}

						if (md.HasMetadataParameters ()) {
							if (!IsCalledWithoutSideEffects (md, instrs, i))
								continue;

							for (int p = 1; p <= md.GetMetadataParametersCount (); ++p) {
								processor.Replace (i - p, Instruction.Create (OpCodes.Nop));
							}
						}

						processor.Replace (i, result.GetPrototype ());
						changed = true;
						continue;

					case Code.Sizeof:
						var operand = (TypeReference) instr.Operand;
						Instruction? value = optimizer.GetSizeOfResult (operand);
						if (value != null) {
							processor.Replace (i, value.GetPrototype ());
							changed = true;
						}

						continue;
					}
				}

				return changed;
			}

			bool CanInlineInstanceCall (Collection<Instruction> instructions, int index)
			{
				//
				// Instance methods called on `this` have no side-effects
				//
				if (instructions[index - 1].OpCode.Code == Code.Ldarg_0)
					return !body.Method.IsStatic;

				// More cases can be added later
				return false;
			}

			static bool IsCalledWithoutSideEffects (MethodDefinition method, Collection<Instruction> instructions, int index)
			{
				for (int i = 1; i <= method.GetMetadataParametersCount (); ++i) {
					if (!IsSideEffectFreeLoad (instructions[index - i]))
						return false;
				}

				return true;
			}
		}

		struct BodyReducer
		{
			readonly LinkContext context;
			Dictionary<Instruction, int>? mapping;

			//
			// Sorted list of body instruction indexes which were
			// replaced pass-through nop
			//
			List<int>? conditionInstrsToRemove;

			//
			// Sorted list of body instruction indexes which were
			// set to be replaced with different intstruction
			//
			List<(int, Instruction)>? conditionInstrsToReplace;

			public BodyReducer (MethodBody body, LinkContext context)
			{
				Body = body;
				this.context = context;

				FoldedInstructions = null;
				mapping = null;
				conditionInstrsToRemove = null;
				conditionInstrsToReplace = null;
				InstructionsReplaced = 0;
			}

			public MethodBody Body { get; }

#pragma warning disable RS0030 // This optimizer is the reason for the banned API, so it needs to use the Cecil directly
			Collection<Instruction> Instructions => Body.Instructions;
			Collection<ExceptionHandler> ExceptionHandlers => Body.ExceptionHandlers;
#pragma warning restore RS0030

			public int InstructionsReplaced { get; set; }

			Collection<Instruction>? FoldedInstructions { get; set; }

			[MemberNotNull (nameof(FoldedInstructions))]
			[MemberNotNull (nameof(mapping))]
			void InitializeFoldedInstruction ()
			{
				FoldedInstructions = new Collection<Instruction> (Instructions);
				mapping = new Dictionary<Instruction, int> ();
			}

			public void Rewrite (int index, Instruction newInstruction)
			{
				if (FoldedInstructions == null)
					InitializeFoldedInstruction ();

				Debug.Assert (mapping != null);

				// Tracks mapping for replaced instructions for easier
				// branch targets resolution later
				mapping[Instructions[index]] = index;

				FoldedInstructions[index] = newInstruction;
			}

			void RewriteCondition (int index, Instruction instr, int operand)
			{
				switch (instr.OpCode.Code) {
				case Code.Brfalse:
				case Code.Brfalse_S:
					if (operand == 0) {
						Rewrite (index, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
					} else {
						RewriteConditionToNop (index);
					}

					break;
				case Code.Brtrue:
				case Code.Brtrue_S:
					if (operand != 0) {
						Rewrite (index, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
					} else {
						RewriteConditionToNop (index);
					}

					break;

				case Code.Switch:
					var targets = (Instruction[]) instr.Operand;
					if (operand < targets.Length) {
						// It does not need to be conditional but existing logic in BodySweeper would
						// need to be updated to deal with 1->2 instruction replacement
						RewriteConditionTo (index, Instruction.Create (operand == 0 ? OpCodes.Brfalse : OpCodes.Brtrue, targets[operand]));
						Rewrite (index, Instruction.Create (OpCodes.Br, targets[operand]));
					} else {
						RewriteConditionToNop (index);
					}

					break;
				}
			}

			void RewriteConditionToNop (int index)
			{
				conditionInstrsToRemove ??= new List<int> ();

				conditionInstrsToRemove.Add (index);
				RewriteToNop (index);
			}

			void RewriteConditionTo (int index, Instruction instruction)
			{
				conditionInstrsToReplace ??= new List<(int, Instruction)> ();

				conditionInstrsToReplace.Add ((index, instruction));
			}

			public void RewriteToNop (int index, int stackDepth)
			{
				if (FoldedInstructions == null)
					InitializeFoldedInstruction ();

				int start_index;
				for (start_index = index; start_index >= 0 && stackDepth > 0; --start_index) {
					stackDepth -= GetStackBehaviourDelta (FoldedInstructions[start_index], out bool undefined);
					if (undefined)
						return;
				}

				if (stackDepth != 0) {
					Debug.Fail ("Invalid IL?");
					return;
				}

				while (start_index != index)
					RewriteToNop (++start_index);
			}

			static int GetStackBehaviourDelta (Instruction instruction, out bool unknown)
			{
				int delta = 0;
				unknown = false;
				switch (instruction.OpCode.StackBehaviourPop) {
				case StackBehaviour.Pop0:
					break;
				case StackBehaviour.Pop1:
				case StackBehaviour.Popref:
				case StackBehaviour.Popi:
					--delta;
					break;
				case StackBehaviour.Pop1_pop1:
				case StackBehaviour.Popi_pop1:
				case StackBehaviour.Popi_popi:
				case StackBehaviour.Popi_popi8:
				case StackBehaviour.Popi_popr4:
				case StackBehaviour.Popi_popr8:
				case StackBehaviour.Popref_pop1:
				case StackBehaviour.Popref_popi:
					delta -= 2;
					break;
				case StackBehaviour.Popi_popi_popi:
				case StackBehaviour.Popref_popi_popi:
				case StackBehaviour.Popref_popi_popi8:
				case StackBehaviour.Popref_popi_popr4:
				case StackBehaviour.Popref_popi_popr8:
				case StackBehaviour.Popref_popi_popref:
					delta -= 3;
					break;
				case StackBehaviour.Varpop:
					if (instruction.Operand is IMethodSignature ms) {
						if (ms.HasThis && instruction.OpCode != OpCodes.Newobj)
							--delta;

						delta -= ms.Parameters.Count;
						break;
					}

					if (instruction.OpCode == OpCodes.Ret) {
						unknown = true;
						return 0;
					}

					Debug.Fail (instruction.Operand?.ToString ());
					unknown = true;
					return 0;
				default:
					Debug.Fail (instruction.OpCode.StackBehaviourPop.ToString ());
					unknown = true;
					return 0;
				}

				switch (instruction.OpCode.StackBehaviourPush) {
				case StackBehaviour.Push0:
					break;
				case StackBehaviour.Push1:
				case StackBehaviour.Pushi:
				case StackBehaviour.Pushi8:
				case StackBehaviour.Pushr4:
				case StackBehaviour.Pushr8:
				case StackBehaviour.Pushref:
					++delta;
					break;
				case StackBehaviour.Push1_push1:
					delta += 2;
					break;
				case StackBehaviour.Varpush:
					if (instruction.Operand is IMethodSignature ms) {
						if (ms.ReturnType.MetadataType != MetadataType.Void)
							++delta;

						break;
					}

					Debug.Fail (instruction.Operand?.ToString ());
					unknown = true;
					return 0;
				default:
					Debug.Fail (instruction.OpCode.StackBehaviourPush.ToString ());
					unknown = true;
					return 0;
				}

				return delta;
			}

			void RewriteToNop (int index)
			{
				Rewrite (index, Instruction.Create (OpCodes.Nop));
			}

			public bool RewriteBody ()
			{
				if (FoldedInstructions == null)
					InitializeFoldedInstruction ();

				if (!RemoveConditions ())
					return false;

				BitArray reachableInstrs = GetReachableInstructionsMap (out var unreachableEH);
				if (reachableInstrs == null)
					return false;

				var bodySweeper = new BodySweeper (Body, reachableInstrs, unreachableEH, context);
				bodySweeper.Initialize ();

				bodySweeper.Process (conditionInstrsToRemove, conditionInstrsToReplace, out var nopInstructions);
				InstructionsReplaced = bodySweeper.InstructionsReplaced;
				if (InstructionsReplaced == 0)
					return false;

				reachableInstrs = GetReachableInstructionsMap (out _);
				if (reachableInstrs != null)
					RemoveUnreachableInstructions (reachableInstrs);

				if (nopInstructions != null) {
					LinkerILProcessor processor = Body.GetLinkerILProcessor ();

					foreach (var instr in nopInstructions)
						processor.Remove (instr);
				}

				return true;
			}

			public bool ApplyTemporaryInlining (in UnreachableBlocksOptimizer optimizer)
			{
				bool changed = false;
				var instructions = Instructions;
				Instruction? targetResult;

				for (int i = 0; i < instructions.Count; ++i) {
					var instr = instructions[i];
					switch (instr.OpCode.Code) {

					case Code.Call:
					case Code.Callvirt:
						var md = context.TryResolve ((MethodReference) instr.Operand);
						if (md == null)
							break;

						// Not supported
						if (md.IsVirtual || md.CallingConvention == MethodCallingConvention.VarArg)
							break;

						Instruction[]? args = GetArgumentsOnStack (md, FoldedInstructions ?? instructions, i);
						targetResult = args?.Length > 0 && md.IsStatic ? EvaluateIntrinsicCall (md, args) : null;

						targetResult ??= optimizer.TryGetMethodCallResult (new CalleePayload (md, args))?.Instruction;

						if (targetResult == null)
							break;

						//
						// Do simple arguments stack removal by replacing argument expressions with nops. For cases
						// that require full stack understanding the logic won't work and will leave more opcodes
						// on the stack and constant won't be propagated
						//
						int depth = args?.Length ?? 0;
						if (!md.IsStatic)
							++depth;

						if (depth != 0)
							RewriteToNop (i - 1, depth);

						Rewrite (i, targetResult);
						changed = true;
						break;

					case Code.Ldsfld:
						var ftarget = (FieldReference) instr.Operand;
						var field = context.TryResolve (ftarget);
						if (field == null)
							break;

						if (context.Annotations.TryGetFieldUserValue (field, out object? value)) {
							targetResult = CodeRewriterStep.CreateConstantResultInstruction (context, field.FieldType, value);
							if (targetResult == null)
								break;
							Rewrite (i, targetResult);
							changed = true;
						}
						break;

					case Code.Sizeof:
						var operand = (TypeReference) instr.Operand;
						targetResult = optimizer.GetSizeOfResult (operand);
						if (targetResult != null) {
							Rewrite (i, targetResult);
							changed = true;
						}
						break;
					}
				}

				return changed;
			}

			static bool IsConditionalBranch (OpCode opCode)
			=> opCode.Code is Code.Brfalse or Code.Brfalse_S or Code.Brtrue or Code.Brtrue_S;

			void RemoveUnreachableInstructions (BitArray reachable)
			{
				LinkerILProcessor processor = Body.GetLinkerILProcessor ();

				int removed = 0;
				for (int i = 0; i < reachable.Count; ++i) {
					if (reachable[i])
						continue;

					int index = i - removed;
					// If we intend to remove the last instruction we replaced it with "ret" above (not "nop")
					// but we can't get rid of it completely because it may happen that the last kept instruction
					// is a conditional branch - in which case to keep the IL valid, there has to be something after
					// the conditional branch instruction (the else branch). So if that's the case
					// inject "ldnull; throw;" at the end - this branch should never be reachable and it's always valid
					// (ret may need to return a value of the right type if the method has a return value which is complicated
					// to construct out of nothing).
					if (index == Instructions.Count - 1 && Instructions[index].OpCode == OpCodes.Ret &&
						index > 0 && IsConditionalBranch (Instructions[index - 1].OpCode)) {
						processor.Replace (index, Instruction.Create (OpCodes.Ldnull));
						processor.InsertAfter (Instructions[index], Instruction.Create (OpCodes.Throw));
					} else {
						processor.RemoveAt (index);
						++removed;
					}
				}
			}

			bool RemoveConditions ()
			{
				Debug.Assert (FoldedInstructions != null);
				bool changed = false;
				object? left, right;

				//
				// Finds any branchable instruction and checks if the operand or operands
				// can be evaluated as constant result.
				//
				// The logic does not remove any instructions but replaces them with nops for
				// easier processing later (makes the mapping straigh-forward).
				//
				for (int i = 0; i < FoldedInstructions.Count; ++i) {
					var instr = FoldedInstructions[i];
					var opcode = instr.OpCode;

					if (opcode.FlowControl == FlowControl.Cond_Branch) {
						if (opcode.StackBehaviourPop == StackBehaviour.Pop1_pop1) {
							if (!GetOperandsConstantValues (i, out left, out right))
								continue;

							if (left is int lint && right is int rint) {
								if (IsJumpTargetRange (i - 1, i))
									continue;

								RewriteToNop (i - 2);
								RewriteToNop (i - 1);

								if (IsComparisonAlwaysTrue (opcode, lint, rint)) {
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
								} else {
									RewriteConditionToNop (i);
								}

								changed = true;
								continue;
							}

							continue;
						}

						if (opcode.StackBehaviourPop == StackBehaviour.Popi) {
							if (i > 0 && GetConstantValue (FoldedInstructions[i - 1], out var operand)) {
								if (operand is int opint) {
									if (IsJumpTargetRange (i, i))
										continue;

									RewriteToNop (i - 1);
									RewriteCondition (i, instr, opint);

									changed = true;
									continue;
								}

								if (operand is null && (opcode.Code == Code.Brfalse || opcode.Code == Code.Brfalse_S)) {
									if (IsJumpTargetRange (i, i))
										continue;

									RewriteToNop (i - 1);
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
									changed = true;
									continue;
								}
							}

							// Common pattern generated by C# compiler in debug mode
							if (i >= 3 && GetConstantValue (FoldedInstructions[i - 3], out operand) && operand is int opint2 && IsPairedStlocLdloc (FoldedInstructions[i - 2], FoldedInstructions[i - 1])) {
								if (IsJumpTargetRange (i - 2, i))
									continue;

								RewriteToNop (i - 3);
								RewriteToNop (i - 2);
								RewriteToNop (i - 1);
								RewriteCondition (i, instr, opint2);

								changed = true;
								continue;
							}

							// Pattern for non-zero based switch with constant input
							if (i >= 5 && opcode == OpCodes.Switch && GetConstantValue (FoldedInstructions[i - 5], out operand) && operand is int opint3 && IsPairedStlocLdloc (FoldedInstructions[i - 4], FoldedInstructions[i - 3])) {
								if (IsJumpTargetRange (i - 4, i))
									continue;

								if (!GetConstantValue (FoldedInstructions[i - 2], out operand) || operand is not int offset)
									continue;

								if (FoldedInstructions[i - 1].OpCode != OpCodes.Sub)
									continue;

								RewriteToNop (i - 5);
								RewriteToNop (i - 4);
								RewriteToNop (i - 3);
								RewriteCondition (i, instr, opint3 - offset);

								changed = true;
								continue;
							}

							continue;
						}

						throw new NotImplementedException ();
					}

					// Mode special for csc in debug mode
					switch (instr.OpCode.Code) {
					case Code.Ceq:
					case Code.Clt:
					case Code.Cgt:
						if (!GetOperandsConstantValues (i, out left, out right))
							continue;

						if (left is int lint && right is int rint) {
							if (IsJumpTargetRange (i - 1, i))
								continue;

							RewriteToNop (i - 2);
							RewriteToNop (i - 1);

							if (IsComparisonAlwaysTrue (instr.OpCode, lint, rint)) {
								Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_1));
							} else {
								Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_0));
							}

							changed = true;
						}

						break;

					case Code.Cgt_Un:
						if (!GetOperandsConstantValues (i, out left, out right))
							continue;

						if (IsJumpTargetRange (i - 1, i))
							continue;

						if (left == null && right == null) {
							Rewrite (i, Instruction.Create (OpCodes.Ldc_I4_0));
						}

						changed = true;
						break;
					}
				}

				return changed;
			}

			BitArray GetReachableInstructionsMap (out List<ExceptionHandler>? unreachableHandlers)
			{
				Debug.Assert (FoldedInstructions != null);
				unreachableHandlers = null;
				var reachable = new BitArray (FoldedInstructions.Count);

				Stack<int>? condBranches = null;
				bool exceptionHandlersChecked = !Body.HasExceptionHandlers;
				Instruction target;
				int i = 0;
				while (true) {
					while (i < FoldedInstructions.Count) {
						if (reachable[i])
							break;

						reachable[i] = true;
						var instr = FoldedInstructions[i++];

						switch (instr.OpCode.FlowControl) {
						case FlowControl.Branch:
							target = (Instruction) instr.Operand;
							i = GetInstructionIndex (target);
							continue;

						case FlowControl.Cond_Branch:
							condBranches ??= new Stack<int> ();

							switch (instr.Operand) {
							case Instruction starget:
								condBranches.Push (GetInstructionIndex (starget));
								continue;
							case Instruction[] mtargets:
								foreach (var t in mtargets)
									condBranches.Push (GetInstructionIndex (t));
								continue;
							default:
								throw new NotImplementedException ();
							}

						case FlowControl.Next:
						case FlowControl.Call:
						case FlowControl.Meta:
							continue;

						case FlowControl.Return:
						case FlowControl.Throw:
							break;

						default:
							throw new NotImplementedException ();
						}

						break;
					}

					if (condBranches?.Count > 0) {
						i = condBranches.Pop ();
						continue;
					}

					if (!exceptionHandlersChecked) {
						exceptionHandlersChecked = true;

						var instrs = Instructions;
						foreach (var handler in ExceptionHandlers) {
							int start = instrs.IndexOf (handler.TryStart);
							int end = instrs.IndexOf (handler.TryEnd) - 1;

							if (!HasAnyBitSet (reachable, start, end)) {
								unreachableHandlers ??= new List<ExceptionHandler> ();

								unreachableHandlers.Add (handler);
								continue;
							}

							condBranches ??= new Stack<int> ();

							condBranches.Push (GetInstructionIndex (handler.HandlerStart));
							if (handler.FilterStart != null)
								condBranches.Push (GetInstructionIndex (handler.FilterStart));
						}

						if (condBranches?.Count > 0) {
							i = condBranches.Pop ();
							continue;
						}
					}

					return reachable;
				}
			}

			static bool HasAnyBitSet (BitArray bitArray, int startIndex, int endIndex)
			{
				for (int i = startIndex; i <= endIndex; ++i) {
					if (bitArray[i])
						return true;
				}

				return false;
			}

			//
			// Returns index of instruction in folded instruction body
			//
			int GetInstructionIndex (Instruction instruction)
			{
				Debug.Assert (FoldedInstructions != null && mapping != null);
				if (mapping.TryGetValue (instruction, out int idx))
					return idx;

				idx = FoldedInstructions.IndexOf (instruction);
				Debug.Assert (idx >= 0);
				return idx;
			}

			bool GetOperandsConstantValues (int index, out object? left, out object? right)
			{
				Debug.Assert (FoldedInstructions != null);
				left = default;
				right = default;

				if (index < 2)
					return false;

				return GetConstantValue (FoldedInstructions[index - 2], out left) &&
					GetConstantValue (FoldedInstructions[index - 1], out right);
			}

			static bool IsPairedStlocLdloc (Instruction first, Instruction second)
			{
				switch (first.OpCode.Code) {
				case Code.Stloc_0:
					return second.OpCode.Code == Code.Ldloc_0;
				case Code.Stloc_1:
					return second.OpCode.Code == Code.Ldloc_1;
				case Code.Stloc_2:
					return second.OpCode.Code == Code.Ldloc_2;
				case Code.Stloc_3:
					return second.OpCode.Code == Code.Ldloc_3;
				case Code.Stloc_S:
				case Code.Stloc:
					if (second.OpCode.Code == Code.Ldloc_S || second.OpCode.Code == Code.Ldloc)
						return ((VariableDefinition) first.Operand).Index == ((VariableDefinition) second.Operand).Index;

					break;
				}

				return false;
			}

			bool IsJumpTargetRange (int firstInstr, int lastInstr)
			{
				Debug.Assert (FoldedInstructions != null);
				return HasJumpIntoTargetRange (FoldedInstructions, firstInstr, lastInstr, GetInstructionIndex);
			}
		}

		struct BodySweeper
		{
			readonly MethodBody body;
#pragma warning disable RS0030 // This optimizer is the reason for the banned API, so it needs to use the Cecil directly
			Collection<Instruction> Instructions => body.Instructions;
			Collection<VariableDefinition> Variables => body.Variables;
			Collection<ExceptionHandler> ExceptionHandlers => body.ExceptionHandlers;
#pragma warning restore RS0030
			readonly BitArray reachable;
			readonly List<ExceptionHandler>? unreachableExceptionHandlers;
			readonly LinkContext context;
			LinkerILProcessor? ilprocessor;
			LinkerILProcessor ILProcessor {
				get {
					Debug.Assert (ilprocessor != null);
					return ilprocessor;
				}
			}

			public BodySweeper (MethodBody body, BitArray reachable, List<ExceptionHandler>? unreachableEH, LinkContext context)
			{
				this.body = body;
				this.reachable = reachable;
				this.unreachableExceptionHandlers = unreachableEH;
				this.context = context;

				InstructionsReplaced = 0;
				ilprocessor = null;
			}

			public int InstructionsReplaced { get; set; }

			public void Initialize ()
			{
				var instrs = Instructions;

				//
				// Reusing same reachable map and altering it at indexes
				// which will remain same during replacement processing
				//
				for (int i = 0; i < instrs.Count; ++i) {
					if (reachable[i])
						continue;

					var instr = instrs[i];
					switch (instr.OpCode.Code) {
					case Code.Nop:
						reachable[i] = true;
						continue;

					case Code.Ret:
						if (i == instrs.Count - 1)
							reachable[i] = true;

						break;
					}
				}

				ilprocessor = body.GetLinkerILProcessor ();
			}

			public void Process (List<int>? conditionInstrsToRemove, List<(int, Instruction)>? conditionInstrsToReplace, out List<Instruction>? sentinelNops)
			{
				List<VariableDefinition>? removedVariablesReferences = null;
				var instrs = Instructions;

				//
				// Process list of conditional instructions that were set to be replaced and not removed
				//
				if (conditionInstrsToReplace != null) {
					foreach (var pair in conditionInstrsToReplace) {
						var instr = instrs[pair.Item1];
						switch (instr.OpCode.StackBehaviourPop) {
						case StackBehaviour.Popi:
							ILProcessor.Replace (pair.Item1, pair.Item2);
							InstructionsReplaced++;
							break;
						default:
							Debug.Fail ("not supported");
							break;
						}
					}

				}

				//
				// Initial pass which replaces unreachable instructions with nops or
				// ret to keep the body verifiable
				//
				for (int i = 0; i < instrs.Count; ++i) {
					if (reachable[i])
						continue;

					var instr = instrs[i];

					Instruction newInstr;
					if (i == instrs.Count - 1) {
						newInstr = Instruction.Create (OpCodes.Ret);
					} else {
						newInstr = Instruction.Create (OpCodes.Nop);
					}

					ILProcessor.Replace (i, newInstr);
					InstructionsReplaced++;

					VariableDefinition? variable = GetVariableReference (instr);
					if (variable != null) {
						removedVariablesReferences ??= new List<VariableDefinition> ();
						if (!removedVariablesReferences.Contains (variable))
							removedVariablesReferences.Add (variable);
					}
				}

				CleanExceptionHandlers ();

				sentinelNops = null;

				//
				// Process list of conditional jump which should be removed. They cannot be
				// replaced with nops as they alter the stack
				//
				if (conditionInstrsToRemove != null) {
					int bodyExpansion = 0;

					foreach (int instrIndex in conditionInstrsToRemove) {
						var index = instrIndex + bodyExpansion;
						var instr = instrs[index];

						switch (instr.OpCode.StackBehaviourPop) {
						case StackBehaviour.Pop1_pop1:

							InstructionsReplaced += 2;

							//
							// One of the operands is most likely constant and could just be removed instead of additional pop
							//
							if (index > 0 && IsSideEffectFreeLoad (instrs[index - 1])) {
								var nop = Instruction.Create (OpCodes.Nop);

								sentinelNops ??= new List<Instruction> ();
								sentinelNops.Add (nop);

								ILProcessor.Replace (index - 1, Instruction.Create (OpCodes.Pop));
								ILProcessor.Replace (index, nop);
							} else {
								var pop = Instruction.Create (OpCodes.Pop);
								ILProcessor.Replace (index, pop);
								ILProcessor.InsertAfter (pop, Instruction.Create (OpCodes.Pop));

								//
								// conditionInstrsToRemove is always sorted and instead of
								// increasing remaining indexes we introduce index delta value
								//
								bodyExpansion++;
							}
							break;
						case StackBehaviour.Popi:
							ILProcessor.Replace (index, Instruction.Create (OpCodes.Pop));
							InstructionsReplaced++;
							break;
						}
					}
				}

				//
				// Replacing instructions with nops can make local variables unused. Process them
				// as the last step to reduce more type dependencies
				//
				if (removedVariablesReferences != null) {
					CleanRemovedVariables (removedVariablesReferences);
				}
			}

			void CleanRemovedVariables (List<VariableDefinition> variables)
			{
				foreach (var instr in Instructions) {
					VariableDefinition? variable = GetVariableReference (instr);
					if (variable == null)
						continue;

					if (!variables.Remove (variable))
						continue;

					if (variables.Count == 0)
						return;
				}

				variables.Sort ((a, b) => b.Index.CompareTo (a.Index));
				var body_variables = Variables;

				foreach (var variable in variables) {
					var index = body_variables.IndexOf (variable);

					//
					// Remove variable only if it's the last one. Instead of
					// re-indexing all variables change it to System.Object,
					// which is enough to drop the dependency
					//
					if (index == body_variables.Count - 1) {
						body_variables.RemoveAt (index);
					} else {
						var objectType = BCL.FindPredefinedType (WellKnownType.System_Object, context);
						body_variables[index].VariableType = objectType ?? throw new NotSupportedException ("Missing predefined 'System.Object' type");
					}
				}
			}

			void CleanExceptionHandlers ()
			{
				if (unreachableExceptionHandlers == null)
					return;

				foreach (var eh in unreachableExceptionHandlers)
					ExceptionHandlers.Remove (eh);
			}

			VariableDefinition? GetVariableReference (Instruction instruction)
			{
				switch (instruction.OpCode.Code) {
				case Code.Stloc_0:
				case Code.Ldloc_0:
					return Variables[0];
				case Code.Stloc_1:
				case Code.Ldloc_1:
					return Variables[1];
				case Code.Stloc_2:
				case Code.Ldloc_2:
					return Variables[2];
				case Code.Stloc_3:
				case Code.Ldloc_3:
					return Variables[3];
				}

				if (instruction.Operand is VariableReference vr)
					return vr.Resolve ();

				return null;
			}
		}

		struct ConstantExpressionMethodAnalyzer
		{
			readonly LinkContext context;
			readonly UnreachableBlocksOptimizer optimizer;

			Stack<Instruction>? stack_instr;
			Dictionary<int, Instruction>? locals;

			public ConstantExpressionMethodAnalyzer (UnreachableBlocksOptimizer optimizer)
			{
				this.optimizer = optimizer;
				this.context = optimizer._context;
				stack_instr = null;
				locals = null;
				Result = null;
				SideEffectFreeResult = true;
			}

			//
			// Single expression that is representing the evaluation result with the specific
			// callee arguments
			//
			public Instruction? Result { get; private set; }

			//
			// Returns true when the method evaluation with specific arguments does not cause
			// any observable side effect (e.g. possible NRE, field access, etc)
			//
			public bool SideEffectFreeResult { get; private set; }

			[MemberNotNullWhen (true, nameof(Result))]
			public bool Analyze (in CalleePayload callee, Stack<MethodDefinition> callStack)
			{
				MethodDefinition method = callee.Method;
				Instruction[]? arguments = callee.Arguments;
#pragma warning disable RS0030 // This optimizer is the reason for the banned API, so it needs to use the Cecil directly
				Collection<Instruction> instructions = callee.Method.Body.Instructions;
#pragma warning restore RS0030
				MethodBody body = method.Body;

				VariableReference vr;
				Instruction? jmpTarget = null;
				Instruction? linstr;
				object? left, right, operand;

				SideEffectFreeResult = !HasSideEffects (method);

				//
				// We could implement a full-blown interpreter here but for now, it handles
				// cases used in runtime libraries
				//
				for (int i = 0; i < instructions.Count; ++i) {
					var instr = instructions[i];

					if (jmpTarget != null) {
						//
						// Handles both backward and forward jumps
						//
						if (instr != jmpTarget)
							continue;

						jmpTarget = null;
					}

					switch (instr.OpCode.Code) {
					case Code.Nop:
					case Code.Volatile:
						continue;
					case Code.Pop:
						Debug.Assert (stack_instr != null, "invalid il?");
						stack_instr?.Pop ();
						continue;

					case Code.Br_S:
					case Code.Br:
						jmpTarget = (Instruction) instr.Operand;
						continue;

					case Code.Brfalse_S:
					case Code.Brfalse: {
							if (!GetOperandConstantValue (out operand))
								return false;

							if (operand is int oint) {
								if (oint == 0)
									jmpTarget = (Instruction) instr.Operand;

								continue;
							}

							return false;
						}

					case Code.Brtrue_S:
					case Code.Brtrue: {
							if (!GetOperandConstantValue (out operand))
								return false;

							if (operand is int oint) {
								if (oint == 1)
									jmpTarget = (Instruction) instr.Operand;

								continue;
							}

							return false;
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
						if (EvaluateConditionalJump (instr, out jmpTarget))
							continue;
						return false;

					case Code.Ldc_I4:
					case Code.Ldc_I4_S:
					case Code.Ldc_I4_0:
					case Code.Ldc_I4_1:
					case Code.Ldc_I4_2:
					case Code.Ldc_I4_3:
					case Code.Ldc_I4_4:
					case Code.Ldc_I4_5:
					case Code.Ldc_I4_6:
					case Code.Ldc_I4_7:
					case Code.Ldc_I4_8:
					case Code.Ldc_I4_M1:
					case Code.Ldc_I8:
					case Code.Ldnull:
					case Code.Ldstr:
					case Code.Ldtoken:
						PushOnStack (instr);
						continue;

					case Code.Ldloc_0:
						linstr = GetLocalsValue (0, body);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;
					case Code.Ldloc_1:
						linstr = GetLocalsValue (1, body);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;
					case Code.Ldloc_2:
						linstr = GetLocalsValue (2, body);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;
					case Code.Ldloc_3:
						linstr = GetLocalsValue (3, body);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;
					case Code.Ldloc:
					case Code.Ldloc_S:
						vr = (VariableReference) instr.Operand;
						linstr = GetLocalsValue (vr.Index, body);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;
					case Code.Stloc_0:
						StoreToLocals (0);
						continue;
					case Code.Stloc_1:
						StoreToLocals (1);
						continue;
					case Code.Stloc_2:
						StoreToLocals (2);
						continue;
					case Code.Stloc_3:
						StoreToLocals (3);
						continue;
					case Code.Stloc_S:
					case Code.Stloc:
						vr = (VariableReference) instr.Operand;
						StoreToLocals (vr.Index);
						continue;

					case Code.Ldarg_0:
						if (!method.IsStatic) {
							PushOnStack (instr);
							continue;
						}

						linstr = GetArgumentValue (arguments, 0);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;

					case Code.Ldarg_1:
						if (!method.IsStatic)
							return false;

						linstr = GetArgumentValue (arguments, 1);
						if (linstr == null)
							return false;

						PushOnStack (linstr);
						continue;

					case Code.Ldsfld: {
							var ftarget = (FieldReference) instr.Operand;
							FieldDefinition? field = context.TryResolve (ftarget);
							if (field == null)
								return false;

							if (context.Annotations.TryGetFieldUserValue (field, out object? value)) {
								linstr = CodeRewriterStep.CreateConstantResultInstruction (context, field.FieldType, value);
								if (linstr == null)
									return false;
							} else {
								SideEffectFreeResult = false;
								linstr = instr;
							}

							PushOnStack (linstr);
							continue;
						}

					case Code.Ceq: {
							if (!GetOperandsConstantValues (out right, out left))
								return false;

							if (left is int lint && right is int rint) {
								PushOnStack (Instruction.Create (OpCodes.Ldc_I4, lint == rint ? 1 : 0));
								continue;
							}

							if (left is long llong && right is long rlong) {
								PushOnStack (Instruction.Create (OpCodes.Ldc_I4, llong == rlong ? 1 : 0));
								continue;
							}

							return false;
						}

					case Code.Conv_I8: {
							if (!GetOperandConstantValue (out operand))
								return false;

							if (operand is int oint) {
								PushOnStack (Instruction.Create (OpCodes.Ldc_I8, (long) oint));
								continue;
							}

							// TODO: Handle more types
							return false;
						}

					case Code.Call:
					case Code.Callvirt: {
							MethodReference mr = (MethodReference) instr.Operand;
							MethodDefinition? md = optimizer._context.TryResolve (mr);
							if (md == null || md == method)
								return false;

							if (md.IsVirtual)
								return false;

							Instruction[]? args;
							if (!md.HasMetadataParameters ()) {
								args = Array.Empty<Instruction> ();
							} else {
								//
								// Don't need to check for ref/out because ldloca like instructions are not supported
								//
								args = GetArgumentsOnStack (md);
								if (args == null)
									return false;
							}

							if (md.ReturnType.MetadataType == MetadataType.Void) {
								// For now consider all void methods as side-effect causing
								SideEffectFreeResult = false;
								continue;
							}

							if (!md.IsStatic && !CanEvaluateInstanceMethodCall (method))
								return false;

							//
							// Evaluate known framework methods
							//
							if (args.Length > 0) {
								linstr = EvaluateIntrinsicCall (md, args);
								if (linstr != null) {
									PushOnStack (linstr);
									continue;
								}
							}

							//
							// Guard against stack overflow on recursive calls. This could be turned into
							// a warning if we check arguments too
							//
							if (callStack.Contains (md))
								return false;

							callStack.Push (method);
							MethodResult? call_result = optimizer.TryGetMethodCallResult (new CalleePayload (md, args), callStack);
							if (!callStack.TryPop (out _))
								return false;

							if (call_result is MethodResult result) {
								if (!result.IsSideEffectFree)
									SideEffectFreeResult = false;

								PushOnStack (result.Instruction);
								continue;
							}

							return false;
						}

					case Code.Sizeof: {
							var type = (TypeReference) instr.Operand;
							linstr = optimizer.GetSizeOfResult (type);
							if (linstr != null) {
								PushOnStack (linstr);
								continue;
							}

							return false;
						}

					case Code.Ret:
						if (ConvertStackToResult ())
							return true;

						break;
					}

					return false;
				}

				return false;
			}

			bool CanEvaluateInstanceMethodCall (MethodDefinition context)
			{
				if (stack_instr == null || !stack_instr.TryPop (out Instruction? instr))
					return false;

				switch (instr.OpCode.Code) {
				case Code.Ldarg_0:
					if (!context.IsStatic)
						return true;

					goto default;
				default:
					// We are not inlining hence can evaluate anything and decide later
					// how to handle sitation when the result is not deterministic
					SideEffectFreeResult = false;
					return true;
				}
			}

			bool EvaluateConditionalJump (Instruction instr, out Instruction? target)
			{
				if (!GetOperandsConstantValues (out object? right, out object? left)) {
					target = null;
					return false;
				}

				if (left is int lint && right is int rint) {
					if (IsComparisonAlwaysTrue (instr.OpCode, lint, rint))
						target = (Instruction) instr.Operand;
					else
						target = null;

					return true;
				}

				target = null;
				return false;
			}

			[MemberNotNullWhen (true, nameof(Result))]
			bool ConvertStackToResult ()
			{
				if (stack_instr == null)
					return false;

				if (stack_instr.Count != 1)
					return false;

				var instr = stack_instr.Pop ();

				switch (instr.OpCode.Code) {
				case Code.Ldc_I4_0:
				case Code.Ldc_I4_1:
				case Code.Ldc_I4_2:
				case Code.Ldc_I4_3:
				case Code.Ldc_I4_4:
				case Code.Ldc_I4_5:
				case Code.Ldc_I4_6:
				case Code.Ldc_I4_7:
				case Code.Ldc_I4_8:
				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
				case Code.Ldc_I4_M1:
				case Code.Ldc_I8:
				case Code.Ldnull:
				case Code.Ldstr:
					Result = instr;
					return true;
				}

				return false;
			}

			static Instruction? GetArgumentValue (Instruction[]? arguments, int index)
			{
				if (arguments == null)
					return null;

				return index < arguments.Length ? arguments[index] : null;
			}

			Instruction[]? GetArgumentsOnStack (MethodDefinition method)
			{
				int length = method.GetMetadataParametersCount ();
				Debug.Assert (length != 0);
				if (stack_instr?.Count < length)
					return null;

				var result = new Instruction[length];
				while (length != 0)
					result[--length] = stack_instr!.Pop ();

				return result;
			}

			Instruction? GetLocalsValue (int index, MethodBody body)
			{
				if (locals != null && locals.TryGetValue (index, out Instruction? instruction))
					return instruction;

				if (!body.InitLocals)
					return null;

#pragma warning disable RS0030 // This optimizer is the reason for the banned API, so it needs to use the Cecil directly
				var variables = body.Variables;
#pragma warning restore RS0030

				// local variables don't need to be explicitly initialized
				return CodeRewriterStep.CreateConstantResultInstruction (context, variables[index].VariableType);
			}

			bool GetOperandConstantValue ([NotNullWhen (true)] out object? value)
			{
				if (stack_instr == null) {
					value = null;
					return false;
				}

				Instruction? instr;
				if (!stack_instr.TryPop (out instr)) {
					value = null;
					return false;
				}

				return GetConstantValue (instr, out value);
			}

			bool GetOperandsConstantValues ([NotNullWhen (true)] out object? left, [NotNullWhen (true)] out object? right)
			{
				if (stack_instr == null) {
					left = right = null;
					return false;
				}

				Instruction? instr;
				if (!stack_instr.TryPop (out instr)) {
					left = right = null;
					return false;
				}

				if (instr == null) {
					left = right = null;
					return false;
				}

				if (!GetConstantValue (instr, out left)) {
					left = right = null;
					return false;
				}

				if (!stack_instr.TryPop (out instr)) {
					left = right = null;
					return false;
				}

				if (instr is null) {
					left = right = null;
					return false;
				}

				return GetConstantValue (instr, out right);
			}

			void PushOnStack (Instruction instruction)
			{
				stack_instr ??= new Stack<Instruction> ();

				stack_instr.Push (instruction);
			}

			void StoreToLocals (int index)
			{
				locals ??= new Dictionary<int, Instruction> ();

				if (stack_instr == null)
					Debug.Fail ("Invalid IL?");
				locals[index] = stack_instr.Pop ();
			}
		}

		readonly record struct CalleePayload (MethodDefinition Method, Instruction[]? Arguments = null)
		{
			public bool HasUnknownArguments => Arguments is null;
		}

		readonly record struct MethodResult (Instruction Instruction, bool IsSideEffectFree)
		{
			public Instruction GetPrototype () => Instruction.GetPrototype ();
		}
	}
}
