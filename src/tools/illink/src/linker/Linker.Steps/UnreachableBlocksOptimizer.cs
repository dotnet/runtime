using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	//
	// Evaluates simple properties or methods for constant expressions and
	// then uses this information to remove unreachable conditional blocks. It does
	// not do any inlining-like code changes.
	//
	public class UnreachableBlocksOptimizer
	{
		readonly LinkContext _context;
		MethodDefinition IntPtrSize, UIntPtrSize;

		readonly struct ProcessingNode
		{
			public ProcessingNode (MethodDefinition method, int lastAttemptStackVersion)
			{
				Method = method;
				LastAttemptStackVersion = lastAttemptStackVersion;
			}

			public ProcessingNode (in ProcessingNode other, int newLastAttempStackVersion)
			{
				Method = other.Method;
				LastAttemptStackVersion = newLastAttempStackVersion;
			}

			public readonly MethodDefinition Method;
			public readonly int LastAttemptStackVersion;
		}

		// Stack of method nodes which are currently being processed.
		// Implemented as linked list to allow easy referal to nodes and efficient moving of nodes within the list.
		// The top of the stack is the first item in the list.
		readonly LinkedList<ProcessingNode> _processingStack;

		// Each time an item is added or removed from the processing stack this value is incremented.
		// Moving items in the stack doesn't increment.
		// This is used to track loops - if there are two methods which have dependencies on each other
		// the processing needs to detect that and mark at least one of them as nonconst (regardless of the method's body)
		// to break the loop.
		// This is done by storing the version of the stack on each method node when that method is processed,
		// if we get around to process the method again and the version of the stack didn't change, then there's a loop
		// (nothing changed in the stack - order is unimportant, as such no new information has been added and so
		// we can't resolve the situation with just the info at hand).
		int _processingStackVersion;

		// Just a fast lookup from method to the node on the stack. This is needed to be able to quickly
		// access the node and move it to the top of the stack.
		readonly Dictionary<MethodDefinition, LinkedListNode<ProcessingNode>> _processingMethods;

		// Stores results of method processing. This state is kept forever to avoid reprocessing of methods.
		// If method is not in the dictionary it has not yet been processed.
		// The value in this dictionary can be
		//   - ProcessedUnchangedSentinel - method has been processed and nothing was changed on it - its value is unknown
		//   - NonConstSentinel - method has been processed and the return value is not a const
		//   - Instruction instance - method has been processed and it has a constant return value (the value of the instruction)
		// Note: ProcessedUnchangedSentinel is used as an optimization. running constant value analysis on a method is relatively expensive
		// and so we delay it and only do it for methods where the value is asked for (or in case of changed methods upfront due to implementation detailds)
		readonly Dictionary<MethodDefinition, Instruction> _processedMethods;
		static readonly Instruction ProcessedUnchangedSentinel = Instruction.Create (OpCodes.Ldstr, "ProcessedUnchangedSentinel");
		static readonly Instruction NonConstSentinel = Instruction.Create (OpCodes.Ldstr, "NonConstSentinel");

		public UnreachableBlocksOptimizer (LinkContext context)
		{
			_context = context;

			_processingStack = new LinkedList<ProcessingNode> ();
			_processingMethods = new Dictionary<MethodDefinition, LinkedListNode<ProcessingNode>> ();
			_processedMethods = new Dictionary<MethodDefinition, Instruction> ();
		}

		/// <summary>
		/// Processes the specified and method and perform all branch removal optimizations on it.
		/// When this returns it's guaranteed that the method has been optimized (if possible).
		/// It may optimize other methods as well - those are remembered for future reuse.
		/// </summary>
		/// <param name="method">The method to process</param>
		public void ProcessMethod (MethodDefinition method)
		{
			if (!IsMethodSupported (method))
				return;

			if (_context.Annotations.GetAction (method.Module.Assembly) != AssemblyAction.Link)
				return;

			Debug.Assert (_processingStack.Count == 0 && _processingMethods.Count == 0);
			_processingStackVersion = 0;

			if (!_processedMethods.ContainsKey (method)) {
				AddMethodForProcessing (method);

				ProcessStack ();
			}

			Debug.Assert (_processedMethods.ContainsKey (method));
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

		void AddMethodForProcessing (MethodDefinition method)
		{
			Debug.Assert (!_processedMethods.ContainsKey (method));

			var processingNode = new ProcessingNode (method, -1);

			var stackNode = _processingStack.AddFirst (processingNode);
			_processingMethods.Add (method, stackNode);
			_processingStackVersion++;
		}

		void StoreMethodAsProcessedAndRemoveFromQueue (LinkedListNode<ProcessingNode> stackNode, Instruction methodValue)
		{
			Debug.Assert (stackNode.List == _processingStack);
			Debug.Assert (methodValue != null);

			var method = stackNode.Value.Method;
			_processingMethods.Remove (method);
			_processingStack.Remove (stackNode);
			_processingStackVersion++;

			_processedMethods[method] = methodValue;
		}

		void ProcessStack ()
		{
			while (_processingStack.Count > 0) {
				var stackNode = _processingStack.First;
				var method = stackNode.Value.Method;

				bool treatUnprocessedDependenciesAsNonConst = false;
				if (stackNode.Value.LastAttemptStackVersion == _processingStackVersion) {
					// Loop was detected - the stack hasn't changed since the last time we tried to process this method
					// as such there's no way to resolve the situation (running the code below would produce the exact same result).

					// Observation:
					//   All nodes on the stack which have `LastAttemptStackVersion` equal to `processingStackVersion` are part of the loop
					//   meaning removing any of them should break the loop and allow to make progress.
					//   There might be other methods in between these which don't have the current version but are dependencies of some of the method
					//   in the loop.
					//   If we don't process these, then we might miss constants and branches to remove. See the doc
					//   `constant-propagation-and-branch-removal.md` in this repo for more details and a sample.

					// To fix this go over the stack and find the "oldest" node with the current version - the "oldest" node which
					// is part of the loop:
					LinkedListNode<ProcessingNode> lastNodeWithCurrentVersion = null;
					var candidateNodeToMoveToTop = _processingStack.Last;
					bool foundNodesWithNonCurrentVersion = false;
					while (candidateNodeToMoveToTop != stackNode) {
						var previousNode = candidateNodeToMoveToTop.Previous;

						if (candidateNodeToMoveToTop.Value.LastAttemptStackVersion == _processingStackVersion) {
							lastNodeWithCurrentVersion = candidateNodeToMoveToTop;
						} else if (lastNodeWithCurrentVersion != null) {
							// We've found the "oldest" node with current version and the current node is not of that version
							// so it's older version. Move this node to the top of the stack.
							_processingStack.Remove (candidateNodeToMoveToTop);
							_processingStack.AddFirst (candidateNodeToMoveToTop);
							foundNodesWithNonCurrentVersion = true;
						}

						candidateNodeToMoveToTop = previousNode;
					}

					// There should be at least 2 nodes with the latest version to form a loop
					Debug.Assert (lastNodeWithCurrentVersion != stackNode);

					// If any node was found which was not of current version (and moved to the top of the stack), move on to processing
					// the stack - this will give a chance for these methods to be processed. It doesn't break the loop and we should come back here
					// again due to the same loop as before, but now with more nodes processed (hopefully all of the dependencies of the nodes in the loop).
					// In the worst case all of those nodes will become part of the loop - in which case we will move on to break the loop anyway.
					if (foundNodesWithNonCurrentVersion) {
						continue;
					}

					// No such node was found -> we only have nodes in the loop now, so we have to break the loop.
					// We do this by processing it with special flag which will make it ignore any unprocessed dependencies
					// treating them as non-const. These should only be nodes in the loop.
					treatUnprocessedDependenciesAsNonConst = true;
				}

				stackNode.Value = new ProcessingNode (stackNode.Value, _processingStackVersion);

				if (!IsMethodSupported (method)) {
					StoreMethodAsProcessedAndRemoveFromQueue (stackNode, ProcessedUnchangedSentinel);
					continue;
				}

				var reducer = new BodyReducer (method.Body, _context);

				//
				// Temporary inlines any calls which return contant expression.
				// If it needs to know the result of analysis of other methods and those has not been processed yet
				// it will still scan the entire body, but we will return the full processing one more time.
				//
				if (!TryInlineBodyDependencies (ref reducer, treatUnprocessedDependenciesAsNonConst, out bool changed)) {
					// Method has unprocessed dependencies - so back off and try again later
					// Leave it in the stack on its current position (it should not be on the first position anymore)
					Debug.Assert (_processingStack.First != stackNode);
					continue;
				}

				if (!changed) {
					// All dependencies are processed and there were no const values found. There's nothing to optimize.
					// Mark the method as processed - without computing the const value of it (we don't know if it's going to be needed)
					StoreMethodAsProcessedAndRemoveFromQueue (stackNode, ProcessedUnchangedSentinel);
					continue;
				}

				// The method has been modified due to constant propagation - we will optimize it.

				//
				// This is the main step which evaluates if inlined calls can
				// produce folded branches. When it finds them the unreachable
				// branch is replaced with nops.
				//
				if (reducer.RewriteBody ())
					_context.LogMessage ($"Reduced '{reducer.InstructionsReplaced}' instructions in conditional branches for [{method.DeclaringType.Module.Assembly.Name}] method {method.GetDisplayName ()}");

				// Even if the rewriter doesn't find any branches to fold the inlining above may have changed the method enough
				// such that we can now deduce its return value.

				if (method.ReturnType.MetadataType == MetadataType.Void) {
					// Method is fully processed and can't be const (since it doesn't return value) - so mark it as processed without const value
					StoreMethodAsProcessedAndRemoveFromQueue (stackNode, NonConstSentinel);
					continue;
				}

				//
				// Run the analyzer in case body change rewrote it to constant expression
				// Note that we have to run it always (even if we may not need the result ever) since it depends on the temporary inlining above
				// Otherwise we would have to remember the inlined code along with the method.
				//
				StoreMethodAsProcessedAndRemoveFromQueue (
					stackNode,
					AnalyzeMethodForConstantResult (method, reducer.FoldedInstructions) ?? NonConstSentinel);
			}

			Debug.Assert (_processingMethods.Count == 0);
		}

		Instruction AnalyzeMethodForConstantResult (MethodDefinition method, Collection<Instruction> instructions)
		{
			if (!method.HasBody)
				return null;

			if (method.ReturnType.MetadataType == MetadataType.Void)
				return null;

			switch (_context.Annotations.GetAction (method)) {
			case MethodAction.ConvertToThrow:
				return null;
			case MethodAction.ConvertToStub:
				return CodeRewriterStep.CreateConstantResultInstruction (_context, method);
			}

			if (method.IsIntrinsic () || method.NoInlining)
				return null;

			if (!_context.IsOptimizationEnabled (CodeOptimizations.IPConstantPropagation, method))
				return null;

			var analyzer = new ConstantExpressionMethodAnalyzer (_context, method, instructions ?? method.Body.Instructions);
			if (analyzer.Analyze ()) {
				return analyzer.Result;
			}

			return null;
		}

		/// <summary>
		/// Determines if a method has constant return value. If the method has not yet been processed it makes sure
		/// it is on the stack for processing and returns without a result.
		/// </summary>
		/// <param name="method">The method to determine result for</param>
		/// <param name="constantResultInstruction">If successfull and the method returns a constant value this will be set to the
		/// instruction with the constant value. If successfulll and the method doesn't have a constant value this is set to null.</param>
		/// <returns>
		/// true - if the method was analyzed and result is known
		///   constantResultInstruction is set to an instance if the method returns a constant, otherwise it's set to null
		/// false - if the method has not yet been analyzed and the caller should retry later
		/// </returns>
		bool TryGetConstantResultForMethod (MethodDefinition method, out Instruction constantResultInstruction)
		{
			if (!_processedMethods.TryGetValue (method, out Instruction methodValue)) {
				if (_processingMethods.TryGetValue (method, out var stackNode)) {
					// Method is already in the stack - not yet processed
					// Move it to the top of the stack
					_processingStack.Remove (stackNode);
					_processingStack.AddFirst (stackNode);

					// Note that stack version is not changing - we're just postponing work, not resolving anything.
					// There's no result available for this method, so return false.
					constantResultInstruction = null;
					return false;
				}

				// Method is not yet in the stack - add it there
				AddMethodForProcessing (method);
				constantResultInstruction = null;
				return false;
			}

			if (methodValue == ProcessedUnchangedSentinel) {
				// Method has been processed and no changes has been made to it.
				// Also its value has not been needed yet. Now we need to know if it's constant, so run the analyzer on it
				var result = AnalyzeMethodForConstantResult (method, instructions: null);
				Debug.Assert (result is Instruction || result == null);
				_processedMethods[method] = result ?? NonConstSentinel;
				constantResultInstruction = result;
			} else if (methodValue == NonConstSentinel) {
				// Method was processed and found to not have a constant value
				constantResultInstruction = null;
			} else {
				// Method was already processed and found to have a constant value
				constantResultInstruction = methodValue;
			}

			return true;
		}

		bool TryInlineBodyDependencies (ref BodyReducer reducer, bool treatUnprocessedDependenciesAsNonConst, out bool changed)
		{
			changed = false;
			bool hasUnprocessedDependencies = false;
			var instructions = reducer.Body.Instructions;
			Instruction targetResult;

			for (int i = 0; i < instructions.Count; ++i) {
				var instr = instructions[i];
				switch (instr.OpCode.Code) {

				case Code.Call:
				case Code.Callvirt:
					var md = _context.TryResolveMethodDefinition ((MethodReference) instr.Operand);
					if (md == null)
						break;

					if (md.CallingConvention == MethodCallingConvention.VarArg)
						break;

					bool explicitlyAnnotated = _context.Annotations.GetAction (md) == MethodAction.ConvertToStub;

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

					if (md == reducer.Body.Method) {
						// Special case for direct recursion - simply assume non-const value
						// since we can't tell.
						break;
					}

					if (!TryGetConstantResultForMethod (md, out targetResult)) {
						if (!treatUnprocessedDependenciesAsNonConst)
							hasUnprocessedDependencies = true;
						break;
					} else if (targetResult == null || hasUnprocessedDependencies) {
						// Even is const is detected, there's no point in rewriting anything
						// if we've found unprocessed dependency since the results of this scan will
						// be thrown away (we back off and wait for the unprocessed dependency to be processed first).
						break;
					}

					reducer.Rewrite (i, targetResult);
					changed = true;

					break;

				case Code.Ldsfld:
					var ftarget = (FieldReference) instr.Operand;
					var field = _context.TryResolveFieldDefinition (ftarget);
					if (field == null)
						break;

					if (_context.Annotations.TryGetFieldUserValue (field, out object value)) {
						targetResult = CodeRewriterStep.CreateConstantResultInstruction (_context, field.FieldType, value);
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
						sizeOfImpl = (UIntPtrSize ??= FindSizeMethod (_context.TryResolveTypeDefinition (operand)));
					} else if (operand.MetadataType == MetadataType.IntPtr) {
						sizeOfImpl = (IntPtrSize ??= FindSizeMethod (_context.TryResolveTypeDefinition (operand)));
					}

					if (sizeOfImpl != null) {
						if (!TryGetConstantResultForMethod (sizeOfImpl, out targetResult)) {
							if (!treatUnprocessedDependenciesAsNonConst)
								hasUnprocessedDependencies = true;
							break;
						} else if (targetResult == null || hasUnprocessedDependencies) {
							break;
						}

						reducer.Rewrite (i, targetResult);
						changed = true;
					}

					break;
				}
			}

			return !hasUnprocessedDependencies;
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
			readonly LinkContext context;

			Stack<Instruction> stack_instr;
			Dictionary<int, Instruction> locals;

			public ConstantExpressionMethodAnalyzer (LinkContext context, MethodDefinition method)
			{
				this.context = context;
				this.method = method;
				instructions = method.Body.Instructions;
				stack_instr = null;
				locals = null;
				Result = null;
			}

			public ConstantExpressionMethodAnalyzer (LinkContext context, MethodDefinition method, Collection<Instruction> instructions)
				: this (context, method)
			{
				this.instructions = instructions;
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
				if (locals != null && locals.TryGetValue (index, out Instruction instruction))
					return instruction;

				if (!body.InitLocals)
					return null;

				// local variables don't need to be explicitly initialized
				return CodeRewriterStep.CreateConstantResultInstruction (context, body.Variables[index].VariableType);
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
