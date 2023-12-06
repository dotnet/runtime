
#include "regnative.hpp"

using Assert::Violation;

Violation::Violation(char const* source, size_t line, char const* funcName, std::string const& msg)
{
    std::stringstream ss;
    ss << source << "(" << line << "):'" << msg << "' in " << funcName << ".";
    _message = ss.str();
}

void Assert::_True(bool result, char const* source, size_t line, char const* funcName)
{
    if (!result)
        throw Violation{ source, line, funcName, "false" };
}

TestResult ConvertViolation(Assert::Violation const& v)
{
    auto msg = v.message();
    char* block = (char*)malloc(msg.length() + 1);
    msg.copy(block, msg.length());
    block[msg.length()] = '\0';
    return { TestState::Fail, block, &free };
}
