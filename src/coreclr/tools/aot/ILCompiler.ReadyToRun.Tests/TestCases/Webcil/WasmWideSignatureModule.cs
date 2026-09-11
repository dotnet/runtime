// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Webcil;

// A wasm function type over 1000 parameters makes the module unloadable, and the ReadyToRun runtime
// responds by silently interpreting the whole assembly. crossgen2 must decline the methods needing
// one, and only those.
public static class WasmWideSignatureModule
{
    // Lowers to 1002 params (1000 + shadow stack pointer + portable entrypoint), over the limit.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TooManyParameters(
        int p000, int p001, int p002, int p003, int p004, int p005, int p006, int p007, int p008, int p009,
        int p010, int p011, int p012, int p013, int p014, int p015, int p016, int p017, int p018, int p019,
        int p020, int p021, int p022, int p023, int p024, int p025, int p026, int p027, int p028, int p029,
        int p030, int p031, int p032, int p033, int p034, int p035, int p036, int p037, int p038, int p039,
        int p040, int p041, int p042, int p043, int p044, int p045, int p046, int p047, int p048, int p049,
        int p050, int p051, int p052, int p053, int p054, int p055, int p056, int p057, int p058, int p059,
        int p060, int p061, int p062, int p063, int p064, int p065, int p066, int p067, int p068, int p069,
        int p070, int p071, int p072, int p073, int p074, int p075, int p076, int p077, int p078, int p079,
        int p080, int p081, int p082, int p083, int p084, int p085, int p086, int p087, int p088, int p089,
        int p090, int p091, int p092, int p093, int p094, int p095, int p096, int p097, int p098, int p099,
        int p100, int p101, int p102, int p103, int p104, int p105, int p106, int p107, int p108, int p109,
        int p110, int p111, int p112, int p113, int p114, int p115, int p116, int p117, int p118, int p119,
        int p120, int p121, int p122, int p123, int p124, int p125, int p126, int p127, int p128, int p129,
        int p130, int p131, int p132, int p133, int p134, int p135, int p136, int p137, int p138, int p139,
        int p140, int p141, int p142, int p143, int p144, int p145, int p146, int p147, int p148, int p149,
        int p150, int p151, int p152, int p153, int p154, int p155, int p156, int p157, int p158, int p159,
        int p160, int p161, int p162, int p163, int p164, int p165, int p166, int p167, int p168, int p169,
        int p170, int p171, int p172, int p173, int p174, int p175, int p176, int p177, int p178, int p179,
        int p180, int p181, int p182, int p183, int p184, int p185, int p186, int p187, int p188, int p189,
        int p190, int p191, int p192, int p193, int p194, int p195, int p196, int p197, int p198, int p199,
        int p200, int p201, int p202, int p203, int p204, int p205, int p206, int p207, int p208, int p209,
        int p210, int p211, int p212, int p213, int p214, int p215, int p216, int p217, int p218, int p219,
        int p220, int p221, int p222, int p223, int p224, int p225, int p226, int p227, int p228, int p229,
        int p230, int p231, int p232, int p233, int p234, int p235, int p236, int p237, int p238, int p239,
        int p240, int p241, int p242, int p243, int p244, int p245, int p246, int p247, int p248, int p249,
        int p250, int p251, int p252, int p253, int p254, int p255, int p256, int p257, int p258, int p259,
        int p260, int p261, int p262, int p263, int p264, int p265, int p266, int p267, int p268, int p269,
        int p270, int p271, int p272, int p273, int p274, int p275, int p276, int p277, int p278, int p279,
        int p280, int p281, int p282, int p283, int p284, int p285, int p286, int p287, int p288, int p289,
        int p290, int p291, int p292, int p293, int p294, int p295, int p296, int p297, int p298, int p299,
        int p300, int p301, int p302, int p303, int p304, int p305, int p306, int p307, int p308, int p309,
        int p310, int p311, int p312, int p313, int p314, int p315, int p316, int p317, int p318, int p319,
        int p320, int p321, int p322, int p323, int p324, int p325, int p326, int p327, int p328, int p329,
        int p330, int p331, int p332, int p333, int p334, int p335, int p336, int p337, int p338, int p339,
        int p340, int p341, int p342, int p343, int p344, int p345, int p346, int p347, int p348, int p349,
        int p350, int p351, int p352, int p353, int p354, int p355, int p356, int p357, int p358, int p359,
        int p360, int p361, int p362, int p363, int p364, int p365, int p366, int p367, int p368, int p369,
        int p370, int p371, int p372, int p373, int p374, int p375, int p376, int p377, int p378, int p379,
        int p380, int p381, int p382, int p383, int p384, int p385, int p386, int p387, int p388, int p389,
        int p390, int p391, int p392, int p393, int p394, int p395, int p396, int p397, int p398, int p399,
        int p400, int p401, int p402, int p403, int p404, int p405, int p406, int p407, int p408, int p409,
        int p410, int p411, int p412, int p413, int p414, int p415, int p416, int p417, int p418, int p419,
        int p420, int p421, int p422, int p423, int p424, int p425, int p426, int p427, int p428, int p429,
        int p430, int p431, int p432, int p433, int p434, int p435, int p436, int p437, int p438, int p439,
        int p440, int p441, int p442, int p443, int p444, int p445, int p446, int p447, int p448, int p449,
        int p450, int p451, int p452, int p453, int p454, int p455, int p456, int p457, int p458, int p459,
        int p460, int p461, int p462, int p463, int p464, int p465, int p466, int p467, int p468, int p469,
        int p470, int p471, int p472, int p473, int p474, int p475, int p476, int p477, int p478, int p479,
        int p480, int p481, int p482, int p483, int p484, int p485, int p486, int p487, int p488, int p489,
        int p490, int p491, int p492, int p493, int p494, int p495, int p496, int p497, int p498, int p499,
        int p500, int p501, int p502, int p503, int p504, int p505, int p506, int p507, int p508, int p509,
        int p510, int p511, int p512, int p513, int p514, int p515, int p516, int p517, int p518, int p519,
        int p520, int p521, int p522, int p523, int p524, int p525, int p526, int p527, int p528, int p529,
        int p530, int p531, int p532, int p533, int p534, int p535, int p536, int p537, int p538, int p539,
        int p540, int p541, int p542, int p543, int p544, int p545, int p546, int p547, int p548, int p549,
        int p550, int p551, int p552, int p553, int p554, int p555, int p556, int p557, int p558, int p559,
        int p560, int p561, int p562, int p563, int p564, int p565, int p566, int p567, int p568, int p569,
        int p570, int p571, int p572, int p573, int p574, int p575, int p576, int p577, int p578, int p579,
        int p580, int p581, int p582, int p583, int p584, int p585, int p586, int p587, int p588, int p589,
        int p590, int p591, int p592, int p593, int p594, int p595, int p596, int p597, int p598, int p599,
        int p600, int p601, int p602, int p603, int p604, int p605, int p606, int p607, int p608, int p609,
        int p610, int p611, int p612, int p613, int p614, int p615, int p616, int p617, int p618, int p619,
        int p620, int p621, int p622, int p623, int p624, int p625, int p626, int p627, int p628, int p629,
        int p630, int p631, int p632, int p633, int p634, int p635, int p636, int p637, int p638, int p639,
        int p640, int p641, int p642, int p643, int p644, int p645, int p646, int p647, int p648, int p649,
        int p650, int p651, int p652, int p653, int p654, int p655, int p656, int p657, int p658, int p659,
        int p660, int p661, int p662, int p663, int p664, int p665, int p666, int p667, int p668, int p669,
        int p670, int p671, int p672, int p673, int p674, int p675, int p676, int p677, int p678, int p679,
        int p680, int p681, int p682, int p683, int p684, int p685, int p686, int p687, int p688, int p689,
        int p690, int p691, int p692, int p693, int p694, int p695, int p696, int p697, int p698, int p699,
        int p700, int p701, int p702, int p703, int p704, int p705, int p706, int p707, int p708, int p709,
        int p710, int p711, int p712, int p713, int p714, int p715, int p716, int p717, int p718, int p719,
        int p720, int p721, int p722, int p723, int p724, int p725, int p726, int p727, int p728, int p729,
        int p730, int p731, int p732, int p733, int p734, int p735, int p736, int p737, int p738, int p739,
        int p740, int p741, int p742, int p743, int p744, int p745, int p746, int p747, int p748, int p749,
        int p750, int p751, int p752, int p753, int p754, int p755, int p756, int p757, int p758, int p759,
        int p760, int p761, int p762, int p763, int p764, int p765, int p766, int p767, int p768, int p769,
        int p770, int p771, int p772, int p773, int p774, int p775, int p776, int p777, int p778, int p779,
        int p780, int p781, int p782, int p783, int p784, int p785, int p786, int p787, int p788, int p789,
        int p790, int p791, int p792, int p793, int p794, int p795, int p796, int p797, int p798, int p799,
        int p800, int p801, int p802, int p803, int p804, int p805, int p806, int p807, int p808, int p809,
        int p810, int p811, int p812, int p813, int p814, int p815, int p816, int p817, int p818, int p819,
        int p820, int p821, int p822, int p823, int p824, int p825, int p826, int p827, int p828, int p829,
        int p830, int p831, int p832, int p833, int p834, int p835, int p836, int p837, int p838, int p839,
        int p840, int p841, int p842, int p843, int p844, int p845, int p846, int p847, int p848, int p849,
        int p850, int p851, int p852, int p853, int p854, int p855, int p856, int p857, int p858, int p859,
        int p860, int p861, int p862, int p863, int p864, int p865, int p866, int p867, int p868, int p869,
        int p870, int p871, int p872, int p873, int p874, int p875, int p876, int p877, int p878, int p879,
        int p880, int p881, int p882, int p883, int p884, int p885, int p886, int p887, int p888, int p889,
        int p890, int p891, int p892, int p893, int p894, int p895, int p896, int p897, int p898, int p899,
        int p900, int p901, int p902, int p903, int p904, int p905, int p906, int p907, int p908, int p909,
        int p910, int p911, int p912, int p913, int p914, int p915, int p916, int p917, int p918, int p919,
        int p920, int p921, int p922, int p923, int p924, int p925, int p926, int p927, int p928, int p929,
        int p930, int p931, int p932, int p933, int p934, int p935, int p936, int p937, int p938, int p939,
        int p940, int p941, int p942, int p943, int p944, int p945, int p946, int p947, int p948, int p949,
        int p950, int p951, int p952, int p953, int p954, int p955, int p956, int p957, int p958, int p959,
        int p960, int p961, int p962, int p963, int p964, int p965, int p966, int p967, int p968, int p969,
        int p970, int p971, int p972, int p973, int p974, int p975, int p976, int p977, int p978, int p979,
        int p980, int p981, int p982, int p983, int p984, int p985, int p986, int p987, int p988, int p989,
        int p990, int p991, int p992, int p993, int p994, int p995, int p996, int p997, int p998, int p999)
    {
        return p000 + p999;
    }

    // Must still be compiled.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int AddIntegers(int left, int right)
    {
        return left + right;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int MultiplyIntegers(int left, int right)
    {
        return left * right;
    }
}
