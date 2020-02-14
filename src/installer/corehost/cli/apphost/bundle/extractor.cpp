// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "extractor.h"
#include "error_codes.h"
#include "dir_utils.h"
#include "pal.h"
#include "utils.h"

using namespace bundle;

// Compute the final extraction location as:
// m_extraction_dir = $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/<id>/...
//
// If DOTNET_BUNDLE_EXTRACT_BASE_DIR is not set in the environment, the 
// base directory defaults to $TMPDIR/.net
void extractor_t::determine_extraction_dir()
{
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
    append_path(&m_extraction_dir, host_name.c_str());
    append_path(&m_extraction_dir, m_bundle_id.c_str());

    trace::info(_X("Files embedded within the bundled will be extracted to [%s] directory."), m_extraction_dir.c_str());
}

// Compute the working extraction location for this process, before the 
// extracted files are committed to the final location
// m_working_extraction_dir = $DOTNET_BUNDLE_EXTRACT_BASE_DIR/<app>/<proc-id-hex>
void extractor_t::determine_working_extraction_dir()
{
    m_working_extraction_dir = get_directory(extraction_dir());
    pal::char_t pid[32];
    pal::snwprintf(pid, 32, _X("%x"), pal::get_pid());
    append_path(&m_working_extraction_dir, pid);

    dir_utils_t::create_directory_tree(m_working_extraction_dir);

    trace::info(_X("Temporary directory used to extract bundled files is [%s]."), m_working_extraction_dir.c_str());
}

// Create a file to be extracted out on disk, including any intermediate sub-directories.
FILE* extractor_t::create_extraction_file(const pal::string_t& relative_path)
{
    pal::string_t file_path = m_working_extraction_dir;
    append_path(&file_path, relative_path.c_str());

    // m_working_extraction_dir is assumed to exist, 
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

pal::string_t& extractor_t::extraction_dir()
{
    if (m_extraction_dir.empty())
    {
        determine_extraction_dir();
    }

    return m_extraction_dir;
}

bool extractor_t::can_reuse_extraction()
{
    // In this version, the extracted files are assumed to be 
    // correct by construction.
    // 
    // Files embedded in the bundle are first extracted to m_working_extraction_dir
    // Once all files are successfully extracted, the extraction location is 
    // committed (renamed) to m_extraction_dir. Therefore, the presence of 
    // m_extraction_dir means that the files are pre-extracted. 

    return pal::directory_exists(extraction_dir());
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
    //    extraction location (m_working_extraction_dir)
    // 2) Upon successful extraction, m_working_extraction_dir is renamed to the actual
    //    extraction location (m_extraction_dir)
    //    
    // This effectively creates a file-lock to protect against races and failed extractions.

    determine_working_extraction_dir();
}

void extractor_t::commit()
{
    // Commit files to the final extraction directory
    // Retry the move operation with some wait in between the attempts. This is to workaround for possible file locking
    // caused by AV software. Basically the extraction process above writes a bunch of executable files to disk
    // and some AV software may decide to scan them on write. If this happens the files will be locked which blocks
    // our ablity to move them.
    int retry_count = 500;
    while (true)
    {
        if (pal::rename(m_working_extraction_dir.c_str(), m_extraction_dir.c_str()) == 0)
            break;

        bool should_retry = errno == EACCES;
        if (can_reuse_extraction())
        {
            // Another process successfully extracted the dependencies
            trace::info(_X("Extraction completed by another process, aborting current extraction."));

            dir_utils_t::remove_directory_tree(m_working_extraction_dir);
            break;
        }

        if (should_retry && (retry_count--) > 0)
        {
            trace::info(_X("Retrying extraction due to EACCES trying to rename the extraction folder to [%s]."), m_extraction_dir.c_str());
            pal::sleep(100);
            continue;
        }
        else
        {
            trace::error(_X("Failure processing application bundle."));
            trace::error(_X("Failed to commit extracted files to directory [%s]."), m_extraction_dir.c_str());
            throw StatusCode::BundleExtractionFailure;
        }
    }
}

void extractor_t::extract(const manifest_t& manifest, reader_t& reader)
{
    begin();
    for (const file_entry_t& entry : manifest.files) {
        extract(entry, reader);
    }
    commit();
}
