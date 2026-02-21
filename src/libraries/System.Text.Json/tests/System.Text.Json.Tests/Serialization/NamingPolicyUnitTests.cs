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

        [Theory]
        // These test cases were copied from Json.NET.
        [InlineData("URLValue", "urlValue")]
        [InlineData("URL", "url")]
        [InlineData("ID", "id")]
        [InlineData("I", "i")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "άλφαΒήταΓάμμα")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("Person", "person")]
        [InlineData("iPhone", "iPhone")]
        [InlineData("IPhone", "iPhone")]
        [InlineData("I Phone", "i Phone")]
        [InlineData("I  Phone", "i  Phone")]
        [InlineData(" IPhone", " IPhone")]
        [InlineData(" IPhone ", " IPhone ")]
        [InlineData("IsCIA", "isCIA")]
        [InlineData("VmQ", "vmQ")]
        [InlineData("Xml2Json", "xml2Json")]
        [InlineData("SnAkEcAsE", "snAkEcAsE")]
        [InlineData("SnA__kEcAsE", "snA__kEcAsE")]
        [InlineData("SnA__ kEcAsE", "snA__ kEcAsE")]
        [InlineData("already_snake_case_ ", "already_snake_case_ ")]
        [InlineData("IsJSONProperty", "isJSONProperty")]
        [InlineData("SHOUTING_CASE", "shoutinG_CASE")]
        [InlineData("9999-12-31T23:59:59.9999999Z", "9999-12-31T23:59:59.9999999Z")]
        [InlineData("Hi!! This is text. Time to test.", "hi!! This is text. Time to test.")]
        [InlineData("BUILDING", "building")]
        [InlineData("BUILDING Property", "building Property")]
        [InlineData("Building Property", "building Property")]
        [InlineData("BUILDING PROPERTY", "building PROPERTY")]
        public static void ToCamelCaseTest(string name, string expectedResult)
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

        [Theory, OuterLoop]
        [MemberData(nameof(GetValidMemberNames))]
        public static void CamelCaseNamingPolicyMatchesNewtonsoftNamingStrategy(string name)
        {
            string newtonsoftResult = s_newtonsoftCamelCaseNamingStrategy.GetPropertyName(name, hasSpecifiedName: false);
            string stjResult = JsonNamingPolicy.CamelCase.ConvertName(name);
            Assert.Equal(newtonsoftResult, stjResult);
        }

        [Theory]
        [InlineData("XMLHttpRequest", "xml_http_request")]
        [InlineData("SHA512HashAlgorithm", "sha512_hash_algorithm")]
        [InlineData("i18n", "i18n")]
        [InlineData("I18nPolicy", "i18n_policy")]
        [InlineData("7samurai", "7samurai")]
        [InlineData("camelCase", "camel_case")]
        [InlineData("CamelCase", "camel_case")]
        [InlineData("snake_case", "snake_case")]
        [InlineData("SNAKE_CASE", "snake_case")]
        [InlineData("kebab-case", "kebab-case")]
        [InlineData("KEBAB-CASE", "kebab-case")]
        [InlineData("double  space", "double_space")]
        [InlineData("double__underscore", "double__underscore")]
        [InlineData("double--dash", "double--dash")]
        [InlineData("abc", "abc")]
        [InlineData("abC", "ab_c")]
        [InlineData("aBc", "a_bc")]
        [InlineData("aBC", "a_bc")]
        [InlineData("ABc", "a_bc")]
        [InlineData("ABC", "abc")]
        [InlineData("abc123def456", "abc123def456")]
        [InlineData("abc123Def456", "abc123_def456")]
        [InlineData("abc123DEF456", "abc123_def456")]
        [InlineData("ABC123DEF456", "abc123_def456")]
        [InlineData("ABC123def456", "abc123def456")]
        [InlineData("Abc123def456", "abc123def456")]
        [InlineData("  abc", "abc")]
        [InlineData("abc  ", "abc")]
        [InlineData("  abc  ", "abc")]
        [InlineData("  Abc  ", "abc")]
        [InlineData("  7ab7  ", "7ab7")]
        [InlineData("  abc def  ", "abc_def")]
        [InlineData("  abc  def  ", "abc_def")]
        [InlineData("  abc   def  ", "abc_def")]
        [InlineData("  abc 7ef  ", "abc_7ef")]
        [InlineData("  ab7 def  ", "ab7_def")]
        [InlineData("_abc", "_abc")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "άλφα_βήτα_γάμμα")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("𐐀AbcDef𐐨Abc😀Def𐐀", "𐐀abc_def𐐨abc😀def𐐀")]
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("a%", "a%")]
        [InlineData("_?#-", "_?#-")]
        [InlineData("? ! ?", "?!?")]
        [InlineData("$type", "$type")]
        [InlineData("abc%def", "abc%def")]
        [InlineData("__abc__def__", "__abc__def__")]
        [InlineData("_abcAbc_abc", "_abc_abc_abc")]
        [InlineData("ABC???def", "abc???def")]
        [InlineData("ABCd  - _ -   DE f", "ab_cd-_-de_f")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "a_haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough",
            "a_towel_it_says_is_about_the_most_massively_useful_thing_an_interstellar_hitchhiker_can_have_partly_it_has_great_practical_value_you_can_wrap_it_around_you_for_warmth_as_you_bound_across_the_cold_moons_of_jaglan_beta_you_can_lie_on_it_on_the_brilliant_marble_sanded_beaches_of_santraginus_v_inhaling_the_heady_sea_vapors_you_can_sleep_under_it_beneath_the_stars_which_shine_so_redly_on_the_desert_world_of_kakrafoon_use_it_to_sail_a_miniraft_down_the_slow_heavy_river_moth_wet_it_for_use_in_hand_to_hand_combat_wrap_it_round_your_head_to_ward_off_noxious_fumes_or_avoid_the_gaze_of_the_ravenous_bugblatter_beast_of_traal_a_mind_bogglingly_stupid_animal_it_assumes_that_if_you_cant_see_it_it_cant_see_you_daft_as_a_brush_but_very_very_ravenous_you_can_wave_your_towel_in_emergencies_as_a_distress_signal_and_of_course_dry_yourself_of_with_it_if_it_still_seems_to_be_clean_enough")]
        public static void ToSnakeLowerCase(string name, string expectedResult)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.SnakeCaseLower;

            string result = policy.ConvertName(name);

            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("XMLHttpRequest", "XML_HTTP_REQUEST")]
        [InlineData("SHA512HashAlgorithm", "SHA512_HASH_ALGORITHM")]
        [InlineData("i18n", "I18N")]
        [InlineData("I18nPolicy", "I18N_POLICY")]
        [InlineData("7samurai", "7SAMURAI")]
        [InlineData("camelCase", "CAMEL_CASE")]
        [InlineData("CamelCase", "CAMEL_CASE")]
        [InlineData("snake_case", "SNAKE_CASE")]
        [InlineData("SNAKE_CASE", "SNAKE_CASE")]
        [InlineData("kebab-case", "KEBAB-CASE")]
        [InlineData("KEBAB-CASE", "KEBAB-CASE")]
        [InlineData("double  space", "DOUBLE_SPACE")]
        [InlineData("double__underscore", "DOUBLE__UNDERSCORE")]
        [InlineData("double--dash", "DOUBLE--DASH")]
        [InlineData("abc", "ABC")]
        [InlineData("abC", "AB_C")]
        [InlineData("aBc", "A_BC")]
        [InlineData("aBC", "A_BC")]
        [InlineData("ABc", "A_BC")]
        [InlineData("ABC", "ABC")]
        [InlineData("abc123def456", "ABC123DEF456")]
        [InlineData("abc123Def456", "ABC123_DEF456")]
        [InlineData("abc123DEF456", "ABC123_DEF456")]
        [InlineData("ABC123DEF456", "ABC123_DEF456")]
        [InlineData("ABC123def456", "ABC123DEF456")]
        [InlineData("Abc123def456", "ABC123DEF456")]
        [InlineData("  ABC", "ABC")]
        [InlineData("ABC  ", "ABC")]
        [InlineData("  ABC  ", "ABC")]
        [InlineData("  Abc  ", "ABC")]
        [InlineData("  7ab7  ", "7AB7")]
        [InlineData("  ABC def  ", "ABC_DEF")]
        [InlineData("  abc  def  ", "ABC_DEF")]
        [InlineData("  abc   def  ", "ABC_DEF")]
        [InlineData("  abc 7ef  ", "ABC_7EF")]
        [InlineData("  ab7 def  ", "AB7_DEF")]
        [InlineData("_abc", "_ABC")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "ΆΛΦΑ_ΒΉΤΑ_ΓΆΜΜΑ")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("𐐀AbcDef𐐨Abc😀Def𐐀", "𐐀ABC_DEF𐐨ABC😀DEF𐐀")]
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("a%", "A%")]
        [InlineData("_?#-", "_?#-")]
        [InlineData("? ! ?", "?!?")]
        [InlineData("$type", "$TYPE")]
        [InlineData("abc%def", "ABC%DEF")]
        [InlineData("__abc__def__", "__ABC__DEF__")]
        [InlineData("_abcAbc_abc", "_ABC_ABC_ABC")]
        [InlineData("ABC???def", "ABC???DEF")]
        [InlineData("ABCd  - _ -   DE f", "AB_CD-_-DE_F")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData(
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "A_HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData(
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough",
            "A_TOWEL_IT_SAYS_IS_ABOUT_THE_MOST_MASSIVELY_USEFUL_THING_AN_INTERSTELLAR_HITCHHIKER_CAN_HAVE_PARTLY_IT_HAS_GREAT_PRACTICAL_VALUE_YOU_CAN_WRAP_IT_AROUND_YOU_FOR_WARMTH_AS_YOU_BOUND_ACROSS_THE_COLD_MOONS_OF_JAGLAN_BETA_YOU_CAN_LIE_ON_IT_ON_THE_BRILLIANT_MARBLE_SANDED_BEACHES_OF_SANTRAGINUS_V_INHALING_THE_HEADY_SEA_VAPORS_YOU_CAN_SLEEP_UNDER_IT_BENEATH_THE_STARS_WHICH_SHINE_SO_REDLY_ON_THE_DESERT_WORLD_OF_KAKRAFOON_USE_IT_TO_SAIL_A_MINIRAFT_DOWN_THE_SLOW_HEAVY_RIVER_MOTH_WET_IT_FOR_USE_IN_HAND_TO_HAND_COMBAT_WRAP_IT_ROUND_YOUR_HEAD_TO_WARD_OFF_NOXIOUS_FUMES_OR_AVOID_THE_GAZE_OF_THE_RAVENOUS_BUGBLATTER_BEAST_OF_TRAAL_A_MIND_BOGGLINGLY_STUPID_ANIMAL_IT_ASSUMES_THAT_IF_YOU_CANT_SEE_IT_IT_CANT_SEE_YOU_DAFT_AS_A_BRUSH_BUT_VERY_VERY_RAVENOUS_YOU_CAN_WAVE_YOUR_TOWEL_IN_EMERGENCIES_AS_A_DISTRESS_SIGNAL_AND_OF_COURSE_DRY_YOURSELF_OF_WITH_IT_IF_IT_STILL_SEEMS_TO_BE_CLEAN_ENOUGH")]
        public static void ToSnakeUpperCase(string name, string expectedResult)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.SnakeCaseUpper;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
        }

        [Theory]
        [InlineData("XMLHttpRequest", "xml-http-request")]
        [InlineData("SHA512HashAlgorithm", "sha512-hash-algorithm")]
        [InlineData("i18n", "i18n")]
        [InlineData("I18nPolicy", "i18n-policy")]
        [InlineData("7samurai", "7samurai")]
        [InlineData("camelCase", "camel-case")]
        [InlineData("CamelCase", "camel-case")]
        [InlineData("snake_case", "snake_case")]
        [InlineData("SNAKE_CASE", "snake_case")]
        [InlineData("kebab-case", "kebab-case")]
        [InlineData("KEBAB-CASE", "kebab-case")]
        [InlineData("double  space", "double-space")]
        [InlineData("double__underscore", "double__underscore")]
        [InlineData("double--dash", "double--dash")]
        [InlineData("abc", "abc")]
        [InlineData("abC", "ab-c")]
        [InlineData("aBc", "a-bc")]
        [InlineData("aBC", "a-bc")]
        [InlineData("ABc", "a-bc")]
        [InlineData("ABC", "abc")]
        [InlineData("abc123def456", "abc123def456")]
        [InlineData("abc123Def456", "abc123-def456")]
        [InlineData("abc123DEF456", "abc123-def456")]
        [InlineData("ABC123DEF456", "abc123-def456")]
        [InlineData("ABC123def456", "abc123def456")]
        [InlineData("Abc123def456", "abc123def456")]
        [InlineData("  abc", "abc")]
        [InlineData("abc  ", "abc")]
        [InlineData("  abc  ", "abc")]
        [InlineData("  Abc  ", "abc")]
        [InlineData("  7ab7  ", "7ab7")]
        [InlineData("  abc def  ", "abc-def")]
        [InlineData("  abc  def  ", "abc-def")]
        [InlineData("  abc   def  ", "abc-def")]
        [InlineData("  abc 7ef  ", "abc-7ef")]
        [InlineData("  ab7 def  ", "ab7-def")]
        [InlineData("-abc", "-abc")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "άλφα-βήτα-γάμμα")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("𐐀AbcDef𐐨Abc😀Def𐐀", "𐐀abc-def𐐨abc😀def𐐀")]
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("a%", "a%")]
        [InlineData("-?#_", "-?#_")]
        [InlineData("? ! ?", "?!?")]
        [InlineData("$type", "$type")]
        [InlineData("abc%def", "abc%def")]
        [InlineData("--abc--def--", "--abc--def--")]
        [InlineData("-abcAbc-abc", "-abc-abc-abc")]
        [InlineData("ABC???def", "abc???def")]
        [InlineData("ABCd  - _ -   DE f", "ab-cd-_-de-f")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "a-haaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough",
            "a-towel-it-says-is-about-the-most-massively-useful-thing-an-interstellar-hitchhiker-can-have_partly-it-has-great-practical-value_you-can-wrap-it-around-you-for-warmth-as-you-bound-across-the-cold-moons-of-jaglan-beta_you-can-lie-on-it-on-the-brilliant-marble-sanded-beaches-of-santraginus-v-inhaling-the-heady-sea-vapors_you-can-sleep-under-it-beneath-the-stars-which-shine-so-redly-on-the-desert-world-of-kakrafoon_use-it-to-sail-a-miniraft-down-the-slow-heavy-river-moth_wet-it-for-use-in-hand-to-hand-combat_wrap-it-round-your-head-to-ward-off-noxious-fumes-or-avoid-the-gaze-of-the-ravenous-bugblatter-beast-of-traal-a-mind-bogglingly-stupid-animal_it-assumes-that-if-you-cant-see-it-it-cant-see-you-daft-as-a-brush-but-very-very-ravenous_you-can-wave-your-towel-in-emergencies-as-a-distress-signal-and-of-course-dry-yourself-of-with-it-if-it-still-seems-to-be-clean-enough")]            
        public static void ToKebabLowerCase(string name, string expectedResult)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.KebabCaseLower;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
        }

        [Theory]
        [InlineData("XMLHttpRequest", "XML-HTTP-REQUEST")]
        [InlineData("SHA512HashAlgorithm", "SHA512-HASH-ALGORITHM")]
        [InlineData("i18n", "I18N")]
        [InlineData("I18nPolicy", "I18N-POLICY")]
        [InlineData("7samurai", "7SAMURAI")]
        [InlineData("camelCase", "CAMEL-CASE")]
        [InlineData("CamelCase", "CAMEL-CASE")]
        [InlineData("snake_case", "SNAKE_CASE")]
        [InlineData("SNAKE_CASE", "SNAKE_CASE")]
        [InlineData("kebab-case", "KEBAB-CASE")]
        [InlineData("KEBAB-CASE", "KEBAB-CASE")]
        [InlineData("double  space", "DOUBLE-SPACE")]
        [InlineData("double__underscore", "DOUBLE__UNDERSCORE")]
        [InlineData("double--dash", "DOUBLE--DASH")]
        [InlineData("abc", "ABC")]
        [InlineData("abC", "AB-C")]
        [InlineData("aBc", "A-BC")]
        [InlineData("aBC", "A-BC")]
        [InlineData("ABc", "A-BC")]
        [InlineData("ABC", "ABC")]
        [InlineData("abc123def456", "ABC123DEF456")]
        [InlineData("abc123Def456", "ABC123-DEF456")]
        [InlineData("abc123DEF456", "ABC123-DEF456")]
        [InlineData("ABC123DEF456", "ABC123-DEF456")]
        [InlineData("ABC123def456", "ABC123DEF456")]
        [InlineData("Abc123def456", "ABC123DEF456")]
        [InlineData("  ABC", "ABC")]
        [InlineData("ABC  ", "ABC")]
        [InlineData("  ABC  ", "ABC")]
        [InlineData("  Abc  ", "ABC")]
        [InlineData("  7ab7  ", "7AB7")]
        [InlineData("  ABC def  ", "ABC-DEF")]
        [InlineData("  abc  def  ", "ABC-DEF")]
        [InlineData("  abc   def  ", "ABC-DEF")]
        [InlineData("  abc 7ef  ", "ABC-7EF")]
        [InlineData("  ab7 def  ", "AB7-DEF")]
        [InlineData("-abc", "-ABC")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "ΆΛΦΑ-ΒΉΤΑ-ΓΆΜΜΑ")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("𐐀AbcDef𐐨Abc😀Def𐐀", "𐐀ABC-DEF𐐨ABC😀DEF𐐀")]
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("a%", "A%")]
        [InlineData("-?#_", "-?#_")]
        [InlineData("? ! ?", "?!?")]
        [InlineData("$type", "$TYPE")]
        [InlineData("abc%def", "ABC%DEF")]
        [InlineData("--abc--def--", "--ABC--DEF--")]
        [InlineData("-abcAbc-abc", "-ABC-ABC-ABC")]
        [InlineData("ABC???def", "ABC???DEF")]
        [InlineData("ABCd  - _ -   DE f", "AB-CD-_-DE-F")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData(
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "A-HAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
        [InlineData(
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough",
            "A-TOWEL-IT-SAYS-IS-ABOUT-THE-MOST-MASSIVELY-USEFUL-THING-AN-INTERSTELLAR-HITCHHIKER-CAN-HAVE_PARTLY-IT-HAS-GREAT-PRACTICAL-VALUE_YOU-CAN-WRAP-IT-AROUND-YOU-FOR-WARMTH-AS-YOU-BOUND-ACROSS-THE-COLD-MOONS-OF-JAGLAN-BETA_YOU-CAN-LIE-ON-IT-ON-THE-BRILLIANT-MARBLE-SANDED-BEACHES-OF-SANTRAGINUS-V-INHALING-THE-HEADY-SEA-VAPORS_YOU-CAN-SLEEP-UNDER-IT-BENEATH-THE-STARS-WHICH-SHINE-SO-REDLY-ON-THE-DESERT-WORLD-OF-KAKRAFOON_USE-IT-TO-SAIL-A-MINIRAFT-DOWN-THE-SLOW-HEAVY-RIVER-MOTH_WET-IT-FOR-USE-IN-HAND-TO-HAND-COMBAT_WRAP-IT-ROUND-YOUR-HEAD-TO-WARD-OFF-NOXIOUS-FUMES-OR-AVOID-THE-GAZE-OF-THE-RAVENOUS-BUGBLATTER-BEAST-OF-TRAAL-A-MIND-BOGGLINGLY-STUPID-ANIMAL_IT-ASSUMES-THAT-IF-YOU-CANT-SEE-IT-IT-CANT-SEE-YOU-DAFT-AS-A-BRUSH-BUT-VERY-VERY-RAVENOUS_YOU-CAN-WAVE-YOUR-TOWEL-IN-EMERGENCIES-AS-A-DISTRESS-SIGNAL-AND-OF-COURSE-DRY-YOURSELF-OF-WITH-IT-IF-IT-STILL-SEEMS-TO-BE-CLEAN-ENOUGH")]
        public static void ToKebabUpperCase(string name, string expectedResult)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.KebabCaseUpper;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
        }

        [Theory]
        [InlineData("XMLHttpRequest", "XmlHttpRequest")]
        [InlineData("SHA512HashAlgorithm", "Sha512HashAlgorithm")]
        [InlineData("i18n", "I18n")]
        [InlineData("I18nPolicy", "I18nPolicy")]
        [InlineData("A11y", "A11y")]
        [InlineData("k8s", "K8s")]
        [InlineData("7samurai", "7samurai")]
        [InlineData("camelCase", "CamelCase")]
        [InlineData("PascalCase", "PascalCase")]
        [InlineData("snake_case", "Snake_Case")]
        [InlineData("SNAKE_CASE", "Snake_Case")]
        [InlineData("kebab-case", "Kebab-Case")]
        [InlineData("KEBAB-CASE", "Kebab-Case")]
        [InlineData("double  space", "DoubleSpace")]
        [InlineData("double__underscore", "Double__Underscore")]
        [InlineData("double--dash", "Double--Dash")]
        [InlineData("abc", "Abc")]
        [InlineData("abC", "AbC")]
        [InlineData("aBc", "ABc")]
        [InlineData("aBC", "ABc")]
        [InlineData("ABc", "ABc")]
        [InlineData("ABC", "Abc")]
        [InlineData("abc123def456", "Abc123def456")]
        [InlineData("abc123Def456", "Abc123Def456")]
        [InlineData("abc123DEF456", "Abc123Def456")]
        [InlineData("ABC123DEF456", "Abc123Def456")]
        [InlineData("ABC123def456", "Abc123def456")]
        [InlineData("Abc123def456", "Abc123def456")]
        [InlineData("  abc", "Abc")]
        [InlineData("abc  ", "Abc")]
        [InlineData("  abc  ", "Abc")]
        [InlineData("  Abc  ", "Abc")]
        [InlineData("  7ab7  ", "7ab7")]
        [InlineData("  abc def  ", "AbcDef")]
        [InlineData("  abc  def  ", "AbcDef")]
        [InlineData("  abc   def  ", "AbcDef")]
        [InlineData("  abc 7ef  ", "Abc7ef")]
        [InlineData("  ab7 def  ", "Ab7Def")]
        [InlineData("_abc", "_Abc")]
        [InlineData("", "")]
        [InlineData("😀葛🀄", "😀葛🀄")] // Surrogate pairs
        [InlineData("ΆλφαΒήταΓάμμα", "ΆλφαΒήταΓάμμα")] // Non-ascii letters
        [InlineData("𐐀𐐨𐐨𐐀𐐨𐐨", "𐐀𐐨𐐨𐐀𐐨𐐨")] // Surrogate pair letters don't normalize
        [InlineData("𐐀AbcDef𐐨Abc😀Def𐐀", "𐐀AbcDef𐐨Abc😀Def𐐀")]
        [InlineData("\ude00\ud83d", "\ude00\ud83d")] // Unpaired surrogates
        [InlineData("a%", "A%")]
        [InlineData("_?#-", "_?#-")]
        [InlineData("? ! ?", "?!?")]
        [InlineData("$type", "$Type")]
        [InlineData("abc%def", "Abc%Def")]
        [InlineData("__abc__def__", "__Abc__Def__")]
        [InlineData("_abcAbc_abc", "_AbcAbc_Abc")]
        [InlineData("ABC???def", "Abc???Def")]
        [InlineData("ABCd  - _ -   DE f", "AbCd-_-DeF")]
        [InlineData(
            "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "Aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "aHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            "AHaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
        [InlineData(
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough",
            "ATowelItSaysIsAboutTheMostMassivelyUsefulThingAnInterstellarHitchhikerCanHave_PartlyItHasGreatPracticalValue_YouCanWrapItAroundYouForWarmthAsYouBoundAcrossTheColdMoonsOfJaglanBeta_YouCanLieOnItOnTheBrilliantMarbleSandedBeachesOfSantraginusVInhalingTheHeadySeaVapors_YouCanSleepUnderItBeneathTheStarsWhichShineSoRedlyOnTheDesertWorldOfKakrafoon_UseItToSailAMiniraftDownTheSlowHeavyRiverMoth_WetItForUseInHandToHandCombat_WrapItRoundYourHeadToWardOffNoxiousFumesOrAvoidTheGazeOfTheRavenousBugblatterBeastOfTraalAMindBogglinglyStupidAnimal_ItAssumesThatIfYouCantSeeItItCantSeeYouDaftAsABrushButVeryVeryRavenous_YouCanWaveYourTowelInEmergenciesAsADistressSignalAndOfCourseDryYourselfOfWithItIfItStillSeemsToBeCleanEnough")]
        public static void ToPascalCase(string name, string expectedResult)
        {
            JsonNamingPolicy policy = JsonNamingPolicy.PascalCase;

            string value = policy.ConvertName(name);

            Assert.Equal(expectedResult, value);
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
