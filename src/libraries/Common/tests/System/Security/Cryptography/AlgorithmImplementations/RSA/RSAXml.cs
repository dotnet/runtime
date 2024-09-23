// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Xml.Linq;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Security.Cryptography.Rsa.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "Not supported on Browser")]
    public static class RSAXml
    {
        [Fact]
        public static void TestRead1032Parameters_Public()
        {
            RSAParameters expectedParameters = ImportExport.MakePublic(TestData.RSA1032Parameters);

            // Bonus trait of this XML: the elements are all in different namespaces,
            // showing that isn't part of the reading consideration.
            TestReadXml(
                @"
<RSAKeyValue xmlns=""urn:ignored:root"" xmlns:n=""urn:ignored:modulus"" xmlns:e=""urn:ignored:exponent"">
  <n:Modulus>
    vKyxpTSdezWlgKw7OZjrFev5AOyzKb8fdXF6ALIZnIoY15G1krfsUr1a8tsNO2Nf
    BZV1Pf97p8mHLb9+Mibe9EoHylaNEBeZLCtBv+XsNXCCTPH0sVkZ/tUT/aViBK8g
    NKLQj/BMLMpJ0Wj6A/ovoy/M00hMFfCi5UZ8dvx2C1UJ
  </n:Modulus>
  <e:Exponent>
    AQAB
  </e:Exponent>
</RSAKeyValue>",
                expectedParameters);
        }

        [Fact]
        public static void TestRead1032Parameters_Private()
        {
            // Bonus trait of this XML: the root element name is wrong
            TestReadXml(
                @"
<DSAKeyValue>
  <Modulus>
    vKyxpTSdezWlgKw7OZjrFev5AOyzKb8fdXF6ALIZnIoY15G1krfsUr1a8tsNO2Nf
    BZV1Pf97p8mHLb9+Mibe9EoHylaNEBeZLCtBv+XsNXCCTPH0sVkZ/tUT/aViBK8g
    NKLQj/BMLMpJ0Wj6A/ovoy/M00hMFfCi5UZ8dvx2C1UJ
  </Modulus>
  <Exponent>
    AQAB
  </Exponent>
  <P>
    DhUwCp00uje2vagxvGcnsvf20O+3szqZya8oz9Yl4kWlTyUbeExHka2lha23Edkw
    Cj1StFDMMH9V0x4SF7n/10U=
  </P>
  <Q>
    DWXGDei29Up3Vv0cy6ds5B70RtAkAx7pxaQJMbBzNs/tNajuWA4Z24WSyw8mbsaQ
    KOuemOPoT/GkWaiiaGCmEPU=
  </Q>
  <DP>
    DZ20vn5zDZ1ypXsq43OFccfILwmnvrXpHZSqzBDMvjMCezxwi+aMyDBxuodUWwB4
    L15NSaRZWIa1b5NCgQhIclU=
  </DP>
  <DQ>
    DPb73eHhiyVwryFpiDqQyYCa6xvofYygtL20l/0kwVodNtwvKc8bfq+YCiCzFGfa
    gX7hjxqdaR9x58GkyFUe3zE=
  </DQ>
  <InverseQ>
    AQzpk26W+634ckDMQZ0BCBu2fJgdRDFOWFg6x/6TeeoCcubEx8FGOOHV7OeEDdsV
    oS1wVKQY+HZPpUzhNOvSY14=
  </InverseQ>
  <D>
    nlkl4uxsu0qD86EZN7binoxkeGUv3Pqd0XiCl3DiU+IHBW0yAchBHBP179rumQhG
    aK5OLtFsG57kx/1uUXMULcWfxkWkwuZlXazmcbMA3g17tS1oqGAmwY8CRybncb0E
    gZlELwNCcJZY7/ZK5C6kFzJH7NJKhimei50RUgACyPXx
  </D>
</DSAKeyValue>",
                TestData.RSA1032Parameters);
        }

        [ConditionalFact]
        [OuterLoop("RSA 16384 takes considerable time.")]
        public static void TestRead16384Parameters_Public()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384");
            }

            RSAParameters expectedParameters = ImportExport.MakePublic(TestData.RSA16384Params);

            // Bonus trait of this XML: the Modulus and Exponent parameters
            // are not in canonical order.
            // Bonus trait of this XML: the document has very non-standard whitespace.
            TestReadXml(
                @"
<RSAKeyValue>
  <Exponent>
    A  Q

A    B
  </Exponent>
  <Modulus>
    myxwX6kQNx+LSMao1StC1p5rKCEwcBjzI136An3B/BjthgezAOuuJ+fAfFVkj7VH

    4ZgI+GCFxxQLKzFimFr1FvqnnKhlugrsuJ8wmJtVURxO+lEKeZICPm2cz43nfKAy
    gsGcfS7zjoh0twyIiAC6++8K/

0rc7MbluIBqwGD3jYsjB0LAZ18gb3KYzuU5lwt2
    uGZWIgm9RGc1L4r4RdE2NCfUeE1unl2VR7yBYFcauMlfGL5bkBMVhEkWbtbdnUfs
    IorWepdEa4GkpPXg6kpUO4iBuF2kigUp21rkGIrzBygy1pFQ/hReGuCb/SV3rF7V

    8qfpn98thqeiiPfziZ6KprlXNtFj/uVAErWHn3P2diYyp3HQx8BGmvJRMbHd0WDr
    iQJiWESYp2VTB3N1dcDTj5E0ckdf9Wt+JR7gWMW5axe7y1xMswHJWaI76jnBTHoh
    qtt+2T6XFluTonYmOdQ8DbgHBUgqG6H
/HJugWBIm3194QDVh55CSsJLIm8LxwcBg
    eUc / H 8 Y 2 F V r 3 W t E s
epc0rb1jNDLkf8sYC+o6jrCMekP9YPF2tPAxf/eodxf/59sB
    iC2wXFMDafnWp1lxXiGcVVu9dE2LeglCgnMUps9QlJD0aXaJHYi2VDQ3zFdMvn8A    imlqKtZGdGf93YaQg+Yq07hc6f8Vi3o1LSK/wp9BbNZs3JhBv4ODIAMfMsCEok8U
    + vFhHSCmoNxzTl8I9pz8KJLRyLQXwfpJylfWY5vAbpAgV8wdyjfKro2QDXNIYCrV
    pQk9KFCMwtekaA76LKRQai95TZuYCb+yQ00yvk17nzIPKJHsv/jHLvxxp9Yz1Kcb
    7rZWkT96/ciDfE0G8fc1knWRQ8Sm5rUsc/rHbgkczzAb0Ha3RWOt3vG/J10T1YJr
    1gIOJBSlpNmPbEhJcBzFk88XOq9DC3xc0j3Xk28Q73AlcEq0GNc+FrjkOJ+az6Pd
    cKqkDQJ862arB4u+4v1w4qr5468x8lfAl+fv2J72chsr31OWonQsVCOmSBtv34r9
    Lu6VU6mk6ibUk0v6zrVv8GSlHuQsFQO7Ri6PmX3dywKJllpTCFQlcqleEPmIyzC3
    H5fV1RVzIw8G017PJb1erXPzkmLQFPsmTSEiJMvorVz7mVgQaT0xZcI6q2R6inkr
    9xU1iC7Erw3nZ9J2O06DoZj3Rwy+3yfCfbbZk+yS/mPIiprHyAgNW5ejWS9qJBtk
    uuYcM+GuSXmE1DG8A/4XV+wMjEyqdRp+AOd3OED38t4MO4Gdpyt742N3olGSdNJq
    IuRjGUGb11l5WI2iGLKO2GgWTannjBUO59m3Afb/RV//3yMsrPFL9xg0mUNpCBuO
    aWYHdl+8LJcu/AoyYPRTJWd6300N4x3sNBqwey3xIjPitHsRmNm+gyF6JTIebFWn
    0Krnv2DmI5qWYIDI4niYE/W8roRt5REp9U6H6VXPBRFr4daB2Jz9hc5Xft/i9/ZE
    2N1P/koRF90IElQ03Kzgo760j5v/WtfCXsY0JWoc3JCQeUwP089xCLFForx9MvnA
    arxtwZjdoJOsfXSVi3Xj9GShgMHxyK4e5Ew6bPMXQZ41WOo1HpcqjZSfbGL39/ZS
    OaUQ8Fx0fb+NKbiRw063MbUSGqQ54uiHif+jOLtxiCEqNJEYAl7ALN1Hh982Es+W
    HNGYKpuOKPnfga80ALWym+WMo4Kp

vpXnF+vqVy6ncQu/+43FdJuYwCFwVLHs/6CA
    on0pCT9jBqHan6oXnXNlBNkAB7j7jQi1BPQ9Eaoy09320uybU2HQ/Go1oep45are
    UT1U5jbDfaNyeGyIDJSdMeVy84nnOL/pZ/er7LxR+Ddei09U0qjGHT4BjDaQnIOj
    hygcQGcZDwPZFzfAvR0GrWGXzAFuOrTR30NXQeSfSa+EnsmydGf8FtRPGF6HFno2
    AJNigcDp8M6tiFnld1jDFq0CDaAc07csiMfMg8WZFlh8


JEb2Zye69xB21mQnNRUw
    1vI2SspCUNh6x6uHtmqYNiE4a4hT6N4wd1SUuP2t2RHaJelvZWvgPZWrNQ+exrmi
    FItsi8GhOcxG9IKj2e8Z2/MtI9e4pvw98uuaM4zdinZZ0y56UqzZP8v7pTf9pLP8
    6Q/WBPB1XLNjQ4IHb498hpI2c3qaZvlK8yayfhi7miTzzx9zv5ieNvwYtV5rHQbe
    cHqBs52IEYxEohKEGwjK6FujoB9w2f9GdY9G+Dy5aBFdwM0GjHA7f+O508Phn/gc
    Na3+BX8NEossBq7hYzoFRakmBm6qm5JC5NNRZXfBQp/Skirh4lcDqgL0JLhmGGy/
    LoqsaTJobbE9jH9PXZapeMX
sSjAWSC15D1rWzzivgE4oUKkWIaa24Tsn22E+4wh9
    jS7xOfJ1/yXnCN8svORJcEv8Te9yMkXEif17VhNJho4+qLDxs7VbUYIyKNJlz3Kr
    NQMBADpey10fnhza0NJSTC7RoRpfko905a1Wo4vtSdp7T5S5OPRMuQNaOq2t2fBh
    dYMvSNno1mcdUBfVDHYFwx6xuFGHS2jYMRDn88MDPdCm/1MrjHEDx6zzxMR1tjjj
    66oxFJQ3o/Wh8hJDK+kMDIYd//kFRreAMhVX1dGJ/ax6p/dw4fE+aWErFwgfZySn
    9v


qKdnL4n1j7bemWOxMmrAigcwt6noH/hX5ZO5X869SV1WvLOvhCt4Ru7LOzqUUL
    k+Y3+gSNHX34/+Jw+VCq5hHlolNkpw+thqvba8lMv

zM=
  </Modulus>
</RSAKeyValue>",
                expectedParameters);
        }

        [ConditionalFact]
        public static void TestRead16384Parameters_Private()
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384");
            }

            // Bonus trait of this XML: the D parameter is not in
            // canonical order.
            TestReadXml(
                @"
<RSAKeyValue>
  <Modulus>
    myxwX6kQNx+LSMao1StC1p5rKCEwcBjzI136An3B/BjthgezAOuuJ+fAfFVkj7VH
    4ZgI+GCFxxQLKzFimFr1FvqnnKhlugrsuJ8wmJtVURxO+lEKeZICPm2cz43nfKAy
    gsGcfS7zjoh0twyIiAC6++8K/0rc7MbluIBqwGD3jYsjB0LAZ18gb3KYzuU5lwt2
    uGZWIgm9RGc1L4r4RdE2NCfUeE1unl2VR7yBYFcauMlfGL5bkBMVhEkWbtbdnUfs
    IorWepdEa4GkpPXg6kpUO4iBuF2kigUp21rkGIrzBygy1pFQ/hReGuCb/SV3rF7V
    8qfpn98thqeiiPfziZ6KprlXNtFj/uVAErWHn3P2diYyp3HQx8BGmvJRMbHd0WDr
    iQJiWESYp2VTB3N1dcDTj5E0ckdf9Wt+JR7gWMW5axe7y1xMswHJWaI76jnBTHoh
    qtt+2T6XFluTonYmOdQ8DbgHBUgqG6H/HJugWBIm3194QDVh55CSsJLIm8LxwcBg
    eUc/H8Y2FVr3WtEsepc0rb1jNDLkf8sYC+o6jrCMekP9YPF2tPAxf/eodxf/59sB
    iC2wXFMDafnWp1lxXiGcVVu9dE2LeglCgnMUps9QlJD0aXaJHYi2VDQ3zFdMvn8A
    imlqKtZGdGf93YaQg+Yq07hc6f8Vi3o1LSK/wp9BbNZs3JhBv4ODIAMfMsCEok8U
    +vFhHSCmoNxzTl8I9pz8KJLRyLQXwfpJylfWY5vAbpAgV8wdyjfKro2QDXNIYCrV
    pQk9KFCMwtekaA76LKRQai95TZuYCb+yQ00yvk17nzIPKJHsv/jHLvxxp9Yz1Kcb
    7rZWkT96/ciDfE0G8fc1knWRQ8Sm5rUsc/rHbgkczzAb0Ha3RWOt3vG/J10T1YJr
    1gIOJBSlpNmPbEhJcBzFk88XOq9DC3xc0j3Xk28Q73AlcEq0GNc+FrjkOJ+az6Pd
    cKqkDQJ862arB4u+4v1w4qr5468x8lfAl+fv2J72chsr31OWonQsVCOmSBtv34r9
    Lu6VU6mk6ibUk0v6zrVv8GSlHuQsFQO7Ri6PmX3dywKJllpTCFQlcqleEPmIyzC3
    H5fV1RVzIw8G017PJb1erXPzkmLQFPsmTSEiJMvorVz7mVgQaT0xZcI6q2R6inkr
    9xU1iC7Erw3nZ9J2O06DoZj3Rwy+3yfCfbbZk+yS/mPIiprHyAgNW5ejWS9qJBtk
    uuYcM+GuSXmE1DG8A/4XV+wMjEyqdRp+AOd3OED38t4MO4Gdpyt742N3olGSdNJq
    IuRjGUGb11l5WI2iGLKO2GgWTannjBUO59m3Afb/RV//3yMsrPFL9xg0mUNpCBuO
    aWYHdl+8LJcu/AoyYPRTJWd6300N4x3sNBqwey3xIjPitHsRmNm+gyF6JTIebFWn
    0Krnv2DmI5qWYIDI4niYE/W8roRt5REp9U6H6VXPBRFr4daB2Jz9hc5Xft/i9/ZE
    2N1P/koRF90IElQ03Kzgo760j5v/WtfCXsY0JWoc3JCQeUwP089xCLFForx9MvnA
    arxtwZjdoJOsfXSVi3Xj9GShgMHxyK4e5Ew6bPMXQZ41WOo1HpcqjZSfbGL39/ZS
    OaUQ8Fx0fb+NKbiRw063MbUSGqQ54uiHif+jOLtxiCEqNJEYAl7ALN1Hh982Es+W
    HNGYKpuOKPnfga80ALWym+WMo4KpvpXnF+vqVy6ncQu/+43FdJuYwCFwVLHs/6CA
    on0pCT9jBqHan6oXnXNlBNkAB7j7jQi1BPQ9Eaoy09320uybU2HQ/Go1oep45are
    UT1U5jbDfaNyeGyIDJSdMeVy84nnOL/pZ/er7LxR+Ddei09U0qjGHT4BjDaQnIOj
    hygcQGcZDwPZFzfAvR0GrWGXzAFuOrTR30NXQeSfSa+EnsmydGf8FtRPGF6HFno2
    AJNigcDp8M6tiFnld1jDFq0CDaAc07csiMfMg8WZFlh8JEb2Zye69xB21mQnNRUw
    1vI2SspCUNh6x6uHtmqYNiE4a4hT6N4wd1SUuP2t2RHaJelvZWvgPZWrNQ+exrmi
    FItsi8GhOcxG9IKj2e8Z2/MtI9e4pvw98uuaM4zdinZZ0y56UqzZP8v7pTf9pLP8
    6Q/WBPB1XLNjQ4IHb498hpI2c3qaZvlK8yayfhi7miTzzx9zv5ieNvwYtV5rHQbe
    cHqBs52IEYxEohKEGwjK6FujoB9w2f9GdY9G+Dy5aBFdwM0GjHA7f+O508Phn/gc
    Na3+BX8NEossBq7hYzoFRakmBm6qm5JC5NNRZXfBQp/Skirh4lcDqgL0JLhmGGy/
    LoqsaTJobbE9jH9PXZapeMXsSjAWSC15D1rWzzivgE4oUKkWIaa24Tsn22E+4wh9
    jS7xOfJ1/yXnCN8svORJcEv8Te9yMkXEif17VhNJho4+qLDxs7VbUYIyKNJlz3Kr
    NQMBADpey10fnhza0NJSTC7RoRpfko905a1Wo4vtSdp7T5S5OPRMuQNaOq2t2fBh
    dYMvSNno1mcdUBfVDHYFwx6xuFGHS2jYMRDn88MDPdCm/1MrjHEDx6zzxMR1tjjj
    66oxFJQ3o/Wh8hJDK+kMDIYd//kFRreAMhVX1dGJ/ax6p/dw4fE+aWErFwgfZySn
    9vqKdnL4n1j7bemWOxMmrAigcwt6noH/hX5ZO5X869SV1WvLOvhCt4Ru7LOzqUUL
    k+Y3+gSNHX34/+Jw+VCq5hHlolNkpw+thqvba8lMvzM=
  </Modulus>
  <Exponent>
    AQAB
  </Exponent>
  <D>
    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
  </D>
  <P>
    yk0AOTleQMp8K97g3ZjrtfWOrSI/xEfUS336yp1a4tQQHn59BPy5HfQQxYvHQ+FO
    OU4jIpWwOjv/fPI0LNx219S8Z+M/7+puZPFUUQghjsMRkxsgXAcDW2B/kDSD1A72
    BdrZ1noLEt+O8EMP0HdXUA7lhFAo/k9jaK6S+dW1g7tERGbyNuVadRMFj/V5I6qK
    zDE2vDUlCRAD6ps/C2X9p8Npxqoweqp5bYDK8gn27aiD+Fii10Z5i8ODUcYEaBJN
    2YXigBTuKpoGwFpku4g3ebBKVxdZdH4FFaZ+AmDLmxMELyiDFurCuxSvZhtSBRU4
    NPfD7jJBOEf84bslMH6gVrc/tuV2xndGyKk8ISuFQj2edkXNaYyx5kaMe+5Sn1cS
    svD0Q65+jyLlWbv3zuOZ5G899NNu1PDQn5U/ev8kgmNspnLMIiB59zjqltGqzfP2
    c6ppARmcF1jSliWdtIXPObry/83KwJg9o2lYEUqg2XVaUGmV5sM2US5benizE/Gk
    2jsiy3AzWTRjTmdypPLqFLbCIYPiSEaQ67Asdrav7uifFU8BQnz2vcDDeMb54LlL
    Ji+wTUBd3guhuQE4tOS3O43uxmKnl1pQVmL5ra0Qf7k7eyDBr4ERUHCnwsSNN5zR
    tGSb5P4No8HEdVF4Iz8bzw4hCXE8yb+8DkKTAV2W9Y2dge5QAwKD6dTsFgkNNgpW
    rqPHQ2injuoqtXftCtRB5BGm+ASesy6Ywxh4vb2Oi7aemLVtD+/cEiIcvEp7/9fG
    C2ewo6UE98GF1ut07ItKA3wGJ1bYRdkuMGTuPIA4vCDDA5gf09yalDTyh2EqOHzc
    Q7mmJhFUjmubYFpruD1oP1xuFuFUuk3uIfpxzeyKls/TUFRTxL+dEEvTMwI8xj+p
    w1wEh8KnmQlydANbo1JacctvJI3Ly+MXkOXse7yF1WAkiLNtC6O9Q9pE1u3MwB9s
    S5xxY2AK7HPP0R1wnqq9fP4hRkerIUYEVk5qpcnQa8u61OT8bAOi8iIRJYVtC1K4
    b0IVbpyiZ/0WYEtt8C6rumZCRX58mrb4Mz+yWpx1fNTeStbWfspU4FqoVwz5UwjR
    BZ2SteMkdflCfyz3KwYTRJFA4hhJAqOo+/wFiXW/TSdDRLfX5RMzGmrCIGdWPyUr
    j810+g93jIFeiKAMM3q64WO3lCgp7A3vgDtvnoDAz1PbubGTIx2ZQDp9sIdnkyUn
    sXOhRD/cQnI8X1wsg1D6FVsL+wpB+IXID/Drvs2E7/J37XOqvHcrnTpixQhhStnd
    N4gzw6Sw3KeLpfVcz/8MAxHPq7Q0wCiuEqo6QhBGES35pUUsgZvcTlfME7TCG5mh
    6XFFmi598pje2nBTY+Epvw==
  </P>
  <Q>
    xFz9wnWwjmsLis7NS5jqQAyr25r5X1emWywNq8asbhS5NimjXdqNTgOE8shpRVdi
    XL38vjoVcdmAfp1CLOef1I1qYwuDd6xxm1gclB8AOU8nk9WKCPGUZwixoqjIFOgz
    UQeydqPqy4Edd64kvdHD016+jPMopgy4RNxAgxI/svcPdQNCQ5k5R2RtM8T+prfO
    v8yiWZ7+Xeoi2GgemdJ+dr/s7RpUCphXHGO94O+WcSiDA97Ii2o5uVU1bhpdfCUp
    DW17zVxRzunRiaoaxYtGlpe5Cc1G+yvPDA2f4BCDu/CGec2uzxnfImIpcMQUgzrt
    8l4ZRTPUX02UUBAf4EZ5swxUDjGFv6egxGqvAL2Ig2cW6Esar1wZ2iBDsXabamM2
    MOdXQAyzR83HtpfIBXwNm9A0qigpP/sxvCPKDX+fPs9+gDKSKaDnxTvNCICCbAFd
    EMS0fwj1FU4Dkbx/wxvtMz9E1nE7HYVUjZueOLcRUGhk1/sysmQMyIuNM52aRHjm
    CEQNYlaFGJpIOEijer7FGsYB5GLnmEy0P3PFyAlzMvyVGUGUrgugUjGpPnEitxMd
    HtqtmUY1HsWQUoI3OqVJ76dugQBLSutly/0uToMsqwfTFVv7xFXek5qerC8iJHPm
    WPCDqHgHKZMXYoPLfwjXOqgy2dx1tAnwXXdCTaH/71zxf+SMMxuURLfZhyPabdIM
    DZMsuQvrc0ra/vCGtGv4Yx6DTzPn9yWGOZlzaO27LC52RWOJKkkpDirdQLn9WJsX
    oWNfo5QEm9I6/IYMvhMFoN7RnU3FCUYklDdrhRyq6Zi92lnJqfKLI2dUymstZMtp
    csJhYJUsBzIjpgoYFzdUGKlps973u62kIypjrDrBN4q17p8l2WLyUyjI0LxCtj5Y
    oGqkQrtxiu9zoje//11ZPgXAgoW2cslg2OhpsBj8msjgCkDWTGQNo+gg+57rzknb
    dDrJy23Ugh9J1bc3Oy2BIyhouaYYJaRdoWmiaysB/pNi6g1zAh9kWoJ6lITBysVc
    Fw10uVurb8SulE5B3mIQuC7xLaw3VjMIfFjD1fxr3Z5sgLG46FQEB1ScmiiJ+eB3
    8e2S4ybqD/LdA8t7Qj+tbRkYO16OpTOxaYg/Xlo/5ab1nyzniMP7YVOqCtXRtog8
    cHIBIE03BhEfCnTGNq/Jdjg0tjKIS8Ua2WlRvNuiBXkXRmex086uF05PNr8/hnWs
    HK50YP9N8M5BpJVOkH2sl/coeJ6SJ3aYbzDwdS0Ustcnz0j0wOz5/VfCasBqpDN8
    Qk7I9FpmCKBHVSpmJTppoxe0hMzviXhooP/nYFJhpKqNcd8XR4u4y9/uXaIfo5g+
    w6EvM+zdroqNDDQ6RC1/jQ==
  </Q>
  <DP>
    GBL3vteT3tP52OKqEdTb4Ah71SCpQ/tkSSORz8DQCwQ/ctGMoSZOBUGBKXEL4okS
    XQFubvQvR47SRZUxHlGSFvcrAJXriupz/rE1XntAOxP9qGrm++yduqcOJyQIuBib
    sHCt0bcuUC2offENFbrN+in7qDY92p2p79Auj2qeMjH72sQBeQTsMdh0pgAJTXRD
    Fi+ZGuacJKryPF4DL6EQgYFguhKQuFhHIP/dptYGu5t9MPWjU0kAt+ApZXbSGWxs
    NUGYhbN38DvqJ8PaDvMT3vhasGiH7bP9eOkaP8AzGp41tkL07qo7SDYa9WS06wPu
    b2c4usTiPAddEaPKti2reQZPn71I2C9jjgeNr0jVj99zVxHRcwkaNpQYrbrbvDiJ
    ch/4gYFncDMv5fDXeZhePO/8CIGMw+xwdz00k7d/KcEZMemhX0JMIV51lEMZN28b
    2gHigw4AJEserF2Hme7+jRkxR72+rhKv6x1jLJOb9qTffYhDHXYHpbuFiVqJvQrZ
    mlrFNj6A7dGtK6xl2TlLH/Hrwj9Gk2FKZ7HMaMguwZiPLeL7/GSQnF4vJNVQ8Sw7
    xCySp27MfNsXgMOjcutw3rZyPsuItBs8Sjt3CPL6bqilam6offE3FUKCxEvNnluc
    HQKIBsUw7FbnwSpTyKX+8jH1PoFqQXv+rhfAFL6Fc21J3Cd3ABSxjAcZnTmwh8jN
    LfUxhlUS84/sSzIdVFeUC8cJ/qPWGu6loTntTG8dYoT19KhKdUYPA11p3AJlJToR
    SFQrkh3WLIGsIrpcbLXatfVxagcMr6s7suif7TU5CzI+4tOcngK3poFyhyfJ9XTu
    ZWTXX9paHKSzldDM1tz/5eJi+3gPNCiH+SUrm9zVVUMgG4Qdf+FpmIHdfUl73/+9
    fREbPOiuNykHpMStiA8J0lbqQAhbw0SgDk8+SC9UIeNSFa58gJEYudVkscsUvZw/
    r/PLDo9kXWUeyvzc5RTefdxkK0/mDoydgYPNbzNICTvyXNlvLI92OahSMAvjwSAz
    8JGFqWcccJGOsyDm0VlMeF8o7coym9rASKEA4YWS+ar/VRqh5e7AEP7Y35scpIP9
    E9T/m4OPWDa0chvwwf70FgnPFdjb/2NofawtIIGRpWXRvIDAQXN6dl9UALUrb1JG
    D9PcYtGqYV8X19xr90hYqu/J7eilrICwCquICe26hDGviTaXkus3zIvpXzON4NXg
    Fl7zRwLufD3J73MxnOLrD9WI5HQBC8kn2LXL6CXe9wr8uJY2MD1iRFCpZlcr9NNe
    XvhnaJXVsjyCAtrjE6F/VXIuK3nDeUaeCHyXeDsli2/UMJW9ySK6IdyS3Zl6K/yp
    ZvVi2glEVbVZd9c8JTurUw==
  </DP>
  <DQ>
    DXlYDEzpFYyw2RCBs8tFX6m+7S3AKNOp3Z2zPnM+h7syTk4jIKCLi6vgJoyr9I8f
    d7+tpRv1Nr+2+nkt/kjShdJCV5OFrOOPVBqCs4NBD62nyJQhiaWSClPlZITyXcTl
    KI0/qLZsuRQeAoVXjhLjvhBFQQS6aFJ9HnSClLve1Rfw3pWfqWXNMWHpwGCnHKeG
    L1EKXt3zFFypkXHrj6CK/vkCd+6Tj7qOV6tcbx/hkdg2zUAvQKnEVjxLk0eJ3Kfs
    HjjCAwBvuKQAtdKPTbjV2iWFE/AbC8cgyPHyY3yenXnOsHL1qM5cqk5UC6HYynxz
    sWrVjxMUYom/QJMqrMgJN8kDx4mMZO9Kr8+mPIXE5UdgogXtSdUnDPmjy3yZA37V
    TBvDt+hnMOkk6BmYJxAxGtz5kCd7VSGWcxN9nNmCAtxYENHnh6W74aPN2OSAjoq1
    aR4mSIVD0/drdUea+Ldk2lxgC9rvNIJen+zquXeOX5caPFvHSchlvCkfQkhxOnuV
    RUHuLS5EqcCEbiBF33lRqxmlLZe/zoqM38HA8436cqg0TuxaGGtB0AIKW/eFa0yL
    df+JY/gWUws5cP/wbDzFSwWRJpbvk699Z7byw35qxT2fNVr/dqRxxm0YsDX3wMqX
    JskyL5A05ZxrQV5Ly2a+5g5+lsZy6Sy5aqBxU0RnfHRDOgRjvmoJDYIUEhratShx
    nUjZC8WOnXWoe0/j3mN+QsboOboVE7dmc3NdIPkXG9wAT5iZ4+XrREaasgNRKBBU
    WcWo3V+dxVdyprtICo7hlv1TItkgSRegEO+QmCy0aZ0Kgf4hQWEcPQytG8qo7b6r
    eK1v4yG5SLEfExikOIuaYKrXTvlgxGcQ2TziZAIQGhCRlVMkVLteZ2hoBzKz3S+A
    A1nt9YpJK0BtDdeHfC1an0/jutEUCOJam+euwN+mDbT08p8qVUmUSgf2o21vPtOA
    lIQoLqZVq6wb8+dDifMAZnoyXXLRO1wA9L973qCv3Vkds3PCzYV77F4BrUlCxvgt
    7oME3Gc25092r0SDbpAKF6lY8Upc7bRIw8ePgJJ2kFl7loUbbA2/zQT4Tfe0KApI
    ELi9mIqmCvweQFFpHs2hx9et+vztCtb2OIiZ2I6WzdpcBlUdehwAltgX1fCAGGdW
    xlx8SUwjF257U8tZgo0jZNJMg6gKBDD9O6fnbO8hOgDqIPGimScYeQ7tjpMm98IB
    UmKCJ9m87mYoyPNZ1b5Z5n+WLlirLLwNj6urBE9YmUD8QVP/P3HDYafw9kRHzQaf
    YylmzqWZVMQywNxM8AcHEqLgxzAMy8EQZOPKa6ibfKIbXJHzVfx3bL3r7E7gnugm
    fmQECisZtw1YylqXGPCKTQ==
  </DQ>
  <InverseQ>
    n+rm+JVUflE2xL9LB+IxUmf85aSrEXtyeJDWONs76lEL3omN8rLP4Fq/s2kVn4Nr
    uobi4mPG2oINvtU+XaxVIb/90rqtYmj5UQekrfIVjkosqPevbonXiHFWGtvQBHVl
    zIP1qoeNim5P/3UnCA07SaK5dFrNSYr0pAFCudEcwe6sAqSE5PomswjacRdehXAL
    ePPuwYcJX1n/L6CMCUpb0c26ilxihihLwDikvT556AfB0QEAsNRrNORzvMtZD1Gz
    dIaEY+2mUCrIz/jAzP7slWTEOHoHtgIeFNFj7AZ3ztc2GcIAmiIjSPbXPsaCZNIN
    tT1jvIEzRSltchQl1ck5BPi0KCJ8WvajfSj/uM2e0I42kVJDbnkKTGsbUUlWns+T
    WiqgvS9UpMjfdzZsvYhabN54/1VqgvpVhg1/ZO3MWkDmNhsFLyopm/FHwE3KylJV
    +zPZ+ZwTm/oH639Fbvyk1WIXQS/5PVZSTSSfDgEfKocOTFEX+9WFDKJvrInAQl+Y
    eyfd/dOKL+VP5YFiWRQiiUCXJrYfqVHJ0/QYy+DqqgihSyYkRJuFj+0zuUvYmjIZ
    qwYCecJY7fceDkUFSwkRHyJK471Dr0zVxoSzW0M395u9J0kA+1sQIgTrpLlVKXSK
    4nNYEhuhDD8ja1yGW17XvdMPl2o5RE+9/fbSZXM1BFXoYV2gOaci6dJQciZgepZp
    6AN8MQOrEmAsJyvE/ZnMta6HZ3ngwPxCbvT3clB07uUxCsDy+ogpw3OL3cIz29l2
    1RDZacWR9rMIZTxAD42aNN6q49MnJt4N9cMTf8w3P5B2rLO4RKaDnaFWwHMN2SPB
    pRbRP1iFFIedtW9yziGXq/BiUD1S5fNlO6r4Gi4bZ/dIUtHxQY2Z8A4kIskxTNp1
    iplE5zptN9j+GJCOxg0qxXt/yr2c+ybzvY5nmk/ONjphs4MnTONb8O1YOjphUDXW
    7KpcXs9Hu68rqkSBEa+MhGmDz8NbDTP1Q4bVgAIZtEoWdlWMEDdEMihjU55XgNQB
    QACfhKn5nJe+SIgxjoqGZc+13uZDcowNySBXr8R1yujvwN1E2ODX2up7T9mFDXsr
    hFBkT7+mW1mOHJUxNJ3L70B+7iepvU1NEbBL2tVGcueSMGduUxXxTebNhCCQxrjC
    GJNKc5jCAVgK70SzHQ/HW1PApA8Vma+AV9aOopw32M0it1kAvuzHLbZTHp8OqFIB
    WEhzUCxK/cVFK8+yixqYU9/Eky936jzwaO6+QATdC/kJjTOTKAQ4yIR/q1HW16sn
    4Do8AsRi+Rtdx7G9UZbgu8rCShuFHzfF83uHwHsl8khazOQetuanBG/GxBdaeIJF
    mTymL1LOru69mA9gwhuFFQ==
  </InverseQ>
</RSAKeyValue>",
                TestData.RSA16384Params);
        }

        [Fact]
        public static void TestReadDiminishedDPParameters_Public()
        {
            RSAParameters expectedParameters =
                ImportExport.MakePublic(TestData.DiminishedDPParameters);

            TestReadXml(
                // Bonus trait of this XML: Canonical element order, pretty-printed.
                // Includes the XML declaration.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<RSAKeyValue>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
  <Exponent>AQAB</Exponent>
</RSAKeyValue>
",
                expectedParameters);
        }

        [Fact]
        public static void TestReadDiminishedDPParameters_Private_Base64Binary()
        {
            // This test uses the base64Binary version of the DP value, where the 0x00
            // is written down.
            TestReadXml(
                // Bonus trait of this XML: Elements are in reverse-canonical order.
                // Includes the XML declaration.
                // Includes unused elements.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
",
                TestData.DiminishedDPParameters);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        public static void TestReadDiminishedDPParameters_Private_CryptoBinary()
        {
            // This test writes the DP value as a CryptoBinary, meaning the leading
            // 0x00 is not written down.
            //
            // .NET Framework does not handle that correctly.
            TestReadXml(
                // Bonus trait of this XML: Elements are in reverse-canonical order.
                // Includes the XML declaration.
                // Includes unused elements.
                @"<?xml version=""1.0"" encoding=""UTF-8""?>
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    CI79xRT1Ei3wDYHziB/Zl+5Xac8xpK5mlM8UL8rlSw==
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
",
                TestData.DiminishedDPParameters);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite1024Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                TestData.RSA1024Params,
                includePrivateParameters,
                (
                    "nwUfznHKLhcPQ8YESm+3hNitYlvbh0sFrQN2GWPuKp3Mfa86USOufxmmY79la143" +
                    "HGoKp/CCsb3DIVAhI+9AMJMREVz3W0JyGN7+G/YOAr8oVvt8tfGIzWaPwcq6+YN+" +
                    "CVvSkbkWXCsMjpG7w4QNc+6iMIIvPJBQiKeSlpkMaL0="
                ),
                "AQAB",
                (
                    "Sub5f97mWkZfWM+NVth/a3I6XSGianxCfKesObJxzR4N48elYvG5MEIPN13Ack3r" +
                    "DJXAVjF6Bim5n1fkfE4l/90RDtnowE+YxhPXJH48PrTHhY/VlAsSVPigC2Fv3eib" +
                    "kZ0qbxWZjJG5Pe9lx3imV1LEF1v8qGVOHpWCaxVJbGs="
                ),
                (
                    "0ksJA5IpledllBlPINK39rmweB+3wt20aLX9fdGSuqhQHtxEeb8hL8K1oAgOzoAM" +
                    "N/Zg6rtXfqV0goqfp1tCAw=="
                ),
                (
                    "wZUxGvTlpB+shkg1m35y+YIXKoYKPTHM0Rwju2SQ3x9Wp0gknq8Y4wy25nvkH/cZ" +
                    "bU7bqVDN1ihi72VySKkOPw=="
                ),
                (
                    "d813nSkvt87T98NTaQei9lRjTIwFTGax2NWVTJCQXvZ0bqBeAl34shTjFACDLvGU" +
                    "BG3AWPnRprzr21LOEbHTsQ=="
                ),
                (
                    "rlIzDhtKUClVqvaLj6Km1piXU+uwfLrDveqhIrbE3qfR2IHWuC7lMlDYw2T9YOub" +
                    "Mhu5IxdoxFlJ/lpUN6pE8Q=="
                ),
                (
                    "Kjmv4nYGSI9Jk98BzXuyxmgVrw7J8eAVPaloJ9ZkHgjcATs4MmJPzQwJ95pryg92" +
                    "wvvdfF3xhRZnn5in80B9HA=="
                ),
                // On Windows 10 RSACng recomputes the D value to D-phi instead of D-lambda
                (
                    "mmmJZxfLcVHm+rKPfBBbLd6RDk+QLiHFUylnRmRo4mz0Ip9Ci4OQb87iaT1zJ/0G" +
                    "msrFqim7XwibL//1DkXGF1ypenkg5lPOSXYlcBsaqpw9zTxBDgPO+w7+26oySN12" +
                    "wugBg2XtnZ3XzvUBr8NxfndykVMONAMPdzBTrWnNeKk="
                ));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite1032Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                TestData.RSA1032Parameters,
                includePrivateParameters,
                (
                    "vKyxpTSdezWlgKw7OZjrFev5AOyzKb8fdXF6ALIZnIoY15G1krfsUr1a8tsNO2Nf" +
                    "BZV1Pf97p8mHLb9+Mibe9EoHylaNEBeZLCtBv+XsNXCCTPH0sVkZ/tUT/aViBK8g" +
                    "NKLQj/BMLMpJ0Wj6A/ovoy/M00hMFfCi5UZ8dvx2C1UJ"
                ),
                "AQAB",
                (
                    "nlkl4uxsu0qD86EZN7binoxkeGUv3Pqd0XiCl3DiU+IHBW0yAchBHBP179rumQhG" +
                    "aK5OLtFsG57kx/1uUXMULcWfxkWkwuZlXazmcbMA3g17tS1oqGAmwY8CRybncb0E" +
                    "gZlELwNCcJZY7/ZK5C6kFzJH7NJKhimei50RUgACyPXx"
                ),
                (
                    "DhUwCp00uje2vagxvGcnsvf20O+3szqZya8oz9Yl4kWlTyUbeExHka2lha23Edkw" +
                    "Cj1StFDMMH9V0x4SF7n/10U="
                ),
                (
                    "DWXGDei29Up3Vv0cy6ds5B70RtAkAx7pxaQJMbBzNs/tNajuWA4Z24WSyw8mbsaQ" +
                    "KOuemOPoT/GkWaiiaGCmEPU="
                ),
                (
                    "DZ20vn5zDZ1ypXsq43OFccfILwmnvrXpHZSqzBDMvjMCezxwi+aMyDBxuodUWwB4" +
                    "L15NSaRZWIa1b5NCgQhIclU="
                ),
                (
                    "DPb73eHhiyVwryFpiDqQyYCa6xvofYygtL20l/0kwVodNtwvKc8bfq+YCiCzFGfa" +
                    "gX7hjxqdaR9x58GkyFUe3zE="
                ),
                (
                    "AQzpk26W+634ckDMQZ0BCBu2fJgdRDFOWFg6x/6TeeoCcubEx8FGOOHV7OeEDdsV" +
                    "oS1wVKQY+HZPpUzhNOvSY14="
                ),
                // RSACng recomputes a smaller D for this key
                (
                    "ENegpwT2nuJH0x/szIQyThtpt7OpfatGOWNnFutPHnp0Y7/p075P3gXxubakrH2/" +
                    "JH42QFHPXce/Za3Pq9Xs9qK2JxcfZ5hUHxvxHKyapWprK8nBCCYWZRqxrmwC4Qx8" +
                    "iALCSmtNGBCH/SQdB1N4LPTNA1X4/RV5G0nJACK+PORV"
                ));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWrite2048Parameters(bool includePrivateParameters)
        {
            TestWriteXml(
                TestData.RSA2048Params,
                includePrivateParameters,
                (
                    "sXzud7RZpGWSjX9Vd4A5uiK6KaX/5VP7QJioNeUtCtyFF27k1pOCIHGN6d3J1b0w" +
                    "R+5ZueaoeZ5Hlo5jzaYonXttg6po4kbWHIwrocQEEq5hB69v6StIXMrCDlJxIQPg" +
                    "BRwHwFb9imF6lTlLqlqdA216UT565KvttAqIgDwH7RkhotzXnD87GVkzOYol286N" +
                    "jhDasTgyylmhaTwjdlA3s79zWHc4xRYDYDYNMYy+pRIv5RatHIABUOs8hnki03/U" +
                    "kIW467B92MiLu4i+/ri6rWHH+WggsqkdtNywW8ez8oM1Q7CuGSum+ohIufuzzfip" +
                    "nCBvYyPlwoXNdXpVBKQImQ=="
                ),
                "AQAB",
                (
                    "LJaGEexs2K/rsUBb6Dl+RxSSUAQz1RjT9dZj66Y3OpNLnCdvtbg46I2eaTIekmOE" +
                    "zY1DXUBk8qigs2HyEKe9bFKloH4e+zlwcJuGGo1zuH22QogARUNqWmVVeuObKAAh" +
                    "Nydjix5Pc4Qpl3NdXt6Es2e9Ysufc/L9NE2xHQX3t8g8afYsW3jGsFW/i1jxwp8Z" +
                    "ShdTYNpSby7eD2SdT7ivUCuPmR4VeDLXNwbDCF0cBLFtRoTJ6PYOjhjsS4TF82Vr" +
                    "mzUK2nvHq21Y+hd2wmfxupvdH7SKVgZnNjnpHoKKqih6p9oYAdUcGQxQqwk1r84m" +
                    "ANTQYcwqDj/Kw9tCFpv/QQ=="
                ),
                (
                    "umH9ZW2Gut13txGeyMl9uGojAz3oybfW3vgVfPcLsR3Ig+x2V59/R17VVzTZFr3c" +
                    "3aCy4BjRcGVOK3UVd8QGPoA+wranQEpTYTA1Rp/U/H2AyQ7hlJvH+r1Gm0wSkH2b" +
                    "9eaCRrmmqfY1vAeexHh0bkSsv1xZM9pZ3pvtPTkbNiM="
                ),
                (
                    "88hrqaLS1qtkWQ9oDMhpxlGyLCi1290AcF6zBDjEqkXveax5j/H2Bzvfrwjj2iE3" +
                    "CEEfoIRN66mRtbHW1ToW1X56nWXxkodp5wH9YJUlG/B54OHKzpl67ceNoj2NMLGO" +
                    "2pnPnmsCKNTsJdWJdMqifkfb6OVCbNTcrdwxskL7LBM="
                ),
                (
                    "dddWuzZQpP05n8nINvMORfb1RCt0b3WIqVj5XRVlkwpdqOtst2Hku18+S/DiAPry" +
                    "Fj5wWjfW09V5YwiYFi0eNY4oIDwT6xYTObOdO5X6t9kx/+0kuyzzd5kMd0vVwP1q" +
                    "CkM/wy/GLFe7CbNXsqjmFIHfJu5gh+RaReEYUkk05zk="
                ),
                (
                    "L18PxLv2Gm7dpgy/XFSJcVcotzoF9L5iOnO8d6KMXMYQPeWNDbKn60nwMnQYyqdP" +
                    "qVP2UFvFRHkD7nmrVG3gSAY2z2Ui5yVXJ+OUF/ODbYVyOYfGwBTE9XWkiRVK3V5z" +
                    "cvkWhiMnHUYayVNQTZiesMlH6165ZKqMY2B5a7lmU28="
                ),
                (
                    "CxZrLofPHCycihyS1NEepIclHKEpeuTWZWluYrrqQWmJvXhaoSxRZqM9HlGmZWGm" +
                    "bC+q211qlE5lSjBC4XqoWy1BBYJfbd2dmmpubnwbswuTWBAc75G2BrvlrRV0aaRk" +
                    "/2uIHqE84+EkU03I0Qv3fRto6rU05PscbMp/LHEZnbw="
                ));
        }

        [ConditionalTheory]
        [InlineData(true)]
        [InlineData(false)]
        [OuterLoop("RSA 16384 takes considerable time for primality tests.")]
        public static void TestWrite16384Parameters(bool includePrivateParameters)
        {
            // Do not move this to the [ConditionalFact], otherwise the platform will check if RSA 16384 is supported
            // during test discovery for innerloop, and the check itself is expensive.
            if (!ImportExport.Supports16384)
            {
                throw new SkipTestException("Platform does not support RSA 16384");
            }

            TestWriteXml(
                TestData.RSA16384Params,
                includePrivateParameters,
                (
                    "myxwX6kQNx+LSMao1StC1p5rKCEwcBjzI136An3B/BjthgezAOuuJ+fAfFVkj7VH" +
                    "4ZgI+GCFxxQLKzFimFr1FvqnnKhlugrsuJ8wmJtVURxO+lEKeZICPm2cz43nfKAy" +
                    "gsGcfS7zjoh0twyIiAC6++8K/0rc7MbluIBqwGD3jYsjB0LAZ18gb3KYzuU5lwt2" +
                    "uGZWIgm9RGc1L4r4RdE2NCfUeE1unl2VR7yBYFcauMlfGL5bkBMVhEkWbtbdnUfs" +
                    "IorWepdEa4GkpPXg6kpUO4iBuF2kigUp21rkGIrzBygy1pFQ/hReGuCb/SV3rF7V" +
                    "8qfpn98thqeiiPfziZ6KprlXNtFj/uVAErWHn3P2diYyp3HQx8BGmvJRMbHd0WDr" +
                    "iQJiWESYp2VTB3N1dcDTj5E0ckdf9Wt+JR7gWMW5axe7y1xMswHJWaI76jnBTHoh" +
                    "qtt+2T6XFluTonYmOdQ8DbgHBUgqG6H/HJugWBIm3194QDVh55CSsJLIm8LxwcBg" +
                    "eUc/H8Y2FVr3WtEsepc0rb1jNDLkf8sYC+o6jrCMekP9YPF2tPAxf/eodxf/59sB" +
                    "iC2wXFMDafnWp1lxXiGcVVu9dE2LeglCgnMUps9QlJD0aXaJHYi2VDQ3zFdMvn8A" +
                    "imlqKtZGdGf93YaQg+Yq07hc6f8Vi3o1LSK/wp9BbNZs3JhBv4ODIAMfMsCEok8U" +
                    "+vFhHSCmoNxzTl8I9pz8KJLRyLQXwfpJylfWY5vAbpAgV8wdyjfKro2QDXNIYCrV" +
                    "pQk9KFCMwtekaA76LKRQai95TZuYCb+yQ00yvk17nzIPKJHsv/jHLvxxp9Yz1Kcb" +
                    "7rZWkT96/ciDfE0G8fc1knWRQ8Sm5rUsc/rHbgkczzAb0Ha3RWOt3vG/J10T1YJr" +
                    "1gIOJBSlpNmPbEhJcBzFk88XOq9DC3xc0j3Xk28Q73AlcEq0GNc+FrjkOJ+az6Pd" +
                    "cKqkDQJ862arB4u+4v1w4qr5468x8lfAl+fv2J72chsr31OWonQsVCOmSBtv34r9" +
                    "Lu6VU6mk6ibUk0v6zrVv8GSlHuQsFQO7Ri6PmX3dywKJllpTCFQlcqleEPmIyzC3" +
                    "H5fV1RVzIw8G017PJb1erXPzkmLQFPsmTSEiJMvorVz7mVgQaT0xZcI6q2R6inkr" +
                    "9xU1iC7Erw3nZ9J2O06DoZj3Rwy+3yfCfbbZk+yS/mPIiprHyAgNW5ejWS9qJBtk" +
                    "uuYcM+GuSXmE1DG8A/4XV+wMjEyqdRp+AOd3OED38t4MO4Gdpyt742N3olGSdNJq" +
                    "IuRjGUGb11l5WI2iGLKO2GgWTannjBUO59m3Afb/RV//3yMsrPFL9xg0mUNpCBuO" +
                    "aWYHdl+8LJcu/AoyYPRTJWd6300N4x3sNBqwey3xIjPitHsRmNm+gyF6JTIebFWn" +
                    "0Krnv2DmI5qWYIDI4niYE/W8roRt5REp9U6H6VXPBRFr4daB2Jz9hc5Xft/i9/ZE" +
                    "2N1P/koRF90IElQ03Kzgo760j5v/WtfCXsY0JWoc3JCQeUwP089xCLFForx9MvnA" +
                    "arxtwZjdoJOsfXSVi3Xj9GShgMHxyK4e5Ew6bPMXQZ41WOo1HpcqjZSfbGL39/ZS" +
                    "OaUQ8Fx0fb+NKbiRw063MbUSGqQ54uiHif+jOLtxiCEqNJEYAl7ALN1Hh982Es+W" +
                    "HNGYKpuOKPnfga80ALWym+WMo4KpvpXnF+vqVy6ncQu/+43FdJuYwCFwVLHs/6CA" +
                    "on0pCT9jBqHan6oXnXNlBNkAB7j7jQi1BPQ9Eaoy09320uybU2HQ/Go1oep45are" +
                    "UT1U5jbDfaNyeGyIDJSdMeVy84nnOL/pZ/er7LxR+Ddei09U0qjGHT4BjDaQnIOj" +
                    "hygcQGcZDwPZFzfAvR0GrWGXzAFuOrTR30NXQeSfSa+EnsmydGf8FtRPGF6HFno2" +
                    "AJNigcDp8M6tiFnld1jDFq0CDaAc07csiMfMg8WZFlh8JEb2Zye69xB21mQnNRUw" +
                    "1vI2SspCUNh6x6uHtmqYNiE4a4hT6N4wd1SUuP2t2RHaJelvZWvgPZWrNQ+exrmi" +
                    "FItsi8GhOcxG9IKj2e8Z2/MtI9e4pvw98uuaM4zdinZZ0y56UqzZP8v7pTf9pLP8" +
                    "6Q/WBPB1XLNjQ4IHb498hpI2c3qaZvlK8yayfhi7miTzzx9zv5ieNvwYtV5rHQbe" +
                    "cHqBs52IEYxEohKEGwjK6FujoB9w2f9GdY9G+Dy5aBFdwM0GjHA7f+O508Phn/gc" +
                    "Na3+BX8NEossBq7hYzoFRakmBm6qm5JC5NNRZXfBQp/Skirh4lcDqgL0JLhmGGy/" +
                    "LoqsaTJobbE9jH9PXZapeMXsSjAWSC15D1rWzzivgE4oUKkWIaa24Tsn22E+4wh9" +
                    "jS7xOfJ1/yXnCN8svORJcEv8Te9yMkXEif17VhNJho4+qLDxs7VbUYIyKNJlz3Kr" +
                    "NQMBADpey10fnhza0NJSTC7RoRpfko905a1Wo4vtSdp7T5S5OPRMuQNaOq2t2fBh" +
                    "dYMvSNno1mcdUBfVDHYFwx6xuFGHS2jYMRDn88MDPdCm/1MrjHEDx6zzxMR1tjjj" +
                    "66oxFJQ3o/Wh8hJDK+kMDIYd//kFRreAMhVX1dGJ/ax6p/dw4fE+aWErFwgfZySn" +
                    "9vqKdnL4n1j7bemWOxMmrAigcwt6noH/hX5ZO5X869SV1WvLOvhCt4Ru7LOzqUUL" +
                    "k+Y3+gSNHX34/+Jw+VCq5hHlolNkpw+thqvba8lMvzM="
                ),
                "AQAB",
                (
                    "Nl0414LjL/TItwwGnXxlE8z3rN0H29YZ5NOpYhMOEdTn7nOnFpT7dHagvM6sBx8T" +
                    "WmmKBv7GD6upiA3qxYbkZBMYAu4KicYHDl2TSHvvRZX943veCB6L07RSYnMMXWDA" +
                    "oYfUXBVFdjO/dFwbP07GM7qZZzyirv+1/tBa1iCCyl+rO4F66BxvQCxtddrgNNdq" +
                    "1grgdVdlLGBeRVRSTB+Sdm5X5Xf3X9tYkAPubcLGlWPTgdc7O/w7pxd2GQoFJXPL" +
                    "uoRaxSNW8LVAahzMmjjFTwAxtlZ0bXiGpBexXxnbMDA4s2zA6+tV1uPHMsbcKRMm" +
                    "sLd8RasKh6kWbBc2hwn4+JVphUaR2n0V2BgqNkaJ2/Xg/EIHS9xEwEdSA++VT6Q9" +
                    "kMg5jUQnGUqJ7svYJJOUazGLptfzugdZcAbjwaYwImFzxTkGlBZ1pQYOKK7oVnNZ" +
                    "dUMmK1Ve2JHn5Nyw4sTE72eAaizQt9KnDq5FXGWrocmQVyp8rQS9J8idKNkBGwjb" +
                    "o9G+v1KRoyS2EWbERwTPi2kVJvYHkPAl8hKzRkd7R+CnFj4ygQy/wt4Q8vyBBwl2" +
                    "/W9IYOgig4/o0MOo0LpEy7Dy7Jq4WV6CIzLPUuvCBvLL9mD1g9fgTRroS5pwRDM5" +
                    "jMSG0hA1KdY/HkvlOJi8e2WVg9N/CFkd5TzN4xEpekibZiOfsUmReHcviHfjX/wF" +
                    "1S8Y/3vvdN8XNKdd/Aye2VYq0j6qLicSkCX68fXg0ruC4U+dRjoKs+HbzKKNgkev" +
                    "hvz4JLYnwqGLM3u/0UEV/UW5oWN4Pj4fZa3Xr810mJ8QqX2KbO1rVz5RUWRdz0xm" +
                    "oFjYdlW/sMb9reBMpRwfdDrlVFFCygRCWTXMhfQCWGI59GyLI+/avAeFGXTmHIDv" +
                    "Z9BbhO+I4vrn4R9oPzONUw4UTNaXTiBZYr0Q2FHqpIBtVWyOsT9DvPE039OnCMUX" +
                    "sT/PbtFm05AqLmAa1erGEFunZcn83TM6Qd4b7RAwNmTnl3vxA+RgnW/J82xNYwuO" +
                    "TVGAFooSQYiuJBbT/XSajaWtJef5u7kNdPaeD8AFovi2HGtzuLDGV+gXkSnjb5CX" +
                    "L6Xh4B/+MRO0J/yI5Wd1kp5TgP9GeHtO/Wm0zSB1WbuAWEZ+pWgvdL+6D08KEZaH" +
                    "PS78jMQZ21yrLHgTPQ7yVfzB8W35NzR2UtXrX4RcMWzjFxBIGwAbMfIr4/SVIqZI" +
                    "QaSZz+FqzsoYq8Dq5pkwM3j7InI/q/xGlemCHr7AP6Hktjpgce9tnYo9ISyj+3K2" +
                    "hZfvUitmvmlV9pzUZAO2wQGigr4aZb0A9mCT2cffwj3yZoorvkFhhGXCE8oGs7T3" +
                    "zVxWE/ZRdmvXJa0q3kXrFN2CvNXlu8sB+96kDpDxtnPdP8wTHF8xeyaP3+kzUe36" +
                    "QUdJzdmjeoE5V8EoBSjgie+96xNOtCAVLU+OxLYh3Eigwpu5AY6ucjuBOAuWoGCa" +
                    "EtNxjXJvhX4OGMC2LX09wMk7zjXmCJrITBXzLPBPPW6PbBYIDTirwafzXGWh0zwR" +
                    "pSVEqr2vvk9tgVvZvQfyxb3NhFJsJnoI2LCIjj+RYy8sPdM12vyX7GakeFJ753tB" +
                    "4+49GvXoTvlc7zFp77/AlUFeikx7SX4klK+VNr7IsI0eue3xqod5lEfPSywoAXWu" +
                    "IC/FI13ym1fsIO99oHBzFN450Xt2sG+9P07Y0N+QazxCYlfwLBkWEbdUsxUmFKdB" +
                    "Ms9rq/2PiC5xhQlijssd6nDm5WzgqbKDX+l01ltr+xiwRLidVCEJ/cNyim6VFCmK" +
                    "B5++CofeusjQMSqUC7b2phXmIqhUcgNJKM2cHhYqCiCLyqTa6t87ck/v9FSm81rV" +
                    "TI7+B6aq5m6VUxgfEr5Wl01g4UMjZiM1LXyUMuTw+5aMaev74DhFlGLIv7gVWy8f" +
                    "S6Ss2Qmt+aO1aOazBl8hbZP2NmX2SqIfmwEnNyBhM2Gj8NccoXkuLbvcTWkWIOBN" +
                    "Hm6CdA6JWzP4FtaS4/AGrzingviWnBP15IEtkJ+fURqb9hp41vfMBzqjGpFhFABZ" +
                    "9ZpyMgrWO8JHNuPkqtmKZXvIf5a5wKfyVa1nLBQM38lONqMiradaXdbRtNlpOYAl" +
                    "lw1/93YIKQRapBJKKdm5dCdAWsZm/7JBnwuE8otfjrkEHnD9rb0DDFCWLICAzaCH" +
                    "sLzwsXcwscrU2sq29uulHndvKmqpRe8bESxe7jxT+TfboUJPO13g5M6Q05AgDBl/" +
                    "BXr3vm7jqmSRQXlIpXJCv+I/mY6Jm36pROqZMu/HkYTrb0jPVmJgtfs6QCWGCE4w" +
                    "i+6CYRyji5S8JR4UqX39A0/ms9WWuT/cUBsrQADduGCOtbuQ7ZNxkUqekrEVacID" +
                    "1rXtlaULblL7FN4cP5mgWE8miobJ4SpbpQ1KBRcBiKltUqp8nCIGMzM9btR5+Bab" +
                    "DF83vot8gj07ybndVY3+bwaAFogG3lX6ybaRPnNHbenTX+4DXxRK8IWqrssOQXL2" +
                    "cSA7nG60tthi68Z5prAChJXjoOgz8TOuwsY74GyArWZHZ3lItpOfzEyyG3dITKt9" +
                    "0svAwNeSThYDqD5prtwAhkcLJpsIBvicWulXq0Xaqw2E7geG4LRmBreuVSJ67Fbp" +
                    "/zWjdoKebJK1zyltXFlQ19g3o9F2r1nMD+oiie0E3dRA1arId67h6HYL5DsiXsH9" +
                    "OCkadldT6JdOYJZR9lzcgfPDeqi+N4O4Nc3PPb52wnU="
                ),
                (
                    "yk0AOTleQMp8K97g3ZjrtfWOrSI/xEfUS336yp1a4tQQHn59BPy5HfQQxYvHQ+FO" +
                    "OU4jIpWwOjv/fPI0LNx219S8Z+M/7+puZPFUUQghjsMRkxsgXAcDW2B/kDSD1A72" +
                    "BdrZ1noLEt+O8EMP0HdXUA7lhFAo/k9jaK6S+dW1g7tERGbyNuVadRMFj/V5I6qK" +
                    "zDE2vDUlCRAD6ps/C2X9p8Npxqoweqp5bYDK8gn27aiD+Fii10Z5i8ODUcYEaBJN" +
                    "2YXigBTuKpoGwFpku4g3ebBKVxdZdH4FFaZ+AmDLmxMELyiDFurCuxSvZhtSBRU4" +
                    "NPfD7jJBOEf84bslMH6gVrc/tuV2xndGyKk8ISuFQj2edkXNaYyx5kaMe+5Sn1cS" +
                    "svD0Q65+jyLlWbv3zuOZ5G899NNu1PDQn5U/ev8kgmNspnLMIiB59zjqltGqzfP2" +
                    "c6ppARmcF1jSliWdtIXPObry/83KwJg9o2lYEUqg2XVaUGmV5sM2US5benizE/Gk" +
                    "2jsiy3AzWTRjTmdypPLqFLbCIYPiSEaQ67Asdrav7uifFU8BQnz2vcDDeMb54LlL" +
                    "Ji+wTUBd3guhuQE4tOS3O43uxmKnl1pQVmL5ra0Qf7k7eyDBr4ERUHCnwsSNN5zR" +
                    "tGSb5P4No8HEdVF4Iz8bzw4hCXE8yb+8DkKTAV2W9Y2dge5QAwKD6dTsFgkNNgpW" +
                    "rqPHQ2injuoqtXftCtRB5BGm+ASesy6Ywxh4vb2Oi7aemLVtD+/cEiIcvEp7/9fG" +
                    "C2ewo6UE98GF1ut07ItKA3wGJ1bYRdkuMGTuPIA4vCDDA5gf09yalDTyh2EqOHzc" +
                    "Q7mmJhFUjmubYFpruD1oP1xuFuFUuk3uIfpxzeyKls/TUFRTxL+dEEvTMwI8xj+p" +
                    "w1wEh8KnmQlydANbo1JacctvJI3Ly+MXkOXse7yF1WAkiLNtC6O9Q9pE1u3MwB9s" +
                    "S5xxY2AK7HPP0R1wnqq9fP4hRkerIUYEVk5qpcnQa8u61OT8bAOi8iIRJYVtC1K4" +
                    "b0IVbpyiZ/0WYEtt8C6rumZCRX58mrb4Mz+yWpx1fNTeStbWfspU4FqoVwz5UwjR" +
                    "BZ2SteMkdflCfyz3KwYTRJFA4hhJAqOo+/wFiXW/TSdDRLfX5RMzGmrCIGdWPyUr" +
                    "j810+g93jIFeiKAMM3q64WO3lCgp7A3vgDtvnoDAz1PbubGTIx2ZQDp9sIdnkyUn" +
                    "sXOhRD/cQnI8X1wsg1D6FVsL+wpB+IXID/Drvs2E7/J37XOqvHcrnTpixQhhStnd" +
                    "N4gzw6Sw3KeLpfVcz/8MAxHPq7Q0wCiuEqo6QhBGES35pUUsgZvcTlfME7TCG5mh" +
                    "6XFFmi598pje2nBTY+Epvw=="
                ),
                (
                    "xFz9wnWwjmsLis7NS5jqQAyr25r5X1emWywNq8asbhS5NimjXdqNTgOE8shpRVdi" +
                    "XL38vjoVcdmAfp1CLOef1I1qYwuDd6xxm1gclB8AOU8nk9WKCPGUZwixoqjIFOgz" +
                    "UQeydqPqy4Edd64kvdHD016+jPMopgy4RNxAgxI/svcPdQNCQ5k5R2RtM8T+prfO" +
                    "v8yiWZ7+Xeoi2GgemdJ+dr/s7RpUCphXHGO94O+WcSiDA97Ii2o5uVU1bhpdfCUp" +
                    "DW17zVxRzunRiaoaxYtGlpe5Cc1G+yvPDA2f4BCDu/CGec2uzxnfImIpcMQUgzrt" +
                    "8l4ZRTPUX02UUBAf4EZ5swxUDjGFv6egxGqvAL2Ig2cW6Esar1wZ2iBDsXabamM2" +
                    "MOdXQAyzR83HtpfIBXwNm9A0qigpP/sxvCPKDX+fPs9+gDKSKaDnxTvNCICCbAFd" +
                    "EMS0fwj1FU4Dkbx/wxvtMz9E1nE7HYVUjZueOLcRUGhk1/sysmQMyIuNM52aRHjm" +
                    "CEQNYlaFGJpIOEijer7FGsYB5GLnmEy0P3PFyAlzMvyVGUGUrgugUjGpPnEitxMd" +
                    "HtqtmUY1HsWQUoI3OqVJ76dugQBLSutly/0uToMsqwfTFVv7xFXek5qerC8iJHPm" +
                    "WPCDqHgHKZMXYoPLfwjXOqgy2dx1tAnwXXdCTaH/71zxf+SMMxuURLfZhyPabdIM" +
                    "DZMsuQvrc0ra/vCGtGv4Yx6DTzPn9yWGOZlzaO27LC52RWOJKkkpDirdQLn9WJsX" +
                    "oWNfo5QEm9I6/IYMvhMFoN7RnU3FCUYklDdrhRyq6Zi92lnJqfKLI2dUymstZMtp" +
                    "csJhYJUsBzIjpgoYFzdUGKlps973u62kIypjrDrBN4q17p8l2WLyUyjI0LxCtj5Y" +
                    "oGqkQrtxiu9zoje//11ZPgXAgoW2cslg2OhpsBj8msjgCkDWTGQNo+gg+57rzknb" +
                    "dDrJy23Ugh9J1bc3Oy2BIyhouaYYJaRdoWmiaysB/pNi6g1zAh9kWoJ6lITBysVc" +
                    "Fw10uVurb8SulE5B3mIQuC7xLaw3VjMIfFjD1fxr3Z5sgLG46FQEB1ScmiiJ+eB3" +
                    "8e2S4ybqD/LdA8t7Qj+tbRkYO16OpTOxaYg/Xlo/5ab1nyzniMP7YVOqCtXRtog8" +
                    "cHIBIE03BhEfCnTGNq/Jdjg0tjKIS8Ua2WlRvNuiBXkXRmex086uF05PNr8/hnWs" +
                    "HK50YP9N8M5BpJVOkH2sl/coeJ6SJ3aYbzDwdS0Ustcnz0j0wOz5/VfCasBqpDN8" +
                    "Qk7I9FpmCKBHVSpmJTppoxe0hMzviXhooP/nYFJhpKqNcd8XR4u4y9/uXaIfo5g+" +
                    "w6EvM+zdroqNDDQ6RC1/jQ=="
                ),
                (
                    "GBL3vteT3tP52OKqEdTb4Ah71SCpQ/tkSSORz8DQCwQ/ctGMoSZOBUGBKXEL4okS" +
                    "XQFubvQvR47SRZUxHlGSFvcrAJXriupz/rE1XntAOxP9qGrm++yduqcOJyQIuBib" +
                    "sHCt0bcuUC2offENFbrN+in7qDY92p2p79Auj2qeMjH72sQBeQTsMdh0pgAJTXRD" +
                    "Fi+ZGuacJKryPF4DL6EQgYFguhKQuFhHIP/dptYGu5t9MPWjU0kAt+ApZXbSGWxs" +
                    "NUGYhbN38DvqJ8PaDvMT3vhasGiH7bP9eOkaP8AzGp41tkL07qo7SDYa9WS06wPu" +
                    "b2c4usTiPAddEaPKti2reQZPn71I2C9jjgeNr0jVj99zVxHRcwkaNpQYrbrbvDiJ" +
                    "ch/4gYFncDMv5fDXeZhePO/8CIGMw+xwdz00k7d/KcEZMemhX0JMIV51lEMZN28b" +
                    "2gHigw4AJEserF2Hme7+jRkxR72+rhKv6x1jLJOb9qTffYhDHXYHpbuFiVqJvQrZ" +
                    "mlrFNj6A7dGtK6xl2TlLH/Hrwj9Gk2FKZ7HMaMguwZiPLeL7/GSQnF4vJNVQ8Sw7" +
                    "xCySp27MfNsXgMOjcutw3rZyPsuItBs8Sjt3CPL6bqilam6offE3FUKCxEvNnluc" +
                    "HQKIBsUw7FbnwSpTyKX+8jH1PoFqQXv+rhfAFL6Fc21J3Cd3ABSxjAcZnTmwh8jN" +
                    "LfUxhlUS84/sSzIdVFeUC8cJ/qPWGu6loTntTG8dYoT19KhKdUYPA11p3AJlJToR" +
                    "SFQrkh3WLIGsIrpcbLXatfVxagcMr6s7suif7TU5CzI+4tOcngK3poFyhyfJ9XTu" +
                    "ZWTXX9paHKSzldDM1tz/5eJi+3gPNCiH+SUrm9zVVUMgG4Qdf+FpmIHdfUl73/+9" +
                    "fREbPOiuNykHpMStiA8J0lbqQAhbw0SgDk8+SC9UIeNSFa58gJEYudVkscsUvZw/" +
                    "r/PLDo9kXWUeyvzc5RTefdxkK0/mDoydgYPNbzNICTvyXNlvLI92OahSMAvjwSAz" +
                    "8JGFqWcccJGOsyDm0VlMeF8o7coym9rASKEA4YWS+ar/VRqh5e7AEP7Y35scpIP9" +
                    "E9T/m4OPWDa0chvwwf70FgnPFdjb/2NofawtIIGRpWXRvIDAQXN6dl9UALUrb1JG" +
                    "D9PcYtGqYV8X19xr90hYqu/J7eilrICwCquICe26hDGviTaXkus3zIvpXzON4NXg" +
                    "Fl7zRwLufD3J73MxnOLrD9WI5HQBC8kn2LXL6CXe9wr8uJY2MD1iRFCpZlcr9NNe" +
                    "XvhnaJXVsjyCAtrjE6F/VXIuK3nDeUaeCHyXeDsli2/UMJW9ySK6IdyS3Zl6K/yp" +
                    "ZvVi2glEVbVZd9c8JTurUw=="
                ),
                (
                    "DXlYDEzpFYyw2RCBs8tFX6m+7S3AKNOp3Z2zPnM+h7syTk4jIKCLi6vgJoyr9I8f" +
                    "d7+tpRv1Nr+2+nkt/kjShdJCV5OFrOOPVBqCs4NBD62nyJQhiaWSClPlZITyXcTl" +
                    "KI0/qLZsuRQeAoVXjhLjvhBFQQS6aFJ9HnSClLve1Rfw3pWfqWXNMWHpwGCnHKeG" +
                    "L1EKXt3zFFypkXHrj6CK/vkCd+6Tj7qOV6tcbx/hkdg2zUAvQKnEVjxLk0eJ3Kfs" +
                    "HjjCAwBvuKQAtdKPTbjV2iWFE/AbC8cgyPHyY3yenXnOsHL1qM5cqk5UC6HYynxz" +
                    "sWrVjxMUYom/QJMqrMgJN8kDx4mMZO9Kr8+mPIXE5UdgogXtSdUnDPmjy3yZA37V" +
                    "TBvDt+hnMOkk6BmYJxAxGtz5kCd7VSGWcxN9nNmCAtxYENHnh6W74aPN2OSAjoq1" +
                    "aR4mSIVD0/drdUea+Ldk2lxgC9rvNIJen+zquXeOX5caPFvHSchlvCkfQkhxOnuV" +
                    "RUHuLS5EqcCEbiBF33lRqxmlLZe/zoqM38HA8436cqg0TuxaGGtB0AIKW/eFa0yL" +
                    "df+JY/gWUws5cP/wbDzFSwWRJpbvk699Z7byw35qxT2fNVr/dqRxxm0YsDX3wMqX" +
                    "JskyL5A05ZxrQV5Ly2a+5g5+lsZy6Sy5aqBxU0RnfHRDOgRjvmoJDYIUEhratShx" +
                    "nUjZC8WOnXWoe0/j3mN+QsboOboVE7dmc3NdIPkXG9wAT5iZ4+XrREaasgNRKBBU" +
                    "WcWo3V+dxVdyprtICo7hlv1TItkgSRegEO+QmCy0aZ0Kgf4hQWEcPQytG8qo7b6r" +
                    "eK1v4yG5SLEfExikOIuaYKrXTvlgxGcQ2TziZAIQGhCRlVMkVLteZ2hoBzKz3S+A" +
                    "A1nt9YpJK0BtDdeHfC1an0/jutEUCOJam+euwN+mDbT08p8qVUmUSgf2o21vPtOA" +
                    "lIQoLqZVq6wb8+dDifMAZnoyXXLRO1wA9L973qCv3Vkds3PCzYV77F4BrUlCxvgt" +
                    "7oME3Gc25092r0SDbpAKF6lY8Upc7bRIw8ePgJJ2kFl7loUbbA2/zQT4Tfe0KApI" +
                    "ELi9mIqmCvweQFFpHs2hx9et+vztCtb2OIiZ2I6WzdpcBlUdehwAltgX1fCAGGdW" +
                    "xlx8SUwjF257U8tZgo0jZNJMg6gKBDD9O6fnbO8hOgDqIPGimScYeQ7tjpMm98IB" +
                    "UmKCJ9m87mYoyPNZ1b5Z5n+WLlirLLwNj6urBE9YmUD8QVP/P3HDYafw9kRHzQaf" +
                    "YylmzqWZVMQywNxM8AcHEqLgxzAMy8EQZOPKa6ibfKIbXJHzVfx3bL3r7E7gnugm" +
                    "fmQECisZtw1YylqXGPCKTQ=="
                ),
                (
                    "n+rm+JVUflE2xL9LB+IxUmf85aSrEXtyeJDWONs76lEL3omN8rLP4Fq/s2kVn4Nr" +
                    "uobi4mPG2oINvtU+XaxVIb/90rqtYmj5UQekrfIVjkosqPevbonXiHFWGtvQBHVl" +
                    "zIP1qoeNim5P/3UnCA07SaK5dFrNSYr0pAFCudEcwe6sAqSE5PomswjacRdehXAL" +
                    "ePPuwYcJX1n/L6CMCUpb0c26ilxihihLwDikvT556AfB0QEAsNRrNORzvMtZD1Gz" +
                    "dIaEY+2mUCrIz/jAzP7slWTEOHoHtgIeFNFj7AZ3ztc2GcIAmiIjSPbXPsaCZNIN" +
                    "tT1jvIEzRSltchQl1ck5BPi0KCJ8WvajfSj/uM2e0I42kVJDbnkKTGsbUUlWns+T" +
                    "WiqgvS9UpMjfdzZsvYhabN54/1VqgvpVhg1/ZO3MWkDmNhsFLyopm/FHwE3KylJV" +
                    "+zPZ+ZwTm/oH639Fbvyk1WIXQS/5PVZSTSSfDgEfKocOTFEX+9WFDKJvrInAQl+Y" +
                    "eyfd/dOKL+VP5YFiWRQiiUCXJrYfqVHJ0/QYy+DqqgihSyYkRJuFj+0zuUvYmjIZ" +
                    "qwYCecJY7fceDkUFSwkRHyJK471Dr0zVxoSzW0M395u9J0kA+1sQIgTrpLlVKXSK" +
                    "4nNYEhuhDD8ja1yGW17XvdMPl2o5RE+9/fbSZXM1BFXoYV2gOaci6dJQciZgepZp" +
                    "6AN8MQOrEmAsJyvE/ZnMta6HZ3ngwPxCbvT3clB07uUxCsDy+ogpw3OL3cIz29l2" +
                    "1RDZacWR9rMIZTxAD42aNN6q49MnJt4N9cMTf8w3P5B2rLO4RKaDnaFWwHMN2SPB" +
                    "pRbRP1iFFIedtW9yziGXq/BiUD1S5fNlO6r4Gi4bZ/dIUtHxQY2Z8A4kIskxTNp1" +
                    "iplE5zptN9j+GJCOxg0qxXt/yr2c+ybzvY5nmk/ONjphs4MnTONb8O1YOjphUDXW" +
                    "7KpcXs9Hu68rqkSBEa+MhGmDz8NbDTP1Q4bVgAIZtEoWdlWMEDdEMihjU55XgNQB" +
                    "QACfhKn5nJe+SIgxjoqGZc+13uZDcowNySBXr8R1yujvwN1E2ODX2up7T9mFDXsr" +
                    "hFBkT7+mW1mOHJUxNJ3L70B+7iepvU1NEbBL2tVGcueSMGduUxXxTebNhCCQxrjC" +
                    "GJNKc5jCAVgK70SzHQ/HW1PApA8Vma+AV9aOopw32M0it1kAvuzHLbZTHp8OqFIB" +
                    "WEhzUCxK/cVFK8+yixqYU9/Eky936jzwaO6+QATdC/kJjTOTKAQ4yIR/q1HW16sn" +
                    "4Do8AsRi+Rtdx7G9UZbgu8rCShuFHzfF83uHwHsl8khazOQetuanBG/GxBdaeIJF" +
                    "mTymL1LOru69mA9gwhuFFQ=="
                ),
                // Windows 10 RSACng recomputes this to D-phi instead of D-lambda
                (
                    "g/NxB1drS4SOW29bCBIGfxwtQO2gE+KTdoKmY1HvD+FesXeAlwrSiGqA+vleTvm3" +
                    "SzWOgy8I8zWvHaacEbRe75Br0UI9Zst9aq0rlMmZ7iQlYKRjROeM8usgyjoAG7DZ" +
                    "4uiimqy/PXf5z+Jfg08jsbIe5uIRJWMo2xCQNlD+kSU8vyLbG8v/d+W53U19AF0m" +
                    "Mj4LhlxDzpP43RnObwgtkIJCIZ6urwojM+IvHe5T8ciDDjZpBAXGaTwBUHVz9BfB" +
                    "y8nGAm75JnYSvJe9D13vbMRykoVGsnsbkcUja19Us8RSHrVpavWE5FQVMVmX/0KR" +
                    "qgtxFZqhSvznsJMwS9k+S/IVIK9D2e+14XLuBgCFFwj6T/rvr7xoDcB6nMiEOFSz" +
                    "VUlquWZzbP0zcoWS33P+Mvol3/ujtL0YgpZT7gkM1+1Rqucs7ZdaUdcsHcvI/LBq" +
                    "SrDll/SqY7+xthfD/67i9kOD7NDlxaOmnPwViG6/EXlMd0UtoM0GgBIBdrp5++kL" +
                    "4HVeTzWsrdIxvs9ahFBp4kfGwQ950NWx+AfQjZ/BhQKlxrbt24TYgtnlLoiA+vb3" +
                    "wYYgjxGkOIzUJHBhf8sS9l7RpsF+FmMjZGxZplNqUTtGKxw6Epw7dzUEMcYWo3K5" +
                    "0fk753tYZAo+DQ8teovR5UHD+NMJzhY4e84txGDKMLPR1G/AkQtTCHi/IdglsSOQ" +
                    "UqfJjgxCxU1Q29bid1sc7Z+Ttpi2DyQ3dVHmI8PBCgOTDTWsK1XwCyij01wxsl0a" +
                    "WYGWuN5uJA1dZ4M855M+Ml12SDFEQx34h1RxDvQyaDgYPcaAzOnO7ryKJU93uZ/0" +
                    "l7QDvvV9L6s/bAbQHhe6PXWt9jOWPV7YkzMwPPiQv/pH3KfmxqGxq4BkrSNwB0Il" +
                    "UtFilvnbtWevl0OM90HwHPWf6i44096Hy9v8oglzHDiADZHovariyE2m/CN0cJcG" +
                    "aZUhdVKlSUN/siX6R2l+gbEkV6GV1l8ajdIT2V+rb3J9hyW8VR52x4GdF3oFUtEM" +
                    "5MjKwF7ktpwYbbzRZM9Shdf/tVoPxjrrGA3l3H70iHn655idPNrZETzGmaan1Sjy" +
                    "v3HMyqq3wps3kavweEYk6VhNSjCugvjiI/pF34ZpsGn+JPKG2gbIJ6DXZQFHVtMd" +
                    "OLmXUNt8MuOe4GFOWrY0Jsk9lPRYpshXkbFYKXqlsJ7HXF2r/wQh3739kIxKNLP6" +
                    "nxen6dJB84bbFdnI6Jg7328BaJiU5omFll09ut88ORDq0/svRYUrjzv48lVtNdvr" +
                    "lwog3sw0qhYSouOlcFz+LTWtqZMOK8eIak1vWsNfZO3yVhvCFLoHf/HcYGu7N8K/" +
                    "Ag9ZzyYvjLduo7JEDsAUpsnrLX6VJfJdUhCldRNRXJLNfMU9TDpA/2P37kcQhHBZ" +
                    "xPJpnViq6RiIvSViXiCQO5+WMmUdw9KfZ/kK/jQnU3sloCGCjClhxSKIPwj0i3ez" +
                    "Yq6hN2T7xYtdiVFh9d8yff0kz91Wuxd5JUUUpV45HiUg1rNuTk42OCnQxAVsch2Y" +
                    "sKbGcUzfRLsIBrRHRt2zkyofWCh6+R2bN3UkFeaAxe8FPu5uKAWLyuwB6hp7HMcC" +
                    "fUKp3XLKNDaXJ63CoHUAcqht6HffmvWmbYpkk1v3tZWP0oYLW38EwKWZACqKYzH3" +
                    "aUQWH7i3XuYgdVun7YckT71VNKMYhO7mAqvoWe6Blr1AljxHaCPS/gGC590oDZSv" +
                    "KV63vJDMpZ8rbMe75n/zSGt6w4eA10tli9tpf0ZVkUeL9N9tMcd8esqw1p+SJR5f" +
                    "uqsVzn1fyLxPEZEu+2RKlUZoDa02xczqcbWBBbiCKAW99GFl0USPt9ZuP0ruaIe4" +
                    "MI7Zw42jzGOk6lz0SqCkqIztLyz3J0S2x1rnyMdnyNaQV03htXv5/TdeUsf41NtH" +
                    "sdcVzvHepoMS9rgJs7+cxMfyDkLBav9NRl9LwQtovfhHVFbmW5vozjLnpJ0RnNWF" +
                    "Ap9fOrm/C64v13Tc53dKbkKZKPYFhhxjsj+NS01SRB6t3x2JsG7XT8+bynGwq+q1" +
                    "uF8/CdCXzJEkTlagI/8pIhdDl4RbyqT2zEkAC/rbh+DjCxbDk6ic7j52kWISZv4x" +
                    "gSZeftEmVM3lyNTLpPU+SJn2DGAXrmUdODUlcMJuM/lQmh5lPuHCfmxUWk7n6lEa" +
                    "KIs4locNJ7UpCCsS2KFmb8oC9rcOcqPvbHDQKHL2TxSHlcNyW1sA253bUrT9Ni5f" +
                    "27J9BF9Y6/hs9s7aF1EGYYTvSGCf3LXOREkkV9pQWPzsIIq2hm6MTsfNJ2vOU0x7" +
                    "oOpedAnT3Pl5uHR1/AIdG9LxO1Y67Z9PSvUsU7BJWTePmRAlHMNX0OxWedk6chET" +
                    "Dm3s+uc06j+cU3CthqC5BjH87GqG02FdB5G62Dld7bdCDUlgHAQ+25yKSCVgbyK3" +
                    "AXrz9vUcu3fz9k+v/FCy8KIjVEixoFq/LMvAVwKbMn472ymkj7qFn9SVqa39LtHA" +
                    "D2/g4SS9is4SXbzFmPAusiUcwgPJP57RnIUlcRjtCiLM8P2xI7A4KvZZnSvU9l2J" +
                    "TyDMqKY3/GUQOtPn8UO5IqMJG8TrFTq8NPIK2KS4Vo0ZJ8lq550HCyiy8oyNUxfZ" +
                    "KtSKYf1oqXHqc4ZUE+tdgR+cXvs0cygn6Su/Rj1mmOt2/khtgwYyuN5uScPK34mW" +
                    "vpCkUXUGrMkvA07fAiWZBKYtEWti3Tr9QzBqrM8VzWk="
                ));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWriteDiminishedDPParameters(bool includePrivateParameters)
        {
            // This test checks for the base64Binary version of DP (leading 0x00 written),
            // instead of the CryptoBinary version (leading 0x00 removed).
            TestWriteXml(
                TestData.DiminishedDPParameters,
                includePrivateParameters,
                (
                    "tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7" +
                    "UERRHhvKNiUn4Xz0KzgGFQ=="
                ),
                "AQAB",
                (
                    "r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5" +
                    "aWgXxQHoi7z97/ACD4X3KQ=="
                ),
                "83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=",
                "wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=",
                "AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=",
                "rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=",
                "wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=",
                // RSACng recalculates this D value
                (
                    "VEdFOzhWxK+zSJJ24rhs+tSl59Zot7VHHbdzk92R36s3sN9VgL82Mf+Ml4743oq5" +
                    "QstCaeRtW0L1TOBi0Dqxsw=="
                ));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void TestWriteUnusualExponentParameters(bool includePrivateParameters)
        {
            // This test ensures we pay attention to the Exponent value, instead of assuming
            // AQAB (0x010001 / 65537)
            TestWriteXml(
                TestData.UnusualExponentParameters,
                includePrivateParameters,
                (
                    "9rqCgyYMOZEbi12xig/zanjVWagNZCk90Aw1h1YAmzzokeHCCK7bnBWrtSSUEAj3" +
                    "U87XfM91zxdFP0zRApIRyzHfte1rI4+NljeOGoEgcUkXBeBDHaTXe7mZCqkLL4CJ" +
                    "m/F52slQ9tUtvL+v2i3vKjUpxbyIMuWtTlyPXNAejpM="
                ),
                "AbE=",
                (
                    "fVvObmKOMVmwlNngaZ7d0ZarEcPxhRH/etnchvqf8EcmWX3v402b6/p0zYz33ZQ5" +
                    "FLTE/JsR4TzlGtc2wguLsoKTYoACMK8Vnlo5fG/KCcnYxSGIjVLuOlBNs/qgiA1n" +
                    "3p1oMgPINc5zOBntOP7SXNYS8BczmQ0f+z2hNSQDFrE="
                ),
                (
                    "/viU9MUtmqmlQG4n6SdGzym0vZPhmSml2ot2KOPRhP8AGf3TjEHe+WPGfIVacDdv" +
                    "bZyWStgMNx0EtKs0QcByjQ=="
                ),
                (
                    "97lpk/oWI0YxJ0q7WjTTuErRzOchPWbsaJCS2dsfAb0CyT4UgmpHjsRH2Eh5dGZ/" +
                    "aAh+dzkkgPMWg4nIHG9Nnw=="
                ),
                (
                    "dADDeWmsGgY8ZzdwKaIgzpWioxxCkyJR9g3JkIjCDvv/dHjNn5crgW0/G67HAMz0" +
                    "BrXM81g+UKZUUjKyFaM7zQ=="
                ),
                (
                    "Mur83pA5wqsaEPHMpJLW+PJonDj3ddvOcufqKAyFfIOsBxKsH4kiN/MiRw984Yhb" +
                    "ONtQ+lpgxyRd5wJOYOSfnw=="
                ),
                (
                    "seFE3fshp/hUjgUdESvD41YpF+vZmz2quo5VaADYEATWU43kvoH2IHPzfKu0YS3Y" +
                    "gTIMHP3LsKsiX3tB2DJZow=="
                ),
                // RSACng recalculates this D value
                (
                    "Af6NLM+IFJEizysHpJbkHFpAZO/q0v1gktPBw0+foqiyEI0O3vYuHe+e8vqt1Y+9" +
                    "as1ZPjNW+bFCezDOQMKCzeT8hs2sQMZGvnJO4NDn3mkHhXakf+vKxZUPMyd6aJCB" +
                    "EhZJOKZ1zafwYOR8NdopvyZQl5n4F/ZRYat0BOsLr30="
                ));
        }

        [Fact]
        public static void FromToXml()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                RSAParameters pubOnly = rsa.ExportParameters(false);
                RSAParameters pubPriv = rsa.ExportParameters(true);

                string xmlPub = rsa.ToXmlString(false);
                string xmlPriv = rsa.ToXmlString(true);

                using (RSA rsaPub = RSAFactory.Create())
                {
                    rsaPub.FromXmlString(xmlPub);

                    ImportExport.AssertKeyEquals(pubOnly, rsaPub.ExportParameters(false));
                }

                using (RSA rsaPriv = RSAFactory.Create())
                {
                    rsaPriv.FromXmlString(xmlPriv);

                    ImportExport.AssertKeyEquals(pubPriv, rsaPriv.ExportParameters(true));
                    ImportExport.AssertKeyEquals(pubOnly, rsaPriv.ExportParameters(false));
                }
            }
        }

        [Fact]
        public static void FromXml_MissingModulus()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_MissingExponent()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_MissingQ()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_MissingDP()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_MissingDQ()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_MissingInverseQ()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>
"));
            }
        }

        [Fact]
        public static void FromXml_BadBase64()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                // The D value is missing the terminating ==.
                Assert.Throws<FormatException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>"));
            }
        }

        private static void TestReadXml(string xmlString, in RSAParameters expectedParameters)
        {
            using (RSA rsa = RSAFactory.Create())
            {
                rsa.FromXmlString(xmlString);
                Assert.Equal(expectedParameters.Modulus.Length * 8, rsa.KeySize);

                bool includePrivateParameters = expectedParameters.D != null;

                ImportExport.AssertKeyEquals(
                    expectedParameters,
                    rsa.ExportParameters(includePrivateParameters));
            }
        }

        [Fact]
        public static void FromNullXml()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                AssertExtensions.Throws<ArgumentNullException>(
                    "xmlString",
                    () => rsa.FromXmlString(null));
            }
        }

        [Fact]
        public static void FromInvalidXml()
        {
            using (RSA rsa = RSAFactory.Create())
            {
                Exception exception = Assert.ThrowsAny<Exception>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    83J7HoZ2IFWU08c6AidNTxqYBEopUchczRLG/FetGXs=
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSA"));

                if (PlatformDetection.IsNetFramework)
                {
                    Assert.Equal("System.Security.XmlSyntaxException", exception.GetType().FullName);
                }
                else
                {
                    Assert.IsType<CryptographicException>(exception);
                    Assert.NotNull(exception.InnerException);
                }
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/29515", TestPlatforms.OSX)]
        public static void FromNonsenseXml()
        {
            // This is DiminishedDPParameters XML, but with a P that is way too long.
            using (RSA rsa = RSAFactory.Create())
            {
                Assert.ThrowsAny<CryptographicException>(
                    () => rsa.FromXmlString(
                        @"
<RSAKeyValue>
  <D>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </D>
  <Unrelated>This is not base64.</Unrelated>
  <InverseQ>
    wsmVlMaMQHY36wRrMflOgRzNDMqqnu32O4Y1s4+GgQs=
  </InverseQ>
  <DQ>
    rCTM8dSbopUADWnD4jArhU50UhWAIaM6ZrKqC8k0RKs=
  </DQ>
  <DP>
    AAiO/cUU9RIt8A2B84gf2ZfuV2nPMaSuZpTPFC/K5Us=
  </DP>
  <Q>
    wLI66OpSqftDTv1KUfNe6+hyoh23ggzUSYiWuVT0Ya8=
  </Q>
  <P>
    r+byNi+cr17FpJH4MCEiPXaKnmkH4c4U52EJtL9yg2gijBrpYkat3c2nWb0EGGi5
    aWgXxQHoi7z97/ACD4X3KQ==
  </P>
  <Exponent>AQAB</Exponent>
  <Modulus>
    tz9Z9e6L1V4kt/8CmtFqhUPJbSU+VDGbk1MsQcPBR3uJ2y0vM9e5qHRYSOBqjmg7
    UERRHhvKNiUn4Xz0KzgGFQ==
  </Modulus>
</RSAKeyValue>"));
            }
        }



        private static void TestWriteXml(
            in RSAParameters keyParameters,
            bool includePrivateParameters,
            string expectedModulus,
            string expectedExponent,
            string expectedD,
            string expectedP,
            string expectedQ,
            string expectedDP,
            string expectedDQ,
            string expectedInverseQ,
            string alternateD = null)
        {
            IEnumerator<XElement> iter;

            using (RSA rsa = RSAFactory.Create(keyParameters))
            {
                iter = VerifyRootAndGetChildren(rsa, includePrivateParameters);
            }

            AssertNextElement(iter, "Modulus", expectedModulus);
            AssertNextElement(iter, "Exponent", expectedExponent);

            if (includePrivateParameters)
            {
                AssertNextElement(iter, "P", expectedP);
                AssertNextElement(iter, "Q", expectedQ);
                AssertNextElement(iter, "DP", expectedDP);
                AssertNextElement(iter, "DQ", expectedDQ);
                AssertNextElement(iter, "InverseQ", expectedInverseQ);
                AssertNextElement(iter, "D", expectedD, alternateD);
            }

            Assert.False(iter.MoveNext(), "Move after last expected value");
        }

        private static IEnumerator<XElement> VerifyRootAndGetChildren(
            RSA rsa,
            bool includePrivateParameters)
        {
            XDocument doc = XDocument.Parse(rsa.ToXmlString(includePrivateParameters));
            XElement root = doc.Root;

            Assert.Equal("RSAKeyValue", root.Name.LocalName);
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
            string expectedValue,
            string alternateValue = null)
        {
            Assert.True(iter.MoveNext(), $"Move to {localName}");

            XElement cur = iter.Current;

            Assert.Equal(localName, cur.Name.LocalName);
            // Technically the namespace name should be the xmldsig namespace, but
            // .NET Framework wrote it as the empty namespace, so just assert that's true.
            Assert.Equal("", cur.Name.NamespaceName);

            // Technically whitespace should be ignored here.
            // But let the test be simple until needs prove otherwise.
            if (alternateValue == null ||
                !string.Equals(alternateValue, cur.Value, StringComparison.Ordinal))
            {
                Assert.Equal(expectedValue, cur.Value);
            }
        }
    }
}
