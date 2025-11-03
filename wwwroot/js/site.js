// Khởi tạo hành vi giao diện sau khi DOM sẵn sàng
document.addEventListener('DOMContentLoaded', function() {
    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Hiệu ứng carousel ngang (Shop by Icon) cuộn mượt dạng vòng lặp
    const sbd = document.querySelector('.shop-by-detail');
    if (sbd) {
        const track = sbd.querySelector('.sbd-track');
        if (track) {
            const items = Array.from(track.querySelectorAll('.sbd-item'));
            const baseSet = items.slice(0, 7);
            const baseWidth = baseSet.reduce((w, el) => w + el.getBoundingClientRect().width + parseFloat(getComputedStyle(track).gap || 0), 0);

            let initialized = false;
            const initPosition = () => {
                if (initialized || !baseWidth) return;
                track.scrollLeft = baseWidth;
                initialized = true;
            };
            requestAnimationFrame(initPosition);

            let targetLeft = 0;
            let rafId = 0;
            let velocity = 0.28;
            const lerp = (a, b, t) => a + (b - a) * t;

            const animate = () => {
                const current = track.scrollLeft;
                const next = lerp(current, targetLeft, 0.08);
                track.scrollLeft = next;

                if (track.scrollLeft <= 0) {
                    track.scrollLeft += baseWidth;
                    targetLeft += baseWidth;
                } else if (track.scrollLeft >= baseWidth * 2) {
                    track.scrollLeft -= baseWidth;
                    targetLeft -= baseWidth;
                }

                targetLeft += velocity;

                rafId = requestAnimationFrame(animate);
            };

            const startAnimation = () => {
                if (!rafId) rafId = requestAnimationFrame(animate);
            };
            track.addEventListener('mouseenter', () => { velocity = 0; });
            track.addEventListener('mouseleave', () => { velocity = 0.28; startAnimation(); });

            const prefersReduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            if (prefersReduced) velocity = 0;
            const io = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting && !prefersReduced) {
                        velocity = 0.28; startAnimation();
                    } else { velocity = 0; }
                });
            }, { threshold: 0.1 });
            io.observe(track);

            document.addEventListener('visibilitychange', () => {
                if (document.hidden) velocity = 0; else if (!prefersReduced) { velocity = 0.28; startAnimation(); }
            });

            startAnimation();
        }
    }

    // Lazy-load ảnh: ưu tiên thuộc tính loading, fallback IO
    const lazyImgs = Array.from(document.querySelectorAll('img:not([loading])'));
    if ('loading' in HTMLImageElement.prototype) {
        lazyImgs.forEach(img => { img.loading = 'lazy'; });
    } else if ('IntersectionObserver' in window) {
        const io = new IntersectionObserver((entries, obs) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    const dataSrc = img.getAttribute('data-src');
                    if (dataSrc) { img.src = dataSrc; }
                    obs.unobserve(img);
                }
            });
        }, { rootMargin: '200px' });
        lazyImgs.forEach(img => io.observe(img));
    }

    // Tiết kiệm tài nguyên: video nền chỉ play khi hiển thị
    const heroVideo = document.querySelector('.hero-video-bg');
    if (heroVideo && 'IntersectionObserver' in window) {
        const vObs = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    if (heroVideo.paused) { heroVideo.play().catch(() => {}); }
                } else {
                    if (!heroVideo.paused) { heroVideo.pause(); }
                }
            });
        }, { threshold: 0.1 });
        vObs.observe(heroVideo);
    }
});

// Khôi phục từ bfcache: ẩn preloader ngay lập tức nếu có
window.addEventListener('pageshow', function(event) {
    if (event.persisted) {
        const preloader = document.getElementById('preloader');
        if (preloader) {
            preloader.classList.add('hide');
            setTimeout(() => preloader.parentNode && preloader.parentNode.removeChild(preloader), 100);
        }
    }
});

// Khi tài nguyên tải xong: ẩn preloader mượt và gỡ khỏi DOM
window.addEventListener('load', function() {
    const preloader = document.getElementById('preloader');
    if (!preloader) return;
    requestAnimationFrame(() => {
        preloader.classList.add('hide');
        const remove = () => preloader.parentNode && preloader.parentNode.removeChild(preloader);
        preloader.addEventListener('transitionend', remove, { once: true });
        setTimeout(remove, 800);
    });
});

