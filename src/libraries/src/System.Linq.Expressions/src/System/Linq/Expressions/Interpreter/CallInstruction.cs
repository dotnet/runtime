// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Dynamic.Utils;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal abstract partial class CallInstruction : Instruction
    {
        /// <summary>
        /// The number of arguments including "this" for instance methods.
        /// </summary>
        public abstract int ArgumentCount { get; }

        #region Construction

        public override string InstructionName => "Call";

#if FEATURE_DLG_INVOKE
        private static readonly CacheDict<MethodInfo, CallInstruction> s_cache = new CacheDict<MethodInfo, CallInstruction>(256);
#endif

        public static CallInstruction Create(MethodInfo info)
        {
            return Create(info, info.GetParametersCached());
        }

        /// <summary>
        /// Creates a new ReflectedCaller which can be used to quickly invoke the provided MethodInfo.
        /// </summary>
        public static CallInstruction Create(MethodInfo info, ParameterInfo[] parameters)
        {
            int argumentCount = parameters.Length;
            if (!info.IsStatic)
            {
                argumentCount++;
            }

            // A workaround for CLR behavior (Unable to create delegates for Array.Get/Set):
            // T[]::Address - not supported by ETs due to T& return value
            if (info.DeclaringType != null && info.DeclaringType.IsArray && (info.Name == "Get" || info.Name == "Set"))
            {
                return GetArrayAccessor(info, argumentCount);
            }

#if !FEATURE_DLG_INVOKE
            return new MethodInfoCallInstruction(info, argumentCount);
#else
            if (!info.IsStatic && info.DeclaringType.IsValueType)
            {
                return new MethodInfoCallInstruction(info, argumentCount);
            }

            if (argumentCount >= MaxHelpers)
            {
                // no delegate for this size, fall back to reflection invoke
                return new MethodInfoCallInstruction(info, argumentCount);
            }

            foreach (ParameterInfo pi in parameters)
            {
                if (pi.ParameterType.IsByRef)
                {
                    // we don't support ref args via generics.
                    return new MethodInfoCallInstruction(info, argumentCount);
                }
            }

            // see if we've created one w/ a delegate
            CallInstruction res;
            if (ShouldCache(info))
            {
                if (s_cache.TryGetValue(info, out res))
                {
                    return res;
                }
            }

            // create it
            try
            {
#if FEATURE_FAST_CREATE
                if (argumentCount < MaxArgs)
                {
                    res = FastCreate(info, parameters);
                }
                else
#endif
                {
                    res = SlowCreate(info, parameters);
                }
            }
            catch (TargetInvocationException tie)
            {
                if (!(tie.InnerException is NotSupportedException))
                {
                    throw;
                }

                res = new MethodInfoCallInstruction(info, argumentCount);
            }
            catch (NotSupportedException)
            {
                // if Delegate.CreateDelegate can't handle the method fall back to
                // the slow reflection version.  For example this can happen w/
                // a generic method defined on an interface and implemented on a class or
                // a virtual generic method.
                res = new MethodInfoCallInstruction(info, argumentCount);
            }

            // cache it for future users if it's a reasonable method to cache
            if (ShouldCache(info))
            {
                s_cache[info] = res;
            }

            return res;
#endif
        }

        private static CallInstruction GetArrayAccessor(MethodInfo info, int argumentCount)
        {
            Type arrayType = info.DeclaringType;
            bool isGetter = info.Name == "Get";
            MethodInfo alternativeMethod = null;

            switch (arrayType.GetArrayRank())
            {
                case 1:
                    alternativeMethod = isGetter ?
                        arrayType.GetMethod("GetValue", new[] { typeof(int) }) :
                        typeof(CallInstruction).GetMethod(nameof(ArrayItemSetter1));
                    break;

                case 2:
                    alternativeMethod = isGetter ?
                        arrayType.GetMethod("GetValue", new[] { typeof(int), typeof(int) }) :
                        typeof(CallInstruction).GetMethod(nameof(ArrayItemSetter2));
                    break;

                case 3:
                    alternativeMethod = isGetter ?
                        arrayType.GetMethod("GetValue", new[] { typeof(int), typeof(int), typeof(int) }) :
                        typeof(CallInstruction).GetMethod(nameof(ArrayItemSetter3));
                    break;
            }

            if ((object)alternativeMethod == null)
            {
                return new MethodInfoCallInstruction(info, argumentCount);
            }

            return Create(alternativeMethod);
        }

        public static void ArrayItemSetter1(Array array, int index0, object value)
        {
            array.SetValue(value, index0);
        }

        public static void ArrayItemSetter2(Array array, int index0, int index1, object value)
        {
            array.SetValue(value, index0, index1);
        }

        public static void ArrayItemSetter3(Array array, int index0, int index1, int index2, object value)
        {
            array.SetValue(value, index0, index1, index2);
        }
#if FEATURE_DLG_INVOKE
        private static bool ShouldCache(MethodInfo info)
        {
            return true;
        }
#endif

#if FEATURE_FAST_CREATE
        /// <summary>
        /// Gets the next type or null if no more types are available.
        /// </summary>
        private static Type TryGetParameterOrReturnType(MethodInfo target, ParameterInfo[] pi, int index)
        {
            if (!target.IsStatic)
            {
                index--;
                if (index < 0)
                {
                    return target.DeclaringType;
                }
            }

            if (index < pi.Length)
            {
                // next in signature
                return pi[index].ParameterType;
            }

            if (target.ReturnType == typeof(void) || index > pi.Length)
            {
                // no more parameters
                return null;
            }

            // last parameter on Invoke is return type
            return target.ReturnType;
        }

        private static bool IndexIsNotReturnType(int index, MethodInfo target, ParameterInfo[] pi)
        {
            return pi.Length != index || (pi.Length == index && !target.IsStatic);
        }
#endif

#if FEATURE_DLG_INVOKE
        /// <summary>
        /// Uses reflection to create new instance of the appropriate ReflectedCaller
        /// </summary>
        private static CallInstruction SlowCreate(MethodInfo info, ParameterInfo[] pis)
        {
            List<Type> types = new List<Type>();
            if (!info.IsStatic) types.Add(info.DeclaringType);
            foreach (ParameterInfo pi in pis)
            {
                types.Add(pi.ParameterType);
            }
            if (info.ReturnType != typeof(void))
            {
                types.Add(info.ReturnType);
            }
            Type[] arrTypes = types.ToArray();

            try
            {
                return (CallInstruction)Activator.CreateInstance(GetHelperType(info, arrTypes), info);
            }
            catch (TargetInvocationException e)
            {
                ExceptionHelpers.UnwrapAndRethrow(e);
                throw ContractUtils.Unreachable;
            }
        }
#endif

        #endregion

        #region Instruction

        public override int ConsumedStack => ArgumentCount;

        #endregion

        /// <summary>
        /// If the target of invocation happens to be a delegate
        /// over enclosed instance lightLambda, return that instance.
        /// We can interpret LightLambdas directly.
        /// </summary>
        protected static bool TryGetLightLambdaTarget(object instance, out LightLambda lightLambda)
        {
            var del = instance as Delegate;
            if ((object)del != null)
            {
                var thunk = del.Target as Func<object[], object>;
                if ((object)thunk != null)
                {
                    lightLambda = thunk.Target as LightLambda;
                    if (lightLambda != null)
                    {
                        return true;
                    }
                }
            }

            lightLambda = null;
            return false;
        }

        protected object InterpretLambdaInvoke(LightLambda targetLambda, object[] args)
        {
            if (ProducedStack > 0)
            {
                return targetLambda.Run(args);
            }
            else
            {
                return targetLambda.RunVoid(args);
            }
        }
    }

    internal class MethodInfoCallInstruction : CallInstruction
    {
        protected readonly MethodInfo _target;
        protected readonly int _argumentCount;

        public override int ArgumentCount => _argumentCount;

        internal MethodInfoCallInstruction(MethodInfo target, int argumentCount)
        {
            _target = target;
            _argumentCount = argumentCount;
        }

        public override int ProducedStack => _target.ReturnType == typeof(void) ? 0 : 1;

        public override int Run(InterpretedFrame frame)
        {
            int first = frame.StackIndex - _argumentCount;

            object ret;
            if (_target.IsStatic)
            {
                object[] args = GetArgs(frame, first, 0);
                try
                {
                    ret = _target.Invoke(null, args);
                }
                catch (TargetInvocationException e)
                {
                    ExceptionHelpers.UnwrapAndRethrow(e);
                    throw ContractUtils.Unreachable;
                }
            }
            else
            {
                object instance = frame.Data[first];
                NullCheck(instance);

                object[] args = GetArgs(frame, first, 1);

                LightLambda targetLambda;
                if (TryGetLightLambdaTarget(instance, out targetLambda))
                {
                    // no need to Invoke, just interpret the lambda body
                    ret = InterpretLambdaInvoke(targetLambda, args);
                }
                else
                {
                    try
                    {
                        ret = _target.Invoke(instance, args);
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionHelpers.UnwrapAndRethrow(e);
                        throw ContractUtils.Unreachable;
                    }
                }
            }

            if (_target.ReturnType != typeof(void))
            {
                frame.Data[first] = ret;
                frame.StackIndex = first + 1;
            }
            else
            {
                frame.StackIndex = first;
            }

            return 1;
        }

        protected object[] GetArgs(InterpretedFrame frame, int first, int skip)
        {
            int count = _argumentCount - skip;

            if (count > 0)
            {
                var args = new object[count];

                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = frame.Data[first + i + skip];
                }

                return args;
            }
            else
            {
                return Array.Empty<object>();
            }
        }

        public override string ToString() => "Call(" + _target + ")";
    }

    internal class ByRefMethodInfoCallInstruction : MethodInfoCallInstruction
    {
        private readonly ByRefUpdater[] _byrefArgs;

        internal ByRefMethodInfoCallInstruction(MethodInfo target, int argumentCount, ByRefUpdater[] byrefArgs)
            : base(target, argumentCount)
        {
            _byrefArgs = byrefArgs;
        }

        public override int ProducedStack => _target.ReturnType == typeof(void) ? 0 : 1;

        public sealed override int Run(InterpretedFrame frame)
        {
            int first = frame.StackIndex - _argumentCount;
            object[] args = null;
            object instance = null;

            try
            {
                object ret;
                if (_target.IsStatic)
                {
                    args = GetArgs(frame, first, 0);
                    try
                    {
                        ret = _target.Invoke(null, args);
                    }
                    catch (TargetInvocationException e)
                    {
                        ExceptionHelpers.UnwrapAndRethrow(e);
                        throw ContractUtils.Unreachable;
                    }
                }
                else
                {
                    instance = frame.Data[first];
                    NullCheck(instance);

                    args = GetArgs(frame, first, 1);

                    LightLambda targetLambda;
                    if (TryGetLightLambdaTarget(instance, out targetLambda))
                    {
                        // no need to Invoke, just interpret the lambda body
                        ret = InterpretLambdaInvoke(targetLambda, args);
                    }
                    else
                    {
                        try
                        {
                            ret = _target.Invoke(instance, args);
                        }
                        catch (TargetInvocationException e)
                        {
                            ExceptionHelpers.UnwrapAndRethrow(e);
                            throw ContractUtils.Unreachable;
                        }
                    }
                }

                if (_target.ReturnType != typeof(void))
                {
                    frame.Data[first] = ret;
                    frame.StackIndex = first + 1;
                }
                else
                {
                    frame.StackIndex = first;
                }
            }
            finally
            {
                if (args != null)
                {
                    foreach (ByRefUpdater arg in _byrefArgs)
                    {
                        // -1: instance param, just copy back the exact instance invoked with, which
                        // gets passed by reference from reflection for value types.
                        arg.Update(frame, arg.ArgumentIndex == -1 ? instance : args[arg.ArgumentIndex]);
                    }
                }
            }

            return 1;
        }
    }
}
