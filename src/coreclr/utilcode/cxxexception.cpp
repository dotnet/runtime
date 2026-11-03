// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <exception>
#include <functional>
#include <memory>
#include <new>
#include <optional>
#include <stdexcept>
#include <system_error>
#include <variant>

#include "stdafx.h"
#include "dn-stdio.h"
#include "ex.h"

static Exception *GetCxxOutOfMemoryException()
{
    LIMITED_METHOD_CONTRACT;

#ifndef DACCESS_COMPILE
    VolatileStoreWithoutBarrier<HRESULT>(&g_hrFatalError, COR_E_OUTOFMEMORY);
#endif

    return Exception::GetOOMException();
}

#ifndef SELF_NO_HOST
Exception *GetExceptionFromCxxSystemError(DWORD errorCode);
#else
static Exception *GetExceptionFromCxxSystemError(DWORD errorCode)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    HRESULT hr = HRESULT_FROM_WIN32(errorCode);
    if (errorCode == ERROR_NOT_ENOUGH_MEMORY || hr == E_OUTOFMEMORY)
    {
        return GetCxxOutOfMemoryException();
    }

    return new HRException(hr);
}
#endif

static DWORD GetWin32ErrorCode(const std::error_code& errorCode)
{
    LIMITED_METHOD_CONTRACT;

    if (!errorCode)
    {
        return ERROR_GEN_FAILURE;
    }

    const std::error_category& category = errorCode.category();

#ifdef HOST_WINDOWS
    if (category == std::system_category())
    {
        return static_cast<DWORD>(errorCode.value());
    }
#endif

    bool isErrnoCategory = category == std::generic_category();
#ifdef HOST_UNIX
    isErrnoCategory = isErrnoCategory || category == std::system_category();
#endif

    if (isErrnoCategory)
    {
        return HRESULT_CODE(HRESULTFromErr(errorCode.value()));
    }

    return ERROR_GEN_FAILURE;
}

Exception *GetExceptionFromCxxException()
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    std::exception_ptr exception = std::current_exception();
    if (exception == nullptr)
    {
        return Exception::GetOOMException();
    }

    // Allocating a non-OOM runtime Exception can itself fail with OOM.
    EX_TRY_CPP_ONLY
    {
        try
        {
            std::rethrow_exception(exception);
        }
        catch (const std::bad_alloc&)
        {
            return Exception::GetOOMException();
        }
        catch (const std::invalid_argument&)
        {
            return new HRException(COR_E_ARGUMENT);
        }
        catch (const std::domain_error&)
        {
            return new HRException(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::length_error&)
        {
            return new HRException(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::out_of_range&)
        {
            return new HRException(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::range_error&)
        {
            return new HRException(COR_E_ARITHMETIC);
        }
        catch (const std::overflow_error&)
        {
            return new HRException(COR_E_OVERFLOW);
        }
        catch (const std::underflow_error&)
        {
            return new HRException(COR_E_OVERFLOW);
        }
        catch (const std::system_error& systemError)
        {
            return GetExceptionFromCxxSystemError(GetWin32ErrorCode(systemError.code()));
        }
        catch (const std::bad_function_call&)
        {
            return new HRException(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_weak_ptr&)
        {
            return new HRException(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_optional_access&)
        {
            return new HRException(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_variant_access&)
        {
            return new HRException(COR_E_INVALIDOPERATION);
        }
        catch (const std::logic_error&)
        {
            return new HRException(COR_E_INVALIDOPERATION);
        }
        catch (const std::runtime_error&)
        {
            return new HRException(COR_E_EXCEPTION);
        }
        catch (const std::exception&)
        {
            return new HRException(COR_E_EXCEPTION);
        }
        catch (...)
        {
            _ASSERTE_ALL_BUILDS(!"Only exceptions derived from std::exception should be thrown in CoreCLR.");
            return new HRException(COR_E_EXCEPTION);
        }
    }
    EX_CATCH_CPP_ONLY
    {
        // The original C++ exception is replaced with OOM here, so record the failure to keep it from being hidden.
        STRESS_LOG0(LF_EH, LL_WARNING, "GetExceptionFromCxxException: converting C++ exception failed; returning OOM\n");
        _ASSERTE(GET_EXCEPTION()->GetHR() == E_OUTOFMEMORY);
    }
    EX_END_CATCH

    return GetCxxOutOfMemoryException();
}
