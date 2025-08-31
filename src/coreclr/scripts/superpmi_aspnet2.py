#!/usr/bin/env python3
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


##################################################################################
#
#  This script sets up an environment for running crank-agent and
#  crank-controller (https://github.com/dotnet/crank) locally in 
#  order to run various ASP.NET benchmarks (TechEmpower, OrchardCMS, etc.)
#  and collect SPMI collections using the provided runtime bits.
#
#  Usage example:
#    python superpmi_aspnet2.py --core_root C:\runtime\artifacts\bin\coreclr\windows.x64.Checked --output_mch aspnet2.mch
#
##################################################################################

CRANK_PORT = 5010

# Convert a filename to the appropriate native DLL name, e.g. "clrjit" -> "libclrjit.so" (on Linux)
def native_dll(name: str) -> str:
    ext = ".dll" if sys.platform.startswith("win") else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if sys.platform.startswith("win") else "lib"
    return f"{prefix}{name}{ext}"


# Same for executables
def native_exe(name: str) -> str:
    ext = ".exe" if sys.platform.startswith("win") else ""
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
    # It doesn't download the resulting artifacts without this:
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
    if os.name == "nt":
        ch = channel.replace("'", "''")
        di = str(install_dir).replace("'", "''")
        ps_script = (
            "[System.Net.ServicePointManager]::SecurityProtocol=[System.Net.SecurityProtocolType]::Tls12;"
            "Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile 'dotnet-install.ps1';"
            f"$DotnetVersion='{ch}';$InstallDir='{di}';"
            "& './dotnet-install.ps1' -Channel $DotnetVersion -InstallDir $InstallDir -NoPath"
        )
        subprocess.check_call(["powershell.exe","-NoProfile","-ExecutionPolicy", "Bypass","-Command", ps_script])
    else:
        with tempfile.TemporaryDirectory() as td:
            script_path = Path(td) / "dotnet-install.sh"
            with urllib.request.urlopen("https://dot.net/v1/dotnet-install.sh") as resp, open(script_path, "wb") as f:
                f.write(resp.read())
            os.chmod(script_path, 0o755)
            subprocess.check_call([str(script_path),"--channel", channel,"--install-dir", str(install_dir),"--no-path"])


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
    # Be more agile and install the latest LTS version as well in case if crank moves to it
    install_dotnet_sdk("LTS", dotnethome_dir)

    # Install crank-agent (runs benchmarks) and crank-controller (or just crank) that schedules them.
    dotnet_exe = dotnet_root_dir / native_exe("dotnet")
    run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Agent", "--version", "0.2.0-*"])
    run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Controller", "--version", "0.2.0-*"])

    # Create a Localhost.yml to define the local environment since we can't access the PerfLab.
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


    print("Starting crank-agent...")
    agent_process = subprocess.Popen(
        [
            str(tools_dir / native_exe("crank-agent")),
            "--url", f"http://*:{CRANK_PORT}",
            "--log-path", str(logs_dir),
            "--build-path", str(build_dir),
            "--dotnethome", str(dotnethome_dir),
        ]
    )
    print(f"Waiting 10s for crank-agent to start ...")
    time.sleep(10)
    return agent_process, tools_dir / native_exe("crank"), localhost_yml


# Run crank scenario
def run_crank_scenario(crank_app: Path, scenario_name: str, framework: str, core_root_path: Path, config_path: Path, *extra_args: str):
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
        "--application.noGlobalJson", "false",
        "--application.framework", framework,
        "--application.Channel", "latest", # should be 'edge', but it causes random build failures sometimes.
        "--application.options.collectCounters", "false",
        "--application.collectDependencies", "false",
        "--load.options.reuseBuild", "true",
        "--load.variables.duration", "30", # default 15s is not enough for Tier1 promotion
        "--load.connections", "32",
        "--load.job", "bombardier", # Bombardier is more cross-platform friendly (wrk is linux only)
        "--application.environmentVariables", f"COMPlus_JitName={spmi_shim}",
        "--application.environmentVariables", "SuperPMIShimLogPath=.",
        "--application.environmentVariables", f"SuperPMIShimPath=./{clrjit}",
        "--application.options.fetch", "true",
        "--application.options.fetchOutput", scenario_name + ".crank.zip",
        "--application.options.outputFiles", str(core_root_path / spmi_shim),
        "--application.options.outputFiles", str(core_root_path / clrjit),
        "--application.options.outputFiles", str(core_root_path / coreclr),
        "--application.options.outputFiles", str(core_root_path / spcorelib),
    ]
    # Append any extra scenario-specific arguments
    if extra_args:
        cmd.extend(extra_args)
    run(cmd)


# Main entry point
def main():
    parser = argparse.ArgumentParser(description="Cross-platform crank runner.")
    parser.add_argument("--core_root", required=True, help="Path to built runtime bits (CORE_ROOT).")
    parser.add_argument("--tfm", default="net10.0", help="Target Framework Moniker (e.g., net10.0).")
    parser.add_argument("--output_mch", required=True, help="File path to copy the resulting merged .mch to (expects a file path, not a directory).")
    parser.add_argument("--work_dir", help="Optional path to a directory in which a new working directory will be created. If specified, a new subdirectory with a random name prefixed with 'aspnet2_' will be created inside this directory. Otherwise a system temp directory is used.")
    args = parser.parse_args()

    core_root_path = Path(args.core_root).expanduser().resolve()
    output_mch_path = Path(args.output_mch).expanduser().resolve()

    print("Running the script with the following parameters:")
    print(f"--core_root: {core_root_path}")
    print(f"--tfm: {args.tfm}")
    print(f"--output_mch: {output_mch_path}")
    if args.work_dir:
        print(f"--work_dir: {Path(args.work_dir).expanduser().resolve()}")

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

    # Set current working directory to work_dir_base
    os.chdir(work_dir_base)

    agent_process = None
    try:
        agent_process, crank_app_path, config_path = setup_and_run_crank_agent(work_dir_base)

        scenarios = [
            # OrchardCMS scenario
            # ("about-sqlite",
            #     # Extra args:
            #     "--config", "https://raw.githubusercontent.com/aspnet/Benchmarks/main/scenarios/orchard.benchmarks.yml"),
            # TODO: re-enable it, currently it randomly fails with build errors.

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
                args.tf,
                core_root_path,
                config_path,
                *extra,
            )

        print("Finished running benchmarks.")

        # Extract .mc files from zip archives into crank_data/tmp instead of the current directory
        print("Extracting .mc files from zip archives...")
        extracted_count = 0
        for z in pathlib.Path('.').glob('*.crank.zip'):
            with zipfile.ZipFile(z) as f:
                for name in f.namelist():
                    if name.endswith('.mc'):
                        f.extract(name, str(work_dir_base))
                        extracted_count += 1
            z.unlink(missing_ok=True)

        # Merge *.mc files into crank.mch
        if extracted_count == 0:
            print("Error: No .mc files found in zip outputs.")
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
                str(work_dir_base / "*.mc")
            ], check=True, cwd=str(work_dir_base))
        print(f"Finished merging .mc files into {output_mch_path}")

    finally:
        print("Cleaning up...")
        if 'agent_process' in locals() and agent_process is not None:
            agent_process.terminate()
        print("Done.")
        sys.exit()

if __name__ == "__main__":
    main()
