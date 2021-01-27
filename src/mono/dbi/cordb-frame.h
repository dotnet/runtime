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
                        public ICorDebugILFrame4 {
public:
  int frameid;
  int methodId;
  int il_offset;
  int flags;
  CordbThread *thread;
  CordbJITILFrame(Connection *conn, int frameid, int methodId, int il_offset,
                  int flags, CordbThread *thread);
  HRESULT STDMETHODCALLTYPE GetChain(/* [out] */ ICorDebugChain **ppChain);
  HRESULT STDMETHODCALLTYPE GetCode(/* [out] */ ICorDebugCode **ppCode);
  HRESULT STDMETHODCALLTYPE
  GetFunction(/* [out] */ ICorDebugFunction **ppFunction);
  HRESULT STDMETHODCALLTYPE GetFunctionToken(/* [out] */ mdMethodDef *pToken);
  HRESULT STDMETHODCALLTYPE GetStackRange(/* [out] */ CORDB_ADDRESS *pStart,
                                          /* [out] */ CORDB_ADDRESS *pEnd);
  HRESULT STDMETHODCALLTYPE GetCaller(/* [out] */ ICorDebugFrame **ppFrame);
  HRESULT STDMETHODCALLTYPE GetCallee(/* [out] */ ICorDebugFrame **ppFrame);
  HRESULT STDMETHODCALLTYPE
  CreateStepper(/* [out] */ ICorDebugStepper **ppStepper);
  HRESULT STDMETHODCALLTYPE
  QueryInterface(/* [in] */ REFIID id, /* [iid_is][out] */
                 _COM_Outptr_ void __RPC_FAR *__RPC_FAR *pInterface);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE
  GetIP(/* [out] */ ULONG32 *pnOffset,
        /* [out] */ CorDebugMappingResult *pMappingResult);
  HRESULT STDMETHODCALLTYPE SetIP(/* [in] */ ULONG32 nOffset);
  HRESULT STDMETHODCALLTYPE
  EnumerateLocalVariables(/* [out] */ ICorDebugValueEnum **ppValueEnum);
  HRESULT STDMETHODCALLTYPE GetLocalVariable(
      /* [in] */ DWORD dwIndex, /* [out] */ ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE
  EnumerateArguments(/* [out] */ ICorDebugValueEnum **ppValueEnum);
  HRESULT STDMETHODCALLTYPE GetArgument(/* [in] */ DWORD dwIndex,
                                        /* [out] */ ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetStackDepth(/* [out] */ ULONG32 *pDepth);
  HRESULT STDMETHODCALLTYPE GetStackValue(/* [in] */ DWORD dwIndex,
                                          /* [out] */ ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE CanSetIP(/* [in] */ ULONG32 nOffset);
  HRESULT STDMETHODCALLTYPE RemapFunction(ULONG32 newILOffset);
  HRESULT STDMETHODCALLTYPE
  EnumerateTypeParameters(ICorDebugTypeEnum **ppTyParEnum);
  HRESULT STDMETHODCALLTYPE
  GetReturnValueForILOffset(ULONG32 ILoffset, ICorDebugValue **ppReturnValue);
  HRESULT STDMETHODCALLTYPE
  EnumerateLocalVariablesEx(ILCodeKind flags, ICorDebugValueEnum **ppValueEnum);
  HRESULT STDMETHODCALLTYPE GetLocalVariableEx(ILCodeKind flags, DWORD dwIndex,
                                               ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetCodeEx(ILCodeKind flags, ICorDebugCode **ppCode);
};

class CordbNativeFrame : public CordbBaseMono,
                         public ICorDebugNativeFrame,
                         public ICorDebugNativeFrame2 {
  CordbJITILFrame *m_JITILFrame;

public:
  CordbThread *thread;
  CordbNativeFrame(Connection *conn, int frameid, int methodId, int il_offset,
                   int flags, CordbThread *thread);
  HRESULT STDMETHODCALLTYPE GetIP(ULONG32 *pnOffset);
  HRESULT STDMETHODCALLTYPE SetIP(ULONG32 nOffset);
  HRESULT STDMETHODCALLTYPE GetRegisterSet(ICorDebugRegisterSet **ppRegisters);
  HRESULT STDMETHODCALLTYPE GetLocalRegisterValue(CorDebugRegister reg,
                                                  ULONG cbSigBlob,
                                                  PCCOR_SIGNATURE pvSigBlob,
                                                  ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetLocalDoubleRegisterValue(
      CorDebugRegister highWordReg, CorDebugRegister lowWordReg,
      ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetLocalMemoryValue(CORDB_ADDRESS address,
                                                ULONG cbSigBlob,
                                                PCCOR_SIGNATURE pvSigBlob,
                                                ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetLocalRegisterMemoryValue(
      CorDebugRegister highWordReg, CORDB_ADDRESS lowWordAddress,
      ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE GetLocalMemoryRegisterValue(
      CORDB_ADDRESS highWordAddress, CorDebugRegister lowWordRegister,
      ULONG cbSigBlob, PCCOR_SIGNATURE pvSigBlob, ICorDebugValue **ppValue);
  HRESULT STDMETHODCALLTYPE CanSetIP(ULONG32 nOffset);
  HRESULT STDMETHODCALLTYPE GetChain(ICorDebugChain **ppChain);
  HRESULT STDMETHODCALLTYPE GetCode(ICorDebugCode **ppCode);
  HRESULT STDMETHODCALLTYPE GetFunction(ICorDebugFunction **ppFunction);
  HRESULT STDMETHODCALLTYPE GetFunctionToken(mdMethodDef *pToken);
  HRESULT STDMETHODCALLTYPE GetStackRange(CORDB_ADDRESS *pStart,
                                          CORDB_ADDRESS *pEnd);
  HRESULT STDMETHODCALLTYPE GetCaller(ICorDebugFrame **ppFrame);
  HRESULT STDMETHODCALLTYPE GetCallee(ICorDebugFrame **ppFrame);
  HRESULT STDMETHODCALLTYPE CreateStepper(ICorDebugStepper **ppStepper);
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
  HRESULT STDMETHODCALLTYPE IsChild(BOOL *pIsChild);
  HRESULT STDMETHODCALLTYPE IsMatchingParentFrame(
      ICorDebugNativeFrame2 *pPotentialParentFrame, BOOL *pIsParent);
  HRESULT STDMETHODCALLTYPE GetStackParameterSize(ULONG32 *pSize);
};

class CordbFrameEnum : public CordbBaseMono, public ICorDebugFrameEnum {
public:
  CordbThread *thread;
  int nframes;
  CordbNativeFrame **frames;
  CordbFrameEnum(Connection *conn, CordbThread *thread);
  HRESULT STDMETHODCALLTYPE Next(ULONG celt, ICorDebugFrame *frames[],
                                 ULONG *pceltFetched);
  HRESULT STDMETHODCALLTYPE Skip(ULONG celt);
  HRESULT STDMETHODCALLTYPE Reset(void);
  HRESULT STDMETHODCALLTYPE Clone(ICorDebugEnum **ppEnum);
  HRESULT STDMETHODCALLTYPE GetCount(ULONG *pcelt);
  HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void **ppvObject);
  ULONG STDMETHODCALLTYPE AddRef(void);
  ULONG STDMETHODCALLTYPE Release(void);
};

#endif
