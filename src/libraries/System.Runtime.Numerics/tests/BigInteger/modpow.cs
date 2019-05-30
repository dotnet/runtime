// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class modpowTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void ModPowValidSmallNumbers()
        {
            BigInteger result;
            bool b = BigInteger.TryParse("22", out result);

            // ModPow Method - with small numbers - valid
            for (int i = 1; i <= 1; i++)//-2
            {
                for (int j = 0; j <= 1; j++)//2
                {
                    for (int k = 1; k <= 1; k++)
                    {
                        if (k != 0)
                        {
                            VerifyModPowString(k.ToString() + " " + j.ToString() + " " + i.ToString() + " tModPow");
                        }
                    }
                }
            }
        }

        [Fact]
        public static void ModPowNegative()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            byte[] tempByteArray3;


            // ModPow Method - with small numbers - invalid - zero modulus
            for (int i = -2; i <= 2; i++)
            {
                for (int j = 0; j <= 2; j++)
                {
                    Assert.Throws<DivideByZeroException>(() =>
                    {
                        VerifyModPowString(BigInteger.Zero.ToString() + " " + j.ToString() + " " + i.ToString() + " tModPow");
                    });
                }
            }

            // ModPow Method - with small numbers - invalid - negative exponent
            for (int i = -2; i <= 2; i++)
            {
                for (int j = -2; j <= -1; j++)
                {
                    for (int k = -2; k <= 2; k++)
                    {
                        if (k != 0)
                        {
                            Assert.Throws<ArgumentOutOfRangeException>(() =>
                            {
                                VerifyModPowString(k.ToString() + " " + j.ToString() + " " + i.ToString() + " tModPow");
                            });
                        }
                    }
                }
            }

            // ModPow Method - Negative Exponent
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomNegByteArray(s_random, 2);
                tempByteArray3 = GetRandomByteArray(s_random);
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");
                });
            }

            // ModPow Method - Zero Modulus
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 1);
                Assert.Throws<DivideByZeroException>(() =>
                {
                    VerifyModPowString(BigInteger.Zero.ToString() + " " + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");
                });
            }
        }

        [Fact]
        public static void ModPow3SmallInt()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            byte[] tempByteArray3;
            
            // ModPow Method - Three Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                tempByteArray3 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");
            }
        }

        [Fact]
        public static void ModPow1Large2SmallInt()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            byte[] tempByteArray3;

            // ModPow Method - One large and two small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random);
                tempByteArray3 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");

                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random, 2);
                tempByteArray3 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 1);
                tempByteArray3 = GetRandomByteArray(s_random);
                VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");
            }
        }

        [Fact]
        public static void ModPow1Large2SmallInt_Threshold()
        {
            // Again, with lower threshold
            BigIntTools.Utils.RunWithFakeThreshold("ReducerThreshold", 8, ModPow1Large2SmallInt);
        }

        [Fact]
        public static void ModPow2Large1SmallInt()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            byte[] tempByteArray3;

            // ModPow Method - Two large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomPosByteArray(s_random);
                tempByteArray3 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow");
            }
        }

        [Fact]
        public static void ModPow2Large1SmallInt_Threshold()
        {
            // Again, with lower threshold
            BigIntTools.Utils.RunWithFakeThreshold("ReducerThreshold", 8, ModPow2Large1SmallInt);
        }

        // InlineData randomly generated using a new Random(0) and the same logic as is used in MyBigIntImp
        // When using the VerifyModPowString approach, these tests were taking over 100s to execute.
        [Theory]
        [OuterLoop]
        [InlineData("16152447169612934532253858872860101823992426489763666485380167221632010974596350526903901586786157942589749704137347759619657187784260082814484793173551647870373927196771852", "81750440863317178198977948957223170296434385667401518194303765341584160931262254891905182291359611229458113456891057547210603179481274334549407149567413965138338569615186383", "-7196805437625531929619339290119232497987723600413951949478998858079935202962418871178150151495255147489121599998392939293913217145771279946557062800866475934948778", "5616380469802854111883204613624062553120851412450654399190717359559254034938493684003207209263083893212739965150170342564431867389596229480294680122528026782956958")]
        [InlineData("-19804882257271871615998867413310154569092691610863183900352723872936597192840601413521317416195627481021453523098672122338092864102790996172", "5005126326020660905045117276428380928616741884628331700680598638081087432348216495484429178211470872172882902036752474804904132643", "-103139435102033366056363338063498713727200190198435", "-4559679593639479333979462122256288215243720737403")]
        [InlineData("-2263742881720266366742488789295767051326353176981812961528848101545308514727342989711034207353102864275894987436688194776201313772226059035143797121004935923223042331190890429211181925543749113890129660515170733848725", "313166794469483345944045915670847620773229708424183728974404367769353433443313247319899209581239311758905801464781585268041623664425", "3984523819293796257818294503433333109083365267654089093971156331037874112339941681299823291492643701164442964256327008451060278818307268485187524722068240", "-1502346344475556570236985614111713440763176490652894928852811056060906839905964408583918853958610145894621840382970373570196361549098246030413222124498085")]
        [InlineData("-342701069914551895507647608205424602441707687704978754182486401529587201154652163656221404036731562708712821963465111719289193200432064875769386559645346920", "247989781302056934762335026151076445832166884867735502165354252207668083157132317377069604102049233014316799294014890817943261246892765157594412634897440785353202366563028", "121555428622583377664148832113619145387775383377032217138159544127299380518157949963314283123957062240152711187509503414343", "87578369862034238407481381238856926729112161247036763388287150463197193460326629595765471822752579542310337770783772458710")]
        [InlineData("-282593950368131775131360433563237877977879464854725217398276355042086422366377452969365517205334520940629323311057746859", "5959258935361466196732788139899933762310441595693546573755448590100882267274831199165902682626769861372370905838130200967035", "6598019436100687108279703832125132357070343951841815860721553173215685978621505459125000339496396952653080051757197617932586296524960251609958919233462530", "-4035534917213670830138661334307123840766321272945538719146336835321463334263841831803093764143165039453996360590495570418141622764990299884920213157241339")]
        [InlineData("-283588760164723199492097446398514238808996680579814508529658395835708678571214111525627362048679162021949222615325057958783388520044013697149834530411072380435126870273057157745943859", "1042611904427950637176782337251399378305726352782300537151930702574076654415735617544217054055762002017753284951033975382044655538090809873113604", "11173562248848713955826639654969725554069867375462328112124015145073186375237811117727665778232780449525476829715663254692620996726130822561707626585790443774041565237684936409844925424596571418502946", "6662129352542591544500713232459850446949913817909326081510506555775206102887692404112984526760120407457772674917650956873499346517965094865621779695963015030158124625116211104048940313058280680420919")]
        [InlineData("683399436848090272429540515929404372035986606104192913128573936597145312908480564700882440819526500604918037963508399983297602351856208190565527", "223751878996658298590720798129833935741775535718632942242965085592936259576946732440406302671204348239437556817289012497483482656", "1204888420642360606571457515385663952017382888522547766961071698778167608427220546474854934298311882921224875807375321847274969073309433075566441363244101914489524538123410250010519308563731930923389473186", "1136631484875680074951300738594593722168933395227758228596156355418704717505715681950525129323209331607163560404958604424924870345828742295978850144844693079191828839673460389985036424301333691983679427765")]
        [InlineData("736513799451968530811005754031332418210960966881742655756522735504778110620671049112529346250333710388060811959329786494662578020803", "2461175085563866950903873687720858523536520498137697316698237108626602445202960480677695918813575265778826908481129155012799", "-4722693720735888562993277045098354134891725536023070176847814685098361292027040929352405620815883795027263132404351040", "4351573186631261607388198896754285562669240685903971199359912143458682154189588696264319780329366022294935204028039787")]
        [InlineData("1596188639947986471148999794547338", "685242191738212089917782567856594513073397739443", "41848166029740752457613562518205823134173790454761036532025758411484449588176128053901271638836032557551179866133091058357374964041641117585422447497779410336188602585660372002644517668041207657383104649333118253", "39246949850380693159338034407642149926180988060650630387722725303281343126585456713282439764667310808891687831648451269002447916277601468727040185218264602698691553232132525542650964722093335105211816394635493987")]
        [InlineData("-1506852741293695463963822334869441845197951776565891060639754936248401744065969556756496718308248025911048010080232290368562210204958094544173209793990218122", "64905085725614938357105826012272472070910693443851911667721848542473785070747281964799379996923261457185847459711", "2740467233603031668807697475486217767705051", "-1905434239471820365929630558127219204166613")]
        public static void ModPow3LargeInt(string value, string exponent, string modulus, string expected)
        {
            BigInteger valueInt = BigInteger.Parse(value);
            BigInteger exponentInt = BigInteger.Parse(exponent);
            BigInteger modulusInt = BigInteger.Parse(modulus);
            BigInteger resultInt = BigInteger.Parse(expected);

            // Once with default threshold
            Assert.Equal(resultInt, BigInteger.ModPow(valueInt, exponentInt, modulusInt));

            // Once with reduced threshold
            BigIntTools.Utils.RunWithFakeThreshold("ReducerThreshold", 8, () =>
            {
                Assert.Equal(resultInt, BigInteger.ModPow(valueInt, exponentInt, modulusInt));
            });
        }

        [Fact]
        public static void ModPow0Power()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            
            // ModPow Method - zero power
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray2) + BigInteger.Zero.ToString() + " " + Print(tempByteArray1) + "tModPow");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyModPowString(Print(tempByteArray2) + BigInteger.Zero.ToString() + " " + Print(tempByteArray1) + "tModPow");

                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray2) + BigInteger.Zero.ToString() + " " + Print(tempByteArray1) + "tModPow");

                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyModPowString(Print(tempByteArray2) + BigInteger.Zero.ToString() + " " + Print(tempByteArray1) + "tModPow");
            }
        }

        [Fact]
        public static void ModPow0Base()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            
            // ModPow Method - zero base
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomPosByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray2) + Print(tempByteArray1) + BigInteger.Zero.ToString() + " tModPow");

                tempByteArray1 = GetRandomPosByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyModPowString(Print(tempByteArray2) + Print(tempByteArray1) + BigInteger.Zero.ToString() + " tModPow");

                tempByteArray1 = GetRandomPosByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyModPowString(Print(tempByteArray2) + Print(tempByteArray1) + BigInteger.Zero.ToString() + " tModPow");

                tempByteArray1 = GetRandomPosByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyModPowString(Print(tempByteArray2) + Print(tempByteArray1) + BigInteger.Zero.ToString() + " tModPow");
            }
        }

        [Fact]
        public static void ModPowAxiom()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;
            byte[] tempByteArray3;
            
            // Axiom (x^y)%z = modpow(x,y,z)
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomPosByteArray(s_random, 1);
                tempByteArray3 = GetRandomByteArray(s_random);
                VerifyIdentityString(
                    Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "tModPow",
                    Print(tempByteArray3) + Print(tempByteArray2) + Print(tempByteArray1) + "bPow" + " bRemainder"
                );
            }
        }

        [Fact]
        public static void ModPowBoundary()
        {
            // Check interesting cases for boundary conditions
            // You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            VerifyModPowString(Math.Pow(2, 35) + " " + Math.Pow(2, 32) + " 2 tModPow");

            // 32 bit boundary  n1=0 n2=1
            VerifyModPowString(Math.Pow(2, 35) + " " + Math.Pow(2, 33) + " 2 tModPow");
        }
        
        private static void VerifyModPowString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);

            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static void VerifyIdentityString(string opstring1, string opstring2)
        {
            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {
                //Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {
                //Run the full calculation
                sc2.DoNextOperation();
            }

            Assert.Equal(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString());
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(1, 100));
        }

        private static byte[] GetRandomPosByteArray(Random random)
        {
            return GetRandomPosByteArray(random, random.Next(1, 100));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetNonZeroRandomByteArray(random, size);
        }

        private static byte[] GetRandomPosByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] &= 0x7F;

            return value;
        }

        private static byte[] GetRandomNegByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }
            value[value.Length - 1] |= 0x80;

            return value;
        }
        
        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
