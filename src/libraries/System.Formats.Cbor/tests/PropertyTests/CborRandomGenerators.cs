using System.Collections.Generic;
using System.Formats.Cbor.Tests.DataModel;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;

namespace System.Formats.Cbor.Tests
{
    public class CborRandomGenerators
    {
        // Custom ArbMap that includes all arbitraries in this class for reflective generation of CborDocument
        private static readonly IArbMap s_customArbMap = ArbMap.Default
            .Merge<CborRandomGenerators>();

        public static Arbitrary<CborPropertyTestContext> PropertyTestInput()
        {
            Arbitrary<NonEmptyArray<CborDocument>> documentArb = s_customArbMap.ArbFor<NonEmptyArray<CborDocument>>();
            Arbitrary<bool> convertArb = s_customArbMap.ArbFor<bool>();
            Gen<CborConformanceMode> conformanceModes = Gen.Elements(
                CborConformanceMode.Lax,
                CborConformanceMode.Strict,
                CborConformanceMode.Canonical,
                CborConformanceMode.Ctap2Canonical);

            Gen<CborPropertyTestContext> inputGen =
                from docs in documentArb.Generator
                from convert in convertArb.Generator
                from mode in conformanceModes
                select CborPropertyTestContextHelper.create(mode, convert, docs.Get);

            IEnumerable<CborPropertyTestContext> Shrinker(CborPropertyTestContext input)
            {
                var nonEmptyArrayInput = NonEmptyArray<CborDocument>.NewNonEmptyArray(input.RootDocuments);

                foreach (NonEmptyArray<CborDocument> shrunkDoc in documentArb.Shrinker(nonEmptyArrayInput))
                {
                    yield return CborPropertyTestContextHelper.create(input.ConformanceMode, input.ConvertIndefiniteLengthItems, input.RootDocuments);
                }
            }

            return Arb.From(inputGen, Shrinker);
        }

        // Do not generate null strings and byte arrays
        public static Arbitrary<string> String() => ArbMap.Default.ArbFor<string>().Filter(s => s is not null);
        public static Arbitrary<byte[]> ByteArray() => ArbMap.Default.ArbFor<byte[]>().Filter(s => s is not null);

        // forgo NaN value generation in order to simplify equality checks
        public static Arbitrary<float> Single() => ArbMap.Default.ArbFor<float>().Filter(s => !float.IsNaN(s));
        public static Arbitrary<double> Double() => ArbMap.Default.ArbFor<double>().Filter(s => !double.IsNaN(s));

        // FsCheck has no built-in System.Half generator, define one here
        public static Arbitrary<Half> Half()
        {
            Arbitrary<float> singleArb = ArbMap.Default.ArbFor<float>();

            Gen<Half> generator =
                from f in singleArb.Generator
                where !float.IsNaN(f)
                select (Half)f;

            IEnumerable<Half> Shrinker(Half h)
            {
                foreach (float shrunk in singleArb.Shrinker((float)h))
                {
                    yield return (Half)shrunk;
                }
            }

            return Arb.From(generator, Shrinker);
        }
    }
}
