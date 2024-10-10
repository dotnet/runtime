#include <gtest/gtest.h>
#include <string>
#ifdef BUILD_WINDOWS
#include <wil/stl.h>
#include <wil/win32_helpers.h>
#else
#define RETURN_IF_FAILED(x) { auto hr = x; if (FAILED(hr)) return hr; }
#endif
#include "baseline.h"
#include "fixtures.h"
#include "pal.hpp"

namespace TestBaseline
{
    minipal::com_ptr<IMetaDataDispenser> Metadata = nullptr;
    minipal::com_ptr<IMetaDataDispenserEx> DeltaMetadataBuilder = nullptr;
    minipal::com_ptr<ISymUnmanagedBinder> Symbol = nullptr;
}


class ThrowListener final : public testing::EmptyTestEventListener
{
    void OnTestPartResult(testing::TestPartResult const& result) override
    {
        if (result.fatally_failed())
        {
            throw testing::AssertionException(result);
        }
    }
};

int main(int argc, char** argv)
{
    RETURN_IF_FAILED(pal::GetBaselineMetadataDispenser(&TestBaseline::Metadata));

    minipal::com_ptr<IMetaDataDispenser> deltaBuilder;
    RETURN_IF_FAILED(pal::GetBaselineMetadataDispenser(&deltaBuilder));
    RETURN_IF_FAILED(deltaBuilder->QueryInterface(IID_IMetaDataDispenserEx, (void**)&TestBaseline::DeltaMetadataBuilder));

    VARIANT vt;
    V_VT(&vt) = VT_UI4;
    V_UI4(&vt) = MDUpdateExtension;
    if (HRESULT hr = TestBaseline::DeltaMetadataBuilder->SetOption(MetaDataSetENC, &vt); FAILED(hr))
        return hr;

    auto coreClrPath = pal::GetCoreClrPath();
    pal::cout() << X("Loaded metadata baseline module: ") << coreClrPath << std::endl;
    SetBaselineModulePath(std::move(coreClrPath));

#ifdef BUILD_WINDOWS
    pal::path regressionAssemblyPath;
    wil::AdaptFixedSizeToAllocatedResult(regressionAssemblyPath, [&](LPWSTR value, size_t valueLength, size_t* valueLengthNeededWithNul)
    {
        *valueLengthNeededWithNul = ::MultiByteToWideChar(CP_UTF8, 0, argv[0], (int)strlen(argv[0]), value, (int)valueLength) + 1;
        if (*valueLengthNeededWithNul == 0)
        {
            return HRESULT_FROM_WIN32(GetLastError());
        }
        return S_OK;
    });
    regressionAssemblyPath.erase(regressionAssemblyPath.find_last_of(X('\\')) + 1).append(X("Regression.TargetAssembly.dll"));
#else
    std::string regressionAssemblyPath = argv[0];
    regressionAssemblyPath.erase(regressionAssemblyPath.find_last_of(X('/')) + 1).append(X("Regression.TargetAssembly.dll"));
#endif

    SetRegressionAssemblyPath(regressionAssemblyPath);

    pal::cout() << X("Regression assembly path: ") << regressionAssemblyPath << std::endl;

    ::testing::InitGoogleTest(&argc, argv);
    testing::UnitTest::GetInstance()->listeners().Append(new ThrowListener);

    return RUN_ALL_TESTS();
}
