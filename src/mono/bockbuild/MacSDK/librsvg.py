class LibrsvgPackage(GnomeXzPackage):

    def __init__(self):
        GnomeXzPackage.__init__(self, 'librsvg', version_major='2.37', version_minor='0',
                                configure_flags=['--disable-Bsymbolic', '--disable-introspection'])

        make = 'make DESTDIR=%{stage_root}'

    def install(self):
        # handle some mislocation
        misdir = '%s%s' % (self.stage_root, self.staged_profile)
        unprotect_dir(self.stage_root)

        Package.install(self)
        # scoop up
        if not os.path.exists(misdir):
            error('Could not find mislocated libsrvg files')

        self.sh(
            'rsync -a --ignore-existing %s/* %s' %
            (misdir, self.staged_prefix))
        self.sh('rm -rf %s/*' % misdir)

    def deploy(self):
        self.sh('gdk-pixbuf-query-loaders --update-cache')

LibrsvgPackage()
