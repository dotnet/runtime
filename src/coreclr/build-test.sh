#!/usr/bin/env bash

build_test_wrappers()
{
    if [ $__BuildTestWrappers -ne -0 ]; then
        echo "${__MsgPrefix}Creating test wrappers..."

        export __Exclude="${__ProjectDir}/tests/issues.targets"
        export __BuildLogRootName="Tests_XunitWrapper"

        buildVerbosity="Summary"

        if [ $__VerboseBuild == 1 ]; then
            buildVerbosity="Diag"
        fi

        # Set up directories and file names
        __BuildLogRootName=$subDirectoryName
        __BuildLog="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.log"
        __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.wrn"
        __BuildErr="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.err"
        __MsbuildLog="/fileloggerparameters:\"Verbosity=normal;LogFile=${__BuildLog}\""
        __MsbuildWrn="/fileloggerparameters1:\"WarningsOnly;LogFile=${__BuildWrn}\""
        __MsbuildErr="/fileloggerparameters2:\"ErrorsOnly;LogFile=${__BuildErr}\""
        __Logging="$__MsbuildLog $__MsbuildWrn $__MsbuildErr /consoleloggerparameters:$buildVerbosity"

        nextCommand="\"${__DotNetCli}\" msbuild \"${__ProjectDir}/tests/src/runtest.proj\" /nodereuse:false /p:BuildWrappers=true /p:TargetsWindows=false $__Logging /p:__BuildOS=$__BuildOS /p:__BuildType=$__BuildType /p:__BuildArch=$__BuildArch"
        eval $nextCommand

        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: XUnit wrapper build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "XUnit Wrappers have been built."
            echo { "\"build_os\"": "\"${__BuildOS}\"", "\"build_arch\"": "\"${__BuildArch}\"", "\"build_type\"": "\"${__BuildType}\"" } > "${__TestWorkingDir}/build_info.json"

        fi
    fi
}

generate_layout()
{
    echo "${__MsgPrefix}Creating test overlay..."

    __TestDir=$__ProjectDir/tests
    __ProjectFilesDir=$__TestDir
    __TestBinDir=$__TestWorkingDir

    if [ $__RebuildTests -ne 0 ]; then
        if [ -d "${__TestBinDir}" ]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf $__TestBinDir
        fi
    fi

    __CMakeBinDir="${__TestBinDir}"

    if [ -z "$__TestIntermediateDir" ]; then
        __TestIntermediateDir="tests/obj/${__BuildOS}.${__BuildArch}.${__BuildType}"
    fi

    echo "__BuildOS: ${__BuildOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediateDir: ${__TestIntermediateDir}"

    if [ ! -f "$__TestBinDir" ]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p $__TestBinDir
    fi
    if [ ! -f "$__LogsDir" ]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p $__LogsDir
    fi
    if [ ! -f "$__MsbuildDebugLogsDir" ]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p $__MsbuildDebugLogsDir
    fi

    # Set up the directory for MSBuild debug logs.
    export MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    build_MSBuild_projects "Restore_Packages" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    echo "${__MsgPrefix}Creating test overlay..."

    if [ -z "$xUnitTestBinBase" ]; then
        xUnitTestBinBase=$__TestWorkingDir
    fi

    export CORE_ROOT=$xUnitTestBinBase/Tests/Core_Root

    if [ -d "${CORE_ROOT}" ]; then
        rm -rf $CORE_ROOT
    fi

    mkdir -p $CORE_ROOT

    build_MSBuild_projects "Tests_Overlay_Managed" "${__ProjectDir}/tests/src/runtest.proj" "Creating test overlay" "/t:CreateTestOverlay"

    chmod +x $__BinDir/corerun
    chmod +x $__CrossgenExe

    # Make sure to copy over the pulled down packages
    cp -r $__BinDir/* $CORE_ROOT/ > /dev/null

    if [ "$__BuildOS" != "OSX" ]; then
        nextCommand="\"$__TestDir/setup-stress-dependencies.sh\" --arch=$__BuildArch --outputDir=$CORE_ROOT"
        echo "Resolve runtime dependences via $nextCommand"
        eval $nextCommand
        if [ $? != 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: setup-stress-dependencies failed."
            exit 1
        fi
    fi

    # Precompile framework assemblies with crossgen if required
    if [[ $__DoCrossgen != 0 || $__DoCrossgen2 != 0 ]]; then
        precompile_coreroot_fx
    fi
}

patch_corefx_libraries()
{
    echo "${__MsgPrefix}Patching CORE_ROOT: '${CORE_ROOT}' with CoreFX libaries from enlistment '${__LocalCoreFXPath} (${__LocalCoreFXConfig})"

    patchCoreFXArguments=("-clr_core_root" "${CORE_ROOT}" "-fx_root" "${__LocalCoreFXPath}" "-arch" "${__BuildArch}" "-build_type" "${__LocalCoreFXConfig}")
    scriptPath="$__ProjectDir/tests/scripts"
    echo "python ${scriptPath}/patch-corefx.py ${patchCoreFXArguments[@]}"
    $__Python "${scriptPath}/patch-corefx.py" "${patchCoreFXArguments[@]}"
}

precompile_coreroot_fx()
{
    local overlayDir=$CORE_ROOT
    local compilerName=Crossgen

    # Read the exclusion file for this platform
    skipCrossGenFiles=($(grep -v '^#' "$(dirname "$0")/tests/skipCrossGenFiles.${__BuildArch}.txt" 2> /dev/null))
    skipCrossGenFiles+=('System.Runtime.WindowsRuntime.dll')

    # Temporary output folder for Crossgen2-compiled assemblies
    local outputDir=${overlayDir}/out

    # Delete previously crossgened assemblies
    rm ${overlayDir}/*.ni.dll

    # Collect reference assemblies for Crossgen2
    local crossgen2References=""

    if [[ $__DoCrossgen2 != 0 ]]; then
        compilerName=Crossgen2

        mkdir ${outputDir}

        skipCrossGenFiles+=('System.Private.CoreLib.dll')
        skipCrossGenFiles+=('System.Runtime.Serialization.Formatters.dll')
        skipCrossGenFiles+=('Microsoft.CodeAnalysis.CSharp.dll')
        skipCrossGenFiles+=('Microsoft.CodeAnalysis.dll')
        skipCrossGenFiles+=('Microsoft.CodeAnalysis.VisualBasic.dll')
        skipCrossGenFiles+=('CommandLine.dll')

        for reference in ${overlayDir}/*.dll
        do
            crossgen2References+=" -r:${reference}"
        done
    fi

    echo "${__MsgPrefix}Running ${compilerName} on framework assemblies in CORE_ROOT: '${CORE_ROOT}'"

    filesToPrecompile=$(find -L $overlayDir -maxdepth 1 -iname \*.dll -not -iname \*.ni.dll -not -iname \*-ms-win-\* -not -iname xunit.\* -type f)
    for fileToPrecompile in ${filesToPrecompile}
    do
        local filename=${fileToPrecompile}
        if is_skip_crossgen_test "$(basename $filename)"; then
                continue
        fi

        echo Precompiling $filename

        if [[ $__DoCrossgen != 0 ]]; then
            $__CrossgenExe /Platform_Assemblies_Paths $overlayDir $filename 1> $filename.stdout 2>$filename.stderr
        fi

        if [[ $__DoCrossgen2 != 0 ]]; then
            ${overlayDir}/crossgen2/crossgen2 ${crossgen2References} -O --inputbubble --out ${outputDir}/$(basename $filename) $filename 1>$filename.stdout 2>$filename.stderr
        fi

        local exitCode=$?
        if [[ $exitCode != 0 ]]; then
            if grep -q -e '0x80131018' $filename.stderr; then
                printf "\n\t$filename is not a managed assembly.\n\n"
            else
                echo Unable to precompile $filename.
                cat $filename.stdout
                cat $filename.stderr
                exit $exitCode
            fi
        else
            rm $filename.{stdout,stderr}
        fi
    done

    if [[ $__DoCrossgen2 != 0 ]]; then
        # Copy the Crossgen-compiled assemblies back to CORE_ROOT
        mv -f ${outputDir}/* ${overlayDir}/
        rm -r ${outputDir}
    fi
}

declare -a skipCrossGenFiles

function is_skip_crossgen_test {
    for skip in "${skipCrossGenFiles[@]}"; do
        if [ "$1" == "$skip" ]; then
            return 0
        fi
    done
    return 1
}

generate_testhost()
{
    echo "${__MsgPrefix}Generating test host..."

    export TEST_HOST=$xUnitTestBinBase/testhost

    if [ -d "${TEST_HOST}" ]; then
        rm -rf $TEST_HOST
    fi

    mkdir -p $TEST_HOST

    build_MSBuild_projects "Tests_Generate_TestHost" "${__ProjectDir}/tests/src/runtest.proj" "Creating test host" "/t:CreateTestHost"
}


build_Tests()
{
    echo "${__MsgPrefix}Building Tests..."

    __TestDir=$__ProjectDir/tests
    __ProjectFilesDir=$__TestDir
    __TestBinDir=$__TestWorkingDir

    if [ -f  "${__TestWorkingDir}/build_info.json" ]; then
        rm  "${__TestWorkingDir}/build_info.json"
    fi

    if [ $__RebuildTests -ne 0 ]; then
        if [ -d "${__TestBinDir}" ]; then
            echo "Removing tests build dir: ${__TestBinDir}"
            rm -rf $__TestBinDir
        fi
    fi

    export __CMakeBinDir="${__TestBinDir}"
    if [ ! -d "${__TestIntermediatesDir}" ]; then
        mkdir -p ${__TestIntermediatesDir}
    fi

    __NativeTestIntermediatesDir="${__TestIntermediatesDir}/Native"
    if [  ! -d "${__NativeTestIntermediatesDir}" ]; then
        mkdir -p ${__NativeTestIntermediatesDir}
    fi

    __ManagedTestIntermediatesDir="${__TestIntermediatesDir}/Managed"
    if [ ! -d "${__ManagedTestIntermediatesDir}" ]; then
        mkdir -p ${__ManagedTestIntermediatesDir}
    fi

    echo "__BuildOS: ${__BuildOS}"
    echo "__BuildArch: ${__BuildArch}"
    echo "__BuildType: ${__BuildType}"
    echo "__TestIntermediatesDir: ${__TestIntermediatesDir}"
    echo "__NativeTestIntermediatesDir: ${__NativeTestIntermediatesDir}"
    echo "__ManagedTestIntermediatesDir: ${__ManagedTestIntermediatesDir}"

    if [ ! -f "$__TestBinDir" ]; then
        echo "Creating TestBinDir: ${__TestBinDir}"
        mkdir -p $__TestBinDir
    fi
    if [ ! -f "$__LogsDir" ]; then
        echo "Creating LogsDir: ${__LogsDir}"
        mkdir -p $__LogsDir
    fi
    if [ ! -f "$__MsbuildDebugLogsDir" ]; then
        echo "Creating MsbuildDebugLogsDir: ${__MsbuildDebugLogsDir}"
        mkdir -p $__MsbuildDebugLogsDir
    fi

    # Set up the directory for MSBuild debug logs.
    export MSBUILDDEBUGPATH="${__MsbuildDebugLogsDir}"

    __BuildProperties="-p:OSGroup=${__BuildOS} -p:BuildOS=${__BuildOS} -p:BuildArch=${__BuildArch} -p:BuildType=${__BuildType}"

    # =========================================================================================
    # ===
    # === Restore product binaries from packages
    # ===
    # =========================================================================================

    if [ ${__SkipRestorePackages} != 1 ]; then
        build_MSBuild_projects "Restore_Product" "${__ProjectDir}/tests/build.proj" "Restore product binaries (build tests)" "/t:BatchRestorePackages"

        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: package restoration failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [ $__SkipNative != 1 ]; then
        build_native_projects "$__BuildArch" "${__NativeTestIntermediatesDir}"

        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: native test build failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [ $__SkipManaged != 1 ]; then
        echo "Starting the Managed Tests Build..."

        build_MSBuild_projects "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "$__up"

        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: managed test build failed. Refer to the build log files for details (above)"
            exit 1
        else
            echo "Checking the Managed Tests Build..."

            build_MSBuild_projects "Check_Test_Build" "${__ProjectDir}/tests/src/runtest.proj" "Check Test Build" "/t:CheckTestBuild"

            if [ $? -ne 0 ]; then
                echo "${__ErrMsgPrefix}${__MsgPrefix}Error: Check Test Build failed."
                exit 1
            fi
        fi

        echo "Managed tests build success!"

        build_test_wrappers
    fi

    if [ $__CopyNativeTestBinaries == 1 ]; then
        echo "Copying native test binaries to output..."

        build_MSBuild_projects "Tests_Managed" "$__ProjectDir/tests/build.proj" "Managed tests build (build tests)" "/t:CopyAllNativeProjectReferenceBinaries"

        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Error: copying native test binaries failed. Refer to the build log files for details (above)"
            exit 1
        fi
    fi

    if [ -n "$__UpdateInvalidPackagesArg" ]; then
        __up="/t:UpdateInvalidPackageVersions"
    fi

    if [ $__SkipGenerateLayout != 1 ]; then
        generate_layout

        if [ ! -z "$__LocalCoreFXPath" ]; then
            patch_corefx_libraries
        fi
    fi
}

build_MSBuild_projects()
{
    subDirectoryName=$1
    shift
    projectName=$1
    shift
    stepName="$1"
    shift
    extraBuildParameters=("$@")

    # Set up directories and file names
    __BuildLogRootName=$subDirectoryName
    __BuildLog="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.log"
    __BuildWrn="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.wrn"
    __BuildErr="$__LogsDir/${__BuildLogRootName}.${__BuildOS}.${__BuildArch}.${__BuildType}.err"

    if [[ "$subDirectoryName" == "Tests_Managed" ]]; then
        # Execute msbuild managed test build in stages - workaround for excessive data retention in MSBuild ConfigCache
        # See https://github.com/Microsoft/msbuild/issues/2993

        # __SkipPackageRestore and __SkipTargetingPackBuild used  to control build by tests/src/dirs.proj
        export __SkipPackageRestore=false
        export __SkipTargetingPackBuild=false
        export __NumberOfTestGroups=3

        __AppendToLog=false

        if [ -n "$__priority1" ]; then
            export __NumberOfTestGroups=10
        fi

        for (( testGroupToBuild=1 ; testGroupToBuild <= __NumberOfTestGroups; testGroupToBuild = testGroupToBuild + 1 ))
        do
            __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog};Append=${__AppendToLog}\""
            __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn};Append=${__AppendToLog}\""
            __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr};Append=${__AppendToLog}\""

            export __TestGroupToBuild=$testGroupToBuild

            # Generate build command
            buildArgs=("$projectName")
            buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
            buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

            buildArgs+=("${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
            buildArgs+=("${extraBuildParameters[@]}")
            buildArgs+=("${__CommonMSBuildArgs}")
            buildArgs+=("${__UnprocessedBuildArgs[@]}")
            buildArgs+=("\"/p:CopyNativeProjectBinaries=${__CopyNativeProjectsAfterCombinedTestBuild}\"");
            buildArgs+=("/p:__SkipPackageRestore=true");

            # Disable warnAsError - coreclr issue 19922
            nextCommand="\"$__RepoRootDir/eng/common/msbuild.sh\" $__ArcadeScriptArgs --warnAsError false ${buildArgs[@]}"
            echo "Building step '$stepName' testGroupToBuild=$testGroupToBuild via $nextCommand"
            eval $nextCommand

            # Make sure everything is OK
            if [ $? -ne 0 ]; then
                echo "${__ErrMsgPrefix}${__MsgPrefix}Failed to build $stepName. See the build logs:"
                echo "    $__BuildLog"
                echo "    $__BuildWrn"
                echo "    $__BuildErr"
                exit 1
            fi
            export __SkipPackageRestore=true
            export __SkipTargetingPackBuild=true
            __AppendToLog=true
        done
    else
        __msbuildLog="\"/flp:Verbosity=normal;LogFile=${__BuildLog}\""
        __msbuildWrn="\"/flp1:WarningsOnly;LogFile=${__BuildWrn}\""
        __msbuildErr="\"/flp2:ErrorsOnly;LogFile=${__BuildErr}\""

        # Generate build command
        buildArgs=("$projectName")
        buildArgs+=("/p:RestoreDefaultOptimizationDataPackage=false" "/p:PortableBuild=true")
        buildArgs+=("/p:UsePartialNGENOptimization=false" "/maxcpucount")

        buildArgs+=("${__msbuildLog}" "${__msbuildWrn}" "${__msbuildErr}")
        buildArgs+=("${extraBuildParameters[@]}")
        buildArgs+=("${__CommonMSBuildArgs}")
        buildArgs+=("${__UnprocessedBuildArgs[@]}")

        # Disable warnAsError - coreclr issue 19922
        nextCommand="\"$__RepoRootDir/eng/common/msbuild.sh\" $__ArcadeScriptArgs --warnAsError false ${buildArgs[@]}"
        echo "Building step '$stepName' via $nextCommand"
        eval $nextCommand

        # Make sure everything is OK
        if [ $? -ne 0 ]; then
            echo "${__ErrMsgPrefix}${__MsgPrefix}Failed to build $stepName. See the build logs:"
            echo "    $__BuildLog"
            echo "    $__BuildWrn"
            echo "    $__BuildErr"
            exit 1
        fi
    fi
}

build_native_projects()
{
    platformArch="$1"
    intermediatesForBuild="$2"

    extraCmakeArguments=""
    message="native tests assets"

    # All set to commence the build
    echo "Commencing build of $message for $__BuildOS.$__BuildArch.$__BuildType in $intermediatesForBuild"

    generator=""
    if [ $__UseNinja == 1 ]; then
        generator="ninja"
        if ! buildTool=$(command -v ninja || command -v ninja-build); then
           echo "Unable to locate ninja!" 1>&2
           exit 1
        fi
    fi

    if [ $__SkipConfigure == 0 ]; then
        # if msbuild is not supported, then set __SkipGenerateVersion to 1
        if [ $__isMSBuildOnNETCoreSupported == 0 ]; then __SkipGenerateVersion=1; fi
        # Drop version.c file
        __versionSourceFile="$intermediatesForBuild/version.c"
        if [ $__SkipGenerateVersion == 0 ]; then
            pwd
            $__RepoRootDir/eng/common/msbuild.sh $__RepoRootDir/eng/empty.csproj \
                                                 /p:NativeVersionFile=$__versionSourceFile \
                                                 /t:GenerateNativeVersionFile /restore \
                                                 $__CommonMSBuildArgs $__UnprocessedBuildArgs
            if [ $? -ne 0 ]; then
                echo "${__ErrMsgPrefix}Failed to generate native version file."
                exit $?
            fi
        else
            # Generate the dummy version.c, but only if it didn't exist to make sure we don't trigger unnecessary rebuild
            __versionSourceLine="static char sccsid[] __attribute__((used)) = \"@(#)No version information produced\";"
            if [ -e $__versionSourceFile ]; then
                read existingVersionSourceLine < $__versionSourceFile
            fi
            if [ "$__versionSourceLine" != "$existingVersionSourceLine" ]; then
                echo $__versionSourceLine > $__versionSourceFile
            fi
        fi

        scriptDir="$__ProjectRoot/src/pal/tools"
        if [[ $__GccBuild == 0 ]]; then
            echo "Invoking \"$scriptDir/find-clang.sh\" $__ClangMajorVersion \"$__ClangMinorVersion\""
            source "$scriptDir/find-clang.sh" $__ClangMajorVersion "$__ClangMinorVersion"
        else
            echo "Invoking \"$scriptDir/find-gcc.sh\" \"$__GccMajorVersion\" \"$__GccMinorVersion\""
            source "$scriptDir/find-gcc.sh" "$__GccMajorVersion" "$__GccMinorVersion"
        fi

        if [[ -n "$__CodeCoverage" ]]; then
            extraCmakeArguments="$extraCmakeArguments -DCLR_CMAKE_ENABLE_CODE_COVERAGE=1"
        fi

        nextCommand="\"$scriptDir/gen-buildsys.sh\" \"$__TestDir\" \"$intermediatesForBuild\" $platformArch $__BuildType $generator $extraCmakeArguments $__cmakeargs"
        echo "Invoking $nextCommand"
        eval $nextCommand

        if [ $? != 0  ]; then
            echo "${__ErrMsgPrefix}Failed to generate $message build project!"
            exit 1
        fi
    fi

    if [ ! -f "$intermediatesForBuild/CMakeCache.txt" ]; then
        echo "${__ErrMsgPrefix}Unable to find generated build files for $message project!"
        exit 1
    fi

    # Build
    if [ $__ConfigureOnly == 1 ]; then
        echo "Finish configuration & skipping $message build."
        return
    fi

    echo "Executing cmake --build \"$intermediatesForBuild\" --target install -j $__NumProc"

    cmake --build "$intermediatesForBuild" --target install -j $__NumProc

    local exit_code=$?
    if [ $exit_code != 0 ]; then
        echo "${__ErrMsgPrefix}Failed to build $message."
        exit $exit_code
    fi

    echo "Native tests build success!"
}

usage_list=("-buildtestwrappersonly - only build the test wrappers.")
usage_list+=("-copynativeonly: Only copy the native test binaries to the managed output. Do not build the native or managed tests.")
usage_list+=("-crossgen - Precompiles the framework managed assemblies in coreroot.")
usage_list+=("-generatelayoutonly - only pull down dependencies and build coreroot.")
usage_list+=("-generatetesthostonly - only pull down dependencies and build coreroot and the CoreFX testhost.")
usage_list+=("-priority1 - include priority=1 tests in the build.")
usage_list+=("-runtests - run tests after building them.")
usage_list+=("-skipgeneratelayout: Do not generate the Core_Root layout or the CoreFX testhost.")
usage_list+=("-skiprestorepackages - skip package restore.")

# Obtain the location of the bash script to figure out where the root of the repo is.
__ProjectRoot="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
__RepoRootDir=${__ProjectRoot}/../..

handle_arguments() {
    case $1 in
        buildtestwrappersonly|-buildtestwrappersonly)
            __BuildTestWrappersOnly=1
            ;;

        copynativeonly|-copynativeonly)
            __SkipNative=1
            __SkipManaged=1
            __CopyNativeTestBinaries=1
            __CopyNativeProjectsAfterCombinedTestBuild=true
            ;;

        crossgen|-crossgen)
            __DoCrossgen=1
            ;;

        crossgen2|-crossgen2)
            __DoCrossgen2=1
            ;;

        generatetesthostonly|-generatetesthostonly)
            __GenerateTestHostOnly=1
            ;;

        generatelayoutonly|-generatelayoutonly)
            __GenerateLayoutOnly=1
            ;;

        priority1|-priority1)
            __priority1=1
            __UnprocessedBuildArgs+=("/p:CLRTestPriorityToBuild=1")
            ;;

        runtests|-runtests)
            __RunTests=1
            ;;

        skiprestorepackages|-skiprestorepackages)
            __SkipRestorePackages=1
            ;;

        skipgeneratelayout|-skipgeneratelayout)
            __SkipGenerateLayout=1
            ;;

        localcorefxpath=*|-localcorefxpath=*)
            __LocalCoreFXPath=$(echo "$1" | cut -d'=' -f 2)
            ;;

        localcorefxconfig=*|-localcorefxconfig=*)
            __LocalCoreFXConfig=$(echo "$1" | cut -d'=' -f 2)
            ;;

        *)
            __UnprocessedBuildArgs+=("$1")
            ;;
    esac
}

__BuildArch=
__BuildType=Debug
__CodeCoverage=
__IncludeTests=INCLUDE_TESTS

# Set the various build properties here so that CMake and MSBuild can pick them up
export __ProjectDir="$__ProjectRoot"
__BuildTestWrappers=1
__BuildTestWrappersOnly=
__ClangMajorVersion=0
__ClangMinorVersion=0
__CommonMSBuildArgs=
__ConfigureOnly=0
__CopyNativeProjectsAfterCombinedTestBuild=true
__CopyNativeTestBinaries=0
__CrossBuild=0
__DistroRid=""
__DoCrossgen=0
__DoCrossgen2=0
__DotNetCli="$__ProjectDir/dotnet.sh"
__GccBuild=0
__GccMajorVersion=0
__GccMinorVersion=0
__GenerateLayoutOnly=
__GenerateTestHostOnly=
__MSBCleanBuildArgs=
__NativeTestIntermediatesDir=
__PortableBuild=1
__RebuildTests=0
__RootBinDir="$__RepoRootDir/artifacts"
__RunTests=0
__SkipConfigure=0
__SkipGenerateLayout=0
__SkipGenerateVersion=0
__SkipManaged=0
__SkipNative=0
__SkipRestore=""
__SkipRestorePackages=0
__SourceDir="$__ProjectDir/src"
__UnprocessedBuildArgs=
__LocalCoreFXPath=
__LocalCoreFXConfig=${__BuildType}
__UseNinja=0
__VerboseBuild=0
__cmakeargs=""
__msbuildonunsupportedplatform=0
__priority1=
CORE_ROOT=

source "$__ProjectRoot"/_build-commons.sh

if [ "${__BuildArch}" != "${__HostArch}" ]; then
    __CrossBuild=1
fi

# Set dependent variables
__LogsDir="$__RootBinDir/log"
__MsbuildDebugLogsDir="$__LogsDir/MsbuildDebugLogs"

# Set the remaining variables based upon the determined build configuration
__BinDir="$__RootBinDir/bin/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__PackagesBinDir="$__BinDir/.nuget"
__TestDir="$__ProjectDir/tests"
__TestWorkingDir="$__RootBinDir/tests/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__IntermediatesDir="$__RootBinDir/obj/coreclr/$__BuildOS.$__BuildArch.$__BuildType"
__TestIntermediatesDir="$__RootBinDir/tests/coreclr/obj/$__BuildOS.$__BuildArch.$__BuildType"
__isMSBuildOnNETCoreSupported=0
__CrossComponentBinDir="$__BinDir"
__CrossCompIntermediatesDir="$__IntermediatesDir/crossgen"

__CrossArch="$__HostArch"
if [ $__CrossBuild == 1 ]; then
    __CrossComponentBinDir="$__CrossComponentBinDir/$__CrossArch"
fi
__CrossgenCoreLibLog="$__LogsDir/CrossgenCoreLib_$__BuildOS.$BuildArch.$__BuildType.log"
__CrossgenExe="$__CrossComponentBinDir/crossgen"

isMSBuildOnNETCoreSupported

# CI_SPECIFIC - On CI machines, $HOME may not be set. In such a case, create a subfolder and set the variable to it.
# This is needed by CLI to function.
if [ -z "$HOME" ]; then
    if [ ! -d "$__ProjectDir/temp_home" ]; then
        mkdir temp_home
    fi
    export HOME=$__ProjectDir/temp_home
    echo "HOME not defined; setting it to $HOME"
fi

# Configure environment if we are doing a cross compile.
if [ $__CrossBuild == 1 ]; then
    export CROSSCOMPILE=1
    if ! [[ -n "$ROOTFS_DIR" ]]; then
        export ROOTFS_DIR="$__RepoRootDir/eng/common/cross/rootfs/$__BuildArch"
    fi
fi

# init the target distro name
initTargetDistroRid

if [ $__PortableBuild == 0 ]; then
    __CommonMSBuildArgs="$__CommonMSBuildArgs /p:PortableBuild=false"
fi

if [[ (-z "$__GenerateLayoutOnly") && (-z "$__GenerateTestHostOnly") && (-z "$__BuildTestWrappersOnly") ]]; then
    build_Tests
elif [ ! -z "$__BuildTestWrappersOnly" ]; then
    build_test_wrappers
else
    generate_layout
    if [ ! -z "$__GenerateTestHostOnly" ]; then
        generate_testhost
    fi
fi

if [ $? -ne 0 ]; then
    echo "Failed to build tests"
    exit 1
fi

echo "${__MsgPrefix}Test build successful."
echo "${__MsgPrefix}Test binaries are available at ${__TestBinDir}"

__testNativeBinDir=$__IntermediatesDir/tests

if [ $__RunTests -ne 0 ]; then

    echo "Run Tests..."

    nextCommand="$__TestDir/runtest.sh --testRootDir=$__TestBinDir --coreClrBinDir=$__BinDir --coreFxBinDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir"
    echo "$nextCommand"
    eval $nextCommand

    echo "Tests run successful."
else
    echo "To run all tests use 'tests/runtests.sh' where:"
    echo "    testRootDir      = $__TestBinDir"
    echo "    coreClrBinDir    = $__BinDir"
    echo "    coreFxBinDir     = $CORE_ROOT"
    echo "    testNativeBinDir = $__testNativeBinDir"
    echo " -------------------------------------------------- "
    echo " Example runtest.sh command"
    echo ""
    echo " ./tests/runtest.sh --coreOverlayDir=$CORE_ROOT --testNativeBinDir=$__testNativeBinDir --testRootDir=$__TestBinDir --copyNativeTestBin"
    echo " -------------------------------------------------- "
    echo "To run single test use the following command:"
    echo "    bash ${__TestBinDir}/__TEST_PATH__/__TEST_NAME__.sh -coreroot=${CORE_ROOT}"
fi

