class FsharpPackage(GitHubTarballPackage):

    def __init__(self):
        GitHubTarballPackage.__init__(self,
                                      'fsharp', 'fsharp',
                                      '4.0.1.9',
                                      '0a6c66a8f18eb8a5c4d0bfac61d883b6994a918a',
                                      configure='./configure --prefix="%{package_prefix}"')

        self.extra_stage_files = [
            'lib/mono/xbuild/Microsoft/VisualStudio/v/FSharp/Microsoft.FSharp.Targets']

    def prep(self):
        Package.prep(self)

        for p in range(1, len(self.sources)):
            self.sh('patch -p1 < "%{local_sources[' + str(p) + ']}"')

    def build(self):
        self.sh('autoreconf')
        Package.configure(self)
        Package.make(self)

FsharpPackage()
