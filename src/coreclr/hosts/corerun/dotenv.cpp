// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "dotenv.hpp"
#include <istream>
#include <locale>
#include <sstream>
#include <algorithm>

namespace
{
    bool read_var_name(std::istream& file, std::string& var_name_out)
    {
        std::string var_name;

        char next;

        file.get(next);

        while (!file.eof() && isspace(next))
        {
            file.get(next);
        }
        if (file.eof())
        {
            return true;
        }

        if (next == '#')
        {
            // This is a comment, skip the line
            std::string comment;
            std::getline(file, comment);
        }

        bool key_quoted = next == '\'';

        if (key_quoted)
        {
            file.get(next);
        }

        if (!isalpha(next) && next != '_')
        {
            return false;
        }
        var_name.push_back(next);

        while (!file.eof())
        {
            if (key_quoted && file.peek() == '\'')
            {
                break;
            }
            else if (file.peek() == '=')
            {
                break;
            }

            file.get(next);
            if (isspace(next))
            {
                continue;
            }
            if (next == '#')
            {
                // Comments in names are unsupported
                return false;
            }
            if (!isalnum(next) && next != '_')
            {
                return false;
            }
            var_name.push_back(next);
        }

        if (key_quoted && file.get() == '\'')
        {
            while (!file.eof() && file.peek() != '=')
            {
                file.get(next);
                if (!isspace(next))
                {
                    return false;
                }
            }
        }

        if (!file.eof())
        {
            // read the = sign
            (void)file.get();
        }

        var_name_out = var_name;

        return !file.eof();
    }

    bool check_endline(char current_char, int next_char)
    {
        return current_char == '\n' || (current_char == '\r' && next_char == '\n');
    }

    void trim_from_end(std::string &s) {
        s.erase(std::find_if(s.rbegin(), s.rend(), [](char ch) {
            return !isspace(ch);
        }).base(), s.end());
    }

    std::string read_substitution(std::istream& file, std::function<std::string(std::string)> substitution_lookup)
    {
        std::string substituted_variable_name;
        // Read the '{' character
        (void)file.get();
        bool first = false;
        bool substitute_parse_failure = false;
        while (!file.eof() && file.peek() != '}')
        {
            char var_name_next;
            file.get(var_name_next);
            //If the name is not a valid environment variable name in the format, treat is as not a substitution.
            if (!((first && isalpha(var_name_next)) || isalnum(var_name_next)) && var_name_next != '_')
            {
                substitute_parse_failure = true;
                return std::string{ "${" } + std::move(substituted_variable_name) + std::string{ var_name_next };
            }
            substituted_variable_name.push_back(var_name_next);
            first = false;
        }

        if (!file.eof() && !substitute_parse_failure && file.get() == '}')
        {
            return substitution_lookup(substituted_variable_name);
        }

        assert(file.eof());
        return substituted_variable_name;
    }

    bool read_var_value(std::istream& file, std::function<std::string(std::string)> substitution_lookup, std::string& var_value_out)
    {
        std::string var_value;
        char next;

        file.get(next);

        while (!file.eof() && isspace(next))
        {
            file.get(next);
        }

        if (file.eof())
        {
            return false;
        }

        bool is_quoted = next == '\'' || next == '"';
        char quote_char = is_quoted ? next : '\0';
        bool can_substitute = next != '\'';

        if (next == '$')
        {
            var_value.append(read_substitution(file, substitution_lookup));
        }
        else if (next == '#')
        {
            // The rest of the line is a comment. Therefore, the value of the environment variable is empty.
            var_value_out = var_value;
            return true;
        }
        else if (!is_quoted)
        {
            var_value.push_back(next);
        }

        while (!file.eof())
        {
            file.get(next);
            if (file.eof())
            {
                break;
            }
            else if (!is_quoted && next == '#')
            {
                // This is a comment, skip the rest of the line
                // and trim any whitespace from the end.
                std::string comment;
                std::getline(file, comment);
                trim_from_end(var_value);
                var_value_out = var_value;
                return true;
            }
            else if (!is_quoted && check_endline(next, file.eof() ? '\0' : file.peek()))
            {
                break;
            }
            else if (is_quoted && next == quote_char)
            {
                break;
            }
            else if (is_quoted && next == '\\')
            {
                int escaped = file.get();
                if (file.eof())
                {
                    return false;
                }
                if (quote_char == '\'')
                {
                    switch (escaped)
                    {
                    case '\\':
                    case '\'':
                        var_value.push_back((char)escaped);
                        break;
                    default:
                        return false;
                    }
                }
                else
                {
                    assert(quote_char == '"');

                    switch (escaped)
                    {
                    case '\\':
                    case '\'':
                    case '"':
                        var_value.push_back((char)escaped);
                        break;
                    case 'a':
                        var_value.push_back('\a');
                        break;
                    case 'b':
                        var_value.push_back('\b');
                        break;
                    case 'f':
                        var_value.push_back('\f');
                        break;
                    case 'n':
                        var_value.push_back('\n');
                        break;
                    case 'r':
                        var_value.push_back('\r');
                        break;
                    case 't':
                        var_value.push_back('\t');
                        break;
                    case 'v':
                        var_value.push_back('\v');
                        break;
                    default:
                        return false;
                    }
                }
            }
            else if (next == '$' && can_substitute && file.peek() == '{')
            {
                var_value.append(read_substitution(file, substitution_lookup));
            }
            else
            {
                var_value.push_back(next);
            }
        }

        var_value_out = var_value;
        if (is_quoted)
        {
            file.get(next);
            while (!file.eof() && isspace(next))
            {
                if (check_endline(next, file.eof() ? '\0' : file.peek()))
                {
                    return true;
                }
            }
            return file.eof();
        }
        return true;
    }
}


dotenv::dotenv(pal::string_t dotEnvFilePath, std::istream& contents)
    : _dotenvFilePath{dotEnvFilePath}
    , _environmentVariables{}
{
    // Peek at the start to set the eof bit if the file is empty
    while (!contents.eof())
    {
        std::string temp_name;
        std::string temp_value;
        if (!read_var_name(contents, temp_name))
        {
            _environmentVariables = {};
            break;
        }

        if (contents.eof())
        {
            _environmentVariables = {};
            break;
        }

        // Handle variable expansion scenarios
        if (!read_var_value(contents, [&](std::string name)
            {
                auto dot_env_entry = _environmentVariables.find(name);
                if (dot_env_entry != _environmentVariables.end())
                {
                    return dot_env_entry->second;
                }
                return pal::getenvA(name.c_str());
            }, temp_value))
        {
            _environmentVariables = {};
            break;
        }
        _environmentVariables.emplace(temp_name, temp_value);
    }
}

void dotenv::load_into_current_process() const
{
    for (std::pair<std::string, std::string> env_vars : _environmentVariables)
    {
        pal::string_utf8_t name_string = env_vars.first;
        pal::string_utf8_t value_string = env_vars.second;
        pal::setenvA(name_string.c_str(), std::move(value_string));
    }
}

#define THROW_IF_FALSE(stmt) if (!(stmt)) throw W(#stmt);

void dotenv::self_test()
{
    {
        std::istringstream contents{""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables.size() == 0);
    }
    {
        std::istringstream contents{"Foo=Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo=Bar # Comment"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo=\"Bar # Not a comment\""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar # Not a comment");
    }
    {
        std::istringstream contents{"Foo=# Comment"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "");
    }
    {
        std::istringstream contents{"Foo# Comment"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables.size() == 0);
    }
    {
        std::istringstream contents{"Foo=Bar#Comment"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo=A"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "A");
    }
    {
        std::istringstream contents{"Foo="};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "");
    }
    {
        std::istringstream contents{"Foo=\r\n"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "");
    }
    {
        std::istringstream contents{"A=Foo"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["A"] == "Foo");
    }
    {
        std::istringstream contents{"Foo =Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo= Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{" Foo= Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"'Foo'= Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"'Foo' = Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{" 'Foo' = Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo=\"Bar\""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo= \"Bar\""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo= \"Bar\" "};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo= 'Bar'"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo='Bar'"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo='Bar' "};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
    }
    {
        std::istringstream contents{"Foo=\"\r\nBar\""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "\r\nBar");
    }
    {
        std::istringstream contents{"Foo=\"\\r\\nBar\""};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "\r\nBar");
    }
    {
        std::istringstream contents{"Foo=Bar\r\nFoo2=Baz42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Baz42");
    }
    {
        std::istringstream contents{"Foo=Bar#Comment\r\nFoo2=Baz"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Baz");
    }
    {
        std::istringstream contents{"Foo=Bar#Comment\nFoo2=Baz"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Baz");
    }
    {
        std::istringstream contents{"Foo=Bar\nFoo2=Baz42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Baz42");
    }
    {
        std::istringstream contents{"_Foo=Bar\r\nFoo2=Baz42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["_Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Baz42");
    }
    {
        std::istringstream contents{"_Foo=Bar\r\nFoo2=${_Foo}42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["_Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "Bar42");
    }
    {
        std::istringstream contents{"_Foo=Bar\r\nFoo2=${UnusedEnvironmentVariable}42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["_Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "42");
    }
    {
        std::istringstream contents{"_Foo=Bar\r\nFoo2=${Invalid-Capture}42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["_Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "${Invalid-Capture}42");
    }
    {
        std::istringstream contents{"Foo=${Foo2}Bar\r\nFoo2=42"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "Bar");
        THROW_IF_FALSE(env._environmentVariables["Foo2"] == "42");
    }
    {
        pal::setenv(W("CORERUN_DOTENV_SELF_TEST"), W("1"));
        std::istringstream contents{"Foo=${CORERUN_DOTENV_SELF_TEST}Bar"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "1Bar");
        pal::setenv(W("CORERUN_DOTENV_SELF_TEST"), W(""));
    }
    {
        pal::setenv(W("CORERUN_DOTENV_SELF_TEST"), W("4"));
        std::istringstream contents{"Foo=${CORERUN_DOTENV_SELF_TEST}Bar\r\nCORERUN_DOTENV_SELF_TEST=10"};
        dotenv env{ W("empty.env"), contents };
        THROW_IF_FALSE(env._environmentVariables["Foo"] == "4Bar");
        THROW_IF_FALSE(env._environmentVariables["CORERUN_DOTENV_SELF_TEST"] == "10");
        pal::setenv(W("CORERUN_DOTENV_SELF_TEST"), W(""));
    }
    {
        THROW_IF_FALSE(pal::getenv(W("CORERUN_DOTENV_SELF_TEST_LOAD")) == W(""));
        std::istringstream contents{"CORERUN_DOTENV_SELF_TEST_LOAD=20"};
        dotenv env{ W("empty.env"), contents };
        env.load_into_current_process();
        THROW_IF_FALSE(pal::getenv(W("CORERUN_DOTENV_SELF_TEST_LOAD")) == W("20"));
    }
    {
        THROW_IF_FALSE(pal::getenv(W("CORERUN_DOTENV_SELF_TEST_LOAD2")) == W(""));
        pal::setenv(W("CORERUN_DOTENV_SELF_TEST_LOAD2"), W("25"));
        std::istringstream contents{"CORERUN_DOTENV_SELF_TEST_LOAD2=A"};
        dotenv env{ W("empty.env"), contents };
        env.load_into_current_process();
        THROW_IF_FALSE(pal::getenv(W("CORERUN_DOTENV_SELF_TEST_LOAD2")) == W("A"));
    }
}
