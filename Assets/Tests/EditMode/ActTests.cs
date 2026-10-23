using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace ChimeralTheater.Films.Tests
{
    internal static class TestActs
    {
        /// <summary>An act whose frames move along +X: time t has the body at x = t * 10 and the head at x = t * 100.</summary>
        public static Act Create(params float[] times)
        {
            var timeList = new List<float>();
            var poseList = new List<float>();
            foreach (float time in times)
            {
                timeList.Add(time);
                var frame = new ActFrame
                {
                    Body = new ActPose(new Vector3(time * 10f, 0f, 0f), Quaternion.identity),
                    Head = new ActPose(new Vector3(time * 100f, 1f, 0f), Quaternion.Euler(0f, time * 90f, 0f)),
                    LeftHand = new ActPose(new Vector3(-1f, time, 0f), Quaternion.identity),
                    RightHand = new ActPose(new Vector3(1f, time, 0f), Quaternion.identity),
                };
                frame.AppendTo(poseList);
            }

            var act = new Act { Number = 1 };
            act.SetFrames(timeList, poseList);
            return act;
        }
    }

    public class ActTests
    {
        [Test]
        public void NewAct_IsEmpty()
        {
            Assert.IsTrue(new Act().IsEmpty);
        }

        [Test]
        public void SetFrames_FillsCountAndDuration()
        {
            Act act = TestActs.Create(0f, 0.5f, 1.25f);

            Assert.IsFalse(act.IsEmpty);
            Assert.AreEqual(3, act.FrameCount);
            Assert.AreEqual(1.25f, act.Duration, 1e-6f);
            Assert.AreEqual(3 * Act.FloatsPerFrame, act.Poses.Length);
        }

        [Test]
        public void SetFrames_RejectsMismatchedPoseData()
        {
            var act = new Act();
            Assert.Throws<System.ArgumentException>(() => act.SetFrames(new List<float> { 0f, 1f }, new List<float>()));
        }

        [Test]
        public void TrySample_AtAFrameReturnsThatFrame()
        {
            Act act = TestActs.Create(0f, 1f, 2f);

            Assert.IsTrue(act.TrySample(1f, out ActFrame frame));
            Assert.AreEqual(10f, frame.Body.Position.x, 1e-4f);
        }

        [Test]
        public void TrySample_BetweenFramesInterpolatesEveryPose()
        {
            Act act = TestActs.Create(0f, 1f);

            Assert.IsTrue(act.TrySample(0.25f, out ActFrame frame));
            Assert.AreEqual(2.5f, frame.Body.Position.x, 1e-4f);
            Assert.AreEqual(25f, frame.Head.Position.x, 1e-4f);
            Assert.AreEqual(0.25f, frame.LeftHand.Position.y, 1e-4f);
            Assert.AreEqual(22.5f, frame.Head.Rotation.eulerAngles.y, 1e-2f);
        }

        [Test]
        public void TrySample_UsesUnevenFrameSpacing()
        {
            Act act = TestActs.Create(0f, 0.1f, 1f);

            Assert.IsTrue(act.TrySample(0.55f, out ActFrame frame));
            Assert.AreEqual(5.5f, frame.Body.Position.x, 1e-3f);
        }

        [Test]
        public void TrySample_BeforeTheFirstFrameHoldsTheFirstFrame()
        {
            Act act = TestActs.Create(0.5f, 1f);

            Assert.IsTrue(act.TrySample(0f, out ActFrame frame));
            Assert.AreEqual(5f, frame.Body.Position.x, 1e-4f);
        }

        [Test]
        public void TrySample_AtTheEndHoldsTheLastFrame()
        {
            Act act = TestActs.Create(0f, 1f);

            Assert.IsTrue(act.TrySample(1f, out ActFrame frame));
            Assert.AreEqual(10f, frame.Body.Position.x, 1e-4f);
        }

        [Test]
        public void TrySample_AfterTheEndIsFalse()
        {
            Act act = TestActs.Create(0f, 1f);

            Assert.IsFalse(act.TrySample(1.001f, out _));
        }

        [Test]
        public void TrySample_WithoutLoadedFramesIsFalse()
        {
            Act act = TestActs.Create(0f, 1f);
            act.ReleaseFrames();

            Assert.IsFalse(act.TrySample(0.5f, out _));
            Assert.AreEqual(2, act.FrameCount, "The count survives releasing the frames.");
            Assert.AreEqual(1f, act.Duration, 1e-6f);
        }

        [Test]
        public void TrySample_SingleFrameActHoldsThatFrame()
        {
            Act act = TestActs.Create(0f);

            Assert.IsTrue(act.TrySample(0f, out ActFrame frame));
            Assert.AreEqual(0f, frame.Body.Position.x, 1e-6f);
        }
    }
}
