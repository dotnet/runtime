// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Moq;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class TypeDescTests
{
    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetModule(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);

        // Arbitrary module value
        TargetPointer module = 0xa000;
        TargetPointer varType = rtsBuilder.AddTypeVarTypeDesc((uint)CorElementType.Var, module, 0);
        TargetPointer typePointerHandle = GetTypeDescHandlePointer(varType);
        TargetPointer paramType = rtsBuilder.AddParamTypeDesc((uint)CorElementType.ValueType, typePointerHandle);
        TargetPointer funcPtr = rtsBuilder.AddFunctionPointerTypeDesc(0, [GetTypeDescHandlePointer(0x1000)], module);

        Target target = CreateTarget(rtsBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            // Type var type
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Param type - pointing at var type
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Function pointer - always null
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(TargetPointer.Null, actualModule);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetTypeParam(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);

        // Param types - arbitrary type pointer value
        TargetPointer typePointerRawAddr = 0xa000;
        TargetPointer typePointerHandle = GetTypeDescHandlePointer(typePointerRawAddr);
        TargetPointer paramType = rtsBuilder.AddParamTypeDesc((uint)CorElementType.ValueType, typePointerHandle);

        // No type param
        TargetPointer funcPtr = rtsBuilder.AddFunctionPointerTypeDesc(0, [GetTypeDescHandlePointer(0x1000)], TargetPointer.Null);
        TargetPointer varType = rtsBuilder.AddTypeVarTypeDesc((uint)CorElementType.Var, TargetPointer.Null, 0);

        Target target = CreateTarget(rtsBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.HasTypeParam(handle);
            Assert.True(res);
            TypeHandle typeParam = rts.GetTypeParam(handle);
            Assert.Equal(typePointerHandle, typeParam.Address);
            Assert.Equal(typePointerRawAddr, typeParam.TypeDescAddress());
        }
        {
            // Function pointer
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.HasTypeParam(handle);
            Assert.False(res);
        }
        {
            // Type var type
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.HasTypeParam(handle);
            Assert.False(res);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsFunctionPointer(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);

        // Function pointer - arbitrary callconv, types, and module values
        byte callConv = 0x9;
        TargetPointer[] retAndArgTypesRawAddr = [0x1000, 0x2000, 0x3000];
        TargetPointer[] retAndArgTypesHandle = retAndArgTypesRawAddr.Select(a => GetTypeDescHandlePointer(a)).ToArray();
        TargetPointer loaderModule = 0xcccc0000;
        TargetPointer funcPtr = rtsBuilder.AddFunctionPointerTypeDesc(callConv, retAndArgTypesHandle, loaderModule);

        // Non-function pointers
        TargetPointer paramType = rtsBuilder.AddParamTypeDesc((uint)CorElementType.ValueType, TargetPointer.Null);
        TargetPointer varType = rtsBuilder.AddTypeVarTypeDesc((uint)CorElementType.Var, TargetPointer.Null, 0);

        Target target = CreateTarget(rtsBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            // Function pointer
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
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsFunctionPointer(handle, out _, out _);
            Assert.False(res);
        }
        {
            // Type var type
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.IsFunctionPointer(handle, out _, out _);
            Assert.False(res);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void IsGenericVariable(MockTarget.Architecture arch)
    {
        TargetTestHelpers helpers = new(arch);
        MockMemorySpace.Builder builder = new(helpers);
        MockDescriptors.RuntimeTypeSystem rtsBuilder = new(builder);

        // Type var types - arbitrary module and token values;
        TargetPointer module = 0x1000;
        uint token = 0xa;
        TargetPointer varType = rtsBuilder.AddTypeVarTypeDesc((uint)CorElementType.Var, module, token);
        TargetPointer mvarType = rtsBuilder.AddTypeVarTypeDesc((uint)CorElementType.MVar, module, token);

        // Non-generic variables
        TargetPointer paramType = rtsBuilder.AddParamTypeDesc((uint)CorElementType.ValueType, TargetPointer.Null);
        TargetPointer funcPtr = rtsBuilder.AddFunctionPointerTypeDesc(0, [GetTypeDescHandlePointer(0x1000)], TargetPointer.Null);

        Target target = CreateTarget(rtsBuilder);
        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;

        {
            // Var
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
        }
        {
            // MVar
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(mvarType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
        }
        {
            // Function pointer
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.IsGenericVariable(handle, out _, out _);
            Assert.False(res);
        }
        {
            // Param type
            TypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsGenericVariable(handle, out _, out _);
            Assert.False(res);
        }
    }

    private static Target CreateTarget(MockDescriptors.RuntimeTypeSystem rtsBuilder)
    {
        var target = new TestPlaceholderTarget(rtsBuilder.Builder.TargetTestHelpers.Arch, rtsBuilder.Builder.GetMemoryContext().ReadFromTarget, rtsBuilder.Types, rtsBuilder.Globals);
        target.SetContracts(Mock.Of<ContractRegistry>(
            c => c.RuntimeTypeSystem == ((IContractFactory<IRuntimeTypeSystem>)new RuntimeTypeSystemFactory()).CreateContract(target, 1)));
        return target;
    }

    private static TargetPointer GetTypeDescHandlePointer(TargetPointer addr)
        => addr | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
}
