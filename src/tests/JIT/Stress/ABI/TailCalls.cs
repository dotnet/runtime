// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ABIStress
{
    internal partial class Program
    {
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
    }
}
