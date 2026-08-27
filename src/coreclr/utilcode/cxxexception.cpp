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

#ifndef SELF_NO_HOST
void DECLSPEC_NORETURN ThrowCxxSystemError(DWORD errorCode);
#else
static void DECLSPEC_NORETURN ThrowCxxSystemError(DWORD errorCode)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
        MODE_ANY;
    }
    CONTRACTL_END;

    ThrowWin32(errorCode);
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

    Exception *result = NULL;

    // EX_TRY also converts std::bad_alloc if allocating a non-OOM runtime Exception fails.
    EX_TRY_CPP_ONLY
    {
        try
        {
            std::rethrow_exception(exception);
        }
        catch (const std::bad_alloc&)
        {
            ThrowOutOfMemory();
        }
        catch (const std::invalid_argument&)
        {
            ThrowHR(COR_E_ARGUMENT);
        }
        catch (const std::domain_error&)
        {
            ThrowHR(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::length_error&)
        {
            ThrowHR(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::out_of_range&)
        {
            ThrowHR(COR_E_ARGUMENTOUTOFRANGE);
        }
        catch (const std::range_error&)
        {
            ThrowHR(COR_E_ARITHMETIC);
        }
        catch (const std::overflow_error&)
        {
            ThrowHR(COR_E_OVERFLOW);
        }
        catch (const std::underflow_error&)
        {
            ThrowHR(COR_E_OVERFLOW);
        }
        catch (const std::system_error& systemError)
        {
            ThrowCxxSystemError(GetWin32ErrorCode(systemError.code()));
        }
        catch (const std::bad_function_call&)
        {
            ThrowHR(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_weak_ptr&)
        {
            ThrowHR(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_optional_access&)
        {
            ThrowHR(COR_E_INVALIDOPERATION);
        }
        catch (const std::bad_variant_access&)
        {
            ThrowHR(COR_E_INVALIDOPERATION);
        }
        catch (const std::logic_error&)
        {
            ThrowHR(COR_E_INVALIDOPERATION);
        }
        catch (const std::runtime_error&)
        {
            ThrowHR(COR_E_EXCEPTION);
        }
        catch (const std::exception&)
        {
            ThrowHR(COR_E_EXCEPTION);
        }
        catch (...)
        {
            _ASSERTE_ALL_BUILDS(!"Only exceptions derived from std::exception should be thrown in CoreCLR.");
            ThrowHR(COR_E_EXCEPTION);
        }
    }
    EX_CATCH_CPP_ONLY
    {
        result = EXTRACT_EXCEPTION();
    }
    EX_END_CATCH

    _ASSERTE(result != NULL);
    return result;
}
