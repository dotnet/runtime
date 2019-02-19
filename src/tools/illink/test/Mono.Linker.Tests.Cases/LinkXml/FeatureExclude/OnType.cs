using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude {
    [SetupLinkerArgument ("--exclude-feature", "one")]
    public class OnType {
        public static void Main ()
        {
        }

        class FeatureOneClass
        {
        }

        [Kept]
        [KeptMember(".ctor()")]
        class FeatureTwoClass {
        }
    }
}
