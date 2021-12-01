// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Threading;

namespace ABIStress
{
    public class StubsTestHelpers
    {
        private const int DefaultSeed = 20010415;
        private static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        public static void CompareNumbers(int actual, int expected)
        {
            if (actual != expected)
                throw new Exception($"Magic number {actual} didn't match expected{expected}");
        }

        public static void IsTypeHandleObject(RuntimeTypeHandle rth, string scenario)
        {
            if (Type.GetTypeFromHandle(rth) != typeof(object))
            {
                throw new Exception($"Type handle isn't object for scenario {scenario}");
            }
        }
        public static void IsTypeHandleInt(RuntimeTypeHandle rth, string scenario)
        {
            if (Type.GetTypeFromHandle(rth) != typeof(int))
            {
                throw new Exception($"Type handle isn't int for scenario {scenario}");
            }
        }
    }
    internal partial class Program
    {
        private static Dictionary<int, Callee> s_instantiatingStubCallees = new Dictionary<int, Callee>();
        private static volatile ModuleBuilder s_stubTypesModule = null;
        private static int s_stubTypesCreated = 0;

        private static MethodInfo s_gcHandleFromIntPtr = typeof(GCHandle).GetMethod("FromIntPtr");
        private static MethodInfo s_gcHandle_getTarget = typeof(GCHandle).GetMethod("get_Target");
        private static MethodInfo s_compareNumbers = typeof(StubsTestHelpers).GetMethod("CompareNumbers");
        private static MethodInfo s_isTypeHandleObject = typeof(StubsTestHelpers).GetMethod("IsTypeHandleObject");
        private static MethodInfo s_isTypeHandleInt = typeof(StubsTestHelpers).GetMethod("IsTypeHandleInt");

        enum GenericShape
        {
            NotGeneric,
            GenericOverReferenceType,
            GenericOverValueType
        }

        private static void EmitTypeHandleCheck(ILGenerator g, GenericShape genericShape, GenericTypeParameterBuilder[] typeParamArr, string scenario)
        {
            if (genericShape == GenericShape.NotGeneric)
                return;
            g.Emit(OpCodes.Ldtoken, typeParamArr[0]);
            g.Emit(OpCodes.Ldstr, scenario);
            if (genericShape == GenericShape.GenericOverReferenceType)
                g.Emit(OpCodes.Call, s_isTypeHandleObject);
            if (genericShape == GenericShape.GenericOverValueType)
                g.Emit(OpCodes.Call, s_isTypeHandleInt);
        }

        private static Type GetDelegateType(List<TypeEx> parameters, Type returnType)
        {
            Type[] genericArguments = parameters.Select(t => t.Type).Append(returnType).ToArray();
            switch (parameters.Count)
            {
                case 0:
                    return typeof(Func<>).MakeGenericType(genericArguments);
                case 1:
                    return typeof(Func<,>).MakeGenericType(genericArguments);
                case 2:
                    return typeof(Func<,,>).MakeGenericType(genericArguments);
                case 3:
                    return typeof(Func<,,,>).MakeGenericType(genericArguments);
                case 4:
                    return typeof(Func<,,,,>).MakeGenericType(genericArguments);
                case 5:
                    return typeof(Func<,,,,,>).MakeGenericType(genericArguments);
                case 6:
                    return typeof(Func<,,,,,,>).MakeGenericType(genericArguments);
                case 7:
                    return typeof(Func<,,,,,,,>).MakeGenericType(genericArguments);
                case 8:
                    return typeof(Func<,,,,,,,,>).MakeGenericType(genericArguments);
                case 9:
                    return typeof(Func<,,,,,,,,,>).MakeGenericType(genericArguments);
                case 10:
                    return typeof(Func<,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 11:
                    return typeof(Func<,,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 12:
                    return typeof(Func<,,,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 13:
                    return typeof(Func<,,,,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 14:
                    return typeof(Func<,,,,,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 15:
                    return typeof(Func<,,,,,,,,,,,,,,,>).MakeGenericType(genericArguments);
                case 16:
                    return typeof(Func<,,,,,,,,,,,,,,,,>).MakeGenericType(genericArguments);

                default:
                    throw new Exception();
            }
        }

        private static bool DoStubCall(int callerIndex, bool staticMethod, bool onValueType, GenericShape typeGenericShape, GenericShape methodGenericShape)
        {
            string callerNameSeed = Config.InstantiatingStubPrefix + "Caller" + callerIndex; // Use a consistent seed value here so that the various various of unboxing/instantiating stubs are generated with the same arg shape
            string callerName = callerNameSeed + (staticMethod ? "Static" : "Instance") + (onValueType ? "Class" : "ValueType") + typeGenericShape.ToString() + methodGenericShape.ToString();
            Random rand = new Random(Seed);
            List<TypeEx> pms;
            do
            {
                pms = RandomParameters(s_allTypes, rand);
            } while (pms.Count > 16);

            Type delegateType = GetDelegateType(pms, typeof(int));

            Callee callee = new Callee(callerName+"Callee", pms);// CreateCallee(Config.PInvokeePrefix + calleeIndex, s_allTypes);
            callee.Emit();

            Delegate calleeDelegate = callee.Method.CreateDelegate(delegateType);

            int newStubCount = Interlocked.Increment(ref s_stubTypesCreated);
            if ((s_stubTypesModule == null) || (newStubCount % 1000) == 0)
            {
                AssemblyBuilder stubsAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ABIStress_Stubs" + newStubCount), AssemblyBuilderAccess.RunAndCollect);
                s_stubTypesModule = stubsAssembly.DefineDynamicModule("ABIStress_Stubs" + newStubCount);
            }

            // This code is based on DelegateHelpers.cs in System.Linq.Expressions.Compiler
            TypeBuilder tb =
                s_stubTypesModule.DefineType(
                        $"{callerName}_GenericTarget",
                        TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass,
                        onValueType ? typeof(object) : typeof(ValueType));
            GenericTypeParameterBuilder[] typeParamsType = null;
            if (typeGenericShape != GenericShape.NotGeneric)
                typeParamsType = tb.DefineGenericParameters(new string[] { "T" });

            FieldInfo fieldDeclaration = tb.DefineField("MagicValue", typeof(int), FieldAttributes.Public);

            Type typeofInstantiatedType;
            FieldInfo fieldInfoMagicValueField;
            if (typeGenericShape == GenericShape.NotGeneric)
            {
                typeofInstantiatedType = tb;
                fieldInfoMagicValueField = fieldDeclaration;
            }
            else
            {
                typeofInstantiatedType = tb.MakeGenericType(typeParamsType[0]);
                fieldInfoMagicValueField = TypeBuilder.GetField(typeofInstantiatedType, fieldDeclaration);
            }

            ConstructorBuilder cb = tb.DefineConstructor(
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new Type[] { typeof(int) });
            cb.SetImplementationFlags(MethodImplAttributes.Managed);

            ILGenerator g = cb.GetILGenerator();
            g.Emit(OpCodes.Ldarg, 0);
            g.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes));
            g.Emit(OpCodes.Ldarg, 0);
            g.Emit(OpCodes.Ldarg_1);
            g.Emit(OpCodes.Stfld, fieldInfoMagicValueField);
            g.Emit(OpCodes.Ret);

            MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.HideBySig;

            if (staticMethod)
                methodAttributes |= MethodAttributes.Static;

            MethodBuilder mbInstance = tb.DefineMethod(
                "Method",
                methodAttributes,
                callee.Method.ReturnType,
                callee.Parameters.Select(t => t.Type).ToArray());

            GenericTypeParameterBuilder[] typeParamsMethod = null;
            if (methodGenericShape != GenericShape.NotGeneric)
                typeParamsMethod = mbInstance.DefineGenericParameters(new string[] { "T" });

            mbInstance.SetImplementationFlags(MethodImplAttributes.Managed);

            int magicNumberEmbeddedInObject = rand.Next();

            GCHandle gchCallee = GCHandle.Alloc(callee.Method.CreateDelegate(delegateType));
            IntPtr gchCalleeIntPtr = GCHandle.ToIntPtr(gchCallee);
            g = mbInstance.GetILGenerator();

            if (!staticMethod)
            {
                // Verify random number made it intact, and this parameter was handled correctly

                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldfld, fieldInfoMagicValueField);
                g.Emit(OpCodes.Ldc_I4, magicNumberEmbeddedInObject);
                g.Emit(OpCodes.Call, s_compareNumbers);
            }

            // Verify generic args are as expected
            EmitTypeHandleCheck(g, typeGenericShape, typeParamsType, "type");
            EmitTypeHandleCheck(g, methodGenericShape, typeParamsMethod, "method");

            // Make the call to callee
            LocalBuilder gcHandleLocal = g.DeclareLocal(typeof(GCHandle));
            // Load GCHandle of callee delegate
            g.Emit(OpCodes.Ldc_I8, (long)gchCalleeIntPtr);
            g.Emit(OpCodes.Conv_I);
            g.Emit(OpCodes.Call, s_gcHandleFromIntPtr);
            g.Emit(OpCodes.Stloc, gcHandleLocal);
            // Resolve to target
            g.Emit(OpCodes.Ldloca, gcHandleLocal);
            g.Emit(OpCodes.Call, s_gcHandle_getTarget);
            // Cast to delegate type
            g.Emit(OpCodes.Castclass, delegateType);
            // Load all args
            int argOffset = 1;
            if (staticMethod)
                argOffset = 0;
            for (int i = 0; i < pms.Count; i++)
                g.Emit(OpCodes.Ldarg, argOffset + i);

            // Call delegate invoke method
            g.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));
            // ret
            g.Emit(OpCodes.Ret);
            Type calleeTypeOpen = tb.CreateType();
            Type calleeType;
            switch(typeGenericShape)
            {
                case GenericShape.NotGeneric:
                    calleeType = calleeTypeOpen;
                    break;
                case GenericShape.GenericOverReferenceType:
                    calleeType = calleeTypeOpen.MakeGenericType(typeof(object));
                    break;
                case GenericShape.GenericOverValueType:
                    calleeType = calleeTypeOpen.MakeGenericType(typeof(int));
                    break;
                default:
                    throw new Exception("Unknown case");
            }

            MethodInfo targetMethodOpen = calleeType.GetMethod("Method");
            MethodInfo targetMethod;

            switch (methodGenericShape)
            {
                case GenericShape.NotGeneric:
                    targetMethod = targetMethodOpen;
                    break;
                case GenericShape.GenericOverReferenceType:
                    targetMethod = targetMethodOpen.MakeGenericMethod(typeof(object));
                    break;
                case GenericShape.GenericOverValueType:
                    targetMethod = targetMethodOpen.MakeGenericMethod(typeof(int));
                    break;
                default:
                    throw new Exception("Unknown case");
            }

            Delegate targetMethodToCallDel;

            if (staticMethod)
            {
                targetMethodToCallDel = targetMethod.CreateDelegate(delegateType);
            }
            else
            {
                targetMethodToCallDel = targetMethod.CreateDelegate(delegateType, Activator.CreateInstance(calleeType, magicNumberEmbeddedInObject));
            }

            GCHandle gchTargetMethod = GCHandle.Alloc(targetMethodToCallDel);

            // CALLER Dynamic method
            DynamicMethod caller = new DynamicMethod(
            callerName, typeof(int), pms.Select(t => t.Type).ToArray(), typeof(Program).Module);

            g = caller.GetILGenerator();

            // Create the args to pass to the callee from the caller.
            List<Value> args = GenCallerToCalleeArgs(pms, callee.Parameters, rand);

            if (Config.Verbose)
                EmitDumpValues("Caller's incoming args", g, pms.Select((p, i) => new ArgValue(p, i)));

            if (Config.Verbose)
                EmitDumpValues($"Caller's args to {callerName} call", g, args);

            gcHandleLocal = g.DeclareLocal(typeof(GCHandle));
            g.Emit(OpCodes.Ldc_I8, (long)GCHandle.ToIntPtr(gchTargetMethod));
            g.Emit(OpCodes.Conv_I);
            g.Emit(OpCodes.Call, s_gcHandleFromIntPtr);
            g.Emit(OpCodes.Stloc, gcHandleLocal);
            // Resolve to target
            g.Emit(OpCodes.Ldloca, gcHandleLocal);
            g.Emit(OpCodes.Call, s_gcHandle_getTarget);
            // Cast to delegate type
            g.Emit(OpCodes.Castclass, delegateType);

            foreach (Value v in args)
                v.Emit(g);

            // Call delegate invoke method
            g.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));
            // ret
            g.Emit(OpCodes.Ret);

            (object callerResult, object calleeResult) = InvokeCallerCallee(caller, pms, callee.Method, args, rand);

            gchCallee.Free();
            gchTargetMethod.Free();
            if (callerResult.Equals(calleeResult))
                return true;

            Console.WriteLine("Mismatch in stub call: expected {0}, got {1}", calleeResult, callerResult);
            Console.WriteLine(callerName);
            WriteSignature(caller);
            WriteSignature(callee.Method);
            return false;

        }
    }
}
