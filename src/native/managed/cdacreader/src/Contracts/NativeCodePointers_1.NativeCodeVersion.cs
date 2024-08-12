// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{

    internal struct ILCodeVersionHandle
    {
        internal readonly TargetPointer MethodDesc;
        internal readonly TargetPointer ILCodeVersionNode;

        internal ILCodeVersionHandle(TargetPointer methodDescAddress, TargetPointer ilCodeVersionNodeAddress)
        {
            MethodDesc = methodDescAddress;
            ILCodeVersionNode = ilCodeVersionNodeAddress;
            if (MethodDesc != TargetPointer.Null && ILCodeVersionNode != TargetPointer.Null)
            {
                throw new ArgumentException("Both MethodDesc and ILCodeVersionNode cannot be non-null");

            }
        }
        public static ILCodeVersionHandle Invalid => new ILCodeVersionHandle(TargetPointer.Null, TargetPointer.Null);
        public bool IsValid => MethodDesc != TargetPointer.Null || ILCodeVersionNode != TargetPointer.Null;
    }

    internal struct NativeCodeVersionContract
    {
        private readonly Target _target;

        public NativeCodeVersionContract(Target target)
        {
            _target = target;
        }

        public NativeCodeVersionHandle GetSpecificNativeCodeVersion(IRuntimeTypeSystem rts, MethodDescHandle md, TargetCodePointer startAddress)
        {
            TargetPointer methodDescVersioningStateAddress = rts.GetMethodDescVersioningState(md);
            if (methodDescVersioningStateAddress == TargetPointer.Null)
            {
                return NativeCodeVersionHandle.Invalid;
            }
            Data.MethodDescVersioningState methodDescVersioningStateData = _target.ProcessedData.GetOrAdd<Data.MethodDescVersioningState>(methodDescVersioningStateAddress);
            // CodeVersionManager::GetNativeCodeVersion(PTR_MethodDesc, PCODE startAddress)
            return FindFirstCodeVersion(methodDescVersioningStateData, (codeVersion) =>
            {
                return codeVersion.MethodDesc == md.Address && codeVersion.NativeCode == startAddress;
            });
        }

        private NativeCodeVersionHandle FindFirstCodeVersion(Data.MethodDescVersioningState versioningState, Func<Data.NativeCodeVersionNode, bool> predicate)
        {
            // NativeCodeVersion::Next, heavily inlined
            TargetPointer currentAddress = versioningState.NativeCodeVersionNode;
            while (currentAddress != TargetPointer.Null)
            {
                Data.NativeCodeVersionNode current = _target.ProcessedData.GetOrAdd<Data.NativeCodeVersionNode>(currentAddress);
                if (predicate(current))
                {
                    return new NativeCodeVersionHandle(methodDescAddress: TargetPointer.Null, currentAddress);
                }
                currentAddress = current.Next;
            }
            return NativeCodeVersionHandle.Invalid;
        }

        public ILCodeVersionHandle FindActiveILCodeVersion(TargetPointer module, uint methodDefinition)
        {
            //TODO[cdac]: implement FindActiveILCodeVersion
#if false
            ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
            TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
            TargetPointer ilNode = _target.Contracts.GetModuleLookupTableElement(module, methodDefinition, out var _);
            if (ilNode == TargetPointer.Null)
            {
                return ILCodeVersionHandle.Invalid;
            }
#endif

            throw new NotImplementedException();
        }

        public NativeCodeVersionHandle FindActiveNativeCodeVersion(ILCodeVersionHandle methodDefActiveVersion)
        {
            throw new NotImplementedException();
        }

    }
}
