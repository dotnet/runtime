// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace System.Reflection
{
    internal static partial class InvokerEmitUtil
    {
        internal unsafe delegate object? InvokeFunc_Debugger(Debugger.FunctionEvaluation evaluation, void** storage);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:MakeGenericMethod",
            Justification = "Debugger function evaluation is inherently executing dynamic code and unreferenced code.")]
        internal static unsafe InvokeFunc_Debugger CreateInvokeDelegate_Debugger(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);
            Type? declaringType = method.DeclaringType;
            Type returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);

            Type[] delegateParameters = [typeof(object), typeof(Debugger.FunctionEvaluation), typeof(void**)];
            var dm = new DynamicMethod(
                InvokeStubPrefix + (declaringType is not null ? declaringType.Name + "." : string.Empty) + method.Name,
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module,
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            int resultSlot = parameters.Length + 1;
            LocalBuilder?[] locals = new LocalBuilder?[parameters.Length + 1];
            LocalBuilder?[] usesLocal = new LocalBuilder?[locals.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                Type type = parameters[i].ParameterType;
                if (type.IsByRef)
                {
                    type = type.GetElementType()!;
                }

                if (type.IsByRefLike)
                {
                    EmitTypedArgument(i + 1, type);
                }
            }

            Label? invocationComplete = null;
            LocalBuilder? newObject = null;
            if (method is ConstructorInfo && !method.IsStatic && !declaringType!.IsAbstract)
            {
                Label callExisting = il.DefineLabel();
                invocationComplete = il.DefineLabel();
                newObject = il.DeclareLocal(declaringType);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, EvaluationMethods.IsNewObject);
                il.Emit(OpCodes.Brfalse, callExisting);
                EmitLoadRefArguments(il, parameters, argumentArrayIndex: 2, argumentOffset: 1);
                EmitCall(il, method, emitNew: true, backwardsCompat: true);
                il.Emit(OpCodes.Stloc, newObject);
                il.Emit(OpCodes.Br, invocationComplete.Value);
                il.MarkLabel(callExisting);
            }

            if (!method.IsStatic && declaringType!.IsByRefLike)
            {
                EmitTypedArgument(0, declaringType);
            }

            if (!method.IsStatic)
            {
                EmitLoadRefArgument(il, 0, argumentArrayIndex: 2);
                if (!declaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Ldind_Ref);
                }
            }

            EmitLoadRefArguments(il, parameters, argumentArrayIndex: 2, argumentOffset: 1);
            EmitCall(il, method, emitNew: false, backwardsCompat: true);

            LocalBuilder? result = null;
            if (returnType != typeof(void))
            {
                result = il.DeclareLocal(returnType);
                il.Emit(OpCodes.Stloc, result);
            }

            if (invocationComplete is Label complete)
            {
                il.MarkLabel(complete);
            }

            // Exact typed locals remain live through every allocating nullable copy-back.
            // There is deliberately no managed exception handler around the evaluated call.
            if (!method.IsStatic && declaringType!.IsValueType)
            {
                if (method is ConstructorInfo)
                {
                    Label done = il.DefineLabel();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Call, EvaluationMethods.IsNewObject);
                    il.Emit(OpCodes.Brtrue, done);
                    EmitCopyBack(0);
                    il.MarkLabel(done);
                }
                else
                {
                    EmitCopyBack(0);
                }
            }

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType.IsByRef)
                {
                    EmitCopyBack(i + 1);
                }
            }

            if (result is not null)
            {
                EmitResult(result, returnType);
            }
            else if (newObject is not null)
            {
                Label done = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Call, EvaluationMethods.IsNewObject);
                il.Emit(OpCodes.Brfalse, done);
                if (declaringType!.IsValueType)
                {
                    EmitResult(newObject, declaringType);
                }
                else
                {
                    il.Emit(OpCodes.Ldloc, newObject);
                    il.Emit(OpCodes.Ret);
                }
                il.MarkLabel(done);
            }

            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            return (InvokeFunc_Debugger)dm.CreateDelegate(typeof(InvokeFunc_Debugger), target: null);

            void EmitTypedArgument(int slot, Type type)
            {
                LocalBuilder local = il.DeclareLocal(type);
                LocalBuilder active = il.DeclareLocal(typeof(bool));
                locals[slot] = local;
                usesLocal[slot] = active;
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, slot);
                il.Emit(OpCodes.Call, EvaluationMethods.UsesTypedArgument);
                il.Emit(OpCodes.Stloc, active);
                Label done = il.DefineLabel();
                il.Emit(OpCodes.Ldloc, active);
                il.Emit(OpCodes.Brfalse, done);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, slot);
                il.Emit(OpCodes.Ldloca, local);
                il.Emit(OpCodes.Call, EvaluationMethods.InitializeTypedArgument.MakeGenericMethod(type));
                il.MarkLabel(done);
            }

            void EmitCopyBack(int slot)
            {
                if (locals[slot] is LocalBuilder local)
                {
                    Label done = il.DefineLabel();
                    il.Emit(OpCodes.Ldloc, usesLocal[slot]!);
                    il.Emit(OpCodes.Brfalse, done);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Ldloca, local);
                    il.Emit(OpCodes.Call, EvaluationMethods.CopyBackTypedArgument.MakeGenericMethod(local.LocalType));
                    il.MarkLabel(done);
                }
                else
                {
                    Label done = il.DefineLabel();
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Call, EvaluationMethods.NeedsCopyBack);
                    il.Emit(OpCodes.Brfalse, done);
                    il.Emit(OpCodes.Ldarg_1);
                    il.Emit(OpCodes.Ldc_I4, slot);
                    il.Emit(OpCodes.Call, EvaluationMethods.CopyBackArgument);
                    il.MarkLabel(done);
                }
            }

            void EmitResult(LocalBuilder local, Type type)
            {
                EmitLoadRefArgument(il, resultSlot, argumentArrayIndex: 2);
                il.Emit(OpCodes.Ldloc, local);
                if (type.IsByRef)
                {
                    il.Emit(OpCodes.Stind_I);
                }
                else
                {
                    il.Emit(OpCodes.Stobj, type.IsPointer || type.IsFunctionPointer ? typeof(IntPtr) : type);
                }

            }
        }

        private static class EvaluationMethods
        {
            public static MethodInfo IsNewObject { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.IsNewObject));
            public static MethodInfo UsesTypedArgument { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.UsesTypedArgument));
            public static MethodInfo InitializeTypedArgument { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.InitializeTypedArgument));
            public static MethodInfo NeedsCopyBack { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.NeedsCopyBack));
            public static MethodInfo CopyBackTypedArgument { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.CopyBackTypedArgument));
            public static MethodInfo CopyBackArgument { get; } = GetMethod(nameof(Debugger.FunctionEvaluation.CopyBackArgument));

            private static MethodInfo GetMethod(string name) =>
                typeof(Debugger.FunctionEvaluation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        }
    }
}
