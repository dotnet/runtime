// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "extractor.h"
#include "error_codes.h"
#include "dir_utils.h"
#include "pal.h"
#include "utils.h"

using namespace bundle;

pal::string_t& extractor_t::extraction_dir()
{
    if (m_extraction_dir.empty())
    {
        // Compute the final extraction location as:
        // m_extraction_dir = $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/<id>/...	
        //	
        // If DOTNET_BUNDLE_EXTRACT_BASE_DIR is not set in the environment, 
        // a default is choosen within the temporary directory.

        if (!pal::getenv(_X("DOTNET_BUNDLE_EXTRACT_BASE_DIR"), &m_extraction_dir))
        {
            if (!pal::get_default_bundle_extraction_base_dir(m_extraction_dir))
            {
                trace::error(_X("Failure processing application bundle."));
                trace::error(_X("Failed to determine location for extracting embedded files."));
                trace::error(_X("DOTNET_BUNDLE_EXTRACT_BASE_DIR is not set, and a read-write temp-directory couldn't be created."));
                throw StatusCode::BundleExtractionFailure;
            }
        }

        pal::string_t host_name = strip_executable_ext(get_filename(m_bundle_path));
        if (!pal::is_path_rooted(m_extraction_dir))
        {
            pal::string_t current_dir = _X(".");
            pal::string_t relative_path(m_extraction_dir);
            m_extraction_dir = pal::realpath(&current_dir);
            append_path(&m_extraction_dir, relative_path.c_str());
        }

        append_path(&m_extraction_dir, host_name.c_str());
        append_path(&m_extraction_dir, m_bundle_id.c_str());

        trace::info(_X("Files embedded within the bundled will be extracted to [%s] directory."), m_extraction_dir.c_str());
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
    size_t size = entry.size();

    if (fwrite(reader, 1, size, file) != size)
    {
        trace::error(_X("Failure extracting contents of the application bundle."));
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
