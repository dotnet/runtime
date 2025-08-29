#!/usr/bin/env python3
import argparse
import os
import re
import requests
import socket
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


##########################################################################################################
#
#  This script sets up an environment for running crank-agent and crank-controller 
#  (https://github.com/dotnet/crank) locally in order to run various ASP.NET benchmarks 
#  (TechEmpower, OrchardCMS, etc.) and collect SPMI collections using the provided runtime bits.
#  The script is cross-platform and does everything locally while requiring only 'git' as a dependency.
#
#  Usage example:
#    py superpmi_aspnet2.py --core_root C:\runtime\artifacts\bin\coreclr\windows.x64.Checked --output_mch aspnet.mch
#
#  Prerequisites:
#  * git (crank-agent relies on it being available from PATH)
#  * python3 (to run this script)
#
##########################################################################################################

CRANK_PORT = 5010


# Convert a filename to the appropriate native DLL name, e.g. "clrjit" -> "libclrjit.so" (on Linux)
def native_dll(name: str) -> str:
    ext = ".dll" if sys.platform.startswith("win") else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if sys.platform.startswith("win") else "lib"
    return f"{prefix}{name}{ext}"

def native_exe(name: str) -> str:
    ext = ".exe" if sys.platform.startswith("win") else ""
    return f"{name}{ext}"

def native_script(name: str) -> str:
    ext = ".ps1" if sys.platform.startswith("win") else ".sh"
    return f"{name}{ext}"

# Run a command
def run(cmd, cwd=None, timeout_seconds=45*60):
    print(f"Running command: {' '.join(map(str, cmd))}")
    kwargs = {
        "stdin": subprocess.DEVNULL,
        "stdout": sys.stdout,
        "stderr": subprocess.STDOUT,
        "cwd": cwd,
    }
    if os.name == "nt":
        kwargs["creationflags"] = getattr(subprocess, "CREATE_NEW_PROCESS_GROUP", 0)
    else:
        kwargs["start_new_session"] = True
    proc = subprocess.Popen(cmd, **kwargs)
    try:
        # Wait with a timeout; let the TimeoutExpired propagate so caller can decide what to do.
        return proc.wait(timeout=timeout_seconds)
    except subprocess.TimeoutExpired:
        print(f"Process timed out after {timeout_seconds} seconds. Leaving process running and raising exception.")
        raise
    except KeyboardInterrupt:
        try:
            proc.terminate()
        except Exception:
            pass
        print("Process terminated")
        return None


# Temp workaround, will be removed once https://github.com/dotnet/crank/pull/841 lands
def download_mingit_windows(dest: str) -> str:
    # Map our arch key to MinGit asset suffix
    m = {"x64": "64-bit", "arm64": "arm64", "x86": "32-bit"}
    assets = requests.get("https://api.github.com/repos/git-for-windows/git/releases/latest", timeout=100).json()["assets"]
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
        raise RuntimeError(f"Unable to find MinGit asset for arch '{arch}'. Available assets: {[a['name'] for a in assets if 'MinGit' in a['name']]}")
    os.makedirs(dest, exist_ok=True); zip_path = os.path.join(dest, asset["name"])
    with requests.get(asset["browser_download_url"], stream=True) as r, open(zip_path,"wb") as f:
        for c in r.iter_content(8192): f.write(c)
    git_dir = os.path.join(dest,"git"); shutil.rmtree(git_dir, ignore_errors=True)
    with zipfile.ZipFile(zip_path) as z: z.extractall(git_dir)
    os.remove(zip_path)
    return git_dir

def ensure_git(dest: Path) -> str:
    existing = shutil.which("git")
    if existing:
        print("git found")
        return
    if sys.platform == "win32":
        print("git not found, downloading portable git...")
        git_dir = download_mingit_windows(str(dest))
        cmd_path = os.path.join(git_dir, "cmd")
        os.environ["PATH"] = cmd_path + os.pathsep + os.environ.get("PATH", "")
        return


def setup_and_run_crank_agent(workdir: Path, port: int, cli=None):
    data_dir = workdir / "crank_data"
    logs_dir = data_dir / "logs"
    build_dir = data_dir / "build"
    tools_dir = data_dir / "dotnet_tools"
    dotnethome_dir = data_dir / "dotnet_home"
    localhost_yml = data_dir / "Localhost.yml"

    ensure_git(tools_dir)

    # If a CLI path is provided, use it as DOTNET_ROOT; otherwise default to our local dotnet_home
    dotnet_root_dir = Path(cli) if cli else dotnethome_dir
    os.environ['DOTNET_ROOT'] = str(dotnet_root_dir)
    os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    os.environ['DOTNET_MULTILEVEL_LOOKUP'] = '0'
    os.environ['UseSharedCompilation'] = 'false'
    os.environ["PATH"] = str(tools_dir) + os.pathsep + os.environ.get("PATH", "")

    if not data_dir.exists():
        print("Installing tools ...")
        logs_dir.mkdir(parents=True, exist_ok=True)
        build_dir.mkdir(parents=True, exist_ok=True)
        dotnethome_dir.mkdir(parents=True, exist_ok=True)
        tools_dir.mkdir(parents=True, exist_ok=True)

        # If a CLI path was provided, skip installing .NET and use that SDK.
        if cli is None:
            # Install .NET 8.0 needed for crank and crank-agent via dotnet-install public script.
            url = "https://dot.net/v1/dotnet-install." + ("ps1" if platform.system()=="Windows" else "sh")
            with tempfile.TemporaryDirectory() as tmp:
                path = os.path.join(tmp, os.path.basename(url))
                urllib.request.urlretrieve(url, path)
                if url.endswith(".ps1"):
                    # Find available PowerShell executable
                    if shutil.which("pwsh"):
                        powershell_exe = "pwsh"
                    else:
                        powershell_exe = "powershell.exe"
                    print(f"Using PowerShell executable: {powershell_exe}")
                    
                    try:
                        result = subprocess.run([
                            powershell_exe, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", path,
                            "-Channel", "8.0", "-InstallDir", str(dotnethome_dir)
                        ], capture_output=True, text=True, check=True)
                        print(f"PowerShell output: {result.stdout}")
                    except subprocess.CalledProcessError as e:
                        print(f"PowerShell script failed with exit code {e.returncode}")
                        print(f"Error output: {e.stderr}")
                        print(f"Standard output: {e.stdout}")
                        raise
                else:
                    os.chmod(path,0o755)
                    subprocess.check_call([path,"-Channel","8.0","-InstallDir", str(dotnethome_dir)])
        else:
            print(f"Using existing .NET SDK at: {dotnet_root_dir}")

        # Determine the dotnet executable to use for installing tools
        dotnet_exe = dotnet_root_dir / native_exe("dotnet")
        run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Agent", "--version", "0.2.0-*"])
        run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Controller", "--version", "0.2.0-*"])

        # Create a Localhost.yml to define the local environment
        yml = textwrap.dedent(
f"""
variables:
    applicationAddress: 127.0.0.1
    loadAddress: 127.0.0.1
    applicationPort: {CRANK_PORT}
    applicationScheme: http
    loadPort: {CRANK_PORT}
    serverPort: 5014
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
    else:
        print("Localhost.yml already present; skipping tool install/scaffold.")
    print("crank-agent is not running yet. Starting...")
    agent_process = subprocess.Popen(
        [
            str(tools_dir / native_exe("crank-agent")),
            "--url", f"http://*:{port}",
            "--log-path", str(logs_dir),
            "--build-path", str(build_dir),
            "--dotnethome", str(dotnethome_dir),
        ]
    )
    print(f"Waiting up to 10s for crank-agent to start ...")
    time.sleep(10)
    return agent_process, tools_dir / native_exe("crank"), localhost_yml

# Build the crank-controller command for execution
def build_crank_command(crank_app: Path, framework: str, runtime_bits_path: Path, scenario: str, config_path: Path):
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
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/src/BenchmarksApps/Mvc/benchmarks.jwtapi.yml",
        "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/orchard.benchmarks.yml",
        "--config", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.Jobs.Wrk/wrk.yml",
        "--config", "https://raw.githubusercontent.com/dotnet/crank/main/src/Microsoft.Crank.Jobs.Bombardier/bombardier.yml",
        "--config", str(config_path),
        "--profile", "Localhost",
        "--application.noGlobalJson", "false",
        "--application.framework", framework,
        "--application.Channel", "latest", # should be 'edge', but it causes random build failures sometimes.
        "--application.options.collectCounters", "false",
        "--application.collectDependencies", "false",
        "--load.options.reuseBuild", "true",
        "--load.variables.duration", "10", # default 15s is not enough for Tier1 promotion
        "--load.job", "bombardier", # Bombardier is more cross-platform friendly (wrk is linux only)
        "--application.environmentVariables", f"COMPlus_JitName={spmi_shim}",
        "--application.environmentVariables", "SuperPMIShimLogPath=.",
        "--application.environmentVariables", f"SuperPMIShimPath=./{clrjit}",
        "--application.options.fetch", "true",
        "--application.options.fetchOutput", scenario + ".crank.zip",
        "--application.options.outputFiles", str(runtime_bits_path / spmi_shim),
        "--application.options.outputFiles", str(runtime_bits_path / clrjit),
        "--application.options.outputFiles", str(runtime_bits_path / coreclr),
        "--application.options.outputFiles", str(runtime_bits_path / spcorelib),
        "--scenario", scenario
    ]
    return cmd


# Main entry point
def main():
    parser = argparse.ArgumentParser(description="Cross-platform crank runner.")
    # Renamed args
    parser.add_argument("--core_root", required=True, help="Path to built runtime bits (CORE_ROOT).")
    parser.add_argument("--tfm", default="net10.0", help="Target Framework Moniker (e.g., net10.0).")
    parser.add_argument("--output_mch", required=True, help="File path to copy the resulting merged .mch to (expects a file path, not a directory).")

    # New args
    parser.add_argument("--work_dir", help="Optional work directory; if not specified, a temp directory is used.")
    parser.add_argument("--no_cleanup", action="store_true", help="If specified, do not clean up temporary files after execution.")
    parser.add_argument("--cli", help="Optional path to an existing .NET SDK root; if provided, DOTNET_ROOT will use this and .NET 8.0 will not be installed.")
    args = parser.parse_args()

    repo_dir = Path.cwd()
    runtime_bits_path = Path(args.core_root).expanduser().resolve()
    output_mch_path = Path(args.output_mch).expanduser().resolve()
    if args.cli:
        args.cli = str(Path(args.cli).expanduser().resolve())

    print("Running the script with the following parameters:")
    print(f"--core_root: {runtime_bits_path}")
    print(f"--tfm: {args.tfm}")
    print(f"--output_mch: {output_mch_path}")
    if args.cli:
        print(f"--cli: {args.cli}")

    mcs_cmd = runtime_bits_path / native_exe("mcs")
    if not mcs_cmd.exists():
        print(f"Error: mcs[.exe] not found at {mcs_cmd}. Ensure runtime bits include mcs.", file=sys.stderr)
        sys.exit(2)

    # Create or use working directory for crank_data
    created_temp = False
    if args.work_dir:
        temp_root = Path(args.work_dir).resolve()
        temp_root.mkdir(parents=True, exist_ok=True)
        print(f"Using work directory: {temp_root}")
    else:
        temp_root = Path(tempfile.mkdtemp(prefix="aspnet4_crank_"))
        created_temp = True
        print(f"Using temp work directory: {temp_root}")

    # Set current working directory to temp_root
    os.chdir(temp_root)

    agent_process = None
    try:
        agent_process, crank_app_path, config_path = setup_and_run_crank_agent(temp_root, CRANK_PORT, cli=args.cli)

        # Benchmarks

        crank_data_path = temp_root / "crank_data"
        # print("### Running OrchardCMS benchmark... ###")
        # run(build_crank_command(crank_app_path, args.tfm, runtime_bits_path, "about-sqlite", config_path))

        print("### Running JsonMVC benchmark... ###")
        run(build_crank_command(crank_app_path, args.tfm, runtime_bits_path, "mvc", config_path))

        print("### Running NoMvcAuth benchmark... ###")
        # run(build_crank_command(crank_app_path, args.tfm, runtime_bits_path, "NoMvcAuth", config_path))

        print("Finished running benchmarks.")

        # Extract .mc files from zip archives into crank_data/tmp instead of the current directory
        print("Extracting .mc files from zip archives...")
        extracted_count = 0
        for z in pathlib.Path('.').glob('*.crank.zip'):
            with zipfile.ZipFile(z) as f:
                for name in f.namelist():
                    # include .mc files from any path inside the zip
                    if name.endswith('.mc'):
                        f.extract(name, str(crank_data_path))
                        extracted_count += 1
            z.unlink(missing_ok=True)

        # Merge *.mc files into crank.mch
        if extracted_count == 0:
            print("No .mc files found in zip outputs; skipping merge.")
            sys.exit(2)
        else:
            # Merge all .mc files into crank.mch, scanning recursively from tmp
            print(f"Extracted {extracted_count} .mc files; merging into crank.mch ...")
            subprocess.run([
                str(mcs_cmd),
                "-merge",
                "-recursive",
                "-dedup",
                "-thin",
                str(output_mch_path),
                str(crank_data_path / "*.mc")
            ], check=True, cwd=str(crank_data_path))
        
    finally:
        print("Cleaning up...")
        if 'agent_process' in locals() and agent_process is not None:
            agent_process.terminate()
        time.sleep(5)
        
        # Clean up only if not suppressed
        if not args.no_cleanup:
            print(f'Removing temp dir {temp_root}')
            # remove the entire temp_root:
            shutil.rmtree(temp_root, ignore_errors=True)
        else:
            print(f'Not removing temp dir {temp_root} due to --no_cleanup')

    print("Done!")

if __name__ == "__main__":
    main()
