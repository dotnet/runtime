//===---- typeBuilder.h -----------------------------------------*- C++ -*-===//
//
// type builder is used to convert .Net types into debuginfo.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#pragma once

#include "llvm/MC/MCObjectStreamer.h"

#include <string>
#include <vector>

using namespace llvm;

// Keep in sync with Internal.TypeSystem.TypeFlags (Common/src/TypeSystem/Common/TypeFlags.cs)
enum class PrimitiveTypeFlags {
  Unknown = 0x00,
  Void    = 0x01,
  Boolean = 0x02,
  Char    = 0x03,
  SByte   = 0x04,
  Byte    = 0x05,
  Int16   = 0x06,
  UInt16  = 0x07,
  Int32   = 0x08,
  UInt32  = 0x09,
  Int64   = 0x0A,
  UInt64  = 0x0B,
  IntPtr  = 0x0C,
  UIntPtr = 0x0D,
  Single  = 0x0E,
  Double  = 0x0F
};

typedef unsigned long long uint64;

#pragma pack(push, 8)

extern "C" struct EnumRecordTypeDescriptor {
  uint64 Value;
  const char *Name;
};

extern "C" struct EnumTypeDescriptor {
  uint32_t ElementType;
  uint64 ElementCount;
  const char *Name;
};

extern "C" struct ClassTypeDescriptor {
  int32_t IsStruct;
  const char *Name;
  uint32_t BaseClassId;
  uint64 InstanceSize;
};

extern "C" struct DataFieldDescriptor {
  uint32_t FieldTypeIndex;
  uint64 Offset;
  const char *Name;
};

extern "C" struct StaticDataFieldDescriptor {
  const char *StaticDataName;
  uint64 StaticOffset;
  int IsStaticDataInObject;
};

extern "C" struct ClassFieldsTypeDescriptior {
  uint64 Size;
  int32_t FieldsCount;
};

extern "C" struct ArrayTypeDescriptor {
  uint32_t Rank;
  uint32_t ElementType;
  uint32_t Size; // ElementSize
  int32_t IsMultiDimensional;
};

extern "C" struct PointerTypeDescriptor {
  uint32_t ElementType;
  int32_t IsReference;
  int32_t IsConst;
  int32_t Is64Bit;
};

extern "C" struct MemberFunctionTypeDescriptor {
  uint32_t ReturnType;
  uint32_t ContainingClass;
  uint32_t TypeIndexOfThisPointer;
  int32_t ThisAdjust;
  uint32_t CallingConvention;
  uint16_t NumberOfArguments;
};

extern "C" struct MemberFunctionIdTypeDescriptor {
  uint32_t MemberFunction;
  uint32_t ParentClass;
  const char *Name;
};

#pragma pack(pop)
class UserDefinedTypesBuilder {
public:
  UserDefinedTypesBuilder() : Streamer(nullptr), TargetPointerSize(0) {}
  virtual ~UserDefinedTypesBuilder() {}
  void SetStreamer(MCObjectStreamer *Streamer) {
    assert(this->Streamer == nullptr);
    assert(Streamer != nullptr);
    this->Streamer = Streamer;
  }
  MCObjectStreamer *GetStreamer() const {
    return Streamer;
  }
  void SetTargetPointerSize(unsigned TargetPointerSize) {
    assert(this->TargetPointerSize == 0);
    assert(TargetPointerSize != 0);
    this->TargetPointerSize = TargetPointerSize;
  }
  virtual void EmitTypeInformation(MCSection *TypeSection, MCSection *StrSection = nullptr) = 0;

  virtual unsigned GetEnumTypeIndex(const EnumTypeDescriptor &TypeDescriptor,
                                    const EnumRecordTypeDescriptor *TypeRecords) = 0;
  virtual unsigned GetClassTypeIndex(const ClassTypeDescriptor &ClassDescriptor) = 0;
  virtual unsigned GetCompleteClassTypeIndex(
      const ClassTypeDescriptor &ClassDescriptor,
      const ClassFieldsTypeDescriptior &ClassFieldsDescriptor,
      const DataFieldDescriptor *FieldsDescriptors,
      const StaticDataFieldDescriptor *StaticsDescriptors) = 0;

  virtual unsigned GetArrayTypeIndex(const ClassTypeDescriptor &ClassDescriptor,
                                     const ArrayTypeDescriptor &ArrayDescriptor) = 0;

  virtual unsigned GetPointerTypeIndex(const PointerTypeDescriptor& PointerDescriptor) = 0;

  virtual unsigned GetMemberFunctionTypeIndex(const MemberFunctionTypeDescriptor& MemberDescriptor,
      uint32_t const *const ArgumentTypes) = 0;

  virtual unsigned GetMemberFunctionId(const MemberFunctionIdTypeDescriptor& MemberIdDescriptor) = 0;

  virtual unsigned GetPrimitiveTypeIndex(PrimitiveTypeFlags Type) = 0;

  virtual const std::vector<std::pair<std::string, uint32_t>> &GetUDTs() {
    return UserDefinedTypes;
  }

protected:
  MCObjectStreamer *Streamer;
  unsigned TargetPointerSize;

  std::vector<std::pair<std::string, uint32_t>> UserDefinedTypes;
};
