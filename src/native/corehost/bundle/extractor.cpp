// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "extractor.h"
#include "error_codes.h"
#include "dir_utils.h"
#include "pal.h"
#include "utils.h"
#include <cinttypes>

#ifdef __sun
#include <alloca.h>
#endif

#if defined(NATIVE_LIBS_EMBEDDED)
extern "C"
{
#include "pal_zlib.h"
}
#endif

// Suppress prefast warning #6255: alloca indicates failure by raising a stack overflow exception
#pragma warning(disable:6255)

using namespace bundle;

pal::string_t& extractor_t::extraction_dir()
{
    if (m_extraction_dir.empty())
    {
        // Compute the final extraction location as:
        // m_extraction_dir = $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/<id>/...
        //
        // If DOTNET_BUNDLE_EXTRACT_BASE_DIR is not set in the environment,
        // a default is chosen within the temporary directory.

        if (!pal::getenv(_X("DOTNET_BUNDLE_EXTRACT_BASE_DIR"), &m_extraction_dir))
        {
            if (!pal::get_default_bundle_extraction_base_dir(m_extraction_dir))
            {
                trace::error(_X("Failure processing application bundle."));
                trace::error(_X("Failed to determine location for extracting embedded files."));
                trace::error(_X("DOTNET_BUNDLE_EXTRACT_BASE_DIR is not set, and a read-write cache directory couldn't be created."));
                throw StatusCode::BundleExtractionFailure;
            }
        }

        pal::string_t host_name = strip_executable_ext(get_filename(m_bundle_path));
        if (!pal::is_path_rooted(m_extraction_dir))
        {
            pal::string_t relative_path(m_extraction_dir);
            if (!pal::getcwd(&m_extraction_dir))
            {
                trace::error(_X("Failure processing application bundle."));
                trace::error(_X("Failed to obtain current working dir."));
                assert(m_extraction_dir.empty());
                throw StatusCode::BundleExtractionFailure;
            }

            append_path(&m_extraction_dir, relative_path.c_str());
        }

        append_path(&m_extraction_dir, host_name.c_str());
        append_path(&m_extraction_dir, m_bundle_id.c_str());

        trace::info(_X("Files embedded within the bundle will be extracted to [%s] directory."), m_extraction_dir.c_str());
    }

    return m_extraction_dir;
}

pal::string_t& extractor_t::working_extraction_dir()
{
    if (m_working_extraction_dir.empty())
    {
        // Compute the working extraction location for this process,
        // before the extracted files are committed to the final location
        // working_extraction_dir = $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/<proc-id-hex>

        m_working_extraction_dir = get_directory(extraction_dir());
        pal::char_t pid[32];
        pal::snwprintf(pid, 32, _X("%x"), pal::get_pid());
        append_path(&m_working_extraction_dir, pid);

        trace::info(_X("Temporary directory used to extract bundled files is [%s]."), m_working_extraction_dir.c_str());
    }

    return m_working_extraction_dir;
}

// Create a file to be extracted out on disk, including any intermediate sub-directories.
FILE* extractor_t::create_extraction_file(const pal::string_t& relative_path)
{
    pal::string_t file_path = working_extraction_dir();
    append_path(&file_path, relative_path.c_str());

    // working_extraction_dir is assumed to exist,
    // so we only create sub-directories if relative_path contains directories
    if (dir_utils_t::has_dirs_in_path(relative_path))
    {
        dir_utils_t::create_directory_tree(get_directory(file_path));
    }

    FILE* file = pal::file_open(file_path.c_str(), _X("wb"));

    if (file == nullptr)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to open file [%s] for writing."), file_path.c_str());
        throw StatusCode::BundleExtractionIOError;
    }

    return file;
}

// Extract one file from the bundle to disk.
void extractor_t::extract(const file_entry_t &entry, reader_t &reader)
{
    FILE* file = create_extraction_file(entry.relative_path());
    reader.set_offset(entry.offset());
    int64_t size = entry.size();
    size_t cast_size = to_size_t_dbgchecked(size);
    size_t extracted_size = 0;

    if (entry.compressedSize() != 0)
    {
#if defined(NATIVE_LIBS_EMBEDDED)
        PAL_ZStream zStream;
        zStream.nextIn = (uint8_t*)(const void*)reader;
        zStream.availIn = static_cast<uint32_t>(entry.compressedSize());

        const int Deflate_DefaultWindowBits = -15; // Legal values are 8..15 and -8..-15. 15 is the window size,
                                                   // negative val causes deflate to produce raw deflate data (no zlib header).

        int ret = CompressionNative_InflateInit2_(&zStream, Deflate_DefaultWindowBits);
        if (ret != PAL_Z_OK)
        {
            trace::error(_X("Failure initializing zLib stream."));
            throw StatusCode::BundleExtractionIOError;
        }

        const int bufSize = 4096;
        uint8_t* buf = (uint8_t*)alloca(bufSize);

        do
        {
            zStream.nextOut = buf;
            zStream.availOut = bufSize;

            ret = CompressionNative_Inflate(&zStream, PAL_Z_NOFLUSH);
            if (ret < 0)
            {
                CompressionNative_InflateEnd(&zStream);
                trace::error(_X("Failure inflating zLib stream. %s"), zStream.msg);
                throw StatusCode::BundleExtractionIOError;
            }

            int produced = bufSize - zStream.availOut;
            if (fwrite(buf, 1, produced, file) != (size_t)produced)
            {
                CompressionNative_InflateEnd(&zStream);
                trace::error(_X("I/O failure when writing decompressed file."));
                throw StatusCode::BundleExtractionIOError;
            }

            extracted_size += produced;
        } while (zStream.availOut == 0);

        CompressionNative_InflateEnd(&zStream);
#else
        trace::error(_X("Failure extracting contents of the application bundle. Compressed files used with a standalone (not singlefile) apphost."));
        throw StatusCode::BundleExtractionIOError;
#endif
    }
    else
    {
        extracted_size = fwrite(reader, 1, cast_size, file);
    }

    if (extracted_size != cast_size)
    {
        trace::error(_X("Failure extracting contents of the application bundle. Expected size:%" PRId64 " Actual size:%zu"), size, extracted_size);
        trace::error(_X("I/O failure when writing extracted files."));
        throw StatusCode::BundleExtractionIOError;
    }

    fclose(file);
}

void extractor_t::begin()
{
    // Files are extracted to a specific deterministic location on disk
    // on first run, and are available for reuse by subsequent similar runs.
    //
    // The extraction should be fault tolerant with respect to:
    //  * Failures/crashes during extraction which result in partial-extraction
    //  * Race between two or more processes concurrently attempting extraction
    //
    // In order to solve these issues, we implement a extraction as a two-phase approach:
    // 1) Files embedded in a bundle are extracted to a process-specific temporary
    //    extraction location (working_extraction_dir)
    // 2) Upon successful extraction, working_extraction_dir is renamed to the actual
    //    extraction location (extraction_dir)
    //
    // This effectively creates a file-lock to protect against races and failed extractions.


    dir_utils_t::create_directory_tree(working_extraction_dir());
}

void extractor_t::clean()
{
    dir_utils_t::remove_directory_tree(working_extraction_dir());
}

void extractor_t::commit_dir()
{
    // Commit an entire new extraction to the final extraction directory
    // Retry the move operation with some wait in between the attempts. This is to workaround for possible file locking
    // caused by AV software. Basically the extraction process above writes a bunch of executable files to disk
    // and some AV software may decide to scan them on write. If this happens the files will be locked which blocks
    // our ablity to move them.

    bool extracted_by_concurrent_process = false;
    bool extracted_by_current_process =
        dir_utils_t::rename_with_retries(working_extraction_dir(), extraction_dir(), extracted_by_concurrent_process);

    if (extracted_by_concurrent_process)
    {
        // Another process successfully extracted the dependencies
        trace::info(_X("Extraction completed by another process, aborting current extraction."));
        clean();
    }

    if (!extracted_by_current_process && !extracted_by_concurrent_process)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to commit extracted files to directory [%s]."), extraction_dir().c_str());
        throw StatusCode::BundleExtractionFailure;
    }

    trace::info(_X("Completed new extraction."));
}

void extractor_t::commit_file(const pal::string_t& relative_path)
{
    // Commit individual files to the final extraction directory.

    pal::string_t working_file_path = working_extraction_dir();
    append_path(&working_file_path, relative_path.c_str());

    pal::string_t final_file_path = extraction_dir();
    append_path(&final_file_path, relative_path.c_str());

    if (dir_utils_t::has_dirs_in_path(relative_path))
    {
        dir_utils_t::create_directory_tree(get_directory(final_file_path));
    }

    bool extracted_by_concurrent_process = false;
    bool extracted_by_current_process =
        dir_utils_t::rename_with_retries(working_file_path, final_file_path, extracted_by_concurrent_process);

    if (extracted_by_concurrent_process)
    {
        // Another process successfully extracted the dependencies
        trace::info(_X("Extraction completed by another process, aborting current extraction."));
    }

    if (!extracted_by_current_process && !extracted_by_concurrent_process)
    {
        trace::error(_X("Failure processing application bundle."));
        trace::error(_X("Failed to commit extracted files to directory [%s]."), extraction_dir().c_str());
        throw StatusCode::BundleExtractionFailure;
    }

    trace::info(_X("Extraction recovered [%s]"), relative_path.c_str());
}

void extractor_t::extract_new(reader_t& reader)
{
    begin();
    for (const file_entry_t& entry : m_manifest.files)
    {
        if (entry.needs_extraction())
        {
            extract(entry, reader);
        }
    }
    commit_dir();
}

// Verify an existing extraction contains all files listed in the bundle manifest.
// If some files are missing, extract them individually.
void extractor_t::verify_recover_extraction(reader_t& reader)
{
    pal::string_t& ext_dir = extraction_dir();
    bool recovered = false;

    for (const file_entry_t& entry : m_manifest.files)
    {
        if (!entry.needs_extraction())
        {
            continue;
        }

        pal::string_t file_path = ext_dir;
        append_path(&file_path, entry.relative_path().c_str());

        if (!pal::file_exists(file_path))
        {
            if (!recovered)
            {
                recovered = true;
                begin();
            }

            extract(entry, reader);
            commit_file(entry.relative_path());
        }
    }

    if (recovered)
    {
        clean();
    }
}

pal::string_t& extractor_t::extract(reader_t& reader)
{
    if (pal::directory_exists(extraction_dir()))
    {
        trace::info(_X("Reusing existing extraction of application bundle."));
        verify_recover_extraction(reader);
    }
    else
    {
        trace::info(_X("Starting new extraction of application bundle."));
        extract_new(reader);
    }

    return m_extraction_dir;
}
