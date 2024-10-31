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
        std::printf("Reading in delta image '%s'.\n", p);
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

struct merge_config_t final
{
    merge_config_t()
        : path{}
        , output_path{}
        , delta_paths{}
        , data{}
    { }

    merge_config_t(merge_config_t const& other) = delete;
    merge_config_t(merge_config_t&& other) = default;

    char const* path;
    char const* output_path;
    std::vector<char const*> delta_paths;
    std::vector<malloc_span<uint8_t>> data;
};

void merge(merge_config_t cfg)
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

    mdhandle_ptr handle;
    if (!create_mdhandle(b, handle)
        || !apply_deltas(handle.get(), cfg.delta_paths, cfg.data)
        || !md_validate(handle.get()))
    {
        std::fprintf(stderr, "invalid metadata!\n");
    }

    size_t save_size;
    md_write_to_buffer(handle.get(), nullptr, &save_size);
    malloc_span<uint8_t> out_buffer { (uint8_t*)malloc(save_size), save_size };
    if (!md_write_to_buffer(handle.get(), out_buffer, &save_size))
    {
        std::fprintf(stderr, "Failed to save image.\n");
    }

    if (!write_out_file(cfg.output_path, std::move(out_buffer)))
    {
        std::fprintf(stderr, "Failed to write out '%s'\n", cfg.output_path);
        return;
    }
    std::printf("Wrote out '%s'.\n", cfg.output_path);
}

static char const* s_usage = "Syntax: mdmerge [-o <output_path>] [-d <path_to_delta>]* <path ecma-335 data>";

int main(int ac, char** av)
{
    if (ac <= 1)
    {
        std::fprintf(stderr, "Missing metadata file.\n\n%s\n", s_usage);
        return EXIT_FAILURE;
    }

    merge_config_t cfg;

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
            case 'o':
            {
                i++;
                if (i >= args.size())
                {
                    std::fprintf(stderr, "Missing output file path.\n");
                    return EXIT_FAILURE;
                }
                cfg.output_path = args[i];
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

    merge(std::move(cfg));

    return EXIT_SUCCESS;
}
