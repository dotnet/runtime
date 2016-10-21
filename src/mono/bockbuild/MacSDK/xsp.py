class XspPackage (GitHubTarballPackage):

    def __init__(self):
        GitHubTarballPackage.__init__(self, 'mono', 'xsp', '4.4',
                                      'c98e068f5647fb06ff2fbef7cd5f1b35417362b1',
                                      configure='./autogen.sh --prefix="%{package_prefix}"')

    def install(self):
        # scoop up some mislocated files
        misdir = '%s%s' % (self.stage_root, self.staged_profile)
        unprotect_dir(self.stage_root)
        Package.install(self)
        if not os.path.exists(misdir):
            for path in iterate_dir(self.stage_root):
                print path
            error('Could not find mislocated files')

        self.sh('rsync -a --ignore-existing %s/* %s' %
                (misdir, self.profile.staged_prefix))
        self.sh('rm -rf %s/*' % misdir)


XspPackage()
