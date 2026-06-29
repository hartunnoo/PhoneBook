window.readFile = function (inputId) {
    return new Promise((resolve) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) {
            resolve(null);
            return;
        }
        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = function (e) {
            const base64 = e.target.result.split(',')[1];
            resolve({ name: file.name, size: file.size, base64: base64 });
        };
        reader.onerror = function () { resolve(null); };
        reader.readAsDataURL(file);
    });
};
