export const updateDirectionsUI = (sections) => {
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
            streetSpan.textContent = step.streetName || "";

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