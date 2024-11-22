// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using static System.Reflection.InvokerEmitUtil;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        /// <summary>
        /// Returns a delegate that can be used to invoke a method with no arguments which are typically property getters.
        /// </summary>
        public static Delegate? GetWellKnownSignatureFor0Args(in InvokeSignatureInfoKey signatureInfo)
        {
            Debug.Assert(signatureInfo.ParameterTypes.Length == 0);

            return null;

            // This approach uses function pointers to call an instance method, which isn't supported
            // due to the HasThis calling convention not present in the function pointer signature and
            // currently no support by the delegate* syntax to specify that calling convention.
            // Todo: change to hard-coding the startup-specific types and methods.

            /*
            if (signatureInfo.DeclaringType != typeof(object))
            {
                // Only reference types are supported.
                return null;
            }

            Type retType = signatureInfo.ReturnType;

            if (!ReferenceEquals(retType.Assembly, typeof(object).Assembly))
            {
                // We can only hard-code types in this assembly.
                return null;
            }

            if (retType == typeof(bool)) return new InvokeFunc_Obj0Args(CallFunc0<bool>);
            if (retType == typeof(byte)) return new InvokeFunc_Obj0Args(CallFunc0<byte>);
            if (retType == typeof(char)) return new InvokeFunc_Obj0Args(CallFunc0<char>);
            if (retType == typeof(DateTime)) return new InvokeFunc_Obj0Args(CallFunc0<DateTime>);
            if (retType == typeof(DateTimeOffset)) return new InvokeFunc_Obj0Args(CallFunc0<DateTimeOffset>);
            if (retType == typeof(decimal)) return new InvokeFunc_Obj0Args(CallFunc0<decimal>);
            if (retType == typeof(double)) return new InvokeFunc_Obj0Args(CallFunc0<double>);
            if (retType == typeof(float)) return new InvokeFunc_Obj0Args(CallFunc0<float>);
            if (retType == typeof(Guid)) return new InvokeFunc_Obj0Args(CallFunc0<Guid>);
            if (retType == typeof(int)) return new InvokeFunc_Obj0Args(CallFunc0<int>);
            if (retType == typeof(IntPtr)) return new InvokeFunc_Obj0Args(CallFunc0<IntPtr>);
            if (retType == typeof(long)) return new InvokeFunc_Obj0Args(CallFunc0<long>);
            if (retType == typeof(object)) return new InvokeFunc_Obj0Args(CallFunc0<object>);
            if (retType == typeof(sbyte)) return new InvokeFunc_Obj0Args(CallFunc0<sbyte>);
            if (retType == typeof(short)) return new InvokeFunc_Obj0Args(CallFunc0<short>);
            if (retType == typeof(uint)) return new InvokeFunc_Obj0Args(CallFunc0<uint>);
            if (retType == typeof(UIntPtr)) return new InvokeFunc_Obj0Args(CallFunc0<UIntPtr>);
            if (retType == typeof(ulong)) return new InvokeFunc_Obj0Args(CallFunc0<ulong>);
            if (retType == typeof(ushort)) return new InvokeFunc_Obj0Args(CallFunc0<ushort>);
            if (retType == typeof(void)) return new InvokeFunc_Obj0Args(CallAction0);
            // System.Diagnostics.Tracing is used during startup.
            if (retType == typeof(System.Diagnostics.Tracing.EventKeywords)) return new InvokeFunc_Obj0Args(CallFunc0<System.Diagnostics.Tracing.EventKeywords>);
            if (retType == typeof(System.Diagnostics.Tracing.EventLevel)) return new InvokeFunc_Obj0Args(CallFunc0<System.Diagnostics.Tracing.EventLevel>);
            if (retType == typeof(System.Diagnostics.Tracing.EventOpcode)) return new InvokeFunc_Obj0Args(CallFunc0<System.Diagnostics.Tracing.EventOpcode>);
            if (retType == typeof(System.Diagnostics.Tracing.EventTask)) return new InvokeFunc_Obj0Args(CallFunc0<System.Diagnostics.Tracing.EventTask>);

            return null;
            */
        }

        /// <summary>
        /// Returns a delegate that can be used to invoke a method with a single argument and no return which are typically property setters.
        /// </summary>
        public static Delegate? GetWellKnownSignatureFor1Arg(in InvokeSignatureInfoKey signatureInfo)
        {
            Debug.Assert(signatureInfo.ParameterTypes.Length == 1);

            return null;

            // This approach uses function pointers to call an instance method, which isn't supported
            // due to the HasThis calling convention not present in the function pointer signature and
            // currently no support by the delegate* syntax to specify that calling convention.
            // Todo: change to hard-coding the startup-specific types and methods.

            /*
            if (signatureInfo.DeclaringType != typeof(object) || signatureInfo.ReturnType != typeof(void))
            {
                // Only reference types and methods with no return are supported.
                return null;
            }

            Type argType = signatureInfo.ParameterTypes[0];

            if (!ReferenceEquals(argType.Assembly, typeof(object).Assembly))
            {
                // We can only hard-code types in this assembly.
                return null;
            }

            if (argType == typeof(bool)) return new InvokeFunc_Obj1Arg(CallAction1<bool>);
            if (argType == typeof(byte)) return new InvokeFunc_Obj1Arg(CallAction1<byte>);
            if (argType == typeof(char)) return new InvokeFunc_Obj1Arg(CallAction1<char>);
            if (argType == typeof(DateTime)) return new InvokeFunc_Obj1Arg(CallAction1<DateTime>);
            if (argType == typeof(DateTimeOffset)) return new InvokeFunc_Obj1Arg(CallAction1<DateTimeOffset>);
            if (argType == typeof(decimal)) return new InvokeFunc_Obj1Arg(CallAction1<decimal>);
            if (argType == typeof(double)) return new InvokeFunc_Obj1Arg(CallAction1<double>);
            if (argType == typeof(float)) return new InvokeFunc_Obj1Arg(CallAction1<float>);
            if (argType == typeof(Guid)) return new InvokeFunc_Obj1Arg(CallAction1<Guid>);
            if (argType == typeof(int)) return new InvokeFunc_Obj1Arg(CallAction1<int>);
            if (argType == typeof(IntPtr)) return new InvokeFunc_Obj1Arg(CallAction1<IntPtr>);
            if (argType == typeof(long)) return new InvokeFunc_Obj1Arg(CallAction1<long>);
            if (argType == typeof(object)) return new InvokeFunc_Obj1Arg(CallAction1<object>);
            if (argType == typeof(sbyte)) return new InvokeFunc_Obj1Arg(CallAction1<sbyte>);
            if (argType == typeof(short)) return new InvokeFunc_Obj1Arg(CallAction1<short>);
            if (argType == typeof(uint)) return new InvokeFunc_Obj1Arg(CallAction1<uint>);
            if (argType == typeof(UIntPtr)) return new InvokeFunc_Obj1Arg(CallAction1<UIntPtr>);
            if (argType == typeof(ulong)) return new InvokeFunc_Obj1Arg(CallAction1<ulong>);
            if (argType == typeof(ushort)) return new InvokeFunc_Obj1Arg(CallAction1<ushort>);
            // System.Diagnostics.Tracing is used during startup.
            if (argType == typeof(Diagnostics.Tracing.EventKeywords)) return new InvokeFunc_Obj1Arg(CallAction1<Diagnostics.Tracing.EventKeywords>);
            if (argType == typeof(Diagnostics.Tracing.EventLevel)) return new InvokeFunc_Obj1Arg(CallAction1<Diagnostics.Tracing.EventLevel>);
            if (argType == typeof(Diagnostics.Tracing.EventOpcode)) return new InvokeFunc_Obj1Arg(CallAction1<Diagnostics.Tracing.EventOpcode>);
            if (argType == typeof(Diagnostics.Tracing.EventTask)) return new InvokeFunc_Obj1Arg(CallAction1<Diagnostics.Tracing.EventTask>);

            return null;
            */
        }

        //private static unsafe object? CallAction0(object? o, IntPtr f) { ((delegate* managed<object?, void>)f)(o); return null; }
        //private static unsafe object? CallAction1<TArg1>(object? o, IntPtr f, object? a) { ((delegate* managed<object?, TArg1, void>)f)(o, (TArg1)a!); return null; }
        //private static unsafe object? CallFunc0<TReturn>(object? o, IntPtr f) => ((delegate* managed<object?, TReturn>)f)(o);
    }
}
