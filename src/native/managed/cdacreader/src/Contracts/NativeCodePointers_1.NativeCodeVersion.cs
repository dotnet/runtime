// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


internal readonly partial struct NativeCodePointers_1 : INativeCodePointers
{

    internal struct ILCodeVersionHandle
    {
        internal readonly TargetPointer Module;
        internal uint MethodDefinition;
        internal readonly TargetPointer ILCodeVersionNode;
        internal readonly uint RejitId;

        internal ILCodeVersionHandle(TargetPointer module, uint methodDef, TargetPointer ilCodeVersionNodeAddress)
        {
            Module = module;
            MethodDefinition = methodDef;
            ILCodeVersionNode = ilCodeVersionNodeAddress;
            if (Module != TargetPointer.Null && ILCodeVersionNode != TargetPointer.Null)
            {
                throw new ArgumentException("Both MethodDesc and ILCodeVersionNode cannot be non-null");

            }
            if (Module != TargetPointer.Null && MethodDefinition == 0)
            {
                throw new ArgumentException("MethodDefinition must be non-zero if Module is non-null");
            }
        }
        public static ILCodeVersionHandle Invalid => new ILCodeVersionHandle(TargetPointer.Null, 0, TargetPointer.Null);
        public bool IsValid => Module != TargetPointer.Null || ILCodeVersionNode != TargetPointer.Null;
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


        private enum ILCodeVersionKind
        {
            Unknown = 0,
            Explicit = 1, // means Node is set
            Synthetic = 2, // means Module and Token are set
        }
        private static ILCodeVersionHandle ILCodeVersionHandleFromState(Data.ILCodeVersioningState ilState)
        {
            switch ((ILCodeVersionKind)ilState.ActiveVersionKind)
            {
                case ILCodeVersionKind.Explicit:
                    return new ILCodeVersionHandle(module: TargetPointer.Null, methodDef: 0, ilState.ActiveVersionNode);
                case ILCodeVersionKind.Synthetic:
                case ILCodeVersionKind.Unknown:
                    return new ILCodeVersionHandle(ilState.ActiveVersionModule, ilState.ActiveVersionMethodDef, TargetPointer.Null);
                default:
                    throw new InvalidOperationException($"Unknown ILCodeVersionKind {ilState.ActiveVersionKind}");
            }
        }

        public ILCodeVersionHandle FindActiveILCodeVersion(TargetPointer module, uint methodDefinition)
        {
            ModuleHandle moduleHandle = _target.Contracts.Loader.GetModuleHandle(module);
            TargetPointer ilCodeVersionTable = _target.Contracts.Loader.GetLookupTables(moduleHandle).MethodDefToILCodeVersioningState;
            TargetPointer ilVersionStateAddress = _target.Contracts.Loader.GetModuleLookupMapElement(ilCodeVersionTable, methodDefinition, out var _);
            if (ilVersionStateAddress == TargetPointer.Null)
            {
                return new ILCodeVersionHandle(module, methodDefinition, TargetPointer.Null);
            }
            Data.ILCodeVersioningState ilState = _target.ProcessedData.GetOrAdd<Data.ILCodeVersioningState>(ilVersionStateAddress);
            return ILCodeVersionHandleFromState(ilState);
        }

        public NativeCodeVersionHandle FindActiveNativeCodeVersion(ILCodeVersionHandle methodDefActiveVersion)
        {
            throw new NotImplementedException();
        }

    }
}
