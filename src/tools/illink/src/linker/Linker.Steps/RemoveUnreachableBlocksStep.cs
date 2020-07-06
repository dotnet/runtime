using System;
using System.Diagnostics;
using System.Linq;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using System.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// This steps evaluates simple properties or methods for constant expressions and
	// then uses this information to remove unreachable conditional blocks. It does
	// not do any inlining-like code changes.
	//
	public class RemoveUnreachableBlocksStep : BaseStep
	{
		Dictionary<MethodDefinition, Instruction> constExprMethods;
		MethodDefinition IntPtrSize, UIntPtrSize;

		protected override void Process ()
		{
			var assemblies = Context.Annotations.GetAssemblies ().ToArray ();

			constExprMethods = new Dictionary<MethodDefinition, Instruction> ();
			foreach (var assembly in assemblies) {
				FindConstantExpressionsMethods (assembly.MainModule.Types);
			}

			if (constExprMethods.Count == 0)
				return;

			int constExprMethodsCount;
			do {
				//
				// Body rewriting can produce more methods with constant expression
				//
				constExprMethodsCount = constExprMethods.Count;

				foreach (var assembly in assemblies) {
					if (Annotations.GetAction (assembly) != AssemblyAction.Link)
						continue;

					RewriteBodies (assembly.MainModule.Types);
				}
			} while (constExprMethodsCount < constExprMethods.Count);
		}

		void FindConstantExpressionsMethods (Collection<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.IsInterface)
					continue;

				if (!type.HasMethods)
					continue;

				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					if (method.ReturnType.MetadataType == MetadataType.Void)
						continue;

					switch (Annotations.GetAction (method)) {
					case MethodAction.ConvertToThrow:
						continue;
					case MethodAction.ConvertToStub:
						var instruction = CodeRewriterStep.CreateConstantResultInstruction (Context, method);
						if (instruction != null)
							constExprMethods[method] = instruction;

						continue;
					}

					if (method.IsIntrinsic () || method.NoInlining)
						continue;

					if (constExprMethods.ContainsKey (method))
						continue;

					if (!Context.IsOptimizationEnabled (CodeOptimizations.IPConstantPropagation, method))
						continue;

					var analyzer = new ConstantExpressionMethodAnalyzer (method);
					if (analyzer.Analyze ()) {
						constExprMethods[method] = analyzer.Result;
					}
				}

				if (type.HasNestedTypes)
					FindConstantExpressionsMethods (type.NestedTypes);
			}
		}

		void RewriteBodies (Collection<TypeDefinition> types)
		{
			foreach (var type in types) {
				if (type.IsInterface)
					continue;

				if (!type.HasMethods)
					continue;

				foreach (var method in type.Methods) {
					if (!method.HasBody)
						continue;

					//
					// Block methods which rewrite does not support
					//
					switch (method.ReturnType.MetadataType) {
					case MetadataType.ByReference:
					case MetadataType.FunctionPointer:
						continue;
					}

					RewriteBody (method);
				}

				if (type.HasNestedTypes)
					RewriteBodies (type.NestedTypes);
			}
		}

		void RewriteBody (MethodDefinition method)
		{
			var reducer = new BodyReducer (method.Body, Context);

			//
			// Temporary inlines any calls which return contant expression
			//
			if (!TryInlineBodyDependencies (ref reducer))
				return;

			//
			// This is the main step which evaluates if inlined calls can
			// produce folded branches. When it finds them the unreachable
			// branch is replaced with nops.
			//
			if (reducer.RewriteBody ())
				Context.LogMessage ($"Reduced '{reducer.InstructionsReplaced}' instructions in conditional branches for [{method.DeclaringType.Module.Assembly.Name}] method {method.GetDisplayName ()}");

			// Even if the rewriter doesn't find any branches to fold the inlining above may have changed the method enough
			// such that we can now deduce its return value.

			if (method.ReturnType.MetadataType == MetadataType.Void)
				return;

			//
			// Re-run the analyzer in case body change rewrote it to constant expression
			//
			var analyzer = new ConstantExpressionMethodAnalyzer (method, reducer.FoldedInstructions);
			if (analyzer.Analyze ()) {
				constExprMethods[method] = analyzer.Result;
			}
		}

		bool TryInlineBodyDependencies (ref BodyReducer reducer)
		{
			bool changed = false;
			var instructions = reducer.Body.Instructions;
			Instruction targetResult;

			for (int i = 0; i < instructions.Count; ++i) {
				var instr = instructions[i];
				switch (instr.OpCode.Code) {

				case Code.Call:
				case Code.Callvirt:
					var target = (MethodReference) instr.Operand;
					var md = target.Resolve ();
					if (md == null)
						break;

					if (!constExprMethods.TryGetValue (md, out targetResult))
						break;

					if (md.CallingConvention == MethodCallingConvention.VarArg)
						break;

					bool explicitlyAnnotated = Annotations.GetAction (md) == MethodAction.ConvertToStub;

					// Allow inlining results of instance methods which are explicitly annotated
					// but don't allow inling results of any other instance method.
					// See https://github.com/mono/linker/issues/1243 for discussion as to why.
					// Also explicitly prevent inlining results of virtual methods.
					if (!md.IsStatic &&
						(md.IsVirtual || !explicitlyAnnotated))
						break;

					// Allow inlining results of methods with by-value parameters which are explicitly annotated
					// but don't allow inlining of results of any other method with parameters.
					if (md.HasParameters) {
						if (!explicitlyAnnotated)
							break;

						bool hasByRefParameter = false;
						foreach (var param in md.Parameters) {
							if (param.ParameterType.IsByReference) {
								hasByRefParameter = true;
								break;
							}
						}

						if (hasByRefParameter)
							break;
					}

					reducer.Rewrite (i, targetResult);
					changed = true;
					break;

				case Code.Ldsfld:
					var ftarget = (FieldReference) instr.Operand;
					var field = ftarget.Resolve ();
					if (field == null)
						break;

					if (Context.Annotations.TryGetFieldUserValue (field, out object value)) {
						targetResult = CodeRewriterStep.CreateConstantResultInstruction (field.FieldType, value);
						if (targetResult == null)
							break;
						reducer.Rewrite (i, targetResult);
						changed = true;
					}
					break;

				case Code.Sizeof:
					//
					// sizeof (IntPtr) and sizeof (UIntPtr) are just aliases for IntPtr.Size and UIntPtr.Size
					// which are simple static properties commonly overwritten. Instead of forcing C# code style
					// we handle both via static Size property
					//
					MethodDefinition sizeOfImpl = null;

					var operand = (TypeReference) instr.Operand;
					if (operand.MetadataType == MetadataType.UIntPtr) {
						sizeOfImpl = UIntPtrSize ?? (UIntPtrSize = FindSizeMethod (operand.Resolve ()));
					}

					if (operand.MetadataType == MetadataType.IntPtr) {
						sizeOfImpl = IntPtrSize ?? (IntPtrSize = FindSizeMethod (operand.Resolve ()));
					}

					if (sizeOfImpl != null && constExprMethods.TryGetValue (sizeOfImpl, out targetResult)) {
						reducer.Rewrite (i, targetResult);
						changed = true;
					}

					break;
				}
			}

			return changed;
		}

		static MethodDefinition FindSizeMethod (TypeDefinition type)
		{
			if (type == null)
				return null;

			return type.Methods.First (l => !l.HasParameters && l.IsStatic && l.Name == "get_Size");
		}

		struct BodyReducer
		{
			readonly LinkContext context;
			Dictionary<Instruction, int> mapping;

			//
			// Sorted list of body instruction indexes which were
			// replaced pass-through nop
			// 
			List<int> conditionInstrsToRemove;

			public BodyReducer (MethodBody body, LinkContext context)
			{
				Body = body;
				this.context = context;

				FoldedInstructions = null;
				mapping = null;
				conditionInstrsToRemove = null;
				InstructionsReplaced = 0;
			}

			public MethodBody Body { get; }

			public int InstructionsReplaced { get; set; }

			public Collection<Instruction> FoldedInstructions { get; private set; }

			public void Rewrite (int index, Instruction newInstruction)
			{
				if (FoldedInstructions == null) {
					FoldedInstructions = new Collection<Instruction> (Body.Instructions);
					mapping = new Dictionary<Instruction, int> ();
				}

				// Tracks mapping for replaced instructions for easier
				// branch targets resolution later
				mapping[Body.Instructions[index]] = index;

				FoldedInstructions[index] = newInstruction;
			}

			void RewriteConditionToNop (int index)
			{
				if (conditionInstrsToRemove == null)
					conditionInstrsToRemove = new List<int> ();

				conditionInstrsToRemove.Add (index);
				RewriteToNop (index);
			}

			void RewriteToNop (int index)
			{
				Rewrite (index, Instruction.Create (OpCodes.Nop));
			}

			public bool RewriteBody ()
			{
				if (FoldedInstructions == null)
					return false;

				if (!RemoveConditions ())
					return false;

				BitArray reachableInstrs = GetReachableInstructionsMap (out var unreachableEH);
				if (reachableInstrs == null)
					return false;

				var bodySweeper = new BodySweeper (Body, reachableInstrs, unreachableEH, context);
				if (!bodySweeper.Initialize ()) {
					context.LogMessage ($"Unreachable IL reduction is not supported for method '{Body.Method.GetDisplayName ()}'");
					return false;
				}

				bodySweeper.Process (conditionInstrsToRemove, out var nopInstructions);
				InstructionsReplaced = bodySweeper.InstructionsReplaced;
				if (InstructionsReplaced == 0)
					return false;

				reachableInstrs = GetReachableInstructionsMap (out _);
				if (reachableInstrs != null)
					RemoveUnreachableInstructions (reachableInstrs);

				if (nopInstructions != null) {
					ILProcessor processor = Body.GetILProcessor ();

					foreach (var instr in nopInstructions)
						processor.Remove (instr);
				}

				return true;
			}

			void RemoveUnreachableInstructions (BitArray reachable)
			{
				ILProcessor processor = Body.GetILProcessor ();

				int removed = 0;
				for (int i = 0; i < reachable.Count; ++i) {
					if (reachable[i])
						continue;

					processor.RemoveAt (i - removed);
					++removed;
				}
			}

			bool RemoveConditions ()
			{
				bool changed = false;
				object left, right;

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

									if (IsConstantBranch (opcode, opint)) {
										Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
									} else {
										RewriteConditionToNop (i);
									}

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
							if (i > 3 && GetConstantValue (FoldedInstructions[i - 3], out operand) && operand is int opint2 && IsPairedStlocLdloc (FoldedInstructions[i - 2], FoldedInstructions[i - 1])) {
								if (IsJumpTargetRange (i - 2, i))
									continue;

								RewriteToNop (i - 3);
								RewriteToNop (i - 2);
								RewriteToNop (i - 1);

								if (IsConstantBranch (opcode, opint2)) {
									Rewrite (i, Instruction.Create (OpCodes.Br, (Instruction) instr.Operand));
								} else {
									RewriteConditionToNop (i);
								}

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

			BitArray GetReachableInstructionsMap (out List<ExceptionHandler> unreachableHandlers)
			{
				unreachableHandlers = null;
				var reachable = new BitArray (FoldedInstructions.Count);

				Stack<int> condBranches = null;
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
							if (condBranches == null)
								condBranches = new Stack<int> ();

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

						var instrs = Body.Instructions;
						foreach (var handler in Body.ExceptionHandlers) {
							int start = instrs.IndexOf (handler.TryStart);
							int end = instrs.IndexOf (handler.TryEnd) - 1;

							if (!HasAnyBitSet (reachable, start, end)) {
								if (unreachableHandlers == null)
									unreachableHandlers = new List<ExceptionHandler> ();

								unreachableHandlers.Add (handler);
								continue;
							}

							if (condBranches == null)
								condBranches = new Stack<int> ();

							condBranches.Push (GetInstructionIndex (handler.HandlerStart));
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
				if (mapping.TryGetValue (instruction, out int idx))
					return idx;

				idx = FoldedInstructions.IndexOf (instruction);
				Debug.Assert (idx >= 0);
				return idx;
			}

			bool GetOperandsConstantValues (int index, out object left, out object right)
			{
				left = default;
				right = default;

				if (index < 2)
					return false;

				return GetConstantValue (FoldedInstructions[index - 2], out left) &&
					GetConstantValue (FoldedInstructions[index - 1], out right);
			}

			static bool GetConstantValue (Instruction instruction, out object value)
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
				case Code.Ldnull:
					value = null;
					return true;
				default:
					value = null;
					return false;
				}
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
				case Code.Bge_Un:
				case Code.Bge_Un_S:
					return left >= right;
				case Code.Bgt:
				case Code.Bgt_S:
				case Code.Bgt_Un:
				case Code.Bgt_Un_S:
				case Code.Cgt:
					return left > right;
				case Code.Ble:
				case Code.Ble_S:
				case Code.Ble_Un:
				case Code.Ble_Un_S:
					return left <= right;
				case Code.Blt:
				case Code.Blt_S:
				case Code.Blt_Un:
				case Code.Blt_Un_S:
				case Code.Clt:
					return left < right;
				}

				throw new NotImplementedException (opCode.ToString ());
			}

			static bool IsConstantBranch (OpCode opCode, int operand)
			{
				switch (opCode.Code) {
				case Code.Brfalse:
				case Code.Brfalse_S:
					return operand == 0;
				case Code.Brtrue:
				case Code.Brtrue_S:
					return operand != 0;
				}

				throw new NotImplementedException (opCode.ToString ());
			}

			bool IsJumpTargetRange (int firstInstr, int lastInstr)
			{
				foreach (var instr in FoldedInstructions) {
					switch (instr.OpCode.FlowControl) {
					case FlowControl.Branch:
					case FlowControl.Cond_Branch:
						if (instr.Operand is Instruction target) {
							var index = GetInstructionIndex (target);
							if (index >= firstInstr && index <= lastInstr)
								return true;
						} else {
							foreach (var rtarget in (Instruction[]) instr.Operand) {
								var index = GetInstructionIndex (rtarget);
								if (index >= firstInstr && index <= lastInstr)
									return true;
							}
						}

						break;
					}
				}

				return false;
			}
		}

		struct BodySweeper
		{
			readonly MethodBody body;
			readonly BitArray reachable;
			readonly List<ExceptionHandler> unreachableExceptionHandlers;
			readonly LinkContext context;
			ILProcessor ilprocessor;

			public BodySweeper (MethodBody body, BitArray reachable, List<ExceptionHandler> unreachableEH, LinkContext context)
			{
				this.body = body;
				this.reachable = reachable;
				this.unreachableExceptionHandlers = unreachableEH;
				this.context = context;

				InstructionsReplaced = 0;
				ilprocessor = null;
			}

			public int InstructionsReplaced { get; set; }

			public bool Initialize ()
			{
				var instrs = body.Instructions;

				//
				// Reusing same reachable map and altering it at indexes which
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

				ilprocessor = body.GetILProcessor ();
				return true;
			}

			public void Process (List<int> conditionInstrsToRemove, out List<Instruction> sentinelNops)
			{
				List<VariableDefinition> removedVariablesReferences = null;

				//
				// Initial pass which replaces unreachable instructions with nops or
				// ret to keep the body verifiable
				//
				var instrs = body.Instructions;
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

					ilprocessor.Replace (i, newInstr);
					InstructionsReplaced++;

					VariableDefinition variable = GetVariableReference (instr);
					if (variable != null) {
						if (removedVariablesReferences == null)
							removedVariablesReferences = new List<VariableDefinition> ();
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

								if (sentinelNops == null)
									sentinelNops = new List<Instruction> ();
								sentinelNops.Add (nop);

								ilprocessor.Replace (index - 1, Instruction.Create (OpCodes.Pop));
								ilprocessor.Replace (index, nop);
							} else {
								var pop = Instruction.Create (OpCodes.Pop);
								ilprocessor.Replace (index, pop);
								ilprocessor.InsertAfter (pop, Instruction.Create (OpCodes.Pop));

								//
								// conditionInstrsToRemove is always sorted and instead of
								// increasing remaining indexes we introduce index delta value
								//
								bodyExpansion++;
							}
							break;
						case StackBehaviour.Popi:
							ilprocessor.Replace (index, Instruction.Create (OpCodes.Pop));
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
				foreach (var instr in body.Instructions) {
					VariableDefinition variable = GetVariableReference (instr);
					if (variable == null)
						continue;

					if (!variables.Remove (variable))
						continue;

					if (variables.Count == 0)
						return;
				}

				variables.Sort ((a, b) => b.Index.CompareTo (a.Index));
				var body_variables = body.Variables;

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
						var objectType = BCL.FindPredefinedType ("System", "Object", context);
						body_variables[index].VariableType = objectType ?? throw new NotSupportedException ("Missing predefined 'System.Object' type");
					}
				}
			}

			void CleanExceptionHandlers ()
			{
				if (unreachableExceptionHandlers == null)
					return;

				foreach (var eh in unreachableExceptionHandlers)
					body.ExceptionHandlers.Remove (eh);
			}

			VariableDefinition GetVariableReference (Instruction instruction)
			{
				switch (instruction.OpCode.Code) {
				case Code.Stloc_0:
				case Code.Ldloc_0:
					return body.Variables[0];
				case Code.Stloc_1:
				case Code.Ldloc_1:
					return body.Variables[1];
				case Code.Stloc_2:
				case Code.Ldloc_2:
					return body.Variables[2];
				case Code.Stloc_3:
				case Code.Ldloc_3:
					return body.Variables[3];
				}

				if (instruction.Operand is VariableReference vr)
					return vr.Resolve ();

				return null;
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
		}

		struct ConstantExpressionMethodAnalyzer
		{
			readonly MethodDefinition method;
			readonly Collection<Instruction> instructions;

			Stack<Instruction> stack_instr;
			Dictionary<int, Instruction> locals;

			public ConstantExpressionMethodAnalyzer (MethodDefinition method)
			{
				this.method = method;
				instructions = method.Body.Instructions;
				stack_instr = null;
				locals = null;
				Result = null;
			}

			public ConstantExpressionMethodAnalyzer (MethodDefinition method, Collection<Instruction> instructions)
			{
				this.method = method;
				this.instructions = instructions;
				stack_instr = null;
				locals = null;
				Result = null;
			}

			public Instruction Result { get; private set; }

			public bool Analyze ()
			{
				var body = method.Body;
				if (body.HasExceptionHandlers)
					return false;

				VariableReference vr;
				Instruction jmpTarget = null;
				Instruction linstr;

				foreach (var instr in instructions) {
					if (jmpTarget != null) {
						if (instr != jmpTarget)
							continue;

						jmpTarget = null;
					}

					switch (instr.OpCode.Code) {
					case Code.Nop:
						continue;
					case Code.Pop:
						stack_instr.Pop ();
						continue;

					case Code.Br_S:
					case Code.Br:
						jmpTarget = (Instruction) instr.Operand;
						continue;

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

					// TODO: handle simple conversions
					//case Code.Conv_I:

					case Code.Ret:
						if (ConvertStackToResult ())
							return true;

						break;
					}

					return false;
				}

				return false;
			}

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

			Instruction GetLocalsValue (int index, MethodBody body)
			{
				var instr = locals?[index];
				if (instr != null)
					return instr;

				if (!body.InitLocals)
					return null;

				// local variables don't need to be explicitly initialized
				return CodeRewriterStep.CreateConstantResultInstruction (body.Variables[index].VariableType);
			}

			void PushOnStack (Instruction instruction)
			{
				if (stack_instr == null)
					stack_instr = new Stack<Instruction> ();

				stack_instr.Push (instruction);
			}

			void StoreToLocals (int index)
			{
				if (locals == null)
					locals = new Dictionary<int, Instruction> ();

				locals[index] = stack_instr.Pop ();
			}
		}
	}
}
