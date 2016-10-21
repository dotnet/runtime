class GtkQuartzEnginePackage (Package):

    def __init__(self):
        Package.__init__(self, 'gtk-quartz-engine',
                         sources=[
                             'git://github.com/mono/gtk-quartz-engine.git'],
                         override_properties={
                             'configure': './autogen.sh --prefix=%{package_prefix}',
                             'needs_lipo': True
                         },
                         revision='9555a08f0c9c98d02153c9d77b54a2dd83ce5d6f')

GtkQuartzEnginePackage()
