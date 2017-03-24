using Xunit;

// The MyBigIntImp class contains public static shared state. This means it is not safe to run tests
// which modify that state in parallel. In order to run these tests in parallel, we need to fix that.
// See issue: https://github.com/dotnet/corefx/issues/17499
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]
