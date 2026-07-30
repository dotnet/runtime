// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef CDAC_H
#define CDAC_H

class CDAC final
{
public: // static
    static CDAC Create(uint64_t descriptorAddr, ICorDebugDataTarget *pDataTarget);

public:
    CDAC() = default;

    CDAC(const CDAC&) = delete;
    CDAC& operator=(const CDAC&) = delete;

    CDAC(CDAC&& other)
        : m_module{ other.m_module }
        , m_cdac_handle{ other.m_cdac_handle }
        , m_target{ other.m_target.Extract() }
    {
        other.m_module = NULL;
        other.m_cdac_handle = 0;
        other.m_target = NULL;
    }

    CDAC& operator=(CDAC&& other)
    {
        m_module = other.m_module;
        m_cdac_handle = other.m_cdac_handle;
        m_target = other.m_target.Extract();

        other.m_module = NULL;
        other.m_cdac_handle = 0;
        other.m_target = NULL;

        return *this;
    }

    ~CDAC();

    bool IsValid() const
    {
        return m_module != NULL && m_cdac_handle != 0;
    }

    void CreateSosInterface(IUnknown** sos);
    void CreateDacDbiInterface(IUnknown** dbi);

private:
    CDAC(HMODULE module, intptr_t handle, ICorDebugDataTarget* target);

private:
    HMODULE m_module;
    intptr_t m_cdac_handle;
    ReleaseHolder<ICorDebugDataTarget> m_target;
};

#endif // CDAC_H
