document.addEventListener("DOMContentLoaded", async () => {
    const map = L.map('map').setView([45.762, 4.835], 13);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    let originMarker = null; // Marker for the origin
    let destinationMarker = null; // Marker for the destination
    let routeLayers = []; // Store route layers to clear them later

    // Function to update the marker
    const updateMarker = (marker, coordinates, label) => {
        if (marker) {
            // Move the existing marker
            marker.setLatLng(coordinates);
        } else {
            // Create a new marker
            marker = L.marker(coordinates, { draggable: false }).addTo(map);
        }
        return marker;
    };

    // Function to execute the search
    const searchDirections = async () => {
        const originInput = document.getElementById('origin-input').value;
        const destinationInput = document.getElementById('destination-input').value;

        // Check if both fields are not empty
        if (!originInput || !destinationInput) {
            return;
        }

        // Extract coordinates from the inputs
        const [originLat, originLng] = originInput.split(',').map(coord => parseFloat(coord.trim()));
        const [destLat, destLng] = destinationInput.split(',').map(coord => parseFloat(coord.trim()));

        if (isNaN(originLat) || isNaN(originLng) || isNaN(destLat) || isNaN(destLng)) {
            alert("Invalid coordinates! Please use the format: latitude, longitude");
            return;
        }

        // Update the markers for the origin and destination
        originMarker = updateMarker(originMarker, [originLat, originLng], "Origin");
        destinationMarker = updateMarker(destinationMarker, [destLat, destLng], "Destination");

        try {
            // Call the backend API
            const response = await fetch(`http://localhost:5117/api/Routing/directions?originLat=${originLat}&originLng=${originLng}&destLat=${destLat}&destLng=${destLng}`);
            if (!response.ok) {
                throw new Error("Error calling the API");
            }

            const routeSections = await response.json();
            console.log(routeSections);

            // Remove previous routes
            routeLayers.forEach(layer => map.removeLayer(layer));
            routeLayers = [];

            // Draw each route section with different styles based on mode
            routeSections.forEach(section => {
                const color = section.mode === "walking" ? "green" : "blue";
                const dashArray = section.mode === "walking" ? "10, 10" : null; // Dashed for walking, solid for biking
                const polyline = L.polyline(
                    section.coordinates.map(coord => [coord.latitude, coord.longitude]),
                    { color: color, weight: 5, dashArray: dashArray }
                ).addTo(map);

                routeLayers.push(polyline);
            });

            // Adjust the view to include the entire route
            if (routeLayers.length > 0) {
                const allBounds = L.featureGroup(routeLayers).getBounds();
                map.fitBounds(allBounds);
            }
        } catch (error) {
            console.error("Error fetching directions:", error);
            alert("Failed to fetch directions. Please try again.");
        }
    };

    // Check if both fields are filled and trigger the search
    const inputs = document.querySelectorAll('#origin-input, #destination-input');
    inputs.forEach(input => {
        input.addEventListener('input', () => {
            const originValue = document.getElementById('origin-input').value.trim();
            const destinationValue = document.getElementById('destination-input').value.trim();

            if (originValue && destinationValue) {
                searchDirections();
            }
        });
    });

    // Fill the fields and add a marker when clicking on the map
    map.on('click', (e) => {
        const { lat, lng } = e.latlng;

        const originInput = document.getElementById('origin-input');
        const destinationInput = document.getElementById('destination-input');

        if (!originInput.value) {
            // If the origin field is empty, fill it with the clicked coordinates
            originInput.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
            originMarker = updateMarker(originMarker, [lat, lng], "Origin");
        } else if (!destinationInput.value) {
            // Otherwise, fill the destination field
            destinationInput.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
            destinationMarker = updateMarker(destinationMarker, [lat, lng], "Destination");
        }

        // Check if both fields are filled to trigger the search automatically
        const originValue = originInput.value.trim();
        const destinationValue = destinationInput.value.trim();

        if (originValue && destinationValue) {
            searchDirections();
        }
    });
});