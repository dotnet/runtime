class LibTiffPackage (Package):

    def __init__(self):
        Package.__init__(self, 'tiff', '4.0.3',
                         configure_flags=[
                         ],
                         sources=[
                             'http://download.osgeo.org/libtiff/tiff-%{version}.tar.gz',
                         ])

        self.needs_lipo = True

        if Package.profile.name == 'darwin':
            self.sources.extend([
                # Fix Snow Leopard build
                # http://jira.freeswitch.org/secure/attachment/17487/tiff-4.0.2-macosx-2.patch
                'patches/tiff-4.0.2-macosx-2.patch'
            ])

    def prep(self):
        Package.prep(self)
        if Package.profile.name == 'darwin':
            for p in range(1, len(self.local_sources)):
                self.sh('patch -p1 < "%{local_sources[' + str(p) + ']}"')

LibTiffPackage()
