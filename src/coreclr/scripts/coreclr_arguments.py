#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
## See the LICENSE file in the project root for more information.
#
##
# Title               : coreclr_arguments.py
#
# Notes:
#  
# Setup script, to avoid re-writing argument validation between different
# coreclr scripts.
#
################################################################################
################################################################################

import argparse
import datetime
import json
import os
import platform
import shutil
import subprocess
import sys
import tempfile
import time
import re
import string

import xml.etree.ElementTree

from collections import defaultdict
from sys import platform as _platform

################################################################################
################################################################################

class CoreclrArguments:

    ############################################################################
    # ctor
    ############################################################################

    def __init__(self, 
                 args,
                 require_built_test_dir=False,
                 require_built_core_root=False,
                 require_built_product_dir=False,
                 default_build_type="Debug"):
        """ Setup the args based on the argparser obj

        Args:
            args(ArgParser): Parsed arguments

        Notes:
            If there is no core_root, or test location passed, create a default
            location using the build type and the arch.
        """

        # Default values. Note that these are extensible.
        self.host_os = None
        self.arch = None
        self.build_type = None
        self.core_root = None
        self.coreclr_repo_location = None

        self.default_build_type = default_build_type

        self.require_built_product_dir = require_built_product_dir
        self.require_built_core_root = require_built_core_root
        self.require_built_test_dir = require_built_test_dir

        self.valid_arches = ["x64", "x86", "arm", "arm64"]
        self.valid_build_types = ["Debug", "Checked", "Release"]
        self.valid_host_os = ["Windows", "Windows_NT", "OSX", "Linux"]

        self.__initialize__(args)

    ############################################################################
    # Instance Methods
    ############################################################################

    def check_build_type(self, build_type):
        if build_type != None and len(build_type) > 0:
            # Force the build type to be capitalized
            build_type = build_type.capitalize()
            return build_type

        elif build_type == None:
            return self.default_build_type

        if not build_type in self.valid_build_types:
            return False

        return True

    def verify(self, 
               args, 
               arg_name,
               verify, 
               failure_str,
               arg_value=None,
               modify_arg=None,
               modify_after_validation=False):
        """ Verify an arg

        Args:
            args        (argParser)             : arg parser args
            arg_name    (String)                : argument to verify
            verify      (lambda: arg -> bool)   : verify method
            failure_str (String)                : failure output if not verified
            modify_arg  (lambda: arg -> arg)    : modify the argument before assigning

        Returns:
            verified (bool)
        """
        verified = False
        arg_value = None

        if isinstance(args, argparse.Namespace):
            try:
                arg_value = getattr(args, arg_name)
            except:
                pass

        else:
            arg_value = args

        if modify_arg != None and not modify_after_validation:
            arg_value = modify_arg(arg_value)

        try:
            verified = verify(arg_value)
        except:
            pass
        
        if verified == False and isinstance(failure_str, str):
            print(failure_str)
            sys.exit(1)
        elif verified == False:
            print(failure_str(arg_value))
            sys.exit(1)
        
        if modify_arg != None and modify_after_validation:
            arg_value = modify_arg(arg_value)

        if verified != True and arg_value is None:
            arg_value = verified

        # Add a new member variable based on the verified arg
        setattr(self, arg_name, arg_value)

    ############################################################################
    # Helper Methods
    ############################################################################

    def __initialize__(self, args):
        def check_host_os(host_os):
            if host_os is None:
                host_os = provide_default_host_os()
                assert(host_os != None)

                return host_os

            else:
                return host_os in self.valid_host_os

        def check_arch(arch):
            if arch is None:
                arch = provide_default_arch()
                assert(arch in self.valid_arches)

                return arch

            else:
                return arch in self.valid_arches

        def provide_default_arch():
            platform_machine = platform.machine()
            if platform_machine == "x86_64":
                return "x64"
            elif platform_machine == "i386":
                return "x86"
            elif platform_machine == "armhf":
                return "arm"
            elif platform_machine == "armel":
                return "armel"
            elif platform_machine == "aarch64" or platform_machine == "arm64":
                return "arm64"
            else:
                raise RuntimeError("Unsupported platform")

        def provide_default_host_os():
            if _platform == "linux" or _platform == "linux2":
                return "Linux"
            elif _platform == "darwin":
                return "OSX"
            elif _platform == "win32":
                return "Windows_NT"
            else:
                print("Unknown OS: %s" % self.host_os)
                sys.exit(1)
            
            return None

        def check_and_return_test_location(test_location):
            default_test_location = os.path.join(self.coreclr_repo_location, "bin", "tests", "%s.%s.%s" % (self.host_os, self.arch, self.build_type))

            if os.path.isdir(default_test_location) or not self.require_built_test_dir:
                return default_test_location

            elif not os.path.isdir(test_location) and self.require_built_test_dir:
                return False

            return test_location

        def check_and_return_default_core_root(core_root):
            default_core_root = os.path.join(self.test_location, "Tests", "Core_Root")

            if os.path.isdir(default_core_root) or not self.require_built_core_root:
                return default_core_root

            elif not os.path.isdir(core_root) and self.require_built_core_root:
                return False

            return core_root

        def check_and_return_default_product_location(product_location):
            default_product_location = os.path.join(self.bin_location, "Product", "%s.%s.%s" % (self.host_os, self.arch, self.build_type))

            if os.path.isdir(default_product_location) or not self.require_built_product_dir:
                return default_product_location
            elif os.path.isdir(product_location) and self.require_build_product_dir:
                return False

            return product_location

        self.coreclr_repo_location = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
        self.bin_location = os.path.join(self.coreclr_repo_location, "bin")

        self.verify(args,
                    "host_os",
                    check_host_os,
                    "Unsupported host_os",
                    modify_arg=lambda host_os: provide_default_host_os() if host_os is None else host_os)

        self.verify(args, 
                    "arch",
                    check_arch,
                    "Unsupported architecture: %s.\nSupported architectures: %s" % (args.arch, ", ".join(self.valid_arches)))

        self.verify(args,
                    "build_type",
                    self.check_build_type,
                    "Unsupported configuration: %s.\nSupported configurations: %s" % (args.build_type, ", ".join(self.valid_build_types)),
                    modify_arg=lambda arg: arg.capitalize())

        self.verify(args,
                    "test_location",
                    check_and_return_test_location,
                    "Error, incorrect test location.")

        self.verify(args,
                    "core_root",
                    check_and_return_default_core_root,
                    "Error, incorrect core_root location.")

        self.verify(args,
                    "product_location",
                    check_and_return_default_product_location,
                    "Error, incorrect product_location.")