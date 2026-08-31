// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#pragma unmanaged
#include <vector>
#include <iostream>

namespace ExposedThis
{
    struct Relative;

    std::vector<Relative*> relatives;

    int numMissedCopies = 0;

    struct Relative
    {
        void* relative;
        Relative()
        {
            std::cout << "Registering " << std::hex << this << "\n";
            relatives.push_back(this);
            relative = this - 1;
        }

        Relative(const Relative& other)
        {
            std::cout << "Registering copy of " << std::hex << &other << " at " << this << "\n";
            relatives.push_back(this);
            relative = this - 1;
        }

        ~Relative()
        {
            auto location = std::find(relatives.begin(), relatives.end(), this);
            if (location != relatives.end())
            {
                std::cout << "Unregistering " << std::hex << this << "\n";
                relatives.erase(location);
            }
            else
            {
                std::cout << "Error: Relative object " << std::hex << this << " not registered\n";
                numMissedCopies++;
            }

            if (relative != this - 1)
            {
                std::cout << " Error: Relative object " << std::hex << this << " has invalid relative pointer " << std::hex << relative << "\n";
                numMissedCopies++;
            }
        }
    };

    void UseRelative(Relative rel)
    {
        std::cout << "Unmanaged: Using relative at address " << std::hex << &rel << "\n";
    }

    void UseRelativeManaged(Relative rel);

    void CallRelative()
    {
        Relative rel;
        UseRelativeManaged(rel);
    }

#pragma managed

    int RunScenario()
    {
        // Managed to unmanaged
        {
            Relative rel;
            UseRelative(rel);
        }

        // Unmanaged to managed
        CallRelative();

        return numMissedCopies;
    }

    void UseRelativeManaged(Relative rel)
    {
        std::cout << "Managed: Using relative at address " << std::hex << &rel << "\n";
    }
}

#pragma unmanaged
namespace ExposedThisUnsafeValueType
{
    struct Relative;

    std::vector<Relative*> relatives;

    int numMissedCopies = 0;

    struct Relative
    {
        void* relative;
        uint8_t buffer[1];
        Relative()
        {
            std::cout << "Registering " << std::hex << this << "\n";
            relatives.push_back(this);
            relative = this - 1;
        }

        Relative(const Relative& other)
        {
            std::cout << "Registering copy of " << std::hex << &other << " at " << this << "\n";
            relatives.push_back(this);
            relative = this - 1;
        }

        ~Relative()
        {
            auto location = std::find(relatives.begin(), relatives.end(), this);
            if (location != relatives.end())
            {
                std::cout << "Unregistering " << std::hex << this << "\n";
                relatives.erase(location);
            }
            else
            {
                std::cout << "Error: Relative object " << std::hex << this << " not registered\n";
                numMissedCopies++;
            }

            if (relative != this - 1)
            {
                std::cout << " Error: Relative object " << std::hex << this << " has invalid relative pointer " << std::hex << relative << "\n";
                numMissedCopies++;
            }
        }
    };

    void UseRelative(Relative rel)
    {
        std::cout << "Unmanaged: Using relative at address " << std::hex << &rel << "\n";
    }

    void UseRelativeManaged(Relative rel);

    void CallRelative()
    {
        Relative rel;
        UseRelativeManaged(rel);
    }

#pragma managed

    int RunScenario()
    {
        // Managed to unmanaged
        {
            Relative rel;
            UseRelative(rel);
        }

        // Unmanaged to managed
        CallRelative();

        return numMissedCopies;
    }

    void UseRelativeManaged(Relative rel)
    {
        std::cout << "Managed: Using relative at address " << std::hex << &rel << "\n";
    }
}

#pragma managed

public ref class TestClass
{
public:
    int ExposedThisCopyConstructorScenario()
    {
        return ExposedThis::RunScenario();
    }

    int ExposedThisUnsafeValueTypeCopyConstructorScenario()
    {
        return ExposedThisUnsafeValueType::RunScenario();
    }
};
