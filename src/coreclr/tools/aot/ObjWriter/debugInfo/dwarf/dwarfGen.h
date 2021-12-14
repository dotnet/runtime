//===---- dwarfGen.h --------------------------------------------*- C++ -*-===//
//
// dwarf generator is used to generate dwarf debuginfo.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#pragma once

#include "llvm/MC/MCObjectStreamer.h"

#include "dwarfTypeBuilder.h"
#include "jitDebugInfo.h"

#include <vector>

class VarInfo : public DwarfInfo
{
public:
  VarInfo(const DebugVarInfo &Info, bool IsThis);

  bool IsDebugLocNeeded() const { return DebugInfo.Ranges.size() > 1; }

  void DumpLocsIfNeeded(MCObjectStreamer *Streamer, MCSection *LocSection, const MCExpr *SymExpr);

  uint64_t GetStartOffset() const { return StartOffset; }

  uint64_t GetEndOffset() const { return EndOffset; }

  bool IsParam() const { return DebugInfo.IsParam; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  DebugVarInfo DebugInfo;
  MCSymbol *LocSymbol;
  bool IsThis;
  uint64_t StartOffset;
  uint64_t EndOffset;
};

class SubprogramInfo : public DwarfInfo
{
public:
  using DwarfInfo::Dump;

  SubprogramInfo(const char *Name,
                 int Size,
                 DwarfMemberFunctionIdTypeInfo *MethodTypeInfo,
                 const std::vector<DebugVarInfo> &DebugVarInfos,
                 const std::vector<DebugEHClauseInfo> &DebugEHClauseInfos);

  void Dump(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection, MCSection *LocSection);

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override {}
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  void DumpDebugLoc(MCObjectStreamer *Streamer, MCSection *LocSection);
  void DumpVars(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection);
  void DumpEHClauses(MCObjectStreamer *Streamer, MCSection *TypeSection);

  std::string Name;
  int Size;
  DwarfMemberFunctionIdTypeInfo *MethodTypeInfo;
  std::vector<DebugEHClauseInfo> DebugEHClauseInfos;
  std::vector<VarInfo> VarInfos;
};

class DwarfGen
{
public:
  DwarfGen() : Streamer(nullptr),
               TypeBuilder(nullptr),
               InfoStart(nullptr),
               InfoEnd(nullptr) {}

  void SetTypeBuilder(UserDefinedDwarfTypesBuilder *TypeBuilder);
  void EmitCompileUnit();
  void EmitSubprogramInfo(const char *FunctionName,
                          int FunctionSize,
                          unsigned MethodTypeIndex,
                          const std::vector<DebugVarInfo> &VarsInfo,
                          const std::vector<DebugEHClauseInfo> &DebugEHClauseInfos);

  void EmitAbbrev();
  void EmitAranges();
  void Finish();

private:
  MCObjectStreamer *Streamer;
  UserDefinedDwarfTypesBuilder *TypeBuilder;

  MCSymbol *InfoStart;
  MCSymbol *InfoEnd;

  std::vector<SubprogramInfo> Subprograms;
};
