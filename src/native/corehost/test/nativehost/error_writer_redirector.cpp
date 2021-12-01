#include "error_writer_redirector.h"
#include "hostpolicy.h"

namespace
{
    thread_local static const pal::char_t* g_prefix = nullptr;

    pal::stringstream_t& get_redirected_error_stream()
    {
        thread_local static pal::stringstream_t error_output;
        return error_output;
    }

    void HOSTPOLICY_CALLTYPE error_writer(const pal::char_t* message)
    {
        if (g_prefix != nullptr)
            get_redirected_error_stream() << g_prefix;

        get_redirected_error_stream() << message;
    }
}

error_writer_redirector::error_writer_redirector(set_error_writer_fn set_error_writer, const pal::char_t* prefix)
    : _set_error_writer(set_error_writer)
{
    g_prefix = prefix;
    get_redirected_error_stream().clear();
    _previous_writer = _set_error_writer(error_writer);
}

error_writer_redirector::~error_writer_redirector()
{
    _set_error_writer(_previous_writer);
}

bool error_writer_redirector::has_errors()
{
    return get_redirected_error_stream().tellp() != std::streampos(0);
}

const pal::string_t error_writer_redirector::get_errors()
{
    return get_redirected_error_stream().str();
}
