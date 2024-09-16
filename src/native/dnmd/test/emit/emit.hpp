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
#endif // DNMD_TEST_EMIT_EMIT_HPP
