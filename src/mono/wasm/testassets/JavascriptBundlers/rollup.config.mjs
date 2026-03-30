import nodeResolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import babel from '@rollup/plugin-babel';
import replace from '@rollup/plugin-replace';
import files from 'rollup-plugin-import-file';

function getConfig(fileName) {
    return {
        input: fileName,
        output: {
            file: `public/${fileName}`,
            format: 'esm'
        },
        plugins: [
            files({
                output: 'public',
                extensions: /\.(wasm|dat|pdb)$/,
                hash: true,
            }),
            files({
                output: 'public',
                extensions: /\.(json|txt)$/,
            }),
            nodeResolve({
                extensions: ['.js']
            }),
            babel({
                babelHelpers: 'bundled',
                extensions: ['.js'],
                generatorOpts: {
                    // Increase the size limit from 500KB to 10MB
                    compact: true,
                    retainLines: true,
                    maxSize: 10000000
                }
            }),
            commonjs(),
            replace({
                preventAssignment: false,
                'process.env.NODE_ENV': '"production"'
            }),
        ]
    }
}

export default [getConfig('main.js'), getConfig('profiler.js')]
