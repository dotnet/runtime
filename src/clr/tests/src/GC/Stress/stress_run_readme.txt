0. Deciding whether you should run stress for GC changes

Most of the code in gc.cpp is intricate - unless you are touching something that you are very confident will not have any adverse effect, you should run stress. When in doubt, feel free to ask.

1. What is this stress run?

This was adapted from one of the ways we run stress internally. The idea is that it takes a config that specifies the tests to run and runs them all in one process to stress random combinations of allocation and survival patterns. 

This is a pretty crude implementation. Feel free to improve it!

2. Setting up stress

It has 3 parts:

The stress framework is built from <REPO_ROOT>\tests\src\GC\Stress\Framework and the resulting .exe is in <REPO_ROOT>\binaries\tests\<arch>\<buildtype>\GC\Stress\Framework\ReliabilityFramework.exe.

The tests are built from <REPO_ROOT>\tests\src\GC\Stress\Tests and the resulting .exe's are in <REPO_ROOT>\binaries\tests\<arch>\<buildtype>\GC\Stress\Framework\*.exe.

The config is at <REPO_ROOT>NDP\clr\tests\src\GC\Stress\testmix_gc.config, this will be copied to the output folder of Framework

3. Running stress

The test .exe's need to be in a directory called Tests next to ReliabilityFramework.exe. So if you keep ReliabilityFramework.exe where it is, you should see the test binaries copied to the <REPO_ROOT>\tests\src\GC\Stress\Framework\Tests.

To run stress:

%CORE_ROOT%\corerun ReliabilityFramework.exe testmix_gc.config

(or if you copied testmix_gc.config somewhere else you need to tell it so, eg, c:\TestConfigs\testmix_gc.config)

4. Test config

There are a few interesting things in this config:

suppressConsoleOutputFromTests <-- set this to true if you want to see the console output from tests. 
concurrentCopies="1" <-- if you specify this to something >1 it will load that many concurrent copies.

Feel free to write your own tests and put them in the Tests directory and specify them in the config to run.


