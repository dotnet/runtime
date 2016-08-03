#!/usr/bin/env python3.5

from benchview.console import write
from benchview.format.helpers import write_object_as_json
from benchview.format.JSONFormat import Machine
from benchview.format.JSONFormat import MachineData
from benchview.format.JSONFormat import OperatingSystem
from benchview.utils.common import is_supported_version, add_dependencies_to_path

from os import environ, path
from sys import exit
from traceback import format_exc

import argparse
import platform
import re
import subprocess

add_dependencies_to_path(__file__)

# External modules: Attempt to exit gracefully if module if not present.
try:
    import cpuinfo
except ImportError as ex:
    write.error(str(ex))
    exit(1)

def get_wmic_keyvalue(provider: str) -> dict:
    output = subprocess.run(
        ['wmic', provider, 'get', '/value'],
        stdout=subprocess.PIPE,
        check=True,
        universal_newlines=True)

    #Output format from command is Key=Value, split on '=' and create a dictionary
    return dict(line.split('=', maxsplit=1) for line in output.stdout.splitlines() if line)

def get_wmic_os() -> dict:
    dct = get_wmic_keyvalue('os')
    dct['architecture'] = environ['PROCESSOR_ARCHITECTURE']
    return dct

def get_wmic_totalmemory() -> int:
    dct = get_wmic_keyvalue('ComputerSystem')
    return int(dct["TotalPhysicalMemory"])

def get_wmic_threads() -> int:
    dct = get_wmic_keyvalue('cpu')
    return int(dct["NumberOfLogicalProcessors"])

def get_wmic_processors() -> int:
    dct = get_wmic_keyvalue('cpu')
    return int(dct["NumberOfCores"])

def meminfo_totalmemory() -> int:
    output = subprocess.run(
        "awk '/MemTotal/ {print $2}' /proc/meminfo",
        stdout=subprocess.PIPE,
        check=True,
        universal_newlines=True,
        shell=True)
    return int(output.stdout.splitlines()[0])

def lscpu_threads() -> int:
    output = subprocess.run(
        """lscpu | awk -F ":" '/^CPU\(s\)/ { print 1*$2 }'""",
        stdout=subprocess.PIPE,
        check=True,
        universal_newlines=True,
        shell=True)
    return int(output.stdout.splitlines()[0])

def lscpu_processors() -> int:
    output = subprocess.run(
        """lscpu | awk -F ":" '/Core/ { c=$2; }; /Socket/ { print c*$2 }'""",
        stdout=subprocess.PIPE,
        check=True,
        universal_newlines=True,
        shell=True)
    return int(output.stdout.splitlines()[0])

def normalize_architecture(arch: str) -> str:
    if arch.casefold() == 'x86_64'.casefold():
        return 'amd64'
    if arch.casefold() == 'aarch64'.casefold():
        return 'arm64'
    if re.search('^[0-9]86$', arch):
        return 'x86'
    if re.search('armv7.*', arch):
        return 'arm'
    return arch

def get_argument_parser() -> dict:
    parser = argparse.ArgumentParser(
        description = 'Gathers machine-specific metadata'
    )

    parser.add_argument(
        '-o',
        '--outfile',
        metavar = '<Output json file name>',
        help = 'The file path to write to (If not specfied, defaults to "machinedata.json").',
        required = False,
        default = 'machinedata.json'
    )

    parser.add_argument(
        '--external',
        help = 'Flag indicating that the machine information is not local (data will be provided by the user).',
        action = 'store_true',
        required = False,
        default = False
    )

    # Machine information.
    parser.add_argument(
        '--machine-name',
        help = 'Computer name.',
        required = False
    )

    # Machine information.
    parser.add_argument(
        '--machine-manufacturer',
        help = 'Machine manufacturer.',
        required = False
    )

    parser.add_argument(
        '--machine-physical-memory',
        help = 'The RAM memory size in MB.',
        required = False,
        type = float
    )

    parser.add_argument(
        '--machine-cores',
        help = 'Number of physical processing units.',
        required = False,
        type = int
    )

    parser.add_argument(
        '--machine-threads',
        help = 'Number of logical processors.',
        required = False,
        type = int
    )

    parser.add_argument(
        '--machine-architecture',
        help = 'Machine architecture.',
        required = False
    )

    # Operating System information.
    parser.add_argument(
        '--os-name',
        help = 'Name of the Operating System where the tests ran.',
        required = False
    )

    parser.add_argument(
        '--os-version',
        help = 'Version of the Operating System where the tests ran.',
        required = False
    )

    parser.add_argument(
        '--os-edition',
        help = 'Edition of the Operating System where the tests ran.',
        required = False
    )

    parser.add_argument(
        '--os-architecture',
        help = 'Architecture of the Operating System where the tests ran.',
        required = False
    )

    return vars(parser.parse_args())

def condition(dct: dict, key: str) -> bool:
    return key in dct and not dct[key] is None

def main() -> int:
    try:
        if not is_supported_version():
            write.error("You need to use Python 3.5 or newer.")
            return 1

        args = get_argument_parser()

        manufacturer = None
        edition = None
        architecture = None
        physicalMemory = None

        if not args['external']:
            cpu = cpuinfo.get_cpu_info()
            manufacturer = cpu['vendor_id']

            #platform.release() is an empty string on windows, get using wmic instead
            if platform.system() == "Windows":
                os = get_wmic_os()
                edition = os['Caption']
                architecture = os['architecture']
                #Convert memory from bytes to megabytes
                physicalMemory = get_wmic_totalmemory() / 1024 / 1024
                cores = get_wmic_processors()
                threads = get_wmic_threads()

            else:
                edition = platform.release()
                architecture = platform.uname().machine
                cores = lscpu_processors()
                threads = lscpu_threads()
                physicalMemory = meminfo_totalmemory() / 1024

        # Override data gathered if the user provided a custom value.
        machine_name            = args['machine_name']              if condition(args, 'machine_name')              else platform.node()
        machine_architecture    = args['machine_architecture']      if condition(args, 'machine_architecture')      else normalize_architecture(platform.machine())
        machine_manufacturer    = args['machine_manufacturer']      if condition(args, 'machine_manufacturer')      else manufacturer
        machine_cores           = args['machine_cores']             if condition(args, 'machine_cores')             else cores
        machine_threads         = args['machine_threads']           if condition(args, 'machine_threads')           else threads
        machine_physicalMemory  = args['machine_physicalMemory']    if condition(args, 'machine_physicalMemory')    else physicalMemory
        machine = Machine(machine_name, machine_architecture, machine_manufacturer, machine_cores, machine_threads, machine_physicalMemory)

        os_name         = args['os_name']           if condition(args, 'os_name')           else platform.system()
        os_version      = args['os_version']        if condition(args, 'os_version')        else platform.version()
        os_edition      = args['os_edition']        if condition(args, 'os_edition')        else edition
        os_architecture = args['os_architecture']   if condition(args, 'os_architecture')   else normalize_architecture(architecture)
        os = OperatingSystem(os_name, os_version, os_edition, os_architecture)

        # Create a intermediate/common object for serialization.
        machinedata = MachineData(machine, os)

        write_object_as_json(args['outfile'], machinedata)

        # TODO: Add schema validation.
    except TypeError as ex:
        write.error(str(ex))
    except Exception as ex:
        write.error('{0}: {1}'.format(type(ex), str(ex)))
        write.error(format_exc())
        return 1

    return 0

if __name__ == "__main__":
    exit(main())
