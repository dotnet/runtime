#include "pch.h"
#include "Component.Contracts.EnumTesting.h"

namespace winrt::Component::Contracts::implementation
{
    Component::Contracts::TestEnum EnumTesting::GetA()
    {
        return TestEnum::A;
    }

    bool EnumTesting::IsB(Component::Contracts::TestEnum const& val)
    {
        return val == TestEnum::B;
    }
}
