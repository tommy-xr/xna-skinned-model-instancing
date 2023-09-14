#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace SkinnedModelInstancing
{
    /// <summary>
    /// This is again a wrapper around a ModelMesh. This is the Instanced equivalent of a ModelMesh
    /// Encapsulates the drawing behavior of instanced model
    /// This is where the instancing magic happens
    /// This is set up for vfetch instancing, but nothing is stopping you from doing shader instancing or hardware instancing
    /// As I mentioned in the doc, the only reason I didn't do this was because I don't have a graphics card that supports vertex texture fetch :-(
    /// </summary>
    public class InstancedSkinnedModelMesh
    {
        // Max number of instances allowed based on the number of shader parameters
        // If you change the shader, make sure to change this value!
        const int MaxInstances = 47;

        #region Fields
        private ModelMesh mesh;
        private int vertexCount;
        private int indexCount;
        private int maxInstances;
        private GraphicsDevice graphicsDevice;
        private Texture2D animationTexture;
        private IndexBuffer indexBuffer;

        // Temporary arrays used during rendering
        private Matrix[] tempTransforms;
        private int[] tempAnimations;
        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modelMesh"></param>
        /// <param name="animationTexture"></param>
        /// <param name="device"></param>
        public InstancedSkinnedModelMesh(ModelMesh modelMesh, Texture2D animationTexture, GraphicsDevice device)
        {
            this.graphicsDevice = device;
            this.mesh = modelMesh;

            // We are assuming all the mesh parts have the same vertex stride
            this.vertexCount = this.mesh.VertexBuffer.SizeInBytes / mesh.MeshParts[0].VertexStride;

            // Calculate the actual number of instances
            // We're using ushort for our instances, so we can only reference up to ushort.MaxValue
            this.maxInstances = Math.Min(ushort.MaxValue / this.vertexCount, MaxInstances);

            // Hold on to a handle to the animation texture
            this.animationTexture = animationTexture;

            // Create our temporary arrays based on the actual number of instances we can handle
            this.tempTransforms = new Matrix[this.maxInstances];
            this.tempAnimations = new int[this.maxInstances];

            // Replicate index data, as required for vfetch instancing
            this.ReplicateIndexData(this.mesh);
        }

        /// <summary>
        /// Draws the model mesh based on the specified input transforms and animations
        /// Any number of transforms and animations can be entered, but the arrays must be the same size
        /// </summary>
        /// <param name="transforms"></param>
        /// <param name="animations"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw(Matrix [] transforms, int [] animations, Matrix view, Matrix projection)
        {

            if (transforms.Length != animations.Length)
                throw new ArgumentException("Transforms array and animation frames array must have same length.");

            // Set the graphics device to use our vertex data
            this.graphicsDevice.Indices = this.indexBuffer ;
            
            // Draw each meshPart
            for(int i = 0; i < this.mesh.MeshParts.Count; i++)
            {
                ModelMeshPart part = this.mesh.MeshParts[i];
                this.graphicsDevice.VertexDeclaration = part.VertexDeclaration;
                this.graphicsDevice.Vertices[0].SetSource(this.mesh.VertexBuffer, part.StreamOffset, part.VertexStride);

                Effect effect = part.Effect;

                // Pass camera matrices through to the effect.
                effect.Parameters["View"].SetValue(view);
                effect.Parameters["Projection"].SetValue(projection);

                // Set the vertex count (used by the VFetch instancing technique).
                // And also set all the parameters the shader needs to get the animation data
                effect.Parameters["VertexCount"].SetValue(this.vertexCount);
                effect.Parameters["AnimationTexture"].SetValue(this.animationTexture);

                // Bone delta and row delta are basically pixel width and pixel height, respectively
                effect.Parameters["BoneDelta"].SetValue(1f / this.animationTexture.Width);
                effect.Parameters["RowDelta"].SetValue(1f / this.animationTexture.Height);

                // Do the actual drawing
                effect.Begin();
                foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Begin();
                    this.DrawPart(transforms, animations, effect, part);
                    pass.End();
                }
                effect.End();
            }
        }

        /// <summary>
        /// DrawPart draws the actual ModelMeshPart
        /// </summary>
        /// <param name="transforms"></param>
        /// <param name="animations"></param>
        /// <param name="effect"></param>
        /// <param name="part"></param>
        private void DrawPart(Matrix [] transforms, int [] animations, Effect effect, ModelMeshPart part)
        {
            for (int i = 0; i < transforms.Length; i += maxInstances)
            {
                // How many instances can we fit into this batch?
                int instanceCount = transforms.Length- i;

                if (instanceCount > maxInstances)
                    instanceCount = maxInstances;

                // Copy transform and animation data
                Array.Copy(transforms, i, tempTransforms, 0, instanceCount);
                Array.Copy(animations, i, tempAnimations, 0, instanceCount);

                // Send the transform and animation data to the shader
                effect.Parameters["InstanceTransforms"].SetValue(tempTransforms);
                effect.Parameters["InstanceAnimations"].SetValue(tempAnimations);
                effect.CommitChanges();

                // Draw maxInstances copies of our geometry in a single batch.
                this.graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList,
                    part.BaseVertex, 0, part.NumVertices * instanceCount,
                    part.StartIndex, part.PrimitiveCount * instanceCount);
            }
        }
            
        /// <summary>
        /// Replicate the index data - we need to have indices for each of our instances, based on vfetch instancing
        /// This is sort of similar to what happens in the InstancingSample, but modified to account for ModelMeshParts
        /// </summary>
        /// <param name="mesh"></param>
        private void ReplicateIndexData(ModelMesh mesh)
        {
            List<ushort> indices = new List<ushort>();

            int size = mesh.IndexBuffer.IndexElementSize == IndexElementSize.SixteenBits ? 2 : 4;
            this.indexCount = mesh.IndexBuffer.SizeInBytes / size;

            ushort[] oldIndices = new ushort[indexCount];

            mesh.IndexBuffer.GetData(oldIndices);
            mesh.IndexBuffer.Dispose();

            // We have to replicate index data for each part
            // Unfortunately, since we have different meshParts we have to do this
            // If you look at the InstancingSample, they defined their own Model class. 
            // This wasn't a possibility this time because I wanted to leverage the capabilities of the ModelProcessor
            // Therefore, we have to add this extra logic to deal with ModelMeshParts

            // Note that if you just replicate the whole index buffer in a pass, the instancing won't work, because 
            // you have to render the instances as contiguous indices

            // So if your buffer looks like: <indices for meshPart1> <indices for meshPart2> etc.., we need to replicate it like:
            // <repeat indices for meshPart1 N times> <repeat indices for meshPart2 N times> where N is the number of instances
     
            foreach (ModelMeshPart part in mesh.MeshParts)
            {
                this.ReplicateIndexData(part, indices, oldIndices);
            }

            // Create a new index buffer, and set the replicated data into it.
            this.indexBuffer = new IndexBuffer(this.graphicsDevice,
                                          sizeof(ushort) * indices.Count,
                                          BufferUsage.None,
                                          IndexElementSize.SixteenBits);

            this.indexBuffer.SetData(indices.ToArray());
        }

 

        /// <summary>
        /// Do the actual replication of the indices, for each ModelMeshPart
        /// </summary>
        /// <param name="part"></param>
        /// <param name="indices"></param>
        /// <param name="oldIndices"></param>
        private void ReplicateIndexData(ModelMeshPart part, List<ushort> indices, ushort [] oldIndices)
        {

            // Replicate one copy of the original index buffer for each instance.
            for (int instanceIndex = 0; instanceIndex < maxInstances; instanceIndex++)
            {
                int instanceOffset = instanceIndex * vertexCount;

                // Basically, keep adding the indices. We have to replicate this ModelMeshPart index for each instance we can have
                // Note that each time we are incrementing by instanceOffset. This is so in the vertex shader, when we divide
                // by the vertex count, we know exactly which index we are at.
                for (int i = part.StartIndex; i < part.PrimitiveCount * 3; i++)
                {
                    indices.Add((ushort)(oldIndices[i] + instanceOffset));
                }
            }
        }
    }
}