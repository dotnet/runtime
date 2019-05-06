// Import the utility functionality.

import jobs.generation.*

// The input project name (e.g. dotnet/coreclr)
def project = GithubProject
// The input branch name (e.g. master)
def branch = GithubBranchName
def projectFolder = Utilities.getFolderName(project) + '/' + Utilities.getFolderName(branch)

// Create a folder for JIT stress jobs and associated folder views
folder('jitstress')
Utilities.addStandardFolderView(this, 'jitstress', project)

// Create a folder for testing via illink
folder('illink')
Utilities.addStandardFolderView(this, 'illink', project)

def static getOSGroup(def os) {
    def osGroupMap = ['Ubuntu'      : 'Linux',
                      'Ubuntu16.04' : 'Linux',
                      'Ubuntu16.10' : 'Linux',
                      'RHEL7.2'     : 'Linux',
                      'Debian8.4'   : 'Linux',
                      'Fedora24'    : 'Linux',
                      'CentOS7.1'   : 'Linux',
                      'Tizen'       : 'Linux',
                      'OSX10.12'    : 'OSX',
                      'Windows_NT'  : 'Windows_NT']
    def osGroup = osGroupMap.get(os, null)
    assert osGroup != null : "Could not find os group for ${os}"
    return osGroupMap[os]
}

// We use this class (vs variables) so that the static functions can access data here.
class Constants {

    // We have very limited Windows ARM64 hardware (used for ARM/ARM64 testing) and Linux/arm32 and Linux/arm64 hardware.
    // So only allow certain branches to use it.
    def static LimitedHardwareBranches = [
               'master']

    // Innerloop build OS's
    // The Windows_NT_BuildOnly OS is a way to speed up the Non-Windows builds by avoiding
    // test execution in the build flow runs.  It generates the exact same build
    // as Windows_NT but without running the tests.
    def static osList = [
               'Ubuntu',
               'Debian8.4',
               'OSX10.12',
               'Windows_NT',
               'Windows_NT_BuildOnly',
               'CentOS7.1',
               'RHEL7.2',
               'Ubuntu16.04',
               'Ubuntu16.10',
               'Fedora24',
               'Tizen']

    def static crossList = [
               'Ubuntu',
               'Debian8.4',
               'OSX10.12',
               'Windows_NT',
               'CentOS7.1',
               'RHEL7.2']

    // This is a set of JIT stress modes combined with the set of variables that
    // need to be set to actually enable that stress mode.  The key of the map is the stress mode and
    // the values are the environment variables
    def static jitStressModeScenarios = [
               'minopts'                        : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JITMinOpts' : '1'],
               'tieredcompilation'              : ['COMPlus_TieredCompilation' : '1'], // this can be removed once tiered compilation is on by default
               'no_tiered_compilation'          : ['COMPlus_TieredCompilation' : '0'],
               'no_tiered_compilation_innerloop': ['COMPlus_TieredCompilation' : '0'],
               'forcerelocs'                    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_ForceRelocs' : '1'],
               'jitstress1'                     : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '1'],
               'jitstress2'                     : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2'],
               'jitstress1_tiered'              : ['COMPlus_TieredCompilation' : '1', 'COMPlus_JitStress' : '1'],
               'jitstress2_tiered'              : ['COMPlus_TieredCompilation' : '1', 'COMPlus_JitStress' : '2'],
               'jitstressregs1'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '1'],
               'jitstressregs2'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '2'],
               'jitstressregs3'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '3'],
               'jitstressregs4'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '4'],
               'jitstressregs8'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '8'],
               'jitstressregs0x10'              : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x10'],
               'jitstressregs0x80'              : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x80'],
               'jitstressregs0x1000'            : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x1000'],
               'jitstress2_jitstressregs1'      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '1'],
               'jitstress2_jitstressregs2'      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '2'],
               'jitstress2_jitstressregs3'      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '3'],
               'jitstress2_jitstressregs4'      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '4'],
               'jitstress2_jitstressregs8'      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '8'],
               'jitstress2_jitstressregs0x10'   : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x10'],
               'jitstress2_jitstressregs0x80'   : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x80'],
               'jitstress2_jitstressregs0x1000' : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x1000'],
               'tailcallstress'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_TailcallStress' : '1'],
               'jitsse2only'                    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableAVX' : '0', 'COMPlus_EnableSSE3_4' : '0'],
               'jitnosimd'                      : ['COMPlus_TieredCompilation' : '0', 'COMPlus_FeatureSIMD' : '0'],
               'jitincompletehwintrinsic'       : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableIncompleteISAClass' : '1'],
               'jitx86hwintrinsicnoavx'         : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableAVX' : '0'], // testing the legacy SSE encoding
               'jitx86hwintrinsicnoavx2'        : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableAVX2' : '0'], // testing SNB/IVB
               'jitx86hwintrinsicnosimd'        : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_FeatureSIMD' : '0'], // match "jitnosimd", may need to remove after decoupling HW intrinsic from FeatureSIMD
               'jitnox86hwintrinsic'            : ['COMPlus_TieredCompilation' : '0', 'COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableSSE' : '0' , 'COMPlus_EnableSSE2' : '0' , 'COMPlus_EnableSSE3' : '0' , 'COMPlus_EnableSSSE3' : '0' , 'COMPlus_EnableSSE41' : '0' , 'COMPlus_EnableSSE42' : '0' , 'COMPlus_EnableAVX' : '0' , 'COMPlus_EnableAVX2' : '0' , 'COMPlus_EnableAES' : '0' , 'COMPlus_EnableBMI1' : '0' , 'COMPlus_EnableBMI2' : '0' , 'COMPlus_EnableFMA' : '0' , 'COMPlus_EnableLZCNT' : '0' , 'COMPlus_EnablePCLMULQDQ' : '0' , 'COMPlus_EnablePOPCNT' : '0'],
               'corefx_baseline'                : ['COMPlus_TieredCompilation' : '0'], // corefx baseline
               'corefx_minopts'                 : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JITMinOpts' : '1'],
               'corefx_tieredcompilation'       : ['COMPlus_TieredCompilation' : '1'],  // this can be removed once tiered compilation is on by default
               'corefx_jitstress1'              : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '1'],
               'corefx_jitstress2'              : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStress' : '2'],
               'corefx_jitstressregs1'          : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '1'],
               'corefx_jitstressregs2'          : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '2'],
               'corefx_jitstressregs3'          : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '3'],
               'corefx_jitstressregs4'          : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '4'],
               'corefx_jitstressregs8'          : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '8'],
               'corefx_jitstressregs0x10'       : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x10'],
               'corefx_jitstressregs0x80'       : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x80'],
               'corefx_jitstressregs0x1000'     : ['COMPlus_TieredCompilation' : '0', 'COMPlus_JitStressRegs' : '0x1000'],
               'gcstress0x3'                    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0x3'],
               'gcstress0xc'                    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC'],
               'zapdisable'                     : ['COMPlus_TieredCompilation' : '0', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0'],
               'heapverify1'                    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_HeapVerify' : '1'],
               'gcstress0xc_zapdisable'             : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0'],
               'gcstress0xc_zapdisable_jitstress2'  : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_zapdisable_heapverify1' : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0', 'COMPlus_HeapVerify' : '1'],
               'gcstress0xc_jitstress1'             : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_JitStress'  : '1'],
               'gcstress0xc_jitstress2'             : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_minopts_heapverify1'    : ['COMPlus_TieredCompilation' : '0', 'COMPlus_GCStress' : '0xC', 'COMPlus_JITMinOpts' : '1', 'COMPlus_HeapVerify' : '1']
    ]

    // This is a set of ReadyToRun stress scenarios
    def static r2rStressScenarios = [
               'r2r_jitstress1'             : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStress": "1"],
               'r2r_jitstress2'             : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStress": "2"],
               'r2r_jitstress1_tiered'      : ['COMPlus_TieredCompilation' : '1', "COMPlus_JitStress": "1"],
               'r2r_jitstress2_tiered'      : ['COMPlus_TieredCompilation' : '1', "COMPlus_JitStress": "2"],
               'r2r_jitstressregs1'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "1"],
               'r2r_jitstressregs2'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "2"],
               'r2r_jitstressregs3'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "3"],
               'r2r_jitstressregs4'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "4"],
               'r2r_jitstressregs8'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "8"],
               'r2r_jitstressregs0x10'      : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "0x10"],
               'r2r_jitstressregs0x80'      : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "0x80"],
               'r2r_jitstressregs0x1000'    : ['COMPlus_TieredCompilation' : '0', "COMPlus_JitStressRegs": "0x1000"],
               'r2r_jitminopts'             : ['COMPlus_TieredCompilation' : '0', "COMPlus_JITMinOpts": "1"], 
               'r2r_jitforcerelocs'         : ['COMPlus_TieredCompilation' : '0', "COMPlus_ForceRelocs": "1"],
               'r2r_gcstress15'             : ['COMPlus_TieredCompilation' : '0', "COMPlus_GCStress": "0xF"],
               'r2r_no_tiered_compilation'  : ['COMPlus_TieredCompilation' : '0'],
    ]

    // This is the basic set of scenarios
    def static basicScenarios = [
               'innerloop',
               'normal',
               'ilrt',
               'r2r',
               'longgc',
               'gcsimulator',
               // 'jitdiff', // jitdiff is currently disabled, until someone spends the effort to make it fully work
               'standalone_gc',
               'gc_reliability_framework',
               'illink',
               'corefx_innerloop',
               'crossgen_comparison',
               'pmi_asm_diffs']

    def static allScenarios = basicScenarios + r2rStressScenarios.keySet() + jitStressModeScenarios.keySet()

    // Valid PR trigger combinations.
    def static prTriggeredValidInnerLoopCombos = [
        'Windows_NT': [
            'arm': [
                'Checked'
            ],
            'arm64': [
                'Checked'
            ]
        ],
        'Windows_NT_BuildOnly': [
            'arm': [
                'Checked'
            ], 
        ]
    ]

    // A set of scenarios that are valid for arm/arm64 tests run on hardware. This is a map from valid scenario name
    // to Tests.lst file categories to exclude.
    //
    // This list should contain a subset of the scenarios from `allScenarios`. Please keep this in the same order as that,
    // and with the same values, with some commented out, for easier maintenance.
    //
    // Note that some scenarios that are commented out should be enabled, but haven't yet been.
    //
    def static validArmWindowsScenarios = [
               'innerloop',
               'normal',
               // 'ilrt'
               'r2r',
               // 'longgc'
               // 'formatting'
               // 'gcsimulator'
               // 'jitdiff'
               // 'standalone_gc'
               // 'gc_reliability_framework'
               // 'illink'
               // 'corefx_innerloop'
               // 'crossgen_comparison'
               // 'pmi_asm_diffs'
               'r2r_jitstress1',
               'r2r_jitstress2',
               'r2r_jitstress1_tiered',
               'r2r_jitstress2_tiered',
               'r2r_jitstressregs1',
               'r2r_jitstressregs2',
               'r2r_jitstressregs3',
               'r2r_jitstressregs4',
               'r2r_jitstressregs8',
               'r2r_jitstressregs0x10',
               'r2r_jitstressregs0x80',
               'r2r_jitstressregs0x1000',
               'r2r_jitminopts',
               'r2r_jitforcerelocs',
               'r2r_gcstress15',
               'r2r_no_tiered_compilation',
               'minopts',
               'tieredcompilation',
               'no_tiered_compilation',
               'no_tiered_compilation_innerloop',
               'forcerelocs',
               'jitstress1',
               'jitstress2',
               'jitstress1_tiered',
               'jitstress2_tiered',
               'jitstressregs1',
               'jitstressregs2',
               'jitstressregs3',
               'jitstressregs4',
               'jitstressregs8',
               'jitstressregs0x10',
               'jitstressregs0x80',
               'jitstressregs0x1000',
               'jitstress2_jitstressregs1',
               'jitstress2_jitstressregs2',
               'jitstress2_jitstressregs3',
               'jitstress2_jitstressregs4',
               'jitstress2_jitstressregs8',
               'jitstress2_jitstressregs0x10',
               'jitstress2_jitstressregs0x80',
               'jitstress2_jitstressregs0x1000',
               'tailcallstress',
               // 'jitsse2only'                     // Only relevant to xarch
               'jitnosimd',                         // Only interesting on platforms where SIMD support exists.
               // 'jitincompletehwintrinsic'
               // 'jitx86hwintrinsicnoavx'
               // 'jitx86hwintrinsicnoavx2'
               // 'jitx86hwintrinsicnosimd'
               // 'jitnox86hwintrinsic'
               'corefx_baseline',                   // corefx tests don't use smarty
               'corefx_minopts',                    // corefx tests don't use smarty
               'corefx_tieredcompilation',          // corefx tests don't use smarty
               'corefx_jitstress1',                 // corefx tests don't use smarty
               'corefx_jitstress2',                 // corefx tests don't use smarty
               'corefx_jitstressregs1',             // corefx tests don't use smarty
               'corefx_jitstressregs2',             // corefx tests don't use smarty
               'corefx_jitstressregs3',             // corefx tests don't use smarty
               'corefx_jitstressregs4',             // corefx tests don't use smarty
               'corefx_jitstressregs8',             // corefx tests don't use smarty
               'corefx_jitstressregs0x10',          // corefx tests don't use smarty
               'corefx_jitstressregs0x80',          // corefx tests don't use smarty
               'corefx_jitstressregs0x1000',        // corefx tests don't use smarty
               'gcstress0x3',
               'gcstress0xc',
               'zapdisable',
               'heapverify1',
               'gcstress0xc_zapdisable',
               'gcstress0xc_zapdisable_jitstress2',
               'gcstress0xc_zapdisable_heapverify1',
               'gcstress0xc_jitstress1',
               'gcstress0xc_jitstress2',
               'gcstress0xc_minopts_heapverify1',

               //
               // NOTE: the following scenarios are not defined in the 'allScenarios' list! Is this a bug?
               //

               'minopts_zapdisable',
               'gcstress0x3_jitstress1',
               'gcstress0x3_jitstress2',
               'gcstress0x3_jitstressregs1',
               'gcstress0x3_jitstressregs2',
               'gcstress0x3_jitstressregs3',
               'gcstress0x3_jitstressregs4',
               'gcstress0x3_jitstressregs8',
               'gcstress0x3_jitstressregs0x10',
               'gcstress0x3_jitstressregs0x80',
               'gcstress0x3_jitstressregs0x1000',
               'gcstress0xc_jitstressregs1',
               'gcstress0xc_jitstressregs2',
               'gcstress0xc_jitstressregs3',
               'gcstress0xc_jitstressregs4',
               'gcstress0xc_jitstressregs8',
               'gcstress0xc_jitstressregs0x10',
               'gcstress0xc_jitstressregs0x80',
               'gcstress0xc_jitstressregs0x1000'
    ]
  
    def static validLinuxArmScenarios = [
               'innerloop',
               'normal',
               // 'ilrt'
               'r2r',
               // 'longgc'
               // 'formatting'
               // 'gcsimulator'
               // 'jitdiff'
               // 'standalone_gc'
               // 'gc_reliability_framework'
               // 'illink'
               // 'corefx_innerloop'
               'crossgen_comparison',
               'pmi_asm_diffs',
               'r2r_jitstress1',
               'r2r_jitstress2',
               'r2r_jitstress1_tiered',
               'r2r_jitstress2_tiered',
               'r2r_jitstressregs1',
               'r2r_jitstressregs2',
               'r2r_jitstressregs3',
               'r2r_jitstressregs4',
               'r2r_jitstressregs8',
               'r2r_jitstressregs0x10',
               'r2r_jitstressregs0x80',
               'r2r_jitstressregs0x1000',
               'r2r_jitminopts',
               'r2r_jitforcerelocs',
               'r2r_gcstress15',
               'r2r_no_tiered_compilation',
               'minopts',
               'tieredcompilation',
               'no_tiered_compilation',
               'no_tiered_compilation_innerloop',
               'forcerelocs',
               'jitstress1',
               'jitstress2',
               'jitstress1_tiered',
               'jitstress2_tiered',
               'jitstressregs1',
               'jitstressregs2',
               'jitstressregs3',
               'jitstressregs4',
               'jitstressregs8',
               'jitstressregs0x10',
               'jitstressregs0x80',
               'jitstressregs0x1000',
               'jitstress2_jitstressregs1',
               'jitstress2_jitstressregs2',
               'jitstress2_jitstressregs3',
               'jitstress2_jitstressregs4',
               'jitstress2_jitstressregs8',
               'jitstress2_jitstressregs0x10',
               'jitstress2_jitstressregs0x80',
               'jitstress2_jitstressregs0x1000',
               'tailcallstress',
               // 'jitsse2only'                          // Only relevant to xarch
               // 'jitnosimd'                            // Only interesting on platforms where SIMD support exists.
               // 'jitincompletehwintrinsic'
               // 'jitx86hwintrinsicnoavx'
               // 'jitx86hwintrinsicnoavx2'
               // 'jitx86hwintrinsicnosimd'
               // 'jitnox86hwintrinsic'
               'corefx_baseline',
               'corefx_minopts',
               'corefx_tieredcompilation',
               'corefx_jitstress1',
               'corefx_jitstress2',
               'corefx_jitstressregs1',
               'corefx_jitstressregs2',
               'corefx_jitstressregs3',
               'corefx_jitstressregs4',
               'corefx_jitstressregs8',
               'corefx_jitstressregs0x10',
               'corefx_jitstressregs0x80',
               'corefx_jitstressregs0x1000',
               'gcstress0x3',
               'gcstress0xc',
               'zapdisable',
               'heapverify1',
               'gcstress0xc_zapdisable',
               'gcstress0xc_zapdisable_jitstress2',
               'gcstress0xc_zapdisable_heapverify1',
               'gcstress0xc_jitstress1',
               'gcstress0xc_jitstress2',
               'gcstress0xc_minopts_heapverify1'
    ]

    def static validLinuxArm64Scenarios = [
               'innerloop',
               'normal',
               // 'ilrt'
               'r2r',
               // 'longgc'
               // 'formatting'
               // 'gcsimulator'
               // 'jitdiff'
               // 'standalone_gc'
               // 'gc_reliability_framework'
               // 'illink'
               // 'corefx_innerloop'
               'crossgen_comparison',
               'pmi_asm_diffs',
               'r2r_jitstress1',
               'r2r_jitstress2',
               'r2r_jitstress1_tiered',
               'r2r_jitstress2_tiered',
               'r2r_jitstressregs1',
               'r2r_jitstressregs2',
               'r2r_jitstressregs3',
               'r2r_jitstressregs4',
               'r2r_jitstressregs8',
               'r2r_jitstressregs0x10',
               'r2r_jitstressregs0x80',
               'r2r_jitstressregs0x1000',
               'r2r_jitminopts',
               'r2r_jitforcerelocs',
               'r2r_gcstress15',
               'r2r_no_tiered_compilation',
               'minopts',
               'tieredcompilation',
               'no_tiered_compilation',
               'no_tiered_compilation_innerloop',
               'forcerelocs',
               'jitstress1',
               'jitstress2',
               'jitstress1_tiered',
               'jitstress2_tiered',
               'jitstressregs1',
               'jitstressregs2',
               'jitstressregs3',
               'jitstressregs4',
               'jitstressregs8',
               'jitstressregs0x10',
               'jitstressregs0x80',
               'jitstressregs0x1000',
               'jitstress2_jitstressregs1',
               'jitstress2_jitstressregs2',
               'jitstress2_jitstressregs3',
               'jitstress2_jitstressregs4',
               'jitstress2_jitstressregs8',
               'jitstress2_jitstressregs0x10',
               'jitstress2_jitstressregs0x80',
               'jitstress2_jitstressregs0x1000',
               'tailcallstress',
               // 'jitsse2only'                         // Only relevant to xarch
               'jitnosimd',                             // Only interesting on platforms where SIMD support exists.
               // 'jitincompletehwintrinsic'
               // 'jitx86hwintrinsicnoavx'
               // 'jitx86hwintrinsicnoavx2'
               // 'jitx86hwintrinsicnosimd'
               // 'jitnox86hwintrinsic'
               'corefx_baseline',
               'corefx_minopts',
               'corefx_tieredcompilation',
               'corefx_jitstress1',
               'corefx_jitstress2',
               'corefx_jitstressregs1',
               'corefx_jitstressregs2',
               'corefx_jitstressregs3',
               'corefx_jitstressregs4',
               'corefx_jitstressregs8',
               'corefx_jitstressregs0x10',
               'corefx_jitstressregs0x80',
               'corefx_jitstressregs0x1000',
               'gcstress0x3',
               'gcstress0xc',
               'zapdisable',
               'heapverify1',
               'gcstress0xc_zapdisable',
               'gcstress0xc_zapdisable_jitstress2',
               'gcstress0xc_zapdisable_heapverify1',
               'gcstress0xc_jitstress1',
               'gcstress0xc_jitstress2',
               'gcstress0xc_minopts_heapverify1'
    ]

    def static configurationList = ['Debug', 'Checked', 'Release']

    // This is the set of architectures
    // Some of these are pseudo-architectures:
    //    armem -- ARM builds/runs using an emulator. Used for Tizen runs.
    def static architectureList = ['arm', 'armem', 'arm64', 'x64', 'x86']

    // This set of architectures that cross build on Windows and run on Windows ARM64 hardware.
    def static armWindowsCrossArchitectureList = ['arm', 'arm64']
}

// **************************************************************
// Create some specific views
// 
// These aren't using the Utilities.addStandardFolderView() function, because that creates
// views based on a single regular expression. These views will be generated by adding a
// specific set of jobs to them.
//
// Utilities.addStandardFolderView() also creates a lot of additional stuff around the
// view, like "Build Statistics", "Job Statistics", "Unstable Jobs". Until it is determined
// those are required, don't add them (which simplifies the view pages, as well).
// **************************************************************

class Views {
    def static MergeJobView = null
    def static PeriodicJobView = null
    def static ArchitectureViews = [:]
    def static OSViews = [:]
}

// MergeJobView: include all jobs that execute when a PR change is merged.
Views.MergeJobView = listView('Merge') {
    recurse()
    columns {
        status()
        weather()
        name()
        lastSuccess()
        lastFailure()
        lastDuration()
        buildButton()
    }
}

// PeriodicJobView: include all jobs that execute on a schedule
Views.PeriodicJobView = listView('Periodic') {
    recurse()
    columns {
        status()
        weather()
        name()
        lastSuccess()
        lastFailure()
        lastDuration()
        buildButton()
    }
}

// Create a view for non-PR jobs for each architecture.
Constants.architectureList.each { architecture ->
    Views.ArchitectureViews[architecture] = listView(architecture) {
        recurse()
        columns {
            status()
            weather()
            name()
            lastSuccess()
            lastFailure()
            lastDuration()
            buildButton()
        }
    }
}

// Create a view for non-PR jobs for each OS.
Constants.osList.each { os ->
    // Don't create one for the special 'Windows_NT_BuildOnly'
    if (os == 'Windows_NT_BuildOnly') {
        return
    }
    Views.OSViews[os] = listView(os) {
        recurse()
        columns {
            status()
            weather()
            name()
            lastSuccess()
            lastFailure()
            lastDuration()
            buildButton()
        }
    }
}

def static addToMergeView(def job) {
    Views.MergeJobView.with {
        jobs {
            name(job.name)
        }
    }
}

def static addToPeriodicView(def job) {
    Views.PeriodicJobView.with {
        jobs {
            name(job.name)
        }
    }
}

def static addToViews(def job, def isFlowJob, def isPR, def architecture, def os, def configuration, def scenario) {
    if (isPR) {
        // No views want PR jobs currently.
        return
    }

    // We don't want to include in view any job that is only used by a flow job (because we want the views to have only the
    // "top-level" jobs. Build only jobs are such jobs.
    if (os == 'Windows_NT_BuildOnly') {
        return
    }

    if (!isFlowJob) {
        // For non-flow jobs, which ones are only used by flow jobs?
        if ((architecture == 'arm') || (architecture == 'arm64')) {
            if (isCoreFxScenario(scenario)) {
                // We have corefx-specific scenario builds for each of the runs, but these are driven by flow jobs.
                return
            }

            // We're left with the basic normal/innerloop builds. We might want to remove these from the views also, if desired.
            // However, there are a few, like the Debug Build, that is build only, not "Build and Test", that we should leave.
        }
    }

    // Add to architecture view.
    Views.ArchitectureViews[architecture].with {
        jobs {
            name(job.name)
        }
    }

    // Add to OS view.
    Views.OSViews[os].with {
        jobs {
            name(job.name)
        }
    }
}

def static addPeriodicTriggerHelper(def job, String cronString, boolean alwaysRuns = false) {
    addToPeriodicView(job)
    Utilities.addPeriodicTrigger(job, cronString, alwaysRuns)
}

def static addGithubPushTriggerHelper(def job) {
    // Disable all Push trigger jobs. All jobs will need to be requested.
    // addToMergeView(job)
    // Utilities.addGithubPushTrigger(job)
}


def static setMachineAffinity(def job, def os, def architecture, def options = null) {
    assert os instanceof String
    assert architecture instanceof String

    def armArches = ['arm', 'armem', 'arm64']

    if (!(architecture in armArches)) {
        assert options == null
        Utilities.setMachineAffinity(job, os, 'latest-or-auto')

        return
    }

    // This is an arm(64) job.
    //
    // There are several options.
    //
    // Windows_NT
    // 
    // Arm32 (Build) -> latest-arm64
    //       |-> os == "Windows_NT" && (architecture == "arm") && options['use_arm64_build_machine'] == true
    // Arm32 (Test)  -> arm64-windows_nt
    //       |-> os == "Windows_NT" && (architecture == "arm") && options['use_arm64_build_machine'] == false
    //
    // Arm64 (Build) -> latest-arm64
    //       |-> os == "Windows_NT" && architecture == "arm64" && options['use_arm64_build_machine'] == true
    // Arm64 (Test)  -> arm64-windows_nt
    //       |-> os == "Windows_NT" && architecture == "arm64" && options['use_arm64_build_machine'] == false
    //
    // Ubuntu
    //
    // Arm32 emulator (Build, Test) -> arm-cross-latest
    //       |-> os == "Tizen" && (architecture == "armem")
    //
    // Arm32 hardware (Flow) -> Ubuntu 16.04 latest-or-auto (don't use limited arm hardware)
    //       |-> os == "Ubuntu" && (architecture == "arm") && options['is_flow_job'] == true
    // Arm32 hardware (Build) -> Ubuntu 16.04 latest-or-auto
    //       |-> os == "Ubuntu" && (architecture == "arm") && options['is_build_job'] == true
    // Arm32 hardware (Test) -> Helix ubuntu.1404.arm32.open queue
    //       |-> os == "Ubuntu" && (architecture == "arm")
    //
    // Arm64 (Build) -> arm64-cross-latest
    //       |-> os != "Windows_NT" && architecture == "arm64" && options['is_build_job'] == true
    // Arm64 (Test) -> Helix Ubuntu.1604.Arm64.Iron.Open queue
    //       |-> os != "Windows_NT" && architecture == "arm64"
    //
    // Note: we are no longer using Jenkins tags "arm64-huge-page-size", "arm64-small-page-size".
    // Support for Linux arm64 large page size has been removed for now, as it wasn't being used.
    //
    // Note: we are no longer using Jenkins tag 'latest-arm64' for arm/arm64 Windows build machines. Instead,
    // we are using public VS2017 arm/arm64 tools in a VM from Helix.

    // This has to be a arm arch
    assert architecture in armArches
    if (os == "Windows_NT") {
        // arm32/arm64 Windows jobs share the same machines for now
        def isBuild = options['use_arm64_build_machine'] == true

        if (isBuild == true) {
            job.with {
                label('Windows.10.Amd64.ClientRS4.DevEx.Open')
            }
        } else {
            Utilities.setMachineAffinity(job, 'windows.10.arm64.open')
        }
    } else {
        assert os != 'Windows_NT'

        if (architecture == 'armem') {
            // arm emulator (Tizen). Build and test on same machine,
            // using Docker.
            assert os == 'Tizen'
            Utilities.setMachineAffinity(job, 'Ubuntu', 'arm-cross-latest')
        }
        else {
            // arm/arm64 Ubuntu on hardware.
            assert architecture == 'arm' || architecture == 'arm64'
            def isFlow  = (options != null) && (options['is_flow_job'] == true)
            def isBuild = (options != null) && (options['is_build_job'] == true)
            if (isFlow || isBuild) {
                // arm/arm64 Ubuntu build machine. Build uses docker, so the actual host OS is not
                // very important. Therefore, use latest or auto. Flow jobs don't need to use arm hardware.
                Utilities.setMachineAffinity(job, 'Ubuntu16.04', 'latest-or-auto')
            } else {
                // arm/arm64 Ubuntu test machine. Specify the Helix queue name here.
                if (architecture == 'arm64') {
                    assert os == 'Ubuntu16.04'
                    job.with {
                        label('Ubuntu.1604.Arm64.Iron.Open')
                    }
                }
                else {
                    assert os == 'Ubuntu'
                    job.with {
                        label('ubuntu.1404.arm32.open')
                    }
                }
            }
        }
    }
}

// setJobMachineAffinity: compute the machine affinity options for a job,
// then set the job with those affinity options.
def static setJobMachineAffinity(def architecture, def os, def isBuildJob, def isTestJob, def isFlowJob, def job)
{
    assert (isBuildJob  && !isTestJob && !isFlowJob) ||
           (!isBuildJob && isTestJob  && !isFlowJob) ||
           (!isBuildJob && !isTestJob && isFlowJob)

    def affinityOptions = null
    def affinityArchitecture = architecture

    if (os == "Windows_NT") {
        if (architecture in Constants.armWindowsCrossArchitectureList) {
            if (isBuildJob) {
                affinityOptions = [ "use_arm64_build_machine" : true ]
            } else if (isTestJob) {
                affinityOptions = [ "use_arm64_build_machine" : false ]
            } else if (isFlowJob) {
                // For the flow jobs set the machine affinity as x64
                affinityArchitecture = 'x64'
            }
        }
    }
    else {
        if ((architecture == 'arm64') || (architecture == 'arm')) {
            if (isBuildJob) {
                affinityOptions = ['is_build_job': true]
            } else if (isFlowJob) {
                affinityOptions = ['is_flow_job': true]
            }
        }
    }

    setMachineAffinity(job, os, affinityArchitecture, affinityOptions)
}

def static isGCStressRelatedTesting(def scenario) {
    // The 'r2r_gcstress15' scenario is a basic scenario.
    // Detect it and make it a GCStress related.
    if (scenario == 'r2r_gcstress15')
    {
        return true;
    }

    def gcStressTestEnvVars = [ 'COMPlus_GCStress', 'COMPlus_ZapDisable', 'COMPlus_HeapVerify']
    def scenarioName = scenario.toLowerCase()
    def isGCStressTesting = false
    Constants.jitStressModeScenarios[scenario].each{ k, v ->
        if (k in gcStressTestEnvVars) {
            isGCStressTesting = true;
        }
    }
    return isGCStressTesting
}

def static isCoreFxScenario(def scenario) {
    def corefx_prefix = 'corefx_'
    if (scenario.length() < corefx_prefix.length()) {
        return false
    }
    return scenario.substring(0,corefx_prefix.length()) == corefx_prefix
}

def static isR2RBaselineScenario(def scenario) {
    return (scenario == 'r2r')
}

def static isR2RStressScenario(def scenario) {
    return Constants.r2rStressScenarios.containsKey(scenario)
}

def static isR2RScenario(def scenario) {
    return isR2RBaselineScenario(scenario) || isR2RStressScenario(scenario)
}

def static isJitStressScenario(def scenario) {
    return Constants.jitStressModeScenarios.containsKey(scenario)
}

def static isLongGc(def scenario) {
    return (scenario == 'longgc' || scenario == 'gcsimulator')
}

def static isJitDiff(def scenario) {
    return (scenario == 'jitdiff')
}

def static isGcReliabilityFramework(def scenario) {
    return (scenario == 'gc_reliability_framework')
}

def static isArmWindowsScenario(def scenario) {
    return Constants.validArmWindowsScenarios.contains(scenario)
}

def static isValidPrTriggeredInnerLoopJob(os, architecture, configuration, isBuildOnly) {
    if (isBuildOnly == true) {
        os = 'Windows_NT_BuildOnly'
    }

    def validOsPrTriggerArchConfigs = Constants.prTriggeredValidInnerLoopCombos[os]
    if (validOsPrTriggerArchConfigs != null) {
        def validOsPrTriggerConfigs = validOsPrTriggerArchConfigs[architecture]
        if (validOsPrTriggerConfigs != null) {
            if (configuration in validOsPrTriggerConfigs) {
                return true
            }
        }
    }

    return false
}

// This means the job builds and runs the 'innerloop' test set. This does not mean the job is 
// scheduled with a default PR trigger despite the correlation being true at the moment.
def static isInnerloopTestScenario(def scenario) {
    return (scenario == 'innerloop' || scenario == 'no_tiered_compilation_innerloop')
}

def static isCrossGenComparisonScenario(def scenario) {
    return (scenario == 'crossgen_comparison')
}

def static shouldGenerateCrossGenComparisonJob(def os, def architecture, def configuration, def scenario) {
    assert isCrossGenComparisonScenario(scenario)
    return ((os == 'Ubuntu' && architecture == 'arm') || (os == 'Ubuntu16.04' && architecture == 'arm64')) && (configuration == 'Checked' || configuration == 'Release')
}

def static getFxBranch(def branch) {
    def fxBranch = branch
    // Map 'dev/unix_test_workflow' to 'master' so we can test CoreFX jobs in the CoreCLR dev/unix_test_workflow
    // branch even though CoreFX doesn't have such a branch.
    if (branch == 'dev/unix_test_workflow') {
        fxBranch = 'master'
    }
    return fxBranch
}

def static setJobTimeout(newJob, isPR, architecture, configuration, scenario, isBuildOnly) {
    // 2 hours (120 minutes) is the default timeout
    def timeout = 120

    if (!isInnerloopTestScenario(scenario)) {
        // Pri-1 test builds take a long time (see calculateBuildCommands()). So up the Pri-1 build jobs timeout.
        timeout = 240
    }

    if (!isBuildOnly) {
        // Note that these can only increase, never decrease, the Pri-1 timeout possibly set above.
        if (isGCStressRelatedTesting(scenario)) {
            timeout = 4320
        }
        else if (isCoreFxScenario(scenario)) {
            timeout = 360
        }
        else if (isJitStressScenario(scenario)) {
            timeout = 300
        }
        else if (isR2RBaselineScenario(scenario)) {
            timeout = 240
        }
        else if (isLongGc(scenario)) {
            timeout = 1440
        }
        else if (isJitDiff(scenario)) {
            timeout = 240
        }
        else if (isGcReliabilityFramework(scenario)) {
            timeout = 1440
        }
        else if (architecture == 'armem' || architecture == 'arm64') {
            timeout = 240
        }

        if (architecture == 'arm') {
            // ARM32 machines are particularly slow.
            timeout += 120
        }
    }

    if (configuration == 'Debug') {
        // Debug runs can be very slow. Add an hour.
        timeout += 60
    }

    // If we've changed the timeout from the default, set it in the job.

    if (timeout != 120) {
        Utilities.setJobTimeout(newJob, timeout)
    }
}

def static getJobFolder(def scenario) {
    if (isJitStressScenario(scenario) || isR2RStressScenario(scenario)) {
        return 'jitstress'
    }
    if (scenario == 'illink') {
        return 'illink'
    }
    return ''
}

def static getStressModeDisplayName(def scenario) {
    def displayStr = ''
    Constants.jitStressModeScenarios[scenario].each{ k, v ->
        def prefixLength = 'COMPlus_'.length()
        if (k.length() >= prefixLength) {
            def modeName = k.substring(prefixLength, k.length())
            if (displayStr != '') {
                // Separate multiple variables with a space.
                displayStr += ' '
            }
            displayStr += modeName + '=' + v
        }
    }

    if (isCoreFxScenario(scenario)) {
        displayStr = ('CoreFx ' + displayStr).trim()
    }

    return displayStr
}

def static getR2RDisplayName(def scenario) {
    // Assume the scenario name is one from the r2rStressScenarios dict, and remove its "r2r_" prefix.
    def displayStr = scenario
    def prefixLength = 'r2r_'.length()
    if (displayStr.length() >= prefixLength) {
        displayStr = "R2R " + displayStr.substring(prefixLength, displayStr.length())
    } else if (scenario == 'r2r') {
        displayStr = "R2R"
    }
    return displayStr
}

def static getScenarioDisplayString(def scenario) {
    switch (scenario) {
        case 'innerloop':
            return "Innerloop Build and Test"

        case 'no_tiered_compilation_innerloop':
            def displayStr = getStressModeDisplayName(scenario)
            return "Innerloop Build and Test (Jit - ${displayStr})"

        case 'corefx_innerloop':
            return "CoreFX Tests"

        case 'normal':
            return "Build and Test"

        case 'jitdiff':
            return "Jit Diff Build and Test"

        case 'ilrt':
            return "IL RoundTrip Build and Test"

        case 'longgc':
            return "Long-Running GC Build & Test"

        case 'gcsimulator':
            return "GC Simulator"

        case 'standalone_gc':
            return "Standalone GC"

        case 'gc_reliability_framework':
            return "GC Reliability Framework"

        case 'illink':
            return "via ILLink"

        default:
            if (isJitStressScenario(scenario)) {
                def displayStr = getStressModeDisplayName(scenario)
                return "Build and Test (Jit - ${displayStr})"
            }
            else if (isR2RScenario(scenario)) {
                def displayStr = getR2RDisplayName(scenario)
                return "${displayStr} Build and Test"
            }
            else {
                return "${scenario}"
            }
            break
    }

    println("Unknown scenario: ${scenario}");
    assert false
}

//
// Functions to create an environment script.
//      envScriptCreate -- initialize the script (call first)
//      envScriptFinalize -- finalize the script (call last)
//      envScriptSetStressModeVariables -- set stress mode variables in the env script
//      envScriptAppendExistingScript -- append an existing script to the generated script
//
// Each script returns a string of commands. Concatenate all the strings together before
// adding them to the builds commands, to make sure they get executed as one Jenkins script.
//

// Initialize the environment setting script.
def static envScriptCreate(def os, def stepScriptLocation) {
    def stepScript = ''
    if (os == 'Windows_NT') {
        stepScript += "echo Creating TestEnv script\r\n"
        stepScript += "if exist ${stepScriptLocation} del ${stepScriptLocation}\r\n"

        // Create at least an empty script.
        stepScript += "echo. > ${stepScriptLocation}\r\n"
    }
    else {
        stepScript += "echo Creating environment setting script\n"
        stepScript += "echo \\#\\!/usr/bin/env bash > ${stepScriptLocation}\n"
    }

    return stepScript
}

// Generates the string for setting stress mode variables.
def static envScriptSetStressModeVariables(def os, def stressModeVars, def stepScriptLocation) {
    def stepScript = ''
    if (os == 'Windows_NT') {
        stressModeVars.each{ k, v ->
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\r\n"
            // Write out the set itself to the script file`
            stepScript += "echo set ${k}=${v} >> ${stepScriptLocation}\r\n"
        }
    }
    else {
        stressModeVars.each{ k, v ->
            // Write out what we are writing to the script file
            stepScript += "echo Setting ${k}=${v}\n"
            // Write out the set itself to the script file`
            stepScript += "echo export ${k}=${v} >> ${stepScriptLocation}\n"
        }
    }

    return stepScript
}

// Append an existing script to an environment script.
// Returns string of commands to do this.
def static envScriptAppendExistingScript(def os, def appendScript, def stepScriptLocation) {
    assert (os == 'Windows_NT')
    def stepScript = ''

    stepScript += "echo Appending ${appendScript} to ${stepScriptLocation}\r\n"
    stepScript += "type ${appendScript} >> ${stepScriptLocation}\r\n"

    return stepScript
}

// Finalize an environment setting script.
// Returns string of commands to do this.
def static envScriptFinalize(def os, def stepScriptLocation) {
    def stepScript = ''

    if (os == 'Windows_NT') {
        // Display the resulting script. This is useful when looking at the output log file.
        stepScript += "echo Display the total script ${stepScriptLocation}\r\n"
        stepScript += "type ${stepScriptLocation}\r\n"
    }
    else {
        stepScript += "chmod +x ${stepScriptLocation}\n"
    }

    return stepScript
}

def static isNeedDocker(def architecture, def os, def isBuild) {
    if (isBuild) {
        if (architecture == 'x86' && os == 'Ubuntu') {
            return true
        }
        else if (architecture == 'armem') {
            return true
        }
        else if (architecture == 'arm') {
            if (os == 'Ubuntu') {
                return true
            }
        }
        else if (architecture == 'arm64') {
            if (os == 'Ubuntu16.04') {
                return true
            }
        }
    }
    else {
        if (architecture == 'x86' && os == 'Ubuntu') {
            return true
        }
    }
    return false
}

def static getDockerImageName(def architecture, def os, def isBuild) {
    // We must change some docker private images to official later
    if (isBuild) {
        if (architecture == 'x86' && os == 'Ubuntu') {
            return "hseok82/dotnet-buildtools-prereqs:ubuntu-16.04-crossx86-ef0ac75-20175511035548"
        }
        else if (architecture == 'armem') {
            if (os == 'Tizen') {
                return "tizendotnet/dotnet-buildtools-prereqs:ubuntu-16.04-cross-e435274-20180426002255-tizen-rootfs-5.0m1"
            }
        }
        else if (architecture == 'arm') {
            if (os == 'Ubuntu') {
                return "mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-14.04-cross-e435274-20180426002420"
            }
        }
        else if (architecture == 'arm64') {
            if (os == 'Ubuntu16.04') {
                return "mcr.microsoft.com/dotnet-buildtools/prereqs:ubuntu-16.04-cross-arm64-a3ae44b-20180315221921"
            }
        }
    }
    else {
        if (architecture == 'x86' && os == 'Ubuntu') {
            return "hseok82/dotnet-buildtools-prereqs:ubuntu1604_x86_test"
        }
    }
    println("Unknown architecture to use docker: ${architecture} ${os}");
    assert false
}

def static getTestArtifactsTgzFileName(def osGroup, def architecture, def configuration) {
    return "bin-tests-${osGroup}.${architecture}.${configuration}.tgz"
}

// We have a limited amount of some hardware. For these, scale back the periodic testing we do,
// and only allowing using this hardware in some specific branches.
def static jobRequiresLimitedHardware(def architecture, def os) {
    if (architecture == 'arm') {
        // arm Windows and Linux hardware is limited.
        return true
    }
    else if (architecture == 'arm64') {
        // arm64 Windows and Linux hardware is limited.
        return true
    }
    else {
        return false
    }
}

// Calculates the name of the build job based on some typical parameters.
//
def static getJobName(def configuration, def architecture, def os, def scenario, def isBuildOnly) {
    // If the architecture is x64, do not add that info into the build name.
    // Need to change around some systems and other builds to pick up the right builds
    // to do that.

    def suffix = scenario != 'normal' ? "_${scenario}" : '';
    if (isBuildOnly) {
        suffix += '_bld'
    }
    def baseName = ''
    switch (architecture) {
        case 'x64':
            if (scenario == 'normal') {
                // For now we leave x64 off of the name for compatibility with other jobs
                baseName = configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else if (scenario == 'formatting') {
                // we don't care about the configuration for the formatting job. It runs all configs
                baseName = architecture.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            break
        case 'armem':
            // These are cross builds
            assert os == 'Tizen'
            baseName = 'armel_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'arm':
        case 'arm64':
            // These are cross builds
            baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'x86':
            baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }

    return baseName + suffix
}

def static addNonPRTriggers(def job, def branch, def isPR, def architecture, def os, def configuration, def scenario, def isFlowJob, def isWindowsBuildOnlyJob, def bidailyCrossList) {

    // The dev/unix_test_workflow branch is used for Jenkins CI testing. We generally do not need any non-PR
    // triggers in the branch, because that would use machine resources unnecessarily.
    if (branch == 'dev/unix_test_workflow') {
        return
    }

    // Limited hardware is restricted for non-PR triggers to certain branches.
    if (jobRequiresLimitedHardware(architecture, os) && (!(branch in Constants.LimitedHardwareBranches))) {
        return
    }

    // Ubuntu x86 CI jobs are failing. Disable non-PR triggered jobs to avoid these constant failures
    // until this is fixed. Tracked by https://github.com/dotnet/coreclr/issues/19003.
    if (architecture == 'x86' && os == 'Ubuntu') {
        return
    }

    def isNormalOrInnerloop = (scenario == "normal" || scenario == "innerloop")

    // Check scenario.
    switch (scenario) {
        case 'crossgen_comparison':
            if (isFlowJob && (configuration == 'Checked' || configuration == 'Release')) {
                if (os == 'Ubuntu' && architecture == 'arm') {
                    // Not enough Linux/arm32 hardware for this.
                    // addPeriodicTriggerHelper(job, '@daily')
                }
                if (os == 'Ubuntu16.04' && architecture == 'arm64') {
                    addPeriodicTriggerHelper(job, '@daily')
                }
            }
            break

        case 'pmi_asm_diffs':
            // No non-PR triggers for now.
            break

        case 'normal':
            switch (architecture) {
                case 'x64':
                case 'x86':
                    if (isFlowJob && architecture == 'x86' && os == 'Ubuntu') {
                        addPeriodicTriggerHelper(job, '@daily')
                    }
                    else if (isFlowJob || os == 'Windows_NT' || (architecture == 'x64' && !(os in Constants.crossList))) {
                        addGithubPushTriggerHelper(job)
                    }
                    break
                case 'arm64':
                    if (os == 'Windows_NT') {
                        if (isFlowJob || (isNormalOrInnerloop && (configuration == 'Debug'))) {
                            // We would normally want a per-push trigger, but with limited hardware we can't keep up.
                            // Do the builds daily.
                            addPeriodicTriggerHelper(job, '@daily')
                        }
                    }
                    else {
                        // Only the flow jobs get push triggers; the build and test jobs are triggered by the flow job.
                        if (isFlowJob) {
                            addPeriodicTriggerHelper(job, '@daily')
                        }
                    }
                    break
                case 'arm':
                    if (os == 'Windows_NT') {
                        if (isFlowJob || (isNormalOrInnerloop && (configuration == 'Debug'))) {
                            // We would normally want a push trigger, but with limited hardware we can't keep up.
                            // Do the builds daily.
                            addPeriodicTriggerHelper(job, '@daily')
                        }
                    }
                    else {
                        assert os == 'Ubuntu'
                        // Only the flow jobs get push triggers; the build and test jobs are triggered by the flow job.
                        if (isFlowJob) {
                            // Currently no push triggers, with limited arm Linux hardware.
                            // TODO: If we have enough machine capacity, add some arm Linux push triggers.

                            // Duplicated by AzDO
                            // addPeriodicTriggerHelper(job, '@daily')
                        }
                    }
                    break
                case 'armem':
                    addGithubPushTriggerHelper(job)
                    break
                default:
                    println("Unknown architecture: ${architecture}");
                    assert false
                    break
            }
            break
        case 'r2r':
            assert !(os in bidailyCrossList)
            // r2r gets a push trigger for checked/release
            if (configuration == 'Checked' || configuration == 'Release') {
                if (architecture == 'x64' && os != 'OSX10.12') {
                    //Flow jobs should be Windows, Ubuntu, OSX0.12, or CentOS
                    if (isFlowJob || os == 'Windows_NT') {
                        addGithubPushTriggerHelper(job)
                    }
                // OSX10.12 r2r jobs should only run every 12 hours, not daily.
                } else if (architecture == 'x64' && os == 'OSX10.12'){
                    if (isFlowJob) {
                        addPeriodicTriggerHelper(job, 'H H/12 * * *')
                    }
                }
                // For x86, only add per-commit jobs for Windows
                else if (architecture == 'x86') {
                    if (os == 'Windows_NT') {
                        addGithubPushTriggerHelper(job)
                    }
                }
                // arm r2r jobs should only run weekly.
                else if (architecture == 'arm') {
                    if (isFlowJob) {
                        // Linux arm32 done in AzDO
                        if (os == 'Windows_NT') {
                            addPeriodicTriggerHelper(job, '@weekly')
                        }
                    }
                }
                // arm64 r2r jobs should only run weekly.
                else if (architecture == 'arm64') {
                    if (isFlowJob) {
                        addPeriodicTriggerHelper(job, '@weekly')
                    }
                }
            }
            break
        case 'r2r_jitstress1':
        case 'r2r_jitstress2':
        case 'r2r_jitstress1_tiered':
        case 'r2r_jitstress2_tiered':
        case 'r2r_jitstressregs1':
        case 'r2r_jitstressregs2':
        case 'r2r_jitstressregs3':
        case 'r2r_jitstressregs4':
        case 'r2r_jitstressregs8':
        case 'r2r_jitstressregs0x10':
        case 'r2r_jitstressregs0x80':
        case 'r2r_jitstressregs0x1000':
        case 'r2r_jitminopts':
        case 'r2r_jitforcerelocs':
        case 'r2r_gcstress15':
        case 'r2r_no_tiered_compilation':
            assert !(os in bidailyCrossList)

            // GCStress=C is currently not supported on OS X
            if (os == 'OSX10.12' && isGCStressRelatedTesting(scenario)) {
                break
            }

            if (configuration == 'Checked' || configuration == 'Release') {
                if (architecture == 'x64') {
                    //Flow jobs should be Windows, Ubuntu, OSX10.12, or CentOS
                    if (isFlowJob || os == 'Windows_NT') {
                        addPeriodicTriggerHelper(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                    }
                }
                // For x86, only add periodic jobs for Windows
                else if (architecture == 'x86') {
                    if (os == 'Windows_NT') {
                        addPeriodicTriggerHelper(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                    }
                }
                else if (architecture == 'arm') {
                    if (isFlowJob) {
                        // Linux arm32 duplicated by AzDO
                        if (os == 'Windows_NT') {
                            addPeriodicTriggerHelper(job, '@weekly')
                        }
                    }
                }
                else if (architecture == 'arm64') {
                    if (isFlowJob) {
                        addPeriodicTriggerHelper(job, '@weekly')
                    }
                }
            }
            break
        case 'longgc':
            assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX10.12')
            assert configuration == 'Release'
            assert architecture == 'x64'
            addPeriodicTriggerHelper(job, '@daily')
            // TODO: Add once external email sending is available again
            // addEmailPublisher(job, 'dotnetgctests@microsoft.com')
            break
        case 'gcsimulator':
            assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX10.12')
            assert configuration == 'Release'
            assert architecture == 'x64'
            addPeriodicTriggerHelper(job, 'H H * * 3,6') // some time every Wednesday and Saturday
            // TODO: Add once external email sending is available again
            // addEmailPublisher(job, 'dotnetgctests@microsoft.com')
            break
        case 'standalone_gc':
            assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX10.12')
            assert (configuration == 'Release' || configuration == 'Checked')
            // TODO: Add once external email sending is available again
            // addEmailPublisher(job, 'dotnetgctests@microsoft.com')
            addPeriodicTriggerHelper(job, '@daily')
            break
        case 'gc_reliability_framework':
            assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX10.12')
            assert (configuration == 'Release' || configuration == 'Checked')
            // Only triggered by phrase.
            break
        case 'ilrt':
            assert !(os in bidailyCrossList)
            // ILASM/ILDASM roundtrip one gets a daily build, and only for release
            if (architecture == 'x64' && configuration == 'Release') {
                if (isFlowJob || os == 'Windows_NT') {
                    addPeriodicTriggerHelper(job, '@daily')
                }
            }
            break
        case 'jitdiff':
            assert (os == 'Ubuntu' || os == 'Windows_NT' || os == 'OSX10.12')
            assert configuration == 'Checked'
            assert (architecture == 'x64' || architecture == 'x86')
            addGithubPushTriggerHelper(job)
            break
        case 'formatting':
            assert (os == 'Windows_NT' || os == "Ubuntu")
            assert architecture == 'x64'
            addGithubPushTriggerHelper(job)
            break
        case 'jitstressregs1':
        case 'jitstressregs2':
        case 'jitstressregs3':
        case 'jitstressregs4':
        case 'jitstressregs8':
        case 'jitstressregs0x10':
        case 'jitstressregs0x80':
        case 'jitstressregs0x1000':
        case 'minopts':
        case 'tieredcompilation':
        case 'no_tiered_compilation':
        case 'forcerelocs':
        case 'jitstress1':
        case 'jitstress2':
        case 'jitstress1_tiered':
        case 'jitstress2_tiered':
        case 'jitstress2_jitstressregs1':
        case 'jitstress2_jitstressregs2':
        case 'jitstress2_jitstressregs3':
        case 'jitstress2_jitstressregs4':
        case 'jitstress2_jitstressregs8':
        case 'jitstress2_jitstressregs0x10':
        case 'jitstress2_jitstressregs0x80':
        case 'jitstress2_jitstressregs0x1000':
        case 'tailcallstress':
        case 'jitsse2only':
        case 'jitnosimd':
        case 'jitnox86hwintrinsic':
        case 'jitincompletehwintrinsic':
        case 'jitx86hwintrinsicnoavx':
        case 'jitx86hwintrinsicnoavx2':
        case 'jitx86hwintrinsicnosimd':
        case 'corefx_baseline':
        case 'corefx_minopts':
        case 'corefx_tieredcompilation':
        case 'corefx_jitstress1':
        case 'corefx_jitstress2':
        case 'corefx_jitstressregs1':
        case 'corefx_jitstressregs2':
        case 'corefx_jitstressregs3':
        case 'corefx_jitstressregs4':
        case 'corefx_jitstressregs8':
        case 'corefx_jitstressregs0x10':
        case 'corefx_jitstressregs0x80':
        case 'corefx_jitstressregs0x1000':
        case 'zapdisable':
            if (os == 'CentOS7.1') {
                break
            }
            if (os in bidailyCrossList) {
                break
            }
            if ((os == 'Ubuntu') && (architecture == 'arm') && !isCoreFxScenario(scenario)) {
                // Linux arm32 duplicated by AzDO
                break
            }
            // ARM corefx testing uses non-flow jobs to provide the configuration-specific
            // build for the flow job. We don't need cron jobs for these. Note that the
            // Windows ARM jobs depend on a Windows "build only" job that exits the trigger
            // function very early, so only non-Windows gets here.
            if ((architecture == 'arm') && isCoreFxScenario(scenario) && !isFlowJob) {
                break
            }
            if ((architecture == 'arm64') && isCoreFxScenario(scenario) && !isFlowJob) {
                break
            }
            if (jobRequiresLimitedHardware(architecture, os)) {
                if ((architecture == 'arm64') && (os == 'Ubuntu16.04')) {
                    // These jobs are very fast on Linux/arm64 hardware, so run them daily.
                    addPeriodicTriggerHelper(job, '@daily')
                }
                else if (scenario == 'corefx_baseline') {
                    addPeriodicTriggerHelper(job, '@daily')
                }
                else {
                    addPeriodicTriggerHelper(job, '@weekly')
                }
            }
            else {
                addPeriodicTriggerHelper(job, '@daily')
            }
            break
        case 'heapverify1':
        case 'gcstress0x3':
            if (os == 'CentOS7.1') {
                break
            }
            if (os in bidailyCrossList) {
                break
            }
            if ((os == 'Ubuntu') && (architecture == 'arm')) {
                // Linux arm32 duplicated by AzDO
                break
            }
            addPeriodicTriggerHelper(job, '@weekly')
            break
        case 'gcstress0xc':
        case 'gcstress0xc_zapdisable':
        case 'gcstress0xc_zapdisable_jitstress2':
        case 'gcstress0xc_zapdisable_heapverify1':
        case 'gcstress0xc_jitstress1':
        case 'gcstress0xc_jitstress2':
        case 'gcstress0xc_minopts_heapverify1':
            if (os == 'OSX10.12') {
                // GCStress=C is currently not supported on OS X
                break
            }
            if (os == 'CentOS7.1') {
                break
            }
            if (os in bidailyCrossList) {
                break
            }
            if ((os == 'Ubuntu') && (architecture == 'arm')) {
                // Linux arm32 duplicated by AzDO
                break
            }
            addPeriodicTriggerHelper(job, '@weekly')
            break

        case 'illink':
            // Testing on other operating systems TBD
            assert (os == 'Windows_NT' || os == 'Ubuntu')
            if (architecture == 'x64' || architecture == 'x86') {
                if (configuration == 'Checked') {
                    addPeriodicTriggerHelper(job, '@daily')
                }
            }
            break

        default:
            println("Unknown scenario: ${scenario}");
            assert false
            break
    }
    return
}

// **************************
// Define the basic inner loop builds for PR and commit.  This is basically just the set
// of coreclr builds over linux/osx 10.12/windows and debug/release/checked.  In addition, the windows
// builds will do a couple extra steps.
// **************************

// Adds a trigger for the PR build if one is needed.  If isFlowJob is true, then this is the
// flow job that rolls up the build and test for non-windows OS's.  // If the job is a windows build only job,
// it's just used for internal builds
// If you add a job with a trigger phrase, please add that phrase to coreclr/Documentation/project-docs/ci-trigger-phrases.md
def static addTriggers(def job, def branch, def isPR, def architecture, def os, def configuration, def scenario, def isFlowJob, def isWindowsBuildOnlyJob) {
    def isNormalOrInnerloop = (scenario == "normal" || scenario == "innerloop")
    
    if (isWindowsBuildOnlyJob) {
        return
    }

    def bidailyCrossList = ['RHEL7.2', 'Debian8.4']
    // Non pull request builds.
    if (!isPR) {
        addNonPRTriggers(job, branch, isPR, architecture, os, configuration, scenario, isFlowJob, isWindowsBuildOnlyJob, bidailyCrossList)
        return
    }

    def arm64Users = [
        'adityamandaleeka',
        'AndyAyersMS',
        'briansull',
        'BruceForstall',
        'CarolEidt',
        'davidwrighton',
        'echesakovMSFT',
        'erozenfeld',
        'janvorli',
        'jashook',
        'pgodeq',
        'RussKeldorph',
        'sandreenko',
        'swaroop-sridhar',
        'jkotas',
        'markwilkie',
        'weshaggard',
        'tannergooding'
    ]

    // Pull request builds.  Generally these fall into two categories: default triggers and on-demand triggers
    // We generally only have a distinct set of default triggers but a bunch of on-demand ones.

    def contextString = ""
    def triggerString = ""
    def needsTrigger = true
    def isDefaultTrigger = false
    def isArm64PrivateJob = false
    def scenarioString = ""

    // Set up default context string and trigger phrases. This is overridden in places, sometimes just to keep
    // the existing non-standard descriptions and phrases. In some cases, the scenarios are asymmetric, as for
    // some jobs where the Debug configuration just does builds, no tests.
    //
    // Some configurations, like arm32/arm64, always use the exact scenario name as part of the context string.
    // This makes it possible to copy/paste the displayed context string as "@dotnet-bot test <context-string>"
    // to invoke the trigger. Any "fancy" context string makes that impossible, requiring the user to either 
    // remember the mapping from context string to trigger string, or use "@dotnet-bot help" to look it up.

    if (architecture == 'armem') {
        assert os == 'Tizen'
        architecture = 'armel'
    }

    switch (architecture) {
        case 'armel':
        case 'arm':
        case 'arm64':
            contextString = "${os} ${architecture} Cross ${configuration}"
            triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+Cross\\W+${configuration}"

            if (scenario == 'innerloop') {
                contextString += " Innerloop"
                triggerString += "\\W+Innerloop"
            }
            else {
                contextString += " ${scenario}"
                triggerString += "\\W+${scenario}"
            }

            if (scenario == 'pmi_asm_diffs') {
                // Don't add the "Build and Test" part
            }
            else if (configuration == 'Debug') {
                contextString += " Build"
                triggerString += "\\W+Build"
            }
            else {
                contextString += " Build and Test"
                triggerString += "\\W+Build and Test"
            }

            triggerString += ".*"
            break

        default:
            scenarioString = getScenarioDisplayString(scenario)
            contextString = "${os} ${architecture} ${configuration} ${scenarioString}"
            triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}"

            switch (scenario) {
                case 'normal':
                    triggerString += "\\W+Build and Test.*"
                    break

                case 'corefx_innerloop': // maintain this asymmetry
                    triggerString += "\\W+CoreFX Tests.*"
                    break

                default:
                    triggerString += "\\W+${scenario}.*"
                    break
            }

            triggerString += ".*"
            break
    }

    // Now determine what kind of trigger this job needs, if any. Any job should be triggerable, except for
    // non-flow jobs that are only used as part of flow jobs.

    switch (architecture) {
        case 'x64': // editor brace matching: {
            if (scenario == 'formatting') {
                assert configuration == 'Checked'
                if (os == 'Windows_NT' || os == 'Ubuntu') {
                    isDefaultTrigger = true
                    contextString = "${os} ${architecture} Formatting"
                }
                break
            }

            if (scenario == 'pmi_asm_diffs') {
                // Everything is already set.
                // No default triggers.
                break
            }

            switch (os) {
                // OpenSUSE, Debian & RedHat get trigger phrases for pri 0 build, and pri 1 build & test
                case 'Debian8.4':
                case 'RHEL7.2':
                    if (scenario == 'innerloop') {
                        assert !isFlowJob
                        contextString = "${os} ${architecture} ${configuration} Innerloop Build"
                        isDefaultTrigger = true
                        break
                    }

                    // fall through

                case 'Fedora24':
                case 'Ubuntu16.04':
                case 'Ubuntu16.10':
                    assert !isFlowJob
                    assert scenario != 'innerloop'
                    contextString = "${os} ${architecture} ${configuration} Build"
                    triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+Build.*"
                    break

                case 'Ubuntu':
                    if (scenario == 'illink') {
                        break
                    }
                    else if (scenario == 'corefx_innerloop') {
                        if (configuration == 'Checked') {
                            isDefaultTrigger = true
                        }
                        break
                    }

                    // fall through

                case 'OSX10.12':
                    // Triggers on the non-flow jobs aren't necessary here
                    // Corefx testing uses non-flow jobs.
                    if (!isFlowJob && !isCoreFxScenario(scenario)) {
                        needsTrigger = false
                        break
                    }
                    switch (scenario) {
                        case 'innerloop':
                            isDefaultTrigger = true
                            break

                        case 'no_tiered_compilation_innerloop':
                            if (os == 'Ubuntu') {
                                isDefaultTrigger = true
                            }
                            break

                        default:
                            break
                    }
                    break

                case 'CentOS7.1':
                    switch (scenario) {
                        case 'innerloop':
                            // CentOS uses checked for default PR tests while debug is build only
                            if (configuration == 'Debug') {
                                isDefaultTrigger = true
                                contextString = "${os} ${architecture} ${configuration} Innerloop Build"
                                break
                            }
                            
                            // Make sure this is a flow job to get build and test.
                            if (!isFlowJob) {
                                needsTrigger = false
                                break
                            }

                            if (configuration == 'Checked') {
                                assert job.name.contains("flow")
                                isDefaultTrigger = true
                                contextString = "${os} ${architecture} ${configuration} Innerloop Build and Test"
                            }
                            break

                        case 'normal':
                            // Make sure this is a flow job to get build and test.
                            if (!isFlowJob) {
                                needsTrigger = false
                                break
                            }
                            break

                        default:
                            break
                    }
                    break

                case 'Windows_NT':
                    switch (scenario) {
                        case 'innerloop':
                        case 'no_tiered_compilation_innerloop':
                            isDefaultTrigger = true
                            break

                        case 'corefx_innerloop':
                            if (configuration == 'Checked' || configuration == 'Release') {
                                isDefaultTrigger = true
                            }
                            break

                        default:
                            break
                    }
                    break

                default:
                    println("Unknown os: ${os}");
                    assert false
                    break

            } // switch (os)

            break
        // editor brace matching: }

        case 'armel': // editor brace matching: {
            job.with {
                publishers {
                    azureVMAgentPostBuildAction {
                        agentPostBuildAction('Delete agent if the build was not successful (when idle).')
                    }
                }
            }

            switch (os) {
                case 'Tizen':
                    if (scenario == 'innerloop') {
                        if (configuration == 'Checked') {
                            isDefaultTrigger = true
                        }
                    }
                    break
            }

            break
        // editor brace matching: }

        case 'arm':
        case 'arm64': // editor brace matching: {

            switch (os) {
                case 'Ubuntu':
                case 'Ubuntu16.04':

                    // Triggers on the non-flow jobs aren't necessary
                    if (!isFlowJob) {
                        needsTrigger = false
                        break
                    }
                    if (os == 'Ubuntu' && architecture == 'arm') {
                        switch (scenario) {
                            case 'innerloop':
                            case 'no_tiered_compilation_innerloop':
                                if (configuration == 'Checked') {
                                    isDefaultTrigger = true
                                }
                                break
                             case 'crossgen_comparison':
                                if (configuration == 'Checked' || configuration == 'Release') {
                                    isDefaultTrigger = true
                                }
                                break
                        }
                    }
                    break

                case 'Windows_NT':
                    assert isArmWindowsScenario(scenario)

                    // For Debug normal/innerloop scenario, we don't do test runs, so we don't use flow jobs. That means we need a trigger for
                    // the non-flow Build job. All others need a trigger on the flow job.
                    def needsFlowJobTrigger = !(isNormalOrInnerloop && (configuration == 'Debug'))
                    if (isFlowJob != needsFlowJobTrigger) {
                        needsTrigger = false
                        break
                    }

                    switch (scenario) {
                        case 'innerloop':
                            if (configuration == 'Checked') {
                                isDefaultTrigger = true
                                isArm64PrivateJob = true
                            }
                            break
                        default:
                            isArm64PrivateJob = true
                            break
                    }
                    break
                default:
                    println("NYI os: ${os}");
                    assert false
                    break
            }
            break

        // editor brace matching: }
        case 'x86': // editor brace matching: {
            assert ((os == 'Windows_NT') || ((os == 'Ubuntu') && isNormalOrInnerloop))
            if (os == 'Ubuntu') {
                // Triggers on the non-flow jobs aren't necessary here
                if (!isFlowJob) {
                    needsTrigger = false
                    break
                }
                
                // on-demand only for ubuntu x86
                contextString = "${os} ${architecture} ${configuration} Build"
                triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}.*"
                break
            }
            switch (scenario) {
                case 'innerloop':
                case 'no_tiered_compilation_innerloop':
                    isDefaultTrigger = true
                    break
                default:
                    break
            }
            break

        // editor brace matching: }

        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }

    if (needsTrigger) {
        if (isArm64PrivateJob) {
            // ignore isDefaultTrigger to disable Jenkins by default
            if (false) {
                Utilities.addDefaultPrivateGithubPRTriggerForBranch(job, branch, contextString, null, arm64Users)
            }
            else {
                Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString, triggerString, null, arm64Users)
            }
        }
        else {
            // ignore isDefaultTrigger to disable Jenkins by default
            if (false) {
                Utilities.addGithubPRTriggerForBranch(job, branch, contextString)
            }
            else {
                Utilities.addGithubPRTriggerForBranch(job, branch, contextString, triggerString)
            }
        }
    }
}

def static calculateBuildCommands(def newJob, def scenario, def branch, def isPR, def architecture, def configuration, def os, def isBuildOnly) {
    def buildCommands = []
    def osGroup = getOSGroup(os)
    def lowerConfiguration = configuration.toLowerCase()

    // Which set of tests to build? Innerloop tests build Pri-0.
    // Currently, we only generate asm diffs on Pri-0 tests, if we generate asm diffs on tests at all.
    // CoreFX testing skipts building tests altogether (done below).
    // All other scenarios build Pri-1 tests.
    def priority = '1'
    if (isInnerloopTestScenario(scenario)) {
        priority = '0'
    }

    def doCoreFxTesting = isCoreFxScenario(scenario)

    def buildCoreclrTests = true
    if (doCoreFxTesting || (scenario == 'pmi_asm_diffs')) {
        // These scenarios don't need the coreclr tests build.
        buildCoreclrTests = false
    }

    // Calculate the build steps, archival, and xunit results
    switch (os) {
        case 'Windows_NT': // editor brace matching: {
            switch (architecture) {
                case 'x64':
                case 'x86':
                    def arch = architecture
                    def buildOpts = ''

                    if (scenario == 'formatting') {
                        buildCommands += "python -u tests\\scripts\\format.py -c %WORKSPACE% -o Windows_NT -a ${arch}"
                        Utilities.addArchival(newJob, "format.patch", "", true, false)
                        break
                    }

                    if (scenario == 'illink') {
                        buildCommands += "tests\\scripts\\build_illink.cmd clone ${arch}"
                    }

                    // If it is a release build for Windows, ensure PGO is used, else fail the build.
                    if ((lowerConfiguration == 'release') &&
                        (scenario in Constants.basicScenarios)) {

                        buildOpts += ' -enforcepgo'
                    }

                    if (buildCoreclrTests) {
                        buildOpts += " -priority=${priority}"
                    } else {
                        buildOpts += ' skiptests';
                    }

                    // Set __TestIntermediateDir to something short. If __TestIntermediateDir is already set, build-test.cmd will
                    // output test binaries to that directory. If it is not set, the binaries are sent to a default directory whose name is about
                    // 35 characters long.

                    buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${arch} ${buildOpts}"

                    if (scenario == 'pmi_asm_diffs') {
                        // Now, generate the layout. We don't have any tests, but we need to restore the packages before calling runtest.cmd.
                        // Call build-test.cmd to do this. It will do a little more than we need, but that's ok.
                        buildCommands += "build-test.cmd ${lowerConfiguration} ${arch} skipmanaged skipnative"
                        buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${arch} GenerateLayoutOnly"

                        // TODO: Add -target_branch and -commit_hash arguments based on GitHub variables.
                        buildCommands += "python -u %WORKSPACE%\\tests\\scripts\\run-pmi-diffs.py -arch ${arch} -ci_arch ${architecture} -build_type ${configuration}"

                        // ZIP up the asm
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('_\\pmi\\asm', '.\\dasm.${os}.${architecture}.${configuration}.zip')\"";

                        // Archive the asm
                        Utilities.addArchival(newJob, "dasm.${os}.${architecture}.${configuration}.zip")
                        break
                    }

                    if (!isBuildOnly) {
                        def runtestArguments = ''
                        def testOpts = 'collectdumps'

                        if (isR2RScenario(scenario)) {

                            // If this is a ReadyToRun scenario, pass 'crossgen'
                            // to cause framework assemblies to be crossgen'ed. Pass 'runcrossgentests'
                            // to cause the tests to be crossgen'ed.

                            testOpts += ' crossgen runcrossgentests'
                        }
                        else if (scenario == 'jitdiff') {
                            testOpts += ' jitdisasm crossgen'
                        }
                        else if (scenario == 'ilrt') {
                            testOpts += ' ilasmroundtrip'
                        }
                        else if (isLongGc(scenario)) {
                            testOpts += " ${scenario} sequential"
                        }
                        else if (scenario == 'standalone_gc') {
                            testOpts += ' gcname clrgc.dll'
                        }
                        else if (scenario == 'illink') {
                            testOpts += " link %WORKSPACE%\\linker\\linker\\bin\\netcore_Release\\netcoreapp2.0\\win10-${arch}\\publish\\illink.exe"
                        }

                        // Default per-test timeout is 10 minutes. For stress modes and Debug scenarios, increase this
                        // to 30 minutes (30 * 60 * 1000 = 180000). The "timeout" argument to runtest.cmd sets this, by
                        // taking a timeout value in milliseconds. (Note that it sets the __TestTimeout environment variable,
                        // which is read by the xunit harness.)
                        if (isJitStressScenario(scenario) || isR2RStressScenario(scenario) || (lowerConfiguration == 'debug'))
                        {
                            def timeout = 1800000
                            testOpts += " timeout ${timeout}"
                        }

                        // If we are running a stress mode, we should write out the set of key
                        // value env pairs to a file at this point and then we'll pass that to runtest.cmd

                        def envScriptPath = ''
                        if (isJitStressScenario(scenario) || isR2RStressScenario(scenario)) {
                            def buildCommandsStr = ''
                            envScriptPath = "%WORKSPACE%\\SetStressModes.bat"
                            buildCommandsStr += envScriptCreate(os, envScriptPath)

                            if (isJitStressScenario(scenario)) {
                                buildCommandsStr += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], envScriptPath)
                            }
                            else if (isR2RStressScenario(scenario)) {
                                buildCommandsStr += envScriptSetStressModeVariables(os, Constants.r2rStressScenarios[scenario], envScriptPath)
                            }

                            envScriptFinalize(os, envScriptPath)

                            // Note that buildCommands is an array of individually executed commands; we want all the commands used to 
                            // create the SetStressModes.bat script to be executed together, hence we accumulate them as strings
                            // into a single script.
                            buildCommands += buildCommandsStr
                        }
                        if (envScriptPath != '') {
                            testOpts += " TestEnv ${envScriptPath}"
                        }

                        runtestArguments = "${lowerConfiguration} ${arch} ${testOpts}"

                        if (doCoreFxTesting) {
                            if (scenario == 'corefx_innerloop') {
                                // Create CORE_ROOT and testhost
                                buildCommands += "build-test.cmd ${lowerConfiguration} ${arch} buildtesthostonly"                                
                                buildCommands += "tests\\runtest.cmd ${runtestArguments} CoreFXTestsAll"

                                // Archive and process (only) the test results
                                Utilities.addArchival(newJob, "bin/Logs/**/testResults.xml", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                                Utilities.addXUnitDotNETResults(newJob, "bin/Logs/**/testResults.xml")
                            }
                            else {
                                def workspaceRelativeFxRoot = "_/fx"
                                def absoluteFxRoot = "%WORKSPACE%\\_\\fx"
                                def fxBranch = getFxBranch(branch)
                                def exclusionRspPath = "%WORKSPACE%\\tests\\scripts\\run-corefx-tests-exclusions.txt"

                                buildCommands += "python -u %WORKSPACE%\\tests\\scripts\\run-corefx-tests.py -arch ${arch} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${fxBranch} -env_script ${envScriptPath} -exclusion_rsp_file ${exclusionRspPath}"

                                // Archive and process (only) the test results
                                Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/artifacts/bin/**/testResults.xml", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                                Utilities.addXUnitDotNETResults(newJob, "${workspaceRelativeFxRoot}/artifacts/bin/**/testResults.xml")

                                //Archive additional build stuff to diagnose why my attempt at fault injection isn't causing CI to fail
                                Utilities.addArchival(newJob, "SetStressModes.bat", "", true, false)
                                Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/artifacts/bin/testhost/**", "", true, false)
                            }
                        }
                        else if (isGcReliabilityFramework(scenario)) {
                            buildCommands += "tests\\runtest.cmd ${runtestArguments} GenerateLayoutOnly"
                            buildCommands += "tests\\scripts\\run-gc-reliability-framework.cmd ${arch} ${configuration}"
                        }
                        else {
                            def buildCommandsStr = "call tests\\runtest.cmd ${runtestArguments}\r\n"
                            if (!isBuildOnly) {
                                // If we ran the tests, collect the test logs collected by xunit. We want to do this even if the tests fail, so we
                                // must do it in the same batch file as the test run.

                                buildCommandsStr += "echo on\r\n" // Show the following commands in the log. "echo" doesn't alter the errorlevel.
                                buildCommandsStr += "set saved_errorlevel=%errorlevel%\r\n"
                                buildCommandsStr += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${arch}.${configuration}\\Reports', '.\\bin\\tests\\testReports.zip')\"\r\n";
                                buildCommandsStr += "exit /b %saved_errorlevel%\r\n"

                                def doNotFailIfNothingArchived = true
                                def archiveOnlyIfSuccessful = false
                                Utilities.addArchival(newJob, "bin/tests/testReports.zip", "", doNotFailIfNothingArchived, archiveOnlyIfSuccessful)
                            }
                            buildCommands += buildCommandsStr
                        }
                    } // end if (!isBuildOnly)

                    if (!doCoreFxTesting) {
                        // Run the rest of the build
                        // Build the mscorlib for the other OS's
                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} linuxmscorlib"
                        buildCommands += "build.cmd ${lowerConfiguration} ${arch} osxmscorlib"
                       
                        if (arch == 'x64') {
                            buildCommands += "build.cmd ${lowerConfiguration} arm64 linuxmscorlib"
                        }

                        if (!isJitStressScenario(scenario)) {
                            // Zip up the tests directory so that we don't use so much space/time copying
                            // 10s of thousands of files around.
                            buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${arch}.${configuration}', '.\\bin\\tests\\tests.zip')\"";

                            // For Windows, pull full test results and test drops for x86/x64.
                            // No need to pull for stress mode scenarios (downstream builds use the default scenario)
                            Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip", "bin/Product/**/.nuget/**")
                        }

                        if (scenario == 'jitdiff') {
                            // retrieve jit-dasm output for base commit, and run jit-diff
                            if (!isBuildOnly) {
                                // if this is a build only job, we want to keep the default (build) artifacts for the flow job
                                Utilities.addArchival(newJob, "bin/tests/${osGroup}.${arch}.${configuration}/dasm/**")
                            }
                        }

                        if (!isBuildOnly) {
                            Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml', true)
                        }
                    }

                    // Archive the logs, even if the build failed (which is when they are most interesting).
                    Utilities.addArchival(newJob, "bin/Logs/*.log,bin/Logs/*.wrn,bin/Logs/*.err,bin/Logs/MsbuildDebugLogs/*", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                    break
                case 'arm':
                case 'arm64':
                    assert isArmWindowsScenario(scenario)

                    def buildOpts = ''

                    if (buildCoreclrTests) {
                        buildOpts += " -priority=${priority}"
                    } else {
                        buildOpts += ' skiptests'
                    }

                    // This is now a build only job. Do not run tests. Use the flow job.
                    buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${architecture} ${buildOpts}"

                    if (doCoreFxTesting) {
                        assert isBuildOnly

                        // Set the stress mode variables; this is incorporated into the generated CoreFx RunTests.cmd files.
                        def envScriptPath = ''
                        def buildCommandsStr = ''
                        envScriptPath = "%WORKSPACE%\\SetStressModes.bat"
                        buildCommandsStr += envScriptCreate(os, envScriptPath)
                        buildCommandsStr += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], envScriptPath)
                        envScriptFinalize(os, envScriptPath)
                        buildCommands += buildCommandsStr

                        def workspaceRelativeFxRootLinux = "_/fx"
                        def workspaceRelativeFxRootWin = "_\\fx"
                        def absoluteFxRoot = "%WORKSPACE%\\_\\fx"
                        def fxBranch = getFxBranch(branch)

                        buildCommands += "python -u %WORKSPACE%\\tests\\scripts\\run-corefx-tests.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${fxBranch} -env_script ${envScriptPath} -no_run_tests"

                        // Zip up the CoreFx runtime and tests. We don't need the CoreCLR binaries; they have been copied to the CoreFX tree.
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('${workspaceRelativeFxRootWin}\\artifacts\\bin\\testhost\\netcoreapp-Windows_NT-Release-${architecture}', '${workspaceRelativeFxRootWin}\\fxruntime.zip')\"";
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('${workspaceRelativeFxRootWin}\\artifacts\\bin\\tests', '${workspaceRelativeFxRootWin}\\fxtests.zip')\"";

                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxruntime.zip")
                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxtests.zip")
                    } else {
                        // Zip up the tests directory so that we don't use so much space/time copying
                        // 10s of thousands of files around.
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${architecture}.${configuration}', '.\\bin\\tests\\tests.zip')\"";

                        // Add archival.
                        Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip", "bin/Product/**/.nuget/**")
                    }

                    // Archive the logs, even if the build failed (which is when they are most interesting).
                    Utilities.addArchival(newJob, "bin/Logs/*.log,bin/Logs/*.wrn,bin/Logs/*.err,bin/Logs/MsbuildDebugLogs/*", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                    break
                default:
                    println("Unknown architecture: ${architecture}");
                    assert false
                    break
            }
            break
        // end case 'Windows_NT'; editor brace matching: }
        case 'Ubuntu':
        case 'Ubuntu16.04':
        case 'Ubuntu16.10':
        case 'Debian8.4':
        case 'OSX10.12':
        case 'CentOS7.1':
        case 'RHEL7.2':
        case 'Tizen':
        case 'Fedora24': // editor brace matching: {
            switch (architecture) {
                case 'x86':
                    if (os == 'Ubuntu') {
                        // build and PAL test
                        def dockerImage = getDockerImageName(architecture, os, true)
                        buildCommands += "docker run -i --rm -v \${WORKSPACE}:/opt/code -w /opt/code -e ROOTFS_DIR=/crossrootfs/x86 ${dockerImage} ./build.sh ${architecture} cross ${lowerConfiguration}"
                        dockerImage = getDockerImageName(architecture, os, false)
                        buildCommands += "docker run -i --rm -v \${WORKSPACE}:/opt/code -w /opt/code ${dockerImage} ./src/pal/tests/palsuite/runpaltests.sh /opt/code/bin/obj/${osGroup}.${architecture}.${configuration} /opt/code/bin/paltestout"
                        Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
                        Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                    }
                    break

                case 'x64':
                    if (scenario == 'formatting') {
                        buildCommands += "python tests/scripts/format.py -c \${WORKSPACE} -o Linux -a ${architecture}"
                        Utilities.addArchival(newJob, "format.patch", "", true, false)
                        break
                    }

                    if (scenario == 'pmi_asm_diffs') {
                        buildCommands += "./build.sh ${lowerConfiguration} ${architecture} skiptests skipbuildpackages"
                        buildCommands += "./build-test.sh ${lowerConfiguration} ${architecture} generatelayoutonly"

                        // TODO: Add -target_branch and -commit_hash arguments based on GitHub variables.
                        buildCommands += "python -u \${WORKSPACE}/tests/scripts/run-pmi-diffs.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration}"

                        // Archive the asm
                        buildCommands += "tar -czf dasm.${os}.${architecture}.${configuration}.tgz ./_/pmi/asm"
                        Utilities.addArchival(newJob, "dasm.${os}.${architecture}.${configuration}.tgz")
                        break
                    }

                    if (scenario == 'illink') {
                        assert(os == 'Ubuntu')
                        buildCommands += "./tests/scripts/build_illink.sh --clone --arch=${architecture}"
                    }

                    if (!doCoreFxTesting) {
                        // We run pal tests on all OS but generate mscorlib (and thus, nuget packages)
                        // only on supported OS platforms.
                        def bootstrapRid = Utilities.getBoostrapPublishRid(os)
                        def bootstrapRidEnv = bootstrapRid != null ? "__PUBLISH_RID=${bootstrapRid} " : ''

                        buildCommands += "${bootstrapRidEnv}./build.sh ${lowerConfiguration} ${architecture}"

                        def testBuildOpts = ""
                        if (priority == '1') {
                            testBuildOpts = "priority1"
                        }

                        buildCommands += "./build-test.sh ${lowerConfiguration} ${architecture} ${testBuildOpts}"
                        buildCommands += "src/pal/tests/palsuite/runpaltests.sh \${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration} \${WORKSPACE}/bin/paltestout"

                        // Archive the bin/tests folder for *_tst jobs
                        def testArtifactsTgzFileName = getTestArtifactsTgzFileName(osGroup, architecture, configuration)
                        buildCommands += "tar -czf ${testArtifactsTgzFileName} bin/tests/${osGroup}.${architecture}.${configuration}"
                        Utilities.addArchival(newJob, "${testArtifactsTgzFileName}", "")
                        // And pal tests
                        Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                    }
                    else {
                        if (scenario == 'corefx_innerloop') {
                            assert os == 'Ubuntu' || 'OSX10.12'
                            assert architecture == 'x64'

                            buildCommands += "./build.sh ${lowerConfiguration} ${architecture} skiptests"
                            buildCommands += "./build-test.sh ${lowerConfiguration} ${architecture} generatetesthostonly"
                            buildCommands += "./tests/runtest.sh ${lowerConfiguration} --corefxtestsall --testHostDir=\${WORKSPACE}/bin/tests/${osGroup}.${architecture}.${configuration}/testhost/ --coreclr-src=\${WORKSPACE}"

                            // Archive and process (only) the test results
                            Utilities.addArchival(newJob, "bin/Logs/**/testResults.xml", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                            Utilities.addXUnitDotNETResults(newJob, "bin/Logs/**/testResults.xml")
                        }
                        else {
                            // Corefx stress testing
                            assert os == 'Ubuntu'
                            assert architecture == 'x64'
                            assert lowerConfiguration == 'checked'
                            assert isJitStressScenario(scenario)

                            // Build coreclr
                            buildCommands += "./build.sh ${lowerConfiguration} ${architecture}"

                            def scriptFileName = "\$WORKSPACE/set_stress_test_env.sh"

                            def envScriptCmds = envScriptCreate(os, scriptFileName)
                            envScriptCmds += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], scriptFileName)
                            envScriptCmds += envScriptFinalize(os, scriptFileName)
                            buildCommands += envScriptCmds

                            // Build and text corefx
                            def workspaceRelativeFxRoot = "_/fx"
                            def absoluteFxRoot = "\$WORKSPACE/${workspaceRelativeFxRoot}"
                            def fxBranch = getFxBranch(branch)
                            def exclusionRspPath = "\$WORKSPACE/tests/scripts/run-corefx-tests-exclusions.txt"

                            buildCommands += "python -u \$WORKSPACE/tests/scripts/run-corefx-tests.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${fxBranch} -env_script ${scriptFileName} -exclusion_rsp_file ${exclusionRspPath}"

                            // Archive and process (only) the test results
                            Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/artifacts/bin/**/testResults.xml", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                            Utilities.addXUnitDotNETResults(newJob, "${workspaceRelativeFxRoot}/artifacts/bin/**/testResults.xml")
                        }
                    }

                    // Archive the logs, even if the build failed (which is when they are most interesting).
                    Utilities.addArchival(newJob, "bin/Logs/*.log,bin/Logs/*.wrn,bin/Logs/*.err,bin/Logs/MsbuildDebugLogs/*", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
                    break
                case 'armem':
                    // Emulator cross builds for ARM runs on Tizen currently
                    assert os == 'Tizen'

                    def arm_abi = "armel"
                    def linuxCodeName = "tizen"

                    // Unzip the Windows test binaries first. Exit with 0
                    buildCommands += "unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.x64.${configuration} || exit 0"

                    // Unpack the corefx binaries
                    buildCommands += "mkdir ./bin/CoreFxBinDir"
                    buildCommands += "tar -xf ./artifacts/bin/build.tar.gz -C ./bin/CoreFxBinDir"

                    // Call the ARM CI script to cross build and test using docker
                    buildCommands += """./tests/scripts/arm32_ci_script.sh \\
                    --mode=docker \\
                    --${arm_abi} \\
                    --linuxCodeName=${linuxCodeName} \\
                    --buildConfig=${lowerConfiguration} \\
                    --testRootDir=./bin/tests/Windows_NT.x64.${configuration} \\
                    --coreFxBinDir=./bin/CoreFxBinDir \\
                    --testDirFile=./tests/testsRunningInsideARM.txt"""

                    // Basic archiving of the build, no pal tests
                    Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.dylib,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
                    break
                case 'arm64':
                case 'arm':
                    // Non-Windows ARM cross builds on hardware run on Ubuntu only
                    assert (os == 'Ubuntu') || (os == 'Ubuntu16.04')

                    // Add some useful information to the log file. Ignore return codes.
                    buildCommands += "uname -a || true"

                    // Cross build the Ubuntu/arm product using docker with a docker image that contains the correct
                    // Ubuntu cross-compilation toolset (running on a Ubuntu x64 host).
                    // For CoreFX testing, we only need the product build; we don't need to generate the layouts. The product
                    // build is then copied into the corefx layout by the run-corefx-test.py script. For CoreFX testing, we
                    // ZIP up the generated CoreFX runtime and tests.

                    def dockerImage = getDockerImageName(architecture, os, true)
                    def dockerCmd = "docker run -i --rm -v \${WORKSPACE}:\${WORKSPACE} -w \${WORKSPACE} -e ROOTFS_DIR=/crossrootfs/${architecture} ${dockerImage} "

                    buildCommands += "${dockerCmd}\${WORKSPACE}/build.sh ${lowerConfiguration} ${architecture} cross"

                    if (doCoreFxTesting) {
                        def scriptFileName = "\$WORKSPACE/set_stress_test_env.sh"

                        def envScriptCmds = envScriptCreate(os, scriptFileName)
                        envScriptCmds += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], scriptFileName)
                        envScriptCmds += envScriptFinalize(os, scriptFileName)
                        buildCommands += envScriptCmds

                        // Build and text corefx
                        def workspaceRelativeFxRootLinux = "_/fx"
                        def absoluteFxRoot = "\$WORKSPACE/${workspaceRelativeFxRootLinux}"
                        def fxBranch = getFxBranch(branch)

                        buildCommands += "${dockerCmd}python -u \$WORKSPACE/tests/scripts/run-corefx-tests.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${fxBranch} -env_script ${scriptFileName} -no_run_tests"

                        // Docker creates files with root permission, so we need to zip in docker also, or else we'll get permission errors.
                        buildCommands += "${dockerCmd}zip -r ${workspaceRelativeFxRootLinux}/fxruntime.zip ${workspaceRelativeFxRootLinux}/artifacts/bin/testhost/netcoreapp-Linux-Release-${architecture}"
                        buildCommands += "${dockerCmd}zip -r ${workspaceRelativeFxRootLinux}/fxtests.zip ${workspaceRelativeFxRootLinux}/artifacts/bin/tests"

                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxruntime.zip")
                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxtests.zip")
                    }
                    else if (isCrossGenComparisonScenario(scenario)) {
                        buildCommands += "${dockerCmd}\${WORKSPACE}/build-test.sh ${lowerConfiguration} ${architecture} cross generatelayoutonly"

                        def workspaceRelativeProductBinDir = "bin/Product/${osGroup}.${architecture}.${configuration}"
                        def workspaceRelativeCoreLib = "${workspaceRelativeProductBinDir}/IL/System.Private.CoreLib.dll"
                        def workspaceRelativeCoreRootDir = "bin/tests/${osGroup}.${architecture}.${configuration}/Tests/Core_Root"
                        def workspaceRelativeCrossGenComparisonScript = "tests/scripts/crossgen_comparison.py"
                        def workspaceRelativeResultsDir = "_"
                        def workspaceRelativeArtifactsArchive = "${os}.${architecture}.${configuration}.${scenario}.zip"
                        def crossGenComparisonCmd = "python -u \${WORKSPACE}/${workspaceRelativeCrossGenComparisonScript} "
                        def crossArch = "x64"
                        def crossGenExecutable = "\${WORKSPACE}/${workspaceRelativeProductBinDir}/${crossArch}/crossgen"
                        def workspaceRelativeCrossArchResultDir = "${workspaceRelativeResultsDir}/${osGroup}.${crossArch}_${architecture}.${configuration}"

                        buildCommands += "${dockerCmd}mkdir -p \${WORKSPACE}/${workspaceRelativeCrossArchResultDir}"
                        buildCommands += "${dockerCmd}${crossGenComparisonCmd}crossgen_corelib --crossgen ${crossGenExecutable} --il_corelib \${WORKSPACE}/${workspaceRelativeCoreLib} --result_dir \${WORKSPACE}/${workspaceRelativeCrossArchResultDir}"
                        buildCommands += "${dockerCmd}${crossGenComparisonCmd}crossgen_framework --crossgen ${crossGenExecutable} --core_root \${WORKSPACE}/${workspaceRelativeCoreRootDir} --result_dir \${WORKSPACE}/${workspaceRelativeCrossArchResultDir}"

                        buildCommands += "${dockerCmd}zip -r ${workspaceRelativeArtifactsArchive} ${workspaceRelativeCoreLib} ${workspaceRelativeCoreRootDir} ${workspaceRelativeCrossGenComparisonScript} ${workspaceRelativeResultsDir}"
                        Utilities.addArchival(newJob, "${workspaceRelativeArtifactsArchive}")
                    }
                    else if (scenario == 'pmi_asm_diffs') {
                        buildCommands += "${dockerCmd}\${WORKSPACE}/build-test.sh ${lowerConfiguration} ${architecture} cross generatelayoutonly"

                        // Pass `--skip_diffs` -- the actual diffs will be done on an arm machine in the test job. This is the build job.
                        // TODO: Add -target_branch and -commit_hash arguments based on GitHub variables.
                        buildCommands += "python -u \${WORKSPACE}/tests/scripts/run-pmi-diffs.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} --skip_diffs"

                        // Archive what we created.
                        buildCommands += "tar -czf product.${os}.${architecture}.${lowerConfiguration}.tgz ./bin/Product/Linux.${architecture}.${configuration}"
                        buildCommands += "tar -czf product.baseline.${os}.${architecture}.${lowerConfiguration}.tgz ./_/pmi/base/bin/Product/Linux.${architecture}.${configuration}"
                        buildCommands += "tar -czf coreroot.${os}.${architecture}.${lowerConfiguration}.tgz ./bin/tests/Linux.${architecture}.${configuration}/Tests/Core_Root"
                        buildCommands += "tar -czf coreroot.baseline.${os}.${architecture}.${lowerConfiguration}.tgz ./_/pmi/base/bin/tests/Linux.${architecture}.${configuration}/Tests/Core_Root"

                        // Archive the built artifacts
                        Utilities.addArchival(newJob, "product.${os}.${architecture}.${lowerConfiguration}.tgz,product.baseline.${os}.${architecture}.${lowerConfiguration}.tgz,coreroot.${os}.${architecture}.${lowerConfiguration}.tgz,coreroot.baseline.${os}.${architecture}.${lowerConfiguration}.tgz")
                    }
                    else {
                        // Then, using the same docker image, build the tests and generate the CORE_ROOT layout.

                        def testBuildOpts = ""
                        if (priority == '1') {
                            testBuildOpts = "priority1"
                        }

                        buildCommands += "${dockerCmd}\${WORKSPACE}/build-test.sh ${lowerConfiguration} ${architecture} cross ${testBuildOpts}"

                        // ZIP up the built tests (including CORE_ROOT and native test components copied to the CORE_ROOT) for the test job (created in the flow job code)
                        def testArtifactsTgzFileName = getTestArtifactsTgzFileName(osGroup, architecture, configuration)
                        buildCommands += "tar -czf ${testArtifactsTgzFileName} bin/tests/${osGroup}.${architecture}.${configuration}"

                        Utilities.addArchival(newJob, "${testArtifactsTgzFileName}", "")
                    }

                    // Archive the logs, even if the build failed (which is when they are most interesting).
                    Utilities.addArchival(newJob, "bin/Logs/*.log,bin/Logs/*.wrn,bin/Logs/*.err,bin/Logs/MsbuildDebugLogs/*", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)

                    // We need to clean up the build machines; the docker build leaves newly built files with root permission, which
                    // the cleanup task in Jenkins can't remove.
                    newJob.with {
                        publishers {
                            azureVMAgentPostBuildAction {
                                agentPostBuildAction('Delete agent after build execution (when idle).')
                            }
                        }
                    }
                    break
                default:
                    println("Unknown architecture: ${architecture}");
                    assert false
                    break
            }
            break
        // editor brace matching: }
        default:
            println("Unknown os: ${os}");
            assert false
            break
    } // os

    return buildCommands
}

// Determine if we should generate a job for the given parameters. This is for non-flow jobs: either build and test, or build only.
// Returns true if the job should be generated.
def static shouldGenerateJob(def scenario, def isPR, def architecture, def configuration, def os, def isBuildOnly)
{
    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))

    // Innerloop jobs (except corefx_innerloop) are no longer created in Jenkins
    // The only exception is windows arm(64)
    if (isInnerloopTestScenario(scenario) && isPR && !windowsArmJob) {
        assert scenario != 'corefx_innerloop'
        return false;
    }

    if (!isPR) {
        if (isInnerloopTestScenario(scenario)) {
            return false
        }

        if (scenario == 'corefx_innerloop') {
            return false
        }
    }

    // Tizen is only supported for armem architecture
    if (os == 'Tizen' && architecture != 'armem') {
        return false
    }

    // Filter based on architecture.

    switch (architecture) {
        case 'arm':
            if ((os != 'Windows_NT') && (os != 'Ubuntu')) {
                return false
            }
            break
        case 'arm64':
            if ((os != 'Windows_NT') && (os != 'Ubuntu16.04')) {
                return false
            }
            break
        case 'armem':
            if (os != 'Tizen') {
                return false
            }
            break
        case 'x86':
            if ((os != 'Windows_NT') && (os != 'Ubuntu')) {
                return false
            }
            break
        case 'x64':
            // Everything implemented
            break
        default:
            println("Unknown architecture: ${architecture}")
            assert false
            break
    }

    // Which (Windows) build only jobs are required?

    def isNormalOrInnerloop = (scenario == 'innerloop' || scenario == 'normal')

    if (isBuildOnly) {
        switch (architecture) {
            case 'arm':
            case 'arm64':
                // We use build only jobs for Windows arm/arm64 cross-compilation corefx testing, so we need to generate builds for that.
                if (!isCoreFxScenario(scenario)) {
                    return false
                }
                break
            case 'x64':
            case 'x86':
                if (!isNormalOrInnerloop) {
                    return false
                }
                break
            default:
                return false
        }
    }

    // Filter based on scenario.

    if (isJitStressScenario(scenario)) {
        if (configuration != 'Checked') {
            return false
        }

        def isEnabledOS = (os == 'Windows_NT') ||
                          (os == 'Ubuntu' && (architecture == 'x64') && isCoreFxScenario(scenario)) ||
                          (os == 'Ubuntu' && architecture == 'arm') ||
                          (os == 'Ubuntu16.04' && architecture == 'arm64')
        if (!isEnabledOS) {
            return false
        }

        switch (architecture) {
            case 'x64':
                break

            case 'x86':
                // x86 ubuntu: no stress modes
                if (os == 'Ubuntu') {
                    return false
                }
                break

            case 'arm':
            case 'arm64':
                // We use build only jobs for Windows arm/arm64 cross-compilation corefx testing, so we need to generate builds for that.
                // No "regular" Windows arm corefx jobs, e.g.
                // For Ubuntu arm corefx testing, we use regular jobs (not "build only" since only Windows has "build only", and
                // the Ubuntu arm "regular" jobs don't run tests anyway).
                if (os == 'Windows_NT') {
                    if (! (isBuildOnly && isCoreFxScenario(scenario)) ) {
                        return false
                    }
                }
                else {
                    if (!isCoreFxScenario(scenario)) {
                        return false
                    }
                }
                break

            default:
                // armem: no stress jobs for ARM emulator.
                return false
        }
    }
    else if (isR2RScenario(scenario)) {
        if (os != 'Windows_NT') {
            return false
        }

        if (isR2RBaselineScenario(scenario)) {
            // no need for Debug scenario; Checked is sufficient
            if (configuration != 'Checked' && configuration != 'Release') {
                return false
            }
        }
        else if (isR2RStressScenario(scenario)) {
            // Stress scenarios only run with Checked builds, not Release (they would work with Debug, but be slow).
            if (configuration != 'Checked') {
                return false
            }
        }

        switch (architecture) {
            case 'arm':
            case 'arm64':
                // Windows arm/arm64 ready-to-run jobs use flow jobs and test jobs, but depend on "normal" (not R2R specific) build jobs.
                return false

            default:
                break
        }
    }
    else if (isCrossGenComparisonScenario(scenario)) {
        return shouldGenerateCrossGenComparisonJob(os, architecture, configuration, scenario)
    }
    else {
        // Skip scenarios
        switch (scenario) {
            case 'ilrt':
                // The ilrt build isn't necessary except for Windows_NT2003.  Non-Windows NT uses
                // the default scenario build
                if (os != 'Windows_NT') {
                    return false
                }
                // Only x64 for now
                if (architecture != 'x64') {
                    return false
                }
                // Release only
                if (configuration != 'Release') {
                    return false
                }
                break
            case 'jitdiff':
                if (os != 'Windows_NT' && os != 'Ubuntu' && os != 'OSX10.12') {
                    return false
                }
                if (architecture != 'x64') {
                    return false
                }
                if (configuration != 'Checked') {
                    return false
                }
                break
            case 'longgc':
            case 'gcsimulator':
                if (os != 'Windows_NT' && os != 'Ubuntu' && os != 'OSX10.12') {
                    return false
                }
                if (architecture != 'x64') {
                    return false
                }
                if (configuration != 'Release') {
                    return false
                }
                break
            case 'gc_reliability_framework':
            case 'standalone_gc':
                if (os != 'Windows_NT' && os != 'Ubuntu' && os != 'OSX10.12') {
                    return false
                }

                if (architecture != 'x64') {
                    return false
                }

                if (configuration != 'Release' && configuration != 'Checked') {
                    return false
                }
                break
            // We only run Windows and Ubuntu x64 Checked for formatting right now
            case 'formatting':
                if (os != 'Windows_NT' && os != 'Ubuntu') {
                    return false
                }
                if (architecture != 'x64') {
                    return false
                }
                if (configuration != 'Checked') {
                    return false
                }
                break
            case 'illink':
                if (os != 'Windows_NT' && (os != 'Ubuntu' || architecture != 'x64')) {
                    return false
                }
                if (architecture != 'x64' && architecture != 'x86') {
                    return false
                }
                break
            case 'normal':
                // Nothing skipped
                break
            case 'innerloop':
                if (!isValidPrTriggeredInnerLoopJob(os, architecture, configuration, isBuildOnly)) {
                    return false
                }
                break
            case 'corefx_innerloop':
                if (os != 'Windows_NT' && os != 'Ubuntu' &&  os != 'OSX10.12') {
                    return false
                }
                if (architecture != 'x64') {
                    return false
                }
                break
            case 'pmi_asm_diffs':
                if (configuration != 'Checked') {
                    return false
                }
                if (architecture == 'armem') {
                    return false
                }
                // Currently, we don't support pmi_asm_diffs for Windows arm/arm64. We don't have a dotnet CLI available to
                // build jitutils. The jobs are not in validArmWindowsScenarios.
                if ((os == 'Windows_NT') && (architecture == 'arm' || architecture == 'arm64')) {
                    return false
                }
                // Currently, no support for Linux x86.
                if ((os != 'Windows_NT') && (architecture == 'x86')) {
                    return false
                }
                break
            default:
                println("Unknown scenario: ${scenario}")
                assert false
                break
        }
    }

    // The job was not filtered out, so we should generate it!
    return true
}

Constants.allScenarios.each { scenario ->
    [true, false].each { isPR ->
        Constants.architectureList.each { architecture ->
            Constants.configurationList.each { configuration ->
                Constants.osList.each { os ->
                    // If the OS is Windows_NT_BuildOnly, set the isBuildOnly flag to true
                    // and reset the os to Windows_NT
                    def isBuildOnly = false
                    if (os == 'Windows_NT_BuildOnly') {
                        isBuildOnly = true
                        os = 'Windows_NT'
                    }

                    if (!shouldGenerateJob(scenario, isPR, architecture, configuration, os, isBuildOnly)) {
                        return
                    }

                    // Calculate names
                    def jobName = getJobName(configuration, architecture, os, scenario, isBuildOnly)
                    def folderName = getJobFolder(scenario)

                    // Create the new job
                    def newJob = job(Utilities.getFullJobName(project, jobName, isPR, folderName)) {}

                    addToViews(newJob, false, isPR, architecture, os, configuration, scenario) // isFlowJob == false

                    setJobMachineAffinity(architecture, os, true, false, false, newJob) // isBuildJob = true, isTestJob = false, isFlowJob = false

                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                    addTriggers(newJob, branch, isPR, architecture, os, configuration, scenario, false, isBuildOnly) // isFlowJob==false
                    setJobTimeout(newJob, isPR, architecture, configuration, scenario, isBuildOnly)

                    // Copy Windows build test binaries and corefx build artifacts for Linux cross build for armem.
                    // We don't use a flow job for this, but we do depend on there being existing builds with these
                    // artifacts produced.
                    if ((architecture == 'armem') && (os == 'Tizen')) {
                        // Define the Windows Tests and Corefx build job names
                        def lowerConfiguration = configuration.toLowerCase()
                        def WindowsTestsName = projectFolder + '/' +
                                               Utilities.getFullJobName(project,
                                                                        getJobName(lowerConfiguration, 'x64' , 'windows_nt', 'normal', true),
                                                                        false)
                        def fxBranch = getFxBranch(branch)
                        def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' +
                                           Utilities.getFolderName(fxBranch)

                        def arm_abi = 'armel'
                        def corefx_os = 'tizen'

                        // Let's use release CoreFX to test checked CoreCLR,
                        // because we do not generate checked CoreFX in CoreFX CI yet.
                        def corefx_lowerConfiguration = lowerConfiguration
                        if (lowerConfiguration == 'checked') {
                            corefx_lowerConfiguration = 'release'
                        }

                        // Copy the Windows test binaries and the Corefx build binaries
                        newJob.with {
                            steps {
                                copyArtifacts(WindowsTestsName) {
                                    includePatterns('bin/tests/tests.zip')
                                    buildSelector {
                                        latestSuccessful(true)
                                    }
                                }
                                copyArtifacts("${corefxFolder}/${corefx_os}_${arm_abi}_cross_${corefx_lowerConfiguration}") {
                                    includePatterns('artifacts/bin/build.tar.gz')
                                    buildSelector {
                                        latestSuccessful(true)
                                    }
                                }
                            } // steps
                        } // newJob.with
                    }

                    def buildCommands = calculateBuildCommands(newJob, scenario, branch, isPR, architecture, configuration, os, isBuildOnly)

                    newJob.with {
                        steps {
                            if (os == 'Windows_NT') {
                                buildCommands.each { buildCommand ->
                                    batchFile(buildCommand)
                                }
                            }
                            else {
                                buildCommands.each { buildCommand ->
                                    shell(buildCommand)
                                }
                            }
                        } // steps
                    } // newJob.with

                } // os
            } // configuration
        } // architecture
    } // isPR
} // scenario

// Create a Windows ARM/ARM64 test job that will be used by a flow job.
// Returns the newly created job.
def static CreateWindowsArmTestJob(def dslFactory, def project, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName)
{
    def osGroup = getOSGroup(os)
    def jobName = getJobName(configuration, architecture, os, scenario, false) + "_tst"

    def jobFolder = getJobFolder(scenario)
    def newJob = dslFactory.job(Utilities.getFullJobName(project, jobName, isPR, jobFolder)) {
        parameters {
            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
        }

        steps {
            // Set up the copies

            // Coreclr build we are trying to test
            //
            //  ** NOTE ** This will, correctly, overwrite the CORE_ROOT from the Windows test archive

            copyArtifacts(inputCoreCLRBuildName) {
                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                buildSelector {
                    buildNumber('${CORECLR_BUILD}')
                }
            }

            if (isCoreFxScenario(scenario)) {

                // Only arm/arm64 supported for corefx testing now.
                assert architecture == 'arm' || architecture == 'arm64'

                // Unzip CoreFx runtime
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('_\\fx\\fxruntime.zip', '_\\fx\\artifacts\\bin\\testhost\\netcoreapp-Windows_NT-Release-${architecture}')\"")

                // Unzip CoreFx tests.
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('_\\fx\\fxtests.zip', '_\\fx\\artifacts\\bin\\tests')\"")

                // Add the script to run the corefx tests
                def corefx_runtime_path   = "%WORKSPACE%\\_\\fx\\artifacts\\bin\\testhost\\netcoreapp-Windows_NT-Release-${architecture}"
                def corefx_tests_dir      = "%WORKSPACE%\\_\\fx\\artifacts\\bin\\tests"
                def corefx_exclusion_file = "%WORKSPACE%\\tests\\${architecture}\\corefx_test_exclusions.txt"
                def exclusionRspPath      = "%WORKSPACE%\\tests\\scripts\\run-corefx-tests-exclusions.txt"
                batchFile("call %WORKSPACE%\\tests\\scripts\\run-corefx-tests.bat ${corefx_runtime_path} ${corefx_tests_dir} ${corefx_exclusion_file} ${architecture} ${exclusionRspPath}")

            } else { // !isCoreFxScenario(scenario)

                // Unzip tests.
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('bin\\tests\\tests.zip', 'bin\\tests\\${osGroup}.${architecture}.${configuration}')\"")

                def buildCommands = ""

                def coreRootLocation = "%WORKSPACE%\\bin\\tests\\Windows_NT.${architecture}.${configuration}\\Tests\\Core_Root"
                def addEnvVariable =  { variable, value -> buildCommands += "set ${variable}=${value}\r\n"}
                def addCommand = { cmd -> buildCommands += "${cmd}\r\n"}

                // Make sure Command Extensions are enabled. Used so %ERRORLEVEL% is available.
                addCommand("SETLOCAL ENABLEEXTENSIONS")

                // For all jobs 
                addEnvVariable("CORE_ROOT", coreRootLocation)
                addEnvVariable("COMPlus_NoGuiOnAssert", "1")
                addEnvVariable("COMPlus_ContinueOnAssert", "0")

                // If we are running a stress mode, we'll set those variables as well
                if (isJitStressScenario(scenario) || isR2RStressScenario(scenario)) {
                    def stressValues = null
                    if (isJitStressScenario(scenario)) {
                        stressValues = Constants.jitStressModeScenarios[scenario]
                    }
                    else {
                        stressValues = Constants.r2rStressScenarios[scenario]
                    }

                    stressValues.each { key, value -> 
                        addEnvVariable(key, value)
                    }
                }

                if (isR2RScenario(scenario)) {
                    // Crossgen the framework assemblies.
                    buildCommands += """
@for %%F in (%CORE_ROOT%\\*.dll) do @call :PrecompileAssembly "%CORE_ROOT%" "%%F" %%~nxF
@goto skip_PrecompileAssembly

:PrecompileAssembly
@REM Skip mscorlib since it is already precompiled.
@if /I "%3" == "mscorlib.dll" exit /b 0
@if /I "%3" == "mscorlib.ni.dll" exit /b 0

"%CORE_ROOT%\\crossgen.exe" /Platform_Assemblies_Paths "%CORE_ROOT%" %2 >nul 2>nul
@if "%errorlevel%" == "-2146230517" (
    echo %2 is not a managed assembly.
) else if "%errorlevel%" == "-2146234344" (
    echo %2 is not a managed assembly.
) else if %errorlevel% neq 0 (
    echo Unable to precompile %2
) else (
    echo Precompiled %2
)
@exit /b 0

:skip_PrecompileAssembly
"""

                    // Set RunCrossGen variable to cause test wrappers to invoke their logic to run
                    // crossgen on tests before running them.
                    addEnvVariable("RunCrossGen", "true")
                } // isR2RScenario(scenario)

                // Run runtest.cmd
                // Do not run generate layout. It will delete the correct CORE_ROOT, and we do not have a correct product
                // dir to copy from.
                def runtestCommand = "call %WORKSPACE%\\tests\\runtest.cmd ${architecture} ${configuration} skipgeneratelayout"

                addCommand("${runtestCommand}")
                addCommand("echo on") // Show the following commands in the log. "echo" doesn't alter the errorlevel.
                addCommand("set saved_errorlevel=%errorlevel%")

                // Collect the test logs collected by xunit. Ignore errors here. We want to collect these even if the run
                // failed for some reason, so it needs to be in this batch file.

                addCommand("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${architecture}.${configuration}\\Reports', '.\\bin\\tests\\testReports.zip')\"");

                // Use the runtest.cmd errorlevel as the script errorlevel.
                addCommand("exit /b %saved_errorlevel%")

                batchFile(buildCommands)
            } // non-corefx testing
        } // steps
    } // job

    if (!isCoreFxScenario(scenario)) {
        def doNotFailIfNothingArchived = true
        def archiveOnlyIfSuccessful = false
        Utilities.addArchival(newJob, "bin/tests/testReports.zip", "", doNotFailIfNothingArchived, archiveOnlyIfSuccessful)

        Utilities.addXUnitDotNETResults(newJob, 'bin/**/TestRun*.xml', true)
    }

    return newJob
}

// Create a test job not covered by the "Windows ARM" case that will be used by a flow job.
// E.g., non-Windows tests.
// Returns the newly created job.
def static CreateOtherTestJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName)
{
    def lowerConfiguration = configuration.toLowerCase()

    def isUbuntuArm64Job = ((os == "Ubuntu16.04") && (architecture == 'arm64'))
    def isUbuntuArm32Job = ((os == "Ubuntu") && (architecture == 'arm'))
    def isUbuntuArmJob = isUbuntuArm32Job || isUbuntuArm64Job

    def doCoreFxTesting = isCoreFxScenario(scenario)
    def isPmiAsmDiffsScenario = (scenario == 'pmi_asm_diffs')

    def workspaceRelativeFxRootLinux = "_/fx" // only used for CoreFX testing

    def osGroup = getOSGroup(os)
    def jobName = getJobName(configuration, architecture, os, scenario, false) + "_tst"

    def testOpts = ''
    def useServerGC = false

    // Enable Server GC for Ubuntu PR builds
    // REVIEW: why? Does this apply to all architectures? Why only PR?
    if (os == 'Ubuntu' && isPR) {
        testOpts += ' --useServerGC'
        useServerGC = true
    }

    if (isR2RScenario(scenario)) {

        testOpts += ' --crossgen --runcrossgentests'

        if (scenario == 'r2r_jitstress1') {
            testOpts += ' --jitstress=1'
        }
        else if (scenario == 'r2r_jitstress2') {
            testOpts += ' --jitstress=2'
        }
        else if (scenario == 'r2r_jitstress1_tiered') {
            testOpts += ' --jitstress=1'
        }
        else if (scenario == 'r2r_jitstress2_tiered') {
            testOpts += ' --jitstress=2'
        }
        else if (scenario == 'r2r_jitstressregs1') {
            testOpts += ' --jitstressregs=1'
        }
        else if (scenario == 'r2r_jitstressregs2') {
            testOpts += ' --jitstressregs=2'
        }
        else if (scenario == 'r2r_jitstressregs3') {
            testOpts += ' --jitstressregs=3'
        }
        else if (scenario == 'r2r_jitstressregs4') {
            testOpts += ' --jitstressregs=4'
        }
        else if (scenario == 'r2r_jitstressregs8') {
            testOpts += ' --jitstressregs=8'
        }
        else if (scenario == 'r2r_jitstressregs0x10') {
            testOpts += ' --jitstressregs=0x10'
        }
        else if (scenario == 'r2r_jitstressregs0x80') {
            testOpts += ' --jitstressregs=0x80'
        }
        else if (scenario == 'r2r_jitstressregs0x1000') {
            testOpts += ' --jitstressregs=0x1000'
        }
        else if (scenario == 'r2r_jitminopts') {
            testOpts += ' --jitminopts'
        }
        else if (scenario == 'r2r_jitforcerelocs') {
            testOpts += ' --jitforcerelocs'
        }
        else if (scenario == 'r2r_gcstress15') {
            testOpts += ' --gcstresslevel=0xF'
        }
    }
    else if (scenario == 'jitdiff') {
        testOpts += ' --jitdisasm --crossgen'
    }
    else if (scenario == 'illink') {
        testOpts += ' --link=\$WORKSPACE/linker/linker/bin/netcore_Release/netcoreapp2.0/ubuntu-x64/publish/illink'
    }
    else if (isLongGc(scenario)) {
        // Long GC tests behave very poorly when they are not
        // the only test running (many of them allocate until OOM).
        testOpts += ' --sequential'

        // A note - runtest.sh does have "--long-gc" and "--gcsimulator" options
        // for running long GC and GCSimulator tests, respectively. We don't use them
        // here because using a playlist file produces much more readable output on the CI machines
        // and reduces running time.
        //
        // The Long GC playlist contains all of the tests that are
        // going to be run. The GCSimulator playlist contains all of
        // the GC simulator tests.
        if (scenario == 'longgc') {
            testOpts += ' --long-gc --playlist=./tests/longRunningGcTests.txt'
        }
        else if (scenario == 'gcsimulator') {
            testOpts += ' --gcsimulator --playlist=./tests/gcSimulatorTests.txt'
        }
    }
    else if (isGcReliabilityFramework(scenario)) {
        testOpts += ' --build-overlay-only'
    }
    else if (scenario == 'standalone_gc') {
        if (osGroup == 'OSX') {
            testOpts += ' --gcname=libclrgc.dylib'
        }
        else if (osGroup == 'Linux') {
            testOpts += ' --gcname=libclrgc.so'
        }
        else {
            println("Unexpected OS group: ${osGroup} for os ${os}")
            assert false
        }
    }

    def jobFolder = getJobFolder(scenario)
    def newJob = dslFactory.job(Utilities.getFullJobName(project, jobName, isPR, jobFolder)) {
        parameters {
            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
        }

        steps {
            // Set up the copies

            // Coreclr build we are trying to test
            //
            // HACK: the Ubuntu arm64 copyArtifacts Jenkins plug-in is ridiculously slow (45 minutes to
            // 1.5 hours for this step). Instead, directly use wget, which is fast (1 minute).

            if (!isUbuntuArm64Job) {
                copyArtifacts(inputCoreCLRBuildName) {
                    excludePatterns('**/testResults.xml', '**/*.ni.dll')
                    buildSelector {
                        buildNumber('${CORECLR_BUILD}')
                    }
                }
            }

            if (isUbuntuArmJob) {
                // Add some useful information to the log file. Ignore return codes.
                shell("uname -a || true")
            }

            if (isUbuntuArm64Job) {
                // Copy the required artifacts directly, using wget, e.g.:
                // 
                //  https://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_checked_ubuntu16.04_innerloop_prtest/16/artifact/testnativebin.checked.zip
                //  https://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_checked_ubuntu16.04_innerloop_prtest/16/artifact/tests.checked.zip
                // 
                // parameterized as:
                //
                //  https://ci.dot.net/job/${mungedProjectName}/job/${mungedBranchName}/job/${inputJobName}/${CORECLR_BUILD}/artifact/testnativebin.checked.zip
                //  https://ci.dot.net/job/${mungedProjectName}/job/${mungedBranchName}/job/${inputJobName}/${CORECLR_BUILD}/artifact/tests.checked.zip
                //
                // CoreFX example artifact URLs:
                //
                //  https://ci.dot.net/job/dotnet_coreclr/job/dev_unix_test_workflow/job/jitstress/job/arm64_cross_checked_ubuntu16.04_corefx_baseline_prtest/1/artifact/_/fx/fxruntime.zip
                //  https://ci.dot.net/job/dotnet_coreclr/job/dev_unix_test_workflow/job/jitstress/job/arm64_cross_checked_ubuntu16.04_corefx_baseline_prtest/1/artifact/_/fx/fxtests.zip
                //
                // Note that the source might be in a "jitstress" folder.
                //
                // Use `--progress=dot:giga` to display some progress output, but limit it in the log file.
                //
                // Use `--directory-prefix=_/fx` to specify where to put the corefx files (to match what other platforms do). Use this instead of `-O`.

                shell("echo \"Using wget instead of the Jenkins copy artifacts plug-in to copy artifacts from ${inputCoreCLRBuildName}\"")

                def mungedProjectName = Utilities.getFolderName(project)
                def mungedBranchName = Utilities.getFolderName(branch)

                def doCrossGenComparison = isCrossGenComparisonScenario(scenario)
                def inputCoreCLRBuildScenario = isInnerloopTestScenario(scenario) ? 'innerloop' : 'normal'
                if (isPmiAsmDiffsScenario || doCoreFxTesting || doCrossGenComparison) {
                    // These depend on unique builds for each scenario
                    inputCoreCLRBuildScenario = scenario
                }
                def sourceJobName = getJobName(configuration, architecture, os, inputCoreCLRBuildScenario, false)
                def inputJobName = Utilities.getFullJobName(sourceJobName, isPR)

                // Need to add the sub-folder if necessary.
                def inputJobPath = "job/${inputJobName}"
                def folderName = getJobFolder(inputCoreCLRBuildScenario)
                if (folderName != '') {
                    inputJobPath = "job/${folderName}/job/${inputJobName}"
                }

                def inputUrlRoot = "https://ci.dot.net/job/${mungedProjectName}/job/${mungedBranchName}/${inputJobPath}/\${CORECLR_BUILD}/artifact"

                if (isPmiAsmDiffsScenario) {
                    def workspaceRelativeRootLinux = "_/pmi"
                    shell("mkdir -p ${workspaceRelativeRootLinux}")
                    shell("wget --progress=dot:giga ${inputUrlRoot}/product.${os}.${architecture}.${lowerConfiguration}.tgz")
                    shell("wget --progress=dot:giga ${inputUrlRoot}/product.baseline.${os}.${architecture}.${lowerConfiguration}.tgz")
                    shell("wget --progress=dot:giga ${inputUrlRoot}/coreroot.${os}.${architecture}.${lowerConfiguration}.tgz")
                    shell("wget --progress=dot:giga ${inputUrlRoot}/coreroot.baseline.${os}.${architecture}.${lowerConfiguration}.tgz")
                }
                else if (doCoreFxTesting) {
                    shell("mkdir -p ${workspaceRelativeFxRootLinux}")
                    shell("wget --progress=dot:giga --directory-prefix=${workspaceRelativeFxRootLinux} ${inputUrlRoot}/${workspaceRelativeFxRootLinux}/fxtests.zip")
                    shell("wget --progress=dot:giga --directory-prefix=${workspaceRelativeFxRootLinux} ${inputUrlRoot}/${workspaceRelativeFxRootLinux}/fxruntime.zip")
                }
                else {
                    def testArtifactsTgzFileName = getTestArtifactsTgzFileName(osGroup, architecture, configuration)
                    shell("wget --progress=dot:giga ${inputUrlRoot}/${testArtifactsTgzFileName}")
                }
            }

            if (architecture == 'x86') {
                shell("mkdir ./bin/CoreFxNative")

                def fxBranch = getFxBranch(branch)
                def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' + Utilities.getFolderName(fxBranch)

                copyArtifacts("${corefxFolder}/ubuntu16.04_x86_release") {
                    includePatterns('artifacts/bin/build.tar.gz')
                    targetDirectory('bin/CoreFxNative')
                    buildSelector {
                        latestSuccessful(true)
                    }
                }

                shell("mkdir ./bin/CoreFxBinDir")
                shell("tar -xf ./bin/CoreFxNative/artifacts/bin/build.tar.gz -C ./bin/CoreFxBinDir")
            }

            if (isPmiAsmDiffsScenario) {
                shell("tar -xzf ./product.${os}.${architecture}.${lowerConfiguration}.tgz || exit 0")
                shell("tar -xzf ./product.baseline.${os}.${architecture}.${lowerConfiguration}.tgz || exit 0")
                shell("tar -xzf ./coreroot.${os}.${architecture}.${lowerConfiguration}.tgz || exit 0")
                shell("tar -xzf ./coreroot.baseline.${os}.${architecture}.${lowerConfiguration}.tgz || exit 0")
            }
            // CoreFX testing downloads the CoreFX tests, not the coreclr tests. Also, unzip the built CoreFX layout/runtime directories.
            else if (doCoreFxTesting) {
                shell("unzip -q -o ${workspaceRelativeFxRootLinux}/fxtests.zip || exit 0")
                shell("unzip -q -o ${workspaceRelativeFxRootLinux}/fxruntime.zip || exit 0")
            }
            else {
                def testArtifactsTgzFileName = getTestArtifactsTgzFileName(osGroup, architecture, configuration)
                shell("tar -xzf ./${testArtifactsTgzFileName} || exit 0") // extracts to ./bin/tests/${osGroup}.${architecture}.${configuration}
            }

            // Execute the tests
            def runDocker = isNeedDocker(architecture, os, false)
            def dockerPrefix = ""
            def dockerCmd = ""
            if (runDocker) {
                def dockerImage = getDockerImageName(architecture, os, false)
                dockerPrefix = "docker run -i --rm -v \${WORKSPACE}:\${WORKSPACE} -w \${WORKSPACE} "
                dockerCmd = dockerPrefix + "${dockerImage} "
            }

            // If we are running a stress mode, we'll set those variables first.
            // For CoreFX, the stress variables are already built into the CoreFX test build per-test wrappers.
            if (!doCoreFxTesting && isJitStressScenario(scenario)) {
                def scriptFileName = "\${WORKSPACE}/set_stress_test_env.sh"
                def envScriptCmds = envScriptCreate(os, scriptFileName)
                envScriptCmds += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], scriptFileName)
                envScriptCmds += envScriptFinalize(os, scriptFileName)
                shell("${envScriptCmds}")
                testOpts += " --test-env=${scriptFileName}"
            }

            // setup-stress-dependencies.sh, invoked by runtest.sh to download the coredistools package, depends on the "dotnet"
            // tool downloaded by the "init-tools.sh" script. However, it only invokes setup-stress-dependencies.sh for x64. The
            // coredistools package is used by GCStress on x86 and x64 to disassemble code to determine instruction boundaries.
            // On arm/arm64, it is not required as determining instruction boundaries is trivial.
            if (isGCStressRelatedTesting(scenario)) {
                if (architecture == 'x64') {
                    shell('./init-tools.sh')
                }
            }

            if (isPmiAsmDiffsScenario) {
                shell("""\
python -u \${WORKSPACE}/tests/scripts/run-pmi-diffs.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} --skip_baseline_build""")

                shell("tar -czf dasm.${os}.${architecture}.${configuration}.tgz ./_/pmi/asm")
            }
            else if (doCoreFxTesting) {
                def exclusionRspPath = "\${WORKSPACE}/tests/scripts/run-corefx-tests-exclusions.txt"
                shell("""\
\${WORKSPACE}/tests/scripts/run-corefx-tests.sh --test-exclude-file \${WORKSPACE}/tests/${architecture}/corefx_linux_test_exclusions.txt --runtime \${WORKSPACE}/${workspaceRelativeFxRootLinux}/artifacts/bin/testhost/netcoreapp-Linux-Release-${architecture} --arch ${architecture} --corefx-tests \${WORKSPACE}/${workspaceRelativeFxRootLinux}/artifacts/bin --configurationGroup Release --exclusion-rsp-file ${exclusionRspPath}""")
            }
            else {
                def runScript = "${dockerCmd}./tests/runtest.sh"

                shell("""\
${runScript} \\
    ${lowerConfiguration} \\
    --testRootDir=\"\${WORKSPACE}/bin/tests/${osGroup}.${architecture}.${configuration}\" \\
    --coreOverlayDir=\"\${WORKSPACE}/bin/tests/${osGroup}.${architecture}.${configuration}/Tests/Core_Root\" \\
    --limitedDumpGeneration ${testOpts}""")
            }

            if (isGcReliabilityFramework(scenario)) {
                // runtest.sh doesn't actually execute the reliability framework - do it here.
                if (useServerGC) {
                    if (runDocker) {
                        dockerCmd = dockerPrefix + "-e COMPlus_gcServer=1 ${dockerImage} "
                    }
                    else {
                        shell("export COMPlus_gcServer=1")
                    }
                }

                shell("${dockerCmd}./tests/scripts/run-gc-reliability-framework.sh ${architecture} ${configuration}")
            }
        } // steps
    } // job

    // Experimental: If on Ubuntu 14.04, then attempt to pull in crash dump links
    if (os in ['Ubuntu']) {
        SummaryBuilder summaries = new SummaryBuilder()
        summaries.addLinksSummaryFromFile('Crash dumps from this run:', 'dumplings.txt')
        summaries.emit(newJob)
    }

    if (isPmiAsmDiffsScenario) {
        // Archive the asm
        Utilities.addArchival(newJob, "dasm.${os}.${architecture}.${configuration}.tgz")
    }
    else if (doCoreFxTesting) {
        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/artifacts/bin/**/testResults.xml", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
        if ((os == "Ubuntu") && (architecture == 'arm')) {
            // We have a problem with the xunit plug-in, where it is consistently failing on Ubuntu arm32 test result uploading with this error:
            //
            //   [xUnit] [ERROR] - The plugin hasn't been performed correctly: remote file operation failed: /ssd/j/workspace/dotnet_coreclr/master/jitstress/arm_cross_checked_ubuntu_corefx_baseline_tst at hudson.remoting.Channel@3697f46d:JNLP4-connect connection from 131.107.159.149/131.107.159.149:58529: java.io.IOException: Remote call on JNLP4-connect connection from 131.107.159.149/131.107.159.149:58529 failed
            //
            // We haven't been able to identify the reason. So, do not add xunit parsing of the test data in this scenario.
            // This is tracked by: https://github.com/dotnet/coreclr/issues/19447.
        }
        else {
            Utilities.addXUnitDotNETResults(newJob, "${workspaceRelativeFxRootLinux}/artifacts/bin/**/testResults.xml")
        }
    }
    else {
        Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')
    }

    return newJob
}

def static CreateNonWindowsCrossGenComparisonTestJob(def dslFactory, def project, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName)
{
    assert isCrossGenComparisonScenario(scenario)

    def osGroup = getOSGroup(os)
    def jobName = getJobName(configuration, architecture, os, scenario, false) + "_tst"

    def workspaceRelativeResultsDir = "_"
    def workspaceRelativeNativeArchResultDir = "${workspaceRelativeResultsDir}/${osGroup}.${architecture}_${architecture}.${configuration}"

    def crossArch = "x64"
    def workspaceRelativeCrossArchResultDir = "${workspaceRelativeResultsDir}/${osGroup}.${crossArch}_${architecture}.${configuration}"

    def jobFolder = getJobFolder(scenario)
    def newJob = dslFactory.job(Utilities.getFullJobName(project, jobName, isPR, jobFolder)) {
        parameters {
            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
        }

        def workspaceRelativeArtifactsArchive = "${os}.${architecture}.${configuration}.${scenario}.zip"

        steps {
            copyArtifacts(inputCoreCLRBuildName) {
                includePatterns("${workspaceRelativeArtifactsArchive}")
                buildSelector {
                    buildNumber('${CORECLR_BUILD}')
                }
            }

            shell("unzip -o ${workspaceRelativeArtifactsArchive} || exit 0")

            def workspaceRelativeCoreLib = "bin/Product/${osGroup}.${architecture}.${configuration}/IL/System.Private.CoreLib.dll"
            def workspaceRelativeCoreRootDir = "bin/tests/${osGroup}.${architecture}.${configuration}/Tests/Core_Root"
            def workspaceRelativeCrossGenComparisonScript = "tests/scripts/crossgen_comparison.py"
            def workspaceRelativeCrossGenExecutable = "${workspaceRelativeCoreRootDir}/crossgen"

            def crossGenComparisonCmd = "python -u \${WORKSPACE}/${workspaceRelativeCrossGenComparisonScript} "
            def crossGenExecutable = "\${WORKSPACE}/${workspaceRelativeCrossGenExecutable}"

            shell("mkdir -p ${workspaceRelativeNativeArchResultDir}")
            shell("${crossGenComparisonCmd}crossgen_corelib --crossgen ${crossGenExecutable} --il_corelib \${WORKSPACE}/${workspaceRelativeCoreLib} --result_dir \${WORKSPACE}/${workspaceRelativeNativeArchResultDir}")
            shell("${crossGenComparisonCmd}crossgen_framework --crossgen ${crossGenExecutable} --core_root \${WORKSPACE}/${workspaceRelativeCoreRootDir} --result_dir \${WORKSPACE}/${workspaceRelativeNativeArchResultDir}")

            shell("${crossGenComparisonCmd}compare --base_dir \${WORKSPACE}/${workspaceRelativeNativeArchResultDir} --diff_dir \${WORKSPACE}/${workspaceRelativeCrossArchResultDir}")
        } // steps
    }  // job

    Utilities.addArchival(newJob, "${workspaceRelativeNativeArchResultDir}/**", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)
    Utilities.addArchival(newJob, "${workspaceRelativeCrossArchResultDir}/**", "", /* doNotFailIfNothingArchived */ true, /* archiveOnlyIfSuccessful */ false)

    return newJob
}

// Create a test job that will be used by a flow job.
// Returns the newly created job.
// Note that we don't add tests jobs to the various views, since they are always used by a flow job, which is in the views,
// and we want the views to be the minimal set of "top-level" jobs that represent all work.
def static CreateTestJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName)
{
    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))

    def newJob = null
    if (windowsArmJob) {
        newJob = CreateWindowsArmTestJob(dslFactory, project, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName)
    }
    else if (isCrossGenComparisonScenario(scenario)) {
        newJob = CreateNonWindowsCrossGenComparisonTestJob(dslFactory, project, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName)
    }
    else {
        newJob = CreateOtherTestJob(dslFactory, project, branch, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName)
    }

    setJobMachineAffinity(architecture, os, false, true, false, newJob) // isBuildJob = false, isTestJob = true, isFlowJob = false

    if (scenario == 'jitdiff') {
        def osGroup = getOSGroup(os)
        Utilities.addArchival(newJob, "bin/tests/${osGroup}.${architecture}.${configuration}/dasm/**")
    }

    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
    setJobTimeout(newJob, isPR, architecture, configuration, scenario, false)

    return newJob
}

// Create a flow job to tie together a build job with the given test job.
// Returns the new flow job.
def static CreateFlowJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def fullTestJobName, def inputCoreCLRBuildName)
{
    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
    // Linux CoreCLR test
    def flowJobName = getJobName(configuration, architecture, os, scenario, false) + "_flow"
    def jobFolder = getJobFolder(scenario)

    def newFlowJob = dslFactory.buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR, jobFolder)) {
        buildFlow("""\
coreclrBuildJob = build(params, '${inputCoreCLRBuildName}')

// And then build the test build
build(params + [CORECLR_BUILD: coreclrBuildJob.build.number], '${fullTestJobName}')
""")
    }
    JobReport.Report.addReference(inputCoreCLRBuildName)
    JobReport.Report.addReference(fullTestJobName)

    addToViews(newFlowJob, true, isPR, architecture, os, configuration, scenario) // isFlowJob = true

    setJobMachineAffinity(architecture, os, false, false, true, newFlowJob) // isBuildJob = false, isTestJob = false, isFlowJob = true

    Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")
    addTriggers(newFlowJob, branch, isPR, architecture, os, configuration, scenario, true, false) // isFlowJob==true, isWindowsBuildOnlyJob==false

    return newFlowJob
}

// Determine if we should generate a flow job for the given parameters.
// Returns true if the job should be generated.
def static shouldGenerateFlowJob(def scenario, def isPR, def architecture, def configuration, def os)
{
    // The various "innerloop" jobs are only available as PR triggered.

    if (!isPR) {
        if (isInnerloopTestScenario(scenario)) {
            return false
        }

        if (scenario == 'corefx_innerloop') {
            return false
        }
    }

    // Disable flow jobs for innerloop pr.
    //
    // The only exception is windows arm(64)
    if (isInnerloopTestScenario(scenario) && isPR && os != 'Windows_NT') {
        assert scenario != 'corefx_innerloop'

        return false;
    }

    // Filter based on OS and architecture.

    switch (architecture) {
        case 'arm':
            if (os != "Ubuntu" && os != "Windows_NT") {
                return false
            }
            break
        case 'arm64':
            if (os != "Ubuntu16.04" && os != "Windows_NT") {
                return false
            }
            break
        case 'x86':
            if (os != "Ubuntu") {
                return false
            }
            break
        case 'x64':
            if (!(os in Constants.crossList)) {
                return false
            }
            if (os == "Windows_NT") {
                return false
            }
            break
        case 'armem':
            // No flow jobs
            return false
        default:
            println("Unknown architecture: ${architecture}")
            assert false
            break
    }

    def isNormalOrInnerloop = (scenario == 'innerloop' || scenario == 'normal')

    // Filter based on scenario in OS.

    if (os == 'Windows_NT') {
        assert architecture == 'arm' || architecture == 'arm64'
        if (!isArmWindowsScenario(scenario)) {
            return false
        }
        if (isNormalOrInnerloop && (configuration == 'Debug')) {
            // The arm32/arm64 Debug configuration for innerloop/normal scenario is a special case: it does a build only, and no test run.
            // To do that, it doesn't require a flow job.
            return false
        }
    }
    else {
        // Non-Windows
        if (architecture == 'arm') {
            if (!(scenario in Constants.validLinuxArmScenarios)) {
                return false
            }
        }
        else if (architecture == 'arm64') {
            if (!(scenario in Constants.validLinuxArm64Scenarios)) {
                return false
            }
        }
        else if (architecture == 'x86') {
            // Linux/x86 only want innerloop and default test
            if (!isNormalOrInnerloop) {
                return false
            }
        }
        else if (architecture == 'x64') {
            // Linux/x64 corefx testing doesn't need a flow job; the "build" job runs run-corefx-tests.py which
            // builds and runs the corefx tests. Other Linux/x64 flow jobs are required to get the test
            // build from a Windows machine.
            if (isCoreFxScenario(scenario)) {
                return false
            }
        }
    }

    // For CentOS, we only want Checked/Release builds.
    if (os == 'CentOS7.1') {
        if (configuration != 'Checked' && configuration != 'Release') {
            return false
        }
        if (!isNormalOrInnerloop && !isR2RScenario(scenario)) {
            return false
        }
    }

    // For RedHat and Debian, we only do Release builds.
    else if (os == 'RHEL7.2' || os == 'Debian8.4') {
        if (configuration != 'Release') {
            return false
        }
        if (!isNormalOrInnerloop) {
            return false
        }
    }

    // Next, filter based on scenario.

    if (isJitStressScenario(scenario)) {
        if (configuration != 'Checked') {
            return false
        }
    }
    else if (isR2RBaselineScenario(scenario)) {
        if (configuration != 'Checked' && configuration != 'Release') {
            return false
        }
    }
    else if (isR2RStressScenario(scenario)) {
        if (configuration != 'Checked') {
            return false
        }
    }
    else if (isCrossGenComparisonScenario(scenario)) {
        return shouldGenerateCrossGenComparisonJob(os, architecture, configuration, scenario)
    }
    else {
        // Skip scenarios
        switch (scenario) {
            case 'ilrt':
            case 'longgc':
            case 'gcsimulator':
                // Long GC tests take a long time on non-Release builds
                // ilrt is also Release only
                if (configuration != 'Release') {
                    return false
                }
                break

            case 'jitdiff':
                if (configuration != 'Checked') {
                    return false
                }
                break

            case 'gc_reliability_framework':
            case 'standalone_gc':
                if (configuration != 'Release' && configuration != 'Checked') {
                    return false
                }
                break

            case 'formatting':
                return false

            case 'illink':
                if (os != 'Windows_NT' && os != 'Ubuntu') {
                    return false
                }
                break

            case 'normal':
                // Nothing skipped
                break

            case 'innerloop':
                if (!isValidPrTriggeredInnerLoopJob(os, architecture, configuration, false)) {
                    return false
                }
                break

            case 'pmi_asm_diffs':
                if (configuration != 'Checked') {
                    return false
                }
                // No need for flow job except for Linux arm/arm64
                if ((os != 'Windows_NT') && (architecture != 'arm') && (architecture != 'arm64')) {
                    return false
                }
                break

            case 'corefx_innerloop':
                // No flow job needed
                return false

            default:
                println("Unknown scenario: ${scenario}")
                assert false
                break
        }
    }

    // The job was not filtered out, so we should generate it!
    return true
}

// Create jobs requiring flow jobs. This includes x64 non-Windows, arm/arm64 Ubuntu, and arm/arm64 Windows.
Constants.allScenarios.each { scenario ->
    [true, false].each { isPR ->
        Constants.architectureList.each { architecture ->
            Constants.configurationList.each { configuration ->
                Constants.osList.each { os ->

                    if (!shouldGenerateFlowJob(scenario, isPR, architecture, configuration, os)) {
                        return
                    }

                    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))
                    def doCoreFxTesting = isCoreFxScenario(scenario)
                    def doCrossGenComparison = isCrossGenComparisonScenario(scenario)
                    def isPmiAsmDiffsScenario = (scenario == 'pmi_asm_diffs')

                    // Figure out the job name of the CoreCLR build the test will depend on.

                    def inputCoreCLRBuildScenario = isInnerloopTestScenario(scenario) ? 'innerloop' : 'normal'
                    def inputCoreCLRBuildIsBuildOnly = false
                    if (doCoreFxTesting || isPmiAsmDiffsScenario) {
                        // Every CoreFx test depends on its own unique build.
                        inputCoreCLRBuildScenario = scenario
                        if (windowsArmJob) {
                            // Only Windows ARM corefx jobs use "build only" jobs. Others, such as Ubuntu ARM corefx, use "regular" jobs.
                            inputCoreCLRBuildIsBuildOnly = true
                        }
                    }
                    else if (doCrossGenComparison) {
                        inputCoreCLRBuildScenario = scenario
                    }

                    def inputCoreCLRFolderName = getJobFolder(inputCoreCLRBuildScenario)
                    def inputCoreCLRBuildName = projectFolder + '/' +
                        Utilities.getFullJobName(project, getJobName(configuration, architecture, os, inputCoreCLRBuildScenario, inputCoreCLRBuildIsBuildOnly), isPR, inputCoreCLRFolderName)

                    // =============================================================================================
                    // Create the test job
                    // =============================================================================================

                    def testJob = CreateTestJob(this, project, branch, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName)

                    // =============================================================================================
                    // Create a build flow to join together the build and tests required to run this test.
                    // =============================================================================================

                    if (os == 'RHEL7.2' || os == 'Debian8.4') {
                        // Do not create the flow job for RHEL jobs.
                        return
                    }

                    def fullTestJobName = projectFolder + '/' + testJob.name
                    def flowJob = CreateFlowJob(this, project, branch, architecture, os, configuration, scenario, isPR, fullTestJobName, inputCoreCLRBuildName)

                } // os
            } // configuration
        } // architecture
    } // isPR
} // scenario

JobReport.Report.generateJobReport(out)

// Make the call to generate the help job
Utilities.createHelperJob(this, project, branch,
    "Welcome to the ${project} Repository",  // This is prepended to the help message
    "Have a nice day!")  // This is appended to the help message.  You might put known issues here.

Utilities.addCROSSCheck(this, project, branch)
