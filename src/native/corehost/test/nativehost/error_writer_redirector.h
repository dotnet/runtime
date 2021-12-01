// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <pal.h>

#if defined(_WIN32)
#define ERROR_WRITER_CALLTYPE __cdecl
#else
#define ERROR_WRITER_CALLTYPE
#endif

class error_writer_redirector
{
public:
    typedef void(ERROR_WRITER_CALLTYPE* error_writer_fn) (const pal::char_t* message);
    typedef error_writer_fn(ERROR_WRITER_CALLTYPE* set_error_writer_fn) (error_writer_fn error_writer);

    error_writer_redirector(set_error_writer_fn set_error_writer, const pal::char_t* prefix = nullptr);
    ~error_writer_redirector();

    bool has_errors();
    const pal::string_t get_errors();

private:
    set_error_writer_fn _set_error_writer;
    error_writer_fn _previous_writer;
};
