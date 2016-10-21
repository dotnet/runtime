
class MonoBasicPackage (GitHubTarballPackage):

    def __init__(self):
        GitHubTarballPackage.__init__(self, 'mono', 'mono-basic', '4.6', 'c93133db1d511f994918391f429fee29b9250004',
                                      configure='./configure --prefix="%{staged_profile}"')

    def install(self):
        self.sh('./configure --prefix="%{staged_prefix}"')
        self.sh('make install')

MonoBasicPackage()
