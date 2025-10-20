import nodeResolve from '@rollup/plugin-node-resolve';
import commonjs from '@rollup/plugin-commonjs';
import babel from '@rollup/plugin-babel';
import replace from '@rollup/plugin-replace';
import files from 'rollup-plugin-import-file';

export default {
   input: 'main.js',
   output: {
      file: 'public/main.js',
      format: 'esm'
   },
   plugins: [
      files({
        output: 'public',
        extensions: /\.(wasm|dat)$/,
        hash: true,
      }),
      files({
        output: 'public',
        extensions: /\.(json)$/,
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