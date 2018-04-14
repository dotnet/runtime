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
    def osGroupMap = ['Ubuntu':'Linux',
        'RHEL7.2': 'Linux',
        'Ubuntu16.04': 'Linux',
        'Ubuntu16.10': 'Linux',
        'Debian8.4':'Linux',
        'Fedora24':'Linux',
        'OSX10.12':'OSX',
        'Windows_NT':'Windows_NT',
        'CentOS7.1': 'Linux',
        'Tizen': 'Linux']
    def osGroup = osGroupMap.get(os, null)
    assert osGroup != null : "Could not find os group for ${os}"
    return osGroupMap[os]
}

// We use this class (vs variables) so that the static functions can access data here.
class Constants {

    // We have very limited ARM64 hardware (used for ARM/ARMLB/ARM64 testing). So only allow certain branches to use it.
    def static WindowsArm64Branches = [
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
               'minopts'                        : ['COMPlus_JITMinOpts' : '1'],
               'tieredcompilation'              : ['COMPlus_EXPERIMENTAL_TieredCompilation' : '1'],
               'forcerelocs'                    : ['COMPlus_ForceRelocs' : '1'],
               'jitstress1'                     : ['COMPlus_JitStress' : '1'],
               'jitstress2'                     : ['COMPlus_JitStress' : '2'],
               'jitstressregs1'                 : ['COMPlus_JitStressRegs' : '1'],
               'jitstressregs2'                 : ['COMPlus_JitStressRegs' : '2'],
               'jitstressregs3'                 : ['COMPlus_JitStressRegs' : '3'],
               'jitstressregs4'                 : ['COMPlus_JitStressRegs' : '4'],
               'jitstressregs8'                 : ['COMPlus_JitStressRegs' : '8'],
               'jitstressregs0x10'              : ['COMPlus_JitStressRegs' : '0x10'],
               'jitstressregs0x80'              : ['COMPlus_JitStressRegs' : '0x80'],
               'jitstressregs0x1000'            : ['COMPlus_JitStressRegs' : '0x1000'],
               'jitstress2_jitstressregs1'      : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '1'],
               'jitstress2_jitstressregs2'      : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '2'],
               'jitstress2_jitstressregs3'      : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '3'],
               'jitstress2_jitstressregs4'      : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '4'],
               'jitstress2_jitstressregs8'      : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '8'],
               'jitstress2_jitstressregs0x10'   : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x10'],
               'jitstress2_jitstressregs0x80'   : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x80'],
               'jitstress2_jitstressregs0x1000' : ['COMPlus_JitStress' : '2', 'COMPlus_JitStressRegs' : '0x1000'],
               'tailcallstress'                 : ['COMPlus_TailcallStress' : '1'],
               'jitsse2only'                    : ['COMPlus_EnableAVX' : '0', 'COMPlus_EnableSSE3_4' : '0'],
               'jitnosimd'                      : ['COMPlus_FeatureSIMD' : '0'],
               'jitincompletehwintrinsic'       : ['COMPlus_EnableIncompleteISAClass' : '1'],
               'jitx86hwintrinsicnoavx'         : ['COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableAVX' : '0'], // testing the legacy SSE encoding
               'jitx86hwintrinsicnoavx2'        : ['COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableAVX2' : '0'], // testing SNB/IVB
               'jitx86hwintrinsicnosimd'        : ['COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_FeatureSIMD' : '0'], // match "jitnosimd", may need to remove after decoupling HW intrinsic from FeatureSIMD
               'jitnox86hwintrinsic'            : ['COMPlus_EnableIncompleteISAClass' : '1', 'COMPlus_EnableSSE' : '0' , 'COMPlus_EnableSSE2' : '0' , 'COMPlus_EnableSSE3' : '0' , 'COMPlus_EnableSSSE3' : '0' , 'COMPlus_EnableSSE41' : '0' , 'COMPlus_EnableSSE42' : '0' , 'COMPlus_EnableAVX' : '0' , 'COMPlus_EnableAVX2' : '0' , 'COMPlus_EnableAES' : '0' , 'COMPlus_EnableBMI1' : '0' , 'COMPlus_EnableBMI2' : '0' , 'COMPlus_EnableFMA' : '0' , 'COMPlus_EnableLZCNT' : '0' , 'COMPlus_EnablePCLMULQDQ' : '0' , 'COMPlus_EnablePOPCNT' : '0'],
               'corefx_baseline'                : [ : ], // corefx baseline
               'corefx_minopts'                 : ['COMPlus_JITMinOpts' : '1'],
               'corefx_tieredcompilation'       : ['COMPlus_EXPERIMENTAL_TieredCompilation' : '1'],
               'corefx_jitstress1'              : ['COMPlus_JitStress' : '1'],
               'corefx_jitstress2'              : ['COMPlus_JitStress' : '2'],
               'corefx_jitstressregs1'          : ['COMPlus_JitStressRegs' : '1'],
               'corefx_jitstressregs2'          : ['COMPlus_JitStressRegs' : '2'],
               'corefx_jitstressregs3'          : ['COMPlus_JitStressRegs' : '3'],
               'corefx_jitstressregs4'          : ['COMPlus_JitStressRegs' : '4'],
               'corefx_jitstressregs8'          : ['COMPlus_JitStressRegs' : '8'],
               'corefx_jitstressregs0x10'       : ['COMPlus_JitStressRegs' : '0x10'],
               'corefx_jitstressregs0x80'       : ['COMPlus_JitStressRegs' : '0x80'],
               'corefx_jitstressregs0x1000'     : ['COMPlus_JitStressRegs' : '0x1000'],
               'gcstress0x3'                    : ['COMPlus_GCStress' : '0x3'],
               'gcstress0xc'                    : ['COMPlus_GCStress' : '0xC'],
               'zapdisable'                     : ['COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0'],
               'heapverify1'                    : ['COMPlus_HeapVerify' : '1'],
               'gcstress0xc_zapdisable'             : ['COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0'],
               'gcstress0xc_zapdisable_jitstress2'  : ['COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_zapdisable_heapverify1' : ['COMPlus_GCStress' : '0xC', 'COMPlus_ZapDisable' : '1', 'COMPlus_ReadyToRun' : '0', 'COMPlus_HeapVerify' : '1'],
               'gcstress0xc_jitstress1'             : ['COMPlus_GCStress' : '0xC', 'COMPlus_JitStress'  : '1'],
               'gcstress0xc_jitstress2'             : ['COMPlus_GCStress' : '0xC', 'COMPlus_JitStress'  : '2'],
               'gcstress0xc_minopts_heapverify1'    : ['COMPlus_GCStress' : '0xC', 'COMPlus_JITMinOpts' : '1', 'COMPlus_HeapVerify' : '1']
    ]

    // This is a set of ReadyToRun stress scenarios
    def static r2rStressScenarios = [
               'r2r_jitstress1'             : ["COMPlus_JitStress": "1"],
               'r2r_jitstress2'             : ["COMPlus_JitStress": "2"],
               'r2r_jitstressregs1'         : ["COMPlus_JitStressRegs": "1"],
               'r2r_jitstressregs2'         : ["COMPlus_JitStressRegs": "2"],
               'r2r_jitstressregs3'         : ["COMPlus_JitStressRegs": "3"],
               'r2r_jitstressregs4'         : ["COMPlus_JitStressRegs": "4"],
               'r2r_jitstressregs8'         : ["COMPlus_JitStressRegs": "8"],
               'r2r_jitstressregs0x10'      : ["COMPlus_JitStressRegs": "0x10"],
               'r2r_jitstressregs0x80'      : ["COMPlus_JitStressRegs": "0x80"],
               'r2r_jitstressregs0x1000'    : ["COMPlus_JitStressRegs": "0x1000"],
               'r2r_jitminopts'             : ["COMPlus_JITMinOpts": "1"], 
               'r2r_jitforcerelocs'         : ["COMPlus_ForceRelocs": "1"],
               'r2r_gcstress15'             : ["COMPlus_GCStress": "0xF"]
    ]

    // This is the basic set of scenarios
    def static basicScenarios = [
               'innerloop',
               'normal',
               'ilrt',
               'r2r',
               'longgc',
               'formatting',
               'gcsimulator',
               // 'jitdiff', // jitdiff is currently disabled, until someone spends the effort to make it fully work
               'standalone_gc',
               'gc_reliability_framework',
               'illink']

    def static allScenarios = basicScenarios + r2rStressScenarios.keySet() + jitStressModeScenarios.keySet()

    // Valid PR trigger combinations.
    def static prTriggeredValidInnerLoopCombos = [
        'Windows_NT': [
            'x64': [
                'Checked'
            ], 
            'x86': [
                'Checked',
                'Release'
            ], 
            'arm': [
                'Checked',
            ],
            'arm64': [
                'Checked'
            ],
            'armlb': [
                'Checked'
            ]
        ],
        'Windows_NT_BuildOnly': [
            'x64': [
                'Checked',
                'Release'
            ],
            'x86': [
                'Checked',
                'Release'
            ], 
            'arm': [
                'Checked'
            ], 
        ],
        'Ubuntu': [
            'x64': [
                'Checked'
            ],
            'arm64': [
                'Debug'
            ],
            'arm': [
                'Checked'
            ]
        ],
        'CentOS7.1': [
            'x64': [
                'Debug',
                'Checked'
            ]
        ],
        'OSX10.12': [
            'x64': [
                'Checked'
            ]
        ],
    ]

    // A set of scenarios that are valid for arm/arm64/armlb tests run on hardware. This is a map from valid scenario name
    // to Tests.lst file categories to exclude.
    //
    // This list should contain a subset of the scenarios from `allScenarios`. Please keep this in the same order as that,
    // and with the same values, with some commented out, for easier maintenance.
    //
    // Note that some scenarios that are commented out should be enabled, but haven't yet been.
    //
    def static validArmWindowsScenarios = [
               'innerloop':                              [],
               'normal':                                 [],
               // 'ilrt'
               'r2r':                                    ["R2R_FAIL", "R2R_EXCLUDE"],
               // 'longgc'
               // 'formatting'
               // 'gcsimulator'
               // 'jitdiff'
               // 'standalone_gc'
               // 'gc_reliability_framework'
               // 'illink'
               'r2r_jitstress1':                         ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstress2':                         ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs1':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs2':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs3':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs4':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs8':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs0x10':                  ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs0x80':                  ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitstressregs0x1000':                ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_jitminopts':                         ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE", "MINOPTS_FAIL", "MINOPTS_EXCLUDE"],
               'r2r_jitforcerelocs':                     ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'r2r_gcstress15':                         ["R2R_FAIL", "R2R_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE", "GCSTRESS_FAIL", "GCSTRESS_EXCLUDE"],
               'minopts':                                ["MINOPTS_FAIL", "MINOPTS_EXCLUDE"],
               'tieredcompilation':                      [],
               'forcerelocs':                            [],
               'jitstress1':                             ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2':                             ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs1':                         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs2':                         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs3':                         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs4':                         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs8':                         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs0x10':                      ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs0x80':                      ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstressregs0x1000':                    ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs1':              ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs2':              ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs3':              ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs4':              ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs8':              ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs0x10':           ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs0x80':           ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'jitstress2_jitstressregs0x1000':         ["JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'tailcallstress':                         ["TAILCALLSTRESS_FAIL", "TAILCALLSTRESS_EXCLUDE"],
               // 'jitsse2only'                          // Only relevant to xarch
               'jitnosimd':                              [],    // Only interesting on platforms where SIMD support exists.
               // 'jitincompletehwintrinsic'
               // 'jitx86hwintrinsicnoavx'
               // 'jitx86hwintrinsicnoavx2'
               // 'jitx86hwintrinsicnosimd'
               // 'jitnox86hwintrinsic'
               'corefx_baseline':                        [],    // corefx tests don't use smarty
               'corefx_minopts':                         [],    // corefx tests don't use smarty
               'corefx_tieredcompilation':               [],    // corefx tests don't use smarty
               'corefx_jitstress1':                      [],    // corefx tests don't use smarty
               'corefx_jitstress2':                      [],    // corefx tests don't use smarty
               'corefx_jitstressregs1':                  [],    // corefx tests don't use smarty
               'corefx_jitstressregs2':                  [],    // corefx tests don't use smarty
               'corefx_jitstressregs3':                  [],    // corefx tests don't use smarty
               'corefx_jitstressregs4':                  [],    // corefx tests don't use smarty
               'corefx_jitstressregs8':                  [],    // corefx tests don't use smarty
               'corefx_jitstressregs0x10':               [],    // corefx tests don't use smarty
               'corefx_jitstressregs0x80':               [],    // corefx tests don't use smarty
               'corefx_jitstressregs0x1000':             [],    // corefx tests don't use smarty
               'gcstress0x3':                            ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE"],
               'gcstress0xc':                            ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE"],
               'zapdisable':                             ["ZAPDISABLE_FAIL", "ZAPDISABLE_EXCLUDE"],
               'heapverify1':                            [],
               'gcstress0xc_zapdisable':                 ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "ZAPDISABLE_FAIL", "ZAPDISABLE_EXCLUDE"],
               'gcstress0xc_zapdisable_jitstress2':      ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "ZAPDISABLE_FAIL", "ZAPDISABLE_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_zapdisable_heapverify1':     ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "ZAPDISABLE_FAIL", "ZAPDISABLE_EXCLUDE"],
               'gcstress0xc_jitstress1':                 ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstress2':                 ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_minopts_heapverify1':        ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "MINOPTS_FAIL", "MINOPTS_EXCLUDE"],

               //
               // NOTE: the following scenarios are not defined in the 'allScenarios' list! Is this a bug?
               //

               'minopts_zapdisable':                     ["ZAPDISABLE_FAIL", "ZAPDISABLE_EXCLUDE", "MINOPTS_FAIL", "MINOPTS_EXCLUDE"],
               'gcstress0x3_jitstress1':                 ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstress2':                 ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs1':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs2':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs3':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs4':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs8':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs0x10':          ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs0x80':          ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0x3_jitstressregs0x1000':        ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs1':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs2':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs3':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs4':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs8':             ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs0x10':          ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs0x80':          ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"],
               'gcstress0xc_jitstressregs0x1000':        ["GCSTRESS_FAIL", "GCSTRESS_EXCLUDE", "JITSTRESS_FAIL", "JITSTRESS_EXCLUDE"]
    ]
  
    def static validLinuxArm64Scenarios = [ 
               'innerloop',
               'normal',
               'r2r',
               'gcstress0x3',
               'gcstress0xc'
    ]

    def static validLinuxArmScenarios = [
               'innerloop',
               'normal',
               'r2r',
               'r2r_jitstress1',
               'r2r_jitstress2',
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
               'minopts',
               'forcerelocs',
               'jitstress1',
               'jitstress2',
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
    //    armlb -- same as arm, but use the LEGACY_BACKEND JIT
    //    armem -- ARM builds/runs using an emulator. Used for Ubuntu/Ubuntu16.04/Tizen runs.
    //    x86_arm_altjit -- ARM runs on x86 using the ARM altjit
    //    x64_arm64_altjit -- ARM64 runs on x64 using the ARM64 altjit
    def static architectureList = ['arm', 'armlb', 'armem', 'x86_arm_altjit', 'x64_arm64_altjit', 'arm64', 'x64', 'x86']

    // This set of architectures that cross build on Windows and run on Windows ARM64 hardware.
    def static armWindowsCrossArchitectureList = ['arm', 'armlb', 'arm64']
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

def static addToViews(def job, def isPR, def architecture, def os) {
    if (isPR) {
        // No views want PR jobs currently.
        return
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
    addToMergeView(job)
    Utilities.addGithubPushTrigger(job)
}


def static setMachineAffinity(def job, def os, def architecture, def options = null) {
    assert os instanceof String
    assert architecture instanceof String

    def armArches = ['arm', 'armlb', 'armem', 'arm64']
    def supportedArmLinuxOs = ['Ubuntu', 'Ubuntu16.04', 'Tizen']

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
    //       |-> os == "Windows_NT" && (architecture == "arm" || architecture == "armlb") && options['use_arm64_build_machine'] == true
    // Arm32 (Test)  -> arm64-windows_nt
    //       |-> os == "Windows_NT" && (architecture == "arm" || architecture == "armlb") && options['use_arm64_build_machine'] == false
    //
    // Arm64 (Build) -> latest-arm64
    //       |-> os == "Windows_NT" && architecture == "arm64" && options['use_arm64_build_machine'] == true
    // Arm64 (Test)  -> arm64-windows_nt
    //       |-> os == "Windows_NT" && architecture == "arm64" && options['use_arm64_build_machine'] == false
    //
    // Ubuntu
    //
    // Arm32 emulator (Build, Test) -> arm-cross-latest
    //       |-> os in supportedArmLinuxOs && (architecture == "armem")
    //
    // Arm32 hardware (Flow) -> Ubuntu 16.04 latest-or-auto (don't use limited arm hardware)
    //       |-> os == "Ubuntu" && (architecture == "arm") && options['is_flow_job'] == true
    // Arm32 hardware (Build) -> Ubuntu 16.04 latest-or-auto
    //       |-> os == "Ubuntu" && (architecture == "arm") && options['is_build_job'] == true
    // Arm32 hardware (Test) -> ubuntu.1404.arm32.open
    //       |-> os == "Ubuntu" && (architecture == "arm")
    //
    // Arm64 (Build) -> arm64-cross-latest
    //       |-> os != "Windows_NT" && architecture == "arm64" && options['is_build_only'] == true
    // Arm64 Small Page Size (Test) -> arm64-small-page-size
    //       |-> os != "Windows_NT" && architecture == "arm64" && options['large_pages'] == false
    // Arm64 Large Page Size (Test) -> arm64-huge-page-size
    //       |-> os != "Windows_NT" && architecture == "arm64" && options['large_pages'] == true

    // This has to be a arm arch
    assert architecture in armArches
    if (os == "Windows_NT") {
        // arm32/arm64 Windows jobs share the same machines for now
        def isBuild = options['use_arm64_build_machine'] == true

        if (isBuild == true) {
            Utilities.setMachineAffinity(job, os, 'latest-arm64')
        } else {
            Utilities.setMachineAffinity(job, os, 'arm64-windows_nt')
        }
    } else {
        assert os != 'Windows_NT'
        assert os in supportedArmLinuxOs

        if (architecture == 'arm64') {
            if ((options != null) && (options['is_build_only'] == true)) {
                // Arm64 Linux build machine
                Utilities.setMachineAffinity(job, os, 'arm64-cross-latest')
            } else {
                // Arm64 Linux test machines
                if ((options != null) && (options['large_pages'] == true)) {
                    Utilities.setMachineAffinity(job, os, 'arm64-huge-page-size')
                } else {
                    Utilities.setMachineAffinity(job, os, 'arm64-small-page-size')
                }
            }
        }
        else if (architecture == 'armem') {
            // arm emulator (Ubuntu/Ubuntu16.04/Tizen). Build and test on same machine,
            // using Docker.
            Utilities.setMachineAffinity(job, 'Ubuntu', 'arm-cross-latest')
        }
        else {
            // arm Ubuntu on hardware.
            assert (architecture == 'arm') && (os == 'Ubuntu')
            def isFlow  = (options != null) && (options['is_flow_job'] == true)
            def isBuild = (options != null) && (options['is_build_job'] == true)
            if (isFlow || isBuild) {
                // arm Ubuntu build machine. Build uses docker, so the actual host OS is not
                // very important. Therefore, use latest or auto. Flow jobs don't need to use
                // arm hardware.
                Utilities.setMachineAffinity(job, 'Ubuntu16.04', 'latest-or-auto')
            } else {
                // arm Ubuntu test machine
                // There is no tag (like, e.g., "arm-latest") for this, so don't call
                // Utilities.setMachineAffinity. Just add the machine affinity
                // manually. We specify the Helix queue name here.
                job.with {
                    label('ubuntu.1404.arm32.open')
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
        if (architecture == 'arm64') {
            if (isBuildJob) {
                affinityOptions = ['is_build_only': true]
            } else if (isTestJob) {
                affinityOptions = [ "large_pages" : false ]
            }
        }
        else if (architecture == 'arm') {
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
    return Constants.validArmWindowsScenarios.containsKey(scenario)
}

def static isValidPrTriggeredInnerLoopJob(os, architecture, configuration, isBuildOnly) {
    if (isBuildOnly == true) {
        os = 'Windows_NT_BuildOnly'
    }

    def validOsPrTriggerArchConfigs = Constants.prTriggeredValidInnerLoopCombos[os]

    if (validOsPrTriggerArchConfigs == null) {
        return false
    }

    if (validOsPrTriggerArchConfigs[architecture] != null) {
        def validOsPrTriggerConfigs = validOsPrTriggerArchConfigs[architecture]

        if (!(configuration in validOsPrTriggerConfigs)) {
            return false
        }
    } else {
        return false
    }

    return true
}

def static setJobTimeout(newJob, isPR, architecture, configuration, scenario, isBuildOnly) {
    // 2 hours (120 minutes) is the default timeout
    def timeout = 120
    def innerLoop = (scenario == "innerloop")

    if (!innerLoop) {
        // Pri-1 test builds take a long time. Default PR jobs are Pri-0; everything else is Pri-1
        // (see calculateBuildCommands()). So up the Pri-1 build jobs timeout.
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
        else if (architecture == 'armlb' || architecture == 'armem' || architecture == 'arm64') {
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

    if (architecture == 'x86_arm_altjit' || architecture == 'x64_arm64_altjit') {
        // AltJit runs compile all methods twice.
        timeout *= 2
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
            displayStr += ' ' + modeName + '=' + v
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
            if (os == 'Ubuntu') {
                return "microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-e435274-20180405193556"
            }
            else if (os == 'Ubuntu16.04') {
                return "microsoft/dotnet-buildtools-prereqs:ubuntu-16.04-cross-e435274-20180404203310"
            }
            else if (os == 'Tizen') {
                return "hqueue/dotnetcore:ubuntu1404_cross_prereqs_v4-tizen_rootfs"
            }
        }
        else if (architecture == 'arm') {
            if (os == 'Ubuntu') {
                return "microsoft/dotnet-buildtools-prereqs:ubuntu-14.04-cross-e435274-20180405193556"
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


// We have a limited amount of some hardware. For these, scale back the periodic testing we do.
def static jobRequiresLimitedHardware(def architecture, def os) {
    if (((architecture == 'arm64') || (architecture == 'arm') || (architecture == 'armlb')) && (os == 'Windows_NT')) {
        // These test jobs require ARM64 hardware
        return true
    }
    else if ((architecture == 'arm') && (os == 'Ubuntu')) {
        // These test jobs require Linux/arm32 hardware
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
        case 'arm64':
            if (os.toLowerCase() == "windows_nt") {
                // These are cross builds
                baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                // Defaults to a small page size set of machines.
                baseName = architecture.toLowerCase() + '_' + configuration.toLowerCase() + '_' + "small_page_size"
            }
            break
        case 'armem':
            // These are cross builds
            if (os == 'Tizen') {
                // ABI: softfp
                baseName = 'armel_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            else {
                baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            }
            break
        case 'armlb':
        case 'arm':
            baseName = architecture.toLowerCase() + '_cross_' + configuration.toLowerCase() + '_' + os.toLowerCase()
            break
        case 'x86':
        case 'x86_arm_altjit':
        case 'x64_arm64_altjit':
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

    // Limited Windows ARM64 hardware is restricted for non-PR triggers to certain branches.
    if (os == 'Windows_NT') {
        if ((architecture == 'arm64') || (architecture == 'arm') || (architecture == 'armlb')) {
            if (!(branch in Constants.WindowsArm64Branches)) {
                return
            }
        }
    }

    if ((architecture == 'arm') && (os != 'Windows_NT') && isGCStressRelatedTesting(scenario)) {
        // Non-Windows Arm GCStress jobs currently don't get cron or push triggers (until they are functional).
        // See https://github.com/dotnet/coreclr/issues/17241.
        return
    }

    // Check scenario.
    switch (scenario) {
        case 'innerloop':
            break
        case 'normal':
            switch (architecture) {
                case 'x64':
                case 'x86':
                    if (isFlowJob && architecture == 'x86' && os == 'Ubuntu') {
                        addPeriodicTriggerHelper(job, '@daily')
                    }
                    else if (isFlowJob || os == 'Windows_NT' || !(os in Constants.crossList)) {
                        addGithubPushTriggerHelper(job)
                    }
                    break
                case 'arm':
                    if (os == 'Windows_NT') {
                        addGithubPushTriggerHelper(job)
                    }
                    else {
                        // Currently no push triggers, with limited arm Linux hardware.
                        // TODO: If we have enough machine capacity, add some arm Linux push triggers.
                        assert os == 'Ubuntu'
                        if (isFlowJob) {
                            addPeriodicTriggerHelper(job, '@daily')
                        }
                    }
                    break
                case 'armem':
                case 'armlb':
                case 'x86_arm_altjit':
                case 'x64_arm64_altjit':
                    addGithubPushTriggerHelper(job)
                    break
                case 'arm64':
                    // We would normally want a per-push trigger, but with limited hardware we can't keep up
                    addPeriodicTriggerHelper(job, "H H/4 * * *")
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
                assert (os == 'Windows_NT') || (os in Constants.crossList)
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
                // arm64 r2r jobs should only run daily.
                else if (architecture == 'arm64') {
                    if (os == 'Windows_NT') {
                        addPeriodicTriggerHelper(job, '@daily')
                    }
                }
            }
            break
        case 'r2r_jitstress1':
        case 'r2r_jitstress2':
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
            assert !(os in bidailyCrossList)

            // GCStress=C is currently not supported on OS X
            if (os == 'OSX10.12' && isGCStressRelatedTesting(scenario)) {
                break
            }

            // GC Stress 15 r2r gets a push trigger for checked/release
            if (configuration == 'Checked' || configuration == 'Release') {
                assert (os == 'Windows_NT') || (os in Constants.crossList)
                if (architecture == 'x64') {
                    //Flow jobs should be Windows, Ubuntu, OSX10.12, or CentOS
                    if (isFlowJob || os == 'Windows_NT') {
                        // Add a weekly periodic trigger
                        addPeriodicTriggerHelper(job, 'H H * * 3,6') // some time every Wednesday and Saturday
                    }
                }
                // For x86, only add per-commit jobs for Windows
                else if (architecture == 'x86') {
                    if (os == 'Windows_NT') {
                        addPeriodicTriggerHelper(job, 'H H * * 3,6') // some time every Wednesday and Saturday
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
                // We don't expect to see a job generated except in these scenarios
                assert (os == 'Windows_NT') || (os in Constants.crossList)
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
        case 'forcerelocs':
        case 'jitstress1':
        case 'jitstress2':
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
            assert (os == 'Windows_NT') || (os in Constants.crossList)
            if (jobRequiresLimitedHardware(architecture, os)) {
                addPeriodicTriggerHelper(job, '@weekly')
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
            if ((architecture == 'arm64') && (os != 'Windows_NT')) {
                // TODO: should we have cron jobs for arm64 Linux GCStress?
                break
            }
            assert (os == 'Windows_NT') || (os in Constants.crossList)
            addPeriodicTriggerHelper(job, '@weekly')
            break
        case 'gcstress0xc':
        case 'gcstress0xc_zapdisable':
        case 'gcstress0xc_zapdisable_jitstress2':
        case 'gcstress0xc_zapdisable_heapverify1':
        case 'gcstress0xc_jitstress1':
        case 'gcstress0xc_jitstress2':
        case 'gcstress0xc_minopts_heapverify1':
            if (os == 'CentOS7.1') {
                break
            }
            if (os == 'OSX10.12') {
                // GCStress=C is currently not supported on OS X
                break
            }
            if (os in bidailyCrossList) {
                break
            }
            if ((architecture == 'arm64') && (os != 'Windows_NT')) {
                // TODO: should we have cron jobs for arm64 Linux GCStress?
                break
            }
            assert (os == 'Windows_NT') || (os in Constants.crossList)
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
        
        case 'tieredcompilation':
        case 'corefx_tieredcompilation':
            // No periodic jobs just yet, still testing
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
        'AndyAyersMS',
        'briansull',
        'BruceForstall',
        'CarolEidt',
        'cmckinsey',
        'echesakovMSFT',
        'erozenfeld',
        'janvorli',
        'jashook',
        'JosephTremoulet',
        'pgodeq',
        'russellhadley',
        'RussKeldorph',
        'sandreenko',
        'sdmaclea',
        'swaroop-sridhar',
        'jkotas',
        'markwilkie',
        'weshaggard'
    ]
    
    // Pull request builds.  Generally these fall into two categories: default triggers and on-demand triggers
    // We generally only have a distinct set of default triggers but a bunch of on-demand ones.
    def osGroup = getOSGroup(os)
    switch (architecture) {
        case 'x64': // editor brace matching: {
            if (scenario == 'formatting') {
                assert configuration == 'Checked'
                if (os == 'Windows_NT' || os == 'Ubuntu') {
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Formatting")
                }

                break
            }

            switch (os) {
                // OpenSUSE, Debian & RedHat get trigger phrases for pri 0 build, and pri 1 build & test
                case 'Debian8.4':
                case 'RHEL7.2':
                    if (scenario == 'innerloop') {
                        assert !isFlowJob
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build")
                    } 
                    else if (scenario == 'normal') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${os}\\W+${architecture}.*")
                    }   
                    break

                case 'Ubuntu16.04':
                    assert !isFlowJob
                    assert scenario != 'innerloop'
                    // Distinguish with the other architectures (arm and x86)
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${os}\\W+${architecture}.*")
                    break

                case 'Fedora24':
                case 'Ubuntu16.10':
                    assert !isFlowJob
                    assert scenario != 'innerloop'
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build", "(?i).*test\\W+${os}\\W+.*")
                    break

                case 'Ubuntu':
                    if (scenario == 'illink') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} via ILLink", "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                        break
                    }
                    // fall through

                case 'OSX10.12':
                    // Triggers on the non-flow jobs aren't necessary here
                    // Corefx testing uses non-flow jobs.
                    if (!isFlowJob && !isCoreFxScenario(scenario)) {
                        break
                    }
                    switch (scenario) {
                        case 'innerloop':
                            // PR Triggered jobs. These jobs will run pri0 tests.
                            if (configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build and Test")
                            }
                            break

                        case 'normal':
                            // OSX uses checked for default PR tests
                            if (configuration == 'Checked') {
                                // Default trigger
                                assert !job.name.contains("centos")
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test", "(?i).*test\\W+${os}\\W+${architecture}\\W+Build and Test.*")
                            }
                            break

                        case 'jitdiff':
                            if (configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Jit Diff Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break

                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break

                        case 'longgc':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'gcsimulator':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'standalone_gc':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Standalone GC", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'gc_reliability_framework':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Reliability Framework", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        default:
                            if (isJitStressScenario(scenario)) {
                                def displayStr = getStressModeDisplayName(scenario)
                                assert (os == 'Windows_NT') || (os in Constants.crossList)
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayStr})",
                                   "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                            }
                            else if (isR2RScenario(scenario)) {
                                if (configuration == 'Release' || configuration == 'Checked') {
                                    def displayStr = getR2RDisplayName(scenario)
                                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${displayStr} Build and Test",
                                        "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                                }
                            }
                            else {
                                println("Unknown scenario: ${scenario}");
                                assert false
                            }
                            break

                    }
                    break

                case 'CentOS7.1':
                    switch (scenario) {
                        case 'innerloop':
                            // CentOS uses checked for default PR tests while debug is build only
                            if (configuration == 'Debug') {
                                // Default trigger
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build")
                            }
                            
                            // Make sure this is a flow job to get build and test.
                            if (configuration == 'Checked' && isFlowJob) {
                                assert job.name.contains("flow")
                                // Default trigger
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build and Test")
                            }
                            break

                        case 'normal':
                            // Make sure this is a flow job to get build and test.
                            if (configuration == 'Checked' && isFlowJob) {
                                assert job.name.contains("flow")
                                // Default trigger
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test", "(?i).*test\\W+${os}\\W+${architecture}\\W+Build and Test.*")
                            }
                            break

                        default:
                            if (isR2RScenario(scenario)) {
                                if (configuration == 'Release' || configuration == 'Checked') {
                                    def displayStr = getR2RDisplayName(scenario)
                                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${displayStr} Build & Test",
                                        "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                                }
                            }
                            break

                    }
                    break

                case 'Windows_NT':
                    switch (scenario) {
                        case 'innerloop':
                            // Default trigger
                            if (configuration == 'Checked' || configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build and Test")
                            }
                            break

                        case 'normal':
                            if (configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test", "(?i).*test\\W+${os}\\W+${architecture}\\W+Build and Test.*")
                            }
                            break

                        case 'jitdiff':
                            if (configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Jit Diff Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break

                        case 'ilrt':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test", "(?i).*test\\W+${os}\\W+${scenario}.*")
                            }
                            break

                        case 'longgc':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Long-Running GC Build & Test", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'gcsimulator':
                            if (configuration == 'Release') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Simulator", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'standalone_gc':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Standalone GC", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'gc_reliability_framework':
                            if (configuration == 'Release' || configuration == 'Checked') {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Reliability Framework", "(?i).*test\\W+${os}\\W+${configuration}\\W+${scenario}.*")
                            }
                            break

                        case 'illink':
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} via ILLink", "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                            break

                        default:
                            if (isJitStressScenario(scenario)) {
                                def displayStr = getStressModeDisplayName(scenario)
                                assert (os == 'Windows_NT') || (os in Constants.crossList)
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayStr})",
                                   "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                            }
                            else if (isR2RScenario(scenario)) {
                                if (configuration == 'Release' || configuration == 'Checked') {
                                    def displayStr = getR2RDisplayName(scenario)
                                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${displayStr} Build & Test",
                                        "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                                }
                            }
                            else {
                                println("Unknown scenario: ${scenario}");
                                assert false
                            }
                            break

                    }
                    break

                default:
                    println("Unknown os: ${os}");
                    assert false
                    break

            }

            break
        // editor brace matching: }

        case 'armem': // editor brace matching: {
            job.with {
                publishers {
                    azureVMAgentPostBuildAction {
                        agentPostBuildAction('Delete agent if the build was not successful (when idle).')
                    }
                }
            }

            switch (os) {
                case 'Ubuntu':
                case 'Ubuntu16.04':
                    assert scenario != 'innerloop'
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Cross ${configuration} Build",
                        "(?i).*test\\W+${os}\\W+${architecture}\\W+Cross\\W+${configuration}\\W+Build.*")
                    break

                case 'Tizen':
                    architecture = 'armel'

                    assert scenario != 'innerloop'
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Cross ${configuration} Build",
                        "(?i).*test\\W+${os}\\W+${architecture}\\W+Cross\\W+${configuration}\\W+Build.*")
                    break
            }

            break
        // editor brace matching: }

        case 'armlb':
        case 'arm': // editor brace matching: {

            // Triggers on the non-flow jobs aren't necessary
            if (!isFlowJob) {
                break
            }

            // Set up a private trigger
            def contextString = "${os} ${architecture} Cross ${configuration}"
            def triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+Cross\\W+${configuration}"
            if (scenario == 'innerloop') {
                contextString += " Innerloop"
                triggerString += "\\W+Innerloop"
            }
            else {
                contextString += " ${scenario}"
                triggerString += "\\W+${scenario}"
            }

            if (configuration == 'Debug') {
                contextString += " Build"
                triggerString += "\\W+Build"
            } else {
                contextString += " Build and Test"
                triggerString += "\\W+Build and Test"
            }

            triggerString += ".*"

            switch (os) {
                case 'Ubuntu':
                    if (architecture == 'armlb') { // No arm legacy backend testing for Ubuntu
                        break
                    }

                    if (scenario == 'innerloop') {
                        if (configuration == 'Checked') {
                            Utilities.addGithubPRTriggerForBranch(job, branch, contextString)
                        }
                    }
                    else {
                        Utilities.addGithubPRTriggerForBranch(job, branch, contextString, triggerString)
                    }
                    break

                case 'Windows_NT':
                    if (architecture == "armlb") {
                        // Disable armlb windows jobs
                        break
                    }
                    switch (scenario) {
                        case 'innerloop':
                            // Only Checked is an innerloop trigger.
                            if (configuration == 'Checked')
                            {
                                Utilities.addDefaultPrivateGithubPRTriggerForBranch(job, branch, contextString, null, arm64Users)
                            }
                            break
                        case 'normal':
                            Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString, triggerString, null, arm64Users)
                            break
                        default:
                            // Stress jobs will use this code path.
                            if (isArmWindowsScenario(scenario)) {
                                Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString, triggerString, null, arm64Users)
                            }
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
        case 'arm64': // editor brace matching: {
            // Set up a private trigger
            def contextString = "${os} ${architecture} Cross ${configuration}"
            def triggerString = "(?i).*test\\W+${os}\\W+${architecture}\\W+Cross\\W+${configuration}"

            if (scenario == 'innerloop') {
                contextString += " Innerloop"
                triggerString += "\\W+Innerloop"
            }
            else {
                contextString += " ${scenario}"
                triggerString += "\\W+${scenario}"
            }

            if (configuration == 'Debug') {
                contextString += " Build"
                triggerString += "\\W+Build"
            } else {
                contextString += " Build and Test"
                triggerString += "\\W+Build and Test"
            }

            triggerString += ".*"

            switch (os) {
                case 'Ubuntu':
                case 'Ubuntu16.04':
                    switch (scenario) {
                        case 'innerloop':
                            if (configuration == 'Debug' && !isFlowJob) {
                                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} Cross ${configuration} Innerloop Build")
                            }
                            
                            break
                        case 'normal':
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test", triggerString)
                            break
                        default:
                            if (isR2RScenario(scenario)) {
                                if (configuration == 'Checked' || configuration == 'Release') {
                                    def displayStr = getR2RDisplayName(scenario)
                                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${displayStr} Build and Test", triggerString)
                                }
                            }
                            break
                    }
                    break

                case 'Windows_NT':
                    // Triggers on the non-flow jobs aren't necessary here
                    if (!isFlowJob) {
                        break
                    }

                    assert isArmWindowsScenario(scenario)
                    switch (scenario) {
                        case 'innerloop':
                            if (configuration == 'Checked') {
                                Utilities.addDefaultPrivateGithubPRTriggerForBranch(job, branch, contextString, null, arm64Users)
                            }
                            
                            break
                        case 'normal':
                            Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString, triggerString, null, arm64Users)
                            break
                        default:
                            // Stress jobs will use this code path.
                            if (isArmWindowsScenario(scenario)) {
                                Utilities.addPrivateGithubPRTriggerForBranch(job, branch, contextString, triggerString, null, arm64Users)
                            }
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
                    break
                }
                
                // on-demand only for ubuntu x86
                Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build",
                    "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}.*")
                break

            }
            switch (scenario) {
                case 'innerloop':
                    if (configuration == 'Checked' || configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Innerloop Build and Test")
                    }
                    break

                case 'normal':
                    if (configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test",
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+Build and Test.*")
                    }
                    break

                case 'ilrt':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} IL RoundTrip Build and Test",
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break

                case 'longgc':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Long-Running GC Build & Test",
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break

                case 'gcsimulator':
                    if (configuration == 'Release') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} GC Simulator",
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break

                case 'standalone_gc':
                    if (configuration == 'Release' || configuration == 'Checked') {
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Standalone GC",
                            "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    }
                    break

                case 'illink':
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} via ILLink", "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    break

                default:
                    if (isJitStressScenario(scenario)) {
                        def displayStr = getStressModeDisplayName(scenario)
                        Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test (Jit - ${displayStr})",
                           "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    }
                    else if (isR2RScenario(scenario)) {
                        if (configuration == 'Release' || configuration == 'Checked') {
                            def displayStr = getR2RDisplayName(scenario)
                            Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${displayStr} Build & Test",
                                "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                        }
                    }
                    else {
                        println("Unknown scenario: ${os} ${architecture} ${scenario}");
                        assert false
                    }
                    break

            }
            break

         // editor brace matching: }
        case 'x64_arm64_altjit':
        case 'x86_arm_altjit': // editor brace matching: {
            assert (os == 'Windows_NT')
            switch (scenario) {
                case 'normal':
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} Build and Test",
                        "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+Build and Test.*")
                    break
                default:
                    Utilities.addGithubPRTriggerForBranch(job, branch, "${os} ${architecture} ${configuration} ${scenario}",
                        "(?i).*test\\W+${os}\\W+${architecture}\\W+${configuration}\\W+${scenario}.*")
                    break
            }
            break

        // editor brace matching: }
        default:
            println("Unknown architecture: ${architecture}");
            assert false
            break
    }
}

def static calculateBuildCommands(def newJob, def scenario, def branch, def isPR, def architecture, def configuration, def os, def isBuildOnly) {
    def buildCommands = []
    def osGroup = getOSGroup(os)
    def lowerConfiguration = configuration.toLowerCase()

    def priority = '1'
    if (scenario == 'innerloop') {
        priority = '0'
    }

    def doCoreFxTesting = isCoreFxScenario(scenario)

    // Calculate the build steps, archival, and xunit results
    switch (os) {
        case 'Windows_NT': // editor brace matching: {
            switch (architecture) {
                case 'x64':
                case 'x86':
                case 'x86_arm_altjit':
                case 'x64_arm64_altjit':
                    def arch = architecture
                    def buildOpts = ''
                    if (architecture == 'x86_arm_altjit') {
                        arch = 'x86'
                    }
                    else if (architecture == 'x64_arm64_altjit') {
                        arch = 'x64'
                    }

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
                        (scenario in Constants.basicScenarios) &&
                        (architecture != 'x86_arm_altjit') &&
                        (architecture != 'x64_arm64_altjit')) {

                        buildOpts += ' -enforcepgo'
                    }

                    if (doCoreFxTesting) {
                        buildOpts += ' skiptests';
                    } else {
                        buildOpts += " -priority=${priority}"
                    }

                    // Set __TestIntermediateDir to something short. If __TestIntermediateDir is already set, build-test.cmd will
                    // output test binaries to that directory. If it is not set, the binaries are sent to a default directory whose name is about
                    // 35 characters long.

                    buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${arch} ${buildOpts}"

                    if (!isBuildOnly) {
                        def runtestArguments = ''
                        def testOpts = 'collectdumps'

                        if (isR2RScenario(scenario)) {

                            // If this is a ReadyToRun scenario, pass 'crossgen' or 'crossgenaltjit'
                            // to cause framework assemblies to be crossgen'ed. Pass 'runcrossgentests'
                            // to cause the tests to be crossgen'ed.

                            if ((architecture == 'x86_arm_altjit') || (architecture == 'x64_arm64_altjit')) {
                                testOpts += ' crossgenaltjit protononjit.dll'
                            } else {
                                testOpts += ' crossgen'
                            }

                            testOpts += ' runcrossgentests'
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

                            if (architecture == 'x86_arm_altjit') {
                                buildCommandsStr += envScriptAppendExistingScript(os, "%WORKSPACE%\\tests\\x86_arm_altjit.cmd", envScriptPath)
                            }
                            else if (architecture == 'x64_arm64_altjit') {
                                buildCommandsStr += envScriptAppendExistingScript(os, "%WORKSPACE%\\tests\\x64_arm64_altjit.cmd", envScriptPath)
                            }

                            envScriptFinalize(os, envScriptPath)

                            // Note that buildCommands is an array of individually executed commands; we want all the commands used to 
                            // create the SetStressModes.bat script to be executed together, hence we accumulate them as strings
                            // into a single script.
                            buildCommands += buildCommandsStr
                        }
                        else if (architecture == 'x86_arm_altjit') {
                            envScriptPath = "%WORKSPACE%\\tests\\x86_arm_altjit.cmd"
                        }
                        else if (architecture == 'x64_arm64_altjit') {
                            envScriptPath = "%WORKSPACE%\\tests\\x64_arm64_altjit.cmd"
                        }
                        if (envScriptPath != '') {
                            testOpts += " TestEnv ${envScriptPath}"
                        }

                        runtestArguments = "${lowerConfiguration} ${arch} ${testOpts}"

                        if (doCoreFxTesting) {
                            def workspaceRelativeFxRoot = "_/fx"
                            def absoluteFxRoot = "%WORKSPACE%\\_\\fx"

                            buildCommands += "python -u %WORKSPACE%\\tests\\scripts\\run-corefx-tests.py -arch ${arch} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${branch} -env_script ${envScriptPath}"

                            // Archive and process (only) the test results
                            Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/bin/**/testResults.xml")
                            Utilities.addXUnitDotNETResults(newJob, "${workspaceRelativeFxRoot}/bin/**/testResults.xml")

                            //Archive additional build stuff to diagnose why my attempt at fault injection isn't causing CI to fail
                            Utilities.addArchival(newJob, "SetStressModes.bat", "", true, false)
                            Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/bin/testhost/**", "", true, false)
                        }
                        else if (isGcReliabilityFramework(scenario)) {
                            buildCommands += "tests\\runtest.cmd ${runtestArguments} GenerateLayoutOnly"
                            buildCommands += "tests\\scripts\\run-gc-reliability-framework.cmd ${arch} ${configuration}"
                        }
                        else {
                            buildCommands += "tests\\runtest.cmd ${runtestArguments}"
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

                        // Zip up the tests directory so that we don't use so much space/time copying
                        // 10s of thousands of files around.
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${arch}.${configuration}', '.\\bin\\tests\\tests.zip')\"";

                        if (!isJitStressScenario(scenario)) {
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
                    break
                case 'armlb':
                case 'arm':
                    assert isArmWindowsScenario(scenario)

                    def buildArchitecture = 'arm'

                    def buildOpts = ''

                    // For 'armlb' (the JIT LEGACY_BACKEND architecture for arm), tell build.cmd to use legacy backend for crossgen compilation.
                    // Legacy backend is not the default JIT; it is an aljit. So, this is a special case.
                    if (architecture == 'armlb') {
                        buildOpts += ' -crossgenaltjit legacyjit.dll'
                    }

                    if (doCoreFxTesting) {
                        // We shouldn't need to build the tests. However, run-corefx-tests.py currently depends on having the restored corefx
                        // package available, to determine the correct corefx version git commit hash, and we need to build the tests before
                        // running "tests\\runtest.cmd GenerateLayoutOnly". So build the pri-0 tests to make this happen.
                        //
                        // buildOpts += ' skiptests';
                        buildOpts += " -priority=0"
                    } else {
                        buildOpts += " -priority=${priority}"
                    }

                    // This is now a build only job. Do not run tests. Use the flow job.
                    buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${buildArchitecture} ${buildOpts}"

                    if (doCoreFxTesting) {
                        assert isBuildOnly
                        assert architecture == 'arm'

                        // Generate the test layout because it restores the corefx package which allows run-corefx-tests.py
                        // to determine the correct matching corefx version git commit hash.
                        buildCommands += "tests\\runtest.cmd ${lowerConfiguration} ${architecture} GenerateLayoutOnly"

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

                        buildCommands += "python -u %WORKSPACE%\\tests\\scripts\\run-corefx-tests.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${branch} -env_script ${envScriptPath} -no_run_tests"

                        // Zip up the CoreFx runtime and tests. We don't need the CoreCLR binaries; they have been copied to the CoreFX tree.
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('${workspaceRelativeFxRootWin}\\bin\\testhost\\netcoreapp-Windows_NT-Release-arm', '${workspaceRelativeFxRootWin}\\fxruntime.zip')\"";
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('${workspaceRelativeFxRootWin}\\bin\\tests', '${workspaceRelativeFxRootWin}\\fxtests.zip')\"";

                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxruntime.zip")
                        Utilities.addArchival(newJob, "${workspaceRelativeFxRootLinux}/fxtests.zip")
                    } else {
                        // Zip up the tests directory so that we don't use so much space/time copying
                        // 10s of thousands of files around.
                        buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${buildArchitecture}.${configuration}', '.\\bin\\tests\\tests.zip')\"";

                        // Add archival.
                        Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip", "bin/Product/**/.nuget/**")
                    }
                    break
                case 'arm64':
                    assert isArmWindowsScenario(scenario)

                    // This is now a build only job. Do not run tests. Use the flow job.
                    buildCommands += "set __TestIntermediateDir=int&&build.cmd ${lowerConfiguration} ${architecture} toolset_dir C:\\ats2 -priority=${priority}"

                    // Zip up the tests directory so that we don't use so much space/time copying
                    // 10s of thousands of files around.
                    buildCommands += "powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${architecture}.${configuration}', '.\\bin\\tests\\tests.zip')\"";

                    // Add archival.
                    Utilities.addArchival(newJob, "bin/Product/**,bin/tests/tests.zip", "bin/Product/**/.nuget/**")
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
                case 'x64':
                case 'x86':
                    if (architecture == 'x86' && os == 'Ubuntu') {
                        // build and PAL test
                        def dockerImage = getDockerImageName(architecture, os, true)
                        buildCommands += "docker run -i --rm -v \${WORKSPACE}:/opt/code -w /opt/code -e ROOTFS_DIR=/crossrootfs/x86 ${dockerImage} ./build.sh ${architecture} cross ${lowerConfiguration}"
                        dockerImage = getDockerImageName(architecture, os, false)
                        buildCommands += "docker run -i --rm -v \${WORKSPACE}:/opt/code -w /opt/code ${dockerImage} ./src/pal/tests/palsuite/runpaltests.sh /opt/code/bin/obj/${osGroup}.${architecture}.${configuration} /opt/code/bin/paltestout"
                        Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
                        Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                        break
                    }

                    if (scenario == 'formatting') {
                        buildCommands += "python tests/scripts/format.py -c \${WORKSPACE} -o Linux -a ${architecture}"
                        Utilities.addArchival(newJob, "format.patch", "", true, false)
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

                        buildCommands += "${bootstrapRidEnv}./build.sh verbose ${lowerConfiguration} ${architecture}"
                        buildCommands += "src/pal/tests/palsuite/runpaltests.sh \${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration} \${WORKSPACE}/bin/paltestout"

                        // Basic archiving of the build
                        Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.dylib,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
                        // And pal tests
                        Utilities.addXUnitDotNETResults(newJob, '**/pal_tests.xml')
                    }
                    else {
                        // Corefx stress testing
                        assert os == 'Ubuntu'
                        assert architecture == 'x64'
                        assert lowerConfiguration == 'checked'
                        assert isJitStressScenario(scenario)

                        // Build coreclr
                        buildCommands += "./build.sh verbose ${lowerConfiguration} ${architecture}"

                        def scriptFileName = "\$WORKSPACE/set_stress_test_env.sh"

                        def envScriptCmds = envScriptCreate(os, scriptFileName)
                        envScriptCmds += envScriptSetStressModeVariables(os, Constants.jitStressModeScenarios[scenario], scriptFileName)
                        envScriptCmds += envScriptFinalize(os, scriptFileName)
                        buildCommands += envScriptCmds

                        // Build and text corefx
                        def workspaceRelativeFxRoot = "_/fx"
                        def absoluteFxRoot = "\$WORKSPACE/${workspaceRelativeFxRoot}"

                        buildCommands += "python -u \$WORKSPACE/tests/scripts/run-corefx-tests.py -arch ${architecture} -ci_arch ${architecture} -build_type ${configuration} -fx_root ${absoluteFxRoot} -fx_branch ${branch} -env_script ${scriptFileName}"

                        // Archive and process (only) the test results
                        Utilities.addArchival(newJob, "${workspaceRelativeFxRoot}/bin/**/testResults.xml")
                        Utilities.addXUnitDotNETResults(newJob, "${workspaceRelativeFxRoot}/bin/**/testResults.xml")
                    }
                    break
                case 'arm64':
                    if (!doCoreFxTesting) {
                        buildCommands += "ROOTFS_DIR=/opt/arm64-xenial-rootfs ./build.sh verbose ${lowerConfiguration} ${architecture} cross clang3.8"
                        
                        // HACK -- Arm64 does not have corefx jobs yet.
                        buildCommands += "git clone https://github.com/dotnet/corefx fx"
                        buildCommands += "ROOTFS_DIR=/opt/arm64-xenial-rootfs-corefx ./fx/build-native.sh -release -buildArch=arm64 -- verbose cross clang3.8"
                        buildCommands += "mkdir ./bin/Product/Linux.arm64.${configuration}/corefxNative"
                        buildCommands += "cp fx/bin/Linux.arm64.Release/native/* ./bin/Product/Linux.arm64.${configuration}/corefxNative"

                        // Basic archiving of the build
                        Utilities.addArchival(newJob, "bin/Product/**,bin/obj/*/tests/**/*.dylib,bin/obj/*/tests/**/*.so", "bin/Product/**/.nuget/**")
                    }
                    break
                case 'armem':
                    // Emulator cross builds for ARM runs on Ubuntu, Ubuntu16.04 and Tizen currently
                    assert (os == 'Ubuntu') || (os == 'Ubuntu16.04') || (os == 'Tizen')

                    // default values for Ubuntu
                    def arm_abi = "arm"
                    def linuxCodeName = "trusty"
                    if (os == 'Ubuntu16.04') {
                        linuxCodeName = "xenial"
                    }
                    else if (os == 'Tizen') {
                        arm_abi = "armel"
                        linuxCodeName = "tizen"
                    }

                    // Unzip the Windows test binaries first. Exit with 0
                    buildCommands += "unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/Windows_NT.x64.${configuration} || exit 0"

                    // Unpack the corefx binaries
                    buildCommands += "mkdir ./bin/CoreFxBinDir"
                    buildCommands += "tar -xf ./bin/build.tar.gz -C ./bin/CoreFxBinDir"
                    if (os != 'Tizen') {
                        buildCommands += "chmod a+x ./bin/CoreFxBinDir/corerun"
                    }
                    // Test environment emulation using docker and qemu has some problem to use lttng library.
                    // We should remove libcoreclrtraceptprovider.so to avoid test hang.
                    if (os == 'Ubuntu') {
                        buildCommands += "rm -f -v ./bin/CoreFxBinDir/libcoreclrtraceptprovider.so"
                    }

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
                case 'arm':
                    // Non-Windows ARM cross builds on hardware run on Ubuntu only
                    assert (os == 'Ubuntu')

                    // Add some useful information to the log file. Ignore return codes.
                    buildCommands += "uname -a || true"

                    // Cross build the Ubuntu/arm product using docker with a docker image that contains the correct
                    // Ubuntu cross-compilation toolset (running on a Ubuntu x64 host).

                    def dockerImage = getDockerImageName(architecture, os, true)
                    def dockerCmd = "docker run -i --rm -v \${WORKSPACE}:\${WORKSPACE} -w \${WORKSPACE} -e ROOTFS_DIR=/crossrootfs/arm -e CAC_ROOTFS_DIR=/crossrootfs/x86 ${dockerImage} "

                    buildCommands += "${dockerCmd}\${WORKSPACE}/build.sh ${lowerConfiguration} ${architecture} cross crosscomponent"

                    // Then, using the same docker image, generate the CORE_ROOT layout using build-test.sh to
                    // download the appropriate CoreFX packages.
                    // Note that docker should not be necessary here, for the "generatelayoutonly" case, but we use it
                    // just to be consistent with the "build.sh" case -- so both are run with the same environment.

                    buildCommands += "${dockerCmd}\${WORKSPACE}/build-test.sh ${lowerConfiguration} ${architecture} cross generatelayoutonly"

                    // ZIP up for the test job (created in the flow job code):
                    // (1) the built CORE_ROOT, /home/user/coreclr/bin/tests/Linux.arm.Checked/Tests/Core_Root,
                    //     used by runtest.sh as the "--coreOverlayDir" argument.
                    // (2) the native parts of the test build: /home/user/coreclr/bin/obj/Linux.arm.Checked/tests,
                    //     used by runtest.sh as the "--testNativeBinDir" argument.

                    // These commands are assumed to be run from the root of the workspace.
                    buildCommands += "zip -r coreroot.${lowerConfiguration}.zip ./bin/tests/Linux.arm.${configuration}/Tests/Core_Root"
                    buildCommands += "zip -r testnativebin.${lowerConfiguration}.zip ./bin/obj/Linux.arm.${configuration}/tests"

                    Utilities.addArchival(newJob, "coreroot.${lowerConfiguration}.zip,testnativebin.${lowerConfiguration}.zip", "")
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
    // The "innerloop" (Pri-0 testing) scenario is only available as PR triggered.
    // All other scenarios do Pri-1 testing.
    if (scenario == 'innerloop' && !isPR) {
        return false
    }

    // Tizen is only supported for armem architecture
    if (os == 'Tizen' && architecture != 'armem') {
        return false
    }

    // Filter based on architecture.

    switch (architecture) {
        case 'arm64':
        case 'arm':
            if ((os != 'Windows_NT') && (os != 'Ubuntu')) {
                return false
            }
            break
        case 'armem':
            if ((os != 'Ubuntu') && (os != 'Ubuntu16.04') && (os != 'Tizen')) {
                return false
            }
            break
        case 'armlb':
            // Do not create armlb jobs
            return false
        case 'x86_arm_altjit':
        case 'x64_arm64_altjit':
            if (os != 'Windows_NT') {
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
                // We use build only jobs for Windows arm cross-compilation corefx testing, so we need to generate builds for that.
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

        def isEnabledOS = (os == 'Windows_NT') || (os == 'Ubuntu' && architecture == 'arm') || (os == 'Ubuntu' && isCoreFxScenario(scenario))
        if (!isEnabledOS) {
            return false
        }

        switch (architecture) {
            case 'x64':
            case 'x86_arm_altjit':
            case 'x64_arm64_altjit':
                break

            case 'x86':
                // x86 ubuntu: no stress modes
                if (os == 'Ubuntu') {
                    return false
                }
                break

            case 'arm':
                // We use build only jobs for Windows arm cross-compilation corefx testing, so we need to generate builds for that.
                if (! (isBuildOnly && isCoreFxScenario(scenario)) ) {
                    return false
                }
                break

            default:
                // arm64, armlb: stress is handled through flow jobs.
                // armem: no stress jobs for ARM emulator.
                return false
        }
    }
    else if (isR2RScenario(scenario)) {
        if (os != 'Windows_NT') {
            return false
        }
        // Stress scenarios only run with Checked builds, not Release (they would work with Debug, but be slow).
        if ((configuration != 'Checked') && isR2RStressScenario(scenario)) {
            return false
        }
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
            default:
                println("Unknown scenario: ${scenario}")
                assert false
                break
        }
    }

    // For altjit, don't do any scenarios that don't change compilation. That is, scenarios that only change
    // runtime behavior, not compile-time behavior, are not interesting.
    switch (architecture) {
        case 'x86_arm_altjit':
        case 'x64_arm64_altjit':
            if (isGCStressRelatedTesting(scenario)) {
                return false
            }
            break
        default:
            break
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
                    addToViews(newJob, isPR, architecture, os)

                    setJobMachineAffinity(architecture, os, true, false, false, newJob) // isBuildJob = true, isTestJob = false, isFlowJob = false

                    Utilities.standardJobSetup(newJob, project, isPR, "*/${branch}")
                    addTriggers(newJob, branch, isPR, architecture, os, configuration, scenario, false, isBuildOnly) // isFlowJob==false
                    setJobTimeout(newJob, isPR, architecture, configuration, scenario, isBuildOnly)

                    // Copy Windows build test binaries and corefx build artifacts for Linux cross build for armem.
                    // We don't use a flow job for this, but we do depend on there being existing builds with these
                    // artifacts produced.
                    if (architecture == 'armem' && (os == 'Ubuntu' || os == 'Ubuntu16.04' || os == 'Tizen')) {
                        // Define the Windows Tests and Corefx build job names
                        def lowerConfiguration = configuration.toLowerCase()
                        def WindowsTestsName = projectFolder + '/' +
                                               Utilities.getFullJobName(project,
                                                                        getJobName(lowerConfiguration, 'x64' , 'windows_nt', 'normal', true),
                                                                        false)
                        def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' +
                                           Utilities.getFolderName(branch)

                        def arm_abi = 'arm'
                        def corefx_os = 'linux'
                        if (os == 'Tizen') {
                            arm_abi = 'armel'
                            corefx_os = 'tizen'
                        }

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
                                    includePatterns('bin/build.tar.gz')
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

// Create a Windows ARM/ARMLB/ARM64 test job that will be used by a flow job.
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

                // Only arm supported for corefx testing now.
                assert architecture == 'arm'

                // Unzip CoreFx runtime
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('_\\fx\\fxruntime.zip', '_\\fx\\bin\\testhost\\netcoreapp-Windows_NT-Release-arm')")

                // Unzip CoreFx tests.
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('_\\fx\\fxtests.zip', '_\\fx\\bin\\tests')")

                // Add the script to run the corefx tests
                def corefx_runtime_path   = "%WORKSPACE%\\_\\fx\\bin\\testhost\\netcoreapp-Windows_NT-Release-arm"
                def corefx_tests_dir      = "%WORKSPACE%\\_\\fx\\bin\\tests"
                def corefx_exclusion_file = "%WORKSPACE%\\tests\\arm\\corefx_test_exclusions.txt"
                batchFile("call %WORKSPACE%\\tests\\scripts\\run-corefx-tests.bat ${corefx_runtime_path} ${corefx_tests_dir} ${corefx_exclusion_file}")

            } else { // !isCoreFxScenario(scenario)

                // Unzip tests.
                batchFile("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::ExtractToDirectory('bin\\tests\\tests.zip', 'bin\\tests\\${osGroup}.${architecture}.${configuration}')")

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

                // ARM legacy backend; this is an altjit.
                if (architecture == 'armlb') {
                    addEnvVariable("COMPlus_AltJit", "*")
                    addEnvVariable("COMPlus_AltJitNgen", "*")
                    addEnvVariable("COMPlus_AltJitName", "legacyjit.dll")
                    addEnvVariable("COMPlus_AltJitAssertOnNYI", "1")
                }

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

                // Create the smarty command
                def smartyCommand = "C:\\Tools\\Smarty.exe /noecid /noie /workers 9 /inc EXPECTED_PASS "
                def addSmartyFlag = { flag -> smartyCommand += flag + " "}
                def addExclude = { exclude -> addSmartyFlag("/exc " + exclude)}
                def addArchSpecificExclude = { architectureToExclude, exclude -> if (architectureToExclude == "armlb") { addExclude("LEGACYJIT_" + exclude) } else { addExclude(exclude) } }

                if (architecture == 'armlb') {
                    addExclude("LEGACYJIT_FAIL")
                }

                // Exclude tests based on scenario.
                Constants.validArmWindowsScenarios[scenario].each { excludeTag ->
                    addArchSpecificExclude(architecture, excludeTag)
                }

                // Innerloop jobs run Pri-0 tests; everyone else runs Pri-1.
                if (scenario == 'innerloop') {
                    addExclude("pri1")
                }

                // Exclude any test marked LONG_RUNNING; these often exceed the standard timeout and fail as a result.
                // TODO: We should create a "long running" job that runs these with a longer timeout.
                addExclude("LONG_RUNNING")

                smartyCommand += "/lstFile Tests.lst"

                def testListArch = [
                    'arm64': 'arm64',
                    'arm': 'arm',
                    'armlb': 'arm'
                ]

                def archLocation = testListArch[architecture]

                addCommand("copy %WORKSPACE%\\tests\\${archLocation}\\Tests.lst bin\\tests\\${osGroup}.${architecture}.${configuration}")
                addCommand("pushd bin\\tests\\${osGroup}.${architecture}.${configuration}")
                addCommand("${smartyCommand}")

                // Save the errorlevel from the smarty command to be used as the errorlevel of this batch file.
                // However, we also need to remove all the variables that were set during this batch file, so we
                // can run the ZIP powershell command (below) in a clean environment. (We can't run the powershell
                // command with the COMPlus_AltJit variables set, for example.) To do that, we do ENDLOCAL as well
                // as save the current errorlevel on the same line. This works because CMD evaluates the %errorlevel%
                // variable expansion (or any variable expansion on the line) BEFORE it executes the ENDLOCAL command.
                // Note that the ENDLOCAL also undoes the pushd command, but we add the popd here for clarity.
                addCommand("popd & ENDLOCAL & set __save_smarty_errorlevel=%errorlevel%")

                // ZIP up the smarty output, no matter what the smarty result.
                addCommand("powershell -NoProfile -Command \"Add-Type -Assembly 'System.IO.Compression.FileSystem'; [System.IO.Compression.ZipFile]::CreateFromDirectory('.\\bin\\tests\\${osGroup}.${architecture}.${configuration}\\Smarty.run.0', '.\\bin\\tests\\${osGroup}.${architecture}.${configuration}\\Smarty.run.0.zip')\"")

                addCommand("echo %errorlevel%")
                addCommand("dir .\\bin\\tests\\${osGroup}.${architecture}.${configuration}")

                // Use the smarty errorlevel as the script errorlevel.
                addCommand("exit /b %__save_smarty_errorlevel%")

                batchFile(buildCommands)
            } // non-corefx testing
        } // steps
    } // job

    if (!isCoreFxScenario(scenario)) {
        Utilities.addArchival(newJob, "bin/tests/${osGroup}.${architecture}.${configuration}/Smarty.run.0/*.smrt", '', true, false)

        // Archive a ZIP file of the entire Smarty.run.0 directory. This is possibly a little too much,
        // but there is no easy way to only archive the HTML/TXT files of the failing tests, so we get
        // all the passing test info as well. Not necessarily a bad thing, but possibly somewhat large.
        Utilities.addArchival(newJob, "bin/tests/${osGroup}.${architecture}.${configuration}/Smarty.run.0.zip", '', true, false)
    }

    return newJob
}

// Create a test job not covered by the "Windows ARM" case that will be used by a flow job.
// E.g., non-Windows tests.
// Returns the newly created job.
def static CreateOtherTestJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName, def inputTestsBuildName)
{
    def isUbuntuArmJob = ((os == "Ubuntu") && (architecture == 'arm')) // ARM Ubuntu running on hardware (not emulator)

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
            stringParam('CORECLR_WINDOWS_BUILD', '', 'Build number to copy CoreCLR Windows test binaries from')
            stringParam('CORECLR_BUILD', '', "Build number to copy CoreCLR ${osGroup} binaries from")
        }

        steps {
            // Set up the copies

            // Coreclr build containing the tests and mscorlib
            // pri1 jobs still need to copy windows_nt built tests
            assert inputTestsBuildName != null
            copyArtifacts(inputTestsBuildName) {
                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                buildSelector {
                    buildNumber('${CORECLR_WINDOWS_BUILD}')
                }
            }

            // Coreclr build we are trying to test
            //
            //  ** NOTE ** This will, correctly, overwrite the CORE_ROOT from the Windows test archive

            copyArtifacts(inputCoreCLRBuildName) {
                excludePatterns('**/testResults.xml', '**/*.ni.dll')
                buildSelector {
                    buildNumber('${CORECLR_BUILD}')
                }
            }

            if (isUbuntuArmJob) {
                // Add some useful information to the log file. Ignore return codes.
                shell("uname -a || true")
            }

            if (architecture == 'arm64') {
                shell("mkdir -p ./bin/CoreFxBinDir")
                shell("cp ./bin/Product/Linux.arm64.${configuration}/corefxNative/* ./bin/CoreFxBinDir")
                shell("chmod +x ./bin/Product/Linux.arm64.${configuration}/corerun")
            }
            else if (architecture == 'x86') {
                shell("mkdir ./bin/CoreFxNative")

                def corefxFolder = Utilities.getFolderName('dotnet/corefx') + '/' + Utilities.getFolderName(branch)

                copyArtifacts("${corefxFolder}/ubuntu16.04_x86_release") {
                    includePatterns('bin/build.tar.gz')
                    targetDirectory('bin/CoreFxNative')
                    buildSelector {
                        latestSuccessful(true)
                    }
                }

                shell("tar -xf ./bin/CoreFxNative/bin/build.tar.gz -C ./bin/CoreFxBinDir")
            }

            // Unzip the tests first.  Exit with 0
            shell("unzip -q -o ./bin/tests/tests.zip -d ./bin/tests/${osGroup}.${architecture}.${configuration} || exit 0")
            shell("rm -r ./bin/tests/${osGroup}.${architecture}.${configuration}/Tests/Core_Root || exit 0")

            // For arm Ubuntu (on hardware), we do the "build-test" step on the build machine, not on the test
            // machine. The arm Ubuntu test machines do no building -- they have no CLI, for example.
            // We should probably do the "generatelayoutonly" step on the build machine for all architectures.
            // However, it's believed that perhaps there's an issue with executable permission bits not getting
            // copied correctly.
            if (isUbuntuArmJob) {
                def lowerConfiguration = configuration.toLowerCase()
                shell("unzip -o ./coreroot.${lowerConfiguration}.zip || exit 0")      // unzips to ./bin/tests/Linux.arm.${configuration}/Tests/Core_Root
                shell("unzip -o ./testnativebin.${lowerConfiguration}.zip || exit 0") // unzips to ./bin/obj/Linux.arm.${configuration}/tests
            }
            else {
                shell("./build-test.sh ${architecture} ${configuration} generatelayoutonly")
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

            // If we are running a stress mode, we'll set those variables first
            if (isJitStressScenario(scenario)) {
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

            def runScript = ""
            if (isUbuntuArmJob) {
                // Use 'runtesttilstable.sh' to rerun failing tests (in sequential mode);
                // there are many tests that pass on rerun (currently), and we don't want
                // that flakiness to affect overall test job robustness.
                runScript = "${dockerCmd}./tests/runtesttilstable.sh"
            } else {
                runScript = "${dockerCmd}./tests/runtest.sh"
            }

            shell("""\
${runScript} \\
    --testRootDir=\"\${WORKSPACE}/bin/tests/${osGroup}.${architecture}.${configuration}\" \\
    --coreOverlayDir=\"\${WORKSPACE}/bin/tests/${osGroup}.${architecture}.${configuration}/Tests/Core_Root\" \\
    --testNativeBinDir=\"\${WORKSPACE}/bin/obj/${osGroup}.${architecture}.${configuration}/tests\" \\
    --copyNativeTestBin --limitedDumpGeneration ${testOpts}""")

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

    Utilities.addArchival(newJob, "bin/tests/${osGroup}.${architecture}.${configuration}/coreclrtests.*.txt")
    Utilities.addXUnitDotNETResults(newJob, '**/coreclrtests.xml')

    return newJob
}

// Create a test job that will be used by a flow job.
// Returns the newly created job.
def static CreateTestJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def inputCoreCLRBuildName, def inputTestsBuildName)
{
    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))

    def newJob = null
    if (windowsArmJob) {
        assert inputTestsBuildName == null
        newJob = CreateWindowsArmTestJob(dslFactory, project, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName)
    } else {
        newJob = CreateOtherTestJob(dslFactory, project, branch, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName, inputTestsBuildName)
    }

    setJobMachineAffinity(architecture, os, false, true, false, newJob) // isBuildJob = false, isTestJob = true, isFlowJob = false

    addToViews(newJob, isPR, architecture, os)

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
def static CreateFlowJob(def dslFactory, def project, def branch, def architecture, def os, def configuration, def scenario, def isPR, def fullTestJobName, def inputCoreCLRBuildName, def inputTestsBuildName)
{
    // Windows CoreCLR build and Linux CoreCLR build (in parallel) ->
    // Linux CoreCLR test
    def flowJobName = getJobName(configuration, architecture, os, scenario, false) + "_flow"
    def jobFolder = getJobFolder(scenario)

    def newFlowJob = null

    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))
    if (windowsArmJob) {

        assert inputTestsBuildName == null

        // For Windows arm jobs there is no reason to build a parallel test job.
        // The product build supports building and archiving the tests.

        newFlowJob = dslFactory.buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR, jobFolder)) {
                        buildFlow("""\
coreclrBuildJob = build(params, '${inputCoreCLRBuildName}')

// And then build the test build
build(params + [CORECLR_BUILD: coreclrBuildJob.build.number], '${fullTestJobName}')
""")
        }
        JobReport.Report.addReference(inputCoreCLRBuildName)
        JobReport.Report.addReference(fullTestJobName)
    }
    else {
        newFlowJob = dslFactory.buildFlowJob(Utilities.getFullJobName(project, flowJobName, isPR, jobFolder)) {
                        buildFlow("""\
// Build the input jobs in parallel
parallel (
{ coreclrBuildJob = build(params, '${inputCoreCLRBuildName}') },
{ windowsBuildJob = build(params, '${inputTestsBuildName}') }
)

// And then build the test build
build(params + [CORECLR_BUILD: coreclrBuildJob.build.number,
                CORECLR_WINDOWS_BUILD: windowsBuildJob.build.number], '${fullTestJobName}')
""")
        }
        JobReport.Report.addReference(inputCoreCLRBuildName)
        JobReport.Report.addReference(inputTestsBuildName)
        JobReport.Report.addReference(fullTestJobName)
    }

    addToViews(newFlowJob, isPR, architecture, os)

    setJobMachineAffinity(architecture, os, false, false, true, newFlowJob) // isBuildJob = false, isTestJob = false, isFlowJob = true

    Utilities.standardJobSetup(newFlowJob, project, isPR, "*/${branch}")
    addTriggers(newFlowJob, branch, isPR, architecture, os, configuration, scenario, true, false) // isFlowJob==true, isWindowsBuildOnlyJob==false

    return newFlowJob
}

// Determine if we should generate a flow job for the given parameters.
// Returns true if the job should be generated.
def static shouldGenerateFlowJob(def scenario, def isPR, def architecture, def configuration, def os)
{
    // The "innerloop" (Pri-0 testing) scenario is only available as PR triggered.
    // All other scenarios do Pri-1 testing.
    if (scenario == 'innerloop' && !isPR) {
        return false
    }

    // Filter based on OS and architecture.

    switch (architecture) {
        case 'arm64':
            if (os != "Ubuntu" && os != "Windows_NT") {
                return false
            }
            break
        case 'armlb':
            if (os != 'Windows_NT') {
                return false
            }
            // Do not create armlb windows jobs.
            return false
        case 'arm':
            if (os != "Ubuntu" && os != "Windows_NT") {
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
        case 'x86_arm_altjit':
        case 'x64_arm64_altjit':
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
        if (!isArmWindowsScenario(scenario)) {
            return false
        }
    }
    else {
        // Non-Windows
        if (architecture == 'arm64') {
            if (!(scenario in Constants.validLinuxArm64Scenarios)) {
                return false
            }
        }
        else if (architecture == 'arm') {
            if (!(scenario in Constants.validLinuxArmScenarios)) {
                return false
            }
        }
        else if (architecture == 'x86') {
            // Linux/x86 only want innerloop and default test
            if (!isNormalOrInnerloop) {
                return false
            }
        }
    }

    // For CentOS, we only want Checked/Release builds.
    if (os == 'CentOS7.1') {
        if (configuration != 'Checked' && configuration != 'Release') {
            return false
        }
        if (!isNormalOrInnerloop && !isR2RScenario(scenario) && !isJitStressScenario(scenario)) {
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

        // CoreFx JIT stress tests currently only implemented for Windows ARM.
        if (isCoreFxScenario(scenario) && !( (architecture == 'arm') && (os == 'Windows_NT') )) {
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
                // Nothing skipped
                if (!isValidPrTriggeredInnerLoopJob(os, architecture, configuration, false)) {
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

// Create jobs requiring flow jobs. This includes x64 non-Windows, arm/arm64 Ubuntu, and arm/arm64/armlb Windows.
// Note: no armlb non-Windows; we expect to deprecate/remove armlb soon, so don't want to add new testing for it.
Constants.allScenarios.each { scenario ->
    [true, false].each { isPR ->
        Constants.architectureList.each { architecture ->
            Constants.configurationList.each { configuration ->
                Constants.osList.each { os ->

                    if (!shouldGenerateFlowJob(scenario, isPR, architecture, configuration, os)) {
                        return
                    }

                    // Figure out the job name of the CoreCLR build the test will depend on.

                    def inputCoreCLRBuildScenario = scenario == 'innerloop' ? 'innerloop' : 'normal'
                    def inputCoreCLRBuildIsBuildOnly = false
                    if (isCoreFxScenario(scenario)) {
                        // Every CoreFx test depends on its own unique build.
                        inputCoreCLRBuildScenario = scenario
                        inputCoreCLRBuildIsBuildOnly = true
                    }
                    def inputCoreCLRFolderName = getJobFolder(inputCoreCLRBuildScenario)
                    def inputCoreCLRBuildName = projectFolder + '/' +
                        Utilities.getFullJobName(project, getJobName(configuration, architecture, os, inputCoreCLRBuildScenario, inputCoreCLRBuildIsBuildOnly), isPR, inputCoreCLRFolderName)

                    // Figure out the name of the build job that the test job will depend on.
                    // For Windows ARM tests, this is not used, as the CoreCLR build creates the tests. For other
                    // tests (e.g., Linux ARM), we depend on a Windows build to get the tests.

                    def inputTestsBuildName = null

                    def windowsArmJob = ((os == "Windows_NT") && (architecture in Constants.armWindowsCrossArchitectureList))
                    if (!windowsArmJob) {
                        def testBuildScenario = scenario == 'innerloop' ? 'innerloop' : 'normal'

                        def inputTestsBuildArch = architecture
                        if (architecture == "arm64") {
                            // Use the x64 test build for arm64 unix
                            inputTestsBuildArch = "x64"
                        }
                        else if (architecture == "arm") {
                            // Use the x86 test build for arm unix
                            inputTestsBuildArch = "x86"
                        }

                        def inputTestsBuildIsBuildOnly = true

                        inputTestsBuildName = projectFolder + '/' +
                            Utilities.getFullJobName(project, getJobName(configuration, inputTestsBuildArch, 'windows_nt', testBuildScenario, inputTestsBuildIsBuildOnly), isPR)
                    }

                    // =============================================================================================
                    // Create the test job
                    // =============================================================================================

                    def testJob = CreateTestJob(this, project, branch, architecture, os, configuration, scenario, isPR, inputCoreCLRBuildName, inputTestsBuildName)

                    // =============================================================================================
                    // Create a build flow to join together the build and tests required to run this test.
                    // =============================================================================================

                    if (os == 'RHEL7.2' || os == 'Debian8.4') {
                        // Do not create the flow job for RHEL jobs.
                        return
                    }

                    def fullTestJobName = projectFolder + '/' + testJob.name
                    def flowJob = CreateFlowJob(this, project, branch, architecture, os, configuration, scenario, isPR, fullTestJobName, inputCoreCLRBuildName, inputTestsBuildName)

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
