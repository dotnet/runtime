#pragma once

#include "Component/Contracts/EnumTesting.g.h"

namespace winrt::Component::Contracts::implementation
{
    struct EnumTesting : EnumTestingT<EnumTesting>
    {
        EnumTesting() = default;

        Component::Contracts::TestEnum GetA();
        bool IsB(Component::Contracts::TestEnum const& val);
    };
}

namespace winrt::Component::Contracts::factory_implementation
{
    struct EnumTesting : EnumTestingT<EnumTesting, implementation::EnumTesting>
    {
    };
}
