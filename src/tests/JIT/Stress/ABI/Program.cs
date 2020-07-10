// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal partial class Program
    {
        private static int Main(string[] args)
        {
            static void Usage()
            {
                Console.WriteLine("Usage: [--verbose] [--caller-index <number>] [--num-calls <number>] [--tailcalls] [--pinvokes] [--instantiatingstubs] [--unboxingstubs] [--sharedgenericunboxingstubs] [--max-params <number>] [--no-ctrlc-summary]");
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
            if (args.Contains("--instantiatingstubs"))
                Config.StressModes |= StressModes.InstantiatingStubs;
            if (args.Contains("--unboxingstubs"))
                Config.StressModes |= StressModes.UnboxingStubs;
            if (args.Contains("--sharedgenericunboxingstubs"))
                Config.StressModes |= StressModes.SharedGenericUnboxingStubs;

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
            if (Config.StressModes.HasFlag(StressModes.InstantiatingStubs))
                Console.WriteLine("Stressing instantiatingstubs");
            if (Config.StressModes.HasFlag(StressModes.UnboxingStubs))
                Console.WriteLine("Stressing unboxingstubs");
            if (Config.StressModes.HasFlag(StressModes.SharedGenericUnboxingStubs))
                Console.WriteLine("Stressing sharedgenericunboxingstubs");

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

            Console.WriteLine($"  Done with {mismatches} mismatches");
            return 100 + mismatches;
        }

        private static bool DoCall(int index)
        {
            bool result = true;
            if (Config.StressModes.HasFlag(StressModes.TailCalls))
                result &= DoTailCall(index);
            if (Config.StressModes.HasFlag(StressModes.PInvokes))
                result &= DoPInvokes(index);
            if (Config.StressModes.HasFlag(StressModes.UnboxingStubs))
            {
                result &= DoStubCall(index, staticMethod: false, onValueType: true, typeGenericShape: GenericShape.NotGeneric, methodGenericShape: GenericShape.NotGeneric);
                result &= DoStubCall(index, staticMethod: false, onValueType: true, typeGenericShape: GenericShape.NotGeneric, methodGenericShape: GenericShape.GenericOverReferenceType);
            }
            if (Config.StressModes.HasFlag(StressModes.SharedGenericUnboxingStubs))
            {
                result &= DoStubCall(index, staticMethod: false, onValueType: true, typeGenericShape: GenericShape.GenericOverReferenceType, methodGenericShape: GenericShape.NotGeneric);
            }
            if (Config.StressModes.HasFlag(StressModes.InstantiatingStubs))
            {
                result &= DoStubCall(index, staticMethod: false, onValueType: false, typeGenericShape: GenericShape.NotGeneric, methodGenericShape: GenericShape.GenericOverReferenceType);
                result &= DoStubCall(index, staticMethod: true, onValueType: false, typeGenericShape: GenericShape.GenericOverReferenceType, methodGenericShape: GenericShape.NotGeneric);
                result &= DoStubCall(index, staticMethod: true, onValueType: true, typeGenericShape: GenericShape.GenericOverReferenceType, methodGenericShape: GenericShape.NotGeneric);
                result &= DoStubCall(index, staticMethod: true, onValueType: false, typeGenericShape: GenericShape.GenericOverValueType, methodGenericShape: GenericShape.GenericOverReferenceType);
                result &= DoStubCall(index, staticMethod: true, onValueType: false, typeGenericShape: GenericShape.GenericOverReferenceType, methodGenericShape: GenericShape.GenericOverValueType);
            }
            return result;
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

        public static void EmitDumpValues(string listName, ILGenerator g, IEnumerable<Value> values)
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

        public static IAbi Abi => s_abi;

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
    }
}
