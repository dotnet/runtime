0. Deciding whether you should run stress for GC changes

Most of the code in gc.cpp is intricate - unless you are touching something that you are very confident will not have any adverse effect, you should run stress. When in doubt, feel free to ask.

1. What is this stress run?

This was adapted from one of the ways we run stress internally. The idea is that it takes a config that specifies the tests to run and runs them all in one process to stress random combinations of allocation and survival patterns. 

This is a pretty crude implementation. Feel free to improve it!

2. Setting up stress

It has 3 parts:

The stress framework is built from NDP\clr\tests\src\GC\Stress\Framework and the resulting .exe is in %CORE_ROOT%\GC\Stress\Framework\ReliabilityFramework.exe.

The tests are built from NDP\clr\tests\src\GC\Stress\Tests and the resulting .exe's are in %CORE_ROOT%\GC\Stress\Framework\*.exe.

The config is at NDP\clr\tests\src\GC\Stress\testmix_gc.config

3. Running stress

The test .exe's need to be in a directory called Tests next to ReliabilityFramework.exe. So if you keep ReliabilityFramework.exe where it is, you should make a directory called Tests in %CORE_ROOT%\GC\Stress\Framework\ and copy the tests into %CORE_ROOT%\GC\Stress\Framework\Tests.

To run stress:

%CORE_ROOT%\corerun ReliabilityFramework.exe testmix_gc.config

(or if you copied testmix_gc.config somewhere else you need to tell it so, eg, c:\TestConfigs\testmix_gc.config)

We recommand to run it for 48 hours (see the comments below on maximumExecutionTime in test config for more detail).

4. Test config

There are a few interesting things in this config:

  suppressConsoleOutputFromTests

Set this to true if you want to see the console output from tests. 

  concurrentCopies

If you specify this to something >1 it will load that many concurrent copies.

  maximumExecutionTime

Right now I set it to about 15 hours (instead of 48) because some of the tests will keep growing in memory usage. If you don't have a machine with a decent amount of memory you can change this to a smaller amount. You can always run this in a loop in a .cmd file for 48 hours:

:test
%CORE_ROOT%\corerun ReliabilityFramework.exe testmix_gc.config
goto test

Feel free to write your own tests and put them in the Tests directory and specify them in the config to run.

If you get an AV, often you would see it in different tests. But if you do get one that's consistently in one test, it's a good sign - it means it's likely that running that one test will give you the AV sooner because it exhibits a certain pattern that causes the AV. 
