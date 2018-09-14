#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.
#
################################################################################
#
# Module: crossgen_comparison.py
#
# Notes:
#
# Script that
#   1) runs crossgen on System.Private.CoreLib.dll and CoreFX assemblies and
#   collects information about the crossgen behaviour (such as the return code,
#   stdout/stderr streams, SHA256 hash sum of the resulting file).
#   2) compares the collected information from two crossgen scenarios (e.g.
#   x86_arm vs. arm_arm) and report all the differences in their behaviour
#   (such as mismatches in the resulting files; hash sums, or missing files).
#
# Example:
#
# The following command
#
#  ~/git/coreclr$ python tests/scripts/crossgen_comparison.py crossgen_corelib
#  --crossgen bin/Product/Linux.arm.Checked/crossgen
#  --il_corelib bin/Product/Linux.arm.Checked/IL/System.Private.CoreLib.dll
#  --result_dir Linux.arm_arm.Checked
#
# runs Hostarm/arm crossgen on System.Private.CoreLib.dll and puts all the
# information in file Linux.arm_arm.Checked/System.Private.CoreLib.dll.json
#
#  ~/git/coreclr$ cat Linux.arm_arm.Checked/System.Private.CoreLib.dll.json
#   {
#     "AssemblyName": "System.Private.CoreLib.dll",
#     "ReturnCode": 0,
#     "OutputFileHash": "4d27c7f694c20974945e4f7cb43263286a18c56f4d00aac09f6124caa372ba0a",
#     "StdErr": [],
#     "StdOut": [
#       "Native image /tmp/System.Private.CoreLib.dll generated successfully."
#     ]
#   }
#
# The following command
#
#  ~/git/coreclr$ python tests/scripts/crossgen_comparison.py crossgen_dotnet_sdk
#  --crossgen bin/Product/Linux.arm.Checked/x64/crossgen
#  --il_corelib bin/Product/Linux.arm.Checked/IL/System.Private.CoreLib.dll
#  --dotnet_sdk dotnet-sdk-latest-linux-arm.tar.gz
#  --result_dir Linux.x64_arm.Checked
#
#  runs Hostx64/arm crossgen on System.Private.CoreLib.dll in bin/Product and on
#  all the assemblies inside dotnet-sdk-latest-linux-arm.tar.gz and stores the
#  collected information in directory Linux.x64_arm.Checked
#
#  ~/git/coreclr$ ls Linux.x64_arm.Checked | head
#   Microsoft.AI.DependencyCollector.dll.json
#   Microsoft.ApplicationInsights.AspNetCore.dll.json
#   Microsoft.ApplicationInsights.dll.json
#   Microsoft.AspNetCore.Antiforgery.dll.json
#   Microsoft.AspNetCore.ApplicationInsights.HostingStartup.dll.json
#   Microsoft.AspNetCore.Authentication.Abstractions.dll.json
#   Microsoft.AspNetCore.Authentication.Cookies.dll.json
#   Microsoft.AspNetCore.Authentication.Core.dll.json
#   Microsoft.AspNetCore.Authentication.dll.json
#   Microsoft.AspNetCore.Authentication.Facebook.dll.json
#
# The following command
#
#  ~/git/coreclr$ python -u tests/scripts/crossgen_comparison.py compare
#  --base_dir Linux.x64_arm.Checked
#  --diff_dir Linux.x86_arm.Checked
#
# compares the results of Hostx64/arm crossgen and Hostx86/arm crossgen.
################################################################################
################################################################################

import argparse
import glob
import json
import hashlib
import os
import tarfile
import tempfile
import re
import sys
import subprocess

################################################################################
# Argument Parser
################################################################################

def build_argument_parser():
    description = """Script that runs crossgen on different assemblies and
        collects/compares information about the crossgen behaviour."""

    parser = argparse.ArgumentParser(description=description)

    subparsers = parser.add_subparsers()

    crossgen_corelib_description = """Runs crossgen on IL System.Private.CoreLib.dll and
        collects information about the run."""

    corelib_parser = subparsers.add_parser('crossgen_corelib', description=crossgen_corelib_description)
    corelib_parser.add_argument('--crossgen', dest='crossgen_executable_filename', required=True)
    corelib_parser.add_argument('--il_corelib', dest='il_corelib_filename', required=True)
    corelib_parser.add_argument('--result_dir', dest='result_dirname', required=True)
    corelib_parser.set_defaults(func=crossgen_corelib)

    frameworks_parser_description = """Runs crossgen on each assembly in Core_Root and 
        collects information about all the runs."""

    frameworks_parser = subparsers.add_parser('crossgen_framework', description=frameworks_parser_description)
    frameworks_parser.add_argument('--crossgen', dest='crossgen_executable_filename', required=True)
    frameworks_parser.add_argument('--core_root', dest='core_root', required=True)
    frameworks_parser.add_argument('--result_dir', dest='result_dirname', required=True)
    frameworks_parser.set_defaults(func=crossgen_framework)

    dotnet_sdk_parser_description = "Unpack .NET Core SDK archive file and runs crossgen on each assembly."
    dotnet_sdk_parser = subparsers.add_parser('crossgen_dotnet_sdk', description=dotnet_sdk_parser_description)
    dotnet_sdk_parser.add_argument('--crossgen', dest='crossgen_executable_filename', required=True)
    dotnet_sdk_parser.add_argument('--il_corelib', dest='il_corelib_filename', required=True)
    dotnet_sdk_parser.add_argument('--dotnet_sdk', dest='dotnet_sdk_filename', required=True)
    dotnet_sdk_parser.add_argument('--result_dir', dest='result_dirname', required=True)
    dotnet_sdk_parser.set_defaults(func=crossgen_dotnet_sdk)

    compare_parser_description = """Compares collected information from two crossgen scenarios - base vs diff"""

    compare_parser = subparsers.add_parser('compare', description=compare_parser_description)
    compare_parser.add_argument('--base_dir', dest='base_dirname', required=True)
    compare_parser.add_argument('--diff_dir', dest='diff_dirname', required=True)
    compare_parser.set_defaults(func=compare)

    return parser

################################################################################
# Globals
################################################################################

# List of framework assemblies used for crossgen_framework command
g_Framework_Assemblies = [
    'CommandLine.dll',
    'Microsoft.CodeAnalysis.CSharp.dll',
    'Microsoft.CodeAnalysis.dll',
    'Microsoft.CodeAnalysis.VisualBasic.dll',
    'Microsoft.CSharp.dll',
    'Microsoft.Diagnostics.FastSerialization.dll',
    'Microsoft.Diagnostics.Tracing.TraceEvent.dll',
    'Microsoft.DotNet.Cli.Utils.dll',
    'Microsoft.DotNet.InternalAbstractions.dll',
    'Microsoft.DotNet.ProjectModel.dll',
    'Microsoft.Extensions.DependencyModel.dll',
    'Microsoft.VisualBasic.dll',
    'Microsoft.Win32.Primitives.dll',
    'Microsoft.Win32.Registry.dll',
    'netstandard.dll',
    'Newtonsoft.Json.dll',
    'NuGet.Common.dll',
    'NuGet.Configuration.dll',
    'NuGet.DependencyResolver.Core.dll',
    'NuGet.Frameworks.dll',
    'NuGet.LibraryModel.dll',
    'NuGet.Packaging.Core.dll',
    'NuGet.Packaging.Core.Types.dll',
    'NuGet.Packaging.dll',
    'NuGet.ProjectModel.dll',
    'NuGet.Protocol.Core.Types.dll',
    'NuGet.Protocol.Core.v3.dll',
    'NuGet.Repositories.dll',
    'NuGet.RuntimeModel.dll',
    'NuGet.Versioning.dll',
    'System.AppContext.dll',
    'System.Buffers.dll',
    'System.Collections.Concurrent.dll',
    'System.Collections.dll',
    'System.Collections.Immutable.dll',
    'System.Collections.NonGeneric.dll',
    'System.Collections.Specialized.dll',
    'System.CommandLine.dll',
    'System.ComponentModel.Annotations.dll',
    'System.ComponentModel.DataAnnotations.dll',
    'System.ComponentModel.dll',
    'System.ComponentModel.EventBasedAsync.dll',
    'System.ComponentModel.Primitives.dll',
    'System.ComponentModel.TypeConverter.dll',
    'System.Configuration.dll',
    'System.Console.dll',
    'System.Core.dll',
    'System.Data.Common.dll',
    'System.Data.dll',
    'System.Diagnostics.Contracts.dll',
    'System.Diagnostics.Debug.dll',
    'System.Diagnostics.DiagnosticSource.dll',
    'System.Diagnostics.EventLog.dll',
    'System.Diagnostics.FileVersionInfo.dll',
    'System.Diagnostics.Process.dll',
    'System.Diagnostics.StackTrace.dll',
    'System.Diagnostics.TextWriterTraceListener.dll',
    'System.Diagnostics.Tools.dll',
    'System.Diagnostics.TraceSource.dll',
    'System.Diagnostics.Tracing.dll',
    'System.dll',
    'System.Drawing.dll',
    'System.Drawing.Primitives.dll',
    'System.Dynamic.Runtime.dll',
    'System.Globalization.Calendars.dll',
    'System.Globalization.dll',
    'System.Globalization.Extensions.dll',
    'System.IO.Compression.Brotli.dll',
    'System.IO.Compression.dll',
    'System.IO.Compression.FileSystem.dll',
    'System.IO.Compression.ZipFile.dll',
    'System.IO.dll',
    'System.IO.FileSystem.AccessControl.dll',
    'System.IO.FileSystem.dll',
    'System.IO.FileSystem.DriveInfo.dll',
    'System.IO.FileSystem.Primitives.dll',
    'System.IO.FileSystem.Watcher.dll',
    'System.IO.IsolatedStorage.dll',
    'System.IO.MemoryMappedFiles.dll',
    'System.IO.Pipes.AccessControl.dll',
    'System.IO.Pipes.dll',
    'System.IO.UnmanagedMemoryStream.dll',
    'System.Linq.dll',
    'System.Linq.Expressions.dll',
    'System.Linq.Parallel.dll',
    'System.Linq.Queryable.dll',
    'System.Memory.dll',
    'System.Net.dll',
    'System.Net.Http.dll',
    'System.Net.HttpListener.dll',
    'System.Net.Mail.dll',
    'System.Net.NameResolution.dll',
    'System.Net.NetworkInformation.dll',
    'System.Net.Ping.dll',
    'System.Net.Primitives.dll',
    'System.Net.Requests.dll',
    'System.Net.Security.dll',
    'System.Net.ServicePoint.dll',
    'System.Net.Sockets.dll',
    'System.Net.WebClient.dll',
    'System.Net.WebHeaderCollection.dll',
    'System.Net.WebProxy.dll',
    'System.Net.WebSockets.Client.dll',
    'System.Net.WebSockets.dll',
    'System.Numerics.dll',
    'System.Numerics.Vectors.dll',
    'System.ObjectModel.dll',
    'System.Private.DataContractSerialization.dll',
    'System.Private.Uri.dll',
    'System.Private.Xml.dll',
    'System.Private.Xml.Linq.dll',
    'System.Reflection.DispatchProxy.dll',
    'System.Reflection.dll',
    'System.Reflection.Emit.dll',
    'System.Reflection.Emit.ILGeneration.dll',
    'System.Reflection.Emit.Lightweight.dll',
    'System.Reflection.Extensions.dll',
    'System.Reflection.Metadata.dll',
    'System.Reflection.Primitives.dll',
    'System.Reflection.TypeExtensions.dll',
    'System.Resources.Reader.dll',
    'System.Resources.ResourceManager.dll',
    'System.Resources.Writer.dll',
    'System.Runtime.CompilerServices.Unsafe.dll',
    'System.Runtime.CompilerServices.VisualC.dll',
    'System.Runtime.dll',
    'System.Runtime.Extensions.dll',
    'System.Runtime.Handles.dll',
    'System.Runtime.InteropServices.dll',
    'System.Runtime.InteropServices.RuntimeInformation.dll',
    'System.Runtime.InteropServices.WindowsRuntime.dll',
    'System.Runtime.Loader.dll',
    'System.Runtime.Numerics.dll',
    'System.Runtime.Serialization.dll',
    'System.Runtime.Serialization.Formatters.dll',
    'System.Runtime.Serialization.Json.dll',
    'System.Runtime.Serialization.Primitives.dll',
    'System.Runtime.Serialization.Xml.dll',
    'System.Security.AccessControl.dll',
    'System.Security.Claims.dll',
    'System.Security.Cryptography.Algorithms.dll',
    'System.Security.Cryptography.Cng.dll',
    'System.Security.Cryptography.Csp.dll',
    'System.Security.Cryptography.Encoding.dll',
    'System.Security.Cryptography.OpenSsl.dll',
    'System.Security.Cryptography.Primitives.dll',
    'System.Security.Cryptography.X509Certificates.dll',
    'System.Security.dll',
    'System.Security.Permissions.dll',
    'System.Security.Principal.dll',
    'System.Security.Principal.Windows.dll',
    'System.Security.SecureString.dll',
    'System.ServiceModel.Web.dll',
    'System.ServiceProcess.dll',
    'System.Text.Encoding.CodePages.dll',
    'System.Text.Encoding.dll',
    'System.Text.Encoding.Extensions.dll',
    'System.Text.RegularExpressions.dll',
    'System.Threading.AccessControl.dll',
    'System.Threading.dll',
    'System.Threading.Overlapped.dll',
    'System.Threading.Tasks.Dataflow.dll',
    'System.Threading.Tasks.dll',
    'System.Threading.Tasks.Extensions.dll',
    'System.Threading.Tasks.Parallel.dll',
    'System.Threading.Thread.dll',
    'System.Threading.ThreadPool.dll',
    'System.Threading.Timer.dll',
    'System.Transactions.dll',
    'System.Transactions.Local.dll',
    'System.ValueTuple.dll',
    'System.Web.dll',
    'System.Web.HttpUtility.dll',
    'System.Windows.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.Xml.ReaderWriter.dll',
    'System.Xml.Serialization.dll',
    'System.Xml.XDocument.dll',
    'System.Xml.XmlDocument.dll',
    'System.Xml.XmlSerializer.dll',
    'System.Xml.XPath.dll',
    'System.Xml.XPath.XDocument.dll',
    'TraceReloggerLib.dll',
    'WindowsBase.dll']

class CrossGenRunner:
    def __init__(self, crossgen_executable_filename, no_logo=True):
        self.crossgen_executable_filename = crossgen_executable_filename
        self.no_logo = no_logo
        self.platform_assemblies_paths_sep = ';' if sys.platform == 'win32' else ':'

    """
        Creates a subprocess running crossgen with specified set of arguments, 
        communicates with the owner process - waits for its termination and pulls 
        returncode, stdour, stderr.
    """
    def run(self, in_filename, out_filename, platform_assemblies_paths):
        p = subprocess.Popen(self._build_args(in_filename, out_filename, platform_assemblies_paths), stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        stdout, stderr = p.communicate()
        return (p.returncode, stdout.decode(), stderr.decode())

    def _build_args(self, in_filename, out_filename, platform_assemblies_paths):
        args = []
        args.append(self.crossgen_executable_filename)
        if self.no_logo:
           args.append('/nologo')
        args.append('/Platform_Assemblies_Paths')
        args.append(self.platform_assemblies_paths_sep.join(platform_assemblies_paths))
        args.append('/out')
        args.append(out_filename)
        args.append(in_filename)
        return args

def compute_file_hashsum(filename):
    """
        Compute SHA256 file hashsum for {filename}.
    """
    algo=hashlib.sha256()
    maximum_block_size_in_bytes = 65536
    with open(filename, 'rb') as file:
        while True:
            block = file.read(maximum_block_size_in_bytes)
            if block:
                algo.update(block)
            else:
                break
    return algo.hexdigest()


################################################################################
# This describes collected during crossgen information.
################################################################################
class CrossGenResult:
    def __init__(self, assembly_name, returncode, stdout, stderr, out_file_hashsum, out_file_size_in_bytes):
        self.assembly_name = assembly_name
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr
        self.out_file_hashsum = out_file_hashsum
        self.out_file_size_in_bytes = out_file_size_in_bytes

################################################################################
# JSON Encoder for CrossGenResult objects.
################################################################################
class CrossGenResultEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, CrossGenResult):
            return {
                'AssemblyName': obj.assembly_name,
                'ReturnCode': obj.returncode,
                'StdOut': obj.stdout.splitlines(),
                'StdErr': obj.stderr.splitlines(),
                'OutputFileHash': obj.out_file_hashsum,
                'OutputFileSizeInBytes': obj.out_file_size_in_bytes }
        # Let the base class default method raise the TypeError
        return json.JSONEncoder.default(self, obj)

################################################################################
# JSON Decoder for CrossGenResult objects.
################################################################################
class CrossGenResultDecoder(json.JSONDecoder):
    def __init__(self, *args, **kwargs):
        json.JSONDecoder.__init__(self, object_hook=self._decode_object, *args, **kwargs)
    def _decode_object(self, dict):
        try:
            assembly_name = dict['AssemblyName']
            returncode = dict['ReturnCode']
            stdout = dict['StdOut']
            stderr = dict['StdErr']
            out_file_hashsum = dict['OutputFileHash']
            out_file_size_in_bytes = dict['OutputFileSizeInBytes']
            return CrossGenResult(assembly_name, returncode, stdout, stderr, out_file_hashsum, out_file_size_in_bytes)
        except KeyError:
            return dict


################################################################################
# Helper Functions
################################################################################

def crossgen_assembly(crossgen_executable_filename, in_filename, out_filename, platform_assemblies_paths):
    runner = CrossGenRunner(crossgen_executable_filename)
    returncode, stdout, stderr = runner.run(in_filename, out_filename, platform_assemblies_paths)
    assembly_name = os.path.basename(in_filename)
    out_file_hashsum = compute_file_hashsum(out_filename) if returncode == 0 else None
    ouf_file_size_in_bytes = os.path.getsize(out_filename) if returncode == 0 else None
    return CrossGenResult(assembly_name, returncode, stdout, stderr, out_file_hashsum, ouf_file_size_in_bytes)

def save_crossgen_result_to_json_file(crossgen_result, json_filename):
    with open(json_filename, 'wt') as json_file:
        json.dump(crossgen_result, json_file, cls=CrossGenResultEncoder, indent=2)

def crossgen_corelib(args):
    il_corelib_filename = args.il_corelib_filename
    ni_corelib_filename = os.path.join(tempfile.gettempdir(), os.path.basename(il_corelib_filename))
    crossgen_result = crossgen_assembly(args.crossgen_executable_filename, il_corelib_filename, ni_corelib_filename, [os.path.dirname(il_corelib_filename)])
    result_filename = os.path.join(args.result_dirname, crossgen_result.assembly_name + '.json')
    save_crossgen_result_to_json_file(crossgen_result, result_filename)

def add_ni_extension(filename):
    filename,ext = os.path.splitext(filename)
    return filename + '.ni' + ext

def crossgen_framework(args):
    global g_Framework_Assemblies
    platform_assemblies_paths = [args.core_root]
    for assembly_name in g_Framework_Assemblies:
        il_filename = os.path.join(args.core_root, assembly_name)
        ni_filename = os.path.join(tempfile.gettempdir(), add_ni_extension(assembly_name))
        crossgen_result = crossgen_assembly(args.crossgen_executable_filename, il_filename, ni_filename, platform_assemblies_paths)
        result_filename = os.path.join(args.result_dirname, crossgen_result.assembly_name + '.json')
        save_crossgen_result_to_json_file(crossgen_result, result_filename)

def load_crossgen_result_from_json_file(json_filename):
    with open(json_filename, 'rt') as json_file:
        return json.load(json_file, cls=CrossGenResultDecoder)

def load_crossgen_results_from_dir(dirname):
    crossgen_results = []
    for filename in glob.glob(os.path.join(dirname, '*.json')):
        loaded_result = load_crossgen_result_from_json_file(filename)
        crossgen_results.append(loaded_result)
    return crossgen_results

def dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
    for dirpath, _, filenames in os.walk(dotnet_sdk_dirname):
        dirname = os.path.dirname(dirpath)
        if dirname.endswith('Microsoft.NETCore.App') or dirname.endswith('Microsoft.AspNetCore.App') or dirname.endswith('Microsoft.AspNetCore.All'):
            filenames = filter(lambda filename: not re.match(r'^(Microsoft|System)\..*dll$', filename) is None, filenames)
            filenames = filter(lambda filename: filename != 'System.Private.CoreLib.dll', filenames)
            yield (dirpath, filenames)

def crossgen_dotnet_sdk(args):
    dotnet_sdk_dirname = tempfile.mkdtemp()
    with tarfile.open(args.dotnet_sdk_filename) as dotnet_sdk_tarfile:
        dotnet_sdk_tarfile.extractall(dotnet_sdk_dirname)

    ni_files_dirname = tempfile.mkdtemp()
    crossgen_results = []

    il_corelib_filename = args.il_corelib_filename
    ni_corelib_filename = os.path.join(ni_files_dirname, os.path.basename(il_corelib_filename))
    corelib_result = crossgen_assembly(args.crossgen_executable_filename, il_corelib_filename, ni_corelib_filename, [os.path.dirname(il_corelib_filename)])
    crossgen_results.append(corelib_result)

    platform_assemblies_paths = [ni_files_dirname]

    for il_files_dirname, _ in dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
        platform_assemblies_paths.append(il_files_dirname)

    for il_files_dirname, assembly_names in dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
        for assembly_name in assembly_names:
            il_filename = os.path.join(il_files_dirname, assembly_name)
            ni_filename = os.path.join(ni_files_dirname, add_ni_extension(assembly_name))
            result = crossgen_assembly(args.crossgen_executable_filename, il_filename, ni_filename, platform_assemblies_paths)
            crossgen_results.append(result)

    for result in crossgen_results:
        result_filename = os.path.join(args.result_dirname, result.assembly_name + '.json')
        save_crossgen_result_to_json_file(result, result_filename)

def print_omitted_assemblies_message(omitted_assemblies, dirname):
    print('The information for the following assemblies was omitted from "{0}" directory:'.format(dirname))
    for assembly_name in sorted(omitted_assemblies):
        print(' - ' + assembly_name)

def print_compare_result_message_helper(message_header, base_value, diff_value, base_dirname, diff_dirname):
    assert base_value != diff_value
    print(message_header)
    print(' - "{0}" has "{1}"'.format(base_dirname, base_value))
    print(' - "{0}" has "{1}"'.format(diff_dirname, diff_value))

def compare_and_print_message(base_result, diff_result, base_dirname, diff_dirname):
    base_diff_are_equal = True

    assert base_result.assembly_name == diff_result.assembly_name
    if base_result.returncode != diff_result.returncode:
        base_diff_are_equal = False
        print_compare_result_message_helper('Return code mismatch for "{0}" assembly:'.format(base_result.assembly_name), base_result.returncode, diff_result.returncode, base_dirname, diff_dirname)
    elif base_result.returncode == 0 and diff_result.returncode == 0:
        assert not base_result.out_file_hashsum is None
        assert not base_result.out_file_size_in_bytes is None

        assert not diff_result.out_file_hashsum is None
        assert not diff_result.out_file_size_in_bytes is None

        if base_result.out_file_hashsum != diff_result.out_file_hashsum:
            base_diff_are_equal = False
            print_compare_result_message_helper('Native image hash sum mismatch for "{0}" assembly:'.format(base_result.assembly_name), base_result.out_file_hashsum, diff_result.out_file_hashsum, base_dirname, diff_dirname)

        if base_result.out_file_size_in_bytes != diff_result.out_file_size_in_bytes:
            base_diff_are_equal = False
            print_compare_result_message_helper('Native image size mismatch for "{0}" assembly:'.format(base_result.assembly_name), base_result.out_file_size_in_bytes, diff_result.out_file_size_in_bytes, base_dirname, diff_dirname)

    return base_diff_are_equal

def compare(args):
    print('Comparing crossgen results in "{0}" and "{1}" directories'.format(args.base_dirname, args.diff_dirname))

    base_results = load_crossgen_results_from_dir(args.base_dirname)
    diff_results = load_crossgen_results_from_dir(args.diff_dirname)

    base_assemblies = { r.assembly_name for r in base_results }
    diff_assemblies = { r.assembly_name for r in diff_results }
    both_assemblies = base_assemblies & diff_assemblies

    # We want to see whether {base} and {diff} crossgens are "equal":
    #  1. their return codes are the same;
    #  2. their binary outputs (native images) are the same.
    num_omitted_results = 0

    omitted_from_base_dir = diff_assemblies - base_assemblies
    omitted_from_diff_dir = base_assemblies - diff_assemblies

    if len(omitted_from_base_dir) != 0:
        num_omitted_results += len(omitted_from_base_dir)
        print_omitted_assemblies_message(omitted_from_base_dir, args.base_dirname)

    if len(omitted_from_diff_dir) != 0:
        num_omitted_results += len(omitted_from_diff_dir)
        print_omitted_assemblies_message(omitted_from_diff_dir, args.diff_dirname)

    base_results_by_name = dict((r.assembly_name, r) for r in base_results)
    diff_results_by_name = dict((r.assembly_name, r) for r in diff_results)

    num_mismatched_results = 0

    for assembly_name in sorted(both_assemblies):
        base_result = base_results_by_name[assembly_name]
        diff_result = diff_results_by_name[assembly_name]
        if not compare_and_print_message(base_result, diff_result, args.base_dirname, args.diff_dirname):
            num_mismatched_results += 1

    print("Number of omitted results: {0}".format(num_omitted_results))
    print("Number of mismatched results: {0}".format(num_mismatched_results))
    print("Total number of assemblies: {0}".format(len(both_assemblies)))
    sys.exit(0 if (num_mismatched_results + num_omitted_results) == 0 else 1)

################################################################################
# __main__
################################################################################

if __name__ == '__main__':
    parser = build_argument_parser()
    args = parser.parse_args()
    func = args.func(args)
