'use strict';

const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const fs = require('node:fs');
const path = require('node:path');

const [metadataPath, mode, rootArgument, band] = process.argv.slice(2);
assert.ok(metadataPath && rootArgument, 'Expected metadata, mode, and provisioned root.');
assert.ok(mode === 'transport' || mode === 'workload', `Unknown validation mode ${mode}.`);
const expected = JSON.parse(fs.readFileSync(metadataPath, 'utf8'));
const root = fs.realpathSync(rootArgument);
const os = { darwin: 'osx', win32: 'win', linux: 'linux' }[process.platform];
assert.ok(os, `Unsupported host ${process.platform}.`);
const musl = os === 'linux' && !process.report.getReport().header.glibcVersionRuntime;
const rid = `${os}${musl ? '-musl' : ''}-${process.arch}`;
const binaryHash = sha256(fs.readFileSync(process.execPath));
assert.ok(expected.binaries[rid], `No verified transport binary for ${rid}.`);
assert.equal(binaryHash, expected.binaries[rid], 'The executable is not the verified Node binary.');
assert.equal(process.versions.node, expected.node);
assert.equal(process.versions.undici, expected.embeddedUndici);

function sha256(bytes) {
  return crypto.createHash('sha256').update(bytes).digest('hex');
}

function sameFile(actual, wanted) {
  assert.equal(fs.realpathSync(actual), fs.realpathSync(wanted));
}

function manifestFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) return manifestFiles(fullPath);
    return entry.name === 'WorkloadManifest.json' ? [fullPath] : [];
  });
}

const receipt = {
  mode,
  rid,
  executable: process.execPath,
  binarySha256: binaryHash,
  node: process.versions.node,
  embeddedUndici: process.versions.undici,
  nodeCommit: expected.nodeCommit,
  nodePackageVersion: expected.nodePackageVersion,
};

if (mode === 'transport') {
  sameFile(process.execPath, path.join(root, 'bin', os === 'win' ? 'node.exe' : 'node'));
  const npmRoot = path.join(root, os === 'win' ? 'bin' : 'lib', 'node_modules', 'npm');
  const npm = JSON.parse(fs.readFileSync(path.join(npmRoot, 'package.json'), 'utf8'));
  const undiciRoot = path.join(npmRoot, 'node_modules', 'undici');
  const undici = JSON.parse(fs.readFileSync(path.join(undiciRoot, 'package.json'), 'utf8'));
  assert.equal(npm.version, expected.npm);
  assert.equal(undici.version, expected.npmUndici);
  const fetchSource = fs.readFileSync(path.join(undiciRoot, 'lib', 'web', 'fetch', 'index.js'), 'utf8');
  assert.equal(sha256(fetchSource.replaceAll('\r\n', '\n')), expected.npmUndiciFetchSha256,
               'The provisioned npm fetch implementation differs from the verified fix.');
  receipt.npm = npm.version;
  receipt.npmUndici = undici.version;
} else {
  assert.ok(band, 'The installed workload SDK band must be provided.');
  assert.ok(expected.workload, 'No verified VMR workload identity is supplied.');
  const workload = expected.workload;
  const packageId = workload.nodePackages[rid];
  assert.ok(packageId, `No verified workload Node pack for ${rid}.`);
  const tools = path.join(root, 'packs', packageId, workload.nodePackVersion, 'tools');
  sameFile(process.execPath, path.join(tools, 'bin', os === 'win' ? 'node.exe' : 'node'));
  assert.equal(fs.existsSync(path.join(tools, 'lib', 'node_modules', 'npm')), false);
  assert.equal(fs.existsSync(path.join(tools, 'bin', 'node_modules', 'npm')), false);
  const manifestBand = path.join(root, 'sdk-manifests', band);
  const directories = fs.readdirSync(manifestBand)
    .filter((name) => name.toLowerCase() === 'microsoft.net.workload.emscripten.current');
  assert.equal(directories.length, 1, 'Expected one current Emscripten manifest directory.');
  const manifests = manifestFiles(path.join(manifestBand, directories[0]));
  assert.equal(manifests.length, 1, 'Expected one installed current Emscripten manifest.');
  const manifestBytes = fs.readFileSync(manifests[0]);
  assert.equal(sha256(manifestBytes), workload.manifestJsonSha256,
               'The installed Emscripten manifest is not the verified VMR manifest.');
  const manifest = JSON.parse(manifestBytes.toString('utf8').replace(/^\uFEFF/, ''));
  const declarations = Object.entries(manifest.packs).filter(([id, pack]) =>
    id === packageId || Object.values(pack['alias-to'] || {}).includes(packageId));
  assert.equal(declarations.length, 1, 'The manifest does not unambiguously select this Node pack.');
  assert.equal(declarations[0][1].version, workload.nodePackVersion);
  receipt.vmrCommit = workload.vmrCommit;
  receipt.vmrBuild = workload.vmrBuild;
  receipt.manifestPackage = workload.manifestPackage;
  receipt.manifestVersion = workload.manifestVersion;
  receipt.installedManifest = manifests[0];
  receipt.manifestJsonSha256 = sha256(manifestBytes);
  receipt.nodePack = packageId;
  receipt.nodePackVersion = workload.nodePackVersion;
  receipt.npmBundled = false;
}

console.log(`NODE_VALIDATION ${JSON.stringify(receipt)}`);
