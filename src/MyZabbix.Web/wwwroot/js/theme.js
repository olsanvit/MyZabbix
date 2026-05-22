// Apply saved theme immediately to avoid flash-of-wrong-theme
(function () {
    const t = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-bs-theme', t);
})();

function setTheme(dark) {
    const theme = dark ? 'dark' : 'light';
    document.documentElement.setAttribute('data-bs-theme', theme);
    localStorage.setItem('theme', theme);
}

function getThemeIsDark() {
    return localStorage.getItem('theme') === 'dark';
}

function downloadBase64File(filename, contentType, base64) {
    const a = document.createElement('a');
    a.href = 'data:' + contentType + ';base64,' + base64;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}
