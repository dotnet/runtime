class LibJpegPackage (Package):

    def __init__(self):
        Package.__init__(self, 'libjpeg', '8', sources=[
                         'http://www.ijg.org/files/jpegsrc.v8.tar.gz'])
        self.source_dir_name = 'jpeg-8'

LibJpegPackage()
