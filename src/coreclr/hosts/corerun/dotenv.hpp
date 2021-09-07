// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef __DOTENV_HPP__
#define __DOTENV_HPP__

#include <string>
#include <map>
#include <iosfwd>

#include "corerun.hpp"

// Implements parsing and loading a .env file based on the format supported by the python-dotenv project
class dotenv
{
private:
    pal::string_t _dotenvFilePath;
    std::map<std::string, std::string> _environmentVariables;

public:
    dotenv() = default;
    dotenv(pal::string_t dotEnvFilePath, std::istream& contents);
    void load_into_current_process() const;

    static void self_test();
};

#endif // __DOTENV_HPP__
