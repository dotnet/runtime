using Xunit;

// Disable all tests in the assembly here. We'd prefer to disable them in src/libraries/tests.proj
// but we can't disable tests just based on RuntimeFlavor there as we build our tests once per target, not per target and runtime flavor
// in our split jobs (where we build libraries in one job and the runtime in another).
// As a result, we'll disable these tests here for now.
[assembly:SkipOnMono("All tests here use RuntimeHelpers.AllocateTypeAssociatedMemory, which is not implemented on Mono")]