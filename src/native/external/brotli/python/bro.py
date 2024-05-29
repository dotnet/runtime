#! /usr/bin/env python
"""Compression/decompression utility using the Brotli algorithm."""

# Note: Python2 has been deprecated long ago, but some projects out in
# the wide world may still use it nevertheless. This should not
# deprive them from being able to run Brotli.
from __future__ import print_function

import argparse
import os
import platform
import sys

import brotli


# default values of encoder parameters
_DEFAULT_PARAMS = {
    'mode': brotli.MODE_GENERIC,
    'quality': 11,
    'lgwin': 22,
    'lgblock': 0,
}


def get_binary_stdio(stream):
    """Return the specified stdin/stdout/stderr stream.

    If the stdio stream requested (i.e. sys.(stdin|stdout|stderr))
    has been replaced with a stream object that does not have a `.buffer`
    attribute, this will return the original stdio stream's buffer, i.e.
    `sys.__(stdin|stdout|stderr)__.buffer`.

    Args:
      stream: One of 'stdin', 'stdout', 'stderr'.

    Returns:
      The stream, as a 'raw' buffer object (i.e. io.BufferedIOBase subclass
      instance such as io.Bufferedreader/io.BufferedWriter), suitable for
      reading/writing binary data from/to it.
    """
    if stream == 'stdin': stdio = sys.stdin
    elif stream == 'stdout': stdio = sys.stdout
    elif stream == 'stderr': stdio = sys.stderr
    else:
        raise ValueError('invalid stream name: %s' % (stream,))
    if sys.version_info[0] < 3:
        if sys.platform == 'win32':
            # set I/O stream binary flag on python2.x (Windows)
            runtime = platform.python_implementation()
            if runtime == 'PyPy':
                # the msvcrt trick doesn't work in pypy, so use fdopen().
                mode = 'rb' if stream == 'stdin' else 'wb'
                stdio = os.fdopen(stdio.fileno(), mode, 0)
            else:
                # this works with CPython -- untested on other implementations
                import msvcrt
                msvcrt.setmode(stdio.fileno(), os.O_BINARY)
        return stdio
    else:
        try:
            return stdio.buffer
        except AttributeError:
            # The Python reference explains
            # (-> https://docs.python.org/3/library/sys.html#sys.stdin)
            # that the `.buffer` attribute might not exist, since
            # the standard streams might have been replaced by something else
            # (such as an `io.StringIO()` - perhaps via
            # `contextlib.redirect_stdout()`).
            # We fall back to the original stdio in these cases.
            if stream == 'stdin': return sys.__stdin__.buffer
            if stream == 'stdout': return sys.__stdout__.buffer
            if stream == 'stderr': return sys.__stderr__.buffer
            assert False, 'Impossible Situation.'


def main(args=None):

    parser = argparse.ArgumentParser(
        prog=os.path.basename(__file__), description=__doc__)
    parser.add_argument(
        '--version', action='version', version=brotli.version)
    parser.add_argument(
        '-i',
        '--input',
        metavar='FILE',
        type=str,
        dest='infile',
        help='Input file',
        default=None)
    parser.add_argument(
        '-o',
        '--output',
        metavar='FILE',
        type=str,
        dest='outfile',
        help='Output file',
        default=None)
    parser.add_argument(
        '-f',
        '--force',
        action='store_true',
        help='Overwrite existing output file',
        default=False)
    parser.add_argument(
        '-d',
        '--decompress',
        action='store_true',
        help='Decompress input file',
        default=False)
    params = parser.add_argument_group('optional encoder parameters')
    params.add_argument(
        '-m',
        '--mode',
        metavar='MODE',
        type=int,
        choices=[0, 1, 2],
        help='The compression mode can be 0 for generic input, '
        '1 for UTF-8 encoded text, or 2 for WOFF 2.0 font data. '
        'Defaults to 0.')
    params.add_argument(
        '-q',
        '--quality',
        metavar='QUALITY',
        type=int,
        choices=list(range(0, 12)),
        help='Controls the compression-speed vs compression-density '
        'tradeoff. The higher the quality, the slower the '
        'compression. Range is 0 to 11. Defaults to 11.')
    params.add_argument(
        '--lgwin',
        metavar='LGWIN',
        type=int,
        choices=list(range(10, 25)),
        help='Base 2 logarithm of the sliding window size. Range is '
        '10 to 24. Defaults to 22.')
    params.add_argument(
        '--lgblock',
        metavar='LGBLOCK',
        type=int,
        choices=[0] + list(range(16, 25)),
        help='Base 2 logarithm of the maximum input block size. '
        'Range is 16 to 24. If set to 0, the value will be set based '
        'on the quality. Defaults to 0.')
    # set default values using global _DEFAULT_PARAMS dictionary
    parser.set_defaults(**_DEFAULT_PARAMS)

    options = parser.parse_args(args=args)

    if options.infile:
        try:
            with open(options.infile, 'rb') as infile:
                data = infile.read()
        except OSError:
            parser.error('Could not read --infile: %s' % (infile,))
    else:
        if sys.stdin.isatty():
            # interactive console, just quit
            parser.error('No input (called from interactive terminal).')
        infile = get_binary_stdio('stdin')
        data = infile.read()

    if options.outfile:
        # Caution! If `options.outfile` is a broken symlink, will try to
        # redirect the write according to symlink.
        if os.path.exists(options.outfile) and not options.force:
            parser.error(('Target --outfile=%s already exists, '
                          'but --force was not requested.') % (outfile,))
        outfile = open(options.outfile, 'wb')
        did_open_outfile = True
    else:
        outfile = get_binary_stdio('stdout')
        did_open_outfile = False
    try:
        try:
            if options.decompress:
                data = brotli.decompress(data)
            else:
                data = brotli.compress(
                    data,
                    mode=options.mode,
                    quality=options.quality,
                    lgwin=options.lgwin,
                    lgblock=options.lgblock)
            outfile.write(data)
        finally:
            if did_open_outfile: outfile.close()
    except brotli.error as e:
        parser.exit(1,
                    'bro: error: %s: %s' % (e, options.infile or '{stdin}'))


if __name__ == '__main__':
    main()
