// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace System.Security.Cryptography.Dsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class DSAXml
    {
        [Fact]
        public static void TestRead512Parameters_Public()
        {
            DSAParameters expectedParameters = DSATestData.Dsa512Parameters;
            expectedParameters.X = null;

            TestReadXml(
                // Bonus trait of this XML: it shows that the namespaces of the elements are not considered
                @"
<DSAKeyValue xmlns:yep=""urn:ignored:yep"" xmlns:nope=""urn:ignored:nope"" xmlns:ign=""urn:ignored:ign"">
  <yep:P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</yep:P>
  <nope:Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</nope:Q>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
  <ign:Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</ign:Y>
</DSAKeyValue>",
                expectedParameters);
        }

        [Fact]
        public static void TestRead512Parameters_Private()
        {
            TestReadXml(
                // Bonus trait of this XML, it shows that the order doesn't matter in the elements,
                // and unknown elements are ignored
                @"
<DSAKeyValue>
  <Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</Y>
  <Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</Q>
  <BananaWeight unit=""lbs"">30000</BananaWeight>
  <X>Lj16hMhbZnheH2/nlpgrIrDLmLw=</X>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
  <P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</P>
</DSAKeyValue>",
                DSATestData.Dsa512Parameters);
        }

        [Fact]
        public static void TestRead576Parameters_Public()
        {
            DSAParameters expectedParameters = DSATestData.Dsa576Parameters;
            expectedParameters.X = null;

            TestReadXml(
                // Bonus trait of this XML: it shows that the default namespaces of the elements is not considered,
                // and is the first test to show that whitespace is not considered.
                @"
<DSAKeyValue xmlns=""urn:ignored:root"">
  <P>
    4hZzBr/9hrti9DJ7d4u/oHukIyPsVnsQa5VjiCvd1tfy7nNg8pmIjen0CmHHjQvY

    RC76nDIrhorTZ7OUHXK3ozLJVOsWKRMr
  </P>
  <Q>zNzsz18LLI/iOOLwbyITfxf66xs=</Q>
  <G>
    rxfUBhMCB54zA0p3oFjdtLgyrLEUt7jS065EUd/4XrjdddRHQhg2nUhbIgZQZAYE
    SrTmQH/apaKeldSWTKVZ6BxvfPzahyZl
  </G>
  <Y>

gVpUm2/QztrwRLALfP4TUZAtdyfW1/tzYAOk4cTNjfv0MeT/RzPz+pLHZfDP+UTj7VaoW3WVPrFpASSJhbtfiROY6rXjlkXn

  </Y>
</DSAKeyValue>",
                expectedParameters);
        }

        [Fact]
        public static void TestRead576Parameters_Private()
        {
            TestReadXml(
                // Bonus trait of this XML: it shows the root element name is not considered.
                @"
<DSA>
  <P>
    4hZzBr/9hrti9DJ7d4u/oHukIyPsVnsQa5VjiCvd1tfy7nNg8pmIjen0CmHHjQvY

    RC76nDIrhorTZ7OUHXK3ozLJVOsWKRMr
  </P>
  <Q>zNzsz18LLI/iOOLwbyITfxf66xs=</Q>
  <G>
    rxfUBhMCB54zA0p3oFjdtLgyrLEUt7jS065EUd/4XrjdddRHQhg2nUhbIgZQZAYE
    SrTmQH/apaKeldSWTKVZ6BxvfPzahyZl
  </G>
  <Y>

gVpUm2/QztrwRLALfP4TUZAtdyfW1/tzYAOk4cTNjfv0MeT/RzPz+pLHZfDP+UTj7VaoW3WVPrFpASSJhbtfiROY6rXjlkXn

  </Y>

  <X>
rDJpPhzXKtY+GgtugVfrvKZx09s=
  </X>
</DSA>",
                DSATestData.Dsa576Parameters);
        }

        [Fact]
        public static void TestRead1024Parameters_Public()
        {
            DSAParameters expectedParameters = DSATestData.GetDSA1024Params();
            expectedParameters.X = null;

            TestReadXml(
                // Bonus trait of this XML: very odd whitespace
                @"
<DSAKeyValue>
  <P>
    wW0mx01sFid5nAkYVI5VP+WMeIHaSEYpyvZDEfSyfP72vbDyEgaw/8SZmi/tU7Q7
    nuKRDGjaLENqgBj0k49kcjafVkfQBbzJbiJZDMFePNTqDRMvXaWvaqoIB7DMTvNA
    SvVC9FRrN73WpH5kETCDfbm
    Tl8hFY1
    1  9   w 2 0 F  N  + S o  S z E =
  </P>
  <Q>2DwOy3NVHi/jDVH89CNsZRiDrdc=</Q>
  <G>
    a8NmtmNVVF4Jjx/pDlRptWfgn6edgX8rNntF3s1DAaWcgdaRH3aR03DhWsaSwEvB
    GHLBcaf+ZU6WPX3aV1qemM4Cb7fTk0olhggTSo7F7WmirtyJQBtnrd5Cfxftrrct
    evRdmrHVnhsT1O + 9F8dkMwJn3eNSwg4FuA2zwQn + i5w =
  </G>
                                          <Y>
    aQuzepFF4F1ue0fEV4mKrt1yUBydFuebGtdahyzwF6qQu/uQ8bO39cA8h+RuhyVm
    VSb9NBV7JvWWofCZf1nz5l78YVpVLV51acX
    /
xFk9WgKZEQ5xyX4SIaWgP+mmk1rt
            2I7ws7L3nTqZ7XX3uHHm6vJoDZbVdKX0
wTus47S0TeE=
  </Y>
</DSAKeyValue>",
                expectedParameters);
        }

        [Fact]
        public static void TestRead1024Parameters_Private()
        {
            TestReadXml(
                // Bonus trait of this XML: very odd whitespace
                @"
<DSAKeyValue>
  <P>
    wW0mx01sFid5nAkYVI5VP+WMeIHaSEYpyvZDEfSyfP72vbDyEgaw/8SZmi/tU7Q7
    nuKRDGjaLENqgBj0k49kcjafVkfQBbzJbiJZDMFePNTqDRMvXaWvaqoIB7DMTvNA
    SvVC9FRrN73WpH5kETCDfbm
    Tl8hFY1
    1  9   w 2 0 F  N  + S o  S z E =
  </P>
  <Q>2DwOy3NVHi/jDVH89CNsZRiDrdc=</Q>
  <G>
    a8NmtmNVVF4Jjx/pDlRptWfgn6edgX8rNntF3s1DAaWcgdaRH3aR03DhWsaSwEvB
    GHLBcaf+ZU6WPX3aV1qemM4Cb7fTk0olhggTSo7F7WmirtyJQBtnrd5Cfxftrrct
    evRdmrHVnhsT1O + 9F8dkMwJn3eNSwg4FuA2zwQn + i5w =
  </G>
                                          <Y>
    aQuzepFF4F1ue0fEV4mKrt1yUBydFuebGtdahyzwF6qQu/uQ8bO39cA8h+RuhyVm
    VSb9NBV7JvWWofCZf1nz5l78YVpVLV51acX
    /
xFk9WgKZEQ5xyX4SIaWgP+mmk1rt
            2I7ws7L3nTqZ7XX3uHHm6vJoDZbVdKX0
wTus47S0TeE=
  </Y>
<X>


w C Z 4  A  H  d   5   5   S    4    2    B     o     I     h
S      9      R      /       j       6       9        C        v        C
       0         =

</X>
</DSAKeyValue>
",
                DSATestData.GetDSA1024Params());
        }

        [ConditionalFact(typeof(DSAFactory), nameof(DSAFactory.SupportsFips186_3))]
        public static void TestRead2048Parameters_Public()
        {
            DSAParameters expectedParameters = DSATestData.Dsa2048DeficientXParameters;
            expectedParameters.X = null;

            TestReadXml(
                // Bonus trait of this XML: Canonical element order, pretty-printed.
                // Includes the XML declaration.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<DSAKeyValue>
  <P>
    lNPks58XJz6PJ7MmkvfDTTVhi9J5VItHNpOcK6TnKRFsrqxgAelXBcJ9fPDDCLyn
    ArtHEtS7RIoQeLYxFQ6rRVyRZ4phxRwtNx4Cu6xw6cLG2nV7V4IOyP0JbhBew2wH
    ik/X3Yck0nXTAH+U8S/5YWcpPGZ7RncH8dyafAs0vE/EdbFdUSQKeJpTpWtk1pwe
    ArfKOtNBs7b2yYbx4GW/5oDYfe5tlZHY445Xw3rCmDsnlL6v/ix7W2ykm5gSSHMy
    XGHeb4IEEQGL6XI/4r2oMywTCIqjKghtFNbwAgEBP1FnhkPzKswAUl2yLwAg2S+c
    L0CIuNNaHnZNzYtwwLPS6w==
  </P>
  <Q>23CgOhWOnMudk9v3Z5bL68pFqHA+gqRYAViO5LaYWrM=</Q>
  <G>
    PPDxRLcKu9RCYNksTgMq3wpZjmgyPiVK/4cQyejqm+GdDSr5OaoN7HbSB7bqzveC
    TjZldTVjAcpfmF74/3r1UYuN8IhaasVw/i5cWVYXDnHydAGUAYyKCkp7D5z5av1+
    JQJvqAuflya2xN/LxBBeuYaHyml/eXlAwTNbFEMR1H/yHk1cQ8AFhHhrwarkrYWK
    wGM1HuRCNHC+URVShpTvzzEtnljU3dHAHig4M/TxSeX5vUVJMEQxthvg2tcXtTjF
    zVL94ajmYZPonQnB4Hlo5vcH71YU6D5hEm9qXzk54HZzdFRL4yJcxPjzxQHVolJv
    e7ZNZWp7vf5+cssW1x6KOA==
  </G>
  <Y>
    cHLO4Kgw8hUDAviwzw8HFHtsaxMs5k309uE7nofw8txeBXRBGbaVgJU1GndCqeRc
    cuI+6L8AmMgv0tB4fyGXRwv7DLwhRirTiT3vfBoN80/VKVf/AcafdsVkwmjrzUPe
    w3bfU4qIdK807QB7TkbQZgBoE3kxqlmjLodbKUKdtVY13rbcjL+GfUSUytXt7n5/
    IF7o6LLIoFK0Uo9HySsfjP7J7QU8IeOnMb/yaa0JnEE9X8h4U1EWXnmqehQ6DH5V
    Ye8DvOPPDe2c7YMWgC+Z3a0DLejBknDzuvWoJgiIkX/Nc+Sx1W+tFWPHHbyS9nJW
    kt3Wo5FBhE0R/aIwt75rrA==
  </Y>
</DSAKeyValue>",
                expectedParameters);
        }

        [ConditionalFact(typeof(DSAFactory), nameof(DSAFactory.SupportsFips186_3))]
        public static void TestRead2048Parameters_Private_CryptoBinary()
        {
            TestReadXml(
                // Bonus trait of this XML: The X parameter is encoded as a CryptoBinary,
                // meaning the leading 0x00 byte is removed.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<DSAKeyValue>
  <P>
    lNPks58XJz6PJ7MmkvfDTTVhi9J5VItHNpOcK6TnKRFsrqxgAelXBcJ9fPDDCLyn
    ArtHEtS7RIoQeLYxFQ6rRVyRZ4phxRwtNx4Cu6xw6cLG2nV7V4IOyP0JbhBew2wH
    ik/X3Yck0nXTAH+U8S/5YWcpPGZ7RncH8dyafAs0vE/EdbFdUSQKeJpTpWtk1pwe
    ArfKOtNBs7b2yYbx4GW/5oDYfe5tlZHY445Xw3rCmDsnlL6v/ix7W2ykm5gSSHMy
    XGHeb4IEEQGL6XI/4r2oMywTCIqjKghtFNbwAgEBP1FnhkPzKswAUl2yLwAg2S+c
    L0CIuNNaHnZNzYtwwLPS6w==
  </P>
  <Q>23CgOhWOnMudk9v3Z5bL68pFqHA+gqRYAViO5LaYWrM=</Q>
  <G>
    PPDxRLcKu9RCYNksTgMq3wpZjmgyPiVK/4cQyejqm+GdDSr5OaoN7HbSB7bqzveC
    TjZldTVjAcpfmF74/3r1UYuN8IhaasVw/i5cWVYXDnHydAGUAYyKCkp7D5z5av1+
    JQJvqAuflya2xN/LxBBeuYaHyml/eXlAwTNbFEMR1H/yHk1cQ8AFhHhrwarkrYWK
    wGM1HuRCNHC+URVShpTvzzEtnljU3dHAHig4M/TxSeX5vUVJMEQxthvg2tcXtTjF
    zVL94ajmYZPonQnB4Hlo5vcH71YU6D5hEm9qXzk54HZzdFRL4yJcxPjzxQHVolJv
    e7ZNZWp7vf5+cssW1x6KOA==
  </G>
  <Y>
    cHLO4Kgw8hUDAviwzw8HFHtsaxMs5k309uE7nofw8txeBXRBGbaVgJU1GndCqeRc
    cuI+6L8AmMgv0tB4fyGXRwv7DLwhRirTiT3vfBoN80/VKVf/AcafdsVkwmjrzUPe
    w3bfU4qIdK807QB7TkbQZgBoE3kxqlmjLodbKUKdtVY13rbcjL+GfUSUytXt7n5/
    IF7o6LLIoFK0Uo9HySsfjP7J7QU8IeOnMb/yaa0JnEE9X8h4U1EWXnmqehQ6DH5V
    Ye8DvOPPDe2c7YMWgC+Z3a0DLejBknDzuvWoJgiIkX/Nc+Sx1W+tFWPHHbyS9nJW
    kt3Wo5FBhE0R/aIwt75rrA==
  </Y>
  <X>yHG344loXbl9k03XAR+rB2/yfsQoL7AMDWRtKdXk5Q==</X>
</DSAKeyValue>",
                DSATestData.Dsa2048DeficientXParameters);
        }

        [ConditionalFact(typeof(DSAFactory), nameof(DSAFactory.SupportsFips186_3))]
        public static void TestRead2048Parameters_Private_Base64Binary()
        {
            TestReadXml(
                // Bonus trait of this XML: The X parameter is encoded as a Base64Binary,
                // meaning the leading 0x00 byte is NOT removed.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<DSAKeyValue>
  <P>
    lNPks58XJz6PJ7MmkvfDTTVhi9J5VItHNpOcK6TnKRFsrqxgAelXBcJ9fPDDCLyn
    ArtHEtS7RIoQeLYxFQ6rRVyRZ4phxRwtNx4Cu6xw6cLG2nV7V4IOyP0JbhBew2wH
    ik/X3Yck0nXTAH+U8S/5YWcpPGZ7RncH8dyafAs0vE/EdbFdUSQKeJpTpWtk1pwe
    ArfKOtNBs7b2yYbx4GW/5oDYfe5tlZHY445Xw3rCmDsnlL6v/ix7W2ykm5gSSHMy
    XGHeb4IEEQGL6XI/4r2oMywTCIqjKghtFNbwAgEBP1FnhkPzKswAUl2yLwAg2S+c
    L0CIuNNaHnZNzYtwwLPS6w==
  </P>
  <Q>23CgOhWOnMudk9v3Z5bL68pFqHA+gqRYAViO5LaYWrM=</Q>
  <G>
    PPDxRLcKu9RCYNksTgMq3wpZjmgyPiVK/4cQyejqm+GdDSr5OaoN7HbSB7bqzveC
    TjZldTVjAcpfmF74/3r1UYuN8IhaasVw/i5cWVYXDnHydAGUAYyKCkp7D5z5av1+
    JQJvqAuflya2xN/LxBBeuYaHyml/eXlAwTNbFEMR1H/yHk1cQ8AFhHhrwarkrYWK
    wGM1HuRCNHC+URVShpTvzzEtnljU3dHAHig4M/TxSeX5vUVJMEQxthvg2tcXtTjF
    zVL94ajmYZPonQnB4Hlo5vcH71YU6D5hEm9qXzk54HZzdFRL4yJcxPjzxQHVolJv
    e7ZNZWp7vf5+cssW1x6KOA==
  </G>
  <Y>
    cHLO4Kgw8hUDAviwzw8HFHtsaxMs5k309uE7nofw8txeBXRBGbaVgJU1GndCqeRc
    cuI+6L8AmMgv0tB4fyGXRwv7DLwhRirTiT3vfBoN80/VKVf/AcafdsVkwmjrzUPe
    w3bfU4qIdK807QB7TkbQZgBoE3kxqlmjLodbKUKdtVY13rbcjL+GfUSUytXt7n5/
    IF7o6LLIoFK0Uo9HySsfjP7J7QU8IeOnMb/yaa0JnEE9X8h4U1EWXnmqehQ6DH5V
    Ye8DvOPPDe2c7YMWgC+Z3a0DLejBknDzuvWoJgiIkX/Nc+Sx1W+tFWPHHbyS9nJW
    kt3Wo5FBhE0R/aIwt75rrA==
  </Y>
  <X>AMhxt+OJaF25fZNN1wEfqwdv8n7EKC+wDA1kbSnV5OU=</X>
</DSAKeyValue>",
                DSATestData.Dsa2048DeficientXParameters);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite512Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                DSATestData.Dsa512Parameters,
                includePrivateParameters,
                (
                    "1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoe" +
                    "BO1b9fRxSG9NmG1CoufflQ=="
                ),
                "+rX2JdXV4WQwoe9jDr4ziXzCJPk=",
                (
                    "CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPO" +
                    "qfch85sFuvlwUt78Z6WKKw=="
                ),
                (
                    "wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQG" +
                    "GiWQXBi9JJmoOWY8PKRWBQ=="
                ),
                "Lj16hMhbZnheH2/nlpgrIrDLmLw=");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite576Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                DSATestData.Dsa576Parameters,
                includePrivateParameters,
                (
                    "4hZzBr/9hrti9DJ7d4u/oHukIyPsVnsQa5VjiCvd1tfy7nNg8pmIjen0CmHHjQvY" +
                    "RC76nDIrhorTZ7OUHXK3ozLJVOsWKRMr"
                ),
                "zNzsz18LLI/iOOLwbyITfxf66xs=",
                (
                    "rxfUBhMCB54zA0p3oFjdtLgyrLEUt7jS065EUd/4XrjdddRHQhg2nUhbIgZQZAYE" +
                    "SrTmQH/apaKeldSWTKVZ6BxvfPzahyZl"
                ),
                (
                    "gVpUm2/QztrwRLALfP4TUZAtdyfW1/tzYAOk4cTNjfv0MeT/RzPz+pLHZfDP+UTj" +
                    "7VaoW3WVPrFpASSJhbtfiROY6rXjlkXn"
                ),
                "rDJpPhzXKtY+GgtugVfrvKZx09s=");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite1024Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                DSATestData.GetDSA1024Params(),
                includePrivateParameters,
                (
                    "wW0mx01sFid5nAkYVI5VP+WMeIHaSEYpyvZDEfSyfP72vbDyEgaw/8SZmi/tU7Q7" +
                    "nuKRDGjaLENqgBj0k49kcjafVkfQBbzJbiJZDMFePNTqDRMvXaWvaqoIB7DMTvNA" +
                    "SvVC9FRrN73WpH5kETCDfbmTl8hFY119w20FN+SoSzE="
                ),
                "2DwOy3NVHi/jDVH89CNsZRiDrdc=",
                (
                    "a8NmtmNVVF4Jjx/pDlRptWfgn6edgX8rNntF3s1DAaWcgdaRH3aR03DhWsaSwEvB" +
                    "GHLBcaf+ZU6WPX3aV1qemM4Cb7fTk0olhggTSo7F7WmirtyJQBtnrd5Cfxftrrct" +
                    "evRdmrHVnhsT1O+9F8dkMwJn3eNSwg4FuA2zwQn+i5w="
                ),
                (
                    "aQuzepFF4F1ue0fEV4mKrt1yUBydFuebGtdahyzwF6qQu/uQ8bO39cA8h+RuhyVm" +
                    "VSb9NBV7JvWWofCZf1nz5l78YVpVLV51acX/xFk9WgKZEQ5xyX4SIaWgP+mmk1rt" +
                    "2I7ws7L3nTqZ7XX3uHHm6vJoDZbVdKX0wTus47S0TeE="
                ),
                "wCZ4AHd55S42BoIhS9R/j69CvC0=");
        }

        [ConditionalTheory(typeof(DSAFactory), nameof(DSAFactory.SupportsFips186_3))]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWriteDeficientXParameters(bool includePrivateParameters)
        {
            TestWriteXml(
                DSATestData.Dsa2048DeficientXParameters,
                includePrivateParameters,
                (
                    "lNPks58XJz6PJ7MmkvfDTTVhi9J5VItHNpOcK6TnKRFsrqxgAelXBcJ9fPDDCLyn" +
                    "ArtHEtS7RIoQeLYxFQ6rRVyRZ4phxRwtNx4Cu6xw6cLG2nV7V4IOyP0JbhBew2wH" +
                    "ik/X3Yck0nXTAH+U8S/5YWcpPGZ7RncH8dyafAs0vE/EdbFdUSQKeJpTpWtk1pwe" +
                    "ArfKOtNBs7b2yYbx4GW/5oDYfe5tlZHY445Xw3rCmDsnlL6v/ix7W2ykm5gSSHMy" +
                    "XGHeb4IEEQGL6XI/4r2oMywTCIqjKghtFNbwAgEBP1FnhkPzKswAUl2yLwAg2S+c" +
                    "L0CIuNNaHnZNzYtwwLPS6w=="
                ),
                "23CgOhWOnMudk9v3Z5bL68pFqHA+gqRYAViO5LaYWrM=",
                (
                    "PPDxRLcKu9RCYNksTgMq3wpZjmgyPiVK/4cQyejqm+GdDSr5OaoN7HbSB7bqzveC" +
                    "TjZldTVjAcpfmF74/3r1UYuN8IhaasVw/i5cWVYXDnHydAGUAYyKCkp7D5z5av1+" +
                    "JQJvqAuflya2xN/LxBBeuYaHyml/eXlAwTNbFEMR1H/yHk1cQ8AFhHhrwarkrYWK" +
                    "wGM1HuRCNHC+URVShpTvzzEtnljU3dHAHig4M/TxSeX5vUVJMEQxthvg2tcXtTjF" +
                    "zVL94ajmYZPonQnB4Hlo5vcH71YU6D5hEm9qXzk54HZzdFRL4yJcxPjzxQHVolJv" +
                    "e7ZNZWp7vf5+cssW1x6KOA=="
                ),
                (
                    "cHLO4Kgw8hUDAviwzw8HFHtsaxMs5k309uE7nofw8txeBXRBGbaVgJU1GndCqeRc" +
                    "cuI+6L8AmMgv0tB4fyGXRwv7DLwhRirTiT3vfBoN80/VKVf/AcafdsVkwmjrzUPe" +
                    "w3bfU4qIdK807QB7TkbQZgBoE3kxqlmjLodbKUKdtVY13rbcjL+GfUSUytXt7n5/" +
                    "IF7o6LLIoFK0Uo9HySsfjP7J7QU8IeOnMb/yaa0JnEE9X8h4U1EWXnmqehQ6DH5V" +
                    "Ye8DvOPPDe2c7YMWgC+Z3a0DLejBknDzuvWoJgiIkX/Nc+Sx1W+tFWPHHbyS9nJW" +
                    "kt3Wo5FBhE0R/aIwt75rrA=="
                ),
                // The rules from xmldsig say that these types are ds:CryptoBinary, which
                // means they should leave off any leading 0x00 bytes.
                //
                // .NET Framework just treated it like base64Binary, though, and happily
                // writes the unwanted zeroes.
                "AMhxt+OJaF25fZNN1wEfqwdv8n7EKC+wDA1kbSnV5OU=");
        }

        [ConditionalFact(typeof(DSAFactory), nameof(DSAFactory.SupportsKeyGeneration))]
        [OuterLoop("DSA key generation is very slow")]
        public static void FromToXml()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                DSAParameters pubOnly = dsa.ExportParameters(false);
                DSAParameters pubPriv = dsa.ExportParameters(true);

                string xmlPub = dsa.ToXmlString(false);
                string xmlPriv = dsa.ToXmlString(true);

                using (DSA dsaPub = DSAFactory.Create())
                {
                    dsaPub.FromXmlString(xmlPub);

                    DSAImportExport.AssertKeyEquals(pubOnly, dsaPub.ExportParameters(false));
                }

                using (DSA dsaPriv = DSAFactory.Create())
                {
                    dsaPriv.FromXmlString(xmlPriv);

                    DSAImportExport.AssertKeyEquals(pubPriv, dsaPriv.ExportParameters(true));
                    DSAImportExport.AssertKeyEquals(pubOnly, dsaPriv.ExportParameters(false));
                }
            }
        }

        [Fact]
        public static void FromNullXml()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "xmlString",
                    () => dsa.FromXmlString(null));
            }
        }

        [Fact]
        public static void FromInvalidXml()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-512 test case, with an unfinished closing element.
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(
                        @"
<DSAKeyValue xmlns:yep=""urn:ignored:yep"" xmlns:nope=""urn:ignored:nope"" xmlns:ign=""urn:ignored:ign"">
  <yep:P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</yep:P>
  <nope:Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</nope:Q>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
  <ign:Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</ign:Y>
</DSA"));
            }
        }

        [Fact]
        public static void FromNonsenseXml()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-512 test case, with the G value from the DSA-1024 case.
                try
                {
                    dsa.FromXmlString(
                        @"
<DSAKeyValue xmlns:yep=""urn:ignored:yep"" xmlns:nope=""urn:ignored:nope"" xmlns:ign=""urn:ignored:ign"">
  <yep:P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</yep:P>
  <nope:Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</nope:Q>
  <G>
    a8NmtmNVVF4Jjx/pDlRptWfgn6edgX8rNntF3s1DAaWcgdaRH3aR03DhWsaSwEvB
    GHLBcaf+ZU6WPX3aV1qemM4Cb7fTk0olhggTSo7F7WmirtyJQBtnrd5Cfxftrrct
    evRdmrHVnhsT1O + 9F8dkMwJn3eNSwg4FuA2zwQn + i5w =
  </G>
  <ign:Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</ign:Y>
</DSAKeyValue>");
                }
                catch (ArgumentException)
                {
                    // DSACng, DSAOpenSsl
                }
                catch (CryptographicException)
                {
                    // DSACryptoServiceProvider
                }
            }
        }

        [Fact]
        public static void FromXml_MissingP()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-576 test case, but with an element missing.
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(
                        @"
<DSAKeyValue>
  <Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</Y>
  <Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</Q>
  <BananaWeight unit=""lbs"">30000</BananaWeight>
  <X>Lj16hMhbZnheH2/nlpgrIrDLmLw=</X>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXml_MissingQ()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-576 test case, but with an element missing.
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(
                        @"
<DSAKeyValue>
  <Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</Y>
  <BananaWeight unit=""lbs"">30000</BananaWeight>
  <X>Lj16hMhbZnheH2/nlpgrIrDLmLw=</X>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
  <P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</P>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXml_MissingG()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-576 test case, but with an element missing.
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(
                        @"
<DSAKeyValue>
  <Y>wwDg5n2HfmztOf7qqsHywr1WjmoyRnIn4Stq5FqNlHhUGkgKyAA4qshjgn1uOYQGGiWQXBi9JJmoOWY8PKRWBQ==</Y>
  <Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</Q>
  <BananaWeight unit=""lbs"">30000</BananaWeight>
  <X>Lj16hMhbZnheH2/nlpgrIrDLmLw=</X>
  <P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</P>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXml_MissingY()
        {
            using (DSA dsa = DSAFactory.Create())
            {
                // This is the DSA-576 test case, but with an element missing.
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(
                        @"
<DSAKeyValue>
  <Q>+rX2JdXV4WQwoe9jDr4ziXzCJPk=</Q>
  <BananaWeight unit=""lbs"">30000</BananaWeight>
  <X>Lj16hMhbZnheH2/nlpgrIrDLmLw=</X>
  <G>CETEkOUu9Y4FkCxjbWTR1essYIKg1PO/0c4Hjoe0On73u+zhmk7+Km2cIp02AIPOqfch85sFuvlwUt78Z6WKKw==</G>
  <P>1qi38cr3ppZNB2Y/xpHSL2q81Vw3rvWNIHRnQNgv4U4UY2NifZGSUULc3uOEvgoeBO1b9fRxSG9NmG1CoufflQ==</P>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXmlWithSeedAndCounterAndJ()
        {
            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dr</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <PgenCounter>aQ==</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>");

                DSATestData.GetDSA1024_186_2(out DSAParameters expected, out _, out _);
                DSAImportExport.AssertKeyEquals(expected, dsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void FromXmlWrongJ_OK()
        {
            // No one really reads the J value on import, but xmldsig defined it,
            // so we read it.

            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dR</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <PgenCounter>aQ==</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>");

                DSATestData.GetDSA1024_186_2(out DSAParameters expected, out _, out _);
                DSAImportExport.AssertKeyEquals(expected, dsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void FromXmlInvalidJ_Fails()
        {
            // No one really reads the J value on import, but xmldsig defined it,
            // so we read it and pass it to ImportParameters.
            // That means it has to be legal base64.

            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                Assert.Throws<FormatException>(
                    () => dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+d</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <PgenCounter>aQ==</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXmlWrongCounter_SometimesOK()
        {
            // DSACryptoServiceProvider doesn't check this error state, DSACng does.
            //
            // So, either the import gets rejected (because the counter value should be 105,
            // but says 106) and throws a CryptographicException derivitive, or it succeeds,
            // and exports the correct key material.

            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                bool checkKey = true;

                try
                {
                    dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dr</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <PgenCounter>ag==</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>");
                }
                catch (CryptographicException)
                {
                    checkKey = false;
                }

                if (checkKey)
                {
                    DSATestData.GetDSA1024_186_2(out DSAParameters expected, out _, out _);
                    DSAImportExport.AssertKeyEquals(expected, dsa.ExportParameters(true));
                }
            }
        }

        [Fact]
        public static void FromXml_CounterOverflow_Succeeds()
        {
            // The counter value should be 105 (0x69).
            // This payload says 0x01_00000069 (4294967401).
            // Since we only end up looking at the last 4 bytes, this is /a/ right answer.

            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dr</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <PgenCounter>AQAAAGk=</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>");

                DSATestData.GetDSA1024_186_2(out DSAParameters expected, out _, out _);
                DSAImportExport.AssertKeyEquals(expected, dsa.ExportParameters(true));
            }
        }

        [Fact]
        public static void FromXmlSeedWithoutCounter()
        {
            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dr</J>
  <Seed>1QFOS2DvK6i2IRtAYroyJOBCfdM=</Seed>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>"));
            }
        }

        [Fact]
        public static void FromXmlCounterWithoutSeed()
        {
            // This key comes from FIPS-186-2, Appendix 5, Example of the DSA.
            // The version in DSATestData does not have the seed or counter supplied.

            using (DSA dsa = DSAFactory.Create())
            {
                Assert.Throws<CryptographicException>(
                    () => dsa.FromXmlString(@"
<DSAKeyValue>
  <P>
    jfKklEkidqo9JXWbsGhpy+rA2Dr7jQz3y7gyTw14guXQdi/FtyEOr8Lprawyq3qs
    SWk9+/g3JMLsBzbuMcgCkQ==
  </P>
  <Q>x3MhjHN+yO6ZO08t7TD0jtrOkV8=</Q>
  <G>
    Ym0CeDnqChNBMWOlW0y1ACmdVSKVbO/LO/8Q85nOLC5xy53l+iS6v1jlt5Uhklyc
    xC6fb0ZLCIzFcq9T5teIAg==
  </G>
  <Y>
    GRMYcddbFhKoGfKdeNGw1zRveqd7tiqFm/1sVnXanSEtOjbvFnLvZguMfCVcwOx0
    hY+6M/RMBmmWMKdrAw7jMw==
  </Y>
  <J>AgRO2deYCHK5/u5+ElWfz2J6fdI2PN/mnjBxceE11r5zt7x/DVqcoWAp2+dr</J>
  <PgenCounter>aQ==</PgenCounter>
  <X>IHCzIj26Ny/eHA/8ey47SYsmBhQ=</X>
</DSAKeyValue>"));
            }
        }

        private static void TestReadXml(string xmlString, in DSAParameters expectedParameters)
        {
            using (DSA dsa = DSAFactory.Create())
            {
                dsa.FromXmlString(xmlString);
                Assert.Equal(expectedParameters.P.Length * 8, dsa.KeySize);

                bool includePrivateParameters = expectedParameters.X != null;

                DSAImportExport.AssertKeyEquals(
                    expectedParameters,
                    dsa.ExportParameters(includePrivateParameters));
            }
        }

        private static void TestWriteXml(
            in DSAParameters keyParameters,
            bool includePrivateParameters,
            string expectedP,
            string expectedQ,
            string expectedG,
            string expectedY,
            string expectedX)
        {
            IEnumerator<XElement> iter;

            using (DSA dsa = DSAFactory.Create(keyParameters))
            {
                iter = VerifyRootAndGetChildren(dsa, includePrivateParameters);
            }

            AssertNextElement(iter, "P", expectedP);
            AssertNextElement(iter, "Q", expectedQ);
            AssertNextElement(iter, "G", expectedG);
            AssertNextElement(iter, "Y", expectedY);


            // We don't produce J.
            // Seed isn't present in the input parameters, so it shouldn't be here.

            if (includePrivateParameters)
            {
                AssertNextElement(iter, "X", expectedX);
            }

            Assert.False(iter.MoveNext(), "Move after last expected value");
        }

        private static IEnumerator<XElement> VerifyRootAndGetChildren(
            DSA dsa,
            bool includePrivateParameters)
        {
            XDocument doc = XDocument.Parse(dsa.ToXmlString(includePrivateParameters));
            XElement root = doc.Root;

            Assert.Equal("DSAKeyValue", root.Name.LocalName);
            // Technically the namespace name should be the xmldsig namespace, but
            // .NET Framework wrote it as the empty namespace, so just assert that's true.
            Assert.Equal("", root.Name.NamespaceName);

            // Test that we're following the schema by looping over each node individually to see
            // that they're in order.
            IEnumerator<XElement> iter = root.Elements().GetEnumerator();
            return iter;
        }

        private static void AssertNextElement(
            IEnumerator<XElement> iter,
            string localName,
            string expectedValue)
        {
            Assert.True(iter.MoveNext(), $"Move to {localName}");

            XElement cur = iter.Current;

            Assert.Equal(localName, cur.Name.LocalName);
            // Technically the namespace name should be the xmldsig namespace, but
            // .NET Framework wrote it as the empty namespace, so just assert that's true.
            Assert.Equal("", cur.Name.NamespaceName);

            // Technically whitespace should be ignored here.
            // But let the test be simple until needs prove otherwise.
            Assert.Equal(expectedValue, cur.Value);
        }
    }
}
