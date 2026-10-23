using System.Collections.Generic;
using ChimeralTheater.Films;
using NUnit.Framework;
using UnityEngine;

namespace ChimeralTheater.Films.Tests
{
    public class ActRecorderTests
    {
        static Transform CreateBox(string name, Vector3 position)
        {
            var box = new GameObject(name).transform;
            box.position = position;
            return box;
        }

        [TestCase(90f)]
        [TestCase(120f)]
        [TestCase(144f)]
        [TestCase(240f)]
        public void TickGridStaysOnTicksPerSecond(float frameRate)
        {
            var recorder = new ActRecorder();
            float delta = 1f / frameRate;

            // Exactly one second of frames, whatever the frame rate is.
            for (int frame = 0; frame < (int)frameRate; frame++)
                recorder.Tick(delta, null, null, null, null);

            var act = new Act();
            recorder.WriteTo(act);

            // The grid points from 0s to 1s inclusive: 121 ticks, each written from its nearest frame.
            Assert.AreEqual(121, act.FrameCount);
            for (int i = 0; i < act.FrameCount; i++)
                Assert.AreEqual(i * ActRecorder.TickInterval, act.Times[i], 1e-5f);
        }

        [Test]
        public void FirstTickIsAtZero()
        {
            var recorder = new ActRecorder();
            recorder.Tick(0f, null, null, null, null);
            recorder.Tick(0.004f, null, null, null, null);

            var act = new Act();
            recorder.WriteTo(act);

            // 4ms is less than half a tick away from tick 0, so tick 1 is not reached yet.
            Assert.AreEqual(1, act.FrameCount);
            Assert.AreEqual(0f, act.Times[0]);

            recorder.Tick(0.005f, null, null, null, null);
            recorder.WriteTo(act);

            Assert.AreEqual(2, act.FrameCount);
            Assert.AreEqual(ActRecorder.TickInterval, act.Times[1], 1e-5f);
        }

        [Test]
        public void DroppedFramesDoNotShiftTheGrid()
        {
            var recorder = new ActRecorder();

            recorder.Tick(0.008f, null, null, null, null);   // first frame already covers ticks 0 and 1
            recorder.Tick(0.4f, null, null, null, null);     // long hitch fills the rest of the grid

            var act = new Act();
            recorder.WriteTo(act);

            Assert.AreEqual(50, act.FrameCount);
            for (int i = 0; i < act.FrameCount; i++)
                Assert.AreEqual(i * ActRecorder.TickInterval, act.Times[i], 1e-5f);
        }

        [Test]
        public void WriteToCopiesFramesAndResetClears()
        {
            var recorder = new ActRecorder();
            var body = CreateBox("body", new Vector3(1f, 0f, 0f));
            recorder.Tick(ActRecorder.TickInterval, body, null, null, null);

            var act = new Act();
            recorder.WriteTo(act);
            recorder.Reset();

            Assert.AreEqual(2, act.FrameCount);
            Assert.AreEqual(0, recorder.FrameCount);
            Assert.AreEqual(ActRecorder.TickInterval, act.Duration, 1e-5f);
            Assert.IsTrue(act.HasFramesLoaded);

            Object.DestroyImmediate(body.gameObject);
        }

        [Test]
        public void PosesFollowMovingTransforms()
        {
            var body = CreateBox("body", Vector3.zero);
            var recorder = new ActRecorder();

            recorder.Tick(0f, body, null, null, null);
            body.position = new Vector3(5f, 0f, 0f);
            recorder.Tick(ActRecorder.TickInterval, body, null, null, null);

            var act = new Act();
            recorder.WriteTo(act);

            Assert.AreEqual(0f, act.Poses[0], 1e-6f);                    // frame 0 body x
            Assert.AreEqual(5f, act.Poses[Act.FloatsPerFrame], 1e-6f);  // frame 1 body x

            Object.DestroyImmediate(body.gameObject);
        }
    }
}
