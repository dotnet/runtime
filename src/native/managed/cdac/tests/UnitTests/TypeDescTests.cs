// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
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
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Param type - pointing at var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            TargetPointer actualModule = rts.GetModule(handle);
            Assert.Equal(module, actualModule);
        }
        {
            // Function pointer - always null
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
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
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.HasTypeParam(handle);
            Assert.True(res);
            ITypeHandle typeParam = rts.GetTypeParam(handle);
            Assert.Equal(typePointerHandle, typeParam.Address);
            Assert.Equal(typePointerRawAddr, typeParam.TypeDescAddress());
        }
        {
            // Function pointer
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.HasTypeParam(handle);
            Assert.False(res);
        }
        {
            // Type var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
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
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.IsFunctionPointer(handle, out ReadOnlySpan<ITypeHandle> actualRetAndArgTypes, out SignatureCallingConvention actualCallConv);
            Assert.True(res);
            Assert.Equal(callConv, (byte)actualCallConv);
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
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsFunctionPointer(handle, out _, out _);
            Assert.False(res);
        }
        {
            // Type var type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
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
        uint varIndex = 2;
        uint mvarIndex = 3;
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
                varTypeDesc.Index = varIndex;
                varType = varTypeDesc.Address;

                MockTypeVarTypeDesc mvarTypeDesc = rtsBuilder.AddTypeVarTypeDesc();
                mvarTypeDesc.TypeAndFlags = (uint)CorElementType.MVar;
                mvarTypeDesc.Module = module;
                mvarTypeDesc.Token = token;
                mvarTypeDesc.Index = mvarIndex;
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
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(varType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken, out uint actualIndex);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
            Assert.Equal(varIndex, actualIndex);
        }
        {
            // MVar
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(mvarType));
            bool res = rts.IsGenericVariable(handle, out TargetPointer actualModule, out uint actualToken, out uint actualIndex);
            Assert.True(res);
            Assert.Equal(module, actualModule);
            Assert.Equal(token, actualToken);
            Assert.Equal(mvarIndex, actualIndex);
        }
        {
            // Function pointer
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(funcPtr));
            bool res = rts.IsGenericVariable(handle, out _, out _, out _);
            Assert.False(res);
        }
        {
            // Param type
            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle handle = rts.GetTypeHandle(GetTypeDescHandlePointer(paramType));
            bool res = rts.IsGenericVariable(handle, out _, out _, out _);
            Assert.False(res);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetConstructedType_UsesNativeParamHashAndChecksCandidates(MockTarget.Architecture arch)
    {
        foreach (CorElementType kind in new[] { CorElementType.Ptr, CorElementType.Byref })
        {
            TargetPointer module = 0x120000;
            TargetPointer element = default;
            TargetPointer different = default;
            TargetPointer unloaded = default;
            TargetPointer expected = default;
            TestPlaceholderTarget target = MethodTableTests.CreateTarget(arch, builder =>
            {
                MockTypeVarTypeDesc variable = builder.AddTypeVarTypeDesc();
                variable.TypeAndFlags = (uint)CorElementType.Var;
                variable.Module = module;
                element = GetTypeDescHandlePointer(variable.Address);

                MockParamTypeDesc other = builder.AddParamTypeDesc();
                other.TypeAndFlags = (uint)kind;
                other.TypeArg = GetTypeDescHandlePointer(0x2000);
                different = GetTypeDescHandlePointer(other.Address);
                MockParamTypeDesc loading = builder.AddParamTypeDesc();
                loading.TypeAndFlags = (uint)kind | (uint)RuntimeTypeSystem_1.TypeDescFlags.IsNotFullyLoaded;
                loading.TypeArg = element;
                unloaded = GetTypeDescHandlePointer(loading.Address);
                MockParamTypeDesc match = builder.AddParamTypeDesc();
                match.TypeAndFlags = (uint)kind;
                match.TypeArg = element;
                expected = GetTypeDescHandlePointer(match.Address);
            });
            uint hash = unchecked(((5381u * 33) ^ (uint)kind) * 33) ^ (uint)element.Value;
            Contracts.ModuleHandle moduleHandle = new(module);
            Mock<ILoader> loader = new(MockBehavior.Strict);
            loader.Setup(l => l.GetModuleHandleFromModulePtr(module)).Returns(moduleHandle);
            loader.Setup(l => l.GetAvailableTypeParams(moduleHandle, hash)).Returns([different, unloaded, expected]);
            var registry = target.SetupContractRegistry();
            registry.SetVersion<IRuntimeTypeSystem>("c1");
            registry.SetMock(loader.Object);

            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ITypeHandle elementHandle = rts.GetTypeHandle(element);
            Assert.Equal(expected, rts.GetConstructedType(elementHandle, kind, 0, [])?.Address);
            Assert.Equal(expected, rts.GetConstructedType(elementHandle, kind, 0, [])?.Address);
            loader.Verify(l => l.GetAvailableTypeParams(moduleHandle, hash), Times.Once);
        }
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetConstructedArrayType_UsesNativeHashAndChecksRank(MockTarget.Architecture arch)
    {
        TargetPointer module = 0x120000;
        TargetPointer element = default;
        TargetPointer rank1 = default;
        TargetPointer rank4 = default;
        TargetPointer szArray = default;
        TestPlaceholderTarget target = MethodTableTests.CreateTarget(arch, builder =>
        {
            MockTypeVarTypeDesc variable = builder.AddTypeVarTypeDesc();
            variable.TypeAndFlags = (uint)CorElementType.Var;
            variable.Module = module;
            element = GetTypeDescHandlePointer(variable.Address);
            rank1 = AddArray(1, false);
            rank4 = AddArray(4, false);
            szArray = AddArray(1, true);

            TargetPointer AddArray(uint rank, bool sz)
            {
                MockEEClass eeClass = builder.AddEEClass("Array");
                MockMethodTable mt = builder.AddMethodTable("Array");
                mt.MTFlags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array);
                if (sz)
                    mt.MTFlags |= (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;
                mt.BaseSize = builder.Builder.TargetTestHelpers.ArrayBaseBaseSize + (sz ? 0 : rank * sizeof(uint) * 2);
                mt.EEClassOrCanonMT = eeClass.Address;
                eeClass.MethodTable = mt.Address;
                mt.PerInstInfo = element;
                MockMethodTableAuxiliaryData auxiliary = builder.AddMethodTableAuxiliaryData();
                mt.AuxiliaryData = auxiliary.Address;
                return mt.Address;
            }
        });
        Contracts.ModuleHandle moduleHandle = new(module);
        uint arrayHash = unchecked(((5381u * 33) ^ (uint)CorElementType.Array) * 33) ^ (uint)element.Value;
        uint szArrayHash = unchecked(((5381u * 33) ^ (uint)CorElementType.SzArray) * 33) ^ (uint)element.Value;
        Mock<ILoader> loader = new(MockBehavior.Strict);
        loader.Setup(l => l.GetModuleHandleFromModulePtr(module)).Returns(moduleHandle);
        loader.Setup(l => l.GetAvailableTypeParams(moduleHandle, arrayHash)).Returns([szArray, rank1, rank4]);
        loader.Setup(l => l.GetAvailableTypeParams(moduleHandle, szArrayHash)).Returns([rank4, szArray]);
        var registry = target.SetupContractRegistry();
        registry.SetVersion<IRuntimeTypeSystem>("c1");
        registry.SetMock(loader.Object);

        IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
        ITypeHandle elementHandle = rts.GetTypeHandle(element);
        Assert.Equal(rank4, rts.GetConstructedType(elementHandle, CorElementType.Array, 4, [])?.Address);
        Assert.Equal(rank1, rts.GetConstructedType(elementHandle, CorElementType.Array, 1, [])?.Address);
        Assert.Null(rts.GetConstructedType(elementHandle, CorElementType.Array, 2, []));
        Assert.Equal(szArray, rts.GetConstructedType(elementHandle, CorElementType.SzArray, 1, [])?.Address);
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void GetConstructedType_UsesNativeFunctionPointerHash(MockTarget.Architecture arch)
    {
        foreach (SignatureCallingConvention callConv in new[] { SignatureCallingConvention.Default, SignatureCallingConvention.CDecl })
        {
            TargetPointer module = 0x120000;
            TargetPointer returnType = default;
            TargetPointer argType = default;
            TargetPointer different = default;
            TargetPointer expected = default;
            TestPlaceholderTarget target = MethodTableTests.CreateTarget(arch, builder =>
            {
                MockTypeVarTypeDesc ret = builder.AddTypeVarTypeDesc();
                ret.TypeAndFlags = (uint)CorElementType.Var;
                ret.Module = module;
                returnType = GetTypeDescHandlePointer(ret.Address);
                MockTypeVarTypeDesc arg = builder.AddTypeVarTypeDesc();
                arg.TypeAndFlags = (uint)CorElementType.Var;
                arg.Module = module;
                argType = GetTypeDescHandlePointer(arg.Address);
                MockFnPtrTypeDesc other = AddFunctionPointer(callConv == SignatureCallingConvention.Default ? SignatureCallingConvention.CDecl : SignatureCallingConvention.Default);
                different = GetTypeDescHandlePointer(other.Address);
                MockFnPtrTypeDesc match = AddFunctionPointer(callConv);
                expected = GetTypeDescHandlePointer(match.Address);

                MockFnPtrTypeDesc AddFunctionPointer(SignatureCallingConvention convention)
                {
                    MockFnPtrTypeDesc desc = builder.AddFunctionPointerTypeDesc(2);
                    desc.TypeAndFlags = (uint)CorElementType.FnPtr;
                    desc.NumArgs = 1;
                    desc.CallConv = (uint)convention;
                    desc.LoaderModule = module;
                    desc[0] = returnType;
                    desc[1] = argType;
                    return desc;
                }
            });
            uint hash = unchecked((5381u * 33) ^ (uint)CorElementType.FnPtr);
            hash = unchecked(hash * 33) ^ (uint)callConv;
            hash = unchecked(hash * 33) ^ 1;
            hash = unchecked(hash * 33) ^ (uint)returnType.Value;
            hash = unchecked(hash * 33) ^ (uint)argType.Value;
            Contracts.ModuleHandle moduleHandle = new(module);
            Mock<ILoader> loader = new(MockBehavior.Strict);
            loader.Setup(l => l.GetModuleHandleFromModulePtr(module)).Returns(moduleHandle);
            loader.Setup(l => l.GetLoaderAllocator(moduleHandle)).Returns(TargetPointer.Null);
            loader.Setup(l => l.GetAvailableTypeParams(moduleHandle, hash)).Returns([different, expected]);
            var registry = target.SetupContractRegistry();
            registry.SetVersion<IRuntimeTypeSystem>("c1");
            registry.SetMock(loader.Object);

            IRuntimeTypeSystem rts = target.Contracts.RuntimeTypeSystem;
            ImmutableArray<ITypeHandle?> arguments = [rts.GetTypeHandle(returnType), rts.GetTypeHandle(argType)];
            Assert.Equal(expected, rts.GetConstructedType(null, CorElementType.FnPtr, 0, arguments, callConv)?.Address);
            Assert.Equal(expected, rts.GetConstructedType(null, CorElementType.FnPtr, 0, arguments, callConv)?.Address);
            loader.Verify(l => l.GetAvailableTypeParams(moduleHandle, hash), Times.Once);
        }
    }

    private static TargetPointer GetTypeDescHandlePointer(TargetPointer addr)
        => addr | (ulong)RuntimeTypeSystem_1.TypeHandleBits.TypeDesc;
}
