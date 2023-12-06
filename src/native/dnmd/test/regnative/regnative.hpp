#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cassert>

#include <internal/dnmd_platform.hpp>
#include <dnmd_interfaces.hpp>

#ifdef _MSC_VER
#define EXPORT extern "C" __declspec(dllexport)
#else
#define EXPORT extern "C" __attribute__((__visibility__("default")))
#endif // !_MSC_VER

#include <exception>
#include <string>
#include <sstream>
#include <vector>

namespace Assert
{
    class Violation : public std::exception
    {
        std::string _message;
    public:
        Violation(char const* source, size_t line, char const* funcName, std::string const& msg);

        std::string const& message() const noexcept
        {
            return _message;
        }

        char const* what() const noexcept override
        {
            return _message.c_str();
        }
    };

    void _True(bool result, char const* source, size_t line, char const* funcName);

    template<typename T>
    void _Equal(T const& expected, T const& actual, char const* source, size_t line, char const* funcName)
    {
        if (expected != actual)
        {
            std::stringstream ss;
            ss << std::hex << expected << " != " << actual << std::endl;
            throw Violation{ source, line, funcName, ss.str() };
        }
    }

    template<typename T>
    T _Equal(T&& expected, T const& actual, char const* source, size_t line, char const* funcName)
    {
        _Equal(expected, actual, source, line, funcName);
        return std::move(expected);
    }

    template<typename T>
    std::vector<T> _Equal(std::vector<T>&& expected, std::vector<T> const& actual, char const* source, size_t line, char const* funcName)
    {
        if (expected != actual)
        {
            std::stringstream ss;
            if (expected.size() != actual.size())
            {
                ss << "Size mismatch: " << expected.size() << " != " << actual.size() << "\n";
                ss << std::hex;
                char const* d = "Expect: ";
                for (auto e : expected)
                {
                    ss << d << e;
                    d = ", ";
                }
                ss << "\n";
                d = "Actual: ";
                for (auto a : actual)
                {
                    ss << d << a;
                    d = ", ";
                }
                ss << "\n";
            }
            else
            {
                auto iters = std::mismatch(std::begin(expected), std::end(expected), std::begin(actual), std::end(actual));
                if (iters.first != std::end(expected) || iters.second != std::end(actual))
                {
                    ss << "Element at " << std::distance(std::begin(expected), iters.first) << " mismatch: "
                       << std::hex << *iters.first << " != " << *iters.second;
                }
            }
            throw Violation{ source, line, funcName, ss.str() };
        }
        return std::move(expected);
    }
}

#define ASSERT_TRUE(e) Assert::_True((e), __FILE__, __LINE__, __func__)
#define ASSERT_EQUAL(e, a) Assert::_Equal((e), (a), __FILE__, __LINE__, __func__)
#define ASSERT_AND_RETURN(e, a) Assert::_Equal((e), (a), __FILE__, __LINE__, __func__)

enum class TestState : uint32_t
{
    Fail = 0,
    Pass,
};

struct TestResult final
{
    TestState State;
    char const* FailureMessage;
    void(*Free)(void*);
};

TestResult ConvertViolation(Assert::Violation const& v);

#define BEGIN_TEST()\
    try \
    { \

#define END_TEST()\
    } \
    catch (Assert::Violation const& v) \
    { \
        return ConvertViolation(v); \
    } \
    return { TestState::Pass, nullptr, nullptr };

// Used when the test calls another test case.
#define END_DELEGATING_TEST()\
    } \
    catch (Assert::Violation const& v) \
    { \
        return ConvertViolation(v); \
    }
