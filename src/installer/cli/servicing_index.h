// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "utils.h"
#include "args.h"

class servicing_index_t
{
public:
    servicing_index_t(const pal::string_t& svc_dir);

    bool find_redirection(const pal::string_t& package_name,
            const pal::string_t& package_version,
            const pal::string_t& package_relative,
            pal::string_t* redirection);

private:
    void ensure_redirections();

    std::unordered_map<pal::string_t, pal::string_t> m_redirections;
    pal::string_t m_patch_root;
    pal::string_t m_index_file;
    bool m_parsed;
};
