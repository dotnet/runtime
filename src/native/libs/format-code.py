# The build machines for Ubuntu don't have clang-format installed yet
# so return 0 immediately until that is fixed
import sys
sys.exit(0)

#import argparse
#import glob
#import os
#import subprocess
#import sys
#
#class A(object):
#    pass
#
#a = A()
#parser = argparse.ArgumentParser(description='Formats native code with clang-format')
#parser.add_argument('checkonly', nargs='?', help='Do not overwrite the files with format changes, only check what would happen')
#parser.parse_args(namespace=a)
#
#try:
#    subprocess.call(['clang-format', '--version'])
#    cf='clang-format '
#except:
#    try:
#        subprocess.call(['clang-format-3.6', ' --version'])
#        cf='clang-format-3.6 '
#    except:
#        print 'Clang Format v3.6+ is required'
#        sys.exit(-1)
#
#if a.checkonly is None:
#    args = '-style=file -i '
#else:
#    args = '-style=file -output-replacements-xml '
#
#print 'Running clang-format style cop on native code...'
#
#os.chdir(os.path.dirname(os.path.realpath(__file__)))
#
#extensions=['cpp', 'c', 'h', 'hpp', 'hxx', 'in']
#for extension in extensions:
#    for nativefile in glob.glob('**/*.' + extension):
#        cmd = cf + args + os.path.realpath(nativefile)
#        print 'Formatting native file with command: ' + cmd
#        if a.checkonly is None:
#            subprocess.call(cmd, shell=True)
#        else:
#            if len(subprocess.check_output(cmd, shell=True)) != 74:
#                print 'File not style compliant, exiting: ' + nativefile
#                sys.exit(-1)
