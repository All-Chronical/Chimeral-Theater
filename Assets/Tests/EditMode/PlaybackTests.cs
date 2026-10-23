using System.Collections.Generic;
using ChimeralTheater.Films;
using NUnit.Framework;
using UnityEngine;

namespace ChimeralTheater.Films.Tests
{
    public class PlaybackTests
    {
        static Act CreateLinearAct(int frames, float interval)
        {
            var times = new List<float>(frames);
            var poses = new List<float>(frames * Act.FloatsPerFrame);
            var body = new GameObject("body").transform;

            for (int i = 0; i < frames; i++)
            {
                times.Add(i * interval);
                body.position = new Vector3(i, 0f, 0f);
                ActFrame.Capture(body, null, null, null).AppendTo(poses);
            }

            var act = new Act();
            act.SetFrames(times, poses);
            Object.DestroyImmediate(body.gameObject);
            return act;
        }

        [Test]
        public void SampleInterpolatesBetweenFrames()
        {
            Act act = CreateLinearAct(3, 0.5f);

            Assert.IsTrue(act.TrySample(0.25f, out ActFrame frame));
            Assert.AreEqual(0.5f, frame.Body.Position.x, 1e-4f);
        }

        [Test]
        public void SampleEndsAtLastFrameTime()
        {
            Act act = CreateLinearAct(3, 0.5f);

            Assert.IsTrue(act.TrySample(act.Duration, out _));
            Assert.IsFalse(act.TrySample(act.Duration + 0.01f, out _));
        }

        [Test]
        public void StandInFollowsSessionClockAndHidesAtEnd()
        {
            Act act = CreateLinearAct(3, 0.5f);
            using (var playback = new ActPlaybackOwner(act))
            {
                playback.Evaluate(0f);
                Assert.IsTrue(playback.AvatarRoot.gameObject.activeSelf);

                playback.Evaluate(0.25f);
                Assert.AreEqual(0.5f, playback.AvatarRoot.position.x, 1e-4f);

                playback.Evaluate(act.Duration + 0.01f);
                Assert.IsFalse(playback.AvatarRoot.gameObject.activeSelf);
            }
        }

        /// <summary>Wraps an ActPlayback so the stand-in is destroyed even when an assertion fails.</summary>
        sealed class ActPlaybackOwner : System.IDisposable
        {
            public readonly ActPlayback Playback;
            public Transform AvatarRoot => Playback.Avatar.Root;

            public ActPlaybackOwner(Act act)
            {
                Playback = new ActPlayback(act);
            }

            public void Evaluate(float time) => Playback.Evaluate(time);

            public void Dispose() => Playback.Dispose();
        }
    }
}
