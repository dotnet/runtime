class PangoPackage (GnomeXzPackage):

    def __init__(self):
        GnomePackage.__init__(self,
                              'pango',
                              version_major='1.35',
                              version_minor='0',
                              configure_flags=[
                                  '--without-x',
                                  '--enable-gtk-doc-html=no'
                              ]
                              )

        self.sources.extend([
            # 1
            # Bug 321419 - Allow environment var substitution in Pango config
            # https://bugzilla.gnome.org/show_bug.cgi?id=321419
            'patches/pango-relative-config-file.patch',

            # BXC 10257 - Characters outside the Basic Multilingual Plane don't render correctly
            # https://bugzilla.xamarin.com/show_bug.cgi?id=10257
            'patches/pango-coretext-astral-plane-1.patch',
            'patches/pango-coretext-astral-plane-2.patch',

            # Bug 15787 - Caret position is wrong when there are ligatures
            # https://bugzilla.xamarin.com/show_bug.cgi?id=15787
            'patches/pango-disable-ligatures.patch',

            # https://bugzilla.xamarin.com/show_bug.cgi?id=22199
            'patches/pango-fix-ct_font_descriptor_get_weight-crasher.patch',

            # https://bugzilla.gnome.org/show_bug.cgi?id=734372
            'patches/pango-coretext-condensed-trait.patch',

            # https://bugzilla.xamarin.com/show_bug.cgi?id=32938
            'patches/pango-coretext-fix-yosemite-crasher.patch',

            'patches/pango-system-font-single.patch',
            'patches/pango-system-font-check-version.patch'
        ])

    def prep(self):
        GnomePackage.prep(self)
        if Package.profile.name == 'darwin':
            for p in range(1, len(self.local_sources)):
                self.sh('patch -p1 < "%{local_sources[' + str(p) + ']}"')

    def deploy(self):
        self.sh('pango-querymodules --update-cache')

PangoPackage()
