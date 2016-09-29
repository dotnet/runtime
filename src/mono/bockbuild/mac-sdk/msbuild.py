import fileinput


class MSBuild (GitHubPackage):

    def __init__(self):
        GitHubPackage.__init__(self, 'mono', 'msbuild', '15.0',
                               git_branch='xplat-c8p')

    def build(self):
        self.sh('./cibuild.sh --scope Compile --target Mono --host Mono')

    def install(self):
        # adjusted from 'install-mono-prefix.sh'

        build_output = 'bin/Debug-MONO/OSX_Deployment'
        new_location = os.path.join(
            self.staged_prefix,
            'lib/mono/msbuild/%s/bin' %
            self.version)
        bindir = os.path.join(self.staged_prefix, 'bin')

        os.makedirs(new_location)
        self.sh('cp -R %s/* %s' % (build_output, new_location))

        os.makedirs(bindir)

        self.sh('cp msbuild-mono-deploy.in %s/msbuild' % bindir)

        for line in fileinput.input('%s/msbuild' % bindir, inplace=True):
            line = line.replace('@bindir@', '%s/bin' % self.staged_prefix)
            line = line.replace(
                '@mono_instdir@',
                '%s/lib/mono' %
                self.staged_prefix)
            print line

        for excluded in glob.glob("%s/*UnitTests*" % new_location):
            self.rm(excluded)

        for excluded in glob.glob("%s/*xunit*" % new_location):
            self.rm(excluded)

MSBuild()
