using System;
using System.Collections.Generic;
using System.Linq;
using ChimeralTheater.Films;
using NUnit.Framework;
using UnityEngine;

namespace ChimeralTheater.Films.Tests
{
    public class FilmDirectorTests
    {
        FilmDirector director;
        readonly List<GameObject> spawned = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            ActStore.ClearAll();
            var host = new GameObject("director");
            spawned.Add(host);
            director = host.AddComponent<FilmDirector>();
            director.Configure(null, null, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            ActStore.ClearAll();
            foreach (GameObject go in spawned)
                UnityEngine.Object.DestroyImmediate(go);
        }

        GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }

        static Act RecordFramesInto(Act act, int frames, float interval)
        {
            var times = new List<float>(frames);
            var poses = new List<float>(frames * Act.FloatsPerFrame);
            for (int i = 0; i < frames; i++)
            {
                times.Add(i * interval);
                for (int p = 0; p < Act.FloatsPerFrame; p++)
                    poses.Add(i);
            }

            act.SetFrames(times, poses);
            return act;
        }

        /// <summary>Runs the director's state machine through the given seconds without any rig, so no
        /// frames are captured and recordings come out empty.</summary>
        void Advance(float seconds, float step = 0.05f)
        {
            for (float t = 0f; t < seconds; t += step)
                director.Tick(step);
        }

        [Test]
        public void TakeAndActNumbersIterateAndAreNotReused()
        {
            Take first = director.AddTake();
            Assert.AreEqual(1, first.Number);

            Take second = director.AddTake();
            Assert.AreEqual(2, second.Number);

            director.OpenTakeView(first);
            Act act1 = director.AddAct();
            Act act2 = director.AddAct();
            Assert.AreEqual(1, act1.Number);
            Assert.AreEqual(2, act2.Number);

            director.RemoveAct(act1);
            Act act3 = director.AddAct();
            Assert.AreEqual(3, act3.Number, "Removed act numbers must not be reused.");
        }

        [Test]
        public void TakingIsBlockedWhileBusy()
        {
            director.AddTake();
            director.OpenTakeView(director.Takes[0]);
            director.SelectAct(director.AddAct());
            director.StartRecording();

            Assert.Throws<InvalidOperationException>(() => director.AddTake());
            Assert.Throws<InvalidOperationException>(() => director.AddAct());
            Assert.Throws<InvalidOperationException>(() => director.PlayTake());

            director.StopRecording(); // cancels: nothing was captured
            Assert.AreEqual(DirectorState.Idle, director.State);
        }

        [Test]
        public void RecordingRequiresSelectedAct()
        {
            Assert.Throws<InvalidOperationException>(() => director.StartRecording());

            director.AddTake();
            director.OpenTakeView(director.Takes[0]);
            Assert.Throws<InvalidOperationException>(() => director.StartRecording());
        }

        [Test]
        public void CountdownRunsThreeSecondsThenRecords()
        {
            director.AddTake();
            director.OpenTakeView(director.Takes[0]);
            director.SelectAct(director.AddAct());
            director.StartRecording();

            Assert.AreEqual(DirectorState.Countdown, director.State);
            Assert.AreEqual(FilmDirector.CountdownDuration, director.CountdownRemaining);

            director.Tick(1f);
            Assert.AreEqual(DirectorState.Countdown, director.State);

            director.Tick(1f);
            director.Tick(1f);
            Assert.AreEqual(DirectorState.Recording, director.State);
            Assert.AreEqual(0f, director.SessionTime, 1e-5f, "The session clock starts at zero after the countdown.");
        }

        [Test]
        public void StopDuringCountdownCancelsWithoutSaving()
        {
            director.AddTake();
            director.OpenTakeView(director.Takes[0]);
            Act act = director.AddAct();
            director.SelectAct(act);
            director.StartRecording();
            director.Tick(1f);

            director.StopRecording();

            Assert.AreEqual(DirectorState.Idle, director.State);
            Assert.IsTrue(act.IsEmpty, "A cancelled recording must leave the act empty.");
        }

        [Test]
        public void StopRecordingSavesAndReleasesFrames()
        {
            director.AddTake();
            Take take = director.Takes[0];
            director.OpenTakeView(take);
            Act act = director.AddAct();
            director.SelectAct(act);
            director.StartRecording();
            Advance(3f); // countdown
            Advance(0.5f); // record; no rig configured so every pose is identity

            director.StopRecording();

            Assert.IsFalse(act.IsEmpty, "Identity frames are still a recording.");
            Assert.IsFalse(act.HasFramesLoaded, "Saving must release the frames from memory.");
            Assert.IsTrue(act.Duration > 0f);
        }

        [Test]
        public void RecordingStagesOtherRecordedActs()
        {
            director.AddTake();
            Take take = director.Takes[0];
            director.OpenTakeView(take);
            Act first = RecordFramesInto(director.AddAct(), 30, ActRecorder.TickInterval);
            ActStore.Save(take, first); // a recorded act lives on disk, frames released
            director.SelectAct(director.AddAct());
            director.StartRecording();

            Assert.AreEqual(1, director.StandInCount, "The previously recorded act should be on stage.");

            director.Tick(0.5f);
            director.StopRecording();
            Assert.AreEqual(0, director.StandInCount);
        }

        [Test]
        public void PlaybackEndsWhenLongestActFinishes()
        {
            director.AddTake();
            Take take = director.Takes[0];
            director.OpenTakeView(take);
            ActStore.Save(take, RecordFramesInto(director.AddAct(), 13, ActRecorder.TickInterval)); // 0.1s
            ActStore.Save(take, RecordFramesInto(director.AddAct(), 121, ActRecorder.TickInterval)); // 1.0s

            director.PlayTake();
            Assert.AreEqual(DirectorState.Playing, director.State);
            Assert.AreEqual(2, director.StandInCount);

            director.Tick(0.11f);
            Assert.AreEqual(DirectorState.Playing, director.State, "The take keeps going while the longest act runs.");

            director.Tick(1f);
            Assert.AreEqual(DirectorState.Idle, director.State);
            Assert.AreEqual(0, director.StandInCount);

            // Finished takes can be played again.
            director.PlayTake();
            Assert.AreEqual(DirectorState.Playing, director.State);
        }

        [Test]
        public void PlayRequiresRecordedActAndOpenTake()
        {
            Assert.Throws<InvalidOperationException>(() => director.PlayTake());

            director.AddTake();
            director.OpenTakeView(director.Takes[0]);
            Assert.Throws<InvalidOperationException>(() => director.PlayTake(), "An empty act list cannot play.");
        }
    }
}
