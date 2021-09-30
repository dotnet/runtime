import { defineConfig } from 'rollup';
import typescript from '@rollup/plugin-typescript';
import { terser } from 'rollup-plugin-terser';
import { readFile, writeFile, mkdir } from 'fs/promises';
import * as fs from 'fs';
import { createHash } from 'crypto';

const outputFileName = 'runtime.iffe.js'
const isDebug = process.env.Configuration !== 'Release';
const nativeBinDir = process.env.NativeBinDir ? process.env.NativeBinDir.replace(/\"/g, '') : 'bin';
const plugins = isDebug ? [writeOnChangePlugin()] : [terser(), writeOnChangePlugin()]

export default defineConfig({
  treeshake: !isDebug,
  input: 'src/startup.ts',
  output: [{
    banner: '//! Licensed to the .NET Foundation under one or more agreements.\n//! The .NET Foundation licenses this file to you under the MIT license.\n',
    name: '__dotnet_runtime',
    file: nativeBinDir + '/src/' + outputFileName,

    // emcc doesn't know how to load ES6 module, that's why we need the whole rollup.js
    format: 'iife',
    plugins: plugins
  }],
  plugins: [typescript()]
});

// this would create .md5 file next to the output file, so that we do not touch datetime of the file if it's same -> faster incremental build.
function writeOnChangePlugin() {
  return {
    name: 'writeOnChange',
    generateBundle: writeWhenChanged
  }
}

async function writeWhenChanged(options, bundle) {
  try {
    const asset = bundle[outputFileName];
    const code = asset.code;
    const hashFileName = options.file + '.sha256';
    const oldHashExists = await checkFileExists(hashFileName);
    const oldFileExists = await checkFileExists(options.file)

    var newHash = createHash('sha256').update(code).digest('hex');

    let isOutputChanged = true;
    if (oldHashExists && oldFileExists) {
      const oldHash = await readFile(hashFileName, { encoding: 'ascii' });
      isOutputChanged = oldHash !== newHash
    }
    if (isOutputChanged) {
      if (!await checkFileExists(hashFileName)) {
        await mkdir(nativeBinDir + '/src', { recursive: true });
      }
      await writeFile(hashFileName, newHash);
    } else {
      // this.warn('No change in ' + options.file)
      delete bundle[outputFileName]
    }
  } catch (ex) {
    this.warn(ex.toString());
  }
}

function checkFileExists(file) {
  return fs.promises.access(file, fs.constants.F_OK)
    .then(() => true)
    .catch(() => false)
}