#include <cstdlib>
#include <cstdio>
#include <cstring>
#include <memory>
#include <vector>

#include <internal/dnmd_tools_platform.hpp>

bool apply_deltas(mdhandle_t handle, std::vector<char const*>& deltas, std::vector<malloc_span<uint8_t>>& data)
{
    for (char const* p : deltas)
    {
        malloc_span<uint8_t> d;
        if (!read_in_file(p, d) || !get_metadata_from_file(d))
        {
            std::fprintf(stderr, "Failed to read '%s'.\n", p);
            return false;
        }

        mdhandle_ptr delta;
        if (!create_mdhandle(d, delta))
        {
            std::fprintf(stderr, "Failed to create handle for '%s'.\n", p);
            return false;
        }

        if (!md_apply_delta(handle, delta.get()))
        {
            std::fprintf(stderr, "Failed to apply delta, '%s'.\n", p);
            return false;
        }

        // Store the loaded delta data
        data.push_back(std::move(d));
    }
    return true;
}

struct dump_config_t final
{
    dump_config_t()
        : path{}
        , delta_paths{}
        , data{}
        , table_id{ -1 }
    { }

    dump_config_t(dump_config_t const& other) = delete;
    dump_config_t(dump_config_t&& other) = default;

    char const* path;
    std::vector<char const*> delta_paths;
    std::vector<malloc_span<uint8_t>> data;
    int32_t table_id;
};

void dump(dump_config_t cfg)
{
    malloc_span<uint8_t> b;
    if (!read_in_file(cfg.path, b))
    {
        std::fprintf(stderr, "Failed to read in '%s'\n", cfg.path);
        return;
    }

    if (!get_metadata_from_pe(b) && !get_metadata_from_file(b))
    {
        std::fprintf(stderr, "Failed to read file as PE or metadata blob.\n");
        return;
    }

    std::printf("Loaded '%s'.\n    Metadata blob size %zu bytes\n", cfg.path, b.size());
    if (cfg.table_id != -1)
        std::printf("    Reading in table %d (0x%x)\n", cfg.table_id, cfg.table_id);

    mdhandle_ptr handle;
    if (!create_mdhandle(b, handle)
        || !apply_deltas(handle.get(), cfg.delta_paths, cfg.data)
        || !md_validate(handle.get())
        || !md_dump_tables(handle.get(), cfg.table_id))
    {
        std::fprintf(stderr, "invalid metadata!\n");
    }
}

static char const* s_usage = "Syntax: mddump [-t <table_id>]? [-d <path_to_delta>]* <path ecma-335 data>";

int main(int ac, char** av)
{
    if (ac <= 1)
    {
        std::fprintf(stderr, "Missing metadata file.\n\n%s\n", s_usage);
        return EXIT_FAILURE;
    }

    dump_config_t cfg;

    // Process arguments
    span<char*> args{ &av[1], (size_t)ac - 1 };
    for (size_t i = 0; i < args.size(); ++i)
    {
        char* arg = args[i];
        if (arg[0] != '-')
        {
            cfg.path = arg;
            continue;
        }

        size_t len = strlen(arg);
        if (len >= 2)
        {
            switch (arg[1])
            {
            case 't':
            {
                i++;
                if (i >= args.size())
                {
                    std::fprintf(stderr, "Missing table ID.\n");
                    return EXIT_FAILURE;
                }

                cfg.table_id = (int32_t)::strtoul(args[i], nullptr, 0);
                if ((errno == ERANGE) || cfg.table_id >= 64)
                {
                    std::fprintf(stderr, "Invalid table ID: '%s'. Must be [0, 64)\n", args[i]);
                    return EXIT_FAILURE;
                }
                continue;
            }
            case 'd':
            {
                i++;
                if (i >= args.size())
                {
                    std::fprintf(stderr, "Missing delta file.\n");
                    return EXIT_FAILURE;
                }
                cfg.delta_paths.push_back(args[i]);
                continue;
            }
            case 'h':
            case '?':
                std::printf("%s\n", s_usage);
                return EXIT_SUCCESS;
            default:
                break;
            }
        }

        std::fprintf(stderr, "Invalid argument: '%s'\n\n%s\n", arg, s_usage);
        return EXIT_FAILURE;
    }

    dump(std::move(cfg));

    return EXIT_SUCCESS;
}
