// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if USE_MDT_EVENTSOURCE
using Microsoft.Diagnostics.Tracing;
#else
using System.Diagnostics.Tracing;
#endif
using Xunit;

namespace BasicEventSourceTests
{
    public partial class FuzzyTests
    {
        /// <summary>
        /// Tests the EventSource.Write[T] method (can only use the self-describing mechanism).  
        /// 
        /// </summary>
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))] // ActiveIssue: https://github.com/dotnet/corefx/issues/29754
        public void Test_Write_Fuzzy()
        {
            using (var logger = new EventSource("EventSourceName"))
            {
                var tests = new List<SubTest>();

                tests.Add(new SubTest("WJUYBHDKQB",
           delegate ()
              {
                  logger.Write("WJUYBHDKQB",
                      new
                      {
                          typeToCheck = new bool[] { false, false },
                          P = new uint[] { 148, 17, 71 },
                          W = false,
                          Z = new double[] { 0.268929284656853f, 0.742295096974026f },
                          A = 'A',
                          D = new uint[] { 189, 130 },
                          E = new
                          {
                              QB = 4,
                              OQ = new uint[] { 217, 87, 57, 82 },
                              WD = 99,
                              LE = new
                              {
                                  YSE = new byte[] { 11 },
                                  PCI = 149,
                                  LNK = 138,
                                  QXH = 58778,
                                  ICZ = 0.0947599141368456f,
                                  ROQ = new
                                  {
                                      MMBW = 0.8394049f,
                                      ZOZL = new byte[] { 152 },
                                      CVQC = new short[] { 15147, 1106, 24561, 3912 },
                                      UMNP = new UIntPtr(144),
                                      QQPE = new decimal(0.286001046321355f),
                                      BALW = new
                                      {
                                          GXOSO = new Guid[] { new Guid("2b2c6a42-1011-8ba5-b799-d5652c22d1aa") },
                                          XVPOF = new
                                          {
                                              HJRKVT = new TimeSpan(321),
                                              DBFFJU = 17,
                                              KAOGPS = 923,
                                              GGBLRC = new ulong[] { 93, 110, 152, 92 },
                                              KHVFCF = new
                                              {
                                                  HGWJUNM = new long[] { 12544, 32021, 27152 },
                                                  KLLPZZS = 250,
                                                  ZPNDOEB = new
                                                  {
                                                      UCUBZMJE = new double[] { 0.093586054674157f, 0.380068612927603f, 0.219240242251773f },
                                                      GYBXWALA = new ulong[] { 57 },
                                                      RCLPECCQ = 0.339707483695684f,
                                                      IETSJOER = new
                                                      {
                                                          YCHWSOMSB = 23,
                                                          SOYKCAJLU = new UIntPtr[] { new UIntPtr(204), new UIntPtr(152), new UIntPtr(162) },
                                                          OYEBBFINH = new decimal(0.381492302465016f),
                                                          BEJVKWDLA = new bool[] { false, false, false, false },
                                                          HLTFIQJFS = new decimal(0.637544767762322f),
                                                          VTXOMKNSO = new
                                                          {
                                                              LPYMUBHQMI = new float[] { 0.7763445f, 0.06919894f, 0.8328319f },
                                                              QRSIWWSECE = new IntPtr(556),
                                                              IPXWJSYIMM = 148,
                                                              HLHISSJNFP = new short[] { 29766, 20985 },
                                                              VEOQAJJLBZ = new
                                                              {
                                                                  LLJUDQKBTEZ = new byte[] { 104, 11 },
                                                                  OJLJVJISNRA = new uint[] { 82, 6, 191, 174 },
                                                                  HHDCFLHZWZY = new
                                                                  {
                                                                      PTDEWXGPDDBR = new TimeSpan(7588),
                                                                      SRSXRTYMCTQB = 0.92578462461279f,
                                                                      TKSKNUCFEWPL = new Guid[] { new Guid("77149377-01d2-8ebb-3de5-b840c76f8325") },
                                                                      AFQFDPTQGYHE = new
                                                                      {
                                                                          QGAKGVMVKTJGK = 27,
                                                                          NWIGJXMWDKYEA = new short[] { 15083, 28400 }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  );
              },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("WJUYBHDKQB", evt.EventName);
                }));
                tests.Add(new SubTest("CJKJMLJBBF",
                       delegate ()
                          {
                              logger.Write("CJKJMLJBBF",
                  new
                  {
                      typeToCheck = new byte[] { 144, 180, 220, 112 },
                      Y = new float[] { 0.8815918f, 0.1432135f },
                      M = new byte[] { 225, 152, 114, 22 },
                      S = new
                      {
                          BI = new Guid[] { new Guid("7920045d-6ec8-890d-e929-1f40a64fb71e") },
                          GW = new ulong[] { 62, 167, 2 },
                          ND = new
                          {
                              JRW = new ulong[] { 203, 37, 15, 211, 188 },
                              SQO = new UIntPtr(84),
                              AMA = new ushort[] { 59438 },
                              MFP = 'Q',
                              YIU = new
                              {
                                  ZWFM = 10944,
                                  GQEM = new ulong[] { 41, 76, 216 },
                                  ECKU = new
                                  {
                                      KMULH = new Guid[] { new Guid("bcf30fe3-027f-98a2-b982-450093ebf0fd"), new Guid("6ffcac80-e5e7-48c3-33fe-8db84f3f08f6"), new Guid("0463ae63-4b15-2c33-3037-c39cdf1c7703") },
                                      UKBPU = 'Z',
                                      JBNJA = new long[] { 8202, 23756, 10620 },
                                      WTIKI = new
                                      {
                                          NXKHCF = new TimeSpan(31898),
                                          NJRGUW = 46159,
                                          CRVKBW = new bool[] { false, false, false },
                                          SJOEPA = new short[] { 2137 },
                                          IWSJCY = new UIntPtr[] { new UIntPtr(21), new UIntPtr(211) },
                                          TZVSWD = new long[] { 16672 },
                                          QPRROX = new
                                          {
                                              MAJQJVK = new DateTime(19619),
                                              GUYXEFE = new int[] { 173 },
                                              ARBBIUP = new int[] { 8347, 10954, 12836 },
                                              NAJLIKK = new
                                              {
                                                  RAMOOKYD = new Guid[] { new Guid("e6be9984-08a9-1aea-2a46-b1c146257008"), new Guid("39c25f4d-32f0-f537-6d20-7460fb4a79fd") },
                                                  FHFKLMJO = new
                                                  {
                                                      GKYBVURGH = new uint[] { 56, 61, 66 },
                                                      LNNMFRQTQ = 111,
                                                      UXSKWZDPW = new sbyte[] { 112, 25, 56 },
                                                      EASZRTXER = new double[] { 0.348114812443086f, 0.107543426615905f },
                                                      QQRUSFTWQ = new
                                                      {
                                                          YNMEDCKUGE = new double[] { 0.0140729285842147f, 0.740236018197721f, 0.0671365298643413f },
                                                          RJNUGAWVLB = 21099,
                                                          GCTBHXVALW = 169,
                                                          OVOJCXMCUW = new Guid("12d5e3d4-43fe-2bb4-2b27-88a1fee864ce"),
                                                          HYZTAUMLVM = new
                                                          {
                                                              WZRTZGKIHVS = new long[] { 22723 },
                                                              LMSOPOXKELU = new float[] { 0.3263926f },
                                                              CZQKFSBYOSJ = new IntPtr(18080),
                                                              SMPDPISOHNC = new
                                                              {
                                                                  XWHPMVFZQIHI = new Guid[] { new Guid("f3cd6b2c-cbb7-89de-cb62-151e3b20bf97"), new Guid("b57c3949-8bf6-dec4-d8fc-efa4a6b46424") },
                                                                  HEMWTJRIWKVA = 0.1434038f,
                                                                  QJVLOMJKMLTM = new UIntPtr[] { new UIntPtr(53), new UIntPtr(12), new UIntPtr(138) },
                                                                  XKEXSDBOCKUS = new
                                                                  {
                                                                      GNLERVKNAZMWC = new short[] { 14879 },
                                                                      SBDGZKLLGXIRK = new sbyte[] { 114, 92, 71 },
                                                                      KRVGWYHTYCWFX = new Guid("16ba60bb-a226-bfc3-78b0-496a1145aff2"),
                                                                      VUBEWBGNTVBAY = new
                                                                      {
                                                                          UMTDYSBEGRFXKH = new int[] { 16891, 17800, 6819, 7975 },
                                                                          FNMYBOQFFFDDCL = new
                                                                          {
                                                                              UYIZLJTHFXDOCVG = 0.593175727218937f,
                                                                              JQPONZSYCXSNXAE = new
                                                                              {
                                                                                  FKZCIUVJXIPVNLVZ = "GGVKWJLXMLMALXVDWZKT",
                                                                                  UMVGKJGNGWOSZCJW = new
                                                                                  {
                                                                                      KHJLHNHSFYDFMHYYB = new ulong[] { 217 },
                                                                                      KEOSBGOMJQPTDPKRL = new TimeSpan(11322),
                                                                                      GFDALFBAZYPQFWRMG = 102,
                                                                                      KAYUIPJJBXWGNCZZN = new ulong[] { 218, 40, 129, 84, 165 },
                                                                                      DMAFPOUZJXLDEZUPV = new
                                                                                      {
                                                                                          HLVLCWADDRCOFWEFCP = new UIntPtr[] { new UIntPtr(27) },
                                                                                          JFOPZMPTRGSMERCLSA = 8705
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("CJKJMLJBBF", evt.EventName);
                }));
                tests.Add(new SubTest("KYAICCKJBZ",
                       delegate ()
                          {
                              logger.Write("KYAICCKJBZ",
                  new
                  {
                      typeToCheck = new sbyte[] { 87, 80, 42 },
                      V = new uint[] { 64, 220, 135, 210 },
                      Z = new double[] { 0.995523607356252f },
                      A = new UIntPtr[] { new UIntPtr(193), new UIntPtr(129), new UIntPtr(83), new UIntPtr(180) },
                      Y = 16830,
                      U = new
                      {
                          OR = new IntPtr[] { new IntPtr(7761), new IntPtr(8213), new IntPtr(18050), new IntPtr(4233) },
                          LK = new IntPtr(25433),
                          CF = new
                          {
                              OJS = new double[] { 0.18515396499315f },
                              GGA = new int[] { 21072, 687 },
                              GZF = new
                              {
                                  FYYV = new char[] { 'S', 'R' },
                                  KKCR = new UIntPtr(90),
                                  UCKP = new uint[] { 18 },
                                  USYI = 141,
                                  HSLD = new
                                  {
                                      PKLFD = 91,
                                      BUVGE = new Guid("04efffae-a94b-c865-f472-b73bb59007df"),
                                      AJULE = 60593,
                                      YKFMG = 48612,
                                      QXPNH = new
                                      {
                                          URFSPN = new UIntPtr(160),
                                          KANOZR = 107,
                                          MGHNAM = new byte[] { 28, 79, 42 },
                                          QAXHLW = new
                                          {
                                              GSXEZUR = new UIntPtr(178),
                                              RBOKRYH = new UIntPtr(140),
                                              ULIGTUF = new uint[] { 114, 72, 90, 157 },
                                              ZMRRBNU = 0.754486f,
                                              USYEHIR = new decimal(0.232300421331218f),
                                              XQISTZU = 10748,
                                              XLDKTPY = new
                                              {
                                                  FLXZZAQW = false,
                                                  OQDAOWVH = 35,
                                                  HTSXLCWJ = 121,
                                                  YIFBWTWZ = new IntPtr(20141),
                                                  MLGTFAIW = new
                                                  {
                                                      WGOMBPYRF = 26075,
                                                      TMTCGGKBD = 0.5224023f,
                                                      WKOWVNYRK = new
                                                      {
                                                          UZMGVMGQCF = 0.555727907249577f,
                                                          YGZRDNZQGT = new sbyte[] { 105, 113, 72 },
                                                          SCNSDEGLEE = 0.668662465954508f,
                                                          MGUZUZYGFL = new
                                                          {
                                                              LHXDJLRUNGK = new ushort[] { 15861, 5797, 30418 },
                                                              TXZBHIGUMZF = new IntPtr(3657),
                                                              AVHUVAGQFVZ = 0.591231960612923f,
                                                              TJQNPMDKFWZ = new IntPtr(18308),
                                                              XFRVXYMSRYG = "BLPYLRKGQDZRSOVCHWUI",
                                                              YWTNBAGMOWD = new Guid("0cb52a6e-d750-3fe1-39d9-0f38cf7aec8d")
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("KYAICCKJBZ", evt.EventName);
                }));
                tests.Add(new SubTest("SEUGYSAHTF",
                       delegate ()
                          {
                              logger.Write("SEUGYSAHTF",
                  new
                  {
                      typeToCheck = new short[] { 20307, 21397 },
                      I = new sbyte[] { 95 },
                      L = new IntPtr[] { new IntPtr(19235), new IntPtr(4206) },
                      U = 'W',
                      H = new float[] { 0.1146578f },
                      X = new
                      {
                          FG = 'R',
                          OA = "UGFSZFUYTDAHSYHUPVPZ",
                          CN = new UIntPtr[] { new UIntPtr(162), new UIntPtr(187), new UIntPtr(253) },
                          TY = new DateTime(901),
                          FU = 98,
                          GP = new
                          {
                              ZHM = 31805,
                              KVU = "RGBMVUXVJPHHRECKLCFE",
                              SNC = false,
                              YID = new
                              {
                                  KWXZ = new bool[] { false },
                                  TTDN = 0.664730781533164f,
                                  ZQQL = new sbyte[] { 56, 88, 36 },
                                  JWGG = 'Z',
                                  QBLT = new sbyte[] { 110, 38, 77 },
                                  WFCW = 75,
                                  IHOV = new
                                  {
                                      VRSHR = 206,
                                      LBGZY = new ulong[] { 11, 120, 30 },
                                      PEBOY = new
                                      {
                                          JZRGRO = new short[] { 7035, 21132, 8363 },
                                          ODUKOA = 6009,
                                          MGPINI = new bool[] { false, false },
                                          XHWTCX = new ulong[] { 74 },
                                          QMVALP = new short[] { 14737 },
                                          QZXXMO = new
                                          {
                                              JFOBSVZ = new DateTime(9863),
                                              QRTHSJU = 229,
                                              AUCEODW = new ushort[] { 64464, 50464 },
                                              SNLCDIX = new
                                              {
                                                  QEBFKTZQ = new IntPtr[] { new IntPtr(29413), new IntPtr(17589), new IntPtr(13598), new IntPtr(15503) },
                                                  WVSHFFAL = 0.8123934f,
                                                  BZYVSUYH = new ulong[] { 233, 193, 154 },
                                                  NLKCESMM = new char[] { 'X', 'R' },
                                                  LAEIAHLK = new double[] { 0.0359409777614945f, 0.912958334625214f, 0.796391336618174f, 0.417881801453364f },
                                                  CRNPPEDJ = new
                                                  {
                                                      RYBIXVGLK = new short[] { 12717 },
                                                      TNEAVAUPV = new Guid[] { new Guid("1ee67609-12d1-94de-de11-2a83ab93be1c"), new Guid("48e7b54c-6332-31cd-c5a5-e7bb83fd9f97") },
                                                      NZBLWXVNZ = 26490,
                                                      VISDCJCOX = new
                                                      {
                                                          WIVLPMNTOJ = 32,
                                                          POIWKKOPCC = new IntPtr(19628),
                                                          SWHSXLMGNM = new uint[] { 54, 46, 69, 232 }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("SEUGYSAHTF", evt.EventName);
                }));
                tests.Add(new SubTest("TZSTGRMMWK",
                       delegate ()
                          {
                              logger.Write("TZSTGRMMWK",
                  new
                  {
                      typeToCheck = new ushort[] { 40470, 43954, 43078 },
                      Z = new decimal(0.620294499034199f),
                      R = new Guid("069d4e29-ca35-cf11-5873-4db168ac93a7"),
                      X = new uint[] { 48, 247, 28 },
                      J = new ulong[] { 84, 88, 170 },
                      W = 'U',
                      L = new IntPtr(23557)
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("TZSTGRMMWK", evt.EventName);
                }));
                tests.Add(new SubTest("VGYNKZSMCF",
                       delegate ()
                          {
                              logger.Write("VGYNKZSMCF",
                  new
                  {
                      typeToCheck = new int[] { 29395, 27988 },
                      P = new uint[] { 91 },
                      B = new
                      {
                          TU = false,
                          ZL = new byte[] { 128, 7 },
                          PN = new Guid("b34b3912-f31b-2b0c-0ee5-6ed734ddba6c"),
                          LG = new
                          {
                              TRQ = 77,
                              FAC = new IntPtr[] { new IntPtr(32083), new IntPtr(26667), new IntPtr(15835) },
                              RMO = new bool[] { false },
                              BRB = new bool[] { false, false },
                              GVP = "NBSSETBDPOPZITUNVUUT",
                              BIB = new UIntPtr[] { new UIntPtr(112), new UIntPtr(3) },
                              WGL = new uint[] { 68, 150, 165 },
                              VEN = new Guid[] { new Guid("92bd75ae-ce7f-10b5-3765-378feaab145b") },
                              TUD = new
                              {
                                  QHYM = new ushort[] { 33766, 12015 },
                                  HFKP = 182,
                                  AOQN = new decimal(0.201885847468807f),
                                  RMKL = new bool[] { false, false, false },
                                  CQXD = new
                                  {
                                      QMXMS = new ushort[] { 11559, 14693, 48980 },
                                      PYYYP = 161,
                                      VRCVP = new IntPtr(9062),
                                      RTCQG = new decimal(0.492955001300646f),
                                      CHMOD = 25061,
                                      SBDBK = 30602,
                                      XBSXT = 21054,
                                      SZVHG = new
                                      {
                                          TVDJBS = new bool[] { false },
                                          RMKLCX = new ushort[] { 57723, 57354 },
                                          YTFFYT = 207,
                                          SNGUKH = 24872,
                                          XVCORB = new
                                          {
                                              JDRLGPI = 14885,
                                              ABBCCAU = new short[] { 22694, 11610, 1417, 27478 },
                                              AIFAFCJ = new DateTime(29164),
                                              NHUOZXJ = new
                                              {
                                                  BOMMDBGW = 'O',
                                                  GNEXBZDR = 193,
                                                  CLJXTEYI = new
                                                  {
                                                      YMCESAMHQ = new Guid("d27ddae9-cce7-9f7d-54f7-991f8c928418"),
                                                      INMSCIYQG = new
                                                      {
                                                          UKIEFZCCOK = new float[] { 0.88895f },
                                                          PHHCUELTJW = 11931,
                                                          VPZQVKGHCD = new sbyte[] { 29, 97 },
                                                          ZNAJEBWDTU = new UIntPtr(43),
                                                          LZNTFMLOPN = new
                                                          {
                                                              KKDLTZPBLLF = new short[] { 28224, 603, 2596 },
                                                              DBOIZPYBQTF = new DateTime(23795),
                                                              MCPSNEMCQGS = new double[] { 0.0949933985690556f, 0.508631406123113f, 0.471376011367597f },
                                                              NRIMZYWGPDX = new ushort[] { 23579, 5868 },
                                                              APJTJGXCDYV = 'A',
                                                              JQENVWZCBGS = new uint[] { 90, 227 },
                                                              GZRWYSOYDXT = new IntPtr(2662),
                                                              LKUKSNSHPGB = new short[] { 15418, 23013, 10500 },
                                                              BDMZIROPAYL = new
                                                              {
                                                                  GADETFSKPYKP = 225,
                                                                  EMMQSPUQQLMD = 18241,
                                                                  JHLMJESJNOJO = new
                                                                  {
                                                                      QMJRNFHVMCDJG = "HZVSROJFGXEFERZOFMZK",
                                                                      NTHBDIGQYDXNU = 0.3297728f,
                                                                      XHEZHGZXHWTQG = new int[] { 20652 },
                                                                      GNSIWUHHKIGTS = new
                                                                      {
                                                                          KHUARDFCBWECNV = new long[] { 12429, 13839, 3334, 11106 },
                                                                          SZMXXJULXEQZUR = new byte[] { 216 },
                                                                          GMABSYDYZEROFJ = new TimeSpan(27265),
                                                                          BZPGGVNXAOTZPD = new
                                                                          {
                                                                              HEDPGIZERTWNXSS = new long[] { 25117, 4881, 31806 },
                                                                              EIXUBUHLZCBEFDJ = new DateTime(13283),
                                                                              NLQCQHXIWKTXUFT = new float[] { 0.6921039f, 0.7196177f, 0.7388009f, 0.04133932f, 0.3286523f },
                                                                              VGGNDJIVJWCNOTD = new
                                                                              {
                                                                                  BVEVPMNAZMEJNOEX = new DateTime(27149),
                                                                                  ICPJJBIHHKVUIQPV = new
                                                                                  {
                                                                                      ZAYTSHFGAYZUGEZUR = new Guid[] { new Guid("ccd2bc8d-bbb5-63a0-336a-b08d63e4b5c1"), new Guid("c83dc34f-cf8a-5ef9-d0d6-497f22f191c4"), new Guid("dbb74de9-9a22-046c-64c5-566e576493e7"), new Guid("0331c59e-57d3-c9ea-1ab4-47befb9edc03") },
                                                                                      MMVTGFFPERWSXSFXJ = new
                                                                                      {
                                                                                          CIAXJJLCOACLACUFPJ = new uint[] { 63 },
                                                                                          MMBPCJCGWNZBLPFKDC = new ushort[] { 28738, 25721 },
                                                                                          QSFCEDOBNIJSSDPVZH = new char[] { 'Z', 'M', 'O' },
                                                                                          IVRMDKONPVLGJUOTHY = "IZVAQIRQLNJMRKVVYBZJ",
                                                                                          UATODHIHZJKWVDBVBS = new TimeSpan(31013),
                                                                                          PBMRXDIXPVOBVIFICD = new float[] { 0.463911f, 0.3401648f },
                                                                                          BGOFKPKGTTSOYSITOB = 162,
                                                                                          YKSDVJBNXTSYKDSIMH = new
                                                                                          {
                                                                                              GITCEWDZCDZMUAMIXQO = new IntPtr[] { new IntPtr(14017), new IntPtr(25483), new IntPtr(14026) }
                                                                                          }
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("VGYNKZSMCF", evt.EventName);
                }));
                tests.Add(new SubTest("KIXXGNPPSV",
                       delegate ()
                          {
                              logger.Write("KIXXGNPPSV",
                  new
                  {
                      typeToCheck = new uint[] { 147, 184 },
                      S = 'G',
                      O = new bool[] { false, false, false },
                      F = new sbyte[] { 105, 16 },
                      A = new bool[] { false, false },
                      H = new
                      {
                          WN = new sbyte[] { 10, 7 },
                          SH = new TimeSpan(10829),
                          MK = "JUAGXCEHNRNIBDPZFKHY",
                          FP = new DateTime(30607),
                          CK = 0.726815f,
                          XJ = 17,
                          FS = new TimeSpan(32702)
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("KIXXGNPPSV", evt.EventName);
                }));
                tests.Add(new SubTest("ZZMPYYMSWO",
                       delegate ()
                          {
                              logger.Write("ZZMPYYMSWO",
                  new
                  {
                      typeToCheck = new long[] { 30657, 17454, 29969, 21119 },
                      X = new short[] { 11810, 16279, 26120 },
                      E = new ushort[] { 56406, 7511 },
                      K = new long[] { 15497, 9818, 15265, 6621 }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("ZZMPYYMSWO", evt.EventName);
                }));
                tests.Add(new SubTest("XMBTXAWEQX",
                       delegate ()
                          {
                              logger.Write("XMBTXAWEQX",
                  new
                  {
                      typeToCheck = new ulong[] { 233, 59, 198, 173 },
                      F = new float[] { 0.9250653f, 0.1527098f, 0.1007128f, 0.6063406f, 0.2549662f },
                      I = new TimeSpan(11793),
                      C = new decimal(0.857221258272054f),
                      O = new sbyte[] { 123, 68, 32 },
                      S = false,
                      U = new UIntPtr[] { new UIntPtr(248), new UIntPtr(226), new UIntPtr(90), new UIntPtr(139), new UIntPtr(3) },
                      X = new ushort[] { 27845, 42792, 20026 },
                      K = new
                      {
                          EV = 111,
                          CJ = new float[] { 0.8725742f, 0.7231242f, 0.9686747f, 0.9467351f, 0.5980527f },
                          VO = new decimal(0.876706107927815f),
                          PZ = new
                          {
                              UBN = new sbyte[] { 107, 86, 106 },
                              XSC = 40815,
                              JRU = 0,
                              CMB = new
                              {
                                  EBTI = new TimeSpan(32196),
                                  AAGA = new DateTime(31518),
                                  IVPW = new UIntPtr[] { new UIntPtr(182), new UIntPtr(74) },
                                  RPGC = false,
                                  ESSO = new
                                  {
                                      AOMKY = new UIntPtr(109)
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("XMBTXAWEQX", evt.EventName);
                }));
                tests.Add(new SubTest("EWJSKUFCDM",
                       delegate ()
                          {
                              logger.Write("EWJSKUFCDM",
                  new
                  {
                      typeToCheck = new char[] { 'M', 'G', 'N' },
                      C = new byte[] { 183, 62, 138 },
                      N = new IntPtr(18625),
                      W = new sbyte[] { 104, 126, 93 },
                      I = new
                      {
                          MW = new byte[] { 116, 201 },
                          TC = new Guid("f0a8bc35-9f45-656a-789d-38e7ce5bedf9"),
                          SQ = false,
                          KE = new
                          {
                              FUY = new byte[] { 140, 126, 125 },
                              ZNA = 119,
                              GGB = new long[] { 26597, 25163 },
                              QDJ = 118,
                              QGA = new
                              {
                                  IKJP = "EYSLDHDFHAEYBXERUJOE",
                                  XFIS = new byte[] { 20 },
                                  HDBM = new float[] { 0.7934469f, 0.4888106f, 0.205552f, 0.8351575f },
                                  NZMV = new IntPtr(6535),
                                  FAJT = new decimal(0.196647235283929f),
                                  OOYL = 50841,
                                  KUOB = new bool[] { false },
                                  XJEO = new
                                  {
                                      IZHBY = 132,
                                      VOMZO = new
                                      {
                                          KDDJLD = new bool[] { false, false },
                                          NECOGL = new UIntPtr(9),
                                          YNLQSJ = "ORNEQWFPVICJETYGHJPQ",
                                          YSVWQB = new Guid[] { new Guid("2d7d5c2a-ce40-e08d-8a5a-83d3e6dd53a8") },
                                          GDXWRM = new
                                          {
                                              FSONMDY = 12985,
                                              CJQYNXS = 0.824358153075147f,
                                              NMKRDEM = 72,
                                              HCOBUMH = 'O',
                                              FXIXODO = new
                                              {
                                                  UHKMWJLD = new ushort[] { 21530 },
                                                  VAKIZKGC = "VKENOKXCMNZQWFFNDLBS",
                                                  BOEQUVNX = new IntPtr[] { new IntPtr(6273), new IntPtr(32489), new IntPtr(28577) },
                                                  YGDJXCGV = new Guid("032603a5-af19-a341-6c15-42d49512d07b"),
                                                  LWRNMSRW = new IntPtr(21112),
                                                  RLRVTTFW = new sbyte[] { 16, 81 },
                                                  XGUNAOXA = new
                                                  {
                                                      KXEKEUHEW = new ushort[] { 7332, 49220, 28804 }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EWJSKUFCDM", evt.EventName);
                }));
                tests.Add(new SubTest("MKBZPTUYLZ",
                       delegate ()
                          {
                              logger.Write("MKBZPTUYLZ",
                  new
                  {
                      typeToCheck = new double[] { 0.517561089022812f, 0.768031917404398f, 0.697841727965438f },
                      A = new ulong[] { 7, 222, 227, 157 }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("MKBZPTUYLZ", evt.EventName);
                }));
                tests.Add(new SubTest("MEVPINJRDC",
                       delegate ()
                          {
                              logger.Write("MEVPINJRDC",
                  new
                  {
                      typeToCheck = new float[] { 0.8504169f, 0.7903075f },
                      B = new Guid("5e2f0c16-18c6-8d90-9eb9-0d4f2abc4d2c"),
                      A = new DateTime(25657),
                      D = new
                      {
                          RY = 214,
                          RU = 16942,
                          EA = new double[] { 0.121163873524016f, 0.701091565052556f, 0.466620340229301f, 0.676146067993783f, 0.986018843942331f },
                          TL = new
                          {
                              BOH = new sbyte[] { 0 },
                              OVX = 25054,
                              LXN = new Guid[] { new Guid("14776595-10f6-4217-7f35-ac37db49694e") },
                              KMD = new
                              {
                                  AZHY = new long[] { 31925, 17938, 20945, 22306 },
                                  LQAJ = new double[] { 0.51103292010307f, 0.239482252970097f, 0.783272697955031f, 0.083597604224271f, 0.563169104309366f },
                                  IHXU = 'P',
                                  DFDD = new DateTime(6128),
                                  YFWB = new DateTime(2863),
                                  QVCS = "TTSARCJZBNWDBYBJZYPJ",
                                  PWRE = false,
                                  UNIY = new ushort[] { 18697, 3594 },
                                  PQOL = new
                                  {
                                      IXXWL = false,
                                      AKUDS = false,
                                      PVDFW = new ulong[] { 129, 170, 10 },
                                      SFIIO = new bool[] { false, false, false }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("MEVPINJRDC", evt.EventName);
                }));
                tests.Add(new SubTest("LLMNVBKLCH",
                       delegate ()
                          {
                              logger.Write("LLMNVBKLCH",
                  new
                  {
                      typeToCheck = new IntPtr[] { new IntPtr(19632), new IntPtr(14358) },
                      O = new ushort[] { 61702 },
                      S = new IntPtr[] { new IntPtr(19109), new IntPtr(24986), new IntPtr(7729) },
                      K = new
                      {
                          XO = "PWFTFSRPXAGCBUYKIZRN",
                          SC = new TimeSpan(26848),
                          EA = new UIntPtr(196),
                          FQ = 17032,
                          WX = new UIntPtr[] { new UIntPtr(92) },
                          VH = 0.370099f,
                          QV = new
                          {
                              UYZ = 30,
                              DMM = 'W',
                              QRO = new
                              {
                                  EJAL = new decimal(0.261909847269724f),
                                  KOTW = "ZDNPYWJYKCSPSANXLJEC",
                                  PCGL = 0.5556409f,
                                  WHBW = new
                                  {
                                      CMIRS = 0.7141032f,
                                      FGWQL = new char[] { 'E', 'P' },
                                      UTLKJ = new long[] { 27038, 5455, 4072 },
                                      CYOKG = new
                                      {
                                          HFMUGD = new UIntPtr(32),
                                          NFRGTB = 13629,
                                          UZQZBA = new
                                          {
                                              KKXSQWK = new ushort[] { 10475, 6419, 28740 },
                                              XNNTLQM = new UIntPtr(141),
                                              LHOUEPE = 16638,
                                              DOUGGFB = new IntPtr[] { new IntPtr(15736), new IntPtr(31929) },
                                              LXAOCBJ = new
                                              {
                                                  IGOWTEMS = 9387,
                                                  WQTCLHNG = 63,
                                                  QGHSCHAB = new float[] { 0.8993408f, 0.758041f },
                                                  EMLVIQSD = new Guid("3c0820ba-b0a1-7ec1-cc8c-7d1173510197"),
                                                  BRQLFTPI = new
                                                  {
                                                      MSBWNVRZG = 39,
                                                      MRHTOJEXU = 0.2055194742072f,
                                                      KHMLZQVMI = 191,
                                                      OPWIZPEIK = 17,
                                                      BPKJXXWPS = 19717,
                                                      DQOAKRJKR = new float[] { 0.21074f, 0.7595066f, 0.6031218f, 0.3303013f },
                                                      UCRWGJPSL = new
                                                      {
                                                          JBXGWAPTYQ = new Guid("313b7889-d866-d41e-f006-71027dc10138"),
                                                          XMFUZFNZAW = new int[] { 17962, 14401 },
                                                          VISAQNAADH = new sbyte[] { 15, 107, 104 },
                                                          HEJBQLAOHQ = new uint[] { 53 },
                                                          RMKKFMJFTK = new
                                                          {
                                                              ARXSHDTYZPK = false,
                                                              VLIOVOOEEMI = new uint[] { 1, 201 },
                                                              QUQYWZXDRDX = new
                                                              {
                                                                  DXVJQOVGZPOT = 0.2069851f,
                                                                  FWVNLKDADDJR = 12,
                                                                  PRDHKKXTFNPR = new short[] { 25626 },
                                                                  NBANDMUAHDSN = new
                                                                  {
                                                                      AXSFHXHTKXLMS = "ZJIFZYXXJOGCFLYMZCLH",
                                                                      RIYTHYMCCVOBS = 17,
                                                                      ZPXKABZGRKIUQ = 18075,
                                                                      USAIJZFYFBKPD = "FXFBTCOBGDDXIMSJFQQS",
                                                                      FBWNMWVZLKRBG = new
                                                                      {
                                                                          JYEWMNBVAJEKUQ = new float[] { 0.2768211f, 0.9715442f, 0.06664746f },
                                                                          LWUJEHXCDRYTPX = 5373,
                                                                          YPGXNDPKMFRSDS = new ulong[] { 9 }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("LLMNVBKLCH", evt.EventName);
                }));
                tests.Add(new SubTest("GSSURXLKGG",
                       delegate ()
                          {
                              logger.Write("GSSURXLKGG",
                  new
                  {
                      typeToCheck = new UIntPtr[] { new UIntPtr(25), new UIntPtr(172) },
                      W = false,
                      B = new TimeSpan(613),
                      V = 15965,
                      P = 14,
                      R = false,
                      O = new float[] { 0.2119886f, 0.3535131f, 0.2250638f },
                      E = new Guid("6c8a83eb-a37d-a308-f5dd-ccc79fdab7f5"),
                      Q = new
                      {
                          DN = new uint[] { 208, 161 },
                          ZV = new int[] { 24675, 31489, 14806, 30491 },
                          RS = new float[] { 0.08375355f, 0.7213013f },
                          EA = new
                          {
                              QHK = new byte[] { 168 },
                              JIP = new IntPtr[] { new IntPtr(27270), new IntPtr(17277), new IntPtr(2039), new IntPtr(11694) },
                              PMV = new DateTime(870),
                              SLZ = new IntPtr[] { new IntPtr(29682), new IntPtr(9895), new IntPtr(4137), new IntPtr(10702) },
                              NUA = new
                              {
                                  WTMX = new uint[] { 10, 77, 175, 250 },
                                  LKOS = 10196,
                                  MJDT = new uint[] { 26, 195, 158 },
                                  UZJJ = new double[] { 0.655860122132981f, 0.643569365443461f, 0.0688796327770127f },
                                  VZXB = 0.3647781f,
                                  DNKP = new double[] { 0.827328642284185f, 0.121152333505988f },
                                  MHKA = new
                                  {
                                      WFXDF = new long[] { 8716, 29806, 28535 },
                                      WPAOZ = new
                                      {
                                          BMJEAF = "NXXLUKSCDJUHFHZXGTTW",
                                          PTPCJX = new byte[] { 214, 160 },
                                          HXOTCE = new
                                          {
                                              MFQSTVF = 0.6321332f,
                                              NICYWLB = new uint[] { 51, 112 },
                                              MPDNCTI = new bool[] { false, false },
                                              ICAFYIX = new Guid("1ad99188-4607-f72d-9e4c-93efad2a4ff4"),
                                              NLQEJZA = new
                                              {
                                                  PAVCGVWJ = 29194,
                                                  IRNYLULX = 79,
                                                  HJUBXBNT = new Guid("c9ce72e0-8a38-a84c-98b3-ce13a0ed6c15"),
                                                  GEMNTMXN = new
                                                  {
                                                      MFQBITOHV = new bool[] { false },
                                                      TMGMIQBBI = 'P',
                                                      QFJIEEAEY = new decimal(0.788357664732429f),
                                                      KNYGBAMQF = new UIntPtr(88),
                                                      DOTYAECLW = new uint[] { 165 },
                                                      WAVZKKCVL = new sbyte[] { 0, 30, 85, 18 },
                                                      UBTOBPAOR = 108,
                                                      XCUOFDVBM = new
                                                      {
                                                          ZTRQXKCPBN = false,
                                                          ETSEMDNKIN = new uint[] { 242 },
                                                          DSEWEYZJOZ = new byte[] { 228 },
                                                          DQRNKMEDYF = new Guid("a5e89b5f-6219-7a0d-c148-454d0abc5fd8"),
                                                          WBRSOBRGME = new
                                                          {
                                                              URXTXPLDCES = new sbyte[] { 126, 73, 2 },
                                                              LAAVIBNXUDZ = new ushort[] { 25579, 6940 },
                                                              GUMHUXYVZPR = 17285,
                                                              SUDTIOZUGOB = new decimal(0.677672771586884f),
                                                              LQXXUTTHEJP = new IntPtr(21018),
                                                              JDAMKIGTITU = new bool[] { false, false },
                                                              KEMZGKLWMHY = new byte[] { 149 },
                                                              XWHNLNWJEVT = 0.899215282359726f,
                                                              TQDCXDXZIOV = new
                                                              {
                                                                  FCEMBXVCLPKK = 0.02340911f,
                                                                  HDLFISFCRVTJ = new UIntPtr[] { new UIntPtr(141), new UIntPtr(178) },
                                                                  EWOHMIIIKHHA = 253,
                                                                  OIGRHOVTNCGC = new bool[] { false },
                                                                  NRIOKHHSVCZM = new
                                                                  {
                                                                      ZZTDYIBWNJOBB = new Guid("da1f577d-cc73-2e6f-dde2-9630ffede26e"),
                                                                      IMAJIIVTZSPJC = new decimal(0.6431795552574f),
                                                                      DRUOWAJDYBEMD = new sbyte[] { 20, 64, 2 }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("GSSURXLKGG", evt.EventName);
                }));
                tests.Add(new SubTest("LOQEBEPSQL",
                       delegate ()
                          {
                              logger.Write("LOQEBEPSQL",
                  new
                  {
                      typeToCheck = new Guid[] { new Guid("7518ca63-b509-c673-d9eb-0824e240dd42"), new Guid("6cd7709c-d8fc-148e-6560-d6210dea45e1"), new Guid("b23b00d7-0044-b8fa-124d-c8d2ec881a60") },
                      M = 0.8267138f,
                      I = 73,
                      D = 9084,
                      N = new decimal(0.713712491427414f),
                      V = new
                      {
                          HP = 49920,
                          ZY = new bool[] { false, false },
                          XG = new IntPtr[] { new IntPtr(421), new IntPtr(16067), new IntPtr(28962) },
                          HY = new ulong[] { 96, 249, 104 },
                          FH = new
                          {
                              XBV = new long[] { 6401, 31685 },
                              RER = 5967,
                              SSA = new
                              {
                                  QJYE = new long[] { 18817 },
                                  OIGQ = new sbyte[] { 42, 99, 53 },
                                  QVQS = false,
                                  AGUM = 32659,
                                  CDUG = new float[] { 0.08679015f, 0.151259f },
                                  ENJG = new
                                  {
                                      YABLZ = new float[] { 0.5790285f, 0.5233452f, 0.5186909f, 0.9762733f },
                                      OGDFA = new IntPtr[] { new IntPtr(8695), new IntPtr(30823) },
                                      ZTAST = new
                                      {
                                          WWGFOC = new double[] { 0.863843390189038f, 0.844991408216297f, 0.979969940604628f },
                                          YLOXJW = 13861,
                                          BSMXBY = new Guid[] { new Guid("aa1f579c-24d6-5314-7191-bb7b9b12b1b6"), new Guid("5569e644-bf9b-216e-164b-9ce64e1c2b24"), new Guid("789b71a3-3d8e-01fe-efe6-145babf8912a") },
                                          WCLGJO = new int[] { 25676 },
                                          PKBTWX = new decimal(0.180795220742372f),
                                          BTPVOH = 3956,
                                          KISIFW = new
                                          {
                                              VYUKRZN = 2001,
                                              HSIZUWU = new ushort[] { 38936, 53683 },
                                              XWHIPGP = "TCSFOKASNSEZRWCEFNLI",
                                              VAWVPTO = new ushort[] { 10107, 27199 },
                                              LGWBZDG = 8670,
                                              ZQSNOOM = new double[] { 0.0428187232663942f },
                                              OQFEBNP = new
                                              {
                                                  FPNLCPLR = new float[] { 0.1014032f, 0.3860993f, 0.6688904f, 0.09752574f },
                                                  ZWWEBAKQ = 0.2813657f,
                                                  IMHIHSAM = 'A',
                                                  EIMUNTDT = new
                                                  {
                                                      KFAQFQNLE = 26239,
                                                      ULSIXDFDU = new decimal(0.645764567258658f),
                                                      EEBTQKCQI = new UIntPtr(146),
                                                      GPFRHQUWO = 28241,
                                                      OMSQDWKOZ = new Guid("6ff17edb-23c9-08c0-03c1-792273a4b281"),
                                                      UVMDXXABT = new
                                                      {
                                                          GHDFCCFWOI = new double[] { 0.306576067724533f, 0.233773795996687f },
                                                          HPPAAHFGUX = 6672,
                                                          VJFDTXVMXR = new char[] { 'G', 'R', 'N' },
                                                          MNAQOZRAFA = 4380,
                                                          OWWTWMLJLN = new char[] { 'A', 'Z', 'F', 'W', 'G' },
                                                          ASVKCUWQHF = 107,
                                                          GOSUFRMVVI = new
                                                          {
                                                              PWBBQKFYXZG = 7505,
                                                              EHXKDYOIIKN = new double[] { 0.381061800467345f, 0.782865050613352f, 0.258972210930182f },
                                                              RDUALXRCYJA = new int[] { 31701 },
                                                              KJIOFYYWQYT = new sbyte[] { 37, 32, 26, 47 },
                                                              WTHNQOIPOUD = new Guid("6895229c-4f0b-7c3d-aaf3-a56014f9516b"),
                                                              KYTGYXGRNEZ = 0.701357293269298f,
                                                              OJYAFAYTAXV = new
                                                              {
                                                                  KSMVWWPFPKOZ = new UIntPtr[] { new UIntPtr(117), new UIntPtr(247) },
                                                                  WVAJOHTLZYRN = 40510,
                                                                  ZBJBXVFLKVQP = new
                                                                  {
                                                                      UEUAKVQFEDTRI = 170,
                                                                      MWEJZDPJFYFOU = new Guid[] { new Guid("fec63d3e-5152-8880-e3a2-7db271ee6980") },
                                                                      FSZNORZOWWKYW = new char[] { 'L' },
                                                                      SJEYZNQHQRPLU = false,
                                                                      CKTSOZJBCPMII = 100,
                                                                      ETBNGWKVMXOKI = new DateTime(19209),
                                                                      GEBGTZFETRIIB = new IntPtr[] { new IntPtr(15287), new IntPtr(32715) },
                                                                      EHTBMWJHGTRPG = new
                                                                      {
                                                                          UZOBFDIIGCTPZO = new DateTime(4336),
                                                                          OJLZSHQHJDXXVN = new short[] { 32162 },
                                                                          YKTDHCYADYFAMM = new char[] { 'Q', 'V' }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("LOQEBEPSQL", evt.EventName);
                }));
                tests.Add(new SubTest("IIKFMEYQTQ",
                       delegate ()
                          {
                              logger.Write("IIKFMEYQTQ",
                  new
                  {
                      typeToCheck = "WMYTEZIZUWXNKVRDSJIG",
                      R = 4262,
                      S = new double[] { 0.0596655388640545f, 0.190778685822514f },
                      X = new
                      {
                          SZ = new IntPtr(32650),
                          WO = new byte[] { 100 },
                          SI = new float[] { 0.7200459f, 0.2501657f },
                          WD = new
                          {
                              IIR = new ushort[] { 65449, 3140 },
                              FLW = new
                              {
                                  ZDLX = "HKJQTKEKDUIUXIYEIMHI",
                                  RGOL = new ushort[] { 65310, 33463, 3995 },
                                  UUPS = new sbyte[] { 85, 120 },
                                  LRMA = 0.62267279514236f,
                                  GAAV = new
                                  {
                                      BUMNA = new byte[] { 195, 88, 126 },
                                      QJUIQ = new sbyte[] { 60, 101, 110, 105, 118 },
                                      LABOW = new IntPtr(7027),
                                      AWZEM = new short[] { 29744, 22884, 22085 },
                                      MKMJW = new uint[] { 78, 102, 105, 196 },
                                      YPFWT = new uint[] { 20, 181, 180 }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("IIKFMEYQTQ", evt.EventName);
                }));
                tests.Add(new SubTest("OTKXXDQCMC",
                       delegate ()
                          {
                              logger.Write("OTKXXDQCMC",
                  new
                  {
                      typeToCheck = false,
                      D = new UIntPtr[] { new UIntPtr(249), new UIntPtr(51), new UIntPtr(168), new UIntPtr(240) },
                      Z = new int[] { 23996, 23464, 22320 },
                      T = new
                      {
                          CD = "IHEEJMBOMBBTQBJQIEGE",
                          PZ = new byte[] { 218, 61 },
                          YI = new long[] { 24691 },
                          PP = 14,
                          CG = new
                          {
                              XCZ = new long[] { 25246, 6446 },
                              JES = "ONXNYPRHNKBVNGREITWF",
                              ZPJ = new Guid("05a49bef-7f0b-7412-0828-0a8a941d5d68"),
                              QGC = new Guid[] { new Guid("ab282266-2677-b6f7-af50-268fc88cafe5"), new Guid("18b696fe-2863-de74-0f51-68f09e5ea530"), new Guid("df2cf0c6-c814-2363-e298-41fde538eebf"), new Guid("ffbe4d57-9837-a616-4dbe-b1c7ea986d3d"), new Guid("ed821f7b-40b5-db46-9d12-6b3002df8c6d") },
                              VCB = new byte[] { 230, 209 }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("OTKXXDQCMC", evt.EventName);
                }));
                tests.Add(new SubTest("DPTBVTJEZM",
                       delegate ()
                          {
                              logger.Write("DPTBVTJEZM",
                  new
                  {
                      typeToCheck = 240,
                      D = new decimal(0.738066556275853f),
                      K = new double[] { 0.229902017037339f, 0.41293801107115f, 0.473091363661499f },
                      B = false,
                      S = new ushort[] { 42468, 21158 },
                      O = 112,
                      E = new
                      {
                          QP = new byte[] { 167, 11 },
                          KQ = new UIntPtr[] { new UIntPtr(62), new UIntPtr(96), new UIntPtr(188) },
                          HL = new Guid[] { new Guid("0f9ea815-e8f1-f446-3734-8924c1651d28") },
                          JG = new float[] { 0.04892373f, 0.1359699f, 0.2531015f },
                          CY = new
                          {
                              OIW = new float[] { 0.6120024f, 0.8221205f, 0.2808428f },
                              ZVM = new ulong[] { 58, 115, 175, 218 },
                              DGB = new UIntPtr(95),
                              HNJ = new DateTime(32745),
                              URT = new
                              {
                                  KGSX = new float[] { 0.3245929f, 0.9920719f },
                                  AMPA = new DateTime(21906),
                                  BCBM = 27456,
                                  DCDE = new
                                  {
                                      JTTZV = new long[] { 24658 },
                                      URCCB = new char[] { 'F', 'J', 'X', 'G' },
                                      BDYPR = new
                                      {
                                          QBCVFK = new IntPtr[] { new IntPtr(3843), new IntPtr(2593), new IntPtr(12594) },
                                          QNQGCJ = new ushort[] { 55646 },
                                          JMAFAG = new
                                          {
                                              ERZAUKO = new uint[] { 11, 190, 127, 195, 48 },
                                              MOHRYUK = 7512,
                                              VDELVZX = new Guid("7843d5a5-b907-6dd3-924d-be1cb93d035d"),
                                              UMOLQVX = new
                                              {
                                                  DFJNYFQI = 0.237980773317619f,
                                                  BMCPBJJE = new short[] { 20062, 5735, 10246 },
                                                  GDBWLNFS = new
                                                  {
                                                      YAXMTUOBD = 32596,
                                                      VHBLVMYEK = new long[] { 28385, 7676, 5331 },
                                                      MSXWCIQNR = new
                                                      {
                                                          LPFTQPSHBA = new Guid[] { new Guid("3a66afd1-95ac-d049-dd5e-c92e29156522"), new Guid("70972ebb-5f42-92cb-459f-73b9335cf4e7") },
                                                          QEYBQBYPAI = 54,
                                                          LUVYLSNKYB = new long[] { 8363, 23005 },
                                                          IVFTQDKAQX = new
                                                          {
                                                              BSYASPOCKHM = new uint[] { 102, 49 },
                                                              XJPHSAQOZVT = 0.2405056f,
                                                              FZWXSHNSCZK = new byte[] { 220 },
                                                              WVRXQNQKWHO = new ushort[] { 62702 },
                                                              QHMTDHWUJCN = new
                                                              {
                                                                  KTNRDUJEGUEU = new byte[] { 6, 201 },
                                                                  GFKIRIUKRFFI = new decimal(0.676525380311778f),
                                                                  EDMTDNXAYHCX = new
                                                                  {
                                                                      JWNLNPSYARJBL = 39247,
                                                                      VLXLNBCRSBUNH = new ulong[] { 193, 37 },
                                                                      DFUYXXGSQPXCM = 0.657904632230245f,
                                                                      HYYPEMJQJIUWT = false,
                                                                      FRULFVQGRLOTH = new DateTime(26772),
                                                                      YQXGTWPRBTUPK = 68
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("DPTBVTJEZM", evt.EventName);
                }));
                tests.Add(new SubTest("GMAJUPRHAJ",
                       delegate ()
                          {
                              logger.Write("GMAJUPRHAJ",
                  new
                  {
                      typeToCheck = 41,
                      H = new bool[] { false, false, false },
                      Z = new long[] { 15790, 12263 },
                      I = 57,
                      R = 0.2723505f,
                      C = 61611,
                      P = new
                      {
                          CH = 'B',
                          NH = new ulong[] { 88, 62 },
                          PI = new IntPtr[] { new IntPtr(8424), new IntPtr(788), new IntPtr(282), new IntPtr(6940) },
                          NN = new TimeSpan(14044),
                          BJ = new TimeSpan(1834),
                          GL = new
                          {
                              IYM = 97,
                              XVC = 73,
                              ERL = new ulong[] { 17, 60 },
                              SRZ = new
                              {
                                  ASJE = new double[] { 0.973957652213963f, 0.746552592025396f, 0.430610016188868f },
                                  ZZAG = new char[] { 'T', 'C', 'V' },
                                  WVNV = false,
                                  PLXU = new long[] { 21852 },
                                  SJWO = new Guid[] { new Guid("e558801f-c31a-e6ef-b2cb-bf4a96d1e543") },
                                  RHIF = 116,
                                  TLQH = new DateTime(32402),
                                  UWHP = new char[] { 'X', 'A' },
                                  XLPC = new
                                  {
                                      CNDXD = new double[] { 0.0393844242391104f, 0.903604804493303f, 0.122730238420297f, 0.379496013456721f },
                                      OKOAO = new Guid("6621804c-18e4-2f3c-9ec0-78dfb01d8ddc"),
                                      RCOBB = new Guid("b75881f5-bee2-9040-ba53-c5a8a9ee8126"),
                                      PZAIN = new
                                      {
                                          DCAQZK = new uint[] { 114, 60, 155 },
                                          CVVKCS = 'Z',
                                          JTIIQP = 66,
                                          KNEVYD = new IntPtr(22043),
                                          WFWNTO = 23886,
                                          TEPHTQ = new UIntPtr(108),
                                          OEPHAF = new
                                          {
                                              MMIHPWK = new decimal(0.471738442998258f),
                                              YANXWFQ = new DateTime(27885),
                                              JOLALPT = new
                                              {
                                                  PYHTOPUR = new IntPtr[] { new IntPtr(5154) },
                                                  LYTOKIRI = 142,
                                                  AKIOTERQ = new
                                                  {
                                                      BLWRWHMCD = new float[] { 0.9205768f, 0.8986465f, 0.8291523f, 0.8944461f, 0.2247913f },
                                                      WFMZEXJUE = new short[] { 17005, 29689, 30056, 27417 },
                                                      DXQONYFUL = new ushort[] { 8615, 56659, 3085, 44246 },
                                                      RXJTCMBHF = new
                                                      {
                                                          HZMGVYJJWU = false,
                                                          CTUGSYEYOW = new ushort[] { 16254, 39925 },
                                                          NOQKVXTWXB = new ushort[] { 3295, 5237, 60213 },
                                                          JTPOVYRGNT = 0.9116467f,
                                                          ZVTDCGTCJK = new double[] { 0.460546442056329f, 0.598205414879231f },
                                                          SYBWDQABPJ = new
                                                          {
                                                              WYIPCPFWIUS = "PILGVNDMFUDGAGUEHQGD",
                                                              DQTQIABZLFV = new UIntPtr(72),
                                                              UDXSTAPZUUN = new uint[] { 14, 0, 177, 109 },
                                                              XNBNRSMBTMW = new
                                                              {
                                                                  JTPIXNDTESLS = new sbyte[] { 59, 3, 14, 62, 69 },
                                                                  ZWAIVGMFDOXX = 15282,
                                                                  LWAQAJALHBHQ = new
                                                                  {
                                                                      TAKMUQLRTQBKR = new IntPtr(1212),
                                                                      MTZPOQTWKBKSY = 10041,
                                                                      YBEMMURTHLWJU = 0.4509597f,
                                                                      ZKMMYOCRCCGOB = 'C',
                                                                      TTECCJMDLTUZR = new
                                                                      {
                                                                          DSGYBUBZFLPAWJ = new UIntPtr[] { new UIntPtr(204), new UIntPtr(166) },
                                                                          GPABSDFSSGNHBT = new int[] { 30221, 12534 },
                                                                          KNKVHNHRGNHOBG = new bool[] { false, false, false, false },
                                                                          SFNFYPLPBGINOR = new uint[] { 250 },
                                                                          LLLVSOXSHJVEVQ = 26563,
                                                                          FEOVFYQVBVUBET = new
                                                                          {
                                                                              DOIPDQVEQEIUYOT = 'N',
                                                                              PWIJCJMNVPUXUVO = 24,
                                                                              HVFNARBNKNLABBU = new byte[] { 135 },
                                                                              MILTMZCPVFKTKMG = new Guid[] { new Guid("b83ff17d-2702-9c12-290b-4303387f219d"), new Guid("200f095f-5732-fe78-0967-abcb01d2e441"), new Guid("1ee05a4c-5f30-dab4-7aa0-2af5ae238327"), new Guid("e8bf5c8e-0440-ab1d-f05e-0a711ef78cf6"), new Guid("b0d94093-586b-d8dc-d314-b88847dae21a") },
                                                                              MMNXOFGWSWBNTOA = new
                                                                              {
                                                                                  WLJGBNUWDXELRUAA = 15778,
                                                                                  OHNLXCFWUCBCBRKW = new Guid("01dc1a64-6e84-d056-0981-3368b8d10193"),
                                                                                  ZIVRQKAKTPBRVWXA = new
                                                                                  {
                                                                                      ZSJALDWQATBUDUHXB = new long[] { 7547, 7641, 11480 },
                                                                                      XWRCLNJQVCGFEARRN = new UIntPtr(3),
                                                                                      WMFQWJFHZHDPYQRDC = new bool[] { false },
                                                                                      PYUGVYMVQCCPKDFJT = new
                                                                                      {
                                                                                          RGUSFGKIRMIONXPRGJ = new sbyte[] { 122, 69, 107, 70 },
                                                                                          KSKNPFWSTMTHXLWFNJ = 30426,
                                                                                          YDLEGCENYDULHNHNKF = 13655
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("GMAJUPRHAJ", evt.EventName);
                }));
                tests.Add(new SubTest("QOFSQMRJSI",
                       delegate ()
                          {
                              logger.Write("QOFSQMRJSI",
                  new
                  {
                      typeToCheck = 17197,
                      C = new bool[] { false },
                      W = 0.6688777f,
                      K = new TimeSpan(13461),
                      H = new IntPtr(1327)
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("QOFSQMRJSI", evt.EventName);
                }));
                tests.Add(new SubTest("RUAPYUJPXJ",
                       delegate ()
                          {
                              logger.Write("RUAPYUJPXJ",
                  new
                  {
                      typeToCheck = 26904,
                      L = new IntPtr(28072),
                      Y = new
                      {
                          SR = 16489,
                          DS = new IntPtr[] { new IntPtr(9161), new IntPtr(15856), new IntPtr(19730) },
                          ZR = new byte[] { 113, 182 },
                          IT = new decimal(0.503867492780028f),
                          PL = 11645,
                          CP = new
                          {
                              YYD = new byte[] { 81, 83 },
                              DTI = new char[] { 'S', 'K' },
                              UWI = new Guid[] { new Guid("0b9ab745-c38f-ae5c-ee8b-be32c888d439"), new Guid("d5c4bd0c-c295-66ed-b3aa-5f086561b2b7"), new Guid("fdab661a-d787-844c-86d0-51ab34b91d13") },
                              HXH = new float[] { 0.03633814f, 0.08205427f },
                              TQV = 199,
                              PLI = 19842,
                              SQB = new ulong[] { 32, 88, 130 },
                              GZR = new
                              {
                                  FMFI = new int[] { 21690, 19727 },
                                  XNMX = new ushort[] { 1517, 46281 },
                                  YTDF = new char[] { 'C', 'M', 'I', 'C', 'W' },
                                  LCPS = new
                                  {
                                      LTNRE = new short[] { 21083, 30470, 1846 },
                                      LIFCW = new ulong[] { 126, 161 },
                                      WRFAB = new UIntPtr(241),
                                      LYTWY = new sbyte[] { 100, 94 },
                                      YNOPT = new
                                      {
                                          UPGMIC = 'S',
                                          VPWXWU = new Guid[] { new Guid("0bec6bd3-0f49-3e13-0a22-f7a6466de69c"), new Guid("9a2d7e8a-e798-6fbd-f47e-c0fbc55d8935") },
                                          FBNUBH = 23436,
                                          HWAHNU = 119,
                                          CMVPEX = new UIntPtr(30),
                                          EXQJXZ = new sbyte[] { 100 },
                                          AHHNBZ = new
                                          {
                                              SIRPAGK = new TimeSpan(5108),
                                              YJUKYUM = new
                                              {
                                                  IDWPDRFA = 0.4160521f,
                                                  HFNDKDTN = new UIntPtr(110),
                                                  WRGSYPLN = new uint[] { 124, 112 },
                                                  IFUYJFJP = 177,
                                                  OSLRETMU = new Guid("1d9a5d0f-2d11-4e49-41bb-c6e749ffb73c"),
                                                  QLWXMLQW = new uint[] { 110, 126 },
                                                  TMCDKBLP = new
                                                  {
                                                      TFTEWLADF = 23,
                                                      DQONAXIXL = new UIntPtr(57),
                                                      NKQSCPIGC = new double[] { 0.335936982341081f, 0.798803394566664f, 0.203710686510294f },
                                                      ATXMOLXJG = new byte[] { 218, 243, 154 },
                                                      ZXDXPCMTN = new
                                                      {
                                                          DVQGUGTXNX = new Guid[] { new Guid("7f510656-4c99-2866-7925-7f05f2e31ee5") },
                                                          FPVPOIXABR = 105,
                                                          KSPEAFTJUU = new
                                                          {
                                                              ECRRIRZWISX = 135,
                                                              CZOEVMBECBK = 5,
                                                              TMSOBCLKSXW = new
                                                              {
                                                                  ZFBFJWHHZBBB = 51826,
                                                                  HSAAQHUNFJQD = new DateTime(16012),
                                                                  HCKBREPLWVGT = new
                                                                  {
                                                                      COCRLBZNUSKRH = new long[] { 25398, 18440, 5124, 16409 },
                                                                      VVSSMHSZQAZLZ = new decimal(0.239939377289237f),
                                                                      FTXFFLNYPEIGO = 16104,
                                                                      UCYXPACNHVWDA = new Guid[] { new Guid("c572c744-9be8-ae55-a5ff-f5663696df37"), new Guid("7c616885-fa34-fb81-8380-46d4df1ce0ff") },
                                                                      NOXWEXKJZGHXD = new
                                                                      {
                                                                          ODBDHYGWMBGVDG = new IntPtr[] { new IntPtr(24878), new IntPtr(3359), new IntPtr(9656), new IntPtr(29215) },
                                                                          IMNNQUISNVJSZQ = new double[] { 0.333982671300873f, 0.709567602588594f }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("RUAPYUJPXJ", evt.EventName);
                }));
                tests.Add(new SubTest("UKAUNDFLFU",
                       delegate ()
                          {
                              logger.Write("UKAUNDFLFU",
                  new
                  {
                      typeToCheck = 27946,
                      R = new TimeSpan(1057),
                      U = new int[] { 22906, 20859, 28666 },
                      C = 189,
                      P = new float[] { 0.519811f, 0.5041391f, 0.7190451f },
                      K = new
                      {
                          MZ = 60259,
                          QH = new ulong[] { 13, 24 },
                          OK = 202,
                          PN = new IntPtr[] { new IntPtr(9374), new IntPtr(10473) },
                          ZV = new UIntPtr[] { new UIntPtr(155), new UIntPtr(14), new UIntPtr(175), new UIntPtr(107) },
                          GO = new
                          {
                              HIN = 0.559999000076204f,
                              XWB = 0.955704208442804f,
                              FEN = 199,
                              CHL = new decimal(0.588603359921185f),
                              YCE = new
                              {
                                  HPDR = 30,
                                  IFKN = new char[] { 'P' },
                                  HNLV = 832,
                                  WCKI = new decimal(0.889976364509191f),
                                  NTCQ = new
                                  {
                                      JDJFI = new int[] { 26731, 12314, 11103, 1921 },
                                      GGNCO = new Guid("ffc16904-8ee6-e9fd-ffaf-5ad72376f7f8"),
                                      ZFMUU = new TimeSpan(12330),
                                      TOGCW = new
                                      {
                                          HHLCUP = new DateTime(24514),
                                          EBQFJU = 50222,
                                          VUNLNW = new TimeSpan(13260),
                                          YIRJJU = new
                                          {
                                              ZFEWJUE = false,
                                              DNVEWPL = new uint[] { 111, 69 },
                                              ITLPLTK = "RCOIKQOSCESBNRQKZIXU",
                                              WHANWJC = new
                                              {
                                                  KAMKVGBI = new float[] { 0.7307441f, 0.7706692f, 0.3229384f, 0.393829f },
                                                  BVEIWMXB = new
                                                  {
                                                      AOQTDZIDW = new decimal(0.7615850319907f),
                                                      QLBLOYTPC = 24143,
                                                      YCTIQWNKN = new
                                                      {
                                                          PVIOUHJLMC = new int[] { 2254 },
                                                          SJQUTUYSHY = new IntPtr[] { new IntPtr(11222), new IntPtr(14872), new IntPtr(26227), new IntPtr(16896) },
                                                          RQJQSHJTIA = 0.8642319f,
                                                          NQHPIDAMYE = new
                                                          {
                                                              KJSWDTMUHXZ = new ushort[] { 38561, 39081, 38208, 52456 },
                                                              ZUUVVKLRONG = new short[] { 29050, 7863 },
                                                              IKEXWNMGZYV = new ushort[] { 56953, 58852, 25618 },
                                                              HOTXDKMRPNI = new uint[] { 63, 46 },
                                                              OTSVEPAHEKC = new TimeSpan(31916),
                                                              PRJAVZRGCXY = new
                                                              {
                                                                  RKWPVTXVSFYS = new int[] { 12618, 22709 },
                                                                  FDIDEQXAVVHP = 16,
                                                                  NLLWUDTTKLLM = new char[] { 'J', 'N', 'A' }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("UKAUNDFLFU", evt.EventName);
                }));
                tests.Add(new SubTest("DVPCSAUOFX",
                       delegate ()
                          {
                              logger.Write("DVPCSAUOFX",
                  new
                  {
                      typeToCheck = 105,
                      U = new byte[] { 121, 90, 148, 115 },
                      Q = new bool[] { false, false, false, false, false },
                      O = 217,
                      D = 28094,
                      E = false,
                      C = new
                      {
                          KR = new byte[] { 184 },
                          BO = new IntPtr(10916),
                          WS = new
                          {
                              POZ = new char[] { 'A', 'I', 'U' },
                              EHV = 0.05825089f,
                              EYD = new bool[] { false, false, false, false },
                              YWY = new int[] { 21685 },
                              JKT = new
                              {
                                  GFRP = 180,
                                  LEEZ = new int[] { 31269, 26380 },
                                  WJKA = new long[] { 12806, 12324 },
                                  HQMV = new UIntPtr(197),
                                  PQBG = new
                                  {
                                      SPFDG = new byte[] { 106 },
                                      PXIFI = new bool[] { false },
                                      EWWBN = 25,
                                      XXDCU = new
                                      {
                                          KTLUWO = "VYGJPCEONUMXWHOSEIRE",
                                          TBJWIQ = new Guid[] { new Guid("3c79a885-fd24-416c-9679-3dbf7cfbd77a"), new Guid("c664d6ab-200a-7746-da79-e02f2bd0734f"), new Guid("0b43812b-5310-bb6a-ce54-c0faf4faaf08") },
                                          DBIMBT = new double[] { 0.813862412615615f, 0.16768836517245f, 0.894481414414235f },
                                          ONSOSI = new TimeSpan(30187),
                                          GQADDL = new
                                          {
                                              RDOFLUM = new uint[] { 226, 42 },
                                              TPUBUHI = new bool[] { false },
                                              AIAMXVL = new
                                              {
                                                  BEQRPHFR = new uint[] { 1 },
                                                  VINGJIUV = new ushort[] { 3688, 44055, 13365 },
                                                  YKYIZZCD = 33,
                                                  RKZTIONH = "VECPUBEVZYXDOSQHGLKC",
                                                  CRPKMSQT = new
                                                  {
                                                      ZRRCRXDUS = 30515,
                                                      COSOFLITY = 0.437890699802847f,
                                                      NJTLGVNVH = new float[] { 0.4743058f, 0.6826588f },
                                                      DZZPSCQNW = new int[] { 14027, 16626, 25968 },
                                                      DHOYAFAJX = 0.1878178f,
                                                      WESXMWZFN = new
                                                      {
                                                          IZTKSOQTGD = new UIntPtr(231),
                                                          VGMWIUHVXL = 37,
                                                          DUWUQUORRJ = new int[] { 24838, 15385, 22103, 5383 },
                                                          EFMPHDUKHF = new IntPtr[] { new IntPtr(27741), new IntPtr(25949), new IntPtr(11463) },
                                                          LPNEXSLNFL = 31829,
                                                          TYPIVGZMCY = 25701,
                                                          ISNTRDRYLK = new
                                                          {
                                                              JAGJTYXQISQ = new DateTime(30107),
                                                              NLOAFXKMXLW = new uint[] { 186, 81, 27 },
                                                              RWCUQIIPPCL = new
                                                              {
                                                                  MAOLQZMQWFFG = 24,
                                                                  OZWCVNPUGUFY = new TimeSpan(2485),
                                                                  NSYJCNEIYWYZ = new ulong[] { 182, 252 },
                                                                  ZLPNCXTITMGY = new
                                                                  {
                                                                      XGIRKAMGJMHWM = 110,
                                                                      BZEVGGVUESVCP = new int[] { 24112, 31732, 1297, 21916 },
                                                                      LCEPYAHLXNAZM = new int[] { 15473, 22430, 7434 },
                                                                      BYJNHTIKRRKNN = 7361,
                                                                      KQFNWUIOPFUOH = new
                                                                      {
                                                                          IGHJQLRXSBWFDR = new Guid("33a7ce97-05af-e92e-d466-73fdf5e33962"),
                                                                          NTTSCJUYHYSKTI = 150
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("DVPCSAUOFX", evt.EventName);
                }));
                tests.Add(new SubTest("GZCDGVUUZQ",
                       delegate ()
                          {
                              logger.Write("GZCDGVUUZQ",
                  new
                  {
                      typeToCheck = 9733,
                      V = new Guid[] { new Guid("e033a0b8-89c9-cc4f-0192-de7ca4b5183a"), new Guid("bece8a4d-2ec1-51c7-0c4e-53bc4e092dc7"), new Guid("217983d2-b7a6-0f4f-e1f1-68f582536d0f"), new Guid("05defa39-7719-007d-7845-43d44edd09dd") },
                      C = new
                      {
                          QY = 51,
                          PR = new bool[] { false },
                          YJ = 23037,
                          YA = new UIntPtr(181),
                          LB = new
                          {
                              HVX = new UIntPtr[] { new UIntPtr(104) },
                              URQ = new ushort[] { 44450, 65128 },
                              UKH = new byte[] { 177, 177, 117 },
                              BKR = new IntPtr(24548),
                              JUB = 61483,
                              OCA = new
                              {
                                  OKUI = 0.723938481288002f,
                                  TPVZ = 45,
                                  AWCE = new Guid("34678f43-f677-5904-534f-57819b535b88"),
                                  PLYN = new
                                  {
                                      JOEYB = 'H',
                                      ZSRBS = 18950,
                                      ANUTM = 57036,
                                      JYIXZ = "ZZLKKPBPIZTYFEPVDZGV",
                                      GNPQW = new
                                      {
                                          KTEVFY = new Guid[] { new Guid("d9575c4f-4b11-4726-780f-323d0d625777"), new Guid("875231e5-e82c-cb4c-0f52-1d8ce5219502"), new Guid("1f9b4034-e700-179c-40fe-6465bc5073a8") },
                                          VHZEMR = new
                                          {
                                              YUVRLYR = new ulong[] { 252, 7, 240 },
                                              EVHISFB = new Guid[] { new Guid("9c0aa810-4313-0aff-c630-6aea14a84d67"), new Guid("ca3c19cd-3131-13fe-c5b3-56e32cbdb1ed") },
                                              FSVRPVR = new int[] { 10121, 27905 },
                                              OQISNUD = false,
                                              YEXTPYF = new bool[] { false, false }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("GZCDGVUUZQ", evt.EventName);
                }));
                tests.Add(new SubTest("PEYHZTCVQQ",
                       delegate ()
                          {
                              logger.Write("PEYHZTCVQQ",
                  new
                  {
                      typeToCheck = 216,
                      Y = new byte[] { 96, 147, 99, 27, 169 },
                      R = new IntPtr(27613),
                      S = 555,
                      F = new IntPtr(31672),
                      O = new UIntPtr[] { new UIntPtr(122), new UIntPtr(145), new UIntPtr(214) },
                      B = new
                      {
                          ZG = 0.9405469f,
                          NK = new UIntPtr[] { new UIntPtr(50), new UIntPtr(177) },
                          LX = new sbyte[] { 4, 105, 73, 44 },
                          RN = new
                          {
                              FJQ = new UIntPtr(99),
                              REQ = "MWQYNVZRSISOGRULTEBQ",
                              LWP = new ushort[] { 42063, 17282, 51741 },
                              BZF = new
                              {
                                  NNED = 0,
                                  HHVT = 6931,
                                  SSRG = new IntPtr(18503),
                                  FCIB = new
                                  {
                                      DJPBN = "CPJJCNDDMXHWPQGTQXDZ",
                                      WCCEA = new byte[] { 131, 73 },
                                      COQQJ = new float[] { 0.8700023f, 0.009697844f },
                                      VHSEX = new IntPtr(8656),
                                      TUCFL = new
                                      {
                                          ATYMTM = 16177,
                                          NDOSQZ = new DateTime(16454),
                                          OMYSHO = new double[] { 0.770742088915194f, 0.754024858471949f },
                                          IXOUCI = new bool[] { false, false, false },
                                          PANDCW = new int[] { 20424, 3478, 17798, 20510 },
                                          RYHDEM = new
                                          {
                                              HXOZFQG = 238,
                                              QDLOGXV = new uint[] { 50, 186 },
                                              PCPMRRR = new
                                              {
                                                  HKBJHEEW = 31409,
                                                  DDJRWHHN = new Guid("e113b463-4e45-1c6b-4f10-98c3ce077c23"),
                                                  QHUDBAOJ = 19261,
                                                  WINJWRJY = "YUZNOCPCWNBLILKZCKRZ",
                                                  OXQRSBUY = new ulong[] { 245, 89 },
                                                  EESGRDHF = new
                                                  {
                                                      FEPMYFJUP = 0.3790679f,
                                                      XJBAWSRCL = new TimeSpan(15366),
                                                      QDKHRPHRT = new UIntPtr[] { new UIntPtr(125), new UIntPtr(4), new UIntPtr(152) },
                                                      THIBYLHXL = new
                                                      {
                                                          TCAJUDSJYL = 0.655032592199292f,
                                                          YZJOXSFIJC = new DateTime(30596),
                                                          QTRVTGVOOM = 82,
                                                          AMOWYSHFJN = new
                                                          {
                                                              JJSJNPRDKAE = new ushort[] { 37585, 27963, 41751, 14954 },
                                                              BOFFIGRLLHO = new Guid[] { new Guid("a9b7e0c7-b854-4c9f-cbc9-a7776b79141e"), new Guid("d30fc9b3-8a62-8288-1193-09e20f8b87be"), new Guid("d7372ddb-b090-da41-8bfc-55a446abfe11") },
                                                              IKQVPKCKHCJ = new
                                                              {
                                                                  SFWKCNXKJFVK = new byte[] { 6, 160 },
                                                                  DWXXLVCLISUF = 229,
                                                                  AZULVMDJGJYJ = new int[] { 17307, 31889 },
                                                                  YNACTTGGMJRO = new
                                                                  {
                                                                      QUNSIHIEIGBYS = 'A',
                                                                      QOZBRANVSRLVZ = 0.009747075f,
                                                                      KTGWJVOAQAZTS = "QYGRDWVFJFOFBTTYFDLZ",
                                                                      JEZNPXJPWPGSZ = new
                                                                      {
                                                                          QWNFNTQFLRKYCN = new byte[] { 66, 70, 156 },
                                                                          HUJQIHWMFPADKH = new short[] { 12018, 3191, 4937 },
                                                                          OKUNUGZVWYMPON = new
                                                                          {
                                                                              JAOEIMJJIOQTIEQ = new byte[] { 95, 117, 7, 145 },
                                                                              IVNDPNYAVNUKUHX = 135,
                                                                              AOGZQTECFWBYUET = new
                                                                              {
                                                                                  WCGTZGLAFHQVPYUJ = 29675,
                                                                                  QNULRBJBJRXCAIAN = 42,
                                                                                  DRZOOARAYPDKNFHV = new
                                                                                  {
                                                                                      OGCRPSQEBJIFPMOLD = 147,
                                                                                      ZBZTTRDTXZERUFUUK = new bool[] { false, false },
                                                                                      KBUNERMKLBSGDMIWN = new char[] { 'W', 'E', 'Q' },
                                                                                      YXPXHBFRKKSUADYIX = new
                                                                                      {
                                                                                          EOUCGGRRESAFMIHMBB = 0.5015839f,
                                                                                          NLZACATGRDFEKSYMGR = 21352,
                                                                                          FCJELYBLHYBKNUQCUB = new ushort[] { 18880, 54860, 6026, 37501 }
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("PEYHZTCVQQ", evt.EventName);
                }));
                tests.Add(new SubTest("MZWAAJERJE",
                       delegate ()
                          {
                              logger.Write("MZWAAJERJE",
                  new
                  {
                      typeToCheck = 'M',
                      P = new double[] { 0.460442545106841f },
                      D = 'E',
                      A = new uint[] { 67, 36, 78 },
                      W = new char[] { 'I', 'G' },
                      H = new UIntPtr(117),
                      R = new
                      {
                          RP = new DateTime(32217),
                          KU = 20,
                          WL = new TimeSpan(18084),
                          RZ = 147,
                          QE = new float[] { 0.6445301f, 0.9593939f, 0.6185194f },
                          JU = new ushort[] { 56151, 47367 },
                          DY = new
                          {
                              KKM = 32,
                              LNK = 30745,
                              BLN = new short[] { 26524, 28314 },
                              XBA = new Guid[] { new Guid("d07beea2-ff9f-f7df-a9ee-dce1b1bf28be") },
                              XPD = 163,
                              XYH = new DateTime(6908),
                              IKM = new IntPtr[] { new IntPtr(6356), new IntPtr(7280), new IntPtr(10740) },
                              CIS = new
                              {
                                  IIDO = new short[] { 16042 },
                                  SVPV = new decimal(0.902215693566117f),
                                  MSEM = new
                                  {
                                      VJWZU = new long[] { 30322, 19992, 8810, 4326 },
                                      WIQZD = new Guid("e79fc3c7-eb57-f50b-eea5-7b2b28444957"),
                                      OISCP = new IntPtr[] { new IntPtr(28509), new IntPtr(12842) },
                                      ACABI = false,
                                      RVSIX = new char[] { 'W', 'G', 'P' },
                                      MTOVX = new Guid("3ebc7cfc-f41e-50e3-4361-a67a334e99d7"),
                                      UYTRR = new
                                      {
                                          JITUPO = new float[] { 0.5914341f },
                                          UYSDTQ = new int[] { 4040, 67, 17391 },
                                          XQGRSC = 15346,
                                          HRKHFD = new
                                          {
                                              HKRDQXB = 'L',
                                              YKRUVRE = new float[] { 0.9551666f, 0.6696661f },
                                              IBCLZQF = new
                                              {
                                                  EPIXSADR = new double[] { 0.63716795930507f, 0.488180534675801f, 0.0381078198729585f, 0.999525385908561f, 0.206607579815484f },
                                                  QZGKUXMS = new double[] { 0.60333141712627f },
                                                  UIPNBHXC = new uint[] { 14, 51, 194 },
                                                  RMLTLUWB = new UIntPtr(36),
                                                  ADNFNIUX = new
                                                  {
                                                      GEYHIUKHS = 229,
                                                      FOMJUDONY = new bool[] { false, false }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("MZWAAJERJE", evt.EventName);
                }));
                tests.Add(new SubTest("RBLWOSFNJM",
                       delegate ()
                          {
                              logger.Write("RBLWOSFNJM",
                  new
                  {
                      typeToCheck = 0.646068023352915f,
                      M = new decimal(0.853183537187606f),
                      D = new
                      {
                          ML = new UIntPtr(28),
                          YY = new int[] { 19574, 14552, 23808, 7767 },
                          VW = new sbyte[] { 86, 3 },
                          KI = new
                          {
                              ZUB = new sbyte[] { 39, 53, 15, 77 },
                              WIH = new decimal(0.425560978905093f),
                              CAB = new
                              {
                                  VVXP = 3044,
                                  IIZF = new TimeSpan(13673),
                                  PAPB = new ushort[] { 51465, 54938 },
                                  JUSE = new
                                  {
                                      KASQC = new byte[] { 186 },
                                      VXCOA = new TimeSpan(12884),
                                      BUMRM = new short[] { 29994 },
                                      CRGIS = new
                                      {
                                          VWZRGD = false,
                                          JASFGB = new IntPtr[] { new IntPtr(4063), new IntPtr(25611), new IntPtr(17961) },
                                          QAAQPC = new
                                          {
                                              IXDGBDX = new IntPtr[] { new IntPtr(17133) },
                                              LGAWGWL = new IntPtr[] { new IntPtr(11687) },
                                              WRUTLFA = new decimal(0.262238032772317f),
                                              AGICGUQ = new
                                              {
                                                  UCWPJSIQ = 0.755853892190314f,
                                                  BNVUOQDS = new
                                                  {
                                                      LODRABHXA = false,
                                                      ZGFMTOUDE = new float[] { 0.5840299f },
                                                      VREZJEFRR = new
                                                      {
                                                          YNJCQKWNCU = 38,
                                                          LAUIJBZWZO = new short[] { 29064, 21536 },
                                                          IZVBXSVMVK = "ZXVDHVZFOJQPCIBWOIDL",
                                                          CVISGBJAQJ = new ulong[] { 245, 64, 120, 139, 14 },
                                                          EBVERVYZGR = new
                                                          {
                                                              IKAMECMBQBO = new uint[] { 166, 158 },
                                                              SHPYEBUYTRX = new decimal(0.0225669848837736f),
                                                              QVALMQKMXSU = new IntPtr(13011),
                                                              OOSFPGWHMKF = new
                                                              {
                                                                  AVEXKRIOIFVG = 20524,
                                                                  HUBUOVBXNFFO = new
                                                                  {
                                                                      CQAPEKUKRLNCG = new uint[] { 23 },
                                                                      BSICPMYBFRVJZ = new IntPtr(27758),
                                                                      REDFYHQQVVELW = new char[] { 'I', 'M' },
                                                                      ODQMWVXDYQRYJ = 11,
                                                                      LLTUWXCFGHKBN = new ulong[] { 104, 125, 200 },
                                                                      ACUGDSTNAVUBF = new
                                                                      {
                                                                          OGIGDGKTSBLBLY = new uint[] { 231 },
                                                                          TKXNPPECKWRYSZ = new sbyte[] { 20, 43 },
                                                                          XQAYEYGVRIAJFR = 0.946589092233493f,
                                                                          IPCKDRZQEDSJSU = new TimeSpan(18649),
                                                                          JEOHIGHUDBVHPU = new
                                                                          {
                                                                              XSCHQDXRSFRJUAF = new short[] { 21356 },
                                                                              IXAADMBCIHHCWRA = new IntPtr[] { new IntPtr(9871), new IntPtr(15047), new IntPtr(6611), new IntPtr(5979), new IntPtr(9756) },
                                                                              ZHLDWFRMIQYMGPF = new Guid("34a20609-2db9-a506-93c4-b9a16555a96a"),
                                                                              YSUXPQGOVPKQIZI = new UIntPtr[] { new UIntPtr(178) },
                                                                              GQRTMRIOUWCPBSB = new ushort[] { 4216, 51572 },
                                                                              TIPJKQFPBQHVZQP = new sbyte[] { 123, 66 },
                                                                              JPHDLBLIVTDYTXJ = new
                                                                              {
                                                                                  MJGAVKVZMELBBSUF = 34,
                                                                                  JLWUHHDAOXBRHSXI = 27930,
                                                                                  PGCNQUZAZKQAORKE = 19063,
                                                                                  QOYTRJHCLMDJDJEV = new IntPtr(5135)
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("RBLWOSFNJM", evt.EventName);
                }));
                tests.Add(new SubTest("QASSUFJCNV",
                       delegate ()
                          {
                              logger.Write("QASSUFJCNV",
                  new
                  {
                      typeToCheck = 0.1526777f,
                      Z = new IntPtr(27130),
                      E = new DateTime(28804),
                      F = 25462,
                      J = new
                      {
                          GM = 100,
                          AX = new bool[] { false, false }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("QASSUFJCNV", evt.EventName);
                }));
                tests.Add(new SubTest("EFVFCVXZGO",
                       delegate ()
                          {
                              logger.Write("EFVFCVXZGO",
                  new
                  {
                      typeToCheck = new DateTime(31997),
                      F = new TimeSpan(21567),
                      Y = new
                      {
                          WT = new byte[] { 88, 238, 253, 105 },
                          VN = 30604,
                          RI = new double[] { 0.969028484061839f },
                          IR = new
                          {
                              SDS = new int[] { 3461, 30290 },
                              WHY = new IntPtr[] { new IntPtr(29269), new IntPtr(28772) },
                              BNG = new
                              {
                                  QPEA = new Guid("26711abf-2742-fbc5-c132-5597287eac60"),
                                  SVXY = new uint[] { 74 },
                                  PTLC = new bool[] { false, false, false },
                                  XOBH = 161,
                                  RAMN = 201,
                                  UALY = new
                                  {
                                      CEHMK = new TimeSpan(3043),
                                      UJYOG = 178,
                                      HABQR = 3,
                                      MCZYD = new
                                      {
                                          FEKEFG = 41,
                                          VOPVPT = false,
                                          YDTYAJ = false,
                                          JUJUSE = false,
                                          YTDKET = new double[] { 0.739441244741642f, 0.999074191785918f, 0.921797857583406f, 0.444924431594519f },
                                          WFMMMV = new sbyte[] { 73, 60, 8 },
                                          VDDVEE = new int[] { 20337, 6873 },
                                          XLVMIW = 31435
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EFVFCVXZGO", evt.EventName);
                }));
                tests.Add(new SubTest("NNYXRKOFDN",
                       delegate ()
                          {
                              logger.Write("NNYXRKOFDN",
                  new
                  {
                      typeToCheck = new decimal(0.771234201160834f),
                      D = new uint[] { 105, 202, 122 },
                      G = new decimal(0.160076419897413f),
                      Z = new ushort[] { 4093, 1900, 31931, 33041, 64090 },
                      P = new
                      {
                          CZ = new byte[] { 96, 125, 84 },
                          ZT = 0.488393806148504f,
                          VX = "UBWEMCXTCXORXLGBUDAP",
                          EB = new double[] { 0.845220311472761f, 0.377726074484049f },
                          XD = new decimal(0.230511360443435f),
                          RF = new char[] { 'O', 'A' },
                          KC = new
                          {
                              IQN = new float[] { 0.2724214f },
                              MHS = 8296,
                              NRL = false,
                              XGA = new
                              {
                                  VOFO = new sbyte[] { 31, 26 },
                                  EYLU = new char[] { 'K', 'V', 'T', 'Q' },
                                  EWLD = new float[] { 0.1778682f },
                                  NIXW = new float[] { 0.1150481f },
                                  HKMK = new int[] { 4267, 20585, 29627 },
                                  IANP = 22,
                                  CDKJ = new
                                  {
                                      ZNWYK = new double[] { 0.679579930696441f, 0.00625421898730761f, 0.808734144926413f, 0.392857339416098f },
                                      ZJTOT = 20818,
                                      RWXCR = false,
                                      UWMGA = 29390,
                                      JAFYE = new
                                      {
                                          IJAKFF = 183,
                                          UVTHVR = new decimal(0.554945690815777f),
                                          EATXGT = new
                                          {
                                              ZLDBSLQ = 67,
                                              PJORMSW = 27102,
                                              IZBSZFY = "TWWHROELRIFASYRVJTZO",
                                              VFEGCQA = new
                                              {
                                                  YCEJGULH = 214,
                                                  DETMDEDX = 0.455708133268965f,
                                                  TEWUZMNI = "LZQCZPMEKLYQOSGLDOAG",
                                                  HXRNQSFN = new
                                                  {
                                                      ONTQGTVHK = new sbyte[] { 66, 67, 24 },
                                                      DEWBFXHOG = new
                                                      {
                                                          HYZRKEFNGW = new TimeSpan(13325),
                                                          JGEDXCZSCE = new ulong[] { 138, 189 },
                                                          ECBPHSKQGH = 54959,
                                                          YWPPUEMRAN = new DateTime(24189),
                                                          XCAFOOFUSF = new uint[] { 189, 68 },
                                                          DXYEAIJBQK = new
                                                          {
                                                              EANJYXRPKRH = 7290,
                                                              RFBSBVBSRNM = new decimal(0.813681333704703f),
                                                              IRSETZNYZRF = new
                                                              {
                                                                  PHCDVOURFUMU = 12121,
                                                                  VILJAXCRJXSM = new
                                                                  {
                                                                      NMEGROWCTSYXD = 0.0571230114703639f,
                                                                      LVWLOTDFMQQHN = 58892,
                                                                      FDBGFAOUGKOGD = new UIntPtr[] { new UIntPtr(78), new UIntPtr(201), new UIntPtr(68), new UIntPtr(140) },
                                                                      FHDLGNBHYUIZT = new byte[] { 47, 236 },
                                                                      PODYSUBMOPXBQ = new byte[] { 195, 19, 239, 43 },
                                                                      MAUYKJECJGEWD = false,
                                                                      AUTJVZCERUEVO = new DateTime(5962),
                                                                      MLGYHHEUQNKMU = new
                                                                      {
                                                                          SOXXZROCHGFZKW = new uint[] { 183, 41, 20 },
                                                                          OPGAUSQEEPWADV = new UIntPtr[] { new UIntPtr(226), new UIntPtr(184), new UIntPtr(203), new UIntPtr(129) },
                                                                          IMDWGVXHGEEAYX = "PPAYHAGGTMQYPCEZDJFG",
                                                                          PKZVFTXVHTVCSC = new
                                                                          {
                                                                              ELVMCJZIZTSZUPO = new long[] { 16798, 22749, 18457 },
                                                                              DULTWPIOOIONAUO = new int[] { 16044, 1683, 23136, 28852 }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("NNYXRKOFDN", evt.EventName);
                }));
                tests.Add(new SubTest("CFJVLQUDLD",
                       delegate ()
                          {
                              logger.Write("CFJVLQUDLD",
                  new
                  {
                      typeToCheck = new IntPtr(32103),
                      I = new char[] { 'G', 'T' },
                      Q = 0.5608475f,
                      T = new short[] { 22323, 25076, 6683 },
                      P = new
                      {
                          CS = new ushort[] { 35499, 18830 },
                          IY = new
                          {
                              QFW = 151,
                              JYY = 172,
                              NQL = 230,
                              EBX = new
                              {
                                  HQUC = new long[] { 12976, 16642 },
                                  XQNN = new
                                  {
                                      MQKRZ = 39,
                                      DEARW = new
                                      {
                                          YCOQDL = new float[] { 0.8138473f, 0.7257694f, 0.8797836f, 0.3725139f },
                                          EAHWCB = new
                                          {
                                              COFSLKB = new ushort[] { 22312, 44089 },
                                              LZPDXTG = new TimeSpan(28973),
                                              DSRLUFC = new uint[] { 83, 99 },
                                              MJJDOQY = 29765,
                                              IFLXBLR = 146,
                                              VFEKTIY = new
                                              {
                                                  ZILDKNJF = 4171,
                                                  OIKYFIUY = 'M',
                                                  GRFUCSHZ = new uint[] { 66, 128, 70, 8 },
                                                  QBFPHNMO = new
                                                  {
                                                      UWFGMSDJC = new TimeSpan(15460),
                                                      PUPLSRFJH = 0.253784295755338f,
                                                      GLNDNCUQS = new
                                                      {
                                                          WGEWGAENQT = new Guid[] { new Guid("11bb0bb8-356f-e16a-3763-e18278502059"), new Guid("255a75cc-b32d-9ac1-8853-ee3eebf33d2f"), new Guid("7e31286e-3427-f3c1-7093-21d3bf1be3c1"), new Guid("08de931e-d54f-17ac-f3f9-77ed454919e0") },
                                                          IMVMTWQHCG = new TimeSpan(16563),
                                                          ZUZOQGHFHA = new float[] { 0.2412148f, 0.5073174f, 0.1115995f },
                                                          SFSMSSODQG = new ushort[] { 58532, 57513, 26707, 14931, 32433 },
                                                          IAJXALTAEB = new
                                                          {
                                                              GFFYANJFSHC = 0.424585855763678f,
                                                              DDOKSWDTQYG = 13825,
                                                              KFVHIMQYNGU = 15,
                                                              ULNVYDGUTWW = new
                                                              {
                                                                  IIIXNPHRLQZA = new bool[] { false, false, false, false },
                                                                  SDQQWUTRBSGV = new TimeSpan(30615)
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("CFJVLQUDLD", evt.EventName);
                }));
                tests.Add(new SubTest("EZLBGFNUSP",
                       delegate ()
                          {
                              logger.Write("EZLBGFNUSP",
                  new
                  {
                      typeToCheck = new UIntPtr(54),
                      Q = false,
                      X = 47,
                      L = new bool[] { false, false, false, false },
                      D = new sbyte[] { 1, 38, 42 },
                      N = new
                      {
                          CD = 180,
                          UH = new uint[] { 84, 87, 235, 153, 10 },
                          BI = new
                          {
                              DJF = new char[] { 'N', 'C', 'F', 'F' },
                              GKM = new byte[] { 126, 142 },
                              MNR = new short[] { 21547, 9297, 2433 },
                              RSK = 6538,
                              EYY = new long[] { 28081, 23219, 30386, 31162 },
                              PID = 204,
                              LLF = new
                              {
                                  UHBP = new byte[] { 166 },
                                  RPMI = 10,
                                  BUZP = false,
                                  MSYL = new ulong[] { 217 },
                                  SVYH = 0.8612404f,
                                  IBHX = 0.5478505f,
                                  BZIR = new Guid("4ca25ff1-35f8-4260-584e-b85907f707d0"),
                                  LPOF = new
                                  {
                                      ONEBY = new Guid("eb5933b2-7ff6-515b-24ea-099f50711c69"),
                                      VVUYA = new long[] { 22593, 1182, 26461 },
                                      KVWZQ = new byte[] { 200, 32 },
                                      ZEONH = new
                                      {
                                          UTJZQC = new double[] { 0.866660404888289f, 0.321525201351161f },
                                          CTQNBU = new sbyte[] { 40, 5, 69, 29, 104 },
                                          ADYDSO = 0.408148484028945f,
                                          PXEJOZ = 63,
                                          OETMWD = new DateTime(26460),
                                          XOHBFM = new
                                          {
                                              OGCAELO = new sbyte[] { 38, 79 },
                                              PKAEIXW = new ushort[] { 9767, 27932 },
                                              FYXAPDW = new char[] { 'T', 'S', 'I', 'A' },
                                              FEHSJDJ = new UIntPtr(48),
                                              NVCXKCP = new
                                              {
                                                  WLCRFZSE = new byte[] { 12, 101 },
                                                  RFMXHEDY = new IntPtr(18221),
                                                  XBXVJQIH = new ushort[] { 36867 },
                                                  OULTRTPF = new
                                                  {
                                                      FMDVIPRSU = 245,
                                                      ULFRYKFVV = new IntPtr[] { new IntPtr(10728), new IntPtr(2822), new IntPtr(6918) },
                                                      WSTZWTTFO = 17,
                                                      TLUBTIDRV = 13,
                                                      FNCPTUHOU = new IntPtr[] { new IntPtr(31659), new IntPtr(5696) },
                                                      CGDPPVYBK = new TimeSpan(17201),
                                                      WSTXXRWRE = new
                                                      {
                                                          NQCNGXHZYX = 0.0678912652972579f,
                                                          XINYDTISUH = 0.242275961787568f,
                                                          QJVWXQRCEI = "APVJWAHHZTGEHPKZZFVL",
                                                          QISNZCMAPI = new
                                                          {
                                                              VKKZNDJMTJH = 8668,
                                                              LDBHHRJPXIH = 0.01723471f,
                                                              USSUUGIOKLY = new
                                                              {
                                                                  DOTSPCWZTFCV = new long[] { 4208 },
                                                                  YYGNBRSDOTAO = 101,
                                                                  GYBRDDTXRAUM = new double[] { 0.083259023764757f, 0.19944675927956f },
                                                                  VSXRPBHJIWKG = new
                                                                  {
                                                                      LLEYONIOMGBEX = new TimeSpan(24364),
                                                                      HFTPJQHWWJZMD = new
                                                                      {
                                                                          WPEHWSHSSLGDUO = 149,
                                                                          UNCGCCDUXUDIBE = 14222,
                                                                          KPQWWAJRHOTMMP = new sbyte[] { 48, 90, 102, 48 },
                                                                          FDPLKWUNBMZXEK = 42,
                                                                          RGJZXUWTDKZOIQ = new
                                                                          {
                                                                              IISJDYPCMOVDVKA = 64,
                                                                              XMUFFCWKHVIZXFE = new UIntPtr[] { new UIntPtr(125), new UIntPtr(225) },
                                                                              REUHSRPTVDTZMTR = new
                                                                              {
                                                                                  WBQVWCCQNSQUUMQP = new UIntPtr[] { new UIntPtr(22), new UIntPtr(134), new UIntPtr(120) },
                                                                                  JHTQEUFLJZCKVCQD = new DateTime(19671),
                                                                                  JOBOOGOBXUHFNVPG = new IntPtr[] { new IntPtr(4926) },
                                                                                  CKMAQQTRKIVRDJUB = 34900,
                                                                                  EUTMCCXLEELHVPSH = new bool[] { false, false, false },
                                                                                  CIYKVXVFGGQYKMFS = new
                                                                                  {
                                                                                      CPPPIJDFMOFDMGYWI = new Guid("4c7d96dc-83ea-3c3a-5134-48c7cbd5b9ee"),
                                                                                      XTZWRWWRUGUYBCMDR = new Guid[] { new Guid("b0a50b66-6c75-56e2-4c2c-3c5b530d6eb4"), new Guid("9b7c5fbd-2681-cb0f-21b6-a03a0cfd549f") },
                                                                                      TETYGRSTZHUPAOYVJ = new
                                                                                      {
                                                                                          QTYLMMKFUGBJTVFXHF = new uint[] { 174 },
                                                                                          SEYDWJARTKDHDNXMQG = 9460,
                                                                                          TMLQJGLORTIVQPXOZI = 23534,
                                                                                          JOBVXCQHHLQZKWAFTI = new
                                                                                          {
                                                                                              TQXLZZCIROPIMIPPYFL = 'Y',
                                                                                              QEHGCJKUORVROAEYPBT = 28711,
                                                                                              BRYZIJVXVUDOMATDSXS = new Guid("12215cba-ee5f-d933-ca8a-7fa0a94a2cb6"),
                                                                                              AYNUTPKAPKWEWOMIGUZ = 18,
                                                                                              SWRDCALUWEAQNBTLFOU = new Guid[] { new Guid("c17cd903-de86-4f57-ad71-1136d667597f"), new Guid("3fc5210e-b80a-5e44-94db-76dbefd5d140"), new Guid("10d851a4-89db-181d-6e6a-d2e3d236ea73"), new Guid("c4cec4ee-287c-7c49-d9d1-813c6595b0d6"), new Guid("3b45fed4-f221-da49-8bb0-083ff17ca4eb") },
                                                                                              FKHKQLHULCTKAHBWRRO = new Guid("d4dfb4f2-c6b0-b05f-4b96-32043d32fc5b")
                                                                                          }
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("EZLBGFNUSP", evt.EventName);
                }));
                tests.Add(new SubTest("GQJIGKFUPF",
                       delegate ()
                          {
                              logger.Write("GQJIGKFUPF",
                  new
                  {
                      typeToCheck = new Guid("fdee0960-9e05-8db9-f585-cec721a01de5"),
                      A = 0.4568011f,
                      O = "KOFWUBTGQVYIJBBEWCHG",
                      C = new long[] { 18091, 4814 },
                      W = new IntPtr(5151),
                      G = 21789,
                      Z = 29203,
                      L = new
                      {
                          QP = 'K',
                          EQ = new TimeSpan(5185),
                          XM = new
                          {
                              QQJ = 25697,
                              QUB = new Guid("8bd2b783-2984-4abf-4152-d3f9b2b390d4"),
                              JQR = new
                              {
                                  RVLJ = new byte[] { 200, 111, 117 },
                                  EKSK = new short[] { 17065, 15247, 28485 },
                                  XJVS = new
                                  {
                                      CIKFG = 32764,
                                      ZJUOM = new
                                      {
                                          MGABQI = 48032,
                                          WQMGQD = new
                                          {
                                              NSSLGFB = new long[] { 16825, 353, 20192 },
                                              LJTJBRC = 27526,
                                              GECZGAQ = new short[] { 6849, 2416 }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("GQJIGKFUPF", evt.EventName);
                }));
                tests.Add(new SubTest("ADCEZUPQSA",
                       delegate ()
                          {
                              logger.Write("ADCEZUPQSA",
                  new
                  {
                      typeToCheck = new TimeSpan(11130),
                      D = 7276,
                      Y = new sbyte[] { 9, 93 },
                      R = 0.860734415641397f,
                      U = new
                      {
                          GE = new float[] { 0.5344664f, 0.8434142f },
                          YW = new IntPtr(9799),
                          GI = new char[] { 'O', 'E', 'U', 'O' },
                          CA = "OEXUCYDHKGOTIBHZEHSB",
                          TT = new
                          {
                              SYD = new IntPtr(8463),
                              FPI = new IntPtr[] { new IntPtr(28179), new IntPtr(10211) },
                              GBM = 0.467223472645145f,
                              ZVT = 52,
                              XEM = new
                              {
                                  XJHT = new byte[] { 90 },
                                  OANL = new decimal(0.520461108777887f),
                                  PGRI = new
                                  {
                                      JNNCZ = 0.992586f,
                                      NAQKC = 1,
                                      TQRNJ = false,
                                      HCUGO = new byte[] { 159, 66, 252, 71 },
                                      NKOBI = new DateTime(16146),
                                      QJXCM = new
                                      {
                                          OIBQDX = 2936,
                                          LXSHWP = new ulong[] { 40 },
                                          ITTUHA = 15262,
                                          IUSKCG = new
                                          {
                                              TLXXLGL = new float[] { 0.7762031f, 0.8219473f },
                                              KUBMVTL = new ulong[] { 173, 174 },
                                              FBEJSGH = new
                                              {
                                                  KOVCGFAE = new char[] { 'I', 'M' },
                                                  YDAZRJDJ = 224,
                                                  DGOILHXF = 1394,
                                                  FQDVXVGJ = new
                                                  {
                                                      FTWIOJNDD = new UIntPtr[] { new UIntPtr(254), new UIntPtr(94), new UIntPtr(124) },
                                                      FITIGIGTU = new UIntPtr(43),
                                                      ZOEFHZECV = new
                                                      {
                                                          DTETCURFVI = 30450,
                                                          UXWXVOOPMP = new char[] { 'O', 'L', 'W' },
                                                          WUKEFDYEPR = "ZWZJTXQYOYVHVOKNYFTE",
                                                          AFMRAYLTMF = new
                                                          {
                                                              UHIKEQNFQEX = new ulong[] { 232, 220, 184 },
                                                              GBQFGMFCLTW = new
                                                              {
                                                                  PCXPODNDRZDV = new UIntPtr(133),
                                                                  QJAHGXZLKZHG = 94,
                                                                  ZRWPNPNPQCAK = new
                                                                  {
                                                                      ZOBDGVHYVVCDN = 41326,
                                                                      MQGXADRHGGGIU = new byte[] { 133, 247 },
                                                                      YGVXKJHTLZOED = 6997,
                                                                      COAWDAVDJWXTW = new bool[] { false },
                                                                      YDUCDWRCFHFWU = new Guid("57d6a279-eae4-dde0-bb73-42853d01a4dc"),
                                                                      YORNDSIYXTCDM = new
                                                                      {
                                                                          BNQKTWALJBLWLL = 5825,
                                                                          DAEYXIBIWWPFWI = new byte[] { 120, 167 },
                                                                          EARRVDYXAPMGRS = new ulong[] { 183, 15, 118 },
                                                                          JAQBZNEEOBWZLF = false,
                                                                          KDGKDXBZCGXIWV = 60,
                                                                          PBWWSZTRWNDTHY = new
                                                                          {
                                                                              MVCTVNLWKMYQZSP = new double[] { 0.898203371976597f, 0.251778495149584f, 0.00988541404245673f },
                                                                              KSKJIQALKPNQUHS = new int[] { 4999, 22298, 16993, 10137 },
                                                                              CGCADSTRKYCZABA = new uint[] { 90, 223, 39 },
                                                                              JOOCMLTKCWIURQY = 0.9272836f,
                                                                              NPFSGZQSYSXLJOM = new Guid[] { new Guid("7897d648-4885-4b1e-f875-56fbba253e6c"), new Guid("cf530ea8-d7ae-8661-434f-6de483db4572"), new Guid("7187cc0f-5ecf-bfac-ebc8-157343ebc06d") },
                                                                              YDQRQUJPAWLXSHD = new
                                                                              {
                                                                                  EDOLASDOXKNTVZTH = new float[] { 0.9288588f, 0.06844223f, 0.02058957f },
                                                                                  SVEFWVVCFDEUGKUG = new ulong[] { 214 },
                                                                                  BKQUOSHCBNQHOQOJ = new
                                                                                  {
                                                                                      PIAXRPEQYLSPHJBTM = 'R',
                                                                                      SXHMYTUIMXPOGWUMF = new float[] { 0.2762389f, 0.3306668f, 0.3118541f, 0.6606092f },
                                                                                      LGZWDARUSMWUGWQRS = new byte[] { 228, 188, 189 },
                                                                                      ZXVWEAWDJLOPNEYHR = 149,
                                                                                      ELHYCVPCISPFCKRJK = new
                                                                                      {
                                                                                          QKVWGWHJDWJQIPZYRX = 11161,
                                                                                          TAOXOLTGLEVUORHWEX = 57091,
                                                                                          PYZGMZCQGLNPRMWJHY = 195,
                                                                                          ODRSNVUXRCMZMSJVBI = new Guid[] { new Guid("5c8d49ea-344f-bcea-5538-d37e8d35c25b"), new Guid("65879a0a-0edb-95ad-ca3e-748b227b85a5") },
                                                                                          JCZAQGDBVXPGEFKMGH = 93,
                                                                                          FEZEZNDYCAIYNEBLDR = new byte[] { 162 }
                                                                                      }
                                                                                  }
                                                                              }
                                                                          }
                                                                      }
                                                                  }
                                                              }
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("ADCEZUPQSA", evt.EventName);
                }));
                tests.Add(new SubTest("FWKFKXHDFY",
                       delegate ()
                          {
                              logger.Write("FWKFKXHDFY",
                  new
                  {
                      typeToCheck = new DateTimeOffset(30147, TimeSpan.FromHours(-8)),
                      A = new char[] { 'X', 'T' },
                      D = new ushort[] { 23656 },
                      P = new UIntPtr[] { new UIntPtr(162), new UIntPtr(224) },
                      R = 0.9887526f,
                      W = new
                      {
                          CI = new byte[] { 124 },
                          BH = new short[] { 10766, 11320 },
                          XL = 83,
                          DK = new
                          {
                              YAR = new sbyte[] { 57, 93 },
                              NGU = 4607,
                              JIQ = new ulong[] { 196, 7, 199 },
                              ZWK = new ushort[] { 34225, 2774 },
                              VDB = new
                              {
                                  OFXN = new DateTime(27082),
                                  VVEL = new IntPtr(14600),
                                  RZHP = new
                                  {
                                      BXMRS = false,
                                      BLQWZ = new IntPtr[] { new IntPtr(21788), new IntPtr(6002), new IntPtr(5366) },
                                      XPPWQ = new
                                      {
                                          ZWYYWN = new int[] { 2721, 21432 },
                                          WIXCNZ = new UIntPtr(218),
                                          KWGLHI = new UIntPtr[] { new UIntPtr(229), new UIntPtr(11) },
                                          ZWUJZL = new sbyte[] { 86, 2, 55 },
                                          RPEYNU = new
                                          {
                                              EVIWEDT = "BFWWQYNYOWSDTKTPMPDR",
                                              EHESQFS = 68,
                                              ZXRIUKL = new ulong[] { 242, 68 },
                                              LXUZDPS = new Guid[] { new Guid("738fc0d8-e2bd-0396-788b-3cf75db338cd"), new Guid("32b05ee8-22fd-6d39-ec2d-2b55a568790f"), new Guid("af3a67d9-8c59-b44c-232f-4bfc7c5ae4ba") },
                                              KDGFEKK = new
                                              {
                                                  FRGJNMXQ = 27,
                                                  MMNLTWZI = 98,
                                                  EEOZEBHK = new double[] { 0.533405699084236f, 0.183722000654657f, 0.847805717889129f, 0.974985818366979f, 0.53808212351896f },
                                                  SHYPSEXG = new Guid("e5240539-ff48-2109-efb1-d071eb1418ae"),
                                                  EPPRGZPZ = new
                                                  {
                                                      JJGVVVANR = new double[] { 0.146959445042051f, 0.241059295945363f },
                                                      PFBTFENYT = 4974,
                                                      SJNLPOWTF = "ICVYXFDMBTTYWYZDMLIO",
                                                      AYNWLDPEN = new
                                                      {
                                                          NYOKYNVMBL = new long[] { 16342, 12878 },
                                                          JNLIJLZUPC = new UIntPtr(53),
                                                          EDGISKJOBM = 'D',
                                                          CMBTKSUUCF = 58,
                                                          KDTDFSYWQD = 'E'
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
              );
                          },
                delegate (Event evt)
                {
                    Assert.Equal(logger.Name, evt.ProviderName);
                    Assert.Equal("FWKFKXHDFY", evt.EventName);
                }));

                Test_Write_Fuzzy_TestEtw(tests, logger);

                using (var listener = new EventListenerListener())
                {
                    EventTestHarness.RunTests(tests, listener, logger);
                }
            }
        }

        static partial void Test_Write_Fuzzy_TestEtw(List<SubTest> tests, EventSource logger);
    }
}
