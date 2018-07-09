using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude {
    [SetupLinkerArgument ("--exclude-feature", "one")]
    public class OnField {
        public static void Main ()
        {
        }

        private int _featureOne;

        [Kept]
        private int _featureTwo;
    }
}
