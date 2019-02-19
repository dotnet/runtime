using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude {
    [SetupLinkerArgument ("--exclude-feature", "one")]
    public class OnMethod {
        public static void Main ()
        {
        }

        public void FeatureOne ()
        {
        }

        [Kept]
        public void FeatureTwo ()
        {
        }
    }
}
