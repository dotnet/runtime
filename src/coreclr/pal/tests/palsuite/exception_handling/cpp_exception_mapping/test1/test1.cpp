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

#include <palsuite.h>
#undef VAL32
#include "ex.h"

template <typename Thrower>
static void VerifyExceptionMapping(const char* name, Thrower thrower, HRESULT expected)
{
    HRESULT actual = S_OK;

    EX_TRY
    {
        thrower();
    }
    EX_CATCH
    {
        actual = GET_EXCEPTION()->GetHR();
    }
    EX_END_CATCH

    if (actual != expected)
    {
        Fail("%s mapped to 0x%08x instead of 0x%08x", name, actual, expected);
    }
}

PALTEST(exception_handling_cpp_exception_mapping_test1_paltest_cpp_exception_mapping_test1,
        "exception_handling/cpp_exception_mapping/test1/paltest_cpp_exception_mapping_test1")
{
    if (PAL_Initialize(argc, argv) != 0)
    {
        return FAIL;
    }

    VerifyExceptionMapping("std::exception", [] { throw std::exception{}; }, COR_E_EXCEPTION);
    VerifyExceptionMapping("std::bad_exception", [] { throw std::bad_exception{}; }, COR_E_EXCEPTION);
    VerifyExceptionMapping("std::bad_alloc", [] { throw std::bad_alloc{}; }, COR_E_OUTOFMEMORY);
    VerifyExceptionMapping("std::bad_array_new_length", [] { throw std::bad_array_new_length{}; }, COR_E_OUTOFMEMORY);
    VerifyExceptionMapping("std::invalid_argument", [] { throw std::invalid_argument{"test"}; }, COR_E_ARGUMENT);
    VerifyExceptionMapping("std::domain_error", [] { throw std::domain_error{"test"}; }, COR_E_ARGUMENTOUTOFRANGE);
    VerifyExceptionMapping("std::length_error", [] { throw std::length_error{"test"}; }, COR_E_ARGUMENTOUTOFRANGE);
    VerifyExceptionMapping("std::out_of_range", [] { throw std::out_of_range{"test"}; }, COR_E_ARGUMENTOUTOFRANGE);
    VerifyExceptionMapping("std::range_error", [] { throw std::range_error{"test"}; }, COR_E_ARITHMETIC);
    VerifyExceptionMapping("std::overflow_error", [] { throw std::overflow_error{"test"}; }, COR_E_OVERFLOW);
    VerifyExceptionMapping("std::underflow_error", [] { throw std::underflow_error{"test"}; }, COR_E_OVERFLOW);

    std::error_code errorCode = std::make_error_code(std::errc::permission_denied);
    VerifyExceptionMapping(
        "std::system_error",
        [errorCode] { throw std::system_error{errorCode}; },
        HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED));

    VerifyExceptionMapping("std::bad_function_call", [] { throw std::bad_function_call{}; }, COR_E_INVALIDOPERATION);
    VerifyExceptionMapping("std::bad_weak_ptr", [] { throw std::bad_weak_ptr{}; }, COR_E_INVALIDOPERATION);
    VerifyExceptionMapping("std::bad_optional_access", [] { throw std::bad_optional_access{}; }, COR_E_INVALIDOPERATION);
    VerifyExceptionMapping("std::bad_variant_access", [] { throw std::bad_variant_access{}; }, COR_E_INVALIDOPERATION);
    VerifyExceptionMapping("std::logic_error", [] { throw std::logic_error{"test"}; }, COR_E_INVALIDOPERATION);
    VerifyExceptionMapping("std::runtime_error", [] { throw std::runtime_error{"test"}; }, COR_E_EXCEPTION);

    PAL_Terminate();
    return PASS;
}
