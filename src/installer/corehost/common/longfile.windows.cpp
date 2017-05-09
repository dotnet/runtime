// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//The logic in this file was ported from https://github.com/dotnet/coreclr/blob/54891e0650e69f08832f75a40dc102efc6115d38/src/utilcode/longfilepathwrappers.cpp
//Please reflect any change here into the above file too!
#include "pal.h"
#include "trace.h"
#include "utils.h"
#include "longfile.h"

const pal::char_t LongFile::DirectorySeparatorChar = _X('\\');
const pal::char_t LongFile::AltDirectorySeparatorChar = _X('/');
const pal::char_t LongFile::VolumeSeparatorChar = _X(':');
const pal::string_t LongFile::ExtendedPrefix = _X("\\\\?\\");
const pal::string_t LongFile::DevicePathPrefix = _X("\\\\.\\");
const pal::string_t LongFile::UNCExtendedPathPrefix = _X("\\\\?\\UNC\\");
const pal::string_t LongFile::UNCPathPrefix = _X("\\\\");

bool ShouldNormalizeWorker(const pal::string_t& path)
{
    if (path.empty() || LongFile::IsDevice(path) || LongFile::IsExtended(path) || LongFile::IsUNCExtended(path))
        return false;

    if (!LongFile::IsPathNotFullyQualified(path) && path.size() < MAX_PATH)
        return false;

    return true;
}

//For longpath names on windows, if the paths are normalized they are always prefixed with
//extended syntax, Windows does not do any more normalizations on this string and uses it as is
//So we should ensure that there are NO adjacent DirectorySeparatorChar 
bool AssertRepeatingDirSeparator(const pal::string_t& path)
{
    if (path.empty())
        return true;

    pal::string_t path_to_check = path;
    if (LongFile::IsDevice(path))
    {
        path_to_check.erase(0, LongFile::DevicePathPrefix.length());
    }
    else if (LongFile::IsExtended(path))
    {
        path_to_check.erase(0, LongFile::ExtendedPrefix.length());
    }
    else if (LongFile::IsUNCExtended(path))
    {
        path_to_check.erase(0, LongFile::UNCExtendedPathPrefix.length());
    }
    else if (path_to_check.compare(0, LongFile::UNCPathPrefix.length(), LongFile::UNCPathPrefix) == 0)
    {
        path_to_check.erase(0, LongFile::UNCPathPrefix.length());
    }

    pal::string_t dirSeparator;
    dirSeparator.push_back(LongFile::DirectorySeparatorChar);
    dirSeparator.push_back(LongFile::DirectorySeparatorChar);

    assert(path_to_check.find(dirSeparator) == pal::string_t::npos);

    pal::string_t altDirSeparator;
    altDirSeparator.push_back(LongFile::AltDirectorySeparatorChar);
    altDirSeparator.push_back(LongFile::AltDirectorySeparatorChar);

    assert(path_to_check.find(altDirSeparator) == pal::string_t::npos);

    return true;
}
bool  LongFile::ShouldNormalize(const pal::string_t& path)
{
    bool retval = ShouldNormalizeWorker(path);
    assert(retval || AssertRepeatingDirSeparator(path));
    return retval;
}

bool LongFile::IsExtended(const pal::string_t& path)
{
    return path.compare(0, ExtendedPrefix.length(), ExtendedPrefix) == 0;
}

bool LongFile::IsUNCExtended(const pal::string_t& path)
{
    return path.compare(0, UNCExtendedPathPrefix.length(), UNCExtendedPathPrefix) == 0;
}

bool LongFile::IsDevice(const pal::string_t& path)
{
    return path.compare(0, DevicePathPrefix.length(), DevicePathPrefix) == 0;
}

// Relative here means it could be relative to current directory on the relevant drive
// NOTE: Relative segments ( \..\) are not considered relative
// Returns true if the path specified is relative to the current drive or working directory.
// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
// validation of the path (URIs will be returned as relative as a result).
// Handles paths that use the alternate directory separator.  It is a frequent mistake to
// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.

bool LongFile::IsPathNotFullyQualified(const pal::string_t& path)
{
    if (path.length() < 2)
    {
        return true;  // It isn't fixed, it must be relative.  There is no way to specify a fixed path with one character (or less).
    }

    if (IsDirectorySeparator(path[0]))
    {
        return !IsDirectorySeparator(path[1]); // There is no valid way to specify a relative path with two initial slashes
    }

    return !((path.length() >= 3)           //The only way to specify a fixed path that doesn't begin with two slashes is the drive, colon, slash format- "i.e. C:\"
        && (path[1] == VolumeSeparatorChar)
        && IsDirectorySeparator(path[2]));
}

bool LongFile::ContainsDirectorySeparator(const pal::string_t & path)
{
    return path.find(DirectorySeparatorChar) != pal::string_t::npos ||
           path.find(AltDirectorySeparatorChar) != pal::string_t::npos;
}

bool LongFile::IsDirectorySeparator(const pal::char_t c)
{
    return c == DirectorySeparatorChar || c == AltDirectorySeparatorChar;
}
