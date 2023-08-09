// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static class NamingPolicyUnitTests
    {
        private readonly static CamelCaseNamingStrategy s_newtonsoftCamelCaseNamingStrategy = new();
        private readonly static SnakeCaseNamingStrategy s_newtonsoftSnakeCaseNamingStrategy = new();
        private readonly static KebabCaseNamingStrategy s_newtonsoftKebabCaseNamingStrategy = new();

        [Theory]
        // These test cases were copied from Json.NET.
        [InlineData("urlValue", "URLValue")]
        [InlineData("url", "URL")]
        [InlineData("id", "ID")]
        [InlineData("i", "I")]
        [InlineData("", "")]
        [InlineData("person", "Person")]
        [InlineData("iPhone", "iPhone")]
        [InlineData("iPhone", "IPhone")]
        [InlineData("i Phone", "I Phone")]
        [InlineData("i  Phone", "I  Phone")]
        [InlineData(" IPhone", " IPhone")]
        [InlineData(" IPhone ", " IPhone ")]
        [InlineData("isCIA", "IsCIA")]
        [InlineData("vmQ", "VmQ")]
        [InlineData("xml2Json", "Xml2Json")]
        [InlineData("snAkEcAsE", "SnAkEcAsE")]
        [InlineData("snA__kEcAsE", "SnA__kEcAsE")]
        [InlineData("snA__ kEcAsE", "SnA__ kEcAsE")]
        [InlineData("already_snake_case_ ", "already_snake_case_ ")]
        [InlineData("isJSONProperty", "IsJSONProperty")]
        [InlineData("shoutinG_CASE", "SHOUTING_CASE")]
        [InlineData("9999-12-31T23:59:59.9999999Z", "9999-12-31T23:59:59.9999999Z")]
        [InlineData("hi!! This is text. Time to test.", "Hi!! This is text. Time to test.")]
        [InlineData("building", "BUILDING")]
        [InlineData("building Property", "BUILDING Property")]
        [InlineData("building Property", "Building Property")]
        [InlineData("building PROPERTY", "BUILDING PROPERTY")]
        public static void ToCamelCaseTest(string expectedResult, string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.CamelCase;
            string newtonsoftResult = s_newtonsoftCamelCaseNamingStrategy.GetPropertyName(name, false);

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
            Assert.Equal(newtonsoftResult, value);
        }

        [Fact]
        public static void CamelCaseNullNameReturnsNull()
        {
            JsonNamingPolicy policy = JsonNamingPolicy.CamelCase;
            Assert.Null(policy.ConvertName(null));
        }

        [Theory]
        [InlineData("xml_http_request", "XMLHttpRequest")]
        [InlineData("camel_case", "camelCase")]
        [InlineData("camel_case", "CamelCase")]
        [InlineData("snake_case", "snake_case")]
        [InlineData("snake_case", "SNAKE_CASE")]
        [InlineData("kebab-case", "kebab-case")]
        [InlineData("keba_b-_case", "KEBAB-CASE")]
        [InlineData("double_space", "double  space")]
        [InlineData("double__underscore", "double__underscore")]
        [InlineData("abc", "abc")]
        [InlineData("ab_c", "abC")]
        [InlineData("a_bc", "aBc")]
        [InlineData("a_bc", "aBC")]
        [InlineData("a_bc", "ABc")]
        [InlineData("abc", "ABC")]
        [InlineData("abc123def456", "abc123def456")]
        [InlineData("abc123_def456", "abc123Def456")]
        [InlineData("abc123_de_f456", "abc123DEF456")]
        [InlineData("ab_c123_de_f456", "ABC123DEF456")]
        [InlineData("ab_c123def456", "ABC123def456")]
        [InlineData("abc123def456", "Abc123def456")]
        [InlineData("abc", "  abc")]
        [InlineData("abc", "abc  ")]
        [InlineData("abc", "  abc  ")]
        [InlineData("abc_def", "  abc def  ")]
        [InlineData("_abc", "_abc")]
        [InlineData("a%", "a%")]
        [InlineData("_?#-", "_?#-")]
        [InlineData("$type", "$type")]
        [InlineData("abc%def", "abc%def")]
        [InlineData("__abc__def__", "__abc__def__")]
        [InlineData("_abc_abc_abc", "_abcAbc_abc")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "a_haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "a_towel_it_says_is_about_the_most_massively_useful_thing_an_interstellar_hitchhiker_can_have_partly_it_has_great_practical_value_you_can_wrap_it_around_you_for_warmth_as_you_bound_across_the_cold_moons_of_jaglan_beta_you_can_lie_on_it_on_the_brilliant_marble_sanded_beaches_of_santraginus_v_inhaling_the_heady_sea_vapors_you_can_sleep_under_it_beneath_the_stars_which_shine_so_redly_on_the_desert_world_of_kakrafoon_use_it_to_sail_a_miniraft_down_the_slow_heavy_river_moth_wet_it_for_use_in_hand_to_hand_combat_wrap_it_round_your_head_to_ward_off_noxious_fumes_or_avoid_the_gaze_of_the_ravenous_bugblatter_beast_of_traal_a_mind_bogglingly_stupid_animal_it_assumes_that_if_you_cant_see_it_it_cant_see_you_daft_as_a_brush_but_very_very_ravenous_you_can_wave_your_towel_in_emergencies_as_a_distress_signal_and_of_course_dry_yourself_of_with_it_if_it_still_seems_to_be_clean_enough",
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
        public static void ToSnakeLowerCase(string expectedResult, string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.SnakeCaseLower;
            string newtonsoftResult = s_newtonsoftSnakeCaseNamingStrategy.GetPropertyName(name, hasSpecifiedName: false);

            string result = policy.ConvertName(name);
            Assert.Equal(expectedResult, result);
            Assert.Equal(newtonsoftResult, result);
        }

        [Theory]
        [InlineData("XML_HTTP_REQUEST", "XMLHttpRequest")]
        [InlineData("CAMEL_CASE", "camelCase")]
        [InlineData("CAMEL_CASE", "CamelCase")]
        [InlineData("SNAKE_CASE", "snake_case")]
        [InlineData("SNAKE_CASE", "SNAKE_CASE")]
        [InlineData("KEBAB-CASE", "kebab-case")]
        [InlineData("KEBA_B-_CASE", "KEBAB-CASE")]
        [InlineData("DOUBLE_SPACE", "double  space")]
        [InlineData("DOUBLE__UNDERSCORE", "double__underscore")]
        [InlineData("ABC", "abc")]
        [InlineData("AB_C", "abC")]
        [InlineData("A_BC", "aBc")]
        [InlineData("A_BC", "aBC")]
        [InlineData("A_BC", "ABc")]
        [InlineData("ABC", "ABC")]
        [InlineData("ABC123DEF456", "abc123def456")]
        [InlineData("ABC123_DEF456", "abc123Def456")]
        [InlineData("ABC123_DE_F456", "abc123DEF456")]
        [InlineData("AB_C123_DE_F456", "ABC123DEF456")]
        [InlineData("AB_C123DEF456", "ABC123def456")]
        [InlineData("ABC123DEF456", "Abc123def456")]
        [InlineData("ABC", "  ABC")]
        [InlineData("AB_C", "ABC  ")]
        [InlineData("AB_C", "  ABC  ")]
        [InlineData("AB_C_DEF", "  ABC def  ")]
        [InlineData("_ABC", "_abc")]
        [InlineData("A%", "a%")]
        [InlineData("_?#-", "_?#-")]
        [InlineData("$TYPE", "$type")]
        [InlineData("ABC%DEF", "abc%def")]
        [InlineData("__ABC__DEF__", "__abc__def__")]
        [InlineData("_ABC_ABC_ABC", "_abcAbc_abc")]
        [InlineData(
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "A_HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "A_TOWEL_IT_SAYS_IS_ABOUT_THE_MOST_MASSIVELY_USEFUL_THING_AN_INTERSTELLAR_HITCHHIKER_CAN_HAVE_PARTLY_IT_HAS_GREAT_PRACTICAL_VALUE_YOU_CAN_WRAP_IT_AROUND_YOU_FOR_WARMTH_AS_YOU_BOUND_ACROSS_THE_COLD_MOONS_OF_JAGLAN_BETA_YOU_CAN_LIE_ON_IT_ON_THE_BRILLIANT_MARBLE_SANDED_BEACHES_OF_SANTRAGINUS_V_INHALING_THE_HEADY_SEA_VAPORS_YOU_CAN_SLEEP_UNDER_IT_BENEATH_THE_STARS_WHICH_SHINE_SO_REDLY_ON_THE_DESERT_WORLD_OF_KAKRAFOON_USE_IT_TO_SAIL_A_MINIRAFT_DOWN_THE_SLOW_HEAVY_RIVER_MOTH_WET_IT_FOR_USE_IN_HAND_TO_HAND_COMBAT_WRAP_IT_ROUND_YOUR_HEAD_TO_WARD_OFF_NOXIOUS_FUMES_OR_AVOID_THE_GAZE_OF_THE_RAVENOUS_BUGBLATTER_BEAST_OF_TRAAL_A_MIND_BOGGLINGLY_STUPID_ANIMAL_IT_ASSUMES_THAT_IF_YOU_CANT_SEE_IT_IT_CANT_SEE_YOU_DAFT_AS_A_BRUSH_BUT_VERY_VERY_RAVENOUS_YOU_CAN_WAVE_YOUR_TOWEL_IN_EMERGENCIES_AS_A_DISTRESS_SIGNAL_AND_OF_COURSE_DRY_YOURSELF_OF_WITH_IT_IF_IT_STILL_SEEMS_TO_BE_CLEAN_ENOUGH",
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
        public static void ToSnakeUpperCase(string expectedResult, string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.SnakeCaseUpper;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
        }

        [Theory]
        [InlineData("xml-http-request", "XMLHttpRequest")]
        [InlineData("camel-case", "camelCase")]
        [InlineData("camel-case", "CamelCase")]
        [InlineData("snake_case", "snake_case")]
        [InlineData("snak-e_-case", "SNAKE_CASE")]
        [InlineData("kebab-case", "kebab-case")]
        [InlineData("kebab-case", "KEBAB-CASE")]
        [InlineData("double-space", "double  space")]
        [InlineData("double__underscore", "double__underscore")]
        [InlineData("double--dash", "double--dash")]
        [InlineData("abc", "abc")]
        [InlineData("ab-c", "abC")]
        [InlineData("a-bc", "aBc")]
        [InlineData("a-bc", "aBC")]
        [InlineData("a-bc", "ABc")]
        [InlineData("abc", "ABC")]
        [InlineData("abc123def456", "abc123def456")]
        [InlineData("abc123-def456", "abc123Def456")]
        [InlineData("abc123-de-f456", "abc123DEF456")]
        [InlineData("ab-c123-de-f456", "ABC123DEF456")]
        [InlineData("ab-c123def456", "ABC123def456")]
        [InlineData("abc123def456", "Abc123def456")]
        [InlineData("abc", "  abc")]
        [InlineData("abc", "abc  ")]
        [InlineData("abc", "  abc  ")]
        [InlineData("abc-def", "  abc def  ")]
        [InlineData("-abc", "-abc")]
        [InlineData("a%", "a%")]
        [InlineData("-?#_", "-?#_")]
        [InlineData("$type", "$type")]
        [InlineData("abc%def", "abc%def")]
        [InlineData("--abc--def--", "--abc--def--")]
        [InlineData("-abc-abc-abc", "-abcAbc-abc")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "a-haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "a-towel-it-says-is-about-the-most-massively-useful-thing-an-interstellar-hitchhiker-can-have_-partly-it-has-great-practical-value_-you-can-wrap-it-around-you-for-warmth-as-you-bound-across-the-cold-moons-of-jaglan-beta_-you-can-lie-on-it-on-the-brilliant-marble-sanded-beaches-of-santraginus-v-inhaling-the-heady-sea-vapors_-you-can-sleep-under-it-beneath-the-stars-which-shine-so-redly-on-the-desert-world-of-kakrafoon_-use-it-to-sail-a-miniraft-down-the-slow-heavy-river-moth_-wet-it-for-use-in-hand-to-hand-combat_-wrap-it-round-your-head-to-ward-off-noxious-fumes-or-avoid-the-gaze-of-the-ravenous-bugblatter-beast-of-traal-a-mind-bogglingly-stupid-animal_-it-assumes-that-if-you-cant-see-it-it-cant-see-you-daft-as-a-brush-but-very-very-ravenous_-you-can-wave-your-towel-in-emergencies-as-a-distress-signal-and-of-course-dry-yourself-of-with-it-if-it-still-seems-to-be-clean-enough",
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]            
        public static void ToKebabLowerCase(string expectedResult, string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.KebabCaseLower;
            string newtonsoftResult = s_newtonsoftKebabCaseNamingStrategy.GetPropertyName(name, false);

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
            Assert.Equal(newtonsoftResult, value);
        }

        [Theory]
        [InlineData("XML-HTTP-REQUEST", "XMLHttpRequest")]
        [InlineData("CAMEL-CASE", "camelCase")]
        [InlineData("CAMEL-CASE", "CamelCase")]
        [InlineData("SNAKE_CASE", "snake_case")]
        [InlineData("SNAK-E_-CASE", "SNAKE_CASE")]
        [InlineData("KEBAB-CASE", "kebab-case")]
        [InlineData("KEBAB-CASE", "KEBAB-CASE")]
        [InlineData("DOUBLE-SPACE", "double  space")]
        [InlineData("DOUBLE__UNDERSCORE", "double__underscore")]
        [InlineData("DOUBLE--DASH", "double--dash")]
        [InlineData("ABC", "abc")]
        [InlineData("AB-C", "abC")]
        [InlineData("A-BC", "aBc")]
        [InlineData("A-BC", "aBC")]
        [InlineData("A-BC", "ABc")]
        [InlineData("ABC", "ABC")]
        [InlineData("ABC123DEF456", "abc123def456")]
        [InlineData("ABC123-DEF456", "abc123Def456")]
        [InlineData("ABC123-DE-F456", "abc123DEF456")]
        [InlineData("AB-C123-DE-F456", "ABC123DEF456")]
        [InlineData("AB-C123DEF456", "ABC123def456")]
        [InlineData("ABC123DEF456", "Abc123def456")]
        [InlineData("ABC", "  ABC")]
        [InlineData("AB-C", "ABC  ")]
        [InlineData("AB-C", "  ABC  ")]
        [InlineData("AB-C-DEF", "  ABC def  ")]
        [InlineData("-ABC", "-abc")]
        [InlineData("A%", "a%")]
        [InlineData("-?#_", "-?#_")]
        [InlineData("$TYPE", "$type")]
        [InlineData("ABC%DEF", "abc%def")]
        [InlineData("--ABC--DEF--", "--abc--def--")]
        [InlineData("-ABC-ABC-ABC", "-abcAbc-abc")]
        [InlineData(
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "A-HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "A-TOWEL-IT-SAYS-IS-ABOUT-THE-MOST-MASSIVELY-USEFUL-THING-AN-INTERSTELLAR-HITCHHIKER-CAN-HAVE_-PARTLY-IT-HAS-GREAT-PRACTICAL-VALUE_-YOU-CAN-WRAP-IT-AROUND-YOU-FOR-WARMTH-AS-YOU-BOUND-ACROSS-THE-COLD-MOONS-OF-JAGLAN-BETA_-YOU-CAN-LIE-ON-IT-ON-THE-BRILLIANT-MARBLE-SANDED-BEACHES-OF-SANTRAGINUS-V-INHALING-THE-HEADY-SEA-VAPORS_-YOU-CAN-SLEEP-UNDER-IT-BENEATH-THE-STARS-WHICH-SHINE-SO-REDLY-ON-THE-DESERT-WORLD-OF-KAKRAFOON_-USE-IT-TO-SAIL-A-MINIRAFT-DOWN-THE-SLOW-HEAVY-RIVER-MOTH_-WET-IT-FOR-USE-IN-HAND-TO-HAND-COMBAT_-WRAP-IT-ROUND-YOUR-HEAD-TO-WARD-OFF-NOXIOUS-FUMES-OR-AVOID-THE-GAZE-OF-THE-RAVENOUS-BUGBLATTER-BEAST-OF-TRAAL-A-MIND-BOGGLINGLY-STUPID-ANIMAL_-IT-ASSUMES-THAT-IF-YOU-CANT-SEE-IT-IT-CANT-SEE-YOU-DAFT-AS-A-BRUSH-BUT-VERY-VERY-RAVENOUS_-YOU-CAN-WAVE-YOUR-TOWEL-IN-EMERGENCIES-AS-A-DISTRESS-SIGNAL-AND-OF-COURSE-DRY-YOURSELF-OF-WITH-IT-IF-IT-STILL-SEEMS-TO-BE-CLEAN-ENOUGH",
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
        public static void ToKebabUpperCase(string expectedResult, string name)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.KebabCaseUpper;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
        }

        [Theory, OuterLoop]
        [MemberData(nameof(GetValidMemberNames))]
        public static void CamelCaseNamingPolicyMatchesNewtonsoftNamingStrategy(string name)
        {
            string newtonsoftResult = s_newtonsoftCamelCaseNamingStrategy.GetPropertyName(name, hasSpecifiedName: false);
            string stjResult = JsonNamingPolicy.CamelCase.ConvertName(name);
            Assert.Equal(newtonsoftResult, stjResult);
        }

        [Theory, OuterLoop]
        [MemberData(nameof(GetValidMemberNames))]
        public static void SnakeCaseNamingPolicyMatchesNewtonsoftNamingStrategy(string name)
        {
            string newtonsoftResult = s_newtonsoftSnakeCaseNamingStrategy.GetPropertyName(name, hasSpecifiedName: false);
            string stjResult = JsonNamingPolicy.SnakeCaseLower.ConvertName(name);
            Assert.Equal(newtonsoftResult, stjResult);
        }

        [Theory, OuterLoop]
        [MemberData(nameof(GetValidMemberNames))]
        public static void KebabCaseNamingPolicyMatchesNewtonsoftNamingStrategy(string name)
        {
            string newtonsoftResult = s_newtonsoftKebabCaseNamingStrategy.GetPropertyName(name, hasSpecifiedName: false);
            string stjResult = JsonNamingPolicy.KebabCaseLower.ConvertName(name);
            Assert.Equal(newtonsoftResult, stjResult);
        }

        public static IEnumerable<object[]> GetValidMemberNames()
            => typeof(PropertyNameTestsDynamic).Assembly.GetTypes()
                .SelectMany(t => t.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Field)
                .Select(m => m.Name)
                .Distinct()
                .Select(name => new object[] { name })
                .ToArray();
    }
}
