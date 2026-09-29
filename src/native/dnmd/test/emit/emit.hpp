#ifndef DNMD_TEST_EMIT_EMIT_HPP
#define DNMD_TEST_EMIT_EMIT_HPP

#ifdef BUILD_WINDOWS
#include <wtypes.h>
#endif
#include <cstdint>
#include <cstddef>

#include <minipal_com.h>
#include <cor.h>
#include <dnmd_interfaces.hpp>
#include <gtest/gtest.h>
#include <array>
#include <string>

using WSTR_string = std::basic_string<WCHAR>;

inline void CreateEmit(minipal::com_ptr<IMetaDataEmit>& emit)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&emit));
}

inline void CreateEmit(minipal::com_ptr<IMetaDataEmit2>& emit)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit2, (IUnknown**)&emit));
}

inline void CreateEmit(minipal::com_ptr<IMetaDataAssemblyEmit>& emit)
{
    minipal::com_ptr<IMetaDataDispenser> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenser, (void**)&dispenser));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataAssemblyEmit, (IUnknown**)&emit));
}

inline void CreateThreadSafeEmit(minipal::com_ptr<IMetaDataEmit>& emit)
{
    minipal::com_ptr<IMetaDataDispenserEx> dispenser;
    ASSERT_EQ(S_OK, GetDispenser(IID_IMetaDataDispenserEx, (void**)&dispenser));

    VARIANT option{};
    ASSERT_EQ(S_OK, dispenser->GetOption(MetaDataThreadSafetyOptions, &option));
    ASSERT_EQ(MDThreadSafetyOff, V_UI4(&option));
    V_VT(&option) = VT_UI4;
    V_UI4(&option) = MDThreadSafetyOn;
    ASSERT_EQ(S_OK, dispenser->SetOption(MetaDataThreadSafetyOptions, &option));
    ASSERT_EQ(S_OK, dispenser->DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataEmit, (IUnknown**)&emit));
}
#endif // DNMD_TEST_EMIT_EMIT_HPP
