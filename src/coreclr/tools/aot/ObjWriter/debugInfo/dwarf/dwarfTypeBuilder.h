//===---- dwarfTypeBuilder.h ------------------------------------*- C++ -*-===//
//
// type builder is used to convert .Net types into Dwarf debuginfo.
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
//===----------------------------------------------------------------------===//

#pragma once

#include "debugInfo/typeBuilder.h"

#include <vector>
#include <unordered_map>

class UserDefinedDwarfTypesBuilder;

class DwarfInfo
{
public:
  DwarfInfo() :
      StrSymbol(nullptr),
      InfoSymbol(nullptr),
      IsDumped(false),
      IsDumpedTypes(false) {}

  virtual ~DwarfInfo() {}

  virtual void Dump(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection);

  virtual void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection);

  MCSymbol *GetInfoSymbol() { return InfoSymbol; }

  const MCExpr *GetInfoExpr() { return InfoExpr; }

protected:
  virtual void DumpStrings(MCObjectStreamer *Streamer) = 0;
  virtual void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) = 0;

  static void EmitSectionOffset(MCObjectStreamer *Streamer,
                                MCSymbol *Symbol,
                                unsigned Size,
                                uint32_t Offset = 0);

  static const MCExpr *CreateOffsetExpr(MCContext &Context,
                                        MCSymbol *BeginSymbol,
                                        MCSymbol *Symbol);

  static void EmitOffset(MCObjectStreamer *Streamer,
                         const MCExpr *OffsetExpr,
                         unsigned Size);

  static void EmitInfoOffset(MCObjectStreamer *Streamer, const DwarfInfo *Info, unsigned Size);

  MCSymbol *StrSymbol;
  MCSymbol *InfoSymbol;
  const MCExpr *InfoExpr;

  bool IsDumped;
  bool IsDumpedTypes;
};

class DwarfPrimitiveTypeInfo : public DwarfInfo
{
public:
  DwarfPrimitiveTypeInfo(PrimitiveTypeFlags PrimitiveType) : Type(PrimitiveType) {}

  PrimitiveTypeFlags GetType() { return Type; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  PrimitiveTypeFlags Type;
};

class DwarfVoidTypeInfo : public DwarfPrimitiveTypeInfo
{
public:
  DwarfVoidTypeInfo() : DwarfPrimitiveTypeInfo(PrimitiveTypeFlags::Void) {}

protected:
  void DumpStrings(MCObjectStreamer* Streamer) override;
  void DumpTypeInfo(MCObjectStreamer* Streamer, UserDefinedDwarfTypesBuilder* TypeBuilder) override;
};

class DwarfEnumTypeInfo;

class DwarfEnumerator : public DwarfInfo
{
public:
  DwarfEnumerator(const EnumRecordTypeDescriptor &Descriptor, DwarfEnumTypeInfo *TypeInfo) :
      Name(Descriptor.Name), Value(Descriptor.Value), EnumTypeInfo(TypeInfo) {}

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  std::string Name;
  uint64 Value;
  DwarfEnumTypeInfo *EnumTypeInfo;
};

class DwarfEnumTypeInfo : public DwarfInfo
{
public:
  DwarfEnumTypeInfo(const EnumTypeDescriptor &TypeDescriptor,
                    const EnumRecordTypeDescriptor *TypeRecords);

  void Dump(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  uint8_t GetByteSize() const { return ByteSize; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  std::string Name;
  uint32_t ElementType;
  std::vector<DwarfEnumerator> Records;
  uint8_t ByteSize;
};

class DwarfDataField : public DwarfInfo
{
public:
  DwarfDataField(const DataFieldDescriptor &Descriptor) :
      Name(Descriptor.Name),
      TypeIndex(Descriptor.FieldTypeIndex),
      Offset(Descriptor.Offset) {}

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  uint32_t GetTypeIndex() const { return TypeIndex; }

  const std::string &GetName() const { return Name; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

  std::string Name;
  uint32_t TypeIndex;
  uint64 Offset;
};

class DwarfStaticDataField : public DwarfDataField
{
public:
  DwarfStaticDataField(const DataFieldDescriptor &Descriptor,
                       const StaticDataFieldDescriptor &StaticDescriptor) :
                       DwarfDataField(Descriptor),
                       StaticDataName(StaticDescriptor.StaticDataName),
                       StaticOffset(StaticDescriptor.StaticOffset),
                       StaticDataInObject(StaticDescriptor.IsStaticDataInObject) {}

  const std::string &GetStaticDataName() const { return StaticDataName; }
  uint64 GetStaticOffset() const { return StaticOffset; }
  bool IsStaticDataInObject() const { return StaticDataInObject; }

protected:
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  std::string StaticDataName;
  uint64 StaticOffset;
  bool StaticDataInObject;
};

class DwarfMemberFunctionIdTypeInfo;

class DwarfClassTypeInfo : public DwarfInfo
{
public:
  DwarfClassTypeInfo(const ClassTypeDescriptor &ClassDescriptor) :
                     Name(ClassDescriptor.Name),
                     IsStruct(ClassDescriptor.IsStruct),
                     BaseClassId(ClassDescriptor.BaseClassId),
                     Size(ClassDescriptor.InstanceSize),
                     IsForwardDecl(true) {}

  DwarfClassTypeInfo(const ClassTypeDescriptor &ClassDescriptor,
                     const ClassFieldsTypeDescriptior &ClassFieldsDescriptor,
                     const DataFieldDescriptor *FieldsDescriptors,
                     const StaticDataFieldDescriptor *StaticsDescriptors);

  void Dump(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  void AddMemberFunction(DwarfMemberFunctionIdTypeInfo* TypeInfo) {
    MemberFunctions.push_back(TypeInfo);
  }

  const std::vector<DwarfStaticDataField> &GetStaticFields() const { return StaticFields; }

  const std::string &GetName() const { return Name; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  std::string Name;
  bool IsStruct;
  uint32_t BaseClassId;
  uint64 Size;
  bool IsForwardDecl;
  std::vector<DwarfDataField> Fields;
  std::vector<DwarfStaticDataField> StaticFields;
  std::vector<DwarfMemberFunctionIdTypeInfo*> MemberFunctions;
};

class DwarfSimpleArrayTypeInfo : public DwarfInfo
{
public:
  DwarfSimpleArrayTypeInfo(uint32_t ArrayElementType, uint64_t Size) :
                           ElementType(ArrayElementType),
                           Size(Size) {}

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  uint32_t ElementType;
  uint64_t Size;
};

class DwarfPointerTypeInfo : public DwarfInfo
{
public:
  DwarfPointerTypeInfo(const PointerTypeDescriptor& PointerDescriptor) :
      TypeDesc(PointerDescriptor) {}

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  PointerTypeDescriptor TypeDesc;
};

class DwarfVoidPtrTypeInfo : public DwarfInfo
{
public:
  DwarfVoidPtrTypeInfo() {}

protected:
  void DumpStrings(MCObjectStreamer* Streamer) override;
  void DumpTypeInfo(MCObjectStreamer* Streamer, UserDefinedDwarfTypesBuilder* TypeBuilder) override;
};

class DwarfMemberFunctionTypeInfo : public DwarfInfo
{
public:
  DwarfMemberFunctionTypeInfo(const MemberFunctionTypeDescriptor& MemberDescriptor,
                              uint32_t const *const ArgumentTypes,
                              bool IsStaticMethod);

  const std::vector<uint32_t> &GetArgTypes() const { return ArgumentTypes; }

  bool IsStatic() const { return IsStaticMethod; }

  uint32_t GetReturnTypeIndex() const { return TypeDesc.ReturnType; }

  uint32_t GetThisPtrTypeIndex() const { return TypeDesc.TypeIndexOfThisPointer; }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  MemberFunctionTypeDescriptor TypeDesc;
  std::vector<uint32_t> ArgumentTypes;
  bool IsStaticMethod;
};

class DwarfMemberFunctionIdTypeInfo : public DwarfInfo
{
public:
  DwarfMemberFunctionIdTypeInfo(const MemberFunctionIdTypeDescriptor& MemberIdDescriptor,
                                DwarfMemberFunctionTypeInfo *TypeInfo) :
                                LinkageName(MemberIdDescriptor.Name),
                                Name(MemberIdDescriptor.Name),
                                ParentClass(MemberIdDescriptor.ParentClass),
                                MemberFunctionTypeInfo(TypeInfo),
                                LinkageNameSymbol(nullptr) {}

  const std::vector<uint32_t> &GetArgTypes() const { return MemberFunctionTypeInfo->GetArgTypes(); }

  void SetLinkageName(const char *Name) { LinkageName = Name; }

  void DumpTypes(UserDefinedDwarfTypesBuilder *TypeBuilder, MCObjectStreamer *Streamer,
      MCSection *TypeSection, MCSection *StrSection) override;

  bool IsStatic() const { return MemberFunctionTypeInfo->IsStatic(); }

protected:
  void DumpStrings(MCObjectStreamer *Streamer) override;
  void DumpTypeInfo(MCObjectStreamer *Streamer, UserDefinedDwarfTypesBuilder *TypeBuilder) override;

private:
  std::string LinkageName;
  std::string Name;
  uint32_t ParentClass;
  DwarfMemberFunctionTypeInfo *MemberFunctionTypeInfo;
  MCSymbol *LinkageNameSymbol;
};

template<class T>
class EnumHash
{
  typedef typename std::underlying_type<T>::type enumType;
public:
  size_t operator()(const T& elem) const {
    return std::hash<enumType>()(static_cast<enumType>(elem));
  }
};

class UserDefinedDwarfTypesBuilder : public UserDefinedTypesBuilder
{
public:
  UserDefinedDwarfTypesBuilder() {}
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

  DwarfInfo *GetTypeInfoByIndex(unsigned Index) const { return DwarfTypes[TypeIndexToArrayIndex(Index)].get(); }

  unsigned GetSimpleArrayTypeIndex(unsigned ElemIndex, unsigned Size);

  const std::vector<DwarfClassTypeInfo*> &GetClassesWithStaticFields() const { return ClassesWithStaticFields; }

private:
  static const unsigned StartTypeIndex = 1; // Make TypeIndex 0 - Invalid
  static unsigned TypeIndexToArrayIndex(unsigned TypeIndex) { return TypeIndex - StartTypeIndex; }
  static unsigned ArrayIndexToTypeIndex(unsigned ArrayIndex) { return ArrayIndex + StartTypeIndex; }

  std::vector<std::unique_ptr<DwarfInfo>> DwarfTypes;
  std::unordered_map<PrimitiveTypeFlags, uint32_t, EnumHash<PrimitiveTypeFlags>> PrimitiveDwarfTypes;
  // map[ElemTypeIndex][Size] -> ArrTypeIndex
  std::unordered_map<uint32_t, std::unordered_map<uint32_t, uint32_t>> SimpleArrayDwarfTypes;

  std::vector<DwarfClassTypeInfo*> ClassesWithStaticFields;
};
