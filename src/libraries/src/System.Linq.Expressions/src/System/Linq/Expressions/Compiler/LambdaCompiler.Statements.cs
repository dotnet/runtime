// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions.Compiler
{
    internal partial class LambdaCompiler
    {
        private void EmitBlockExpression(Expression expr, CompilationFlags flags)
        {
            // emit body
            Emit((BlockExpression)expr, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsDefaultType));
        }

        private void Emit(BlockExpression node, CompilationFlags flags)
        {
            int count = node.ExpressionCount;

            if (count == 0)
            {
                return;
            }

            EnterScope(node);

            CompilationFlags emitAs = flags & CompilationFlags.EmitAsTypeMask;

            CompilationFlags tailCall = flags & CompilationFlags.EmitAsTailCallMask;
            for (int index = 0; index < count - 1; index++)
            {
                Expression e = node.GetExpression(index);
                Expression next = node.GetExpression(index + 1);

                CompilationFlags tailCallFlag;
                if (tailCall != CompilationFlags.EmitAsNoTail)
                {
                    var g = next as GotoExpression;
                    if (g != null && (g.Value == null || !Significant(g.Value)) && ReferenceLabel(g.Target).CanReturn)
                    {
                        // Since tail call flags are not passed into EmitTryExpression, CanReturn means the goto will be emitted
                        // as Ret. Therefore we can emit the current expression with tail call.
                        tailCallFlag = CompilationFlags.EmitAsTail;
                    }
                    else
                    {
                        // In the middle of the block.
                        // We may do better here by marking it as Tail if the following expressions are not going to emit any IL.
                        tailCallFlag = CompilationFlags.EmitAsMiddle;
                    }
                }
                else
                {
                    tailCallFlag = CompilationFlags.EmitAsNoTail;
                }

                flags = UpdateEmitAsTailCallFlag(flags, tailCallFlag);
                EmitExpressionAsVoid(e, flags);
            }

            // if the type of Block it means this is not a Comma
            // so we will force the last expression to emit as void.
            // We don't need EmitAsType flag anymore, should only pass
            // the EmitTailCall field in flags to emitting the last expression.
            if (emitAs == CompilationFlags.EmitAsVoidType || node.Type == typeof(void))
            {
                EmitExpressionAsVoid(node.GetExpression(count - 1), tailCall);
            }
            else
            {
                EmitExpressionAsType(node.GetExpression(count - 1), node.Type, tailCall);
            }

            ExitScope(node);
        }

        private void EnterScope(object node)
        {
            if (HasVariables(node) &&
                (_scope.MergedScopes == null || !_scope.MergedScopes.Contains(node)))
            {
                CompilerScope scope;
                if (!_tree.Scopes.TryGetValue(node, out scope))
                {
                    //
                    // Very often, we want to compile nodes as reductions
                    // rather than as IL, but usually they need to allocate
                    // some IL locals. To support this, we allow emitting a
                    // BlockExpression that was not bound by VariableBinder.
                    // This works as long as the variables are only used
                    // locally -- i.e. not closed over.
                    //
                    // User-created blocks will never hit this case; only our
                    // internally reduced nodes will.
                    //
                    scope = new CompilerScope(node, false) { NeedsClosure = _scope.NeedsClosure };
                }

                _scope = scope.Enter(this, _scope);
                Debug.Assert(_scope.Node == node);
            }
        }

        private static bool HasVariables(object node)
        {
            var block = node as BlockExpression;
            if (block != null)
            {
                return block.Variables.Count > 0;
            }
            return ((CatchBlock)node).Variable != null;
        }

        private void ExitScope(object node)
        {
            if (_scope.Node == node)
            {
                _scope = _scope.Exit();
            }
        }

        private void EmitDefaultExpression(Expression expr)
        {
            var node = (DefaultExpression)expr;
            if (node.Type != typeof(void))
            {
                // emit default(T)
                _ilg.EmitDefault(node.Type);
            }
        }

        private void EmitLoopExpression(Expression expr)
        {
            LoopExpression node = (LoopExpression)expr;

            PushLabelBlock(LabelScopeKind.Statement);
            LabelInfo breakTarget = DefineLabel(node.BreakLabel);
            LabelInfo continueTarget = DefineLabel(node.ContinueLabel);

            continueTarget.MarkWithEmptyStack();

            EmitExpressionAsVoid(node.Body);

            _ilg.Emit(OpCodes.Br, continueTarget.Label);

            PopLabelBlock(LabelScopeKind.Statement);

            breakTarget.MarkWithEmptyStack();
        }

        #region SwitchExpression

        private void EmitSwitchExpression(Expression expr, CompilationFlags flags)
        {
            SwitchExpression node = (SwitchExpression)expr;

            if (node.Cases.Count == 0)
            {
                // Emit the switch value in case it has side-effects, but as void
                // since the value is ignored.
                EmitExpressionAsVoid(node.SwitchValue);

                // Now if there is a default body, it happens unconditionally.
                if (node.DefaultBody != null)
                {
                    EmitExpressionAsType(node.DefaultBody, node.Type, flags);
                }
                else
                {
                    // If there are no cases and no default then the type must be void.
                    // Assert that earlier validation caught any exceptions to that.
                    Debug.Assert(node.Type == typeof(void));
                }

                return;
            }

            // Try to emit it as an IL switch. Works for integer types.
            if (TryEmitSwitchInstruction(node, flags))
            {
                return;
            }

            // Try to emit as a hashtable lookup. Works for strings.
            if (TryEmitHashtableSwitch(node, flags))
            {
                return;
            }

            //
            // Fall back to a series of tests. We need to IL gen instead of
            // transform the tree to avoid stack overflow on a big switch.
            //

            ParameterExpression switchValue = Expression.Parameter(node.SwitchValue.Type, "switchValue");
            ParameterExpression testValue = Expression.Parameter(GetTestValueType(node), "testValue");
            _scope.AddLocal(this, switchValue);
            _scope.AddLocal(this, testValue);

            EmitExpression(node.SwitchValue);
            _scope.EmitSet(switchValue);

            // Emit tests
            var labels = new Label[node.Cases.Count];
            var isGoto = new bool[node.Cases.Count];
            for (int i = 0, n = node.Cases.Count; i < n; i++)
            {
                DefineSwitchCaseLabel(node.Cases[i], out labels[i], out isGoto[i]);
                foreach (Expression test in node.Cases[i].TestValues)
                {
                    // Pull the test out into a temp so it runs on the same
                    // stack as the switch. This simplifies spilling.
                    EmitExpression(test);
                    _scope.EmitSet(testValue);
                    Debug.Assert(TypeUtils.AreReferenceAssignable(testValue.Type, test.Type));
                    EmitExpressionAndBranch(true, Expression.Equal(switchValue, testValue, false, node.Comparison), labels[i]);
                }
            }

            // Define labels
            Label end = _ilg.DefineLabel();
            Label @default = (node.DefaultBody == null) ? end : _ilg.DefineLabel();

            // Emit the case and default bodies
            EmitSwitchCases(node, labels, isGoto, @default, end, flags);
        }

        /// <summary>
        /// Gets the common test value type of the SwitchExpression.
        /// </summary>
        private static Type GetTestValueType(SwitchExpression node)
        {
            if (node.Comparison == null)
            {
                // If we have no comparison, all right side types must be the
                // same.
                return node.Cases[0].TestValues[0].Type;
            }

            // Otherwise, get the type from the method.
            Type result = node.Comparison.GetParametersCached()[1].ParameterType.GetNonRefType();
            if (node.IsLifted)
            {
                result = result.GetNullableType();
            }
            return result;
        }

        private sealed class SwitchLabel
        {
            internal readonly decimal Key;
            internal readonly Label Label;

            // Boxed version of Key, preserving the original type.
            internal readonly object Constant;

            internal SwitchLabel(decimal key, object @constant, Label label)
            {
                Key = key;
                Constant = @constant;
                Label = label;
            }
        }

        private sealed class SwitchInfo
        {
            internal readonly SwitchExpression Node;
            internal readonly LocalBuilder Value;
            internal readonly Label Default;
            internal readonly Type Type;
            internal readonly bool IsUnsigned;
            internal readonly bool Is64BitSwitch;

            internal SwitchInfo(SwitchExpression node, LocalBuilder value, Label @default)
            {
                Node = node;
                Value = value;
                Default = @default;
                Type = Node.SwitchValue.Type;
                IsUnsigned = Type.IsUnsigned();
                TypeCode code = Type.GetTypeCode();
                Is64BitSwitch = code == TypeCode.UInt64 || code == TypeCode.Int64;
            }
        }

        private static bool FitsInBucket(List<SwitchLabel> buckets, decimal key, int count)
        {
            Debug.Assert(key > buckets[buckets.Count - 1].Key);
            decimal jumpTableSlots = key - buckets[0].Key + 1;
            if (jumpTableSlots > int.MaxValue)
            {
                return false;
            }
            // density must be > 50%
            return (buckets.Count + count) * 2 > jumpTableSlots;
        }

        private static void MergeBuckets(List<List<SwitchLabel>> buckets)
        {
            while (buckets.Count > 1)
            {
                List<SwitchLabel> first = buckets[buckets.Count - 2];
                List<SwitchLabel> second = buckets[buckets.Count - 1];

                if (!FitsInBucket(first, second[second.Count - 1].Key, second.Count))
                {
                    return;
                }

                // Merge them
                first.AddRange(second);
                buckets.RemoveAt(buckets.Count - 1);
            }
        }

        // Add key to a new or existing bucket
        private static void AddToBuckets(List<List<SwitchLabel>> buckets, SwitchLabel key)
        {
            if (buckets.Count > 0)
            {
                List<SwitchLabel> last = buckets[buckets.Count - 1];
                if (FitsInBucket(last, key.Key, 1))
                {
                    last.Add(key);
                    // we might be able to merge now
                    MergeBuckets(buckets);
                    return;
                }
            }
            // else create a new bucket
            buckets.Add(new List<SwitchLabel> { key });
        }

        // Determines if the type is an integer we can switch on.
        private static bool CanOptimizeSwitchType(Type valueType)
        {
            // enums & char are allowed
            switch (valueType.GetTypeCode())
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        // Tries to emit switch as a jmp table
        private bool TryEmitSwitchInstruction(SwitchExpression node, CompilationFlags flags)
        {
            // If we have a comparison, bail
            if (node.Comparison != null)
            {
                return false;
            }

            // Make sure the switch value type and the right side type
            // are types we can optimize
            Type type = node.SwitchValue.Type;
            if (!CanOptimizeSwitchType(type) ||
                !TypeUtils.AreEquivalent(type, node.Cases[0].TestValues[0].Type))
            {
                return false;
            }

            // Make sure all test values are constant, or we can't emit the
            // jump table.
            if (!node.Cases.All(c => c.TestValues.All(t => t is ConstantExpression)))
            {
                return false;
            }

            //
            // We can emit the optimized switch, let's do it.
            //

            // Build target labels, collect keys.
            var labels = new Label[node.Cases.Count];
            var isGoto = new bool[node.Cases.Count];

            var uniqueKeys = new HashSet<decimal>();
            var keys = new List<SwitchLabel>();
            for (int i = 0; i < node.Cases.Count; i++)
            {
                DefineSwitchCaseLabel(node.Cases[i], out labels[i], out isGoto[i]);

                foreach (ConstantExpression test in node.Cases[i].TestValues)
                {
                    // Guaranteed to work thanks to CanOptimizeSwitchType.
                    //
                    // Use decimal because it can hold Int64 or UInt64 without
                    // precision loss or signed/unsigned conversions.
                    decimal key = ConvertSwitchValue(test.Value);

                    // Only add each key once. If it appears twice, it's
                    // allowed, but can't be reached.
                    if (uniqueKeys.Add(key))
                    {
                        keys.Add(new SwitchLabel(key, test.Value, labels[i]));
                    }
                }
            }

            // Sort the keys, and group them into buckets.
            keys.Sort((x, y) => Math.Sign(x.Key - y.Key));
            var buckets = new List<List<SwitchLabel>>();
            foreach (SwitchLabel key in keys)
            {
                AddToBuckets(buckets, key);
            }

            // Emit the switchValue
            LocalBuilder value = GetLocal(node.SwitchValue.Type);
            EmitExpression(node.SwitchValue);
            _ilg.Emit(OpCodes.Stloc, value);

            // Create end label, and default label if needed
            Label end = _ilg.DefineLabel();
            Label @default = (node.DefaultBody == null) ? end : _ilg.DefineLabel();

            // Emit the switch
            var info = new SwitchInfo(node, value, @default);
            EmitSwitchBuckets(info, buckets, 0, buckets.Count - 1);

            // Emit the case bodies and default
            EmitSwitchCases(node, labels, isGoto, @default, end, flags);

            FreeLocal(value);
            return true;
        }

        private static decimal ConvertSwitchValue(object value)
        {
            if (value is char)
            {
                return (int)(char)value;
            }
            return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Creates the label for this case.
        /// Optimization: if the body is just a goto, and we can branch
        /// to it, put the goto target directly in the jump table.
        /// </summary>
        private void DefineSwitchCaseLabel(SwitchCase @case, out Label label, out bool isGoto)
        {
            var jump = @case.Body as GotoExpression;
            // if it's a goto with no value
            if (jump != null && jump.Value == null)
            {
                // Reference the label from the switch. This will cause us to
                // analyze the jump target and determine if it is safe.
                LabelInfo jumpInfo = ReferenceLabel(jump.Target);

                // If we have are allowed to emit the "branch" opcode, then we
                // can jump directly there from the switch's jump table.
                // (Otherwise, we need to emit the goto later as a "leave".)
                if (jumpInfo.CanBranch)
                {
                    label = jumpInfo.Label;
                    isGoto = true;
                    return;
                }
            }
            // otherwise, just define a new label
            label = _ilg.DefineLabel();
            isGoto = false;
        }

        private void EmitSwitchCases(SwitchExpression node, Label[] labels, bool[] isGoto, Label @default, Label end, CompilationFlags flags)
        {
            // Jump to default (to handle the fallthrough case)
            _ilg.Emit(OpCodes.Br, @default);

            // Emit the cases
            for (int i = 0, n = node.Cases.Count; i < n; i++)
            {
                // If the body is a goto, we already emitted an optimized
                // branch directly to it. No need to emit anything else.
                if (isGoto[i])
                {
                    continue;
                }

                _ilg.MarkLabel(labels[i]);
                EmitExpressionAsType(node.Cases[i].Body, node.Type, flags);

                // Last case doesn't need branch
                if (node.DefaultBody != null || i < n - 1)
                {
                    if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail)
                    {
                        //The switch case is at the tail of the lambda so
                        //it is safe to emit a Ret.
                        _ilg.Emit(OpCodes.Ret);
                    }
                    else
                    {
                        _ilg.Emit(OpCodes.Br, end);
                    }
                }
            }

            // Default value
            if (node.DefaultBody != null)
            {
                _ilg.MarkLabel(@default);
                EmitExpressionAsType(node.DefaultBody, node.Type, flags);
            }

            _ilg.MarkLabel(end);
        }

        private void EmitSwitchBuckets(SwitchInfo info, List<List<SwitchLabel>> buckets, int first, int last)
        {
            if (first == last)
            {
                EmitSwitchBucket(info, buckets[first]);
                return;
            }

            // Split the buckets into two groups, and use an if test to find
            // the right bucket. This ensures we'll only need O(lg(B)) tests
            // where B is the number of buckets
            int mid = (int)(((long)first + last + 1) / 2);

            if (first == mid - 1)
            {
                EmitSwitchBucket(info, buckets[first]);
            }
            else
            {
                // If the first half contains more than one, we need to emit an
                // explicit guard
                Label secondHalf = _ilg.DefineLabel();
                _ilg.Emit(OpCodes.Ldloc, info.Value);
                _ilg.EmitConstant(buckets[mid - 1].Last().Constant);
                _ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, secondHalf);
                EmitSwitchBuckets(info, buckets, first, mid - 1);
                _ilg.MarkLabel(secondHalf);
            }

            EmitSwitchBuckets(info, buckets, mid, last);
        }

        private void EmitSwitchBucket(SwitchInfo info, List<SwitchLabel> bucket)
        {
            // No need for switch if we only have one value
            if (bucket.Count == 1)
            {
                _ilg.Emit(OpCodes.Ldloc, info.Value);
                _ilg.EmitConstant(bucket[0].Constant);
                _ilg.Emit(OpCodes.Beq, bucket[0].Label);
                return;
            }

            //
            // If we're switching off of Int64/UInt64, we need more guards here
            // because we'll have to narrow the switch value to an Int32, and
            // we can't do that unless the value is in the right range.
            //
            Label? after = null;
            if (info.Is64BitSwitch)
            {
                after = _ilg.DefineLabel();
                _ilg.Emit(OpCodes.Ldloc, info.Value);
                _ilg.EmitConstant(bucket.Last().Constant);
                _ilg.Emit(info.IsUnsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, after.Value);
                _ilg.Emit(OpCodes.Ldloc, info.Value);
                _ilg.EmitConstant(bucket[0].Constant);
                _ilg.Emit(info.IsUnsigned ? OpCodes.Blt_Un : OpCodes.Blt, after.Value);
            }

            _ilg.Emit(OpCodes.Ldloc, info.Value);

            // Normalize key
            decimal key = bucket[0].Key;
            if (key != 0)
            {
                _ilg.EmitConstant(bucket[0].Constant);
                _ilg.Emit(OpCodes.Sub);
            }

            if (info.Is64BitSwitch)
            {
                _ilg.Emit(OpCodes.Conv_I4);
            }

            // Collect labels
            int len = (int)(bucket[bucket.Count - 1].Key - bucket[0].Key + 1);
            Label[] jmpLabels = new Label[len];

            // Initialize all labels to the default
            int slot = 0;
            foreach (SwitchLabel label in bucket)
            {
                while (key++ != label.Key)
                {
                    jmpLabels[slot++] = info.Default;
                }
                jmpLabels[slot++] = label.Label;
            }

            // check we used all keys and filled all slots
            Debug.Assert(key == bucket[bucket.Count - 1].Key + 1);
            Debug.Assert(slot == jmpLabels.Length);

            // Finally, emit the switch instruction
            _ilg.Emit(OpCodes.Switch, jmpLabels);

            if (info.Is64BitSwitch)
            {
                _ilg.MarkLabel(after.Value);
            }
        }

        private bool TryEmitHashtableSwitch(SwitchExpression node, CompilationFlags flags)
        {
            // If we have a comparison other than string equality, bail
            MethodInfo equality = String_op_Equality_String_String;
            if (equality != null && !equality.IsStatic)
            {
                equality = null;
            }

            if (node.Comparison != equality)
            {
                return false;
            }

            // All test values must be constant.
            int tests = 0;
            foreach (SwitchCase c in node.Cases)
            {
                foreach (Expression t in c.TestValues)
                {
                    if (!(t is ConstantExpression))
                    {
                        return false;
                    }
                    tests++;
                }
            }

            // Must have >= 7 labels for it to be worth it.
            if (tests < 7)
            {
                return false;
            }

            // If we're in a DynamicMethod, we could just build the dictionary
            // immediately. But that would cause the two code paths to be more
            // different than they really need to be.
            var initializers = new List<ElementInit>(tests);
            var cases = new ArrayBuilder<SwitchCase>(node.Cases.Count);

            int nullCase = -1;
            MethodInfo add = DictionaryOfStringInt32_Add_String_Int32;
            for (int i = 0, n = node.Cases.Count; i < n; i++)
            {
                foreach (ConstantExpression t in node.Cases[i].TestValues)
                {
                    if (t.Value != null)
                    {
                        initializers.Add(Expression.ElementInit(add, t, Utils.Constant(i)));
                    }
                    else
                    {
                        nullCase = i;
                    }
                }
                cases.UncheckedAdd(Expression.SwitchCase(node.Cases[i].Body, Utils.Constant(i)));
            }

            // Create the field to hold the lazily initialized dictionary
            MemberExpression dictField = CreateLazyInitializedField<Dictionary<string, int>>("dictionarySwitch");

            // If we happen to initialize it twice (multithreaded case), it's
            // not the end of the world. The C# compiler does better here by
            // emitting a volatile access to the field.
            Expression dictInit = Expression.Condition(
                Expression.Equal(dictField, Expression.Constant(null, dictField.Type)),
                Expression.Assign(
                    dictField,
                    Expression.ListInit(
                        Expression.New(
                            DictionaryOfStringInt32_Ctor_Int32,
                            Utils.Constant(initializers.Count)
                        ),
                        initializers
                    )
                ),
                dictField
            );

            //
            // Create a tree like:
            //
            // switchValue = switchValueExpression;
            // if (switchValue == null) {
            //     switchIndex = nullCase;
            // } else {
            //     if (_dictField == null) {
            //         _dictField = new Dictionary<string, int>(count) { { ... }, ... };
            //     }
            //     if (!_dictField.TryGetValue(switchValue, out switchIndex)) {
            //         switchIndex = -1;
            //     }
            // }
            // switch (switchIndex) {
            //     case 0: ...
            //     case 1: ...
            //     ...
            //     default:
            // }
            //
            ParameterExpression switchValue = Expression.Variable(typeof(string), "switchValue");
            ParameterExpression switchIndex = Expression.Variable(typeof(int), "switchIndex");
            BlockExpression reduced = Expression.Block(
                new TrueReadOnlyCollection<ParameterExpression>(switchIndex, switchValue),
                Expression.Assign(switchValue, node.SwitchValue),
                Expression.IfThenElse(
                    Expression.Equal(switchValue, Expression.Constant(null, typeof(string))),
                    Expression.Assign(switchIndex, Utils.Constant(nullCase)),
                    Expression.IfThenElse(
                        Expression.Call(dictInit, "TryGetValue", null, switchValue, switchIndex),
                        Utils.Empty,
                        Expression.Assign(switchIndex, Utils.Constant(-1))
                    )
                ),
                Expression.Switch(node.Type, switchIndex, node.DefaultBody, null, cases.ToReadOnly())
            );

            EmitExpression(reduced, flags);
            return true;
        }

        #endregion

        private void CheckRethrow()
        {
            // Rethrow is only valid inside a catch.
            for (LabelScopeInfo j = _labelBlock; j != null; j = j.Parent)
            {
                if (j.Kind == LabelScopeKind.Catch)
                {
                    return;
                }
                else if (j.Kind == LabelScopeKind.Finally)
                {
                    // Rethrow from inside finally is not verifiable
                    break;
                }
            }
            throw Error.RethrowRequiresCatch();
        }

        #region TryStatement

        private void CheckTry()
        {
            // Try inside a filter is not verifiable
            for (LabelScopeInfo j = _labelBlock; j != null; j = j.Parent)
            {
                if (j.Kind == LabelScopeKind.Filter)
                {
                    throw Error.TryNotAllowedInFilter();
                }
            }
        }

        private void EmitSaveExceptionOrPop(CatchBlock cb)
        {
            if (cb.Variable != null)
            {
                // If the variable is present, store the exception
                // in the variable.
                _scope.EmitSet(cb.Variable);
            }
            else
            {
                // Otherwise, pop it off the stack.
                _ilg.Emit(OpCodes.Pop);
            }
        }

        private void EmitTryExpression(Expression expr)
        {
            var node = (TryExpression)expr;

            CheckTry();

            //******************************************************************
            // 1. ENTERING TRY
            //******************************************************************

            PushLabelBlock(LabelScopeKind.Try);
            _ilg.BeginExceptionBlock();

            //******************************************************************
            // 2. Emit the try statement body
            //******************************************************************

            EmitExpression(node.Body);

            Type tryType = node.Type;
            LocalBuilder value = null;
            if (tryType != typeof(void))
            {
                //store the value of the try body
                value = GetLocal(tryType);
                _ilg.Emit(OpCodes.Stloc, value);
            }
            //******************************************************************
            // 3. Emit the catch blocks
            //******************************************************************

            foreach (CatchBlock cb in node.Handlers)
            {
                PushLabelBlock(LabelScopeKind.Catch);

                // Begin the strongly typed exception block
                if (cb.Filter == null)
                {
                    _ilg.BeginCatchBlock(cb.Test);
                }
                else
                {
                    _ilg.BeginExceptFilterBlock();
                }

                EnterScope(cb);

                EmitCatchStart(cb);

                //
                // Emit the catch block body
                //
                EmitExpression(cb.Body);
                if (tryType != typeof(void))
                {
                    //store the value of the catch block body
                    _ilg.Emit(OpCodes.Stloc, value);
                }

                ExitScope(cb);

                PopLabelBlock(LabelScopeKind.Catch);
            }

            //******************************************************************
            // 4. Emit the finally block
            //******************************************************************

            if (node.Finally != null || node.Fault != null)
            {
                PushLabelBlock(LabelScopeKind.Finally);

                if (node.Finally != null)
                {
                    _ilg.BeginFinallyBlock();
                }
                else
                {
                    _ilg.BeginFaultBlock();
                }

                // Emit the body
                EmitExpressionAsVoid(node.Finally ?? node.Fault);

                _ilg.EndExceptionBlock();
                PopLabelBlock(LabelScopeKind.Finally);
            }
            else
            {
                _ilg.EndExceptionBlock();
            }

            if (tryType != typeof(void))
            {
                _ilg.Emit(OpCodes.Ldloc, value);
                FreeLocal(value);
            }
            PopLabelBlock(LabelScopeKind.Try);
        }

        /// <summary>
        /// Emits the start of a catch block.  The exception value that is provided by the
        /// CLR is stored in the variable specified by the catch block or popped if no
        /// variable is provided.
        /// </summary>
        private void EmitCatchStart(CatchBlock cb)
        {
            if (cb.Filter == null)
            {
                EmitSaveExceptionOrPop(cb);
                return;
            }

            // emit filter block. Filter blocks are untyped so we need to do
            // the type check ourselves.
            Label endFilter = _ilg.DefineLabel();
            Label rightType = _ilg.DefineLabel();

            // skip if it's not our exception type, but save
            // the exception if it is so it's available to the
            // filter
            _ilg.Emit(OpCodes.Isinst, cb.Test);
            _ilg.Emit(OpCodes.Dup);
            _ilg.Emit(OpCodes.Brtrue, rightType);
            _ilg.Emit(OpCodes.Pop);
            _ilg.Emit(OpCodes.Ldc_I4_0);
            _ilg.Emit(OpCodes.Br, endFilter);

            // it's our type, save it and emit the filter.
            _ilg.MarkLabel(rightType);
            EmitSaveExceptionOrPop(cb);
            PushLabelBlock(LabelScopeKind.Filter);
            EmitExpression(cb.Filter);
            PopLabelBlock(LabelScopeKind.Filter);

            // begin the catch, clear the exception, we've
            // already saved it
            _ilg.MarkLabel(endFilter);
            _ilg.BeginCatchBlock(exceptionType: null);
            _ilg.Emit(OpCodes.Pop);
        }

        #endregion
    }
}
