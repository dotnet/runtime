export function saveProfile(Module) {
    let profileData = readProfileFile(Module);

    const a = document.createElement('a');
    const blob = new Blob([profileData]);
    a.href = URL.createObjectURL(blob);
    a.download = "output.mlpd";
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    // Remove anchor from body
    document.body.removeChild(a);
}

function readProfileFile(Module) {
    let profileFilePath="output.mlpd";

    var stat = Module.FS.stat(profileFilePath);

    if (stat && stat.size > 0) {
        return Module.FS.readFile(profileFilePath);
    }
    else {
        console.debug(`Unable to fetch the profile file ${profileFilePath} as it is empty`);
        return null;
    }
}