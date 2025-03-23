export function saveProfile(Module) {
    const fileName = "output.mlpd";
    let profileData = readProfileFile(Module, fileName);

    if (!profileData) {
        console.error("Profile data is empty or could not be read.");
        return;
    }

    const a = document.createElement('a');
    const blob = new Blob([profileData]);
    a.href = URL.createObjectURL(blob);
    a.download = fileName;
    // Append anchor to body.
    document.body.appendChild(a);
    a.click();

    console.log(`TestOutput -> Profile data of size ${profileData.length} bytes started downloading.`);

    // Remove anchor from body
    document.body.removeChild(a);
}

function readProfileFile(Module, fileName) {

    var stat = Module.FS.stat(fileName);

    if (stat && stat.size > 0) {
        return Module.FS.readFile(fileName);
    }
    else {
        console.debug(`Unable to fetch the profile file ${fileName} as it is empty`);
        return null;
    }
}