#include "error_writer_redirector.h"
#include "hostpolicy.h"

namespace
{
    thread_local static pal::stringstream_t g_error_output;
    thread_local static const pal::char_t* g_prefix = nullptr;

    void HOSTPOLICY_CALLTYPE error_writer(const pal::char_t* message)
    {
        if (g_prefix != nullptr)
            g_error_output << g_prefix;

        g_error_output << message;
    }
}

error_writer_redirector::error_writer_redirector(set_error_writer_fn set_error_writer, const pal::char_t* prefix)
    : _set_error_writer(set_error_writer)
{
    g_prefix = prefix;
    g_error_output.clear();
    _previous_writer = _set_error_writer(error_writer);
}

error_writer_redirector::~error_writer_redirector()
{
    _set_error_writer(_previous_writer);
}

bool error_writer_redirector::has_errors()
{
    return g_error_output.tellp() != std::streampos(0);
}

const pal::string_t error_writer_redirector::get_errors()
{
    return g_error_output.str();
}
