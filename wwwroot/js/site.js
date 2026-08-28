// Write your JavaScript code.

// ---- Dark mode toggle ------------------------------------------------
// Theme is applied early (in _Layout's <head>) to avoid a flash of light
// mode; this just wires up the button and keeps localStorage in sync.
(function () {
    var toggle = document.getElementById('themeToggle');
    if (!toggle) return;

    function currentTheme() {
        return document.documentElement.getAttribute('data-bs-theme') || 'light';
    }

    function applyIcon() {
        toggle.textContent = currentTheme() === 'dark' ? '☀️' : '🌙';
    }

    toggle.addEventListener('click', function () {
        var next = currentTheme() === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-bs-theme', next);
        localStorage.setItem('theme', next);
        applyIcon();
    });

    applyIcon();
})();
