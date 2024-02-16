// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.BindingFlagSupport;
using System.Reflection.Runtime.General;

using Internal.Runtime.Augments;

namespace System
{
    internal static class ActivatorImplementation
    {
        [DebuggerGuidedStepThrough]
        public static object CreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, bool nonPublic)
        {
            ArgumentNullException.ThrowIfNull(type);

            type = type.UnderlyingSystemType;
            CreateInstanceCheckType(type);

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            if (nonPublic)
                bindingFlags |= BindingFlags.NonPublic;
            ConstructorInfo? constructor = type.GetConstructor(bindingFlags, null, CallingConventions.Any, Array.Empty<Type>(), null);
            if (constructor == null)
            {
                if (type.IsValueType)
                {
                    RuntimeTypeHandle typeHandle = type.TypeHandle;

                    if (RuntimeAugments.IsNullable(typeHandle))
                        return null;

                    return RuntimeAugments.RawNewObject(typeHandle);
                }

                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, type));
            }
            object result = constructor.Invoke(Array.Empty<object>());
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public static object CreateInstance(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type, BindingFlags bindingAttr, Binder binder, object?[]? args, CultureInfo? culture, object?[]? activationAttributes)
        {
            ArgumentNullException.ThrowIfNull(type);

            // If they didn't specify a lookup, then we will provide the default lookup.
            const BindingFlags LookupMask = (BindingFlags)0x000000FF;
            if ((bindingAttr & LookupMask) == 0)
                bindingAttr |= BindingFlags.Instance | BindingFlags.Public | BindingFlags.CreateInstance;

            if (activationAttributes != null && activationAttributes.Length > 0)
                throw new PlatformNotSupportedException(SR.NotSupported_ActivAttr);

            type = type.UnderlyingSystemType;
            CreateInstanceCheckType(type);

            args ??= Array.Empty<object>();
            int numArgs = args.Length;

            Type?[] argTypes = new Type[numArgs];
            for (int i = 0; i < numArgs; i++)
            {
                argTypes[i] = args[i]?.GetType();
            }

            ConstructorInfo[] candidates = type.GetConstructors(bindingAttr);
            ListBuilder<MethodBase> matches = new ListBuilder<MethodBase>(candidates.Length);
            for (int i = 0; i < candidates.Length; i++)
            {
                if (candidates[i].QualifiesBasedOnParameterCount(bindingAttr, CallingConventions.Any, argTypes))
                    matches.Add(candidates[i]);
            }
            if (matches.Count == 0)
            {
                if (numArgs == 0 && type.IsValueType)
                {
                    RuntimeTypeHandle typeHandle = type.TypeHandle;

                    if (RuntimeAugments.IsNullable(typeHandle))
                        return null;

                    return RuntimeAugments.RawNewObject(typeHandle);
                }

                throw new MissingMethodException(SR.Format(SR.Arg_NoDefCTor, type));
            }

            binder ??= Type.DefaultBinder;

            MethodBase invokeMethod = binder.BindToMethod(bindingAttr, matches.ToArray(), ref args, null, culture, null, out object? state);
            if (invokeMethod.GetParametersAsSpan().Length == 0)
            {
                if (args.Length != 0)
                {

                    Debug.Assert((invokeMethod.CallingConvention & CallingConventions.VarArgs) == CallingConventions.VarArgs);
                    throw new NotSupportedException(SR.NotSupported_CallToVarArg);
                }

                // Desktop compat: CoreClr invokes a "fast-path" here (call Activator.CreateInstance(type, true)) that also
                // bypasses the binder.ReorderArgumentArray() call. That "fast-path" isn't a fast-path for us so we won't do that
                // but we'll still null out the "state" variable to bypass the Reorder call.
                //
                // The only time this matters at all is if (1) a third party binder is being used and (2) it actually reordered the array
                // which it shouldn't have done because (a) we didn't request it to bind arguments by name, and (b) it's kinda hard to
                // reorder a zero-length args array. But who knows what a third party binder will do if we make a call to it that we didn't
                // used to do, so we'll preserve the CoreClr order of calls just to be safe.
                state = null;
            }

            object result = ((ConstructorInfo)invokeMethod).Invoke(bindingAttr, binder, args, culture);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            if (state != null)
                binder.ReorderArgumentArray(ref args, state);
            return result;
        }

        private static void CreateInstanceCheckType(Type type)
        {
            if (type is not RuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            if (type.IsAbstract)
                throw new MissingMethodException(type.IsInterface ? SR.Acc_CreateInterface : SR.Acc_CreateAbst);  // Strange but compatible exception.

            if (type.ContainsGenericParameters)
                throw new ArgumentException(SR.Format(SR.Acc_CreateGenericEx, type));

            if (type.IsByRefLike)
                throw new NotSupportedException(SR.NotSupported_ByRefLike);

            Type elementType = type;
            while (elementType.HasElementType)
                elementType = elementType.GetElementType()!;
            if (elementType == typeof(void))
                throw new NotSupportedException(SR.Acc_CreateVoid);
        }
    }
}
