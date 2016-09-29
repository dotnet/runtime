import os
import string


class IronLanguagesPackage(GitHubTarballPackage):

    def __init__(self):
        GitHubTarballPackage.__init__(self,
                                      'IronLanguages', 'iron-languages',
                                      '2.11',
                                      'de63773744ccf9873c1826470730ae0446fd64d7',
                                      configure='')

        # override: avoid naming the package 'main' because of the repo name
        self.sources = [
            'https://github.com/%{organization}/main/tarball/%{revision}']
        self.source_dir_name = '%s-%s-%s' % (
            self.organization, 'main', self.revision[:7])

    def build(self):
        self.ironruby = os.path.join(
            self.workspace, 'ironruby', 'bin') + os.sep
        self.ironpython = os.path.join(
            self.workspace, 'ironpython', 'bin') + os.sep
        self.sh(
            'xbuild /p:Configuration=Release /p:OutDir="%{ironruby}" Solutions/Ruby.sln')
        self.sh(
            'xbuild /p:Configuration=Release /p:OutDir="%{ironpython}" Solutions/IronPython.Mono.sln')

    def install_ruby_scripts(self, path, installdir):
        for cmd, ext in map(os.path.splitext, os.listdir(path)):
            if ext != '.exe':
                continue
            wrapper = os.path.join(self.staged_prefix, "bin", cmd)
            with open(wrapper, "w") as output:
                output.write("#!/bin/sh\n")
                output.write(
                    "exec {0}/bin/mono {0}/lib/{1}/{2}.exe \"$@\"\n".format(
                        self.staged_prefix, installdir, cmd))
            os.chmod(wrapper, 0o755)

    def install_python_scripts(self, path, installdir):
        for cmd, ext in map(os.path.splitext, os.listdir(path)):
            if ext != '.exe':
                continue
            wrapper = os.path.join(self.staged_prefix, "bin", cmd)
            with open(wrapper, "w") as output:
                output.write("#!/bin/sh\n")
                output.write(
                    'export IRONPYTHONPATH=/System/Library/Frameworks/Python.framework/Versions/2.7/lib/python2.7/\n')
                output.write(
                    "exec {0}/bin/mono {0}/lib/{1}/{2}.exe \"$@\"\n".format(
                        self.staged_prefix, installdir, cmd))
            os.chmod(wrapper, 0o755)

    def install(self):
        self.sh("mkdir -p %{staged_prefix}/lib/ironruby/")
        self.sh("mkdir -p %{staged_prefix}/bin/")
        self.sh("cp -R %{ironruby} %{staged_prefix}/lib/ironruby/")
        self.install_ruby_scripts(self.ironruby, 'ironruby')

        self.sh("mkdir -p %{staged_prefix}/lib/ironpython/")
        self.sh("cp -R %{ironpython} %{staged_prefix}/lib/ironpython/")
        self.install_python_scripts(self.ironpython, 'ironpython')

IronLanguagesPackage()
