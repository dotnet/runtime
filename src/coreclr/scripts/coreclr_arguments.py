#!/usr/bin/env python
#
## Licensed to the .NET Foundation under one or more agreements.
## The .NET Foundation licenses this file to you under the MIT license.
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
import os
import platform
import subprocess
import sys

################################################################################
################################################################################


class CoreclrArguments:
    """ Class to process arguments for CoreCLR specific Python code.
    """

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
            args(argparse.Namespace return value from argparse.ArgumentParser.parse_args()): Parsed arguments

        Notes:
            If there is no core_root, or test location passed, create a default
            location using the build type and the arch.
        """

        # Default values. Note that these are extensible.
        self.host_os = None
        self.arch = None
        self.build_type = None
        self.core_root = None
        self.runtime_repo_location = None
        self.artifacts_location = None
        self.coreclr_dir = None

        self.default_build_type = default_build_type

        self.require_built_product_dir = require_built_product_dir
        self.require_built_core_root = require_built_core_root
        self.require_built_test_dir = require_built_test_dir

        self.valid_arches = ["x64", "x86", "arm", "arm64", "loongarch64", "riscv64", "wasm"]
        self.valid_build_types = ["Debug", "Checked", "Release"]
        self.valid_host_os = ["windows", "osx", "linux", "illumos", "solaris", "haiku", "browser", "android", "wasi"]

        self.__initialize__(args)

    ############################################################################
    # Instance Methods
    ############################################################################

    def check_build_type(self, build_type):
        """ Process the `build_type` argument.

            If unset, provide a default. Otherwise, check that it is valid.
        """

        if build_type is None:
            build_type = self.default_build_type
            assert build_type in self.valid_build_types
            return build_type

        if len(build_type) > 0:
            # Force the build type to be capitalized
            build_type = build_type.capitalize()

        return build_type in self.valid_build_types

    def verify(self,
               args,
               arg_name,
               verify,
               failure_str,
               arg_value=None,
               modify_arg=None,
               modify_after_validation=False):
        """ Verify an arg

        Note that every argument must call verify() for it to have an attribute added to this class.
        We do not use the argparse parsed argument object; we use this CoreclrArguments object instead,
        after transferring all arguments to it. Arguments that have already been adequately verified
        by parse_args() need no further verification, but do need to be transferred to this class for
        future use.

        `args` can also be an object (likely a string), in which case it is treated as
        the value to assign to arg_name after the verify function is called.

        The `verify` lambda returns True or False if the value is ok or not ok. It can also return a
        non-bool result, in which case the argument value will be set to the return value of the `verify`
        function call. This can be used to set a default.

        Args:
            args
                (argparse.Namespace return value from argparse.ArgumentParser.parse_args()) : Parsed arguments
                (object)                                                                    : value to verify for arg_name
            arg_name    (String)                                : argument to verify
            verify      (lambda: arg -> bool or value)          : verify method
            failure_str (String or lambda: arg_value -> String) : failure output if not verified
            arg_value
            modify_arg  (lambda: arg -> arg)                    : modify the argument before assigning
            modify_after_validation (bool)                      : If True, run the modify_arg function after validation.
                                                                  Otherwise (default), run it before validation.

        Returns:
            verified (bool)

        Note:
            The CoreclrArguments object gets a new member named by `arg_name` with the argument value.
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

        if modify_arg is not None and not modify_after_validation:
            arg_value = modify_arg(arg_value)

        try:
            verified = verify(arg_value)
        except:
            pass

        if verified is False and isinstance(failure_str, str):
            print(failure_str)
            sys.exit(1)
        elif verified is False:
            print(failure_str(arg_value))
            sys.exit(1)

        if modify_arg is not None and modify_after_validation:
            arg_value = modify_arg(arg_value)

        if verified is not True and arg_value is None:
            arg_value = verified

        # Add a new member variable based on the verified arg
        setattr(self, arg_name, arg_value)

    ############################################################################
    # Helper Methods
    ############################################################################

    @staticmethod
    def provide_default_host_os():
        """ Return a string representing the current host operating system.

            Returns one of: linux, osx, windows, illumos, solaris, haiku
        """

        if sys.platform == "linux" or sys.platform == "linux2":
            return "linux"
        elif sys.platform == "darwin":
            return "osx"
        elif sys.platform == "win32":
            return "windows"
        elif sys.platform.startswith("sunos"):
            is_illumos = ('illumos' in subprocess.Popen(["uname", "-o"], stdout=subprocess.PIPE, stderr=subprocess.PIPE).communicate()[0].decode('utf-8'))
            return 'illumos' if is_illumos else 'solaris'
        elif sys.platform == "haiku":
            return "haiku"
        else:
            print("Unknown OS: %s" % sys.platform)
            sys.exit(1)

    @staticmethod
    def provide_default_arch():
        """ Return a string representing the current processor architecture.

            Returns one of: x64, x86, arm, armel, arm64.
        """

        platform_machine = platform.machine().lower()
        if platform_machine == "x86_64" or platform_machine == "amd64":
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
            print("Unknown architecture: %s" % platform_machine)
            sys.exit(1)

    def __initialize__(self, args):

        def check_host_os(host_os):
            return (host_os is not None) and (host_os in self.valid_host_os)

        def check_arch(arch):
            return (arch is not None) and (arch in self.valid_arches)

        def check_and_return_test_location(test_location):
            default_test_location = os.path.join(self.artifacts_location, "tests", "coreclr", "%s.%s.%s" % (self.host_os, self.arch, self.build_type))

            if os.path.isdir(default_test_location) or not self.require_built_test_dir:
                return default_test_location

            elif not os.path.isdir(test_location) and self.require_built_test_dir:
                return False

            return test_location

        def check_and_return_default_core_root(core_root):
            if core_root is not None:
                # core_root was specified on the command-line, so use that one. But verify it.
                return os.path.isdir(core_root) or not self.require_built_core_root

            # No core_root specified; use a default location if possible.
            default_core_root = os.path.join(self.test_location, "Tests", "Core_Root")
            if os.path.isdir(default_core_root) or not self.require_built_core_root:
                return default_core_root

            return False

        def check_and_return_default_product_location(product_location):
            default_product_location = os.path.join(self.artifacts_location, "bin", "coreclr", "%s.%s.%s" % (self.host_os, self.arch, self.build_type))

            if os.path.isdir(default_product_location) or not self.require_built_product_dir:
                return default_product_location
            elif os.path.isdir(product_location) and self.require_build_product_dir:
                return False

            return product_location

        self.runtime_repo_location = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
        self.artifacts_location = os.path.join(self.runtime_repo_location, "artifacts")
        self.coreclr_dir = os.path.join(self.runtime_repo_location, "src", "coreclr")

        self.verify(args,
                    "host_os",
                    check_host_os,
                    lambda host_os: "Unknown host_os {}\nSupported OS: {}".format(host_os, (", ".join(self.valid_host_os))),
                    modify_arg=lambda host_os: CoreclrArguments.provide_default_host_os() if host_os is None else host_os)

        self.verify(args,
                    "arch",
                    check_arch,
                    lambda arch: "Unknown arch {}.\nSupported architectures: {}".format(arch, (", ".join(self.valid_arches))),
                    modify_arg=lambda arch: CoreclrArguments.provide_default_arch() if arch is None else arch)

        self.verify(args,
                    "build_type",
                    self.check_build_type,
                    lambda build_type: "Unknown build_type {}.\nSupported build types: {}".format(build_type, ", ".join(self.valid_build_types)),
                    modify_arg=lambda arg: arg.capitalize() if arg is not None else None)

        self.verify(args,
                    "test_location",
                    check_and_return_test_location,
                    "Error, incorrect test location.")

        self.verify(args,
                    "core_root",
                    check_and_return_default_core_root,
                    "Error, Core_Root could not be determined, or points to a location that doesn't exist.")

        self.verify(args,
                    "product_location",
                    check_and_return_default_product_location,
                    "Error, incorrect product_location.")
