/* test_crc32.cc -- crc32 unit test
 * Copyright (C) 2019-2021 IBM Corporation
 * Authors: Rogerio Alves    <rogealve@br.ibm.com>
 *          Matheus Castanho <msc@linux.ibm.com>
 * For conditions of distribution and use, see copyright notice in zlib.h
 */

#include <stdio.h>
#include <string.h>
#include <stdlib.h>

extern "C" {
#  include "zbuild.h"
#  include "arch_functions.h"
#  include "test_cpu_features.h"
}

#include <gtest/gtest.h>

typedef struct {
    unsigned long crc;
    const uint8_t *buf;
    size_t len;
    unsigned long expect;
} crc32_test;

static const crc32_test tests[] = {
  {0x0, (const uint8_t *)0x0, 0, 0x0},
  {0xffffffff, (const uint8_t *)0x0, 0, 0x0},
  {0x0, (const uint8_t *)0x0, 255, 0x0}, /*  BZ 174799.  */
  {0x0, (const uint8_t *)0x0, 256, 0x0},
  {0x0, (const uint8_t *)0x0, 257, 0x0},
  {0x0, (const uint8_t *)0x0, 32767, 0x0},
  {0x0, (const uint8_t *)0x0, 32768, 0x0},
  {0x0, (const uint8_t *)0x0, 32769, 0x0},
  {0x0, (const uint8_t *)"", 0, 0x0},
  {0xffffffff, (const uint8_t *)"", 0, 0xffffffff},
  {0x0, (const uint8_t *)"abacus", 6, 0xc3d7115b},
  {0x0, (const uint8_t *)"backlog", 7, 0x269205},
  {0x0, (const uint8_t *)"campfire", 8, 0x22a515f8},
  {0x0, (const uint8_t *)"delta", 5, 0x9643fed9},
  {0x0, (const uint8_t *)"executable", 10, 0xd68eda01},
  {0x0, (const uint8_t *)"file", 4, 0x8c9f3610},
  {0x0, (const uint8_t *)"greatest", 8, 0xc1abd6cd},
  {0x0, (const uint8_t *)"hello", 5, 0x3610a686},
  {0x0, (const uint8_t *)"inverter", 8, 0xc9e962c9},
  {0x0, (const uint8_t *)"jigsaw", 6, 0xce4e3f69},
  {0x0, (const uint8_t *)"karate", 6, 0x890be0e2},
  {0x0, (const uint8_t *)"landscape", 9, 0xc4e0330b},
  {0x0, (const uint8_t *)"machine", 7, 0x1505df84},
  {0x0, (const uint8_t *)"nanometer", 9, 0xd4e19f39},
  {0x0, (const uint8_t *)"oblivion", 8, 0xdae9de77},
  {0x0, (const uint8_t *)"panama", 6, 0x66b8979c},
  {0x0, (const uint8_t *)"quest", 5, 0x4317f817},
  {0x0, (const uint8_t *)"resource", 8, 0xbc91f416},
  {0x0, (const uint8_t *)"secret", 6, 0x5ca2e8e5},
  {0x0, (const uint8_t *)"test", 4, 0xd87f7e0c},
  {0x0, (const uint8_t *)"ultimate", 8, 0x3fc79b0b},
  {0x0, (const uint8_t *)"vector", 6, 0x1b6e485b},
  {0x0, (const uint8_t *)"walrus", 6, 0xbe769b97},
  {0x0, (const uint8_t *)"xeno", 4, 0xe7a06444},
  {0x0, (const uint8_t *)"yelling", 7, 0xfe3944e5},
  {0x0, (const uint8_t *)"zlib", 4, 0x73887d3a},
  {0x0, (const uint8_t *)"4BJD7PocN1VqX0jXVpWB", 20, 0xd487a5a1},
  {0x0, (const uint8_t *)"F1rPWI7XvDs6nAIRx41l", 20, 0x61a0132e},
  {0x0, (const uint8_t *)"ldhKlsVkPFOveXgkGtC2", 20, 0xdf02f76},
  {0x0, (const uint8_t *)"5KKnGOOrs8BvJ35iKTOS", 20, 0x579b2b0a},
  {0x0, (const uint8_t *)"0l1tw7GOcem06Ddu7yn4", 20, 0xf7d16e2d},
  {0x0, (const uint8_t *)"MCr47CjPIn9R1IvE1Tm5", 20, 0x731788f5},
  {0x0, (const uint8_t *)"UcixbzPKTIv0SvILHVdO", 20, 0x7112bb11},
  {0x0, (const uint8_t *)"dGnAyAhRQDsWw0ESou24", 20, 0xf32a0dac},
  {0x0, (const uint8_t *)"di0nvmY9UYMYDh0r45XT", 20, 0x625437bb},
  {0x0, (const uint8_t *)"2XKDwHfAhFsV0RhbqtvH", 20, 0x896930f9},
  {0x0, (const uint8_t *)"ZhrANFIiIvRnqClIVyeD", 20, 0x8579a37},
  {0x0, (const uint8_t *)"v7Q9ehzioTOVeDIZioT1", 20, 0x632aa8e0},
  {0x0, (const uint8_t *)"Yod5hEeKcYqyhfXbhxj2", 20, 0xc829af29},
  {0x0, (const uint8_t *)"GehSWY2ay4uUKhehXYb0", 20, 0x1b08b7e8},
  {0x0, (const uint8_t *)"kwytJmq6UqpflV8Y8GoE", 20, 0x4e33b192},
  {0x0, (const uint8_t *)"70684206568419061514", 20, 0x59a179f0},
  {0x0, (const uint8_t *)"42015093765128581010", 20, 0xcd1013d7},
  {0x0, (const uint8_t *)"88214814356148806939", 20, 0xab927546},
  {0x0, (const uint8_t *)"43472694284527343838", 20, 0x11f3b20c},
  {0x0, (const uint8_t *)"49769333513942933689", 20, 0xd562d4ca},
  {0x0, (const uint8_t *)"54979784887993251199", 20, 0x233395f7},
  {0x0, (const uint8_t *)"58360544869206793220", 20, 0x2d167fd5},
  {0x0, (const uint8_t *)"27347953487840714234", 20, 0x8b5108ba},
  {0x0, (const uint8_t *)"07650690295365319082", 20, 0xc46b3cd8},
  {0x0, (const uint8_t *)"42655507906821911703", 20, 0xc10b2662},
  {0x0, (const uint8_t *)"29977409200786225655", 20, 0xc9a0f9d2},
  {0x0, (const uint8_t *)"85181542907229116674", 20, 0x9341357b},
  {0x0, (const uint8_t *)"87963594337989416799", 20, 0xf0424937},
  {0x0, (const uint8_t *)"21395988329504168551", 20, 0xd7c4c31f},
  {0x0, (const uint8_t *)"51991013580943379423", 20, 0xf11edcc4},
  {0x0, (const uint8_t *)"*]+@!);({_$;}[_},?{?;(_?,=-][@", 30, 0x40795df4},
  {0x0, (const uint8_t *)"_@:_).&(#.[:[{[:)$++-($_;@[)}+", 30, 0xdd61a631},
  {0x0, (const uint8_t *)"&[!,[$_==}+.]@!;*(+},[;:)$;)-@", 30, 0xca907a99},
  {0x0, (const uint8_t *)"]{.[.+?+[[=;[?}_#&;[=)__$$:+=_", 30, 0xf652deac},
  {0x0, (const uint8_t *)"-%.)=/[@].:.(:,()$;=%@-$?]{%+%", 30, 0xaf39a5a9},
  {0x0, (const uint8_t *)"+]#$(@&.=:,*];/.!]%/{:){:@(;)$", 30, 0x6bebb4cf},
  {0x0, (const uint8_t *)")-._.:?[&:.=+}(*$/=!.${;(=$@!}", 30, 0x76430bac},
  {0x0, (const uint8_t *)":(_*&%/[[}+,?#$&*+#[([*-/#;%(]", 30, 0x6c80c388},
  {0x0, (const uint8_t *)"{[#-;:$/{)(+[}#]/{&!%(@)%:@-$:", 30, 0xd54d977d},
  {0x0, (const uint8_t *)"_{$*,}(&,@.)):=!/%(&(,,-?$}}}!", 30, 0xe3966ad5},
  {0x0, (const uint8_t *)"e$98KNzqaV)Y:2X?]77].{gKRD4G5{mHZk,Z)SpU%L3FSgv!Wb8MLAFdi{+fp)c,@8m6v)yXg@]HBDFk?.4&}g5_udE*JHCiH=aL", 100, 0xe7c71db9},
  {0x0, (const uint8_t *)"r*Fd}ef+5RJQ;+W=4jTR9)R*p!B;]Ed7tkrLi;88U7g@3v!5pk2X6D)vt,.@N8c]@yyEcKi[vwUu@.Ppm@C6%Mv*3Nw}Y,58_aH)", 100, 0xeaa52777},
  {0x0, (const uint8_t *)"h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&", 100, 0xcd472048},
  {0x7a30360d, (const uint8_t *)"abacus", 6, 0xf8655a84},
  {0x6fd767ee, (const uint8_t *)"backlog", 7, 0x1ed834b1},
  {0xefeb7589, (const uint8_t *)"campfire", 8, 0x686cfca},
  {0x61cf7e6b, (const uint8_t *)"delta", 5, 0x1554e4b1},
  {0xdc712e2,  (const uint8_t *)"executable", 10, 0x761b4254},
  {0xad23c7fd, (const uint8_t *)"file", 4, 0x7abdd09b},
  {0x85cb2317, (const uint8_t *)"greatest", 8, 0x4ba91c6b},
  {0x9eed31b0, (const uint8_t *)"inverter", 8, 0xd5e78ba5},
  {0xb94f34ca, (const uint8_t *)"jigsaw", 6, 0x23649109},
  {0xab058a2,  (const uint8_t *)"karate", 6, 0xc5591f41},
  {0x5bff2b7a, (const uint8_t *)"landscape", 9, 0xf10eb644},
  {0x605c9a5f, (const uint8_t *)"machine", 7, 0xbaa0a636},
  {0x51bdeea5, (const uint8_t *)"nanometer", 9, 0x6af89afb},
  {0x85c21c79, (const uint8_t *)"oblivion", 8, 0xecae222b},
  {0x97216f56, (const uint8_t *)"panama", 6, 0x47dffac4},
  {0x18444af2, (const uint8_t *)"quest", 5, 0x70c2fe36},
  {0xbe6ce359, (const uint8_t *)"resource", 8, 0x1471d925},
  {0x843071f1, (const uint8_t *)"secret", 6, 0x50c9a0db},
  {0xf2480c60, (const uint8_t *)"ultimate", 8, 0xf973daf8},
  {0x2d2feb3d, (const uint8_t *)"vector", 6, 0x344ac03d},
  {0x7490310a, (const uint8_t *)"walrus", 6, 0x6d1408ef},
  {0x97d247d4, (const uint8_t *)"xeno", 4, 0xe62670b5},
  {0x93cf7599, (const uint8_t *)"yelling", 7, 0x1b36da38},
  {0x73c84278, (const uint8_t *)"zlib", 4, 0x6432d127},
  {0x228a87d1, (const uint8_t *)"4BJD7PocN1VqX0jXVpWB", 20, 0x997107d0},
  {0xa7a048d0, (const uint8_t *)"F1rPWI7XvDs6nAIRx41l", 20, 0xdc567274},
  {0x1f0ded40, (const uint8_t *)"ldhKlsVkPFOveXgkGtC2", 20, 0xdcc63870},
  {0xa804a62f, (const uint8_t *)"5KKnGOOrs8BvJ35iKTOS", 20, 0x6926cffd},
  {0x508fae6a, (const uint8_t *)"0l1tw7GOcem06Ddu7yn4", 20, 0xb52b38bc},
  {0xe5adaf4f, (const uint8_t *)"MCr47CjPIn9R1IvE1Tm5", 20, 0xf83b8178},
  {0x67136a40, (const uint8_t *)"UcixbzPKTIv0SvILHVdO", 20, 0xc5213070},
  {0xb00c4a10, (const uint8_t *)"dGnAyAhRQDsWw0ESou24", 20, 0xbc7648b0},
  {0x2e0c84b5, (const uint8_t *)"di0nvmY9UYMYDh0r45XT", 20, 0xd8123a72},
  {0x81238d44, (const uint8_t *)"2XKDwHfAhFsV0RhbqtvH", 20, 0xd5ac5620},
  {0xf853aa92, (const uint8_t *)"ZhrANFIiIvRnqClIVyeD", 20, 0xceae099d},
  {0x5a692325, (const uint8_t *)"v7Q9ehzioTOVeDIZioT1", 20, 0xb07d2b24},
  {0x3275b9f,  (const uint8_t *)"Yod5hEeKcYqyhfXbhxj2", 20, 0x24ce91df},
  {0x38371feb, (const uint8_t *)"GehSWY2ay4uUKhehXYb0", 20, 0x707b3b30},
  {0xafc8bf62, (const uint8_t *)"kwytJmq6UqpflV8Y8GoE", 20, 0x16abc6a9},
  {0x9b07db73, (const uint8_t *)"70684206568419061514", 20, 0xae1fb7b7},
  {0xe75b214,  (const uint8_t *)"42015093765128581010", 20, 0xd4eecd2d},
  {0x72d0fe6f, (const uint8_t *)"88214814356148806939", 20, 0x4660ec7},
  {0xf857a4b1, (const uint8_t *)"43472694284527343838", 20, 0xfd8afdf7},
  {0x54b8e14,  (const uint8_t *)"49769333513942933689", 20, 0xc6d1b5f2},
  {0xd6aa5616, (const uint8_t *)"54979784887993251199", 20, 0x32476461},
  {0x11e63098, (const uint8_t *)"58360544869206793220", 20, 0xd917cf1a},
  {0xbe92385,  (const uint8_t *)"27347953487840714234", 20, 0x4ad14a12},
  {0x49511de0, (const uint8_t *)"07650690295365319082", 20, 0xe37b5c6c},
  {0x3db13bc1, (const uint8_t *)"42655507906821911703", 20, 0x7cc497f1},
  {0xbb899bea, (const uint8_t *)"29977409200786225655", 20, 0x99781bb2},
  {0xf6cd9436, (const uint8_t *)"85181542907229116674", 20, 0x132256a1},
  {0x9109e6c3, (const uint8_t *)"87963594337989416799", 20, 0xbfdb2c83},
  {0x75770fc,  (const uint8_t *)"21395988329504168551", 20, 0x8d9d1e81},
  {0x69b1d19b, (const uint8_t *)"51991013580943379423", 20, 0x7b6d4404},
  {0xc6132975, (const uint8_t *)"*]+@!);({_$;}[_},?{?;(_?,=-][@", 30, 0x8619f010},
  {0xd58cb00c, (const uint8_t *)"_@:_).&(#.[:[{[:)$++-($_;@[)}+", 30, 0x15746ac3},
  {0xb63b8caa, (const uint8_t *)"&[!,[$_==}+.]@!;*(+},[;:)$;)-@", 30, 0xaccf812f},
  {0x8a45a2b8, (const uint8_t *)"]{.[.+?+[[=;[?}_#&;[=)__$$:+=_", 30, 0x78af45de},
  {0xcbe95b78, (const uint8_t *)"-%.)=/[@].:.(:,()$;=%@-$?]{%+%", 30, 0x25b06b59},
  {0x4ef8a54b, (const uint8_t *)"+]#$(@&.=:,*];/.!]%/{:){:@(;)$", 30, 0x4ba0d08f},
  {0x76ad267a, (const uint8_t *)")-._.:?[&:.=+}(*$/=!.${;(=$@!}", 30, 0xe26b6aac},
  {0x569e613c, (const uint8_t *)":(_*&%/[[}+,?#$&*+#[([*-/#;%(]", 30, 0x7e2b0a66},
  {0x36aa61da, (const uint8_t *)"{[#-;:$/{)(+[}#]/{&!%(@)%:@-$:", 30, 0xb3430dc7},
  {0xf67222df, (const uint8_t *)"_{$*,}(&,@.)):=!/%(&(,,-?$}}}!", 30, 0x626c17a},
  {0x74b34fd3, (const uint8_t *)"e$98KNzqaV)Y:2X?]77].{gKRD4G5{mHZk,Z)SpU%L3FSgv!Wb8MLAFdi{+fp)c,@8m6v)yXg@]HBDFk?.4&}g5_udE*JHCiH=aL", 100, 0xccf98060},
  {0x351fd770, (const uint8_t *)"r*Fd}ef+5RJQ;+W=4jTR9)R*p!B;]Ed7tkrLi;88U7g@3v!5pk2X6D)vt,.@N8c]@yyEcKi[vwUu@.Ppm@C6%Mv*3Nw}Y,58_aH)", 100, 0xd8b95312},
  {0xc45aef77, (const uint8_t *)"h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&", 100, 0xbb1c9912},
  {0xc45aef77, (const uint8_t *)
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&"
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&"
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&"
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&"
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&"
    "h{bcmdC+a;t+Cf{6Y_dFq-{X4Yu&7uNfVDh?q&_u.UWJU],-GiH7ADzb7-V.Q%4=+v!$L9W+T=bP]$_:]Vyg}A.ygD.r;h-D]m%&", 600, 0x888AFA5B}
};

class crc32_variant : public ::testing::TestWithParam<crc32_test> {
public:
    void hash(crc32_test param, crc32_func crc32) {
        uint32_t crc = 0;
        if (param.buf != NULL) {
            if (param.len) {
                crc = crc32(param.crc, param.buf, param.len);
            } else {
                crc = param.crc;
            }
        }
        EXPECT_EQ(crc, param.expect);
    }
};

INSTANTIATE_TEST_SUITE_P(crc32, crc32_variant, testing::ValuesIn(tests));

#define TEST_CRC32(name, func, support_flag) \
    TEST_P(crc32_variant, name) { \
        if (!(support_flag)) { \
            GTEST_SKIP(); \
            return; \
        } \
        hash(GetParam(), func); \
    }

TEST_CRC32(braid, PREFIX(crc32_braid), 1)

#ifdef DISABLE_RUNTIME_CPU_DETECTION
TEST_CRC32(native, native_crc32, 1)
#else

#ifdef ARM_ACLE
TEST_CRC32(acle, crc32_acle, test_cpu_features.arm.has_crc32)
#endif
#ifdef POWER8_VSX_CRC32
TEST_CRC32(power8, crc32_power8, test_cpu_features.power.has_arch_2_07)
#endif
#ifdef S390_CRC32_VX
TEST_CRC32(vx, crc32_s390_vx, test_cpu_features.s390.has_vx)
#endif
#ifdef X86_PCLMULQDQ_CRC
TEST_CRC32(pclmulqdq, crc32_pclmulqdq, test_cpu_features.x86.has_pclmulqdq)
#endif
#ifdef X86_VPCLMULQDQ_CRC
TEST_CRC32(vpclmulqdq, crc32_vpclmulqdq, (test_cpu_features.x86.has_pclmulqdq && test_cpu_features.x86.has_avx512_common && test_cpu_features.x86.has_vpclmulqdq))
#endif

#endif
