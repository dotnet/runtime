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
class A
{
    int copyCount;

public:
    A()
    :copyCount(0)
    {};

    A(A& other)
    :copyCount(other.copyCount + 1)
    {}

    int GetCopyCount()
    {
        return copyCount;
    }
};

class B : public A
{
    int bCopyCount;

public:
    B()
    :A(),
    bCopyCount(0)
    {};

    B(B& other)
    :A(other),
    bCopyCount(other.bCopyCount + 1)
    {}

    int GetBCopyCount()
    {
        return bCopyCount;
    }
};

int Managed_GetCopyCount(A a)
{
    return a.GetCopyCount();
}

int Managed_GetCopyCount(B b)
{
    return b.GetBCopyCount();
}

#pragma unmanaged

int GetCopyCount(A a)
{
    return a.GetCopyCount();
}

int GetCopyCount_ViaManaged(A a)
{
    return Managed_GetCopyCount(a);
}

int GetCopyCount(B b)
{
    return b.GetBCopyCount();
}

int GetCopyCount_ViaManaged(B b)
{
    return Managed_GetCopyCount(b);
}

#pragma managed

public ref class TestClass
{
public:
    int PInvokeNumCopies()
    {
        A a;
        return GetCopyCount(a);
    }

    int PInvokeNumCopiesDerivedType()
    {
        B b;
        return GetCopyCount(b);
    }

    int ReversePInvokeNumCopies()
    {
        A a;
        return GetCopyCount_ViaManaged(a);
    }

    int ReversePInvokeNumCopiesDerivedType()
    {
        B b;
        return GetCopyCount_ViaManaged(b);
    }

    int ExposedThisCopyConstructorScenario()
    {
        return ExposedThis::RunScenario();
    }

    int ExposedThisUnsafeValueTypeCopyConstructorScenario()
    {
        return ExposedThisUnsafeValueType::RunScenario();
    }
};
