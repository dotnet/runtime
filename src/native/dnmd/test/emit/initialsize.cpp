#include "emit.hpp"
#include <metadatainitialsize.h>

TEST(InitialSize, AcceptsOnlySupportedAllocationHints)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));

    VARIANT hint{};
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataInitialSize, &hint));
    EXPECT_EQ(VT_UI4, V_VT(&hint));
    EXPECT_EQ(MDInitialSizeDefault, V_UI4(&hint));

    V_VT(&hint) = VT_UI4;
    V_UI4(&hint) = MDInitialSizeMinimal;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataInitialSize, &hint));
    V_UI4(&hint) = MDInitialSizeDefault;
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataInitialSize, &hint));
    EXPECT_EQ(MDInitialSizeMinimal, V_UI4(&hint));

    V_VT(&hint) = VT_I4;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataInitialSize, &hint));
    V_VT(&hint) = VT_UI4;
    V_UI4(&hint) = 2;
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataInitialSize, &hint));
    EXPECT_EQ(E_INVALIDARG, dispenser->SetOption(MetaDataInitialSize, nullptr));
    EXPECT_EQ(E_INVALIDARG, dispenser->GetOption(MetaDataInitialSize, nullptr));
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataInitialSize, &hint));
    EXPECT_EQ(MDInitialSizeMinimal, V_UI4(&hint));

    minipal::com_ptr<IMetaDataEmit> minimalScope;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&minimalScope));

    V_UI4(&hint) = MDInitialSizeDefault;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataInitialSize, &hint));
    minipal::com_ptr<IMetaDataEmit> defaultScope;
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0,
        IID_IMetaDataEmit, (IUnknown**)&defaultScope));

    DWORD minimalSize, defaultSize;
    ASSERT_EQ(S_OK, minimalScope->GetSaveSize(cssAccurate, &minimalSize));
    ASSERT_EQ(S_OK, defaultScope->GetSaveSize(cssAccurate, &defaultSize));
    EXPECT_EQ(minimalSize, defaultSize);

    mdTypeDef minimalType, defaultType;
    ASSERT_EQ(S_OK, minimalScope->DefineTypeDef(W("Type"), tdPublic, mdTypeDefNil,
        nullptr, &minimalType));
    ASSERT_EQ(S_OK, defaultScope->DefineTypeDef(W("Type"), tdPublic, mdTypeDefNil,
        nullptr, &defaultType));
    EXPECT_EQ(minimalType, defaultType);
}
