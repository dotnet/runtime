//===---- objwriter.h ------------------------------------------*- C++ -*-===//
//
// object writer
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#include "llvm/CodeGen/AsmPrinter.h"
#include "llvm/MC/MCInstrInfo.h"
#include "llvm/MC/MCObjectFileInfo.h"
#include "llvm/Target/TargetOptions.h"
#include "llvm/DebugInfo/CodeView/SymbolRecord.h"

#include "cfi.h"
#include "jitDebugInfo.h"
#include "debugInfo/dwarf/dwarfGen.h"

#include <set>
#include <string>

using namespace llvm;
using namespace llvm::codeview;

#ifdef _WIN32
#define DLL_EXPORT extern "C" __declspec(dllexport)
#else
#define DLL_EXPORT extern "C" __attribute((visibility("default")))
#endif

// ***
// Define default call conventions
// ***
#if defined(HOST_X86) && !defined(HOST_UNIX)
#define STDMETHODCALLTYPE  __stdcall
#else
#define STDMETHODCALLTYPE
#endif //  defined(HOST_X86) && !defined(HOST_UNIX)

typedef uint16_t CVRegNum;

enum CustomSectionAttributes : int32_t {
  CustomSectionAttributes_ReadOnly = 0x0000,
  CustomSectionAttributes_Writeable = 0x0001,
  CustomSectionAttributes_Executable = 0x0002,
  CustomSectionAttributes_MachO_Init_Func_Pointers = 0x0100,
};

enum class RelocType {
  IMAGE_REL_BASED_ABSOLUTE = 0x00,
  IMAGE_REL_BASED_HIGHLOW = 0x03,
  IMAGE_REL_BASED_THUMB_MOV32 = 0x07,
  IMAGE_REL_BASED_DIR64 = 0x0A,
  IMAGE_REL_BASED_REL32 = 0x10,
  IMAGE_REL_BASED_THUMB_BRANCH24 = 0x13,
  IMAGE_REL_BASED_ARM64_BRANCH26 = 0x15,
  IMAGE_REL_BASED_RELPTR32 = 0x7C,
  IMAGE_REL_BASED_ARM64_PAGEBASE_REL21 = 0x81,
  IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A = 0x82,
};

enum class SymbolRefFlags
{
  SymbolRefFlags_AddressTakenFunction = 0x0001,
};

class ObjectWriter {
public:
  bool Init(StringRef FunctionName, const char* tripleName = nullptr);
  void Finish();

  void SwitchSection(const char *SectionName,
                     CustomSectionAttributes attributes,
                     const char *ComdatName);
  void SetCodeSectionAttribute(const char *SectionName,
                               CustomSectionAttributes attributes,
                               const char *ComdatName);

  void EmitAlignment(int ByteAlignment);
  void EmitBlob(int BlobSize, const char *Blob);
  void EmitIntValue(uint64_t Value, unsigned Size);
  void EmitSymbolDef(const char *SymbolName, bool global);
  void EmitWinFrameInfo(const char *FunctionName, int StartOffset,
                        int EndOffset, const char *BlobSymbolName);
  int EmitSymbolRef(const char *SymbolName, RelocType RelocType, int Delta, SymbolRefFlags Flags);

  void EmitDebugFileInfo(int FileId, const char *FileName);
  void EmitDebugFunctionInfo(const char *FunctionName, int FunctionSize, unsigned MethodTypeIndex);
  void EmitDebugVar(char *Name, int TypeIndex, bool IsParm, int RangeCount,
                    const ICorDebugInfo::NativeVarInfo *Ranges);
  void EmitDebugLoc(int NativeOffset, int FileId, int LineNumber,
                    int ColNumber);
  void EmitDebugEHClause(unsigned TryOffset, unsigned TryLength,
                         unsigned HandlerOffset, unsigned HandlerLength);
  void EmitDebugModuleInfo();

  void EmitCFIStart(int Offset);
  void EmitCFIEnd(int Offset);
  void EmitCFILsda(const char *LsdaBlobSymbolName);
  void EmitCFICode(int Offset, const char *Blob);

  unsigned GetEnumTypeIndex(const EnumTypeDescriptor &TypeDescriptor,
                            const EnumRecordTypeDescriptor *TypeRecords);
  unsigned GetClassTypeIndex(const ClassTypeDescriptor &ClassDescriptor);
  unsigned GetCompleteClassTypeIndex(
      const ClassTypeDescriptor &ClassDescriptor,
      const ClassFieldsTypeDescriptior &ClassFieldsDescriptor,
      const DataFieldDescriptor *FieldsDescriptors,
      const StaticDataFieldDescriptor *StaticsDescriptors);

  unsigned GetArrayTypeIndex(const ClassTypeDescriptor &ClassDescriptor,
                             const ArrayTypeDescriptor &ArrayDescriptor);

  unsigned GetPointerTypeIndex(const PointerTypeDescriptor& PointerDescriptor);

  unsigned GetMemberFunctionTypeIndex(const MemberFunctionTypeDescriptor& MemberDescriptor,
                                      uint32_t const *const ArgumentTypes);

  unsigned GetMemberFunctionId(const MemberFunctionIdTypeDescriptor& MemberIdDescriptor);

  unsigned GetPrimitiveTypeIndex(int Type);

  void EmitARMFnStart();
  void EmitARMFnEnd();
  void EmitARMExIdxCode(int Offset, const char *Blob);
  void EmitARMExIdxLsda(const char *Blob);

private:
  void EmitLabelDiff(const MCSymbol *From, const MCSymbol *To,
                     unsigned int Size = 4);
  void EmitSymRecord(int Size, SymbolRecordKind SymbolKind);
  void EmitCOFFSecRel32Value(MCExpr const *Value);

  void EmitVarDefRange(const MCSymbol *Fn, const LocalVariableAddrRange &Range);

  CVRegNum GetCVRegNum(unsigned RegNum);
  void EmitCVDebugVarInfo(const MCSymbol *Fn, const DebugVarInfo LocInfos[],
                          int NumVarInfos);
  void EmitCVDebugFunctionInfo(const char *FunctionName, int FunctionSize);

  void EmitDwarfFunctionInfo(const char *FunctionName, int FunctionSize, unsigned MethodTypeIndex);

  const MCSymbolRefExpr *GetSymbolRefExpr(
      const char *SymbolName,
      MCSymbolRefExpr::VariantKind Kind = MCSymbolRefExpr::VK_None);

  MCSection *GetSection(const char *SectionName,
                        CustomSectionAttributes attributes,
                        const char *ComdatName);

  MCSection *GetSpecificSection(const char *SectionName,
                                CustomSectionAttributes attributes,
                                const char *ComdatName);

  void EmitCVUserDefinedTypesSymbols();

  void InitTripleName(const char* tripleName = nullptr);
  Triple GetTriple();
  unsigned GetDFSize();
  void EmitRelocDirective(const int Offset, StringRef Name, const MCExpr *Expr);
  const MCExpr *GenTargetExpr(const MCSymbol* Symbol,
                              MCSymbolRefExpr::VariantKind Kind, int Delta,
                              bool IsPCRel = false, int Size = 0);
  void EmitARMExIdxPerOffset();


private:
  std::unique_ptr<MCRegisterInfo> RegisterInfo;
  std::unique_ptr<MCAsmInfo> AsmInfo;
  std::unique_ptr<MCObjectFileInfo> ObjFileInfo;
  std::unique_ptr<MCContext> OutContext;
  MCAsmBackend *AsmBackend; // Owned by MCStreamer
  std::unique_ptr<MCInstrInfo> InstrInfo;
  std::unique_ptr<MCSubtargetInfo> SubtargetInfo;
  MCCodeEmitter *CodeEmitter; // Owned by MCStreamer
  MCAssembler *Assembler; // Owned by MCStreamer
  std::unique_ptr<DwarfGen> DwarfGenerator;

  std::unique_ptr<raw_fd_ostream> OS;
  MCTargetOptions TargetMOptions;
  bool FrameOpened;
  std::vector<DebugVarInfo> DebugVarInfos;
  std::vector<DebugEHClauseInfo> DebugEHClauseInfos;
  DenseSet<MCSymbol *> AddressTakenFunctions;

  std::set<MCSection *> Sections;
  int FuncId;

  std::unique_ptr<UserDefinedTypesBuilder> TypeBuilder;

  std::string TripleName;

  MCObjectStreamer *Streamer; // Owned by AsmPrinter

  SmallVector<CFI_CODE, 32> CFIsPerOffset;
};

// When object writer is created/initialized successfully, it is returned.
// Or null object is returned. Client should check this.
DLL_EXPORT STDMETHODCALLTYPE ObjectWriter *InitObjWriter(const char *ObjectFilePath, const char* TripleName = nullptr) {
  ObjectWriter *OW = new ObjectWriter();
  if (OW->Init(ObjectFilePath, TripleName)) {
    return OW;
  }
  delete OW;
  return nullptr;
}

DLL_EXPORT STDMETHODCALLTYPE void FinishObjWriter(ObjectWriter *OW) {
  assert(OW && "ObjWriter is null");
  OW->Finish();
  delete OW;
}

DLL_EXPORT STDMETHODCALLTYPE void SwitchSection(ObjectWriter *OW, const char *SectionName,
                              CustomSectionAttributes attributes,
                              const char *ComdatName) {
  assert(OW && "ObjWriter is null");
  OW->SwitchSection(SectionName, attributes, ComdatName);
}

DLL_EXPORT STDMETHODCALLTYPE void SetCodeSectionAttribute(ObjectWriter *OW,
                                        const char *SectionName,
                                        CustomSectionAttributes attributes,
                                        const char *ComdatName) {
  assert(OW && "ObjWriter is null");
  OW->SetCodeSectionAttribute(SectionName, attributes, ComdatName);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitAlignment(ObjectWriter *OW, int ByteAlignment) {
  assert(OW && "ObjWriter is null");
  OW->EmitAlignment(ByteAlignment);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitBlob(ObjectWriter *OW, int BlobSize, const char *Blob) {
  assert(OW && "ObjWriter null");
  OW->EmitBlob(BlobSize, Blob);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitIntValue(ObjectWriter *OW, uint64_t Value, unsigned Size) {
  assert(OW && "ObjWriter is null");
  OW->EmitIntValue(Value, Size);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitSymbolDef(ObjectWriter *OW, const char *SymbolName, bool global) {
  assert(OW && "ObjWriter is null");
  OW->EmitSymbolDef(SymbolName, global);
}

DLL_EXPORT STDMETHODCALLTYPE int EmitSymbolRef(ObjectWriter *OW, const char *SymbolName,
                             RelocType RelocType, int Delta, SymbolRefFlags Flags) {
  assert(OW && "ObjWriter is null");
  return OW->EmitSymbolRef(SymbolName, RelocType, Delta, Flags);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitWinFrameInfo(ObjectWriter *OW, const char *FunctionName,
                                 int StartOffset, int EndOffset,
                                 const char *BlobSymbolName) {
  assert(OW && "ObjWriter is null");
  OW->EmitWinFrameInfo(FunctionName, StartOffset, EndOffset, BlobSymbolName);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitCFIStart(ObjectWriter *OW, int Offset) {
  assert(OW && "ObjWriter is null");
  OW->EmitCFIStart(Offset);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitCFIEnd(ObjectWriter *OW, int Offset) {
  assert(OW && "ObjWriter is null");
  OW->EmitCFIEnd(Offset);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitCFILsda(ObjectWriter *OW, const char *LsdaBlobSymbolName) {
  assert(OW && "ObjWriter is null");
  OW->EmitCFILsda(LsdaBlobSymbolName);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitCFICode(ObjectWriter *OW, int Offset, const char *Blob) {
  assert(OW && "ObjWriter is null");
  OW->EmitCFICode(Offset, Blob);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitDebugFileInfo(ObjectWriter *OW, int FileId,
                                  const char *FileName) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugFileInfo(FileId, FileName);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitDebugFunctionInfo(ObjectWriter *OW,
                                      const char *FunctionName,
                                      int FunctionSize,
                                      unsigned MethodTypeIndex) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugFunctionInfo(FunctionName, FunctionSize, MethodTypeIndex);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitDebugVar(ObjectWriter *OW, char *Name, int TypeIndex,
                             bool IsParam, int RangeCount,
                             ICorDebugInfo::NativeVarInfo *Ranges) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugVar(Name, TypeIndex, IsParam, RangeCount, Ranges);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitDebugEHClause(ObjectWriter *OW, unsigned TryOffset,
                                  unsigned TryLength, unsigned HandlerOffset,
                                  unsigned HandlerLength) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugEHClause(TryOffset, TryLength, HandlerOffset, HandlerLength);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitDebugLoc(ObjectWriter *OW, int NativeOffset, int FileId,
                             int LineNumber, int ColNumber) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugLoc(NativeOffset, FileId, LineNumber, ColNumber);
}

// This should be invoked at the end of module emission to finalize
// debug module info.
DLL_EXPORT STDMETHODCALLTYPE void EmitDebugModuleInfo(ObjectWriter *OW) {
  assert(OW && "ObjWriter is null");
  OW->EmitDebugModuleInfo();
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetEnumTypeIndex(ObjectWriter *OW,
                                     EnumTypeDescriptor TypeDescriptor,
                                     EnumRecordTypeDescriptor *TypeRecords) {
  assert(OW && "ObjWriter is null");
  return OW->GetEnumTypeIndex(TypeDescriptor, TypeRecords);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetClassTypeIndex(ObjectWriter *OW,
                                      ClassTypeDescriptor ClassDescriptor) {
  assert(OW && "ObjWriter is null");
  return OW->GetClassTypeIndex(ClassDescriptor);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned
GetCompleteClassTypeIndex(ObjectWriter *OW, ClassTypeDescriptor ClassDescriptor,
                          ClassFieldsTypeDescriptior ClassFieldsDescriptor,
                          DataFieldDescriptor *FieldsDescriptors,
                          StaticDataFieldDescriptor *StaticsDescriptors) {
  assert(OW && "ObjWriter is null");
  return OW->GetCompleteClassTypeIndex(ClassDescriptor, ClassFieldsDescriptor,
                                       FieldsDescriptors, StaticsDescriptors);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetArrayTypeIndex(ObjectWriter *OW,
                                      ClassTypeDescriptor ClassDescriptor,
                                      ArrayTypeDescriptor ArrayDescriptor) {
  assert(OW && "ObjWriter is null");
  return OW->GetArrayTypeIndex(ClassDescriptor, ArrayDescriptor);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetPointerTypeIndex(ObjectWriter *OW,
    PointerTypeDescriptor PointerDescriptor) {
    assert(OW && "ObjWriter is null");
    return OW->GetPointerTypeIndex(PointerDescriptor);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetMemberFunctionTypeIndex(ObjectWriter *OW,
    MemberFunctionTypeDescriptor MemberDescriptor,
    uint32_t *ArgumentTypes) {
    assert(OW && "ObjWriter is null");
    return OW->GetMemberFunctionTypeIndex(MemberDescriptor, ArgumentTypes);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetMemberFunctionIdTypeIndex(ObjectWriter *OW,
    MemberFunctionIdTypeDescriptor MemberIdDescriptor) {
    assert(OW && "ObjWriter is null");
    return OW->GetMemberFunctionId(MemberIdDescriptor);
}

DLL_EXPORT STDMETHODCALLTYPE unsigned GetPrimitiveTypeIndex(ObjectWriter *OW, int Type) {
    assert(OW && "ObjWriter is null");
    return OW->GetPrimitiveTypeIndex(Type);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitARMFnStart(ObjectWriter *OW) {
    assert(OW && "ObjWriter is null");
    return OW->EmitARMFnStart();
}

DLL_EXPORT STDMETHODCALLTYPE void EmitARMFnEnd(ObjectWriter *OW) {
  assert(OW && "ObjWriter is null");
  return OW->EmitARMFnEnd();
}

DLL_EXPORT STDMETHODCALLTYPE void EmitARMExIdxLsda(ObjectWriter *OW, const char *Blob) {
  assert(OW && "ObjWriter is null");
  return OW->EmitARMExIdxLsda(Blob);
}

DLL_EXPORT STDMETHODCALLTYPE void EmitARMExIdxCode(ObjectWriter *OW, int Offset, const char *Blob) {
  assert(OW && "ObjWriter is null");
  return OW->EmitARMExIdxCode(Offset, Blob);
}
