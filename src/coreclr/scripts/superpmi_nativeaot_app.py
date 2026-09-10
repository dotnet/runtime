#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
#
# Title: superpmi_nativeaot_app.py
#
# Notes:
#
#   NativeAOT SuperPMI collection over a real-world, third-party application. It defaults to the
#   Aspire CLI (https://github.com/microsoft/aspire), but most repositories holding an executable
#   project work, see --repo. The project doesn't have to opt into AOT itself: PublishAot is
#   turned on from the command line.
#
#   The app is published with the SDK pointed at the locally built framework (IlcFrameworkPath,
#   IlcSdkPath, ...), so that the `*.ilc.rsp` response file the SDK writes describes a compilation
#   against the runtime we want to collect over. That response file is then replayed with the
#   locally built ILC under the SuperPMI collector shim.
#
#   Replaying it, rather than letting the SDK run the compilation, keeps the collection from
#   needing the C++ toolchain the SDK would go on to link the result with: `IlcToolsPath` points
#   at a directory holding no ILC, so the build stops right after the response file is written,
#   and `IlcUseEnvironmentalTools` skips the toolchain search that happens before that.
#
#   Everything (clone, build, collect) happens on the machine running this script, so it needs
#   nothing but a Core_Root, a network connection and, outside Windows, a `git` on the PATH.
#
#   The app is pinned to DEFAULT_COMMIT so that it can't change under the collection; note that
#   the SDK it is built with is a channel, so it does move. Update DEFAULT_COMMIT (and
#   DEFAULT_SDK_CHANNEL if the app moves to a newer SDK) to pick up a newer version of the app.
#
# Usage examples:
#
#   # Collect using the locally built Checked Core_Root:
#   python superpmi_nativeaot_app.py
#
#   # Collect with an explicit Core_Root and output file:
#   python superpmi_nativeaot_app.py --core_root C:\runtime\artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root ^
#                                    --output_mch aspire.nativeaot.mch
#
#   # Collect over a different application:
#   python superpmi_nativeaot_app.py --repo Tyrrrz/DiscordChatExporter --branch prime --commit <sha> ^
#                                    --project DiscordChatExporter.Cli/DiscordChatExporter.Cli.csproj ^
#                                    --sdk_channel 10.0
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
import traceback
import urllib.request
import zipfile
from pathlib import Path

# The Aspire CLI: a real-world, NativeAOT-published .NET application with a large dependency
# closure. Pinned to a commit so that the app can't change under the collection.
DEFAULT_REPO = "microsoft/aspire"
DEFAULT_BRANCH = "main"
DEFAULT_COMMIT = "0b39431e7411552a856147c3bed0803ea69523f1"
DEFAULT_PROJECT = "src/Aspire.Cli/Aspire.Cli.csproj"

# `global.json` is deliberately not parsed; this just has to be an SDK that satisfies it.
DEFAULT_SDK_CHANNEL = "11.0"

# The framework, as wildcards over Core_Root. Core_Root can't be handed to the SDK as is: it also
# holds test dependencies (xunit, Newtonsoft.Json, ...) and the compilers themselves.
FRAMEWORK_ASSEMBLY_PATTERNS = ["System.*.dll", "Microsoft.*.dll", "mscorlib.dll", "netstandard.dll"]

# Compatibility with response files written by older SDKs: options they spell as a plain flag but
# the ILC built from this repo expects to have a value. Maps such an option to the value to use.
ILC_RSP_FIXUPS = {
    # Used to mean "emit stack trace metadata"; now takes one of 'frames', 'lines' or 'none'.
    "--stacktracedata": "frames",
}

is_windows = sys.platform.startswith("win")


def native_dll(name):
    """ "clrjit" -> "clrjit.dll" on Windows, "libclrjit.so" on Linux. """
    ext = ".dll" if is_windows else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if is_windows else "lib"
    return f"{prefix}{name}{ext}"


def native_exe(name):
    """ "ilc" -> "ilc.exe" on Windows. """
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
    """ The architecture of the machine, in the naming used by the runtime repo. """
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
    """ The .NET RID of the machine, e.g. "win-x64". """
    os_part = "win" if is_windows else ("osx" if sys.platform == "darwin" else "linux")
    return f"{os_part}-{host_arch()}"


def default_core_root():
    """ The Checked Core_Root of the enclosing dotnet/runtime checkout, if there is one. """
    repo_root = Path(__file__).resolve().parents[3]
    host_os = "windows" if is_windows else ("osx" if sys.platform == "darwin" else "linux")
    core_root = repo_root / "artifacts" / "tests" / "coreclr" / f"{host_os}.{host_arch()}.Checked" / "Tests" / "Core_Root"
    return core_root if core_root.is_dir() else None


def msbuild_directory(path):
    """ A directory for an MSBuild property that wants a trailing separator. Forward slashes are
        used everywhere: a trailing backslash would escape the quote around the argument. """
    return str(path).replace(os.sep, "/") + "/"


def powershell_quote(value):
    """ Quote a value for use inside a single-quoted PowerShell string. """
    return str(value).replace("'", "''")


def download_file(url, destination):
    """ Download `url` into `destination`. Windows goes through PowerShell: the Python on Helix's
        Windows machines can't verify certificates, while PowerShell uses the Windows store. """
    if is_windows:
        script = ("[System.Net.ServicePointManager]::SecurityProtocol=[System.Net.SecurityProtocolType]::Tls12;"
                  f"Invoke-WebRequest -UseBasicParsing -Uri '{powershell_quote(url)}'"
                  f" -OutFile '{powershell_quote(destination)}'")
        run_command(["powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-Command", script], retries=3)
    else:
        with urllib.request.urlopen(url, timeout=100) as response:
            destination.write_bytes(response.read())


def http_get(url):
    """ The content of `url`, as bytes. """
    with tempfile.TemporaryDirectory() as temp_dir:
        destination = Path(temp_dir) / "download"
        download_file(url, destination)
        return destination.read_bytes()


def ensure_git(tools_dir):
    """ Put `git` on the PATH; Helix Windows machines don't have it installed. """
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


def install_dotnet_sdk(channel, install_dir):
    """ Install an SDK into `install_dir`. Whatever SDK the machine happens to have is deliberately
        not used: it varies from machine to machine, and may not satisfy the app's `global.json`. """
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


def clone_url(repo):
    """ Accepts both a GitHub "owner/name" shorthand and a full clone URL. """
    return repo if "://" in repo or repo.startswith("git@") else f"https://github.com/{repo}"


def repo_name(repo):
    """ The name of the repository, used to name the clone directory. """
    name = repo.rstrip("/").rsplit("/", 1)[-1]
    return name[:-len(".git")] if name.endswith(".git") else name


def clone_repo(url, branch, commit, destination):
    """ Clone the app (with its submodules) at `commit`, reusing an existing clone if it matches. """
    head = None
    if (destination / ".git").is_dir():
        head = subprocess.run(["git", "rev-parse", "HEAD"], cwd=str(destination),
                              capture_output=True, text=True).stdout.strip()
    if head == commit:
        print(f"Reusing the existing clone of {commit} in {destination}")
    else:
        if head is not None:
            print(f"The clone in {destination} is at {head or 'an unknown commit'}, re-cloning ...")
        shutil.rmtree(destination, ignore_errors=True)
        # Cloning a single commit needs it to be fetched explicitly; cloning the branch works anywhere.
        run_command(["git", "clone", "--quiet", "--branch", branch, url, destination], retries=2)
        run_command(["git", "checkout", "--quiet", commit], cwd=destination)

    # Also on a reused clone: a previous run may have been interrupted half way through this.
    run_command(["git", "submodule", "update", "--init", "--recursive", "--depth", "1", "--quiet"], cwd=destination)


def create_framework_dir(core_root, destination):
    """ The framework to compile against, for the SDK to reference through `IlcFrameworkPath`. The
        assemblies NativeAOT implements itself are left out: Core_Root holds them in their CoreCLR
        flavour, and the SDK takes the NativeAOT one from `aotsdk` (`IlcSdkPath`) instead. """
    private_assemblies = {path.name for path in (core_root / "aotsdk").glob("*.dll")}
    shutil.rmtree(destination, ignore_errors=True)
    destination.mkdir(parents=True)
    for pattern in FRAMEWORK_ASSEMBLY_PATTERNS:
        for assembly in core_root.glob(pattern):
            if assembly.name not in private_assemblies:
                shutil.copyfile(assembly, destination / assembly.name)
    print(f"Prepared {len(list(destination.glob('*.dll')))} framework assemblies in {destination}")
    return destination


def generate_ilc_rsp(dotnet, project, clone_dir, rid, work_dir, core_root, framework_dir):
    """ Publish just far enough for the SDK to write the ILC response file; see the notes on top. """
    # Where the response file lands is up to the repository (Arcade, for one, redirects the
    # intermediate output out of the project directory), so look for it afterwards instead. Clear
    # out any that a previous run left behind, so that only the one we want can be found.
    for stale in clone_dir.rglob("*.ilc.rsp"):
        stale.unlink()

    aotsdk = msbuild_directory(core_root / "aotsdk")
    run_command([dotnet, "publish", project,
                 "-c", "Release",
                 "-r", rid,
                 "-p:PublishAot=true",
                 "-p:UseSharedCompilation=false",
                 "-p:IlcUseEnvironmentalTools=true",
                 f"-p:IlcToolsPath={msbuild_directory(work_dir / 'no_ilc_here')}",
                 f"-p:IlcSdkPath={aotsdk}",
                 f"-p:IlcFrameworkNativePath={aotsdk}",
                 f"-p:IlcFrameworkPath={msbuild_directory(framework_dir)}"],
                cwd=project.parent, check=False)

    rsp_files = sorted(clone_dir.rglob("*.ilc.rsp"))
    if len(rsp_files) == 0:
        raise RuntimeError(f"No '*.ilc.rsp' file was written under {clone_dir}; the publish above must have "
                           f"failed before it got that far.")
    if len(rsp_files) > 1:
        raise RuntimeError(f"Expected the publish to write a single '*.ilc.rsp' file, but it wrote "
                           f"{len(rsp_files)}: {', '.join(str(rsp) for rsp in rsp_files)}. Only projects "
                           f"that compile to a single native image are supported.")
    print(f"Generated ILC response file: {rsp_files[0]}")
    return rsp_files[0]


def rewrite_ilc_rsp(original_rsp, destination, core_root, parallelism):
    """ The response file the collection runs with: the SDK's, plus the arguments that make ILC
        compile through the collector shim. Everything else is left as a real build has it. """
    lines = [line for line in original_rsp.read_text(encoding="utf-8").splitlines() if line.strip()]
    lines = [f"{line}:{ILC_RSP_FIXUPS[line]}" if line in ILC_RSP_FIXUPS else line for line in lines]
    if parallelism is not None:
        # IlcSingleThreaded makes the SDK write one of its own, and ILC only accepts a single value.
        lines = [line for line in lines if not line.startswith("--parallelism:")]

    lines.append(f"--jitpath:{core_root / native_dll('superpmi-shim-collector')}")
    lines.append("--codegenopt:EnableExtraSuperPmiQueries=1")
    if parallelism is not None:
        lines.append(f"--parallelism:{parallelism}")

    # Echo it: the working directory doesn't survive a CI run, the log does.
    text = "\n".join(lines) + "\n"
    destination.write_text(text, encoding="utf-8")
    print(f"--- {destination} ---\n{text}--- end of {destination.name} ---")
    return destination


def collect(core_root, project, rsp_path, mc_dir, jit_path):
    """ Replay the response file with the locally built ILC under the collector shim. """
    ilc = core_root / "ilc-published" / native_exe("ilc")
    if not ilc.exists():
        raise RuntimeError(f"Couldn't find {ilc}. Is the NativeAOT compiler built? (build.cmd clr.aot)")

    env = os.environ.copy()
    env["SuperPMIShimLogPath"] = str(mc_dir)
    env["SuperPMIShimPath"] = str(jit_path)

    # A failed compilation leaves a truncated collection behind, so don't let it pass for a good
    # one. The response file may hold paths relative to the project directory, which is where the
    # publish that wrote it ran, so run ILC from there too.
    if not run_command([ilc, f"@{rsp_path}"], cwd=project.parent, env=env, check=False):
        raise RuntimeError(f"ILC failed; see the response file logged above ({rsp_path}).")


def merge_mc_files(core_root, mc_dir, output_mch):
    """ Merge the collected *.mc files into `output_mch`, dropping contexts that don't replay. """
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

    # Concatenate, without deduplicating yet: ILC asks the JIT to compile methods it rejects
    # outright (e.g. reverse P/Invoke stubs), and those contexts can't be hashed. The replay below
    # drops them, after which deduplication is safe.
    run_command([mcs, "-merge", "-recursive", raw_mch, mc_dir / "*.mc"])
    for mc_file in mc_files:
        mc_file.unlink(missing_ok=True)

    # Drop the contexts that don't replay with the JIT they were collected with. A failed run with
    # an empty failure list is something worse (a mismatched JIT, say) than a few bad contexts, and
    # would otherwise look like a perfectly clean replay.
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


def main():
    parser = argparse.ArgumentParser(description="NativeAOT SuperPMI collection over a real-world application.")
    parser.add_argument("--core_root", help="Path to the Core_Root of a Checked build. Defaults to the "
                                            "Core_Root of the enclosing dotnet/runtime checkout.")
    parser.add_argument("--output_mch", help="Path of the .mch file to produce. Defaults to a file named "
                                             "after the project in the current directory.")
    parser.add_argument("--work_dir", help="Directory to create the working directory of the collection in. "
                                           "Defaults to the system temp folder, in which case the working "
                                           "directory is removed again unless --skip_cleanup is passed.")
    parser.add_argument("--dotnet", help="Path to the 'dotnet' executable to build the application with. "
                                         "By default an SDK is downloaded into the working directory.")
    parser.add_argument("--repo", default=DEFAULT_REPO, help="Application to collect over, either as a GitHub "
                                                             "'owner/name' or as a full clone URL.")
    parser.add_argument("--branch", default=DEFAULT_BRANCH, help="Branch the commit to check out lives on.")
    parser.add_argument("--commit", default=DEFAULT_COMMIT, help="Commit to check out.")
    parser.add_argument("--project", default=DEFAULT_PROJECT, help="Project to build, relative to the clone.")
    parser.add_argument("--rid", default=host_rid(), help="Runtime identifier to publish for. Only the RID of "
                                                          "the machine is supported: the collection uses the "
                                                          "host's ILC and JIT.")
    parser.add_argument("--parallelism", type=int,
                        help="Number of threads ILC may use; it uses its own default when this isn't specified.")
    parser.add_argument("--sdk_channel", default=DEFAULT_SDK_CHANNEL,
                        help="dotnet-install channel to get the SDK to build the application with from.")
    parser.add_argument("--skip_cleanup", action="store_true",
                        help="Don't delete the working directory created under the system temp folder.")
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
    if not (core_root / "aotsdk").is_dir():
        print(f"Error: {core_root / 'aotsdk'} not found. Is the NativeAOT framework built?", file=sys.stderr)
        return 1
    if args.rid != host_rid():
        print(f"Error: --rid {args.rid} isn't the RID of this machine ({host_rid()}); the collection runs "
              f"the host's ILC and JIT, so it can't cross-target.", file=sys.stderr)
        return 1
    if args.parallelism is not None and args.parallelism < 1:
        print("Error: --parallelism must be at least 1.", file=sys.stderr)
        return 1

    # The collection is named after the project, not the repository: a repository may well hold
    # more than one application worth collecting over.
    name = Path(args.project).stem
    output_mch = Path(args.output_mch).expanduser().resolve() if args.output_mch \
        else Path.cwd() / f"{name}.mch"

    if args.work_dir:
        work_dir = Path(args.work_dir).expanduser().resolve() / f"{name}_spmi"
        work_dir.mkdir(parents=True, exist_ok=True)
        remove_work_dir = False
    else:
        work_dir = Path(tempfile.mkdtemp(prefix=f"{name}_spmi_"))
        remove_work_dir = not args.skip_cleanup

    print("Running with the following parameters:")
    print(f"  --core_root:    {core_root}")
    print(f"  --output_mch:   {output_mch}")
    print(f"  --repo:         {args.repo}")
    print(f"  --commit:       {args.commit}")
    print(f"  --project:      {args.project}")
    print(f"  --rid:          {args.rid}")
    print(f"  work directory: {work_dir}")

    try:
        ensure_git(work_dir / "tools")

        if args.dotnet:
            dotnet = Path(args.dotnet).expanduser().resolve()
        else:
            dotnet = install_dotnet_sdk(args.sdk_channel, work_dir / f"dotnet-{args.sdk_channel}")
            os.environ["DOTNET_ROOT"] = str(dotnet.parent)
        print(f"Using dotnet: {dotnet}")

        os.environ["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1"
        os.environ["DOTNET_NOLOGO"] = "1"
        # Don't leave MSBuild nodes behind holding on to files in the working directory.
        os.environ["MSBUILDDISABLENODEREUSE"] = "1"

        clone_dir = work_dir / repo_name(args.repo)
        clone_repo(clone_url(args.repo), args.branch, args.commit, clone_dir)
        project = clone_dir / args.project
        if not project.is_file():
            raise RuntimeError(f"{project} doesn't exist; pass --project with a path relative to the clone.")

        # The SuperPMI shim loads the JIT it wraps from SuperPMIShimPath. Use a copy of the JIT so
        # that ILC doesn't have the same binary loaded twice.
        jit_name = Path(native_dll("clrjit"))
        jit_copy = work_dir / f"{jit_name.stem}_superpmi{jit_name.suffix}"
        shutil.copyfile(core_root / jit_name, jit_copy)

        mc_dir = work_dir / "mc"
        shutil.rmtree(mc_dir, ignore_errors=True)
        mc_dir.mkdir(parents=True)

        framework_dir = create_framework_dir(core_root, work_dir / "framework")
        rsp_path = generate_ilc_rsp(dotnet, project, clone_dir, args.rid, work_dir, core_root, framework_dir)
        collection_rsp = rewrite_ilc_rsp(rsp_path, work_dir / f"{name}.ilc.rsp", core_root, args.parallelism)
        collect(core_root, project, collection_rsp, mc_dir, jit_copy)
        merge_mc_files(core_root, mc_dir, output_mch)

        print("Done.")
        return 0
    except Exception as exception:
        # Anything but a RuntimeError is a bug in this script rather than a failed collection, and
        # the message alone doesn't say where it came from.
        if not isinstance(exception, RuntimeError):
            traceback.print_exc()
        print(f"Error: {exception}", file=sys.stderr)
        return 1
    finally:
        if remove_work_dir:
            print(f"Removing {work_dir} ...")
            shutil.rmtree(work_dir, ignore_errors=True)


if __name__ == "__main__":
    sys.exit(main())
