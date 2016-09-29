from bockbuild.package import Package


class MonoExtensionsPackage(Package):

    def __init__(self):
        Package.__init__(self, 'mono-extensions', None,
                         sources=['git@github.com:xamarin/mono-extensions.git'],
                         git_branch=self.profile.release_packages[
                             'mono'].git_branch
                         )
        self.source_dir_name = 'mono-extensions'

        # Mono pull requests won't have mono-extensions branches
        if not self.git_branch or 'pull/' in self.git_branch:
            warn('Using master branch for mono_extensions')
            self.git_branch = 'master'

    def build(self):
        pass

    def install(self):
        pass

MonoExtensionsPackage()
