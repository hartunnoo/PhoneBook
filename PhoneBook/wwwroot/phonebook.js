window.readFile = function (inputId) {
    return new Promise((resolve) => {
        const input = document.getElementById(inputId);
        if (!input || !input.files || input.files.length === 0) { resolve(null); return; }
        const file = input.files[0];
        const reader = new FileReader();
        reader.onload = function (e) { resolve({ name: file.name, size: file.size, base64: e.target.result.split(',')[1] }); };
        reader.onerror = function () { resolve(null); };
        reader.readAsDataURL(file);
    });
};

window.toggleDarkMode = function () {
    var el = document.documentElement;
    var isDark = el.getAttribute('data-theme') === 'dark';
    if (isDark) { el.removeAttribute('data-theme'); localStorage.setItem('pb_theme', 'light'); }
    else { el.setAttribute('data-theme', 'dark'); localStorage.setItem('pb_theme', 'dark'); }
    return !isDark;
};

window.initTheme = function () {
    var saved = localStorage.getItem('pb_theme');
    if (saved === 'dark') document.documentElement.setAttribute('data-theme', 'dark');
};
