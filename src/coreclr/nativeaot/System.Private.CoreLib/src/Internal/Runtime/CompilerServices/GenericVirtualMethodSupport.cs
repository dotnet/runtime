// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerServices
{
    internal static class GenericVirtualMethodSupport
    {
        private static unsafe IntPtr GVMLookupForSlotWorker(RuntimeTypeHandle type, RuntimeTypeHandle declaringType, RuntimeTypeHandle[] genericArguments, MethodNameAndSignature methodNameAndSignature)
        {
            bool slotChanged = false;

            IntPtr resolution = IntPtr.Zero;
            IntPtr functionPointer;
            IntPtr genericDictionary;

            bool lookForDefaultImplementations = false;

        again:
            // Walk parent hierarchy attempting to resolve
            EETypePtr eeType = type.ToEETypePtr();

            while (!eeType.IsNull)
            {
                RuntimeTypeHandle handle = new RuntimeTypeHandle(eeType);
                string methodName = methodNameAndSignature.Name;
                RuntimeSignature methodSignature = methodNameAndSignature.Signature;
                if (RuntimeAugments.TypeLoaderCallbacks.TryGetGenericVirtualTargetForTypeAndSlot(handle, ref declaringType, genericArguments, ref methodName, ref methodSignature, lookForDefaultImplementations, out functionPointer, out genericDictionary, out slotChanged))
                {
                    methodNameAndSignature = new MethodNameAndSignature(methodName, methodSignature);

                    if (!slotChanged)
                        resolution = FunctionPointerOps.GetGenericMethodFunctionPointer(functionPointer, genericDictionary);
                    break;
                }

                eeType = eeType.BaseType;
            }

            // If the current slot to examine has changed, restart the lookup.
            // This happens when there is an interface call.
            if (slotChanged)
            {
                return GVMLookupForSlotWorker(type, declaringType, genericArguments, methodNameAndSignature);
            }

            if (resolution == IntPtr.Zero
                && !lookForDefaultImplementations
                && declaringType.ToEETypePtr().IsInterface)
            {
                lookForDefaultImplementations = true;
                goto again;
            }

            if (resolution == IntPtr.Zero)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Generic virtual method pointer lookup failure.");
                sb.AppendLine();
                sb.AppendLine("Declaring type: " + declaringType.LastResortToString);
                sb.AppendLine("Target type: " + type.LastResortToString);
                sb.AppendLine("Method name: " + methodNameAndSignature.Name);
                sb.AppendLine("Instantiation:");
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    sb.AppendLine("  Argument " + i.LowLevelToString() + ": " + genericArguments[i].LastResortToString);
                }

                Environment.FailFast(sb.ToString());
            }

            return resolution;
        }

        internal static unsafe IntPtr GVMLookupForSlot(RuntimeTypeHandle type, RuntimeMethodHandle slot)
        {
            RuntimeTypeHandle declaringTypeHandle;
            MethodNameAndSignature nameAndSignature;
            RuntimeTypeHandle[] genericMethodArgs;
            if (!RuntimeAugments.TypeLoaderCallbacks.GetRuntimeMethodHandleComponents(slot, out declaringTypeHandle, out nameAndSignature, out genericMethodArgs))
            {
                System.Diagnostics.Debug.Assert(false);
                return IntPtr.Zero;
            }

            return GVMLookupForSlotWorker(type, declaringTypeHandle, genericMethodArgs, nameAndSignature);
        }
    }
}
