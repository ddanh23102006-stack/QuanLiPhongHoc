// Chuẩn hóa ô nhập giờ tự do: 17:00, 17h00, 17h, 1700, 17, 5h chiều → HH:mm.
(() => {
    const chuanHoa = (giaTri) => {
        let s = String(giaTri ?? '').trim().toLowerCase();
        if (!s) return '';
        const buoiChieu = /chieu|chiều|toi|tối/.test(s);
        s = s.replace(/chiều|chieu|tối|toi|sáng|sang|giờ/g, '')
            .replace(/\s+/g, '')
            .replace(/[hg.]/g, ':');

        const phan = s.split(':').filter(Boolean);
        if (!phan.length) return '';

        let gio, phut = 0;
        if (phan.length === 1) {
            if (!/^\d+$/.test(phan[0])) return '';
            let so = phan[0];
            if (so.length <= 2) {
                gio = Number(so);
            } else if (so.length === 3 || so.length === 4) {
                so = so.padStart(4, '0');
                gio = Number(so.slice(0, 2));
                phut = Number(so.slice(2));
            } else {
                return '';
            }
        } else {
            if (!/^\d+$/.test(phan[0]) || !/^\d+$/.test(phan[1])) return '';
            gio = Number(phan[0]);
            phut = Number(phan[1]);
        }

        if (buoiChieu && gio >= 1 && gio <= 11) gio += 12;
        if (gio < 0 || gio > 23 || phut < 0 || phut > 59) return '';
        return `${String(gio).padStart(2, '0')}:${String(phut).padStart(2, '0')}`;
    };

    const chuanHoaO = (o) => {
        const v = chuanHoa(o.value);
        if (v) o.value = v;
    };

    document.addEventListener('focusout', (e) => {
        if (e.target instanceof HTMLInputElement && e.target.matches('input[data-gio]')) chuanHoaO(e.target);
    }, true);

    document.addEventListener('submit', (e) => {
        e.target.querySelectorAll?.('input[data-gio]').forEach(chuanHoaO);
    }, true);
})();
