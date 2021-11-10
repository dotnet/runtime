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
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbJITILFrame";
    }
    HRESULT STDMETHODCALLTYPE GetChain(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetCode(ICorDebugCode** ppCode);
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetFunctionToken(mdMethodDef* pToken);
    HRESULT STDMETHODCALLTYPE GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT STDMETHODCALLTYPE GetCaller(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE GetCallee(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE CreateStepper(ICorDebugStepper** ppStepper);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID id, _COM_Outptr_ void __RPC_FAR* __RPC_FAR* pInterface);

    HRESULT STDMETHODCALLTYPE GetIP(ULONG32* pnOffset, CorDebugMappingResult* pMappingResult);
    HRESULT STDMETHODCALLTYPE SetIP(ULONG32 nOffset);
    HRESULT STDMETHODCALLTYPE EnumerateLocalVariables(ICorDebugValueEnum** ppValueEnum);
    HRESULT STDMETHODCALLTYPE GetLocalVariable(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE EnumerateArguments(ICorDebugValueEnum** ppValueEnum);
    HRESULT STDMETHODCALLTYPE GetArgument(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetStackDepth(ULONG32* pDepth);
    HRESULT STDMETHODCALLTYPE GetStackValue(DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE CanSetIP(ULONG32 nOffset);
    HRESULT STDMETHODCALLTYPE RemapFunction(ULONG32 newILOffset);
    HRESULT STDMETHODCALLTYPE EnumerateTypeParameters(ICorDebugTypeEnum** ppTyParEnum);
    HRESULT STDMETHODCALLTYPE GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue** ppReturnValue);
    HRESULT STDMETHODCALLTYPE EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum** ppValueEnum);
    HRESULT STDMETHODCALLTYPE GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex, ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetCodeEx(ILCodeKind flags, ICorDebugCode** ppCode);
};

class CordbNativeFrame : public CordbBaseMono, public ICorDebugNativeFrame, public ICorDebugNativeFrame2
{
    CordbJITILFrame* m_JITILFrame;
    CordbThread*     m_pThread;
    int              m_nPosFrame;

public:
    CordbNativeFrame(Connection* conn, int frameid, int methodId, int il_offset, int flags, CordbThread* thread, int posFrame);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbNativeFrame";
    }
    ~CordbNativeFrame();
    HRESULT STDMETHODCALLTYPE GetIP(ULONG32* pnOffset);
    HRESULT STDMETHODCALLTYPE SetIP(ULONG32 nOffset);
    HRESULT STDMETHODCALLTYPE GetRegisterSet(ICorDebugRegisterSet** ppRegisters);
    HRESULT STDMETHODCALLTYPE GetLocalRegisterValue(CorDebugRegister reg,
                                  ULONG            cbSigBlob,
                                  PCCOR_SIGNATURE  pvSigBlob,
                                  ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetLocalDoubleRegisterValue(CorDebugRegister highWordReg,
                                        CorDebugRegister lowWordReg,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetLocalMemoryValue(CORDB_ADDRESS    address,
                                ULONG            cbSigBlob,
                                PCCOR_SIGNATURE  pvSigBlob,
                                ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetLocalRegisterMemoryValue(CorDebugRegister highWordReg,
                                        CORDB_ADDRESS    lowWordAddress,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE GetLocalMemoryRegisterValue(CORDB_ADDRESS    highWordAddress,
                                        CorDebugRegister lowWordRegister,
                                        ULONG            cbSigBlob,
                                        PCCOR_SIGNATURE  pvSigBlob,
                                        ICorDebugValue** ppValue);
    HRESULT STDMETHODCALLTYPE CanSetIP(ULONG32 nOffset);
    HRESULT STDMETHODCALLTYPE GetChain(ICorDebugChain** ppChain);
    HRESULT STDMETHODCALLTYPE GetCode(ICorDebugCode** ppCode);
    HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction** ppFunction);
    HRESULT STDMETHODCALLTYPE GetFunctionToken(mdMethodDef* pToken);
    HRESULT STDMETHODCALLTYPE GetStackRange(CORDB_ADDRESS* pStart, CORDB_ADDRESS* pEnd);
    HRESULT STDMETHODCALLTYPE GetCaller(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE GetCallee(ICorDebugFrame** ppFrame);
    HRESULT STDMETHODCALLTYPE CreateStepper(ICorDebugStepper** ppStepper);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);

    HRESULT STDMETHODCALLTYPE IsChild(BOOL* pIsChild);
    HRESULT STDMETHODCALLTYPE IsMatchingParentFrame(ICorDebugNativeFrame2* pPotentialParentFrame, BOOL* pIsParent);
    HRESULT STDMETHODCALLTYPE GetStackParameterSize(ULONG32* pSize);
};

class CordbFrameEnum : public CordbBaseMono, public ICorDebugFrameEnum
{
    CordbThread*       m_pThread;
    int                m_nFrames;
    CordbNativeFrame** m_ppFrames;

public:
    CordbFrameEnum(Connection* conn, CordbThread* thread);
    ULONG STDMETHODCALLTYPE AddRef(void)
    {
        return (BaseAddRef());
    }
    ULONG STDMETHODCALLTYPE Release(void)
    {
        return (BaseRelease());
    }
    const char* GetClassName()
    {
        return "CordbFrameEnum";
    }
    ~CordbFrameEnum();
    HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugFrame* frames[], ULONG* pceltFetched);
    HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
    HRESULT STDMETHODCALLTYPE Reset(void);
    HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum** ppEnum);
    HRESULT STDMETHODCALLTYPE GetCount(ULONG* pcelt);
    HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject);
    
    HRESULT GetCount();
};

#endif
