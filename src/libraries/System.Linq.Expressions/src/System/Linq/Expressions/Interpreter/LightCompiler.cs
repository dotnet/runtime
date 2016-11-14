// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.CachedReflectionInfo;

using AstUtils = System.Linq.Expressions.Utils;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class ExceptionFilter
    {
        public readonly int LabelIndex;
        public readonly int StartIndex;
        public readonly int EndIndex;

        internal ExceptionFilter(int labelIndex, int start, int end)
        {
            LabelIndex = labelIndex;
            StartIndex = start;
            EndIndex = end;
        }
    }

    internal sealed class ExceptionHandler
    {
        public readonly Type ExceptionType;
        public readonly int LabelIndex;
        public readonly int HandlerStartIndex;
        public readonly int HandlerEndIndex;
        public readonly ExceptionFilter Filter;

        internal TryCatchFinallyHandler Parent = null;

        internal ExceptionHandler(int labelIndex, int handlerStartIndex, int handlerEndIndex, Type exceptionType, ExceptionFilter filter)
        {
            Debug.Assert(exceptionType != null);
            LabelIndex = labelIndex;
            ExceptionType = exceptionType;
            HandlerStartIndex = handlerStartIndex;
            HandlerEndIndex = handlerEndIndex;
            Filter = filter;
        }

        internal void SetParent(TryCatchFinallyHandler tryHandler)
        {
            Debug.Assert(Parent == null);
            Parent = tryHandler;
        }

        public bool Matches(Type exceptionType) => ExceptionType.IsAssignableFrom(exceptionType);

        public override string ToString() =>
            string.Format(CultureInfo.InvariantCulture, "catch({0}) [{1}->{2}]", ExceptionType.Name, HandlerStartIndex, HandlerEndIndex);
    }

    internal sealed class TryCatchFinallyHandler
    {
        internal readonly int TryStartIndex;
        internal readonly int TryEndIndex;
        internal readonly int FinallyStartIndex;
        internal readonly int FinallyEndIndex;
        internal readonly int GotoEndTargetIndex;

        private readonly ExceptionHandler[] _handlers;

        internal bool IsFinallyBlockExist
        {
            get
            {
                Debug.Assert((FinallyStartIndex != Instruction.UnknownInstrIndex) == (FinallyEndIndex != Instruction.UnknownInstrIndex));
                return FinallyStartIndex != Instruction.UnknownInstrIndex;
            }
        }

        internal ExceptionHandler[] Handlers => _handlers;

        internal bool IsCatchBlockExist => _handlers != null;

        /// <summary>
        /// No finally block
        /// </summary>
        internal TryCatchFinallyHandler(int tryStart, int tryEnd, int gotoEndTargetIndex, ExceptionHandler[] handlers)
            : this(tryStart, tryEnd, gotoEndTargetIndex, Instruction.UnknownInstrIndex, Instruction.UnknownInstrIndex, handlers)
        {
            Debug.Assert(handlers != null, "catch blocks should exist");
        }

        /// <summary>
        /// Generic constructor
        /// </summary>
        internal TryCatchFinallyHandler(int tryStart, int tryEnd, int gotoEndLabelIndex, int finallyStart, int finallyEnd, ExceptionHandler[] handlers)
        {
            TryStartIndex = tryStart;
            TryEndIndex = tryEnd;
            FinallyStartIndex = finallyStart;
            FinallyEndIndex = finallyEnd;
            GotoEndTargetIndex = gotoEndLabelIndex;

            _handlers = handlers;

            if (_handlers != null)
            {
                foreach (ExceptionHandler handler in _handlers)
                {
                    handler.SetParent(this);
                }
            }
        }

        internal bool HasHandler(InterpretedFrame frame, ref Exception exception, out ExceptionHandler handler)
        {
#if DEBUG
            if (exception is RethrowException)
            {
                // Unreachable.
                // Want to assert that this case isn't hit, but an assertion failure here will be eaten because
                // we are in an exception filter. Therefore return true here and assert in the catch block.
                handler = null;
                return true;
            }
#endif
            frame.SaveTraceToException(exception);

            if (IsCatchBlockExist)
            {
                Type exceptionType = exception.GetType();
                for (int i = 0; i != _handlers.Length; ++i)
                {
                    ExceptionHandler candidate = _handlers[i];
                    if (candidate.Matches(exceptionType) && (candidate.Filter == null || FilterPasses(frame, ref exception, candidate.Filter)))
                    {
                        handler = candidate;
                        return true;
                    }
                }
            }

            handler = null;
            return false;
        }

        internal bool FilterPasses(InterpretedFrame frame, ref Exception exception, ExceptionFilter filter)
        {
            Interpreter interpreter = frame.Interpreter;
            Instruction[] instructions = interpreter.Instructions.Instructions;
            int stackIndex = frame.StackIndex;
            try
            {
                int index = interpreter._labels[filter.LabelIndex].Index;
                frame.Push(exception);
                while (index >= filter.StartIndex && index < filter.EndIndex)
                {
                    index += instructions[index].Run(frame);
                }

                if ((bool)frame.Pop())
                {
                    // If this is the handler that will be executed, then if the filter has assigned to the exception variable
                    // that change should be visible to the handler. Otherwise, it should not.
                    exception = (Exception)frame.Peek();
                    return true;
                }
            }
            catch
            {
                // Silently eating exceptions and returning false matches the CLR behavior.
                // Restore stack depth first.
                frame.StackIndex = stackIndex;
            }

            return false;
        }
    }

    internal sealed class TryFaultHandler
    {
        internal readonly int TryStartIndex;
        internal readonly int TryEndIndex;
        internal readonly int FinallyStartIndex;
        internal readonly int FinallyEndIndex;

        internal TryFaultHandler(int tryStart, int tryEnd, int finallyStart, int finallyEnd)
        {
            TryStartIndex = tryStart;
            TryEndIndex = tryEnd;
            FinallyStartIndex = finallyStart;
            FinallyEndIndex = finallyEnd;
        }
    }

    /// <summary>
    /// The re-throw instruction will throw this exception
    /// </summary>
    internal sealed class RethrowException : Exception
    {
    }

    internal sealed class DebugInfo
    {
        public int StartLine, EndLine;
        public int Index;
        public string FileName;
        public bool IsClear;
        private static readonly DebugInfoComparer s_debugComparer = new DebugInfoComparer();

        private class DebugInfoComparer : IComparer<DebugInfo>
        {
            //We allow comparison between int and DebugInfo here
            int IComparer<DebugInfo>.Compare(DebugInfo d1, DebugInfo d2)
            {
                if (d1.Index > d2.Index) return 1;
                else if (d1.Index == d2.Index) return 0;
                else return -1;
            }
        }

        public static DebugInfo GetMatchingDebugInfo(DebugInfo[] debugInfos, int index)
        {
            //Create a faked DebugInfo to do the search
            var d = new DebugInfo { Index = index };

            //to find the closest debug info before the current index

            int i = Array.BinarySearch<DebugInfo>(debugInfos, d, s_debugComparer);
            if (i < 0)
            {
                //~i is the index for the first bigger element
                //if there is no bigger element, ~i is the length of the array
                i = ~i;
                if (i == 0)
                {
                    return null;
                }
                //return the last one that is smaller
                i = i - 1;
            }

            return debugInfos[i];
        }

        public override string ToString()
        {
            if (IsClear)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}: clear", Index);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}: [{1}-{2}] '{3}'", Index, StartLine, EndLine, FileName);
            }
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes")]
    internal struct InterpretedFrameInfo
    {
        private readonly string _methodName;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        private readonly DebugInfo _debugInfo;

        public InterpretedFrameInfo(string methodName, DebugInfo info)
        {
            _methodName = methodName;
            _debugInfo = info;
        }

        public override string ToString() => _debugInfo != null ? _methodName + ": " + _debugInfo : _methodName;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    internal sealed class LightCompiler
    {
        private readonly InstructionList _instructions;
        private readonly LocalVariables _locals = new LocalVariables();

        private readonly List<DebugInfo> _debugInfos = new List<DebugInfo>();
        private readonly HybridReferenceDictionary<LabelTarget, LabelInfo> _treeLabels = new HybridReferenceDictionary<LabelTarget, LabelInfo>();
        private LabelScopeInfo _labelBlock = new LabelScopeInfo(null, LabelScopeKind.Lambda);

        private readonly Stack<ParameterExpression> _exceptionForRethrowStack = new Stack<ParameterExpression>();

        private readonly LightCompiler _parent;

        private readonly StackGuard _guard = new StackGuard();

        private static LocalDefinition[] s_emptyLocals = Array.Empty<LocalDefinition>();

        public LightCompiler()
        {
            _instructions = new InstructionList();
        }

        private LightCompiler(LightCompiler parent)
            : this()
        {
            _parent = parent;
        }

        public InstructionList Instructions => _instructions;

        public LightDelegateCreator CompileTop(LambdaExpression node)
        {
            //Console.WriteLine(node.DebugView);
            foreach (ParameterExpression p in node.Parameters)
            {
                LocalDefinition local = _locals.DefineLocal(p, 0);
                _instructions.EmitInitializeParameter(local.Index);
            }

            Compile(node.Body);

            // pop the result of the last expression:
            if (node.Body.Type != typeof(void) && node.ReturnType == typeof(void))
            {
                _instructions.EmitPop();
            }

            Debug.Assert(_instructions.CurrentStackDepth == (node.ReturnType != typeof(void) ? 1 : 0));

            return new LightDelegateCreator(MakeInterpreter(node.Name), node);
        }

        private Interpreter MakeInterpreter(string lambdaName)
        {
            DebugInfo[] debugInfos = _debugInfos.ToArray();
            return new Interpreter(lambdaName, _locals, _instructions.ToArray(), debugInfos);
        }

        private void CompileConstantExpression(Expression expr)
        {
            var node = (ConstantExpression)expr;
            _instructions.EmitLoad(node.Value, node.Type);
        }

        private void CompileDefaultExpression(Expression expr)
        {
            CompileDefaultExpression(expr.Type);
        }

        private void CompileDefaultExpression(Type type)
        {
            if (type != typeof(void))
            {
                if (type.GetTypeInfo().IsValueType)
                {
                    object value = ScriptingRuntimeHelpers.GetPrimitiveDefaultValue(type);
                    if (value != null)
                    {
                        _instructions.EmitLoad(value);
                    }
                    else
                    {
                        _instructions.EmitDefaultValue(type);
                    }
                }
                else
                {
                    _instructions.EmitLoad(value: null);
                }
            }
        }

        private LocalVariable EnsureAvailableForClosure(ParameterExpression expr)
        {
            LocalVariable local;
            if (_locals.TryGetLocalOrClosure(expr, out local))
            {
                if (!local.InClosure && !local.IsBoxed)
                {
                    _locals.Box(expr, _instructions);
                }
                return local;
            }
            else if (_parent != null)
            {
                _parent.EnsureAvailableForClosure(expr);
                return _locals.AddClosureVariable(expr);
            }
            else
            {
                throw new InvalidOperationException("unbound variable: " + expr);
            }
        }

        private LocalVariable ResolveLocal(ParameterExpression variable)
        {
            LocalVariable local;
            if (!_locals.TryGetLocalOrClosure(variable, out local))
            {
                local = EnsureAvailableForClosure(variable);
            }
            return local;
        }

        private void CompileGetVariable(ParameterExpression variable)
        {
            LoadLocalNoValueTypeCopy(variable);

            _instructions.SetDebugCookie(variable.Name);

            EmitCopyValueType(variable.Type);
        }

        private void EmitCopyValueType(Type valueType)
        {
            if (MaybeMutableValueType(valueType))
            {
                // loading a value type on the stack has copy semantics unless
                // we are specifically loading the address of the object, so we
                // emit a copy here if we don't know the type is immutable.
                _instructions.Emit(ValueTypeCopyInstruction.Instruction);
            }
        }

        private void LoadLocalNoValueTypeCopy(ParameterExpression variable)
        {
            LocalVariable local = ResolveLocal(variable);

            if (local.InClosure)
            {
                _instructions.EmitLoadLocalFromClosure(local.Index);
            }
            else if (local.IsBoxed)
            {
                _instructions.EmitLoadLocalBoxed(local.Index);
            }
            else
            {
                _instructions.EmitLoadLocal(local.Index);
            }
        }

        private bool MaybeMutableValueType(Type type)
        {
            return type.GetTypeInfo().IsValueType && !type.GetTypeInfo().IsEnum && !type.GetTypeInfo().IsPrimitive;
        }

        private void CompileGetBoxedVariable(ParameterExpression variable)
        {
            LocalVariable local = ResolveLocal(variable);

            if (local.InClosure)
            {
                _instructions.EmitLoadLocalFromClosureBoxed(local.Index);
            }
            else
            {
                Debug.Assert(local.IsBoxed);
                _instructions.EmitLoadLocal(local.Index);
            }

            _instructions.SetDebugCookie(variable.Name);
        }

        private void CompileSetVariable(ParameterExpression variable, bool isVoid)
        {
            LocalVariable local = ResolveLocal(variable);

            if (local.InClosure)
            {
                if (isVoid)
                {
                    _instructions.EmitStoreLocalToClosure(local.Index);
                }
                else
                {
                    _instructions.EmitAssignLocalToClosure(local.Index);
                }
            }
            else if (local.IsBoxed)
            {
                if (isVoid)
                {
                    _instructions.EmitStoreLocalBoxed(local.Index);
                }
                else
                {
                    _instructions.EmitAssignLocalBoxed(local.Index);
                }
            }
            else
            {
                if (isVoid)
                {
                    _instructions.EmitStoreLocal(local.Index);
                }
                else
                {
                    _instructions.EmitAssignLocal(local.Index);
                }
            }

            _instructions.SetDebugCookie(variable.Name);
        }

        private void CompileParameterExpression(Expression expr)
        {
            var node = (ParameterExpression)expr;
            CompileGetVariable(node);
        }

        private void CompileBlockExpression(Expression expr, bool asVoid)
        {
            var node = (BlockExpression)expr;

            if (node.ExpressionCount != 0)
            {
                LocalDefinition[] end = CompileBlockStart(node);

                Expression lastExpression = node.Expressions[node.Expressions.Count - 1];
                Compile(lastExpression, asVoid);
                CompileBlockEnd(end);
            }
        }

        private LocalDefinition[] CompileBlockStart(BlockExpression node)
        {
            int start = _instructions.Count;

            LocalDefinition[] locals;
            ReadOnlyCollection<ParameterExpression> variables = node.Variables;
            if (variables.Count != 0)
            {
                // TODO: basic flow analysis so we don't have to initialize all
                // variables.
                locals = new LocalDefinition[variables.Count];
                int localCnt = 0;
                foreach (ParameterExpression variable in variables)
                {
                    LocalDefinition local = _locals.DefineLocal(variable, start);
                    locals[localCnt++] = local;

                    _instructions.EmitInitializeLocal(local.Index, variable.Type);
                    _instructions.SetDebugCookie(variable.Name);
                }
            }
            else
            {
                locals = s_emptyLocals;
            }

            for (int i = 0; i < node.Expressions.Count - 1; i++)
            {
                CompileAsVoid(node.Expressions[i]);
            }
            return locals;
        }

        private void CompileBlockEnd(LocalDefinition[] locals)
        {
            foreach (LocalDefinition local in locals)
            {
                _locals.UndefineLocal(local, _instructions.Count);
            }
        }

        private void CompileIndexExpression(Expression expr)
        {
            var index = (IndexExpression)expr;

            // instance:
            if (index.Object != null)
            {
                EmitThisForMethodCall(index.Object);
            }

            // indexes, byref args not allowed.
            for (int i = 0, n = index.ArgumentCount; i < n; i++)
            {
                Compile(index.GetArgument(i));
            }

            EmitIndexGet(index);
        }

        private void EmitIndexGet(IndexExpression index)
        {
            if (index.Indexer != null)
            {
                _instructions.EmitCall(index.Indexer.GetGetMethod(nonPublic: true));
            }
            else if (index.ArgumentCount != 1)
            {
                _instructions.EmitCall(index.Object.Type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance));
            }
            else
            {
                _instructions.EmitGetArrayItem();
            }
        }

        private void CompileIndexAssignment(BinaryExpression node, bool asVoid)
        {
            var index = (IndexExpression)node.Left;

            // instance:
            if (index.Object != null)
            {
                EmitThisForMethodCall(index.Object);
            }

            // indexes, byref args not allowed.
            for (int i = 0, n = index.ArgumentCount; i < n; i++)
            {
                Compile(index.GetArgument(i));
            }

            // value:
            Compile(node.Right);
            LocalDefinition local = default(LocalDefinition);
            if (!asVoid)
            {
                local = _locals.DefineLocal(Expression.Parameter(node.Right.Type), _instructions.Count);
                _instructions.EmitAssignLocal(local.Index);
            }

            if (index.Indexer != null)
            {
                _instructions.EmitCall(index.Indexer.GetSetMethod(nonPublic: true));
            }
            else if (index.ArgumentCount != 1)
            {
                _instructions.EmitCall(index.Object.Type.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance));
            }
            else
            {
                _instructions.EmitSetArrayItem();
            }

            if (!asVoid)
            {
                _instructions.EmitLoadLocal(local.Index);
                _locals.UndefineLocal(local, _instructions.Count);
            }
        }

        private void CompileMemberAssignment(BinaryExpression node, bool asVoid)
        {
            var member = (MemberExpression)node.Left;
            Expression expr = member.Expression;
            if (expr != null)
            {
                EmitThisForMethodCall(expr);
            }

            CompileMemberAssignment(asVoid, member.Member, node.Right, forBinding: false);
        }

        private void CompileMemberAssignment(bool asVoid, MemberInfo refMember, Expression value, bool forBinding)
        {
            var pi = refMember as PropertyInfo;
            if (pi != null)
            {
                MethodInfo method = pi.GetSetMethod(nonPublic: true);
                if (forBinding && method.IsStatic)
                {
                    throw Error.InvalidProgram();
                }

                EmitThisForMethodCall(value);

                int start = _instructions.Count;
                if (!asVoid)
                {
                    LocalDefinition local = _locals.DefineLocal(Expression.Parameter(value.Type), start);
                    _instructions.EmitAssignLocal(local.Index);
                    _instructions.EmitCall(method);
                    _instructions.EmitLoadLocal(local.Index);
                    _locals.UndefineLocal(local, _instructions.Count);
                }
                else
                {
                    _instructions.EmitCall(method);
                }
            }
            else
            {
                // other types inherited from MemberInfo (EventInfo\MethodBase\Type) cannot be used in MemberAssignment
                var fi = (FieldInfo)refMember;
                Debug.Assert(fi != null);
                if (fi.IsLiteral)
                {
                    throw Error.NotSupported();
                }

                if (forBinding && fi.IsStatic)
                {
                    _instructions.UnEmit(); // Undo having pushed the instance to the stack.
                }

                EmitThisForMethodCall(value);

                int start = _instructions.Count;
                if (!asVoid)
                {
                    LocalDefinition local = _locals.DefineLocal(Expression.Parameter(value.Type), start);
                    _instructions.EmitAssignLocal(local.Index);
                    _instructions.EmitStoreField(fi);
                    _instructions.EmitLoadLocal(local.Index);
                    _locals.UndefineLocal(local, _instructions.Count);
                }
                else
                {
                    _instructions.EmitStoreField(fi);
                }
            }
        }

        private void CompileVariableAssignment(BinaryExpression node, bool asVoid)
        {
            Compile(node.Right);

            var target = (ParameterExpression)node.Left;
            CompileSetVariable(target, asVoid);
        }

        private void CompileAssignBinaryExpression(Expression expr, bool asVoid)
        {
            var node = (BinaryExpression)expr;

            switch (node.Left.NodeType)
            {
                case ExpressionType.Index:
                    CompileIndexAssignment(node, asVoid);
                    break;

                case ExpressionType.MemberAccess:
                    CompileMemberAssignment(node, asVoid);
                    break;

                case ExpressionType.Parameter:
                case ExpressionType.Extension:
                    CompileVariableAssignment(node, asVoid);
                    break;

                default:
                    throw Error.InvalidLvalue(node.Left.NodeType);
            }
        }

        private void CompileBinaryExpression(Expression expr)
        {
            var node = (BinaryExpression)expr;

            if (node.Method != null)
            {
                if (node.IsLifted)
                {
                    // lifting: we need to do the null checks for nullable types and reference types.  If the value
                    // is null we return null, or false for a comparison unless it's not equal, in which case we return
                    // true.

                    // INCOMPAT: The DLR binder short circuits on comparisons other than equal and not equal,
                    // but C# doesn't.
                    BranchLabel end = _instructions.MakeLabel();

                    LocalDefinition leftTemp = _locals.DefineLocal(Expression.Parameter(node.Left.Type), _instructions.Count);
                    Compile(node.Left);
                    _instructions.EmitStoreLocal(leftTemp.Index);

                    LocalDefinition rightTemp = _locals.DefineLocal(Expression.Parameter(node.Right.Type), _instructions.Count);
                    Compile(node.Right);
                    _instructions.EmitStoreLocal(rightTemp.Index);

                    switch (node.NodeType)
                    {
                        case ExpressionType.Equal:
                        case ExpressionType.NotEqual:
                            /* generating (equal/not equal):
                                * if(left == null) {
                                *      right == null/right != null
                                * }else if(right == null) {
                                *      False/True
                                * }else{
                                *      op_Equality(left, right)/op_Inequality(left, right)
                                * }
                                */
                            if (node.IsLiftedToNull)
                            {
                                goto default;
                            }

                            BranchLabel testRight = _instructions.MakeLabel();
                            BranchLabel callMethod = _instructions.MakeLabel();

                            _instructions.EmitLoadLocal(leftTemp.Index);
                            _instructions.EmitLoad(null, typeof(object));
                            _instructions.EmitEqual(typeof(object));
                            _instructions.EmitBranchFalse(testRight);

                            // left is null
                            _instructions.EmitLoadLocal(rightTemp.Index);
                            _instructions.EmitLoad(null, typeof(object));
                            if (node.NodeType == ExpressionType.Equal)
                            {
                                _instructions.EmitEqual(typeof(object));
                            }
                            else
                            {
                                _instructions.EmitNotEqual(typeof(object));
                            }
                            _instructions.EmitBranch(end, hasResult: false, hasValue: true);

                            _instructions.MarkLabel(testRight);

                            // left is not null, check right
                            _instructions.EmitLoadLocal(rightTemp.Index);
                            _instructions.EmitLoad(null, typeof(object));
                            _instructions.EmitEqual(typeof(object));
                            _instructions.EmitBranchFalse(callMethod);

                            if (node.NodeType == ExpressionType.Equal)
                            {
                                // right null, left not, false
                                _instructions.EmitLoad(ScriptingRuntimeHelpers.Boolean_False, typeof(bool));
                            }
                            else
                            {
                                // right null, left not, true
                                _instructions.EmitLoad(ScriptingRuntimeHelpers.Boolean_True, typeof(bool));
                            }
                            _instructions.EmitBranch(end, hasResult: false, hasValue: true);

                            // both are not null
                            _instructions.MarkLabel(callMethod);
                            _instructions.EmitLoadLocal(leftTemp.Index);
                            _instructions.EmitLoadLocal(rightTemp.Index);
                            _instructions.EmitCall(node.Method);
                            break;
                        default:
                            BranchLabel loadDefault = _instructions.MakeLabel();

                            if (node.Left.Type.IsNullableOrReferenceType())
                            {
                                _instructions.EmitLoadLocal(leftTemp.Index);
                                _instructions.EmitLoad(null, typeof(object));
                                _instructions.EmitEqual(typeof(object));
                                _instructions.EmitBranchTrue(loadDefault);
                            }

                            if (node.Right.Type.IsNullableOrReferenceType())
                            {
                                _instructions.EmitLoadLocal(rightTemp.Index);
                                _instructions.EmitLoad(null, typeof(object));
                                _instructions.EmitEqual(typeof(object));
                                _instructions.EmitBranchTrue(loadDefault);
                            }

                            _instructions.EmitLoadLocal(leftTemp.Index);
                            _instructions.EmitLoadLocal(rightTemp.Index);
                            _instructions.EmitCall(node.Method);
                            _instructions.EmitBranch(end, hasResult: false, hasValue: true);

                            _instructions.MarkLabel(loadDefault);
                            switch (node.NodeType)
                            {
                                case ExpressionType.LessThan:
                                case ExpressionType.LessThanOrEqual:
                                case ExpressionType.GreaterThan:
                                case ExpressionType.GreaterThanOrEqual:
                                    if (node.IsLiftedToNull)
                                    {
                                        goto default;
                                    }
                                    _instructions.EmitLoad(ScriptingRuntimeHelpers.Boolean_False, typeof(object));
                                    break;
                                default:
                                    _instructions.EmitLoad(null, typeof(object));
                                    break;
                            }
                            break;
                    }

                    _instructions.MarkLabel(end);

                    _locals.UndefineLocal(leftTemp, _instructions.Count);
                    _locals.UndefineLocal(rightTemp, _instructions.Count);
                }
                else
                {
                    Compile(node.Left);
                    Compile(node.Right);
                    _instructions.EmitCall(node.Method);
                }
            }
            else
            {
                switch (node.NodeType)
                {
                    case ExpressionType.ArrayIndex:
                        Debug.Assert(node.Right.Type == typeof(int));
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitGetArrayItem();
                        return;

                    case ExpressionType.Add:
                    case ExpressionType.AddChecked:
                    case ExpressionType.Subtract:
                    case ExpressionType.SubtractChecked:
                    case ExpressionType.Multiply:
                    case ExpressionType.MultiplyChecked:
                    case ExpressionType.Divide:
                    case ExpressionType.Modulo:
                        CompileArithmetic(node.NodeType, node.Left, node.Right);
                        return;

                    case ExpressionType.ExclusiveOr:
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitExclusiveOr(node.Left.Type);
                        break;
                    case ExpressionType.Or:
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitOr(node.Left.Type);
                        break;
                    case ExpressionType.And:
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitAnd(node.Left.Type);
                        break;

                    case ExpressionType.Equal:
                        CompileEqual(node.Left, node.Right, node.IsLiftedToNull);
                        return;

                    case ExpressionType.NotEqual:
                        CompileNotEqual(node.Left, node.Right, node.IsLiftedToNull);
                        return;

                    case ExpressionType.LessThan:
                    case ExpressionType.LessThanOrEqual:
                    case ExpressionType.GreaterThan:
                    case ExpressionType.GreaterThanOrEqual:
                        CompileComparison((BinaryExpression)node);
                        return;

                    case ExpressionType.LeftShift:
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitLeftShift(node.Left.Type);
                        break;
                    case ExpressionType.RightShift:
                        Compile(node.Left);
                        Compile(node.Right);
                        _instructions.EmitRightShift(node.Left.Type);
                        break;
                    default:
                        throw new PlatformNotSupportedException(SR.Format(SR.UnsupportedExpressionType, node.NodeType));
                }
            }
        }

        private void CompileEqual(Expression left, Expression right, bool liftedToNull)
        {
            Debug.Assert(left.Type == right.Type || !left.Type.GetTypeInfo().IsValueType && !right.Type.GetTypeInfo().IsValueType);
            Compile(left);
            Compile(right);
            _instructions.EmitEqual(left.Type, liftedToNull);
        }

        private void CompileNotEqual(Expression left, Expression right, bool liftedToNull)
        {
            Debug.Assert(left.Type == right.Type || !left.Type.GetTypeInfo().IsValueType && !right.Type.GetTypeInfo().IsValueType);
            Compile(left);
            Compile(right);
            _instructions.EmitNotEqual(left.Type, liftedToNull);
        }

        private void CompileComparison(BinaryExpression node)
        {
            Expression left = node.Left;
            Expression right = node.Right;
            Debug.Assert(left.Type == right.Type && left.Type.IsNumeric());

            Compile(left);
            Compile(right);

            switch (node.NodeType)
            {
                case ExpressionType.LessThan: _instructions.EmitLessThan(left.Type, node.IsLiftedToNull); break;
                case ExpressionType.LessThanOrEqual: _instructions.EmitLessThanOrEqual(left.Type, node.IsLiftedToNull); break;
                case ExpressionType.GreaterThan: _instructions.EmitGreaterThan(left.Type, node.IsLiftedToNull); break;
                case ExpressionType.GreaterThanOrEqual: _instructions.EmitGreaterThanOrEqual(left.Type, node.IsLiftedToNull); break;
                default: throw ContractUtils.Unreachable;
            }
        }

        private void CompileArithmetic(ExpressionType nodeType, Expression left, Expression right)
        {
            Debug.Assert(left.Type == right.Type && left.Type.IsArithmetic());
            Compile(left);
            Compile(right);
            switch (nodeType)
            {
                case ExpressionType.Add: _instructions.EmitAdd(left.Type, @checked: false); break;
                case ExpressionType.AddChecked: _instructions.EmitAdd(left.Type, @checked: true); break;
                case ExpressionType.Subtract: _instructions.EmitSub(left.Type, @checked: false); break;
                case ExpressionType.SubtractChecked: _instructions.EmitSub(left.Type, @checked: true); break;
                case ExpressionType.Multiply: _instructions.EmitMul(left.Type, @checked: false); break;
                case ExpressionType.MultiplyChecked: _instructions.EmitMul(left.Type, @checked: true); break;
                case ExpressionType.Divide: _instructions.EmitDiv(left.Type); break;
                case ExpressionType.Modulo: _instructions.EmitModulo(left.Type); break;
                default: throw ContractUtils.Unreachable;
            }
        }

        private void CompileConvertUnaryExpression(Expression expr)
        {
            var node = (UnaryExpression)expr;
            if (node.Method != null)
            {
                BranchLabel end = _instructions.MakeLabel();
                BranchLabel loadDefault = _instructions.MakeLabel();

                LocalDefinition opTemp = _locals.DefineLocal(Expression.Parameter(node.Operand.Type), _instructions.Count);
                Compile(node.Operand);
                _instructions.EmitStoreLocal(opTemp.Index);

                if (!node.Operand.Type.GetTypeInfo().IsValueType ||
                    (node.Operand.Type.IsNullableType() && node.IsLiftedToNull))
                {
                    _instructions.EmitLoadLocal(opTemp.Index);
                    _instructions.EmitLoad(null, typeof(object));
                    _instructions.EmitEqual(typeof(object));
                    _instructions.EmitBranchTrue(loadDefault);
                }

                _instructions.EmitLoadLocal(opTemp.Index);
                if (node.Operand.Type.IsNullableType() &&
                    node.Method.GetParametersCached()[0].ParameterType.Equals(node.Operand.Type.GetNonNullableType()))
                {
                    _instructions.Emit(NullableMethodCallInstruction.CreateGetValue());
                }

                _instructions.EmitCall(node.Method);

                _instructions.EmitBranch(end, hasResult: false, hasValue: true);

                _instructions.MarkLabel(loadDefault);
                _instructions.EmitLoad(null, typeof(object));

                _instructions.MarkLabel(end);

                _locals.UndefineLocal(opTemp, _instructions.Count);
            }
            else if (node.Type == typeof(void))
            {
                CompileAsVoid(node.Operand);
            }
            else
            {
                Compile(node.Operand);
                CompileConvertToType(node.Operand.Type, node.Type, node.NodeType == ExpressionType.ConvertChecked, node.IsLiftedToNull);
            }
        }

        private void CompileConvertToType(Type typeFrom, Type typeTo, bool isChecked, bool isLiftedToNull)
        {
            Debug.Assert(typeFrom != typeof(void) && typeTo != typeof(void));

            if (typeTo.Equals(typeFrom))
            {
                return;
            }

            if (typeFrom.GetTypeInfo().IsValueType &&
                typeTo.IsNullableType() &&
                typeTo.GetNonNullableType().Equals(typeFrom))
            {
                // VT -> vt?, no conversion necessary
                return;
            }

            if (typeTo.GetTypeInfo().IsValueType &&
                typeFrom.IsNullableType() &&
                typeFrom.GetNonNullableType().Equals(typeTo))
            {
                // VT? -> vt, call get_Value
                _instructions.Emit(NullableMethodCallInstruction.CreateGetValue());
                return;
            }

            Type nonNullableFrom = typeFrom.GetNonNullableType();
            Type nonNullableTo = typeTo.GetNonNullableType();

            // use numeric conversions for both numeric types and enums
            if ((nonNullableFrom.IsNumericOrBool() || nonNullableFrom.GetTypeInfo().IsEnum)
                 && (nonNullableTo.IsNumeric() || nonNullableTo.GetTypeInfo().IsEnum || nonNullableTo == typeof(decimal)))
            {
                Type enumTypeTo = null;

                if (nonNullableFrom.GetTypeInfo().IsEnum)
                {
                    nonNullableFrom = Enum.GetUnderlyingType(nonNullableFrom);
                }
                if (nonNullableTo.GetTypeInfo().IsEnum)
                {
                    enumTypeTo = nonNullableTo;
                    nonNullableTo = Enum.GetUnderlyingType(nonNullableTo);
                }

                TypeCode from = nonNullableFrom.GetTypeCode();
                TypeCode to = nonNullableTo.GetTypeCode();

                if (isChecked)
                {
                    _instructions.EmitNumericConvertChecked(from, to, isLiftedToNull);
                }
                else
                {
                    _instructions.EmitNumericConvertUnchecked(from, to, isLiftedToNull);
                }

                if ((object)enumTypeTo != null)
                {
                    // Convert from underlying to the enum
                    _instructions.EmitCastToEnum(enumTypeTo);
                }

                if (typeTo.IsNullableType())
                {
                    BranchLabel whenNull = _instructions.MakeLabel();
                    _instructions.EmitDup();
                    _instructions.EmitLoad(null, typeof(object));
                    _instructions.EmitEqual(typeof(object));
                    _instructions.EmitBranchTrue(whenNull);

                    // get constructor for nullable type
                    ConstructorInfo constructor = typeTo.GetConstructor(new[] { typeTo.GetNonNullableType() });
                    _instructions.EmitNew(constructor);

                    _instructions.MarkLabel(whenNull);
                }

                return;
            }

            if (typeTo.GetTypeInfo().IsEnum)
            {
                _instructions.Emit(NullCheckInstruction.Instance);
                _instructions.EmitCastReferenceToEnum(typeTo);
                return;
            }

            if (typeTo == typeof(object) || typeTo.IsAssignableFrom(typeFrom))
            {
                // Conversions to a super-class or implemented interfaces are no-op.
                return;
            }

            // A conversion to a non-implemented interface or an unrelated class, etc. should fail.
            _instructions.EmitCast(typeTo);
        }

        private void CompileNotExpression(UnaryExpression node)
        {
            Compile(node.Operand);
            _instructions.EmitNot(node.Operand.Type);
        }

        private void CompileUnaryExpression(Expression expr)
        {
            var node = (UnaryExpression)expr;

            if (node.Method != null)
            {
                EmitUnaryMethodCall(node);
            }
            else
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Not:
                        CompileNotExpression(node);
                        break;
                    case ExpressionType.TypeAs:
                        CompileTypeAsExpression(node);
                        break;
                    case ExpressionType.ArrayLength:
                        Compile(node.Operand);
                        _instructions.EmitArrayLength();
                        break;
                    case ExpressionType.NegateChecked:
                        Compile(node.Operand);
                        _instructions.EmitNegateChecked(node.Type);
                        break;
                    case ExpressionType.Negate:
                        Compile(node.Operand);
                        _instructions.EmitNegate(node.Type);
                        break;
                    case ExpressionType.Increment:
                        Compile(node.Operand);
                        _instructions.EmitIncrement(node.Type);
                        break;
                    case ExpressionType.Decrement:
                        Compile(node.Operand);
                        _instructions.EmitDecrement(node.Type);
                        break;
                    case ExpressionType.UnaryPlus:
                        Compile(node.Operand);
                        break;
                    case ExpressionType.IsTrue:
                    case ExpressionType.IsFalse:
                        EmitUnaryBoolCheck(node);
                        break;
                    case ExpressionType.OnesComplement:
                        Compile(node.Operand);
                        _instructions.EmitOnesComplement(node.Type);
                        break;
                    default:
                        throw new PlatformNotSupportedException(SR.Format(SR.UnsupportedExpressionType, node.NodeType));
                }
            }
        }

        private void EmitUnaryMethodCall(UnaryExpression node)
        {
            Compile(node.Operand);
            if (node.IsLifted)
            {
                BranchLabel notNull = _instructions.MakeLabel();
                BranchLabel computed = _instructions.MakeLabel();

                _instructions.EmitCoalescingBranch(notNull);
                _instructions.EmitBranch(computed);

                _instructions.MarkLabel(notNull);
                _instructions.EmitCall(node.Method);

                _instructions.MarkLabel(computed);
            }
            else
            {
                _instructions.EmitCall(node.Method);
            }
        }

        private void EmitUnaryBoolCheck(UnaryExpression node)
        {
            Compile(node.Operand);
            if (node.IsLifted)
            {
                BranchLabel notNull = _instructions.MakeLabel();
                BranchLabel computed = _instructions.MakeLabel();

                _instructions.EmitCoalescingBranch(notNull);
                _instructions.EmitBranch(computed);

                _instructions.MarkLabel(notNull);
                _instructions.EmitLoad(node.NodeType == ExpressionType.IsTrue);
                _instructions.EmitEqual(typeof(bool));

                _instructions.MarkLabel(computed);
            }
            else
            {
                _instructions.EmitLoad(node.NodeType == ExpressionType.IsTrue);
                _instructions.EmitEqual(typeof(bool));
            }
        }

        private void CompileAndAlsoBinaryExpression(Expression expr)
        {
            CompileLogicalBinaryExpression((BinaryExpression)expr, andAlso: true);
        }

        private void CompileOrElseBinaryExpression(Expression expr)
        {
            CompileLogicalBinaryExpression((BinaryExpression)expr, andAlso: false);
        }

        private void CompileLogicalBinaryExpression(BinaryExpression b, bool andAlso)
        {
            if (b.Method != null && !b.IsLiftedLogical)
            {
                CompileMethodLogicalBinaryExpression(b, andAlso);
            }
            else if (b.Left.Type == typeof(bool?))
            {
                CompileLiftedLogicalBinaryExpression(b, andAlso);
            }
            else if (b.IsLiftedLogical)
            {
                Compile(b.ReduceUserdefinedLifted());
            }
            else
            {
                CompileUnliftedLogicalBinaryExpression(b, andAlso);
            }
        }

        private void CompileMethodLogicalBinaryExpression(BinaryExpression expr, bool andAlso)
        {
            BranchLabel labEnd = _instructions.MakeLabel();
            Compile(expr.Left);
            _instructions.EmitDup();

            MethodInfo opTrue = TypeUtils.GetBooleanOperator(expr.Method.DeclaringType, andAlso ? "op_False" : "op_True");
            Debug.Assert(opTrue != null, "factory should check that the method exists");
            _instructions.EmitCall(opTrue);
            _instructions.EmitBranchTrue(labEnd);

            Compile(expr.Right);

            Debug.Assert(expr.Method.IsStatic);
            _instructions.EmitCall(expr.Method);

            _instructions.MarkLabel(labEnd);
        }

        private void CompileLiftedLogicalBinaryExpression(BinaryExpression node, bool andAlso)
        {
            BranchLabel computeRight = _instructions.MakeLabel();
            BranchLabel returnFalse = _instructions.MakeLabel();
            BranchLabel returnNull = _instructions.MakeLabel();
            BranchLabel returnValue = _instructions.MakeLabel();
            LocalDefinition result = _locals.DefineLocal(Expression.Parameter(node.Left.Type), _instructions.Count);
            LocalDefinition leftTemp = _locals.DefineLocal(Expression.Parameter(node.Left.Type), _instructions.Count);

            Compile(node.Left);
            _instructions.EmitStoreLocal(leftTemp.Index);

            _instructions.EmitLoadLocal(leftTemp.Index);
            _instructions.EmitLoad(null, typeof(object));
            _instructions.EmitEqual(typeof(object));

            _instructions.EmitBranchTrue(computeRight);

            _instructions.EmitLoadLocal(leftTemp.Index);

            if (andAlso)
            {
                _instructions.EmitBranchFalse(returnFalse);
            }
            else
            {
                _instructions.EmitBranchTrue(returnFalse);
            }

            // compute right
            _instructions.MarkLabel(computeRight);
            LocalDefinition rightTemp = _locals.DefineLocal(Expression.Parameter(node.Right.Type), _instructions.Count);
            Compile(node.Right);
            _instructions.EmitStoreLocal(rightTemp.Index);

            _instructions.EmitLoadLocal(rightTemp.Index);
            _instructions.EmitLoad(null, typeof(object));
            _instructions.EmitEqual(typeof(object));
            _instructions.EmitBranchTrue(returnNull);

            _instructions.EmitLoadLocal(rightTemp.Index);
            if (andAlso)
            {
                _instructions.EmitBranchFalse(returnFalse);
            }
            else
            {
                _instructions.EmitBranchTrue(returnFalse);
            }

            // check left for null again
            _instructions.EmitLoadLocal(leftTemp.Index);
            _instructions.EmitLoad(null, typeof(object));
            _instructions.EmitEqual(typeof(object));
            _instructions.EmitBranchTrue(returnNull);

            // return true
            _instructions.EmitLoad(andAlso ? ScriptingRuntimeHelpers.Boolean_True : ScriptingRuntimeHelpers.Boolean_False, typeof(object));
            _instructions.EmitStoreLocal(result.Index);
            _instructions.EmitBranch(returnValue);

            // return false
            _instructions.MarkLabel(returnFalse);
            _instructions.EmitLoad(andAlso ? ScriptingRuntimeHelpers.Boolean_False : ScriptingRuntimeHelpers.Boolean_True, typeof(object));
            _instructions.EmitStoreLocal(result.Index);
            _instructions.EmitBranch(returnValue);

            // return null
            _instructions.MarkLabel(returnNull);
            _instructions.EmitLoad(null, typeof(object));
            _instructions.EmitStoreLocal(result.Index);

            _instructions.MarkLabel(returnValue);
            _instructions.EmitLoadLocal(result.Index);

            _locals.UndefineLocal(leftTemp, _instructions.Count);
            _locals.UndefineLocal(rightTemp, _instructions.Count);
            _locals.UndefineLocal(result, _instructions.Count);
        }

        private void CompileUnliftedLogicalBinaryExpression(BinaryExpression expr, bool andAlso)
        {
            BranchLabel elseLabel = _instructions.MakeLabel();
            BranchLabel endLabel = _instructions.MakeLabel();
            Compile(expr.Left);

            if (andAlso)
            {
                _instructions.EmitBranchFalse(elseLabel);
            }
            else
            {
                _instructions.EmitBranchTrue(elseLabel);
            }
            Compile(expr.Right);
            _instructions.EmitBranch(endLabel, hasResult: false, hasValue: true);
            _instructions.MarkLabel(elseLabel);
            _instructions.EmitLoad(!andAlso);
            _instructions.MarkLabel(endLabel);
        }

        private void CompileConditionalExpression(Expression expr, bool asVoid)
        {
            var node = (ConditionalExpression)expr;
            Compile(node.Test);

            if (node.IfTrue == AstUtils.Empty)
            {
                BranchLabel endOfFalse = _instructions.MakeLabel();
                _instructions.EmitBranchTrue(endOfFalse);
                Compile(node.IfFalse, asVoid);
                _instructions.MarkLabel(endOfFalse);
            }
            else
            {
                BranchLabel endOfTrue = _instructions.MakeLabel();
                _instructions.EmitBranchFalse(endOfTrue);
                Compile(node.IfTrue, asVoid);

                if (node.IfFalse != AstUtils.Empty)
                {
                    BranchLabel endOfFalse = _instructions.MakeLabel();
                    _instructions.EmitBranch(endOfFalse, false, !asVoid);
                    _instructions.MarkLabel(endOfTrue);
                    Compile(node.IfFalse, asVoid);
                    _instructions.MarkLabel(endOfFalse);
                }
                else
                {
                    _instructions.MarkLabel(endOfTrue);
                }
            }
        }

        private void CompileLoopExpression(Expression expr)
        {
            var node = (LoopExpression)expr;

            PushLabelBlock(LabelScopeKind.Statement);
            LabelInfo breakLabel = DefineLabel(node.BreakLabel);
            LabelInfo continueLabel = DefineLabel(node.ContinueLabel);

            _instructions.MarkLabel(continueLabel.GetLabel(this));

            // emit loop body:
            CompileAsVoid(node.Body);

            // emit loop branch:
            _instructions.EmitBranch(continueLabel.GetLabel(this), node.Type != typeof(void), hasValue: false);

            _instructions.MarkLabel(breakLabel.GetLabel(this));

            PopLabelBlock(LabelScopeKind.Statement);
        }

        private void CompileSwitchExpression(Expression expr)
        {
            var node = (SwitchExpression)expr;

            if (node.Cases.All(c => c.TestValues.All(t => t is ConstantExpression)))
            {
                if (node.Cases.Count == 0)
                {
                    // Emit the switch value in case it has side-effects, but as void
                    // since the value is ignored.
                    CompileAsVoid(node.SwitchValue);

                    // Now if there is a default body, it happens unconditionally.
                    if (node.DefaultBody != null)
                    {
                        Compile(node.DefaultBody);
                    }
                    else
                    {
                        // If there are no cases and no default then the type must be void.
                        // Assert that earlier validation caught any exceptions to that.
                        Debug.Assert(node.Type == typeof(void));
                    }
                    return;
                }

                TypeCode switchType = node.SwitchValue.Type.GetTypeCode();

                if (node.Comparison == null)
                {
                    switch (switchType)
                    {
                        case TypeCode.Int32:
                            CompileIntSwitchExpression<int>(node);
                            return;

                        // the following cases are uncommon,
                        // so to avoid numerous unnecessary generic
                        // instantiations of Dictionary<K, V> and related types
                        // in AOT scenarios, we will just use "object" as the key
                        // NOTE: this does not actually result in any
                        //       extra boxing since both keys and values
                        //       are already boxed when we get them
                        case TypeCode.Byte:
                        case TypeCode.SByte:
                        case TypeCode.UInt16:
                        case TypeCode.Int16:
                        case TypeCode.UInt32:
                        case TypeCode.UInt64:
                        case TypeCode.Int64:
                            CompileIntSwitchExpression<object>(node);
                            return;
                    }
                }

                if (switchType == TypeCode.String)
                {
                    // If we have a comparison other than string equality, bail
                    MethodInfo equality = String_op_Equality_String_String;
                    if (equality != null && !equality.IsStatic)
                    {
                        equality = null;
                    }

                    if (object.Equals(node.Comparison, equality))
                    {
                        CompileStringSwitchExpression(node);
                        return;
                    }
                }
            }

            LocalDefinition temp = _locals.DefineLocal(Expression.Parameter(node.SwitchValue.Type), _instructions.Count);
            Compile(node.SwitchValue);
            _instructions.EmitStoreLocal(temp.Index);

            LabelTarget doneLabel = Expression.Label(node.Type, "done");

            foreach (SwitchCase @case in node.Cases)
            {
                foreach (Expression val in @case.TestValues)
                {
                    //  temp == val ?
                    //          goto(Body) doneLabel:
                    //          {};
                    CompileConditionalExpression(
                        Expression.Condition(
                            Expression.Equal(temp.Parameter, val, false, node.Comparison),
                            Expression.Goto(doneLabel, @case.Body),
                            AstUtils.Empty
                        ),
                        asVoid: true);
                }
            }

            // doneLabel(DefaultBody):
            CompileLabelExpression(Expression.Label(doneLabel, node.DefaultBody));

            _locals.UndefineLocal(temp, _instructions.Count);
        }

        private void CompileIntSwitchExpression<T>(SwitchExpression node)
        {
            LabelInfo end = DefineLabel(node: null);
            bool hasValue = node.Type != typeof(void);

            Compile(node.SwitchValue);
            var caseDict = new Dictionary<T, int>();
            int switchIndex = _instructions.Count;
            _instructions.EmitIntSwitch(caseDict);

            if (node.DefaultBody != null)
            {
                Compile(node.DefaultBody, !hasValue);
            }
            else
            {
                Debug.Assert(!hasValue);
            }
            _instructions.EmitBranch(end.GetLabel(this), false, hasValue);

            for (int i = 0; i < node.Cases.Count; i++)
            {
                SwitchCase switchCase = node.Cases[i];

                int caseOffset = _instructions.Count - switchIndex;
                foreach (ConstantExpression testValue in switchCase.TestValues)
                {
                    var key = (T)testValue.Value;
                    if (!caseDict.ContainsKey(key))
                    {
                        caseDict.Add(key, caseOffset);
                    }
                }

                Compile(switchCase.Body, !hasValue);

                if (i < node.Cases.Count - 1)
                {
                    _instructions.EmitBranch(end.GetLabel(this), false, hasValue);
                }
            }

            _instructions.MarkLabel(end.GetLabel(this));
        }

        private void CompileStringSwitchExpression(SwitchExpression node)
        {
            LabelInfo end = DefineLabel(node: null);
            bool hasValue = node.Type != typeof(void);

            Compile(node.SwitchValue);
            var caseDict = new Dictionary<string, int>();
            int switchIndex = _instructions.Count;
            // by default same as default
            var nullCase = new StrongBox<int>(1);
            _instructions.EmitStringSwitch(caseDict, nullCase);

            if (node.DefaultBody != null)
            {
                Compile(node.DefaultBody, !hasValue);
            }
            else
            {
                Debug.Assert(!hasValue);
            }
            _instructions.EmitBranch(end.GetLabel(this), false, hasValue);

            for (int i = 0; i < node.Cases.Count; i++)
            {
                SwitchCase switchCase = node.Cases[i];

                int caseOffset = _instructions.Count - switchIndex;
                foreach (ConstantExpression testValue in switchCase.TestValues)
                {
                    var key = (string)testValue.Value;
                    if (key == null)
                    {
                        if (nullCase.Value == 1)
                        {
                            nullCase.Value = caseOffset;
                        }
                    }
                    else if (!caseDict.ContainsKey(key))
                    {
                        caseDict.Add(key, caseOffset);
                    }
                }

                Compile(switchCase.Body, !hasValue);

                if (i < node.Cases.Count - 1)
                {
                    _instructions.EmitBranch(end.GetLabel(this), false, hasValue);
                }
            }

            _instructions.MarkLabel(end.GetLabel(this));
        }

        private void CompileLabelExpression(Expression expr)
        {
            var node = (LabelExpression)expr;

            // If we're an immediate child of a block, our label will already
            // be defined. If not, we need to define our own block so this
            // label isn't exposed except to its own child expression.
            LabelInfo label = null;

            if (_labelBlock.Kind == LabelScopeKind.Block)
            {
                _labelBlock.TryGetLabelInfo(node.Target, out label);

                // We're in a block but didn't find our label, try switch
                if (label == null && _labelBlock.Parent.Kind == LabelScopeKind.Switch)
                {
                    _labelBlock.Parent.TryGetLabelInfo(node.Target, out label);
                }

                // if we're in a switch or block, we should've found the label
                Debug.Assert(label != null);
            }

            if (label == null)
            {
                label = DefineLabel(node.Target);
            }

            if (node.DefaultValue != null)
            {
                if (node.Target.Type == typeof(void))
                {
                    CompileAsVoid(node.DefaultValue);
                }
                else
                {
                    Compile(node.DefaultValue);
                }
            }

            _instructions.MarkLabel(label.GetLabel(this));
        }

        private void CompileGotoExpression(Expression expr)
        {
            var node = (GotoExpression)expr;
            LabelInfo labelInfo = ReferenceLabel(node.Target);

            if (node.Value != null)
            {
                Compile(node.Value);
            }

            _instructions.EmitGoto(labelInfo.GetLabel(this),
                node.Type != typeof(void),
                node.Value != null && node.Value.Type != typeof(void),
                node.Target.Type != typeof(void));
        }

        private void PushLabelBlock(LabelScopeKind type)
        {
            _labelBlock = new LabelScopeInfo(_labelBlock, type);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "kind")]
        private void PopLabelBlock(LabelScopeKind kind)
        {
            Debug.Assert(_labelBlock != null && _labelBlock.Kind == kind);
            _labelBlock = _labelBlock.Parent;
        }

        private LabelInfo EnsureLabel(LabelTarget node)
        {
            LabelInfo result;
            if (!_treeLabels.TryGetValue(node, out result))
            {
                _treeLabels[node] = result = new LabelInfo(node);
            }
            return result;
        }

        private LabelInfo ReferenceLabel(LabelTarget node)
        {
            LabelInfo result = EnsureLabel(node);
            result.Reference(_labelBlock);
            return result;
        }

        private LabelInfo DefineLabel(LabelTarget node)
        {
            if (node == null)
            {
                return new LabelInfo(null);
            }
            LabelInfo result = EnsureLabel(node);
            result.Define(_labelBlock);
            return result;
        }

        private bool TryPushLabelBlock(Expression node)
        {
            // Anything that is "statement-like" -- e.g. has no associated
            // stack state can be jumped into, with the exception of try-blocks
            // We indicate this by a "Block"
            //
            // Otherwise, we push an "Expression" to indicate that it can't be
            // jumped into
            switch (node.NodeType)
            {
                default:
                    if (_labelBlock.Kind != LabelScopeKind.Expression)
                    {
                        PushLabelBlock(LabelScopeKind.Expression);
                        return true;
                    }
                    return false;
                case ExpressionType.Label:
                    // LabelExpression is a bit special, if it's directly in a
                    // block it becomes associate with the block's scope. Same
                    // thing if it's in a switch case body.
                    if (_labelBlock.Kind == LabelScopeKind.Block)
                    {
                        LabelTarget label = ((LabelExpression)node).Target;
                        if (_labelBlock.ContainsTarget(label))
                        {
                            return false;
                        }
                        if (_labelBlock.Parent.Kind == LabelScopeKind.Switch &&
                            _labelBlock.Parent.ContainsTarget(label))
                        {
                            return false;
                        }
                    }
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;
                case ExpressionType.Block:
                    PushLabelBlock(LabelScopeKind.Block);
                    // Labels defined immediately in the block are valid for
                    // the whole block.
                    if (_labelBlock.Parent.Kind != LabelScopeKind.Switch)
                    {
                        DefineBlockLabels(node);
                    }
                    return true;
                case ExpressionType.Switch:
                    PushLabelBlock(LabelScopeKind.Switch);
                    // Define labels inside of the switch cases so they are in
                    // scope for the whole switch. This allows "goto case" and
                    // "goto default" to be considered as local jumps.
                    var @switch = (SwitchExpression)node;
                    foreach (SwitchCase c in @switch.Cases)
                    {
                        DefineBlockLabels(c.Body);
                    }
                    DefineBlockLabels(@switch.DefaultBody);
                    return true;

                // Remove this when Convert(Void) goes away.
                case ExpressionType.Convert:
                    if (node.Type != typeof(void))
                    {
                        // treat it as an expression
                        goto default;
                    }
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;

                case ExpressionType.Conditional:
                case ExpressionType.Loop:
                case ExpressionType.Goto:
                    PushLabelBlock(LabelScopeKind.Statement);
                    return true;
            }
        }

        private void DefineBlockLabels(Expression node)
        {
            var block = node as BlockExpression;
            if (block == null)
            {
                return;
            }

            for (int i = 0, n = block.Expressions.Count; i < n; i++)
            {
                Expression e = block.Expressions[i];

                var label = e as LabelExpression;
                if (label != null)
                {
                    DefineLabel(label.Target);
                }
            }
        }

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

        private void CompileThrowUnaryExpression(Expression expr, bool asVoid)
        {
            var node = (UnaryExpression)expr;

            if (node.Operand == null)
            {
                CheckRethrow();

                CompileParameterExpression(_exceptionForRethrowStack.Peek());
                if (asVoid)
                {
                    _instructions.EmitRethrowVoid();
                }
                else
                {
                    _instructions.EmitRethrow();
                }
            }
            else
            {
                Compile(node.Operand);
                if (asVoid)
                {
                    _instructions.EmitThrowVoid();
                }
                else
                {
                    _instructions.EmitThrow();
                }
            }
        }

        private void CompileTryExpression(Expression expr)
        {
            var node = (TryExpression)expr;
            if (node.Fault != null)
            {
                CompileTryFaultExpression(node);
            }
            else
            {

                BranchLabel end = _instructions.MakeLabel();
                BranchLabel gotoEnd = _instructions.MakeLabel();
                int tryStart = _instructions.Count;

                BranchLabel startOfFinally = null;
                if (node.Finally != null)
                {
                    startOfFinally = _instructions.MakeLabel();
                    _instructions.EmitEnterTryFinally(startOfFinally);
                }
                else
                {
                    _instructions.EmitEnterTryCatch();
                }

                List<ExceptionHandler> exHandlers = null;
                var enterTryInstr = _instructions.GetInstruction(tryStart) as EnterTryCatchFinallyInstruction;
                Debug.Assert(enterTryInstr != null);

                PushLabelBlock(LabelScopeKind.Try);
                bool hasValue = node.Type != typeof(void);

                Compile(node.Body, !hasValue);

                int tryEnd = _instructions.Count;

                // handlers jump here:
                _instructions.MarkLabel(gotoEnd);
                _instructions.EmitGoto(end, hasValue, hasValue, hasValue);

                // keep the result on the stack:
                if (node.Handlers.Count > 0)
                {
                    exHandlers = new List<ExceptionHandler>();
                    foreach (CatchBlock handler in node.Handlers)
                    {
                        ParameterExpression parameter = handler.Variable ?? Expression.Parameter(handler.Test);

                        LocalDefinition local = _locals.DefineLocal(parameter, _instructions.Count);
                        _exceptionForRethrowStack.Push(parameter);

                        ExceptionFilter filter = null;

                        if (handler.Filter != null)
                        {
                            PushLabelBlock(LabelScopeKind.Filter);

                            _instructions.EmitEnterExceptionFilter();

                            // at this point the stack balance is prepared for the hidden exception variable:
                            int filterLabel = _instructions.MarkRuntimeLabel();
                            int filterStart = _instructions.Count;

                            CompileSetVariable(parameter, isVoid: true);
                            Compile(handler.Filter);

                            filter = new ExceptionFilter(filterLabel, filterStart, _instructions.Count);

                            // keep the value of the body on the stack:
                            _instructions.EmitLeaveExceptionFilter();

                            PopLabelBlock(LabelScopeKind.Filter);
                        }

                        PushLabelBlock(LabelScopeKind.Catch);

                        // add a stack balancing nop instruction (exception handling pushes the current exception):
                        if (hasValue)
                        {
                            _instructions.EmitEnterExceptionHandlerNonVoid();
                        }
                        else
                        {
                            _instructions.EmitEnterExceptionHandlerVoid();
                        }

                        // at this point the stack balance is prepared for the hidden exception variable:
                        int handlerLabel = _instructions.MarkRuntimeLabel();
                        int handlerStart = _instructions.Count;

                        CompileSetVariable(parameter, isVoid: true);
                        Compile(handler.Body, !hasValue);

                        _exceptionForRethrowStack.Pop();

                        // keep the value of the body on the stack:
                        _instructions.EmitLeaveExceptionHandler(hasValue, gotoEnd);

                        exHandlers.Add(new ExceptionHandler(handlerLabel, handlerStart, _instructions.Count, handler.Test, filter));
                        PopLabelBlock(LabelScopeKind.Catch);

                        _locals.UndefineLocal(local, _instructions.Count);
                    }
                }

                if (node.Finally != null)
                {
                    Debug.Assert(startOfFinally != null);
                    PushLabelBlock(LabelScopeKind.Finally);

                    _instructions.MarkLabel(startOfFinally);
                    _instructions.EmitEnterFinally(startOfFinally);
                    CompileAsVoid(node.Finally);
                    _instructions.EmitLeaveFinally();

                    enterTryInstr.SetTryHandler(
                        new TryCatchFinallyHandler(tryStart, tryEnd, gotoEnd.TargetIndex,
                            startOfFinally.TargetIndex, _instructions.Count,
                            exHandlers != null ? exHandlers.ToArray() : null));
                    PopLabelBlock(LabelScopeKind.Finally);
                }
                else
                {
                    Debug.Assert(exHandlers != null);
                    enterTryInstr.SetTryHandler(
                        new TryCatchFinallyHandler(tryStart, tryEnd, gotoEnd.TargetIndex, exHandlers.ToArray()));
                }

                _instructions.MarkLabel(end);

                PopLabelBlock(LabelScopeKind.Try);
            }
        }

        private void CompileTryFaultExpression(TryExpression expr)
        {
            Debug.Assert(expr.Finally == null);
            Debug.Assert(expr.Handlers.Count == 0);

            // Mark where we begin.
            int tryStart = _instructions.Count;
            BranchLabel end = _instructions.MakeLabel();
            EnterTryFaultInstruction enterTryInstr = _instructions.EmitEnterTryFault(end);
            Debug.Assert(enterTryInstr == _instructions.GetInstruction(tryStart));

            // Emit the try block.
            PushLabelBlock(LabelScopeKind.Try);
            bool hasValue = expr.Type != typeof(void);
            Compile(expr.Body, !hasValue);
            int tryEnd = _instructions.Count;

            // Jump out of the try block to the end of the finally. If we got
            // This far, then the fault block shouldn't be run.
            _instructions.EmitGoto(end, hasValue, hasValue, hasValue);

            // Emit the fault block. The scope kind used is the same as for finally
            // blocks, which matches the Compiler.LambdaCompiler.EmitTryExpression approach.
            PushLabelBlock(LabelScopeKind.Finally);
            BranchLabel startOfFault = _instructions.MakeLabel();
            _instructions.MarkLabel(startOfFault);
            _instructions.EmitEnterFault(startOfFault);
            CompileAsVoid(expr.Fault);
            _instructions.EmitLeaveFault();
            enterTryInstr.SetTryHandler(new TryFaultHandler(tryStart, tryEnd, startOfFault.TargetIndex, _instructions.Count));
            PopLabelBlock(LabelScopeKind.Finally);
            PopLabelBlock(LabelScopeKind.Try);
            _instructions.MarkLabel(end);
        }

        private void CompileMethodCallExpression(Expression expr)
        {
            var node = (MethodCallExpression)expr;
            CompileMethodCallExpression(node.Object, node.Method, node);
        }

        private void CompileMethodCallExpression(Expression @object, MethodInfo method, IArgumentProvider arguments)
        {
            ParameterInfo[] parameters = method.GetParameters();

            // TODO: Support pass by reference.
            List<ByRefUpdater> updaters = null;
            if (!method.IsStatic)
            {
                ByRefUpdater updater = CompileAddress(@object, -1);
                if (updater != null)
                {
                    updaters = new List<ByRefUpdater>() { updater };
                }
            }

            Debug.Assert(parameters.Length == arguments.ArgumentCount);

            for (int i = 0, n = arguments.ArgumentCount; i < n; i++)
            {
                Expression arg = arguments.GetArgument(i);

                // byref calls leave out values on the stack, we use a callback
                // to emit the code which processes each value left on the stack.
                if (parameters[i].ParameterType.IsByRef)
                {
                    ByRefUpdater updater = CompileAddress(arg, i);
                    if (updater != null)
                    {
                        if (updaters == null)
                        {
                            updaters = new List<ByRefUpdater>();
                        }

                        updaters.Add(updater);
                    }
                }
                else
                {
                    Compile(arg);
                }
            }

            if (!method.IsStatic &&
                @object.Type.IsNullableType())
            {
                // reflection doesn't let us call methods on Nullable<T> when the value
                // is null...  so we get to special case those methods!
                _instructions.EmitNullableCall(method, parameters);
            }
            else
            {
                if (updaters == null)
                {
                    _instructions.EmitCall(method, parameters);
                }
                else
                {
                    _instructions.EmitByRefCall(method, parameters, updaters.ToArray());

                    foreach (ByRefUpdater updater in updaters)
                    {
                        updater.UndefineTemps(_instructions, _locals);
                    }
                }
            }
        }

        private ByRefUpdater CompileArrayIndexAddress(Expression array, Expression index, int argumentIndex)
        {
            LocalDefinition left = _locals.DefineLocal(Expression.Parameter(array.Type, nameof(array)), _instructions.Count);
            LocalDefinition right = _locals.DefineLocal(Expression.Parameter(index.Type, nameof(index)), _instructions.Count);
            Compile(array);
            _instructions.EmitStoreLocal(left.Index);
            Compile(index);
            _instructions.EmitStoreLocal(right.Index);

            _instructions.EmitLoadLocal(left.Index);
            _instructions.EmitLoadLocal(right.Index);
            _instructions.EmitGetArrayItem();

            return new ArrayByRefUpdater(left, right, argumentIndex);
        }

        private void EmitThisForMethodCall(Expression node)
        {
            CompileAddress(node, -1);
        }

        private static bool ShouldWritebackNode(Expression node)
        {
            if (node.Type.GetTypeInfo().IsValueType)
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Parameter:
                    case ExpressionType.Call:
                    case ExpressionType.ArrayIndex:
                        return true;
                    case ExpressionType.Index:
                        return ((IndexExpression)node).Object.Type.IsArray;
                    case ExpressionType.MemberAccess:
                        return ((MemberExpression)node).Member is FieldInfo;
                    // ExpressionType.Unbox does have the behaviour write-back is used to simulate, but
                    // it doesn't need explicit write-back to produce it, so include it in the default
                    // false cases.
                }
            }
            return false;
        }

        /// <summary>
        /// Emits the address of the specified node.
        /// </summary>
        private ByRefUpdater CompileAddress(Expression node, int index)
        {
            if (index != -1 || ShouldWritebackNode(node))
            {
                switch (node.NodeType)
                {
                    case ExpressionType.Parameter:
                        LoadLocalNoValueTypeCopy((ParameterExpression)node);

                        return new ParameterByRefUpdater(ResolveLocal((ParameterExpression)node), index);
                    case ExpressionType.ArrayIndex:
                        var array = (BinaryExpression)node;

                        return CompileArrayIndexAddress(array.Left, array.Right, index);
                    case ExpressionType.Index:
                        var indexNode = (IndexExpression)node;
                        if (/*!TypeUtils.AreEquivalent(type, node.Type) || */indexNode.Indexer != null)
                        {
                            LocalDefinition? objTmp = null;
                            if (indexNode.Object != null)
                            {
                                objTmp = _locals.DefineLocal(Expression.Parameter(indexNode.Object.Type), _instructions.Count);
                                EmitThisForMethodCall(indexNode.Object);
                                _instructions.EmitDup();
                                _instructions.EmitStoreLocal(objTmp.Value.Index);
                            }

                            int count = indexNode.ArgumentCount;
                            var indexLocals = new LocalDefinition[count];
                            for (int i = 0; i < count; i++)
                            {
                                Expression arg = indexNode.GetArgument(i);
                                Compile(arg);

                                LocalDefinition argTmp = _locals.DefineLocal(Expression.Parameter(arg.Type), _instructions.Count);
                                _instructions.EmitDup();
                                _instructions.EmitStoreLocal(argTmp.Index);

                                indexLocals[i] = argTmp;
                            }

                            EmitIndexGet(indexNode);

                            return new IndexMethodByRefUpdater(objTmp, indexLocals, indexNode.Indexer.GetSetMethod(), index);
                        }
                        else if (indexNode.ArgumentCount == 1)
                        {
                            return CompileArrayIndexAddress(indexNode.Object, indexNode.GetArgument(0), index);
                        }
                        else
                        {
                            return CompileMultiDimArrayAccess(indexNode.Object, indexNode, index);
                        }
                    case ExpressionType.MemberAccess:
                        var member = (MemberExpression)node;

                        LocalDefinition? memberTemp = null;
                        if (member.Expression != null)
                        {
                            memberTemp = _locals.DefineLocal(Expression.Parameter(member.Expression.Type, "member"), _instructions.Count);
                            EmitThisForMethodCall(member.Expression);
                            _instructions.EmitDup();
                            _instructions.EmitStoreLocal(memberTemp.Value.Index);
                        }

                        var field = member.Member as FieldInfo;
                        if (field != null)
                        {
                            _instructions.EmitLoadField(field);
                            if (!field.IsLiteral && !field.IsInitOnly)
                            {
                                return new FieldByRefUpdater(memberTemp, field, index);
                            }
                            return null;
                        }
                        Debug.Assert(member.Member is PropertyInfo);
                        var property = (PropertyInfo)member.Member;
                        _instructions.EmitCall(property.GetGetMethod(nonPublic: true));
                        if (property.CanWrite)
                        {
                            return new PropertyByRefUpdater(memberTemp, property, index);
                        }
                        return null;
                    case ExpressionType.Call:
                        // An array index of a multi-dimensional array is represented by a call to Array.Get,
                        // rather than having its own array-access node. This means that when we are trying to
                        // get the address of a member of a multi-dimensional array, we'll be trying to
                        // get the address of a Get method, and it will fail to do so. Instead, detect
                        // this situation and replace it with a call to the Address method.
                        var call = (MethodCallExpression)node;
                        if (!call.Method.IsStatic &&
                            call.Object.Type.IsArray &&
                            call.Method == call.Object.Type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance))
                        {
                            return CompileMultiDimArrayAccess(
                                call.Object,
                                call,
                                index
                            );
                        }
                        break;
                }
            }
            // Includes Unbox case as it doesn't need explicit write-back.
            Compile(node);
            return null;
        }

        private ByRefUpdater CompileMultiDimArrayAccess(Expression array, IArgumentProvider arguments, int index)
        {
            Compile(array);
            LocalDefinition objTmp = _locals.DefineLocal(Expression.Parameter(array.Type), _instructions.Count);
            _instructions.EmitDup();
            _instructions.EmitStoreLocal(objTmp.Index);

            int count = arguments.ArgumentCount;
            var indexLocals = new LocalDefinition[count];
            for (int i = 0; i < count; i++)
            {
                Expression arg = arguments.GetArgument(i);
                Compile(arg);

                LocalDefinition argTmp = _locals.DefineLocal(Expression.Parameter(arg.Type), _instructions.Count);
                _instructions.EmitDup();
                _instructions.EmitStoreLocal(argTmp.Index);

                indexLocals[i] = argTmp;
            }

            _instructions.EmitCall(array.Type.GetMethod("Get", BindingFlags.Public | BindingFlags.Instance));

            return new IndexMethodByRefUpdater(objTmp, indexLocals, array.Type.GetMethod("Set", BindingFlags.Public | BindingFlags.Instance), index);
        }

        private void CompileNewExpression(Expression expr)
        {
            var node = (NewExpression)expr;

            if (node.Constructor != null)
            {
                if (node.Constructor.DeclaringType.GetTypeInfo().IsAbstract)
                    throw Error.NonAbstractConstructorRequired();

                ParameterInfo[] parameters = node.Constructor.GetParameters();
                List<ByRefUpdater> updaters = null;

                for (int i = 0; i < parameters.Length; i++)
                {
                    Expression arg = node.GetArgument(i);

                    if (parameters[i].ParameterType.IsByRef)
                    {
                        ByRefUpdater updater = CompileAddress(arg, i);
                        if (updater != null)
                        {
                            if (updaters == null)
                            {
                                updaters = new List<ByRefUpdater>();
                            }
                            updaters.Add(updater);
                        }
                    }
                    else
                    {
                        Compile(arg);
                    }
                }

                if (updaters != null)
                {
                    _instructions.EmitByRefNew(node.Constructor, parameters, updaters.ToArray());
                }
                else
                {
                    _instructions.EmitNew(node.Constructor, parameters);
                }
            }
            else
            {
                Debug.Assert(node.Type.GetTypeInfo().IsValueType);
                _instructions.EmitDefaultValue(node.Type);
            }
        }

        private void CompileMemberExpression(Expression expr)
        {
            var node = (MemberExpression)expr;

            CompileMember(node.Expression, node.Member, forBinding: false);
        }

        private void CompileMember(Expression from, MemberInfo member, bool forBinding)
        {
            var fi = member as FieldInfo;
            if (fi != null)
            {
                if (fi.IsLiteral)
                {
                    Debug.Assert(!forBinding);
                    _instructions.EmitLoad(fi.GetValue(obj: null), fi.FieldType);
                }
                else if (fi.IsStatic)
                {
                    if (forBinding)
                    {
                        throw Error.InvalidProgram();
                    }

                    if (fi.IsInitOnly)
                    {
                        _instructions.EmitLoad(fi.GetValue(obj: null), fi.FieldType);
                    }
                    else
                    {
                        _instructions.EmitLoadField(fi);
                    }
                }
                else
                {
                    if (from != null)
                    {
                        EmitThisForMethodCall(from);
                    }

                    _instructions.EmitLoadField(fi);
                }
            }
            else
            {
                // MemberExpression can use either FieldInfo or PropertyInfo - other types derived from MemberInfo are not permitted
                var pi = (PropertyInfo)member;
                if (pi != null)
                {
                    MethodInfo method = pi.GetGetMethod(nonPublic: true);
                    if (forBinding && method.IsStatic)
                    {
                        throw Error.InvalidProgram();
                    }

                    if (from != null)
                    {
                        EmitThisForMethodCall(from);
                    }

                    if (!method.IsStatic &&
                        (from != null && from.Type.IsNullableType()))
                    {
                        // reflection doesn't let us call methods on Nullable<T> when the value
                        // is null...  so we get to special case those methods!
                        _instructions.EmitNullableCall(method, Array.Empty<ParameterInfo>());
                    }
                    else
                    {
                        _instructions.EmitCall(method);
                    }
                }
            }
        }

        private void CompileNewArrayExpression(Expression expr)
        {
            var node = (NewArrayExpression)expr;

            foreach (Expression arg in node.Expressions)
            {
                Compile(arg);
            }

            Type elementType = node.Type.GetElementType();
            int rank = node.Expressions.Count;

            if (node.NodeType == ExpressionType.NewArrayInit)
            {
                _instructions.EmitNewArrayInit(elementType, rank);
            }
            else
            {
                Debug.Assert(node.NodeType == ExpressionType.NewArrayBounds);
                if (rank == 1)
                {
                    _instructions.EmitNewArray(elementType);
                }
                else
                {
                    _instructions.EmitNewArrayBounds(elementType, rank);
                }
            }
        }

        private void CompileDebugInfoExpression(Expression expr)
        {
            var node = (DebugInfoExpression)expr;
            int start = _instructions.Count;
            var info = new DebugInfo()
            {
                Index = start,
                FileName = node.Document.FileName,
                StartLine = node.StartLine,
                EndLine = node.EndLine,
                IsClear = node.IsClear
            };
            _debugInfos.Add(info);
        }

        private void CompileRuntimeVariablesExpression(Expression expr)
        {
            // Generates IRuntimeVariables for all requested variables
            var node = (RuntimeVariablesExpression)expr;
            foreach (ParameterExpression variable in node.Variables)
            {
                EnsureAvailableForClosure(variable);
                CompileGetBoxedVariable(variable);
            }

            _instructions.EmitNewRuntimeVariables(node.Variables.Count);
        }

        private void CompileLambdaExpression(Expression expr)
        {
            var node = (LambdaExpression)expr;
            var compiler = new LightCompiler(this);
            LightDelegateCreator creator = compiler.CompileTop(node);

            if (compiler._locals.ClosureVariables != null)
            {
                foreach (ParameterExpression variable in compiler._locals.ClosureVariables.Keys)
                {
                    EnsureAvailableForClosure(variable);
                    CompileGetBoxedVariable(variable);
                }
            }
            _instructions.EmitCreateDelegate(creator);
        }

        private void CompileCoalesceBinaryExpression(Expression expr)
        {
            var node = (BinaryExpression)expr;

            bool hasConversion = node.Conversion != null;
            bool hasImplicitConversion = false;
            if (!hasConversion && node.Left.Type.IsNullableType())
            {
                // reference types don't need additional conversions (the interpreter operates on Object
                // anyway); non-nullable value types can't occur on the left side; all that's left is
                // nullable value types with implicit (numeric) conversions which are allowed by Coalesce
                // factory methods

                Type nnLeftType = node.Left.Type.GetNonNullableType();
                if (!TypeUtils.AreEquivalent(node.Type, nnLeftType))
                {
                    hasImplicitConversion = true;
                    hasConversion = true;
                }
            }

            BranchLabel leftNotNull = _instructions.MakeLabel();
            BranchLabel end = null;

            Compile(node.Left);
            _instructions.EmitCoalescingBranch(leftNotNull);
            _instructions.EmitPop();
            Compile(node.Right);

            if (hasConversion)
            {
                // skip over conversion on RHS
                end = _instructions.MakeLabel();
                _instructions.EmitBranch(end);
            }

            _instructions.MarkLabel(leftNotNull);

            if (node.Conversion != null)
            {
                ParameterExpression temp = Expression.Parameter(node.Left.Type, "temp");
                LocalDefinition local = _locals.DefineLocal(temp, _instructions.Count);
                _instructions.EmitStoreLocal(local.Index);

                CompileMethodCallExpression(
                    Expression.Call(node.Conversion, node.Conversion.Type.GetMethod("Invoke"), new[] { temp })
                );

                _locals.UndefineLocal(local, _instructions.Count);
            }
            else if (hasImplicitConversion)
            {
                Type nnLeftType = node.Left.Type.GetNonNullableType();
                CompileConvertToType(nnLeftType, node.Type, isChecked: true, isLiftedToNull: false);
            }

            if (hasConversion)
            {
                _instructions.MarkLabel(end);
            }
        }

        private void CompileInvocationExpression(Expression expr)
        {
            var node = (InvocationExpression)expr;

            if (typeof(LambdaExpression).IsAssignableFrom(node.Expression.Type))
            {
                MethodInfo compMethod = node.Expression.Type.GetMethod("Compile", Array.Empty<Type>());
                CompileMethodCallExpression(
                    Expression.Call(
                        node.Expression,
                        compMethod
                    ),
                    compMethod.ReturnType.GetMethod("Invoke"),
                    node
                );
            }
            else
            {
                CompileMethodCallExpression(
                    node.Expression, node.Expression.Type.GetMethod("Invoke"), node
                );
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
        private void CompileListInitExpression(Expression expr)
        {
            var node = (ListInitExpression)expr;
            EmitThisForMethodCall(node.NewExpression);
            ReadOnlyCollection<ElementInit> initializers = node.Initializers;
            CompileListInit(initializers);
        }

        private void CompileListInit(ReadOnlyCollection<ElementInit> initializers)
        {
            for (int i = 0; i < initializers.Count; i++)
            {
                ElementInit initializer = initializers[i];
                _instructions.EmitDup();
                foreach (Expression arg in initializer.Arguments)
                {
                    Compile(arg);
                }
                MethodInfo add = initializer.AddMethod;
                _instructions.EmitCall(add);
                if (add.ReturnType != typeof(void))
                    _instructions.EmitPop();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
        private void CompileMemberInitExpression(Expression expr)
        {
            var node = (MemberInitExpression)expr;
            EmitThisForMethodCall(node.NewExpression);
            CompileMemberInit(node.Bindings);
        }

        private void CompileMemberInit(ReadOnlyCollection<MemberBinding> bindings)
        {
            foreach (MemberBinding binding in bindings)
            {
                switch (binding.BindingType)
                {
                    case MemberBindingType.Assignment:
                        _instructions.EmitDup();
                        CompileMemberAssignment(
                            true,
                            ((MemberAssignment)binding).Member,
                            ((MemberAssignment)binding).Expression,
                            forBinding: true
                        );
                        break;
                    case MemberBindingType.ListBinding:
                        var memberList = (MemberListBinding)binding;
                        _instructions.EmitDup();
                        CompileMember(null, memberList.Member, forBinding: true);
                        CompileListInit(memberList.Initializers);
                        _instructions.EmitPop();
                        break;
                    case MemberBindingType.MemberBinding:
                        var memberMember = (MemberMemberBinding)binding;
                        _instructions.EmitDup();
                        Type type = GetMemberType(memberMember.Member);
                        if (memberMember.Member is PropertyInfo && type.GetTypeInfo().IsValueType)
                        {
                            throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(memberMember.Bindings);
                        }

                        CompileMember(null, memberMember.Member, forBinding: true);
                        CompileMemberInit(memberMember.Bindings);
                        _instructions.EmitPop();
                        break;
                }
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            var fi = member as FieldInfo;
            if (fi != null) return fi.FieldType;
            var pi = member as PropertyInfo;
            if (pi != null) return pi.PropertyType;
            throw new InvalidOperationException("MemberNotFieldOrProperty");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "expr")]
        private void CompileQuoteUnaryExpression(Expression expr)
        {
            var unary = (UnaryExpression)expr;

            var visitor = new QuoteVisitor();
            visitor.Visit(unary.Operand);

            var mapping = new Dictionary<ParameterExpression, LocalVariable>();

            foreach (ParameterExpression local in visitor._hoistedParameters)
            {
                EnsureAvailableForClosure(local);
                mapping[local] = ResolveLocal(local);
            }

            _instructions.Emit(new QuoteInstruction(unary.Operand, mapping.Count > 0 ? mapping : null));
        }

        private sealed class QuoteVisitor : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, int> _definedParameters = new Dictionary<ParameterExpression, int>();
            public readonly HashSet<ParameterExpression> _hoistedParameters = new HashSet<ParameterExpression>();

            protected internal override Expression VisitParameter(ParameterExpression node)
            {
                if (!_definedParameters.ContainsKey(node))
                {
                    _hoistedParameters.Add(node);
                }
                return node;
            }

            protected internal override Expression VisitBlock(BlockExpression node)
            {
                PushParameters(node.Variables);

                base.VisitBlock(node);

                PopParameters(node.Variables);

                return node;
            }

            protected override CatchBlock VisitCatchBlock(CatchBlock node)
            {
                if (node.Variable != null)
                {
                    PushParameters(new[] { node.Variable });
                }
                Visit(node.Body);
                Visit(node.Filter);
                if (node.Variable != null)
                {
                    PopParameters(new[] { node.Variable });
                }
                return node;
            }

            protected internal override Expression VisitLambda<T>(Expression<T> node)
            {
                PushParameters(node.Parameters);

                base.VisitLambda(node);

                PopParameters(node.Parameters);

                return node;
            }

            private void PushParameters(ICollection<ParameterExpression> parameters)
            {
                foreach (ParameterExpression param in parameters)
                {
                    int count;
                    if (_definedParameters.TryGetValue(param, out count))
                    {
                        _definedParameters[param] = count + 1;
                    }
                    else
                    {
                        _definedParameters[param] = 1;
                    }
                }
            }

            private void PopParameters(ICollection<ParameterExpression> parameters)
            {
                foreach (ParameterExpression param in parameters)
                {
                    int count = _definedParameters[param];
                    if (count == 0)
                    {
                        _definedParameters.Remove(param);
                    }
                    else
                    {
                        _definedParameters[param] = count - 1;
                    }
                }
            }
        }

        private void CompileUnboxUnaryExpression(Expression expr)
        {
            var node = (UnaryExpression)expr;

            Compile(node.Operand);

            if (node.Type.GetTypeInfo().IsValueType && !node.Type.IsNullableType())
            {
                _instructions.Emit(NullCheckInstruction.Instance);
            }
        }

        private void CompileTypeEqualExpression(Expression expr)
        {
            Debug.Assert(expr.NodeType == ExpressionType.TypeEqual);
            var node = (TypeBinaryExpression)expr;

            Compile(node.Expression);
            if (node.Expression.Type == typeof(void))
            {
                _instructions.EmitLoad(node.TypeOperand == node.Expression.Type, typeof(bool));
            }
            else if (node.TypeOperand.GetTypeInfo().IsGenericType && node.TypeOperand.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                _instructions.EmitLoad(node.TypeOperand.GenericTypeArguments[0]);
                _instructions.EmitNullableTypeEquals();
            }
            else
            {
                _instructions.EmitLoad(node.TypeOperand);
                _instructions.EmitTypeEquals();
            }
        }

        private void CompileTypeAsExpression(UnaryExpression node)
        {
            Compile(node.Operand);
            _instructions.EmitTypeAs(node.Type);
        }

        private void CompileTypeIsExpression(Expression expr)
        {
            Debug.Assert(expr.NodeType == ExpressionType.TypeIs);
            var node = (TypeBinaryExpression)expr;

            AnalyzeTypeIsResult result = ConstantCheck.AnalyzeTypeIs(node);

            Compile(node.Expression);

            if (result == AnalyzeTypeIsResult.KnownTrue ||
                result == AnalyzeTypeIsResult.KnownFalse)
            {
                // Result is known statically, so just emit the expression for
                // its side effects and return the result
                if (node.Expression.Type != typeof(void))
                {
                    _instructions.EmitPop();
                }

                _instructions.EmitLoad(
                    ScriptingRuntimeHelpers.BooleanToObject(result == AnalyzeTypeIsResult.KnownTrue),
                    typeof(bool)
                );
                return;
            }

            if (node.TypeOperand.GetTypeInfo().IsGenericType && node.TypeOperand.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                _instructions.EmitLoad(node.TypeOperand.GenericTypeArguments[0]);
                _instructions.EmitNullableTypeEquals();
            }
            else
            {
                _instructions.EmitTypeIs(node.TypeOperand);
            }
        }

        private void Compile(Expression expr, bool asVoid)
        {
            if (asVoid)
            {
                CompileAsVoid(expr);
            }
            else
            {
                Compile(expr);
            }
        }

        private void CompileAsVoid(Expression expr)
        {
            bool pushLabelBlock = TryPushLabelBlock(expr);
            int startingStackDepth = _instructions.CurrentStackDepth;
            switch (expr.NodeType)
            {
                case ExpressionType.Assign:
                    CompileAssignBinaryExpression(expr, asVoid: true);
                    break;

                case ExpressionType.Block:
                    CompileBlockExpression(expr, asVoid: true);
                    break;

                case ExpressionType.Throw:
                    CompileThrowUnaryExpression(expr, asVoid: true);
                    break;

                case ExpressionType.Constant:
                case ExpressionType.Default:
                case ExpressionType.Parameter:
                    // no-op
                    break;

                default:
                    CompileNoLabelPush(expr);
                    if (expr.Type != typeof(void))
                    {
                        _instructions.EmitPop();
                    }
                    break;
            }
            Debug.Assert(_instructions.CurrentStackDepth == startingStackDepth);
            if (pushLabelBlock)
            {
                PopLabelBlock(_labelBlock.Kind);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private void CompileNoLabelPush(Expression expr)
        {
            // When compling deep trees, we run the risk of triggering a terminating StackOverflowException,
            // so we use the StackGuard utility here to probe for sufficient stack and continue the work on
            // another thread when we run out of stack space.
            if (!_guard.TryEnterOnCurrentStack())
            {
                _guard.RunOnEmptyStack((LightCompiler @this, Expression e) => @this.CompileNoLabelPush(e), this, expr);
                return;
            }

            int startingStackDepth = _instructions.CurrentStackDepth;
            switch (expr.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.And:
                case ExpressionType.ArrayIndex:
                case ExpressionType.Divide:
                case ExpressionType.Equal:
                case ExpressionType.ExclusiveOr:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LeftShift:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.Modulo:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.NotEqual:
                case ExpressionType.Or:
                case ExpressionType.Power:
                case ExpressionType.RightShift:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked: CompileBinaryExpression(expr); break;
                case ExpressionType.AndAlso: CompileAndAlsoBinaryExpression(expr); break;
                case ExpressionType.OrElse: CompileOrElseBinaryExpression(expr); break;
                case ExpressionType.Coalesce: CompileCoalesceBinaryExpression(expr); break;
                case ExpressionType.ArrayLength:
                case ExpressionType.Decrement:
                case ExpressionType.Increment:
                case ExpressionType.IsTrue:
                case ExpressionType.IsFalse:
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.OnesComplement:
                case ExpressionType.TypeAs:
                case ExpressionType.UnaryPlus: CompileUnaryExpression(expr); break;
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked: CompileConvertUnaryExpression(expr); break;
                case ExpressionType.Quote: CompileQuoteUnaryExpression(expr); break;
                case ExpressionType.Throw: CompileThrowUnaryExpression(expr, expr.Type == typeof(void)); break;
                case ExpressionType.Unbox: CompileUnboxUnaryExpression(expr); break;
                case ExpressionType.Call: CompileMethodCallExpression(expr); break;
                case ExpressionType.Conditional: CompileConditionalExpression(expr, expr.Type == typeof(void)); break;
                case ExpressionType.Constant: CompileConstantExpression(expr); break;
                case ExpressionType.Invoke: CompileInvocationExpression(expr); break;
                case ExpressionType.Lambda: CompileLambdaExpression(expr); break;
                case ExpressionType.ListInit: CompileListInitExpression(expr); break;
                case ExpressionType.MemberAccess: CompileMemberExpression(expr); break;
                case ExpressionType.MemberInit: CompileMemberInitExpression(expr); break;
                case ExpressionType.New: CompileNewExpression(expr); break;
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds: CompileNewArrayExpression(expr); break;
                case ExpressionType.Parameter: CompileParameterExpression(expr); break;
                case ExpressionType.TypeIs: CompileTypeIsExpression(expr); break;
                case ExpressionType.TypeEqual: CompileTypeEqualExpression(expr); break;
                case ExpressionType.Assign: CompileAssignBinaryExpression(expr, expr.Type == typeof(void)); break;
                case ExpressionType.Block: CompileBlockExpression(expr, expr.Type == typeof(void)); break;
                case ExpressionType.DebugInfo: CompileDebugInfoExpression(expr); break;
                case ExpressionType.Default: CompileDefaultExpression(expr); break;
                case ExpressionType.Goto: CompileGotoExpression(expr); break;
                case ExpressionType.Index: CompileIndexExpression(expr); break;
                case ExpressionType.Label: CompileLabelExpression(expr); break;
                case ExpressionType.RuntimeVariables: CompileRuntimeVariablesExpression(expr); break;
                case ExpressionType.Loop: CompileLoopExpression(expr); break;
                case ExpressionType.Switch: CompileSwitchExpression(expr); break;
                case ExpressionType.Try: CompileTryExpression(expr); break;
                default:
                    Compile(expr.ReduceAndCheck());
                    break;
            }
            Debug.Assert(_instructions.CurrentStackDepth == startingStackDepth + (expr.Type == typeof(void) ? 0 : 1),
                string.Format("{0} vs {1} for {2}", _instructions.CurrentStackDepth, startingStackDepth + (expr.Type == typeof(void) ? 0 : 1), expr.NodeType));
        }

        private void Compile(Expression expr)
        {
            bool pushLabelBlock = TryPushLabelBlock(expr);
            CompileNoLabelPush(expr);
            if (pushLabelBlock)
            {
                PopLabelBlock(_labelBlock.Kind);
            }
        }
    }

    internal abstract class ByRefUpdater
    {
        public readonly int ArgumentIndex;

        public ByRefUpdater(int argumentIndex)
        {
            ArgumentIndex = argumentIndex;
        }

        public abstract void Update(InterpretedFrame frame, object value);

        public virtual void UndefineTemps(InstructionList instructions, LocalVariables locals)
        {
        }
    }

    internal sealed class ParameterByRefUpdater : ByRefUpdater
    {
        private readonly LocalVariable _parameter;

        public ParameterByRefUpdater(LocalVariable parameter, int argumentIndex)
            : base(argumentIndex)
        {
            _parameter = parameter;
        }

        public override void Update(InterpretedFrame frame, object value)
        {
            if (_parameter.InClosure)
            {
                IStrongBox box = frame.Closure[_parameter.Index];
                box.Value = value;
            }
            else if (_parameter.IsBoxed)
            {
                var box = (IStrongBox)frame.Data[_parameter.Index];
                box.Value = value;
            }
            else
            {
                frame.Data[_parameter.Index] = value;
            }
        }
    }

    internal sealed class ArrayByRefUpdater : ByRefUpdater
    {
        private readonly LocalDefinition _array, _index;

        public ArrayByRefUpdater(LocalDefinition array, LocalDefinition index, int argumentIndex)
            : base(argumentIndex)
        {
            _array = array;
            _index = index;
        }

        public override void Update(InterpretedFrame frame, object value)
        {
            object index = frame.Data[_index.Index];
            ((Array)frame.Data[_array.Index]).SetValue(value, (int)index);
        }

        public override void UndefineTemps(InstructionList instructions, LocalVariables locals)
        {
            locals.UndefineLocal(_array, instructions.Count);
            locals.UndefineLocal(_index, instructions.Count);
        }
    }

    internal sealed class FieldByRefUpdater : ByRefUpdater
    {
        private readonly LocalDefinition? _object;
        private readonly FieldInfo _field;

        public FieldByRefUpdater(LocalDefinition? obj, FieldInfo field, int argumentIndex)
            : base(argumentIndex)
        {
            _object = obj;
            _field = field;
        }

        public override void Update(InterpretedFrame frame, object value)
        {
            object obj = _object == null ? null : frame.Data[_object.Value.Index];
            _field.SetValue(obj, value);
        }

        public override void UndefineTemps(InstructionList instructions, LocalVariables locals)
        {
            if (_object != null)
            {
                locals.UndefineLocal(_object.Value, instructions.Count);
            }
        }
    }

    internal sealed class PropertyByRefUpdater : ByRefUpdater
    {
        private readonly LocalDefinition? _object;
        private readonly PropertyInfo _property;

        public PropertyByRefUpdater(LocalDefinition? obj, PropertyInfo property, int argumentIndex)
            : base(argumentIndex)
        {
            _object = obj;
            _property = property;
        }

        public override void Update(InterpretedFrame frame, object value)
        {
            object obj = _object == null ? null : frame.Data[_object.Value.Index];

            try
            {
                _property.SetValue(obj, value);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }
        }

        public override void UndefineTemps(InstructionList instructions, LocalVariables locals)
        {
            if (_object != null)
            {
                locals.UndefineLocal(_object.Value, instructions.Count);
            }
        }
    }

    internal sealed class IndexMethodByRefUpdater : ByRefUpdater
    {
        private readonly MethodInfo _indexer;
        private readonly LocalDefinition? _obj;
        private readonly LocalDefinition[] _args;

        public IndexMethodByRefUpdater(LocalDefinition? obj, LocalDefinition[] args, MethodInfo indexer, int argumentIndex)
            : base(argumentIndex)
        {
            _obj = obj;
            _args = args;
            _indexer = indexer;
        }

        public override void Update(InterpretedFrame frame, object value)
        {
            var args = new object[_args.Length + 1];
            for (int i = 0; i < args.Length - 1; i++)
            {
                args[i] = frame.Data[_args[i].Index];
            }
            args[args.Length - 1] = value;

            object instance = _obj == null ? null : frame.Data[_obj.Value.Index];

            try
            {
                _indexer.Invoke(instance, args);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UpdateForRethrow(e.InnerException);
                throw e.InnerException;
            }
        }

        public override void UndefineTemps(InstructionList instructions, LocalVariables locals)
        {
            if (_obj != null)
            {
                locals.UndefineLocal(_obj.Value, instructions.Count);
            }

            for (int i = 0; i < _args.Length; i++)
            {
                locals.UndefineLocal(_args[i], instructions.Count);
            }
        }
    }
}
