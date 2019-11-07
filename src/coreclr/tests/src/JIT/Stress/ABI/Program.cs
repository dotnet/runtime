// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Intrinsics;
using System.Text;

namespace ABIStress
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            static void Usage()
            {
                Console.WriteLine("Usage: [--verbose] [--caller-index <number>] [--num-calls <number>] [--tailcalls] [--pinvokes] [--max-params <number>] [--no-ctrlc-summary]");
                Console.WriteLine("Either --caller-index or --num-calls must be specified.");
                Console.WriteLine("Example: --num-calls 100");
                Console.WriteLine("  Stress first 100 tailcalls and pinvokes");
                Console.WriteLine("Example: --tailcalls --caller-index 37 --verbose");
                Console.WriteLine("  Stress tailcaller 37, verbose output");
                Console.WriteLine("Example: --pinvokes --num-calls 1000");
                Console.WriteLine("  Stress first 1000 pinvokes");
                Console.WriteLine("Example: --tailcalls --num-calls 100 --max-params 2");
                Console.WriteLine("  Stress 100 tailcalls with either 1 or 2 parameters");
            }

            if (args.Contains("-help") || args.Contains("--help") || args.Contains("-h"))
            {
                Usage();
                return 1;
            }

            Config.Verbose = args.Contains("--verbose");

            if (args.Contains("--tailcalls"))
                Config.StressModes |= StressModes.TailCalls;
            if (args.Contains("--pinvokes"))
                Config.StressModes |= StressModes.PInvokes;

            if (Config.StressModes == StressModes.None)
                Config.StressModes = StressModes.All;

            int callerIndex = -1;
            int numCalls = -1;
            int argIndex;
            if ((argIndex = Array.IndexOf(args, "--caller-index")) != -1)
                callerIndex = int.Parse(args[argIndex + 1]);
            if ((argIndex = Array.IndexOf(args, "--num-calls")) != -1)
                numCalls = int.Parse(args[argIndex + 1]);
            if ((argIndex = Array.IndexOf(args, "--max-params")) != -1)
                Config.MaxParams = int.Parse(args[argIndex + 1]);

            if ((callerIndex == -1) == (numCalls == -1))
            {
                Usage();
                return 1;
            }

            bool ctrlCSummary = !args.Contains("--no-ctrlc-summary");

            if (Config.StressModes.HasFlag(StressModes.TailCalls))
                Console.WriteLine("Stressing tailcalls");
            if (Config.StressModes.HasFlag(StressModes.PInvokes))
                Console.WriteLine("Stressing pinvokes");

            using var tcel = new TailCallEventListener();

            int mismatches = 0;
            if (callerIndex != -1)
            {
                if (!DoCall(callerIndex))
                    mismatches++;
            }
            else
            {
                bool abortLoop = false;
                if (ctrlCSummary)
                {
                    Console.CancelKeyPress += (sender, args) =>
                    {
                        args.Cancel = true;
                        abortLoop = true;
                    };
                }

                for (int i = 0; i < numCalls && !abortLoop; i++)
                {
                    if (!DoCall(i))
                        mismatches++;

                    if ((i + 1) % 50 == 0)
                    {
                        Console.Write("{0} callers done", i + 1);
                        if (Config.StressModes.HasFlag(StressModes.TailCalls))
                            Console.Write($" ({tcel.NumSuccessfulTailCalls} successful tailcalls tested)");
                        Console.WriteLine();
                    }
                }
            }

            if (Config.StressModes.HasFlag(StressModes.TailCalls))
            {
                Console.WriteLine("{0} tailcalls tested", tcel.NumSuccessfulTailCalls);
                lock (tcel.FailureReasons)
                {
                    if (tcel.FailureReasons.Count != 0)
                    {
                        int numRejected = tcel.FailureReasons.Values.Sum();
                        Console.WriteLine("{0} rejected tailcalls. Breakdown:", numRejected);
                        foreach (var (reason, count) in tcel.FailureReasons.OrderByDescending(kvp => kvp.Value))
                            Console.WriteLine("[{0:00.00}%]: {1}", count / (double)numRejected * 100, reason);
                    }
                }
            }

            return 100 + mismatches;
        }

        private static bool DoCall(int index)
        {
            bool result = true;
            if (Config.StressModes.HasFlag(StressModes.TailCalls))
                result &= DoTailCall(index);
            if (Config.StressModes.HasFlag(StressModes.PInvokes))
                result &= DoPInvokes(index);

            return result;
        }

        private static List<Callee> s_tailCallees;
        private static bool DoTailCall(int callerIndex)
        {
            // We pregenerate tail callee parameter lists because we want to be able to select
            // a callee with less arg stack space than this caller.
            if (s_tailCallees == null)
            {
                s_tailCallees =
                    Enumerable.Range(0, Config.NumCallees)
                              .Select(i => CreateCallee(Config.TailCalleePrefix + i, s_tailCalleeCandidateArgTypes))
                              .ToList();
            }

            string callerName = Config.TailCallerPrefix + callerIndex;
            Random rand = new Random(GetSeed(callerName));
            List<TypeEx> callerParams;
            List<Callee> callable;
            do
            {
                callerParams = RandomParameters(s_tailCalleeCandidateArgTypes, rand);
                int argStackSizeApprox = s_abi.ApproximateArgStackAreaSize(callerParams);
                callable = s_tailCallees.Where(t => t.ArgStackSizeApprox < argStackSizeApprox).ToList();
            } while (callable.Count <= 0);

            int calleeIndex = rand.Next(callable.Count);
            Callee callee = callable[calleeIndex];
            callee.Emit();

            DynamicMethod caller = new DynamicMethod(
                callerName, typeof(int), callerParams.Select(t => t.Type).ToArray(), typeof(Program).Module);

            ILGenerator g = caller.GetILGenerator();

            // Create the args to pass to the callee from the caller.
            List<Value> args = GenCallerToCalleeArgs(callerParams, callee.Parameters, rand);

            if (Config.Verbose)
            {
                EmitDumpValues("Caller's incoming args", g, callerParams.Select((p, i) => new ArgValue(p, i)));
                EmitDumpValues("Caller's args to tailcall", g, args);
            }

            foreach (Value v in args)
                v.Emit(g);

            g.Emit(OpCodes.Tailcall);
            g.EmitCall(OpCodes.Call, callee.Method, null);
            g.Emit(OpCodes.Ret);

            (object callerResult, object calleeResult) = InvokeCallerCallee(caller, callerParams, callee.Method, args, rand);

            if (callerResult.Equals(calleeResult))
                return true;

            Console.WriteLine("Mismatch in tailcall: expected {0}, got {1}", calleeResult, callerResult);
            WriteSignature(caller);
            WriteSignature(callee.Method);
            return false;
        }

        private static readonly List<DynamicMethod> s_keepRooted = new List<DynamicMethod>();
        private static readonly Dictionary<int, Callee> s_pinvokees = new Dictionary<int, Callee>();
        private static bool DoPInvokes(int callerIndex)
        {
            string callerName = Config.PInvokerPrefix + callerIndex;
            Random rand = new Random(GetSeed(callerName));
            List<TypeEx> pms = RandomParameters(s_allTypes, rand);

            int calleeIndex = rand.Next(0, Config.NumCallees);
            Callee callee;
            if (!s_pinvokees.TryGetValue(calleeIndex, out callee))
            {
                callee = CreateCallee(Config.PInvokeePrefix + calleeIndex, s_pinvokeeCandidateArgTypes);
                callee.Emit();
                callee.EmitPInvokeDelegateTypes();
                s_pinvokees.Add(calleeIndex, callee);
            }

            DynamicMethod caller = new DynamicMethod(
                callerName, typeof(int[]), pms.Select(t => t.Type).ToArray(), typeof(Program).Module);

            // We need to keep callers rooted due to a stale cache bug in the runtime related to calli.
            s_keepRooted.Add(caller);

            ILGenerator g = caller.GetILGenerator();

            // Create the args to pass to the callee from the caller.
            List<Value> args = GenCallerToCalleeArgs(pms, callee.Parameters, rand);

            if (Config.Verbose)
                EmitDumpValues("Caller's incoming args", g, pms.Select((p, i) => new ArgValue(p, i)));

            // Create array to store results in
            LocalBuilder resultsArrLocal = g.DeclareLocal(typeof(int[]));
            g.Emit(OpCodes.Ldc_I4, callee.PInvokeDelegateTypes.Count);
            g.Emit(OpCodes.Newarr, typeof(int));
            g.Emit(OpCodes.Stloc, resultsArrLocal);

            // Emit pinvoke calls for each calling convention. Keep delegates rooted in a list.
            LocalBuilder resultLocal = g.DeclareLocal(typeof(int));
            List<Delegate> delegates = new List<Delegate>();
            int resultIndex = 0;
            foreach (var (cc, delegateType) in callee.PInvokeDelegateTypes)
            {
                Delegate dlg = callee.Method.CreateDelegate(delegateType);
                delegates.Add(dlg);

                if (Config.Verbose)
                    EmitDumpValues($"Caller's args to {cc} calli", g, args);

                foreach (Value v in args)
                    v.Emit(g);

                IntPtr ptr = Marshal.GetFunctionPointerForDelegate(dlg);
                g.Emit(OpCodes.Ldc_I8, (long)ptr);
                g.Emit(OpCodes.Conv_I);
                g.EmitCalli(OpCodes.Calli, cc, typeof(int), callee.Parameters.Select(p => p.Type).ToArray());
                g.Emit(OpCodes.Stloc, resultLocal);

                g.Emit(OpCodes.Ldloc, resultsArrLocal);
                g.Emit(OpCodes.Ldc_I4, resultIndex); // where to store result
                g.Emit(OpCodes.Ldloc, resultLocal); // result
                g.Emit(OpCodes.Stelem_I4);
                resultIndex++;
            }

            g.Emit(OpCodes.Ldloc, resultsArrLocal);
            g.Emit(OpCodes.Ret);

            (object callerResult, object calleeResult) =
                InvokeCallerCallee(caller, pms, callee.Method, args, rand);

            // The pointers used in the calli instructions are only valid while the delegates are alive,
            // so keep these alive until we're done executing.
            GC.KeepAlive(delegates);

            int[] results = (int[])callerResult;

            bool allCorrect = true;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i] == (int)calleeResult)
                    continue;

                allCorrect = false;
                string callType = callee.PInvokeDelegateTypes.ElementAt(i).Key.ToString();
                Console.WriteLine("Mismatch in {0}: expected {1}, got {2}", callType, calleeResult, results[i]);
            }

            if (!allCorrect)
            {
                WriteSignature(caller);
                WriteSignature(callee.Method);
            }

            return allCorrect;
        }

        private static Callee CreateCallee(string name, TypeEx[] candidateParamTypes)
        {
            Random rand = new Random(GetSeed(name));
            List<TypeEx> pms = RandomParameters(candidateParamTypes, rand);
            var tc = new Callee(name, pms);
            return tc;
        }

        private static int GetSeed(string name)
            => Fnv1a(BitConverter.GetBytes(Config.Seed).Concat(Encoding.UTF8.GetBytes(name)).ToArray());

        private static int Fnv1a(byte[] data)
        {
            uint hash = 2166136261;
            foreach (byte b in data)
            {
                hash ^= b;
                hash *= 16777619; 
            }

            return (int)hash;
        }

        private static List<TypeEx> RandomParameters(TypeEx[] candidateParamTypes, Random rand)
        {
            List<TypeEx> pms = new List<TypeEx>(rand.Next(Config.MinParams, Config.MaxParams + 1));
            for (int j = 0; j < pms.Capacity; j++)
                pms.Add(candidateParamTypes[rand.Next(candidateParamTypes.Length)]);

            return pms;
        }

        private static List<Value> GenCallerToCalleeArgs(List<TypeEx> callerParameters, List<TypeEx> calleeParameters, Random rand)
        {
            List<Value> args = new List<Value>(calleeParameters.Count);
            List<Value> candidates = new List<Value>();
            for (int j = 0; j < args.Capacity; j++)
            {
                TypeEx targetTy = calleeParameters[j];
                // Collect candidate args. For each parameter to the caller we might be able to just
                // forward it or one of its fields.
                candidates.Clear();
                CollectCandidateArgs(targetTy.Type, callerParameters, candidates);

                if (candidates.Count > 0)
                {
                    args.Add(candidates[rand.Next(candidates.Count)]);
                }
                else
                {
                    // No candidates to forward, so just create a new value here dynamically.
                    args.Add(new ConstantValue(targetTy, Gen.GenConstant(targetTy.Type, targetTy.Fields, rand)));
                }
            }

            return args;
        }

        private static void CollectCandidateArgs(Type targetTy, List<TypeEx> pms, List<Value> candidates)
        {
            for (int i = 0; i < pms.Count; i++)
            {
                TypeEx pm = pms[i];
                Value arg = null;
                if (pm.Type == targetTy)
                    candidates.Add(arg = new ArgValue(pm, i));

                if (pm.Fields == null)
                    continue;

                for (int j = 0; j < pm.Fields.Length; j++)
                {
                    FieldInfo fi = pm.Fields[j];
                    if (fi.FieldType != targetTy)
                        continue;

                    arg ??= new ArgValue(pm, i);
                    candidates.Add(new FieldValue(arg, j));
                }
            }
        }

        private static (object callerResult, object calleeResult) InvokeCallerCallee(
            DynamicMethod caller, List<TypeEx> callerParameters,
            DynamicMethod callee, List<Value> passedArgs,
            Random rand)
        {
            object[] outerArgs = callerParameters.Select(p => Gen.GenConstant(p.Type, p.Fields, rand)).ToArray();
            object[] innerArgs = passedArgs.Select(v => v.Get(outerArgs)).ToArray();

            if (Config.Verbose)
            {
                WriteSignature(caller);
                WriteSignature(callee);

                Console.WriteLine("Invoking caller through reflection with args");
                for (int j = 0; j < outerArgs.Length; j++)
                {
                    Console.Write($"arg{j}=");
                    DumpObject(outerArgs[j]);
                }
            }

            object callerResult = caller.Invoke(null, outerArgs);

            if (Config.Verbose)
            {
                Console.WriteLine("Invoking callee through reflection with args");
                for (int j = 0; j < innerArgs.Length; j++)
                {
                    Console.Write($"arg{j}=");
                    DumpObject(innerArgs[j]);
                }
            }
            object calleeResult = callee.Invoke(null, innerArgs);

            return (callerResult, calleeResult);
        }

        private static void WriteSignature(MethodInfo mi)
        {
            string ns = typeof(S1P).Namespace;
            // The normal output will include a bunch of namespaces before types which just clutter things.
            Console.WriteLine(mi.ToString().Replace(ns + ".", ""));
        }

        private static readonly MethodInfo s_writeString = typeof(Console).GetMethod("Write", new[] { typeof(string) });
        private static readonly MethodInfo s_writeLineString = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
        private static readonly MethodInfo s_dumpValue = typeof(Program).GetMethod("DumpValue", BindingFlags.NonPublic | BindingFlags.Static);

        private static void DumpObject(object o)
        {
            TypeEx ty = new TypeEx(o.GetType());
            if (ty.Fields != null)
            {
                Console.WriteLine();
                foreach (FieldInfo field in ty.Fields)
                    Console.WriteLine("  {0}={1}", field.Name, field.GetValue(o));

                return;
            }

            Console.WriteLine(o);
        }

        private static void DumpValue<T>(T value)
        {
            DumpObject(value);
        }

        // Dumps the value on the top of the stack, consuming the value.
        private static void EmitDumpValue(ILGenerator g, Type ty)
        {
            MethodInfo instantiated = s_dumpValue.MakeGenericMethod(ty);
            g.Emit(OpCodes.Call, instantiated);
        }

        private static void EmitDumpValues(string listName, ILGenerator g, IEnumerable<Value> values)
        {
            g.Emit(OpCodes.Ldstr, $"{listName}:");
            g.Emit(OpCodes.Call, s_writeLineString);
            int index = 0;
            foreach (Value v in values)
            {
                g.Emit(OpCodes.Ldstr, $"arg{index}=");
                g.Emit(OpCodes.Call, s_writeString);

                v.Emit(g);
                EmitDumpValue(g, v.Type.Type);
                index++;
            }
        }

        private static readonly TypeEx[] s_allTypes =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(Vector<int>), typeof(Vector128<int>), typeof(Vector256<int>),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U), typeof(S17U),
                typeof(S31U), typeof(S32U),
                typeof(Hfa1), typeof(Hfa2),
            }.Select(t => new TypeEx(t)).ToArray();

        private static readonly IAbi s_abi = SelectAbi();
        private static readonly TypeEx[] s_tailCalleeCandidateArgTypes =
            s_abi.TailCalleeCandidateArgTypes.Select(t => new TypeEx(t)).ToArray();

        // We cannot marshal generic types so we cannot just use all types for pinvokees.
        // This can be relaxed once https://github.com/dotnet/coreclr/pull/23899 is merged.
        private static readonly TypeEx[] s_pinvokeeCandidateArgTypes =
            new[]
            {
                typeof(byte), typeof(short), typeof(int), typeof(long),
                typeof(float), typeof(double),
                typeof(S1P), typeof(S2P), typeof(S2U), typeof(S3U),
                typeof(S4P), typeof(S4U), typeof(S5U), typeof(S6U),
                typeof(S7U), typeof(S8P), typeof(S8U), typeof(S9U),
                typeof(S10U), typeof(S11U), typeof(S12U), typeof(S13U),
                typeof(S14U), typeof(S15U), typeof(S16U), typeof(S17U),
                typeof(S31U), typeof(S32U),
                typeof(Hfa1), typeof(Hfa2),
            }.Select(t => new TypeEx(t)).ToArray();

        private static IAbi SelectAbi()
        {
            Console.WriteLine("OSVersion: {0}", Environment.OSVersion);
            Console.WriteLine("OSArchitecture: {0}", RuntimeInformation.OSArchitecture);
            Console.WriteLine("ProcessArchitecture: {0}", RuntimeInformation.ProcessArchitecture);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                if (IntPtr.Size == 8)
                {
                    Console.WriteLine("Selecting win64 ABI");
                    return new Win64Abi();
                }

                Console.WriteLine("Selecting win86 ABI");
                return new Win86Abi();
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    Console.WriteLine("Selecting arm64 ABI.");
                    return new Arm64Abi();
                }
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
                {
                    Console.WriteLine("Selecting armhf ABI.");
                    return new Arm32Abi();
                }

                Trace.Assert(RuntimeInformation.ProcessArchitecture == Architecture.X64);
                Console.WriteLine("Selecting SysV ABI");
                return new SysVAbi();
            }

            throw new NotSupportedException($"Platform {Environment.OSVersion.Platform} is not supported");
        }

        private class Callee
        {
            private static readonly MethodInfo s_hashCodeAddMethod =
                typeof(HashCode).GetMethods().Single(mi => mi.Name == "Add" && mi.GetParameters().Length == 1);
            private static readonly MethodInfo s_hashCodeToHashCodeMethod =
                typeof(HashCode).GetMethod("ToHashCode");

            public Callee(string name, List<TypeEx> parameters)
            {
                Name = name;
                Parameters = parameters;
                ArgStackSizeApprox = s_abi.ApproximateArgStackAreaSize(Parameters);
            }

            public string Name { get; }
            public List<TypeEx> Parameters { get; }
            public int ArgStackSizeApprox { get; }
            public DynamicMethod Method { get; private set; }
            public Dictionary<CallingConvention, Type> PInvokeDelegateTypes { get; private set; }

            public void Emit()
            {
                if (Method != null)
                    return;

                Method = new DynamicMethod(
                    Name, typeof(int), Parameters.Select(t => t.Type).ToArray(), typeof(Program));

                ILGenerator g = Method.GetILGenerator();
                LocalBuilder hashCode = g.DeclareLocal(typeof(HashCode));

                if (Config.Verbose)
                    EmitDumpValues("Callee's incoming args", g, Parameters.Select((t, i) => new ArgValue(t, i)));

                g.Emit(OpCodes.Ldloca, hashCode);
                g.Emit(OpCodes.Initobj, typeof(HashCode));

                for (int i = 0; i < Parameters.Count; i++)
                {
                    TypeEx pm = Parameters[i];
                    g.Emit(OpCodes.Ldloca, hashCode);
                    g.Emit(OpCodes.Ldarg, checked((short)i));
                    g.Emit(OpCodes.Call, s_hashCodeAddMethod.MakeGenericMethod(pm.Type));
                }

                g.Emit(OpCodes.Ldloca, hashCode);
                g.Emit(OpCodes.Call, s_hashCodeToHashCodeMethod);
                g.Emit(OpCodes.Ret);
            }

            private static ModuleBuilder s_delegateTypesModule;
            private static ConstructorInfo s_unmanagedFunctionPointerCtor =
                typeof(UnmanagedFunctionPointerAttribute).GetConstructor(new[] { typeof(CallingConvention) });

            public void EmitPInvokeDelegateTypes()
            {
                if (PInvokeDelegateTypes != null)
                    return;

                PInvokeDelegateTypes = new Dictionary<CallingConvention, Type>();

                if (s_delegateTypesModule == null)
                {
                    AssemblyBuilder delegates = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ABIStress_Delegates"), AssemblyBuilderAccess.RunAndCollect);
                    s_delegateTypesModule = delegates.DefineDynamicModule("ABIStress_Delegates");
                }

                foreach (CallingConvention cc in s_abi.PInvokeConventions)
                {
                    // This code is based on DelegateHelpers.cs in System.Linq.Expressions.Compiler
                    TypeBuilder tb =
                        s_delegateTypesModule.DefineType(
                            $"{Name}_Delegate_{cc}",
                            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass,
                            typeof(MulticastDelegate));

                    tb.DefineConstructor(
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName,
                        CallingConventions.Standard,
                        new[] { typeof(object), typeof(IntPtr) })
                      .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                    tb.DefineMethod(
                        "Invoke",
                        MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                        typeof(int),
                        Parameters.Select(t => t.Type).ToArray())
                      .SetImplementationFlags(MethodImplAttributes.Runtime | MethodImplAttributes.Managed);

                    tb.SetCustomAttribute(new CustomAttributeBuilder(s_unmanagedFunctionPointerCtor, new object[] { cc }));
                    PInvokeDelegateTypes.Add(cc, tb.CreateType());
                }
            }
        }
    }
}
