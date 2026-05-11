// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class TypeDescTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetModule(MockTarget.Architecture arch)
    {
        TargetPointer module = 0xa000;
        TargetPointer varType = TargetPointer.Null;
        TargetPointer typePointerHandle = TargetPointer.Null;
        TargetPointer paramType = TargetPointer.Null;
        TargetPointer funcPtr = TargetPointer.Null;

        TestPlaceholderTarget target = MethodTableTests.CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockTypeVarTypeDesc varTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                varTypeDesc.TypeAndFlags = (uint)CorElementType.Var;
                varTypeDesc.Module = module;
                varType = varTypeDesc.Address;
                typePointerHandle = GetTypeDescHandlePointer(varType);
                MockParamTypeDesc paramTypeDesc = rtsBuilder.AddParamTypeDesc();
                paramTypeDesc.TypeAndFlags = (uint)CorElementType.ValueType;
                paramTypeDesc.TypeArg = typePointerHandle;
                paramType = paramTypeDesc.Address;

                MockFnPtrTypeDesc fnPtrTypeDesc = rtsBuilder.AddFunctionPointerTypeDesc(1);
                fnPtrTypeDesc.TypeAndFlags = (uint)CorElementType.FnPtr;
                fnPtrTypeDesc.LoaderModule = module;
                fnPtrTypeDesc[0] = GetTypeDescHandlePointer(0x1000);
                funcPtr = fnPtrTypeDesc.Address;
            });

        {
            // Type var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Param type - pointing at var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Function pointer - always null
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(TargetPointer.Null, actualModule);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTypeParam(MockTarget.Architecture arch)
    {
        TargetPointer typePointerRawAddr = 0xa000;
        TargetPointer typePointerHandle = GetTypeDescHandlePointer(typePointerRawAddr);
        TargetPointer paramType = TargetPointer.Null;
        TargetPointer funcPtr = TargetPointer.Null;
        TargetPointer varType = TargetPointer.Null;

        TestPlaceholderTarget target = MethodTableTests.CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockParamTypeDesc paramTypeDesc = rtsBuilder.AddParamTypeDesc();
                paramTypeDesc.TypeAndFlags = (uint)CorElementType.ValueType;
                paramTypeDesc.TypeArg = typePointerHandle;
                paramType = paramTypeDesc.Address;

                MockFnPtrTypeDesc fnPtrTypeDesc = rtsBuilder.AddFunctionPointerTypeDesc(1);
                fnPtrTypeDesc.TypeAndFlags = (uint)CorElementType.FnPtr;
                fnPtrTypeDesc[0] = GetTypeDescHandlePointer(0x1000);
                funcPtr = fnPtrTypeDesc.Address;

                MockTypeVarTypeDesc varTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                varTypeDesc.TypeAndFlags = (uint)CorElementType.Var;
                varType = varTypeDesc.Address;
            });

        {
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.HasTypeParam(handle);
            Assert.True(res);
            TypeHandle typeParam = rts.GetTypeParam(handle);
            Assert.Equal(typePointerHandle, typeParam.Address);
            Assert.Equal(typePointerRawAddr, typeParam.TypeDescAddress());
        }
        {
            // Function pointer
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.HasTypeParam(handle);
            Assert.False(res);
        }
        {
            // Type var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.HasTypeParam(handle);
            Assert.False(res);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsFunctionPointer(MockTarget.Architecture arch)
    {
        byte callConv = 0x9;
        TargetPointer[] retAndArgTypesRawAddr = [0x1000, 0x2000, 0x3000];
        TargetPointer[] retAndArgTypesHandle = retAndArgTypesRawAddr.Select(a => GetTypeDescHandlePointer(a)).ToArray();
        TargetPointer loaderModule = 0xcccc0000;
        TargetPointer funcPtr = TargetPointer.Null;
        TargetPointer paramType = TargetPointer.Null;
        TargetPointer varType = TargetPointer.Null;

        TestPlaceholderTarget target = MethodTableTests.CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockFnPtrTypeDesc fnPtrTypeDesc = rtsBuilder.AddFunctionPointerTypeDesc(retAndArgTypesHandle.Length);
                fnPtrTypeDesc.TypeAndFlags = (uint)CorElementType.FnPtr;
                fnPtrTypeDesc.NumArgs = (uint)(retAndArgTypesHandle.Length - 1);
                fnPtrTypeDesc.CallConv = callConv;
                fnPtrTypeDesc.LoaderModule = loaderModule;
                for (int i = 0; i < retAndArgTypesHandle.Length; i++)
                {
                    fnPtrTypeDesc[i] = retAndArgTypesHandle[i];
                }
                funcPtr = fnPtrTypeDesc.Address;

                MockParamTypeDesc paramTypeDesc = rtsBuilder.AddParamTypeDesc();
                paramTypeDesc.TypeAndFlags = (uint)CorElementType.ValueType;
                paramType = paramTypeDesc.Address;

                MockTypeVarTypeDesc varTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                varTypeDesc.TypeAndFlags = (uint)CorElementType.Var;
                varType = varTypeDesc.Address;
            });

        {
            // Function pointer
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.IsFunctionPointer(handle, out ReadOnlySpan<TypeHandle> actualRetAndArgTypes, out byte actualCallConv);
            Assert.True(res);
            Assert.Equal(callConv, actualCallConv);
            Assert.Equal(retAndArgTypesHandle.Length, actualRetAndArgTypes.Length);
            for (int i = 0; i < retAndArgTypesHandle.Length; i++)
            {
                Assert.Equal(retAndArgTypesHandle[i], actualRetAndArgTypes[i].Address);
                Assert.Equal(retAndArgTypesRawAddr[i], actualRetAndArgTypes[i].TypeDescAddress());
            }
        }
        {
            // Param type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsFunctionPointer(handle, out _, out _);
            Assert.False(res);
        }
        {
            // Type var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.IsFunctionPointer(handle, out _, out _);
            Assert.False(res);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsGenericVariable(MockTarget.Architecture arch)
    {
        TargetPointer module = 0x1000;
        uint token = 0xa;
        TargetPointer varType = TargetPointer.Null;
        TargetPointer mvarType = TargetPointer.Null;
        TargetPointer paramType = TargetPointer.Null;
        TargetPointer funcPtr = TargetPointer.Null;

        TestPlaceholderTarget target = MethodTableTests.CreateTarget(
            arch,
            rtsBuilder =>
            {
                MockTypeVarTypeDesc varTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                varTypeDesc.TypeAndFlags = (uint)CorElementType.Var;
                varTypeDesc.Module = module;
                varTypeDesc.Token = token;
                varType = varTypeDesc.Address;

                MockTypeVarTypeDesc mvarTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                mvarTypeDesc.TypeAndFlags = (uint)CorElementType.MVar;
                mvarTypeDesc.Module = module;
                mvarTypeDesc.Token = token;
                mvarType = mvarTypeDesc.Address;

                MockParamTypeDesc paramTypeDesc = rtsBuilder.AddParamTypeDesc();
                paramTypeDesc.TypeAndFlags = (uint)CorElementType.ValueType;
                paramType = paramTypeDesc.Address;

                MockFnPtrTypeDesc fnPtrTypeDesc = rtsBuilder.AddFunctionPointerTypeDesc(1);
                fnPtrTypeDesc.TypeAndFlags = (uint)CorElementType.FnPtr;
                fnPtrTypeDesc[0] = GetTypeDescHandlePointer(0x1000);
                funcPtr = fnPtrTypeDesc.Address;
            });

        {
            // Var
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
        }
        {
            // MVar
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(mvarType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
        }
        {
            // Function pointer
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.IsGenericVariable(handle, out _, out _);
            Assert.False(res);
        }
        {
            // Param type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsGenericVariable(handle, out _, out _);
            Assert.False(res);
        }
    }

    private static TargetPointer GetTypeDescHandlePointer(TargetPointer addr)
        => addr | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
}
