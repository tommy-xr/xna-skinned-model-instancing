#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace SkinnedModelInstancing
{

    /// <summary>
    /// InstancedSkinnedModel is a wrapper over the Model class to add functionality
    /// for skinned instancing
    /// </summary>
    public class InstancedSkinnedModel
    {
        #region Fields
        private Model model;
        private InstancedSkinningData instancedSkinningData;
        private List<InstancedSkinnedModelMesh> meshes = new List<InstancedSkinnedModelMesh>();
        #endregion

        #region Properties
        /// <summary>
        /// Gets the list of InstancedSkinnedModelMeshes that make up this InstancedSkinnedModel
        /// </summary>
        public IList<InstancedSkinnedModelMesh> Meshes 
        { 
            get 
            { 
                return this.meshes; 
            } 
        }

        /// <summary>
        /// Gets the dictionary of animations for this model
        /// </summary>
        public IDictionary<string, InstancedAnimationClip> Animations
        {
            get 
            { 
                return this.instancedSkinningData.Animations;
            }
        }

        /// <summary>
        /// Gets the skinning data for this model
        /// </summary>
        public InstancedSkinningData SkinningData
        {
            get { return this.instancedSkinningData; }
        }
        #endregion

       
        public InstancedSkinnedModel(ContentReader reader)
        {
            this.model = reader.ReadObject<Model>();
            this.instancedSkinningData = new InstancedSkinningData(reader);

            GraphicsDevice device = ((IGraphicsDeviceService)reader.ContentManager.ServiceProvider.GetService(typeof(IGraphicsDeviceService))).GraphicsDevice;

            // Create InstancedSkinnedModelMeshes from the ModelMeshes
            foreach (ModelMesh mesh in this.model.Meshes)
            {
                meshes.Add(new InstancedSkinnedModelMesh(mesh, this.instancedSkinningData.AnimationTexture, device));
            }

        }

        /// <summary>
        /// Draws all InstancedModelMeshes in this model based on the specified transforms and animations
        /// This isn't used in our sample, because we render the bodyparts specifically, which is probably
        /// more useful for most implementations of Instanced Skinned Models. However, if you have a lot of the same models
        /// to render, then this would be helpful
        /// </summary>
        /// <param name="transforms"></param>
        /// <param name="animations"></param>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw(Matrix [] transforms, int [] animations, Matrix view, Matrix projection)
        {
            // Simply loop through each mesh and draw it
            foreach (InstancedSkinnedModelMesh mesh in this.meshes)
            {
                mesh.Draw(transforms, animations, view, projection);
            }
        }
        
    }
}