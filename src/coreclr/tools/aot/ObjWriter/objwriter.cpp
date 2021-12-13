//===---- objwriter.cpp -----------------------------------------*- C++ -*-===//
//
// object writer
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//
///
/// \file
/// \brief Implementation of object writer API for JIT/AOT
///
//===----------------------------------------------------------------------===//

#include "objwriter.h"
#include "debugInfo/dwarf/dwarfTypeBuilder.h"
#include "debugInfo/codeView/codeViewTypeBuilder.h"
#include "cvconst.h"
#include "llvm/DebugInfo/CodeView/CodeView.h"
#include "llvm/DebugInfo/CodeView/Line.h"
#include "llvm/DebugInfo/CodeView/SymbolRecord.h"
#include "llvm/MC/MCAsmBackend.h"
#include "llvm/MC/MCAsmInfo.h"
#include "llvm/MC/MCContext.h"
#include "llvm/MC/MCCodeEmitter.h"
#include "llvm/MC/MCDwarf.h"
#include "llvm/MC/MCInstPrinter.h"
#include "llvm/MC/MCInstrInfo.h"
#include "llvm/MC/MCParser/AsmLexer.h"
#include "llvm/MC/MCParser/MCTargetAsmParser.h"
#include "llvm/MC/MCRegisterInfo.h"
#include "llvm/MC/MCSectionCOFF.h"
#include "llvm/MC/MCSectionELF.h"
#include "llvm/MC/MCSectionMachO.h"
#include "llvm/MC/MCObjectStreamer.h"
#include "llvm/MC/MCObjectWriter.h"
#include "llvm/MC/MCSubtargetInfo.h"
#include "llvm/MC/MCTargetOptionsCommandFlags.h"
#include "llvm/MC/MCELFStreamer.h"
#include "llvm/BinaryFormat/COFF.h"
#include "llvm/Support/CommandLine.h"
#include "llvm/Support/Compression.h"
#include "llvm/BinaryFormat/ELF.h"
#include "llvm/Support/FileUtilities.h"
#include "llvm/Support/FormattedStream.h"
#include "llvm/Support/Host.h"
#include "llvm/Support/ManagedStatic.h"
#include "llvm/Support/MemoryBuffer.h"
#include "llvm/Support/PrettyStackTrace.h"
#include "llvm/Support/SourceMgr.h"
#include "llvm/Support/TargetRegistry.h"
#include "llvm/Support/TargetSelect.h"
#include "llvm/Support/ToolOutputFile.h"
#include "llvm/Support/Win64EH.h"
#include "llvm/Target/TargetMachine.h"
#include "../../../lib/Target/AArch64/MCTargetDesc/AArch64MCExpr.h"

using namespace llvm;
using namespace llvm::codeview;

bool error(const Twine &Error) {
  errs() << Twine("error: ") + Error + "\n";
  return false;
}

void ObjectWriter::InitTripleName(const char* tripleName) {
  TripleName = tripleName != nullptr ? tripleName : sys::getDefaultTargetTriple();
}

Triple ObjectWriter::GetTriple() {
  Triple TheTriple(TripleName);

  if (TheTriple.getOS() == Triple::OSType::Darwin) {
    TheTriple = Triple(
        TheTriple.getArchName(), TheTriple.getVendorName(), "darwin",
        TheTriple
            .getEnvironmentName()); // it is workaround for llvm bug
                                    // https://bugs.llvm.org//show_bug.cgi?id=24927.
  }
  return TheTriple;
}

bool ObjectWriter::Init(llvm::StringRef ObjectFilePath, const char* tripleName) {
  llvm_shutdown_obj Y; // Call llvm_shutdown() on exit.

  // Initialize targets
  InitializeAllTargetInfos();
  InitializeAllTargetMCs();

  InitTripleName(tripleName);
  Triple TheTriple = GetTriple();

  // Get the target specific parser.
  std::string TargetError;
  const Target *TheTarget =
      TargetRegistry::lookupTarget(TripleName, TargetError);
  if (!TheTarget) {
    return error("Unable to create target for " + ObjectFilePath + ": " +
                 TargetError);
  }

  std::error_code EC;
  OS.reset(new raw_fd_ostream(ObjectFilePath, EC, sys::fs::F_None));
  if (EC)
    return error("Unable to create file for " + ObjectFilePath + ": " +
                 EC.message());

  RegisterInfo.reset(TheTarget->createMCRegInfo(TripleName));
  if (!RegisterInfo)
    return error("Unable to create target register info!");

  AsmInfo.reset(TheTarget->createMCAsmInfo(*RegisterInfo, TripleName, TargetMOptions));
  if (!AsmInfo)
    return error("Unable to create target asm info!");

  ObjFileInfo.reset(new MCObjectFileInfo);
  OutContext.reset(
      new MCContext(AsmInfo.get(), RegisterInfo.get(), ObjFileInfo.get()));
  ObjFileInfo->InitMCObjectFileInfo(TheTriple, false,
                                    *OutContext);

  InstrInfo.reset(TheTarget->createMCInstrInfo());
  if (!InstrInfo)
    return error("no instr info info for target " + TripleName);

  std::string FeaturesStr;
  std::string MCPU;
  SubtargetInfo.reset(
      TheTarget->createMCSubtargetInfo(TripleName, MCPU, FeaturesStr));
  if (!SubtargetInfo)
    return error("no subtarget info for target " + TripleName);

  CodeEmitter =
      TheTarget->createMCCodeEmitter(*InstrInfo, *RegisterInfo, *OutContext);
  if (!CodeEmitter)
    return error("no code emitter for target " + TripleName);

  AsmBackend = TheTarget->createMCAsmBackend(*SubtargetInfo, *RegisterInfo, TargetMOptions);
  if (!AsmBackend)
    return error("no asm backend for target " + TripleName);

  Streamer = (MCObjectStreamer *)TheTarget->createMCObjectStreamer(
      TheTriple, *OutContext, std::unique_ptr<MCAsmBackend>(AsmBackend), AsmBackend->createObjectWriter(*OS),
      std::unique_ptr<MCCodeEmitter>(CodeEmitter), *SubtargetInfo,
      /*RelaxAll*/ true,
      /*IncrementalLinkerCompatible*/ false,
      /*DWARFMustBeAtTheEnd*/ false);
  if (!Streamer)
    return error("no object streamer for target " + TripleName);
  Assembler = &Streamer->getAssembler();

  FrameOpened = false;
  FuncId = 1;

  SetCodeSectionAttribute("text", CustomSectionAttributes_Executable, nullptr);

  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    TypeBuilder.reset(new UserDefinedCodeViewTypesBuilder());
  } else {
    TypeBuilder.reset(new UserDefinedDwarfTypesBuilder());
  }

  TypeBuilder->SetStreamer(Streamer);
  unsigned TargetPointerSize = Streamer->getContext().getAsmInfo()->getCodePointerSize();
  TypeBuilder->SetTargetPointerSize(TargetPointerSize);

  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsELF) {
    DwarfGenerator.reset(new DwarfGen());
    DwarfGenerator->SetTypeBuilder(static_cast<UserDefinedDwarfTypesBuilder*>(TypeBuilder.get()));
  }

  CFIsPerOffset.set_size(0);

  return true;
}

void ObjectWriter::Finish() {

  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF
    && AddressTakenFunctions.size() > 0) {

    // Emit all address-taken functions into the GFIDs section
    // to support control flow guard.
    Streamer->SwitchSection(ObjFileInfo->getGFIDsSection());
    for (const MCSymbol* S : AddressTakenFunctions) {
      Streamer->EmitCOFFSymbolIndex(S);
    }

    // Emit the feat.00 symbol that controls various linker behaviors
    MCSymbol* S = OutContext->getOrCreateSymbol(StringRef("@feat.00"));
    Streamer->BeginCOFFSymbolDef(S);
    Streamer->EmitCOFFSymbolStorageClass(COFF::IMAGE_SYM_CLASS_STATIC);
    Streamer->EmitCOFFSymbolType(COFF::IMAGE_SYM_DTYPE_NULL);
    Streamer->EndCOFFSymbolDef();
    int64_t Feat00Flags = 0;

    Feat00Flags |= 0x800; // cfGuardCF flags this object as control flow guard aware

    Streamer->emitSymbolAttribute(S, MCSA_Global);
    Streamer->emitAssignment(
      S, MCConstantExpr::create(Feat00Flags, *OutContext));
  }

  Streamer->Finish();
}

void ObjectWriter::SwitchSection(const char *SectionName,
                                 CustomSectionAttributes attributes,
                                 const char *ComdatName) {
  MCSection *Section = GetSection(SectionName, attributes, ComdatName);
  Streamer->SwitchSection(Section);
  if (Sections.count(Section) == 0) {
    Sections.insert(Section);
    if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsMachO) {
      assert(!Section->getBeginSymbol());
      // Output a DWARF linker-local symbol.
      // This symbol is used as a base for other symbols in a section.
      MCSymbol *SectionStartSym = OutContext->createTempSymbol();
      Streamer->emitLabel(SectionStartSym);
      Section->setBeginSymbol(SectionStartSym);
    }
  }
}

MCSection *ObjectWriter::GetSection(const char *SectionName,
                                    CustomSectionAttributes attributes,
                                    const char *ComdatName) {
  MCSection *Section = nullptr;

  if (strcmp(SectionName, "text") == 0) {
    Section = ObjFileInfo->getTextSection();
  } else if (strcmp(SectionName, "data") == 0) {
    Section = ObjFileInfo->getDataSection();
  } else if (strcmp(SectionName, "rdata") == 0) {
    Section = ObjFileInfo->getReadOnlySection();
  } else if (strcmp(SectionName, "xdata") == 0) {
    Section = ObjFileInfo->getXDataSection();
  } else if (strcmp(SectionName, "bss") == 0) {
    if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsMachO) {
      Section = ObjFileInfo->getDataBSSSection();
    } else {
      Section = ObjFileInfo->getBSSSection();
    } 
  } else {
    Section = GetSpecificSection(SectionName, attributes, ComdatName);
  }
  assert(Section);
  return Section;
}

MCSection *ObjectWriter::GetSpecificSection(const char *SectionName,
                                            CustomSectionAttributes attributes,
                                            const char *ComdatName) {
  Triple TheTriple(TripleName);
  MCSection *Section = nullptr;
  SectionKind Kind = (attributes & CustomSectionAttributes_Executable)
                         ? SectionKind::getText()
                         : (attributes & CustomSectionAttributes_Writeable)
                               ? SectionKind::getData()
                               : SectionKind::getReadOnly();
  switch (TheTriple.getObjectFormat()) {
  case Triple::MachO: {
    unsigned typeAndAttributes = 0;
    if (attributes & CustomSectionAttributes_MachO_Init_Func_Pointers) {
      typeAndAttributes |= MachO::SectionType::S_MOD_INIT_FUNC_POINTERS;
    }
    Section = OutContext->getMachOSection(
        (attributes & CustomSectionAttributes_Executable) ? "__TEXT" : "__DATA",
        SectionName, typeAndAttributes, Kind);
    break;
  }
  case Triple::COFF: {
    unsigned Characteristics = COFF::IMAGE_SCN_MEM_READ;

    if (attributes & CustomSectionAttributes_Executable) {
      Characteristics |= COFF::IMAGE_SCN_CNT_CODE | COFF::IMAGE_SCN_MEM_EXECUTE;
    } else if (attributes & CustomSectionAttributes_Writeable) {
      Characteristics |=
          COFF::IMAGE_SCN_CNT_INITIALIZED_DATA | COFF::IMAGE_SCN_MEM_WRITE;
    } else {
      Characteristics |= COFF::IMAGE_SCN_CNT_INITIALIZED_DATA;
    }

    if (ComdatName != nullptr) {
      Section = OutContext->getCOFFSection(
          SectionName, Characteristics | COFF::IMAGE_SCN_LNK_COMDAT, Kind,
          ComdatName, COFF::COMDATType::IMAGE_COMDAT_SELECT_ANY);
    } else {
      Section = OutContext->getCOFFSection(SectionName, Characteristics, Kind);
    }
    break;
  }
  case Triple::ELF: {
    unsigned Flags = ELF::SHF_ALLOC;
    if (ComdatName != nullptr) {
      MCSymbolELF *GroupSym =
          cast<MCSymbolELF>(OutContext->getOrCreateSymbol(ComdatName));
      OutContext->createELFGroupSection(GroupSym);
      Flags |= ELF::SHF_GROUP;
    }
    if (attributes & CustomSectionAttributes_Executable) {
      Flags |= ELF::SHF_EXECINSTR;
    } else if (attributes & CustomSectionAttributes_Writeable) {
      Flags |= ELF::SHF_WRITE;
    }
    Section =
        OutContext->getELFSection(SectionName, ELF::SHT_PROGBITS, Flags, 0,
                                  ComdatName != nullptr ? ComdatName : "");
    break;
  }
  default:
    error("Unknown output format for target " + TripleName);
    break;
  }
  return Section;
}

void ObjectWriter::SetCodeSectionAttribute(const char *SectionName,
                                           CustomSectionAttributes attributes,
                                           const char *ComdatName) {
  MCSection *Section = GetSection(SectionName, attributes, ComdatName);

  assert(!Section->hasInstructions());
  Section->setHasInstructions(true);
  if (ObjFileInfo->getObjectFileType() != ObjFileInfo->IsCOFF) {
    OutContext->addGenDwarfSection(Section);
  }
}

void ObjectWriter::EmitAlignment(int ByteAlignment) {
  int64_t fillValue = 0;

  if (Streamer->getCurrentSectionOnly()->getKind().isText()) {
    if (ObjFileInfo->getTargetTriple().getArch() == llvm::Triple::ArchType::x86 ||
        ObjFileInfo->getTargetTriple().getArch() == llvm::Triple::ArchType::x86_64) {
      fillValue = 0x90; // x86 nop
    }
  }

  Streamer->emitValueToAlignment(ByteAlignment, fillValue);
}

void ObjectWriter::EmitBlob(int BlobSize, const char *Blob) {
  if (Streamer->getCurrentSectionOnly()->getKind().isText()) {
    Streamer->emitInstructionBytes(StringRef(Blob, BlobSize));
  } else {
    Streamer->emitBytes(StringRef(Blob, BlobSize));
  }
}

void ObjectWriter::EmitIntValue(uint64_t Value, unsigned Size) {
  Streamer->emitIntValue(Value, Size);
}

void ObjectWriter::EmitSymbolDef(const char *SymbolName, bool global) {
  MCSymbol *Sym = OutContext->getOrCreateSymbol(Twine(SymbolName));

  Streamer->emitSymbolAttribute(Sym, MCSA_Global);

  Triple TheTriple = ObjFileInfo->getTargetTriple();

  if (TheTriple.getObjectFormat() == Triple::ELF) {
    // An ARM function symbol should be marked with an appropriate ELF attribute
    // to make later computation of a relocation address value correct
    if (Streamer->getCurrentSectionOnly()->getKind().isText()) {
      switch (TheTriple.getArch()) {
      case Triple::arm:
      case Triple::armeb:
      case Triple::thumb:
      case Triple::thumbeb:
      case Triple::aarch64:
      case Triple::aarch64_be:
        Streamer->emitSymbolAttribute(Sym, MCSA_ELF_TypeFunction);
        break;
      default:
        break;
      }
    }

    // Mark the symbol hidden if requested
    if (!global) {
      Streamer->emitSymbolAttribute(Sym, MCSA_Hidden);
    }
  }

  Streamer->emitLabel(Sym);
}

const MCSymbolRefExpr *
ObjectWriter::GetSymbolRefExpr(const char *SymbolName,
                               MCSymbolRefExpr::VariantKind Kind) {
  // Create symbol reference
  MCSymbol *T = OutContext->getOrCreateSymbol(SymbolName);
  Assembler->registerSymbol(*T);
  return MCSymbolRefExpr::create(T, Kind, *OutContext);
}

unsigned ObjectWriter::GetDFSize() {
  return Streamer->getOrCreateDataFragment()->getContents().size();
}

void ObjectWriter::EmitRelocDirective(const int Offset, StringRef Name, const MCExpr *Expr) {
  const MCExpr *OffsetExpr = MCConstantExpr::create(Offset, *OutContext);
  Optional<std::pair<bool, std::string>> result = Streamer->emitRelocDirective(*OffsetExpr, Name, Expr, SMLoc(), *SubtargetInfo);
  assert(!result.hasValue());
}

const MCExpr *ObjectWriter::GenTargetExpr(const MCSymbol* Symbol, MCSymbolRefExpr::VariantKind Kind,
                                          int Delta, bool IsPCRel, int Size) {
  const MCExpr *TargetExpr = MCSymbolRefExpr::create(Symbol, Kind, *OutContext);
  if (IsPCRel && Size != 0) {
    // If the fixup is pc-relative, we need to bias the value to be relative to
    // the start of the field, not the end of the field
    TargetExpr = MCBinaryExpr::createSub(
        TargetExpr, MCConstantExpr::create(Size, *OutContext), *OutContext);
  }
  if (Delta != 0) {
    TargetExpr = MCBinaryExpr::createAdd(
        TargetExpr, MCConstantExpr::create(Delta, *OutContext), *OutContext);
  }
  return TargetExpr;
}

int ObjectWriter::EmitSymbolRef(const char *SymbolName,
                                RelocType RelocationType, int Delta, SymbolRefFlags Flags) {
  bool IsPCRel = false;
  int Size = 0;
  MCSymbolRefExpr::VariantKind Kind = MCSymbolRefExpr::VK_None;

  MCSymbol* Symbol = OutContext->getOrCreateSymbol(SymbolName);
  Assembler->registerSymbol(*Symbol);

  if ((int)Flags & (int)SymbolRefFlags::SymbolRefFlags_AddressTakenFunction) {
    AddressTakenFunctions.insert(Symbol);
  }

  // Convert RelocationType to MCSymbolRefExpr
  switch (RelocationType) {
  case RelocType::IMAGE_REL_BASED_ABSOLUTE:
    assert(ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF);
    Kind = MCSymbolRefExpr::VK_COFF_IMGREL32;
    Size = 4;
    break;
  case RelocType::IMAGE_REL_BASED_HIGHLOW:
    Size = 4;
    break;
  case RelocType::IMAGE_REL_BASED_DIR64:
    Size = 8;
    break;
  case RelocType::IMAGE_REL_BASED_REL32:
    Size = 4;
    IsPCRel = true;
    if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsELF) {
      // PLT is valid only for code symbols,
      // but there shouldn't be references to global data symbols
      Kind = MCSymbolRefExpr::VK_PLT;
    }
    break;
  case RelocType::IMAGE_REL_BASED_RELPTR32:
    Size = 4;
    IsPCRel = true;
    Delta += 4;
    break;
  case RelocType::IMAGE_REL_BASED_THUMB_MOV32: {
    const unsigned Offset = GetDFSize();
    const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta);
    EmitRelocDirective(Offset, "R_ARM_THM_MOVW_ABS_NC", TargetExpr);
    EmitRelocDirective(Offset + 4, "R_ARM_THM_MOVT_ABS", TargetExpr);
    return 8;
  }
  case RelocType::IMAGE_REL_BASED_THUMB_BRANCH24: {
    const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta);
    EmitRelocDirective(GetDFSize(), "R_ARM_THM_CALL", TargetExpr);
    return 4;
  }
  case RelocType::IMAGE_REL_BASED_ARM64_BRANCH26: {
    const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta);
    EmitRelocDirective(GetDFSize(), "R_AARCH64_CALL26", TargetExpr);
    return 4;
  }
  case RelocType::IMAGE_REL_BASED_ARM64_PAGEBASE_REL21: {
    const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta);
    TargetExpr =
        AArch64MCExpr::create(TargetExpr, AArch64MCExpr::VK_CALL, *OutContext);
    EmitRelocDirective(GetDFSize(), "R_AARCH64_ADR_PREL_PG_HI21", TargetExpr);
    return 4;
  }
  case RelocType::IMAGE_REL_BASED_ARM64_PAGEOFFSET_12A: {
    const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta);
    TargetExpr =
        AArch64MCExpr::create(TargetExpr, AArch64MCExpr::VK_LO12, *OutContext);
    EmitRelocDirective(GetDFSize(), "R_AARCH64_ADD_ABS_LO12_NC", TargetExpr);
    return 4;
  }
  }

  const MCExpr *TargetExpr = GenTargetExpr(Symbol, Kind, Delta, IsPCRel, Size);
  Streamer->emitValueImpl(TargetExpr, Size, SMLoc(), IsPCRel);
  return Size;
}

void ObjectWriter::EmitWinFrameInfo(const char *FunctionName, int StartOffset,
                                    int EndOffset, const char *BlobSymbolName) {
  assert(ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF);

  // .pdata emission
  MCSection *Section = ObjFileInfo->getPDataSection();

  // If the function was emitted to a Comdat section, create an associative
  // section to place the frame info in. This is due to the Windows linker
  // requirement that a function and its unwind info come from the same
  // object file.
  MCSymbol *Fn = OutContext->getOrCreateSymbol(Twine(FunctionName));
  const MCSectionCOFF *FunctionSection = cast<MCSectionCOFF>(&Fn->getSection());
  if (FunctionSection->getCharacteristics() & COFF::IMAGE_SCN_LNK_COMDAT) {
    Section = OutContext->getAssociativeCOFFSection(
        cast<MCSectionCOFF>(Section), FunctionSection->getCOMDATSymbol());
  }

  Streamer->SwitchSection(Section);
  Streamer->emitValueToAlignment(4);

  const MCExpr *BaseRefRel =
      GetSymbolRefExpr(FunctionName, MCSymbolRefExpr::VK_COFF_IMGREL32);

  Triple::ArchType Arch = ObjFileInfo->getTargetTriple().getArch();

  if (Arch == Triple::thumb || Arch == Triple::thumbeb) {
    StartOffset |= 1;
  }

  // start Offset
  const MCExpr *StartOfs = MCConstantExpr::create(StartOffset, *OutContext);
  Streamer->emitValue(
      MCBinaryExpr::createAdd(BaseRefRel, StartOfs, *OutContext), 4);

  if (Arch == Triple::x86 || Arch == Triple::x86_64) {
    // end Offset
    const MCExpr *EndOfs = MCConstantExpr::create(EndOffset, *OutContext);
    Streamer->emitValue(
        MCBinaryExpr::createAdd(BaseRefRel, EndOfs, *OutContext), 4);
  }

  // frame symbol reference
  Streamer->emitValue(
      GetSymbolRefExpr(BlobSymbolName, MCSymbolRefExpr::VK_COFF_IMGREL32), 4);
}

void ObjectWriter::EmitCFIStart(int Offset) {
  assert(!FrameOpened && "frame should be closed before CFIStart");
  Streamer->emitCFIStartProc(false);
  FrameOpened = true;
}

void ObjectWriter::EmitCFIEnd(int Offset) {
  assert(FrameOpened && "frame should be opened before CFIEnd");
  Streamer->emitCFIEndProc();
  FrameOpened = false;
}

void ObjectWriter::EmitCFILsda(const char *LsdaBlobSymbolName) {
  assert(FrameOpened && "frame should be opened before CFILsda");

  // Create symbol reference
  MCSymbol *T = OutContext->getOrCreateSymbol(LsdaBlobSymbolName);
  Assembler->registerSymbol(*T);
  Streamer->emitCFILsda(T, llvm::dwarf::Constants::DW_EH_PE_pcrel |
                               llvm::dwarf::Constants::DW_EH_PE_sdata4);
}

void ObjectWriter::EmitCFICode(int Offset, const char *Blob) {
  assert(FrameOpened && "frame should be opened before CFICode");

  const CFI_CODE *CfiCode = (const CFI_CODE *)Blob;
  switch (CfiCode->CfiOpCode) {
  case CFI_ADJUST_CFA_OFFSET:
    assert(CfiCode->DwarfReg == DWARF_REG_ILLEGAL &&
           "Unexpected Register Value for OpAdjustCfaOffset");
    Streamer->emitCFIAdjustCfaOffset(CfiCode->Offset);
    break;
  case CFI_REL_OFFSET:
    Streamer->emitCFIRelOffset(CfiCode->DwarfReg, CfiCode->Offset);
    break;
  case CFI_DEF_CFA_REGISTER:
    assert(CfiCode->Offset == 0 &&
           "Unexpected Offset Value for OpDefCfaRegister");
    Streamer->emitCFIDefCfaRegister(CfiCode->DwarfReg);
    break;
  case CFI_DEF_CFA:
    assert(CfiCode->Offset != 0 &&
           "Unexpected Offset Value for OpDefCfa");
    Streamer->emitCFIDefCfa(CfiCode->DwarfReg, CfiCode->Offset);
    break;
  default:
    assert(false && "Unrecognized CFI");
    break;
  }
}

void ObjectWriter::EmitLabelDiff(const MCSymbol *From, const MCSymbol *To,
                                 unsigned int Size) {
  MCSymbolRefExpr::VariantKind Variant = MCSymbolRefExpr::VK_None;
  const MCExpr *FromRef = MCSymbolRefExpr::create(From, Variant, *OutContext),
               *ToRef = MCSymbolRefExpr::create(To, Variant, *OutContext);
  const MCExpr *AddrDelta =
      MCBinaryExpr::create(MCBinaryExpr::Sub, ToRef, FromRef, *OutContext);
  Streamer->emitValue(AddrDelta, Size);
}

void ObjectWriter::EmitSymRecord(int Size, SymbolRecordKind SymbolKind) {
  RecordPrefix Rec;
  Rec.RecordLen = ulittle16_t(Size + sizeof(ulittle16_t));
  Rec.RecordKind = ulittle16_t((uint16_t)SymbolKind);
  Streamer->emitBytes(StringRef((char *)&Rec, sizeof(Rec)));
}

void ObjectWriter::EmitCOFFSecRel32Value(MCExpr const *Value) {
  MCDataFragment *DF = Streamer->getOrCreateDataFragment();
  MCFixup Fixup = MCFixup::create(DF->getContents().size(), Value, FK_SecRel_4);
  DF->getFixups().push_back(Fixup);
  DF->getContents().resize(DF->getContents().size() + 4, 0);
}

void ObjectWriter::EmitVarDefRange(const MCSymbol *Fn,
                                   const LocalVariableAddrRange &Range) {
  const MCSymbolRefExpr *BaseSym = MCSymbolRefExpr::create(Fn, *OutContext);
  const MCExpr *Offset = MCConstantExpr::create(Range.OffsetStart, *OutContext);
  const MCExpr *Expr = MCBinaryExpr::createAdd(BaseSym, Offset, *OutContext);
  EmitCOFFSecRel32Value(Expr);
  Streamer->EmitCOFFSectionIndex(Fn);
  Streamer->emitIntValue(Range.Range, 2);
}

// Maps an ICorDebugInfo register number to the corresponding CodeView
// register number
CVRegNum ObjectWriter::GetCVRegNum(unsigned RegNum) {
  static const CVRegNum CVRegMapAmd64[] = {
    CV_AMD64_RAX, CV_AMD64_RCX, CV_AMD64_RDX, CV_AMD64_RBX,
    CV_AMD64_RSP, CV_AMD64_RBP, CV_AMD64_RSI, CV_AMD64_RDI,
    CV_AMD64_R8, CV_AMD64_R9, CV_AMD64_R10, CV_AMD64_R11,
    CV_AMD64_R12, CV_AMD64_R13, CV_AMD64_R14, CV_AMD64_R15,
  };

  switch (ObjFileInfo->getTargetTriple().getArch()) {
  case Triple::x86:
    if (X86::ICorDebugInfo::REGNUM_EAX <= RegNum &&
        RegNum <= X86::ICorDebugInfo::REGNUM_EDI) {
      return RegNum - X86::ICorDebugInfo::REGNUM_EAX + CV_REG_EAX;
    }
    break;
  case Triple::x86_64:
    if (RegNum < sizeof(CVRegMapAmd64) / sizeof(CVRegMapAmd64[0])) {
      return CVRegMapAmd64[RegNum];
    }
    break;
  case Triple::arm:
  case Triple::armeb:
  case Triple::thumb:
  case Triple::thumbeb:
    if (Arm::ICorDebugInfo::REGNUM_R0 <= RegNum &&
        RegNum <= Arm::ICorDebugInfo::REGNUM_PC) {
      return RegNum - Arm::ICorDebugInfo::REGNUM_R0 + CV_ARM_R0;
    }
    break;
  case Triple::aarch64:
  case Triple::aarch64_be:
    if (Arm64::ICorDebugInfo::REGNUM_X0 <= RegNum &&
        RegNum < Arm64::ICorDebugInfo::REGNUM_PC) {
      return RegNum - Arm64::ICorDebugInfo::REGNUM_X0 + CV_ARM64_X0;
    }
    // Special registers are ordered FP, LR, SP, PC in ICorDebugInfo's
    // enumeration and FP, LR, SP, *ZR*, PC in CodeView's enumeration.
    // For that reason handle the PC register separately.
    if (RegNum == Arm64::ICorDebugInfo::REGNUM_PC) {
      return CV_ARM64_PC;
    }
    break;
  default:
    assert(false && "Unexpected architecture");
    break;
  }
  return CV_REG_NONE;
}

void ObjectWriter::EmitCVDebugVarInfo(const MCSymbol *Fn,
                                      const DebugVarInfo LocInfos[],
                                      int NumVarInfos) {
  for (int I = 0; I < NumVarInfos; I++) {
    // Emit an S_LOCAL record
    DebugVarInfo Var = LocInfos[I];
    TypeIndex Type = TypeIndex(Var.TypeIndex);
    LocalSymFlags Flags = LocalSymFlags::None;
    unsigned SizeofSym = sizeof(Type) + sizeof(Flags);
    unsigned NameLength = Var.Name.length() + 1;
    EmitSymRecord(SizeofSym + NameLength, SymbolRecordKind::LocalSym);
    if (Var.IsParam) {
      Flags |= LocalSymFlags::IsParameter;
    }
    Streamer->emitBytes(StringRef((char *)&Type, sizeof(Type)));
    Streamer->emitIntValue(static_cast<uint16_t>(Flags), sizeof(Flags));
    Streamer->emitBytes(StringRef(Var.Name.c_str(), NameLength));

    for (const auto &Range : Var.Ranges) {
      // Emit a range record
      switch (Range.loc.vlType) {
      case ICorDebugInfo::VLT_REG:
      case ICorDebugInfo::VLT_REG_FP: {

        // Currently only support integer registers.
        // TODO: support xmm registers
        CVRegNum CVReg = GetCVRegNum(Range.loc.vlReg.vlrReg);
        if (CVReg == CV_REG_NONE) {
          break;
        }
        SymbolRecordKind SymbolKind = SymbolRecordKind::DefRangeRegisterSym;
        unsigned SizeofDefRangeRegisterSym = sizeof(DefRangeRegisterSym::Hdr) +
                                             sizeof(DefRangeRegisterSym::Range);
        EmitSymRecord(SizeofDefRangeRegisterSym, SymbolKind);

        DefRangeRegisterSym DefRangeRegisterSymbol(SymbolKind);
        DefRangeRegisterSymbol.Range.OffsetStart = Range.startOffset;
        DefRangeRegisterSymbol.Range.Range =
            Range.endOffset - Range.startOffset;
        DefRangeRegisterSymbol.Range.ISectStart = 0;
        DefRangeRegisterSymbol.Hdr.Register = CVReg;
        DefRangeRegisterSymbol.Hdr.MayHaveNoName = 0;

        unsigned Length = sizeof(DefRangeRegisterSymbol.Hdr);
        Streamer->emitBytes(
            StringRef((char *)&DefRangeRegisterSymbol.Hdr, Length));
        EmitVarDefRange(Fn, DefRangeRegisterSymbol.Range);
        break;
      }

      case ICorDebugInfo::VLT_STK: {

        // TODO: support REGNUM_AMBIENT_SP
        CVRegNum CVReg = GetCVRegNum(Range.loc.vlStk.vlsBaseReg);
        if (CVReg == CV_REG_NONE) {
          break;
        }

        SymbolRecordKind SymbolKind = SymbolRecordKind::DefRangeRegisterRelSym;
        unsigned SizeofDefRangeRegisterRelSym =
            sizeof(DefRangeRegisterRelSym::Hdr) +
            sizeof(DefRangeRegisterRelSym::Range);
        EmitSymRecord(SizeofDefRangeRegisterRelSym, SymbolKind);

        DefRangeRegisterRelSym DefRangeRegisterRelSymbol(SymbolKind);
        DefRangeRegisterRelSymbol.Range.OffsetStart = Range.startOffset;
        DefRangeRegisterRelSymbol.Range.Range =
            Range.endOffset - Range.startOffset;
        DefRangeRegisterRelSymbol.Range.ISectStart = 0;
        DefRangeRegisterRelSymbol.Hdr.Register = CVReg;
        DefRangeRegisterRelSymbol.Hdr.Flags = 0;
        DefRangeRegisterRelSymbol.Hdr.BasePointerOffset =
            Range.loc.vlStk.vlsOffset;

        unsigned Length = sizeof(DefRangeRegisterRelSymbol.Hdr);
        Streamer->emitBytes(
            StringRef((char *)&DefRangeRegisterRelSymbol.Hdr, Length));
        EmitVarDefRange(Fn, DefRangeRegisterRelSymbol.Range);
        break;
      }

      case ICorDebugInfo::VLT_REG_BYREF:
      case ICorDebugInfo::VLT_STK_BYREF:
      case ICorDebugInfo::VLT_REG_REG:
      case ICorDebugInfo::VLT_REG_STK:
      case ICorDebugInfo::VLT_STK_REG:
      case ICorDebugInfo::VLT_STK2:
      case ICorDebugInfo::VLT_FPSTK:
      case ICorDebugInfo::VLT_FIXED_VA:
        // TODO: for optimized debugging
        break;

      default:
        assert(false && "Unknown varloc type!");
        break;
      }
    }
  }
}

void ObjectWriter::EmitCVDebugFunctionInfo(const char *FunctionName,
                                           int FunctionSize) {
  assert(ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF);

  // Mark the end of function.
  MCSymbol *FnEnd = OutContext->createTempSymbol();
  Streamer->emitLabel(FnEnd);

  MCSection *Section = ObjFileInfo->getCOFFDebugSymbolsSection();
  Streamer->SwitchSection(Section);
  // Emit debug section magic before the first entry.
  if (FuncId == 1) {
    Streamer->emitIntValue(COFF::DEBUG_SECTION_MAGIC, 4);
  }
  MCSymbol *Fn = OutContext->getOrCreateSymbol(Twine(FunctionName));

  // Emit a symbol subsection, required by VS2012+ to find function boundaries.
  MCSymbol *SymbolsBegin = OutContext->createTempSymbol(),
           *SymbolsEnd = OutContext->createTempSymbol();
  Streamer->emitIntValue(unsigned(DebugSubsectionKind::Symbols), 4);
  EmitLabelDiff(SymbolsBegin, SymbolsEnd);
  Streamer->emitLabel(SymbolsBegin);
  {
    ProcSym ProcSymbol(SymbolRecordKind::GlobalProcIdSym);
    ProcSymbol.CodeSize = FunctionSize;
    ProcSymbol.DbgEnd = FunctionSize;

    unsigned FunctionNameLength = strlen(FunctionName) + 1;
    unsigned HeaderSize =
        sizeof(ProcSymbol.Parent) + sizeof(ProcSymbol.End) +
        sizeof(ProcSymbol.Next) + sizeof(ProcSymbol.CodeSize) +
        sizeof(ProcSymbol.DbgStart) + sizeof(ProcSymbol.DbgEnd) +
        sizeof(ProcSymbol.FunctionType);
    unsigned SymbolSize = HeaderSize + 4 + 2 + 1 + FunctionNameLength;
    EmitSymRecord(SymbolSize, SymbolRecordKind::GlobalProcIdSym);

    Streamer->emitBytes(StringRef((char *)&ProcSymbol.Parent, HeaderSize));
    // Emit relocation
    Streamer->EmitCOFFSecRel32(Fn, 0);
    Streamer->EmitCOFFSectionIndex(Fn);

    // Emit flags
    Streamer->emitIntValue(0, 1);

    // Emit the function display name as a null-terminated string.

    Streamer->emitBytes(StringRef(FunctionName, FunctionNameLength));

    // Emit local var info
    int NumVarInfos = DebugVarInfos.size();
    if (NumVarInfos > 0) {
      EmitCVDebugVarInfo(Fn, &DebugVarInfos[0], NumVarInfos);
      DebugVarInfos.clear();
    }

    // We're done with this function.
    EmitSymRecord(0, SymbolRecordKind::ProcEnd);
  }

  Streamer->emitLabel(SymbolsEnd);

  // Every subsection must be aligned to a 4-byte boundary.
  Streamer->emitValueToAlignment(4);

  // We have an assembler directive that takes care of the whole line table.
  // We also increase function id for the next function.
  Streamer->emitCVLinetableDirective(FuncId++, Fn, FnEnd);
}

void ObjectWriter::EmitDwarfFunctionInfo(const char *FunctionName,
                                         int FunctionSize,
                                         unsigned MethodTypeIndex) {
  if (FuncId == 1) {
    DwarfGenerator->EmitCompileUnit();
  }

  DwarfGenerator->EmitSubprogramInfo(FunctionName, FunctionSize,
      MethodTypeIndex, DebugVarInfos, DebugEHClauseInfos);

  DebugVarInfos.clear();
  DebugEHClauseInfos.clear();

  FuncId++;
}

void ObjectWriter::EmitDebugFileInfo(int FileId, const char *FileName) {
  assert(FileId > 0 && "FileId should be greater than 0.");
  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    // TODO: we could pipe through the checksum and hash algorithm from the managed PDB
    ArrayRef<uint8_t> ChecksumAsBytes;
    Streamer->EmitCVFileDirective(FileId, FileName, ChecksumAsBytes, 0);
  } else {
    Streamer->emitDwarfFileDirective(FileId, "", FileName);
  }
}

void ObjectWriter::EmitDebugFunctionInfo(const char *FunctionName,
                                         int FunctionSize,
                                         unsigned MethodTypeIndex) {
  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    Streamer->EmitCVFuncIdDirective(FuncId);
    EmitCVDebugFunctionInfo(FunctionName, FunctionSize);
  } else {
    if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsELF) {
      MCSymbol *Sym = OutContext->getOrCreateSymbol(Twine(FunctionName));
      Streamer->emitSymbolAttribute(Sym, MCSA_ELF_TypeFunction);
      Streamer->emitELFSize(Sym,
                            MCConstantExpr::create(FunctionSize, *OutContext));
      EmitDwarfFunctionInfo(FunctionName, FunctionSize, MethodTypeIndex);
    }
    // TODO: Should test it for Macho.
  }
}

void ObjectWriter::EmitDebugVar(char *Name, int TypeIndex, bool IsParm,
                                int RangeCount,
                                const ICorDebugInfo::NativeVarInfo *Ranges) {
  assert(RangeCount != 0);
  DebugVarInfo NewVar(Name, TypeIndex, IsParm);

  for (int I = 0; I < RangeCount; I++) {
    assert(Ranges[0].varNumber == Ranges[I].varNumber);
    NewVar.Ranges.push_back(Ranges[I]);
  }

  DebugVarInfos.push_back(NewVar);
}

void ObjectWriter::EmitDebugEHClause(unsigned TryOffset, unsigned TryLength,
                                unsigned HandlerOffset, unsigned HandlerLength) {
  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsELF) {
    DebugEHClauseInfos.emplace_back(TryOffset, TryLength, HandlerOffset, HandlerLength);
  }
}

void ObjectWriter::EmitDebugLoc(int NativeOffset, int FileId, int LineNumber,
                                int ColNumber) {
  assert(FileId > 0 && "FileId should be greater than 0.");
  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    Streamer->EmitCVFuncIdDirective(FuncId);
    Streamer->emitCVLocDirective(FuncId, FileId, LineNumber, ColNumber, false,
                                 true, "", SMLoc());
  } else {
    Streamer->emitDwarfLocDirective(FileId, LineNumber, ColNumber, 1, 0, 0, "");
  }
}

void ObjectWriter::EmitCVUserDefinedTypesSymbols() {
  const auto &UDTs = TypeBuilder->GetUDTs();
  if (UDTs.empty()) {
    return;
  }
  MCSection *Section = ObjFileInfo->getCOFFDebugSymbolsSection();
  Streamer->SwitchSection(Section);

  MCSymbol *SymbolsBegin = OutContext->createTempSymbol(),
           *SymbolsEnd = OutContext->createTempSymbol();
  Streamer->emitIntValue(unsigned(DebugSubsectionKind::Symbols), 4);
  EmitLabelDiff(SymbolsBegin, SymbolsEnd);
  Streamer->emitLabel(SymbolsBegin);

  for (const std::pair<std::string, uint32_t> &UDT : UDTs) {
    unsigned NameLength = UDT.first.length() + 1;
    unsigned RecordLength = 2 + 4 + NameLength;
    Streamer->emitIntValue(RecordLength, 2);
    Streamer->emitIntValue(unsigned(SymbolKind::S_UDT), 2);
    Streamer->emitIntValue(UDT.second, 4);
    Streamer->emitBytes(StringRef(UDT.first.c_str(), NameLength));
  }
  Streamer->emitLabel(SymbolsEnd);
  Streamer->emitValueToAlignment(4);
}

void ObjectWriter::EmitDebugModuleInfo() {
  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    TypeBuilder->EmitTypeInformation(ObjFileInfo->getCOFFDebugTypesSection());
    EmitCVUserDefinedTypesSymbols();
  }

  // Ensure ending all sections.
  for (auto Section : Sections) {
    Streamer->endSection(Section);
  }

  if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsCOFF) {
    MCSection *Section = ObjFileInfo->getCOFFDebugSymbolsSection();
    Streamer->SwitchSection(Section);
    Streamer->emitCVFileChecksumsDirective();
    Streamer->emitCVStringTableDirective();
  } else if (ObjFileInfo->getObjectFileType() == ObjFileInfo->IsELF) {
    DwarfGenerator->EmitAbbrev();
    DwarfGenerator->EmitAranges();
    DwarfGenerator->Finish();
  } else {
    OutContext->setGenDwarfForAssembly(true);
  }
}

unsigned
ObjectWriter::GetEnumTypeIndex(const EnumTypeDescriptor &TypeDescriptor,
                               const EnumRecordTypeDescriptor *TypeRecords) {
  return TypeBuilder->GetEnumTypeIndex(TypeDescriptor, TypeRecords);
}

unsigned
ObjectWriter::GetClassTypeIndex(const ClassTypeDescriptor &ClassDescriptor) {
  unsigned res = TypeBuilder->GetClassTypeIndex(ClassDescriptor);
  return res;
}

unsigned ObjectWriter::GetCompleteClassTypeIndex(
    const ClassTypeDescriptor &ClassDescriptor,
    const ClassFieldsTypeDescriptior &ClassFieldsDescriptor,
    const DataFieldDescriptor *FieldsDescriptors,
    const StaticDataFieldDescriptor *StaticsDescriptors) {
  unsigned res = TypeBuilder->GetCompleteClassTypeIndex(ClassDescriptor,
      ClassFieldsDescriptor, FieldsDescriptors, StaticsDescriptors);
  return res;
}

unsigned
ObjectWriter::GetArrayTypeIndex(const ClassTypeDescriptor &ClassDescriptor,
                                const ArrayTypeDescriptor &ArrayDescriptor) {
  return TypeBuilder->GetArrayTypeIndex(ClassDescriptor, ArrayDescriptor);
}

unsigned 
ObjectWriter::GetPointerTypeIndex(const PointerTypeDescriptor& PointerDescriptor) {
    return TypeBuilder->GetPointerTypeIndex(PointerDescriptor);
}

unsigned 
ObjectWriter::GetMemberFunctionTypeIndex(const MemberFunctionTypeDescriptor& MemberDescriptor,
                                         uint32_t const *const ArgumentTypes) {
    return TypeBuilder->GetMemberFunctionTypeIndex(MemberDescriptor, ArgumentTypes);
}

unsigned 
ObjectWriter::GetMemberFunctionId(const MemberFunctionIdTypeDescriptor& MemberIdDescriptor) {
    return TypeBuilder->GetMemberFunctionId(MemberIdDescriptor);
}

unsigned
ObjectWriter::GetPrimitiveTypeIndex(int Type) {
  return TypeBuilder->GetPrimitiveTypeIndex(static_cast<PrimitiveTypeFlags>(Type));
}

void
ObjectWriter::EmitARMFnStart() {
  MCTargetStreamer &TS = *(Streamer->getTargetStreamer());
  ARMTargetStreamer &ATS = static_cast<ARMTargetStreamer &>(TS);

  ATS.emitFnStart();
}

void ObjectWriter::EmitARMFnEnd() {

  if (!CFIsPerOffset.empty())
  {
    EmitARMExIdxPerOffset();
  }

  MCTargetStreamer &TS = *(Streamer->getTargetStreamer());
  ARMTargetStreamer &ATS = static_cast<ARMTargetStreamer &>(TS);

  ATS.emitFnEnd();
}

void ObjectWriter::EmitARMExIdxLsda(const char *LsdaBlobSymbolName)
{
  MCTargetStreamer &TS = *(Streamer->getTargetStreamer());
  ARMTargetStreamer &ATS = static_cast<ARMTargetStreamer &>(TS);

  MCSymbol *T = OutContext->getOrCreateSymbol(LsdaBlobSymbolName);
  Assembler->registerSymbol(*T);

  ATS.emitLsda(T);
}

void ObjectWriter::EmitARMExIdxPerOffset()
{
  MCTargetStreamer &TS = *(Streamer->getTargetStreamer());
  ARMTargetStreamer &ATS = static_cast<ARMTargetStreamer &>(TS);
  const MCRegisterInfo *MRI = OutContext->getRegisterInfo();

  SmallVector<unsigned, 32> RegSet;
  bool IsVector = false;

  // LLVM reverses opcodes that are fed to ARMTargetStreamer, so we do the same,
  // but per code offset. Opcodes with different code offsets are already given in
  // the correct order.
  for (int i = CFIsPerOffset.size() - 1; i >= 0; --i)
  {
    unsigned char opCode = CFIsPerOffset[i].CfiOpCode;
    short Reg = CFIsPerOffset[i].DwarfReg;

    if (RegSet.empty() && opCode == CFI_REL_OFFSET)
    {
      IsVector = Reg >= 16;
    }
    else if (!RegSet.empty() && opCode != CFI_REL_OFFSET)
    {
      ATS.emitRegSave(RegSet, IsVector);
      RegSet.clear();
    }

    switch (opCode)
    {
    case CFI_REL_OFFSET:
      assert(IsVector == (Reg >= 16) && "Unexpected Register Type");
      RegSet.push_back(MRI->getLLVMRegNum(Reg, true).getValue());
      break;
    case CFI_ADJUST_CFA_OFFSET:
      assert(Reg == DWARF_REG_ILLEGAL &&
          "Unexpected Register Value for OpAdjustCfaOffset");
      ATS.emitPad(CFIsPerOffset[i].Offset);
      break;
    case CFI_DEF_CFA_REGISTER:
      ATS.emitMovSP(MRI->getLLVMRegNum(Reg, true).getValue());
      break;
    default:
      assert(false && "Unrecognized CFI");
      break;
    }
  }

  // if we have some registers left over, emit them
  if (!RegSet.empty())
  {
      ATS.emitRegSave(RegSet, IsVector);
  }

  CFIsPerOffset.clear();
}

void ObjectWriter::EmitARMExIdxCode(int Offset, const char *Blob)
{
  const CFI_CODE *CfiCode = (const CFI_CODE *)Blob;

  if (!CFIsPerOffset.empty() && CFIsPerOffset[0].CodeOffset != CfiCode->CodeOffset)
  {
    EmitARMExIdxPerOffset();
  }
  
  CFIsPerOffset.push_back(*CfiCode);
}
