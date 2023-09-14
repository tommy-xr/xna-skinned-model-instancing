//-----------------------------------------------------------------------------
// InstancedSkinnedModel.fx
//
// Ionixx Interactive, LLC
// Copyright (c) Bryan Phelps. All Rights Reserved.
//-----------------------------------------------------------------------------


// Input parameters.
float4x4 View;
float4x4 Projection;



//ANIMATION INSTANCING
float BoneDelta; //delta value between bones
float RowDelta; //delta value between rows
int VertexCount;

float4x4 InstanceTransforms[48];
int InstanceAnimations[48];

texture AnimationTexture;
sampler AnimationSampler = sampler_state
{
	Texture = (AnimationTexture);
	MinFilter = None;
	MagFilter = None;
	MipFilter = None;
};

texture Texture;
sampler Sampler = sampler_state
{
    Texture = (Texture);

    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
};


// Vertex shader input structure.
struct VS_INPUT
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    float4 BoneIndices : BLENDINDICES0;
    float4 BoneWeights : BLENDWEIGHT0;
};


// Vertex shader output structure.
struct VS_OUTPUT
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};


// Pixel shader input structure.
struct PS_INPUT
{
    float2 TexCoord : TEXCOORD0;
};


// This function returns the transform matrix, based on the animation row and bone
float4x4 ReadBoneMatrix(float row, float bone)
{
	// Each bone index is 4 pixels apart, because we use 4 pixels to encode a bone transform
	float index = bone * 4;
	
	// We need to do a slight offset to get the center of the pixel - otherwise we won't read the proper value
	float halfWidth = BoneDelta / 2;
	float halfHeight = RowDelta / 2;
	
	// Calculate the actual texture coordinate for the row
	row *= RowDelta;
	
	// Do the actual matrix reads - each pixel is a row in the matrix.
	// Note that it is possible to encode/decode transform matrices using only 3 vertex texture fetches
	// That would be a great optimization to make here!
	float4 mat1 = tex2Dlod(AnimationSampler, float4( (index + 0) * BoneDelta + halfWidth, row + halfHeight,0,0));
	float4 mat2 = tex2Dlod(AnimationSampler, float4( (index + 1) * BoneDelta + halfWidth, row + halfHeight,0,0));
	float4 mat3 = tex2Dlod(AnimationSampler, float4( (index + 2) * BoneDelta + halfWidth, row + halfHeight,0,0));
	float4 mat4 = tex2Dlod(AnimationSampler, float4( (index + 3) * BoneDelta + halfWidth, row + halfHeight,0,0));
	
	// Return the bone matrix
	return float4x4 (mat1, mat2, mat3, mat4);
}

// Vertex shader program.
VS_OUTPUT VertexShader(VS_INPUT input, Matrix transform, float animation)
{
    VS_OUTPUT output;
    
    // Blend between the weighted bone matrices.
    float4x4 skinTransform = 0;
   
    // This is the logic to create the transform matrix for this particular vertex
    // It is very similar to the SkinnedModelSample, except there are branches to try
    // and save us vertex texture reads if some of the weights are 0
    skinTransform = ReadBoneMatrix(animation, input.BoneIndices.x) * input.BoneWeights.x;    
    if(input.BoneWeights.y > 0)
	{
		skinTransform += ReadBoneMatrix(animation, input.BoneIndices.y) * input.BoneWeights.y;
		if(input.BoneWeights.z > 0)
		{
			skinTransform += ReadBoneMatrix(animation, input.BoneIndices.z) * input.BoneWeights.z;
			if(input.BoneWeights.w > 0) skinTransform += ReadBoneMatrix(animation, input.BoneIndices.w) * input.BoneWeights.w;
		}
	}
	
    // Skin the vertex position.
    float4 position = mul(input.Position, skinTransform);
    
    //Get the transformed postion
    output.Position = mul(mul(mul(position, transform), View), Projection);

    output.TexCoord = input.TexCoord;
    
    return output;
}


// This is basically verbatin from the InstancedModelSample
// This function uses the vfetch asm instruction to do instancing 
VS_OUTPUT VFetchInstancingVertexShader(int index : INDEX)
{
    int vertexIndex = (index + 0.5) % VertexCount;
    int instanceIndex = (index + 0.5) / VertexCount;

    float4 position;
    float4 normal;
    float4 textureCoordinate;
    float4 boneIndices;
    float4 boneWeights;
    

    asm
    {
        vfetch position,          vertexIndex, position0
        vfetch normal,            vertexIndex, normal0
        vfetch textureCoordinate, vertexIndex, texcoord0
        vfetch boneIndices,		  vertexIndex, blendindices0
        vfetch boneWeights,		  vertexIndex, blendweight0
    };

    VS_INPUT input;

    input.Position = position;
    input.Normal = normal;
    input.TexCoord = textureCoordinate.xy;
    input.BoneIndices = boneIndices;
    input.BoneWeights = boneWeights;

    return VertexShader(input, InstanceTransforms[instanceIndex], InstanceAnimations[instanceIndex]);
}

// Pixel shader program.
float4 PixelShader(PS_INPUT input) : COLOR0
{
    float4 color = tex2D(Sampler, input.TexCoord);

    return color;
}


technique InstancedSkinnedModelTechnique
{
    pass InstancedSkinnedModelPass
    {
        VertexShader = compile vs_3_0 VFetchInstancingVertexShader();
        PixelShader = compile ps_2_0 PixelShader();
    }
}
