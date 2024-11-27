document.addEventListener("DOMContentLoaded", () => {
    const locations = [
        { lat: 48.8566, lng: 2.3522 },
        { lat: 48.8584, lng: 2.2945 },
        { lat: 48.8606, lng: 2.3376 }
    ];

    const map = L.map('map').setView([45.7640, 4.8357], 12);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    locations.forEach(location => {
        L.marker([location.lat, location.lng]).addTo(map);
    });

    fetch("URL_DE_VOTRE_SERVICE")
        .then(response => response.json())
        .then(data => {
            data.forEach(location => {
                L.marker([location.lat, location.lng]).addTo(map);
            });
        })
        .catch(error => {
            console.error("Error while fetching locations :", error);
        });
});