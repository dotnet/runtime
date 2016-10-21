class CairoPackage (CairoGraphicsXzPackage):

    def __init__(self):
        CairoGraphicsXzPackage.__init__(self, 'cairo', '1.12.14')
        self.sources.extend([
            'patches/cairo-quartz-crash.patch',
            'patches/cairo-fix-color-bitmap-fonts.patch',
            'patches/cairo-fix-CGFontGetGlyphPath-deprecation.patch',
            #			'patches/cairo-cglayer.patch',
        ])

    def prep(self):
        Package.prep(self)

        if Package.profile.name == 'darwin':
            for p in range(1, len(self.local_sources)):
                self.sh('patch -p1 < "%{local_sources[' + str(p) + ']}"')

    def build(self):
        self.configure_flags = [
            '--enable-pdf',
        ]

        if Package.profile.name == 'darwin':
            self.configure_flags.extend([
                '--enable-quartz',
                '--enable-quartz-font',
                '--enable-quartz-image',
                '--disable-xlib',
                '--without-x'
            ])
        elif Package.profile.name == 'linux':
            self.configure_flags.extend([
                '--disable-quartz',
                '--with-x'
            ])

        Package.build(self)

CairoPackage()
