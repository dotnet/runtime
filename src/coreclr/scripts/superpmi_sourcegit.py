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
#   Script to perform SuperPMI collections over SourceGit, a real-world, third-party Avalonia
#   desktop application (https://github.com/sourcegit-scm/sourcegit). Two kinds of collections
#   are produced:
#
#     * crossgen2 - the app is published framework-dependent and every managed assembly in the
#                   publish folder is compiled with the locally built crossgen2.
#     * nativeaot - the app is published with PublishAot=true just far enough to make the SDK
#                   write the `*.ilc.rsp` response file, which is then replayed with the locally
#                   built ILC. This gives real customer ILC settings (feature switches, trimming
#                   roots, direct P/Invokes, ...) over a large app.
#
#   Everything (clone, publish, collect) happens on the machine running this script, so it is
#   self-contained and needs nothing but a network connection and a Core_Root.
#
#   The collection is taken from a fork pinned to DEFAULT_COMMIT rather than from the upstream
#   branch tip, so that the same runtime commit always produces the same collection. To move to
#   a newer version of the app, update DEFAULT_COMMIT (and DEFAULT_SDK_* if the app moves to a
#   newer SDK).
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
import fnmatch
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
from dataclasses import dataclass
from pathlib import Path

# Repository that is cloned and compiled. It is a large, real-world Avalonia application that
# pulls in a lot of third-party code (Avalonia, AvaloniaEdit, Azure.AI.OpenAI, ...).
DEFAULT_REPO_URL = "https://github.com/EgorBo/sourcegit"
DEFAULT_BRANCH = "master"
DEFAULT_COMMIT = "05f0abba9002b72ac0aa103188ea60002cad5b61"
DEFAULT_PROJECT = "src/SourceGit.csproj"

# The .NET SDK used to build the app. `global.json` is deliberately not parsed; these values are
# simply chosen so that the SDK we end up using satisfies the `global.json` of the app above,
# which asks for a released 10.0 or newer SDK. Update them if the app moves on.
DEFAULT_SDK_CHANNEL = "10.0"    # dotnet-install channel used when no suitable SDK is installed
DEFAULT_SDK_MIN_MAJOR = 10      # lowest SDK major version accepted from the PATH

COLLECTION_TYPES = ["crossgen2", "nativeaot"]

# The assemblies the locally built runtime provides, as wildcards over Core_Root and the ILC sdk
# directory. `framework_reference_args` turns these into the `-r:` arguments the compilers get and
# `framework_reference_dir` is its inverse, used to recognize the references we replace. Keep the
# two in sync: an assembly that is dropped but not offered again makes the compilation fail.
FRAMEWORK_REFERENCE_PATTERNS = ["System.*", "Microsoft.*", "mscorlib", "netstandard"]

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


def default_core_root():
    """ Guess the Checked Core_Root of the enclosing dotnet/runtime checkout, if there is one. """
    repo_root = Path(__file__).resolve().parents[3]
    host_os = "windows" if is_windows else ("osx" if sys.platform == "darwin" else "linux")
    core_root = repo_root / "artifacts" / "tests" / "coreclr" / f"{host_os}.{host_arch()}.Checked" / "Tests" / "Core_Root"
    return core_root if core_root.is_dir() else None


def powershell_quote(value):
    """ Quote a value for use inside a single-quoted PowerShell string. """
    return str(value).replace("'", "''")


def download_file(url, destination):
    """ Download `url` into the file `destination`.

        On Windows this goes through PowerShell: the Python on Helix's Windows machines fails to
        verify certificates with urllib's default certificate store, while PowerShell uses the
        (up to date) Windows certificate store. """
    if is_windows:
        script = ("[System.Net.ServicePointManager]::SecurityProtocol=[System.Net.SecurityProtocolType]::Tls12;"
                  f"Invoke-WebRequest -UseBasicParsing -Uri '{powershell_quote(url)}'"
                  f" -OutFile '{powershell_quote(destination)}'")
        run_command(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], retries=3)
    else:
        with urllib.request.urlopen(url, timeout=100) as response:
            destination.write_bytes(response.read())


def http_get(url):
    """ Fetch `url` and return its content as bytes. """
    with tempfile.TemporaryDirectory() as temp_dir:
        destination = Path(temp_dir) / "download"
        download_file(url, destination)
        return destination.read_bytes()


def ensure_git(tools_dir):
    """ Make sure `git` is on the PATH. Helix Windows machines don't have git installed, so
        download a portable one there. """
    if shutil.which("git") is not None:
        return
    if not is_windows:
        raise RuntimeError("git is required but was not found on the PATH")

    print("git was not found, downloading portable git ...")
    assets = json.loads(http_get("https://api.github.com/repos/git-for-windows/git/releases/latest"))["assets"]
    arch_suffix = {"x64": "64-bit", "arm64": "arm64", "x86": "32-bit"}.get(host_arch(), "64-bit")
    name_regex = re.compile(r"^MinGit-.*-(32-bit|64-bit|arm64)\.zip$", re.I)
    try:
        asset = next(a for a in assets if name_regex.match(a["name"]) and arch_suffix in a["name"])
    except StopIteration:
        raise RuntimeError("Unable to find a MinGit asset for " + arch_suffix)

    tools_dir.mkdir(parents=True, exist_ok=True)
    zip_path = tools_dir / asset["name"]
    download_file(asset["browser_download_url"], zip_path)
    git_dir = tools_dir / "git"
    shutil.rmtree(git_dir, ignore_errors=True)
    with zipfile.ZipFile(zip_path) as archive:
        archive.extractall(git_dir)
    zip_path.unlink()
    os.environ["PATH"] = str(git_dir / "cmd") + os.pathsep + os.environ.get("PATH", "")
    if shutil.which("git") is None:
        raise RuntimeError(f"git is still not on the PATH after unpacking {asset['name']}")


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
            # Download from within PowerShell: it uses the Windows certificate store, which is the
            # only one the Helix Windows machines have up to date.
            script_path = Path(temp_dir) / "dotnet-install.ps1"
            script = ("[System.Net.ServicePointManager]::SecurityProtocol=[System.Net.SecurityProtocolType]::Tls12;"
                      f"Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1'"
                      f" -OutFile '{powershell_quote(script_path)}';"
                      f"& '{powershell_quote(script_path)}' -Channel '{powershell_quote(channel)}'"
                      f" -InstallDir '{powershell_quote(install_dir)}' -NoPath")
            run_command(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], retries=3)
        else:
            script_path = Path(temp_dir) / "dotnet-install.sh"
            download_file("https://dot.net/v1/dotnet-install.sh", script_path)
            script_path.chmod(script_path.stat().st_mode | stat.S_IXUSR)
            run_command([script_path, "--channel", channel, "--install-dir", install_dir, "--no-path"], retries=3)

    if not dotnet.exists():
        raise RuntimeError(f"dotnet-install reported success but {dotnet} doesn't exist")
    return dotnet


def write_build_isolation_files(directory):
    """ Drop marker files so that MSBuild/NuGet don't walk up into an enclosing repository
        (e.g. when the working directory lives inside the dotnet/runtime checkout, whose
        Directory.Build.props, global.json and NuGet.config would break the build of the
        cloned app). """
    (directory / "Directory.Build.props").write_text("<Project />\n", encoding="utf-8")
    (directory / "Directory.Build.targets").write_text("<Project />\n", encoding="utf-8")
    (directory / "Directory.Build.rsp").write_text("", encoding="utf-8")
    (directory / "Directory.Packages.props").write_text(
        "<Project><PropertyGroup><ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>"
        "</PropertyGroup></Project>\n", encoding="utf-8")
    (directory / "global.json").write_text("{}\n", encoding="utf-8")
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
    """ Clone the application repository (including its submodules) into `destination`, or bring an
        existing clone to `commit` when the working directory is being reused. """
    if (destination / ".git").is_dir():
        head = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(destination),
                              capture_output=True, text=True).stdout.strip()
        if head == commit:
            print(f"Reusing the existing clone of {commit} in {destination}")
            return
        print(f"The clone in {destination} is at {head or 'an unknown commit'}, re-cloning ...")
    shutil.rmtree(destination, ignore_errors=True)

    # `git clone` of a single commit needs the commit to be fetched explicitly; cloning the branch
    # shallowly and deepening until the commit shows up is simpler and works on any git.
    run_command(["git", "clone", "--quiet", "--branch", branch, url, destination], retries=2)
    run_command(["git", "checkout", "--quiet", commit], cwd=destination)
    run_command(["git", "submodule", "update", "--init", "--recursive", "--depth", "1", "--quiet"], cwd=destination)


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
        # The CLI header is data directory number 14, so there have to be at least 15 of them.
        cli_header = data_directories + 14 * 8
        if len(data) < cli_header + 8:
            return False
        if int.from_bytes(data[data_directories - 4:data_directories], "little") < 15:
            return False
        return int.from_bytes(data[cli_header:cli_header + 4], "little") != 0
    except OSError:
        return False


def framework_reference_dir(simple_name, core_root, aotsdk=None):
    """ Return the directory the locally built runtime provides `simple_name` from, or None when it
        isn't part of the framework. Only names covered by FRAMEWORK_REFERENCE_PATTERNS qualify:
        Core_Root also holds test dependencies (Newtonsoft.Json, xunit, FSharp.Core, ...) that must
        not shadow an application's own copy. """
    if not any(fnmatch.fnmatch(simple_name, pattern) for pattern in FRAMEWORK_REFERENCE_PATTERNS):
        return None
    for directory in ([aotsdk] if aotsdk is not None else []) + [core_root]:
        candidate = directory / f"{simple_name}.dll"
        if candidate.exists() and is_managed_assembly(candidate):
            return directory
    return None


def framework_reference_args(core_root, aotsdk=None):
    """ The `-r:` arguments that make a compilation use the locally built framework. Must stay in
        sync with `framework_reference_dir`. """
    references = [aotsdk / "System.*.dll"] if aotsdk is not None else []
    references += [core_root / "System.*.dll",
                   core_root / "Microsoft.*.dll",
                   core_root / "mscorlib.dll",
                   core_root / "netstandard.dll"]
    return [f"-r:{reference}" for reference in references]


@dataclass
class CollectionContext:
    """ Everything the two collection paths need to record into the same place. """
    core_root: Path
    jit_path: Path      # the copy of the JIT that the collector shim wraps
    mc_dir: Path
    temp_dir: Path
    parallelism: int

    @property
    def shim_path(self):
        return self.core_root / native_dll("superpmi-shim-collector")

    @property
    def aotsdk(self):
        return self.core_root / "aotsdk"

    def collector_args(self, parallelism):
        """ The arguments that make crossgen2/ILC compile through the collector shim. """
        args = [f"--jitpath:{self.shim_path}", "--codegenopt:EnableExtraSuperPmiQueries=1"]
        if parallelism is not None:
            args.append(f"--parallelism:{parallelism}")
        return args

    def write_rsp(self, name, arguments):
        """ Write a response file and echo it, so that a failed CI collection can be diagnosed from
            the log alone (the working directory doesn't survive the run). """
        rsp_path = self.temp_dir / name
        text = "\n".join(str(argument) for argument in arguments) + "\n"
        rsp_path.write_text(text, encoding="utf-8")
        print(f"--- {rsp_path} ---\n{text}--- end of {rsp_path.name} ---")
        return rsp_path

    def run_collector(self, tool, rsp_path, cwd=None):
        """ Run `tool` with `rsp_path`, recording every JIT compilation into `mc_dir`. """
        env = os.environ.copy()
        env["SuperPMIShimLogPath"] = str(self.mc_dir)
        env["SuperPMIShimPath"] = str(self.jit_path)
        return run_command([tool, f"@{rsp_path}"], cwd=cwd, env=env, check=False)


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
        This avoids both a (redundant) compilation with the SDK's ILC and the native link step.
        `IlcUseEnvironmentalTools` skips the search for a C++ toolchain that the SDK otherwise
        performs (and errors out on) before it gets that far; we never link, so we don't need one. """
    native_dirs = list(project.parent.glob(f"obj/*/*/{rid}/native"))
    for stale in native_dirs:
        shutil.rmtree(stale)

    missing_ilc_dir = work_dir / "no_ilc_here"
    run_command([dotnet, "publish", project,
                 "-c", "Release",
                 "-r", rid,
                 "-p:PublishAot=true",
                 "-p:UseSharedCompilation=false",
                 "-p:IlcUseEnvironmentalTools=true",
                 f"-p:IlcToolsPath={missing_ilc_dir}"],
                cwd=project.parent, retries=0, check=False)

    rsp_files = sorted(project.parent.glob(f"obj/Release/*/{rid}/native/*.ilc.rsp"))
    if len(rsp_files) != 1:
        raise RuntimeError(f"Expected exactly one '*.ilc.rsp' file under {project.parent}, found {len(rsp_files)}. "
                           f"The publish above must have failed before the response file was written.")
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


def rewrite_ilc_rsp(context, ilc, original_rsp, root_all):
    """ Rewrite the SDK-generated ILC response file so that it compiles against the locally built
        framework using the SuperPMI collector shim. Everything else (feature switches, trimming
        roots, direct P/Invokes, ...) is left untouched so that we collect over what a real
        customer build looks like. """
    lines = [line.rstrip("\n") for line in original_rsp.read_text(encoding="utf-8").splitlines() if line.strip()]
    arities = ilc_option_arities(ilc)

    result = []
    app_assemblies = []
    for line in lines:
        if line.startswith("-o:"):
            # Redirected below; we don't want to write into the app's obj folder.
            continue
        if line.startswith("-r:"):
            # Drop the references the locally built framework provides instead and keep the
            # application's own ones. ILC resolves duplicate simple names on a first-one-wins
            # basis, so the surviving references must come before the framework wildcards.
            reference = Path(line[len("-r:"):])
            if framework_reference_dir(reference.stem, context.core_root, context.aotsdk) is not None:
                continue
            # The SDK also lists native libraries here; they can be referenced but not rooted.
            if is_managed_assembly(reference):
                app_assemblies.append(reference.stem)
        line = adapt_ilc_rsp_argument(line, arities)
        if line is not None:
            result.append(line)

    if root_all:
        # Compile everything reachable from any public method of the app's own assemblies instead
        # of only what its entry point needs. This is not what a real publish does, but it makes
        # for a considerably larger collection. Framework assemblies are deliberately not rooted:
        # they are already covered by the libraries collections, and rooting them makes ILC's
        # dependency analysis take hours.
        already_rooted = {line[len("--root:"):] for line in result if line.startswith("--root:")}
        result += [f"--root:{name}" for name in dict.fromkeys(app_assemblies) if name not in already_rooted]

    result += [f"-o:{context.temp_dir / 'sourcegit.obj'}"]
    result += framework_reference_args(context.core_root, context.aotsdk)
    result += context.collector_args(context.parallelism)
    return context.write_rsp("sourcegit.ilc.rsp", result)


def collect_nativeaot(context, project, rsp_path, root_all):
    """ Replay the ILC response file with the locally built ILC under the SuperPMI collector shim. """
    ilc = context.core_root / "ilc-published" / native_exe("ilc")
    if not ilc.exists():
        raise RuntimeError(f"Couldn't find {ilc}. Is the NativeAOT compiler built? (build.cmd clr.aot)")
    if not context.aotsdk.is_dir():
        raise RuntimeError(f"Couldn't find {context.aotsdk}. Is the NativeAOT framework built?")

    collection_rsp = rewrite_ilc_rsp(context, ilc, rsp_path, root_all)

    # This is a single compilation of the whole app: if it fails we have, at best, a truncated
    # collection, so don't let it pass for a successful one. The response file contains paths
    # relative to the project directory, so run ILC from there.
    if not context.run_collector(ilc, collection_rsp, cwd=project.parent):
        raise RuntimeError(f"ILC failed; see the response file logged above ({collection_rsp}).")


def collect_crossgen2(context, publish_dir, entry_assembly):
    """ Compile every managed assembly of the published app with the locally built crossgen2
        under the SuperPMI collector shim. """
    crossgen2 = context.core_root / "crossgen2" / native_exe("crossgen2")
    if not crossgen2.exists():
        raise RuntimeError(f"Couldn't find {crossgen2}. Is crossgen2 built?")

    assemblies = sorted(path for path in publish_dir.glob("*.dll") if is_managed_assembly(path))
    if not assemblies:
        raise RuntimeError(f"No managed assemblies found in {publish_dir}")

    failed = []
    for index, assembly in enumerate(assemblies, 1):
        print(f"### [{index}/{len(assemblies)}] crossgen2 {assembly.name} ###")
        arguments = [assembly, f"-o:{context.temp_dir / (assembly.stem + '.out.dll')}"]
        arguments += framework_reference_args(context.core_root)
        # The application's own assemblies come after the framework so that its copies of, say,
        # Microsoft.Extensions.*, never shadow the runtime we are collecting for.
        arguments += [f"-r:{publish_dir / '*.dll'}"]
        # The SDK optimizes ReadyToRun images, so do the same here. crossgen2 compiles in parallel
        # by default, which leads to sharing violations on the .mc file the collector shim writes.
        arguments += ["-O"] + context.collector_args(1)
        rsp_path = context.write_rsp(f"crossgen2_{assembly.stem}.rsp", arguments)
        if not context.run_collector(crossgen2, rsp_path):
            failed.append(assembly.name)

    if failed:
        print(f"crossgen2 failed for {len(failed)}/{len(assemblies)} assemblies: {', '.join(failed)}")
    # Individual assemblies may legitimately fail, but a wholesale failure (or losing the app
    # itself) means the collection isn't worth keeping.
    if entry_assembly in failed:
        raise RuntimeError(f"crossgen2 failed for the application assembly {entry_assembly}.")
    if len(failed) * 2 > len(assemblies):
        raise RuntimeError(f"crossgen2 failed for {len(failed)} of {len(assemblies)} assemblies.")


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

    # Remove the contexts that don't replay cleanly with the JIT they were collected with. An
    # empty failure list together with a failed run means something worse than a few bad contexts
    # (a mismatched or broken JIT, say), which would otherwise look like a perfectly clean replay.
    replayed = run_command([superpmi, "-v", "ewmi", "-f", fail_mcl, jit, raw_mch], check=False)
    failures = sum(1 for _ in fail_mcl.open()) if fail_mcl.is_file() else 0
    if not replayed and failures == 0:
        raise RuntimeError("superpmi replay failed without producing a list of failing method "
                           "contexts; the JIT or the collection is bad.")
    if failures != 0:
        print(f"Replay failed for {failures} method contexts, removing them ...")
        run_command([mcs, "-strip", "-thin", fail_mcl, raw_mch, clean_mch])
        raw_mch.unlink(missing_ok=True)
    else:
        print("Replay was clean ...")
        clean_mch = raw_mch

    # Deduplicate and drop the compilation results, as all the other collections do.
    run_command([mcs, "-removeDup", "-thin", clean_mch, output_mch])
    if not output_mch.is_file() or output_mch.stat().st_size == 0:
        raise RuntimeError(f"{output_mch} wasn't produced or is empty; every method context must "
                           f"have failed to replay.")
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
    parser.add_argument("--branch", default=DEFAULT_BRANCH, help="Branch the commit to check out lives on.")
    parser.add_argument("--commit", default=DEFAULT_COMMIT, help="Commit to check out.")
    parser.add_argument("--project", default=DEFAULT_PROJECT, help="Project to build, relative to the clone.")
    parser.add_argument("--rid", default=host_rid(), help="Runtime identifier to publish for. Only the RID of "
                                                          "the machine is supported: the collection uses the "
                                                          "host's JIT and compilers.")
    parser.add_argument("--parallelism", type=int,
                        help="Number of threads ILC may use; it uses its own default when this isn't "
                             "specified. crossgen2 always collects single-threaded, as collecting in "
                             "parallel leads to sharing violations on the .mc file.")
    parser.add_argument("--sdk_channel", default=DEFAULT_SDK_CHANNEL,
                        help="dotnet-install channel to use when no suitable SDK is found on the machine.")
    parser.add_argument("--no_root_all_assemblies", action="store_true",
                        help="For the nativeaot collection, only collect over what the app's entry point "
                             "reaches. By default every one of the app's own assemblies is rooted, which "
                             "roughly doubles the size of the collection.")
    parser.add_argument("--skip_cleanup", action="store_true", help="Don't delete the working directory.")
    args = parser.parse_args()

    core_root = Path(args.core_root).expanduser().resolve() if args.core_root else default_core_root()
    if core_root is None:
        print("Error: unable to locate Core_Root; pass --core_root explicitly.", file=sys.stderr)
        return 1
    if not core_root.is_dir():
        print(f"Error: Core_Root {core_root} doesn't exist.", file=sys.stderr)
        return 1
    shim_path = core_root / native_dll("superpmi-shim-collector")
    if not shim_path.exists():
        print(f"Error: {shim_path} not found.", file=sys.stderr)
        return 1
    if args.rid != host_rid():
        print(f"Error: --rid {args.rid} isn't the RID of this machine ({host_rid()}); the collection runs "
              f"the host's crossgen2/ILC and JIT, so it can't cross-target.", file=sys.stderr)
        return 1
    if args.parallelism is not None and args.parallelism < 1:
        print("Error: --parallelism must be at least 1.", file=sys.stderr)
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
    print(f"  --repo_url:        {args.repo_url}")
    print(f"  --commit:          {args.commit}")
    print(f"  --rid:             {args.rid}")
    print(f"  work directory:    {work_dir}")

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
        jit_name = Path(native_dll("clrjit"))
        jit_copy = work_dir / f"{jit_name.stem}_superpmi{jit_name.suffix}"
        shutil.copyfile(core_root / jit_name, jit_copy)

        for collection_type in collection_types:
            print(f"### Starting the '{collection_type}' collection ###")
            mc_dir = work_dir / f"mc_{collection_type}"
            temp_dir = work_dir / f"temp_{collection_type}"
            shutil.rmtree(mc_dir, ignore_errors=True)
            shutil.rmtree(temp_dir, ignore_errors=True)
            mc_dir.mkdir(parents=True)
            temp_dir.mkdir(parents=True)
            context = CollectionContext(core_root, jit_copy, mc_dir, temp_dir, args.parallelism)

            if collection_type == "crossgen2":
                publish_dir = work_dir / "publish"
                shutil.rmtree(publish_dir, ignore_errors=True)
                publish_app(dotnet, project, args.rid, publish_dir)
                collect_crossgen2(context, publish_dir, f"{project.stem}.dll")
            else:
                rsp_path = generate_ilc_rsp(dotnet, project, args.rid, work_dir)
                collect_nativeaot(context, project, rsp_path, not args.no_root_all_assemblies)

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
