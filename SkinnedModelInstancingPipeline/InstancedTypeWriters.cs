#region File Description
//-----------------------------------------------------------------------------
// Ionixx Games 3/9/2009
// Copyright (C) Bryan Phelps. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using SkinnedModelInstancing;
#endregion

namespace SkinnedModelInstancingPipeline
{

    /// <summary>
    /// Writes the instanced skinned model
    /// </summary>
    [ContentTypeWriter]
    public class InstancedSkinnedModelWriter : ContentTypeWriter<InstancedSkinnedModelContent>
    {
        protected override void Write(ContentWriter output, InstancedSkinnedModelContent value)
        {
            value.Write(output);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(InstancedModelReader).AssemblyQualifiedName;
        }
    }

    /// <summary>
    /// Writes the instanced animation clip data
    /// </summary>
    [ContentTypeWriter]
    public class InstancedAnimationClipWriter : ContentTypeWriter<InstancedAnimationClip>
    {
        protected override void Write(ContentWriter output, InstancedAnimationClip value)
        {
            output.WriteObject(value.Duration);
            output.WriteObject(value.StartRow);
            output.WriteObject(value.EndRow);
            output.WriteObject(value.FrameRate);
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(InstancedAnimationClipReader).AssemblyQualifiedName;
        }
    }



}
