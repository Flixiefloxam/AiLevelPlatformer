shader_type canvas_item;

uniform vec2 velocity = vec2(0.0);
uniform float maxStretch = 0.25;
uniform float speedScale = 1.0;

void vertex() {
	// Called for every vertex the material is visible on.
	float speed = length(velocity) * speedScale;
    vec2 dir = normalize(velocity);

    if (speed < 0.01) {
        dir = vec2(0.0);
    }

    float stretch = clamp(speed / 600.0, 0.0, maxStretch);

    // Center pivot
    vec2 offset = VERTEX - vec2(0.0);

    // Stretch matrix (same fix as before)
    mat2 stretchMatrix = mat2(
        vec2(1.0 + dir.x * stretch, dir.x * stretch),
        vec2(dir.y * stretch, 1.0 + dir.y * stretch)
    );

    offset = stretchMatrix * offset;
    VERTEX = offset;
}

// Called for every pixel the material is visible on.
void fragment() {
	////calculate direction and speed
	//float speed = length(velocity) * speedScale;
	//vec2 dir = normalize(velocity);
	//
	//// Avoid NaNs for zero velocity
	//if (speed < 0.01){
		//dir = vec2(0,0);
	//}
	//
	////compute stretch
	//float stretch = clamp(speed / 600.0, 0.0, maxStretch);
	//
	////build stretch matrix
	//vec2 offset = UV - vec2(0.5); //centered UVs
	//mat2 stretchMatrix = mat2(
        //vec2(1.0 + dir.x * stretch, dir.x * stretch),
		//vec2(dir.y * stretch, 1.0 + dir.y * stretch)
    //);
	//
	//offset = stretchMatrix * offset;
	//vec2 stretchedUV = offset + vec2(0.5);
	//
	//COLOR = texture(TEXTURE, stretchedUV) * texture(TEXTURE, UV).a;
	////COLOR = vec4(dir * 0.5 + 0.5, 0.0, 1.0); // Debug: shows direction of movement
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
