#!/usr/bin/python -u -OO

import itertools
import os
import re
import shutil
import string
import sys
import tempfile
import traceback

from glob import glob

if __name__ == "__main__":
    sys.path.append(os.path.realpath('../..'))
    sys.path.append(os.path.realpath('../../packages'))
    sys.path.append(os.path.realpath('../mono-mac'))

from MonoReleaseProfile import MonoReleaseProfile
from bockbuild.util.util import *


class MonoXamarinPackageProfile(MonoReleaseProfile):

    def __init__(self):
        MonoReleaseProfile.__init__(self)

        # add the private stuff
        self.packages_to_build.extend(['mono-extensions'])

        if self.cmd_options.release_build:
            self.setup_codesign()
        else:
            info("'--release' option not set, will not attempt to sign package!")

        self.cache_host = 'http://storage.bos.xamarin.com/bockbuild_cache/'

    def setup_codesign(self):
        self.identity = "Developer ID Installer: Xamarin Inc"

        output = backtick("security -v find-identity")
        if self.identity not in " ".join(output):
            error("Identity '%s' was not found. You can create an unsigned package by removing '--release' to your command line." % self.identity)

        password = os.getenv("CODESIGN_KEYCHAIN_PASSWORD")
        if password:
            print "Unlocking the keychain"
            run_shell("security unlock-keychain -p %s" % password)
        else:
            error("CODESIGN_KEYCHAIN_PASSWORD needs to be defined.")

    def setup_release(self):
        MonoReleaseProfile.setup_release(self)
        self.release_packages['mono'].configure_flags.extend(
            ['--enable-extension-module=xamarin --enable-native-types --enable-pecrypt'])
        info('Xamarin extensions enabled')

    def run_pkgbuild(self, working_dir, package_type):
        output = MonoReleaseProfile.run_pkgbuild(
            self, working_dir, package_type)

        output_unsigned = os.path.join(os.path.dirname(
            output), os.path.basename(output).replace('.pkg', '.UNSIGNED.pkg'))
        shutil.move(output, output_unsigned)

        if not self.cmd_options.release_build:
            return output_unsigned

        productsign = "/usr/bin/productsign"
        productsign_cmd = ' '.join([productsign,
                                    "-s '%s'" % self.identity,
                                    "'%s'" % output_unsigned,
                                    "'%s'" % output])
        run_shell(productsign_cmd)
        os.remove(output_unsigned)
        self.verify_codesign(output)

        return output

    def verify_codesign(self, pkg):
        oldcwd = os.getcwd()
        try:
            name = os.path.basename(pkg)
            pkgdir = os.path.dirname(pkg)
            os.chdir(pkgdir)
            spctl = "/usr/sbin/spctl"
            spctl_cmd = ' '.join(
                [spctl, "-vvv", "--assess", "--type install", name, "2>&1"])
            output = backtick(spctl_cmd)

            if "accepted" in " ".join(output):
                warn("%s IS SIGNED" % pkg)
            else:
                error("%s IS NOT SIGNED:" % pkg)
        finally:
            os.chdir(oldcwd)

if __name__ == "__main__":
    try:
        MonoXamarinPackageProfile().build()
    except Exception as e:
        exc_type, exc_value, exc_traceback = sys.exc_info()
        error('%s (%s)' % (e, exc_type.__name__), more_output=True)
        error(('%s:%s @%s\t\t"%s"' % p for p in traceback.extract_tb(
            exc_traceback)[-3:]), more_output=True)
    except KeyboardInterrupt:
        error('Interrupted.')
