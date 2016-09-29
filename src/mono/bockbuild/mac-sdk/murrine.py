class MurrinePackage (GnomeXzPackage):

    def __init__(self):
        GnomePackage.__init__(self,
                              'murrine',
                              version_major='0.98',
                              version_minor='2')

        # FIXME: this may need porting
        # self.sources.append ('patches/murrine-osx.patch')

    def prep(self):
        Package.prep(self)

MurrinePackage()
