#!/usr/bin/env bash
: '
  Compares contents of `env/Version.Details.xml` between HEAD and difftarget, and emits variables named for
  dependencies that satisfy either of:
    1. version, or sha changed
    2. it is missing from one of the xmls

  The dependency names have `.` replaced with `_`.

  In order to consume these variables in a yaml pipeline, reference them via: $[ dependencies.<JobName>.outputs["<StepName>.<DependencyName>"] ]

  Example:
  -difftarget ''HEAD^1''
'

# Disable globbing in this bash script since we iterate over path patterns
set -f

# Stop script if unbound variable found (use ${var:-} if intentional)
set -u

# Stop script if command returns non-zero exit code.
# Prevents hidden errors caused by missing error code propagation.
set -e

usage()
{
  echo "Script that emits an azure devops variable with all the dependencies that changed in 'eng/Version.Details.xml' contained in the current HEAD against the difftarget"
  echo "  --difftarget <value>       SHA or branch to diff against. (i.e: HEAD^1, origin/main, 0f4hd36, etc.)"
  echo "  --azurevariableprefix      Name of azure devops variable to create if change meets filter criteria"
  echo ""

  echo "Arguments can also be passed in with a single hyphen."
}

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done

scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
eng_root=`cd -P "$scriptroot/.." && pwd`

azure_variable_prefix=''
diff_target=''

while [[ $# > 0 ]]; do
  opt="$(echo "${1/#--/-}" | tr "[:upper:]" "[:lower:]")"
  case "$opt" in
    -help|-h)
      usage
      exit 0
      ;;
    -difftarget)
      diff_target=$2
      shift
      ;;
    -azurevariableprefix)
      azure_variable_prefix=$2
      shift
      ;;
  esac

  shift
done

if [[ -z "$diff_target" ]]; then
    echo "Argument -difftarget is required"
    usage
    exit 1
fi

oldXmlPath=`mktemp`

ci=true # Needed in order to use pipeline-logging-functions.sh
. "$eng_root/common/pipeline-logging-functions.sh"

git show $diff_target:eng/Version.Details.xml > $oldXmlPath
# FIXME: errors?
changed_deps=$(python3 "$eng_root/pipelines/get-changed-darc-deps.py" $oldXmlPath eng/Version.Details.xml)
rm -f $oldXmlPath

if [[ -n "$azure_variable_prefix" ]]; then
    azure_variable_prefix="${azure_variable_prefix}_"
fi

for dep in $changed_deps; do
    dep=`echo $dep | tr \. _`
    var_name=${azure_variable_prefix}${dep}
    echo "Setting pipeline variable $var_name=true"
    Write-PipelineSetVariable -name $var_name -value true
done
