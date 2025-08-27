#!/usr/bin/env python3
import argparse
import os
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


# Check if a port is listening
def port_is_listening(host: str, port: int, timeout_s: float = 0.5) -> bool:
    with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as s:
        s.settimeout(timeout_s)
        try:
            s.connect((host, port))
            return True
        except Exception:
            return False


# Wait for the port to be available
def wait_for_port(host: str, port: int, timeout_s: int) -> bool:
    start = time.time()
    while time.time() - start < timeout_s:
        if port_is_listening(host, port):
            return True
        time.sleep(0.2)
    return False


# Convert a filename to the appropriate native DLL name, e.g. "clrjit" -> "libclrjit.so" (on Linux)
def native_dll(name: str) -> str:
    ext = ".dll" if sys.platform.startswith("win") else (".dylib" if sys.platform == "darwin" else ".so")
    prefix = "" if sys.platform.startswith("win") else "lib"
    return f"{prefix}{name}{ext}"


# Run a command
def run(cmd, cwd=None):
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
        return proc.wait()
    except KeyboardInterrupt:
        proc.terminate()
        print("Process terminated")
        return None


# Ensure .NET tools are installed and Localhost.yml is present
def ensure_tools_and_localhost_yaml(workdir: Path, port: int):
    data_dir = workdir / "crank_data"
    logs_dir = data_dir / "logs"
    build_dir = data_dir / "build"
    tools_dir = data_dir / "dotnet_tools"
    dotnethome_dir = data_dir / "dotnet_home"
    localhost_yml = data_dir / "Localhost.yml"

    os.environ['DOTNET_ROOT'] = str(dotnethome_dir)
    os.environ['DOTNET_MULTILEVEL_LOOKUP'] = "1"
    os.environ["PATH"] = str(tools_dir) + os.pathsep + os.environ.get("PATH", "")

    if not data_dir.exists():
        print("Installing tools ...")
        logs_dir.mkdir(parents=True, exist_ok=True)
        build_dir.mkdir(parents=True, exist_ok=True)
        dotnethome_dir.mkdir(parents=True, exist_ok=True)
        tools_dir.mkdir(parents=True, exist_ok=True)

        # Install .NET 8.0 needed for crank and crank-agent via dotnet-install public script.
        url = "https://dot.net/v1/dotnet-install." + ("ps1" if platform.system()=="Windows" else "sh")
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, os.path.basename(url))
            urllib.request.urlretrieve(url, path)
            if url.endswith(".ps1"):
                subprocess.check_call(["powershell","-ExecutionPolicy","Bypass","-File",path,
                                    "-Channel","8.0","-InstallDir", str(dotnethome_dir)])
            else:
                os.chmod(path,0o755)
                subprocess.check_call([path,"-Channel","8.0","-InstallDir", str(dotnethome_dir)])

        dotnet_exe = dotnethome_dir / "dotnet"
        run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Agent", "--version", "0.2.0-*"], cwd=dotnethome_dir)
        run([dotnet_exe, "tool", "install", "--tool-path", str(tools_dir), "Microsoft.Crank.Controller", "--version", "0.2.0-*"], cwd=dotnethome_dir)

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


# Start the crank-agent
def start_crank_agent(workdir: Path, port: int):
    if port_is_listening("127.0.0.1", port):
        raise ValueError("Port already in use")

    print("crank-agent is not running yet. Starting...")
    logs_dir = workdir / "crank_data" / "logs"
    build_dir = workdir / "crank_data" / "build"
    # Keep naming consistent with ensure_tools_and_localhost_yaml()
    dotnethome_dir = workdir / "crank_data" / "dotnet_home"

    agent_process = subprocess.Popen(
        [
            "crank-agent",
            "--url", f"http://*:{port}",
            "--log-path", str(logs_dir),
            "--build-path", str(build_dir),
            "--dotnethome", str(dotnethome_dir),
        ]
    )

    print(f"Waiting up to 20s for crank-agent to start ...")
    if not wait_for_port("127.0.0.1", port, 20):
        print("Warning: crank-agent didn't open the port in time. Proceeding anyway.", file=sys.stderr)
    else:
        print("crank-agent started.")
    return agent_process

# Build the crank-controller command for execution
def build_crank_command(framework: str, runtime_bits_path: Path, scenario: str, config_path: Path):
    spmi_shim = native_dll("superpmi-shim-collector")
    clrjit = native_dll("clrjit")
    coreclr = native_dll("coreclr")
    spcorelib = "System.Private.CoreLib.dll"
    cmd = [
        "crank",
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
        "--load.variables.duration", "5", # default 15s is not enough for Tier1 promotion
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
    args = parser.parse_args()
    repo_dir = Path.cwd()
    runtime_bits_path = Path(args.core_root).expanduser().resolve()
    output_mch_path = Path(args.output_mch).expanduser().resolve()

    print("Running the script with the following parameters:")
    print(f"--core_root: {runtime_bits_path}")
    print(f"--tfm: {args.tfm}")
    print(f"--output_mch: {output_mch_path}")

    mcs_cmd = runtime_bits_path / ("mcs.exe" if sys.platform == "win32" else "mcs")
    if not mcs_cmd.exists():
        print(f"Error: mcs.exe not found at {mcs_cmd}. Ensure runtime bits include mcs.", file=sys.stderr)
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

    ensure_tools_and_localhost_yaml(temp_root, CRANK_PORT)
    try:
        agent_process = None
        agent_process = start_crank_agent(temp_root, CRANK_PORT)

        # Benchmarks

        config_path = temp_root / "crank_data" / "Localhost.yml"
        # print("### Running OrchardCMS benchmark... ###")
        # run(build_crank_command(framework=args.tfm, runtime_bits_path=runtime_bits_path, scenario="about-sqlite", config_path=config_path))

        print("### Running JsonMVC benchmark... ###")
        run(build_crank_command(framework=args.tfm, runtime_bits_path=runtime_bits_path, scenario="mvc", config_path=config_path))

        print("### Running NoMvcAuth benchmark... ###")
        run(build_crank_command(framework=args.tfm, runtime_bits_path=runtime_bits_path, scenario="NoMvcAuth", config_path=config_path))


        print("Finished running benchmarks.")

        # Extract .mc files from zip archives into crank_data/tmp instead of the current directory
        print("Extracting .mc files from zip archives...")
        tmp_dir = temp_root / "crank_data" / "tmp"
        if tmp_dir.exists():
            shutil.rmtree(tmp_dir, ignore_errors=True)
        tmp_dir.mkdir(parents=True, exist_ok=True)
        produced_mch = False
        extracted_count = 0
        for z in pathlib.Path('.').glob('*.crank.zip'):
            with zipfile.ZipFile(z) as f:
                for name in f.namelist():
                    # include .mc files from any path inside the zip
                    if name.endswith('.mc'):
                        f.extract(name, str(tmp_dir))
                        extracted_count += 1
            z.unlink(missing_ok=True)

        # Merge *.mc files into crank.mch
        if extracted_count == 0:
            print("No .mc files found in zip outputs; skipping merge.")
        else:
            # Merge all .mc files into crank.mch, scanning recursively from tmp
            print(f"Extracted {extracted_count} .mc files; merging into crank.mch ...")
            subprocess.run([
                str(mcs_cmd),
                "-merge",
                "-recursive",
                "-dedup",
                "-thin",
                "crank.mch",
                "."
            ], check=True, cwd=str(tmp_dir))

            # Move the produced crank.mch back to the workspace root
            print(f"Moving produced crank.mch to {repo_dir / 'crank.mch'}")
            shutil.copyfile(tmp_dir / "crank.mch", repo_dir / "crank.mch")
            produced_mch = True
            
            # Copy the resulting MCH to the specified output file
            if args.output_mch:
                mch_src = repo_dir / "crank.mch"
                if not mch_src.exists():
                    print(f"Error: expected MCH not found at {mch_src}", file=sys.stderr)
                    sys.exit(2)
                out_path = output_mch_path
                if out_path.exists() and out_path.is_dir():
                    print(f"Error: --output_mch points to a directory, expected a file path: {out_path}", file=sys.stderr)
                    sys.exit(2)
                # Ensure the destination directory exists
                if out_path.parent and not out_path.parent.exists():
                    out_path.parent.mkdir(parents=True, exist_ok=True)
                print(f"Copying {mch_src} -> {out_path}")
                shutil.copyfile(mch_src, out_path)

        # Clean up the temp crank directory at the very end
        if not args.no_cleanup:
            # Only delete the working directory if we created a temp one
            if 'created_temp' in locals() and created_temp:
                try:
                    shutil.rmtree(temp_root, ignore_errors=True)
                except Exception:
                    pass
        
    finally:
        print("Cleaning up...")
        if 'agent_process' in locals() and agent_process is not None:
            agent_process.terminate()
        time.sleep(3)
        
        # Clean up only if not suppressed
        if not args.no_cleanup:
            if produced_mch:
                shutil.rmtree(tmp_dir, ignore_errors=True)

    print("Done!")

if __name__ == "__main__":
    main()
