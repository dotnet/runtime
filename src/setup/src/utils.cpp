#include "utils.h"

bool ends_with(const pal::string_t& value, const pal::string_t& suffix)
{
	return (0 == value.compare(value.length() - suffix.length(), suffix.length(), suffix));
}

bool find_coreclr(const pal::string_t& appbase, pal::string_t& recv)
{
	pal::string_t candidate;
	// Check if it exists in the appbase
	candidate.assign(appbase);
	append_path(candidate, LIBCORECLR_NAME);
	if (pal::file_exists(candidate))
	{
		recv.assign(appbase);
		return true;
	}

	// TODO: Have a cleaner search strategy that supports multiple versions
	// Search the PATH
	pal::string_t path;
	if (!pal::getenv(_X("PATH"), path))
	{
		return false;
	}
	pal::stringstream_t path_stream(path);
	pal::string_t entry;
	while (std::getline(path_stream, entry, PATH_SEPARATOR))
	{
		candidate.assign(entry);
		append_path(candidate, LIBCORECLR_NAME);
		if (pal::file_exists(candidate))
		{
			recv.assign(entry);
			return true;
		}
	}
	return false;
}

void append_path(pal::string_t& path1, const pal::char_t* path2)
{
	if (pal::is_path_rooted(path2))
	{
		path1.assign(path2);
	}
	else
	{
		if (path1.back() != DIR_SEPARATOR)
		{
			path1.push_back(DIR_SEPARATOR);
		}
		path1.append(path2);
	}
}

pal::string_t get_executable(const pal::string_t& filename)
{
	pal::string_t result(filename);

	if (ends_with(result, _X(".exe")))
	{
		// We need to strip off the old extension
		result.erase(result.length() - 4);
	}

	return result;
}

pal::string_t get_filename(const pal::string_t& path)
{
	// Find the last dir separator
	auto path_sep = path.find_last_of(DIR_SEPARATOR);
	if (path_sep == pal::string_t::npos)
	{
		return pal::string_t(path);
	}

	return path.substr(path_sep + 1);
}

pal::string_t get_directory(const pal::string_t& path)
{
	// Find the last dir separator
	auto path_sep = path.find_last_of(DIR_SEPARATOR);
	if (path_sep == pal::string_t::npos)
	{
		return pal::string_t(path);
	}

	return path.substr(0, path_sep);
}
