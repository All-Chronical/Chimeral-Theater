using System.IO;
using NUnit.Framework;
using UniRes;

namespace ChimeralTheater.Films.Tests
{
    public class TakeAndStoreTests
    {
        private string previousRoot;
        private string tempRoot;

        [SetUp]
        public void SetUp()
        {
            previousRoot = ResourcePaths.UserDataRoot;
            tempRoot = Path.Combine(Path.GetTempPath(), "ChimeralTheaterTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            ResourcePaths.UserDataRoot = tempRoot;
            ResourceLoader.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            ResourceLoader.ClearCache();
            ResourcePaths.UserDataRoot = previousRoot;
            if (Directory.Exists(tempRoot))
                Directory.Delete(tempRoot, true);
        }

        [Test]
        public void AddAct_NumbersActsInOrder()
        {
            var take = new Take { Number = 1 };

            Act first = take.AddAct();
            Act second = take.AddAct();

            Assert.AreEqual(1, first.Number);
            Assert.AreEqual(2, second.Number);
            Assert.AreEqual("Act 2", second.ResourceName);
            Assert.IsTrue(first.IsEmpty);
        }

        [Test]
        public void AddAct_NeverReusesANumberAfterADelete()
        {
            var take = new Take { Number = 1 };
            take.AddAct();
            Act second = take.AddAct();
            take.Acts.Remove(second);

            Assert.AreEqual(3, take.AddAct().Number);
        }

        [Test]
        public void HasRecordedActs_IsFalseUntilAnActHasFrames()
        {
            var take = new Take { Number = 1 };
            Act act = take.AddAct();
            Assert.IsFalse(take.HasRecordedActs);

            Act recorded = TestActs.Create(0f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));
            Assert.IsTrue(take.HasRecordedActs);
        }

        [Test]
        public void Save_WritesTheFileAndReleasesFrames()
        {
            var take = new Take { Number = 2 };
            Act act = take.AddAct();
            Act recorded = TestActs.Create(0f, 0.5f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));

            ActStore.Save(take, act);

            string savedFile = ResourcePaths.ToFileSystemPath("user://films/take_2/act_1.chact");
            Assert.IsTrue(File.Exists(savedFile));
            byte[] fileBytes = File.ReadAllBytes(savedFile);
            Assert.GreaterOrEqual(fileBytes.Length, 5);
            Assert.AreEqual((byte)'U', fileBytes[0]);
            Assert.AreEqual((byte)'N', fileBytes[1]);
            Assert.AreEqual((byte)'R', fileBytes[2]);
            Assert.AreEqual((byte)'E', fileBytes[3]);
            Assert.AreEqual((byte)'S', fileBytes[4]);

            Assert.IsFalse(act.HasFramesLoaded);
            Assert.AreEqual(3, act.FrameCount);
            Assert.AreEqual("user://films/take_2/act_1.chact", act.ResourcePath);
        }

        [Test]
        public void LoadFrames_RoundTripsTheRecording()
        {
            var take = new Take { Number = 1 };
            Act act = take.AddAct();
            Act recorded = TestActs.Create(0f, 0.25f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));
            ActStore.Save(take, act);

            Act loaded = ActStore.LoadFrames(act);

            Assert.IsNotNull(loaded);
            Assert.AreNotSame(act, loaded);
            Assert.AreEqual(recorded.Times, loaded.Times);
            Assert.AreEqual(recorded.Poses, loaded.Poses);
            Assert.IsFalse(act.HasFramesLoaded, "Loading a copy must not put frames back on the take's act.");
            Assert.IsTrue(loaded.TrySample(0.125f, out ActFrame frame));
            Assert.AreEqual(1.25f, frame.Body.Position.x, 1e-3f);
        }

        [Test]
        public void LoadFrames_OfAnEmptyActIsNull()
        {
            var take = new Take { Number = 1 };

            Assert.IsNull(ActStore.LoadFrames(take.AddAct()));
        }

        [Test]
        public void Save_OverwritesAnEarlierRecording()
        {
            var take = new Take { Number = 1 };
            Act act = take.AddAct();
            Act first = TestActs.Create(0f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(first.Times), new System.Collections.Generic.List<float>(first.Poses));
            ActStore.Save(take, act);

            Act second = TestActs.Create(0f, 2f, 4f);
            act.SetFrames(new System.Collections.Generic.List<float>(second.Times), new System.Collections.Generic.List<float>(second.Poses));
            ActStore.Save(take, act);

            Act loaded = ActStore.LoadFrames(act);
            Assert.AreEqual(3, loaded.FrameCount);
            Assert.AreEqual(4f, loaded.Duration, 1e-6f);
        }

        [Test]
        public void Delete_RemovesTheActFile()
        {
            var take = new Take { Number = 1 };
            Act act = take.AddAct();
            Act recorded = TestActs.Create(0f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));
            ActStore.Save(take, act);
            string file = ResourcePaths.ToFileSystemPath(act.ResourcePath);

            ActStore.Delete(act);

            Assert.IsFalse(File.Exists(file));
        }

        [Test]
        public void Delete_OfAnUnsavedActDoesNothing()
        {
            var take = new Take { Number = 1 };

            Assert.DoesNotThrow(() => ActStore.Delete(take.AddAct()));
        }

        [Test]
        public void DeleteTake_RemovesItsActsAndFolder()
        {
            var take = new Take { Number = 3 };
            Act act = take.AddAct();
            Act recorded = TestActs.Create(0f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));
            ActStore.Save(take, act);

            ActStore.DeleteTake(take);

            Assert.IsFalse(Directory.Exists(ResourcePaths.ToFileSystemPath("user://films/take_3")));
        }

        [Test]
        public void TakeWithSavedActs_RoundTripsAsExternalReferences()
        {
            var take = new Take { Number = 1 };
            Act act = take.AddAct();
            Act recorded = TestActs.Create(0f, 1f);
            act.SetFrames(new System.Collections.Generic.List<float>(recorded.Times), new System.Collections.Generic.List<float>(recorded.Poses));
            ActStore.Save(take, act);
            take.AddAct();

            string text = ResourceSaver.SaveToString(take);
            ResourceLoader.ClearCache();
            Take loaded = ResourceLoader.LoadFromString<Take>(text);

            Assert.AreEqual(2, loaded.Acts.Count);
            StringAssert.Contains("ExtResource", text);
            Assert.AreEqual(2, loaded.Acts[0].FrameCount);
            Assert.IsTrue(loaded.Acts[1].IsEmpty);
            Assert.AreEqual(3, loaded.NextActNumber);
        }
    }
}
