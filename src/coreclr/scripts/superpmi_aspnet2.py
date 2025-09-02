#!/usr/bin/env python3
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
#
#
# Title: superpmi_aspnet2.py
#
# Notes:
#
#   Script to perform the superpmi collection for Techempower Benchmarks
#   via "crank" (https://github.com/dotnet/crank).
#
# Usage example:
#
#   python superpmi_aspnet2.py --core_root C:\runtime\artifacts\bin\coreclr\windows.x64.Checked --output_mch aspnet2.mch
#

import argparse
import os
import re
import requests
import pathlib
import subprocess
import sys
import textwrap
import time
import zipfile
import shutil
import platform
import tempfile
import urllib.request
from pathlib import Path


CRANK_PORT = 5010

# Global variable to track the agent process for cleanup
_crank_agent_process = None


# Convert a filename to the appropriate native DLL name, e.g. "clrjit" -> "libclrjit.so" (on Linux)
def native_dll(name: str) -> str:
    ext = ".dll" if sys.platform.startswith("win") else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if sys.platform.startswith("win") else "lib"
    return f"{prefix}{name}{ext}"


# Same for executables
def native_exe(name: str) -> str:
    ext = ".exe" if sys.platform.startswith("win") else ""
    return f"{name}{ext}"


# Run a command with retries
def run_command(cmd, retries=1):
    print(f"Running command: {' '.join(map(str, cmd))}")
    attempt = 0
    while True:
        try:
            subprocess.run(cmd, check=True)
            return True
        except subprocess.CalledProcessError as e:
            print(f"Command failed with return code {e.returncode}")
        except Exception as e:
            print(f"Failed to start command: {e}")
        attempt += 1
        if attempt > retries:
            print(f"command failed after {retries} attempts")
            break
        time.sleep(3)
    return False


# Temp workaround, will be removed once https://github.com/dotnet/crank/pull/841 lands
# Our Windows Helix machines don't have git installed (and no winget) while crank relies on
# it internally to download benchmarks.
def ensure_git(dest: Path) -> str:
    existing = shutil.which("git")
    if existing:
        print("git found")
        return
    if sys.platform == "win32":
        print("git not found, downloading portable git...")
        m = {"x64": "64-bit", "arm64": "arm64", "x86": "32-bit"}
        assets = requests.get(
            "https://api.github.com/repos/git-for-windows/git/releases/latest", timeout=100
        ).json()["assets"]
        rx = re.compile(r"^MinGit-.*-(32-bit|64-bit|arm64)\.zip$", re.I)
        arch = "x64"
        mach = platform.machine().lower()
        if "arm64" in mach:
            arch = "arm64"
        elif mach in ("x86", "i386", "i686"):
            arch = "x86"
        try:
            asset = next(a for a in assets if rx.match(a["name"]) and m[arch] in a["name"]) 
        except StopIteration:
            raise RuntimeError(
                f"Unable to find MinGit asset for arch '{arch}'. Available assets: "
                f"{[a['name'] for a in assets if 'MinGit' in a['name']]}"
            )
        dest_str = str(dest)
        os.makedirs(dest_str, exist_ok=True)
        zip_path = os.path.join(dest_str, asset["name"]) 
        with requests.get(asset["browser_download_url"], stream=True) as r, open(zip_path, "wb") as f:
            for c in r.iter_content(8192):
                f.write(c)
        git_dir = os.path.join(dest_str, "git")
        shutil.rmtree(git_dir, ignore_errors=True)
        with zipfile.ZipFile(zip_path) as z:
            z.extractall(git_dir)
        os.remove(zip_path)
        cmd_path = os.path.join(git_dir, "cmd")
        os.environ["PATH"] = cmd_path + os.pathsep + os.environ.get("PATH", "")
        return


# Install .NET SDK using the official dotnet-install script.
def install_dotnet_sdk(channel: str, install_dir: Path) -> None:
    install_dir.mkdir(parents=True, exist_ok=True)
    if sys.platform == "win32":
        ch = channel.replace("'", "''")
        di = str(install_dir).replace("'", "''")
        ps_script = (
            "[System.Net.ServicePointManager]::SecurityProtocol=[System.Net.SecurityProtocolType]::Tls12;"
            "Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'dotnet-install.ps1';"
            f"$DotnetVersion='{ch}';$InstallDir='{di}';"
            "& './dotnet-install.ps1' -Channel $DotnetVersion -InstallDir $InstallDir -NoPath"
        )
        run_command(["powershell.exe","-NoProfile","-ExecutionPolicy", "Bypass","-Command", ps_script], retries=3)
    else:
        with tempfile.TemporaryDirectory() as td:
            script_path = Path(td) / "dotnet-install.sh"
            with urllib.request.urlopen("https://dot.net/v1/dotnet-install.sh") as resp, open(script_path, "wb") as f:
                f.write(resp.read())
            os.chmod(script_path, 0o755)
            run_command([str(script_path),"--channel", channel,"--install-dir", str(install_dir),"--no-path"], retries=3)


# Prepare the environment and run crank-agent
def setup_and_run_crank_agent(workdir: Path):
    logs_dir = workdir / "logs"
    build_dir = workdir / "build"
    tools_dir = workdir / "dotnet_tools"
    dotnethome_dir = workdir / "dotnet_home"
    localhost_yml = workdir / "localhost.yml"
    ensure_git(tools_dir)

    dotnet_root_dir = dotnethome_dir
    os.environ['DOTNET_ROOT'] = str(dotnet_root_dir)
    os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    os.environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    os.environ['UseSharedCompilation'] = 'false'

    print("Installing tools ...")
    logs_dir.mkdir(parents=True, exist_ok=True)
    build_dir.mkdir(parents=True, exist_ok=True)
    dotnethome_dir.mkdir(parents=True, exist_ok=True)
    tools_dir.mkdir(parents=True, exist_ok=True)

    # Install .NET SDK needed for crank and crank-agent via dotnet-install public script.
    install_dotnet_sdk("8.0", dotnethome_dir)
    # Be more flexible and install the latest LTS version as well in case if crank moves to it
    install_dotnet_sdk("LTS", dotnethome_dir)

    # Install crank-agent (runs benchmarks) and crank-controller (or just crank) that schedules them.
    dotnet_exe = dotnet_root_dir / native_exe("dotnet")
    run_command([str(dotnet_exe), "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Agent", "--version", "0.2.0-*"], retries=3)
    run_command([str(dotnet_exe), "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Controller", "--version", "0.2.0-*"], retries=3)

    # Create a Localhost.yml to define the local environment since we can't access the PerfLab.
    yml = textwrap.dedent(
f"""
variables:
    applicationAddress: 127.0.0.1
    loadAddress: 127.0.0.1
    applicationPort: {CRANK_PORT}
    applicationScheme: http
    loadPort: {CRANK_PORT}
    loadScheme: http
profiles:
    Localhost:
        variables:
            serverAddress: "{{{{applicationAddress}}}}"
        jobs:
            application:
                endpoints:
                    - "{{{{applicationScheme}}}}://{{{{applicationAddress}}}}:{{{{applicationPort}}}}"
            load:
                endpoints:
                    - "{{{{loadScheme}}}}://{{{{loadAddress}}}}:{{{{loadPort}}}}"
""")
    localhost_yml.write_text(yml, encoding="utf-8")


    print("Starting crank-agent...")
    # Create process with proper flags for Windows process management
    if sys.platform == "win32":
        # On Windows, create a new process group and detach from console
        creation_flags = subprocess.CREATE_NEW_PROCESS_GROUP | subprocess.DETACHED_PROCESS
        start_new_session = False
    else:
        # On Unix-like systems, start a new session
        creation_flags = 0
        start_new_session = True
    
    agent_process = subprocess.Popen( 
        [
            str(tools_dir / native_exe("crank-agent")),
            "--url", f"http://*:{CRANK_PORT}",
            "--log-path", str(logs_dir),
            "--build-path", str(build_dir),
            "--dotnethome", str(dotnethome_dir),
        ],
        stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL, stdin=subprocess.DEVNULL,
        creationflags=creation_flags,
        start_new_session=start_new_session
    )
    print(f"Waiting 10s for crank-agent to start ...")
    time.sleep(10)
    return agent_process, tools_dir / native_exe("crank"), localhost_yml


# Run crank scenario
def run_crank_scenario(crank_app: Path, scenario_name: str, framework: str, work_dir: Path, core_root_path: Path, config_path: Path, dryrun: bool, *extra_args: str):
    spmi_shim = native_dll("superpmi-shim-collector")
    clrjit = native_dll("clrjit")
    coreclr = native_dll("coreclr")
    spcorelib = "System.Private.CoreLib.dll"
    cmd = [
        str(crank_app),
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/build/azure.profile.yml",
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/build/ci.profile.yml",
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/steadystate.profile.yml",
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/json.benchmarks.yml",
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/orchard.benchmarks.yml",
        "--config", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.Jobs.Wrk/wrk.yml",
        "--config", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.Jobs.Bombardier/bombardier.yml",
        "--config", str(config_path),
        "--profile", "Localhost",
        "--scenario", scenario_name,
        "--application.framework", framework,
        "--application.Channel", "latest", # should be 'edge', but it causes random build failures sometimes.
        "--application.noGlobalJson", "false",
        "--application.collectDependencies", "false",
        "--application.options.collectCounters", "false",
        "--load.options.reuseBuild", "true",
        "--load.variables.duration", "45",
        "--load.variables.warmup", "15",
        "--load.job", "bombardier", # Bombardier is more cross-platform friendly (wrk is linux only)
    ]
    
    # Only add SPMI collection environment variables and output files if not in dry run mode
    if not dryrun:
        cmd.extend([
            "--application.environmentVariables", f"DOTNET_JitName={spmi_shim}",
            "--application.environmentVariables", f"SuperPMIShimLogPath={str(work_dir)}",
            "--application.environmentVariables", f"SuperPMIShimPath=./{clrjit}",
            "--application.environmentVariables", "DOTNET_EnableExtraSuperPmiQueries=1",
            "--application.options.outputFiles", str(core_root_path / spmi_shim),
            "--application.options.outputFiles", str(core_root_path / clrjit),
            "--application.options.outputFiles", str(core_root_path / coreclr),
            "--application.options.outputFiles", str(core_root_path / spcorelib),
        ])
    
    # Append any extra scenario-specific arguments
    if extra_args:
        cmd.extend(extra_args)
    run_command(cmd, retries=3)


# Main entry point
def main():
    parser = argparse.ArgumentParser(description="Cross-platform crank runner.")
    parser.add_argument("--core_root", help="Path to built runtime bits (CORE_ROOT).")
    parser.add_argument("--tfm", default="net10.0", help="Target Framework Moniker (e.g., net10.0).")
    parser.add_argument("--output_mch", help="File path to copy the resulting merged .mch to (expects a file path, not a directory).")
    parser.add_argument("--work_dir", help="Optional path to a directory in which a new working directory will be created. If specified, a new subdirectory with a random name prefixed with 'aspnet2_' will be created inside this directory. Otherwise a system temp directory is used.")
    parser.add_argument("--dryrun", action="store_true", help="Run benchmarks only without collecting SPMI data or generating .mch files.")
    args = parser.parse_args()

    # Validate required arguments when not in dry run mode
    if not args.dryrun:
        if not args.core_root:
            parser.error("--core_root is required when not using --dryrun")
        if not args.output_mch:
            parser.error("--output_mch is required when not using --dryrun")

    core_root_path = Path(args.core_root).expanduser().resolve() if args.core_root else None
    output_mch_path = Path(args.output_mch).expanduser().resolve() if args.output_mch else None

    print("Running the script with the following parameters:")
    if args.core_root:
        print(f"--core_root: {core_root_path}")
    print(f"--tfm: {args.tfm}")
    if args.output_mch:
        print(f"--output_mch: {output_mch_path}")
    if args.work_dir:
        print(f"--work_dir: {Path(args.work_dir).expanduser().resolve()}")
    print(f"--dryrun: {args.dryrun}")

    if not args.dryrun:
        mcs_cmd = core_root_path / native_exe("mcs")
        if not mcs_cmd.exists():
            print(f"Error: mcs[.exe] not found at {mcs_cmd}. Ensure runtime bits include mcs.", file=sys.stderr)
            sys.exit(2)

    # Create or use working directory for crank_data
    if args.work_dir:
        work_dir_base = Path(args.work_dir).expanduser().resolve()
        work_dir_base.mkdir(parents=True, exist_ok=True)
    else:
        # if not specified, use a temp directory
        work_dir_base = Path(tempfile.mkdtemp(prefix="aspnet2_"))
    print(f"Using temp work directory: {work_dir_base}")

    work_dir_base = work_dir_base / "crank_data"
    work_dir_base.mkdir(parents=True, exist_ok=True)

    # Set current working directory to work_dir_base
    os.chdir(work_dir_base)

    try:
        _crank_agent_process, crank_app_path, config_path = setup_and_run_crank_agent(work_dir_base)

        scenarios = [
            # OrchardCMS scenario
            ("about-sqlite",
                # Extra args:
                "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/orchard.benchmarks.yml"),

            # JsonMVC scenario
            ("mvc",
                # Extra args:
                "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/json.benchmarks.yml"),

            # NoMvcAuth scenario
            ("NoMvcAuth",
                # Extra args:
                "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/src/BenchmarksApps/Mvc/benchmarks.jwtapi.yml"),

            # PlatformPlaintext scenario
            ("plaintext",
                # Extra args:
                "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/platform.benchmarks.yml"),
        ]

        for entry in scenarios:
            scenario_name, *extra = entry
            print(f"### Running {scenario_name} benchmark... ###")
            run_crank_scenario(
                crank_app_path,
                scenario_name,
                args.tfm,
                work_dir_base,
                core_root_path,
                config_path,
                args.dryrun,
                *extra,
            )

        print("Finished running benchmarks.")

    except Exception as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
    finally:
        print("Cleaning up...")
        if _crank_agent_process is not None:
            print(f"Terminating agent process {_crank_agent_process.pid}...")
            if sys.platform == "win32":
                # On Windows, use taskkill first to terminate the entire process tree
                try:
                    result = subprocess.run(["taskkill", "/F", "/T", "/PID", str(_crank_agent_process.pid)])
                except Exception as e:
                    print(f"Failed to use taskkill: {e}")

            try:    
                _crank_agent_process.terminate()
                _crank_agent_process.wait(timeout=10)
                print(f"Agent process {_crank_agent_process.pid} terminated gracefully.")
            except Exception as e:
                print(f"Error during standard termination: {e}")

            # Clear the global reference
            _crank_agent_process = None

        # Skip .mc/.mch processing in dry run mode
        if not args.dryrun:
            # Count number of *.mc files in work_dir_base:
            mc_file_count = len(list(work_dir_base.glob("*.mc")))
            if mc_file_count == 0:
                print("Error: No .mc files found.")
                sys.exit(2)

            print(f"Merging {mc_file_count} .mc files...")

            # Merge
            run_command([
                str(mcs_cmd),
                "-merge",
                "-recursive",
                "-dedup",
                "-thin",
                "temp.mch",
                str(work_dir_base / "*.mc")
            ])

            # clean
            jitlib = str(core_root_path / native_dll("clrjit"))
            run_command([str(core_root_path / native_exe("superpmi")), "-v", "ewmi", "-f", "fail.mcl", jitlib, "temp.mch"])

            # strip
            if os.path.isfile("fail.mcl") and os.stat("fail.mcl").st_size != 0:
                print("Replay had failures, cleaning...")
                run_command([str(mcs_cmd), "-strip", "fail.mcl", "temp.mch", str(output_mch_path)])
            else:
                print("Replay was clean...")
                shutil.copy2("temp.mch", str(output_mch_path))

            # index
            run_command([str(mcs_cmd), "-toc", str(output_mch_path)])

            # overall summary
            print("Merged summary for " + str(output_mch_path))
            run_command([str(mcs_cmd), "-jitflags", str(output_mch_path)])

            print(f"Finished merging .mc files into {output_mch_path}")

            # delete all .mc files
            for mc_file in work_dir_base.glob("*.mc"):
                mc_file.unlink()

            # delete temp.mch and fail.mcl if they exist
            if os.path.exists("temp.mch"):
                os.remove("temp.mch")
            if os.path.exists("fail.mcl"):
                os.remove("fail.mcl")
        else:
            print("Dry run mode: Skipping SPMI data collection and .mch file generation.")
        
        # validate that mch file was created under non-dryrun:
        if not args.dryrun and output_mch_path and output_mch_path.exists():
            print(f"Successfully created {output_mch_path}")
        print("Done.")

if __name__ == "__main__":
    main()
    sys.exit(0)
