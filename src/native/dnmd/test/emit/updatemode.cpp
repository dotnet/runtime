// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "emit.hpp"
#include <mdinternalemit.h>
#include <cstring>

TEST(UpdateMode, DispenserValidatesAndReportsMode)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));

    VARIANT option{};
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(VT_UI4, V_VT(&option));
    EXPECT_EQ(MDUpdateFull, V_UI4(&option));

    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateFull;
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(MDUpdateExtension, V_UI4(&option));

    V_VT(&option) = VT_I4;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateENC;
    EXPECT_EQ(E_NOTIMPL, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateDelta;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDUpdateIncremental;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataSetUpdate, nullptr));
    EXPECT_EQ(E_INVALIDARG, dispenser->GetOption(MetaDataSetUpdate, nullptr));
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataSetUpdate, &option));
    EXPECT_EQ(MDUpdateExtension, V_UI4(&option));
}

TEST(UpdateMode, ScopesCaptureModeAndExposeInternalEmitter)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));
    VARIANT option{};
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDUpdateExtension;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));

    minipal::com_ptr<IMetaDataEmit> emit;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&emit));
    minipal::com_ptr<IMDInternalEmit> internal;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMDInternalEmit, (void**)&internal));

    ULONG previous = UINT32_MAX;
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateExtension, previous);
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateExtension, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
    EXPECT_EQ(E_NOTIMPL, internal->SetMDUpdateMode(MDUpdateENC, &previous));
    EXPECT_EQ(MDUpdateFull, previous);

    GUID expected = { 0x9be71e5c, 0xae85, 0x4ebd, { 0x81, 0x02, 0x11, 0xe7, 0x33, 0xa2, 0x15, 0x6c } };
    ASSERT_EQ(S_OK, internal->ChangeMvid(expected));
    minipal::com_ptr<IMetaDataImport> import;
    ASSERT_EQ(S_OK, emit->QueryInterface(IID_IMetaDataImport, (void**)&import));
    GUID actual{};
    ASSERT_EQ(S_OK, import->GetScopeProps(nullptr, 0, nullptr, &actual));
    EXPECT_EQ(0, std::memcmp(&expected, &actual, sizeof(expected)));

    V_UI4(&option) = MDUpdateFull;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataSetUpdate, &option));
    minipal::com_ptr<IMetaDataEmit> other;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&other));
    minipal::com_ptr<IMDInternalEmit> otherInternal;
    ASSERT_EQ(S_OK, other->QueryInterface(IID_IMDInternalEmit, (void**)&otherInternal));
    ASSERT_EQ(S_OK, otherInternal->SetMDUpdateMode(MDUpdateExtension, &previous));
    EXPECT_EQ(MDUpdateFull, previous);
    ASSERT_EQ(S_OK, internal->SetMDUpdateMode(MDUpdateFull, &previous));
    EXPECT_EQ(MDUpdateExtension, previous);
}
