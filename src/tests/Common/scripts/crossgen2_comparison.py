#!/usr/bin/env python
#
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
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
#  --crossgen artifacts/bin/coreclr/linux.arm.Checked/crossgen
#  --il_corelib artifacts/bin/coreclr/linux.arm.Checked/IL/System.Private.CoreLib.dll
#  --result_dir linux.arm_arm.Checked
#
# runs Hostarm/arm crossgen on System.Private.CoreLib.dll and puts all the
# information in files linux.arm_arm.Checked/System.Private.CoreLib-NativeOrReadyToRunImage.json
# and System.Private.CoreLib-DebuggingFile.json
#
#  ~/git/coreclr$ cat linux.arm_arm.Checked/System.Private.CoreLib-NativeOrReadyToRunImage.json
#  {
#    "AssemblyName": "System.Private.CoreLib",
#    "ReturnCode": 0,
#    "OutputFileHash": "4d27c7f694c20974945e4f7cb43263286a18c56f4d00aac09f6124caa372ba0a",
#    "StdErr": [],
#    "StdOut": [
#      "Native image /tmp/tmp9ZX7gl/System.Private.CoreLib.dll generated successfully."
#    ],
#    "OutputFileType": "NativeOrReadyToRunImage",
#    "OutputFileSizeInBytes": 9564160
#  }
#
#  ~/git/coreclr$ cat linux.x64_arm.Checked/System.Private.CoreLib-DebuggingFile.json
#  {
#    "ReturnCode": 0,
#    "StdOut": [
#      "Successfully generated perfmap for native assembly '/tmp/tmp9ZX7gl/System.Private.CoreLib.dll'."
#    ],
#    "OutputFileHash": "f4fff0d88193d3a1422f9f0806a6cea6ac6c1aab0499968c183cbb0755e1084b",
#    "OutputFileType": "DebuggingFile",
#    "StdErr": [],
#    "OutputFileSizeInBytes": 1827867,
#    "AssemblyName": "System.Private.CoreLib"
#  }
#
# The following command
#
#  ~/git/coreclr$ python tests/scripts/crossgen_comparison.py crossgen_dotnet_sdk
#  --crossgen artifacts/bin/coreclr/linux.arm.Checked/x64/crossgen
#  --il_corelib artifacts/bin/coreclr/linux.arm.Checked/IL/System.Private.CoreLib.dll
#  --dotnet_sdk dotnet-sdk-latest-linux-arm.tar.gz
#  --result_dir linux.x64_arm.Checked
#
#  runs Hostx64/arm crossgen on System.Private.CoreLib.dll in artifacts/Product and on
#  all the assemblies inside dotnet-sdk-latest-linux-arm.tar.gz and stores the
#  collected information in directory linux.x64_arm.Checked
#
#  ~/git/coreclr$ ls linux.x64_arm.Checked | head
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
#  --base_dir linux.x64_arm.Checked
#  --diff_dir linux.x86_arm.Checked
#
# compares the results of Hostx64/arm crossgen and Hostx86/arm crossgen.
################################################################################
################################################################################

import argparse
import datetime
import asyncio
import glob
import json
import hashlib
import multiprocessing
import os
import tarfile
import tempfile
import re
import shutil
import subprocess
import sys
from xml.dom import minidom

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

    framework_parser_description = """Runs crossgen on each assembly in Core_Root and
        collects information about all the runs."""

    framework_parser = subparsers.add_parser('crossgen_framework', description=framework_parser_description)
    framework_parser.add_argument('--dotnet', dest='dotnet', required=True)
    framework_parser.add_argument('--crossgen', dest='crossgen_executable_filename', required=True)
    framework_parser.add_argument('--target_os', dest='target_os', required=True)
    framework_parser.add_argument('--target_arch', dest='target_arch', required=True)
    framework_parser.add_argument('--core_root', dest='core_root', required=True)
    framework_parser.add_argument('--result_dir', dest='result_dirname', required=True)
    framework_parser.add_argument('--compiler_arch_os', dest='compiler_arch_os', required=True)
    framework_parser.set_defaults(func=crossgen_framework)


    dotnet_sdk_parser_description = "Unpack .NET SDK archive file and runs crossgen on each assembly."
    dotnet_sdk_parser = subparsers.add_parser('crossgen_dotnet_sdk', description=dotnet_sdk_parser_description)
    dotnet_sdk_parser.add_argument('--crossgen', dest='crossgen_executable_filename', required=True)
    dotnet_sdk_parser.add_argument('--il_corelib', dest='il_corelib_filename', required=True)
    dotnet_sdk_parser.add_argument('--dotnet_sdk', dest='dotnet_sdk_filename', required=True)
    dotnet_sdk_parser.add_argument('--result_dir', dest='result_dirname', required=True)
    dotnet_sdk_parser.set_defaults(func=crossgen_dotnet_sdk)

    compare_parser_description = "Compares crossgen results in directories {base_dir} and {diff_dir}"

    compare_parser = subparsers.add_parser('compare', description=compare_parser_description)
    compare_parser.add_argument('--base_dir', dest='base_dirname', required=True)
    compare_parser.add_argument('--diff_dir', dest='diff_dirname', required=True)
    compare_parser.add_argument('--testresults', dest='testresultsxml', required=True)
    compare_parser.add_argument('--target_arch_os', dest='target_arch_os', required=True)
    compare_parser.set_defaults(func=compare_results)

    return parser

################################################################################
# Helper class
################################################################################

class AsyncSubprocessHelper:
    def __init__(self, items, subproc_count=multiprocessing.cpu_count(), verbose=False):
        item_queue = asyncio.Queue()
        for item in items:
            item_queue.put_nowait(item)

        self.items = items
        self.subproc_count = subproc_count
        self.verbose = verbose

        if 'win32' in sys.platform:
            # Windows specific event-loop policy & cmd
            asyncio.set_event_loop_policy(asyncio.WindowsProactorEventLoopPolicy())

    async def __get_item__(self, item, index, size, async_callback, *extra_args):
        """ Wrapper to the async callback which will schedule based on the queue
        """

        # Wait for the queue to become free. Then start
        # running the sub process.
        subproc_id = await self.subproc_count_queue.get()

        print_prefix = ""

        if self.verbose:
            print_prefix = "[{}:{}]: ".format(index, size)

        await async_callback(print_prefix, item, *extra_args)

        # Add back to the queue, incase another process wants to run.
        self.subproc_count_queue.put_nowait(subproc_id)

    async def __run_to_completion__(self, async_callback, *extra_args):
        """ async wrapper for run_to_completion
        """

        chunk_size = self.subproc_count

        # Create a queue with a chunk size of the cpu count
        #
        # Each run_crossgen invocation will remove an item from the
        # queue before running a potentially long running pmi run.
        #
        # When the queue is drained, we will wait queue.get which
        # will wait for when a run_crossgen instance has added back to the
        subproc_count_queue = asyncio.Queue(chunk_size)
        diff_queue = asyncio.Queue()

        for item in self.items:
            diff_queue.put_nowait(item)

        for item in range(chunk_size):
            subproc_count_queue.put_nowait(item)

        self.subproc_count_queue = subproc_count_queue
        tasks = []
        size = diff_queue.qsize()

        count = 1
        item = diff_queue.get_nowait() if not diff_queue.empty() else None
        while item is not None:
            tasks.append(self.__get_item__(item, count, size, async_callback, *extra_args))
            count += 1

            item = diff_queue.get_nowait() if not diff_queue.empty() else None

        await asyncio.gather(*tasks)

    def run_to_completion(self, async_callback, *extra_args):
        """ Run until the item queue has been depleted

             Notes:
            Acts as a wrapper to abstract the async calls to
            async_callback. Note that this will allow cpu_count
            amount of running subprocesses. Each time the queue
            is emptied, another process will start. Note that
            the python code is single threaded, it will just
            rely on async/await to start subprocesses at
            subprocess_count
        """

        reset_env = os.environ.copy()

        loop = asyncio.get_event_loop()
        loop.run_until_complete(self.__run_to_completion__(async_callback, *extra_args))
        loop.close()

        os.environ.update(reset_env)

################################################################################
# Globals
################################################################################

g_frameworkcompile_failed = False

# List of framework assemblies used for crossgen_framework command
g_Framework_Assemblies = [
    'Microsoft.Bcl.AsyncInterfaces.dll',
    'Microsoft.CSharp.dll',
    'Microsoft.Extensions.Caching.Abstractions.dll',
    'Microsoft.Extensions.Caching.Memory.dll',
    'Microsoft.Extensions.Configuration.Abstractions.dll',
    'Microsoft.Extensions.Configuration.Binder.dll',
    'Microsoft.Extensions.Configuration.CommandLine.dll',
    'Microsoft.Extensions.Configuration.dll',
    'Microsoft.Extensions.Configuration.EnvironmentVariables.dll',
    'Microsoft.Extensions.Configuration.FileExtensions.dll',
    'Microsoft.Extensions.Configuration.Ini.dll',
    'Microsoft.Extensions.Configuration.Json.dll',
    'Microsoft.Extensions.Configuration.UserSecrets.dll',
    'Microsoft.Extensions.Configuration.Xml.dll',
    'Microsoft.Extensions.DependencyInjection.Abstractions.dll',
    'Microsoft.Extensions.DependencyInjection.dll',
    'Microsoft.Extensions.DependencyModel.dll',
    'Microsoft.Extensions.FileProviders.Abstractions.dll',
    'Microsoft.Extensions.FileProviders.Composite.dll',
    'Microsoft.Extensions.FileProviders.Physical.dll',
    'Microsoft.Extensions.FileSystemGlobbing.dll',
    'Microsoft.Extensions.Hosting.Abstractions.dll',
    'Microsoft.Extensions.Hosting.dll',
    'Microsoft.Extensions.Http.dll',
    'Microsoft.Extensions.Logging.Abstractions.dll',
    'Microsoft.Extensions.Logging.Configuration.dll',
    'Microsoft.Extensions.Logging.Console.dll',
    'Microsoft.Extensions.Logging.Debug.dll',
    'Microsoft.Extensions.Logging.dll',
    'Microsoft.Extensions.Logging.EventLog.dll',
    'Microsoft.Extensions.Logging.EventSource.dll',
    'Microsoft.Extensions.Logging.TraceSource.dll',
    'Microsoft.Extensions.Options.ConfigurationExtensions.dll',
    'Microsoft.Extensions.Options.DataAnnotations.dll',
    'Microsoft.Extensions.Options.dll',
    'Microsoft.Extensions.Primitives.dll',
    'Microsoft.VisualBasic.Core.dll',
    'Microsoft.VisualBasic.dll',
    'Microsoft.Win32.Primitives.dll',
    'Microsoft.Win32.Registry.AccessControl.dll',
    'Microsoft.Win32.Registry.dll',
    'Microsoft.Win32.SystemEvents.dll',
    'mscorlib.dll',
    'netstandard.dll',
    'System.AppContext.dll',
    'System.Buffers.dll',
    'System.CodeDom.dll',
    'System.Collections.Concurrent.dll',
    'System.Collections.dll',
    'System.Collections.Immutable.dll',
    'System.Collections.NonGeneric.dll',
    'System.Collections.Specialized.dll',
    'System.ComponentModel.Annotations.dll',
    'System.ComponentModel.Composition.dll',
    'System.ComponentModel.Composition.Registration.dll',
    'System.ComponentModel.DataAnnotations.dll',
    'System.ComponentModel.dll',
    'System.ComponentModel.EventBasedAsync.dll',
    'System.ComponentModel.Primitives.dll',
    'System.ComponentModel.TypeConverter.dll',
    'System.Composition.AttributedModel.dll',
    'System.Composition.Convention.dll',
    'System.Composition.Hosting.dll',
    'System.Composition.Runtime.dll',
    'System.Composition.TypedParts.dll',
    'System.Configuration.ConfigurationManager.dll',
    'System.Configuration.dll',
    'System.Console.dll',
    'System.Core.dll',
    'System.Data.Common.dll',
    'System.Data.DataSetExtensions.dll',
    'System.Data.dll',
    'System.Data.Odbc.dll',
    'System.Data.OleDb.dll',
    'System.Diagnostics.Contracts.dll',
    'System.Diagnostics.Debug.dll',
    'System.Diagnostics.DiagnosticSource.dll',
    'System.Diagnostics.EventLog.dll',
    'System.Diagnostics.FileVersionInfo.dll',
    'System.Diagnostics.PerformanceCounter.dll',
    'System.Diagnostics.Process.dll',
    'System.Diagnostics.StackTrace.dll',
    'System.Diagnostics.TextWriterTraceListener.dll',
    'System.Diagnostics.Tools.dll',
    'System.Diagnostics.TraceSource.dll',
    'System.Diagnostics.Tracing.dll',
    'System.DirectoryServices.AccountManagement.dll',
    'System.DirectoryServices.dll',
    'System.DirectoryServices.Protocols.dll',
    'System.dll',
    'System.Drawing.dll',
    'System.Drawing.Primitives.dll',
    'System.Dynamic.Runtime.dll',
    'System.Formats.Asn1.dll',
    'System.Formats.Cbor.dll',
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
    'System.IO.Packaging.dll',
    'System.IO.Pipelines.dll',
    'System.IO.Pipes.AccessControl.dll',
    'System.IO.Pipes.dll',
    'System.IO.Ports.dll',
    'System.IO.UnmanagedMemoryStream.dll',
    'System.Linq.dll',
    'System.Linq.Expressions.dll',
    'System.Linq.Parallel.dll',
    'System.Linq.Queryable.dll',
    'System.Management.dll',
    'System.Memory.dll',
    'System.Net.dll',
    'System.Net.Http.dll',
    'System.Net.Http.Json.dll',
    'System.Net.Http.WinHttpHandler.dll',
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
    'System.Numerics.Tensors.dll',
    'System.Numerics.Vectors.dll',
    'System.ObjectModel.dll',
    'System.Private.CoreLib.dll',
    'System.Private.DataContractSerialization.dll',
    'System.Private.Uri.dll',
    'System.Private.Xml.dll',
    'System.Private.Xml.Linq.dll',
    'System.Reflection.Context.dll',
    'System.Reflection.DispatchProxy.dll',
    'System.Reflection.dll',
    'System.Reflection.Emit.dll',
    'System.Reflection.Emit.ILGeneration.dll',
    'System.Reflection.Emit.Lightweight.dll',
    'System.Reflection.Extensions.dll',
    'System.Reflection.Metadata.dll',
    'System.Reflection.MetadataLoadContext.dll',
    'System.Reflection.Primitives.dll',
    'System.Reflection.TypeExtensions.dll',
    'System.Resources.Extensions.dll',
    'System.Resources.Reader.dll',
    'System.Resources.ResourceManager.dll',
    'System.Resources.Writer.dll',
    'System.Runtime.Caching.dll',
    'System.Runtime.CompilerServices.Unsafe.dll',
    'System.Runtime.CompilerServices.VisualC.dll',
    'System.Runtime.dll',
    'System.Runtime.Extensions.dll',
    'System.Runtime.Handles.dll',
    'System.Runtime.InteropServices.dll',
    'System.Runtime.InteropServices.RuntimeInformation.dll',
    'System.Runtime.Intrinsics.dll',
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
    'System.Security.Cryptography.Pkcs.dll',
    'System.Security.Cryptography.Primitives.dll',
    'System.Security.Cryptography.ProtectedData.dll',
    'System.Security.Cryptography.X509Certificates.dll',
    'System.Security.Cryptography.Xml.dll',
    'System.Security.dll',
    'System.Security.Permissions.dll',
    'System.Security.Principal.dll',
    'System.Security.Principal.Windows.dll',
    'System.Security.SecureString.dll',
    'System.ServiceModel.Syndication.dll',
    'System.ServiceModel.Web.dll',
    'System.ServiceProcess.dll',
    'System.ServiceProcess.ServiceController.dll',
    'System.Text.Encoding.CodePages.dll',
    'System.Text.Encoding.dll',
    'System.Text.Encoding.Extensions.dll',
    'System.Text.Encodings.Web.dll',
    'System.Text.Json.dll',
    'System.Text.RegularExpressions.dll',
    'System.Threading.AccessControl.dll',
    'System.Threading.Channels.dll',
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
    'System.Windows.Extensions.dll',
    'System.Xml.dll',
    'System.Xml.Linq.dll',
    'System.Xml.ReaderWriter.dll',
    'System.Xml.Serialization.dll',
    'System.Xml.XDocument.dll',
    'System.Xml.XmlDocument.dll',
    'System.Xml.XmlSerializer.dll',
    'System.Xml.XPath.dll',
    'System.Xml.XPath.XDocument.dll',
    'WindowsBase.dll']

class CrossGenRunner:
    def __init__(self, dotnet, crossgen_executable_filename):
        self.dotnet = dotnet
        self.crossgen_executable_filename = crossgen_executable_filename
        self.platform_directory_sep = '\\' if sys.platform == 'win32' else '/'

    async def crossgen_il_file(self, il_filename, ni_filename, platform_assemblies_paths, target_os, target_arch):
        """
            Runs a subprocess "{crossgen_executable_filename} /nologo /Platform_Assemblies_Paths <path[:path]> /out {ni_filename} /in {il_filename}"
            and returns returncode, stdour, stderr.
        """
        args = self._build_args_crossgen_il_file(il_filename, ni_filename, platform_assemblies_paths, target_os, target_arch)
        return await self._run_with_args(args)

    async def create_debugging_file(self, ni_filename, debugging_files_dirname, platform_assemblies_paths):
        """
            Runs a subprocess "{crossgen_executable_filename} /nologo /Platform_Assemblies_Paths <path[:path]> /CreatePerfMap {debugging_files_dirname} /in {il_filename}" on Unix
            or "{crossgen_executable_filename} /nologo /Platform_Assemblies_Paths <path[:path]> /CreatePdb {debugging_files_dirname} /in {il_filename}" on Windows
            and returns returncode, stdout, stderr.
        """
        args = self._build_args_create_debugging_file(ni_filename, debugging_files_dirname, platform_assemblies_paths)
        return await self._run_with_args(args)

    def _build_args_crossgen_il_file(self, il_filename, ni_filename, platform_assemblies_paths, target_os, target_arch):
        args = []
        args.append(self.dotnet)
        args.append(self.crossgen_executable_filename)
        args.append('-r')
        args.append('"' + platform_assemblies_paths + self.platform_directory_sep + '*.dll"' )
        args.append('-O')
        args.append('--out')
        args.append(ni_filename)
        args.append('--targetos ')
        args.append(target_os)
        args.append('--targetarch ')
        args.append(target_arch)
        args.append(il_filename)
        return args

    def _build_args_create_debugging_file(self, ni_filename, debugging_files_dirname, platform_assemblies_paths):
        args = []
        args.append(self.crossgen_executable_filename)
        args.append('/nologo')
        args.append('/Platform_Assemblies_Paths')
        args.append(self.platform_assemblies_paths_sep.join(platform_assemblies_paths))
        args.append('/CreatePdb' if sys.platform == 'win32' else '/CreatePerfMap')
        args.append(debugging_files_dirname)
        args.append('/in')
        args.append(ni_filename)
        return args

    async def _run_with_args(self, args):
        """
            Creates a subprocess running crossgen with specified set of arguments,
            communicates with the owner process - waits for its termination and pulls
            returncode, stdour, stderr.
        """
        stdout = None
        stderr = None

        proc = await asyncio.create_subprocess_shell(" ".join(args),
                                                     stdin=asyncio.subprocess.PIPE,
                                                     stdout=asyncio.subprocess.PIPE,
                                                     stderr=asyncio.subprocess.PIPE)
        stdout, stderr = await proc.communicate()

        return (proc.returncode, stdout.decode(), stderr.decode(), " ".join(args))


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
    def __init__(self, assembly_name, returncode, stdout, stderr, output_file_hashsum, output_file_size_in_bytes, output_file_type, args, compiler_arch_os):
        self.assembly_name = assembly_name
        self.returncode = returncode
        self.stdout = stdout
        self.stderr = stderr
        self.output_file_hashsum = output_file_hashsum
        self.output_file_size_in_bytes = output_file_size_in_bytes
        self.output_file_type = output_file_type
        self.args = args
        self.compiler_arch_os = compiler_arch_os

################################################################################
# JSON Encoder for CrossGenResult objects.
################################################################################
class CrossGenResultEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, CrossGenResult):
            return {
                'CompilerArchOS': obj.compiler_arch_os,
                'AssemblyName': obj.assembly_name,
                'ReturnCode': obj.returncode,
                'StdOut': obj.stdout if isinstance(obj.stdout, list) else obj.stdout.splitlines(),
                'StdErr': obj.stderr if isinstance(obj.stderr, list) else obj.stderr.splitlines(),
                'OutputFileHash': obj.output_file_hashsum,
                'OutputFileSizeInBytes': obj.output_file_size_in_bytes,
                'OutputFileType': obj.output_file_type }
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
            compiler_arch_os = dict['CompilerArchOS']
            assembly_name = dict['AssemblyName']
            returncode = dict['ReturnCode']
            stdout = dict['StdOut']
            stderr = dict['StdErr']
            output_file_hashsum = dict['OutputFileHash']
            output_file_size_in_bytes = dict['OutputFileSizeInBytes']
            output_file_type = dict['OutputFileType']
            return CrossGenResult(assembly_name, returncode, stdout, stderr, output_file_hashsum, output_file_size_in_bytes, output_file_type, "", compiler_arch_os)
        except KeyError:
            return dict


################################################################################
# Helper Functions
################################################################################
def get_assembly_name(il_filename):
    basename = os.path.basename(il_filename)
    assembly_name, _ = os.path.splitext(basename)
    return assembly_name

class FileTypes:
    NativeOrReadyToRunImage = 'NativeOrReadyToRunImage'
    DebuggingFile = 'DebuggingFile'

async def run_crossgen(dotnet, crossgen_executable_filename, il_filename, ni_filename, platform_assemblies_paths, debugging_files_dirname, target_os, target_arch, compiler_arch_os):
    runner = CrossGenRunner(dotnet, crossgen_executable_filename)
    returncode, stdout, stderr, args = await runner.crossgen_il_file(il_filename, ni_filename, platform_assemblies_paths, target_os, target_arch)
    ni_file_hashsum = compute_file_hashsum(ni_filename) if returncode == 0 else None
    ni_file_size_in_bytes = os.path.getsize(ni_filename) if returncode == 0 else None
    assembly_name = get_assembly_name(il_filename)
    crossgen_assembly_result = CrossGenResult(assembly_name, returncode, stdout, stderr, ni_file_hashsum, ni_file_size_in_bytes, output_file_type=FileTypes.NativeOrReadyToRunImage, args=args, compiler_arch_os=compiler_arch_os)

    if returncode != 0:
        return [crossgen_assembly_result]

# Until crossgen2 can produce debugging file, don't do it
    return [crossgen_assembly_result]

#    platform_assemblies_paths = platform_assemblies_paths + [os.path.dirname(ni_filename)]
#    returncode, stdout, stderr = await runner.create_debugging_file(ni_filename, debugging_files_dirname, platform_assemblies_paths)

#    if returncode == 0:
#        filenames = list(filter(lambda filename: not re.match("^{0}\.ni\.".format(assembly_name), filename, re.IGNORECASE) is None, os.listdir(debugging_files_dirname)))
#        assert len(filenames) == 1
#        debugging_filename = os.path.join(debugging_files_dirname, filenames[0])
#        debugging_file_hashsum = compute_file_hashsum(debugging_filename)
#        debugging_file_size_in_bytes = os.path.getsize(debugging_filename)
#    else:
#        debugging_file_hashsum = None
#        debugging_file_size_in_bytes = None

#    create_debugging_file_result = CrossGenResult(assembly_name, returncode, stdout, stderr, debugging_file_hashsum, debugging_file_size_in_bytes, output_file_type=FileTypes.DebuggingFile)

#    return [crossgen_assembly_result, create_debugging_file_result]


def save_crossgen_result_to_json_file(crossgen_result, json_filename):
    with open(json_filename, 'wt') as json_file:
        json.dump(crossgen_result, json_file, cls=CrossGenResultEncoder, indent=2)

def save_crossgen_results_to_json_files(crossgen_results, result_dirname):
    for result in crossgen_results:
        json_filename = os.path.join(result_dirname, "{0}-{1}.json".format(result.assembly_name, result.output_file_type))
        save_crossgen_result_to_json_file(result, json_filename)

def create_output_folders():
    ni_files_dirname = tempfile.mkdtemp()
    debugging_files_dirname = os.path.join(ni_files_dirname, "DebuggingFiles")
    os.mkdir(debugging_files_dirname)
    return ni_files_dirname, debugging_files_dirname

async def crossgen_corelib(args):
    il_corelib_filename = args.il_corelib_filename
    assembly_name = os.path.basename(il_corelib_filename)
    ni_corelib_dirname, debugging_files_dirname = create_output_folders()
    ni_corelib_filename = os.path.join(ni_corelib_dirname, assembly_name)
    platform_assemblies_paths = [os.path.dirname(il_corelib_filename)]

    # Validate the paths are correct.
    if not os.path.exists(il_corelib_filename):
        print("IL Corelib path does not exist.")
        sys.exit(1)

    crossgen_results = await run_crossgen(args.crossgen_executable_filename, il_corelib_filename, ni_corelib_filename, platform_assemblies_paths, debugging_files_dirname)
    shutil.rmtree(ni_corelib_dirname, ignore_errors=True)
    save_crossgen_results_to_json_files(crossgen_results, args.result_dirname)

def add_ni_extension(filename):
    filename,ext = os.path.splitext(filename)
    return filename + '.ni' + ext

def crossgen_framework(args):
    ni_files_dirname, debugging_files_dirname = create_output_folders()

    async def run_crossgen_helper(print_prefix, assembly_name):
        global g_frameworkcompile_failed
        platform_assemblies_paths = args.core_root
        print("{}{} {}".format(print_prefix, args.crossgen_executable_filename, assembly_name))

        il_filename = os.path.join(args.core_root, assembly_name)
        ni_filename = os.path.join(ni_files_dirname, add_ni_extension(assembly_name))
        crossgen_results = await run_crossgen(args.dotnet, args.crossgen_executable_filename, il_filename, ni_filename, platform_assemblies_paths, debugging_files_dirname, args.target_os, args.target_arch, args.compiler_arch_os)
        if crossgen_results[0].returncode != 0:
            g_frameworkcompile_failed = True
            print("{}{} {} return code={} args'{}' stdout '{}' stderr'{}'".format(print_prefix, args.crossgen_executable_filename, assembly_name, crossgen_results[0].returncode, crossgen_results[0].args, crossgen_results[0].stdout, crossgen_results[0].stderr))
        save_crossgen_results_to_json_files(crossgen_results, args.result_dirname)

    helper = AsyncSubprocessHelper(g_Framework_Assemblies, verbose=True)
    helper.run_to_completion(run_crossgen_helper)

    shutil.rmtree(ni_files_dirname, ignore_errors=True)
    if g_frameworkcompile_failed:
        sys.exit(1)

def load_crossgen_result_from_json_file(json_filename):
    with open(json_filename, 'rt') as json_file:
        return json.load(json_file, cls=CrossGenResultDecoder)

def load_crossgen_results_from_dir(dirname, output_file_type):
    crossgen_results = []
    for filename in glob.glob(os.path.join(dirname, '*.json')):
        loaded_result = load_crossgen_result_from_json_file(filename)
        if loaded_result.output_file_type == output_file_type:
            crossgen_results.append(loaded_result)
    return crossgen_results

def dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
    for dirpath, _, filenames in os.walk(dotnet_sdk_dirname):
        dirname = os.path.dirname(dirpath)
        if dirname.endswith('Microsoft.NETCore.App') or dirname.endswith('Microsoft.AspNetCore.App') or dirname.endswith('Microsoft.AspNetCore.All'):
            filenames = filter(lambda filename: not re.match(r'^(Microsoft|System)\..*dll$', filename) is None, filenames)
            filenames = filter(lambda filename: filename != 'System.Private.CoreLib.dll', filenames)
            yield (dirpath, filenames)

async def crossgen_dotnet_sdk(args):
    dotnet_sdk_dirname = tempfile.mkdtemp()
    with tarfile.open(args.dotnet_sdk_filename) as dotnet_sdk_tarfile:
        dotnet_sdk_tarfile.extractall(dotnet_sdk_dirname)

    il_corelib_filename = args.il_corelib_filename
    ni_files_dirname, debugging_files_dirname = create_output_folders()
    ni_corelib_filename = os.path.join(ni_files_dirname, os.path.basename(il_corelib_filename))
    platform_assemblies_paths = [os.path.dirname(il_corelib_filename)]
    crossgen_results = await run_crossgen(args.crossgen_executable_filename, il_corelib_filename, ni_corelib_filename, platform_assemblies_paths, debugging_files_dirname)
    save_crossgen_results_to_json_files(crossgen_results, args.result_dirname)

    platform_assemblies_paths = [ni_files_dirname]

    for il_files_dirname, _ in dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
        platform_assemblies_paths.append(il_files_dirname)

    for il_files_dirname, assembly_names in dotnet_sdk_enumerate_assemblies(dotnet_sdk_dirname):
        for assembly_name in assembly_names:
            il_filename = os.path.join(il_files_dirname, assembly_name)
            ni_filename = os.path.join(ni_files_dirname, add_ni_extension(assembly_name))
            crossgen_results = await run_crossgen(args.crossgen_executable_filename, il_filename, ni_filename, platform_assemblies_paths, debugging_files_dirname)
            save_crossgen_results_to_json_files(crossgen_results, args.result_dirname)
    shutil.rmtree(ni_files_dirname, ignore_errors=True)

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
    assert base_result.output_file_type == diff_result.output_file_type

    if base_result.returncode != diff_result.returncode:
        base_diff_are_equal = False
        print_compare_result_message_helper('Return code mismatch for "{0}" assembly for files of type "{1}":'.format(base_result.assembly_name, base_result.output_file_type), base_result.returncode, diff_result.returncode, base_dirname, diff_dirname)
    elif base_result.returncode == 0 and diff_result.returncode == 0:
        assert not base_result.output_file_hashsum is None
        assert not base_result.output_file_size_in_bytes is None

        assert not diff_result.output_file_hashsum is None
        assert not diff_result.output_file_size_in_bytes is None

        if base_result.output_file_hashsum != diff_result.output_file_hashsum:
            base_diff_are_equal = False
            print_compare_result_message_helper('File hash sum mismatch for "{0}" assembly for files of type "{1}":'.format(base_result.assembly_name, base_result.output_file_type), base_result.output_file_hashsum, diff_result.output_file_hashsum, base_dirname, diff_dirname)

        if base_result.output_file_size_in_bytes != diff_result.output_file_size_in_bytes:
            base_diff_are_equal = False
            print_compare_result_message_helper('File size mismatch for "{0}" assembly for files of type "{1}":'.format(base_result.assembly_name, base_result.output_file_type), base_result.output_file_size_in_bytes, diff_result.output_file_size_in_bytes, base_dirname, diff_dirname)

    return base_diff_are_equal

def compare_results(args):
    """
        Checks whether {base} and {diff} crossgens are "equal":
         1. their return codes are the same;
         2. and if they both succeeded in step 1, their outputs (native images and debugging files (i.e. pdb or perfmap files)) are the same.
    """
    base_diff_are_equal = True
    did_compare = False

    for output_file_type in [FileTypes.NativeOrReadyToRunImage, FileTypes.DebuggingFile]:
        print('Comparing crossgen results in "{0}" and "{1}" directories for files of type "{2}":'.format(args.base_dirname, args.diff_dirname, output_file_type))

        base_results = load_crossgen_results_from_dir(args.base_dirname, output_file_type)
        diff_results = load_crossgen_results_from_dir(args.diff_dirname, output_file_type)

        base_assemblies = { r.assembly_name for r in base_results }
        diff_assemblies = { r.assembly_name for r in diff_results }
        both_assemblies = base_assemblies & diff_assemblies

        num_omitted_results = 0

        omitted_from_base_dir = diff_assemblies - base_assemblies
        omitted_from_diff_dir = base_assemblies - diff_assemblies

        if len(omitted_from_base_dir) != 0:
            num_omitted_results += len(omitted_from_base_dir)
            base_diff_are_equal = False
            print_omitted_assemblies_message(omitted_from_base_dir, args.base_dirname)

        if len(omitted_from_diff_dir) != 0:
            num_omitted_results += len(omitted_from_diff_dir)
            base_diff_are_equal = False
            print_omitted_assemblies_message(omitted_from_diff_dir, args.diff_dirname)

        base_results_by_name = dict((r.assembly_name, r) for r in base_results)
        diff_results_by_name = dict((r.assembly_name, r) for r in diff_results)

        num_mismatched_results = 0

        for assembly_name in sorted(both_assemblies):
            base_result = base_results_by_name[assembly_name]
            diff_result = diff_results_by_name[assembly_name]
            did_compare = True
            if not compare_and_print_message(base_result, diff_result, args.base_dirname, args.diff_dirname):
                base_diff_are_equal = False
                num_mismatched_results += 1

        print("Number of omitted results: {0}".format(num_omitted_results))
        print("Number of mismatched results: {0}".format(num_mismatched_results))
        print("Total number of files compared: {0}".format(len(both_assemblies)))

        root = minidom.Document()
        assemblies = root.createElement('assemblies')
        root.appendChild(assemblies)

        assembly = root.createElement('assembly')
        assembly.setAttribute('name', 'crossgen2_comparison_job_targeting_{0}'.format(args.target_arch_os))
        assembly.setAttribute('total', '{0}'.format(len(both_assemblies)))
        assembly.setAttribute('passed', '{0}'.format(len(both_assemblies) - num_omitted_results - num_mismatched_results))
        assembly.setAttribute('failed', '{0}'.format(num_omitted_results+num_mismatched_results))
        assembly.setAttribute('skipped', '0')
        assemblies.appendChild(assembly)

        collection = root.createElement('collection')
        collection.setAttribute('name', 'crossgen2_comparison_job_targeting_{0}'.format(args.target_arch_os))
        collection.setAttribute('total', '{0}'.format(len(both_assemblies)))
        collection.setAttribute('passed', '{0}'.format(len(both_assemblies) - num_omitted_results - num_mismatched_results))
        collection.setAttribute('failed', '{0}'.format(num_omitted_results+num_mismatched_results))
        collection.setAttribute('skipped', '0')
        assembly.appendChild(collection)

        for assembly_name in sorted(omitted_from_base_dir):
            diff_result = diff_results_by_name[assembly_name]
            message = 'Expected nothing, got {0}'.format(json.dumps(diff_result, cls=CrossGenResultEncoder, indent=2))
            testresult = root.createElement('test')
            testresult.setAttribute('name', 'CrossgenCompile_{2}_Target_{0}_Omitted_vs_{1}'.format(args.target_arch_os, diff_result.compiler_arch_os, assembly_name))
            testresult.setAttribute('type', 'Target_{0}'.format(args.target_arch_os))
            testresult.setAttribute('method', diff_result.compiler_arch_os)
            testresult.setAttribute('time', '0')
            testresult.setAttribute('result', 'Fail')
            collection.appendChild(testresult)

            failureXml = root.createElement('failure')
            failureXml.setAttribute('exception-type', 'OmittedFromBase')
            testresult.appendChild(failureXml)

            messageXml = root.createElement('message')
            messageXml.appendChild(root.createTextNode(message))
            failureXml.appendChild(messageXml)
            messageXml = root.createElement('output')
            messageXml.appendChild(root.createTextNode(message))
            failureXml.appendChild(messageXml)

        for assembly_name in omitted_from_diff_dir:
            base_result = diff_results_by_name[assembly_name]
            message = 'Expected {0} got nothing'.format(json.dumps(base_result, cls=CrossGenResultEncoder, indent=2))
            testresult = root.createElement('test')
            testresult.setAttribute('name', 'CrossgenCompile_{2}_Target_{0}_{1}_vs__Omitted'.format(args.target_arch_os, base_result.compiler_arch_os, assembly_name))
            testresult.setAttribute('type', 'Target_{0}'.format(args.target_arch_os))
            testresult.setAttribute('method', base_result.compiler_arch_os)
            testresult.setAttribute('time', '0')
            testresult.setAttribute('result', 'Fail')
            collection.appendChild(testresult)

            failureXml = root.createElement('failure')
            failureXml.setAttribute('exception-type', 'OmittedFromDiff')
            testresult.appendChild(failureXml)

            messageXml = root.createElement('message')
            messageXml.appendChild(root.createTextNode(message))
            failureXml.appendChild(messageXml)
            messageXml = root.createElement('output')
            messageXml.appendChild(root.createTextNode(message))
            failureXml.appendChild(messageXml)

        for assembly_name in sorted(both_assemblies):
            base_result = base_results_by_name[assembly_name]
            diff_result = diff_results_by_name[assembly_name]
            base_diff_are_equal = True

            if base_result.returncode != diff_result.returncode:
                base_diff_are_equal = False
            elif base_result.returncode == 0 and diff_result.returncode == 0:
                if base_result.output_file_hashsum != diff_result.output_file_hashsum:
                    base_diff_are_equal = False
                if base_result.output_file_size_in_bytes != diff_result.output_file_size_in_bytes:
                    base_diff_are_equal = False
            else:
                base_diff_are_equal = False

            base_result_string = json.dumps(base_result, cls=CrossGenResultEncoder, indent=2)
            diff_result_string = json.dumps(diff_result, cls=CrossGenResultEncoder, indent=2)
            message = 'Expected {0} got {1}'.format(base_result_string, diff_result_string)
            testresult = root.createElement('test')
            testresult.setAttribute('name', 'CrossgenCompile_{3}_Target_{0}_{1}_vs_{2}'.format(args.target_arch_os, base_result.compiler_arch_os, diff_result.compiler_arch_os, assembly_name))
            testresult.setAttribute('type', 'Target_{0}'.format(args.target_arch_os))
            testresult.setAttribute('method', '{0}_{1}'.format(base_result.compiler_arch_os, diff_result.compiler_arch_os))
            testresult.setAttribute('time', '0')
            if base_diff_are_equal:
                testresult.setAttribute('result', 'Pass')
            else:
                testresult.setAttribute('result', 'Fail')

            collection.appendChild(testresult)

            if not base_diff_are_equal:
                failureXml = root.createElement('failure')
                failureXml.setAttribute('exception-type', 'MismatchOrReturnCodeFail')
                testresult.appendChild(failureXml)

                messageXml = root.createElement('message')
                messageXml.appendChild(root.createTextNode(message))

                failureXml.appendChild(messageXml)
                messageXml = root.createElement('output')
                messageXml.appendChild(root.createTextNode(message))

                failureXml.appendChild(messageXml)

        xml_str = root.toprettyxml(indent ="\t")

        if output_file_type == FileTypes.NativeOrReadyToRunImage:
            with open(args.testresultsxml, "w") as f:
                f.write(xml_str)

    if not did_compare:
        sys.exit(1)

    sys.exit(0 if base_diff_are_equal else 1)

################################################################################
# __main__
################################################################################

if __name__ == '__main__':
    start = datetime.datetime.now()

    parser = build_argument_parser()
    args = parser.parse_args()
    func = args.func(args)

    end = datetime.datetime.now()
    elapsed = end - start

    print("Elapsed time: {}".format(elapsed.total_seconds()))
