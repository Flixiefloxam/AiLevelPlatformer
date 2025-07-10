shader_type canvas_item;

uniform vec2 velocity = vec2(0.0);
uniform float max_stretch = 0.3; // Maximum stretch scale factor
uniform float responsiveness = 0.02; // How fast the effect reacts

void vertex() {
    float speed = length(velocity);

    // Normalize velocity direction
    vec2 dir = normalize(velocity);
    if (speed < 0.01) {
        dir = vec2(0.0, -1.0); // Default to up if no velocity
    }

    // Calculate squash/stretch amount
    float stretch_amount = clamp(speed * responsiveness, 0.0, max_stretch);

    // Build scale matrix
	float squash = 1.0 - stretch_amount;
	float stretch = 1.0 + stretch_amount;
	
    mat2 scale = mat2(
        vec2(1.0 + stretch_amount * dir.x, 0.0),
        vec2(0.0, 1.0 + stretch_amount * dir.y)
    );

    // Offset vertex from center
    vec2 centered_pos = VERTEX - vec2(0.0);
    centered_pos = scale * centered_pos;

    VERTEX = centered_pos;
}