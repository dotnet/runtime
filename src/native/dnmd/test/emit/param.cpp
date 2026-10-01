// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <vector>

TEST(Param, Define)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdParamDef param;
    mdMethodDef method;
    std::array<uint8_t, 4> signature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("Method"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method));
    WSTR_string paramName{ W("Param") };
    ASSERT_EQ(S_OK, emit->DefineParam(method, 1, paramName.c_str(), pdIn, ELEMENT_TYPE_VOID, nullptr, 0, &param));
    ASSERT_EQ(1, RidFromToken(param));
    ASSERT_EQ(mdtParamDef, TypeFromToken(param));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(paramName.size() + 1);
    mdMethodDef readMethod;
    ULONG readNameLength;
    ULONG flags;
    ULONG sequence;
    DWORD paramConstType;
    UVCP_CONSTANT constValue;
    ULONG constValueLength;
    ASSERT_EQ(S_OK, import->GetParamProps(param, & readMethod, & sequence, &readName[0], (ULONG)readName.capacity(), & readNameLength, & flags, & paramConstType, & constValue, & constValueLength));

    EXPECT_EQ(paramName, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(method, readMethod);
    EXPECT_EQ(1, sequence);
    EXPECT_EQ(pdIn, flags);
    EXPECT_EQ(ELEMENT_TYPE_VOID, paramConstType);
    EXPECT_EQ(nullptr, constValue);
    EXPECT_EQ(0, constValueLength);
}

TEST(Param, DefineWithConstant)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdParamDef param;
    mdMethodDef method;
    std::array<uint8_t, 4> signature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("Method"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method));
    WSTR_string paramName{ W("Param") };

    int value = 42;
    ASSERT_EQ(S_OK, emit->DefineParam(method, 1, paramName.c_str(), pdIn, ELEMENT_TYPE_I4, &value, sizeof(int), &param));
    ASSERT_EQ(1, RidFromToken(param));
    ASSERT_EQ(mdtParamDef, TypeFromToken(param));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(paramName.size() + 1);
    mdMethodDef readMethod;
    ULONG readNameLength;
    ULONG flags;
    ULONG sequence;
    DWORD paramConstType;
    UVCP_CONSTANT constValue;
    ULONG constValueLength;
    ASSERT_EQ(S_OK, import->GetParamProps(param, & readMethod, & sequence, &readName[0], (ULONG)readName.capacity(), & readNameLength, & flags, & paramConstType, & constValue, & constValueLength));

    EXPECT_EQ(paramName, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(method, readMethod);
    EXPECT_EQ(1, sequence);
    EXPECT_EQ(pdIn, flags);
    EXPECT_EQ(ELEMENT_TYPE_I4, paramConstType);

    // The constant value should be stored in the metadata in the #Blob heap, not just as a pointer to the passed-in value.
    EXPECT_NE(&value, constValue);
    EXPECT_EQ(value, *(int*)constValue);

    // Constant length only returned for string constants.
    EXPECT_EQ(0, constValueLength);
}

TEST(Param, DefineWithConstantString)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdParamDef param;
    mdMethodDef method;
    std::array<uint8_t, 4> signature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_STRING };
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("Method"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method));
    WSTR_string paramName{ W("Param") };

    WSTR_string value = { W("ConstantValue") };
    ASSERT_EQ(S_OK, emit->DefineParam(method, 1, paramName.c_str(), pdIn, ELEMENT_TYPE_STRING, value.c_str(), (ULONG)value.length(), &param));
    ASSERT_EQ(1, RidFromToken(param));
    ASSERT_EQ(mdtParamDef, TypeFromToken(param));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    WSTR_string readName;
    readName.resize(paramName.size() + 1);
    mdMethodDef readMethod;
    ULONG readNameLength;
    ULONG flags;
    ULONG sequence;
    DWORD paramConstType;
    UVCP_CONSTANT constValue;
    ULONG constValueLength;
    ASSERT_EQ(S_OK, import->GetParamProps(param, &readMethod, &sequence, &readName[0], (ULONG)readName.capacity(), &readNameLength, &flags, &paramConstType, &constValue, &constValueLength));

    EXPECT_EQ(paramName, readName.substr(0, readNameLength - 1));
    EXPECT_EQ(method, readMethod);
    EXPECT_EQ(1, sequence);
    EXPECT_EQ(pdIn, flags);
    EXPECT_EQ(ELEMENT_TYPE_STRING, paramConstType);

    // The constant value should be stored in the metadata in the #Blob heap, not just as a pointer to the passed-in value.
    EXPECT_NE(&value, constValue);
    WSTR_string retrievedConstant{ (WCHAR*)constValue, constValueLength };
    EXPECT_EQ(value, retrievedConstant);
}

TEST(Param, DefineOutOfOrder)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));
    mdParamDef param1;
    mdParamDef param;
    mdMethodDef method;
    std::array<uint8_t, 4> signature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
    ASSERT_EQ(S_OK, emit->DefineMethod(TokenFromRid(1, mdtTypeDef), W("Method"), 0, signature.data(), (ULONG)signature.size(), 0, 0, &method));
    WSTR_string param1Name{ W("Param") };
    WSTR_string paramName{ W("Param0") };
    ASSERT_EQ(S_OK, emit->DefineParam(method, 1, param1Name.c_str(), pdIn, ELEMENT_TYPE_VOID, nullptr, 0, &param1));
    ASSERT_EQ(S_OK, emit->DefineParam(method, 0, paramName.c_str(), pdIn, ELEMENT_TYPE_VOID, nullptr, 0, &param));
    ASSERT_EQ(2, RidFromToken(param));
    ASSERT_EQ(mdtParamDef, TypeFromToken(param));

    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

    HCORENUM paramEnum = nullptr;
    mdParamDef readParams[2];
    ULONG readParamsCount;
    ASSERT_EQ(S_OK, import->EnumParams(&paramEnum, method, readParams, 2, &readParamsCount));
    import->CloseEnum(paramEnum);
    EXPECT_EQ(2, readParamsCount);
    EXPECT_EQ(param, readParams[0]);
    EXPECT_EQ(param1, readParams[1]);
}

TEST(Param, DefineAcrossMethodsInEitherOrder)
{
    for (bool earlierFirst : { true, false })
    {
        minipal::com_ptr<IMetaDataEmit> emit;
        ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

        mdTypeDef type;
        ASSERT_EQ(S_OK, emit->DefineTypeDef(W("ParamOrdering"), tdPublic, mdTypeDefNil, nullptr, &type));

        std::array<uint8_t, 3> getterSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 0, ELEMENT_TYPE_I4 };
        std::array<uint8_t, 4> setterSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT_HASTHIS, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
        mdMethodDef getter, setter;
        ASSERT_EQ(S_OK, emit->DefineMethod(type, W("get_Value"), mdPublic, getterSignature.data(),
                                          (ULONG)getterSignature.size(), 0, 0, &getter));
        ASSERT_EQ(S_OK, emit->DefineMethod(type, W("set_Value"), mdPublic, setterSignature.data(),
                                          (ULONG)setterSignature.size(), 0, 0, &setter));

        mdParamDef setterParam, getterReturn;
        if (earlierFirst)
        {
            ASSERT_EQ(S_OK, emit->DefineParam(getter, 0, W("return"), pdOut, ELEMENT_TYPE_VOID, nullptr, 0, &getterReturn));
            ASSERT_EQ(S_OK, emit->DefineParam(setter, 1, W("value"), pdIn, ELEMENT_TYPE_VOID, nullptr, 0, &setterParam));
        }
        else
        {
            ASSERT_EQ(S_OK, emit->DefineParam(setter, 1, W("value"), pdIn, ELEMENT_TYPE_VOID, nullptr, 0, &setterParam));
            ASSERT_EQ(S_OK, emit->DefineParam(getter, 0, W("return"), pdOut, ELEMENT_TYPE_VOID, nullptr, 0, &getterReturn));
        }

        EXPECT_EQ(earlierFirst ? 1u : 2u, RidFromToken(getterReturn));
        EXPECT_EQ(earlierFirst ? 2u : 1u, RidFromToken(setterParam));

        minipal::com_ptr<IMetaDataImport> import;
        ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));

        DWORD size;
        ASSERT_EQ(S_OK, emit->GetSaveSize(cssAccurate, &size));
        std::vector<uint8_t> metadata(size);
        ASSERT_EQ(S_OK, emit->SaveToMemory(metadata.data(), size));
        minipal::com_ptr<IMetaDataDispenser> dispenser;
        ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
        minipal::com_ptr<IMetaDataImport> reopened;
        ASSERT_EQ(S_OK, dispenser->OpenScopeOnMemory(metadata.data(), size, ofReadOnly | ofCopyMemory,
            IID_IMetaDataImport, (IUnknown**)&reopened));

        for (IMetaDataImport* scope : { import.p, reopened.p })
        {
            mdParamDef tokens[] = { getterReturn, setterParam };
            mdMethodDef methods[] = { getter, setter };
            ULONG sequences[] = { 0, 1 };
            for (size_t i = 0; i < 2; ++i)
            {
                HCORENUM enumeration = nullptr;
                mdParamDef parameters[2];
                ULONG count;
                ASSERT_EQ(S_OK, scope->EnumParams(&enumeration, methods[i], parameters, 2, &count));
                scope->CloseEnum(enumeration);
                ASSERT_EQ(1u, count);
                EXPECT_EQ(tokens[i], parameters[0]);

                mdMethodDef owner;
                ULONG sequence, nameLength, flags, constantLength;
                DWORD constantType;
                UVCP_CONSTANT constant;
                WCHAR name[16];
                ASSERT_EQ(S_OK, scope->GetParamProps(tokens[i], &owner, &sequence, name, 16, &nameLength,
                    &flags, &constantType, &constant, &constantLength));
                EXPECT_EQ(methods[i], owner);
                EXPECT_EQ(sequences[i], sequence);
                EXPECT_EQ(i == 0 ? pdOut : pdIn, flags);
            }
        }
    }
}

TEST(Param, RejectsWrongMethodToken)
{
    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_NO_FATAL_FAILURE(CreateEmit(emit));

    mdTypeDef type;
    ASSERT_EQ(S_OK, emit->DefineTypeDef(W("InvalidParamOwner"), tdPublic, mdTypeDefNil, nullptr, &type));

    mdParamDef param = mdParamDefNil;
    EXPECT_EQ(E_FAIL, emit->DefineParam(type, 0, W("result"), pdOut, ELEMENT_TYPE_VOID, nullptr, 0, &param));
    EXPECT_EQ(mdParamDefNil, param);
}
