import itertools
import os
import re
import shutil
import string
import sys
import tempfile
import subprocess
import stat

from bockbuild.darwinprofile import DarwinProfile
from bockbuild.util.util import *
from glob import glob

class MonoReleaseProfile(DarwinProfile):
    description = 'The Mono Framework for MacOS'
    packages = [
        'gettext',
        'pkg-config',

        # Base Libraries
        'libpng',
        'libjpeg',
        'libtiff',
        'libgif',
        'libxml2',
        'freetype',
        'fontconfig',
        'pixman',
        'cairo',
        'libffi',
        'glib',
        'pango',
        'atk',
        'intltool',
        'gdk-pixbuf',
        'gtk+',
        'libglade',
        'sqlite',
        'expat',
        'ige-mac-integration',

        # Theme
        'libcroco',
        'librsvg',
        'hicolor-icon-theme',
        'gtk-engines',
        'murrine',
        'xamarin-gtk-theme',
        'gtk-quartz-engine',

        # Mono
        'mono-llvm',
        'mono',
        'msbuild',
        'pcl-reference-assemblies',
        'libgdiplus',
        'xsp',
        'gtk-sharp',
        'ironlangs',
        'fsharp',
        'mono-basic',
        'nuget'
    ]

    def attach (self, bockbuild):
        self.min_version = 7
        DarwinProfile.attach (self, bockbuild)

        # quick disk space check (http://stackoverflow.com/questions/787776/)
        s = os.statvfs(bockbuild.root)
        free_space = (s.f_bavail * s.f_frsize) / (1024 * 1024 * 1024)  # in GB

        if free_space < 10:
            error('Low disk space (less than 10GB), aborting')

        # check for XQuartz installation (needed for libgdiplus)
        if not os.path.exists('/opt/X11/include/X11/Xlib.h'):
            error(
                'XQuartz is required to be installed (download from http://xquartz.macosforge.org/) ')

        self.MONO_ROOT = "/Library/Frameworks/Mono.framework"
        self.BUILD_NUMBER = "0"
        self.MDK_GUID = "964ebddd-1ffe-47e7-8128-5ce17ffffb05"

        system_mono_dir = '/Library/Frameworks/Mono.framework/Versions/Current'
        self.env.set('system_mono', os.path.join(
            system_mono_dir, 'bin', 'mono'))
        self.env.set('system_mcs', os.path.join(system_mono_dir, 'bin', 'mcs'))

        self.env.set('system_mono_version', backtick(
            '%s --version' % self.env.system_mono)[0])

        # config overrides for some programs to be functional while staged

        self.env.set('GDK_PIXBUF_MODULE_FILE',
                     '%{staged_prefix}/lib/gdk-pixbuf-2.0/2.10.0/loaders.cache')
        self.env.set('GDK_PIXBUF_MODULEDIR',
                     '%{staged_prefix}/lib/gdk-pixbuf-2.0/2.10.0/loaders')
        self.env.set('PANGO_SYSCONFDIR', '%{staged_prefix}/etc')
        self.env.set('PANGO_LIBDIR', '%{staged_prefix}/lib')
        # self.env.set ('MONO_PATH', '%{staged_prefix}/lib/mono/4.0')
        self.debug_info = ['gtk+', 'cairo',
                           'pango', 'mono', 'llvm', 'libgdiplus']
        self.cache_host = None

    def setup_release(self):
        self.mono_package = self.release_packages['mono']
        self.mono_package.fetch()

        verbose('Mono version: %s' % self.mono_package.version)
        self.RELEASE_VERSION = self.mono_package.version
        self.prefix = os.path.join(
            self.MONO_ROOT, "Versions", self.RELEASE_VERSION)

        if os.path.exists(self.prefix):
            error('Prefix %s exists, and may interfere with the staged build. Please remove and try again.' % self.prefix)

        self.calculate_updateid()
        trace(self.package_info('MDK'))

        self.dont_optimize = ['pixman']

        for p in self.release_packages.values():
            if p.name in self.dont_optimize:
                continue
            self.gcc_flags.extend(['-O2'])

    # THIS IS THE MAIN METHOD FOR MAKING A PACKAGE
    def package(self):
        self.fix_gtksharp_configs()
        self.verify_binaries()

        working = self.setup_working_dir()
        uninstall_script = os.path.join(working, "uninstallMono.sh")

        # make the MDK
        self.apply_blacklist(working, 'mdk_blacklist.sh')
        self.make_updateinfo(working, self.MDK_GUID)
        mdk_pkg = self.run_pkgbuild(working, "MDK")
        title(mdk_pkg)
        # self.make_dmg(mdk_dmg, title, mdk_pkg, uninstall_script)

        shutil.rmtree(working)

    def calculate_updateid(self):
        # Create the updateid
        pwd = os.getcwd()
        git_bin = self.bockbuild.git_bin
        trace("cur path is %s and git is %s" % (pwd, git_bin))
        blame_rev_str = 'cd %s; %s blame configure.ac HEAD | grep AC_INIT | sed \'s/ .*//\' ' % (
            self.mono_package.workspace, git_bin)
        blame_rev = backtick(blame_rev_str)[0]
        trace("Last commit to the version string %s" % (blame_rev))
        version_number_str = 'cd %s; %s log %s..HEAD --oneline | wc -l | sed \'s/ //g\'' % (
            self.mono_package.workspace, git_bin, blame_rev)
        self.BUILD_NUMBER = backtick(version_number_str)[0]
        trace("Calculating commit distance, %s" % (self.BUILD_NUMBER))
        self.FULL_VERSION = self.RELEASE_VERSION + "." + self.BUILD_NUMBER
        os.chdir(pwd)

        parts = self.RELEASE_VERSION.split(".")
        version_list = (parts + ["0"] * (3 - len(parts)))[:4]
        for i in range(1, 3):
            version_list[i] = version_list[i].zfill(2)
            self.updateid = "".join(version_list)
            self.updateid += self.BUILD_NUMBER.replace(
                ".", "").zfill(9 - len(self.updateid))
        trace(self.updateid)

    # creates and returns the path to a working directory containing:
    #   PKGROOT/ - this root will be bundled into the .pkg and extracted at /
    #   uninstallMono.sh - copied onto the DMG
    #   Info{_sdk}.plist - used by packagemaker to make the installer
    #   resources/ - other resources used by packagemaker for the installer
    def setup_working_dir(self):
        def make_package_symlinks(root):
            os.symlink(self.prefix, os.path.join(root, "Versions", "Current"))
            currentlink = os.path.join(self.MONO_ROOT, "Versions", "Current")
            links = [
                ("bin", "Commands"),
                ("include", "Headers"),
                ("lib", "Libraries"),
                ("", "Home"),
                (os.path.join("lib", "libmono-2.0.dylib"), "Mono")
            ]
            for srcname, destname in links:
                src = os.path.join(currentlink, srcname)
                dest = os.path.join(root, destname)
                # If the symlink exists, we remove it so we can create a fresh
                # one
                if os.path.exists(dest):
                    os.unlink(dest)
                os.symlink(src, dest)

        tmpdir = tempfile.mkdtemp()
        monoroot = os.path.join(tmpdir, "PKGROOT", self.MONO_ROOT[1:])
        versions = os.path.join(monoroot, "Versions")
        os.makedirs(versions)

        print "Setting up temporary package directory:", tmpdir

        # setup metadata
        self.packaging_dir = os.path.join(self.resource_path, "packaging")
        run_shell('rsync -aPq %s/* %s' % (self.packaging_dir, tmpdir), False)

        packages_list = string.join(
            [pkg.desc for pkg in self.release_packages.values()], "\\\n")
        deps_list = 'bockbuild (rev. %s)\\\n' % bockbuild.bockbuild_rev + string.join(
            [pkg.desc for pkg in self.toolchain_packages.values()], "\\\n")

        parameter_map = {
            '@@MONO_VERSION@@': self.RELEASE_VERSION,
            '@@MONO_RELEASE@@': self.BUILD_NUMBER,
            '@@MONO_VERSION_RELEASE@@': self.RELEASE_VERSION + '_' + self.BUILD_NUMBER,
            '@@MONO_CSDK_GUID@@': self.MDK_GUID,
            '@@MONO_VERSION_RELEASE_INT@@': self.updateid,
            '@@PACKAGES@@': packages_list,
            '@@DEP_PACKAGES@@': deps_list
        }
        for dirpath, d, files in os.walk(tmpdir):
            for name in files:
                if not name.startswith('.'):
                    replace_in_file(os.path.join(dirpath, name), parameter_map)

        make_package_symlinks(monoroot)

        # copy to package root
        run_shell('rsync -aPq "%s"/* "%s/%s"' %
                  (bockbuild.package_root, versions, self.RELEASE_VERSION), False)

        return tmpdir

    def apply_blacklist(self, working_dir, blacklist_name):
        print "Applying blacklist script:", blacklist_name
        blacklist = os.path.join(self.packaging_dir, blacklist_name)
        root = os.path.join(working_dir, "PKGROOT", self.prefix[1:])
        run_shell('%s "%s" > /dev/null' % (blacklist, root), print_cmd=False)

    def run_pkgbuild(self, working_dir, package_type):
        print 'Running pkgbuild & productbuild...',
        info = self.package_info(package_type)
        output = os.path.join(self.resource_path, info["filename"])
        identifier = "com.xamarin.mono-" + info["type"] + ".pkg"
        resources_dir = os.path.join(working_dir, "resources")
        distribution_xml = os.path.join(resources_dir, "distribution.xml")

        old_cwd = os.getcwd()
        os.chdir(working_dir)
        pkgbuild = "/usr/bin/pkgbuild"
        pkgbuild_cmd = ' '.join([pkgbuild,
                                 "--identifier " + identifier,
                                 "--root '%s/PKGROOT'" % working_dir,
                                 "--version '%s'" % self.RELEASE_VERSION,
                                 "--install-location '/'",
                                 "--scripts '%s'" % resources_dir,
                                 "--quiet",
                                 os.path.join(working_dir, "mono.pkg")])

        run_shell(pkgbuild_cmd)

        productbuild = "/usr/bin/productbuild"
        productbuild_cmd = ' '.join([productbuild,
                                     "--resources %s" % resources_dir,
                                     "--distribution %s" % distribution_xml,
                                     "--package-path %s" % working_dir,
                                     "--quiet",
                                     output])

        run_shell(productbuild_cmd)

        assert_exists(output)
        os.chdir(old_cwd)
        print output
        return output

    def make_updateinfo(self, working_dir, guid):
        updateinfo = os.path.join(
            working_dir, "PKGROOT", self.prefix[1:], "updateinfo")
        with open(updateinfo, "w") as updateinfo:
            updateinfo.write(guid + ' ' + self.updateid + "\n")

    def package_info(self, pkg_type):
        arch = self.bockbuild.cmd_options.arch
        arch_str = None
        if  arch == "darwin-32":
            arch_str = "x86"
        elif arch == "darwin-64":
            arch_str = "x64"
        elif arch == "darwin-universal":
            arch_str = "universal"
        else:
            error ("Unknown architecture")

        if self.bockbuild.cmd_options.release_build:
            info = (pkg_type, self.FULL_VERSION, arch_str)
        else:
            info = (pkg_type, '%s-%s' % (git_shortid(self.bockbuild,
                                                     self.mono_package.workspace), self.FULL_VERSION), arch_str)

        filename = "MonoFramework-%s-%s.macos10.xamarin.%s.pkg" % info
        return {
            "type": pkg_type,
            "filename": filename
        }

    def fix_line(self, line, matcher):
        def insert_install_root(matches):
            root = self.prefix
            captures = matches.groupdict()
            return 'target="%s"' % os.path.join(root, "lib", captures["lib"])

        if matcher(line):
            pattern = r'target="(?P<lib>.+\.dylib)"'
            result = re.sub(pattern, insert_install_root, line)
            return result
        else:
            return line

    def fix_dllmap(self, config, matcher):
        handle, temp = tempfile.mkstemp()
        with open(config) as c:
            with open(temp, "w") as output:
                for line in c:
                    output.write(self.fix_line(line, matcher))
        os.rename(temp, config)
        os.system('chmod a+r %s' % config)

    def fix_gtksharp_configs(self):
        print 'Fixing GTK# configuration files...',
        count = 0
        libs = [
            'atk-sharp',
            'gdk-sharp',
            'glade-sharp',
            'glib-sharp',
            'gtk-dotnet',
            'gtk-sharp',
            'pango-sharp'
        ]
        gac = os.path.join(bockbuild.package_root, "lib", "mono", "gac")
        confs = [glob.glob(os.path.join(gac, x, "*", "*.dll.config")) for x in libs]
        for c in itertools.chain(*confs):
            count = count + 1
            self.fix_dllmap(c, lambda line: "dllmap" in line)
        print count

    def verify(self, f):
        result = " ".join(backtick("otool -L " + f))
        regex = os.path.join(self.MONO_ROOT, "Versions", r"(\d+\.\d+\.\d+)")

        match = re.search(regex, result)
        if match is None:
            return
        token = match.group(1)
        trace(token)
        if self.RELEASE_VERSION not in token:
            raise Exception("%s references Mono %s\n%s" % (f, token, text))

    def verify_binaries(self):
        bindir = os.path.join(bockbuild.package_root, "bin")
        for path, dirs, files in os.walk(bindir):
            for name in files:
                f = os.path.join(path, name)
                file_type = backtick('file "%s"' % f)
                if "Mach-O executable" in "".join(file_type):
                    self.verify(f)

    def shell(self):
        envscript = '''#!/bin/sh
        PROFNAME="%s"
        INSTALLDIR="%s"
        ROOT="%s"
        export DYLD_FALLBACK_LIBRARY_PATH="$INSTALLDIR/lib:/lib:/usr/lib"
        export ACLOCAL_PATH="$INSTALLDIR/share/aclocal"
        export CONFIG_SITE="$INSTALLDIR/$PROFNAME-config.site"
        export MONO_GAC_PREFIX="$INSTALLDIR"
        export MONO_ADDINS_REGISTRY="$ROOT/addinreg"
        export MONO_INSTALL_PREFIX="$INSTALLDIR"

        export PS1="\[\e[1;3m\][$PROFNAME] \w @ "
        bash -i
        ''' % (self.profile_name, self.staged_prefix, self.root)

        path = os.path.join(self.root, self.profile_name + '.sh')

        with open(path, 'w') as f:
            f.write(envscript)

        os.chmod(path, os.stat(path).st_mode | stat.S_IEXEC)

        subprocess.call(['bash', '-c', path])

MonoReleaseProfile()