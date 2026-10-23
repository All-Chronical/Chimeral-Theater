using System.Collections.Generic;
using UnityEngine;

namespace ChimeralTheater.Films
{
    /// <summary>A world-space position and rotation.</summary>
    public struct ActPose
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public ActPose(Vector3 position, Quaternion rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public static ActPose Identity => new ActPose(Vector3.zero, Quaternion.identity);

        public static ActPose FromTransform(Transform source)
        {
            return source == null ? Identity : new ActPose(source.position, source.rotation);
        }

        public static ActPose Lerp(ActPose a, ActPose b, float t)
        {
            return new ActPose(Vector3.Lerp(a.Position, b.Position, t), Quaternion.Slerp(a.Rotation, b.Rotation, t));
        }
    }

    /// <summary>
    /// Everything recorded about the performer at one moment: the player body and the
    /// head and both hands, all in world space.
    /// </summary>
    public struct ActFrame
    {
        public ActPose Body;
        public ActPose Head;
        public ActPose LeftHand;
        public ActPose RightHand;

        public static ActFrame Capture(Transform body, Transform head, Transform leftHand, Transform rightHand)
        {
            return new ActFrame
            {
                Body = ActPose.FromTransform(body),
                Head = ActPose.FromTransform(head),
                LeftHand = ActPose.FromTransform(leftHand),
                RightHand = ActPose.FromTransform(rightHand),
            };
        }

        /// <summary>Appends this frame as <see cref="Act.FloatsPerFrame"/> floats: body, head, left, right.</summary>
        public void AppendTo(List<float> poses)
        {
            AppendPose(poses, Body);
            AppendPose(poses, Head);
            AppendPose(poses, LeftHand);
            AppendPose(poses, RightHand);
        }

        /// <summary>Reads the frame at <paramref name="frameIndex"/> from a flat pose array.</summary>
        public static ActFrame ReadFrom(float[] poses, int frameIndex)
        {
            int offset = frameIndex * Act.FloatsPerFrame;
            return new ActFrame
            {
                Body = ReadPose(poses, offset),
                Head = ReadPose(poses, offset + Act.FloatsPerPose),
                LeftHand = ReadPose(poses, offset + Act.FloatsPerPose * 2),
                RightHand = ReadPose(poses, offset + Act.FloatsPerPose * 3),
            };
        }

        public static ActFrame Lerp(ActFrame a, ActFrame b, float t)
        {
            return new ActFrame
            {
                Body = ActPose.Lerp(a.Body, b.Body, t),
                Head = ActPose.Lerp(a.Head, b.Head, t),
                LeftHand = ActPose.Lerp(a.LeftHand, b.LeftHand, t),
                RightHand = ActPose.Lerp(a.RightHand, b.RightHand, t),
            };
        }

        private static void AppendPose(List<float> poses, ActPose pose)
        {
            poses.Add(pose.Position.x);
            poses.Add(pose.Position.y);
            poses.Add(pose.Position.z);
            poses.Add(pose.Rotation.x);
            poses.Add(pose.Rotation.y);
            poses.Add(pose.Rotation.z);
            poses.Add(pose.Rotation.w);
        }

        private static ActPose ReadPose(float[] poses, int offset)
        {
            return new ActPose(
                new Vector3(poses[offset], poses[offset + 1], poses[offset + 2]),
                new Quaternion(poses[offset + 3], poses[offset + 4], poses[offset + 5], poses[offset + 6]));
        }
    }
}
