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
using System.IO;

using SkinnedModelInstancing;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Storage;
#endregion

namespace InstancedSkinningSample
{
    /// <summary>
    /// The DwarfArmy class is responsible for rendering a collection of dwarves
    /// </summary>
    public class DwarfArmy
    {
        /// <summary>
        /// The data we will use to privately keep track of instances
        /// </summary>
        private class InstancingData
        {
            public Matrix[] transforms;
            public int[] animations;
            public int currentIndex;
        }


        private InstancedSkinnedModel dwarfModel;
        private int numInstances = 1000;
        private int visibleInstances;

        private List<Dwarf> dwarves = new List<Dwarf>(); //list of the dwarves we are managing

        private int [] meshPartCount; //this keeps the count of how many dwarves are using a particular body part
        private List<InstancingData> meshInstances = new List<InstancingData>(); //list of instance data

        #region Properties
        /// <summary>
        /// Gets/sets the total number of dwarf instances
        /// </summary>
        public int InstanceCount
        {
            get { return this.numInstances; }
            set 
            {
                if (value > dwarves.Count)
                    this.numInstances = dwarves.Count;
                else if (value < 0)
                    this.numInstances = 0;
                else
                    this.numInstances = value;        
            }
        }

        /// <summary>
        /// Gets the total number of visible instances
        /// </summary>
        public int VisibleInstances
        {
            get { return this.visibleInstances; }
        }
        #endregion 

        public DwarfArmy(InstancedSkinnedModel model)
        {
            this.dwarfModel = model;
            this.meshPartCount = new int[this.dwarfModel.Meshes.Count];

            for (int i = 0; i < this.dwarfModel.Meshes.Count; i++)
                this.meshInstances.Add(new InstancingData());

            // Read the dwarf positions from the Positions.txt file
            FileStream fs = new FileStream(Path.Combine(StorageContainer.TitleLocation, "Data\\Positions.txt"), FileMode.Open, FileAccess.Read);
            StreamReader sr = new StreamReader(fs);
            while (!sr.EndOfStream)
            {
                string sz = sr.ReadLine();
                string[] items = sz.Split('{', ',', '}');
                Matrix transform = CreateMatrixFromSplitString(items);
                Dwarf dwarf = new Dwarf(transform.Translation, this.dwarfModel.SkinningData);
                this.dwarves.Add(dwarf);

            }

        }

        /// <summary>
        /// Updates all visible dwarves
        /// </summary>
        /// <param name="gameTime"></param>
        /// <param name="position"></param>
        public void Update(GameTime gameTime, Vector3 position)
        {
            for (int i = 0; i < this.numInstances; i++)
            {
                Dwarf dwarf = this.dwarves[i];

                // We only update visible dwarves, for efficiency. This is acceptable for this particular simulation.
                // However for other types of games/simulation it may not be - you might need other strategies to cut
                // down the CPU overhead

                if (dwarf.IsVisible)
                {
                    dwarf.Update(gameTime, ref position);
                }
            }
        }

        /// <summary>
        /// Draws all the dwarves
        /// </summary>
        /// <param name="view"></param>
        /// <param name="projection"></param>
        public void Draw(Matrix view, Matrix projection)
        {
            this.RefreshMeshCounts(new BoundingFrustum(view * projection));

            this.visibleInstances = 0;

            // Reset the current index of each instance, so we know where to put instance data
            for (int i = 0; i < this.meshInstances.Count; i++)
                this.meshInstances[i].currentIndex = 0;

            for (int i = 0; i < this.numInstances; i++)
            {
                Dwarf dwarf = dwarves[i];
                if (dwarf.IsVisible)
                {
                    this.visibleInstances++;
                    // Get the current index each part is at
                    int currentBodyIndex = this.meshInstances[dwarf.BodyPart].currentIndex;
                    this.meshInstances[dwarf.BodyPart].currentIndex++;

                    int currentHeadIndex = this.meshInstances[dwarf.HeadPart].currentIndex;
                    this.meshInstances[dwarf.HeadPart].currentIndex++;

                    int currentLegIndex = this.meshInstances[dwarf.LegPart].currentIndex;
                    this.meshInstances[dwarf.LegPart].currentIndex++;

                    // Assign transform data
                    this.meshInstances[dwarf.BodyPart].transforms[currentBodyIndex] = dwarf.Transform;
                    this.meshInstances[dwarf.HeadPart].transforms[currentHeadIndex] = dwarf.Transform;
                    this.meshInstances[dwarf.LegPart].transforms[currentLegIndex] = dwarf.Transform;

                    // Assign animation data
                    this.meshInstances[dwarf.BodyPart].animations[currentBodyIndex] = dwarf.AnimationFrame;
                    this.meshInstances[dwarf.HeadPart].animations[currentHeadIndex] = dwarf.AnimationFrame;
                    this.meshInstances[dwarf.LegPart].animations[currentLegIndex] = dwarf.AnimationFrame;
                }
                

            }
 
            // Now that we have our data, do the actual drawing!
            for (int i = 0; i < this.meshInstances.Count; i++)
            {
                InstancingData instance = this.meshInstances[i];
                if (instance.currentIndex > 0)
                    this.dwarfModel.Meshes[i].Draw(instance.transforms, instance.animations, view, projection);
            }
        }

        /// <summary>
        /// Get the total number of visible dwarves, and resize our instance arrays accordingly
        /// </summary>
        /// <param name="frustum"></param>
        private void RefreshMeshCounts(BoundingFrustum frustum)
        {
            // Reset the counts of each part to 0
            for (int i = 0; i < this.meshPartCount.Length; i++)
                this.meshPartCount[i] = 0;

            // Loop through, and see if they are visible
            // If the dwarf is visible, make sure we add to the body part counters
            for (int i = 0; i < this.numInstances; i++)
            {

                Dwarf dwarf = this.dwarves[i];
                bool intersects = true;
                frustum.Intersects(ref dwarf.boundingSphere, out intersects);
                if (intersects)
                {
                    this.meshPartCount[dwarf.HeadPart]++;
                    this.meshPartCount[dwarf.BodyPart]++;
                    this.meshPartCount[dwarf.LegPart]++;
                }
                dwarf.IsVisible = intersects;
            }

            // Resize all the arrays accordingly
            for (int i = 0; i < this.meshInstances.Count; i++)
            {
                Array.Resize(ref this.meshInstances[i].transforms, meshPartCount[i]);
                Array.Resize(ref this.meshInstances[i].animations, meshPartCount[i]);
            }
        }

        /// <summary>
        /// Helper function create a transform matrix from an array of strings
        /// </summary>
        /// <param name="sz"></param>
        /// <returns></returns>
        private static Matrix CreateMatrixFromSplitString(string[] sz)
        {
            float[] vals = new float[16];

            int currentIndex = 0;
            for (int i = 0; i < sz.Length; i++)
            {
                if (!string.IsNullOrEmpty(sz[i]))
                {
                    vals[currentIndex] = float.Parse(sz[i]);
                    currentIndex++;
                }
            }

            // Actually we only care about the translation... kind of sloppy :-)
            Vector3 translation = new Vector3(vals[12], vals[13], vals[14]);

            // Apply a rotation so the dwarves are properly placed in the world
            Matrix rot = Matrix.CreateRotationX(-MathHelper.PiOver2);
            Vector3 newPos = Vector3.Transform(translation, rot);

            // Return a final translation matrix
            return Matrix.CreateTranslation(newPos);
        }

    }
}