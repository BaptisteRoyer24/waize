document.addEventListener("DOMContentLoaded", async () => {
    const map = L.map('map').setView([45.762, 4.835], 15);

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 19,
        attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);

    let originMarker = null; // Marker for the origin
    let destinationMarker = null; // Marker for the destination
    let pickupStationMarker = null; //Marker for the pickup station
    let dropOffStationMarker = null //Marker for the bike drop-off station
    let routeLayers = []; // Store route layers to clear them later

    const updateMarker = (marker, coordinates, iconUrl = null) => {
        if (marker) {
            // Update the existing marker position
            marker.setLatLng(coordinates);

            // Update the icon if a new icon URL is provided
            if (iconUrl) {
                const newIcon = L.icon({
                    iconUrl: iconUrl,
                    iconSize: [30, 30], // Adjust size as needed
                    iconAnchor: [15, 30], // Anchor point of the icon
                });
                marker.setIcon(newIcon);
            }
        } else {
            // Create a new marker with or without a custom icon
            const options = { draggable: false };
            if (iconUrl) {
                options.icon = L.icon({
                    iconUrl: iconUrl,
                    iconSize: [30, 30], // Adjust size as needed
                    iconAnchor: [15, 30], // Anchor point of the icon
                });
            }
            marker = L.marker(coordinates, options).addTo(map);
        }
        return marker;
    };

    const searchDirections = async () => {
        const originInput = document.getElementById('origin-input').value;
        const destinationInput = document.getElementById('destination-input').value;

        if (!originInput || !destinationInput) {
            return;
        }

        const [originLat, originLng] = originInput.split(',').map(coord => parseFloat(coord.trim()));
        const [destLat, destLng] = destinationInput.split(',').map(coord => parseFloat(coord.trim()));

        if (isNaN(originLat) || isNaN(originLng) || isNaN(destLat) || isNaN(destLng)) {
            alert("Invalid coordinates! Please use the format: latitude, longitude");
            return;
        }

        originMarker = updateMarker(originMarker, [originLat, originLng], "./img/origin.png");
        destinationMarker = updateMarker(destinationMarker, [destLat, destLng], "./img/destination.png");

        try {
            const response = await fetch(`http://localhost:5117/api/Routing/directions?originLat=${originLat}&originLng=${originLng}&destLat=${destLat}&destLng=${destLng}`);
            if (!response.ok) {
                throw new Error("Error calling the API");
            }

            const routeDetails = await response.json();
            console.log(routeDetails);

            routeLayers.forEach(layer => map.removeLayer(layer));
            routeLayers = [];

            const sections = [
                routeDetails.walkingToStation,
                routeDetails.bikingBetweenStations,
                routeDetails.walkingToDestination
            ];

            sections.forEach(section => {
                const color = section.mode === "walking" ? "green" : "blue";
                const dashArray = section.mode === "walking" ? "10, 10" : null;
                const polyline = L.polyline(
                    section.coordinates.map(coord => [coord.latitude, coord.longitude]),
                    { color: color, weight: 5, dashArray: dashArray }
                ).addTo(map);

                routeLayers.push(polyline);
            });

            if (routeLayers.length > 0) {
                const allBounds = L.featureGroup(routeLayers).getBounds();
                map.fitBounds(allBounds);
            }

            pickupStationMarker = updateMarker(
                pickupStationMarker,
                [routeDetails.pickupStation.coordinate.latitude, routeDetails.pickupStation.coordinate.longitude],
                "./img/location.png"
            );

            dropOffStationMarker = updateMarker(
                dropOffStationMarker,
                [routeDetails.dropOffStation.coordinate.latitude, routeDetails.dropOffStation.coordinate.longitude],
                "./img/location.png"
            );

            updateDirectionsUI(sections);
        } catch (error) {
            console.error("Error fetching directions:", error);
            alert("Failed to fetch directions. Please try again.");
        }
    };

    const updateDirectionsUI = (sections) => {
        const directionsContainer = document.querySelector(".directions-suggestions-container");
        directionsContainer.innerHTML = ""; // Clear previous directions

        sections.forEach(section => {
            section.steps.forEach(step => {
                const directionItem = document.createElement("div");
                directionItem.classList.add("direction-item");

                const directionInfo = document.createElement("div");
                directionInfo.classList.add("direction-info");

                // Distance
                const distanceSpan = document.createElement("span");
                distanceSpan.classList.add("direction__distance");
                distanceSpan.textContent = `${step.distance.toFixed(1)}m`;

                // Street Name
                const streetSpan = document.createElement("span");
                streetSpan.classList.add("direction__street");
                streetSpan.textContent = step.streetName || "Unknown street";

                directionInfo.appendChild(distanceSpan);
                directionInfo.appendChild(streetSpan);

                // Determine instruction type (departure, arrival, or maneuver)
                let instructionText;
                if (!step.instruction) {
                    instructionText = "DEPARTURE"
                } else {
                    instructionText = step.instruction;
                }

                // Rotate arrow based on instruction
                const arrowRotation = getArrowRotation(instructionText);

                // Arrow Icon
                const directionIcon = document.createElement("div");
                directionIcon.classList.add("direction__icon");
                directionIcon.innerHTML = `<span style="transform: rotate(${arrowRotation}deg);">&#11014;</span>`; // Rotated arrow

                directionItem.appendChild(directionInfo);
                directionItem.appendChild(directionIcon);

                directionsContainer.appendChild(directionItem);
            });
        });
    };

    // Function to get the arrow rotation based on the instruction
    const getArrowRotation = (instruction) => {
        switch (instruction) {
            case "left":
                return -90;
            case "slight left":
                return -45;
            case "sharp left":
                return -135;
            case "right":
                return 90;
            case "slight right":
                return 45;
            case "sharp right":
                return 135;
            case "straight":
                return 0;
            default:
                return 0; // Default is straight if no instruction is given
        }
    };

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

    map.on('click', (e) => {
        const { lat, lng } = e.latlng;

        const originInput = document.getElementById('origin-input');
        const destinationInput = document.getElementById('destination-input');

        if (!originInput.value) {
            originInput.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
            originMarker = updateMarker(originMarker, [lat, lng], "./img/origin.png");
        } else if (!destinationInput.value) {
            destinationInput.value = `${lat.toFixed(6)}, ${lng.toFixed(6)}`;
            destinationMarker = updateMarker(destinationMarker, [lat, lng], "./img/destination.png");
        }

        const originValue = originInput.value.trim();
        const destinationValue = destinationInput.value.trim();

        if (originValue && destinationValue) {
            searchDirections();
        }
    });
});