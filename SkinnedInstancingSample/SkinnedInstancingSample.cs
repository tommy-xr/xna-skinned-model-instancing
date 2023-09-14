#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using SkinnedModelInstancing;

using System.IO;
#endregion

namespace InstancedSkinningSample
{
    /// <summary>
    /// Sample game showing how to display skinned character animation.
    /// </summary>
    public class SkinnedInstancingSample : Microsoft.Xna.Framework.Game
    {
        #region Fields
        static readonly Vector3 startPosition = new Vector3(0f, 20f, 0f);


        private GraphicsDeviceManager graphics;

        private GamePadState currentGamePadState = new GamePadState();

        private Model arenaModel;
        private InstancedSkinnedModel skinnedModel;

        private float cameraRotationX = 0;
        private float cameraRotationY = 0;
        private Vector3 cameraPosition = startPosition;

        private DwarfArmy dwarfArmy;

        //Framerate measuring
        private int frameCounter;
        private TimeSpan elapsedTime;
        private int frameRate;

        //Text rendering
        private SpriteFont font;
        private SpriteBatch spriteBatch;

        #endregion

        #region Initialization
        public SkinnedInstancingSample()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 1280;
            graphics.PreferredBackBufferHeight = 720;

            graphics.MinimumVertexShaderProfile = ShaderProfile.VS_2_0;
            graphics.MinimumPixelShaderProfile = ShaderProfile.PS_2_0;

            //We're turning these values off, so we can better measure the performance of the sample
            IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
        }


        /// <summary>
        /// Load your graphics content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load the model.
            //currentModel = Content.Load<Model>("Models\\PlayerMarine");
            skinnedModel = Content.Load<InstancedSkinnedModel>("Models\\dwarf-lod1");
            this.arenaModel = Content.Load<Model>("Arena\\arena-final");

            this.dwarfArmy = new DwarfArmy(skinnedModel);

            this.spriteBatch = new SpriteBatch(this.GraphicsDevice);
            this.font = this.Content.Load<SpriteFont>("Fonts\\Font");

        }



        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows the game to run logic.
        /// </summary>
        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            UpdateCamera(gameTime);

            this.dwarfArmy.Update(gameTime, cameraPosition);

            elapsedTime += gameTime.ElapsedGameTime;

            if (elapsedTime > TimeSpan.FromSeconds(1))
            {
                elapsedTime -= TimeSpan.FromSeconds(1);
                frameRate = frameCounter;
                frameCounter = 0;
            } 


            base.Update(gameTime);
        }


        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice device = graphics.GraphicsDevice;

            device.Clear(Color.CornflowerBlue);

            // Reset render states that spritebatch may have reset
            device.RenderState.DepthBufferEnable = true;
            device.RenderState.AlphaBlendEnable = false;
            device.RenderState.AlphaTestEnable = false;

            float aspectRatio = (float)device.Viewport.Width /
                                (float)device.Viewport.Height;


            Matrix view = Matrix.CreateLookAt(cameraPosition,
                cameraPosition + new Vector3((float)Math.Cos(cameraRotationX), (float)Math.Sin(cameraRotationY), (float)Math.Sin(cameraRotationX)),
                Vector3.Up);

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, 
                                                                    aspectRatio,
                                                                    1,
                                                                    10000);

            // Sort of a hack to get the textures on the arena to display properly.
            // For some reason, spritebatch changes these to clamp.
            GraphicsDevice.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
            GraphicsDevice.SamplerStates[0].AddressV = TextureAddressMode.Wrap;

            // Draw the arena
            foreach (ModelMesh mesh in this.arenaModel.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = Matrix.Identity;
                    effect.View = view;
                    effect.Projection = projection;

                }

                mesh.Draw();
            }

            // Draw the dwarf army
            this.dwarfArmy.Draw(view, projection);

            // Draw the text overlay
            DrawText();

            frameCounter++;

        }

        /// <summary>
        /// Draws text information for the sample
        /// </summary>
        private void DrawText()
        {
            string text = string.Format("Frames per second: {0}\n" +
                            "Total Instances: {1}\n" +
                            "Visible Instances: {2}\n" +
                            "X = Add instances\n" +
                            "Y = Remove instances\n",
                            frameRate,
                            this.dwarfArmy.InstanceCount,
                            this.dwarfArmy.VisibleInstances);

            spriteBatch.Begin();

            spriteBatch.DrawString(font, text, new Vector2(65, 65), Color.Black);
            spriteBatch.DrawString(font, text, new Vector2(64, 64), Color.White);

            spriteBatch.End();
        }

        
        #endregion

        #region Handle Input


        /// <summary>
        /// Handles input for quitting the game.
        /// </summary>
        private void HandleInput()
        {
            currentGamePadState = GamePad.GetState(PlayerIndex.One);

            // Check for exit.
            if (currentGamePadState.Buttons.Back == ButtonState.Pressed)
            {
                Exit();
            }
        }


        /// <summary>
        /// Handles camera input.
        /// </summary>
        private void UpdateCamera(GameTime gameTime)
        {
            float time = (float)gameTime.ElapsedGameTime.TotalMilliseconds;
            float rotationFactor = 0.005f;
            float moveFactor = 0.05f;

            // Implement a basic first person camera
            cameraRotationX += currentGamePadState.ThumbSticks.Right.X * time * rotationFactor;
            cameraRotationY += currentGamePadState.ThumbSticks.Right.Y * time * rotationFactor;

            // Clamp the head rotation
            if (cameraRotationY > 1.4f)
                cameraRotationY = 1.4f;

            if (cameraRotationY < -1.4f)
                cameraRotationY = -1.4f;

            // Figure out our right & forward angle sso we can move properly
            float angle = cameraRotationX;
            float rightAngle = cameraRotationX + MathHelper.PiOver2;
            Vector3 forward = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle));
            Vector3 right = new Vector3((float)Math.Cos(rightAngle), 0f, (float)Math.Sin(rightAngle));
            forward.Normalize();
            right.Normalize();

            cameraPosition += currentGamePadState.ThumbSticks.Left.X * right * time * moveFactor;
            cameraPosition += currentGamePadState.ThumbSticks.Left.Y * forward * time * moveFactor;
            
            // Reset position if they pressed the right stick
            if (currentGamePadState.Buttons.RightStick == ButtonState.Pressed)
            {
                cameraRotationX = 0;
                cameraRotationY = 0;
                cameraPosition = startPosition;
            }

            int instanceChangeRate = Math.Max(this.dwarfArmy.InstanceCount / 100, 1);

            // Increase the number of instances?
            if ( currentGamePadState.Buttons.Y == ButtonState.Pressed)
            {
                    this.dwarfArmy.InstanceCount -= instanceChangeRate;
            }

            // Decrease the number of instances?
            if ( currentGamePadState.Buttons.X == ButtonState.Pressed)
            {
                this.dwarfArmy.InstanceCount += instanceChangeRate;
            }
        }


        #endregion
    }


    #region Entry Point

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static class Program
    {
        static void Main()
        {
            using (SkinnedInstancingSample game = new SkinnedInstancingSample())
            {
                game.Run();
            }
        }
    }

    #endregion
}
