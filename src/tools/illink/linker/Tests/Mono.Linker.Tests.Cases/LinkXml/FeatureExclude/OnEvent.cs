using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml.FeatureExclude {
    [SetupLinkerArgument ("--exclude-feature", "one")]
    public class OnEvent {
        public static void Main ()
        {
        }

        public event EventHandler<EventArgs> FeatureOne;

        [Kept]
        public event EventHandler<EventArgs> FeatureTwo {[Kept] add {}[Kept] remove {} }
    }
}
