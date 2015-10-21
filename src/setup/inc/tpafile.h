#ifndef TPAFILE_H
#define TPAFILE_H

#include <vector>

#include "pal.h"
#include "trace.h"

struct tpaentry_t
{
    pal::string_t library_type;
    pal::string_t library_name;
    pal::string_t library_version;
    pal::string_t library_hash;
    pal::string_t asset_type;
    pal::string_t asset_name;
    pal::string_t relative_path;
};

class tpafile
{
public:
    bool load(pal::string_t path);

    void add_from_local_dir(const pal::string_t& dir);
    void add_package_dir(pal::string_t dir);
    void add_native_search_path(pal::string_t dir);

    void write_tpa_list(pal::string_t& output);
    void write_native_paths(pal::string_t& output);

private:
    std::vector<tpaentry_t> m_entries;
    std::vector<pal::string_t> m_native_search_paths;
    std::vector<pal::string_t> m_package_search_paths;
};

#endif // TPAFILE_H
