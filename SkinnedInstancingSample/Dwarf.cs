#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using SkinnedModelInstancing;

namespace InstancedSkinningSample
{
    /// <summary>
    /// This class is responsible for storing per-instance data for each Dwarf
    /// </summary>
    public class Dwarf
    {
        private static Random random = new Random();

        #region Fields
        //This data was simply pulled from the model
        //I know that ModelMeshes 0,1,2 are different heads, and 3,7,8 are different bodies, etc
        private static int [] HeadParts = new int[] { 0, 1, 2 };
        private static int [] BodyParts = new int[] { 3, 7, 8 };
        private static int [] LegParts = new int[] { 4, 5, 6 };

        private Matrix transform;
        private Matrix translationScaleMatrix;

        private int headPart;
        private int bodyPart;
        private int legPart;

        private Vector3 translation;

        private InstancedSkinningData animationData;
        private InstancedAnimationClip currentAnimation;
        private float animationFrame;
        private int repeatAnimation; //how many times we should replay the animation
        #endregion

        public BoundingSphere boundingSphere; //This is made public to save from copying across a getter/setter

        #region Properties
        /// <summary>
        /// Return the transform matrix for this Dwarf
        /// </summary>
        public Matrix Transform
        {
            get { return transform; }
        }

        /// <summary>
        /// The index of the InstancedModelPart for the head.
        /// </summary>
        public int HeadPart
        {
            get { return this.headPart; }
        }

        /// <summary>
        /// The index of the InstancedModelPart for the body.
        /// </summary>
        public int BodyPart
        {
            get { return this.bodyPart; }
        }

        /// <summary>
        /// The index of the InstancedModelPart of the leg.
        /// </summary>
        public int LegPart
        {
            get { return this.legPart; }
        }

        /// <summary>
        /// The current animation frame for this dwarf.
        /// </summary>
        public int AnimationFrame
        {
            get { return (int)this.animationFrame; }
        }

        /// <summary>
        /// Gets/sets whether or not this Dwarf is visible. Used by DwarfArmy.
        /// </summary>
        public bool IsVisible
        {
            get;
            set;
        }
        #endregion

        /// <summary>
        /// Constructor for the Dwarf
        /// </summary>
        /// <param name="position"></param>
        /// <param name="skinningData"></param>
        public Dwarf(Vector3 position, InstancedSkinningData skinningData)
        {
            this.animationData = skinningData;

            //Choose random body parts - having the same dwarf everywhere would be boring!
            this.bodyPart = BodyParts[random.Next(0, 3)];
            this.legPart = LegParts[random.Next(0, 3)];
            this.headPart = HeadParts[random.Next(0, 3)];
            
            //Mess with the scale some - dwarves come in all shapes and sizes!
            float scale = (float)(random.NextDouble() * 0.4) + 0.8f;
            this.translation = position;
            
            //Precompute the matrix for translation and scale - rotation will be done dynamically
            this.translationScaleMatrix = Matrix.CreateScale(scale) * Matrix.CreateTranslation(this.translation);
            
            //Create the bounding sphere for this dwarf
            this.boundingSphere = new BoundingSphere(translation, 1f);
        }
    
        /// <summary>
        /// Update the animation and behavior of this dwarf
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="playerPosition"></param>
        public void Update(GameTime gameTime, ref Vector3 playerPosition)
        {
            //If we don't have an animation, we should probably pick a new one out
            if (this.currentAnimation == null)
            {
                this.currentAnimation = this.GetNextAnimation(ref playerPosition);
                this.animationFrame = this.currentAnimation.StartRow;
            }

            //Otherwise, increment the animation frame on our current animation
            if (this.currentAnimation != null)
            {
                this.animationFrame += (float)(gameTime.ElapsedGameTime.TotalSeconds * this.currentAnimation.FrameRate);

                if ((int)this.animationFrame >= this.currentAnimation.EndRow)
                {
                    //If we still need to repeat some, repeat
                    if (this.repeatAnimation > 0)
                    {
                        this.repeatAnimation--;
                        this.animationFrame = this.currentAnimation.StartRow;
                    }
                    else
                    {
                        this.animationFrame = this.currentAnimation.EndRow;
                        //Time to switch animation
                        this.currentAnimation = null;
                    }
                }
            }

            //Rotate the dwarf to face the player
            float zDelta = playerPosition.Z - this.translation.Z;
            float xDelta = playerPosition.X - this.translation.X;
            float rotation = (float)(Math.Atan2(xDelta, zDelta));

            //Create the rotation matrix based on the rotation we calculated
            Matrix rotationMatrix;
            Matrix.CreateRotationY(rotation, out rotationMatrix);

            //Calculate our final transform matrix for the dwarf
            Matrix.Multiply(ref rotationMatrix, ref this.translationScaleMatrix, out this.transform); 
        }

        /// <summary>
        /// This function decides what animation we want to do next!
        /// </summary>
        /// <returns></returns>
        private InstancedAnimationClip GetNextAnimation( ref Vector3 playerPosition)
        {
            //Otherwise, we're going to pick an animation based on the players proximity
            //If the player is really close, we'll play the JumpCheer
            //If he's sort of close, we'll play one of the Cheers 
            //If he's far, we'll play an Idle

            float distanceSquared;
            Vector3.DistanceSquared(ref playerPosition, ref this.translation, out distanceSquared);

            //If we're close, then play the jump cheer animation. We also give a small random chance
            //of playing it anyway, because some dwarves may be extra peppy
            if (distanceSquared < 15000f || random.NextDouble() < 0.05)
            {
                this.repeatAnimation = 1;
                return animationData.Animations["JumpCheer"];
            }
            //Play a cheer if we're sort of close.
            else if (distanceSquared < 50000f || random.NextDouble() < 0.05)
            {
                this.repeatAnimation = 1;
                int anim = random.Next(1, 4);
                switch (anim)
                {
                    case 1:
                        return animationData.Animations["Cheer1"];
                    case 2:
                        return animationData.Animations["Cheer2"];
                    case 3:
                        return animationData.Animations["Cheer3"];

                }
            }
            //Otherwise, we'll probably play an idle animation
            else
            {
                //For idle animations, they are very short, so we'll repeat them some
                this.repeatAnimation = random.Next(5, 10);

                int anim = random.Next(1, 3);
                switch (anim)
                {
                    case 1:
                        return animationData.Animations["Idle1"];
                    case 2:
                        return animationData.Animations["Idle2"];
                }
            }

            return null;
        }
    }
}