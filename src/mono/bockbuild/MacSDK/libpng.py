class LibPngPackage (Package):

    def __init__(self):
        Package.__init__(self, 'libpng', '1.4.12',
                         sources=[
                             'http://downloads.sourceforge.net/project/libpng/libpng14/older-releases/1.4.12/libpng-1.4.12.tar.xz'],
                         configure_flags=['--enable-shared'])

LibPngPackage()
