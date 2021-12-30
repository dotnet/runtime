//===---- codeViewTypeBuilder.h ---------------------------------*- C++ -*-===//
//
// type builder is used to convert .Net types into CodeView descriptors.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#pragma once

#include "debugInfo/typeBuilder.h"
#include "llvm/DebugInfo/CodeView/ContinuationRecordBuilder.h"
#include "llvm/DebugInfo/CodeView/GlobalTypeTableBuilder.h"

#include <vector>

using namespace llvm::codeview;

class ArrayDimensionsDescriptor {
public:
  const char *GetLengthName(unsigned index);
  const char *GetBoundsName(unsigned index);

private:
  void Resize(unsigned NewSize);

  std::vector<std::string> Lengths;
  std::vector<std::string> Bounds;
};

class UserDefinedCodeViewTypesBuilder : public UserDefinedTypesBuilder {
public:
  UserDefinedCodeViewTypesBuilder();
  void EmitTypeInformation(MCSection *TypeSection, MCSection *StrSection = nullptr) override;

  unsigned GetEnumTypeIndex(const EnumTypeDescriptor &TypeDescriptor,
                            const EnumRecordTypeDescriptor *TypeRecords) override;
  unsigned GetClassTypeIndex(const ClassTypeDescriptor &ClassDescriptor) override;
  unsigned GetCompleteClassTypeIndex(
      const ClassTypeDescriptor &ClassDescriptor,
      const ClassFieldsTypeDescriptior &ClassFieldsDescriptor,
      const DataFieldDescriptor *FieldsDescriptors,
      const StaticDataFieldDescriptor *StaticsDescriptors) override;

  unsigned GetArrayTypeIndex(const ClassTypeDescriptor &ClassDescriptor,
                             const ArrayTypeDescriptor &ArrayDescriptor) override;

  unsigned GetPointerTypeIndex(const PointerTypeDescriptor& PointerDescriptor) override;

  unsigned GetMemberFunctionTypeIndex(const MemberFunctionTypeDescriptor& MemberDescriptor,
      uint32_t const *const ArgumentTypes) override;

  unsigned GetMemberFunctionId(const MemberFunctionIdTypeDescriptor& MemberIdDescriptor) override;

  unsigned GetPrimitiveTypeIndex(PrimitiveTypeFlags Type) override;

private:
  void EmitCodeViewMagicVersion();
  ClassOptions GetCommonClassOptions();

  unsigned GetEnumFieldListType(uint64 Count,
                                const EnumRecordTypeDescriptor *TypeRecords);

  void AddBaseClass(ContinuationRecordBuilder &CRB, unsigned BaseClassId);
  void AddClassVTShape(ContinuationRecordBuilder &CRB);

  BumpPtrAllocator Allocator;
  GlobalTypeTableBuilder TypeTable;

  ArrayDimensionsDescriptor ArrayDimentions;
  TypeIndex ClassVTableTypeIndex;
  TypeIndex VFuncTabTypeIndex;
};
