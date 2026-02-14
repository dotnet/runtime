#ifndef _TEST_REGTEST_ASSERTS_H_
#define _TEST_REGTEST_ASSERTS_H_

#include <gtest/gtest.h>
#include <gmock/gmock.h>

#define EXPECT_THAT_AND_RETURN(a, match) ([&](){ auto&& _actual = (a); EXPECT_THAT(_actual, match); return _actual; }())

template<typename T>
void AssertEqualAndSet(T& result, T&& expected, T&& actual)
{
    ASSERT_EQ(actual, expected);
    result = std::move(actual);
}

template<typename T>
void AssertEqualAndSet(std::vector<T>& result, std::vector<T>&& expected, std::vector<T>&& actual)
{
    ASSERT_THAT(actual, ::testing::ElementsAreArray(expected));
    result = std::move(actual);
}

#define ASSERT_EQUAL_AND_SET(result, expected, actual) ASSERT_NO_FATAL_FAILURE(AssertEqualAndSet(result, expected, actual))

#endif // !_TEST_REGTEST_ASSERTS_H_
