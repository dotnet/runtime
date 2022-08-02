// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Dynamic;
using System.Dynamic.Utils;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Linq.Expressions.Compiler
{
    internal static partial class DelegateHelpers
    {
        /// <summary>
        /// Finds a delegate type for a CallSite using the types in the ReadOnlyCollection of Expression.
        ///
        /// We take the read-only collection of Expression explicitly to avoid allocating memory (an array
        /// of types) on lookup of delegate types.
        /// </summary>
        internal static Type MakeCallSiteDelegate(ReadOnlyCollection<Expression> types, Type returnType)
        {
            lock (_DelegateCache)
            {
                TypeInfo curTypeInfo = _DelegateCache;

                // CallSite
                curTypeInfo = NextTypeInfo(typeof(CallSite), curTypeInfo);

                // arguments
                for (int i = 0; i < types.Count; i++)
                {
                    curTypeInfo = NextTypeInfo(types[i].Type, curTypeInfo);
                }

                // return type
                curTypeInfo = NextTypeInfo(returnType, curTypeInfo);

                // see if we have the delegate already
                if (curTypeInfo.DelegateType == null)
                {
                    curTypeInfo.MakeDelegateType(returnType, types);
                }

                return curTypeInfo.DelegateType!;
            }
        }

        /// <summary>
        /// Finds a delegate type for a CallSite using the MetaObject array.
        ///
        /// We take the array of MetaObject explicitly to avoid allocating memory (an array of types) on
        /// lookup of delegate types.
        /// </summary>
        internal static Type MakeDeferredSiteDelegate(DynamicMetaObject[] args, Type returnType)
        {
            lock (_DelegateCache)
            {
                TypeInfo curTypeInfo = _DelegateCache;

                // CallSite
                curTypeInfo = NextTypeInfo(typeof(CallSite), curTypeInfo);

                // arguments
                for (int i = 0; i < args.Length; i++)
                {
                    DynamicMetaObject mo = args[i];
                    Type paramType = mo.Expression.Type;
                    if (IsByRef(mo))
                    {
                        paramType = paramType.MakeByRefType();
                    }
                    curTypeInfo = NextTypeInfo(paramType, curTypeInfo);
                }

                // return type
                curTypeInfo = NextTypeInfo(returnType, curTypeInfo);

                // see if we have the delegate already
                if (curTypeInfo.DelegateType == null)
                {
                    // nope, go ahead and create it and spend the
                    // cost of creating the array.
                    Type[] paramTypes = new Type[args.Length + 2];
                    paramTypes[0] = typeof(CallSite);
                    paramTypes[paramTypes.Length - 1] = returnType;
                    for (int i = 0; i < args.Length; i++)
                    {
                        DynamicMetaObject mo = args[i];
                        Type paramType = mo.Expression.Type;
                        if (IsByRef(mo))
                        {
                            paramType = paramType.MakeByRefType();
                        }
                        paramTypes[i + 1] = paramType;
                    }

                    curTypeInfo.DelegateType = MakeNewDelegate(paramTypes);
                }

                return curTypeInfo.DelegateType;
            }
        }

        private static bool IsByRef(DynamicMetaObject mo)
        {
            return mo.Expression is ParameterExpression pe && pe.IsByRef;
        }

        private static Type MakeNewCustomDelegate(Type[] types)
        {
            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                Type returnType = types[types.Length - 1];
                Type[] parameters = types.RemoveLast();
                Type[] delegateCtorSignature = { typeof(object), typeof(IntPtr) };

                const MethodAttributes ctorAttributes = MethodAttributes.RTSpecialName | MethodAttributes.HideBySig | MethodAttributes.Public;
                const MethodImplAttributes implAttributes = MethodImplAttributes.Runtime | MethodImplAttributes.Managed;
                const MethodAttributes invokeAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual;

                TypeBuilder builder = AssemblyGen.DefineDelegateType("Delegate" + types.Length);
                builder.DefineConstructor(ctorAttributes, CallingConventions.Standard, delegateCtorSignature).SetImplementationFlags(implAttributes);
                builder.DefineMethod("Invoke", invokeAttributes, returnType, parameters).SetImplementationFlags(implAttributes);
                return builder.CreateTypeInfo();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
