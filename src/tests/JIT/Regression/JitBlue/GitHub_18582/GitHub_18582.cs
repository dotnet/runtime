// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

// Repro for issue fixed 18582 (also seein in 23309) -- stack overflow when remorphing
// call with a lot of arguments and some CSEs when running with limited stack (as is
// done when CLR is hosted by IIS).

public class GitHub_18582
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(
        int x000, int x001, int x002, int x003, int x004, int x005, int x006, int x007, int x008, int x009,
        int x010, int x011, int x012, int x013, int x014, int x015, int x016, int x017, int x018, int x019,
        int x020, int x021, int x022, int x023, int x024, int x025, int x026, int x027, int x028, int x029,
        int x030, int x031, int x032, int x033, int x034, int x035, int x036, int x037, int x038, int x039,
        int x040, int x041, int x042, int x043, int x044, int x045, int x046, int x047, int x048, int x049,
        int x050, int x051, int x052, int x053, int x054, int x055, int x056, int x057, int x058, int x059,
        int x060, int x061, int x062, int x063, int x064, int x065, int x066, int x067, int x068, int x069,
        int x070, int x071, int x072, int x073, int x074, int x075, int x076, int x077, int x078, int x079,
        int x080, int x081, int x082, int x083, int x084, int x085, int x086, int x087, int x088, int x089,
        int x090, int x091, int x092, int x093, int x094, int x095, int x096, int x097, int x098, int x099,
        int x100, int x101, int x102, int x103, int x104, int x105, int x106, int x107, int x108, int x109,
        int x110, int x111, int x112, int x113, int x114, int x115, int x116, int x117, int x118, int x119,
        int x120, int x121, int x122, int x123, int x124, int x125, int x126, int x127, int x128, int x129,
        int x130, int x131, int x132, int x133, int x134, int x135, int x136, int x137, int x138, int x139,
        int x140, int x141, int x142, int x143, int x144, int x145, int x146, int x147, int x148, int x149,
        int x150, int x151, int x152, int x153, int x154, int x155, int x156, int x157, int x158, int x159,
        int x160, int x161, int x162, int x163, int x164, int x165, int x166, int x167, int x168, int x169,
        int x170, int x171, int x172, int x173, int x174, int x175, int x176, int x177, int x178, int x179,
        int x180, int x181, int x182, int x183, int x184, int x185, int x186, int x187, int x188, int x189,
        int x190, int x191, int x192, int x193, int x194, int x195, int x196, int x197, int x198, int x199,
        int x200, int x201, int x202, int x203, int x204, int x205, int x206, int x207, int x208, int x209,
        int x210, int x211, int x212, int x213, int x214, int x215, int x216, int x217, int x218, int x219,
        int x220, int x221, int x222, int x223, int x224, int x225, int x226, int x227, int x228, int x229,
        int x230, int x231, int x232, int x233, int x234, int x235, int x236, int x237, int x238, int x239,
        int x240, int x241, int x242, int x243, int x244, int x245, int x246, int x247, int x248, int x249,
        int x250, int x251, int x252, int x253, int x254, int x255, int x256, int x257, int x258, int x259,
        int x260, int x261, int x262, int x263, int x264, int x265, int x266, int x267, int x268, int x269,
        int x270, int x271, int x272, int x273, int x274, int x275, int x276, int x277, int x278, int x279,
        int x280, int x281, int x282, int x283, int x284, int x285, int x286, int x287, int x288, int x289,
        int x290, int x291, int x292, int x293, int x294, int x295, int x296, int x297, int x298, int x299,
        int x300, int x301, int x302, int x303, int x304, int x305, int x306, int x307, int x308, int x309,
        int x310, int x311, int x312, int x313, int x314, int x315, int x316, int x317, int x318, int x319,
        int x320, int x321, int x322, int x323, int x324, int x325, int x326, int x327, int x328, int x329,
        int x330, int x331, int x332, int x333, int x334, int x335, int x336, int x337, int x338, int x339,
        int x340, int x341, int x342, int x343, int x344, int x345, int x346, int x347, int x348, int x349,
        int x350, int x351, int x352, int x353, int x354, int x355, int x356, int x357, int x358, int x359,
        int x360, int x361, int x362, int x363, int x364, int x365, int x366, int x367, int x368, int x369,
        int x370, int x371, int x372, int x373, int x374, int x375, int x376, int x377, int x378, int x379,
        int x380, int x381, int x382, int x383, int x384, int x385, int x386, int x387, int x388, int x389,
        int x390, int x391, int x392, int x393, int x394, int x395, int x396, int x397, int x398, int x399,
        int x400, int x401, int x402, int x403, int x404, int x405, int x406, int x407, int x408, int x409,
        int x410, int x411, int x412, int x413, int x414, int x415, int x416, int x417, int x418, int x419,
        int x420, int x421, int x422, int x423, int x424, int x425, int x426, int x427, int x428, int x429,
        int x430, int x431, int x432, int x433, int x434, int x435, int x436, int x437, int x438, int x439,
        int x440, int x441, int x442, int x443, int x444, int x445, int x446, int x447, int x448, int x449,
        int x450, int x451, int x452, int x453, int x454, int x455, int x456, int x457, int x458, int x459,
        int x460, int x461, int x462, int x463, int x464, int x465, int x466, int x467, int x468, int x469,
        int x470, int x471, int x472, int x473, int x474, int x475, int x476, int x477, int x478, int x479,
        int x480, int x481, int x482, int x483, int x484, int x485, int x486, int x487, int x488, int x489,
        int x490, int x491, int x492, int x493, int x494, int x495, int x496, int x497, int x498, int x499,
        int x500, int x501, int x502, int x503, int x504, int x505, int x506, int x507, int x508, int x509,
        int x510, int x511, int x512, int x513, int x514, int x515, int x516, int x517, int x518, int x519,
        int x520, int x521, int x522, int x523, int x524, int x525, int x526, int x527, int x528, int x529,
        int x530, int x531, int x532, int x533, int x534, int x535, int x536, int x537, int x538, int x539,
        int x540, int x541, int x542, int x543, int x544, int x545, int x546, int x547, int x548, int x549,
        int x550, int x551, int x552, int x553, int x554, int x555, int x556, int x557, int x558, int x559,
        int x560, int x561, int x562, int x563, int x564, int x565, int x566, int x567, int x568, int x569,
        int x570, int x571, int x572, int x573, int x574, int x575, int x576, int x577, int x578, int x579,
        int x580, int x581, int x582, int x583, int x584, int x585, int x586, int x587, int x588, int x589,
        int x590, int x591, int x592, int x593, int x594, int x595, int x596, int x597, int x598, int x599,
        int x600, int x601, int x602, int x603, int x604, int x605, int x606, int x607, int x608, int x609,
        int x610, int x611, int x612, int x613, int x614, int x615, int x616, int x617, int x618, int x619,
        int x620, int x621, int x622, int x623, int x624, int x625, int x626, int x627, int x628, int x629,
        int x630, int x631, int x632, int x633, int x634, int x635, int x636, int x637, int x638, int x639,
        int x640, int x641, int x642, int x643, int x644, int x645, int x646, int x647, int x648, int x649,
        int x650, int x651, int x652, int x653, int x654, int x655, int x656, int x657, int x658, int x659,
        int x660, int x661, int x662, int x663, int x664, int x665, int x666, int x667, int x668, int x669,
        int x670, int x671, int x672, int x673, int x674, int x675, int x676, int x677, int x678, int x679,
        int x680, int x681, int x682, int x683, int x684, int x685, int x686, int x687, int x688, int x689,
        int x690, int x691, int x692, int x693, int x694, int x695, int x696, int x697, int x698, int x699,
        int x700, int x701, int x702, int x703, int x704, int x705, int x706, int x707, int x708, int x709,
        int x710, int x711, int x712, int x713, int x714, int x715, int x716, int x717, int x718, int x719,
        int x720, int x721, int x722, int x723, int x724, int x725, int x726, int x727, int x728, int x729,
        int x730, int x731, int x732, int x733, int x734, int x735, int x736, int x737, int x738, int x739,
        int x740, int x741, int x742, int x743, int x744, int x745, int x746, int x747, int x748, int x749,
        int x750, int x751, int x752, int x753, int x754, int x755, int x756, int x757, int x758, int x759,
        int x760, int x761, int x762, int x763, int x764, int x765, int x766, int x767, int x768, int x769,
        int x770, int x771, int x772, int x773, int x774, int x775, int x776, int x777, int x778, int x779,
        int x780, int x781, int x782, int x783, int x784, int x785, int x786, int x787, int x788, int x789,
        int x790, int x791, int x792, int x793, int x794, int x795, int x796, int x797, int x798, int x799,
        int x800, int x801, int x802, int x803, int x804, int x805, int x806, int x807, int x808, int x809,
        int x810, int x811, int x812, int x813, int x814, int x815, int x816, int x817, int x818, int x819,
        int x820, int x821, int x822, int x823, int x824, int x825, int x826, int x827, int x828, int x829,
        int x830, int x831, int x832, int x833, int x834, int x835, int x836, int x837, int x838, int x839,
        int x840, int x841, int x842, int x843, int x844, int x845, int x846, int x847, int x848, int x849)
    {
        s_x = x000 + x099 + x749 + x849;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int q() => s_x + 1;

    internal static void Test()
    {
        int z = s_x;
        Consume(
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(),
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(),
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(),
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(),
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(),
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            q(), q(), q(), q(), q(), q(), q(), q(), q(), q(), 
            z + 1, z + 1, z + 1, z + 1, z + 1, z + 1, z + 1, z + 1, z + 1, z + 1); 
    }

    static int s_x;

    [Fact]
    public static int TestEntryPoint()
    {
        s_x = 1;
        int expected = 8;
        Thread t = new Thread(Test, 512 * 1024);
        t.Start();
        t.Join();

        if (s_x == expected)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine($"FAILED, got {s_x}, expected {expected}");
            return -1;
        }
    }
}
