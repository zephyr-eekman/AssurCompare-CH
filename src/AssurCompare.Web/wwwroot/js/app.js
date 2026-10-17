// Petites fonctions navigateur utilisées par l'application Blazor.
window.assurApp = {
    init(dotnet) {
        document.addEventListener('keydown', e => {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                dotnet.invokeMethodAsync('OnCommandPalette');
            } else if (e.key === 'Escape') {
                dotnet.invokeMethodAsync('OnEscape');
            }
        });

        const mobile = window.matchMedia('(max-width: 959.98px)');
        mobile.addEventListener('change', ev => dotnet.invokeMethodAsync('OnViewportChanged', ev.matches));

        return {
            mobile: mobile.matches,
            reducedMotion: window.matchMedia('(prefers-reduced-motion: reduce)').matches
        };
    },

    focus(id) {
        const el = document.getElementById(id);
        if (el) el.focus();
    },

    scrollToTop() {
        const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' });
    }
};
