#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
#
# Title: superpmi_sourcegit.py
#
# Notes:
#
#   Script to perform SuperPMI collections over SourceGit
#   (https://github.com/sourcegit-scm/sourcegit), a real-world, third-party Avalonia
#   desktop application. Two kinds of collections are produced:
#
#     * crossgen2 - the app is published framework-dependent and every managed assembly
#                   in the publish folder is compiled with the locally built crossgen2.
#     * nativeaot - the app is published with PublishAot=true just far enough to make the
#                   SDK write the `*.ilc.rsp` response file, which is then replayed with
#                   the locally built ILC. This gives real customer ILC settings (feature
#                   switches, trimming roots, direct P/Invokes, ...) over a large app.
#
#   Everything (clone, publish, collect) happens on the machine running this script, so
#   it is self-contained and needs nothing but a network connection and a Core_Root.
#
# Usage examples:
#
#   # Collect both crossgen2 and nativeaot using the locally built Checked Core_Root:
#   python superpmi_sourcegit.py
#
#   # Only the nativeaot collection, with an explicit Core_Root and output file:
#   python superpmi_sourcegit.py --core_root C:\runtime\artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root ^
#                                --collection_type nativeaot --output_mch sourcegit.nativeaot.mch
#

import argparse
import json
import os
import platform
import re
import shutil
import stat
import subprocess
import sys
import tempfile
import time
import urllib.request
import zipfile
from pathlib import Path

# Repository that is cloned and compiled. It is a large, real-world Avalonia application
# that pulls in a lot of third-party code (Avalonia, AvaloniaEdit, Azure.AI.OpenAI, ...).
DEFAULT_REPO_URL = "https://github.com/EgorBo/sourcegit"
DEFAULT_BRANCH = "master"
DEFAULT_PROJECT = "src/SourceGit.csproj"

# The .NET SDK used to build the app. `global.json` is deliberately not parsed; these values
# are simply chosen so that the SDK we end up using satisfies the `global.json` of the app
# above, which asks for a released 10.0 or newer SDK. Update them if the app moves on.
DEFAULT_SDK_CHANNEL = "10.0"    # dotnet-install channel used when no suitable SDK is installed
DEFAULT_SDK_MIN_MAJOR = 10      # lowest SDK major version accepted from the PATH

COLLECTION_TYPES = ["crossgen2", "nativeaot"]

# ILC options whose shape changed between the SDK that writes the response file and the ILC built
# from this repo. Maps an option that now requires a value to the value to use when the response
# file spells it as a plain flag.
ILC_RSP_FIXUPS = {
    # `--stacktracedata` used to be a flag meaning "emit stack trace metadata"; it now takes one
    # of 'frames', 'lines' or 'none'.
    "--stacktracedata": "frames",
}

is_windows = sys.platform.startswith("win")


def native_dll(name):
    """ Convert a simple name to a native shared library name, e.g. "clrjit" -> "libclrjit.so" on Linux. """
    ext = ".dll" if is_windows else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if is_windows else "lib"
    return f"{prefix}{name}{ext}"


def native_exe(name):
    """ Convert a simple name to a native executable name, e.g. "crossgen2" -> "crossgen2.exe" on Windows. """
    return f"{name}.exe" if is_windows else name


def run_command(cmd, cwd=None, env=None, retries=0, check=True):
    """ Run a command, optionally retrying it. Returns True on success. """
    cmd = [str(c) for c in cmd]
    attempt = 0
    while True:
        print(f"Running: {' '.join(cmd)}" + (f" (cwd: {cwd})" if cwd else ""))
        try:
            subprocess.run(cmd, check=True, cwd=None if cwd is None else str(cwd), env=env)
            return True
        except subprocess.CalledProcessError as e:
            print(f"Command failed with exit code {e.returncode}")
        except Exception as e:
            print(f"Failed to start command: {e}")
        if attempt >= retries:
            if check:
                raise RuntimeError(f"Command failed: {' '.join(cmd)}")
            return False
        attempt += 1
        time.sleep(5)


def default_core_root():
    """ Guess the Checked Core_Root of the enclosing dotnet/runtime checkout, if there is one. """
    repo_root = Path(__file__).resolve().parents[3]
    host_os = "windows" if is_windows else ("osx" if sys.platform == "darwin" else "linux")
    core_root = repo_root / "artifacts" / "tests" / "coreclr" / f"{host_os}.{host_arch()}.Checked" / "Tests" / "Core_Root"
    return core_root if core_root.is_dir() else None


def host_arch():
    """ Return the architecture of the machine in the naming used by the runtime repo. """
    machine = platform.machine().lower()
    if machine in ("amd64", "x86_64"):
        return "x64"
    if machine in ("arm64", "aarch64"):
        return "arm64"
    if machine in ("i386", "i686", "x86"):
        return "x86"
    if machine.startswith("arm"):
        return "arm"
    return machine


def host_rid():
    """ Return the .NET RID of the machine, e.g. "win-x64". """
    os_part = "win" if is_windows else ("osx" if sys.platform == "darwin" else "linux")
    return f"{os_part}-{host_arch()}"


def ensure_git(tools_dir):
    """ Make sure `git` is on the PATH. Helix Windows machines don't have git installed, so
        download a portable one there. """
    if shutil.which("git") is not None:
        return
    if not is_windows:
        raise RuntimeError("git is required but was not found on the PATH")

    print("git was not found, downloading portable git ...")
    with urllib.request.urlopen("https://api.github.com/repos/git-for-windows/git/releases/latest") as response:
        assets = json.loads(response.read())["assets"]
    arch_suffix = {"x64": "64-bit", "arm64": "arm64", "x86": "32-bit"}.get(host_arch(), "64-bit")
    name_regex = re.compile(r"^MinGit-.*-(32-bit|64-bit|arm64)\.zip$", re.I)
    try:
        asset = next(a for a in assets if name_regex.match(a["name"]) and arch_suffix in a["name"])
    except StopIteration:
        raise RuntimeError("Unable to find a MinGit asset for " + arch_suffix)

    tools_dir.mkdir(parents=True, exist_ok=True)
    zip_path = tools_dir / asset["name"]
    urllib.request.urlretrieve(asset["browser_download_url"], zip_path)
    git_dir = tools_dir / "git"
    shutil.rmtree(git_dir, ignore_errors=True)
    with zipfile.ZipFile(zip_path) as archive:
        archive.extractall(git_dir)
    zip_path.unlink()
    os.environ["PATH"] = str(git_dir / "cmd") + os.pathsep + os.environ.get("PATH", "")


def find_usable_dotnet(min_major):
    """ Return the `dotnet` on the PATH if it has a released (non-preview) SDK whose major version
        is at least `min_major`, otherwise None. This only approximates what the app's
        `global.json` asks for; if the SDK turns out not to satisfy it, the build fails with the
        usual, descriptive SDK resolution error. """
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        return None
    try:
        sdks = subprocess.run([dotnet, "--list-sdks"], check=True, capture_output=True, text=True).stdout
    except Exception:
        return None
    for line in sdks.splitlines():
        version = line.split(" ")[0]
        if "-" in version:  # skip previews, `global.json` files usually set allowPrerelease=false
            continue
        try:
            if int(version.split(".")[0]) >= min_major:
                return Path(dotnet)
        except ValueError:
            continue
    return None


def install_dotnet_sdk(channel, install_dir):
    """ Install a .NET SDK into `install_dir` using the official dotnet-install script. """
    install_dir.mkdir(parents=True, exist_ok=True)
    dotnet = install_dir / native_exe("dotnet")
    if dotnet.exists():
        print(f"Reusing the .NET SDK previously installed in {install_dir}")
        return dotnet

    with tempfile.TemporaryDirectory() as temp_dir:
        if is_windows:
            script = Path(temp_dir) / "dotnet-install.ps1"
            urllib.request.urlretrieve("https://dot.net/v1/dotnet-install.ps1", script)
            run_command(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script,
                         "-Channel", channel, "-InstallDir", install_dir, "-NoPath"], retries=3)
        else:
            script = Path(temp_dir) / "dotnet-install.sh"
            urllib.request.urlretrieve("https://dot.net/v1/dotnet-install.sh", script)
            script.chmod(script.stat().st_mode | stat.S_IXUSR)
            run_command([script, "--channel", channel, "--install-dir", install_dir, "--no-path"], retries=3)
    return dotnet


def write_build_isolation_files(directory):
    """ Drop marker files so that MSBuild/NuGet don't walk up into an enclosing repository
        (e.g. when the working directory lives inside the dotnet/runtime checkout, whose
        Directory.Build.props and NuGet.config would break the build of the cloned app). """
    (directory / "Directory.Build.props").write_text("<Project />\n", encoding="utf-8")
    (directory / "Directory.Build.targets").write_text("<Project />\n", encoding="utf-8")
    (directory / "Directory.Build.rsp").write_text("", encoding="utf-8")
    (directory / "Directory.Packages.props").write_text(
        "<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>"
        "</PropertyGroup></Project>\n", encoding="utf-8")
    (directory / "NuGet.config").write_text(
        '<?xml version="1.0" encoding="utf-8"?>\n'
        '<configuration>\n'
        '  <packageSources>\n'
        '    <clear />\n'
        '    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />\n'
        '  </packageSources>\n'
        '  <disabledPackageSources>\n'
        '    <clear />\n'
        '  </disabledPackageSources>\n'
        '</configuration>\n', encoding="utf-8")


def clone_repo(url, branch, commit, destination):
    """ Clone the application repository (including its submodules) into `destination`. """
    if destination.exists():
        print(f"Reusing the existing clone in {destination}")
        return
    run_command(["git", "clone", "--quiet", "--recurse-submodules", "--shallow-submodules",
                 "--branch", branch] + ([] if commit else ["--depth", "1"]) + [url, destination], retries=2)
    if commit:
        run_command(["git", "checkout", "--quiet", commit], cwd=destination)
        run_command(["git", "submodule", "update", "--init", "--recursive", "--quiet"], cwd=destination)


def is_managed_assembly(path):
    """ Return True if `path` is a managed assembly (i.e. a PE file with a CLI header). """
    try:
        with open(path, "rb") as file_handle:
            data = file_handle.read(4096)
        if len(data) < 0x40 or data[:2] != b"MZ":
            return False
        pe_offset = int.from_bytes(data[0x3C:0x40], "little")
        if len(data) < pe_offset + 24 or data[pe_offset:pe_offset + 4] != b"PE\0\0":
            return False
        optional_header = pe_offset + 24
        magic = int.from_bytes(data[optional_header:optional_header + 2], "little")
        if magic == 0x10B:      # PE32
            data_directories = optional_header + 96
        elif magic == 0x20B:    # PE32+
            data_directories = optional_header + 112
        else:
            return False
        # The CLI header is data directory number 14.
        cli_header = data_directories + 14 * 8
        if len(data) < cli_header + 8:
            return False
        return int.from_bytes(data[cli_header:cli_header + 4], "little") != 0
    except OSError:
        return False


def publish_app(dotnet, project, rid, output_dir):
    """ Publish `project` for `rid` into `output_dir` as a plain, framework-dependent app. """
    run_command([dotnet, "publish", project,
                 "-c", "Release",
                 "-r", rid,
                 "--self-contained", "false",
                 "-o", output_dir,
                 "-p:PublishAot=false",
                 "-p:PublishTrimmed=false",
                 "-p:PublishSingleFile=false",
                 "-p:PublishReadyToRun=false",
                 "-p:UseSharedCompilation=false"],
                cwd=project.parent, retries=1)


def generate_ilc_rsp(dotnet, project, rid, work_dir):
    """ Run a NativeAOT publish just far enough for the SDK to write the ILC response file.

        `IlcToolsPath` is pointed at a directory that doesn't contain an ILC, so the build stops
        with an error right after `WriteIlcRspFileForCompilation` has produced the response file.
        This avoids both a (redundant) compilation with the SDK's ILC and the native link step,
        which would otherwise require a C++ toolchain to be installed. """
    for stale in project.parent.glob(f"obj/*/*/{rid}/native"):
        shutil.rmtree(stale, ignore_errors=True)

    missing_ilc_dir = work_dir / "no_ilc_here"
    run_command([dotnet, "publish", project,
                 "-c", "Release",
                 "-r", rid,
                 "-p:PublishAot=true",
                 "-p:UseSharedCompilation=false",
                 f"-p:IlcToolsPath={missing_ilc_dir}"],
                cwd=project.parent, retries=0, check=False)

    rsp_files = sorted(project.parent.glob(f"obj/Release/*/{rid}/native/*.ilc.rsp"))
    if len(rsp_files) != 1:
        raise RuntimeError(f"Expected exactly one '*.ilc.rsp' file under {project.parent}, found {len(rsp_files)}")
    print(f"Generated ILC response file: {rsp_files[0]}")
    return rsp_files[0]


def ilc_option_arities(ilc):
    """ Parse `ilc --help` into a map of option name -> whether the option takes a value. The
        response file is written by the .NET SDK the app is built with, which is usually older
        than the ILC in this repo, so we use this to adapt it to the options ILC accepts now. """
    try:
        help_text = subprocess.run([str(ilc), "--help"], capture_output=True, text=True, check=True).stdout
    except Exception as exception:
        print(f"Warning: unable to query ILC for its options ({exception}); using the response file as is.")
        return None

    arities = {}
    for line in help_text.splitlines():
        if not line.startswith("  ") or not line.lstrip().startswith("-"):
            continue
        # Lines look like "  -r, --reference <reference>    Reference file(s) for compilation".
        signature = re.split(r"\s{2,}", line.strip())[0]
        takes_value = signature.endswith(">")
        for name in re.sub(r"\s*<[^>]*>$", "", signature).split(","):
            name = name.strip()
            if name.startswith("-"):
                arities[name] = takes_value
    return arities


def adapt_ilc_rsp_argument(line, arities):
    """ Adapt a single response file argument to the options the local ILC understands. Returns
        None for arguments that have to be dropped. """
    if arities is None or not line.startswith("-"):
        return line

    name, separator, _ = line.partition(":")
    if name not in arities:
        print(f"Warning: dropping '{line}', which the local ILC doesn't understand.")
        return None
    if arities[name] and not separator:
        fixup = ILC_RSP_FIXUPS.get(name)
        if fixup is None:
            print(f"Warning: dropping '{line}', which the local ILC expects to have a value.")
            return None
        return f"{name}:{fixup}"
    if not arities[name] and separator:
        return name
    return line


def rewrite_ilc_rsp(original_rsp, ilc, core_root, aotsdk, output_object, shim_path, parallelism, temp_dir):
    """ Rewrite the SDK-generated ILC response file so that it compiles against the locally built
        framework using the SuperPMI collector shim. Everything else (feature switches, trimming
        roots, direct P/Invokes, ...) is left untouched so that we collect over what a real
        customer build looks like. """
    lines = [line.rstrip("\n") for line in original_rsp.read_text(encoding="utf-8").splitlines() if line.strip()]
    arities = ilc_option_arities(ilc)

    result = []
    for line in lines:
        if line.startswith("-o:"):
            # Redirected below; we don't want to write into the app's obj folder.
            continue
        if line.startswith("-r:"):
            # Drop the references we are going to provide from the locally built framework
            # (the NativeAOT runtime pack of the SDK) and keep the third-party ones. ILC resolves
            # duplicate simple names on a first-one-wins basis, so the surviving references must
            # come before the framework wildcards appended below.
            simple_name = Path(line[len("-r:"):]).stem
            if (aotsdk / f"{simple_name}.dll").exists() or (core_root / f"{simple_name}.dll").exists():
                continue
        line = adapt_ilc_rsp_argument(line, arities)
        if line is not None:
            result.append(line)

    result += [
        f"-o:{output_object}",
        f"-r:{aotsdk / 'System.*.dll'}",
        f"-r:{core_root / 'System.*.dll'}",
        f"-r:{core_root / 'Microsoft.*.dll'}",
        f"-r:{core_root / 'mscorlib.dll'}",
        f"-r:{core_root / 'netstandard.dll'}",
        f"--jitpath:{shim_path}",
        "--codegenopt:EnableExtraSuperPmiQueries=1",
        f"--parallelism:{parallelism}",
    ]

    rsp_path = temp_dir / "sourcegit.ilc.rsp"
    rsp_path.write_text("\n".join(result) + "\n", encoding="utf-8")
    return rsp_path


def collect_nativeaot(core_root, project, rsp_path, jit_path, mc_dir, parallelism, temp_dir):
    """ Replay the ILC response file with the locally built ILC under the SuperPMI collector shim. """
    ilc = core_root / "ilc-published" / native_exe("ilc")
    aotsdk = core_root / "aotsdk"
    if not ilc.exists():
        raise RuntimeError(f"Couldn't find {ilc}. Is the NativeAOT compiler built? (build.cmd clr.aot)")
    if not aotsdk.is_dir():
        raise RuntimeError(f"Couldn't find {aotsdk}. Is the NativeAOT framework built?")

    collection_rsp = rewrite_ilc_rsp(rsp_path, ilc, core_root, aotsdk, temp_dir / "sourcegit.obj",
                                     core_root / native_dll("superpmi-shim-collector"), parallelism, temp_dir)

    # The response file contains paths relative to the project directory, so run ILC from there.
    run_command([ilc, f"@{collection_rsp}"], cwd=project.parent,
                env=collection_env(mc_dir, jit_path), check=False)


def collect_crossgen2(core_root, publish_dir, jit_path, mc_dir, parallelism, temp_dir):
    """ Compile every managed assembly of the published app with the locally built crossgen2
        under the SuperPMI collector shim. """
    crossgen2 = core_root / "crossgen2" / native_exe("crossgen2")
    if not crossgen2.exists():
        raise RuntimeError(f"Couldn't find {crossgen2}. Is crossgen2 built?")

    assemblies = sorted(path for path in publish_dir.glob("*.dll") if is_managed_assembly(path))
    if not assemblies:
        raise RuntimeError(f"No managed assemblies found in {publish_dir}")

    env = collection_env(mc_dir, jit_path)
    for index, assembly in enumerate(assemblies, 1):
        print(f"### [{index}/{len(assemblies)}] crossgen2 {assembly.name} ###")
        rsp_path = temp_dir / f"crossgen2_{assembly.stem}.rsp"
        rsp_path.write_text("\n".join([
            str(assembly),
            f"-o:{temp_dir / (assembly.stem + '.out.dll')}",
            # Reference the locally built framework first; crossgen2 resolves duplicate simple
            # names on a first-one-wins basis, so the app's own copies never shadow it.
            f"-r:{core_root / 'System.*.dll'}",
            f"-r:{core_root / 'Microsoft.*.dll'}",
            f"-r:{core_root / 'mscorlib.dll'}",
            f"-r:{core_root / 'netstandard.dll'}",
            f"-r:{publish_dir / '*.dll'}",
            # The SDK optimizes ReadyToRun images, so do the same here.
            "-O",
            f"--parallelism:{parallelism}",
            f"--jitpath:{core_root / native_dll('superpmi-shim-collector')}",
            "--codegenopt:EnableExtraSuperPmiQueries=1",
        ]) + "\n", encoding="utf-8")
        # Individual assemblies may legitimately fail (e.g. reference assemblies); keep going.
        run_command([crossgen2, f"@{rsp_path}"], env=env, check=False)


def collection_env(mc_dir, jit_path):
    """ Environment that makes the SuperPMI collector shim wrap `jit_path` and record into `mc_dir`. """
    env = os.environ.copy()
    env["SuperPMIShimLogPath"] = str(mc_dir)
    env["SuperPMIShimPath"] = str(jit_path)
    return env


def merge_mc_files(core_root, mc_dir, output_mch):
    """ Merge the collected *.mc files into `output_mch`, removing any context that doesn't
        replay cleanly, and produce the table of contents for it. """
    mcs = core_root / native_exe("mcs")
    superpmi = core_root / native_exe("superpmi")
    jit = core_root / native_dll("clrjit")

    mc_files = list(mc_dir.glob("*.mc"))
    if not mc_files:
        raise RuntimeError(f"No .mc files were produced in {mc_dir}; the collection failed.")
    print(f"Merging {len(mc_files)} .mc files into {output_mch} ...")

    output_mch.parent.mkdir(parents=True, exist_ok=True)
    raw_mch = mc_dir / "raw.mch"
    clean_mch = mc_dir / "clean.mch"
    fail_mcl = mc_dir / "fail.mcl"

    # Concatenate the collected contexts. Note that we deliberately don't deduplicate yet:
    # crossgen2 and ILC ask the JIT to compile methods it rejects outright (e.g. reverse
    # P/Invoke stubs), and the resulting contexts don't hold enough information to be hashed.
    # They are dropped by the replay step below, after which deduplication is safe.
    run_command([mcs, "-merge", "-recursive", raw_mch, mc_dir / "*.mc"])
    for mc_file in mc_files:
        mc_file.unlink(missing_ok=True)

    # Remove the contexts that don't replay cleanly with the JIT they were collected with.
    run_command([superpmi, "-v", "ewmi", "-f", fail_mcl, jit, raw_mch], check=False)
    if fail_mcl.is_file() and fail_mcl.stat().st_size != 0:
        print("Replay had failures, cleaning ...")
        run_command([mcs, "-strip", fail_mcl, raw_mch, clean_mch])
    else:
        print("Replay was clean ...")
        clean_mch = raw_mch

    # Deduplicate and drop the compilation results, as all the other collections do.
    run_command([mcs, "-removeDup", "-thin", clean_mch, output_mch])
    run_command([mcs, "-toc", output_mch])
    print(f"Summary for {output_mch}:")
    run_command([mcs, "-jitflags", output_mch], check=False)

    raw_mch.unlink(missing_ok=True)
    clean_mch.unlink(missing_ok=True)
    fail_mcl.unlink(missing_ok=True)


def output_mch_for(output_mch, collection_type, single_collection):
    """ Compute the .mch file name of a collection. When more than one collection is requested,
        the collection type is inserted into the name so that they don't overwrite each other. """
    if single_collection:
        return output_mch
    return output_mch.with_name(f"{output_mch.stem}.{collection_type}{output_mch.suffix}")


def main():
    parser = argparse.ArgumentParser(description="SuperPMI collection over the SourceGit application.")
    parser.add_argument("--core_root", help="Path to the Core_Root of a Checked build. Defaults to the "
                                            "Core_Root of the enclosing dotnet/runtime checkout.")
    parser.add_argument("--collection_type", default="both", choices=COLLECTION_TYPES + ["both"],
                        help="Which collection(s) to perform. Defaults to both.")
    parser.add_argument("--output_mch", help="Path of the .mch file to produce. When both collections are "
                                             "requested, the collection type is inserted into the file name. "
                                             "Defaults to 'sourcegit.mch' in the current directory.")
    parser.add_argument("--work_dir", help="Directory to clone and build the application in. Defaults to a "
                                           "new directory under the system temp folder.")
    parser.add_argument("--dotnet", help="Path to the 'dotnet' executable to build the application with. "
                                         "By default a suitable SDK is looked up on the PATH and, if none is "
                                         "found, downloaded into the working directory.")
    parser.add_argument("--repo_url", default=DEFAULT_REPO_URL, help="Repository to clone.")
    parser.add_argument("--branch", default=DEFAULT_BRANCH, help="Branch to clone.")
    parser.add_argument("--commit", help="Commit to check out (the branch tip by default).")
    parser.add_argument("--project", default=DEFAULT_PROJECT, help="Project to build, relative to the clone.")
    parser.add_argument("--rid", default=host_rid(), help="Runtime identifier to publish for.")
    parser.add_argument("--parallelism", default=1, type=int,
                        help="Number of threads crossgen2/ILC may use. Collecting in parallel leads to "
                             "sharing violations on the .mc file, so this defaults to 1.")
    parser.add_argument("--sdk_channel", default=DEFAULT_SDK_CHANNEL,
                        help="dotnet-install channel to use when no suitable SDK is found on the machine.")
    parser.add_argument("--skip_cleanup", action="store_true", help="Don't delete the working directory.")
    args = parser.parse_args()

    core_root = Path(args.core_root).expanduser().resolve() if args.core_root else default_core_root()
    if core_root is None:
        print("Error: unable to locate Core_Root; pass --core_root explicitly.", file=sys.stderr)
        return 1
    if not core_root.is_dir():
        print(f"Error: Core_Root {core_root} doesn't exist.", file=sys.stderr)
        return 1

    output_mch = Path(args.output_mch).expanduser().resolve() if args.output_mch \
        else Path.cwd() / "sourcegit.mch"
    collection_types = COLLECTION_TYPES if args.collection_type == "both" else [args.collection_type]

    if args.work_dir:
        work_dir = Path(args.work_dir).expanduser().resolve() / "sourcegit_spmi"
        work_dir.mkdir(parents=True, exist_ok=True)
        remove_work_dir = False
    else:
        work_dir = Path(tempfile.mkdtemp(prefix="sourcegit_spmi_"))
        remove_work_dir = not args.skip_cleanup

    print("Running with the following parameters:")
    print(f"  --core_root:       {core_root}")
    print(f"  --collection_type: {args.collection_type}")
    print(f"  --output_mch:      {output_mch}")
    print(f"  --rid:             {args.rid}")
    print(f"  work directory:    {work_dir}")

    jit_path = core_root / native_dll("clrjit")
    if not (core_root / native_dll("superpmi-shim-collector")).exists():
        print(f"Error: {core_root / native_dll('superpmi-shim-collector')} not found.", file=sys.stderr)
        return 1

    try:
        write_build_isolation_files(work_dir)
        ensure_git(work_dir / "tools")

        dotnet = Path(args.dotnet).expanduser().resolve() if args.dotnet else find_usable_dotnet(DEFAULT_SDK_MIN_MAJOR)
        if dotnet is None:
            dotnet = install_dotnet_sdk(args.sdk_channel, work_dir / "dotnet")
            # Only point DOTNET_ROOT at an installation we laid out ourselves; a `dotnet` found on
            # the PATH may well be a symlink whose parent directory isn't a .NET root.
            os.environ["DOTNET_ROOT"] = str(dotnet.parent)
        print(f"Using dotnet: {dotnet}")

        os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        os.environ["DOTNET_NOLOGO"] = "1"
        # Don't leave MSBuild nodes behind holding on to files in the working directory.
        os.environ["MSBUILDDISABLENODEREUSE"] = "1"

        clone_dir = work_dir / "sourcegit"
        clone_repo(args.repo_url, args.branch, args.commit, clone_dir)
        project = clone_dir / args.project

        # The SuperPMI shim loads the JIT it wraps from SuperPMIShimPath. Use a copy of the JIT so
        # that crossgen2/ILC don't have the same binary loaded twice.
        jit_copy = work_dir / (jit_path.stem + "_superpmi" + jit_path.suffix)
        shutil.copyfile(jit_path, jit_copy)

        for collection_type in collection_types:
            print(f"### Starting the '{collection_type}' collection ###")
            mc_dir = work_dir / f"mc_{collection_type}"
            temp_dir = work_dir / f"temp_{collection_type}"
            shutil.rmtree(mc_dir, ignore_errors=True)
            shutil.rmtree(temp_dir, ignore_errors=True)
            mc_dir.mkdir(parents=True)
            temp_dir.mkdir(parents=True)

            if collection_type == "crossgen2":
                publish_dir = work_dir / "publish"
                shutil.rmtree(publish_dir, ignore_errors=True)
                publish_app(dotnet, project, args.rid, publish_dir)
                collect_crossgen2(core_root, publish_dir, jit_copy, mc_dir, args.parallelism, temp_dir)
            else:
                rsp_path = generate_ilc_rsp(dotnet, project, args.rid, work_dir)
                collect_nativeaot(core_root, project, rsp_path, jit_copy, mc_dir, args.parallelism, temp_dir)

            merge_mc_files(core_root, mc_dir,
                           output_mch_for(output_mch, collection_type, len(collection_types) == 1))
            shutil.rmtree(temp_dir, ignore_errors=True)

        print("Done.")
        return 0
    except Exception as exception:
        print(f"Error: {exception}", file=sys.stderr)
        return 1
    finally:
        if remove_work_dir:
            print(f"Removing {work_dir} ...")
            shutil.rmtree(work_dir, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
