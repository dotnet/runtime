#include <gtest/gtest.h>
#include <string>
#include "baseline.h"
#include "fixtures.h"
#include "pal.hpp"

namespace TestBaseline
{
    dncp::com_ptr<IMetaDataDispenser> Metadata = nullptr;
    dncp::com_ptr<IMetaDataDispenserEx> DeltaMetadataBuilder = nullptr;
    dncp::com_ptr<ISymUnmanagedBinder> Symbol = nullptr;
}

#define RETURN_IF_FAILED(x) { auto hr = x; if (FAILED(hr)) return hr; }

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

    dncp::com_ptr<IMetaDataDispenser> deltaBuilder;
    RETURN_IF_FAILED(pal::GetBaselineMetadataDispenser(&deltaBuilder));
    RETURN_IF_FAILED(deltaBuilder->QueryInterface(IID_IMetaDataDispenserEx, (void**)&TestBaseline::DeltaMetadataBuilder));

    VARIANT vt;
    V_VT(&vt) = VT_UI4;
    V_UI4(&vt) = MDUpdateExtension;
    if (HRESULT hr = TestBaseline::DeltaMetadataBuilder->SetOption(MetaDataSetENC, &vt); FAILED(hr))
        return hr;

    auto coreClrPath = pal::GetCoreClrPath();
    std::cout << "Loaded metadata baseline module: " << coreClrPath.generic_string() << std::endl;
    SetBaselineModulePath(std::move(coreClrPath));

    std::filesystem::path regressionAssemblyPath = argv[0];
    regressionAssemblyPath = regressionAssemblyPath.parent_path() / "Regression.TargetAssembly.dll";

    SetRegressionAssemblyPath(regressionAssemblyPath.generic_string());

    std::cout << "Regression assembly path: " << regressionAssemblyPath.generic_string() << std::endl;

    ::testing::InitGoogleTest(&argc, argv);
    testing::UnitTest::GetInstance()->listeners().Append(new ThrowListener);

    return RUN_ALL_TESTS();
}
