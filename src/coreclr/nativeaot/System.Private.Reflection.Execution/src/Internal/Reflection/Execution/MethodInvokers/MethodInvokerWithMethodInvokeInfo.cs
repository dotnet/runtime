// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Reflection;

using global::Internal.Runtime.Augments;
using global::Internal.Reflection.Core.Execution;

using global::Internal.Metadata.NativeFormat;
using System.Reflection.Runtime.General;

namespace Internal.Reflection.Execution.MethodInvokers
{
    internal abstract class MethodInvokerWithMethodInvokeInfo : MethodInvoker
    {
        public MethodInvokerWithMethodInvokeInfo(MethodInvokeInfo methodInvokeInfo)
        {
            MethodInvokeInfo = methodInvokeInfo;
        }

        public override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            return RuntimeAugments.CreateDelegate(
                delegateType,
                MethodInvokeInfo.LdFtnResult,
                target,
                isStatic: isStatic,
                isOpen: isOpen);
        }

        //
        // Creates the appropriate flavor of Invoker depending on the calling convention "shape" (static, instance or virtual.)
        //
        internal static MethodInvoker CreateMethodInvoker(RuntimeTypeHandle declaringTypeHandle, QMethodDefinition methodHandle, MethodInvokeInfo methodInvokeInfo)
        {
            bool isStatic = false;

            if (methodHandle.IsNativeFormatMetadataBased)
            {
                Method method = methodHandle.NativeFormatHandle.GetMethod(methodHandle.NativeFormatReader);
                MethodAttributes methodAttributes = method.Flags;
                if (0 != (methodAttributes & MethodAttributes.Static))
                    isStatic = true;
            }
#if ECMA_METADATA_SUPPORT
            if (methodHandle.IsEcmaFormatMetadataBased)
            {
                var reader = methodHandle.EcmaFormatReader;
                var method = reader.GetMethodDefinition(methodHandle.EcmaFormatHandle);
                var blobReader = reader.GetBlobReader(method.Signature);
                byte sigByte = blobReader.ReadByte();
                if ((sigByte & (byte)System.Reflection.Metadata.SignatureAttributes.Instance) == 0)
                    isStatic = true;
            }
#endif

            if (isStatic)
                return new StaticMethodInvoker(methodInvokeInfo);
            else if (methodInvokeInfo.VirtualResolveData != IntPtr.Zero)
                return new VirtualMethodInvoker(methodInvokeInfo, declaringTypeHandle);
            else
                return new InstanceMethodInvoker(methodInvokeInfo, declaringTypeHandle);
        }

        protected MethodInvokeInfo MethodInvokeInfo { get; private set; }
    }
}
