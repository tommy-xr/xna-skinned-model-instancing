#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using SkinnedModelInstancing;
#endregion

namespace SkinnedModelInstancingPipeline
{
    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class InstancedSkinningDataContent
    {
        #region Fields
        private IDictionary<string, InstancedAnimationClip> animations;
        private TextureContent texture;


        #endregion


        /// <summary>
        /// Constructs a new skinning data object.
        /// </summary>
        public InstancedSkinningDataContent(IDictionary<string, InstancedAnimationClip> animationClips, TextureContent animationTexture)
        {
            this.animations = animationClips;
            this.texture = animationTexture;
        }

        public void Write(ContentWriter writer)
        {
            writer.WriteObject(this.texture);
            writer.WriteObject(this.animations);
        }
    }
}
