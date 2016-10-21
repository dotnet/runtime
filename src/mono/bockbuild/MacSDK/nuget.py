
class NuGetPackage(GitHubPackage):

    def __init__(self):
        GitHubPackage.__init__(self,
                               'mono', 'nuget',
                               '2.12.0',
                               '9e2d2c1cc09d2a40eeb72e8c5db789e3b9bf2586',
                               configure='')

    def build(self):
        self.sh('%{make} update_submodules')
        self.sh('%{make} PREFIX=%{package_prefix}')

    def install(self):
        self.sh('%{makeinstall} PREFIX=%{staged_prefix}')

NuGetPackage()
