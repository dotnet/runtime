# Deciding whether you should run stress for GC changes

Most of the code in `gc.cpp` is intricate - unless you are touching something that you are very confident will not have any adverse effect, you should run stress. When in doubt, feel free to ask.

# What is this stress run?

This was adapted from one of the ways we run stress internally. The idea is that it takes a config that specifies the tests to run and runs them all in one process to stress random combinations of allocation and survival patterns.

Note that these tests were picked from functional tests so some of them could have failures because some condition it checks for is not met. In stress runs, we only care about AVs so we want to run them as long as possible and don't care about failures indicated by the tests themselves.

This is a pretty crude implementation. Feel free to improve it!

# Setting up stress

It has 3 parts:

- The stress framework is built from `<REPO_ROOT>\src\coreclr\tests\src\GC\Stress\Framework`

- The tests are built from `<REPO_ROOT>\src\coreclr\tests\src\GC\Stress\Tests`

- The config is at `<REPO_ROOT>\src\coreclr\tests\src\GC\Stress\testmix_gc.config`, this will be copied to the output folder of Framework

The easiest way to build the Framework+Tests is by building all tests - `<REPO_ROOT>\src\coreclr\build_test[.bat|.sh]`

Sometimes there is a need (after initial build) to rebuild Framework+Tests. For example when modifying the Framework to add a new scenario or when investigating a failure.
In such case it is possible to go directly into the the Framework directory and build manually- Ex: `dotnet build -c:debug`.

# Running stress

The test binaries need to be in a directory called Tests next to `ReliabilityFramework.dll`. So if you keep `ReliabilityFramework.dll` where it is, you should see the test binaries copied to the `<TestBin>\GC\Stress\Framework\ReliabilityFramework\Tests`.

To run stress:

`%CORE_ROOT%\corerun ReliabilityFramework.dll testmix_gc.config`

(or if you copied `testmix_gc.config` somewhere else you need to tell it so, eg, `c:\TestConfigs\testmix_gc.config`)

We recommend to run it for 48 hours (see the comments below on `maximumExecutionTime` in test config for more detail).

# Test config

There are a few interesting things in this config:

  `suppressConsoleOutputFromTests`

Set this to true if you want to see the console output from tests.

  `concurrentCopies`

If you specify this to something >1 it will load that many concurrent copies.

  `maximumExecutionTime`

Right now I set it to about 15 hours (instead of 48) because some of the tests will keep growing in memory usage. If you don't have a machine with a decent amount of memory you can change this to a smaller amount. You can always run this in a loop in a .cmd file for 48 hours:

```
:test
%CORE_ROOT%\corerun ReliabilityFramework.dll testmix_gc.config
goto test
```

Feel free to write your own tests and put them in the Tests directory and specify them in the config to run.

If you get an AV, often you would see it in different tests. But if you do get one that's consistently in one test, it's a good sign - it means it's likely that running that one test will give you the AV sooner because it exhibits a certain pattern that causes the AV.
