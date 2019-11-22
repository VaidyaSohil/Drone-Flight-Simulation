//----------------------------------------------------
//--      This effect file derived from:            --
//--		   www.riemers.net						--
//--   		    Basic shaders						--
//--                                                --
//-- Modified for MonoGame by John K. Bennett		--
//--                                                --
//--		Use/modify as you like					--
//--                                                --
//----------------------------------------------------

#define TWO_PI 6.28318f
#define EXPLOSION_SIZE_FACTOR 2.0f
#define EXPLOSION_DISTANCE_FACTOR 0.5f

struct VertexToPixel
{
    float4 Position   	: POSITION;    
    float4 Color		: COLOR0;
    float LightingFactor: TEXCOORD0;
    float2 TextureCoords: TEXCOORD1;
};

struct PixelToFrame
{
    float4 Color : COLOR0;
};

struct ExpVertexToPixel
{
	float4 Position : POSITION;
	float2 TexCoord : TEXCOORD0;
	float4 Color : COLOR0;
};
struct ExpPixelToFrame
{
	float4 Color : COLOR0;
};

//------- Constants --------
float4x4 xView;
float4x4 xProjection;
float4x4 xWorld;
float3 xLightDirection;
float xAmbient;
bool xEnableLighting;
bool xShowNormals;
float3 xCamPos;
float3 xCamUp;
float xPointSpriteSize;

// the next two are explosion-specific
float3 xAllowedRotDir;
float xTime;

//------- Texture Samplers --------
Texture xTexture;
sampler TextureSampler = sampler_state {
	texture = <xTexture>; magfilter = LINEAR;
	minfilter = LINEAR; mipfilter = LINEAR; AddressU = mirror; AddressV = mirror;
};
Texture xExplosionTexture;
sampler textureSampler = sampler_state {
	texture = <xExplosionTexture>; magfilter =
		LINEAR; minfilter = LINEAR; mipfilter = LINEAR; AddressU = CLAMP; AddressV = CLAMP;
};

//------- Technique: Pretransformed --------

VertexToPixel PretransformedVS( float4 inPos : POSITION, float4 inColor: COLOR)
{	
	VertexToPixel Output = (VertexToPixel)0;
	
	Output.Position = inPos;
	Output.Color = inColor;
    
	return Output;    
}

PixelToFrame PretransformedPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = PSIn.Color;

	return Output;
}

technique Pretransformed
{
	pass Pass0
	{   
		VertexShader = compile vs_4_0_level_9_1 PretransformedVS();
		PixelShader  = compile ps_4_0_level_9_1 PretransformedPS();
	}
}

//------- Technique: ColoredNormal --------
VertexToPixel ColoredNormalVS(float4 inPos : POSITION, float4 inColor : COLOR, float3
	inNormal : NORMAL)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;
	float3 Normal = (float3) normalize(mul(normalize(float4(inNormal, 0.0)), xWorld));
	Output.LightingFactor = 1;
	if (xEnableLighting)
		Output.LightingFactor = dot(Normal, -xLightDirection);

	return Output;
}
PixelToFrame ColoredNormalPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = PSIn.Color;
	Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;
	return Output;
}
technique ColoredNormal
{
	pass Pass0
	{
		VertexShader = compile vs_4_0_level_9_1 ColoredNormalVS();
		PixelShader = compile ps_4_0_level_9_1 ColoredNormalPS();
	}
}

//------- Technique: ColoredNoShading --------
VertexToPixel ColoredNoShadingVS(float4 inPos : POSITION, float4 inColor : COLOR)
{
	VertexToPixel Output = (VertexToPixel)0;
	float4x4 preViewProjection = mul(xView, xProjection);
	float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

	Output.Position = mul(inPos, preWorldViewProjection);
	Output.Color = inColor;

	return Output;
}
PixelToFrame ColoredNoShadingPS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;

	Output.Color = PSIn.Color;
	return Output;
}
technique ColoredNoShading
{
	pass Pass0
	{
		VertexShader = compile vs_4_0_level_9_1 ColoredNoShadingVS();
		PixelShader = compile ps_4_0_level_9_1 ColoredNoShadingPS();
	}
}

	//------- Technique: Textured --------
	VertexToPixel TexturedVS(float4 inPos : POSITION, float3 inNormal : NORMAL, float2
		inTexCoords : TEXCOORD0)
	{
		VertexToPixel Output = (VertexToPixel)0;
		float4x4 preViewProjection = mul(xView, xProjection);
		float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
		Output.TextureCoords = inTexCoords;
		float3 Normal = (float3) normalize(mul(normalize(float4(inNormal, 0.0)),
			xWorld));;
		Output.LightingFactor = 1;
		if (xEnableLighting)
			Output.LightingFactor = dot(Normal, -xLightDirection);

		return Output;
	}
	PixelToFrame TexturedPS(VertexToPixel PSIn)
	{
		PixelToFrame Output = (PixelToFrame)0;
		Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);
		Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;
		return Output;
	}
	technique Textured
	{
		pass Pass0
		{
			VertexShader = compile vs_4_0_level_9_1 TexturedVS();
			PixelShader = compile ps_4_0_level_9_1 TexturedPS();
		}
	}

	//------- Technique: Textured No Shading--------
	VertexToPixel TexturedNSVS(float4 inPos : POSITION, /*float3 inNormal: NORMAL,*/ float2
		inTexCoords : TEXCOORD0)
	{
		VertexToPixel Output = (VertexToPixel)0;
		float4x4 preViewProjection = mul(xView, xProjection);
		float4x4 preWorldViewProjection = mul(xWorld, preViewProjection);

		Output.Position = mul(inPos, preWorldViewProjection);
		Output.TextureCoords = inTexCoords;

	//float3 Normal = (float3) normalize(mul(normalize(float4(inNormal, 0.0)), xWorld));;
	//Output.LightingFactor = 1;
	//if (xEnableLighting)
	//Output.LightingFactor = dot(Normal, -xLightDirection);
    
	return Output;    
}

PixelToFrame TexturedNSPS(VertexToPixel PSIn) 
{
	PixelToFrame Output = (PixelToFrame)0;		
	
	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);
	//Output.Color.rgb *= saturate(PSIn.LightingFactor) + xAmbient;

	return Output;
}

technique TexturedNoShading
{
	pass Pass0
	{   
		VertexShader = compile vs_4_0_level_9_1 TexturedNSVS();
		PixelShader  = compile ps_4_0_level_9_1 TexturedNSPS();
	}
}

//------- Technique: PointSprites --------
VertexToPixel PointSpriteVS(float3 inPos: POSITION0, float2 inTexCoord : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;
	float3 center = (float3) mul(float4(inPos, 0.0), xWorld);
	float3 eyeVector = center - xCamPos;
	float3 sideVector = cross(eyeVector, xCamUp);
	sideVector = normalize(sideVector);
	float3 upVector = cross(sideVector, eyeVector);
	upVector = normalize(upVector);
	float3 finalPosition = center;
	finalPosition += (inTexCoord.x - 0.5f)*sideVector*0.5f*xPointSpriteSize;
	finalPosition += (0.5f - inTexCoord.y)*upVector*0.5f*xPointSpriteSize;
	float4 finalPosition4 = float4(finalPosition, 1);
	float4x4 preViewProjection = mul(xView, xProjection);
	Output.Position = mul(finalPosition4, preViewProjection);
	Output.TextureCoords = inTexCoord;
	return Output;
}
PixelToFrame PointSpritePS(VertexToPixel PSIn)
{
	PixelToFrame Output = (PixelToFrame)0;
	Output.Color = tex2D(TextureSampler, PSIn.TextureCoords);
	return Output;
}

technique PointSprites
{
	pass Pass0
	{   
		VertexShader = compile vs_4_0_level_9_1 PointSpriteVS();
		PixelShader  = compile ps_4_0_level_9_1 PointSpritePS();
	}
}

//------- Technique: Explosion --------
// This implementation of explosions uses billboard-based particle effects
// This code is generally based upon "XNA 3.0 Game Programming Recipes," Riemer
//Grootjans, Chapter 3.12, APress 2009
float3 BillboardVertex(float3 billboardCenter, float2 cornerID, float size)
// A function to create a spherical billboard by translating the vertices
// so as to ensure that the resulting billboard texture will always be perpendicular to
//the camera
// Inputs are the center of the billboard, a texture coordinate, and how big we want the
//billboard to be
{
	float3 eyeVector = billboardCenter - xCamPos;
	float3 sideVector = cross(eyeVector, xCamUp);
	sideVector = normalize(sideVector);
	float3 upVector = cross(sideVector, eyeVector);
	upVector = normalize(upVector);
	float3 finalPosition = billboardCenter;
	finalPosition += (cornerID.x - 0.5f)*sideVector*size;
	finalPosition += (0.5f - cornerID.y)*upVector*size;
	return finalPosition;
}
ExpVertexToPixel ExplosionVS(float3 inPos: POSITION0, float4 inTexCoord : TEXCOORD0,
	float4 inExtra : TEXCOORD1)
{
	ExpVertexToPixel Output = (ExpVertexToPixel)0;
	float3 startingPosition = (float3) mul(float4(inPos, 0), xWorld);
	// Take our packaged inputs apart into meaningful variables
	float2 texCoords = inTexCoord.xy;
	float birthTime = inTexCoord.z;
	float maxAge = inTexCoord.w;
	float3 moveDirection = inExtra.xyz;
	float random = inExtra.w;
	float age = xTime - birthTime; // How long has this particle been alive
	float relAge = age / maxAge; // When relAge is 1, kill this particle
	 // Adjust the size of the
	//particle according to its age,
		// (but keep it between 0
		//(biggest)and 1 (smallest))
	// Particles start big and get
	//smaller, but have a lower size bound
		float sizer = saturate(1 - relAge * relAge / 2.0f);
	// Particles of size one are too small, so multiply by EXPLOSION_SIZE_FACTOR,
	// and the particle's unique random value
	float size = EXPLOSION_SIZE_FACTOR * random*sizer;
	// Now compute the 3D position of the center of the particle
	// This depends upon how far the particle has moved, which in turn depends
	// upon the particle's relAge. At first we want displacement to increase
	// approximately linearly with age, but at the end we want very little
	//displacement.
		// The desired displacement could be computed by simulating the physics of
		//acceleration,
		// setting and initial velocity and negative acceleration, and computing the
		//displacement
		// using the classic formula:
		// currentPosition = (1/2 acceleration * time * time) + (initialSpeed * time) +
		//initialPosition
		// However, the first quarter [(two*PI)/4] of a sine wave approximates the desired
		//value,
		// and is easier to compute, since we don't have to keep track of acceleration
		// (Note that this would be a bad choice on a processor that did not have a
		//built - in sine function)
		//
		// Mulitply relAge times this value, then times a distance factor, then times
		// the particle's unique random value
		float totalDisplacement = sin(relAge*TWO_PI /
			4.0f)*EXPLOSION_DISTANCE_FACTOR*random;
	// Now compute the 3D position
	float3 billboardCenter = startingPosition + totalDisplacement * moveDirection;
	// And add some gravitational (downward) force to the explosion by
	// pulling particles down one unit each second (because xTime is in msec)
	billboardCenter += age * float3(0, -1, 0) / 1000.0f;
	// Call our billboarding function to determine the location of the vertex
	// and convert to screen coordinates
	float3 finalPosition = BillboardVertex(billboardCenter, texCoords, size);
	float4 finalPosition4 = float4(finalPosition, 1);
	float4x4 preViewProjection = mul(xView, xProjection);
	Output.Position = mul(finalPosition4, preViewProjection);
	// Blend away each particle near the end of its life
	// Do this by computing an alpha value based upon age
	// Alpha will change as the square of the relAge
	float alpha = 1 - relAge * relAge;
	// The pixel shader will multiply each pixel color by this color, which will cause
// the particles to become more transparent over time.
// We also soften the color by a factor of two, so we don't notice individual
	//particles
		Output.Color = float4(0.5f, 0.5f, 0.5f, alpha);
	// Pass the texture coordinates to the pixel shader
	Output.TexCoord = texCoords;
	return Output;
}
ExpPixelToFrame ExplosionPS(ExpVertexToPixel PSIn)
{
	ExpPixelToFrame Output = (ExpPixelToFrame)0;
	Output.Color = tex2D(textureSampler, PSIn.TexCoord)*PSIn.Color;
	return Output;
}
technique Explosion
{
	pass Pass0
	{
		VertexShader = compile vs_4_0_level_9_1 ExplosionVS();
		PixelShader = compile ps_4_0_level_9_1 ExplosionPS();
	}
}