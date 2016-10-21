class GlibPackage (GnomeXzPackage):

    def __init__(self):
        GnomeXzPackage.__init__(self,
                                'glib',
                                version_major='2.36',
                                version_minor='4')

        self.darwin = Package.profile.name == 'darwin'

        if self.darwin:
            # link to specific revisions for glib 2.30.x
            self.sources.extend([
                # https://trac.macports.org/export/91680/trunk/dports/devel/glib2/files/config.h.ed
                'patches/glib/config.h.ed',
                # https://trac.macports.org/export/98985/trunk/dports/devel/glib2/files/patch-configure.diff
                'patches/glib/patch-configure.diff',
                # https://trac.macports.org/export/42728/trunk/dports/devel/glib2/files/patch-gi18n.h.diff
                'patches/glib/patch-gi18n.h.diff',
                # https://trac.macports.org/export/92608/trunk/dports/devel/glib2/files/patch-gio_gdbusprivate.c.diff
                'patches/glib/patch-gio_gdbusprivate.c.diff',
                # https://trac.macports.org/export/49466/trunk/dports/devel/glib2/files/patch-gio_xdgmime_xdgmime.c.diff
                'patches/glib/patch-gio_xdgmime_xdgmime.c.diff',
                # https://trac.macports.org/export/91680/trunk/dports/devel/glib2/files/patch-glib-2.0.pc.in.diff
                'patches/glib/patch-glib-2.0.pc.in.diff',
                # https://trac.macports.org/export/64476/trunk/dports/devel/glib2/files/patch-glib_gunicollate.c.diff
                'patches/glib/patch-glib_gunicollate.c.diff',

                # Bug 6156 - [gtk] Quitting the application with unsaved file and answering Cancel results in crash
                # https://bugzilla.xamarin.com/attachment.cgi?id=2214
                'patches/glib-recursive-poll.patch',
            ])

    def prep(self):
        Package.prep(self)
        if self.darwin:
            for p in range(2, 8):
                self.sh('patch -p0 < %{local_sources[' + str(p) + ']}')
            for p in range(8, len(self.local_sources)):
                self.sh(
                    'patch --ignore-whitespace -p1 < %{local_sources[' + str(p) + ']}')

    def arch_build(self, arch):
        if arch == 'darwin-universal':  # multi-arch  build pass
            self.local_ld_flags = ['-arch i386', '-arch x86_64']
            self.local_gcc_flags = ['-arch i386', '-arch x86_64', '-Os']
            self.local_configure_flags = ['--disable-dependency-tracking']
        else:
            Package.arch_build(self, arch)

        if self.darwin:
            self.local_configure_flags.extend(['--disable-compile-warnings'])

    def build(self):
        # modified build for darwin
        if self.darwin:
            self.local_configure_flags.extend(['--disable-compile-warnings'])
            Package.configure(self)
            self.sh([
                # 'autoconf',
                    #'%{configure} --disable-compile-warnings',
                    'ed - config.h < %{local_sources[1]}',
                    # work around
                    # https://bugzilla.gnome.org/show_bug.cgi?id=700350
                    'touch docs/reference/*/Makefile.in',
                    'touch docs/reference/*/*/Makefile.in',
                    #'%{make}'
                    ])
            Package.make(self)
        else:
            Package.build(self)

    def install(self):
        Package.install(self)
        if self.darwin:
            # FIXME: necessary?
            self.sh('rm -f %{staged_prefix}/lib/charset.alias')

GlibPackage()
