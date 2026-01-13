// ZEN reveal: animate elements when they enter the viewport.
(function () {
	const targets = Array.from(document.querySelectorAll('.zen-animate-fade, .zen-animate-rise'));
	if (!targets.length) return;

	const revealNow = el => el.classList.add('is-visible');

	if ('IntersectionObserver' in window) {
		const observer = new IntersectionObserver((entries) => {
			entries.forEach(entry => {
				if (entry.isIntersecting) {
					revealNow(entry.target);
					observer.unobserve(entry.target);
				}
			});
		}, { threshold: 0.2, rootMargin: '0px 0px -10% 0px' });

		targets.forEach(el => observer.observe(el));
	} else {
		// Fallback: reveal everything if the browser lacks IntersectionObserver.
		targets.forEach(revealNow);
	}
})();
// Navbar scroll effect: blur + shrink on scroll
(function() {
    const navbar = document.querySelector('.navbar');
    if (!navbar) return;

    let lastScroll = 0;
    const handleScroll = () => {
        const currentScroll = window.pageYOffset;
        if (currentScroll > 60) {
            navbar.classList.add('zen-nav--scrolled');
        } else {
            navbar.classList.remove('zen-nav--scrolled');
        }
        lastScroll = currentScroll;
    };

    window.addEventListener('scroll', handleScroll, { passive: true });
})();

// Add to cart button animation
document.addEventListener('DOMContentLoaded', () => {
    const addToCartButtons = document.querySelectorAll('.zen-add-to-cart');
    addToCartButtons.forEach(btn => {
        btn.addEventListener('click', function(e) {
            if (this.querySelector('form')) return; // Let form submit normally
            
            // Ripple effect
            const ripple = document.createElement('span');
            ripple.className = 'zen-ripple';
            this.appendChild(ripple);
            
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = (e.clientX - rect.left - size / 2) + 'px';
            ripple.style.top = (e.clientY - rect.top - size / 2) + 'px';
            
            setTimeout(() => ripple.remove(), 600);
        });
    });
});