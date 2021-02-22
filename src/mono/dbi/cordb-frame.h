// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: CORDB-FRAME.H
//

#ifndef __MONO_DEBUGGER_CORDB_FRAME_H__
#define __MONO_DEBUGGER_CORDB_FRAME_H__

#include <cordb.h>

class CordbJITILFrame : public CordbBaseMono,
                        public ICorDebugILFrame,
                        public ICorDebugILFrame2,
                        public ICorDebugILFrame3,
                        public ICorDebugILFrame4
{
    int          m_debuggerFrameId;
    int          m_debuggerMethodId;
    int          m_ilOffset;
    int          m_flags;
    CordbThread* m_pThread;

public:
    CordbJITILFrame(Connection* conn, int frameid, int methodId, int il_offset, int flags, CordbThread* thread);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbJITILFrame";
    }
    HRESULT GetChain(ICorDebugChain** ppChain);
    HRESULT GetCode(ICorDebugCode** ppCode);
    HRESULT
    GetFunction(ICorDebugFunction** ppFunction);
    HRESULT GetFunctionToken(mdMethodDef* pToken);
    HRESULT GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT GetCaller(ICorDebugFrame** ppFrame);
    HRESULT GetCallee(ICorDebugFrame** ppFrame);
    HRESULT
    CreateStepper(ICorDebugStepper** ppStepper);
    HRESULT
    QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT
    GetIP(ULONG32* pnOffset, CorDebugMappingResult* pMappingResult);
    HRESULT SetIP(ULONG32 nOffset);
    HRESULT
    EnumerateLocalVariables(ICorDebugValueEnum** ppValueEnum);
    HRESULT GetLocalVariable(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT
    EnumerateArguments(ICorDebugValueEnum** ppValueEnum);
    HRESULT GetArgument(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT GetStackDepth(ULONG32* pDepth);
    HRESULT GetStackValue(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT CanSetIP(ULONG32 nOffset);
    HRESULT RemapFunction(ULONG32 newILOffset);
    HRESULT
    EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum);
    HRESULT
    GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue** ppReturnValue);
    HRESULT
    EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum** ppValueEnum);
    HRESULT GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT GetCodeEx(ILCodeKind flags, ICorDebugCode** ppCode);
};

class CordbNativeFrame : public CordbBaseMono, public ICorDebugNativeFrame, public ICorDebugNativeFrame2
{
    CordbJITILFrame* m_JITILFrame;
    CordbThread*     m_pThread;

public:
    CordbNativeFrame(Connection* conn, int frameid, int methodId, int il_offset, int flags, CordbThread* thread);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbNativeFrame";
    }
    ~CordbNativeFrame();
    HRESULT GetIP(ULONG32* pnOffset);
    HRESULT SetIP(ULONG32 nOffset);
    HRESULT GetRegisterSet(ICorDebugRegisterSet** ppRegisters);
    HRESULT GetLocalRegisterValue(CorDebugRegister reg,
                                  ULONG            cbSigBlob,
                                  PCCOR_SIGNATURE  pvSigBlob,
                                  ICorDebugValue** ppValue);
    HRESULT GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                        CorDebugRegister lowWordReg,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT GetLocalMemoryValue(CORDB_ADDRESS    address,
                                ULONG            cbSigBlob,
                                PCCOR_SIGNATURE  pvSigBlob,
                                ICorDebugValue** ppValue);
    HRESULT GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                        CORDB_ADDRESS    lowWordAddress,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT GetLocalMemoryRegisterValue(CORDB_ADDRESS    highWordAddress,
                                        CorDebugRegister lowWordRegister,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT CanSetIP(ULONG32 nOffset);
    HRESULT GetChain(ICorDebugChain** ppChain);
    HRESULT GetCode(ICorDebugCode** ppCode);
    HRESULT GetFunction(ICorDebugFunction** ppFunction);
    HRESULT GetFunctionToken(mdMethodDef* pToken);
    HRESULT GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT GetCaller(ICorDebugFrame** ppFrame);
    HRESULT GetCallee(ICorDebugFrame** ppFrame);
    HRESULT CreateStepper(ICorDebugStepper** ppStepper);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);

    HRESULT IsChild(BOOL* pIsChild);
    HRESULT IsMatchingParentFrame(ICorDebugNativeFrame2* pPotentialParentFrame, BOOL* pIsParent);
    HRESULT GetStackParameterSize(ULONG32* pSize);
};

class CordbFrameEnum : public CordbBaseMono, public ICorDebugFrameEnum
{
    CordbThread*       m_pThread;
    int                m_nFrames;
    CordbNativeFrame** m_ppFrames;

public:
    CordbFrameEnum(Connection* conn, CordbThread* thread);
    ULONG AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbFrameEnum";
    }
    ~CordbFrameEnum();
    HRESULT Next(ULONG celt, ICorDebugFrame* frames[], ULONG* pceltFetched);
    HRESULT Skip(ULONG celt);
    HRESULT Reset(void);
    HRESULT Clone(ICorDebugEnum** ppEnum);
    HRESULT GetCount(ULONG* pcelt);
    HRESULT QueryInterface(REFIID riid, void** ppvObject);
};

#endif
