// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Compiler
{
    internal sealed partial class LambdaCompiler
    {
        private static readonly FieldInfo s_callSiteTargetField = typeof(CallSite<>).GetField("Target")!;

        [Flags]
        internal enum CompilationFlags
        {
            EmitExpressionStart = 0x0001,
            EmitNoExpressionStart = 0x0002,
            EmitAsDefaultType = 0x0010,
            EmitAsVoidType = 0x0020,
            EmitAsTail = 0x0100,   // at the tail position of a lambda, tail call can be safely emitted
            EmitAsMiddle = 0x0200, // in the middle of a lambda, tail call can be emitted if it is in a return
            EmitAsNoTail = 0x0400, // neither at the tail or in a return, or tail call is not turned on, no tail call is emitted

            EmitExpressionStartMask = 0x000f,
            EmitAsTypeMask = 0x00f0,
            EmitAsTailCallMask = 0x0f00
        }

        /// <summary>
        /// Update the flag with a new EmitAsTailCall flag
        /// </summary>
        private static CompilationFlags UpdateEmitAsTailCallFlag(CompilationFlags flags, CompilationFlags newValue)
        {
            Debug.Assert(newValue == CompilationFlags.EmitAsTail || newValue == CompilationFlags.EmitAsMiddle || newValue == CompilationFlags.EmitAsNoTail);
            CompilationFlags oldValue = flags & CompilationFlags.EmitAsTailCallMask;
            return flags ^ oldValue | newValue;
        }

        /// <summary>
        /// Update the flag with a new EmitExpressionStart flag
        /// </summary>
        private static CompilationFlags UpdateEmitExpressionStartFlag(CompilationFlags flags, CompilationFlags newValue)
        {
            Debug.Assert(newValue == CompilationFlags.EmitExpressionStart || newValue == CompilationFlags.EmitNoExpressionStart);
            CompilationFlags oldValue = flags & CompilationFlags.EmitExpressionStartMask;
            return flags ^ oldValue | newValue;
        }

        /// <summary>
        /// Update the flag with a new EmitAsType flag
        /// </summary>
        private static CompilationFlags UpdateEmitAsTypeFlag(CompilationFlags flags, CompilationFlags newValue)
        {
            Debug.Assert(newValue == CompilationFlags.EmitAsDefaultType || newValue == CompilationFlags.EmitAsVoidType);
            CompilationFlags oldValue = flags & CompilationFlags.EmitAsTypeMask;
            return flags ^ oldValue | newValue;
        }

        /// <summary>
        /// Generates code for this expression in a value position.
        /// This method will leave the value of the expression
        /// on the top of the stack typed as Type.
        /// </summary>
        internal void EmitExpression(Expression node)
        {
            EmitExpression(node, CompilationFlags.EmitAsNoTail | CompilationFlags.EmitExpressionStart);
        }

        /// <summary>
        /// Emits an expression and discards the result.  For some nodes this emits
        /// more optimal code then EmitExpression/Pop
        /// </summary>
        private void EmitExpressionAsVoid(Expression node)
        {
            EmitExpressionAsVoid(node, CompilationFlags.EmitAsNoTail);
        }

        private void EmitExpressionAsVoid(Expression node, CompilationFlags flags)
        {
            Debug.Assert(node != null);

            CompilationFlags startEmitted = EmitExpressionStart(node);

            switch (node.NodeType)
            {
                case ExpressionType.Assign:
                    EmitAssign((AssignBinaryExpression)node, CompilationFlags.EmitAsVoidType);
                    break;
                case ExpressionType.Block:
                    Emit((BlockExpression)node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
                    break;
                case ExpressionType.Throw:
                    EmitThrow((UnaryExpression)node, CompilationFlags.EmitAsVoidType);
                    break;
                case ExpressionType.Goto:
                    EmitGotoExpression(node, UpdateEmitAsTypeFlag(flags, CompilationFlags.EmitAsVoidType));
                    break;
                case ExpressionType.Constant:
                case ExpressionType.Default:
                case ExpressionType.Parameter:
                    // no-op
                    break;
                default:
                    if (node.Type == typeof(void))
                    {
                        EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitNoExpressionStart));
                    }
                    else
                    {
                        EmitExpression(node, CompilationFlags.EmitAsNoTail | CompilationFlags.EmitNoExpressionStart);
                        _ilg.Emit(OpCodes.Pop);
                    }
                    break;
            }
            EmitExpressionEnd(startEmitted);
        }

        private void EmitExpressionAsType(Expression node, Type type, CompilationFlags flags)
        {
            if (type == typeof(void))
            {
                EmitExpressionAsVoid(node, flags);
            }
            else
            {
                // if the node is emitted as a different type, CastClass IL is emitted at the end,
                // should not emit with tail calls.
                if (!TypeUtils.AreEquivalent(node.Type, type))
                {
                    EmitExpression(node);
                    Debug.Assert(TypeUtils.AreReferenceAssignable(type, node.Type));
                    _ilg.Emit(OpCodes.Castclass, type);
                }
                else
                {
                    // emit the node with the flags and emit expression start
                    EmitExpression(node, UpdateEmitExpressionStartFlag(flags, CompilationFlags.EmitExpressionStart));
                }
            }
        }

        #region label block tracking

        private CompilationFlags EmitExpressionStart(Expression node)
        {
            if (TryPushLabelBlock(node))
            {
                return CompilationFlags.EmitExpressionStart;
            }
            return CompilationFlags.EmitNoExpressionStart;
        }

        private void EmitExpressionEnd(CompilationFlags flags)
        {
            if ((flags & CompilationFlags.EmitExpressionStartMask) == CompilationFlags.EmitExpressionStart)
            {
                PopLabelBlock(_labelBlock.Kind);
            }
        }

        #endregion

        #region InvocationExpression

        private void EmitInvocationExpression(Expression expr, CompilationFlags flags)
        {
            InvocationExpression node = (InvocationExpression)expr;

            // Optimization: inline code for literal lambda's directly
            //
            // This is worth it because otherwise we end up with an extra call
            // to DynamicMethod.CreateDelegate, which is expensive.
            //
            if (node.LambdaOperand != null)
            {
                EmitInlinedInvoke(node, flags);
                return;
            }

            expr = node.Expression;
            if (typeof(LambdaExpression).IsAssignableFrom(expr.Type))
            {
                // if the invoke target is a lambda expression tree, first compile it into a delegate
                expr = Expression.Call(expr, LambdaExpression.GetCompileMethod(expr.Type));
            }

            EmitMethodCall(expr, expr.Type.GetInvokeMethod(), node, CompilationFlags.EmitAsNoTail | CompilationFlags.EmitExpressionStart);
        }

        private void EmitInlinedInvoke(InvocationExpression invoke, CompilationFlags flags)
        {
            LambdaExpression lambda = invoke.LambdaOperand!;

            // This is tricky: we need to emit the arguments outside of the
            // scope, but set them inside the scope. Fortunately, using the IL
            // stack it is entirely doable.

            // 1. Emit invoke arguments
            List<WriteBack>? wb = EmitArguments(lambda.Type.GetInvokeMethod(), invoke);

            // 2. Create the nested LambdaCompiler
            var inner = new LambdaCompiler(this, lambda, invoke);

            // 3. Emit the body
            // if the inlined lambda is the last expression of the whole lambda,
            // tail call can be applied.
            if (wb != null)
            {
                Debug.Assert(wb.Count > 0);
                flags = UpdateEmitAsTailCallFlag(flags, CompilationFlags.EmitAsNoTail);
            }
            inner.EmitLambdaBody(_scope, true, flags);

            // 4. Emit write-backs if needed
            EmitWriteBack(wb);
        }

        #endregion

        #region IndexExpression

        private void EmitIndexExpression(Expression expr)
        {
            var node = (IndexExpression)expr;

            // Emit instance, if calling an instance method
            Type? objectType = null;
            if (node.Object != null)
            {
                EmitInstance(node.Object, out objectType);
            }

            // Emit indexes. We don't allow byref args, so no need to worry
            // about write-backs or EmitAddress
            for (int i = 0, n = node.ArgumentCount; i < n; i++)
            {
                Expression arg = node.GetArgument(i);
                EmitExpression(arg);
            }

            EmitGetIndexCall(node, objectType);
        }

        private void EmitIndexAssignment(AssignBinaryExpression node, CompilationFlags flags)
        {
            Debug.Assert(!node.IsByRef);

            var index = (IndexExpression)node.Left;

            CompilationFlags emitAs = flags & CompilationFlags.EmitAsTypeMask;

            // Emit instance, if calling an instance method
            Type? objectType = null;
            if (index.Object != null)
            {
                EmitInstance(index.Object, out objectType);
            }

            // Emit indexes. We don't allow byref args, so no need to worry
            // about write-backs or EmitAddress
            for (int i = 0, n = index.ArgumentCount; i < n; i++)
            {
                Expression arg = index.GetArgument(i);
                EmitExpression(arg);
            }

            // Emit value
            EmitExpression(node.Right);

            // Save the expression value, if needed
            LocalBuilder? temp = null;
            if (emitAs != CompilationFlags.EmitAsVoidType)
            {
                _ilg.Emit(OpCodes.Dup);
                _ilg.Emit(OpCodes.Stloc, temp = GetLocal(node.Type));
            }

            EmitSetIndexCall(index, objectType);

            // Restore the value
            if (emitAs != CompilationFlags.EmitAsVoidType)
            {
                _ilg.Emit(OpCodes.Ldloc, temp!);
                FreeLocal(temp!);
            }
        }

        private void EmitGetIndexCall(IndexExpression node, Type? objectType)
        {
            if (node.Indexer != null)
            {
                // For indexed properties, just call the getter
                MethodInfo method = node.Indexer.GetGetMethod(nonPublic: true)!;
                EmitCall(objectType, method);
            }
            else
            {
                EmitGetArrayElement(objectType!);
            }
        }

        private void EmitGetArrayElement(Type arrayType)
        {
            if (arrayType.IsSZArray)
            {
                // For one dimensional arrays, emit load
                _ilg.EmitLoadElement(arrayType.GetElementType()!);
            }
            else
            {
                // Multidimensional arrays, call get
                _ilg.Emit(OpCodes.Call, TypeUtils.GetArrayGetMethod(arrayType));
            }
        }

        private void EmitSetIndexCall(IndexExpression node, Type? objectType)
        {
            if (node.Indexer != null)
            {
                // For indexed properties, just call the setter
                MethodInfo method = node.Indexer.GetSetMethod(nonPublic: true)!;
                EmitCall(objectType, method);
            }
            else
            {
                EmitSetArrayElement(objectType!);
            }
        }

        private void EmitSetArrayElement(Type arrayType)
        {
            if (arrayType.IsSZArray)
            {
                // For one dimensional arrays, emit store
                _ilg.EmitStoreElement(arrayType.GetElementType()!);
            }
            else
            {
                // Multidimensional arrays, call set
                _ilg.Emit(OpCodes.Call, TypeUtils.GetArraySetMethod(arrayType));
            }
        }

        #endregion

        #region MethodCallExpression

        private void EmitMethodCallExpression(Expression expr, CompilationFlags flags)
        {
            MethodCallExpression node = (MethodCallExpression)expr;

            EmitMethodCall(node.Object, node.Method, node, flags);
        }

        private void EmitMethodCallExpression(Expression expr)
        {
            EmitMethodCallExpression(expr, CompilationFlags.EmitAsNoTail);
        }

        private void EmitMethodCall(Expression obj, MethodInfo method, IArgumentProvider methodCallExpr)
        {
            EmitMethodCall(obj, method, methodCallExpr, CompilationFlags.EmitAsNoTail);
        }

        private void EmitMethodCall(Expression? obj, MethodInfo method, IArgumentProvider methodCallExpr, CompilationFlags flags)
        {
            // Emit instance, if calling an instance method
            Type? objectType = null;
            if (!method.IsStatic)
            {
                Debug.Assert(obj != null);
                EmitInstance(obj, out objectType);
            }
            // if the obj has a value type, its address is passed to the method call so we cannot destroy the
            // stack by emitting a tail call
            if (obj != null && obj.Type.IsValueType)
            {
                EmitMethodCall(method, methodCallExpr, objectType);
            }
            else
            {
                EmitMethodCall(method, methodCallExpr, objectType, flags);
            }
        }

        // assumes 'object' of non-static call is already on stack
        private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type? objectType)
        {
            EmitMethodCall(mi, args, objectType, CompilationFlags.EmitAsNoTail);
        }

        // assumes 'object' of non-static call is already on stack
        private void EmitMethodCall(MethodInfo mi, IArgumentProvider args, Type? objectType, CompilationFlags flags)
        {
            // Emit arguments
            List<WriteBack>? wb = EmitArguments(mi, args);

            // Emit the actual call
            OpCode callOp = UseVirtual(mi) ? OpCodes.Callvirt : OpCodes.Call;
            if (callOp == OpCodes.Callvirt && objectType!.IsValueType)
            {
                // This automatically boxes value types if necessary.
                _ilg.Emit(OpCodes.Constrained, objectType);
            }
            // The method call can be a tail call if
            // 1) the method call is the last instruction before Ret
            // 2) the method does not have any ByRef parameters, refer to ECMA-335 Partition III Section 2.4.
            //    "Verification requires that no managed pointers are passed to the method being called, since
            //    it does not track pointers into the current frame."
            if ((flags & CompilationFlags.EmitAsTailCallMask) == CompilationFlags.EmitAsTail && !MethodHasByRefParameter(mi))
            {
                _ilg.Emit(OpCodes.Tailcall);
            }
            if (mi.CallingConvention == CallingConventions.VarArgs)
            {
                int count = args.ArgumentCount;
                Type[] types = new Type[count];
                for (int i = 0; i < count; i++)
                {
                    types[i] = args.GetArgument(i).Type;
                }

                _ilg.EmitCall(callOp, mi, types);
            }
            else
            {
                _ilg.Emit(callOp, mi);
            }

            // Emit write-backs for properties passed as "ref" arguments
            EmitWriteBack(wb);
        }

        private static bool MethodHasByRefParameter(MethodInfo mi)
        {
            foreach (ParameterInfo pi in mi.GetParametersCached())
            {
                if (pi.IsByRefParameter())
                {
                    return true;
                }
            }
            return false;
        }

        private void EmitCall(Type? objectType, MethodInfo method)
        {
            if (method.CallingConvention == CallingConventions.VarArgs)
            {
                throw Error.UnexpectedVarArgsCall(method);
            }

            OpCode callOp = UseVirtual(method) ? OpCodes.Callvirt : OpCodes.Call;
            if (callOp == OpCodes.Callvirt && objectType!.IsValueType)
            {
                _ilg.Emit(OpCodes.Constrained, objectType);
            }
            _ilg.Emit(callOp, method);
        }

        private static bool UseVirtual(MethodInfo mi)
        {
            // There are two factors: is the method static, virtual or non-virtual instance?
            // And is the object ref or value?
            // The cases are:
            //
            // static, ref:     call
            // static, value:   call
            // virtual, ref:    callvirt
            // virtual, value:  call -- e.g. double.ToString must be a non-virtual call to be verifiable.
            // instance, ref:   callvirt -- this looks wrong, but is verifiable and gives us a free null check.
            // instance, value: call
            //
            // We never need to generate a non-virtual call to a virtual method on a reference type because
            // expression trees do not support "base.Foo()" style calling.
            //
            // We could do an optimization here for the case where we know that the object is a non-null
            // reference type and the method is a non-virtual instance method.  For example, if we had
            // (new Foo()).Bar() for instance method Bar we don't need the null check so we could do a
            // call rather than a callvirt.  However that seems like it would not be a very big win for
            // most dynamically generated code scenarios, so let's not do that for now.

            if (mi.IsStatic)
            {
                return false;
            }
            if (mi.DeclaringType!.IsValueType)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Emits arguments to a call, and returns an array of write-backs that
        /// should happen after the call.
        /// </summary>
        private List<WriteBack>? EmitArguments(MethodBase method, IArgumentProvider args)
        {
            return EmitArguments(method, args, 0);
        }

        /// <summary>
        /// Emits arguments to a call, and returns an array of write-backs that
        /// should happen after the call. For emitting dynamic expressions, we
        /// need to skip the first parameter of the method (the call site).
        /// </summary>
        private List<WriteBack>? EmitArguments(MethodBase method, IArgumentProvider args, int skipParameters)
        {
            ParameterInfo[] pis = method.GetParametersCached();
            Debug.Assert(args.ArgumentCount + skipParameters == pis.Length);

            List<WriteBack>? writeBacks = null;
            for (int i = skipParameters, n = pis.Length; i < n; i++)
            {
                ParameterInfo parameter = pis[i];
                Expression argument = args.GetArgument(i - skipParameters);
                Type type = parameter.ParameterType;

                if (type.IsByRef)
                {
                    type = type.GetElementType()!;

                    WriteBack? wb = EmitAddressWriteBack(argument, type);
                    if (wb != null)
                    {
                        if (writeBacks == null)
                        {
                            writeBacks = new List<WriteBack>();
                        }

                        writeBacks.Add(wb);
                    }
                }
                else
                {
                    EmitExpression(argument);
                }
            }
            return writeBacks;
        }

        private void EmitWriteBack(List<WriteBack>? writeBacks)
        {
            if (writeBacks != null)
            {
                foreach (WriteBack wb in writeBacks)
                {
                    wb(this);
                }
            }
        }

        #endregion

        private void EmitConstantExpression(Expression expr)
        {
            ConstantExpression node = (ConstantExpression)expr;

            EmitConstant(node.Value, node.Type);
        }

        private void EmitConstant(object value)
        {
            Debug.Assert(value != null);
            EmitConstant(value, value.GetType());
        }

        private void EmitConstant(object? value, Type type)
        {
            // Try to emit the constant directly into IL
            if (!_ilg.TryEmitConstant(value, type, this))
            {
                _boundConstants.EmitConstant(this, value!, type);
            }
        }

        private void EmitDynamicExpression(Expression expr)
        {
#if FEATURE_COMPILE_TO_METHODBUILDER
            if (!(_method is DynamicMethod))
            {
                throw Error.CannotCompileDynamic();
            }
#else
            Debug.Assert(_method is DynamicMethod);
#endif

            var node = (IDynamicExpression)expr;

            object site = node.CreateCallSite();
            Type siteType = site.GetType();

            MethodInfo invoke = node.DelegateType.GetInvokeMethod();

            // site.Target.Invoke(site, args)
            EmitConstant(site, siteType);

            // Emit the temp as type CallSite so we get more reuse
            _ilg.Emit(OpCodes.Dup);
            LocalBuilder siteTemp = GetLocal(siteType);
            _ilg.Emit(OpCodes.Stloc, siteTemp);
            _ilg.Emit(OpCodes.Ldfld, GetCallSiteTargetField(siteType));
            _ilg.Emit(OpCodes.Ldloc, siteTemp);
            FreeLocal(siteTemp);

            List<WriteBack>? wb = EmitArguments(invoke, node, 1);
            _ilg.Emit(OpCodes.Callvirt, invoke);
            EmitWriteBack(wb);
        }

        private static FieldInfo GetCallSiteTargetField(Type siteType)
        {
            Debug.Assert(siteType.IsGenericType && siteType.GetGenericTypeDefinition() == typeof(CallSite<>));
            return (FieldInfo)siteType.GetMemberWithSameMetadataDefinitionAs(s_callSiteTargetField);
        }

        private void EmitNewExpression(Expression expr)
        {
            NewExpression node = (NewExpression)expr;

            if (node.Constructor != null)
            {
                if (node.Constructor.DeclaringType!.IsAbstract)
                    throw Error.NonAbstractConstructorRequired();

                List<WriteBack>? wb = EmitArguments(node.Constructor, node);
                _ilg.Emit(OpCodes.Newobj, node.Constructor);
                EmitWriteBack(wb);
            }
            else
            {
                Debug.Assert(node.ArgumentCount == 0, "Node with arguments must have a constructor.");
                Debug.Assert(node.Type.IsValueType, "Only value type may have constructor not set.");
                LocalBuilder temp = GetLocal(node.Type);
                _ilg.Emit(OpCodes.Ldloca, temp);
                _ilg.Emit(OpCodes.Initobj, node.Type);
                _ilg.Emit(OpCodes.Ldloc, temp);
                FreeLocal(temp);
            }
        }

        private void EmitTypeBinaryExpression(Expression expr)
        {
            TypeBinaryExpression node = (TypeBinaryExpression)expr;

            if (node.NodeType == ExpressionType.TypeEqual)
            {
                EmitExpression(node.ReduceTypeEqual());
                return;
            }

            Type type = node.Expression.Type;

            // Try to determine the result statically
            AnalyzeTypeIsResult result = ConstantCheck.AnalyzeTypeIs(node);

            if (result == AnalyzeTypeIsResult.KnownTrue ||
                result == AnalyzeTypeIsResult.KnownFalse)
            {
                // Result is known statically, so just emit the expression for
                // its side effects and return the result
                EmitExpressionAsVoid(node.Expression);
                _ilg.EmitPrimitive(result == AnalyzeTypeIsResult.KnownTrue);
                return;
            }

            if (result == AnalyzeTypeIsResult.KnownAssignable)
            {
                // We know the type can be assigned, but still need to check
                // for null at runtime
                if (type.IsNullableType())
                {
                    EmitAddress(node.Expression, type);
                    _ilg.EmitHasValue(type);
                    return;
                }

                Debug.Assert(!type.IsValueType);
                EmitExpression(node.Expression);
                _ilg.Emit(OpCodes.Ldnull);
                _ilg.Emit(OpCodes.Cgt_Un);
                return;
            }

            Debug.Assert(result == AnalyzeTypeIsResult.Unknown);

            // Emit a full runtime "isinst" check
            EmitExpression(node.Expression);
            if (type.IsValueType)
            {
                _ilg.Emit(OpCodes.Box, type);
            }
            _ilg.Emit(OpCodes.Isinst, node.TypeOperand);
            _ilg.Emit(OpCodes.Ldnull);
            _ilg.Emit(OpCodes.Cgt_Un);
        }

        private void EmitVariableAssignment(AssignBinaryExpression node, CompilationFlags flags)
        {
            var variable = (ParameterExpression)node.Left;
            CompilationFlags emitAs = flags & CompilationFlags.EmitAsTypeMask;

            if (node.IsByRef)
            {
                EmitAddress(node.Right, node.Right.Type);
            }
            else
            {
                EmitExpression(node.Right);
            }

            if (emitAs != CompilationFlags.EmitAsVoidType)
            {
                _ilg.Emit(OpCodes.Dup);
            }

            if (variable.IsByRef)
            {
                // Note: the stloc/ldloc pattern is a bit suboptimal, but it
                // saves us from having to spill stack when assigning to a
                // byref parameter. We already make this same trade-off for
                // hoisted variables, see ElementStorage.EmitStore

                LocalBuilder value = GetLocal(variable.Type);
                _ilg.Emit(OpCodes.Stloc, value);
                _scope.EmitGet(variable);
                _ilg.Emit(OpCodes.Ldloc, value);
                FreeLocal(value);
                _ilg.EmitStoreValueIndirect(variable.Type);
            }
            else
            {
                _scope.EmitSet(variable);
            }
        }

        private void EmitAssignBinaryExpression(Expression expr)
        {
            EmitAssign((AssignBinaryExpression)expr, CompilationFlags.EmitAsDefaultType);
        }

        private void EmitAssign(AssignBinaryExpression node, CompilationFlags emitAs)
        {
            switch (node.Left.NodeType)
            {
                case ExpressionType.Index:
                    EmitIndexAssignment(node, emitAs);
                    return;
                case ExpressionType.MemberAccess:
                    EmitMemberAssignment(node, emitAs);
                    return;
                case ExpressionType.Parameter:
                    EmitVariableAssignment(node, emitAs);
                    return;
                default:
                    throw ContractUtils.Unreachable;
            }
        }

        private void EmitParameterExpression(Expression expr)
        {
            ParameterExpression node = (ParameterExpression)expr;
            _scope.EmitGet(node);
            if (node.IsByRef)
            {
                _ilg.EmitLoadValueIndirect(node.Type);
            }
        }

        private void EmitLambdaExpression(Expression expr)
        {
            LambdaExpression node = (LambdaExpression)expr;
            EmitDelegateConstruction(node);
        }

        private void EmitRuntimeVariablesExpression(Expression expr)
        {
            RuntimeVariablesExpression node = (RuntimeVariablesExpression)expr;
            _scope.EmitVariableAccess(this, node.Variables);
        }

        private void EmitMemberAssignment(AssignBinaryExpression node, CompilationFlags flags)
        {
            Debug.Assert(!node.IsByRef);

            MemberExpression lvalue = (MemberExpression)node.Left;
            MemberInfo member = lvalue.Member;

            // emit "this", if any
            Type? objectType = null;
            if (lvalue.Expression != null)
            {
                EmitInstance(lvalue.Expression, out objectType);
            }

            // emit value
            EmitExpression(node.Right);

            LocalBuilder? temp = null;
            CompilationFlags emitAs = flags & CompilationFlags.EmitAsTypeMask;
            if (emitAs != CompilationFlags.EmitAsVoidType)
            {
                // save the value so we can return it
                _ilg.Emit(OpCodes.Dup);
                _ilg.Emit(OpCodes.Stloc, temp = GetLocal(node.Type));
            }

            var fld = member as FieldInfo;
            if (fld is not null)
            {
                _ilg.EmitFieldSet((FieldInfo)member);
            }
            else
            {
                // MemberExpression.Member can only be a FieldInfo or a PropertyInfo
                Debug.Assert(member is PropertyInfo);
                var prop = (PropertyInfo)member;
                EmitCall(objectType, prop.GetSetMethod(nonPublic: true)!);
            }

            if (emitAs != CompilationFlags.EmitAsVoidType)
            {
                _ilg.Emit(OpCodes.Ldloc, temp!);
                FreeLocal(temp!);
            }
        }

        private void EmitMemberExpression(Expression expr)
        {
            MemberExpression node = (MemberExpression)expr;

            // emit "this", if any
            Type? instanceType = null;
            if (node.Expression != null)
            {
                EmitInstance(node.Expression, out instanceType);
            }

            EmitMemberGet(node.Member, instanceType);
        }

        // assumes instance is already on the stack
        private void EmitMemberGet(MemberInfo member, Type? objectType)
        {
            var fi = member as FieldInfo;
            if (fi is not null)
            {
                if (fi.IsLiteral)
                {
                    EmitConstant(fi.GetRawConstantValue(), fi.FieldType);
                }
                else
                {
                    _ilg.EmitFieldGet(fi);
                }
            }
            else
            {
                // MemberExpression.Member or MemberBinding.Member can only be a FieldInfo or a PropertyInfo
                Debug.Assert(member is PropertyInfo);
                var prop = (PropertyInfo)member;
                EmitCall(objectType, prop.GetGetMethod(nonPublic: true)!);
            }
        }

        private void EmitInstance(Expression instance, out Type type)
        {
            type = instance.Type;

            // NB: Instance can be a ByRef type due to stack spilling introducing ref locals for
            //     accessing an instance of a value type. In that case, we don't have to take the
            //     address of the instance anymore; we just load the ref local.

            if (type.IsByRef)
            {
                type = type.GetElementType()!;

                Debug.Assert(instance.NodeType == ExpressionType.Parameter);
                Debug.Assert(type!.IsValueType);

                EmitExpression(instance);
            }
            else if (type.IsValueType)
            {
                EmitAddress(instance, type);
            }
            else
            {
                EmitExpression(instance);
            }
        }

        private void EmitNewArrayExpression(Expression expr)
        {
            NewArrayExpression node = (NewArrayExpression)expr;

            ReadOnlyCollection<Expression> expressions = node.Expressions;
            int n = expressions.Count;

            if (node.NodeType == ExpressionType.NewArrayInit)
            {
                Type elementType = node.Type.GetElementType()!;

                _ilg.EmitArray(elementType, n);

                for (int i = 0; i < n; i++)
                {
                    _ilg.Emit(OpCodes.Dup);
                    _ilg.EmitPrimitive(i);
                    EmitExpression(expressions[i]);
                    _ilg.EmitStoreElement(elementType);
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    Expression x = expressions[i];
                    EmitExpression(x);
                    _ilg.EmitConvertToType(x.Type, typeof(int), isChecked: true, locals: this);
                }
                _ilg.EmitArray(node.Type);
            }
        }

        private void EmitDebugInfoExpression(Expression expr)
        {
            return;
        }

        #region ListInit, MemberInit

        private void EmitListInitExpression(Expression expr)
        {
            EmitListInit((ListInitExpression)expr);
        }

        private void EmitMemberInitExpression(Expression expr)
        {
            EmitMemberInit((MemberInitExpression)expr);
        }

        private void EmitBinding(MemberBinding binding, Type objectType)
        {
            switch (binding.BindingType)
            {
                case MemberBindingType.Assignment:
                    EmitMemberAssignment((MemberAssignment)binding, objectType);
                    break;
                case MemberBindingType.ListBinding:
                    EmitMemberListBinding((MemberListBinding)binding);
                    break;
                case MemberBindingType.MemberBinding:
                    EmitMemberMemberBinding((MemberMemberBinding)binding);
                    break;
            }
        }

        private void EmitMemberAssignment(MemberAssignment binding, Type objectType)
        {
            EmitExpression(binding.Expression);
            if (binding.Member is FieldInfo fi)
            {
                _ilg.Emit(OpCodes.Stfld, fi);
            }
            else
            {
                Debug.Assert(binding.Member is PropertyInfo);
                EmitCall(objectType, (binding.Member as PropertyInfo)!.GetSetMethod(nonPublic: true)!);
            }
        }

        private void EmitMemberMemberBinding(MemberMemberBinding binding)
        {
            Type type = GetMemberType(binding.Member);
            if (binding.Member is PropertyInfo && type.IsValueType)
            {
                throw Error.CannotAutoInitializeValueTypeMemberThroughProperty(binding.Member);
            }
            if (type.IsValueType)
            {
                EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
            }
            else
            {
                EmitMemberGet(binding.Member, binding.Member.DeclaringType);
            }
            EmitMemberInit(binding.Bindings, false, type);
        }

        private void EmitMemberListBinding(MemberListBinding binding)
        {
            Type type = GetMemberType(binding.Member);
            if (binding.Member is PropertyInfo && type.IsValueType)
            {
                throw Error.CannotAutoInitializeValueTypeElementThroughProperty(binding.Member);
            }
            if (type.IsValueType)
            {
                EmitMemberAddress(binding.Member, binding.Member.DeclaringType);
            }
            else
            {
                EmitMemberGet(binding.Member, binding.Member.DeclaringType);
            }
            EmitListInit(binding.Initializers, false, type);
        }

        private void EmitMemberInit(MemberInitExpression init)
        {
            EmitExpression(init.NewExpression);
            LocalBuilder? loc = null;
            if (init.NewExpression.Type.IsValueType && init.Bindings.Count > 0)
            {
                loc = GetLocal(init.NewExpression.Type);
                _ilg.Emit(OpCodes.Stloc, loc);
                _ilg.Emit(OpCodes.Ldloca, loc);
            }
            EmitMemberInit(init.Bindings, loc == null, init.NewExpression.Type);
            if (loc != null)
            {
                _ilg.Emit(OpCodes.Ldloc, loc);
                FreeLocal(loc);
            }
        }

        // This method assumes that the instance is on the stack and is expected, based on "keepOnStack" flag
        // to either leave the instance on the stack, or pop it.
        private void EmitMemberInit(ReadOnlyCollection<MemberBinding> bindings, bool keepOnStack, Type objectType)
        {
            int n = bindings.Count;
            if (n == 0)
            {
                // If there are no initializers and instance is not to be kept on the stack, we must pop explicitly.
                if (!keepOnStack)
                {
                    _ilg.Emit(OpCodes.Pop);
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    if (keepOnStack || i < n - 1)
                    {
                        _ilg.Emit(OpCodes.Dup);
                    }
                    EmitBinding(bindings[i], objectType);
                }
            }
        }

        private void EmitListInit(ListInitExpression init)
        {
            EmitExpression(init.NewExpression);
            LocalBuilder? loc = null;
            if (init.NewExpression.Type.IsValueType)
            {
                loc = GetLocal(init.NewExpression.Type);
                _ilg.Emit(OpCodes.Stloc, loc);
                _ilg.Emit(OpCodes.Ldloca, loc);
            }
            EmitListInit(init.Initializers, loc == null, init.NewExpression.Type);
            if (loc != null)
            {
                _ilg.Emit(OpCodes.Ldloc, loc);
                FreeLocal(loc);
            }
        }

        // This method assumes that the list instance is on the stack and is expected, based on "keepOnStack" flag
        // to either leave the list instance on the stack, or pop it.
        private void EmitListInit(ReadOnlyCollection<ElementInit> initializers, bool keepOnStack, Type objectType)
        {
            int n = initializers.Count;

            if (n == 0)
            {
                // If there are no initializers and instance is not to be kept on the stack, we must pop explicitly.
                if (!keepOnStack)
                {
                    _ilg.Emit(OpCodes.Pop);
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    if (keepOnStack || i < n - 1)
                    {
                        _ilg.Emit(OpCodes.Dup);
                    }
                    EmitMethodCall(initializers[i].AddMethod, initializers[i], objectType);

                    // Some add methods, ArrayList.Add for example, return non-void
                    if (initializers[i].AddMethod.ReturnType != typeof(void))
                    {
                        _ilg.Emit(OpCodes.Pop);
                    }
                }
            }
        }

        private static Type GetMemberType(MemberInfo member)
        {
            Debug.Assert(member is FieldInfo || member is PropertyInfo);
            return member is FieldInfo fi ? fi.FieldType : (member as PropertyInfo)!.PropertyType;
        }

        #endregion

        #region Expression helpers

        private void EmitLift(ExpressionType nodeType, Type resultType, MethodCallExpression mc, ParameterExpression[] paramList, Expression[] argList)
        {
            Debug.Assert(TypeUtils.AreEquivalent(resultType.GetNonNullableType(), mc.Type.GetNonNullableType()));

            switch (nodeType)
            {
                default:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    {
                        Label exit = _ilg.DefineLabel();
                        Label exitNull = _ilg.DefineLabel();
                        LocalBuilder anyNull = GetLocal(typeof(bool));
                        _ilg.Emit(OpCodes.Ldc_I4_0);
                        _ilg.Emit(OpCodes.Stloc, anyNull);
                        for (int i = 0, n = paramList.Length; i < n; i++)
                        {
                            ParameterExpression v = paramList[i];
                            Expression arg = argList[i];
                            if (arg.Type.IsNullableType())
                            {
                                _scope.AddLocal(this, v);
                                EmitAddress(arg, arg.Type);
                                _ilg.Emit(OpCodes.Dup);
                                _ilg.EmitHasValue(arg.Type);
                                _ilg.Emit(OpCodes.Ldc_I4_0);
                                _ilg.Emit(OpCodes.Ceq);
                                _ilg.Emit(OpCodes.Stloc, anyNull);
                                _ilg.EmitGetValueOrDefault(arg.Type);
                                _scope.EmitSet(v);
                            }
                            else
                            {
                                _scope.AddLocal(this, v);
                                EmitExpression(arg);
                                if (!arg.Type.IsValueType)
                                {
                                    _ilg.Emit(OpCodes.Dup);
                                    _ilg.Emit(OpCodes.Ldnull);
                                    _ilg.Emit(OpCodes.Ceq);
                                    _ilg.Emit(OpCodes.Stloc, anyNull);
                                }
                                _scope.EmitSet(v);
                            }
                            _ilg.Emit(OpCodes.Ldloc, anyNull);
                            _ilg.Emit(OpCodes.Brtrue, exitNull);
                        }
                        EmitMethodCallExpression(mc);
                        if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
                        {
                            ConstructorInfo ci = TypeUtils.GetNullableConstructor(resultType);
                            _ilg.Emit(OpCodes.Newobj, ci);
                        }
                        _ilg.Emit(OpCodes.Br_S, exit);
                        _ilg.MarkLabel(exitNull);
                        if (TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
                        {
                            if (resultType.IsValueType)
                            {
                                LocalBuilder result = GetLocal(resultType);
                                _ilg.Emit(OpCodes.Ldloca, result);
                                _ilg.Emit(OpCodes.Initobj, resultType);
                                _ilg.Emit(OpCodes.Ldloc, result);
                                FreeLocal(result);
                            }
                            else
                            {
                                _ilg.Emit(OpCodes.Ldnull);
                            }
                        }
                        else
                        {
                            Debug.Assert(nodeType == ExpressionType.LessThan
                                || nodeType == ExpressionType.LessThanOrEqual
                                || nodeType == ExpressionType.GreaterThan
                                || nodeType == ExpressionType.GreaterThanOrEqual);

                            _ilg.Emit(OpCodes.Ldc_I4_0);
                        }
                        _ilg.MarkLabel(exit);
                        FreeLocal(anyNull);
                        return;
                    }
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    {
                        if (TypeUtils.AreEquivalent(resultType, mc.Type.GetNullableType()))
                        {
                            goto default;
                        }
                        Label exit = _ilg.DefineLabel();
                        Label exitAllNull = _ilg.DefineLabel();
                        Label exitAnyNull = _ilg.DefineLabel();

                        LocalBuilder anyNull = GetLocal(typeof(bool));
                        LocalBuilder allNull = GetLocal(typeof(bool));
                        _ilg.Emit(OpCodes.Ldc_I4_0);
                        _ilg.Emit(OpCodes.Stloc, anyNull);
                        _ilg.Emit(OpCodes.Ldc_I4_1);
                        _ilg.Emit(OpCodes.Stloc, allNull);

                        for (int i = 0, n = paramList.Length; i < n; i++)
                        {
                            ParameterExpression v = paramList[i];
                            Expression arg = argList[i];
                            _scope.AddLocal(this, v);
                            if (arg.Type.IsNullableType())
                            {
                                EmitAddress(arg, arg.Type);
                                _ilg.Emit(OpCodes.Dup);
                                _ilg.EmitHasValue(arg.Type);
                                _ilg.Emit(OpCodes.Ldc_I4_0);
                                _ilg.Emit(OpCodes.Ceq);
                                _ilg.Emit(OpCodes.Dup);
                                _ilg.Emit(OpCodes.Ldloc, anyNull);
                                _ilg.Emit(OpCodes.Or);
                                _ilg.Emit(OpCodes.Stloc, anyNull);
                                _ilg.Emit(OpCodes.Ldloc, allNull);
                                _ilg.Emit(OpCodes.And);
                                _ilg.Emit(OpCodes.Stloc, allNull);
                                _ilg.EmitGetValueOrDefault(arg.Type);
                            }
                            else
                            {
                                EmitExpression(arg);
                                if (!arg.Type.IsValueType)
                                {
                                    _ilg.Emit(OpCodes.Dup);
                                    _ilg.Emit(OpCodes.Ldnull);
                                    _ilg.Emit(OpCodes.Ceq);
                                    _ilg.Emit(OpCodes.Dup);
                                    _ilg.Emit(OpCodes.Ldloc, anyNull);
                                    _ilg.Emit(OpCodes.Or);
                                    _ilg.Emit(OpCodes.Stloc, anyNull);
                                    _ilg.Emit(OpCodes.Ldloc, allNull);
                                    _ilg.Emit(OpCodes.And);
                                    _ilg.Emit(OpCodes.Stloc, allNull);
                                }
                                else
                                {
                                    _ilg.Emit(OpCodes.Ldc_I4_0);
                                    _ilg.Emit(OpCodes.Stloc, allNull);
                                }
                            }
                            _scope.EmitSet(v);
                        }
                        _ilg.Emit(OpCodes.Ldloc, allNull);
                        _ilg.Emit(OpCodes.Brtrue, exitAllNull);
                        _ilg.Emit(OpCodes.Ldloc, anyNull);
                        _ilg.Emit(OpCodes.Brtrue, exitAnyNull);

                        EmitMethodCallExpression(mc);
                        if (resultType.IsNullableType() && !TypeUtils.AreEquivalent(resultType, mc.Type))
                        {
                            ConstructorInfo ci = TypeUtils.GetNullableConstructor(resultType);
                            _ilg.Emit(OpCodes.Newobj, ci);
                        }
                        _ilg.Emit(OpCodes.Br_S, exit);

                        _ilg.MarkLabel(exitAllNull);
                        _ilg.EmitPrimitive(nodeType == ExpressionType.Equal);
                        _ilg.Emit(OpCodes.Br_S, exit);

                        _ilg.MarkLabel(exitAnyNull);
                        _ilg.EmitPrimitive(nodeType == ExpressionType.NotEqual);

                        _ilg.MarkLabel(exit);
                        FreeLocal(anyNull);
                        FreeLocal(allNull);
                        return;
                    }
            }
        }

        #endregion
    }
}
