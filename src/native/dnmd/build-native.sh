__ProjectRoot="$(cd "$(dirname "$0")"; pwd -P)"
__RepoRootDir="$(cd "$__ProjectRoot"/../../..; pwd -P)"

__TargetArch=
__BuildType=Debug
__TargetOS=
__Compiler=clang
__UseNinja=0
__SkipConfigure=0
__PortableBuild=1

source "$__RepoRootDir"/eng/native/build-commons.sh

__ConfigTriplet="$__TargetOS.$__TargetArch.$__BuildType"
__ArtifactsObjDir="$__RepoRootDir/artifacts/obj"
__IntermediatesDir="$__ArtifactsObjDir/dnmd/$__ConfigTriplet"

build_native "$__HostOS" "$__HostArch" "$__ProjectRoot" "$__IntermediatesDir" "" "$__CMakeArgs" "DNMD"
