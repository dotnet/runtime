set(SOURCES
  InterlockedExchangePointer.cpp
)

add_executable(paltest_interlockedexchangepointer_test1
  ${SOURCES}
)

add_dependencies(paltest_interlockedexchangepointer_test1 coreclrpal)

target_link_libraries(paltest_interlockedexchangepointer_test1
  ${COMMON_TEST_LIBRARIES}
)
