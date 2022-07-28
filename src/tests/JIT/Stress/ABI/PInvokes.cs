// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace ABIStress
{
    internal partial class Program
    {
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
    }
}
