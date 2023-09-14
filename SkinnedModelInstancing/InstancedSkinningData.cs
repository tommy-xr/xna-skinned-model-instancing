#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
#endregion

namespace SkinnedModelInstancing
{
    /// <summary>
    /// Combines all the data needed to render and animate a skinned object.
    /// This is typically stored in the Tag property of the Model being animated.
    /// </summary>
    public class InstancedSkinningData
    {
        #region Fields

        private Texture2D texture;
        private IDictionary<string, InstancedAnimationClip> animations;


        #endregion

        /// <summary>
        /// Get the animation texture associated 
        /// </summary>
        public Texture2D AnimationTexture 
        { 
            get 
            { 
                return this.texture; 
            } 
        }

        /// <summary>
        /// Get the list of animations
        /// </summary>
        public IDictionary<string, InstancedAnimationClip> Animations 
        { 
            get 
            { 
                return this.animations; 
            }
        }

        /// <summary>
        /// Constructs a new skinning data object.
        /// </summary>
        public InstancedSkinningData(ContentReader input)
        {
            this.texture = input.ReadObject<Texture2D>();
            this.animations = input.ReadObject<IDictionary<string, InstancedAnimationClip>>();

            Vector4[] data = new Vector4[this.texture.Width * this.texture.Height];
            this.texture.GetData<Vector4>(data);
        }
    }
}
